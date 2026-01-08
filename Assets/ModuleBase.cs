using System;
using UnityEngine;

public abstract class ModuleBase : MonoBehaviour
{
    public string moduleID { get; set; } = "";
    public double angle { get; set; } = 0; // also optional, for Servo/DC angle

    public abstract void OnSelect();
    public abstract void DeSelect();

    public void SendToControlLibrary(string moduleType, float currentAngle)
    {
        var json = "";
        if (moduleType.Contains("Servo"))
        {
            json = JsonUtility.ToJson(new ServoCommand
            {
                ModuleId = moduleID,
                Type = moduleType,
                TargetAngle = currentAngle
            });
        }
        else if(moduleType == "DC")
        {
            string direction = "Forwards";
            if (currentAngle < 0)
            {
                direction = "Backwards";
            }
            json = JsonUtility.ToJson(new DCCommand
            {
                ModuleId = moduleID,
                Type = moduleType,
                RotateByDegrees = Math.Abs(currentAngle),
                Direction = direction
            });
        }
        Debug.Log($"[ControlLibrary] Sending command: {json}");
        if (0 != ControlLibrary.send_angle_control(Int32.Parse(moduleID), (int)currentAngle))
        {
            Debug.Log("Control library exited with error");
        }
    }
}
