using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Google.FlatBuffers;

public class ControlLibrary : MonoBehaviour
{
    [DllImport("libcontrol")]
    public static extern void init();

    [DllImport("libcontrol")]
    public static extern void cleanup();

    [DllImport("libcontrol")]
    public static extern int send_angle_control(int module_id, int angle);

    [DllImport("libcontrol")]
    private static extern IntPtr get_configuration(out int module_id); // the data this points to will be invalidated when called again


    public static Frontend.RobotConfiguration getRobotConfiguration() // this is not thread safe
    {
        int size;
        IntPtr ptr = get_configuration(out size);

        byte[] buffer = new byte[size];
        Marshal.Copy(ptr, buffer, 0, size);

        ByteBuffer bb = new ByteBuffer(buffer);
        var myObject = Frontend.RobotConfiguration.GetRootAsRobotConfiguration(bb);
        return myObject;
    }
}
