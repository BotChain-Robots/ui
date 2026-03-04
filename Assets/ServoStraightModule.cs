using UnityEngine;
public class ServoStraightModule : ServoMotorModule
{
    public override string servoType => "Servo2";
    public override string moduleType => "Servo2";
    public override string moduleName => "Servo Straight Module";

    // public float currentAngle = 90f;

    // public float lastSentAngle = 90f;

    void Start()
    {
        // SetAngle(currentAngle, false);
        // SetHighlight(false);
        // currentAngle = 0f;
        // lastSentAngle = 0f;
    }

    public override void MoveArmPivot(float currentAngle)
    {
        float relativeAngle = -currentAngle;
        armPivot.localRotation = Quaternion.Euler(relativeAngle, 0f, 0f);
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
        MoveArmPivot(currentAngle);
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
