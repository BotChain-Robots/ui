using System;
using UnityEngine;

public abstract class ModuleBase : MonoBehaviour
{
    public string moduleID { get; set; } = "";
    public double angle { get; set; } = 0; // also optional, for Servo/DC angle
    public abstract string moduleType { get; }
    public abstract string moduleName { get; }

    private static bool _nativeLibFailed = false;

    public void SendToControlLibrary(string moduleType, float currentAngle)
    {
        int angleRounded = Mathf.RoundToInt(currentAngle);
        var json = "";
        if (moduleType.Contains("Servo") || moduleType == "Gripper")
        {
            json = JsonUtility.ToJson(new ServoCommand
            {
                ModuleId = moduleID,
                Type = moduleType,
                TargetAngle = angleRounded
            });
        }
        else if (moduleType == "DC")
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
                RotateByDegrees = Math.Abs(angleRounded),
                Direction = direction
            });
        }
        Debug.Log($"[ControlLibrary] Sending command: {json}");
        if (TopologyBuilder.SkipControlLibraryCalls || _nativeLibFailed)
        {
            return;
        }
        try
        {
            if (0 != ControlLibrary.send_angle_control(Int32.Parse(moduleID), angleRounded))
            {
                Debug.Log("Control library exited with error");
            }
        }
        catch (DllNotFoundException)
        {
            Debug.LogWarning("[ControlLibrary] Native library libc_control not found. Disabling further native calls.");
            _nativeLibFailed = true;
        }
        catch (EntryPointNotFoundException)
        {
            Debug.LogWarning("[ControlLibrary] Entry point not found in libc_control. Disabling further native calls.");
            _nativeLibFailed = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ControlLibrary] Native call failed: {e.Message}");
            _nativeLibFailed = true;
        }
    }
}
