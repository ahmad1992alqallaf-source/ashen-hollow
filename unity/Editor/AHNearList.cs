// Ashen Hollow (editor test): "Test: List What's Near". Writes every drawn thing within 8 m of the hero (its path in
// the scene, mesh, materials and size) to HeroShots/near.txt, nearest first, to track down odd-looking props.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHNearList
{
    [MenuItem("Ashen Hollow/Test: List What's Near %&f")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        Vector3 at = g.player.transform.position;
        var list = new List<KeyValuePair<float, string>>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == null || !r.enabled || r.transform.IsChildOf(g.player.transform)) continue;
            float d = Vector3.Distance(r.bounds.ClosestPoint(at), at); if (d > 8f) continue;
            var sb = new StringBuilder();
            for (var t = r.transform; t != null; t = t.parent) sb.Insert(0, "/" + t.name);
            var mf = r.GetComponent<MeshFilter>();
            sb.Append("  mesh=" + (mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : r.GetType().Name));
            sb.Append("  size=" + r.bounds.size.ToString("F2") + "  mats=");
            foreach (var m in r.sharedMaterials) if (m != null) sb.Append(m.name + "[" + (m.HasProperty("_BaseColor") ? ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor")) : "-") + (m.mainTexture != null ? " tex" : "") + "] ");
            list.Add(new KeyValuePair<float, string>(d, d.ToString("F1") + "m " + sb));
        }
        list.Sort((a, b) => a.Key.CompareTo(b.Key));
        var o = new StringBuilder(); foreach (var kv in list) o.AppendLine(kv.Value);
        string p = Path.Combine(Application.dataPath, "../HeroShots/near.txt"); File.WriteAllText(p, o.ToString());
        Debug.Log("Ashen Hollow: " + list.Count + " things near the hero listed in " + p);
    }
}
