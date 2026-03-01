using UnityEngine;

public class DisplayModule : ModuleBase
{
    public string displayText = "";

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
    }
}
