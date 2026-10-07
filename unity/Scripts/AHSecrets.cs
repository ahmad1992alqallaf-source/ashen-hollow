// Ashen Hollow: secret spots and surprise world events.
//  - Secrets: every land hides four secret spots off the beaten path. Each is a faint glimmer you only notice when you
//    are close; walk onto it to find a hidden cache (coins, experience, marks, and sometimes a gem or a mystery sack).
//    Each is found once per hero; the realm keeps count ("Secrets found: 3 / 4 in this land").
//  - World events: out in the wild, every 12 to 20 minutes something happens nearby: a falling star crashes down, a
//    lost caravan crate turns up, or a wandering spirit leaves a gift. A tall beam of light marks the spot for two
//    minutes: get there first for the reward.
using System.Collections.Generic;
using UnityEngine;

public static class AHSecrets
{
    public const int PerLand = 4;
    const float FindR = 2.2f, EventLife = 120f;
    static AHWorldData builtFor; static string builtArea;
    static readonly List<GameObject> marks = new List<GameObject>();
    static readonly List<Vector3> spots = new List<Vector3>();
    static GameObject evGo; static Vector3 evAt; static float evLife, evWait = 420f; static int evKind;
    static Material glow, beam;

    static string Key(string area, int i) { return "ah_secret_" + area + "_" + i; }
    public static bool Found(string area, int i) { return AHPrefs.GetInt(Key(area, i)) == 1; }
    public static int FoundHere() { int n = 0; for (int i = 0; i < PerLand; i++) if (Found(AHGame.AreaId, i)) n++; return n; }

    static Material Mat(Color c, ref Material m)
    {
        if (m != null) return m;
        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        m = new Material(sh); if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }

    // the land's secret spots: picked from the land's own bounds with a fixed seed (the same for everyone), kept away
    // from towns and from the land's edge, and set on walkable ground
    static void Build(AHGame g)
    {
        foreach (var m in marks) if (m != null) Object.Destroy(m);
        marks.Clear(); spots.Clear();
        builtFor = g.data; builtArea = AHGame.AreaId;
        if (g.data == null || g.data.bounds == null || AHDungeon.IsDungeon(AHGame.AreaId)) return;
        var b = g.data.bounds; var rnd = new System.Random(AHGame.AreaId.GetHashCode() ^ 0x5ec2e7);
        int tries = 0;
        while (spots.Count < PerLand && tries++ < 200)
        {
            float x = Mathf.Lerp(b.x0, b.x1, 0.12f + 0.76f * (float)rnd.NextDouble()), z = Mathf.Lerp(b.z0, b.z1, 0.12f + 0.76f * (float)rnd.NextDouble());
            Vector3 p = g.Resolve(g.W(x, z), 0.5f);
            if (g.InTown(p)) continue;
            bool near = false; foreach (var s in spots) if ((s - p).sqrMagnitude < 40f * 40f) near = true;
            if (near) continue;
            spots.Add(p);
        }
        for (int i = 0; i < spots.Count; i++)
        {
            if (Found(AHGame.AreaId, i)) { marks.Add(null); continue; }
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "SecretGlimmer";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = spots[i] + Vector3.up * 0.35f; go.transform.localScale = Vector3.one * 0.22f;
            go.GetComponent<Renderer>().sharedMaterial = Mat(new Color(1f, 0.95f, 0.6f), ref glow);
            go.SetActive(false);
            marks.Add(go);
        }
    }

    public static void Tick(AHGame g, float dt)
    {
        var p = g.player; if (p == null) return;
        if (builtFor != g.data || builtArea != AHGame.AreaId) { Build(g); ClearEvent(); }
        Vector3 me = p.transform.position;
        // secrets: a glimmer you only see when close, that bobs and pulses
        for (int i = 0; i < marks.Count; i++)
        {
            var m = marks[i]; if (m == null) continue;
            float d = (spots[i] - me).magnitude;
            bool show = d < 14f; if (m.activeSelf != show) m.SetActive(show);
            if (show)
            {
                m.transform.position = spots[i] + Vector3.up * (0.35f + Mathf.Sin(Time.time * 2.4f + i) * 0.08f);
                m.transform.localScale = Vector3.one * (0.2f + Mathf.Sin(Time.time * 5f + i) * 0.04f);
                if (Random.value < dt * 3f) AHFx.Pop(m.transform.position, 0.35f, new Color(1f, 0.9f, 0.5f));
            }
            if (d < FindR && !p.dead) FindSecret(g, i);
        }
        // world events
        if (evGo != null)
        {
            evLife -= dt;
            evGo.transform.localScale = new Vector3(0.6f + Mathf.Sin(Time.time * 3f) * 0.1f, 30f, 0.6f + Mathf.Sin(Time.time * 3f) * 0.1f);
            if ((evAt - me).magnitude < FindR + 0.8f && !p.dead) ClaimEvent(g);
            else if (evLife <= 0f) { ClearEvent(); if (g.ui != null) g.ui.Toast("The light fades. Someone else got there first.", 2.5f); }
            return;
        }
        evWait -= dt; if (evWait > 0f) return;
        evWait = Random.Range(720f, 1200f);   // every twelve to twenty minutes
        if (p.dead || AHKQ.Mode != "" || AHDungeon.IsDungeon(AHGame.AreaId) || g.InTown(me)) return;
        StartEvent(g, Random.Range(0, 3));
    }

    static readonly string[] EvName = { "A falling star!", "A lost caravan crate!", "A wandering spirit!" };
    static readonly string[] EvSub = { "It crashed nearby: follow the beam of light", "A crate fell off a wagon nearby: follow the beam of light", "It left a gift nearby: follow the beam of light" };
    static readonly Color[] EvCol = { new Color(0.6f, 0.8f, 1f), new Color(1f, 0.8f, 0.4f), new Color(0.7f, 1f, 0.8f) };

    public static bool StartEvent(AHGame g, int kind)
    {
        var p = g.player; if (p == null || evGo != null) return false;
        evKind = Mathf.Clamp(kind, 0, 2);
        float a = Random.value * Mathf.PI * 2f, r = Random.Range(22f, 34f);
        evAt = g.Resolve(p.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r, 0.6f);
        evGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder); evGo.name = "EventBeam";
        Object.Destroy(evGo.GetComponent<Collider>());
        evGo.transform.position = evAt + Vector3.up * 30f;
        var mat = new Material(Mat(Color.white, ref beam)); if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", EvCol[evKind]); if (mat.HasProperty("_Color")) mat.SetColor("_Color", EvCol[evKind]);
        evGo.GetComponent<Renderer>().sharedMaterial = mat;
        evGo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        evLife = EventLife;
        AHFx.Pop(evAt + Vector3.up, 3f, EvCol[evKind], 0.8f);
        if (g.ui != null) g.ui.Banner(EvName[evKind], EvSub[evKind] + " (" + Mathf.RoundToInt(r) + " m away, 2 minutes)");
        return true;
    }

    static void ClearEvent() { if (evGo != null) Object.Destroy(evGo); evGo = null; }

    static void ClaimEvent(AHGame g)
    {
        var p = g.player; int L = Mathf.Max(1, p.level);
        var rw = new AHDaily.Reward { money = 25 + L * 10, xp = 30 + L * 12, marks = 3 };
        if (evKind == 0) { rw.items.Add(new KeyValuePair<string, int>(Random.value < 0.5f ? "sapphire" : "enh_stone", 1)); rw.name = "Star fragment"; }
        else if (evKind == 1) { rw.money *= 2; if (Random.value < 0.4f) rw.items.Add(new KeyValuePair<string, int>("mystery_sack", 1)); rw.name = "Caravan crate"; }
        else { rw.xp *= 2; rw.marks += 3; rw.name = "Spirit's gift"; }
        AHDaily.Grant(g, rw);
        AHFx.Pop(evAt + Vector3.up, 3f, EvCol[evKind], 0.8f); AHSound.Play("coin");
        if (g.ui != null) g.ui.Banner(rw.name + "!", AHDaily.Text(rw));
        ClearEvent();
    }

    static void FindSecret(AHGame g, int i)
    {
        if (Found(AHGame.AreaId, i)) return;
        AHPrefs.SetInt(Key(AHGame.AreaId, i), 1);
        var m = marks[i]; if (m != null) { AHFx.Pop(m.transform.position, 1.6f, new Color(1f, 0.9f, 0.5f), 0.6f); Object.Destroy(m); } marks[i] = null;
        var p = g.player; int L = Mathf.Max(1, p.level);
        var rw = new AHDaily.Reward { money = 40 + L * 15, xp = 50 + L * 15, marks = 5 };
        float r = Random.value;
        if (r < 0.3f) rw.items.Add(new KeyValuePair<string, int>("mystery_sack", 1));
        else if (r < 0.55f) { string[] gs = { "ruby", "sapphire", "emerald" }; rw.items.Add(new KeyValuePair<string, int>(gs[Random.Range(0, 3)], 1)); }
        AHDaily.Grant(g, rw); AHSound.Play("coin");
        if (g.ui != null) g.ui.Banner("Secret found! (" + FoundHere() + " / " + spots.Count + " in this land)", AHDaily.Text(rw));
    }

    // the map: secrets found in this land, and the world event's beam
    public static List<Vector3> FoundSpots() { var l = new List<Vector3>(); for (int i = 0; i < spots.Count; i++) if (Found(AHGame.AreaId, i)) l.Add(spots[i]); return l; }
    public static bool EventAt(out Vector3 at) { at = evAt; return evGo != null; }

    // map pins: up to six of your own marks per land, where you stood when you pinned
    public const int MaxPins = 6;
    static string PinKey { get { return "ah_pins_" + AHGame.AreaId; } }
    public static List<Vector3> Pins()
    {
        var l = new List<Vector3>(); var s = AHPrefs.GetString(PinKey, "");
        foreach (var e in s.Split(';'))
        {
            var a = e.Split(','); float x, y, z;
            if (a.Length == 3 && float.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(a[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y) && float.TryParse(a[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) l.Add(new Vector3(x, y, z));
        }
        return l;
    }
    static void SavePins(List<Vector3> l)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var v in l) { if (sb.Length > 0) sb.Append(';'); sb.Append(v.x.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "," + v.y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "," + v.z.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)); }
        AHPrefs.SetString(PinKey, sb.ToString());
    }
    public static bool AddPin(Vector3 at) { var l = Pins(); if (l.Count >= MaxPins) return false; l.Add(at); SavePins(l); return true; }
    public static void ClearPins() { SavePins(new List<Vector3>()); }

    // tests: the nearest unfound secret spot (or null)
    public static bool NearestSecret(AHGame g, out Vector3 at)
    {
        at = Vector3.zero; float best = 1e9f; bool any = false; var me = g.player.transform.position;
        for (int i = 0; i < spots.Count; i++) { if (Found(AHGame.AreaId, i)) continue; float d = (spots[i] - me).sqrMagnitude; if (d < best) { best = d; at = spots[i]; any = true; } }
        return any;
    }
}
