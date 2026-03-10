using System;
using UnityEngine;

public class DistanceSensorModule : ModuleBase
{
    // Keep a fixed-size array (no GC alloc every frame)
    public string[] infoLines = new string[1];

    public override string moduleType => "Distance";
    public override string moduleName => "Distance Sensor Module";

    // Optional: slow down polling to avoid spamming hardware
    [SerializeField] float pollHz = 10f;
    float nextPollTime;

    void Update()
    {
        // Poll at a fixed rate (avoids frame spam)
        if (Time.unscaledTime < nextPollTime) return;
        nextPollTime = Time.unscaledTime + (1f / Mathf.Max(1f, pollHz));

        if (!int.TryParse(moduleID, out var id))
        {
            infoLines[0] = "ERROR: moduleID is not an int";
            return;
        }

        try
        {
            double distance = ControlLibrary.get_distance_control(id);
            infoLines[0] = $"Distance: {distance:F0} mm";
        }
        catch (Exception e)
        {
            // infoLines[1] = $"EXCEPTION: {e.GetType().Name}: {e.Message}";
            // Debug.LogException(e);
            infoLines[0] = "Error: Could not detect any distance";
        }
    }

    public string[] GetInfoLines() => infoLines;
}