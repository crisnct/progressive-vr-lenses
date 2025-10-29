using System;
using System.Collections.Generic;
using ProgressiveVrLenses.Optics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace ProgressiveVrLenses.Runtime
{
    public sealed class PalSimulationRenderPass : IDisposable
    {
        private readonly ComputeShader _computeShader;
        private readonly IReadOnlyList<Texture2D> _psfKernels;
        private readonly int _kernelIndex;
        private readonly int _tileSize;

        private PalLensProfile _profile;
        private RenderTexture _palResult;

        private static readonly int SourceTextureId = Shader.PropertyToID("_SourceTexture");
        private static readonly int ResultTextureId = Shader.PropertyToID("_PalResultTexture");
        private static readonly int PalRightMapId = Shader.PropertyToID("_PalRightMap");
        private static readonly int PalLeftMapId = Shader.PropertyToID("_PalLeftMap");
        private static readonly int PalRightMagId = Shader.PropertyToID("_PalRightMagnification");
        private static readonly int PalLeftMagId = Shader.PropertyToID("_PalLeftMagnification");
        private static readonly int EyeIndexId = Shader.PropertyToID("_EyeIndex");
        private static readonly int SourceSizeId = Shader.PropertyToID("_SourceSize");
        private static readonly int PixelsPerDegreeId = Shader.PropertyToID("_PixelsPerDegree");
        private static readonly int NearDistanceId = Shader.PropertyToID("_NearDistance");
        private static readonly int IntermediateDistanceId = Shader.PropertyToID("_IntermediateDistance");
        private static readonly int DistanceId = Shader.PropertyToID("_Distance");

        public PalSimulationRenderPass(ComputeShader computeShader, IReadOnlyList<Texture2D> psfKernels, PalLensProfile profile, int tileSize = 32)
        {
            _computeShader = computeShader;
            _psfKernels = psfKernels;
            _profile = profile;
            _tileSize = tileSize;
            _kernelIndex = _computeShader.FindKernel("CSMain");
        }

        public void SetLensProfile(PalLensProfile profile)
        {
            _profile = profile;
        }

        public void Enqueue(ScriptableRenderContext context, Camera camera)
        {
            if (_profile == null || !_profile.IsValid)
                return;

            EnsureResultTexture(camera);

            var cmd = CommandBufferPool.Get("PAL Simulation");
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex, SourceTextureId, BuiltinRenderTextureType.CameraTarget);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex, ResultTextureId, _palResult);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex, PalRightMapId, _profile.RightEyeMap);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex, PalLeftMapId, _profile.LeftEyeMap);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex, PalRightMagId, _profile.RightEyeMagnificationMap);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex, PalLeftMagId, _profile.LeftEyeMagnificationMap);
            cmd.SetComputeVectorParam(_computeShader, SourceSizeId, new Vector4(_palResult.width, _palResult.height, 0f, 0f));
            cmd.SetComputeFloatParam(_computeShader, PixelsPerDegreeId, _profile.PixelsPerDegree);
            cmd.SetComputeFloatParam(_computeShader, NearDistanceId, _profile.NearReferenceDistanceMeters);
            cmd.SetComputeFloatParam(_computeShader, IntermediateDistanceId, _profile.IntermediateReferenceDistanceMeters);
            cmd.SetComputeFloatParam(_computeShader, DistanceId, _profile.DistanceReferenceDistanceMeters);

            for (var i = 0; i < _psfKernels.Count; i++)
            {
                cmd.SetComputeTextureParam(_computeShader, _kernelIndex, $"_PSF_{i}", _psfKernels[i]);
            }

            var threadGroupsX = Mathf.CeilToInt(_palResult.width / (float)_tileSize);
            var threadGroupsY = Mathf.CeilToInt(_palResult.height / (float)_tileSize);

            cmd.SetComputeIntParam(_computeShader, EyeIndexId, 0);
            cmd.DispatchCompute(_computeShader, _kernelIndex, threadGroupsX, threadGroupsY, 1);

            cmd.Blit(_palResult, BuiltinRenderTextureType.CameraTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void EnsureResultTexture(Camera camera)
        {
            var descriptor = XRGraphics.eyeTextureDesc;
            if (!descriptor.Valid())
            {
                descriptor = camera.targetTexture != null
                    ? camera.targetTexture.descriptor
                    : new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 0);
            }

            if (_palResult != null && _palResult.width == descriptor.width && _palResult.height == descriptor.height)
                return;

            Release();

            _palResult = new RenderTexture(descriptor)
            {
                name = "PAL_Result",
                enableRandomWrite = true
            };
            _palResult.Create();
        }

        public void Dispose()
        {
            Release();
        }

        private void Release()
        {
            if (_palResult == null) return;
            if (_palResult.IsCreated())
                _palResult.Release();
            UnityEngine.Object.Destroy(_palResult);
            _palResult = null;
        }
    }

    internal static class RenderTextureDescriptorExtensions
    {
        public static bool Valid(this RenderTextureDescriptor descriptor)
        {
            return descriptor.width > 0 && descriptor.height > 0;
        }
    }
}
