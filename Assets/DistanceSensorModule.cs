using System;
using UnityEngine;

public class DistanceSensorModule : ModuleBase
{
    // Keep a fixed-size array (no GC alloc every frame)
    public string[] infoLines = new string[2];

    public override string moduleType => "Distance";
    public override string moduleName => "Distance Sensor Module";

    // Optional: slow down polling to avoid spamming hardware
    [SerializeField] float pollHz = 10f;
    float nextPollTime;

    void Update()
    {
        // Always show something immediately
        infoLines[0] = $"ModuleID raw: '{moduleID}'";

        // Poll at a fixed rate (avoids frame spam)
        if (Time.unscaledTime < nextPollTime) return;
        nextPollTime = Time.unscaledTime + (1f / Mathf.Max(1f, pollHz));

        if (!int.TryParse(moduleID, out var id))
        {
            infoLines[1] = "ERROR: moduleID is not an int";
            return;
        }

        try
        {
            // Control library call
            double distance = ControlLibrary.get_distance_control(id);

            infoLines[1] = $"Distance: {distance:F0} mm";
        }
        catch (Exception e)
        {
            // SHOW THE PROBLEM ON SCREEN (works in release builds)
            infoLines[1] = $"EXCEPTION: {e.GetType().Name}: {e.Message}";

            // Also log to Player.log (works in builds)
            Debug.LogException(e);
        }
    }

    public string[] GetInfoLines() => infoLines;
}