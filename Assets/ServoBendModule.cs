using UnityEngine;
public class ServoBendModule : ServoMotorModule
{
    public override string servoType => "Servo1";
    public override string moduleType => "Servo1";
    public override string moduleName => "Servo Bend Module";

    void Start()
    {
        // SetAngle(currentAngle, false);
        // SetHighlight(false);
        // currentAngle = 90f;
        // lastSentAngle = 90f;
    }

    public override void MoveArmPivot(float currentAngle)
    {
        float relativeAngle = currentAngle - 90f;
        armPivot.localRotation = Quaternion.Euler(0f, 0f, relativeAngle);
    }

    public override void SetAngle(float angle)
    {
        currentAngle = Mathf.Clamp(angle, 0f, 180f);
        MoveArmPivot(currentAngle);
    }

    public override void InitialSetAngle(float angle)
    {
        currentAngle = Mathf.Clamp(angle, 0f, 180f);
        lastSentAngle = currentAngle;
        float relativeAngle = currentAngle - 90f;
        armPivot.localRotation = Quaternion.Euler(0f, 0f, relativeAngle);
    }

    // public void SetHighlight(bool enabled)
    // {
    //     if (highlightVisual != null)
    //     {
    //         highlightVisual.SetActive(enabled);
    //         Debug.Log($"[Highlight] {(enabled ? "Enabled" : "Disabled")} for {name}");
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"[Highlight] No highlightVisual assigned on {name}");
    //     }
    // }
}
