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
    }
}
