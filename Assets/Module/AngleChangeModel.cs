using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ServoCommand
{
    public string ModuleId;
    public string Type = "Servo";
    public float TargetAngle;
}

[System.Serializable]
public class DCCommand
{
    public string ModuleId;
    public string Type = "DC";
    public float RotateByDegrees;
    public string Direction; // "Clockwise" or "CounterClockwise"
}