// Ashen Hollow: the hero's extra power, as in the web game (v66):
//  - monster cards (CARD_SETS / addCard / cardDrop): monsters sometimes drop their card; add it to your collection for
//    +1% damage against that monster, and a lasting bonus for every finished set;
//  - gem sockets (GEM_FX / SOCKETS / socketGem): rubies, sapphires, emeralds, diamonds and void shards in your gear slots
//    (the gems stay in the slot when you change gear); taking one out costs 50 copper;
//  - monster licenses (LIC_TIERS): 25, 100, 300 and 1,000 kills of one kind give +3%, +6%, +10% and +15% damage against it;
//  - character stats (STATS5): a point every class level for STR, END, DEX, INT or SPR.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHPower
{
    // ---------- cards ----------
    public static List<object> Sets { get { return AHDB.List("monsters", "CARD_SETS"); } }
    public static string SetOf(string type) { return AHJson.S(AHDB.Table("monsters", "CARD_OF"), type, null); }
    static bool SetDone(AHPlayer p, object S0)
    {
        var mobs = AHJson.A(S0, "mobs"); if (mobs == null) return false;
        foreach (var t in mobs) if (AHJson.O(AHDB.Mobs, (string)t) != null && !p.prog.cards.Contains((string)t)) return false;
        return true;
    }
    public static bool AddCard(AHGame g, string id)
    {
        var p = g.player; string t = AHJson.S(AHItems.Raw(id), "card", null);
        if (t == null || p.prog.cards.Contains(t) || !p.bag.Take(id)) return false;
        p.prog.cards.Add(t);
        object S0 = null; var ss = Sets; if (ss != null) foreach (var s in ss) { var mobs = AHJson.A(s, "mobs"); if (mobs != null && mobs.Contains(t)) S0 = s; }
        if (S0 != null && SetDone(p, S0)) g.ui.Banner(AHJson.S(S0, "name") + " set complete!", AHJson.S(S0, "txt"));
        else g.ui.Toast(AHItems.Get(id).name + " added to your collection: +1% damage against " + AHJson.S(AHJson.O(AHDB.Mobs, t), "name", t) + ".", 4f);
        p.Recalc(); g.SaveProgress();
        return true;
    }

    // ---------- gems ----------
    public static object GemFx { get { return AHDB.Table("items", "GEM_FX"); } }
    public static int Sockets(string slot) { return (int)AHJson.N(AHDB.Table("items", "SOCKETS"), slot, 0); }
    public static string GemIn(AHPlayer p, string slot, int i) { string k = slot + ":" + i; foreach (var e in p.prog.gems) if (e.StartsWith(k + ":")) return e.Substring(k.Length + 1); return null; }
    public static string GemText(string gem) { var r = AHJson.A(GemFx, gem); return r != null && r.Count > 1 ? (string)r[1] : ""; }
    public static void Socket(AHGame g, string slot, int i, string gem)
    {
        var p = g.player;
        if (i >= Sockets(slot) || GemIn(p, slot, i) != null || AHJson.A(GemFx, gem) == null || !p.bag.Take(gem)) return;
        p.prog.gems.Add(slot + ":" + i + ":" + gem);
        g.ui.Toast(AHItems.Get(gem).name + " set into your " + AHItems.SlotName(slot).ToLowerInvariant() + " socket: " + GemText(gem) + ".", 3f);
        p.Recalc(); g.SaveProgress();
    }
    public const long GemOut = 50;
    public static void Unsocket(AHGame g, string slot, int i)
    {
        var p = g.player; string gem = GemIn(p, slot, i); if (gem == null) return;
        long c = GemOut * AHDB.CU;
        if (p.bag.money < c) { g.ui.Toast("Taking a gem out costs " + AHItems.MoneyText(c) + "."); return; }
        if (!p.bag.Add(gem)) { g.ui.Toast("Your bag is full."); return; }
        p.bag.money -= c; p.prog.gems.Remove(slot + ":" + i + ":" + gem);
        p.Recalc(); g.SaveProgress();
    }

    // ---------- licenses ----------
    static List<object> Lic { get { return AHDB.List("items", "LIC_TIERS"); } }
    public static int LicTier(AHPlayer p, string type)
    {
        long k = AHProgress.Get(p.prog.kills, type); int t = 0; var L = Lic;
        if (L != null) while (t < L.Count && k >= (double)((List<object>)L[t])[0]) t++;
        return t;
    }
    public static string LicName(string type, int t)
    {
        var L = Lic; string n = AHJson.S(AHJson.O(AHDB.Mobs, type), "name", type).Split(',')[0];
        if (n.StartsWith("The ")) n = n.Substring(4); else if (n.StartsWith("A ")) n = n.Substring(2);
        return n + " " + (string)((List<object>)L[t - 1])[1];
    }
    // damage against one kind: its card (+1%) and its license
    public static float VsBonus(AHPlayer p, string type)
    {
        float b = p.prog.cards.Contains(type) ? 0.01f : 0f; int t = LicTier(p, type);
        if (t > 0) b += (float)(double)((List<object>)Lic[t - 1])[2];
        return b;
    }
    public static void OnKill(AHGame g, AHMob m)
    {
        var p = g.player; var t = m.type;
        // a new license? (the kill count was just raised by AHRep.OnKill)
        long k = AHProgress.Get(p.prog.kills, t.id); var L = Lic;
        if (L != null) for (int i = 0; i < L.Count; i++) if (k == (long)(double)((List<object>)L[i])[0])
                {
                    g.ui.Banner("License: " + LicName(t.id, i + 1), "+" + Mathf.RoundToInt((float)(double)((List<object>)L[i])[2] * 100) + "% damage against them");
                    g.ui.Toast("You earned the " + LicName(t.id, i + 1) + " license. See it in Menu → Collection.", 4f);
                }
        // web cardDrop
        if (m.add || SetOf(t.id) == null) return;
        bool boss = t.id == AHDungeon.BossOf(AHGame.AreaId) || t.id == "grull" || t.id == AHRaid.Boss || AHEvents.IsWorldBoss(t.id);
        float ch = boss ? 0.2f : (t.elite || t.rare || AHEvents.IsEvent(t.id)) ? 0.05f : 0.006f;
        if (UnityEngine.Random.value < ch && p.bag.Add("card_" + t.id))
            g.ui.Banner(AHItems.Get("card_" + t.id).name + "!", p.prog.cards.Contains(t.id) ? "A spare for trading" : "Add it to your collection (tap it twice in the bag)");
    }

    // ---------- STR, END, DEX, INT, SPR ----------
    public static readonly string[] StatK = { "str", "end", "dex", "int", "spr" };
    public static readonly string[] StatName = { "Strength", "Endurance", "Dexterity", "Intelligence", "Spirit" };
    public static readonly string[] StatTxt = { "Physical damage +0.3% (warriors, rogues, rangers)", "Max HP +0.6% and +0.4 armor", "Evasion +0.1% and attack speed +0.15%", "Magic damage +0.3% and healing +0.3% (mages, priests, druids)", "Critical chance +0.12% and max mana +1%" };
    static readonly string[] Magic = { "mage", "priest", "druid", "shaman" };
    public static bool IsMagic(AHPlayer p) { return Array.IndexOf(Magic, p.cls.id) >= 0; }
    public static int Pts(AHPlayer p, string k) { return (int)AHProgress.Get(p.prog.sp, p.cls.id + ":" + k); }
    public static int Spent(AHPlayer p) { int n = 0; foreach (var k in StatK) n += Pts(p, k); return n; }
    // stat points were folded into talents (AHEvo): none are given any more; old points stay saved but do nothing
    public static int Free(AHPlayer p) { return 0; }
    public static void Spend(AHGame g, string k)
    {
        var p = g.player; if (Free(p) <= 0) return;
        AHProgress.Add(p.prog.sp, p.cls.id + ":" + k, 1); p.Recalc(); g.MarkDirty();
    }
    public static void ResetPts(AHGame g)
    {
        var p = g.player; p.prog.sp.RemoveAll(e => e.k.StartsWith(p.cls.id + ":")); p.Recalc(); g.MarkDirty();
    }

    // all of it into the stats (web recalcStats: cardFx, gemFx, statFx)
    public static void Apply(AHPlayer p, AHStats s)
    {
        if (p.prog == null) return;
        Action<object> add = fx =>
        {
            var d = fx as Dictionary<string, object>; if (d == null) return;
            foreach (var kv in d)
            {
                double v = kv.Value is double ? (double)kv.Value : 0;
                switch (kv.Key)
                {
                    case "atk": s.atk += (int)v; break; case "def": s.def += (int)v; break; case "hp": s.hp += (int)v; break;
                    case "dmg": s.dmg += (float)v; break; case "crit": s.crit += (float)v; break; case "cdr": s.cdr += (float)v; break;
                    case "evade": s.evade += (float)v; break; case "manaK": s.manaK += (float)v; break; case "heal": s.heal += (float)v; break;
                }
            }
        };
        var ss = Sets; if (ss != null) foreach (var S0 in ss) if (SetDone(p, S0)) add(AHJson.O(S0, "fx"));
        foreach (var e in p.prog.gems) { var parts = e.Split(':'); if (parts.Length == 3 && p.bag.Worn(parts[0]) != null) { var r = AHJson.A(GemFx, parts[2]); if (r != null) add(r[0]); } }
        if (p.cls == null) return;
    }
}

public partial class AHUI
{
    public void OpenCollection() { wkMode = "coll"; wkPageI = 0; ShowWork(true); RenderWork(); }
    public void OpenGems() { wkMode = "gems"; wkPageI = 0; ShowWork(true); RenderWork(); }
    public void OpenStats() { wkMode = "stats"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderCollection(AHPlayer p)
    {
        int total = 0, have = 0; var rows = new List<Action<int>>();
        var ss = AHPower.Sets;
        if (ss != null)
            foreach (var S0 in ss)
            {
                var list = new List<string>(); var mobs = AHJson.A(S0, "mobs"); if (mobs != null) foreach (var t in mobs) if (AHJson.O(AHDB.Mobs, (string)t) != null) list.Add((string)t);
                int got = list.FindAll(t => p.prog.cards.Contains(t)).Count; total += list.Count; have += got;
                var names = list.ConvertAll(t => { string n = AHJson.S(AHJson.O(AHDB.Mobs, t), "name", t).Split(',')[0]; return p.prog.cards.Contains(t) ? "<color=#9be37a>" + n + "</color>" : "<color=#8a8078>" + n + "</color>"; });
                rows.Add(s => Row(s, AHJson.S(S0, "name") + " · " + got + "/" + list.Count + (got == list.Count ? " ✓" : ""), got == list.Count ? new Color(0.6f, 0.9f, 0.48f) : new Color(1f, 0.8f, 0.45f), "Set bonus: " + AHJson.S(S0, "txt"), string.Join(" · ", names.ToArray())));
            }
        // licenses held
        var lic = new List<string>();
        foreach (var e in p.prog.kills) { int t = AHPower.LicTier(p, e.k); if (t > 0) lic.Add(AHPower.LicName(e.k, t)); }
        rows.Insert(0, s => Row(s, "Monster licenses · " + lic.Count, new Color(0.56f, 0.85f, 1f), lic.Count > 0 ? string.Join(" · ", lic.ToArray()) : "Defeat 25 of one kind for its first license (+3% damage against them).", "Then 100 (+6%), 300 (+10%) and 1,000 (+15%)."));
        wkTitle.text = "Collection · cards " + have + " / " + total;
        wkHint.text = "Monsters sometimes drop their card (bosses and elites more often). Tap a card twice in your bag to add it: +1% damage against that monster, and a lasting bonus for each finished set.";
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void RenderGems(AHPlayer p)
    {
        wkTitle.text = "Gem sockets";
        wkHint.text = "Gems stay in the slot when you change gear. Ruby +4 attack · sapphire +6 armor · emerald +30 HP · diamond +2% crit · void shard +4% damage. Taking a gem out costs " + AHItems.MoneyText(AHPower.GemOut * AHDB.CU) + ".";
        var rows = new List<Action<int>>();
        var gems = new List<string>(); var gf = AHPower.GemFx as Dictionary<string, object>; if (gf != null) foreach (var kv in gf) if (p.bag.Count(kv.Key) > 0) gems.Add(kv.Key);
        foreach (var sl in AHItems.GearSlots)
        {
            int n = AHPower.Sockets(sl); if (n == 0) continue;
            for (int i = 0; i < n; i++)
            {
                string slot = sl; int ii = i; string gem = AHPower.GemIn(p, sl, i);
                string worn = p.bag.Worn(sl); var wi = AHItems.Get(worn);
                if (gem != null)
                    rows.Add(s => Row(s, AHItems.SlotName(slot) + " · socket " + (ii + 1) + ": " + AHItems.Get(gem).name, new Color(0.61f, 0.89f, 1f), AHPower.GemText(gem) + (worn == null ? " · <color=#ff8a7a>no gear worn here: inactive</color>" : ""), wi != null ? wi.name : "",
                        new WkBtn { label = "Take out", on = true, col = Plain, act = () => { AHPower.Unsocket(g, slot, ii); RenderWork(); } }));
                else
                {
                    var btns = new List<WkBtn>();
                    foreach (var gm in gems) { string gg = gm; btns.Add(new WkBtn { label = AHItems.Get(gg).name, on = true, col = Go, act = () => { AHPower.Socket(g, slot, ii, gg); RenderWork(); } }); }
                    if (btns.Count > 3) btns = btns.GetRange(0, 3);
                    rows.Add(s => Row(s, AHItems.SlotName(slot) + " · socket " + (ii + 1) + ": empty", new Color(1f, 1f, 1f, 0.75f), gems.Count > 0 ? "Pick a gem to set" : "Mine or buy gems (ruby, sapphire, emerald, diamond) to fill it", wi != null ? wi.name : "", btns.ToArray()));
                }
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    // the Stats page now only shows what your gear and talents add up to, against the caps
    // your hero standing on the left (in all its gear, turning a little), your stats on the right
    void RenderStats(AHPlayer p)
    {
        var st = p.stat; int fr = AHEvo.Points(p) - AHEvo.Spent(p);
        SetPager(false); wkPage.text = "";
        wkTitle.text = (p.heroName ?? "Hero") + " · " + AHEvo.Title(p) + " · level " + p.level;
        wkHint.text = "Stats come from your gear, gems, sets and talents. Each has a cap so no build runs away.";
        // the hero
        var stand = Img("Stand", wkList, panel9 != null ? panel9 : white, new Vector2(0f, 1f), Vector2.zero, new Vector2(250, 420), new Color(0.16f, 0.12f, 0.09f, 1f));
        stand.pivot = new Vector2(0f, 1f); stand.anchoredPosition = new Vector2(0f, -4f); wkItems.Add(stand.gameObject);
        if (panel9 != null) stand.GetComponent<Image>().type = Image.Type.Sliced;
        if (glow != null) Img("Glow", stand, glow, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(260, 300), new Color(AHEvo.PathColor(p).r, AHEvo.PathColor(p).g, AHEvo.PathColor(p).b, 0.35f));
        var view = Box("Hero", stand, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(240, 336));
        var raw = view.gameObject.AddComponent<RawImage>(); raw.raycastTarget = false; raw.texture = AHItemStudio.Hero(p);
        var nm = Label(stand, "Name", p.heroName ?? "Hero", 20, TextAnchor.UpperCenter, new Vector2(0, -346), new Vector2(250, 26), Gold); nm.fontStyle = FontStyle.Bold;
        Label(stand, "Class", p.cls.name + " · level " + p.level, 15, TextAnchor.UpperCenter, new Vector2(0, -372), new Vector2(250, 20), new Color(1f, 1f, 1f, 0.75f));
        // the numbers: two columns of cards, each with a bar to its cap
        var rows = new List<object[]>
        {
            new object[] { "Health", p.maxHp, 0f, false }, new object[] { "Mana", p.maxMana, 0f, false },
            new object[] { "Damage bonus", st.dmg, AHPlayer.Cap.dmg, true }, new object[] { "Critical chance", st.crit, AHPlayer.Cap.crit, true },
            new object[] { "Evasion", st.evade, AHPlayer.Cap.evade, true }, new object[] { "Attack speed", st.aspd, AHPlayer.Cap.aspd, true },
            new object[] { "Shorter cooldowns", st.cdr, AHPlayer.Cap.cdr, true }, new object[] { "Healing bonus", st.heal, AHPlayer.Cap.heal, true },
            new object[] { "Max HP bonus", st.hpK, AHPlayer.Cap.hpK, true }, new object[] { "Max mana bonus", st.manaK, AHPlayer.Cap.manaK, true },
        };
        float x0 = 262f, cw = 256f, ch = 62f;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; string label = (string)r[0]; float v = (float)r[1], cap = (float)r[2]; bool pc = (bool)r[3];
            if (label == "Mana" && !p.cls.mana) { v = 0f; }
            int col = i % 2, row = i / 2;
            var card = Img("Stat", wkList, white, new Vector2(0f, 1f), Vector2.zero, new Vector2(cw - 8, ch - 8), new Color(0.16f, 0.12f, 0.09f, 1f));
            card.pivot = new Vector2(0f, 1f); card.anchoredPosition = new Vector2(x0 + col * cw, -4f - row * ch); wkItems.Add(card.gameObject);
            bool capped = cap > 0f && v >= cap - 1e-4f;
            Label(card, "N", label, 15, TextAnchor.UpperLeft, new Vector2(10, -6), new Vector2(150, 20), new Color(1f, 1f, 1f, 0.75f));
            string val = pc ? (v * 100f).ToString("0.#") + "%" : (label == "Mana" && !p.cls.mana ? "—" : Mathf.RoundToInt(v).ToString("#,0"));
            var vt = Label(card, "V", val, 20, TextAnchor.UpperRight, new Vector2(cw - 8 - 130, -4), new Vector2(120, 24), capped ? new Color(1f, 0.55f, 0.4f) : Gold); vt.fontStyle = FontStyle.Bold;
            if (cap > 0f)
            {
                var bg = Img("Bar", card, white, new Vector2(0f, 0f), new Vector2((cw - 28) / 2f + 10, 12), new Vector2(cw - 28, 6), new Color(0.3f, 0.24f, 0.18f, 1f));
                var fill = Img("Fill", bg, white, new Vector2(0f, 0.5f), Vector2.zero, new Vector2((cw - 28) * Mathf.Clamp01(v / cap), 6), capped ? new Color(1f, 0.5f, 0.35f) : new Color(1f, 0.78f, 0.35f));
                fill.pivot = new Vector2(0f, 0.5f);
                Label(card, "C", capped ? "capped" : "cap " + (cap * 100f).ToString("0") + "%", 11, TextAnchor.UpperLeft, new Vector2(10, -28), new Vector2(120, 16), new Color(1f, 1f, 1f, 0.45f));
            }
        }
        // talents
        var tb = Img("Talents", wkList, white, new Vector2(0f, 1f), Vector2.zero, new Vector2(cw * 2 - 8, 46), fr > 0 ? Go : Plain);
        tb.pivot = new Vector2(0f, 1f); tb.anchoredPosition = new Vector2(x0, -4f - 5 * ch - 6f); wkItems.Add(tb.gameObject);
        Center(Label(tb, "T", fr > 0 ? "Talents · " + fr + " point" + (fr == 1 ? "" : "s") + " to spend" : "Talents", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(cw * 2 - 8, 36), Color.white));
        taps.Add(new TapBtn { rt = tb, layer = 5, group = "work", act = () => OpenClassWin("tal") });
    }
}
