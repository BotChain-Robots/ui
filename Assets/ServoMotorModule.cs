using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ServoMotorModule : ModuleBase
{
    public static ServoMotorModule selectedModule;
    public abstract string servoType { get; }
    [Header("References")]
    public Transform armPivot;
    public GameObject highlightVisual;

    [Header("Servo Settings")]
    [Range(0f, 180f)]
    public float currentAngle;
    public float lastSentAngle;

    public abstract void SetAngle(float angle);

    // public void SetInitialAngle(float angle)
    // {
    //     SetAngle(angle);
    //     lastSentAngle = currentAngle;
    // }

    public abstract void InitialSetAngle(float angle);

    public void SetAngleAndSendControlLibrary(float angle)
    {
        SetAngle(angle);
        if (Mathf.Abs(currentAngle - lastSentAngle) > 0.1f)
        {
            SendToControlLibrary(servoType, currentAngle);
            lastSentAngle = currentAngle;
        }
    }

    // public void SetAngle(float angle)
    // {
    //     this.currentAngle = Mathf.Clamp(angle, 0f, 180f);
    //     this.lastSentAngle = currentAngle;
    //     this.MoveArmPivot(currentAngle);
    // }

    // public void SetAngleAndSendControlLibrary(float angle)
    // {
    //     this.SetAngle(angle);
    //     if (Mathf.Abs(this.currentAngle - this.lastSentAngle) > 0.1f)
    //     {
    //         this.SendToControlLibrary(this.servoType, currentAngle);
    //         this.lastSentAngle = this.currentAngle;
    //     }
    // }

    public abstract void MoveArmPivot(float currentAngle);

    public override void OnSelect()
    {
    }

    public override void DeSelect()
    {
    }
}
