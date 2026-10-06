// Ashen Hollow: the starter village. Hollow Meadow's square was a big bare patch of dirt with three little houses on
// it, so it looked empty. Now a ring of proper buildings stands around the square, each facing it: an inn, the guild
// hall, a smithy by the furnace and anvil, the bank tower, homes, the barracks where the arena crier calls, the
// auction tower and a lumber mill. Each lot is checked first: nothing is built on a road, an exit, water, a rock you
// mine, a station or the waystone, and the trees standing in a lot are cleared (AHForest skips them).
// Models: KayKit Medieval Hexagon buildings (Resources/AH/Models/KK/kk_building_*), in the land's own colour.
using System.Collections.Generic;
using UnityEngine;

public static class AHVillage
{
    // lots kept clear of trees: AHForest asks before it plants
    public static readonly List<Bounds> Lots = new List<Bounds>();

    struct Plan { public float x, z, size; public string kind, name; public Plan(float x, float z, string kind, float size, string name) { this.x = x; this.z = z; this.kind = kind; this.size = size; this.name = name; } }

    // area units (before a land is spread out); the square spans x 11.6–44.8, z 35.9–70; roads leave east at
    // z ≈ 53 and south at x ≈ 31–35
    static readonly Dictionary<string, Plan[]> Plans = new Dictionary<string, Plan[]>
    {
        { "meadow", new[] {
            new Plan(17f, 29f, "tavern", 10f, "The Ashen Hearth inn"),
            new Plan(29.5f, 28.5f, "church", 9f, "Guild hall"),
            new Plan(41.5f, 29f, "blacksmith", 9f, "Smithy"),
            new Plan(5.5f, 41f, "tower_A", 6.5f, "Bank tower"),
            new Plan(5.5f, 53.5f, "home_A", 8f, "Cottage"),
            new Plan(5.5f, 66f, "home_B", 8f, "Cottage"),
            new Plan(51.5f, 41f, "barracks", 9f, "Barracks"),
            new Plan(17f, 77.5f, "tower_B", 6.5f, "Auction tower"),
            new Plan(44f, 78f, "lumbermill", 9f, "Lumber mill"),
            new Plan(5.5f, 78.5f, "home_A", 7.5f, "Cottage"),
        } },
    };

    // roads per land, as (x0, z0, x1, z1) strips in area units: nothing is built on them
    static readonly Dictionary<string, Rect[]> Roads = new Dictionary<string, Rect[]>
    {
        { "meadow", new[] { Rect.MinMaxRect(44f, 50.5f, 97f, 56f), Rect.MinMaxRect(28.5f, 69f, 37f, 109f) } },
    };

    public static void Setup(AHGame g, Transform world)
    {
        Lots.Clear();
        Plan[] plans; if (world == null || !Plans.TryGetValue(AHGame.AreaId, out plans)) return;
        string col; if (!AHCity.ColourOf(AHGame.AreaId, out col)) col = "green";
        Rect[] roads; Roads.TryGetValue(AHGame.AreaId, out roads);
        Vector3 heart = g.W(g.data.spawn.x, g.data.spawn.z);
        var root = new GameObject("Village").transform;
        int built = 0;
        foreach (var pl in plans)
        {
            float half = pl.size * 0.5f + 0.6f;
            // roads and exits stay open
            bool clash = false;
            if (roads != null) foreach (var r in roads) if (pl.x + half > r.xMin && pl.x - half < r.xMax && pl.z + half > r.yMin && pl.z - half < r.yMax) { clash = true; break; }
            if (clash) continue;
            Vector3 c = g.W(pl.x, pl.z); c.y = world.position.y;
            float wHalf = half * AHGame.Spread;
            if (!g.InArea(c + new Vector3(wHalf, 0, wHalf)) || !g.InArea(c - new Vector3(wHalf, 0, wHalf))) continue;
            // nothing you use (rocks, herbs, stations, fishing, gates, the bank) inside the lot
            foreach (var s in AHGather.Spots)
            {
                if (s.kind == "chop") continue;
                Vector3 d = s.pos - c; if (Mathf.Abs(d.x) < wHalf + 1.2f && Mathf.Abs(d.z) < wHalf + 1.2f) { clash = true; break; }
            }
            if (clash) continue;

            var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_" + pl.kind + "_" + col);
            if (pf == null) continue;
            var go = Object.Instantiate(pf, root, false); go.name = pl.name;
            foreach (var cl in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
            Vector3 dir = heart - c; dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized);
            go.transform.position = Vector3.zero; go.transform.localScale = Vector3.one;
            Bounds b0 = Measure(go);
            float k = pl.size * AHGame.Spread / Mathf.Max(0.1f, Mathf.Max(b0.size.x, b0.size.z));
            go.transform.localScale = Vector3.one * k;
            Bounds b1 = Measure(go);
            go.transform.position = new Vector3(c.x - b1.center.x, c.y - b1.min.y, c.z - b1.center.z);
            Bounds lot = Measure(go);
            AHModel.SetShadows(go);
            g.AddBlockBox(lot, 0.3f);
            var keep = lot; keep.Expand(new Vector3(2.4f, 0f, 2.4f)); Lots.Add(keep);
            // the old ground clutter and chop spots inside the lot go
            foreach (var r in world.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || r.name.StartsWith("AH_")) continue;
                var rb = r.bounds; if (rb.size.y > 6f || Mathf.Max(rb.size.x, rb.size.z) > 8f) continue;
                Vector3 rc = rb.center; if (rc.x > lot.min.x && rc.x < lot.max.x && rc.z > lot.min.z && rc.z < lot.max.z) r.enabled = false;
            }
            AHGather.Spots.RemoveAll(s => s.kind == "chop" && InLots(s.pos));
            built++;
        }
        if (built > 0) Debug.Log("Ashen Hollow: " + built + " village buildings around the square");
    }

    public static bool InLots(Vector3 p)
    {
        foreach (var b in Lots) if (p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z) return true;
        return false;
    }

    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
