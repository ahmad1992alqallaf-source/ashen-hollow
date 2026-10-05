// Ashen Hollow: dressing for the dungeons. The caves were bare rock and flat floor. Now, along their walls: iron
// torches on posts every few strides with warm flickering light and drifting embers, stacked crates and barrels,
// kegs and trunks, fallen rubble and broken shields, and by the boss two great banner-hung pillars and a chest of
// gold. Each dungeon gets its own flame colour (blue-white in Frostpeak, hot orange in the Molten Depths, sickly
// green in the Mire Warrens, gold in the Sand King's tomb). Props come from KayKit Dungeon Remastered (CC0).
// Nothing here blocks your way; it only stands along the walls.
using System.Collections.Generic;
using UnityEngine;

public static class AHDungeonDress
{
    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    static GameObject P(string n) { GameObject g; if (!cache.TryGetValue(n, out g)) { g = Resources.Load<GameObject>("AH/Models/KK/kk_dg_" + n); cache[n] = g; } return g; }
    public static int Torches { get; private set; }

    static Color FlameOf(string area)
    {
        if (area.Contains("frost")) return new Color(0.6f, 0.8f, 1f);
        if (area.Contains("molten") || area == "forge") return new Color(1f, 0.45f, 0.12f);
        if (area.Contains("warren")) return new Color(0.55f, 1f, 0.45f);
        if (area.Contains("tomb")) return new Color(1f, 0.82f, 0.4f);
        return new Color(1f, 0.62f, 0.28f);
    }

    public static void Setup(AHGame g)
    {
        Torches = 0;
        if (!AHDungeon.IsDungeon(AHGame.AreaId) || P("torch_lit") == null) return;
        var root = new GameObject("Dungeon dressing").transform;
        var rnd = new System.Random(AHGame.AreaId.GetHashCode());
        Color flame = FlameOf(AHGame.AreaId);
        var area = g.AreaRect;
        // spots along the walls: walkable, with rock 1 to 3 m away on one side
        var edges = new List<KeyValuePair<Vector3, Vector3>>();
        float step = 2.4f; float y = g.W(0, 0).y;
        var dirs = new Vector3[12]; for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6f; dirs[i] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); }
        for (float x = area.xMin + 1f; x < area.xMax - 1f; x += step)
            for (float z = area.yMin + 1f; z < area.yMax - 1f; z += step)
            {
                var p = new Vector3(x + (float)rnd.NextDouble() * 0.8f, y, z + (float)rnd.NextDouble() * 0.8f);
                if (g.Blocked(p, 0.8f)) continue;
                float best = 99f; Vector3 wall = Vector3.zero;
                foreach (var d in dirs) for (float k = 1.2f; k <= 3.2f; k += 0.5f) if (g.Blocked(p + d * k, 0.15f)) { if (k < best) { best = k; wall = d; } break; }
                if (best > 3.2f) continue;
                // not where the way is narrow (rock on the opposite side too)
                bool narrow = false; for (float k = 1.2f; k <= 3.2f; k += 0.5f) if (g.Blocked(p - wall * k, 0.15f)) narrow = true;
                if (narrow) continue;
                edges.Add(new KeyValuePair<Vector3, Vector3>(p + wall * Mathf.Max(0f, best - 1.0f), wall));
            }
        // shuffle, then keep spots apart: torches every ~9 m, props in between
        for (int i = edges.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); var t = edges[i]; edges[i] = edges[j]; edges[j] = t; }
        var torches = new List<Vector3>(); var props = new List<Vector3>();
        var exits = new List<Vector3>(); foreach (var s in AHGather.Spots) if (s.kind == "gate" || s.kind == "use" || s.kind == "door") exits.Add(s.pos);
        string[] kit = { "barrel_small_stack", "crates_stacked", "box_stacked", "rubble_half", "keg", "trunk_medium_A", "sword_shield_broken", "barrel_large", "candle_triple" };
        float[] kitK = { 0.55f, 0.5f, 0.55f, 0.35f, 0.6f, 0.6f, 0.7f, 0.5f, 0.8f };
        foreach (var e in edges)
        {
            Vector3 p = e.Key, wall = e.Value; bool clash = false;
            foreach (var x in exits) if ((x - p).sqrMagnitude < 9f) clash = true;
            if (clash) continue;
            bool farT = true; foreach (var t in torches) if ((t - p).sqrMagnitude < 81f) { farT = false; break; }
            if (farT && torches.Count < 40) { Torch(root, p, wall, flame); torches.Add(p); continue; }
            bool farP = true; foreach (var t in torches) if ((t - p).sqrMagnitude < 4f) farP = false; foreach (var q in props) if ((q - p).sqrMagnitude < 36f) farP = false;
            if (!farP || rnd.NextDouble() > 0.55 || props.Count > 60) continue;
            int k = rnd.Next(kit.Length); var pf = P(kit[k]); if (pf == null) continue;
            var go = Object.Instantiate(pf, root, false); Strip(go);
            go.transform.position = p + wall * 0.3f; go.transform.rotation = Quaternion.LookRotation(-wall) * Quaternion.Euler(0, (float)rnd.NextDouble() * 50f - 25f, 0);
            go.transform.localScale = Vector3.one * kitK[k];
            if (kit[k] == "candle_triple") Light(go.transform, new Vector3(0, 0.6f, 0), flame, 3.5f, 0.7f);
            AHModel.SetShadows(go); props.Add(p);
        }
        // the boss's hall: two banner-hung pillars and a chest of gold
        AHMob boss = null; foreach (var m in g.mobs) if (m != null && (boss == null || m.type.hp > boss.type.hp)) boss = m;
        if (boss != null && boss.type.elite)
        {
            Vector3 b = boss.Home; Vector3 toIn = (g.W(g.data.spawn.x, g.data.spawn.z) - b); toIn.y = 0; toIn = toIn.sqrMagnitude > 0.01f ? toIn.normalized : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, toIn);
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 at = b + toIn * 4f + side * sx * 5f; if (g.Blocked(at, 1f)) at = b + toIn * 3f + side * sx * 3.5f;
                var pil = P("pillar_decorated"); if (pil == null || g.Blocked(at, 0.8f)) continue;
                var go = Object.Instantiate(pil, root, false); Strip(go); go.transform.position = at; go.transform.rotation = Quaternion.LookRotation(toIn); go.transform.localScale = Vector3.one * 0.85f; AHModel.SetShadows(go);
                Torch(root, at + toIn * 1.2f, -toIn, flame);
            }
            var ch = P("chest_gold"); Vector3 cat = b - toIn * 3.5f;
            if (ch != null && !g.Blocked(cat, 0.6f)) { var go = Object.Instantiate(ch, root, false); Strip(go); go.transform.position = cat; go.transform.rotation = Quaternion.LookRotation(toIn); go.transform.localScale = Vector3.one * 0.9f; AHModel.SetShadows(go); var coins = P("coin_stack_large"); if (coins != null) { var c2 = Object.Instantiate(coins, root, false); Strip(c2); c2.transform.position = cat + side * 1.2f; c2.transform.localScale = Vector3.one * 0.8f; } }
        }
        Torches = torches.Count;
        Debug.Log("Ashen Hollow: dungeon dressed with " + torches.Count + " torches and " + props.Count + " props");
    }

    static void Strip(GameObject go) { foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c); }

    // an iron torch on a wooden post, leaning a little off the wall, with a flickering light and embers
    static void Torch(Transform root, Vector3 at, Vector3 wall, Color flame)
    {
        var t = new GameObject("Torch").transform; t.SetParent(root, false); t.position = at; t.rotation = Quaternion.LookRotation(-wall);
        var post = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cylinder), PrimitiveType.Cylinder); Object.Destroy(post.GetComponent<Collider>()); post.transform.SetParent(t, false);
        post.transform.localPosition = new Vector3(0, 0.75f, 0); post.transform.localScale = new Vector3(0.12f, 0.75f, 0.12f);
        var pm = new Material(Shader.Find("Universal Render Pipeline/Lit")); pm.SetColor("_BaseColor", new Color(0.28f, 0.19f, 0.12f)); post.GetComponent<Renderer>().sharedMaterial = pm;
        var head = Object.Instantiate(P("torch_lit"), t, false); Strip(head);
        head.transform.localPosition = new Vector3(0, 1.75f, 0); head.transform.localScale = Vector3.one * 0.75f;
        AHModel.SetShadows(t.gameObject);
        Light(t, new Vector3(0, 2.4f, 0.15f), flame, 9f, 1.7f).gameObject.AddComponent<AHTorchFlicker>().col = flame;
        Torches++;
    }
    static Light Light(Transform parent, Vector3 local, Color c, float range, float intensity)
    {
        var lg = new GameObject("Flame"); lg.transform.SetParent(parent, false); lg.transform.localPosition = local;
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = c; li.range = range; li.intensity = intensity; li.shadows = LightShadows.None;
        return li;
    }
}

// a living flame: the light breathes and flickers, and an ember drifts up now and then
public class AHTorchFlicker : MonoBehaviour
{
    public Color col; Light li; float baseI, ph;
    void Start() { li = GetComponent<Light>(); baseI = li.intensity; ph = Random.value * 10f; }
    void Update()
    {
        float t = Time.time + ph;
        li.intensity = baseI * (0.85f + 0.1f * Mathf.Sin(t * 9.1f) + 0.06f * Mathf.Sin(t * 23.7f) + 0.04f * Mathf.PerlinNoise(t * 3f, 0.5f));
        if (Random.value < Time.deltaTime * 2.5f) AHSpark.Emit(transform.position + Random.insideUnitSphere * 0.1f, new Vector3(Random.Range(-0.2f, 0.2f), 1.2f, Random.Range(-0.2f, 0.2f)), 0.08f, 1.2f, col);
    }
}
