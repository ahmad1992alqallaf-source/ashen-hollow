// Ashen Hollow (editor): makes Resources/AH/Materials/Range.mat and CaveStone.mat for the mountain-range and cave-wall shaders, so the shader is part of
// every build (a shader found only by name at run time is left out of phone builds).
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AHRangeMat
{
    const string Path = "Assets/AshenHollow/Resources/AH/Materials/Range.mat";
    static AHRangeMat() { EditorApplication.delayCall += Make; }
    static void Make()
    {
        One("AshenHollow/Range", Path);
        One("AshenHollow/CaveStone", "Assets/AshenHollow/Resources/AH/Materials/CaveStone.mat");   // the cave walls' rock grain
    }
    static void One(string shader, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var sh = Shader.Find(shader); if (sh == null) return;
        AssetDatabase.CreateAsset(new Material(sh) { name = System.IO.Path.GetFileNameWithoutExtension(path) }, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Ashen Hollow: made " + path);
    }
}
