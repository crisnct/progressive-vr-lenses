using System;
using System.Collections.Generic;
using ProgressiveVrLenses.Optics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProgressiveVrLenses.Runtime
{
    /// <summary>
    /// Builds a lightweight runtime UI that lets users adjust PAL parameters while the experience is running.
    /// </summary>
    public class PalRuntimeParameterPanel : MonoBehaviour
    {
        [Header("Simulation")]
        [Tooltip("Simulation controller to receive runtime parameter updates.")]
        public PalSimulationController simulationController;
        [Tooltip("Automatically search for a PalSimulationController in the scene when none is assigned.")]
        public bool autoFindSimulation = true;

        [Header("UI")]
        public KeyCode toggleKey = KeyCode.F1;
        public Vector2 panelPosition = new Vector2(24f, -24f);
        public Vector2 panelSize = new Vector2(320f, 520f);
        public string panelTitle = "PAL Parameters";

        private PalProfileParameters _runtimeParameters;
        private GameObject _canvasRoot;
        private GameObject _panelRoot;
        private readonly List<SliderBinding> _bindings = new List<SliderBinding>();
        private bool _applying;
        private Font _uiFont;

        private void Start()
        {
            if (simulationController == null && autoFindSimulation)
                simulationController = FindObjectOfType<PalSimulationController>();

            if (simulationController == null)
            {
                Debug.LogWarning("PalRuntimeParameterPanel: could not locate PalSimulationController. Runtime UI disabled.");
                enabled = false;
                return;
            }

            CreateRuntimeParameters();
            BuildUi();
            RefreshUi();
            ApplyParameters();
        }

        private void Update()
        {
            if (_panelRoot != null && Input.GetKeyDown(toggleKey))
            {
                _panelRoot.SetActive(!_panelRoot.activeSelf);
            }
        }

        private void OnDestroy()
        {
            if (_runtimeParameters != null)
            {
                Destroy(_runtimeParameters);
                _runtimeParameters = null;
            }

            if (_canvasRoot != null)
            {
                Destroy(_canvasRoot);
                _canvasRoot = null;
            }
        }

        private void CreateRuntimeParameters()
        {
            PalProfileParameters source = null;
            if (simulationController.ActiveProfile != null && simulationController.ActiveProfile.SourceParameters != null)
                source = simulationController.ActiveProfile.SourceParameters;
            else if (simulationController.DefaultParameters != null)
                source = simulationController.DefaultParameters;

            _runtimeParameters = ScriptableObject.CreateInstance<PalProfileParameters>();
            if (source != null)
            {
                var json = JsonUtility.ToJson(source);
                JsonUtility.FromJsonOverwrite(json, _runtimeParameters);
            }
        }

        private void BuildUi()
        {
            EnsureEventSystem();
            _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _canvasRoot = new GameObject("PalRuntimeCanvas");
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasRoot.AddComponent<CanvasScaler>();
            _canvasRoot.AddComponent<GraphicRaycaster>();

            _panelRoot = CreatePanel(canvas.transform);
            CreateHeader(_panelRoot.transform);

            AddSlider("Right Eye Sph", -8f, 4f, 0.25f,
                delegate { return _runtimeParameters.RightEye.Sph; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.RightEye;
                    eye.Sph = value;
                    _runtimeParameters.RightEye = eye;
                },
                FormatDiopters);

            AddSlider("Right Eye Cyl", -4f, 0f, 0.25f,
                delegate { return _runtimeParameters.RightEye.Cyl; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.RightEye;
                    eye.Cyl = value;
                    _runtimeParameters.RightEye = eye;
                },
                FormatDiopters);

            AddSlider("Right Eye Axis", 0f, 180f, 1f,
                delegate { return _runtimeParameters.RightEye.Axis; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.RightEye;
                    eye.Axis = value;
                    _runtimeParameters.RightEye = eye;
                },
                FormatAngle);

            AddSlider("Left Eye Sph", -8f, 4f, 0.25f,
                delegate { return _runtimeParameters.LeftEye.Sph; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.LeftEye;
                    eye.Sph = value;
                    _runtimeParameters.LeftEye = eye;
                },
                FormatDiopters);

            AddSlider("Left Eye Cyl", -4f, 0f, 0.25f,
                delegate { return _runtimeParameters.LeftEye.Cyl; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.LeftEye;
                    eye.Cyl = value;
                    _runtimeParameters.LeftEye = eye;
                },
                FormatDiopters);

            AddSlider("Left Eye Axis", 0f, 180f, 1f,
                delegate { return _runtimeParameters.LeftEye.Axis; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.LeftEye;
                    eye.Axis = value;
                    _runtimeParameters.LeftEye = eye;
                },
                FormatAngle);

            AddSlider("Addition (Add)", 0f, 3f, 0.1f,
                delegate { return _runtimeParameters.Add; },
                delegate(float value) { _runtimeParameters.Add = value; },
                FormatDiopters);

            AddSlider("Corridor Length", 10f, 20f, 0.5f,
                delegate { return _runtimeParameters.CorridorLengthMm; },
                delegate(float value) { _runtimeParameters.CorridorLengthMm = value; },
                delegate(float value) { return value.ToString("0.0") + " mm"; });

            AddSlider("Fitting Height", 12f, 28f, 0.5f,
                delegate { return _runtimeParameters.FittingHeightMm; },
                delegate(float value) { _runtimeParameters.FittingHeightMm = value; },
                delegate(float value) { return value.ToString("0.0") + " mm"; });

            AddSlider("Inset Near", 0f, 4f, 0.1f,
                delegate { return _runtimeParameters.InsetNearMm; },
                delegate(float value) { _runtimeParameters.InsetNearMm = value; },
                delegate(float value) { return value.ToString("0.0") + " mm"; });

            AddSlider("Headset FOV Horizontal", 80f, 130f, 1f,
                delegate { return _runtimeParameters.HeadsetFovHorizontalDeg; },
                delegate(float value) { _runtimeParameters.HeadsetFovHorizontalDeg = value; },
                FormatAngle);

            AddSlider("Headset FOV Vertical", 70f, 110f, 1f,
                delegate { return _runtimeParameters.HeadsetFovVerticalDeg; },
                delegate(float value) { _runtimeParameters.HeadsetFovVerticalDeg = value; },
                FormatAngle);
        }

        private GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("PalParameterPanel");
            var rect = panel.AddComponent<RectTransform>();
            panel.transform.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = panelPosition;
            rect.sizeDelta = panelSize;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;

            return panel;
        }

        private void CreateHeader(Transform parent)
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);
            var text = titleObj.AddComponent<Text>();
            text.font = _uiFont;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = panelTitle + " (toggle " + toggleKey + ")";

            var descObj = new GameObject("Description");
            descObj.transform.SetParent(parent, false);
            var desc = descObj.AddComponent<Text>();
            desc.font = _uiFont;
            desc.fontSize = 12;
            desc.alignment = TextAnchor.UpperLeft;
            desc.color = new Color(1f, 1f, 1f, 0.6f);
            desc.supportRichText = false;
            desc.text = "Adjust prescription, corridor and headset parameters in real time.";
        }

        private void AddSlider(string label, float min, float max, float step, Func<float> getter, Action<float> setter, Func<float, string> formatter)
        {
            var row = new GameObject(label.Replace(" ", string.Empty) + "Row");
            row.transform.SetParent(_panelRoot.transform, false);
            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            var labelText = labelObj.AddComponent<Text>();
            labelText.font = _uiFont;
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.text = label;

            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            var sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0f, 20f);

            var sliderBackground = sliderObj.AddComponent<Image>();
            sliderBackground.color = new Color(1f, 1f, 1f, 0.1f);
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = step >= 1f && Mathf.Approximately(step % 1f, 0f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(5f, 5f);
            fillAreaRect.offsetMax = new Vector2(-5f, -5f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.75f, 1f, 0.8f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;

            var handleSlideArea = new GameObject("Handle Slide Area");
            handleSlideArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleSlideArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(5f, 5f);
            handleAreaRect.offsetMax = new Vector2(-5f, -5f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleSlideArea.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.9f);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);

            slider.targetGraphic = handleImage;
            slider.handleRect = handleRect;

            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(row.transform, false);
            var valueText = valueObj.AddComponent<Text>();
            valueText.font = _uiFont;
            valueText.fontSize = 12;
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = new Color(0.8f, 0.95f, 1f, 0.9f);

            var binding = new SliderBinding
            {
                slider = slider,
                valueText = valueText,
                getter = getter,
                setter = setter,
                step = step,
                formatter = formatter
            };

            slider.onValueChanged.AddListener(delegate(float rawValue) { OnSliderChanged(binding, rawValue); });
            _bindings.Add(binding);
        }

        private void OnSliderChanged(SliderBinding binding, float rawValue)
        {
            if (_applying)
                return;

            var quantized = Quantize(rawValue, binding.step);
            if (!Mathf.Approximately(quantized, rawValue))
            {
                _applying = true;
                binding.slider.value = quantized;
                _applying = false;
            }

            binding.setter(quantized);
            binding.valueText.text = binding.formatter != null ? binding.formatter(quantized) : quantized.ToString("0.00");
            ApplyParameters();
        }

        private void RefreshUi()
        {
            _applying = true;
            for (var i = 0; i < _bindings.Count; i++)
            {
                var binding = _bindings[i];
                var value = binding.getter();
                binding.slider.value = value;
                binding.valueText.text = binding.formatter != null ? binding.formatter(value) : value.ToString("0.00");
            }
            _applying = false;
        }

        private void ApplyParameters()
        {
            if (simulationController != null && _runtimeParameters != null)
            {
                simulationController.UpdateProfile(_runtimeParameters);
            }
        }

        private static float Quantize(float value, float step)
        {
            if (step <= 0f)
                return value;
            return Mathf.Round(value / step) * step;
        }

        private static string FormatDiopters(float value)
        {
            return value.ToString("+0.00;-0.00;+0.00") + " D";
        }

        private static string FormatAngle(float value)
        {
            return value.ToString("0") + " deg";
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private class SliderBinding
        {
            public Slider slider;
            public Text valueText;
            public Func<float> getter;
            public Action<float> setter;
            public float step;
            public Func<float, string> formatter;
        }
    }
}
