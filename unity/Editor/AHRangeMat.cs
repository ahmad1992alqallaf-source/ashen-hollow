// Ashen Hollow (editor): makes Resources/AH/Materials/Range.mat for the mountain-range shader, so the shader is part of
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
        if (AssetDatabase.LoadAssetAtPath<Material>(Path) != null) return;
        var sh = Shader.Find("AshenHollow/Range"); if (sh == null) return;
        AssetDatabase.CreateAsset(new Material(sh) { name = "Range" }, Path);
        AssetDatabase.SaveAssets();
        Debug.Log("Ashen Hollow: made " + Path);
    }
}
