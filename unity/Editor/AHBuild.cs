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
        // keep Unity's original names: the editor keeps your saved hero under them (changing them hides the save)
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "AshenHollow";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.ashenhollow.game");
        // the version testers see is "0.9.<build>"; the build number only goes up (each phone build adds one, which
        // Firebase and Google Play need to offer it as an update)
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(PlayerSettings.Android.bundleVersionCode, 3);
        PlayerSettings.bundleVersion = Major + "." + PlayerSettings.Android.bundleVersionCode;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
        PlayerSettings.stripEngineCode = false;   // the game makes colliders and other engine parts in code, which stripping removed on phones
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        EditorUserBuildSettings.buildAppBundle = false;   // an .apk you can copy straight to a phone
        AssetDatabase.SaveAssets();
        AlwaysInclude("Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit", "Unlit/Color", "AshenHollow/Range");
        Debug.Log("Ashen Hollow: Android settings ready");
    }

    // shaders the game finds by name in code (Shader.Find) and that no material in the build uses: without this a
    // phone build leaves them out, Shader.Find gives nothing there, and whatever needed them fails (the hero hall
    // came up blue and stuck on phones)
    static void AlwaysInclude(params string[] names)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (all == null || all.Length == 0) return;
        var so = new SerializedObject(all[0]); var arr = so.FindProperty("m_AlwaysIncludedShaders"); if (arr == null) return;
        int added = 0;
        foreach (var n in names)
        {
            var sh = Shader.Find(n); if (sh == null) { Debug.LogWarning("Ashen Hollow: shader not found: " + n); continue; }
            bool have = false; for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { have = true; break; }
            if (have) continue;
            arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; added++;
        }
        if (added > 0) { so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); Debug.Log("Ashen Hollow: " + added + " shaders always included in builds"); }
    }

    [MenuItem("Ashen Hollow/Build Android APK %#&5")]
    public static void BuildApk() { Build(false); }

    // builds, installs on the phone picked as Run Device (USB debugging on) and starts it
    [MenuItem("Ashen Hollow/Build And Run On Phone")]
    public static void BuildAndRun() { Build(true); }

    const string Major = "0.9";   // raise to 1.0 for the release
    const string InfoFile = "Assets/AshenHollow/Resources/AH/Data/build.txt";

    // the next build number, and the file the game reads it from (Settings > Version, feedback notes)
    static void NextBuild()
    {
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(PlayerSettings.Android.bundleVersionCode, 3) + 1;
        File.WriteAllText(InfoFile, PlayerSettings.Android.bundleVersionCode + "|" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");
        AssetDatabase.ImportAsset(InfoFile, ImportAssetOptions.ForceSynchronousImport);
    }

    static void Build(bool run)
    {
        NextBuild();
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
            options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None   // (LZ4HC was tried: 717 MB against 648; the APK's own zip packs the loose data better)
        };
        var report = BuildPipeline.BuildPlayer(opts);
        var s = report.summary;
        string msg = s.result == BuildResult.Succeeded
            ? "Ashen Hollow: APK built: Builds/AshenHollow.apk · version " + PlayerSettings.bundleVersion + " (build " + PlayerSettings.Android.bundleVersionCode + ") · " + (File.Exists("Builds/AshenHollow.apk") ? new FileInfo("Builds/AshenHollow.apk").Length / 1048576 : (long)(s.totalSize / 1048576)) + " MB in " + s.totalTime.ToString(@"hh\:mm\:ss")
            : "Ashen Hollow: build " + s.result + " with " + s.totalErrors + " errors";
        Debug.Log(msg);
        File.WriteAllText("Builds/build_result.txt", msg + "\n");
    }
}
