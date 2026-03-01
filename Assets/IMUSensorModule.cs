using UnityEngine;

public class IMUSensorModule : ModuleBase
{
    [Header("Latest IMU reading (from hardware)")]
    public bool hasReading;
    public Vector3 eulerDeg;          // roll/pitch/yaw or x/y/z depending on your sensor
    public Vector3 gyroRadPerSec;     // angular velocity
    public Vector3 accelMS2;          // acceleration

    [Header("UI Lines")]
    public string[] infoLines = new string[0];

    void Update()
    {
        // 1) Pull latest reading from your ControlLibrary (pseudo-code)
        // Replace this with your real call:
        // var reading = ControlLibrary.get_imu(Int32.Parse(moduleID));
        // hasReading = reading.valid;
        // eulerDeg = reading.eulerDeg;
        // gyroRadPerSec = reading.gyro;
        // accelMS2 = reading.accel;

        // 2) Build UI lines (always safe even if no reading)
        if (!hasReading)
        {
            infoLines = new[]
            {
                "Status: No data"
            };
            return;
        }

        infoLines = new[]
        {
            "Status: OK",
            $"Orientation (deg): X {eulerDeg.x:F1}  Y {eulerDeg.y:F1}  Z {eulerDeg.z:F1}",
            $"Gyro (rad/s):     X {gyroRadPerSec.x:F2}  Y {gyroRadPerSec.y:F2}  Z {gyroRadPerSec.z:F2}",
            $"Accel (m/s²):     X {accelMS2.x:F2}  Y {accelMS2.y:F2}  Z {accelMS2.z:F2}",
        };
    }

    public string[] GetInfoLines() => infoLines;
}