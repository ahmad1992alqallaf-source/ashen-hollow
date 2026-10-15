// Ashen Hollow: Looks - weapon skins, hit effects, footstep trails and name colours (Resources/AH/Data/looks.json).
// Looks only: none of them gives stats. Each is earned (a skill level, your level, kills with your weapon, a boss beaten,
// trophies), free, or bought in the store with Crowns. Menu -> Looks (or the store) lists them all with how to get
// each; anything can be tried on for 20 seconds first.
//  - Weapon skin: the weapon in your hand takes the skin's colour; the living ones glow and throw off embers, frost,
//    leaves, violet smoke, sparks, golden motes or every colour in turn.
//  - Hit effect: a burst on everything you hit (sparks, embers, stars, gold coins, petals, ink, frost).
//  - Trail: something left behind your feet as you walk and run.
//  - Name colour: your name above your head (Royal gold shimmers).
using System.Collections.Generic;
using UnityEngine;

public static class AHLooks
{
    public static readonly string[] Kinds = { "skin", "hit", "trail", "name" };
    public static readonly string[] KindName = { "Weapon skins", "Hit effects", "Footstep trails", "Name colours" };

    public class Look { public string id, kind, name, desc, how, skill, trophy, fx; public int lvl, n; public long price; public Color col; public float amt; public bool glow; }
    static List<Look> all; static List<object> src;
    public static List<Look> All
    {
        get
        {
            var l = AHJson.A(AHDB.File("looks"), "LOOKS");
            if (all != null && ReferenceEquals(l, src)) return all;
            src = l; all = new List<Look>();
            if (l != null) foreach (var o in l)
            {
                var d = o as Dictionary<string, object>; if (d == null) continue;
                Color c; if (!ColorUtility.TryParseHtmlString(AHJson.S(d, "col", "#ffffff"), out c)) c = Color.white;
                all.Add(new Look { id = AHJson.S(d, "id"), kind = AHJson.S(d, "kind"), name = AHJson.S(d, "name"), desc = AHJson.S(d, "desc", ""), how = AHJson.S(d, "how", "free"),
                    skill = AHJson.S(d, "skill"), trophy = AHJson.S(d, "trophy"), fx = AHJson.S(d, "fx"), lvl = (int)AHJson.N(d, "lvl"), n = (int)AHJson.N(d, "n"),
                    price = (long)AHJson.N(d, "price"), col = c, amt = (float)AHJson.N(d, "amt", 0.5), glow = AHJson.B(d, "glow") });
            }
            return all;
        }
    }
    public static Look Get(string id) { return id == null ? null : All.Find(x => x.id == id); }

    // ---------- owning ----------
    public static bool Earned(AHPlayer p, Look d)
    {
        switch (d.how)
        {
            case "free": return true;
            case "skill": return p.Skill(d.skill) >= d.lvl;
            case "level": return p.level >= d.lvl;
            case "kills": return AHExtras.Kills(p) >= d.n;
            case "trophy": return AHTrophies.Has(p, d.trophy);
            case "trophies": return AHTrophies.Count(p) >= d.n;
            default: return false;
        }
    }
    public static bool Owned(AHPlayer p, Look d) { return Earned(p, d) || p.prog.looks.Contains(d.id); }
    public static string HowText(AHPlayer p, Look d)
    {
        switch (d.how)
        {
            case "free": return "Yours from the start";
            case "skill": return AHDB.SkillName(d.skill) + " level " + d.lvl + " (you have " + p.Skill(d.skill) + ")";
            case "level": return "Reach level " + d.lvl + " (you are " + p.level + ")";
            case "kills": return d.n.ToString("#,0") + " kills with your weapon (" + AHExtras.Kills(p).ToString("#,0") + " so far)";
            case "trophy": return "Beat " + AHJson.S(AHJson.O(AHDB.Mobs, d.trophy), "name", d.trophy);
            case "trophies": return "Beat " + d.n + " great foes (bosses, elites, rare beasts: " + AHTrophies.Count(p) + " so far)";
            case "store": return "Store · " + AHStore.Amount(d.price);
        }
        return "";
    }
    public static int OwnedCount(AHPlayer p, string kind) { int n = 0; foreach (var d in All) if (d.kind == kind && Owned(p, d)) n++; return n; }

    // ---------- wearing ----------
    static readonly Dictionary<string, string> preview = new Dictionary<string, string>(); static readonly Dictionary<string, float> previewUntil = new Dictionary<string, float>();
    public static string On(AHPlayer p, string kind)
    {
        float u; if (preview.ContainsKey(kind) && previewUntil.TryGetValue(kind, out u) && Time.time < u) return preview[kind];
        foreach (var e in p.prog.lookOn) if (e.k == kind) return e.v;
        return null;
    }
    public static Look Worn(AHPlayer p, string kind) { var d = Get(On(p, kind)); return d != null && (Owned(p, d) || preview.ContainsKey(kind)) ? d : null; }
    public static void Wear(AHGame g, string kind, string id)
    {
        var p = g.player; p.prog.lookOn.RemoveAll(e => e.k == kind); if (id != null) p.prog.lookOn.Add(new AHKS { k = kind, v = id });
        preview.Remove(kind); Dirty = true; g.MarkDirty();
    }
    public static void Try(string kind, string id) { preview[kind] = id; previewUntil[kind] = Time.time + 20f; Dirty = true; }
    public static bool Trying(string kind, string id) { float u; return preview.ContainsKey(kind) && preview[kind] == id && previewUntil.TryGetValue(kind, out u) && Time.time < u; }
    public static bool Buy(AHGame g, Look d)
    {
        var p = g.player; if (d.how != "store" || Owned(p, d) || p.prog.crowns < d.price) return false;
        p.prog.crowns -= d.price; p.prog.looks.Add(d.id); Wear(g, d.kind, d.id); g.SaveProgress(); return true;
    }
    public static bool Dirty = true;

    // ---------- showing ----------
    public static void Ensure(AHPlayer p) { if (p != null && p.GetComponent<AHLooksFx>() == null) p.gameObject.AddComponent<AHLooksFx>(); }

    public static string NameRich(AHPlayer p, string name)
    {
        var d = p != null ? Worn(p, "name") : null; if (d == null) return name;
        Color c = d.col;
        if (d.fx == "shimmer") c = Color.Lerp(d.col, new Color(1f, 0.97f, 0.8f), 0.5f + 0.5f * Mathf.Sin(Time.time * 3f));
        return "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + name + "</color>";
    }

    // a hit landed: the hit effect bursts on it
    public static void OnHit(AHPlayer p, Vector3 at)
    {
        var d = p != null ? Worn(p, "hit") : null; if (d == null) return;
        Color c = d.col;
        switch (d.fx)
        {
            case "coins": AHSpark.Burst(at, c, 14, 3.2f, 0.7f, 0.12f, 2.2f); AHSpark.Ring(at, new Color(1f, 0.92f, 0.6f), 10, 2.5f, 0.4f, 0.1f); break;
            case "petals": AHSpark.Burst(at, c, 16, 2.4f, 1.1f, 0.14f, 0.6f); break;
            case "ink": AHSpark.Burst(at, c, 18, 3.4f, 0.5f, 0.2f); AHSpark.Burst(at, new Color(0.08f, 0.07f, 0.12f), 8, 1.5f, 0.6f, 0.28f); break;
            case "frost": AHSpark.Burst(at, c, 14, 4f, 0.45f, 0.12f); AHSpark.Ring(at, new Color(0.85f, 0.97f, 1f), 12, 3f, 0.4f, 0.12f); break;
            case "stars": AHSpark.Burst(at, c, 10, 2.6f, 0.8f, 0.16f, 1.2f); AHSpark.Ring(at, new Color(1f, 1f, 0.85f), 8, 2f, 0.5f, 0.1f); break;
            case "ember": AHSpark.Burst(at, c, 16, 3.5f, 0.6f, 0.12f, 1.5f); break;
            default: AHSpark.Burst(at, c, 12, 4.2f, 0.35f, 0.1f); break;
        }
    }
}

// on the hero: the weapon's skin and its magic, and the trail behind your feet
public class AHLooksFx : MonoBehaviour
{
    AHPlayer p; string skinOn; readonly List<Renderer> rs = new List<Renderer>();
    readonly Dictionary<Material, Color[]> keep = new Dictionary<Material, Color[]>();
    float scanT, fxT, trailT; Vector3 lastPos; int handCount;
    static readonly string[] ColProps = { "_BaseColor", "baseColorFactor", "_Color" };
    static readonly string[] EmiProps = { "_EmissionColor", "emissiveFactor" };

    void Start() { p = GetComponent<AHPlayer>(); lastPos = transform.position; }

    // the weapon in the hand (not a shield): renderers under a "Weapon_" object
    List<Renderer> Weapon()
    {
        var l = new List<Renderer>();
        foreach (var t in GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("Weapon_") || t.name.Contains("Shield")) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is ParticleSystemRenderer) && !l.Contains(r)) l.Add(r);
        }
        return l;
    }

    void Restore()
    {
        foreach (var kv in keep)
        {
            if (kv.Key == null) continue;
            for (int i = 0; i < ColProps.Length; i++) if (kv.Key.HasProperty(ColProps[i])) kv.Key.SetColor(ColProps[i], kv.Value[i]);
            for (int i = 0; i < EmiProps.Length; i++) if (kv.Key.HasProperty(EmiProps[i])) kv.Key.SetColor(EmiProps[i], kv.Value[ColProps.Length + i]);
        }
        keep.Clear(); rs.Clear();
    }

    void Apply(AHLooks.Look d, float t)
    {
        foreach (var r in rs)
        {
            if (r == null) continue;
            foreach (var m in r.materials)
            {
                if (!keep.ContainsKey(m))
                {
                    var save = new Color[ColProps.Length + EmiProps.Length];
                    for (int i = 0; i < ColProps.Length; i++) save[i] = m.HasProperty(ColProps[i]) ? m.GetColor(ColProps[i]) : Color.white;
                    for (int i = 0; i < EmiProps.Length; i++) save[ColProps.Length + i] = m.HasProperty(EmiProps[i]) ? m.GetColor(EmiProps[i]) : Color.black;
                    keep[m] = save;
                }
                var o = keep[m]; Color c = Tint(d, t);
                for (int i = 0; i < ColProps.Length; i++) if (m.HasProperty(ColProps[i])) { var b = o[i]; float l = b.r * 0.3f + b.g * 0.59f + b.b * 0.11f; var nc = Color.Lerp(b, c * (0.45f + l * 0.9f), d.amt); nc.a = b.a; m.SetColor(ColProps[i], nc); }
                if (d.glow) { m.EnableKeyword("_EMISSION"); for (int i = 0; i < EmiProps.Length; i++) if (m.HasProperty(EmiProps[i])) m.SetColor(EmiProps[i], c * (0.55f + 0.25f * Mathf.Sin(t * 2.2f))); }
            }
        }
    }
    static Color Tint(AHLooks.Look d, float t) { return d.fx == "prism" ? Color.HSVToRGB(Mathf.Repeat(t * 0.08f, 1f), 0.65f, 1f) : d.col; }

    void Update()
    {
        if (p == null) return;
        float dt = Time.deltaTime; float t = Time.time;
        // the skin: put on (or taken off) when it changes, or when a different weapon is in the hand
        var skin = AHLooks.Worn(p, "skin"); string want = skin != null ? skin.id : null;
        scanT -= dt;
        if (scanT <= 0f || AHLooks.Dirty)
        {
            scanT = 1f; AHLooks.Dirty = false;
            var w = Weapon(); bool same = w.Count == rs.Count && w.TrueForAll(r => rs.Contains(r));
            if (!same || want != skinOn) { Restore(); skinOn = want; if (skin != null) rs.AddRange(w); }
        }
        if (skin != null && rs.Count > 0 && (skin.glow || skin.fx == "prism")) Apply(skin, t);
        else if (skin != null && rs.Count > 0 && keep.Count == 0) Apply(skin, t);
        // its magic: off the top of the weapon
        if (skin != null && skin.fx != null && rs.Count > 0 && rs[0] != null && (fxT -= dt) <= 0f)
        {
            fxT = 0.12f; Bounds b = rs[0].bounds; foreach (var r in rs) if (r != null) b.Encapsulate(r.bounds);
            Vector3 at = b.center + new Vector3(Random.Range(-b.extents.x, b.extents.x), Random.Range(-b.extents.y, b.extents.y), Random.Range(-b.extents.z, b.extents.z)) * 0.8f;
            Color c = Tint(skin, t);
            switch (skin.fx)
            {
                case "ember": AHSpark.Emit(at, new Vector3(0f, 0.6f, 0f) + Random.insideUnitSphere * 0.3f, 0.07f, 0.8f, c); break;
                case "frost": AHSpark.Emit(at, new Vector3(0f, -0.4f, 0f) + Random.insideUnitSphere * 0.15f, 0.06f, 1f, new Color(0.9f, 0.97f, 1f)); break;
                case "nature": AHSpark.Emit(at, new Vector3(0f, -0.3f, 0f) + Random.insideUnitSphere * 0.3f, 0.09f, 1.2f, c); break;
                case "void": AHSpark.Emit(at, new Vector3(0f, 0.25f, 0f) + Random.insideUnitSphere * 0.1f, 0.16f, 1.1f, new Color(c.r, c.g, c.b, 0.55f)); break;
                case "storm": if (Random.value < 0.4f) AHSpark.Burst(at, new Color(0.85f, 0.92f, 1f), 3, 2.5f, 0.15f, 0.05f); break;
                case "holy": AHSpark.Emit(at, new Vector3(0f, 0.7f, 0f), 0.06f, 1f, c); break;
                case "runes": if (Random.value < 0.5f) AHSpark.Emit(at, Vector3.up * 0.2f, 0.08f, 0.6f, c); break;
                case "sand": AHSpark.Emit(at, new Vector3(0f, -0.5f, 0f) + Random.insideUnitSphere * 0.2f, 0.05f, 0.9f, c); break;
                case "prism": AHSpark.Emit(at, Random.insideUnitSphere * 0.3f, 0.07f, 0.7f, c); break;
            }
        }
        // the trail: a little behind the feet while you move on the ground
        var tr = AHLooks.Worn(p, "trail");
        Vector3 d = transform.position - lastPos; d.y = 0f; lastPos = transform.position;
        if (tr != null && !p.mounted && d.magnitude > 0.01f && (trailT -= dt) <= 0f)
        {
            trailT = 0.09f; Vector3 at = transform.position + Vector3.up * 0.08f - d.normalized * 0.3f + Random.insideUnitSphere * 0.15f;
            Color c = tr.col;
            switch (tr.fx)
            {
                case "ember": AHSpark.Emit(at, Vector3.up * 0.5f + Random.insideUnitSphere * 0.2f, 0.07f, 0.8f, c); break;
                case "leaf": case "petal": AHSpark.Emit(at + Vector3.up * 0.2f, Vector3.up * 0.15f + Random.insideUnitSphere * 0.35f, 0.1f, 1.3f, c); break;
                case "frost": AHSpark.Emit(at, Vector3.zero, 0.12f, 1.4f, c); break;
                case "shadow": AHSpark.Emit(at, Vector3.up * 0.35f, 0.18f, 1f, new Color(c.r, c.g, c.b, 0.5f)); break;
                case "star": AHSpark.Emit(at + Vector3.up * 0.15f, Random.insideUnitSphere * 0.25f, 0.07f, 0.9f, c); break;
                default: AHSpark.Trail(at, c, 0.1f, 1); break;
            }
        }
    }
    void OnDestroy() { Restore(); }
}

public partial class AHUI
{
    public void OpenLooks() { wkMode = "looks"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderLooks(AHPlayer p)
    {
        int own = 0; foreach (var d in AHLooks.All) if (AHLooks.Owned(p, d)) own++;
        wkTitle.text = "Looks · " + own + " / " + AHLooks.All.Count + " collected";
        wkHint.text = "Weapon skins, hit effects, trails and name colours. Looks only: none of them makes you stronger. Try anything on for 20 seconds first.";
        var rows = new List<System.Action<int>>();
        for (int ki = 0; ki < AHLooks.Kinds.Length; ki++)
        {
            string kind = AHLooks.Kinds[ki]; var worn = AHLooks.Worn(p, kind);
            rows.Add(s => Row(s, AHLooks.KindName[System.Array.IndexOf(AHLooks.Kinds, kind)] + " · " + AHLooks.OwnedCount(p, kind) + " / " + AHLooks.All.FindAll(x => x.kind == kind).Count, new Color(1f, 0.85f, 0.4f),
                "Now: " + (worn != null ? worn.name : "none"), "",
                worn != null ? new WkBtn { label = "Take off", on = true, col = Plain, act = () => { AHLooks.Wear(g, kind, null); RenderWork(); } } : null));
            foreach (var d0 in AHLooks.All)
            {
                if (d0.kind != kind) continue; var d = d0;
                bool owned = AHLooks.Owned(p, d), on = worn != null && worn.id == d.id && !AHLooks.Trying(kind, d.id), trying = AHLooks.Trying(kind, d.id);
                string hex = "#" + ColorUtility.ToHtmlStringRGB(d.col);
                string title = "<color=" + hex + ">●</color> " + d.name + (on ? "  <color=#9be37a>in use</color>" : trying ? "  <color=#7ad0ff>trying on</color>" : owned ? "  <color=#c8b89a>yours</color>" : "");
                var tryB = owned ? null : new WkBtn { label = trying ? "Trying" : "Try", on = !trying, col = Plain, act = () => { AHLooks.Try(kind, d.id); Toast("Trying " + d.name + " for 20 seconds"); RenderWork(); } };
                WkBtn main;
                if (owned) main = new WkBtn { label = on ? "In use" : "Use", on = !on, col = Go, act = () => { AHLooks.Wear(g, kind, d.id); RenderWork(); } };
                else if (d.how == "store") main = new WkBtn { label = "Buy " + d.price.ToString("#,0"), on = p.prog.crowns >= d.price, col = Go, act = () => { if (AHLooks.Buy(g, d)) { Banner(d.name, "Yours now · in use"); AHSound.Play("level"); } else Toast("Not enough " + AHStore.Currency + "."); RenderWork(); } };
                else main = null;
                rows.Add(s => Row(s, title, Color.white, owned ? d.desc : AHLooks.HowText(p, d), owned ? AHLooks.HowText(p, d) : d.desc, tryB, main));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
