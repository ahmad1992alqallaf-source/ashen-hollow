// Ashen Hollow: imports a downloaded Asset Store pack straight from Unity's download cache, without the import window.
using UnityEditor;

public static class AHImportPack
{
    [MenuItem("Ashen Hollow/Setup: Import CraftPix 6200 RPG Icons")]
    static void Icons6200()
    {
        string p = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "/Unity/Asset Store-5.x/CraftPix/Textures MaterialsIcons UI/6200 Fantasy RPG Icons Pack.unitypackage";
        if (!System.IO.File.Exists(p)) { UnityEngine.Debug.LogWarning("Ashen Hollow: icon pack not found at " + p); return; }
        UnityEngine.Debug.Log("Ashen Hollow: importing the 6200 RPG icons (this takes a while)");
        AssetDatabase.ImportPackage(p, false);
    }

    [MenuItem("Ashen Hollow/Setup: Import Dreamscape URP Materials %&4")]
    static void DreamURP()
    {
        string p = "Assets/Polyart/PolyartStudio/DreamscapeMeadows/MeadowsURP Click me! Unity 6.0.unitypackage";
        if (!System.IO.File.Exists(p)) { UnityEngine.Debug.LogWarning("Ashen Hollow: not found " + p); return; }
        UnityEngine.Debug.Log("Ashen Hollow: importing the Dreamscape URP materials");
        AssetDatabase.ImportPackage(p, false);
    }
}
