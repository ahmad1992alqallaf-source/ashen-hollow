// Ashen Hollow: the Requests board, where heroes of the realm ask each other for help, and both paths need each other.
//  - "Wanted: made or gathered": Adventurers ask for bars, potions, meals, cloth and fish. An Artisan earns 25% more on these.
//  - "Wanted: from the wilds": Artisans ask for hides, fangs and beast drops, or for beasts cleared from their camps.
//    An Adventurer earns 25% more on these.
//  Anyone may fill any request; only goods you gathered, made or won yourself count (not shop or auction goods).
//  - "Ask the realm": post your own request (the coin is held until it is filled); another hero fills it in 15 to 90
//    minutes and the goods come to your bag (or your auction mail if the bag is full). Cancel before then for a refund.
// Every request filled earns Renown; every 10th brings a mystery sack. Opened from the menu (Menu -> Requests board).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHReq { public string kind, from, fromPath, id; public int n; public long pay, until, base0; public bool taken; }
[Serializable]
public class AHReqState
{
    public List<AHReq> open = new List<AHReq>(), mine = new List<AHReq>(), asks = new List<AHReq>();
    public string askDay = ""; public int filled, renown, posted;
}

public static class AHRequests
{
    public const int Open = 6, MaxMine = 3;
    static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
    static AHReqState S(AHPlayer p) { if (p.prog.req == null) p.prog.req = new AHReqState(); return p.prog.req; }
    public static AHReqState State(AHPlayer p) { return S(p); }

    static readonly string[] Names = { "Kaelen", "Brynja", "Sera the Swift", "Hollis", "Mirelle", "Dunstan", "Yara Oakhand", "Fenwick", "Tobias Grey", "Liora", "Garrick", "Nessa", "Bram Ironhide", "Isolde", "Quinn", "Rowan", "Magda", "Pip", "Ottilie", "Corin", "Wren", "Talia Brook" };
    // beasts an Artisan may want cleared, with how many (the bounty board's own list)
    static readonly string[] SlayT = { "boar", "wolf", "bandit", "bogtoad", "lurker", "crab", "jackal", "scorpion", "icewolf", "troll", "raptor", "hornback", "pterra" };
    static readonly int[] SlayN = { 6, 6, 5, 6, 3, 6, 5, 5, 5, 2, 5, 3, 4 };
    static readonly string[] Hides = { "boar_hide", "deer_hide", "wolf_pelt", "wolf_fang" };
    static readonly int[] HideLv = { 1, 1, 4, 4 };

    public static string ItemName(string id) { var it = AHItems.Get(id); return it != null ? it.name : id; }
    public static string KindName(string k) { return k == "make" ? "Made or gathered" : k == "slay" ? "Clear the beasts" : "From the wilds"; }
    // the path that earns 25% more on a request
    public static string Favours(string kind) { return kind == "make" ? "artisan" : "adventurer"; }
    public static long PayFor(AHPlayer p, AHReq r) { return Favours(r.kind) == p.path ? (long)Math.Round(r.pay * 1.25) : r.pay; }

    // ---------- what can be asked for ----------
    static List<KeyValuePair<string, int>> MakePool(AHPlayer p)
    {
        var l = new List<KeyValuePair<string, int>>();
        Action<string, int> add = (id, n) => { if (id != null && AHItems.Get(id) != null && !l.Exists(q => q.Key == id)) l.Add(new KeyValuePair<string, int>(id, n)); };
        var R = AHJson.O(AHDB.Rules, "RECIPES") as Dictionary<string, object>;
        if (R != null)
            foreach (var st in new[] { "anvil", "loom", "brew", "oven", "jewel", "spin", "mill", "dairy" })
            {
                var rl = AHJson.A(R, st); if (rl == null) continue;
                foreach (var r in rl)
                {
                    if (AHJson.Has(r, "mastery")) continue;
                    string outId = AHJson.S(r, "out", ""); if (outId.EndsWith("_mw")) continue;
                    int lv = (int)AHJson.N(r, "lvl", 1), cap = Mathf.Max(6, p.Skill(AHJson.S(r, "skill", "")) + 5);
                    if (lv > cap) continue;
                    add(outId, st == "anvil" || st == "loom" || st == "jewel" ? 1 : st == "brew" ? 3 : 4);
                }
            }
        var rocks = AHJson.O(AHDB.Rules, "ROCKS") as Dictionary<string, object>;
        if (rocks != null) foreach (var r in rocks.Values) if ((int)AHJson.N(r, "req", 1) <= Mathf.Max(6, p.Skill("mining") + 5)) add(AHJson.S(r, "ore"), 8);
        string[] fish = { "raw_trout", "raw_eel", "raw_salmon", "raw_tuna", "raw_swordfish" }; int[] fl = { 1, 5, 10, 12, 16 };
        for (int i = 0; i < fish.Length; i++) if (fl[i] <= Mathf.Max(6, p.Skill("fishing") + 5)) add(fish[i], 6);
        return l;
    }

    static List<KeyValuePair<string, int>> WildPool(AHPlayer p)
    {
        var l = new List<KeyValuePair<string, int>>();
        for (int i = 0; i < Hides.Length; i++) if (HideLv[i] <= p.level + 3 && AHItems.Get(Hides[i]) != null) l.Add(new KeyValuePair<string, int>(Hides[i], 4));
        var mobs = AHDB.Mobs;
        if (mobs != null)
            foreach (var kv in mobs)
            {
                var m = kv.Value; if (AHJson.B(m, "elite") || AHJson.B(m, "rare") || AHJson.B(m, "boss")) continue;
                if ((int)AHJson.N(m, "lvl", 1) > p.level + 3) continue;
                var dr = AHJson.A(m, "drops"); if (dr == null) continue;
                foreach (var d in dr)
                {
                    var row = d as List<object>; if (row == null || row.Count < 2) continue;
                    string id = Convert.ToString(row[0]); double ch = Convert.ToDouble(row[1], System.Globalization.CultureInfo.InvariantCulture);
                    if (id.StartsWith("mount:") || ch < 0.15 || AHItems.Get(id) == null || l.Exists(q => q.Key == id)) continue;
                    if (AHJson.S(AHDB.Table("items", "AH_CAT"), id, "mats") == "gear") continue;
                    l.Add(new KeyValuePair<string, int>(id, ch >= 0.5 ? 4 : 2));
                }
            }
        return l;
    }

    static List<int> SlayPool(AHPlayer p)
    {
        var l = new List<int>();
        for (int i = 0; i < SlayT.Length; i++) { var m = AHJson.O(AHDB.Mobs, SlayT[i]); if (m != null && (int)AHJson.N(m, "lvl", 1) <= p.level + 3) l.Add(i); }
        return l;
    }

    static T Pick<T>(List<T> l) { return l[UnityEngine.Random.Range(0, l.Count)]; }
    static int Vary(int n) { return Mathf.Max(1, Mathf.RoundToInt(n * (0.75f + UnityEngine.Random.value * 0.6f))); }

    static AHReq NewMake(AHPlayer p)
    {
        var pool = MakePool(p); if (pool.Count == 0) return null;
        var e = Pick(pool); int n = Vary(e.Value);
        return new AHReq { kind = "make", from = Pick(new List<string>(Names)), fromPath = "adventurer", id = e.Key, n = n,
            pay = (long)Math.Round(AHComp.Value(e.Key) * n * 1.8 + 30 + p.level * 4), until = NowMs + UnityEngine.Random.Range(3, 9) * 3600000L };
    }

    static AHReq NewWild(AHPlayer p)
    {
        var who = Pick(new List<string>(Names));
        var slay = SlayPool(p); var pool = WildPool(p);
        if (slay.Count > 0 && (pool.Count == 0 || UnityEngine.Random.value < 0.45f))
        {
            int i = Pick(slay); int lvl = (int)AHJson.N(AHJson.O(AHDB.Mobs, SlayT[i]), "lvl", 1);
            return new AHReq { kind = "slay", from = who, fromPath = "artisan", id = SlayT[i], n = SlayN[i], pay = (long)Math.Round(lvl * SlayN[i] * 9.0 + 40),
                until = NowMs + UnityEngine.Random.Range(3, 9) * 3600000L };
        }
        if (pool.Count == 0) return null;
        var e = Pick(pool); int n = Vary(e.Value);
        return new AHReq { kind = "wild", from = who, fromPath = "artisan", id = e.Key, n = n,
            pay = (long)Math.Round(AHComp.Value(e.Key) * n * 2.0 + 30 + p.level * 4), until = NowMs + UnityEngine.Random.Range(3, 9) * 3600000L };
    }

    // keep the board full: half made-goods requests, half from-the-wilds; old ones leave; your own requests get filled
    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        var s = S(p); long t = NowMs; bool changed = false;
        int before = s.open.Count;
        s.open.RemoveAll(q => q == null || q.until < t);
        if (s.open.Count != before) changed = true;
        int guard = 0;
        while (s.open.Count < Open && guard++ < 20)
        {
            int makes = s.open.FindAll(q => q.kind == "make").Count;
            var nr = makes < Open / 2 ? NewMake(p) : NewWild(p);
            if (nr == null) nr = makes < Open / 2 ? NewWild(p) : NewMake(p);
            if (nr == null) break;
            if (s.open.Exists(q => q.id == nr.id && q.kind == nr.kind)) continue;
            s.open.Add(nr); changed = true;
        }
        for (int i = 0; i < s.mine.Count; i++)
        {
            var m = s.mine[i]; if (m.until > t) continue;
            s.mine.RemoveAt(i); i--; changed = true;
            bool inBag = p.bag.Add(m.id, m.n);
            if (inBag) p.NoteBought(m.id, m.n); else if (AHAuction.S != null) AHAuction.MailAdd(m.id, m.n);
            string who = Names[UnityEngine.Random.Range(0, Names.Length)];
            g.ui.Banner("Request filled", who + " brought " + m.n + " " + ItemName(m.id).ToLowerInvariant());
            g.ui.Toast(inBag ? "They are in your bag." : "Your bag is full: collect them from your auction mail at an Auctioneer.", 4f);
            p.bag.Touch();
        }
        if (changed) g.MarkDirty();
    }

    public static int Progress(AHPlayer p, AHReq r)
    {
        if (r.kind == "slay") return r.taken ? (int)Mathf.Min(r.n, AHProgress.Get(p.prog.kills, r.id) - r.base0) : 0;
        return Mathf.Min(r.n, p.OwnOf(r.id));
    }
    public static bool Ready(AHPlayer p, AHReq r) { return Progress(p, r) >= r.n; }

    public static void Take(AHGame g, AHReq r)
    {
        var p = g.player; if (r.kind != "slay" || r.taken) return;
        if (S(p).open.FindAll(q => q.kind == "slay" && q.taken).Count >= 3) { g.ui.Toast("Three beast hunts at a time. Finish one first."); return; }
        r.taken = true; r.base0 = AHProgress.Get(p.prog.kills, r.id);
        g.ui.Toast("Taken: clear " + r.n + " " + AHJson.S(AHJson.O(AHDB.Mobs, r.id), "name", r.id).ToLowerInvariant() + " for " + r.from + ".", 3f);
        g.MarkDirty();
    }

    public static void Fill(AHGame g, AHReq r)
    {
        var p = g.player; var s = S(p);
        if (!s.open.Contains(r) || !Ready(p, r)) return;
        if (r.kind != "slay") p.bag.Take(r.id, r.n);
        long pay = PayFor(p, r);
        p.AddMoney(pay * AHDB.CU, p.transform.position);
        int ren = Favours(r.kind) == p.path ? 2 : 1;
        s.renown += ren; s.filled++; p.prog.Bump("requests");
        p.GainClassXpDirect(Mathf.RoundToInt(pay * 0.6f));
        s.open.Remove(r);
        g.ui.Banner("Request filled", r.from + " pays " + AHItems.MoneyText(pay * AHDB.CU) + " · +" + ren + " renown");
        if (s.filled % 10 == 0 && AHItems.Get("mystery_sack") != null) { p.bag.Add("mystery_sack", 1); g.ui.Toast(s.filled + " requests filled: the realm sends a mystery sack as thanks.", 4f); }
        p.bag.Touch(); g.MarkDirty();
        Tick(g);
    }

    // ---------- asking the realm yourself ----------
    // today's suggestions: an Adventurer asks for made goods, an Artisan for things from the wilds
    public static List<AHReq> Asks(AHPlayer p)
    {
        var s = S(p); string day = DateTime.Now.ToString("yyyy-MM-dd") + p.path;
        if (s.askDay == day && s.asks.Count > 0) return s.asks;
        s.askDay = day; s.asks.Clear();
        var pool = p.path == "artisan" ? WildPool(p) : MakePool(p);
        if (pool.Count == 0) pool = p.path == "artisan" ? MakePool(p) : WildPool(p);
        for (int i = pool.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp; }
        for (int i = 0; i < Mathf.Min(5, pool.Count); i++)
        {
            int n = pool[i].Value;
            s.asks.Add(new AHReq { kind = "ask", id = pool[i].Key, n = n, pay = (long)Math.Round(AHComp.Value(pool[i].Key) * n * 1.5 + 10) });
        }
        return s.asks;
    }

    public static void Post(AHGame g, AHReq a)
    {
        var p = g.player; var s = S(p);
        if (s.mine.Count >= MaxMine) { g.ui.Toast("You can have " + MaxMine + " requests up at a time."); return; }
        long cost = a.pay * AHDB.CU;
        if (p.bag.money < cost) { g.ui.Toast("You need " + AHItems.MoneyText(cost) + " to post this request."); return; }
        p.bag.money -= cost; p.bag.Touch();
        s.mine.Add(new AHReq { kind = "ask", id = a.id, n = a.n, pay = a.pay, until = NowMs + UnityEngine.Random.Range(15, 91) * 60000L });
        s.posted++;
        g.ui.Toast("Posted. The coin is held until someone fills it.", 3f);
        g.MarkDirty();
    }

    public static void Cancel(AHGame g, AHReq m)
    {
        var p = g.player; var s = S(p); if (!s.mine.Remove(m)) return;
        p.bag.money += m.pay * AHDB.CU; p.bag.Touch();
        g.ui.Toast("Request taken down: " + AHItems.MoneyText(m.pay * AHDB.CU) + " back.", 3f);
        g.MarkDirty();
    }
}

public partial class AHUI
{
    string rqTab = "board";
    public void OpenRequests() { wkMode = "requests"; rqTab = "board"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderRequests(AHPlayer p)
    {
        AHRequests.Tick(g);
        var s = AHRequests.State(p);
        bool art = p.path == "artisan";
        wkTitle.text = rqTab == "board" ? "Requests board" : "Ask the realm";
        wkHint.text = rqTab == "board"
            ? "Heroes ask each other for help. " + (art ? "Artisans earn 25% more on made or gathered goods." : "Adventurers earn 25% more on things from the wilds.") + " Only goods you gathered, made or won count. Renown " + s.renown + " · " + s.filled + " filled."
            : "Post what you need; another hero fills it in 15 to 90 minutes. The coin is held until then. Up to " + AHRequests.MaxMine + " at a time.";
        var rows = new List<Action<int>>();
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (rqTab == "board")
        {
            rows.Add(sl => Row(sl, "Ask the realm", new Color(0.56f, 0.85f, 1f), s.mine.Count > 0 ? s.mine.Count + " of your requests waiting" : "Post your own request", "",
                new WkBtn { label = "Open", on = true, col = Plain, act = () => { rqTab = "ask"; wkPageI = 0; RenderWork(); } }));
            // your path's own requests first
            var list = new List<AHReq>(s.open);
            list.Sort((a, b) => (AHRequests.Favours(b.kind) == p.path ? 1 : 0).CompareTo(AHRequests.Favours(a.kind) == p.path ? 1 : 0));
            foreach (var r in list)
            {
                var rr = r; bool fav = AHRequests.Favours(r.kind) == p.path;
                long pay = AHRequests.PayFor(p, r);
                int have = AHRequests.Progress(p, r);
                string what = r.kind == "slay" ? "Clear " + r.n + " " + AHJson.S(AHJson.O(AHDB.Mobs, r.id), "name", r.id).ToLowerInvariant() + " near their camp"
                    : r.n + " " + AHRequests.ItemName(r.id).ToLowerInvariant();
                string who = r.from + " (" + (r.fromPath == "artisan" ? "Artisan" : "Adventurer") + ")";
                string prog = r.kind == "slay" && !r.taken ? "" : Bad(have + " / " + r.n, have >= r.n);
                WkBtn btn = r.kind == "slay" && !r.taken
                    ? new WkBtn { label = "Take", on = true, col = Plain, act = () => { AHRequests.Take(g, rr); RenderWork(); } }
                    : new WkBtn { label = "Fill", on = have >= r.n, col = Go, act = () => { AHRequests.Fill(g, rr); RenderWork(); } };
                rows.Add(sl => Row(sl, what, fav ? new Color(1f, 0.8f, 0.45f) : Color.white,
                    who + " · " + AHRequests.KindName(rr.kind) + " · " + Mins(rr.until - now) + " left" + (prog.Length > 0 ? " · " + prog : ""),
                    "Pays " + AHItems.MoneyText(pay * AHDB.CU) + (fav ? " <color=#c9e0a0>(+25% for your path)</color>" : "") + " · renown +" + (fav ? 2 : 1), btn));
            }
            if (s.open.Count == 0) rows.Add(sl => Row(sl, "No requests right now", new Color(1f, 1f, 1f, 0.7f), "New ones come up as old ones are filled or run out.", ""));
        }
        else
        {
            foreach (var m in s.mine)
            {
                var mm = m;
                rows.Add(sl => Row(sl, "Your request: " + mm.n + " " + AHRequests.ItemName(mm.id).ToLowerInvariant(), new Color(0.56f, 0.85f, 1f),
                    "Offered " + AHItems.MoneyText(mm.pay * AHDB.CU) + " · someone is on it, about " + Mins(mm.until - now), "",
                    new WkBtn { label = "Cancel", on = true, col = Plain, act = () => { AHRequests.Cancel(g, mm); RenderWork(); } }));
            }
            foreach (var a in AHRequests.Asks(p))
            {
                var aa = a; long cost = a.pay * AHDB.CU;
                rows.Add(sl => Row(sl, "Ask for " + aa.n + " " + AHRequests.ItemName(aa.id).ToLowerInvariant(), Color.white,
                    "Offer " + AHItems.MoneyText(cost) + " · filled in 15 to 90 minutes", "",
                    new WkBtn { label = "Post", on = s.mine.Count < AHRequests.MaxMine && p.bag.money >= cost, col = Go, act = () => { AHRequests.Post(g, aa); RenderWork(); } }));
            }
            rows.Add(sl => Row(sl, "Back to the board", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { rqTab = "board"; wkPageI = 0; RenderWork(); } }));
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
