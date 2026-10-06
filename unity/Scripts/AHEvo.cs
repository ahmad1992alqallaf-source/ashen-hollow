// Ashen Hollow: class progression, as in the web game (v66, TALENTS / PATHS / EVO_SPELLS / BOOK / evolve / calcPass / buildSpells).
//  - Evolutions: at level 30 each class picks one of two paths (a new spell replaces one of the five, plus lasting
//    passives); at level 60 one of two forms of that path (an ultimate, a sixth button, and more passives).
//    The choices are permanent. (The Pyromancer's Phoenix form is not part of this port.)
//  - Talents: two points every 3 levels (they replace the old stat points); tier I is open from the start, tier II with a path, tier III with a form.
//  - The spellbook: a new spell every 5 levels (ten per class). Choose which five go on your ring.
// Data: Resources/AH/Data/evo.json, taken from the web game; "U" says how each spell plays here (kind, multiplier, size).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AHClassProg { public string cls, path, form; public List<AHKV> tal = new List<AHKV>(); public List<string> load = new List<string>(); }

public static class AHEvo
{
    static object D { get { return AHDB.File("evo"); } }
    public static List<object> Paths(string cls) { return AHJson.A(AHJson.O(D, "PATHS"), cls); }
    public static object Path(string cls, string id) { var l = Paths(cls); if (l != null && id != null) foreach (var p in l) if (AHJson.S(p, "id") == id) return p; return null; }
    public static object Form(object path, string id) { var l = AHJson.A(path, "forms"); if (l != null && id != null) foreach (var f in l) if (AHJson.S(f, "id") == id) return f; return null; }

    public static AHClassProg CP(AHPlayer p)
    {
        foreach (var c in p.prog.cps) if (c.cls == p.cls.id) return c;
        var n = new AHClassProg { cls = p.cls.id }; p.prog.cps.Add(n); return n;
    }
    public static object CurPath(AHPlayer p) { return Path(p.cls.id, CP(p).path); }
    public static object CurForm(AHPlayer p) { return Form(CurPath(p), CP(p).form); }
    public static string Title(AHPlayer p) { var f = CurForm(p); if (f != null) return AHJson.S(f, "name"); var pa = CurPath(p); return pa != null ? AHJson.S(pa, "name") : p.cls.name; }
    public static Color PathColor(AHPlayer p) { var pa = CurPath(p); return pa != null ? AHGame.Hex((int)AHJson.N(pa, "color")) : p.cls.color; }

    // ---------- talents ----------
    public class Tier { public string name, req; public List<object> nodes = new List<object>(); public bool open; }
    public static List<Tier> Tiers(AHPlayer p)
    {
        var pa = CurPath(p); var f = CurForm(p);
        var t = new List<Tier>();
        t.Add(new Tier { name = "Tier I", req = "Open from level 1", nodes = AHJson.A(AHJson.O(D, "TALENTS"), p.cls.id) ?? new List<object>(), open = true });
        t.Add(new Tier { name = "Tier II", req = pa != null ? AHJson.S(pa, "name") + " talents" : "Choose a path at level 30 to unlock", nodes = pa != null ? AHJson.A(pa, "tal") : new List<object>(), open = pa != null });
        t.Add(new Tier { name = "Tier III", req = f != null ? AHJson.S(f, "name") + " talents" : "Choose a form at level 60 to unlock", nodes = f != null ? AHJson.A(f, "tal") : new List<object>(), open = f != null });
        return t;
    }
    // two points every three levels (the old stat points were folded in here)
    public static int Points(AHPlayer p) { return p.level * 2 / 3; }
    public static int Spent(AHPlayer p) { long n = 0; foreach (var e in CP(p).tal) n += e.v; return (int)n; }
    public static int Rank(AHPlayer p, string id) { return (int)AHProgress.Get(CP(p).tal, id); }
    public static void Learn(AHGame g, object node)
    {
        var p = g.player; string id = AHJson.S(node, "id"); int max = (int)AHJson.N(node, "max", 3);
        if (Points(p) - Spent(p) <= 0 || Rank(p, id) >= max) return;
        AHProgress.Add(CP(p).tal, id, 1); p.Recalc(); g.MarkDirty();
    }
    public static void ResetTalents(AHGame g) { var p = g.player; CP(p).tal.Clear(); p.Recalc(); g.MarkDirty(); }

    // web calcPass: path, form and talent passives
    public static Dictionary<string, float> CalcPass(AHPlayer p)
    {
        var o = new Dictionary<string, float>();
        Action<object, int> add = (fx, times) =>
        {
            var d = fx as Dictionary<string, object>; if (d == null) return;
            foreach (var kv in d) { float v = kv.Value is bool ? ((bool)kv.Value ? 1f : 0f) : (float)(double)kv.Value; float cur; o.TryGetValue(kv.Key, out cur); o[kv.Key] = kv.Value is bool ? Mathf.Max(cur, v) : cur + v * times; }
        };
        if (p.cls == null || p.prog == null) return o;
        var pa = CurPath(p); var f = CurForm(p);
        if (pa != null) add(AHJson.O(pa, "pass"), 1);
        if (f != null) add(AHJson.O(f, "pass"), 1);
        foreach (var t in Tiers(p)) if (t.nodes != null) foreach (var n in t.nodes) { int r = Rank(p, AHJson.S(n, "id")); if (r > 0) add(AHJson.O(n, "per"), r); }
        return o;
    }
    public static float Pass(AHPlayer p, string k) { float v; return p.pass != null && p.pass.TryGetValue(k, out v) ? v : 0f; }
    static readonly Dictionary<string, Func<float, string>> PassTxt = new Dictionary<string, Func<float, string>>
    {
        { "dmg", v => "+" + Pc(v) + " damage" }, { "hp", v => "+" + v + " max HP" }, { "armor", v => "+" + Pc(v) + " armor" }, { "cdr", v => Pc(v) + " shorter cooldowns" },
        { "leech", v => "heal " + Pc(v) + " of damage dealt" }, { "thorns", v => "reflect " + Pc(v) + " of damage taken" }, { "block", v => Pc(v) + " chance to block a hit" },
        { "burn", v => "burns deal +" + Pc(v) }, { "fireball", v => "Fireball +" + Pc(v) + " damage" }, { "holyfire", v => "Holy Fire +" + Pc(v) + " damage" },
        { "frozen", v => "+" + Pc(v) + " damage to frozen or stunned enemies" }, { "novaCd", v => "Frost Nova cooldown −" + v + "s" }, { "divineCd", v => "Divine Shield cooldown −" + v + "s" },
        { "chargeCd", v => "Charge cooldown −" + v + "s" }, { "barrier", v => "Mana Barrier absorbs +" + Pc(v) }, { "heal", v => "healing +" + Pc(v) },
        { "killHeal", v => "kills heal " + Pc(v) + " of your HP" }, { "atkspd", v => "attack speed +" + Pc(v) }, { "evo", v => "evolution spells +" + Pc(v) + " damage" },
        { "arc", v => "basic bolts have " + Pc(v) + " chance to chain" }, { "smite", v => "Holy Fire heals you for " + Pc(v) + " of its damage" },
        { "rage", v => "+20% damage while below half HP" }, { "explode", v => "enemies you kill explode in fire" }, { "cheat", v => "once every 2 minutes, survive a killing blow" },
        { "crit", v => Pc(v) + " chance to land a critical hit" }, { "evade", v => Pc(v) + " chance to evade attacks" },
        { "stunHit", v => "attacks stun for 1.2s (" + Pc(v) + " chance)" }, { "fearHit", v => "attacks terrify for 2.5s (" + Pc(v) + " chance)" },
        { "venom", v => "attacks poison for " + Pc(v) + " of your power a second" }, { "ranged", v => "you fight at range with throwing knives" },
        { "pctHeal", v => "heals restore +" + Pc(v) + " of max HP" }, { "manaHeal", v => "heals draw on " + Pc(v) + " of your max mana" },
        { "healShield", v => "heals also shield for " + Pc(v) + " of the heal" }, { "totem", v => "totems last +" + Pc(v) },
    };
    static string Pc(float v) { return Mathf.RoundToInt(v * 100) + "%"; }
    public static string PassText(object fx, int times = 1)
    {
        var d = fx as Dictionary<string, object>; if (d == null) return "";
        var l = new List<string>();
        foreach (var kv in d) { Func<float, string> f; float v = kv.Value is bool ? 1f : (float)(double)kv.Value * Mathf.Max(1, times); if (PassTxt.TryGetValue(kv.Key, out f)) l.Add(f((float)Math.Round(v, 3))); }
        string s = string.Join(" · ", l.ToArray());
        return s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1) : s;
    }

    // ---------- evolving ----------
    public static void Evolve(AHGame g, int tier, string id)
    {
        var p = g.player; var cp = CP(p);
        if (tier == 0) { if (p.level < 30 || cp.path != null || Path(p.cls.id, id) == null) return; cp.path = id; }
        else { var pa = CurPath(p); if (p.level < 60 || pa == null || cp.form != null || Form(pa, id) == null) return; cp.form = id; }
        if (tier == 0 && cp.load.Count > 0)
        {
            var pa = CurPath(p); var sw = AHJson.A(pa, "swap"); int si = (int)(double)sw[0]; string ns = (string)sw[1], bs = p.cls.spells[si].id;
            int i = cp.load.IndexOf(bs); if (i >= 0) cp.load[i] = ns; else if (!cp.load.Contains(ns) && si < cp.load.Count) cp.load[si] = ns;
        }
        p.RebuildRing(); p.Recalc();
        g.ui.Banner(Title(p), tier == 1 ? "Final evolution" : "You evolved");
        g.ui.Toast(Title(p) + (tier == 1 ? ": ultimate added to your ring (the ★ button)." : ": new spell on your ring."), 4f);
        AHFx.Ring(p.transform.position, 0.5f, 4f, PathColor(p), 0.8f);
        AHFx.Pillar(p.transform.position, 1f, 4f, PathColor(p), 1f);
        g.ui.RefreshClass();
        g.SaveProgress();
    }

    // ---------- spells ----------
    static SpellDef Make(string id, object def)
    {
        var u = AHJson.A(AHJson.O(D, "U"), id); if (u == null) return null;
        string name = AHJson.S(def, "name", id);
        SpellKind kind; try { kind = (SpellKind)Enum.Parse(typeof(SpellKind), (string)u[0]); } catch { kind = SpellKind.Strike; }
        string ab = name.Replace("’", "").Replace("'", "").Replace(" ", "");
        var sp = new SpellDef(id, name, ab.Substring(0, Math.Min(3, ab.Length)).ToUpperInvariant(), kind, (float)AHJson.N(def, "cd", 10));
        sp.mult = (float)(double)u[1]; sp.radius = (float)(double)u[2]; sp.range = (float)(double)u[3]; sp.value = (float)(double)u[4]; sp.time = (float)(double)u[5];
        string st = (string)u[6]; if (!string.IsNullOrEmpty(st)) { try { sp.status = (AHStatus)Enum.Parse(typeof(AHStatus), st); } catch { } sp.statusTime = (float)(double)u[7]; }
        sp.color = AHGame.Hex(Convert.ToInt32((string)u[8], 16));
        sp.desc = AHJson.S(def, "desc");
        sp.lvl = (int)AHJson.N(def, "lvl", 1);
        return sp;
    }
    static readonly Dictionary<string, SpellDef> made = new Dictionary<string, SpellDef>();
    public static SpellDef Evo(string id) { SpellDef s; if (made.TryGetValue(id, out s)) return s; s = Make(id, AHJson.O(AHJson.O(D, "EVO_SPELLS"), id)); made[id] = s; return s; }
    public static bool IsEvo(string id) { return AHJson.O(AHJson.O(D, "EVO_SPELLS"), id) != null; }
    public static List<SpellDef> Book(string cls)
    {
        var l = new List<SpellDef>(); var b = AHJson.A(AHJson.O(D, "BOOK"), cls);
        if (b != null) foreach (var o in b) { string id = AHJson.S(o, "id"); SpellDef s; if (!made.TryGetValue(id, out s)) { s = Make(id, o); made[id] = s; } if (s != null) l.Add(s); }
        return l;
    }
    // web spellPool
    public static List<SpellDef> Pool(AHPlayer p)
    {
        var o = new List<SpellDef>(p.cls.spells);
        foreach (var s0 in o) if (s0.desc == null) s0.desc = AHJson.S(AHJson.O(D, "DESC"), s0.id, "");
        var pa = CurPath(p); if (pa != null) { var s = Evo((string)AHJson.A(pa, "swap")[1]); if (s != null) { s.lvl = 30; o.Add(s); } }
        o.AddRange(Book(p.cls.id));
        return o;
    }
    static List<string> DefaultLoad(AHPlayer p)
    {
        var ids = new List<string>(); foreach (var s in p.cls.spells) ids.Add(s.id);
        var pa = CurPath(p); if (pa != null) { var sw = AHJson.A(pa, "swap"); ids[(int)(double)sw[0]] = (string)sw[1]; }
        return ids;
    }
    // web buildSpells: five from the load (unlocked), the ultimate sixth
    public static SpellDef[] Ring(AHPlayer p)
    {
        var pool = Pool(p); var cp = CP(p);
        Func<string, SpellDef> byId = id => pool.Find(q => q.id == id);
        var list = new List<SpellDef>();
        foreach (var id in (cp.load.Count > 0 ? cp.load : DefaultLoad(p))) { var q = byId(id); if (q != null && q.lvl <= p.level && !list.Contains(q)) list.Add(q); }
        foreach (var id in DefaultLoad(p)) { if (list.Count >= 5) break; var q = byId(id); if (q != null && !list.Contains(q)) list.Add(q); }
        if (list.Count > 5) list.RemoveRange(5, list.Count - 5);
        var f = CurForm(p); if (f != null) { var u = Evo(AHJson.S(f, "ult")); if (u != null) { u.lvl = 60; list.Add(u); } }
        if (p.cls.mana) foreach (var s in list) s.cost = Mathf.Round(10f + s.cd * 1.6f);
        return list.ToArray();
    }
    public static void Equip(AHGame g, string id, int slot)
    {
        var p = g.player; var cp = CP(p);
        var load = new List<string>(); for (int i = 0; i < 5 && i < p.Spells.Length; i++) load.Add(p.Spells[i].id);
        int from = load.IndexOf(id);
        if (from >= 0) { string t = load[slot]; load[slot] = load[from]; load[from] = t; } else load[slot] = id;
        cp.load = load; p.RebuildRing(); g.ui.RefreshClass(); g.MarkDirty();
    }

    // web spellCd
    public static float Cd(AHPlayer p, SpellDef sp)
    {
        float cd = sp.cd;
        if (sp.id == "nova") cd -= Pass(p, "novaCd");
        if (sp.id == "divine") cd -= Pass(p, "divineCd");
        if (sp.id == "charge") cd -= Pass(p, "chargeCd");
        return Mathf.Max(1f, cd * (1f - Mathf.Min(0.4f, Pass(p, "cdr") + p.stat.cdr)));
    }
    // the evolved hero glows faintly in its path's colour (brighter after the final form)
    public static void Aura(AHPlayer p)
    {
        var t = p.transform.Find("EvoAura"); var pa = CurPath(p);
        if (pa == null) { if (t != null) UnityEngine.Object.Destroy(t.gameObject); return; }
        if (t == null) { t = new GameObject("EvoAura").transform; t.SetParent(p.transform, false); t.localPosition = new Vector3(0, 1.3f, 0); var l0 = t.gameObject.AddComponent<Light>(); l0.type = LightType.Point; l0.shadows = LightShadows.None; }
        var li = t.GetComponent<Light>(); li.color = PathColor(p); li.range = CurForm(p) != null ? 5f : 3.5f; li.intensity = CurForm(p) != null ? 1.6f : 1f;
    }

    // leech: heal for part of the damage you deal
    public static void Leech(AHPlayer p, int dmg)
    {
        float l = Pass(p, "leech"); if (l <= 0f || p.dead) return;
        p.hp = Mathf.Min(p.maxHp, p.hp + dmg * l);
    }
    // basic bolts sometimes jump to a second enemy (Stormcaller, Tempest)
    public static void Arc(AHPlayer p, AHMob from, int dmg)
    {
        float a = Pass(p, "arc"); if (a <= 0f || UnityEngine.Random.value >= a) return;
        AHMob best = null; float bd = 8f;
        foreach (var m in AHGame.I.mobs) if (m != null && m != from && !m.dead) { float d = (m.transform.position - from.transform.position).magnitude; if (d < bd) { bd = d; best = m; } }
        if (best == null) return;
        AHFx.Shoot(from.transform.position + Vector3.up, best, new Color(0.75f, 0.9f, 1f), 0.15f, 30f, m => m.Hurt(Mathf.Max(1, Mathf.RoundToInt(dmg * 0.7f)), p));
    }
    // kills heal (Soul Reaper) and explode (Inferno Lord)
    public static void OnKill(AHGame g, AHMob m)
    {
        var p = g.player; float kh = Pass(p, "killHeal");
        if (kh > 0f && !p.dead) p.Heal(p.maxHp * kh);
        if (Pass(p, "explode") > 0f && !m.add)
        {
            Vector3 at = m.transform.position;
            AHFx.Ring(at, 0.3f, 3f, new Color(1f, 0.5f, 0.15f), 0.4f);
            foreach (var q in new List<AHMob>(g.mobs)) if (q != m && !q.dead && (q.transform.position - at).magnitude < 3f + q.type.radius) { q.Hurt(Mathf.Max(1, Mathf.RoundToInt(p.Power * 0.8f)), p, true); q.AddStatus(AHStatus.Burn, 3f, p.Power * 0.3f, p); }
        }
    }

    // extra damage for one spell (evolution spells, Fireball, Holy Fire)
    public static float SpellK(AHPlayer p, SpellDef sp)
    {
        float k = 1f;
        if (IsEvo(sp.id)) k += Pass(p, "evo");
        if (sp.id == "fireball") k += Pass(p, "fireball");
        if (sp.id == "holyfire") k += Pass(p, "holyfire");
        return k;
    }
}

public partial class AHUI
{
    string bookSel; string classTab = "evo";
    public void OpenClassWin(string tab = "evo") { classTab = tab; bookSel = null; wkMode = "class"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderClass(AHPlayer p)
    {
        var cp = AHEvo.CP(p); var rows = new List<Action<int>>();
        int free = AHEvo.Points(p) - AHEvo.Spent(p);
        wkTitle.text = AHEvo.Title(p) + " · level " + p.level;
        rows.Add(s => Row(s, "Evolution · Talents (" + free + " free) · Spellbook", new Color(1f, 0.8f, 0.45f), "Switch between the three pages.", "",
            new WkBtn { label = "Evolution", on = true, col = classTab == "evo" ? Go : Plain, act = () => OpenClassWin("evo") },
            new WkBtn { label = "Talents", on = true, col = classTab == "tal" ? Go : Plain, act = () => OpenClassWin("tal") },
            new WkBtn { label = "Spells", on = true, col = classTab == "book" ? Go : Plain, act = () => OpenClassWin("book") }));
        if (classTab == "evo")
        {
            wkHint.text = "At level 30 choose a path, at level 60 one of its two forms. Evolution choices are permanent.";
            var paths = AHEvo.Paths(p.cls.id);
            if (paths != null)
                foreach (var pa in paths)
                {
                    var paa = pa; string pid = AHJson.S(pa, "id"); bool mine = cp.path == pid, other = cp.path != null && !mine;
                    var sw = AHJson.A(pa, "swap"); var ns = AHEvo.Evo((string)sw[1]); string bs = p.cls.spells[(int)(double)sw[0]].name;
                    rows.Add(s => Row(s, "Level 30 path: " + AHJson.S(paa, "name") + (mine ? "  <color=#9be37a>yours</color>" : ""), other ? new Color(0.55f, 0.5f, 0.45f) : AHGame.Hex((int)AHJson.N(paa, "color")),
                        AHJson.S(paa, "blurb") + " New spell: " + (ns != null ? ns.name : "") + " replaces " + bs + ".", AHEvo.PassText(AHJson.O(paa, "pass")),
                        mine || other ? null : new WkBtn { label = p.level >= 30 ? "Choose" : "Level 30", on = p.level >= 30, col = Go, act = () => { AHEvo.Evolve(g, 0, pid); RenderWork(); } }));
                    var forms = AHJson.A(pa, "forms");
                    if (forms != null && !other)
                        foreach (var f in forms)
                        {
                            var ff = f; string fid = AHJson.S(f, "id"); bool fm = cp.form == fid, fo = cp.form != null && !fm;
                            var ult = AHEvo.Evo(AHJson.S(f, "ult"));
                            rows.Add(s => Row(s, "   Level 60 form: " + AHJson.S(ff, "name") + (fm ? "  <color=#9be37a>yours</color>" : ""), fo ? new Color(0.55f, 0.5f, 0.45f) : new Color(1f, 0.85f, 0.6f),
                                AHJson.S(ff, "blurb") + " Ultimate: " + (ult != null ? ult.name : "") + ".", AHEvo.PassText(AHJson.O(ff, "pass")),
                                fm || fo || !mine ? null : new WkBtn { label = p.level >= 60 ? "Choose" : "Level 60", on = p.level >= 60, col = Go, act = () => { AHEvo.Evolve(g, 1, fid); RenderWork(); } }));
                        }
                }
        }
        else if (classTab == "tal")
        {
            wkHint.text = "A talent point every 3 levels: " + AHEvo.Points(p) + " earned, " + free + " free. Tier II opens with a path, tier III with a form.";
            foreach (var t in AHEvo.Tiers(p))
            {
                var tt = t;
                if (!t.open) { rows.Add(s => Row(s, tt.name, new Color(0.55f, 0.5f, 0.45f), tt.req, "")); continue; }
                foreach (var n in t.nodes)
                {
                    var nn = n; string id = AHJson.S(n, "id"); int r = AHEvo.Rank(p, id), max = (int)AHJson.N(n, "max", 3);
                    rows.Add(s => Row(s, tt.name + " · " + AHJson.S(nn, "name") + " " + r + "/" + max, r > 0 ? new Color(1f, 0.8f, 0.45f) : Color.white,
                        AHEvo.PassText(AHJson.O(nn, "per"), Mathf.Max(1, r)) + (r == 0 ? " per rank" : ""), "",
                        new WkBtn { label = "+1", on = free > 0 && r < max, col = Go, act = () => { AHEvo.Learn(g, nn); RenderWork(); } }));
                }
            }
            rows.Add(s => Row(s, "Reset talents", new Color(1f, 1f, 1f, 0.7f), "Take back every point, free.", "", new WkBtn { label = "Reset", on = AHEvo.Spent(p) > 0, col = Plain, act = () => { AHEvo.ResetTalents(g); RenderWork(); } }));
        }
        else
        {
            var ring = p.Spells;
            var names = new List<string>(); for (int i = 0; i < ring.Length; i++) names.Add((i < 5 ? (i + 1).ToString() : "★") + " " + ring[i].name);
            wkHint.text = "Ring: " + string.Join(" · ", names.ToArray()) + ". A new spell unlocks every 5 levels.";
            if (bookSel != null)
            {
                var sel = AHEvo.Pool(p).Find(q => q.id == bookSel);
                for (int i = 0; i < 5 && i < ring.Length; i++)
                {
                    int ii = i;
                    rows.Add(s => { rowIcon = AHSpellLook.Icon(ring[ii]); Row(s, "Slot " + (ii + 1) + ": " + ring[ii].name, Color.white, "Put " + (sel != null ? sel.name : bookSel) + " here.", "", new WkBtn { label = "Put here", on = true, col = Go, act = () => { AHEvo.Equip(g, bookSel, ii); bookSel = null; RenderWork(); } }); });
                }
                rows.Add(s => Row(s, "Cancel", new Color(1f, 1f, 1f, 0.7f), "", "", new WkBtn { label = "Cancel", on = true, col = Plain, act = () => { bookSel = null; RenderWork(); } }));
            }
            else
            {
                var pool = AHEvo.Pool(p); pool.Sort((a, b) => a.lvl.CompareTo(b.lvl));
                foreach (var sp in pool)
                {
                    var ss = sp; bool un = sp.lvl <= p.level, on = Array.IndexOf(ring, sp) >= 0 && Array.IndexOf(ring, sp) < 5;
                    rows.Add(s => { rowIcon = AHSpellLook.Icon(ss); rowIconDim = !un; Row(s, ss.name + "  <color=#c8b68a>Lv " + ss.lvl + "</color>", un ? ss.color : new Color(0.55f, 0.5f, 0.45f),
                        Mathf.RoundToInt(AHEvo.Cd(p, ss)) + "s" + (p.cls.mana ? " · " + Mathf.Round(10f + ss.cd * 1.6f) + " mana" : "") + " · " + (ss.desc ?? ""), "",
                        !un ? new WkBtn { label = "Level " + ss.lvl, on = false, col = Plain } : on ? new WkBtn { label = "On ring", on = false, col = Plain } : new WkBtn { label = "Equip", on = true, col = Go, act = () => { bookSel = ss.id; wkPageI = 0; RenderWork(); } }); });
                }
                if (ring.Length > 5) rows.Add(s => { rowIcon = AHSpellLook.Icon(ring[5]); Row(s, "★ " + ring[5].name + " (ultimate)", ring[5].color, ring[5].desc ?? "", "Always on"); });
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
