// Ashen Hollow: one-click phone build.
// Ashen Hollow > Build Android APK sets the phone settings (name, id, landscape, 64-bit IL2CPP, ASTC textures) and
// writes Builds/AshenHollow.apk next to the Assets folder. Copy that file to an Android phone and open it to install
// (allow "install unknown apps" for your file manager when Android asks).
// The settings alone: Ashen Hollow > Android Settings Only.
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AHBuild
{
    [MenuItem("Ashen Hollow/Android Settings Only")]
    public static void Settings()
    {
        PlayerSettings.companyName = "Ashen Hollow";
        PlayerSettings.productName = "Ashen Hollow";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.ashenhollow.game");
        PlayerSettings.bundleVersion = "0.3.0";
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(PlayerSettings.Android.bundleVersionCode, 3);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        EditorUserBuildSettings.buildAppBundle = false;   // an .apk you can copy straight to a phone
        AssetDatabase.SaveAssets();
        Debug.Log("Ashen Hollow: Android settings ready");
    }

    [MenuItem("Ashen Hollow/Build Android APK")]
    public static void BuildApk()
    {
        Settings();
        string scene = null;
        foreach (var guid in AssetDatabase.FindAssets("AshenMeadow t:Scene")) { scene = AssetDatabase.GUIDToAssetPath(guid); break; }
        if (scene == null) { EditorUtility.DisplayDialog("Ashen Hollow", "The AshenMeadow scene was not found. Run Ashen Hollow > Set Up Meadow Scene first.", "OK"); return; }
        Directory.CreateDirectory("Builds");
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { scene },
            locationPathName = "Builds/AshenHollow.apk",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        var s = report.summary;
        string msg = s.result == BuildResult.Succeeded
            ? "Ashen Hollow: APK built: Builds/AshenHollow.apk (" + (s.totalSize / 1048576) + " MB) in " + s.totalTime.ToString(@"hh\:mm\:ss")
            : "Ashen Hollow: build " + s.result + " with " + s.totalErrors + " errors";
        Debug.Log(msg);
        File.WriteAllText("Builds/build_result.txt", msg + "\n");
    }
}
