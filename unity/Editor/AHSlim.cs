// Ashen Hollow: getting the game ready for phones.
// Ashen Hollow > Slim Build:
//  1. moves each area's old "_world" model out of Resources when a newer "_world_kk" exists (the game only loads the
//     _kk one), so about 65 MB stops going into the build. The files are kept in Assets/AshenHollow/Unused.
//  2. makes three small materials that keep the Lit shader variants the game turns on in code (glow, bumps), so they
//     are not stripped from a phone build and nothing turns pink.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHSlim
{
    [MenuItem("Ashen Hollow/Slim Build")]
    public static void Slim()
    {
        string root = "Assets/AshenHollow", areas = root + "/Resources/AH/Areas", unused = root + "/Unused";
        if (!AssetDatabase.IsValidFolder(unused)) AssetDatabase.CreateFolder(root, "Unused");
        int moved = 0; long bytes = 0;
        foreach (var kk in Directory.GetFiles(areas, "*_world_kk.glb"))
        {
            string old = kk.Replace("_world_kk.glb", "_world.glb").Replace('\\', '/');
            if (!File.Exists(old)) continue;
            bytes += new FileInfo(old).Length;
            string err = AssetDatabase.MoveAsset(old, unused + "/" + Path.GetFileName(old));
            if (string.IsNullOrEmpty(err)) moved++; else Debug.LogWarning("Ashen Hollow: could not move " + old + ": " + err);
        }

        string matDir = root + "/Resources/AH/Materials";
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit != null)
        {
            Keep(lit, matDir + "/Keep_Glow.mat", true, false);
            Keep(lit, matDir + "/Keep_Bump.mat", false, true);
            Keep(lit, matDir + "/Keep_GlowBump.mat", true, true);
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Ashen Hollow: slim build: " + moved + " unused world models moved out of the build (" + (bytes / 1048576) + " MB); shader keep materials ready");
    }

    static void Keep(Shader s, string path, bool glow, bool bump)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var m = new Material(s);
        if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.white); }
        if (bump) { m.EnableKeyword("_NORMALMAP"); m.SetTexture("_BumpMap", Texture2D.normalTexture); }
        AssetDatabase.CreateAsset(m, path);
    }
}
