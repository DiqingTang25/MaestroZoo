using System;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features;

public static class RokidXRBuildConfigurator
{
    private const string LoaderSettingsKey = "com.unity.xr.management.loader_settings";
    private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    private static readonly string[] RequiredFeatureIds =
    {
        "com.rokid.openxr.feature",
        "com.unity.openxr.feature.input.rokidhandtrackingprofile",
        "com.unity.openxr.feature.input.RokidHandTrackingAim",
        "com.unity.openxr.feature.RokidARFoundation",
        "com.unity.openxr.feature.input.rokidcontrollerprofile",
        "com.unity.openxr.feature.input.handtracking"
    };

    [MenuItem("Maestro Zoo/Configure Rokid XR Build")]
    public static void Configure()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        XRGeneralSettingsPerBuildTarget buildTargetSettings = GetBuildTargetSettings();
        if (buildTargetSettings == null)
            throw new InvalidOperationException("XR Plug-in Management settings asset was not found.");

        if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
            buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);

        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        XRManagerSettings manager = buildTargetSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        if (manager == null)
            throw new InvalidOperationException("Could not create Android XR Manager settings.");

        bool loaderAssigned = XRPackageMetadataStore.AssignLoader(
            manager,
            OpenXRLoaderTypeName,
            BuildTargetGroup.Android);

        if (!loaderAssigned)
            throw new InvalidOperationException("Could not assign Unity OpenXR loader for Android.");

        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        foreach (string featureId in RequiredFeatureIds)
        {
            OpenXRFeature feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
            if (feature == null)
            {
                Debug.LogWarning($"[RokidXR] OpenXR feature not found: {featureId}");
                continue;
            }

            feature.enabled = true;
            EditorUtility.SetDirty(feature);
            Debug.Log($"[RokidXR] Enabled OpenXR feature: {feature.GetType().Name} ({featureId})");
        }

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.optimizedFramePacing = false;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(buildTargetSettings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RokidXR] Android OpenXR loader and Rokid features configured.");
    }

    private static XRGeneralSettingsPerBuildTarget GetBuildTargetSettings()
    {
        if (EditorBuildSettings.TryGetConfigObject(
                LoaderSettingsKey,
                out XRGeneralSettingsPerBuildTarget buildTargetSettings))
        {
            return buildTargetSettings;
        }

        return AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
            "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
    }
}
