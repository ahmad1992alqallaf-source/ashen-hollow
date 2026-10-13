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

    // the animations by model: how much each model's clips take (the clips are most of what is left after the meshes
    // and pictures), with how many clips and curves, to see which are worth thinning
    [MenuItem("Ashen Hollow/Animation Weight Report")]
    public static void Anims()
    {
        var rows = new List<KeyValuePair<long, string>>(); long all = 0;
        var files = new List<string>();
        foreach (var dir in Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories))
            foreach (var f in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) if (f.EndsWith(".glb") || f.EndsWith(".fbx") || f.EndsWith(".anim") || f.EndsWith(".vrm")) files.Add(f.Replace('\\', '/'));
        foreach (var dep in AssetDatabase.GetDependencies(files.ToArray(), true)) if ((dep.EndsWith(".fbx") || dep.EndsWith(".anim") || dep.EndsWith(".glb")) && !files.Contains(dep)) files.Add(dep);
        int n = 0;
        foreach (var p in files)
        {
            if (++n % 50 == 0) EditorUtility.DisplayProgressBar("Animation weight", p, n / (float)files.Count);
            long sum = 0; int clips = 0, curves = 0, keys = 0;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                var c = o as AnimationClip; if (c == null) continue;
                clips++; sum += Profiler.GetRuntimeMemorySizeLong(c);
                foreach (var b in AnimationUtility.GetCurveBindings(c)) { curves++; var cv = AnimationUtility.GetEditorCurve(c, b); if (cv != null) keys += cv.length; }
            }
            if (clips == 0) continue; all += sum;
            rows.Add(new KeyValuePair<long, string>(sum, (sum / 1024) + " KB  " + clips + " clips, " + curves + " curves, " + keys + " keys  " + p));
        }
        EditorUtility.ClearProgressBar();
        var sb = new StringBuilder(); sb.AppendLine("Animations: " + (all / 1048576) + " MB in " + rows.Count + " files");
        var byDir = new Dictionary<string, long>();
        foreach (var r in rows) { string d = Path.GetDirectoryName(r.Value.Substring(r.Value.IndexOf("Assets/"))).Replace('\\', '/'); long v; byDir.TryGetValue(d, out v); byDir[d] = v + r.Key; }
        foreach (var kv in byDir.OrderByDescending(x => x.Value)) sb.AppendLine("  " + (kv.Value / 1048576) + " MB  " + kv.Key);
        foreach (var r in rows.OrderByDescending(x => x.Key).Take(80)) sb.AppendLine(r.Value);
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/anim_report.txt", sb.ToString());
        Debug.Log("Ashen Hollow: animation report in Builds/anim_report.txt");
    }


    // what each mesh stores per corner (position, normal, tangent, colours, uv sets, bone weights) and how big that is,
    // with the phone build's vertex compression settings: to see which channels are worth dropping or packing smaller
    // the build's vertex compression (not public in the editor API: read and written through reflection)
    public static object VertexMask()
    {
        var pr = typeof(PlayerSettings).GetProperty("vertexChannelCompressionMask", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        return pr != null ? pr.GetValue(null, null) : "(unknown)";
    }

    [MenuItem("Ashen Hollow/Mesh Layout Report %#&j")]
    public static void Layout()
    {
        var sb = new StringBuilder();
        sb.AppendLine("vertex compression (Android): " + VertexMask() + ", strip unused mesh parts: " + PlayerSettings.stripUnusedMeshComponents);
        var byLayout = new Dictionary<string, long>(); var byLayoutN = new Dictionary<string, int>(); long total = 0; int n = 0;
        var rows = new List<KeyValuePair<long, string>>();
        foreach (var dir in Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories))
            foreach (var f in Directory.GetFiles(dir, "*.glb", SearchOption.AllDirectories))
            {
                string p = f.Replace('\\', '/'); if (++n % 40 == 0) EditorUtility.DisplayProgressBar("Mesh layout", p, 0.5f);
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    var m = o as Mesh; if (m == null) continue;
                    var attrs = m.GetVertexAttributes(); int stride = 0; var parts = new List<string>();
                    foreach (var a in attrs)
                    {
                        int sz = a.format == UnityEngine.Rendering.VertexAttributeFormat.Float32 || a.format == UnityEngine.Rendering.VertexAttributeFormat.UInt32 || a.format == UnityEngine.Rendering.VertexAttributeFormat.SInt32 ? 4 : a.format == UnityEngine.Rendering.VertexAttributeFormat.Float16 || a.format == UnityEngine.Rendering.VertexAttributeFormat.UNorm16 || a.format == UnityEngine.Rendering.VertexAttributeFormat.SNorm16 || a.format == UnityEngine.Rendering.VertexAttributeFormat.UInt16 || a.format == UnityEngine.Rendering.VertexAttributeFormat.SInt16 ? 2 : 1;
                        stride += sz * a.dimension; parts.Add(a.attribute + ":" + a.format + "x" + a.dimension);
                    }
                    long idx = 0; for (int s2 = 0; s2 < m.subMeshCount; s2++) idx += m.GetIndexCount(s2);
                    long bytes = (long)stride * m.vertexCount + idx * (m.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32 ? 4 : 2);
                    string key = string.Join(" ", parts) + " idx" + (m.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32 ? 32 : 16);
                    long v; byLayout.TryGetValue(key, out v); byLayout[key] = v + bytes; int c; byLayoutN.TryGetValue(key, out c); byLayoutN[key] = c + 1; total += bytes;
                    rows.Add(new KeyValuePair<long, string>(bytes, (bytes / 1024) + " KB  " + m.vertexCount + " verts x " + stride + " B  " + p + " : " + m.name));
                }
            }
        EditorUtility.ClearProgressBar();
        sb.AppendLine("all meshes (raw): " + (total / 1048576) + " MB");
        foreach (var kv in byLayout.OrderByDescending(x => x.Value)) sb.AppendLine("  " + (kv.Value / 1048576) + " MB in " + byLayoutN[kv.Key] + " meshes: " + kv.Key);
        sb.AppendLine("largest:"); foreach (var r in rows.OrderByDescending(x => x.Key).Take(40)) sb.AppendLine("  " + r.Value);
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/mesh_layout.txt", sb.ToString());
        Debug.Log("Ashen Hollow: mesh layout in Builds/mesh_layout.txt");
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
