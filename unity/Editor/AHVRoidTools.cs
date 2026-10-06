// Ashen Hollow: VRoid heroes. Moves exported .vrm files into Resources/AH/VRoid and writes a report of each one
// (humanoid avatar, height, materials and shaders, bone names) to HeroShots/vroid_<name>.txt.
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHVRoidTools
{
    const string Dir = "Assets/AshenHollow/Resources/AH/VRoid", Src = "Assets/AshenHollow/VRoidSource";

    [MenuItem("Ashen Hollow/VRoid: Collect And Report %&v")]
    static void Collect()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/AshenHollow/Resources/AH", "VRoid");
        if (!AssetDatabase.IsValidFolder(Src)) AssetDatabase.CreateFolder("Assets/AshenHollow", "VRoidSource");
        // the .vrm files themselves stay out of Resources (they would bloat the build); what UniVRM made from them
        // (the prefab and its materials, meshes, textures, avatar) goes into Resources so the game can load it
        foreach (var root in new[] { "Assets", Src, Dir })
        {
            foreach (var f in Directory.GetFiles(root, "*.vrm", SearchOption.TopDirectoryOnly))
                Move(f, Src + "/" + Path.GetFileName(f));
            foreach (var f in Directory.GetFiles(root, "*.prefab", SearchOption.TopDirectoryOnly))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (!File.Exists(Path.Combine(Src, n + ".vrm")) && !File.Exists(Path.Combine(root, n + ".vrm"))) continue;
                if (root != Dir) Move(f, Dir + "/" + Path.GetFileName(f));
                foreach (var d in Directory.GetDirectories(root, n + ".*")) if (root != Dir) Move(d, Dir + "/" + Path.GetFileName(d));
            }
        }
        AssetDatabase.Refresh();
        foreach (var f in Directory.GetFiles(Dir, "*.prefab"))
        {
            string path = f.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var sb = new StringBuilder();
            sb.AppendLine("asset " + path + " root " + (go != null ? go.name : "NULL"));
            if (go != null)
            {
                var an = go.GetComponent<Animator>();
                sb.AppendLine("animator " + (an != null) + " avatar " + (an != null && an.avatar != null ? an.avatar.name + " human=" + an.avatar.isHuman + " valid=" + an.avatar.isValid : "none"));
                foreach (var c in go.GetComponents<Component>()) sb.AppendLine("component " + c.GetType().FullName);
                var b = new Bounds(); bool any = false;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var rb = r is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)r).bounds : r.bounds;
                    if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
                    sb.Append("renderer " + r.name + " " + r.GetType().Name + " mats:");
                    foreach (var m in r.sharedMaterials) sb.Append(" [" + (m != null ? m.name + " / " + (m.shader != null ? m.shader.name : "no shader") : "null") + "]");
                    sb.AppendLine();
                }
                sb.AppendLine("bounds " + b);
                if (an != null && an.avatar != null && an.avatar.isHuman)
                    foreach (HumanBodyBones hb in System.Enum.GetValues(typeof(HumanBodyBones)))
                    {
                        if (hb == HumanBodyBones.LastBone) continue;
                        var t = GetBone(go, an.avatar, hb); if (t != null) sb.AppendLine("bone " + hb + " = " + t.name + " pos=" + t.position.ToString("F3"));
                    }
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/vroid_" + Path.GetFileNameWithoutExtension(f) + ".txt"), sb.ToString());
            Debug.Log("Ashen Hollow: VRoid report written for " + path);
        }
    }

    [MenuItem("Ashen Hollow/VRoid: Dump Hero Bones %&2")]
    static void DumpBones()
    {
        var sb = new StringBuilder();
        foreach (var n in new[] { "qMaleRanger", "qMalePeasant", "qFemalePeasant" })
        {
            var go = Resources.Load<GameObject>("AH/Models/Web/" + n); if (go == null) { sb.AppendLine("missing " + n); continue; }
            sb.AppendLine("== " + n);
            Walk(go.transform, 0, sb);
        }
        File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/hero_bones.txt"), sb.ToString());
        Debug.Log("Ashen Hollow: hero bones written");
    }
    static void Walk(Transform t, int d, StringBuilder sb)
    {
        sb.AppendLine(new string(' ', d) + t.name + " lp=" + t.localPosition.ToString("F3") + " lr=" + t.localRotation.eulerAngles.ToString("F1") + " wp=" + t.position.ToString("F3"));
        foreach (Transform c in t) Walk(c, d + 1, sb);
    }

    static void Move(string from, string to)
    {
        from = from.Replace('\\', '/'); if (from == to) return;
        string err = AssetDatabase.MoveAsset(from, to);
        Debug.Log("Ashen Hollow: moved " + from + " -> " + to + (string.IsNullOrEmpty(err) ? "" : " ERROR " + err));
    }

    static Transform GetBone(GameObject root, Avatar av, HumanBodyBones hb)
    {
        string want = null;
        foreach (var h in av.humanDescription.human) if (h.humanName == HumanTrait.BoneName[(int)hb]) { want = h.boneName; break; }
        if (want == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == want) return t;
        return null;
    }
}
