using UnityEditor;

public class BuildScript
{
    public static void BuildWindows()
    {
        BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/SampleScene.unity" },
            "Builds/Windows/botchain.exe",
            BuildTarget.StandaloneWindows64,
            BuildOptions.None
        );
    }
}
