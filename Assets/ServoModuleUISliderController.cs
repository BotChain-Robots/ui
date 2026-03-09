using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ServoModuleUISliderController : MonoBehaviour
{
    public Slider angleSlider;
    public Text selectedModuleText;

    private bool isDragging = false;

    void Start()
    {
        if (angleSlider == null)
            angleSlider = GetComponentInChildren<Slider>();

        if (angleSlider == null) return;

        angleSlider.minValue = 0f;
        angleSlider.maxValue = 180f;

        angleSlider.onValueChanged.AddListener(OnSliderChanged);

        var et = angleSlider.GetComponent<EventTrigger>() ?? angleSlider.gameObject.AddComponent<EventTrigger>();
        var begin = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        begin.callback.AddListener(_ => isDragging = true);
        et.triggers.Add(begin);

        var end = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        end.callback.AddListener(_ => isDragging = false);
        et.triggers.Add(end);
    }

    void Update()
    {
        if (selectedModuleText == null || angleSlider == null) return;

        var selected = ServoMotorModule.selectedModule;

        if (selected != null)
        {
            selectedModuleText.text = $"Selected: {selected.name} | Angle: {selected.currentAngle:F1}°";

            if (!isDragging)
            {
                angleSlider.SetValueWithoutNotify(selected.currentAngle);
            }

            if (!angleSlider.gameObject.activeSelf)
            {
                angleSlider.gameObject.SetActive(true);
            }
        }
        else
        {
            selectedModuleText.text = "No module selected";

            if (angleSlider.gameObject.activeSelf)
            {
                angleSlider.gameObject.SetActive(false);
            }
        }
    }

    public void OnSliderChanged(float value)
    {
        if (ServoMotorModule.selectedModule != null)
        {
            ServoMotorModule.selectedModule.SetAngleAndSendControlLibrary(value);
        }
    }
}