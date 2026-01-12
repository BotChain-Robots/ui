using UnityEditor;
using System.IO;

public class BuildScript
{
    public static void BuildWindows()
    {
        string outputPath = "Builds/Windows/botchain.exe";

        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            outputPath,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None
        );

        File.Copy(sourceSoPath, destSoPath, overwrite: true);
    }

    public static void BuildLinux()
    {
        string outputPath = "Builds/Linux/botchain.x86_64";

        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            outputPath,
            BuildTarget.StandaloneLinux64,
            BuildOptions.None
        );

        string sourceSoPath = "Assets/ControlLibrary/libc_control.so";
        string destSoPath = Path.Combine(Path.GetDirectoryName(outputPath), "libc_control.so");

        File.Copy(sourceSoPath, destSoPath, overwrite: true);
    }
    
    public static void BuildMac()
    {
        string outputPath = "Builds/macOS/botchain.app";

        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            outputPath,
            BuildTarget.StandaloneOSX,
            BuildOptions.None
        );

        string sourceSoPath = "Assets/ControlLibrary/libc_control.dylib";
        string destSoPath = Path.Combine(Path.GetDirectoryName(outputPath), "libc_control.dylib");

        File.Copy(sourceSoPath, destSoPath, overwrite: true);
    }
}
