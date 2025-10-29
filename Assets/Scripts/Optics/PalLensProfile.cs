using UnityEngine;

namespace ProgressiveVrLenses.Optics
{
    [CreateAssetMenu(fileName = "PalLensProfile", menuName = "ProgressiveVR/PAL Lens Profile")]
    public class PalLensProfile : ScriptableObject
    {
        [Header("Generated Maps")]
        public Texture2D RightEyeMap;
        public Texture2D LeftEyeMap;
        public Texture2D RightEyeMagnificationMap;
        public Texture2D LeftEyeMagnificationMap;

        [Header("Meta")]
        public PalProfileParameters SourceParameters;
        public float PixelsPerDegree;
        public float NearReferenceDistanceMeters = 0.4f;
        public float IntermediateReferenceDistanceMeters = 1.5f;
        public float DistanceReferenceDistanceMeters = 6f;

        public bool IsValid => RightEyeMap && LeftEyeMap;
    }
}
