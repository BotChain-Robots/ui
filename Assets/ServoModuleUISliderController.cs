using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ServoModuleUISliderController : MonoBehaviour
{
    public Slider angleSlider;
    public TextMeshProUGUI selectedModuleText;

    private bool isDragging = false;

    void Start()
    {
        if (angleSlider == null)
            angleSlider = GetComponentInChildren<Slider>();

        angleSlider.minValue = 0f;
        angleSlider.maxValue = 180f;

        angleSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    void Update()
    {
        var selected = ServoMotorModule.selectedModule;

        if (selected != null)
        {
            selectedModuleText.text = $"Selected: {selected.name} | Angle: {selected.currentAngle:F1}°";

            // Sync slider with angle if not dragging
            if (!isDragging)
            {
                angleSlider.value = selected.currentAngle;
            }

            // Make slider visible and interactable
            if (!angleSlider.gameObject.activeSelf)
            {
                angleSlider.gameObject.SetActive(true);
            }
        }
        else
        {
            selectedModuleText.text = "No module selected";

            // Hide slider when no module selected
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

    public void OnEndDrag()
    {
        isDragging = false;
    }
}