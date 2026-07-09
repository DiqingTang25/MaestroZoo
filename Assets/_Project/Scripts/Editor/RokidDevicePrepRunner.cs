using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RokidDevicePrepRunner
{
    private const string TriggerPath = "Temp/MaestroZoo_RunRokidPrep.flag";
    private const string LogPath = "Logs/RokidDevicePrep.log";

    static RokidDevicePrepRunner()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    [MenuItem("Maestro Zoo/Prepare Rokid Device Build")]
    public static void PrepareRokidDeviceBuild()
    {
        RunPreparation();
    }

    private static void RunIfRequested()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunIfRequested;
            return;
        }

        if (!File.Exists(TriggerPath))
            return;

        File.Delete(TriggerPath);
        RunPreparation();
    }

    private static void RunPreparation()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
        WriteLog("=== Rokid device prep started ===");

        try
        {
            WriteLog("1/5 Create Difficulty Profiles");
            MaestroSceneBuilder.CreateDifficultyProfiles();

            WriteLog("2/5 Create Latency Presets");
            MaestroSceneBuilder.CreateLatencyPresets();

            WriteLog("3/5 Build Production Scene");
            MaestroSceneBuilder.Build();

            WriteLog("4/5 Switch Platform: Android");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            WriteLog("5/6 Configure Rokid XR Build");
            RokidXRBuildConfigurator.Configure();

            WriteLog("6/6 Build Android APK");
            MaestroBuildPipeline.BuildAndroidApk();

            WriteLog("DONE: Builds/Android/MaestroZoo-Rokid-Demo.apk");
            EditorUtility.DisplayDialog(
                "Maestro Zoo",
                "Rokid build is ready:\nBuilds/Android/MaestroZoo-Rokid-Demo.apk",
                "OK");
        }
        catch (Exception ex)
        {
            WriteLog("FAILED:");
            WriteLog(ex.ToString());
            Debug.LogException(ex);
            EditorUtility.DisplayDialog(
                "Maestro Zoo build failed",
                "Check Logs/RokidDevicePrep.log and the Unity Console.",
                "OK");
        }
    }

    private static void WriteLog(string message)
    {
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(LogPath, line + Environment.NewLine);
        Debug.Log("[RokidPrep] " + message);
    }
}
