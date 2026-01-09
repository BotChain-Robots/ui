using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using Google.FlatBuffers;

public class ControlLibrary : MonoBehaviour
{
    [DllImport("libc_control")]
    public static extern void init();

    [DllImport("libc_control")]
    public static extern void cleanup();

    [DllImport("libc_control")]
    public static extern int send_angle_control(int module_id, int angle);

    [DllImport("libc_control")]
    private static extern IntPtr get_configuration(out int module_id); // the data this points to will be invalidated when called again

    [DllImport("libc_control")]
    public static extern bool control_sentry_init(
        string dsn,
        string environment,
        string release
    );

    [DllImport("libc_control")]
    public static extern void control_sentry_set_app_info(
        string app_name,
        string app_version,
        string build_number);

    [DllImport("libc_control")]
    public static extern void control_sentry_shutdown();


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
