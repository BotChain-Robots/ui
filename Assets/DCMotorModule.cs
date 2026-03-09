using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class DCMotorModule : ModuleBase
{
    private static ControlPanel motorControlPanel;
    public Transform motorShaft;

    public float rotationSpeed = 90f;
    private float targetPosition = 0f;
    public override string moduleType => "DC";
    public override string moduleName => "DC Module";

    public void Rotate(float degrees, int direction)
    {
        float rotation = degrees * direction;
        targetPosition += rotation;
        this.SendToControlLibrary("DC", targetPosition);
        StartCoroutine(RotateOverTime(rotation));
    }

    private IEnumerator RotateOverTime(float rotation)
    {
        float currentRotationDegrees = 0f;

        while (Mathf.Abs(currentRotationDegrees) < Mathf.Abs(rotation))
        {
            float step = rotationSpeed * Time.deltaTime;
            if (Mathf.Abs(currentRotationDegrees + step) > Mathf.Abs(rotation))
            {
                step = Mathf.Abs(rotation) - Mathf.Abs(currentRotationDegrees);
            }

            motorShaft.Rotate(Vector3.up, step * Mathf.Sign(rotation));
            currentRotationDegrees += step * Mathf.Sign(rotation);

            yield return null;
        }
    }
}
