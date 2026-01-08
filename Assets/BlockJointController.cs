using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlockJointController : MonoBehaviour
{
    public Transform rotatingCap;
    public Slider angleSlider;
    public TextMeshProUGUI angleLabel;

    private float currentAngle = 0f;

    void Start()
    {
        angleSlider.onValueChanged.AddListener(OnAngleChanged);
        angleSlider.value = 0;
    }

    void OnAngleChanged(float value)
    {
        currentAngle = value;
        angleLabel.text = $"Angle: {value:F0}°";

        if (rotatingCap != null)
            rotatingCap.localRotation = Quaternion.Euler(0, value, 0); // rotate Y
    }
}