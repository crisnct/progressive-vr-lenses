using System.Collections.Generic;
using ProgressiveVrLenses.Optics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProgressiveVrLenses.Runtime
{
    public class PalSimulationController : MonoBehaviour
    {
        [SerializeField] private PalProfileParameters defaultParameters;
        [SerializeField] private PalMapSettings mapSettings;
        [SerializeField] private ComputeShader palComputeShader;
        [SerializeField] private TextAsset psfBankAsset;

        private PalLensProfile _activeProfile;
        private PalMapGenerator _mapGenerator;
        private PalSimulationRenderPass _renderPass;

        public PalLensProfile ActiveProfile => _activeProfile;
        public PalProfileParameters DefaultParameters => defaultParameters;

        private void Awake()
        {
            _mapGenerator = new PalMapGenerator(mapSettings);
            GenerateDefaultProfile();
            SetupRenderPass();
        }

        public void UpdateProfile(PalProfileParameters parameters)
        {
            var newProfile = _mapGenerator.Generate(parameters);
            var oldProfile = _activeProfile;

            _activeProfile = newProfile;
            if (_renderPass != null)
            {
                _renderPass.SetLensProfile(_activeProfile);
            }

            ReleaseProfile(oldProfile);
        }

        private void GenerateDefaultProfile()
        {
            if (defaultParameters == null)
            {
                Debug.LogWarning("No default PAL parameters assigned; skipping generation.");
                return;
            }

            ReleaseProfile(_activeProfile);
            _activeProfile = _mapGenerator.Generate(defaultParameters);
        }

        private void SetupRenderPass()
        {
            if (palComputeShader == null)
            {
                Debug.LogError("PAL compute shader missing. Assign PalBlur.compute in inspector.");
                return;
            }

            var renderer = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            if (renderer == null)
            {
                Debug.LogWarning("URP asset not active. PAL render pass will not be added.");
                return;
            }

            _renderPass = new PalSimulationRenderPass(palComputeShader, LoadPsfKernels(), _activeProfile);
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private IReadOnlyList<Texture2D> LoadPsfKernels()
        {
            var bank = new List<Texture2D>();
            if (psfBankAsset == null)
            {
                Debug.LogWarning("PSF bank asset missing; using fallback procedural kernels.");
                return bank;
            }

            var psfDescriptor = JsonUtility.FromJson<PsfBankDescriptor>(psfBankAsset.text);
            foreach (var entry in psfDescriptor.entries)
            {
                var texture = new Texture2D(entry.size, entry.size, TextureFormat.RFloat, false)
                {
                    name = $"PSF_{entry.label}",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                texture.SetPixelData(entry.kernelData, 0);
                texture.Apply();
                bank.Add(texture);
            }

            return bank;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_renderPass == null)
                return;

            _renderPass.Enqueue(context, camera);
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _renderPass?.Dispose();
            ReleaseProfile(_activeProfile);
        }

        [System.Serializable]
        private struct PsfBankDescriptor
        {
            public PsfEntry[] entries;
        }

        [System.Serializable]
        private struct PsfEntry
        {
            public string label;
            public int size;
            public float[] kernelData;
        }

        private void ReleaseProfile(PalLensProfile profile)
        {
            if (profile == null)
                return;

            DestroyTexture(profile.RightEyeMap);
            DestroyTexture(profile.LeftEyeMap);
            DestroyTexture(profile.RightEyeMagnificationMap);
            DestroyTexture(profile.LeftEyeMagnificationMap);
            UnityEngine.Object.Destroy(profile);
        }

        private static void DestroyTexture(Texture texture)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
