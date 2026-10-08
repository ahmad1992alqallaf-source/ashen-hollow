// Ashen Hollow: timed events, as in the web game (v66, RARES / WORLD_BOSS / rareWin / bossWin / evTick / renderEvents).
// Seven rare monsters, each out for 25 minutes 8 to 10 times a day (staggered), and nine world bosses, one for every two
// lands, each descending four times a day for 45 minutes (Voidmaw over Kingsvale, Old Tusk in Hollow Meadow, and the
// rest), spread 40 minutes apart. Each can be slain once per appearance; killing them earns Deeds (AHAch).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHEvents
{
    public struct Win { public bool active; public long id; public double left, next; }
    const int RareMin = 25;
    static double NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0; } }

    public static List<object> Rares { get { return AHDB.List("monsters", "RARES"); } }
    public static string BossType { get { return AHJson.S(AHDB.Table("monsters", "WORLD_BOSS"), "type", "voidmaw"); } }
    // every world boss with its land and four daily hours (monsters.json WORLD_BOSSES)
    public static List<object> Bosses { get { var l = AHDB.List("monsters", "WORLD_BOSSES"); if (l == null || l.Count == 0) l = new List<object> { AHDB.Table("monsters", "WORLD_BOSS") }; return l; } }
    public static object BossDef(string type) { foreach (var b in Bosses) if (AHJson.S(b, "type") == type) return b; return null; }
    public static bool IsWorldBoss(string type) { return BossDef(type) != null; }
    public static string MobName(string type) { return AHJson.S(AHJson.O(AHDB.Mobs, type), "name", type); }

    // web rareWin
    public static Win RareWin(object r)
    {
        double per = AHJson.N(r, "per", 9), t = NowS - AHJson.N(r, "off") * 60, cyc = Math.Round(86400.0 / Math.Max(1.0, per)), pos = ((t % cyc) + cyc) % cyc;
        return new Win { active = pos < RareMin * 60, id = (long)Math.Floor(t / cyc), left = RareMin * 60 - pos, next = cyc - pos };
    }
    // web bossWin (local hours)
    public static Win BossWin() { return BossWin(BossDef(BossType) ?? AHDB.Table("monsters", "WORLD_BOSS")); }
    public static Win BossWin(object B)
    {
        var hours = AHJson.A(B, "hours"); double mins = AHJson.N(B, "mins", 45);
        var now = DateTime.Now; double best = double.PositiveInfinity;
        for (int day = -1; day <= 1; day++)
            if (hours != null) foreach (var h in hours)
                {
                    var s = now.Date.AddDays(day).AddHours((double)h); var e = s.AddMinutes(mins);
                    if (now >= s && now < e) return new Win { active = true, id = new DateTimeOffset(s).ToUnixTimeSeconds(), left = (e - now).TotalSeconds };
                    if (s > now) best = Math.Min(best, (s - now).TotalSeconds);
                }
        return new Win { active = false, id = -1, next = best };
    }
    public static string Fmt(double s)
    {
        s = Math.Max(0, Math.Floor(s)); int h = (int)(s / 3600), m = (int)(s / 60) % 60, x = (int)s % 60;
        return h > 0 ? h + "h " + m.ToString("00") + "m" : m + ":" + x.ToString("00");
    }
    static Win WinOf(string type)
    {
        var bd = BossDef(type); if (bd != null) return BossWin(bd);
        var rs = Rares; if (rs != null) foreach (var r in rs) if (AHJson.S(r, "type") == type) return RareWin(r);
        return new Win { id = -2 };
    }
    public static bool IsEvent(string type) { return WinOf(type).id != -2; }
    static string Where(string type)
    {
        var bd = BossDef(type); if (bd != null) return AHJson.S(bd, "where");
        var rs = Rares; if (rs != null) foreach (var r in rs) if (AHJson.S(r, "type") == type) return AHJson.S(r, "where");
        return "";
    }

    // web evTick for every event beast in this area
    static float t; static readonly HashSet<string> introDone = new HashSet<string>();
    public static void Tick(AHGame g, float dt)
    {
        t -= dt; if (t > 0f) return; t = 1f;
        var p = g.player; if (p == null) return;
        foreach (var m in g.mobs)
        {
            if (m == null || m.add) continue;
            var w = WinOf(m.type.id); if (w.id == -2) continue;
            bool killed = AHProgress.Get(p.prog.evKill, m.type.id) == w.id + 1;
            if (w.active && !killed)
            {
                if (m.evHold) { m.evHold = false; }
                // a world boss makes its entrance the first time it turns on you (AHBossIntro)
                if (IsWorldBoss(m.type.id) && m.Chasing && !m.dead && !p.dead)
                {
                    string key = m.type.id + ":" + w.id;
                    if (!introDone.Contains(key)) { introDone.Add(key); AHBossIntro.Play(g, m, m.type.name, "World boss of " + Where(m.type.id)); }
                }
                if (AHProgress.Get(p.prog.evSeen, m.type.id) != w.id + 1)
                {
                    Set(p.prog.evSeen, m.type.id, w.id + 1);
                    bool boss = IsWorldBoss(m.type.id);
                    string msg = m.type.name + (boss ? " descends on " : " has appeared in ") + Where(m.type.id) + "!";
                    g.ui.Banner(boss ? "World boss" : "Rare monster", msg); g.ui.Toast(msg, 4f);
                }
            }
            else
            {
                m.evHold = true;
                if (!m.dead && !m.Chasing) m.EvHide();
            }
        }
        // the warning before a world boss (the next one anywhere)
        foreach (var B in Bosses)
        {
            var bw = BossWin(B);
            if (bw.active || bw.next >= 900) continue;
            long wid = (long)Math.Round(NowS + bw.next);
            if (p.prog.evWarn != wid) { p.prog.evWarn = wid; g.ui.Toast("The sky darkens… " + MobName(AHJson.S(B, "type")) + " descends on " + AHJson.S(B, "where") + " in " + Mathf.CeilToInt((float)(bw.next / 60)) + " minutes.", 5f); }
            break;
        }
        g.ui.RefreshEvChip();
    }
    static void Set(List<AHKV> l, string k, long v) { foreach (var e in l) if (e.k == k) { e.v = v; return; } l.Add(new AHKV { k = k, v = v }); }

    public static void OnKill(AHGame g, AHMob m)
    {
        var w = WinOf(m.type.id); if (w.id == -2 || m.add) return;
        Set(g.player.prog.evKill, m.type.id, w.id + 1);
        m.evHold = true;
        g.quests.Event(IsWorldBoss(m.type.id) ? "wboss" : "rare", m.type.id, 1, g);   // monthly goals and the week's event count these
    }

    public static string ChipText(AHPlayer p, out bool hot)
    {
        hot = false;
        var kw = AHKQ.Window();
        if (kw.active && p.prog.kqDone != kw.id && p.level >= 5) { hot = true; return (kw.mode == "siege" ? "Siege" : "Gold Rush") + " open · " + Fmt(kw.left); }
        // a world boss that is out now: the one in this land first
        string hereB = null, anyB = null; double hereL = 0, anyL = 0;
        foreach (var B in Bosses)
        {
            var bw = BossWin(B); string ty = AHJson.S(B, "type");
            if (!bw.active || AHProgress.Get(p.prog.evKill, ty) == bw.id + 1) continue;
            if (AHJson.S(B, "area") == AHGame.AreaId) { hereB = ty; hereL = bw.left; } else if (anyB == null) { anyB = ty; anyL = bw.left; }
        }
        if (hereB != null) { hot = true; return MobName(hereB).Split(',')[0] + " · " + Fmt(hereL); }
        if (anyB != null) { hot = true; return MobName(anyB).Split(',')[0] + " · " + Fmt(anyL); }
        var rs = Rares; if (rs == null) return "";
        double best = double.PositiveInfinity;
        foreach (var r in rs)
        {
            var w = RareWin(r); string ty = AHJson.S(r, "type");
            if (w.active && AHProgress.Get(p.prog.evKill, ty) != w.id + 1) { hot = true; return AHJson.S(AHJson.O(AHDB.Mobs, ty), "name", ty) + " · " + Fmt(w.left); }
            best = Math.Min(best, w.next);
        }
        return "Next rare " + Fmt(best);
    }
}

public partial class AHUI
{
    RectTransform evChip; Text evChipText;

    void BuildEvHud()
    {
        evChip = Img("EvChip", transform, white, new Vector2(0, 1), new Vector2(112, -270), new Vector2(190, 34), new Color(0.2f, 0.14f, 0.1f, 0.85f));
        evChipText = Center(Label(evChip, "T", "", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(190, 30), new Color(1f, 0.85f, 0.5f)));
        taps.Add(new TapBtn { rt = evChip, act = OpenEvents });
    }
    public void RefreshEvChip()
    {
        var p = g.player; if (evChip == null || p == null) return;
        bool hot; evChipText.text = AHEvents.ChipText(p, out hot);
        evChip.GetComponent<Image>().color = hot ? new Color(0.42f, 0.16f, 0.5f, 0.92f) : new Color(0.2f, 0.14f, 0.1f, 0.85f);
        if (WorkOpen && wkMode == "events") RenderWork();
    }

    public void OpenEvents() { wkMode = "events"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderEvents(AHPlayer p)
    {
        wkTitle.text = "Events";
        wkHint.text = "World boss twice a day (1 pm and 9 pm, 45 minutes). Rare monsters every 2 hours, for 25 minutes. Each can be slain once per appearance.";
        var rows = new List<Action<int>>();
        var bw = AHEvents.BossWin(); string bt = AHEvents.BossType; var bd = AHJson.O(AHDB.Mobs, bt); var B = AHDB.Table("monsters", "WORLD_BOSS");
        bool bslain = bw.active && AHProgress.Get(p.prog.evKill, bt) == bw.id + 1;
        rows.Add(s => Row(s, "World boss: " + AHJson.S(bd, "name", bt), new Color(0.75f, 0.48f, 1f), "Lv " + (int)AHJson.N(bd, "lvl") + " · " + AHJson.S(B, "where"),
            bslain ? "Slain · well done" : bw.active ? "<color=#9be37a>Active now</color> · " + AHEvents.Fmt(bw.left) + " left" : "Next in " + AHEvents.Fmt(bw.next)));
        var rs = AHEvents.Rares;
        if (rs != null)
            foreach (var r in rs)
            {
                var w = AHEvents.RareWin(r); string ty = AHJson.S(r, "type"); var d = AHJson.O(AHDB.Mobs, ty);
                bool slain = w.active && AHProgress.Get(p.prog.evKill, ty) == w.id + 1;
                var drops = new List<string>(); var dl = AHJson.A(d, "drops");
                if (dl != null) foreach (var o in dl) { var row = o as List<object>; if (row == null) continue; string id = (string)row[0]; if (id.StartsWith("mount:")) drops.Add(AHComp.MountName(id.Substring(6)) + " mount (" + Mathf.RoundToInt((float)(double)row[1] * 100) + "%)"); else { var it = AHItems.Get(id); if (it != null && it.cosmetic) drops.Add(it.name + " (" + Mathf.RoundToInt((float)(double)row[1] * 100) + "%)"); } }
                rows.Add(s => Row(s, AHJson.S(d, "name", ty), new Color(1f, 0.85f, 0.42f), "Lv " + (int)AHJson.N(d, "lvl") + " · " + AHJson.S(r, "where") + (drops.Count > 0 ? " · " + string.Join(", ", drops.ToArray()) : ""),
                    slain ? "Slain · back next cycle" : w.active ? "<color=#9be37a>Active now</color> · " + AHEvents.Fmt(w.left) + " left" : "Next in " + AHEvents.Fmt(w.next)));
            }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
