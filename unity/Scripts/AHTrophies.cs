// Ashen Hollow: trophies and the homestead level.
//  - Trophies: the first time you beat a boss, an elite, a world boss or a rare beast, its trophy is yours. Build the
//    Trophy stand at your homestead and the last eight you won stand on it as little golden statues; the house window
//    lists them all.
//  - Homestead level (1 to 10): renown from your house, workshops and fields, comforts and trophies. Each level fills
//    your rested XP further (+3% cap a level) and levels 3, 6 and 10 give your pens room for more animals.
using System.Collections.Generic;
using UnityEngine;

public static class AHTrophies
{
    public static int Count(AHPlayer p) { return p != null && p.prog != null ? p.prog.trophies.Count : 0; }
    public static bool Has(AHPlayer p, string id) { return p.prog.trophies.Exists(t => t.k == id); }

    static bool Worthy(AHMob m)
    {
        if (m == null || m.type == null || m.type.id == null) return false;
        return WorthyId(m.type.id) || ((m.type.elite || m.type.rare) && !m.type.id.StartsWith("kq_"));
    }
    static bool WorthyId(string id)
    {
        if (id == null || id.StartsWith("kq_") || id == "treasure_goblin") return false;   // arena foes and goblins are not trophies
        var d = AHJson.O(AHDB.Mobs, id); if (d == null) return false;
        if (AHJson.B(d, "boss") || AHJson.B(d, "elite") || AHJson.B(d, "world") || AHJson.B(d, "dboss") || AHJson.B(d, "rare") || AHJson.Has(d, "boss2")) return true;
        if (AHEvents.Rares != null) foreach (var r in AHEvents.Rares) if (AHJson.S(r, "type") == id) return true;
        return false;
    }

    // AHMob.Die
    public static void OnKill(AHGame g, AHMob m, AHPlayer by)
    {
        if (by == null || by.prog == null || !Worthy(m) || Has(by, m.type.id)) return;
        by.prog.trophies.Add(new AHKS { k = m.type.id, v = m.type.name + "|" + (m.type.model ?? "") });
        AHRareRecipes.Give(g, by, m.type.name);   // a first win always brings a rare recipe scroll
        g.MarkDirty();
        if (g.ui != null) g.ui.Banner("Trophy won: " + m.type.name, by.home != null && by.home.comf.Contains("trophies") ? "It now stands on your Trophy stand at home" : "Build a Trophy stand at your homestead to show it off");
    }

    // bosses beaten before trophies came to the realm count too (from your kill record)
    public static void Backfill(AHPlayer p)
    {
        if (p == null || p.prog == null) return;
        foreach (var kv in p.prog.kills)
        {
            if (kv.v <= 0 || Has(p, kv.k) || !WorthyId(kv.k)) continue;
            var d = AHJson.O(AHDB.Mobs, kv.k);
            p.prog.trophies.Add(new AHKS { k = kv.k, v = (AHJson.S(d, "name") ?? kv.k) + "|" + (AHJson.S(d, "model") ?? "") });
        }
    }

    // a test: these bosses as a stand (their names and models from the bestiary)
    public static List<AHKS> Sample(params string[] ids)
    {
        var l = new List<AHKS>();
        foreach (var id in ids) { var d = AHJson.O(AHDB.Mobs, id); if (d != null) l.Add(new AHKS { k = id, v = (AHJson.S(d, "name") ?? id) + "|" + (AHJson.S(d, "model") ?? "") }); }
        return l;
    }

    public static string Name(AHKS t) { int i = t.v.IndexOf('|'); return i >= 0 ? t.v.Substring(0, i) : t.v; }
    static string Model(AHKS t)
    {
        int i = t.v.IndexOf('|'); string m = i >= 0 ? t.v.Substring(i + 1) : "";
        if (!string.IsNullOrEmpty(m)) return m;
        // trophies from before (or a boss whose bestiary entry has no model): the model its land uses
        if (models == null)
        {
            models = new Dictionary<string, string>();
            var ta = Resources.Load<TextAsset>("AH/Data/mobmodels");
            if (ta != null) foreach (var line in ta.text.Split('\n')) { int e = line.IndexOf('='); if (e > 0) models[line.Substring(0, e).Trim()] = line.Substring(e + 1).Trim(); }
        }
        string mm; return models.TryGetValue(t.k, out mm) ? mm : "";
    }
    static Dictionary<string, string> models;

    // ---------- the homestead level ----------
    static readonly int[] LevelAt = { 0, 15, 30, 45, 60, 80, 100, 125, 150, 180 };
    public static int Renown(AHPlayer p)
    {
        if (p == null || p.home == null) return 0;
        return p.home.tier * 5 + p.home.built.Count * 2 + AHHome.ComfortPts(p) * 2 + Count(p) * 3;
    }
    public static int Level(AHPlayer p) { int r = Renown(p), L = 1; for (int i = 1; i < LevelAt.Length; i++) if (r >= LevelAt[i]) L = i + 1; return p != null && p.home != null ? L : 0; }
    public static int NextAt(AHPlayer p) { int L = Level(p); return L >= 1 && L < LevelAt.Length ? LevelAt[L] : -1; }
    public static float RestK(AHPlayer p) { int L = Level(p); return 1f + Mathf.Max(0, L - 1) * 0.03f; }
    public static int PenBonus(AHPlayer p) { int L = Level(p); return (L >= 3 ? 1 : 0) + (L >= 6 ? 1 : 0) + (L >= 10 ? 2 : 0); }
    public static string Perks(AHPlayer p)
    {
        int L = Level(p);
        return "Rested XP +" + Mathf.RoundToInt((RestK(p) - 1f) * 100f) + "% · +" + PenBonus(p) + " pen space" + (L < 10 ? " · next: " + (L + 1 == 3 || L + 1 == 6 || L + 1 == 10 ? "more pen space" : "more rested XP") : "");
    }

    // ---------- the statues on the Trophy stand ----------
    public static void BuildStand(AHGame g, Transform stand, List<AHKS> only = null)
    {
        var p = g != null ? g.player : null; if (p == null || p.prog == null) return;
        var list = only ?? p.prog.trophies; int n = Mathf.Min(8, list.Count);
        int Gold = 0xd4a640;
        for (int i = 0; i < n; i++)
        {
            var t = list[list.Count - n + i];
            float x = (i - (n - 1) / 2f) * 0.7f;
            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(plinth.GetComponent<Collider>());
            plinth.transform.SetParent(stand, false); plinth.transform.localPosition = new Vector3(x, 1.08f, 0f); plinth.transform.localScale = new Vector3(0.42f, 0.08f, 0.42f);
            plinth.GetComponent<Renderer>().sharedMaterial = Mat(0x3a2a1c);
            var holder = new GameObject("Trophy_" + t.k).transform; holder.SetParent(stand, false); holder.localPosition = new Vector3(x, 1.16f, 0f);
            if (!Statue(g, holder, t)) Cup(holder, Gold);
        }
    }

    static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
    static Material Mat(int hex)
    {
        Material m; if (mats.TryGetValue(hex, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")); var c = new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);
        m.SetColor("_BaseColor", c); if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", hex == 0xd4a640 ? 0.8f : 0f); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.6f);
        mats[hex] = m; return m;
    }

    // a golden cup when the beast can't be made small
    static void Cup(Transform h, int gold)
    {
        var a = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(a.GetComponent<Collider>()); a.transform.SetParent(h, false); a.transform.localPosition = new Vector3(0, 0.06f, 0); a.transform.localScale = new Vector3(0.1f, 0.06f, 0.1f); a.GetComponent<Renderer>().sharedMaterial = Mat(gold);
        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(b.GetComponent<Collider>()); b.transform.SetParent(h, false); b.transform.localPosition = new Vector3(0, 0.26f, 0); b.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f); b.GetComponent<Renderer>().sharedMaterial = Mat(gold);
    }

    // the beast itself, made small and golden
    static bool Statue(AHGame g, Transform h, AHKS t)
    {
        try
        {
            var tp = new AHMobType { id = t.k, name = Name(t), model = Model(t), hp = 1, radius = 0.5f, speed = 0f, aggro = 0f, respawn = 1e9f, noSkin = true };
            var m = AHMob.Create(g, tp, h.position);
            if (m == null) return false;
            var go = m.gameObject; m.enabled = false; Object.Destroy(m);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            foreach (var c in go.GetComponentsInChildren<Canvas>(true)) Object.Destroy(c.gameObject);
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { Object.Destroy(go); return false; }
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float k = 0.55f / Mathf.Max(0.05f, Mathf.Max(b.size.y, Mathf.Max(b.size.x, b.size.z) * 0.8f));
            go.transform.SetParent(h, true); go.transform.localScale *= k;
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            go.transform.position += new Vector3(h.position.x - b.center.x, h.position.y - b.min.y, h.position.z - b.center.z);
            go.transform.rotation = h.rotation * Quaternion.Euler(0f, 180f, 0f);
            var gold = Mat(0xd4a640);
            foreach (var r in rs) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = gold; r.sharedMaterials = ms; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            return true;
        }
        catch (System.Exception e) { Debug.Log("Ashen Hollow: trophy statue " + t.k + " failed: " + e.Message); return false; }
    }
}
