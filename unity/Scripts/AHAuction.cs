// Ashen Hollow: the Auction House, as in the web game (v67, ahListings / ahTick / renderAH): a living market.
// Traders from all over the realm list goods, 4 new listings every 5 minutes, each lasting 30-120 minutes;
// the same listings at the same time as the web game (same random numbers). Prices drift with supply and
// demand, and four goods are "in demand" (+40%) for 6 hours at a time. Buy outright, bid in auctions (rival
// traders bid as the end nears), or list your own loot for 4 hours (3% fee up front, 8% cut when it sells).
// What you win, buy or get back waits in your auction mail; collect it at an Auctioneer (Varrow, Ashen Hollow).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHAucMine { public string id; public int n; public long unit, start, end; public string done; }
[Serializable] public class AHAucBid { public string key, id; public int n; public long amt, start, end, open, cap; public float seed; }
[Serializable]
public class AHAucState
{
    public List<AHAucMine> mine = new List<AHAucMine>();
    public long mailCoins; public List<AHStack> mailItems = new List<AHStack>();
    public List<AHAucBid> bids = new List<AHAucBid>();
    public List<string> gone = new List<string>();
    public int sold, bought; public long earned, last;
}

public static class AHAuction
{
    public const double Fee = 0.03, Cut = 0.08;
    const long Bucket = 5 * 60000; const double HotEvery = 6 * 3.6e6;
    public static readonly string[] Cats = { "all", "gear", "mats", "food", "potions", "rare" };
    public static readonly string[] CatNames = { "All", "Gear", "Materials", "Food", "Potions", "Gems & rare" };
    public static AHAucState S;
    static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }

    public class Listing { public string key, id, seller; public int n; public long start, end, price, open, cap, bo; public bool auction; public float seed; }

    // ---------- the web game's numbers ----------
    static double JsRound(double x) { return Math.Floor(x + 0.5); }
    class Rng
    {
        int s;
        public Rng(double seed) { s = unchecked((int)(long)seed); }
        public double Next()
        {
            unchecked
            {
                s = s + 0x6D2B79F5;
                int t = s;
                t = (t ^ (int)((uint)t >> 15)) * (1 | t);
                t = (t + ((t ^ (int)((uint)t >> 7)) * (61 | t))) ^ t;
                return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
            }
        }
    }
    static double HashS(string s) { uint h = 2166136261; foreach (char c in s) { h ^= c; h = unchecked(h * 16777619); } return h / 4294967296.0; }

    static Dictionary<string, List<string>> pool;
    static Dictionary<string, List<string>> Pool
    {
        get
        {
            if (pool != null) return pool;
            pool = new Dictionary<string, List<string>>();
            var t = AHDB.Table("items", "AH_POOL");
            foreach (var c in new[] { "gear", "mats", "food", "potions", "rare" }) { var l = new List<string>(); var a = AHJson.A(t, c); if (a != null) foreach (var o in a) l.Add((string)o); pool[c] = l; }
            return pool;
        }
    }
    static HashSet<string> shopSold, ahOk;
    static HashSet<string> Set(string k) { var h = new HashSet<string>(); var l = AHDB.List("items", k); if (l != null) foreach (var o in l) h.Add((string)o); return h; }
    public static bool ShopSold(string id) { if (shopSold == null) shopSold = Set("SHOP_SOLD"); return shopSold.Contains(id); }
    public static bool Allowed(string id) { if (ahOk == null) ahOk = Set("AH_OK"); return ahOk.Contains(id); }
    public static string Cat(string id) { return AHJson.S(AHDB.Table("items", "AH_CAT"), id, "mats"); }
    static double Val(string id) { return AHComp.Value(id); }

    static long hotB = long.MinValue; static List<string> hotL;
    public static List<string> Hot(double t)
    {
        long b = (long)Math.Floor(t / HotEvery);
        if (b == hotB && hotL != null) return hotL;
        var all = new List<string>(); all.AddRange(Pool["mats"]); all.AddRange(Pool["food"]); all.AddRange(Pool["potions"]); all.AddRange(Pool["gear"]);
        var r = new Rng(b * 7 + 3); var l = new List<string>();
        for (int i = 0; i < 4; i++) l.Add(all[(int)Math.Floor(r.Next() * all.Count)]);
        if (Math.Abs(t - NowMs) < HotEvery) { hotB = b; hotL = l; }
        return l;
    }
    public static double Mkt(string id, double t)
    {
        double h = HashS(id) * 6.28, x = t / 3.6e6;
        return Math.Max(0.55, 1 + 0.3 * Math.Sin(x / 5 + h) + 0.15 * Math.Sin(x / 1.3 + 2 * h) + (Hot(t).Contains(id) ? 0.4 : 0));
    }
    public static long MktPrice(string id, double t) { return (long)Math.Max(1, JsRound(Val(id) * (ShopSold(id) ? Math.Min(Mkt(id, t), AHShops.Markup * 0.85) : Mkt(id, t)))); }
    public static long MktPrice(string id) { return MktPrice(id, NowMs); }
    public static int Trend(string id) { double a = Mkt(id, NowMs), b = Mkt(id, NowMs - 3.6e6); return a > b * 1.03 ? 1 : a < b * 0.97 ? -1 : 0; }

    static List<Listing> At(long b)
    {
        var r = new Rng(b * 13 + 1); var outp = new List<Listing>();
        var sellers = AHDB.List("items", "AH_SELLERS");
        for (int i = 0; i < 4; i++)
        {
            double x = r.Next(); string cat = x < 0.4 ? "mats" : x < 0.6 ? "food" : x < 0.85 ? "gear" : x < 0.95 ? "potions" : "rare";
            var list = Pool[cat]; if (list.Count == 0) continue;
            string id = list[(int)Math.Floor(r.Next() * list.Count)]; long start = b * Bucket;
            int n = cat == "mats" ? 1 + (int)Math.Floor(r.Next() * 20) : cat == "food" ? 1 + (int)Math.Floor(r.Next() * 10) : cat == "potions" ? 1 + (int)Math.Floor(r.Next() * 5) : cat == "rare" ? 1 + (int)Math.Floor(r.Next() * 2) : 1;
            bool auction = r.Next() < 0.3; double unit = MktPrice(id, start); long dur = (30 + (long)Math.Floor(r.Next() * 90)) * 60000;
            var L = new Listing { key = b + ":" + i, id = id, n = n, seller = sellers != null ? (string)sellers[(int)Math.Floor(r.Next() * sellers.Count)] : "A trader", start = start, end = start + dur, auction = auction };
            if (auction)
            {
                L.open = (long)Math.Max(Math.Max(1, JsRound(unit * n * (0.45 + r.Next() * 0.25))), Math.Ceiling(Val(id) * n * 0.85));
                L.cap = (long)Math.Max(JsRound(unit * n * (0.95 + r.Next() * 0.45)), Math.Ceiling(Val(id) * n * 0.9));
                L.bo = (long)Math.Max(JsRound(unit * n * (1.5 + r.Next() * 0.3)), Math.Ceiling(L.cap * 1.05));
                L.seed = (float)r.Next();
            }
            else L.price = (long)Math.Max(Math.Max(1, JsRound(unit * (0.8 + r.Next() * 0.6))), Math.Ceiling(Val(id) * 0.85));
            outp.Add(L);
        }
        return outp;
    }

    public static List<Listing> Listings()
    {
        long t = NowMs, b = t / Bucket; var outp = new List<Listing>();
        for (long k = b - 24; k <= b; k++) foreach (var L in At(k)) if (L.end > t && !S.gone.Contains(L.key)) outp.Add(L);
        outp.Sort((a, c) => a.end.CompareTo(c.end));
        return outp;
    }

    static long NpcBid(long open, long cap, float seed, long start, long end, long t)
    {
        double f = Math.Max(0, Math.Min(1, (t - start) / (double)(end - start)));
        return (long)JsRound(open + (cap - open) * Math.Pow(f, 1.6) * (0.6 + seed * 0.5));
    }
    public static long NpcBid(Listing L, long t) { return NpcBid(L.open, L.cap, L.seed, L.start, L.end, t); }
    public static AHAucBid MyBid(string key) { return S.bids.Find(b => b.key == key); }
    public static long CurBid(Listing L) { var my = MyBid(L.key); return Math.Max(NpcBid(L, NowMs), my != null ? my.amt : 0); }

    // ---------- keeping the state ----------
    const string Key = "ah_auction";
    public static void Load()
    {
        S = null;
        try { if (PlayerPrefs.HasKey(Key)) S = JsonUtility.FromJson<AHAucState>(PlayerPrefs.GetString(Key)); } catch (Exception) { S = null; }
        if (S == null) S = new AHAucState { last = NowMs };
    }
    public static void Save() { if (S != null) PlayerPrefs.SetString(Key, JsonUtility.ToJson(S)); }
    public static void Clear() { PlayerPrefs.DeleteKey(Key); S = new AHAucState { last = NowMs }; }
    public static void MailAdd(string id, int n) { var s = S.mailItems.Find(q => q.id == id); if (s != null) s.n += n; else S.mailItems.Add(new AHStack { id = id, n = n }); }
    public static bool HasMail { get { return S.mailCoins > 0 || S.mailItems.Exists(q => q.n > 0); } }

    // web ahTick: your bids (outbid, won), buyers for your listings (minute by minute, more often when the price is fair)
    public static void Tick(AHGame g)
    {
        if (S == null) return;
        long t = NowMs; bool changed = false;
        foreach (var bd in new List<AHAucBid>(S.bids))
        {
            string nm = AHItems.Get(bd.id) != null ? AHItems.Get(bd.id).name : bd.id;
            if (NpcBid(bd.open, bd.cap, bd.seed, bd.start, bd.end, Math.Min(t, bd.end)) >= bd.amt)
            {
                S.mailCoins += bd.amt; S.bids.Remove(bd); changed = true;
                g.ui.Toast("Outbid on " + nm + "! Your " + AHItems.MoneyText(bd.amt * AHDB.CU) + " is back in your auction mail.", 4f); continue;
            }
            if (t >= bd.end) { MailAdd(bd.id, bd.n); S.gone.Add(bd.key); S.bought++; S.bids.Remove(bd); changed = true; g.ui.Banner("Auction won!", bd.n + "× " + nm); g.ui.Toast("You won the auction. Collect it from an Auctioneer.", 4f); }
        }
        if (S.last <= 0) S.last = t;
        if (t - S.last > 720 * 60000L) S.last = t - 720 * 60000L;
        int mins = (int)Math.Min(720, (t - S.last) / 60000);
        if (mins > 0)
        {
            S.last += mins * 60000L; changed = true;
            foreach (var m in S.mine)
            {
                if (m.done != null && m.done != "") continue;
                string nm = AHItems.Get(m.id) != null ? AHItems.Get(m.id).name : m.id;
                for (int k = 0; k < mins && string.IsNullOrEmpty(m.done); k++)
                {
                    long at = S.last - (mins - k - 1) * 60000L;
                    if (at >= m.end) { m.done = "expired"; MailAdd(m.id, m.n); g.ui.Toast("Your " + nm + " did not sell. It is back in your auction mail.", 4f); break; }
                    double ratio = MktPrice(m.id, at) * 1.1 / m.unit;
                    double p = 0.035 * Math.Max(0.02, Math.Min(4, ratio * ratio * ratio)) * (Hot(at).Contains(m.id) ? 1.6 : 1) / Math.Max(1, Math.Sqrt(m.n / 5.0));
                    if (UnityEngine.Random.value < p)
                    {
                        long got = (long)JsRound(m.unit * m.n * (1 - Cut)); m.done = "sold"; S.mailCoins += got; S.sold++; S.earned += got;
                        g.ui.Banner("Auction sold", m.n + "× " + nm);
                        g.ui.Toast("Sold " + m.n + "× " + nm + " for " + AHItems.MoneyText(got * AHDB.CU) + " (after the 8% house cut). Collect it from an Auctioneer.", 4f);
                    }
                }
            }
            S.mine.RemoveAll(m => !string.IsNullOrEmpty(m.done) && t - m.end >= 3600000L);
        }
        long cut = t / Bucket - 30;
        S.gone.RemoveAll(k => { long b; return long.TryParse(k.Split(':')[0], out b) && b < cut; });
        if (changed) Save();
    }
}

public partial class AHUI
{
    string ahTab = "browse", ahCatSel = "all"; int ahConfirm = -1;

    public void OpenAH()
    {
        if (g.player.mounted) AHComp.Dismount(g, true);
        AHAuction.Tick(g);
        wkMode = "ah"; wkPageI = 0; ahConfirm = -1;
        ShowWork(true);
        RenderWork();
    }

    static string TrendMark(string id) { int t = AHAuction.Trend(id); return t > 0 ? " <color=#9be37a>▲</color>" : t < 0 ? " <color=#ff8a7a>▼</color>" : ""; }
    static string Mins(long ms) { long m = Math.Max(0, ms / 60000); return m >= 60 ? (m / 60) + "h " + (m % 60) + "m" : m + "m"; }

    void RenderAH(AHPlayer p)
    {
        var A = AHAuction.S; long t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), CU = AHDB.CU;
        wkTitle.text = "Auction House";
        var hot = AHAuction.Hot(t);
        var hn = hot.ConvertAll(id => AHItems.Get(id) != null ? AHItems.Get(id).name : id);
        wkHint.text = "In demand now: " + string.Join(", ", hn.ToArray()) + " (prices +40%). You have " + AHItems.MoneyText(p.bag.money) + ".";
        var rows = new List<Action<int>>();
        rows.Add(slot => Row(slot, "", Color.white, "", "",
            new WkBtn { label = "Browse", on = ahTab != "browse", col = Plain, act = () => { ahTab = "browse"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Sell", on = ahTab != "sell", col = Plain, act = () => { ahTab = "sell"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "My listings", on = ahTab != "mine", col = Plain, act = () => { ahTab = "mine"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Mail" + (AHAuction.HasMail ? " •" : ""), on = ahTab != "mail", col = Plain, act = () => { ahTab = "mail"; wkPageI = 0; RenderWork(); } }));
        if (ahTab == "browse")
        {
            var cb = new List<WkBtn>();
            for (int i = 0; i < AHAuction.Cats.Length; i++) { string c = AHAuction.Cats[i]; cb.Add(new WkBtn { label = AHAuction.CatNames[i], on = ahCatSel != c, col = Plain, act = () => { ahCatSel = c; wkPageI = 0; RenderWork(); } }); }
            rows.Add(slot => Row(slot, "", Color.white, "", "", cb.ToArray()));
            foreach (var L in AHAuction.Listings())
            {
                if (ahCatSel != "all" && AHAuction.Cat(L.id) != ahCatSel) continue;
                var it = AHItems.Get(L.id); if (it == null) continue;
                var LL = L; long mp = AHAuction.MktPrice(L.id) * L.n;
                string nm = (L.n > 1 ? L.n + "× " : "") + it.name;
                string sub = L.seller + " · " + (L.auction ? "ends in " : "") + Mins(L.end - t) + (L.auction ? "" : " left") + " · market " + AHItems.MoneyText(mp * CU, 2) + TrendMark(L.id);
                string st = it.slot != null ? AHItems.StatLine(it) : "";
                if (!L.auction)
                {
                    long cost = L.price * L.n;
                    rows.Add(slot => RowI(it, slot, nm, AHItems.Quality(it), sub, st, new WkBtn { label = "Buy " + AHItems.MoneyText(cost * CU, 1), on = p.bag.money >= cost * CU, col = Go, act = () => AhBuy(p, LL, cost, false) }));
                }
                else
                {
                    long cur = AHAuction.CurBid(L), next = (long)Math.Ceiling(cur * 1.06) + 1; var my = AHAuction.MyBid(L.key);
                    rows.Add(slot => RowI(it, slot, "<color=#ffd66a>Auction</color> " + nm, AHItems.Quality(it), sub, "Top bid " + AHItems.MoneyText(cur * CU, 2) + (my != null ? " <color=#9be37a>(yours)</color>" : "") + (st != "" ? " · " + st : ""),
                        my != null ? null : new WkBtn { label = "Bid " + AHItems.MoneyText(next * CU, 1), on = p.bag.money >= next * CU, col = Plain, act = () => AhBid(p, LL, next) },
                        new WkBtn { label = "Buyout " + AHItems.MoneyText(LL.bo * CU, 1), on = p.bag.money >= LL.bo * CU, col = Go, act = () => AhBuy(p, LL, LL.bo, true) }));
                }
            }
            if (rows.Count == 2) rows.Add(slot => Row(slot, "Nothing listed in this category right now", new Color(1f, 1f, 1f, 0.6f), "New goods arrive every few minutes.", ""));
        }
        else if (ahTab == "sell")
        {
            rows.Add(slot => Row(slot, "List for 4 hours", new Color(1f, 0.8f, 0.45f), "The house takes 3% up front and 8% when it sells. Fair prices sell quickly; greedy ones may not sell at all.", "Loot, rare drops and gear sell here; everyday trade goods and shop goods don’t."));
            int active = A.mine.FindAll(m => string.IsNullOrEmpty(m.done)).Count;
            foreach (var id in p.bag.order)
            {
                int n = p.bag.Count(id); var it = AHItems.Get(id);
                if (n <= 0 || it == null || it.cosmetic || !AHAuction.Allowed(id)) continue;
                long m = AHAuction.MktPrice(id); string iid = id;
                var ks = new[] { 0.85, 1.0, 1.25 }; var labs = new[] { "Quick −15%", "Market", "+25%" };
                var bs = new List<WkBtn>();
                for (int i = 0; i < 3; i++) { double k = ks[i]; bs.Add(new WkBtn { label = labs[i], on = active < 8, col = i == 1 ? Go : Plain, act = () => AhList(p, iid, k) }); }
                rows.Add(slot => RowI(it, slot, n + "× " + it.name + TrendMark(iid) + (AHAuction.Hot(t).Contains(iid) ? " <color=#ffd66a>in demand</color>" : ""), AHItems.Quality(it),
                    "Market " + AHItems.MoneyText(m * CU, 2) + " each · a shop pays about " + AHItems.MoneyText(AHShops.PriceSell("general", iid), 2), "", bs.ToArray()));
            }
            if (rows.Count == 2) rows.Add(slot => Row(slot, "Nothing in your bag can be auctioned", new Color(1f, 1f, 1f, 0.6f), "Monster drops, rare finds and gear can.", ""));
        }
        else if (ahTab == "mine")
        {
            int act = A.mine.FindAll(m => string.IsNullOrEmpty(m.done)).Count;
            rows.Add(slot => Row(slot, act + " / 8 listings", new Color(1f, 0.8f, 0.45f), A.sold + " sold · " + AHItems.MoneyText(A.earned * CU) + " earned in total", ""));
            for (int i = 0; i < A.mine.Count; i++)
            {
                var m = A.mine[i]; var it = AHItems.Get(m.id); if (it == null) continue; int ii = i;
                string st = m.done == "sold" ? "<color=#9be37a>Sold</color> · money is in your mail" : !string.IsNullOrEmpty(m.done) ? "Did not sell · back in your mail" : AHItems.MoneyText(m.unit * CU, 2) + " each · " + Mins(m.end - t) + " left · market now " + AHItems.MoneyText(AHAuction.MktPrice(m.id) * CU, 2) + TrendMark(m.id);
                rows.Add(slot => RowI(it, slot, m.n + "× " + it.name, AHItems.Quality(it), st, "",
                    string.IsNullOrEmpty(m.done) ? new WkBtn { label = ahConfirm == ii ? "Tap again" : "Cancel", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => AhCancel(ii) } : null));
            }
            foreach (var bd in A.bids)
            {
                var it = AHItems.Get(bd.id); if (it == null) continue;
                rows.Add(slot => RowI(it, slot, bd.n + "× " + it.name, AHItems.Quality(it), "Your bid " + AHItems.MoneyText(bd.amt * CU, 2) + " · ends in " + Mins(bd.end - t) + " · rivals may still outbid you", ""));
            }
        }
        else
        {
            rows.Add(slot => Row(slot, "Coins", new Color(1f, 0.84f, 0.35f), AHItems.MoneyText(A.mailCoins * CU) + " from sales and refunds", "",
                new WkBtn { label = "Collect", on = A.mailCoins > 0, col = Go, act = () => { long c = A.mailCoins * CU; p.AddMoney(c, p.transform.position); A.mailCoins = 0; Toast("You collect " + AHItems.MoneyText(c) + "."); AhDone(); } }));
            foreach (var s in A.mailItems)
            {
                if (s.n <= 0) continue; var it = AHItems.Get(s.id); if (it == null) continue; var ss = s;
                rows.Add(slot => RowI(it, slot, ss.n + "× " + it.name, AHItems.Quality(it), "Won, bought or returned", "",
                    new WkBtn { label = "Take", on = true, col = Go, act = () => { if (!p.bag.Add(ss.id, ss.n)) { Toast(p.bag.lastWarn ?? "Your bag is full."); return; } p.NoteBought(ss.id, ss.n); A.mailItems.Remove(ss); AhDone(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void AhDone() { AHAuction.Save(); g.player.bag.Touch(); g.MarkDirty(); RenderWork(); }

    void AhBuy(AHPlayer p, AHAuction.Listing L, long cost, bool buyout)
    {
        var A = AHAuction.S; if (p.bag.money < cost * AHDB.CU) return;
        p.bag.money -= cost * AHDB.CU;
        var my = AHAuction.MyBid(L.key); if (my != null) { A.mailCoins += my.amt; A.bids.Remove(my); }
        A.gone.Add(L.key); A.bought++; AHAuction.MailAdd(L.id, L.n);
        Toast((buyout ? "Bought out " : "Bought " + L.n + "× ") + AHItems.Get(L.id).name + (buyout ? "" : " from " + L.seller) + ". It is in your auction mail.", 3f);
        AhDone();
    }

    void AhBid(AHPlayer p, AHAuction.Listing L, long amt)
    {
        if (amt <= AHAuction.CurBid(L) || p.bag.money < amt * AHDB.CU) return;
        p.bag.money -= amt * AHDB.CU;
        AHAuction.S.bids.Add(new AHAucBid { key = L.key, id = L.id, n = L.n, amt = amt, start = L.start, end = L.end, open = L.open, cap = L.cap, seed = L.seed });
        Toast("You bid " + AHItems.MoneyText(amt * AHDB.CU) + " on " + AHItems.Get(L.id).name + ". Rival traders may bid higher before it ends.", 3f);
        AhDone();
    }

    void AhList(AHPlayer p, string id, double k)
    {
        var A = AHAuction.S; int n = p.bag.Count(id); if (n <= 0) return;
        if (A.mine.FindAll(m => string.IsNullOrEmpty(m.done)).Count >= 8) return;
        long unit = Math.Max(1, (long)Math.Floor(AHAuction.MktPrice(id) * k + 0.5)), fee = Math.Max(1, (long)Math.Floor(unit * n * AHAuction.Fee + 0.5));
        if (p.bag.money < fee * AHDB.CU) { Toast("The listing fee is " + AHItems.MoneyText(fee * AHDB.CU) + "."); return; }
        long t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        p.bag.money -= fee * AHDB.CU; p.bag.Take(id, n);
        if (A.last <= 0) A.last = t;
        A.mine.Add(new AHAucMine { id = id, n = n, unit = unit, start = t, end = t + 4 * 3600000L });
        Toast("Listed " + n + "× " + AHItems.Get(id).name + " at " + AHItems.MoneyText(unit * AHDB.CU) + " each (fee " + AHItems.MoneyText(fee * AHDB.CU) + ").", 3f);
        AhDone();
    }

    void AhCancel(int i)
    {
        var A = AHAuction.S; if (i < 0 || i >= A.mine.Count) return; var m = A.mine[i];
        if (!string.IsNullOrEmpty(m.done)) return;
        if (ahConfirm != i) { ahConfirm = i; RenderWork(); return; }
        ahConfirm = -1; m.done = "cancelled"; m.end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); AHAuction.MailAdd(m.id, m.n);
        AhDone();
    }
}
