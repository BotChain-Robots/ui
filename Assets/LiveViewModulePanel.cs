using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LiveViewModulePanel : MonoBehaviour
{
    [Header("Module Info")]
    public TextMeshProUGUI moduleInfoText;

    [Header("Servo Control")]
    public GameObject servoControlSection;
    public Slider servoAngleSlider;
    public TextMeshProUGUI servoAngleLabel;

    [Header("DC + Display Control (Reused)")]
    public GameObject moduleControlSection;

    [Header("Panel Layout")]
    public float panelWidth = 260f;
    [Range(0f, 0.5f)]
    public float cornerRadius = 0.12f;

    private bool _sliderDragging;
    private ControlPanel panel;
    private ModuleBase _lastSelected;
    private GameObject sensorTextContainer;
    private readonly System.Collections.Generic.List<TextMeshProUGUI> sensorTextLines = new System.Collections.Generic.List<TextMeshProUGUI>();

    void Start()
    {
        CreateSensorTextUI();
        ApplyRoundedCornersToPanel();

        if (servoAngleSlider != null)
        {
            servoAngleSlider.minValue = 0f;
            servoAngleSlider.maxValue = 180f;
            servoAngleSlider.direction = Slider.Direction.BottomToTop;
            servoAngleSlider.onValueChanged.AddListener(OnServoSliderChanged);
            ConfigureSliderForVertical(servoAngleSlider);

            var et = servoAngleSlider.GetComponent<EventTrigger>() ?? servoAngleSlider.gameObject.AddComponent<EventTrigger>();
            var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            begin.callback.AddListener(_ => _sliderDragging = true);
            et.triggers.Add(begin);

            var end = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            end.callback.AddListener(_ => _sliderDragging = false);
            et.triggers.Add(end);
        }

        // Get ControlPanel from the reused section
        panel = moduleControlSection != null ? moduleControlSection.GetComponent<ControlPanel>() : null;
        if (panel == null)
            panel = FindObjectOfType<ControlPanel>(true);

        UpdatePanel();
    }

    void Update()
    {
        UpdatePanel();
    }

    void UpdatePanel()
    {
        var selected = ModuleSelector.SelectedModule;

        if (selected == null)
        {
            _lastSelected = null;
            SetModuleInfo("No module selected");
            SetServoSectionActive(false);
            SetModuleControlSectionActive(false);
            SetSensorTextActive(false);
            return;
        }

        // Info text
        string typeName = GetModuleTypeName(selected);
        string degreeInfo = GetDegreeInfo(selected);
        SetModuleInfo(string.IsNullOrEmpty(degreeInfo) ? $"Type: {typeName}" : $"Type: {typeName}\n{degreeInfo}");

        var servo = selected as ServoMotorModule;
        var dc = selected as DCMotorModule;
        var display = selected as DisplayModule;
        var distance = selected as DistanceSensorModule;
        var imu = selected as IMUSensorModule;

        if (servo != null)
        {
            SetServoSectionActive(true);
            SetModuleControlSectionActive(false);
            SetSensorTextActive(false);

            if (servoAngleSlider != null && !_sliderDragging)
                servoAngleSlider.SetValueWithoutNotify(servo.currentAngle);

            return;
        }

        if (distance != null)
        {
            SetServoSectionActive(false);
            SetModuleControlSectionActive(false);

            SetSensorTextActive(true);
            SetSensorLines(distance.GetInfoLines()); // implement like IMU or build here
            return;
        }

        if (imu != null)
        {
            SetServoSectionActive(false);
            SetModuleControlSectionActive(false);

            SetSensorTextActive(true);
            SetSensorLines(imu.GetInfoLines());
            return;
        }

        // DC, Display, or Gripper use the same control section (degrees input + button)
        var gripper = selected as GripperModule;
        if (dc != null || display != null || gripper != null)
        {
            SetServoSectionActive(false);
            SetModuleControlSectionActive(true);
            SetSensorTextActive(false);

            if (selected != _lastSelected)
            {
                _lastSelected = selected;
                panel?.Initialize(selected);
            }

            return;
        }

        // Other types
        SetServoSectionActive(false);
        SetModuleControlSectionActive(false);
        SetSensorTextActive(false);
    }

    void ApplyRoundedCornersToPanel()
    {
        var img = GetComponent<Image>();
        if (img != null && img.sprite == null)
        {
            img.sprite = UIRoundedSprite.GetRoundedRectSprite();
            img.type = Image.Type.Sliced; // 9-slice keeps corners rounded when panel stretches
        }
    }

    void SetModuleControlSectionActive(bool active)
    {
        if (moduleControlSection == null || panel == null) return;

        if (active)
            panel.gameObject.SetActive(true);
        else
            panel.HidePanel();
    }

    void SetModuleInfo(string text)
    {
        if (moduleInfoText != null)
            moduleInfoText.text = text;
    }

    void SetServoSectionActive(bool active)
    {
        if (servoControlSection != null && servoControlSection.activeSelf != active)
            servoControlSection.SetActive(active);
    }

    void OnServoSliderChanged(float value)
    {
        if (ServoMotorModule.selectedModule != null)
            ServoMotorModule.selectedModule.SetAngleAndSendControlLibrary(value);
    }

    void ConfigureSliderForVertical(Slider slider)
    {
        if (slider == null || servoControlSection == null) return;
        var sectionRect = servoControlSection.GetComponent<RectTransform>();
        if (sectionRect != null)
            EnsureDegreeLabels(sectionRect);
    }

    void EnsureDegreeLabels(RectTransform sectionRect)
    {
        SetDegreeLabel(sectionRect, "0deg", "0", 0, 14);
        SetDegreeLabel(sectionRect, "180deg", "180", 1, -14);
    }

    void SetDegreeLabel(RectTransform sectionRect, string goName, string text, float anchorY, float posY)
    {
        var t = sectionRect.Find(goName);
        if (t == null)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(sectionRect, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(1f, anchorY);
            r.anchorMax = new Vector2(1f, anchorY);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(20, posY);
            r.sizeDelta = new Vector2(50, 24);
        }
        else
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = Color.white;
                tmp.fontSize = 16;
            }
            var r = t.GetComponent<RectTransform>();
            if (r != null)
            {
                r.anchorMin = new Vector2(1f, anchorY);
                r.anchorMax = new Vector2(1f, anchorY);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(20, posY);
                r.sizeDelta = new Vector2(50, 24);
            }
        }
    }

    void CreateSensorTextUI()
    {
        if (moduleInfoText == null) return;
        var parent = moduleInfoText.transform.parent;
        if (parent == null) return;

        sensorTextContainer = new GameObject("SensorTextContainer");
        sensorTextContainer.transform.SetParent(parent, false);

        var src = moduleInfoText.rectTransform;
        var rt = sensorTextContainer.AddComponent<RectTransform>();

        // Place under the moduleInfoText
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, -70f);
        rt.sizeDelta = new Vector2(src.sizeDelta.x, 220f);

        sensorTextContainer.SetActive(false);
    }

    void SetSensorTextActive(bool active)
    {
        if (sensorTextContainer != null && sensorTextContainer.activeSelf != active)
            sensorTextContainer.SetActive(active);
    }

    void EnsureSensorLineCount(int count)
    {
        if (sensorTextContainer == null) return;

        while (sensorTextLines.Count < count)
        {
            var go = new GameObject($"SensorLine{sensorTextLines.Count}");
            go.transform.SetParent(sensorTextContainer.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();

            // Match your UI font + material
            tmp.font = moduleInfoText.font;
            tmp.fontSharedMaterial = moduleInfoText.fontSharedMaterial;

            // Your requested style (like screenshot)
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = 22f;
            tmp.enableAutoSizing = false;
            tmp.color = Color.white;
            tmp.enableVertexGradient = false;

            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;

            // Layout: stacked lines
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            float lineH = 26f;
            float y = -sensorTextLines.Count * lineH;
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, lineH);

            sensorTextLines.Add(tmp);
        }

        // Hide extras
        for (int i = 0; i < sensorTextLines.Count; i++)
            sensorTextLines[i].gameObject.SetActive(i < count);
    }

    void SetSensorLines(string[] lines)
    {
        if (lines == null) lines = new string[0];

        EnsureSensorLineCount(lines.Length);

        for (int i = 0; i < lines.Length; i++)
            sensorTextLines[i].text = lines[i];
    }

    string GetModuleTypeName(ModuleBase m)
    {
        if (m is ServoBendModule) return "Servo Bend";
        if (m is ServoStraightModule) return "Servo Straight";
        if (m is DCMotorModule) return "DC";
        if (m is DisplayModule) return "Display";
        if (m is HubModule) return "Hub";
        if (m is PowerModule) return "Battery";
        if (m is GripperModule) return "Gripper";
        if (m is DisplayModule) return "Display";
        if (m is DistanceSensorModule) return "Distance Sensor";
        if (m is IMUSensorModule) return "IMU Sensor";
        return m.GetType().Name;
    }

    string GetDegreeInfo(ModuleBase m)
    {
        var servo = m as ServoMotorModule;
        if (servo != null)
            return $"Joint: {servo.currentAngle:F1}°";
        return "";
    }
}