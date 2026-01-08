using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DCMotorControlPanel : MonoBehaviour
{
    public TMP_InputField degreesInputField;
    public TMP_Dropdown directionDropdown;
    public Button rotateButton;

    private DCMotorModule currentDCModule;

    public void Initialize(DCMotorModule DCModule)
    {
        currentDCModule = DCModule;
        gameObject.SetActive(true);
        rotateButton.onClick.RemoveAllListeners();
        rotateButton.onClick.AddListener(HandleRotateClicked);
    }

    public void HandleRotateClicked()
    {
        // Parse degrees
        if (!float.TryParse(degreesInputField.text, out float degrees))
        {
            Debug.LogWarning("Invalid degree input");
            return;
        }

        // Determine direction
        int direction = directionDropdown.value == 0 ? 1 : -1;
        currentDCModule.Rotate(degrees, direction);
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
    }
}
