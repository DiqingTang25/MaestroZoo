using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class MaestroBuildPipeline
{
    private const string MainScene = "Assets/_Project/Scenes/Main.unity";
    private const string AndroidOutputPath = "Builds/Android/MaestroZoo-Rokid-Demo.apk";

    [MenuItem("Maestro Zoo/Build Android APK")]
    public static void BuildAndroidApk()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AndroidOutputPath));

        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Android,
            BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;

        PlayerSettings.SetApplicationIdentifier(
            BuildTargetGroup.Android,
            "com.diqingtang.maestrozoo");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = AndroidOutputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception(
                $"Android APK build failed: {summary.result}, {summary.totalErrors} errors");
        }

        UnityEngine.Debug.Log($"[Maestro] Android APK built: {AndroidOutputPath} ({summary.totalSize} bytes)");
    }
}
