using UnityEngine;
using System;

public class DisplayModule : ModuleBase
{
    public string displayText = "";
    public override string moduleType => "Display";
    public override string moduleName => "Display Module";

    public void SetDisplayText(string text)
    {
        displayText = text;
        SendToDisplayHardware(text);
    }

    private void SendToDisplayHardware(string text)
    {
        // Replace with actual hardware communication logic
        Debug.Log($"[DisplayModule] Sending display text: {text}");
        // Example: ControlLibrary.send_display_text(Int32.Parse(moduleID), text);
        ControlLibrary.send_string_control(Int32.Parse(moduleID), text);
    }
}
