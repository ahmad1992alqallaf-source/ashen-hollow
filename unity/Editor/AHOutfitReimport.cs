// Ashen Hollow: "Outfits: Reimport" (ctrl+shift+alt+F11). Imports every suit (Resources/AH/Models/Outfits/*.glb) again
// and counts the pictures each one carries inside itself. A suit's pictures should come from its <suit>_tex folder
// (compressed for phones); a copy kept inside the .glb is uncompressed and many times bigger. That happens when the
// glTF importer reads a suit before its pictures are imported, so this imports the pictures first, then the suits,
// and writes what it finds to Builds/outfit_textures.txt.
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHOutfitReimport
{
    const string Dir = "Assets/AshenHollow/Resources/AH/Models/Outfits";

    static int Inside(string glb) { int n = 0; foreach (var o in AssetDatabase.LoadAllAssetsAtPath(glb)) if (o is Texture2D) n++; return n; }

    [MenuItem("Ashen Hollow/Outfits: Reimport %#&F11")]
    static void Run()
    {
        var sb = new StringBuilder(); int before = 0, after = 0, files = 0;
        var glbs = Directory.GetFiles(Dir, "*.glb");
        foreach (var g in glbs) before += Inside(g.Replace('\\', '/'));
        // the pictures first, so the suits find them as finished textures
        foreach (var d in Directory.GetDirectories(Dir, "*_tex"))
            foreach (var f in Directory.GetFiles(d))
                if (!f.EndsWith(".meta")) AssetDatabase.ImportAsset(f.Replace('\\', '/'), ImportAssetOptions.ForceSynchronousImport);
        foreach (var g in glbs)
        {
            string p = g.Replace('\\', '/');
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            int n = Inside(p); after += n; files++;
            if (n > 0) sb.AppendLine(n + " pictures inside  " + Path.GetFileName(p));
        }
        string head = "Suits: " + files + " · pictures kept inside the suits: " + before + " before, " + after + " after\n";
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/outfit_textures.txt", head + sb);
        Debug.Log("Ashen Hollow: outfits reimported · " + head.Trim());
    }
}
