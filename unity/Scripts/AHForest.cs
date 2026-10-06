// Ashen Hollow: better trees. The areas' trees were baked into a few instanced meshes (icosphere crowns on sticks).
// Here every one of them, the trees you can chop and the ones that only stand in the woods, is replaced on the same
// spot by a new stylised tree: leafy trees with lumpy rounded crowns and little branches, pines with drooping
// jagged tiers, three shapes of each, light on top and shaded underneath. Each keeps its height and size, and the
// colour of its area (green meadows, olive mire, snow-white Frostfang pines, ash-black Ember pines...). The crowns
// sway a little in the wind. A felled tree drops its crown and leaves a stump, and grows back as before.
// The trees come from Resources/AH/Data/treelook.json (read from the area models) and Models/KK/kk_ah_tree_*.glb /
// kk_ah_pine_*.glb. These are now real leafy trees: broadleaf trees with leaf-card crowns and bark branches (Idyllic
// Fantasy Nature) and spruces with drooping needle boughs (Ultimate Nature Starter), rebuilt for the game: grey leaves
// so each area tints them its own colour, darker low and inside the crown, the branches swaying with the crown, and the
// low trunk kept apart so a felled tree still leaves a stump.
using System.Collections.Generic;
using UnityEngine;

public static class AHForest
{
    public class Tree { public Transform go, crown, trunk; public Vector3 pos; public float phase, sway; }
    public static readonly List<Tree> Trees = new List<Tree>();
    static object data;
    static readonly GameObject[] prefabs = new GameObject[6];

    public static void Setup(AHGame g, Transform world)
    {
        Trees.Clear();
        if (world == null) return;
        if (data == null) { var ta = Resources.Load<TextAsset>("AH/Data/treelook"); if (ta == null) return; data = AHJson.Parse(ta.text); }
        var area = AHJson.O(data, AHGame.AreaId); if (area == null) return;
        var list = AHJson.A(area, "trees"); if (list == null || list.Count == 0) return;
        for (int i = 0; i < 6; i++) if (prefabs[i] == null) prefabs[i] = Resources.Load<GameObject>("AH/Models/KK/kk_ah_" + (i < 3 ? "tree_" : "pine_") + (i % 3));
        if (prefabs[0] == null || prefabs[3] == null) return;

        // hide the old trees
        var hide = new HashSet<string>(); var hl = AHJson.A(area, "hide"); if (hl != null) foreach (var o in hl) hide.Add((string)o);
        foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (hide.Contains(r.gameObject.name)) r.enabled = false;

        // the area's colours: per kind, three shades of its usual crown colour (keeps batching to a handful of materials)
        var med = new Color[2]; var n = new int[2];
        foreach (var o in list) { var a = o as List<object>; int k = (int)(double)a[5]; med[k] += new Color((float)(double)a[6], (float)(double)a[7], (float)(double)a[8]); n[k]++; }
        var crownMats = new Material[2, 3]; Material trunkMat = null;
        for (int k = 0; k < 2; k++)
        {
            if (n[k] == 0) continue; Color c = med[k] / n[k];
            if (c.r + c.g + c.b < 0.2f) c = new Color(0.07f, 0.1f, 0.05f);   // the Fossil Lands' near-black crowns: a dark moss green instead
            var src = Part(prefabs[k * 3], "Crown"); if (src == null) continue;
            for (int s = 0; s < 3; s++)
            {
                var m = new Material(src.sharedMaterial); m.enableInstancing = true;
                Color lin = c * (0.86f + 0.14f * s) * 1.25f; Color tint = new Color(lin.r, lin.g, lin.b, 1f).gamma;
                foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) m.SetColor(pr, tint);
                crownMats[k, s] = m;
            }
            if (trunkMat == null) { var t = Part(prefabs[k * 3], "Trunk"); if (t != null) { trunkMat = new Material(t.sharedMaterial); trunkMat.enableInstancing = true; } }
        }

        var root = new GameObject("Forest").transform;
        var rnd = new System.Random(AHGame.AreaId.GetHashCode());
        foreach (var o in list)
        {
            var a = o as List<object>;
            float x = (float)(double)a[0], z = (float)(double)a[1], y0 = (float)(double)a[2], h = (float)(double)a[3], rad = (float)(double)a[4]; int k = (int)(double)a[5];
            Color c = new Color((float)(double)a[6], (float)(double)a[7], (float)(double)a[8]);
            Color mc = n[k] > 0 ? med[k] / n[k] : c; float lum = (c.r + c.g + c.b) / Mathf.Max(0.001f, mc.r + mc.g + mc.b);
            int shade = lum < 0.93f ? 0 : lum > 1.07f ? 2 : 1;
            var pf = prefabs[k * 3 + rnd.Next(3)];
            var go = Object.Instantiate(pf, root, false).transform; go.name = k == 0 ? "Tree" : "Pine";
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(col);
            Vector3 p = g.W(x, z); p.y += y0;
            go.position = p; go.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            float sy = h / 0.97f, sxz = Mathf.Clamp(rad / 0.34f, sy * 0.45f, sy * 0.8f);
            go.localScale = new Vector3(sxz, sy, sxz);
            var tr = new Tree { go = go, crown = Part(go.gameObject, "Crown") != null ? Part(go.gameObject, "Crown").transform : null, trunk = Part(go.gameObject, "Trunk") != null ? Part(go.gameObject, "Trunk").transform : null, pos = p, phase = (float)rnd.NextDouble() * 6.28f, sway = 0.6f + (float)rnd.NextDouble() * 0.8f };
            if (tr.crown != null && crownMats[k, shade] != null) tr.crown.GetComponent<Renderer>().sharedMaterial = crownMats[k, shade];
            if (tr.trunk != null && trunkMat != null) tr.trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            AHModel.SetShadows(go.gameObject);
            Trees.Add(tr);
        }
        // a spread-out area keeps its woods thick: a second tree near each one
        if (AHGame.Spread > 1.12f)
        {
            int orig = Trees.Count;
            for (int i = 0; i < orig; i++)
            {
                var src = Trees[i]; float ang = (float)rnd.NextDouble() * 6.283f, dist = 2.6f + (float)rnd.NextDouble() * 2.6f * AHGame.Spread;
                Vector3 p = src.pos + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
                if (!g.InArea(p) || g.Blocked(p, 1.2f)) continue;
                bool near = false; foreach (var t2 in Trees) { Vector3 dd = t2.pos - p; dd.y = 0; if (dd.sqrMagnitude < 4f) { near = true; break; } } if (near) continue;
                var go = Object.Instantiate(src.go.gameObject, root, false).transform; go.name = src.go.name;
                go.position = p; go.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f); go.localScale = src.go.localScale * (0.8f + (float)rnd.NextDouble() * 0.3f);
                Trees.Add(new Tree { go = go, crown = Part(go.gameObject, "Crown") != null ? Part(go.gameObject, "Crown").transform : null, trunk = Part(go.gameObject, "Trunk") != null ? Part(go.gameObject, "Trunk").transform : null, pos = p, phase = (float)rnd.NextDouble() * 6.28f, sway = 0.6f + (float)rnd.NextDouble() * 0.8f });
            }
        }
        root.gameObject.AddComponent<AHForestWind>();

        // the trees you can chop: each finds its new tree
        foreach (var s in AHGather.Spots)
        {
            if (s.kind != "chop") continue;
            int best = -1; float bd = 2.2f;
            for (int i = 0; i < Trees.Count; i++) { Vector3 d = Trees[i].pos - s.pos; d.y = 0; float m = d.magnitude; if (m < bd) { bd = m; best = i; } }
            s.forest = best;
        }
        // every other tree can be chopped too, and none can be walked through: a solid trunk for each
        var linked = new HashSet<int>(); foreach (var s in AHGather.Spots) if (s.kind == "chop" && s.forest >= 0) linked.Add(s.forest);
        var treeRules = AHJson.O(AHDB.Rules, "TREES");
        for (int i = 0; i < Trees.Count; i++)
        {
            var t = Trees[i]; Vector3 at = new Vector3(t.pos.x, 0f, t.pos.z);
            float trunk = Mathf.Clamp(t.go.localScale.x * 0.22f, 0.3f, 0.65f);
            g.AddBlocker(at, trunk);
            if (linked.Contains(i)) continue;
            string type = t.go.name == "Pine" ? "pine" : "tree";
            AHGather.Spots.Add(new AHSpot { kind = "chop", type = type, pos = at, r = trunk, reach = trunk + 1.4f, forest = i, name = AHJson.S(AHJson.O(treeRules, type), "name", type) });
        }
        Debug.Log("Ashen Hollow: " + Trees.Count + " trees planted, all of them choppable");
    }

    static Renderer Part(GameObject go, string name)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) if (r.name == name || r.transform.name == name) return r;
        return null;
    }

    // felled: the crown goes and a stump is left; grown back: the whole tree again. True when this spot has a new tree.
    public static bool SetFelled(AHSpot s, bool felled)
    {
        if (s.forest < 0 || s.forest >= Trees.Count) return false;
        var t = Trees[s.forest];
        if (t.crown != null) t.crown.gameObject.SetActive(!felled);
        if (t.trunk != null) t.trunk.localScale = new Vector3(1f, felled ? 0.22f : 1f, 1f);
        if (felled) AHSpark.Burst(t.pos + Vector3.up * 1.2f, new Color(0.55f, 0.75f, 0.35f, 0.8f), 14, 2.5f, 0.8f, 0.14f, 0.6f);
        return true;
    }
}

// a gentle breeze through the crowns
public class AHForestWind : MonoBehaviour
{
    void Update()
    {
        float t = Time.time;
        var trees = AHForest.Trees;
        for (int i = 0; i < trees.Count; i++)
        {
            var tr = trees[i]; if (tr.crown == null || !tr.crown.gameObject.activeSelf) continue;
            float a = Mathf.Sin(t * 1.1f * tr.sway + tr.phase) * 1.4f, b = Mathf.Sin(t * 0.7f + tr.phase * 1.7f) * 1.0f;
            tr.crown.localRotation = Quaternion.Euler(a, 0f, b);
        }
    }
}
