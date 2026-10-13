// Ashen Hollow: phone-sized textures for the glTF models.
// The glTF importer keeps the pictures packed inside a .glb uncompressed (a 2048 picture is 16 MB, mip-maps on top), and
// with a few hundred models the phone build passed Android's 4 GB limit. "Phone Textures" takes each picture out of the
// .glb into a file of its own beside it (<model>_tex/img<N>.png or .jpg) and points the model at that file, so Unity's
// own texture importer handles it: compressed (ASTC on phones), capped in size (1024; 2048 for a land's ground) and
// with mip-maps. Colour pictures stay sRGB; normal, roughness and occlusion maps are linear.
// The original .glb is copied first to <project>/GlbBackup/... so "Phone Textures: Restore" can put it back.
// Models only (no code involved): the files stay on this computer.
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class AHPhoneTex
{
    const string Backup = "GlbBackup";

    [MenuItem("Ashen Hollow/Phone Textures: Test One %#&1")]
    static void TestOne()
    {
        var p = "Assets/AshenHollow/Resources/AH/Models/Beasts/b_pachy.glb";
        string msg = Process(p); AssetDatabase.Refresh(); Debug.Log("Ashen Hollow: phone textures " + p + ": " + msg);
    }

    [MenuItem("Ashen Hollow/Phone Textures: Check One %#&4")]
    static void CheckOne()
    {
        var p = "Assets/AshenHollow/Resources/AH/Models/Beasts/b_pachy.glb"; var sb = new StringBuilder();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
        {
            var m = o as Material; if (m == null) continue;
            foreach (var pn in m.GetTexturePropertyNames()) { var t = m.GetTexture(pn); if (t != null) sb.Append(m.name + "." + pn + " = " + AssetDatabase.GetAssetPath(t) + " " + t.width + "x" + t.height + " " + (t is Texture2D ? ((Texture2D)t).format.ToString() : "") + "; "); }
        }
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p)) if (o is Texture2D) sb.Append(" [still inside: " + o.name + "]");
        Debug.Log("Ashen Hollow: check " + sb);
    }

    [MenuItem("Ashen Hollow/Phone Textures: All Models %#&2")]
    static void All()
    {
        var list = new List<string>();
        foreach (var dir in Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories))
            foreach (var f in Directory.GetFiles(dir, "*.glb", SearchOption.AllDirectories)) list.Add(f.Replace('\\', '/'));
        int done = 0, skip = 0; long before = 0; var log = new StringBuilder();
        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Phone textures", list[i], i / (float)list.Count)) break;
                before += new FileInfo(list[i]).Length;
                string r = Process(list[i]); log.AppendLine(list[i] + ": " + r);
                if (r.StartsWith("done")) done++; else skip++;
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/phone_textures.txt", log.ToString());
        Debug.Log("Ashen Hollow: phone textures: " + done + " models changed, " + skip + " left (Builds/phone_textures.txt)");
    }

    // tighter still for phones: ASTC 8x8 everywhere (about half the size of 6x6), and props, buildings and old web
    // models at 512; heroes' outfits, beasts, mounts, weapons and the lands keep their size
    [MenuItem("Ashen Hollow/Phone Textures: Squeeze %#&6")]
    static void Squeeze()
    {
        var paths = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/AshenHollow/Resources", "Assets/Polyart" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid); if (p.Contains("_tex/") || p.StartsWith("Assets/Polyart") || p.Contains("/VRoid/")) paths.Add(p);
        }
        int n = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var p in paths)
            {
                var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
                bool small = p.Contains("/Models/KK/") || p.Contains("/Models/Town/") || p.Contains("/Models/Nature/") || p.Contains("/Models/Web/") || p.Contains("/Models/QD/") || p.Contains("/Models/Mobs/");
                bool ground = p.Contains("/Areas/") || p.Contains("_world_tex/") || p.Contains("_world_kk_tex/");
                bool hero = p.Contains("/VRoid/");   // faces and hair stay sharp: 6x6
                int max = ground ? 2048 : small ? 512 : 1024;
                if (p.StartsWith("Assets/Polyart") || hero) { var cur = ti.GetPlatformTextureSettings("Android"); if (ti.textureType == TextureImporterType.NormalMap || hero) { if (cur.overridden && cur.maxTextureSize <= 1024) continue; cur.overridden = true; cur.maxTextureSize = 1024; cur.format = TextureImporterFormat.ASTC_6x6; ti.SetPlatformTextureSettings(cur); ti.SaveAndReimport(); n++; continue; } }
                var a = ti.GetPlatformTextureSettings("Android");
                if (a.overridden && a.maxTextureSize == max && a.format == TextureImporterFormat.ASTC_8x8) continue;
                a.overridden = true; a.maxTextureSize = max; a.format = ground ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_8x8; a.compressionQuality = 50;
                ti.SetPlatformTextureSettings(a); ti.SaveAndReimport(); n++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        Debug.Log("Ashen Hollow: phone textures squeezed: " + n + " of " + paths.Count);
    }

    [MenuItem("Ashen Hollow/Phone Textures: Restore %#&3")]
    static void Restore()
    {
        if (!Directory.Exists(Backup)) return; int n = 0;
        foreach (var f in Directory.GetFiles(Backup, "*.glb", SearchOption.AllDirectories))
        {
            string dst = f.Substring(Backup.Length + 1).Replace('\\', '/');
            File.Copy(f, dst, true); n++;
            string tex = dst.Substring(0, dst.Length - 4) + "_tex";
            if (AssetDatabase.IsValidFolder(tex)) AssetDatabase.DeleteAsset(tex);
        }
        AssetDatabase.Refresh(); Debug.Log("Ashen Hollow: phone textures: " + n + " models put back");
    }

    // one top-level JSON array (by name) split into its objects, with their places in the text
    static List<KeyValuePair<int, int>> Objects(string json, string key, out int arrStart, out int arrEnd)
    {
        var res = new List<KeyValuePair<int, int>>(); arrStart = arrEnd = -1;
        int k = json.IndexOf("\"" + key + "\"");
        while (k >= 0)
        {
            int c = k + key.Length + 2; while (c < json.Length && char.IsWhiteSpace(json[c])) c++;
            if (c < json.Length && json[c] == ':') { c++; while (char.IsWhiteSpace(json[c])) c++; if (json[c] == '[') { arrStart = c; break; } }
            k = json.IndexOf("\"" + key + "\"", k + 1);
        }
        if (arrStart < 0) return res;
        int depth = 0, objStart = -1; bool str = false;
        for (int i = arrStart; i < json.Length; i++)
        {
            char ch = json[i];
            if (str) { if (ch == '\\') i++; else if (ch == '"') str = false; continue; }
            if (ch == '"') { str = true; continue; }
            if (ch == '[' || ch == '{') { depth++; if (ch == '{' && depth == 2) objStart = i; }
            else if (ch == ']' || ch == '}') { depth--; if (ch == '}' && depth == 1) res.Add(new KeyValuePair<int, int>(objStart, i)); if (depth == 0) { arrEnd = i; break; } }
        }
        return res;
    }

    static int Num(string obj, string key, int def)
    {
        var m = Regex.Match(obj, "\"" + key + "\"\\s*:\\s*(\\d+)"); return m.Success ? int.Parse(m.Groups[1].Value) : def;
    }

    static string Process(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 20 || bytes[0] != 'g' || bytes[1] != 'l' || bytes[2] != 'T' || bytes[3] != 'F') return "not a glb";
        int jsonLen = System.BitConverter.ToInt32(bytes, 12);
        string json = Encoding.UTF8.GetString(bytes, 20, jsonLen);
        int binStart = 20 + jsonLen, binLen = 0, binData = 0;
        if (binStart + 8 <= bytes.Length) { binLen = System.BitConverter.ToInt32(bytes, binStart); binData = binStart + 8; }
        int a0, a1; var imgs = Objects(json, "images", out a0, out a1);
        if (imgs.Count == 0) return "no pictures";
        bool anyPacked = false; foreach (var o in imgs) if (json.Substring(o.Key, o.Value - o.Key + 1).Contains("\"bufferView\"")) anyPacked = true;
        if (!anyPacked) return "already done";
        int b0, b1; var views = Objects(json, "bufferViews", out b0, out b1);
        int t0, t1; var texs = Objects(json, "textures", out t0, out t1);
        // which pictures hold data rather than colour
        var linear = new HashSet<int>(); bool ground = path.Contains("/Areas/") || path.EndsWith("_world.glb");
        foreach (Match m in Regex.Matches(json, "\"(normalTexture|metallicRoughnessTexture|occlusionTexture)\"\\s*:\\s*\\{[^}]*?\"index\"\\s*:\\s*(\\d+)"))
        {
            int ti = int.Parse(m.Groups[2].Value); if (ti < texs.Count) { int src = Num(json.Substring(texs[ti].Key, texs[ti].Value - texs[ti].Key + 1), "source", -1); if (src >= 0) linear.Add(src); }
        }
        string name = Path.GetFileNameWithoutExtension(path), dir = Path.GetDirectoryName(path).Replace('\\', '/'), texDir = dir + "/" + name + "_tex";
        Directory.CreateDirectory(texDir);
        // back up the original once
        string bak = Backup + "/" + path; if (!File.Exists(bak)) { Directory.CreateDirectory(Path.GetDirectoryName(bak)); File.Copy(path, bak); }
        var sb = new StringBuilder(); int last = a0 + 1; sb.Append(json, 0, a0 + 1);
        var written = new List<KeyValuePair<string, bool>>();
        for (int i = 0; i < imgs.Count; i++)
        {
            var o = imgs[i]; string obj = json.Substring(o.Key, o.Value - o.Key + 1);
            sb.Append(json, last, o.Key - last); last = o.Value + 1;
            int bv = Num(obj, "bufferView", -1);
            if (bv < 0 || bv >= views.Count) { sb.Append(obj); continue; }
            string view = json.Substring(views[bv].Key, views[bv].Value - views[bv].Key + 1);
            int off = Num(view, "byteOffset", 0), len = Num(view, "byteLength", 0);
            bool jpg = obj.Contains("image/jpeg");
            string file = "img" + i + (jpg ? ".jpg" : ".png");
            var data = new byte[len]; System.Array.Copy(bytes, binData + off, data, 0, len);
            File.WriteAllBytes(texDir + "/" + file, data);
            written.Add(new KeyValuePair<string, bool>(texDir + "/" + file, linear.Contains(i)));
            sb.Append("{\"uri\":\"" + name + "_tex/" + file + "\",\"mimeType\":\"" + (jpg ? "image/jpeg" : "image/png") + "\"}");
        }
        sb.Append(json, last, json.Length - last);
        // the new JSON chunk (padded with spaces to four bytes), the BIN chunk as it was
        var nj = Encoding.UTF8.GetBytes(sb.ToString()); int pad = (4 - nj.Length % 4) % 4;
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            int total = 12 + 8 + nj.Length + pad + (binLen > 0 ? 8 + binLen : 0);
            w.Write(0x46546C67); w.Write(2); w.Write(total);
            w.Write(nj.Length + pad); w.Write(0x4E4F534A); w.Write(nj); for (int i = 0; i < pad; i++) w.Write((byte)0x20);
            if (binLen > 0) w.Write(bytes, binStart, 8 + binLen);
            File.WriteAllBytes(path, ms.ToArray());
        }
        // the pictures' import settings
        foreach (var kv in written)
        {
            AssetDatabase.ImportAsset(kv.Key, ImportAssetOptions.ForceSynchronousImport);
            var ti = AssetImporter.GetAtPath(kv.Key) as TextureImporter; if (ti == null) continue;
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = !kv.Value; ti.mipmapEnabled = true;
            ti.maxTextureSize = ground ? 2048 : 1024; ti.textureCompression = TextureImporterCompression.Compressed; ti.isReadable = false;
            var android = ti.GetPlatformTextureSettings("Android"); android.overridden = true; android.maxTextureSize = ground ? 2048 : 1024; android.format = TextureImporterFormat.ASTC_6x6; ti.SetPlatformTextureSettings(android);
            ti.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return "done (" + written.Count + " pictures)";
    }

    // Vertex colours that are all white do nothing (the colour comes from the picture) but every vertex still carries
    // them: in the outfits that is a sixth of the mesh. "Phone Meshes: Drop White Colours" takes them out of every model
    // whose vertex colours are pure white (models that really are coloured by their vertices are left alone), and the
    // extra colour sets glTF allows (which the game never reads) with them.
    [MenuItem("Ashen Hollow/Phone Meshes: Drop White Colours %#&0")]
    static void DropWhite()
    {
        var list = new List<string>();
        foreach (var dir in Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories))
            foreach (var f in Directory.GetFiles(dir, "*.glb", SearchOption.AllDirectories)) list.Add(f.Replace('\\', '/'));
        int done = 0; var log = new StringBuilder();
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < list.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Drop white colours", list[i], i / (float)list.Count)) break;
                string r = WhiteColours(list[i]); if (r.StartsWith("done")) { done++; log.AppendLine(list[i] + ": " + r); }
                else if (r != "no colours") log.AppendLine(list[i] + ": " + r);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); EditorUtility.ClearProgressBar(); }
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/white_colours.txt", log.ToString());
        Debug.Log("Ashen Hollow: white vertex colours dropped from " + done + " models (Builds/white_colours.txt)");
    }

    static string WhiteColours(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 20 || bytes[0] != 'g' || bytes[1] != 'l' || bytes[2] != 'T' || bytes[3] != 'F') return "not a glb";
        int jsonLen = System.BitConverter.ToInt32(bytes, 12);
        string json = Encoding.UTF8.GetString(bytes, 20, jsonLen);
        if (!json.Contains("\"COLOR_")) return "no colours";
        int binStart = 20 + jsonLen, binLen = 0, binData = 0;
        if (binStart + 8 <= bytes.Length) { binLen = System.BitConverter.ToInt32(bytes, binStart); binData = binStart + 8; }
        int a0, a1; var accs = Objects(json, "accessors", out a0, out a1);
        int b0, b1; var views = Objects(json, "bufferViews", out b0, out b1);
        // every COLOR_0 must be white
        foreach (Match m in Regex.Matches(json, "\"COLOR_0\"\\s*:\\s*(\\d+)"))
        {
            int ai = int.Parse(m.Groups[1].Value); if (ai >= accs.Count) return "bad accessor";
            string acc = json.Substring(accs[ai].Key, accs[ai].Value - accs[ai].Key + 1);
            int bv = Num(acc, "bufferView", -1); if (bv < 0 || bv >= views.Count || binData == 0) return "colour not in the file";
            string view = json.Substring(views[bv].Key, views[bv].Value - views[bv].Key + 1);
            int count = Num(acc, "count", 0), ct = Num(acc, "componentType", 5126), off = binData + Num(view, "byteOffset", 0) + Num(acc, "byteOffset", 0);
            int comps = acc.Contains("\"VEC4\"") ? 4 : 3, csz = ct == 5126 ? 4 : ct == 5123 ? 2 : 1, stride = Num(view, "byteStride", comps * csz);
            for (int v = 0; v < count; v++)
                for (int c = 0; c < 3; c++)   // red, green, blue (alpha does not tint)
                {
                    int p = off + v * stride + c * csz; float x;
                    if (ct == 5126) x = System.BitConverter.ToSingle(bytes, p); else if (ct == 5123) x = System.BitConverter.ToUInt16(bytes, p) / 65535f; else x = bytes[p] / 255f;
                    if (x < 0.995f) return "coloured (kept)";
                }
        }
        string bak = Backup + "/" + path; if (!File.Exists(bak)) { Directory.CreateDirectory(Path.GetDirectoryName(bak)); File.Copy(path, bak); }
        int before = json.Length;
        json = Regex.Replace(json, ",\\s*\"COLOR_\\d+\"\\s*:\\s*\\d+", "");
        json = Regex.Replace(json, "\"COLOR_\\d+\"\\s*:\\s*\\d+\\s*,", "");
        var nj = Encoding.UTF8.GetBytes(json); int pad = (4 - nj.Length % 4) % 4;
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            int total = 12 + 8 + nj.Length + pad + (binLen > 0 ? 8 + binLen : 0);
            w.Write(0x46546C67); w.Write(2); w.Write(total);
            w.Write(nj.Length + pad); w.Write(0x4E4F534A); w.Write(nj); for (int i = 0; i < pad; i++) w.Write((byte)0x20);
            if (binLen > 0) w.Write(bytes, binStart, 8 + binLen);
            File.WriteAllBytes(path, ms.ToArray());
        }
        return "done";
    }
}
