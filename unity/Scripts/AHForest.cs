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
    public class Tree { public Transform go, crown, trunk; public Vector3 pos; public float phase, sway; public GameObject stump; public float girth; }
    public static readonly List<Tree> Trees = new List<Tree>();
    static object data;
    static readonly GameObject[] prefabs = new GameObject[6];

    // the crowns through the year (AHSeason calls this): autumn reds and golds on the leafy trees, frost on every crown
    // and snow on the pines in winter, a little blossom in spring, the area's own green in summer
    static readonly Material[,] seasonMats = new Material[2, 3]; static readonly Color[,] seasonBase = new Color[2, 3];
    // the Dreamscape trees (Hollow Meadow) keep their own textured leaves: they get their own copies of the leaf
    // materials (never the pack's), tinted over the texture
    static readonly Dictionary<Material, Material> dreamCopy = new Dictionary<Material, Material>();
    static readonly List<Material> dreamLeaves = new List<Material>(); static readonly List<Color> dreamBase = new List<Color>(), dreamTop = new List<Color>(), dreamBot = new List<Color>();
    static bool Leafy(Material m) { string n = m.name.ToLowerInvariant(); return n.Contains("leaf") || n.Contains("leav") || n.Contains("foli") || n.Contains("canopy") || n.Contains("crown") || n.Contains("needle") || n.Contains("_bb"); }   // _BB: the far-off billboard of the whole tree
    static void CopyDreamLeaves()
    {
        if (dreamLeaves.Count > 0) return;
        var names = new HashSet<string>();
        foreach (var t in Trees)
        {
            if (t.crown == null || t.trunk != null) continue;   // the Dreamscape trees have no separate trunk
            foreach (var r in t.crown.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; bool ch = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    var m = ms[i]; if (m == null) continue; names.Add(m.name + (Leafy(m) ? "*" : ""));
                    if (!Leafy(m)) continue;
                    Material c; if (!dreamCopy.TryGetValue(m, out c)) { c = new Material(m); c.name = m.name + " (season)"; dreamCopy[m] = c; dreamLeaves.Add(c); dreamBase.Add(c.HasProperty("_BaseColor") ? c.GetColor("_BaseColor") : c.HasProperty("_Color") ? c.GetColor("_Color") : c.HasProperty("_TintColor") ? c.GetColor("_TintColor") : Color.white);
                        dreamTop.Add(c.HasProperty("_Foliage_Color_Top") ? c.GetColor("_Foliage_Color_Top") : Color.white); dreamBot.Add(c.HasProperty("_Foliage_Color_Bottom") ? c.GetColor("_Foliage_Color_Bottom") : Color.white); }
                    ms[i] = c; ch = true;
                }
                if (ch) r.sharedMaterials = ms;
            }
        }
        if (names.Count > 0) Debug.Log("Ashen Hollow: tree materials (* = leaves, tinted by season): " + string.Join(", ", names));
    }

    public static void ApplySeason(string season)
    {
        CopyDreamLeaves();
        for (int i = 0; i < dreamLeaves.Count; i++)
        {
            var m = dreamLeaves[i];
            if (m.HasProperty("_Foliage_Color_Top"))
            {
                // the Polyart foliage: a colour at the top of the crown and one underneath; set both
                Color top = dreamTop[i], bot = dreamBot[i];
                if (season == "autumn")
                {
                    Color[] t3 = { new Color(1f, 0.42f, 0.06f), new Color(1f, 0.72f, 0.1f), new Color(0.92f, 0.24f, 0.08f) };
                    top = t3[i % 3]; bot = Color.Lerp(t3[i % 3], new Color(0.5f, 0.12f, 0.04f), 0.55f);
                }
                else if (season == "winter") { top = Color.Lerp(top, new Color(0.88f, 0.92f, 0.97f), 0.6f); bot = Color.Lerp(bot, new Color(0.45f, 0.5f, 0.55f), 0.4f); }
                else if (season == "spring" && i % 2 == 1) top = Color.Lerp(top, new Color(1f, 0.78f, 0.88f), 0.45f);
                m.SetColor("_Foliage_Color_Top", top); m.SetColor("_Foliage_Color_Bottom", bot);
                continue;
            }
            Color b = dreamBase[i], k = Color.white;
            if (season == "autumn") k = i % 3 == 0 ? new Color(2.1f, 0.55f, 0.28f) : i % 3 == 1 ? new Color(2.2f, 1.05f, 0.25f) : new Color(1.9f, 0.85f, 0.3f);
            else if (season == "winter") k = new Color(1.25f, 1.12f, 1.4f);
            else if (season == "spring") k = new Color(1.1f, 1.15f, 0.95f);
            var c = new Color(b.r * k.r, b.g * k.g, b.b * k.b, b.a);
            foreach (var pr in new[] { "_BaseColor", "_Color", "_TintColor" }) if (m.HasProperty(pr)) m.SetColor(pr, c);
        }
        Color[] autumn = { new Color(0.78f, 0.28f, 0.1f), new Color(0.86f, 0.52f, 0.12f), new Color(0.7f, 0.42f, 0.1f) };
        for (int k = 0; k < 2; k++) for (int s = 0; s < 3; s++)
        {
            var m = seasonMats[k, s]; if (m == null) continue; Color b = seasonBase[k, s], c = b;
            if (season == "autumn" && k == 0) c = Color.Lerp(b, autumn[s], 0.72f);
            else if (season == "winter") c = Color.Lerp(b, new Color(0.86f, 0.9f, 0.95f), k == 0 ? 0.5f : 0.38f);
            else if (season == "spring" && k == 0) c = s == 2 ? Color.Lerp(b, new Color(0.98f, 0.78f, 0.86f), 0.45f) : Color.Lerp(b, new Color(0.55f, 0.82f, 0.35f), 0.25f);
            c.a = 1f;
            foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) m.SetColor(pr, c);
        }
    }

    public static void Setup(AHGame g, Transform world)
    {
        Trees.Clear();
        for (int k = 0; k < 2; k++) for (int s = 0; s < 3; s++) seasonMats[k, s] = null;
        dreamCopy.Clear(); dreamLeaves.Clear(); dreamBase.Clear(); dreamTop.Clear(); dreamBot.Clear();
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
                crownMats[k, s] = m; seasonMats[k, s] = m; seasonBase[k, s] = tint;
            }
            if (trunkMat == null) { var t = Part(prefabs[k * 3], "Trunk"); if (t != null) { trunkMat = new Material(t.sharedMaterial); trunkMat.enableInstancing = true; } }
        }

        var root = new GameObject("Forest").transform;
        var rnd = new System.Random(AHGame.AreaId.GetHashCode());
        var dream = AHDreamSet.UseIn(AHGame.AreaId) ? AHDreamSet.Get() : null;
        foreach (var o in list)
        {
            var a = o as List<object>;
            float x = (float)(double)a[0], z = (float)(double)a[1], y0 = (float)(double)a[2], h = (float)(double)a[3], rad = (float)(double)a[4]; int k = (int)(double)a[5];
            Color c = new Color((float)(double)a[6], (float)(double)a[7], (float)(double)a[8]);
            Color mc = n[k] > 0 ? med[k] / n[k] : c; float lum = (c.r + c.g + c.b) / Mathf.Max(0.001f, mc.r + mc.g + mc.b);
            int shade = lum < 0.93f ? 0 : lum > 1.07f ? 2 : 1;
            var pf = prefabs[k * 3 + rnd.Next(3)];
            if (AHVillage.InLots(g.W(x, z))) continue;   // a village building stands here now
            if (k == 0 && dream != null)
            {
                // a Dreamscape tree: big round oaks with a few birches between them, its own painted colours and wind
                Vector3 dp = g.W(x, z); dp.y += y0;
                var dt = DreamTree(dream, root, dp, h, rnd);
                if (dt != null) { Trees.Add(dt); continue; }
            }
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
                if (!g.InArea(p) || g.Blocked(p, 1.2f) || AHVillage.InLots(p)) continue;
                bool near = false; foreach (var t2 in Trees) { Vector3 dd = t2.pos - p; dd.y = 0; if (dd.sqrMagnitude < 4f) { near = true; break; } } if (near) continue;
                if (src.stump != null) { var dt = DreamTree(dream, root, p, 5.5f, rnd); if (dt != null) Trees.Add(dt); continue; }
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
            float trunk = t.girth > 0f ? t.girth : Mathf.Clamp(t.go.localScale.x * 0.22f, 0.3f, 0.65f);
            g.AddBlocker(at, trunk);
            if (linked.Contains(i)) continue;
            string type = t.go.name == "Pine" ? "pine" : "tree";
            AHGather.Spots.Add(new AHSpot { kind = "chop", type = type, pos = at, r = trunk, reach = trunk + 1.4f, forest = i, name = AHJson.S(AHJson.O(treeRules, type), "name", type) });
        }
        Debug.Log("Ashen Hollow: " + Trees.Count + " trees planted, all of them choppable");
    }

    // a Dreamscape tree on this spot, about h metres tall (h < 0: copy the size of 'like'); felled it leaves the pack's stump
    static Tree DreamTree(AHDreamSet d, Transform root, Vector3 p, float h, System.Random rnd, Transform like = null)
    {
        if (d == null || d.trees == null || d.trees.Length == 0) return null;
        bool birch = d.birches != null && d.birches.Length > 0 && rnd.NextDouble() < 0.3;
        var arr = birch ? d.birches : d.trees; var pf = arr[rnd.Next(arr.Length)]; if (pf == null) return null;
        var holder = new GameObject("Tree").transform; holder.SetParent(root, false); holder.position = p;
        var t = Object.Instantiate(pf, holder, false).transform; t.name = "Crown";
        t.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        foreach (var col in t.GetComponentsInChildren<Collider>(true)) Object.Destroy(col);
        // size: as tall as the old tree, a little more (the pack's trees are broad, they look best big)
        var b = Bounds(t.gameObject); float nat = Mathf.Max(0.5f, b.size.y);
        float want = Mathf.Clamp(h * (birch ? 1.45f : 1.3f), 4f, 16f) * (0.9f + (float)rnd.NextDouble() * 0.2f);
        t.localScale = Vector3.one * (want / nat);
        GameObject stump = null;
        if (d.stump != null)
        {
            stump = Object.Instantiate(d.stump, holder, false); stump.name = "Trunk";
            foreach (var col in stump.GetComponentsInChildren<Collider>(true)) Object.Destroy(col);
            var sb = Bounds(stump); stump.transform.localScale = Vector3.one * Mathf.Clamp(want * 0.11f / Mathf.Max(0.2f, sb.size.x), 0.3f, 3f);
            stump.SetActive(false);
        }
        holder.localScale = Vector3.one; AHModel.SetShadows(holder.gameObject);
        // the forest's code reads a tree's girth from its scale: a holder sized like an old tree of this height
        var tree = new Tree { go = holder, crown = t, trunk = null, pos = p, phase = 0f, sway = 0f, stump = stump };
        holder.localScale = Vector3.one; tree.girth = Mathf.Clamp(want * 0.05f, 0.3f, 0.65f);
        return tree;
    }
    static Bounds Bounds(GameObject go)
    {
        var b = new Bounds(go.transform.position, Vector3.zero); bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
        return b;
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
        if (t.stump != null) t.stump.SetActive(felled);
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
            var tr = trees[i]; if (tr.crown == null || tr.stump != null || !tr.crown.gameObject.activeSelf) continue;   // Dreamscape trees sway in their own shader
            float a = Mathf.Sin(t * 1.1f * tr.sway + tr.phase) * 1.4f, b = Mathf.Sin(t * 0.7f + tr.phase * 1.7f) * 1.0f;
            tr.crown.localRotation = Quaternion.Euler(a, 0f, b);
        }
    }
}
