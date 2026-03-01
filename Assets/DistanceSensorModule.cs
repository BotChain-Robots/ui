public class DistanceSensorModule : ModuleBase
{
    public bool objectDetected;
    public float distanceMeters;
    public string[] infoLines = new string[0];

    void Update()
    {
        // Replace with your ControlLibrary call pattern
        // var reading = ControlLibrary.get_distance(...)

        if (!objectDetected)
            infoLines = new[] {"Object: Not detected" };
        else
            infoLines = new[] { "Object: Detected", $"Distance: {distanceMeters:F2} m" };
    }

    public string[] GetInfoLines() => infoLines;
}