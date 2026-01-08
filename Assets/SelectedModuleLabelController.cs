using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class SelectedModuleLabelController : MonoBehaviour
{
    public TextMeshProUGUI selectedModuleText;

    void Update()
    {
        var selected = ServoMotorModule.selectedModule;

        if (selected != null)
        {
            Debug.Log($"[UI] Selected module: {selected.name} | Angle: {selected.currentAngle:F1}°");
            selectedModuleText.text = $"Selected: {selected.name} | Angle: {selected.currentAngle:F1}°";
        }
        else
        {
            Debug.Log("[UI] No module selected.");
            selectedModuleText.text = "No module selected";
        }
    }
}