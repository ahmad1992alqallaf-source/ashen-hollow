// Ashen Hollow: your apprentice. From level 8 a youngster of the town asks to learn from you: a squire for an
// Adventurer, a journeyman's helper for an Artisan. They learn from what you do (fights and dungeons for a squire,
// gathering, crafting and orders for a helper) and grow from level 1 to 10. Every day they bring you a small gift (better
// as they grow), and another each time they go up a level. At level 10 they are ready to go their own way: a farewell
// gift, and room for a new apprentice. Gifts are things you could find yourself; no power can be bought here.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHApprenticeState { public string name = ""; public long xp; public int lv; public string giftDay = ""; public int grads; public bool active; }

public static class AHApprentice
{
    public const int MaxLv = 10, OpenLv = 8;
    static readonly string[] Names = { "Wren", "Pip", "Tamsin", "Ollie", "Juniper", "Bram", "Nell", "Fitch" };
    public static AHApprenticeState S(AHPlayer p) { if (p.prog.appr == null) p.prog.appr = new AHApprenticeState(); return p.prog.appr; }
    public static bool Artisan(AHPlayer p) { return p.path == "artisan"; }
    public static string Kind(AHPlayer p) { return Artisan(p) ? "helper" : "squire"; }
    public static bool Open(AHPlayer p) { return p.level >= OpenLv || p.BestMastery() >= 3; }
    public static long Need(int lv) { return (long)Mathf.Round(60f * Mathf.Pow(lv, 1.5f)); }   // XP from lv to lv+1
    static string Today { get { return DateTime.UtcNow.ToString("yyyy-MM-dd"); } }
    public static bool GiftReady(AHPlayer p) { var s = S(p); return s.active && s.giftDay != Today; }

    // the one who asks (the same name until they go their own way)
    public static string Candidate(AHPlayer p) { var s = S(p); return Names[(s.grads * 3 + (p.heroName ?? "").Length) % Names.Length]; }

    public static void Take(AHGame g)
    {
        var p = g.player; var s = S(p); if (s.active || !Open(p)) return;
        s.active = true; s.name = Candidate(p); s.xp = 0; s.lv = 1; s.giftDay = "";
        g.ui.Banner(s.name + " is your " + Kind(p), Artisan(p) ? "They watch you gather and craft, and learn." : "They watch you fight, and learn.");
        g.MarkDirty();
    }

    // everything the quest log hears (AHQuests.Event)
    public static void Event(AHGame g, string t, string id, int n)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        var s = S(p); if (!s.active || s.lv >= MaxLv) return;
        int x = 0;
        if (Artisan(p)) x = t == "gather" ? 2 : t == "craft" ? 4 : t == "order" ? 20 : 0;
        else x = t == "kill" ? 2 : t == "dclear" ? 60 : 0;
        if (x <= 0) return;
        s.xp += x * n;
        while (s.lv < MaxLv && s.xp >= Need(s.lv))
        {
            s.xp -= Need(s.lv); s.lv++;
            if (s.lv >= MaxLv) { g.ui.Banner(s.name + " is ready!", "Your " + Kind(p) + " has learned all you can teach. Say goodbye in Menu → Apprentice."); s.xp = 0; }
            else { string gift = Gift(g, p, s.lv); g.ui.Banner(s.name + " is now level " + s.lv, "“I made you something!” " + gift); }
        }
        g.MarkDirty();
    }

    static readonly string[][] Pools =
    {
        new[] { "bread", "hp_potion", "logs", "copper_ore", "raw_trout", "sunpetal" },
        new[] { "big_potion", "mana_potion", "iron_ore", "pine_logs", "dye_ember", "enh_stone" },
        new[] { "mystery_sack", "sapphire", "ruby", "emerald", "enh_stone", "elixir_might" },
    };
    static string Gift(AHGame g, AHPlayer p, int lv)
    {
        var pool = Pools[lv <= 3 ? 0 : lv <= 6 ? 1 : 2];
        for (int t = 0; t < 8; t++)
        {
            string id = pool[UnityEngine.Random.Range(0, pool.Length)]; var it = AHItems.Get(id); if (it == null) continue;
            int n = lv <= 3 && !it.potion ? 2 + UnityEngine.Random.Range(0, 3) : 1;
            if (!p.bag.Add(id, n) && AHAuction.S != null) AHAuction.MailAdd(id, n);
            p.bag.Touch();
            return n + "× " + it.name;
        }
        return "";
    }

    static readonly string[] Lines =
    {
        "“I found this on the road. I think it's for you.”", "“I practised all night. Look!”", "“The baker says you're the best in town. I told her I know.”",
        "“Can we go somewhere dangerous tomorrow? Not too dangerous.”", "“I kept the best one for you.”", "“I'm going to be just like you. But taller.”",
    };
    public static void ClaimGift(AHGame g)
    {
        var p = g.player; var s = S(p); if (!GiftReady(p)) return;
        s.giftDay = Today;
        string got = Gift(g, p, s.lv);
        g.ui.Banner(s.name + "'s gift", Lines[UnityEngine.Random.Range(0, Lines.Length)] + " " + got);
        g.MarkDirty();
    }

    public static void Graduate(AHGame g)
    {
        var p = g.player; var s = S(p); if (!s.active || s.lv < MaxLv) return;
        long coin = 2000L * AHDB.CU; p.AddMoney(coin, p.transform.position);
        string gift = Gift(g, p, MaxLv);
        g.ui.Banner("Farewell, " + s.name, "“I won't forget you.” " + AHItems.MoneyText(coin) + " and " + gift + ". Another youngster may ask to learn from you soon.");
        s.grads++; s.active = false; s.name = ""; s.lv = 0; s.xp = 0;
        g.MarkDirty();
    }

    public static string Status(AHPlayer p)
    {
        var s = S(p);
        if (!s.active) return Open(p) ? Candidate(p) + " wants to learn from you" + (s.grads > 0 ? " · " + s.grads + " taught so far" : "") : "From level " + OpenLv;
        return s.name + " · level " + s.lv + (s.lv < MaxLv ? " · " + s.xp + " / " + Need(s.lv) : " · ready to go their own way") + (GiftReady(p) ? " · a gift for you" : "");
    }
}

public partial class AHUI
{
    public void OpenApprentice() { wkMode = "appr"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderApprentice(AHPlayer p)
    {
        var s = AHApprentice.S(p); var rows = new List<Action<int>>();
        wkTitle.text = s.active ? "Your " + AHApprentice.Kind(p) + ": " + s.name : "Apprentice";
        wkHint.text = AHApprentice.Artisan(p) ? "A helper learns as you gather, craft and fill orders; a gift every day, and another at each new level."
                                             : "A squire learns as you fight and clear dungeons; a gift every day, and another at each new level.";
        if (!s.active)
            rows.Add(sl => Row(sl, AHApprentice.Candidate(p) + " asks to learn from you", new Color(1f, 0.85f, 0.55f), AHApprentice.Status(p), "“I'll carry your things. I'll be quiet. Mostly.”",
                new WkBtn { label = "Take them on", on = AHApprentice.Open(p), col = Go, act = () => { AHApprentice.Take(g); RenderWork(); } }));
        else
        {
            rows.Add(sl => Row(sl, s.name + " · level " + s.lv + " / " + AHApprentice.MaxLv, new Color(1f, 0.85f, 0.55f),
                s.lv < AHApprentice.MaxLv ? s.xp + " / " + AHApprentice.Need(s.lv) + " to the next level" : "Ready to go their own way", AHApprentice.Artisan(p) ? "Learns from gathering, crafting and orders" : "Learns from fights and dungeons"));
            rows.Add(sl => Row(sl, "Today's gift", Color.white, AHApprentice.GiftReady(p) ? s.name + " has something for you" : "Come back tomorrow", "Better gifts as they grow",
                new WkBtn { label = "Take it", on = AHApprentice.GiftReady(p), col = Go, act = () => { AHApprentice.ClaimGift(g); RenderWork(); } }));
            if (s.lv >= AHApprentice.MaxLv)
                rows.Add(sl => Row(sl, "Say goodbye", new Color(0.62f, 0.9f, 0.55f), "A farewell gift, and room for a new apprentice", "",
                    new WkBtn { label = "Farewell", on = true, col = Go, act = () => { AHApprentice.Graduate(g); RenderWork(); } }));
        }
        if (s.grads > 0) rows.Add(sl => Row(sl, "Apprentices taught: " + s.grads, new Color(1f, 1f, 1f, 0.7f), "", ""));
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
