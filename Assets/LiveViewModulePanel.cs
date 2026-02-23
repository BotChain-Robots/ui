using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Unified side panel for LiveView: shows module info and either servo (vertical slider) or DC controls.
/// Attach to ModuleControlSidePanel in the scene; assign refs in inspector.
/// </summary>
public class LiveViewModulePanel : MonoBehaviour
{
    [Header("Module Info")]
    public TextMeshProUGUI moduleInfoText;

    [Header("Servo Control")]
    public GameObject servoControlSection;
    public Slider servoAngleSlider;
    public TextMeshProUGUI servoAngleLabel;

    [Header("DC Control")]
    public GameObject dcControlSection;

    [Header("Panel Layout")]
    [Tooltip("Width of the side panel")]
    public float panelWidth = 260f;
    [Tooltip("Corner radius for rounded corners (0-0.5)")]
    [Range(0f, 0.5f)]
    public float cornerRadius = 0.12f;

    [Header("Slider Styling")]
    public Color sliderTrackColor = new Color(0.4f, 0.4f, 0.45f, 0.9f);
    public Color sliderFillColor = new Color(0.75f, 0.75f, 0.8f, 1f);
    public Color sliderHandleColor = new Color(0.95f, 0.95f, 0.98f, 1f);
    public Color sliderTextColor = new Color(0.92f, 0.92f, 0.95f, 1f);

    private bool _sliderDragging;
    private DCMotorControlPanel _dcPanel;

    void Start()
    {
        if (servoAngleSlider != null)
        {
            servoAngleSlider.minValue = 0f;
            servoAngleSlider.maxValue = 180f;
            servoAngleSlider.direction = Slider.Direction.BottomToTop;
            servoAngleSlider.onValueChanged.AddListener(OnServoSliderChanged);
            ConfigureSliderForVertical(servoAngleSlider);
            var et = servoAngleSlider.GetComponent<EventTrigger>() ?? servoAngleSlider.gameObject.AddComponent<EventTrigger>();
            var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            begin.callback.AddListener(_ => OnServoSliderBeginDrag());
            et.triggers.Add(begin);
            var end = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            end.callback.AddListener(_ => OnServoSliderEndDrag());
            et.triggers.Add(end);
        }

        var oldController = FindObjectOfType<ServoModuleUISliderController>(true);
        if (oldController != null) oldController.enabled = false;

        _dcPanel = dcControlSection != null ? dcControlSection.GetComponent<DCMotorControlPanel>() : null;
        if (_dcPanel == null)
            _dcPanel = FindObjectOfType<DCMotorControlPanel>(true);

        ApplyRoundedMaterial();
        UpdatePanel();
    }

    void ApplyRoundedMaterial()
    {
        var img = GetComponent<Image>();
        if (img == null || (img.material != null && img.material.shader != null && img.material.shader.name == "UI/RoundedRect")) return;
        var shader = Shader.Find("UI/RoundedRect");
        if (shader == null) return;
        var mat = new Material(shader);
        mat.SetFloat("_Radius", cornerRadius);
        img.material = mat;
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
        SetDegreeLabel(sectionRect, "0deg", "0°", 0, 14);
        SetDegreeLabel(sectionRect, "180deg", "180°", 1, -14);
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

    void Update()
    {
        UpdatePanel();
    }

    void UpdatePanel()
    {
        var selected = ModuleSelector.SelectedModule;

        if (selected == null)
        {
            SetModuleInfo("No module selected");
            SetServoSectionActive(false);
            SetDCSectionActive(false);
            return;
        }

        string typeName = GetModuleTypeName(selected);
        string degreeInfo = GetDegreeInfo(selected);
        string info = string.IsNullOrEmpty(degreeInfo)
            ? $"Type: {typeName}"
            : $"Type: {typeName}\n{degreeInfo}";
        SetModuleInfo(info);

        var servo = selected as ServoMotorModule;
        var dc = selected as DCMotorModule;

        if (servo != null)
        {
            SetServoSectionActive(true);
            SetDCSectionActive(false);
            if (servoAngleSlider != null && !_sliderDragging)
                servoAngleSlider.SetValueWithoutNotify(servo.currentAngle);
        }
        else if (dc != null)
        {
            SetServoSectionActive(false);
            SetDCSectionActive(true);
        }
        else
        {
            SetServoSectionActive(false);
            SetDCSectionActive(false);
        }
    }

    string GetModuleTypeName(ModuleBase m)
    {
        if (m is ServoBendModule) return "Servo Bend";
        if (m is ServoStraightModule) return "Servo Straight";
        if (m is DCMotorModule) return "DC";
        if (m is HubModule) return "Hub";
        if (m is BatteryModule) return "Battery";
        return m.GetType().Name;
    }

    string GetDegreeInfo(ModuleBase m)
    {
        var servo = m as ServoMotorModule;
        if (servo != null)
            return $"Joint: {servo.currentAngle:F1}°";
        return "";
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

    void SetDCSectionActive(bool active)
    {
        if (dcControlSection != null)
        {
            if (active && _dcPanel != null)
                _dcPanel.gameObject.SetActive(true);
            else if (!active && _dcPanel != null)
                _dcPanel.HidePanel();
        }
    }

    void OnServoSliderChanged(float value)
    {
        if (ServoMotorModule.selectedModule != null)
            ServoMotorModule.selectedModule.SetAngleAndSendControlLibrary(value);
    }

    public void OnServoSliderBeginDrag()
    {
        _sliderDragging = true;
    }

    public void OnServoSliderEndDrag()
    {
        _sliderDragging = false;
    }
}
