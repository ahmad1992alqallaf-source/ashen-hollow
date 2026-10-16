// Ashen Hollow: getting the game ready for phones.
// Ashen Hollow > Slim Build:
//  1. moves each area's old "_world" model out of Resources when a newer "_world_kk" exists (the game only loads the
//     _kk one), so about 65 MB stops going into the build. The files are kept in Assets/AshenHollow/Unused.
//  2. moves the plain animal, mount and pet models that have a detailed "_hd" twin (the game only loads the _hd one)
//     to Assets/AshenHollow/Unused/Twins, and pictures in a model's "_tex" folder that its .glb no longer points at
//     (left over from an older version of the model) to Unused/Pictures.
//  3. makes three small materials that keep the Lit shader variants the game turns on in code (glow, bumps), so they
//     are not stripped from a phone build and nothing turns pink.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHSlim
{
    // models no longer in the game: moved out of Resources so they stop going into the build
    static readonly string[] Retired = { "Models/Comp/mount_tyrant_hd.glb" };

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

        // models taken out of the game (their data entries are gone too)
        foreach (var r in Retired)
        {
            string p = root + "/Resources/AH/" + r; if (!File.Exists(p)) continue;
            bytes += new FileInfo(p).Length; AssetDatabase.MoveAsset(p, unused + "/" + Path.GetFileName(p));
            string tex = p.Replace(".glb", "_tex"); if (AssetDatabase.IsValidFolder(tex)) AssetDatabase.MoveAsset(tex, unused + "/" + Path.GetFileName(tex));
            moved++;
        }

        int twins = 0, pics = 0;
        string twinDir = unused + "/Twins", picDir = unused + "/Pictures";
        if (!AssetDatabase.IsValidFolder(twinDir)) AssetDatabase.CreateFolder(unused, "Twins");
        if (!AssetDatabase.IsValidFolder(picDir)) AssetDatabase.CreateFolder(unused, "Pictures");
        foreach (var sub in new[] { "Beasts", "Comp" })
            foreach (var f in Directory.GetFiles(root + "/Resources/AH/Models/" + sub, "*.glb"))
            {
                string p = f.Replace('\\', '/'); if (p.EndsWith("_hd.glb") || !File.Exists(p.Replace(".glb", "_hd.glb"))) continue;
                bytes += new FileInfo(p).Length;
                if (string.IsNullOrEmpty(AssetDatabase.MoveAsset(p, twinDir + "/" + Path.GetFileName(p)))) twins++;
                string tex = p.Replace(".glb", "_tex"); if (AssetDatabase.IsValidFolder(tex)) AssetDatabase.MoveAsset(tex, twinDir + "/" + Path.GetFileName(tex));
            }
        foreach (var dir in Directory.GetDirectories(root + "/Resources", "*_tex", SearchOption.AllDirectories))
        {
            string d = dir.Replace('\\', '/'), glb = d.Substring(0, d.Length - 4) + ".glb";
            if (!File.Exists(glb)) continue;
            string json = GlbJson(glb); if (json == null) continue;
            foreach (var f in Directory.GetFiles(d))
            {
                if (f.EndsWith(".meta")) continue;
                string name = Path.GetFileName(f);
                if (json.Contains(name)) continue;   // (kept whenever the model names the file at all)
                string dst = picDir + "/" + Path.GetFileName(d); if (!AssetDatabase.IsValidFolder(dst)) AssetDatabase.CreateFolder(picDir, Path.GetFileName(d));
                bytes += new FileInfo(f).Length;
                if (string.IsNullOrEmpty(AssetDatabase.MoveAsset(f.Replace('\\', '/'), dst + "/" + name))) pics++;
            }
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
        Debug.Log("Ashen Hollow: slim build: " + moved + " unused or retired models, " + twins + " plain twins of _hd models and " + pics + " left-over pictures moved out of the build (" + (bytes / 1048576) + " MB on disk); shader keep materials ready");
    }

    // the JSON part of a .glb (null if it is not one)
    static string GlbJson(string path)
    {
        var b = File.ReadAllBytes(path);
        if (b.Length < 20 || b[0] != 'g' || b[1] != 'l' || b[2] != 'T' || b[3] != 'F') return null;
        return System.Text.Encoding.UTF8.GetString(b, 20, System.BitConverter.ToInt32(b, 12));
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
