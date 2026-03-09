using UnityEngine;
using UnityEngine.UI;

public class AngleControllerSlider : MonoBehaviour
{
    public GameObject angleUI;
    public Slider angleSlider;
    public Text angleLabel;

    private Transform currentCap;

    void Start()
{
    Debug.Log("AngleControllerSlider script is alive!");

    angleSlider.minValue = 0;
    angleSlider.maxValue = 180;
    angleSlider.onValueChanged.AddListener(OnSliderChanged);

    if (angleUI != null)
        angleUI.SetActive(false);
    else
        Debug.LogError("angleUI is not assigned.");
}

    private GameObject currentSelectedObject;

void Update()
{
    if (ObjectSelector.selectedObject != null)
    {
        if (!angleUI.activeSelf)
            angleUI.SetActive(true);

        // Detect object switch
        if (ObjectSelector.selectedObject != currentSelectedObject)
        {
            currentSelectedObject = ObjectSelector.selectedObject;

            Debug.Log("[Slider] Selected new object: " + currentSelectedObject.name);

            // Update cap reference
            Transform newCap = currentSelectedObject.transform.Find("Cap");

            if (newCap != null)
            {
                currentCap = newCap;
                float currentAngle = Mathf.Clamp(currentCap.localEulerAngles.y, 0f, 180f);
                angleSlider.SetValueWithoutNotify(currentAngle);
                angleLabel.text = $"Angle: {Mathf.RoundToInt(currentAngle)}°";
                Debug.Log("[Slider] Found new Cap and updated UI.");
            }
            else
            {
                Debug.LogWarning("[Slider] No Cap found on selected object.");
                currentCap = null;
            }
        }
    }
    else
    {
        if (angleUI.activeSelf)
            angleUI.SetActive(false);
        currentSelectedObject = null;
        currentCap = null;
    }
}


    public void OnSliderChanged(float value)
    {
        if (currentCap != null)
        {
            float clampedValue = Mathf.Clamp(value, 0f, 180f);
            currentCap.localEulerAngles = new Vector3(
                currentCap.localEulerAngles.x,
                clampedValue,
                currentCap.localEulerAngles.z
            );

            Debug.Log("[Slider] Updated Cap Angle to: " + clampedValue);
            angleLabel.text = $"Angle: {Mathf.RoundToInt(clampedValue)}°";
        }
        else
        {
            Debug.LogWarning("[Slider] No currentCap to apply angle change.");
        }
    }
}
