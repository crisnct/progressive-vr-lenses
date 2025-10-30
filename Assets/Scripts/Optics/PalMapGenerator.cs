using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace ProgressiveVrLenses.Optics
{
    /// <summary>
    /// Generate PAL maps for each eye based on prescription and mounting parameters.
    /// Maps encode spherical power, cylindrical power and axis in RGBA channels
    /// as well as magnification vector fields.
    /// </summary>
    public class PalMapGenerator
    {
        private readonly PalMapSettings _settings;

        public PalMapGenerator(PalMapSettings settings)
        {
            _settings = settings;
        }

        public PalLensProfile Generate(PalProfileParameters parameters)
        {
            var profile = ScriptableObject.CreateInstance<PalLensProfile>();
            profile.SourceParameters = parameters;
            profile.PixelsPerDegree = CalibratePixelsPerDegree(parameters);

            var mapResolution = _settings.TextureResolution;
            profile.RightEyeMap = CreateMapTexture(mapResolution, "PAL_OD_Map");
            profile.LeftEyeMap = CreateMapTexture(mapResolution, "PAL_OS_Map");
            profile.RightEyeMagnificationMap = CreateMapTexture(mapResolution, "PAL_OD_Mag");
            profile.LeftEyeMagnificationMap = CreateMapTexture(mapResolution, "PAL_OS_Mag");

            using (var rightData = new NativeArray<float4>(mapResolution.x * mapResolution.y, Allocator.TempJob))
            using (var leftData = new NativeArray<float4>(mapResolution.x * mapResolution.y, Allocator.TempJob))
            using (var rightMag = new NativeArray<float4>(mapResolution.x * mapResolution.y, Allocator.TempJob))
            using (var leftMag = new NativeArray<float4>(mapResolution.x * mapResolution.y, Allocator.TempJob))
            {
                FillLensData(parameters.RightEye, parameters, rightData, rightMag, true);
                FillLensData(parameters.LeftEye, parameters, leftData, leftMag, false);

                ApplyToTexture(profile.RightEyeMap, rightData);
                ApplyToTexture(profile.LeftEyeMap, leftData);
                ApplyToTexture(profile.RightEyeMagnificationMap, rightMag);
                ApplyToTexture(profile.LeftEyeMagnificationMap, leftMag);
            }

            return profile;
        }

        private static Texture2D CreateMapTexture(Vector2Int resolution, string name)
        {
            var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBAFloat, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            return texture;
        }

        private float CalibratePixelsPerDegree(PalProfileParameters parameters)
        {
            var res = parameters.EyeTextureResolution;
            var horizontal = res.x / math.max(parameters.HeadsetFovHorizontalDeg, 1f);
            var vertical = res.y / math.max(parameters.HeadsetFovVerticalDeg, 1f);
            return math.min(horizontal, vertical);
        }

        private void FillLensData(EyePrescription eye, PalProfileParameters parameters, NativeArray<float4> buffer, NativeArray<float4> magnification, bool isRightEye)
        {
            var resolution = _settings.TextureResolution;
            var pixelsPerDegree = CalibratePixelsPerDegree(parameters);
            var nearShift = ComputeNearInset(parameters, isRightEye);

            for (var y = 0; y < resolution.y; y++)
            {
                for (var x = 0; x < resolution.x; x++)
                {
                    var uv = new float2((float)x / (resolution.x - 1), (float)y / (resolution.y - 1));
                    var meridianAngle = ComputeMeridianAngle(uv, parameters, isRightEye);
                    var progressivePower = EvaluateProgressivePower(uv, eye, parameters);
                    var cylPower = EvaluateCylPower(uv, eye, parameters);

                    var index = x + y * resolution.x;
                    buffer[index] = new float4(progressivePower, cylPower, meridianAngle, 0f);

                    var magnificationVector = ComputeMagnification(progressivePower, uv, nearShift, pixelsPerDegree);
                    magnification[index] = new float4(magnificationVector, 0f, 0f);
                }
            }
        }

        private float2 ComputeNearInset(PalProfileParameters parameters, bool isRightEye)
        {
            var insetMeters = parameters.InsetNearMm * 0.001f;
            var direction = isRightEye ? -1f : 1f;
            return new float2(direction * insetMeters, -parameters.FittingHeightMm * 0.001f);
        }

        private float EvaluateProgressivePower(float2 uv, EyePrescription eye, PalProfileParameters parameters)
        {
            var lengthScale = math.max(parameters.CorridorLengthMm, 0.01f) / math.max(_settings.ReferenceCorridorLengthMm, 0.01f);
            var normalized = math.saturate(uv.y * lengthScale);
            var corridorCurve = _settings.CorridorProfile.Evaluate(normalized);
            var basePower = eye.Sph + corridorCurve * parameters.Add;
            return basePower;
        }

        private float EvaluateCylPower(float2 uv, EyePrescription eye, PalProfileParameters parameters)
        {
            var lateralWeight = math.pow(math.saturate(math.abs(uv.x - 0.5f) * 2f), _settings.AstigmatismFalloff);
            return eye.Cyl * lateralWeight;
        }

        private float ComputeMeridianAngle(float2 uv, PalProfileParameters parameters, bool isRightEye)
        {
            var baseAxis = isRightEye ? parameters.RightEye.Axis : parameters.LeftEye.Axis;
            var corridorInfluence = math.lerp(0f, _settings.MaxMeridianRotationDeg, uv.y);
            return baseAxis + corridorInfluence;
        }

        private float2 ComputeMagnification(float progressivePower, float2 uv, float2 insetMeters, float pixelsPerDegree)
        {
            var magnification = _settings.MagnificationScale * progressivePower;
            var offset = uv - new float2(0.5f, 0.5f) + insetMeters * pixelsPerDegree * _settings.MetersToDegrees;
            return magnification * offset;
        }

        private void ApplyToTexture(Texture2D texture, NativeArray<float4> data)
        {
            var colors = new Color[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                var sample = data[i];
                colors[i] = new Color(sample.x, sample.y, sample.z, sample.w);
            }

            texture.SetPixels(colors);
            texture.Apply();
        }
    }

    [Serializable]
    public class PalMapSettings
    {
        public Vector2Int TextureResolution = new Vector2Int(512, 512);
        public AnimationCurve CorridorProfile = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float ReferenceCorridorLengthMm = 14f;
        public float AstigmatismFalloff = 2.5f;
        public float MaxMeridianRotationDeg = 20f;
        public float MagnificationScale = 0.01f;
        public float MetersToDegrees = 57.29578f; // 180 / PI
    }
}
