// Ashen Hollow: "Phone Size Report". What goes into a phone build and how big each piece is: everything under a
// Resources folder and everything those use (textures, meshes, sounds), by size in memory on the current build target.
// Writes Builds/size_report.txt: totals by folder and kind, the 80 largest files, and the textures that are stored
// uncompressed (each one many times the size it needs to be).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;

public static class AHPhoneSize
{
    // what really went into the last build, from Unity's own build report
    [MenuItem("Ashen Hollow/Last Build Contents %#&7")]
    public static void LastBuild()
    {
        var rep = BuildReport.GetLatestReport(); if (rep == null) { Debug.Log("Ashen Hollow: no build report"); return; }
        var byType = new Dictionary<string, ulong>(); var byFile = new Dictionary<string, ulong>(); ulong total = 0;
        foreach (var pa in rep.packedAssets)
            foreach (var c in pa.contents)
            {
                string t = c.type != null ? c.type.Name : "?"; ulong v; byType.TryGetValue(t, out v); byType[t] = v + c.packedSize;
                byFile.TryGetValue(c.sourceAssetPath, out v); byFile[c.sourceAssetPath] = v + c.packedSize; total += c.packedSize;
            }
        var sb = new StringBuilder();
        sb.AppendLine("Last build " + rep.summary.platform + " " + rep.summary.result + ": " + (rep.summary.totalSize / 1048576) + " MB file, " + (total / 1048576) + " MB of assets");
        foreach (var kv in byType.OrderByDescending(x => x.Value).Take(15)) sb.AppendLine("  " + (kv.Value / 1048576).ToString().PadLeft(6) + " MB  " + kv.Key);
        var byDir = new Dictionary<string, ulong>();
        foreach (var kv in byFile) { var parts = kv.Key.Split('/'); string d = string.Join("/", parts.Take(Mathf.Min(parts.Length - 1, 6))); ulong v; byDir.TryGetValue(d, out v); byDir[d] = v + kv.Value; }
        sb.AppendLine("By folder:");
        foreach (var kv in byDir.OrderByDescending(x => x.Value).Take(30)) sb.AppendLine("  " + (kv.Value / 1048576).ToString().PadLeft(6) + " MB  " + kv.Key);
        sb.AppendLine("Largest:");
        foreach (var kv in byFile.OrderByDescending(x => x.Value).Take(60)) sb.AppendLine("  " + (kv.Value / 1048576).ToString().PadLeft(6) + " MB  " + kv.Key);
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/last_build.txt", sb.ToString());
        Debug.Log("Ashen Hollow: last build contents in Builds/last_build.txt");
    }

    // the heaviest models by triangles (the meshes are most of a phone build now)
    [MenuItem("Ashen Hollow/Mesh Weight Report %#&8")]
    public static void Meshes()
    {
        var rows = new List<KeyValuePair<long, string>>(); long all = 0;
        foreach (var dir in Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories))
            foreach (var f in Directory.GetFiles(dir, "*.glb", SearchOption.AllDirectories))
            {
                string p = f.Replace('\\', '/'); long tris = 0, verts = 0; int n = 0; bool readable = false, blend = false;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    var m = o as Mesh; if (m == null) continue; n++; verts += m.vertexCount; readable |= m.isReadable; blend |= m.blendShapeCount > 0;
                    for (int s2 = 0; s2 < m.subMeshCount; s2++) tris += m.GetIndexCount(s2) / 3;
                }
                all += tris;
                rows.Add(new KeyValuePair<long, string>(tris, (tris / 1000) + "k tris, " + (verts / 1000) + "k verts, " + n + " meshes" + (readable ? " readable" : "") + (blend ? " blendshapes" : "") + "  " + p));
            }
        var sb = new StringBuilder(); sb.AppendLine("All glb models: " + (all / 1000) + "k triangles");
        var byDir = new Dictionary<string, long>();
        foreach (var r in rows) { string d = r.Value.Substring(r.Value.IndexOf("Assets/")); d = Path.GetDirectoryName(d).Replace('\\', '/'); long v; byDir.TryGetValue(d, out v); byDir[d] = v + r.Key; }
        foreach (var kv in byDir.OrderByDescending(x => x.Value)) sb.AppendLine("  " + (kv.Value / 1000) + "k  " + kv.Key);
        foreach (var r in rows.OrderByDescending(x => x.Key).Take(120)) sb.AppendLine(r.Value);
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/mesh_report.txt", sb.ToString());
        Debug.Log("Ashen Hollow: mesh report in Builds/mesh_report.txt");
    }

    [MenuItem("Ashen Hollow/Phone Size Report #&z")]
    public static void Report()
    {
        var roots = new List<string>();
        foreach (var dir in Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories))
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) if (!f.EndsWith(".meta")) roots.Add(f.Replace('\\', '/'));
        var all = new HashSet<string>();
        foreach (var d in AssetDatabase.GetDependencies(roots.ToArray(), true)) all.Add(d);
        var byFile = new Dictionary<string, long>(); var byKind = new Dictionary<string, long>(); var raw = new List<string>();
        int n = 0;
        foreach (var path in all)
        {
            if (path.EndsWith(".cs") || path.EndsWith(".shader") || path.EndsWith(".hlsl")) continue;
            if (++n % 200 == 0) EditorUtility.DisplayProgressBar("Phone size", path, n / (float)all.Count);
            long sum = 0;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (o == null) continue;
                string kind = o is Texture ? "textures" : o is Mesh ? "meshes" : o is AudioClip ? "sounds" : o is AnimationClip ? "animations" : null;
                if (kind == null) continue;
                long s = Profiler.GetRuntimeMemorySizeLong(o); sum += s;
                long k; byKind.TryGetValue(kind, out k); byKind[kind] = k + s;
                var t = o as Texture2D;
                if (t != null && s > 4L * 1024 * 1024 && (t.format == TextureFormat.RGBA32 || t.format == TextureFormat.RGB24 || t.format == TextureFormat.ARGB32 || t.format == TextureFormat.RGBAHalf))
                    raw.Add((s / 1048576) + " MB\t" + t.width + "x" + t.height + " " + t.format + "\t" + path + " : " + t.name);
            }
            if (sum > 0) byFile[path] = sum;
        }
        EditorUtility.ClearProgressBar();
        var sb = new StringBuilder();
        long total = byFile.Values.Sum();
        sb.AppendLine("Build target " + EditorUserBuildSettings.activeBuildTarget + ": " + (total / 1048576) + " MB in " + byFile.Count + " files (" + roots.Count + " Resources files)");
        foreach (var kv in byKind.OrderByDescending(x => x.Value)) sb.AppendLine("  " + kv.Key + ": " + (kv.Value / 1048576) + " MB");
        sb.AppendLine("\nBy folder:");
        var byDir = new Dictionary<string, long>();
        foreach (var kv in byFile) { var parts = kv.Key.Split('/'); string d = string.Join("/", parts.Take(Mathf.Min(parts.Length - 1, 6))); long v; byDir.TryGetValue(d, out v); byDir[d] = v + kv.Value; }
        foreach (var kv in byDir.OrderByDescending(x => x.Value).Take(40)) sb.AppendLine("  " + (kv.Value / 1048576).ToString().PadLeft(6) + " MB  " + kv.Key);
        sb.AppendLine("\nLargest files:");
        foreach (var kv in byFile.OrderByDescending(x => x.Value).Take(80)) sb.AppendLine("  " + (kv.Value / 1048576).ToString().PadLeft(6) + " MB  " + kv.Key);
        sb.AppendLine("\nUncompressed textures over 4 MB (" + raw.Count + "):");
        foreach (var r in raw.OrderByDescending(x => int.Parse(x.Split(' ')[0])).Take(120)) sb.AppendLine("  " + r);
        Directory.CreateDirectory("Builds");
        File.WriteAllText("Builds/size_report.txt", sb.ToString());
        Debug.Log("Ashen Hollow: phone size report: " + (total / 1048576) + " MB (Builds/size_report.txt)");
    }
}
