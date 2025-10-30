using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProgressiveVrLenses.Runtime
{
    /// <summary>
    /// Procedurally builds a lightweight nature scene so the default environment feels alive.
    /// Runs automatically after a scene loads if the environment is missing.
    /// </summary>
    public sealed class NatureEnvironmentBuilder : MonoBehaviour
    {
        public const string EnvironmentRootName = "NatureEnvironment";

        private readonly Dictionary<string, Material> _materialCache = new Dictionary<string, Material>();

        private void Awake()
        {
            BuildEnvironment();
            Destroy(gameObject);
        }

        private void BuildEnvironment()
        {
            if (GameObject.Find(EnvironmentRootName) != null)
                return;

            var root = new GameObject(EnvironmentRootName);
            root.transform.position = Vector3.zero;

            ConfigureLighting();
            CreateGround(root.transform);
            CreateTree(root.transform);
            CreateFlowerField(root.transform);
            CreateClouds(root.transform);
            CreateBeeSwarm(root.transform);
            EnsureDefaultPlayer();
            EnsureRuntimeParameterPanel();
        }

        private void ConfigureLighting()
        {
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var skyMaterial = new Material(skyShader)
                {
                    name = "GeneratedProceduralSky"
                };
                if (skyMaterial.HasProperty("_SkyTint"))
                    skyMaterial.SetColor("_SkyTint", new Color(0.45f, 0.65f, 0.95f));
                if (skyMaterial.HasProperty("_GroundColor"))
                    skyMaterial.SetColor("_GroundColor", new Color(0.25f, 0.35f, 0.2f));
                if (skyMaterial.HasProperty("_AtmosphereThickness"))
                    skyMaterial.SetFloat("_AtmosphereThickness", 0.6f);

                RenderSettings.skybox = skyMaterial;
                DynamicGI.UpdateEnvironment();
            }
            else if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0.45f, 0.65f, 0.95f);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.67f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.52f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.19f);
        }

        private void CreateGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ApplyMaterial(ground, "GroundMat", new Color(0.26f, 0.58f, 0.28f));
        }

        private void CreateTree(Transform parent)
        {
            var treeRoot = new GameObject("Tree");
            treeRoot.transform.SetParent(parent, false);
            treeRoot.transform.localPosition = new Vector3(-3.5f, 0f, 6f);

            var random = new System.Random(182739);

            // Build a trunk composed of stacked segments with slight taper and lean
            var trunkSegments = random.Next(3, 5);
            var currentHeight = 0f;
            var trunkColor = new Color(0.36f, 0.22f, 0.11f);

            for (var i = 0; i < trunkSegments; i++)
            {
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                segment.name = $"TrunkSegment_{i:D2}";
                segment.transform.SetParent(treeRoot.transform, false);
                var radius = Mathf.Lerp(0.55f, 0.35f, i / (float)(trunkSegments - 1));
                var height = 2f + random.NextFloat(-0.15f, 0.25f);
                segment.transform.localScale = new Vector3(radius, height * 0.5f, radius);
                segment.transform.localPosition = new Vector3(
                    random.NextFloat(-0.1f, 0.1f) * (i + 1) * 0.1f,
                    currentHeight + height * 0.5f,
                    random.NextFloat(-0.1f, 0.1f) * (i + 1) * 0.1f);
                segment.transform.localRotation = Quaternion.Euler(random.NextFloat(-2f, 2f), random.NextFloat(-3f, 3f), random.NextFloat(-2f, 2f));
                ApplyMaterial(segment, "TreeTrunk", trunkColor);
                DisableCollider(segment);
                currentHeight += height;
            }

            // Add subtle roots at base
            for (var r = 0; r < 3; r++)
            {
                var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.name = $"Root_{r:D2}";
                root.transform.SetParent(treeRoot.transform, false);
                root.transform.localScale = new Vector3(0.25f, 0.3f, 0.25f);
                var angle = r * 120f + random.NextFloat(-10f, 10f);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                root.transform.localPosition = direction * 0.4f + new Vector3(0f, 0.3f, 0f);
                root.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
                ApplyMaterial(root, "TreeRoot", trunkColor * 0.95f);
                DisableCollider(root);
            }

            // Layered canopy blobs for fuller foliage
            var canopyRoot = new GameObject("Canopy");
            canopyRoot.transform.SetParent(treeRoot.transform, false);
            canopyRoot.transform.localPosition = new Vector3(0f, currentHeight * 0.95f, 0f);

            var baseColor = new Color(0.16f, 0.38f, 0.17f);
            var highlightColor = Color.Lerp(baseColor, new Color(0.35f, 0.65f, 0.3f), 0.4f);

            const int canopyLayers = 3;
            for (var layer = 0; layer < canopyLayers; layer++)
            {
                var spheresInLayer = 3 + layer;
                var layerRadius = Mathf.Lerp(2.2f, 1.1f, layer / (float)(canopyLayers - 1));
                var layerHeight = Mathf.Lerp(3.4f, 1.6f, layer / (float)(canopyLayers - 1));

                for (var i = 0; i < spheresInLayer; i++)
                {
                    var angle = (Mathf.PI * 2f / spheresInLayer) * i + random.NextFloat(-0.25f, 0.25f);
                    var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * layerRadius;
                    offset.y = layerHeight + random.NextFloat(-0.6f, 0.6f);

                    var canopySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    canopySphere.name = $"CanopyLayer_{layer:D2}_Sphere_{i:D2}";
                    canopySphere.transform.SetParent(canopyRoot.transform, false);
                    canopySphere.transform.localPosition = offset;
                    canopySphere.transform.localScale = Vector3.Lerp(new Vector3(4.2f, 3f, 4.2f), new Vector3(2.4f, 1.6f, 2.4f), layer / (float)(canopyLayers - 1));
                    var colorLerp = layer / (float)(canopyLayers - 1);
                    ApplyMaterial(canopySphere, $"TreeCanopy_{layer}_{i}", Color.Lerp(baseColor, highlightColor, colorLerp + random.NextFloat(-0.08f, 0.08f)));
                    DisableCollider(canopySphere);
                }
            }

            // Add small top crown
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(canopyRoot.transform, false);
            crown.transform.localPosition = new Vector3(0f, currentHeight * 0.25f + 4.5f, 0f);
            crown.transform.localScale = new Vector3(1.8f, 1.6f, 1.8f);
            ApplyMaterial(crown, "TreeCrown", highlightColor);
            DisableCollider(crown);
        }

        private void CreateFlowerField(Transform parent)
        {
            var random = new System.Random(42137);
            var flowersRoot = new GameObject("Flowers");
            flowersRoot.transform.SetParent(parent, false);

            const int flowerCount = 18;
            for (var i = 0; i < flowerCount; i++)
            {
                var flower = new GameObject($"Flower_{i:D2}");
                flower.transform.SetParent(flowersRoot.transform, false);
                var position = new Vector3(
                    Mathf.Lerp(-5f, 5.5f, (float)random.NextDouble()),
                    0f,
                    Mathf.Lerp(2f, 10f, (float)random.NextDouble()));
                flower.transform.localPosition = position;

                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(flower.transform, false);
                stem.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
                stem.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                ApplyMaterial(stem, "FlowerStem", new Color(0.18f, 0.45f, 0.21f));
                DisableCollider(stem);

                var blossom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blossom.name = "Blossom";
                blossom.transform.SetParent(flower.transform, false);
                blossom.transform.localScale = new Vector3(0.28f, 0.2f, 0.28f);
                blossom.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                var hue = (float)random.NextDouble();
                var blossomColor = Color.HSVToRGB(hue, 0.65f, 0.9f);
                ApplyMaterial(blossom, $"FlowerBlossom_{i}", blossomColor);
                DisableCollider(blossom);

                for (var p = 0; p < 4; p++)
                {
                    var petal = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    petal.name = $"Petal_{p}";
                    petal.transform.SetParent(flower.transform, false);
                    petal.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
                    petal.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                    petal.transform.localRotation = Quaternion.Euler(0f, p * 90f, 0f);
                    ApplyMaterial(petal, $"FlowerPetal_{i}_{p}", Color.Lerp(blossomColor, Color.white, 0.35f));
                    DisableCollider(petal);
                }
            }
        }

        private void CreateClouds(Transform parent)
        {
            var cloudsRoot = new GameObject("Clouds");
            cloudsRoot.transform.SetParent(parent, false);
            var cloudPositions = new[]
            {
                new Vector3(-6f, 8f, 15f),
                new Vector3(3f, 7.5f, 18f),
                new Vector3(8f, 9f, 12f)
            };

            for (var i = 0; i < cloudPositions.Length; i++)
            {
                var cloud = new GameObject($"Cloud_{i:D2}");
                cloud.transform.SetParent(cloudsRoot.transform, false);
                cloud.transform.localPosition = cloudPositions[i];

                for (var j = 0; j < 3; j++)
                {
                    var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puff.name = $"Puff_{j}";
                    puff.transform.SetParent(cloud.transform, false);
                    var scale = new Vector3(
                        UnityEngine.Random.Range(3f, 5.5f),
                        UnityEngine.Random.Range(1.8f, 2.6f),
                        UnityEngine.Random.Range(2.5f, 4f));
                    puff.transform.localScale = scale;
                    puff.transform.localPosition = new Vector3(j - 1, UnityEngine.Random.Range(-0.2f, 0.3f), UnityEngine.Random.Range(-0.4f, 0.4f));
                    ApplyMaterial(puff, "Cloud", new Color(0.92f, 0.95f, 0.98f));
                    DisableCollider(puff);
                }
            }
        }

        private void CreateBeeSwarm(Transform parent)
        {
            var swarmRoot = new GameObject("Bees");
            swarmRoot.transform.SetParent(parent, false);
            var random = new System.Random(93021);

            const int beeCount = 6;
            for (var i = 0; i < beeCount; i++)
            {
                var bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bee.name = $"Bee_{i:D2}";
                bee.transform.SetParent(swarmRoot.transform, false);
                bee.transform.localScale = Vector3.one * 0.15f;
                ApplyMaterial(bee, "BeeBody", new Color(1f, 0.86f, 0.27f));
                DisableCollider(bee);

                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stripe.name = "Stripe";
                stripe.transform.SetParent(bee.transform, false);
                stripe.transform.localScale = new Vector3(0.9f, 0.03f, 0.9f);
                stripe.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ApplyMaterial(stripe, "BeeStripe", new Color(0.1f, 0.1f, 0.1f));
                DisableCollider(stripe);

                var wings = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wings.name = "Wings";
                wings.transform.SetParent(bee.transform, false);
                wings.transform.localScale = new Vector3(0.35f, 0.25f, 0.35f);
                wings.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                wings.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                ApplyMaterial(wings, "BeeWing", new Color(0.9f, 0.95f, 1f, 0.65f));
                DisableCollider(wings);

                var phase = new Vector2(
                    (float)random.NextDouble() * Mathf.PI * 2f,
                    (float)random.NextDouble() * Mathf.PI * 2f);
                var radius = Mathf.Lerp(1.2f, 2.5f, (float)random.NextDouble());
                var height = Mathf.Lerp(1.4f, 2.2f, (float)random.NextDouble());
                var speed = Mathf.Lerp(0.6f, 1.2f, (float)random.NextDouble());

                var behaviour = bee.AddComponent<BeeBehaviour>();
                behaviour.Initialize(new Vector3(0f, 0.6f, 5f), radius, height, speed, phase);
            }
        }

        private void EnsureDefaultPlayer()
        {
            if (FindObjectOfType<PlayerLocomotionController>() != null)
                return;

            var rig = new GameObject("PlayerRig");
            rig.transform.position = new Vector3(0f, 1.0f, -3.5f);
            rig.transform.rotation = Quaternion.identity;

            var characterController = rig.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            var locomotion = rig.AddComponent<PlayerLocomotionController>();
            locomotion.cameraTransform = cameraObject.transform;

            var lightObject = new GameObject("SunLight");
            lightObject.transform.SetParent(rig.transform, false);
            lightObject.transform.localPosition = new Vector3(-10f, 15f, -10f);
            lightObject.transform.localRotation = Quaternion.Euler(45f, 30f, 0f);
            var directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.1f;
            directional.color = new Color(1f, 0.96f, 0.85f);
        }

        private void EnsureRuntimeParameterPanel()
        {
            var simulation = FindObjectOfType<PalSimulationController>();
            if (simulation == null)
                return;

            if (simulation.GetComponent<PalRuntimeParameterPanel>() != null)
                return;

            simulation.gameObject.AddComponent<PalRuntimeParameterPanel>();
        }

        private void ApplyMaterial(GameObject target, string key, Color color)
        {
            if (target.TryGetComponent<MeshRenderer>(out var meshRenderer))
                meshRenderer.sharedMaterial = GetOrCreateMaterial(key, color);
        }

        private Material GetOrCreateMaterial(string key, Color color)
        {
            if (_materialCache.TryGetValue(key, out var cached))
                return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            material.name = key;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 0.05f);

            if (color.a < 0.99f && material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f); // Transparent for URP Lit

            if (color.a < 0.99f && material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);

            _materialCache[key] = material;
            return material;
        }

        private static void DisableCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }
    }

    internal static class NatureSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureEnvironment()
        {
            if (GameObject.Find(NatureEnvironmentBuilder.EnvironmentRootName) != null)
                return;

            var bootstrap = new GameObject("NatureEnvironmentBootstrap")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            bootstrap.AddComponent<NatureEnvironmentBuilder>();
        }
    }

    internal static class RandomExtensions
    {
        public static float NextFloat(this System.Random random, float min, float max)
        {
            return (float)(random.NextDouble() * (max - min) + min);
        }
    }
}
