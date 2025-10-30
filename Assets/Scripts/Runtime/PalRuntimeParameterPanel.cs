using System;
using System.Collections.Generic;
using ProgressiveVrLenses.Optics;
using UnityEngine;
using UnityEngine.UI;

namespace ProgressiveVrLenses.Runtime
{
    /// <summary>
    /// Displays a runtime panel that always stays visible in the top-right corner.
    /// Users can navigate with the keyboard and adjust PAL parameters on the fly.
    /// </summary>
    public class PalRuntimeParameterPanel : MonoBehaviour
    {
        [Header("Simulation")]
        [Tooltip("Simulation controller that will receive runtime parameter updates.")]
        public PalSimulationController simulationController;
        [Tooltip("Search automatically for a PalSimulationController when none is assigned.")]
        public bool autoFindSimulation = true;

        [Header("UI")]
        [Tooltip("Panel size in pixels.")]
        public Vector2 panelSize = new Vector2(360f, 560f);
        [Tooltip("Offset from the top-right corner of the screen.")]
        public Vector2 panelMargin = new Vector2(24f, 24f);
        [Tooltip("Heading text displayed at the top of the panel.")]
        public string panelTitle = "PAL Parameters";
        [Tooltip("Time before held arrow keys start repeating adjustments.")]
        public float initialRepeatDelay = 0.35f;
        [Tooltip("Repeat rate while holding an arrow key.")]
        public float repeatDelay = 0.12f;

        private PalProfileParameters _runtimeParameters;
        private GameObject _canvasRoot;
        private RectTransform _panelRect;
        private Font _uiFont;

        private readonly List<ParameterEntry> _entries = new List<ParameterEntry>();
        private int _selectedIndex;
        private int _adjustDirection;
        private float _nextAdjustTime;

        private void Start()
        {
            if (simulationController == null && autoFindSimulation)
                simulationController = FindObjectOfType<PalSimulationController>();

            if (simulationController == null)
            {
                Debug.LogWarning("PalRuntimeParameterPanel: no PalSimulationController found. Runtime UI disabled.");
                enabled = false;
                return;
            }

            CreateRuntimeParameters();
            BuildUi();
            RefreshUi(true);
            ApplyParameters();
        }

        private void Update()
        {
            HandleSelectionInput();
            HandleAdjustmentInput();
            RefreshUi(false);
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
            _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _canvasRoot = new GameObject("PalRuntimeCanvas");
            DontDestroyOnLoad(_canvasRoot);

            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasRoot.AddComponent<CanvasScaler>();
            _canvasRoot.AddComponent<GraphicRaycaster>();

            var panelObj = new GameObject("PalParameterPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            _panelRect = panelObj.AddComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(1f, 1f);
            _panelRect.anchorMax = new Vector2(1f, 1f);
            _panelRect.pivot = new Vector2(1f, 1f);
            _panelRect.anchoredPosition = new Vector2(-panelMargin.x, -panelMargin.y);
            _panelRect.sizeDelta = panelSize;

            var background = panelObj.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.7f);

            var vertical = panelObj.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 12, 12);
            vertical.spacing = 8f;
            vertical.childAlignment = TextAnchor.UpperLeft;

            CreateHeader(panelObj.transform);
            CreateInstructions(panelObj.transform);
            CreateParameterEntries(panelObj.transform);
            UpdateSelectionHighlight();
        }

        private void CreateHeader(Transform parent)
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(parent, false);
            var text = headerObj.AddComponent<Text>();
            text.font = _uiFont;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = panelTitle;
        }

        private void CreateInstructions(Transform parent)
        {
            var instructionsObj = new GameObject("Instructions");
            instructionsObj.transform.SetParent(parent, false);
            var text = instructionsObj.AddComponent<Text>();
            text.font = _uiFont;
            text.fontSize = 12;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(1f, 1f, 1f, 0.7f);
            text.text = "Use Up/Down to select a parameter.\nUse Left/Right to decrease/increase.\nValues apply instantly.";
        }

        private void CreateParameterEntries(Transform parent)
        {
            AddParameter(parent, "Right Eye Sph", -8f, 4f, 0.25f,
                delegate { return _runtimeParameters.RightEye.Sph; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.RightEye;
                    eye.Sph = value;
                    _runtimeParameters.RightEye = eye;
                },
                FormatDiopters);

            AddParameter(parent, "Right Eye Cyl", -4f, 0f, 0.25f,
                delegate { return _runtimeParameters.RightEye.Cyl; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.RightEye;
                    eye.Cyl = value;
                    _runtimeParameters.RightEye = eye;
                },
                FormatDiopters);

            AddParameter(parent, "Right Eye Axis", 0f, 180f, 1f,
                delegate { return _runtimeParameters.RightEye.Axis; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.RightEye;
                    eye.Axis = value;
                    _runtimeParameters.RightEye = eye;
                },
                FormatAngle);

            AddParameter(parent, "Left Eye Sph", -8f, 4f, 0.25f,
                delegate { return _runtimeParameters.LeftEye.Sph; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.LeftEye;
                    eye.Sph = value;
                    _runtimeParameters.LeftEye = eye;
                },
                FormatDiopters);

            AddParameter(parent, "Left Eye Cyl", -4f, 0f, 0.25f,
                delegate { return _runtimeParameters.LeftEye.Cyl; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.LeftEye;
                    eye.Cyl = value;
                    _runtimeParameters.LeftEye = eye;
                },
                FormatDiopters);

            AddParameter(parent, "Left Eye Axis", 0f, 180f, 1f,
                delegate { return _runtimeParameters.LeftEye.Axis; },
                delegate(float value)
                {
                    var eye = _runtimeParameters.LeftEye;
                    eye.Axis = value;
                    _runtimeParameters.LeftEye = eye;
                },
                FormatAngle);

            AddParameter(parent, "Addition (Add)", 0f, 3f, 0.1f,
                delegate { return _runtimeParameters.Add; },
                delegate(float value) { _runtimeParameters.Add = value; },
                FormatDiopters);

            AddParameter(parent, "Corridor Length", 10f, 20f, 0.5f,
                delegate { return _runtimeParameters.CorridorLengthMm; },
                delegate(float value) { _runtimeParameters.CorridorLengthMm = value; },
                delegate(float value) { return value.ToString("0.0") + " mm"; });

            AddParameter(parent, "Fitting Height", 12f, 28f, 0.5f,
                delegate { return _runtimeParameters.FittingHeightMm; },
                delegate(float value) { _runtimeParameters.FittingHeightMm = value; },
                delegate(float value) { return value.ToString("0.0") + " mm"; });

            AddParameter(parent, "Inset Near", 0f, 4f, 0.1f,
                delegate { return _runtimeParameters.InsetNearMm; },
                delegate(float value) { _runtimeParameters.InsetNearMm = value; },
                delegate(float value) { return value.ToString("0.0") + " mm"; });

            AddParameter(parent, "Headset FOV Horizontal", 80f, 130f, 1f,
                delegate { return _runtimeParameters.HeadsetFovHorizontalDeg; },
                delegate(float value) { _runtimeParameters.HeadsetFovHorizontalDeg = value; },
                FormatAngle);

            AddParameter(parent, "Headset FOV Vertical", 70f, 110f, 1f,
                delegate { return _runtimeParameters.HeadsetFovVerticalDeg; },
                delegate(float value) { _runtimeParameters.HeadsetFovVerticalDeg = value; },
                FormatAngle);
        }

        private void AddParameter(Transform parent, string label, float min, float max, float step, Func<float> getter, Action<float> setter, Func<float, string> formatter)
        {
            var row = new GameObject(label.Replace(" ", string.Empty) + "Row");
            row.transform.SetParent(parent, false);

            var background = row.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.05f);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = true;
            layout.childControlWidth = true;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            var labelText = labelObj.AddComponent<Text>();
            labelText.font = _uiFont;
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.text = label;
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.minWidth = 120f;

            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(row.transform, false);
            var valueText = valueObj.AddComponent<Text>();
            valueText.font = _uiFont;
            valueText.fontSize = 14;
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = new Color(0.8f, 0.95f, 1f, 0.95f);
            var valueLayout = valueObj.AddComponent<LayoutElement>();
            valueLayout.minWidth = 110f;
            valueLayout.flexibleWidth = 0f;

            var entry = new ParameterEntry
            {
                label = label,
                min = Mathf.Min(min, max),
                max = Mathf.Max(min, max),
                step = Mathf.Max(step, 0.0001f),
                getter = getter,
                setter = setter,
                formatter = formatter,
                background = background,
                labelText = labelText,
                valueText = valueText
            };

            _entries.Add(entry);
        }

        private void HandleSelectionInput()
        {
            if (_entries.Count == 0)
                return;

            var previousIndex = _selectedIndex;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                _selectedIndex = (_selectedIndex - 1 + _entries.Count) % _entries.Count;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                _selectedIndex = (_selectedIndex + 1) % _entries.Count;

            if (previousIndex != _selectedIndex)
                UpdateSelectionHighlight();
        }

        private void HandleAdjustmentInput()
        {
            if (_entries.Count == 0)
                return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                StartAdjust(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                StartAdjust(1);

            if (Input.GetKeyUp(KeyCode.LeftArrow) && _adjustDirection == -1)
                StopAdjust();
            if (Input.GetKeyUp(KeyCode.RightArrow) && _adjustDirection == 1)
                StopAdjust();

            if (_adjustDirection != 0 && Time.unscaledTime >= _nextAdjustTime)
            {
                ApplyAdjustment(_adjustDirection);
                _nextAdjustTime = Time.unscaledTime + repeatDelay;
            }
        }

        private void StartAdjust(int direction)
        {
            _adjustDirection = direction;
            ApplyAdjustment(direction);
            _nextAdjustTime = Time.unscaledTime + initialRepeatDelay;
        }

        private void StopAdjust()
        {
            _adjustDirection = 0;
        }

        private void ApplyAdjustment(int direction)
        {
            var entry = _entries[_selectedIndex];
            var current = entry.getter();
            var delta = entry.step * direction;
            var target = Mathf.Clamp(Quantize(current + delta, entry.step), entry.min, entry.max);

            if (!Mathf.Approximately(current, target))
            {
                entry.setter(target);
                ApplyParameters();
            }
        }

        private void RefreshUi(bool force)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var value = entry.getter();
                var formatted = entry.formatter != null ? entry.formatter(value) : value.ToString("0.00");

                if (force || entry.lastValue != value || entry.lastFormatted != formatted)
                {
                    entry.valueText.text = formatted;
                    entry.lastValue = value;
                    entry.lastFormatted = formatted;
                    _entries[i] = entry;
                }
            }
        }

        private void UpdateSelectionHighlight()
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                entry.background.color = i == _selectedIndex
                    ? new Color(0.2f, 0.55f, 0.9f, 0.35f)
                    : new Color(1f, 1f, 1f, 0.05f);
            }
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

        private struct ParameterEntry
        {
            public string label;
            public float min;
            public float max;
            public float step;
            public Func<float> getter;
            public Action<float> setter;
            public Func<float, string> formatter;
            public Image background;
            public Text labelText;
            public Text valueText;
            public float lastValue;
            public string lastFormatted;
        }
    }
}
