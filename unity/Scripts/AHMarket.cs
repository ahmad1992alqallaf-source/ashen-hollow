// Ashen Hollow: the Merchants' Quarter, as in the web game (v66, MK_CITIES / MK_TRADES / mkFloor / mkCeil / mkRent /
// mkSimMine / openWarden). Talk to the Market Warden of a city: rent a stall in the hall of your own trade (a week at a
// time), stock it with up to eight goods you made or gathered, and set each price between the good's floor and ceiling.
// Townsfolk come by all day, even while you are away, and buy more often the cheaper you sell (bigger cities buy more).
// The takings (less 5% market tax) wait in your till. In this port there are no other heroes' stalls to compete with.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHMkSlot { public string id; public int n; public long p; }
[Serializable] public class AHMkStall { public string city, trade; public long until, till, earned, sim; public List<AHMkSlot> slots = new List<AHMkSlot>(); public int nameI; }

public static class AHMarket
{
    public static readonly string[] Cities = { "ashen", "varrow", "highcairn", "mirewatch", "sunspire", "cinderhold", "coralport" };
    static readonly int[] RentSilver = { 2, 5, 3, 3, 3, 4, 4 };
    public static readonly string[][] Trades =
    {
        new[] { "smith", "Smiths’ Row", "Weapons, armor and metal bars" }, new[] { "tailor", "Tailors & Tanners", "Cloth, leather, robes, hides and pelts" }, new[] { "chef", "Cooks’ Corner", "Meals, bread and pies" },
        new[] { "alchemist", "Apothecaries", "Potions, elixirs and herbs" }, new[] { "jeweler", "Jewelers’ Court", "Rings, amulets and set gems" }, new[] { "farmer", "Farmers’ Market", "Crops, eggs, milk, honey and wool" },
        new[] { "miner", "Miners’ Exchange", "Ores and raw gems" }, new[] { "fisher", "Fishmongers", "Fresh fish and pearls" }, new[] { "woodcutter", "Timber Yard", "Logs and timber" },
    };
    // stall names: tap Rename to try the next one; the name hangs over your stall in the market lists
    static readonly Dictionary<string, string> TradeWord = new Dictionary<string, string> { { "smith", "Anvil" }, { "tailor", "Needle" }, { "chef", "Kettle" }, { "alchemist", "Cauldron" }, { "jeweler", "Gem" }, { "farmer", "Harvest" }, { "miner", "Pickaxe" }, { "fisher", "Net" }, { "woodcutter", "Axe" } };
    static readonly string[] NamePat = { "{h}'s {w}", "The Golden {w}", "The Honest {w}", "{h} & Sons", "The Lucky {w}", "The Silver {w}", "Fair Prices by {h}", "The Crooked Crow", "Ashen Wares", "The Merry {w}", "{h}'s Finest", "The Lantern and {w}" };
    public const int NameCount = 12;
    public static string StallName(AHPlayer p, AHMkStall s)
    {
        if (s == null) return "";
        string w; if (!TradeWord.TryGetValue(s.trade ?? "", out w)) w = "Stall";
        string h = p != null && !string.IsNullOrEmpty(p.heroName) ? p.heroName : "Hero";
        return NamePat[((s.nameI % NamePat.Length) + NamePat.Length) % NamePat.Length].Replace("{h}", h).Replace("{w}", w);
    }
    const int Slots = 8; const float Tax = 0.05f; const long Week = 7L * 86400000L;
    static long Now { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
    public static string CityName(string c) { var f = AHRep.Get(c); return f != null ? f.name : c; }
    public static long Rent(string c) { int i = Array.IndexOf(Cities, c); return (i >= 0 ? RentSilver[i] : 3) * 1000L; }   // copper
    public static bool CanRent(AHPlayer p, string trade) { return trade == "woodcutter" ? p.Skill("woodcutting") >= 5 : p.profs.Contains(trade); }
    public static long Floor(string id) { return Math.Max(1, (long)Math.Ceiling(AHComp.Value(id) * (AHAuction.ShopSold(id) ? 1.3 : 1.25))); }
    public static long Ceil(string id) { long f = Floor(id); return Math.Max(f + 1, (long)Math.Round(f * 1.6)); }

    // web buildTradeOf
    static Dictionary<string, string> tradeOf;
    public static string TradeOf(string id)
    {
        if (tradeOf == null)
        {
            tradeOf = new Dictionary<string, string>();
            var sk = new Dictionary<string, string> { { "smithing", "smith" }, { "tailoring", "tailor" }, { "cooking", "chef" }, { "herblore", "alchemist" }, { "jewelcrafting", "jeweler" }, { "farming", "farmer" } };
            foreach (var st in new[] { "anvil", "loom", "brew", "jewel", "oven", "mill", "dairy", "spin", "fire", "furnace" })
                foreach (var r in AHGather.Recipes(st)) { string t; if (r.skill != null && sk.TryGetValue(r.skill, out t) && !tradeOf.ContainsKey(r.outId)) { tradeOf[r.outId] = t; tradeOf[r.outId + "_mw"] = t; foreach (var q in new[] { "_fine", "_master" }) tradeOf[r.outId + q] = t; } }
            Action<IEnumerable<string>, string> set = (ids, t) => { foreach (var id0 in ids) if (id0 != null && !tradeOf.ContainsKey(id0)) tradeOf[id0] = t; };
            var rocks = AHJson.O(AHDB.Rules, "ROCKS") as Dictionary<string, object>; var ores = new List<string> { "gold_ore", "sapphire", "ruby", "emerald", "diamond" }; if (rocks != null) foreach (var kv in rocks) ores.Add(AHJson.S(kv.Value, "ore")); set(ores, "miner");
            var trees = AHJson.O(AHDB.Rules, "TREES") as Dictionary<string, object>; var logs = new List<string>(); if (trees != null) foreach (var kv in trees) logs.Add(AHJson.S(kv.Value, "log", "logs")); set(logs, "woodcutter");
            var herbs = AHJson.O(AHDB.Rules, "HERB_T") as Dictionary<string, object>; if (herbs != null) set(herbs.Keys, "alchemist");
            set(new[] { "raw_trout", "raw_eel", "raw_salmon", "raw_tuna", "raw_swordfish", "raw_lavaeel", "pearl" }, "fisher");
            var crops = AHDB.Table("home", "CROPS"); var cl = new List<string>(); if (crops != null) foreach (var kv in crops) { cl.Add(kv.Key); cl.Add(kv.Key + "_seed"); cl.Add(AHJson.S(kv.Value, "id")); } set(cl, "farmer");
            var an = AHDB.Table("home", "ANIMALS"); var al = new List<string>(); if (an != null) foreach (var kv in an) foreach (var k in new[] { "prod", "butcher" }) { var l = AHJson.A(kv.Value, k); if (l != null) foreach (var q in l) al.Add((string)((List<object>)q)[0]); } set(al, "farmer");
            var mobs = AHDB.Mobs; var sl = new List<string>(); if (mobs != null) foreach (var kv in mobs) { var l = AHJson.A(kv.Value, "skin"); if (l != null) foreach (var q in l) sl.Add((string)q); } set(sl, "tailor");
        }
        string tr; if (tradeOf.TryGetValue(id, out tr)) return tr;
        var it = AHItems.Get(id); if (it == null || it.cosmetic) return null;
        if (id.EndsWith("_bar")) return "smith"; if (it.potion) return "alchemist"; if (it.IsFood && !id.StartsWith("raw_")) return "chef";
        return null;
    }

    public static AHMkStall Stall(AHPlayer p, string city, string trade) { return p.prog.stalls.Find(s => s.city == city && s.trade == trade); }
    public static bool Live(AHMkStall s) { return s != null && Now < s.until; }

    public static void RentStall(AHGame g, string city, string trade)
    {
        var p = g.player; long c = Rent(city) * AHDB.CU;
        if (!CanRent(p, trade)) { g.ui.Toast(trade == "woodcutter" ? "The Timber Yard wants woodcutters (Woodcutting 5)." : "Only members of that trade may rent here. Take it up at the Guild."); return; }
        if (p.bag.money < c) { g.ui.Toast("A week here costs " + AHItems.MoneyText(c) + "."); return; }
        var s = Stall(p, city, trade); if (s == null) { s = new AHMkStall { city = city, trade = trade, sim = Now }; p.prog.stalls.Add(s); }
        p.bag.money -= c; p.bag.Touch(); s.until = Math.Max(Now, s.until) + Week; if (s.sim <= 0) s.sim = Now;
        g.ui.Toast("Your stall is yours for another week.", 3f); g.SaveProgress();
    }
    public static void Stock(AHGame g, AHMkStall s, string id, int n)
    {
        var p = g.player; if (!Live(s) || TradeOf(id) != s.trade) return;
        var slot = s.slots.Find(q => q.id == id);
        if (slot == null && s.slots.Count >= Slots) { g.ui.Toast("Your stall holds " + Slots + " kinds of goods."); return; }
        n = Mathf.Min(n, p.bag.Count(id)); if (n <= 0 || !p.bag.Take(id, n)) return;
        if (slot == null) { slot = new AHMkSlot { id = id, n = 0, p = (Floor(id) + Ceil(id)) / 2 }; s.slots.Add(slot); }
        slot.n += n; g.SaveProgress();
    }
    public static void Unstock(AHGame g, AHMkStall s, AHMkSlot q)
    {
        var p = g.player; if (q.n > 0 && !p.bag.Add(q.id, q.n)) { g.ui.Toast(p.bag.lastWarn ?? "Your bag is full."); return; }
        s.slots.Remove(q); g.SaveProgress();
    }
    public static void Price(AHMkSlot q, int dir) { long f = Floor(q.id), c = Ceil(q.id); long step = Math.Max(1, (c - f) / 6); q.p = Math.Max(f, Math.Min(c, q.p + dir * step)); }
    public static void Collect(AHGame g, AHMkStall s) { if (s.till <= 0) return; var p = g.player; p.AddMoney(s.till * AHDB.CU, p.transform.position); g.ui.Toast("You collect " + AHItems.MoneyText(s.till * AHDB.CU) + " from your till."); s.till = 0; g.SaveProgress(); }

    // web mkSimMine: townsfolk buy while you are away (no rival stalls here)
    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null) return; int sold = 0; long coin = 0;
        foreach (var s in p.prog.stalls)
        {
            if (!Live(s) || s.slots.Count == 0) { s.sim = Now; continue; }
            double hours = Math.Min(24, (Now - s.sim) / 3.6e6); if (hours < 0.05) continue;
            int ci = Array.IndexOf(Cities, s.city); float city = 0.8f + (ci >= 0 ? RentSilver[ci] : 3) * 0.08f;
            foreach (var q in s.slots)
            {
                long f = Floor(q.id), c = Ceil(q.id), pr = Math.Max(f, Math.Min(c, q.p));
                double cheap = 0.25 + 0.75 * (c - pr) / Math.Max(1, c - f), exp = 1.3 * cheap * 1.15 * city * hours;
                int n = Math.Min(q.n, (int)Math.Floor(exp) + (UnityEngine.Random.value < exp % 1 ? 1 : 0));
                if (n <= 0) continue;
                q.n -= n; long net = (long)Math.Round(pr * n * (1 - Tax)); s.till += net; s.earned += net; sold += n; coin += net;
            }
            s.slots.RemoveAll(q => q.n <= 0); s.sim = Now;
        }
        if (sold > 0) { g.ui.Toast("Townsfolk bought " + sold + " item" + (sold > 1 ? "s" : "") + " from your stall: " + AHItems.MoneyText(coin * AHDB.CU) + " waits in your till.", 4f); g.MarkDirty(); }
    }
}

public partial class AHUI
{
    string mkCity, mkTrade;
    public void OpenWarden(string city) { mkCity = city; mkTrade = null; wkMode = "mq"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderMarket(AHPlayer p)
    {
        var rows = new List<Action<int>>();
        if (mkTrade == null)
        {
            wkTitle.text = AHMarket.CityName(mkCity) + " · Merchants’ Quarter";
            wkHint.text = "Rent a stall in your own trade for " + AHItems.MoneyText(AHMarket.Rent(mkCity) * AHDB.CU) + " a week, stock it with goods you made or gathered, and set your prices. Townsfolk buy all day, even while you are away. 5% market tax.";
            foreach (var t in AHMarket.Trades)
            {
                string tr = t[0]; var s = AHMarket.Stall(p, mkCity, tr); bool live = AHMarket.Live(s), can = AHMarket.CanRent(p, tr);
                string l2 = t[2] + (live ? " · " + AHMarket.StallName(p, s) + ": " + s.slots.Count + " goods" + (s.till > 0 ? " · till " + AHItems.MoneyText(s.till * AHDB.CU) : "") : "");
                rows.Add(r => Row(r, t[1] + (live ? "  <color=#9be37a>your stall</color>" : ""), live ? new Color(0.6f, 0.9f, 0.5f) : can ? Color.white : new Color(0.6f, 0.55f, 0.5f), l2, live ? "Rented for " + Mathf.CeilToInt((s.until - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 86400000f) + " more days" : can ? "" : (tr == "woodcutter" ? "Needs Woodcutting 5" : "Needs the " + tr + " profession"),
                    live ? new WkBtn { label = "Manage", on = true, col = Go, act = () => { mkTrade = tr; wkPageI = 0; RenderWork(); } } : new WkBtn { label = "Rent", on = can, col = Plain, act = () => { AHMarket.RentStall(g, mkCity, tr); RenderWork(); } }));
            }
        }
        else
        {
            var s = AHMarket.Stall(p, mkCity, mkTrade); var T = Array.Find(AHMarket.Trades, x => x[0] == mkTrade);
            wkTitle.text = AHMarket.StallName(p, s) + " · " + T[1];
            wkHint.text = "Till: " + AHItems.MoneyText(s.till * AHDB.CU) + " · earned in all: " + AHItems.MoneyText(s.earned * AHDB.CU) + ". Cheaper prices sell faster. Prices stay between each good's floor and ceiling.";
            rows.Add(r => Row(r, "Till and lease", new Color(1f, 0.82f, 0.3f), AHItems.MoneyText(s.till * AHDB.CU) + " waiting", "",
                new WkBtn { label = "Collect", on = s.till > 0, col = Go, act = () => { AHMarket.Collect(g, s); RenderWork(); } },
                new WkBtn { label = "+1 week", on = true, col = Plain, act = () => { AHMarket.RentStall(g, mkCity, mkTrade); RenderWork(); } },
                new WkBtn { label = "Back", on = true, col = Plain, act = () => { mkTrade = null; wkPageI = 0; RenderWork(); } }));
            rows.Add(r => Row(r, "Sign: " + AHMarket.StallName(p, s), new Color(0.95f, 0.8f, 0.55f), "The name over your stall", "",
                new WkBtn { label = "Rename", on = true, col = Plain, act = () => { s.nameI = (s.nameI + 1) % AHMarket.NameCount; g.MarkDirty(); RenderWork(); } }));
            foreach (var q in s.slots)
            {
                var qq = q; var it = AHItems.Get(q.id);
                rows.Add(r => RowI(it, r, (it != null ? it.name : qq.id) + " × " + qq.n, AHItems.Quality(it), "Price " + AHItems.MoneyText(qq.p * AHDB.CU) + " each (" + AHItems.MoneyText(AHMarket.Floor(qq.id) * AHDB.CU) + " – " + AHItems.MoneyText(AHMarket.Ceil(qq.id) * AHDB.CU) + ")", "",
                    new WkBtn { label = "−", on = qq.p > AHMarket.Floor(qq.id), col = Plain, act = () => { AHMarket.Price(qq, -1); g.MarkDirty(); RenderWork(); } },
                    new WkBtn { label = "+", on = qq.p < AHMarket.Ceil(qq.id), col = Plain, act = () => { AHMarket.Price(qq, 1); g.MarkDirty(); RenderWork(); } },
                    new WkBtn { label = "Take back", on = true, col = Plain, act = () => { AHMarket.Unstock(g, s, qq); RenderWork(); } }));
            }
            foreach (var id in new List<string>(p.bag.order))
            {
                if (AHMarket.TradeOf(id) != mkTrade) continue; string ii = id; var it = AHItems.Get(id); int have = p.bag.Count(id);
                rows.Add(r => RowI(it, r, "Stock: " + it.name, AHItems.Quality(it), "You have " + have + " · sells for " + AHItems.MoneyText(AHMarket.Floor(ii) * AHDB.CU) + " – " + AHItems.MoneyText(AHMarket.Ceil(ii) * AHDB.CU), "",
                    new WkBtn { label = "Add 1", on = true, col = Plain, act = () => { AHMarket.Stock(g, s, ii, 1); RenderWork(); } },
                    new WkBtn { label = "Add all", on = true, col = Go, act = () => { AHMarket.Stock(g, s, ii, have); RenderWork(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
