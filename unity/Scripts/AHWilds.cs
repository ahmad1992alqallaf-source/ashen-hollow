// Ashen Hollow: the wild lands filled out. The lands are laid out much wider than the web game drew them (AHGame
// spreadK), so the open ground between its camps and rocks is filled here, the same way every time a land loads:
//  - monster camps: three to five of the land's own beasts round a spot marked with a few boulders or a broken ruin
//  - ore veins: two or three rocks of the land's own ores together
//  - herb patches: two or three of the land's own herbs
//  - outcrops and ruins between them, so the open ground is never bare for long
// Nothing goes on a road out, at a gate, in a town, in the sea or a lake, on top of something solid, or close to the
// camps, rocks and herbs the land already has. How much of each: by the size of the land (see Want()).
using System.Collections.Generic;
using UnityEngine;

public static class AHWilds
{
    static readonly HashSet<string> Lands = new HashSet<string> { "meadow", "silkwood", "frost", "mire", "vale", "sands", "isle", "ember", "tide", "fossil", "mill", "fenwick", "whitepine", "kingsroad", "causeway", "scorchwind", "ashfall" };
    class Camp { public Vector3 at; public AHMobType type; public int n; }
    static readonly List<Camp> camps = new List<Camp>();
    static readonly List<Vector3> taken = new List<Vector3>();
    public static int Camps, Mobs, Ore, Herbs, Props;

    static float Area(AHGame g) { var R = g.WalkArea; return R.width * R.height; }

    // spots, rocks and herbs (before the ore and herb looks are built); the camps' beasts come later (Spawn)
    public static void Plan(AHGame g, Transform world)
    {
        camps.Clear(); taken.Clear(); Camps = Mobs = Ore = Herbs = Props = 0; G = g;
        string id = AHGame.AreaId;
        if (g == null || g.data == null || !Lands.Contains(id)) return;
        var R = g.WalkArea; if (R.width < 40f || R.height < 40f) return;
        // what already stands on the land (crags, houses, tents, ruins, trees), on a two-metre grid
        occX0 = R.xMin; occZ0 = R.yMin; occNx = Mathf.CeilToInt(R.width / 2f) + 1; occNz = Mathf.CeilToInt(R.height / 2f) + 1; occ = new bool[occNx * occNz];
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!mr.enabled || mr.gameObject.layer == 30) continue;
            var nm = mr.name; if (nm.StartsWith("AH_GROUND") || nm.StartsWith("AH_SEA") || nm.StartsWith("AH_WATER") || nm == "Outskirts") continue;
            var bb = mr.bounds; if (bb.size.y < 0.6f || Mathf.Max(bb.size.x, bb.size.z) > 60f || bb.min.y > 2f) continue;
            bool tree = false; for (var a = mr.transform; a != null; a = a.parent) if (a.name == "Forest" || a.name == "Tree" || a.name == "Pine") { tree = true; break; }
            if (!tree && AHSolidGrid.Skip(mr.transform)) continue;   // grass, flowers, bushes, banners: not in the way
            if (tree) { var c = bb.center; bb = new Bounds(c, new Vector3(1.6f, bb.size.y, 1.6f)); }   // a tree: its trunk, not its crown
            int ax0 = Mathf.Max(0, Mathf.FloorToInt((bb.min.x - occX0) / 2f)), ax1 = Mathf.Min(occNx - 1, Mathf.FloorToInt((bb.max.x - occX0) / 2f));
            int az0 = Mathf.Max(0, Mathf.FloorToInt((bb.min.z - occZ0) / 2f)), az1 = Mathf.Min(occNz - 1, Mathf.FloorToInt((bb.max.z - occZ0) / 2f));
            for (int z = az0; z <= az1; z++) for (int x = ax0; x <= ax1; x++) occ[z * occNx + x] = true;
        }
        var rnd = new System.Random(id.GetHashCode() * 31 + 7);
        float S = AHDB.S;
        // what is there already
        var mobAt = new List<Vector3>(); if (g.data.mobs != null) foreach (var m in g.data.mobs) mobAt.Add(g.W(m.x, m.z));
        var trees = new List<Vector3>();
        foreach (var s in AHGather.Spots) { if (s.kind == "chop") trees.Add(s.pos); else taken.Add(s.pos); }
        var keep = new List<Vector3>();
        keep.Add(g.W(g.data.spawn.x, g.data.spawn.z));
        if (g.data.exits != null) foreach (var e in g.data.exits) keep.Add(e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) / 2f, (e.z0 + e.z1) / 2f));
        if (g.data.gates != null) foreach (var gt in g.data.gates) keep.Add(g.W(gt.x, gt.z));
        foreach (var s in AHGather.Spots) if (s.kind != "mine" && s.kind != "herb" && s.kind != "chop" && s.kind != "fish") keep.Add(s.pos);

        // the land's own kinds, as often as they turn up already
        var oreKinds = new List<string>(); var herbKinds = new List<string>();
        foreach (var s in AHGather.Spots) { if (s.kind == "mine") oreKinds.Add(s.type); if (s.kind == "herb") herbKinds.Add(s.type); }
        var types = new List<AHMobType>(); var counts = new Dictionary<string, int>();
        if (g.data.mobs != null) foreach (var m in g.data.mobs) { int c; counts.TryGetValue(m.type, out c); counts[m.type] = c + 1; }
        if (g.data.mobTypes != null) foreach (var t in g.data.mobTypes) if (!t.elite && !t.rare && !t.night && counts.ContainsKey(t.id)) for (int k = 0; k < Mathf.Max(1, counts[t.id]); k++) types.Add(t);

        float A = Area(g);
        int wantCamps = types.Count == 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt((A / 650f - mobAt.Count) / 4f), 0, 40);
        int wantOre = oreKinds.Count == 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt((A / 1500f - oreKinds.Count) / 2.5f), 0, 24);
        int wantHerb = herbKinds.Count == 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt((A / 1300f - herbKinds.Count) / 2.5f), 0, 24);
        int wantProps = Mathf.Clamp(Mathf.RoundToInt(A / 2600f), 0, 40);

        System.Func<float, Vector3> free = room =>
        {
            for (int tries = 0; tries < 60; tries++)
            {
                var p = new Vector3(R.xMin + 9f + (float)rnd.NextDouble() * (R.width - 18f), 0f, R.yMin + 9f + (float)rnd.NextDouble() * (R.height - 18f));
                if (!Open(g, p, room)) continue;
                bool ok = true;
                foreach (var k in keep) if (Flat(k - p) < 16f) { ok = false; break; }
                if (ok) foreach (var k in mobAt) if (Flat(k - p) < room + 6f) { ok = false; break; }
                if (ok) foreach (var k in taken) if (Flat(k - p) < room + 4f) { ok = false; break; }
                if (ok) foreach (var c in camps) if (Flat(c.at - p) < room + 14f) { ok = false; break; }
                if (ok) foreach (var k in trees) if (Flat(k - p) < Mathf.Min(room, 4f)) { ok = false; break; }
                if (ok) return p;
            }
            return new Vector3(float.NaN, 0f, 0f);
        };
        var root = new GameObject("Wilds").transform;

        for (int i = 0; i < wantCamps; i++)
        {
            var p = free(9f); if (float.IsNaN(p.x)) break;
            var t = types[rnd.Next(types.Count)];
            camps.Add(new Camp { at = p, type = t, n = 3 + rnd.Next(3) });
            taken.Add(p); Camps++;
            // the camp's mark: a few boulders, or a broken ruin
            if (rnd.Next(3) == 0) Ruin(root, p, rnd); else Rocks(root, p, rnd, 2 + rnd.Next(3), 5f, 7f);
        }
        for (int i = 0; i < wantOre; i++)
        {
            var p = free(5f); if (float.IsNaN(p.x)) break;
            string kind = oreKinds[rnd.Next(oreKinds.Count)]; int n = 2 + rnd.Next(2);
            for (int k = 0; k < n; k++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2); var q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (k == 0 ? 0f : 2.6f + (float)rnd.NextDouble());
                if (!Open(g, q, 1.2f)) continue;
                var s = new AHSpot { kind = "mine", type = kind, pos = q, r = 16f * S, reach = 62f * S };
                s.name = AHJson.S(AHJson.O(AHJson.O(AHDB.Rules, "ROCKS"), kind), "name", "Rock");
                s.body = Placeholder(root, q, 1.3f + (float)rnd.NextDouble() * 0.5f, 0.9f + (float)rnd.NextDouble() * 0.4f);
                AHGather.Spots.Add(s); taken.Add(q); Ore++;
            }
        }
        for (int i = 0; i < wantHerb; i++)
        {
            var p = free(4f); if (float.IsNaN(p.x)) break;
            string kind = herbKinds[rnd.Next(herbKinds.Count)]; int n = 2 + rnd.Next(2);
            for (int k = 0; k < n; k++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2); var q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (k == 0 ? 0f : 1.8f + (float)rnd.NextDouble());
                if (!Open(g, q, 0.8f)) continue;
                var s = new AHSpot { kind = "herb", type = kind, pos = q, r = 12f * S, reach = 60f * S };
                var it = AHItems.Get(kind); s.name = it != null ? it.name : kind;
                AHGather.Spots.Add(s); taken.Add(q); Herbs++;
            }
        }
        for (int i = 0; i < wantProps; i++)
        {
            var p = free(6f); if (float.IsNaN(p.x)) break;
            taken.Add(p);
            int kind = rnd.Next(10);
            if (kind < 3) Ruin(root, p, rnd);
            else if (kind < 6) Rocks(root, p, rnd, 1 + rnd.Next(2), 0f, 3f, 5f + (float)rnd.NextDouble() * 5f);   // a big outcrop
            else Rocks(root, p, rnd, 2 + rnd.Next(4), 0f, 4.5f);
        }
        if (root.childCount == 0) Object.Destroy(root.gameObject);
        Debug.Log("Ashen Hollow: wild " + id + " (" + R.width.ToString("F0") + "x" + R.height.ToString("F0") + " m): " + Camps + " camps (wanted " + wantCamps + ", " + types.Count + " kinds, " + mobAt.Count + " beasts there already), " + Ore + " ore rocks, " + Herbs + " herbs, " + Props + " outcrops and ruins added");
    }

    // the camps' beasts (once the hero and the land's own beasts are in)
    public static void Spawn(AHGame g)
    {
        if (g == null) return;
        foreach (var c in camps)
        {
            for (int k = 0; k < c.n; k++)
            {
                float a = k * Mathf.PI * 2f / c.n + 0.4f, d = 3f + (k % 2) * 2f;
                var p = c.at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
                if (!Open(g, p, 0.6f)) p = c.at;
                p = g.Resolve(p, 0.4f);
                g.mobs.Add(AHMob.Create(g, c.type, p)); Mobs++;
            }
        }
        if (Mobs > 0) Debug.Log("Ashen Hollow: " + Mobs + " beasts in " + camps.Count + " new camps");
    }

    static float Flat(Vector3 d) { d.y = 0f; return d.magnitude; }
    static bool[] occ; static int occNx, occNz; static float occX0, occZ0;
    static bool Occupied(Vector3 p, float r)
    {
        if (occ == null) return false;
        int x0 = Mathf.FloorToInt((p.x - r - occX0) / 2f), x1 = Mathf.FloorToInt((p.x + r - occX0) / 2f), z0 = Mathf.FloorToInt((p.z - r - occZ0) / 2f), z1 = Mathf.FloorToInt((p.z + r - occZ0) / 2f);
        for (int z = Mathf.Max(0, z0); z <= Mathf.Min(occNz - 1, z1); z++) for (int x = Mathf.Max(0, x0); x <= Mathf.Min(occNx - 1, x1); x++) if (occ[z * occNx + x]) return true;
        return false;
    }
    static bool Open(AHGame g, Vector3 p, float r)
    {
        if (!g.InArea(p) || g.Blocked(p, Mathf.Min(r, 2.5f)) || g.InTown(p) || AHSea.Sea(p, r + 3f) || Occupied(p, Mathf.Min(r, 3f))) return false;
        var R = g.WalkArea; return p.x > R.xMin + r + 4f && p.x < R.xMax - r - 4f && p.z > R.yMin + r + 4f && p.z < R.yMax - r - 4f;
    }

    // the old rock AHOre measures and replaces with an ore boulder: an invisible box of the size wanted
    static Renderer Placeholder(Transform root, Vector3 at, float wide, float tall)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Ore place";
        var cl = go.GetComponent<Collider>(); if (cl != null) Object.Destroy(cl);
        var holder = new GameObject("Ore spot").transform; holder.SetParent(root, false); holder.position = at;
        go.transform.SetParent(holder, false); go.transform.localScale = new Vector3(wide, tall, wide); go.transform.localPosition = new Vector3(0f, tall * 0.5f, 0f);
        return go.GetComponent<Renderer>();   // (AHOre hides it once it has measured it)
    }

    static void Rocks(Transform root, Vector3 at, System.Random rnd, int n, float r0, float r1, float big = 0f)
    {
        for (int k = 0; k < n; k++)
        {
            int pick = rnd.Next(1000);
            // (in the desert, snow and ash the plain rocks, which take the land's stone colour; the mossy ones elsewhere)
            var pf = Stone.ContainsKey(AHGame.AreaId) ? Resources.Load<GameObject>("AH/Models/KK/kk_rock_single_" + "BCDE"[pick % 4]) : null;
            if (pf == null) pf = AHScenery.RockPrefab(pick); if (pf == null) return;
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2), d = r0 + (float)rnd.NextDouble() * (r1 - r0);
            var go = Object.Instantiate(pf, root, false); go.name = "Wild rock";
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            go.transform.position = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d; go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            go.transform.localScale = Vector3.one;
            var b = Measure(go); float want = big > 0f && k == 0 ? big : 1.4f + (float)rnd.NextDouble() * (k == 0 ? 3.2f : 1.8f);
            float s = want / Mathf.Max(0.1f, Mathf.Max(b.size.x, b.size.z));
            go.transform.localScale = new Vector3(s, s * (0.7f + (float)rnd.NextDouble() * 0.6f), s);
            b = Measure(go); go.transform.position += Vector3.up * (at.y - b.min.y - 0.15f);
            Tint(go); AHModel.SetShadows(go); Props++;
            Solid(go, false);
        }
    }

    // rocks and ruins in the land's own stone: warm sandstone in the desert, pale in the snow, dark by the fire
    static readonly Dictionary<string, Color> Stone = new Dictionary<string, Color>
    {
        { "sands", new Color(1.35f, 1.05f, 0.72f) }, { "scorchwind", new Color(1.35f, 1.0f, 0.7f) }, { "isle", new Color(1.2f, 1.05f, 0.85f) },
        { "frost", new Color(1.15f, 1.2f, 1.3f) }, { "whitepine", new Color(1.1f, 1.15f, 1.25f) }, { "ember", new Color(0.6f, 0.52f, 0.5f) }, { "ashfall", new Color(0.65f, 0.58f, 0.55f) },
    };
    static readonly Dictionary<Material, Material> tinted = new Dictionary<Material, Material>();
    public static void TintStone(GameObject go) { Tint(go); }   // (the passes out of a land: AHPass)
    static void Tint(GameObject go)
    {
        Color k; if (!Stone.TryGetValue(AHGame.AreaId, out k)) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null) continue; Material m;
                if (!tinted.TryGetValue(ms[i], out m) || m == null)
                {
                    m = new Material(ms[i]);
                    foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) { var c = m.GetColor(pr); m.SetColor(pr, new Color(c.r * k.r, c.g * k.g, c.b * k.b, c.a)); }
                    tinted[ms[i]] = m;
                }
                ms[i] = m;
            }
            r.sharedMaterials = ms;
        }
    }

    static readonly string[] Standing = { "kk_dg_pillar", "kk_dg_column" }, Heaps = { "kk_dg_rubble_large", "kk_dg_rubble_half" };
    static void Ruin(Transform root, Vector3 at, System.Random rnd)
    {
        // a ring of broken pillars round a heap of rubble: most still standing, the odd one fallen flat in the sand
        int n = 3 + rnd.Next(3); float rr = 3f + (float)rnd.NextDouble() * 2f, a0 = (float)(rnd.NextDouble() * Mathf.PI * 2);
        for (int k = 0; k <= n; k++)
        {
            bool heap = k == n, fallen = !heap && rnd.Next(4) == 0;
            string part = heap ? Heaps[rnd.Next(Heaps.Length)] : Standing[rnd.Next(Standing.Length)];
            var pf = Resources.Load<GameObject>("AH/Models/KK/" + part); if (pf == null) continue;
            var go = Object.Instantiate(pf, root, false); go.name = "Wild ruin";
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            float a = a0 + k * Mathf.PI * 2f / n, yaw = (float)rnd.NextDouble() * 360f;
            go.transform.position = heap ? at : at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr;
            go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            var b = Measure(go);
            float want = heap ? 2.6f : 2.2f + (float)rnd.NextDouble() * 2.4f;
            float s = want / Mathf.Max(0.1f, heap ? Mathf.Max(b.size.x, b.size.z) : b.size.y);
            go.transform.localScale = Vector3.one * s;
            float dia = Mathf.Max(b.size.x, b.size.z) * s, len = b.size.y * s;
            // a fallen pillar lies right down on its side (never propped up in the air on a corner)
            go.transform.rotation = fallen ? Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, yaw, 0f);
            b = Measure(go); go.transform.position += Vector3.up * (at.y - b.min.y - (fallen ? 0.12f : 0.05f));
            Tint(go); AHModel.SetShadows(go); Props++;
            Solid(go, fallen, dia, len);
        }
    }

    // you walk round a rock or a broken pillar, never through it: round blockers on its footprint (a fallen pillar,
    // long and thin, gets a row of them along its length) that the camera keeps out of as well
    static AHGame G;
    static void Solid(GameObject go, bool fallen, float dia = 0f, float len = 0f)
    {
        if (G == null) return;
        var b = Measure(go); if (b.size.y < 0.35f) return;   // a flat scatter of stones you just step over
        if (!fallen) { G.AddBlocker(b.center, Mathf.Max(0.35f, Mathf.Min(b.size.x, b.size.z) * 0.45f)); return; }
        // lying down: its own up is now flat, along the ground
        Vector3 ax = go.transform.up; ax.y = 0f; if (ax.sqrMagnitude < 1e-4f) ax = Vector3.forward; ax.Normalize();
        float r = Mathf.Max(0.35f, dia * 0.5f), span = Mathf.Max(0f, len * 0.5f - r);
        int n = Mathf.Max(2, Mathf.CeilToInt(len / Mathf.Max(0.5f, r * 1.4f)));
        for (int i = 0; i < n; i++) G.AddBlocker(b.center + ax * Mathf.Lerp(-span, span, i / (float)(n - 1)), r);
    }

    static Bounds Measure(GameObject go) { var rs = go.GetComponentsInChildren<Renderer>(true); Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.zero); for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); return b; }
}
