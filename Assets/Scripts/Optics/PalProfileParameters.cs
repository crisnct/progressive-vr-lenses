using System;
using UnityEngine;

namespace ProgressiveVrLenses.Optics
{
    [Serializable]
    public struct EyePrescription
    {
        public float Sph;
        public float Cyl;
        public float Axis;
    }

    public enum CorridorProfile
    {
        Hard,
        Soft,
        Office,
        Sport
    }

    [CreateAssetMenu(fileName = "PalProfileParameters", menuName = "ProgressiveVR/PAL Profile Parameters")]
    public class PalProfileParameters : ScriptableObject
    {
        [Header("Prescription")]
        public EyePrescription RightEye;
        public EyePrescription LeftEye;
        public float Add;

        [Header("Design")]
        public CorridorProfile Corridor;
        public float CorridorLengthMm = 14f;
        public float FittingHeightMm = 18f;
        public float InsetNearMm = 2f;

        [Header("Frame")]
        public float MonoPdRightMm = 31.5f;
        public float MonoPdLeftMm = 31.5f;
        public float PantoscopicTiltDeg = 10f;
        public float WrapAngleDeg = 6f;
        public float VertexDistanceMm = 12f;

        [Header("Headset")]
        public float HeadsetIpdMm = 63f;
        public float HeadsetFovHorizontalDeg = 100f;
        public float HeadsetFovVerticalDeg = 90f;
        public Vector2Int EyeTextureResolution = new(2048, 2048);

        [Header("Runtime")]
        public bool UseEyeTracking;
    }
}
