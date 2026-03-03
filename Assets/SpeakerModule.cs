using System;
using System.IO;
using UnityEngine;

public class SpeakerModule : ModuleBase
{
    [Header("Selected Audio")]
    public string audioFilePath = "";
    public string audioFileName = "";
    public byte[] audioBytes;

    // Called by UI after user picks a file
    public void SetAudioFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogWarning("[SpeakerModule] Invalid audio file path.");
            ClearAudio();
            return;
        }

        audioFilePath = path;
        audioFileName = Path.GetFileName(path);

        try
        {
            audioBytes = File.ReadAllBytes(path);
            Debug.Log($"[SpeakerModule] Loaded audio file: {audioFileName} ({audioBytes.Length} bytes)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpeakerModule] Failed to read audio file: {e.Message}");
            ClearAudio();
        }
    }

    // Called by UI when user presses the shared Send button
    public void SendAudioToHardware()
    {
        if (audioBytes == null || audioBytes.Length == 0)
        {
            Debug.LogWarning("[SpeakerModule] No audio selected to send.");
            return;
        }

        // TODO: replace with your real control library call
        Debug.Log($"[SpeakerModule] Sending audio '{audioFileName}' ({audioBytes.Length} bytes) to hardware");

        // Example:
        // ControlLibrary.send_speaker_audio(Int32.Parse(moduleID), audioBytes, audioBytes.Length);
        // or chunk it if your library requires.
    }

    public void PlayAudioOnHardware()
    {
        // TODO: Replace with your real ControlLibrary call
        // Example:
        // ControlLibrary.speaker_play(Int32.Parse(moduleID));

        Debug.Log($"[SpeakerModule] Play requested on hardware for '{audioFileName}'.");
    }

    private void ClearAudio()
    {
        audioFilePath = "";
        audioFileName = "";
        audioBytes = null;
    }
}