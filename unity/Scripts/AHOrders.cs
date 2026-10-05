// Ashen Hollow: work orders from your guild, as in the web game (v67, newOrder / genOrders / ordersTick / deliverOrder).
// Your ★ focus profession's guild sends 3 orders (4 from Honored): Common, Rare (1 h), Epic (1.5 h) and Legendary
// (3 h, for the Revered). Deliver at a Guild Clerk for money, mastery, profession XP, gems and guild reputation;
// a late order costs half its reputation. Only goods you gathered or made yourself count: things bought in a shop
// or at the auction don't. Guild ranks: Member, Trusted, Honored (+1 order slot), Revered, Exalted, +10% pay each.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHOrderItem { public string id; public int n, q; }
[Serializable]
public class AHOrder
{
    public string tier, client; public List<AHOrderItem> items = new List<AHOrderItem>();
    public long pay, due; public int mxp, rep; public List<AHStack> bonus = new List<AHStack>();
}

public static class AHOrders
{
    static readonly int[] RankAt = { 0, 300, 1000, 2500, 6000 };
    public static readonly string[] RankName = { "Member", "Trusted", "Honored", "Revered", "Exalted" };
    static readonly string[] Clients = { "the City Guard", "the Wayfarer Inn", "Captain Mara", "the Highcairn Watch", "the Royal Kitchen", "a Sunspire merchant", "the Mirewatch healer", "a travelling knight", "the harbour master" };
    static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
    static object Tier(string t) { return AHJson.O(AHDB.Table("economy", "OTIERS"), t); }
    public static string TierName(string t) { return AHJson.S(Tier(t), "name", t); }
    public static Color TierCol(string t) { return AHDB.Col(((Dictionary<string, object>)Tier(t))["col"], Color.white); }

    public static int Rank(AHPlayer p) { int i = 0; while (i + 1 < RankAt.Length && p.guildRep >= RankAt[i + 1]) i++; return i; }
    public static int Slots(AHPlayer p) { return Rank(p) >= 2 ? 4 : 3; }

    // which versions of an item satisfy a quality demand (web qualIds)
    public static List<string> QualIds(string id, int q)
    {
        var l = new List<string>();
        var it = AHItems.Get(id);
        if (q == 0) { l.Add(id); if (AHItems.Get(id + "_fine") != null) { l.Add(id + "_fine"); l.Add(id + "_master"); } if (AHItems.Get(id + "_mw") != null) l.Add(id + "_mw"); return l; }
        if (it != null && it.meal) { if (q == 1) l.Add(id + "_fine"); l.Add(id + "_master"); return l; }
        l.Add(id + "_mw"); return l;
    }
    public static int Have(AHPlayer p, string id, int q) { int n = 0; foreach (var k in QualIds(id, q)) n += p.bag.Count(k); return n; }
    public static int Usable(AHPlayer p, string id, int q) { int n = 0; foreach (var k in QualIds(id, q)) n += p.OwnOf(k); return n; }
    public static string QualName(string id, int q)
    {
        var it = AHItems.Get(id); string nm = it != null ? it.name : id;
        if (q == 0) return nm;
        return (it != null && it.meal ? (q == 1 ? "Fine or better " : "Masterwork ") : "Masterwork ") + nm.ToLowerInvariant();
    }

    // web orderPool: what your guild asks for at your skill level
    static List<KeyValuePair<string, int>> Pool(AHPlayer p, string k)
    {
        var outp = new List<KeyValuePair<string, int>>();
        string skill = AHJson.S(AHJson.O(AHPlayer.Profs, k), "skill");
        int L = p.Skill(skill);
        Action<string, int, int> add = (id, lvl, n) => { if (AHItems.Get(id) != null && lvl <= L) outp.Add(new KeyValuePair<string, int>(id, n)); };
        var R = AHJson.O(AHDB.Rules, "RECIPES");
        Action<string, Func<object, bool>, Func<object, int>> fromRecipes = (st, ok, n) => { var l = AHJson.A(R, st); if (l != null) foreach (var r in l) if (ok(r)) add(AHJson.S(r, "out"), (int)AHJson.N(r, "lvl", 1), n(r)); };
        Func<object, bool> noMastery = r => !AHJson.Has(r, "mastery");
        switch (k)
        {
            case "smith": add("bronze_bar", 1, 6); add("iron_bar", 10, 5); fromRecipes("anvil", r => noMastery(r) && !AHJson.S(r, "out", "").EndsWith("_mw"), r => 1 + (AHJson.N(r, "lvl", 1) < 8 ? 1 : 0)); break;
            case "tailor": add("yarn", 1, 4); add("linen", 3, 3); fromRecipes("loom", noMastery, r => 1); break;
            case "alchemist": fromRecipes("brew", r => true, r => 3); break;
            case "chef": add("trout", 1, 6); add("meat", 1, 6); add("bread", 1, 6); add("salmon", 10, 5); fromRecipes("oven", r => true, r => 2); break;
            case "farmer":
                var crops = AHDB.Table("home", "CROPS");
                if (crops != null) foreach (var c in crops.Values) add(AHJson.S(c, "id"), (int)AHJson.N(c, "lvl", 1), 8);
                add("egg", 1, 8); add("milk", 3, 6); add("wool", 5, 5); add("honey", 3, 4); add("flour", 1, 6); break;
            case "miner":
                var rocks = AHJson.O(AHDB.Rules, "ROCKS") as Dictionary<string, object>;
                if (rocks != null) foreach (var r in rocks.Values) add(AHJson.S(r, "ore"), (int)AHJson.N(r, "req", 1), 8);
                add("sapphire", 12, 1); break;
            case "jeweler": add("gold_ring", 8, 1); fromRecipes("jewel", noMastery, r => 1); break;
            case "fisher":
                foreach (var f in new[] { new KeyValuePair<string, int>("raw_trout", 1), new KeyValuePair<string, int>("raw_eel", 5), new KeyValuePair<string, int>("raw_salmon", 10), new KeyValuePair<string, int>("raw_tuna", 12), new KeyValuePair<string, int>("raw_swordfish", 16), new KeyValuePair<string, int>("raw_lavaeel", 18) }) add(f.Key, f.Value, 6);
                break;
        }
        return outp;
    }

    public static AHOrder New(AHPlayer p)
    {
        string k = p.profMain; if (k == null) return null;
        int L = p.MasteryLv(k), rr = Rank(p); float x = UnityEngine.Random.value;
        string tier = rr >= 3 && L >= 30 && x < 0.06f ? "legend" : rr >= 1 && L >= 15 && x < 0.2f ? "epic" : L >= 5 && x < 0.45f ? "rare" : "common";
        var T = Tier(tier);
        var pool = Pool(p, k); if (pool.Count == 0) return null;
        for (int i = pool.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp; }
        int cnt = Mathf.Min(pool.Count, (int)AHJson.N(T, "items", 1));
        var o = new AHOrder { tier = tier, client = Clients[UnityEngine.Random.Range(0, Clients.Length)] };
        double v = 0;
        for (int i = 0; i < cnt; i++)
        {
            string id = pool[i].Key; int n0 = pool[i].Value; var it = AHItems.Get(id); int q = 0;
            bool canMw = AHItems.Get(id + "_mw") != null && ((k == "smith" && p.Perk("smith", 10)) || (k == "tailor" && p.Perk("tailor", 10)));
            if (it.meal && (tier == "rare" || tier == "epic") && UnityEngine.Random.value < 0.6f) q = 1;
            if (it.meal && tier == "legend") q = 2;
            if (canMw && (tier == "epic" || tier == "legend") && UnityEngine.Random.value < 0.5f) q = 2;
            int n = Mathf.Max(1, Mathf.RoundToInt(n0 * (float)AHJson.N(T, "nk", 1) * (1f + L / 40f) * (0.8f + UnityEngine.Random.value * 0.5f) / (q == 2 ? 2f : 1f)));
            o.items.Add(new AHOrderItem { id = id, n = n, q = q });
            v += AHComp.Value(id) * n * (q == 2 ? 2.2 : q == 1 ? 1.4 : 1);
        }
        o.pay = (long)Math.Round((v * AHJson.N(T, "pay", 2) + 40 * (1 + L / 5.0)) * (1 + rr * 0.1));
        o.mxp = (int)Math.Round((200 + v * 1.2) * AHJson.N(T, "mxp", 1) * (1 + L / 10.0));
        o.rep = (int)AHJson.N(T, "rep", 10);
        string[] gems = { "sapphire", "ruby", "emerald" };
        if (tier == "legend") { o.bonus.Add(new AHStack { id = "diamond", n = 1 }); o.bonus.Add(new AHStack { id = "mystery_sack", n = 2 }); }
        else if (tier == "epic") o.bonus.Add(new AHStack { id = gems[UnityEngine.Random.Range(0, 3)], n = 1 });
        else if (tier == "rare" && UnityEngine.Random.value < 0.4f) o.bonus.Add(new AHStack { id = "mystery_sack", n = 1 });
        long due = (long)AHJson.N(T, "due", 0);
        o.due = due > 0 ? NowMs + due * 60000 : 0;
        return o;
    }

    // web ordersTick: late orders cost reputation and are replaced; empty slots fill up
    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null || p.profMain == null) return;
        long t = NowMs; bool changed = false;
        for (int i = 0; i < p.orders.Count; i++)
        {
            var o = p.orders[i];
            if (o.due > 0 && t > o.due)
            {
                int loss = Mathf.RoundToInt(o.rep / 2f); p.guildRep = Mathf.Max(0, p.guildRep - loss); changed = true;
                g.ui.Toast("Too late: the " + TierName(o.tier).ToLowerInvariant() + " order for " + o.client + " expired. Guild reputation −" + loss + ".", 4f);
                var n = New(p); if (n != null) p.orders[i] = n; else { p.orders.RemoveAt(i); i--; }
            }
        }
        while (p.orders.Count < Slots(p)) { var o = New(p); if (o == null) break; p.orders.Add(o); changed = true; }
        if (changed) g.MarkDirty();
    }

    public static bool Ready(AHPlayer p, AHOrder o) { foreach (var it in o.items) if (Usable(p, it.id, it.q) < it.n) return false; return true; }

    public static void Deliver(AHGame g, int i)
    {
        var p = g.player; if (i < 0 || i >= p.orders.Count) return;
        var o = p.orders[i]; if (!Ready(p, o)) return;
        foreach (var it in o.items)
        {
            int need = it.n;
            foreach (var k in QualIds(it.id, it.q)) { int take = Mathf.Min(need, p.OwnOf(k)); if (take > 0) { p.bag.Take(k, take); need -= take; } }
        }
        int rb = Rank(p);
        p.ordersDone++; p.guildRep += o.rep;
        g.quests.Event("order", "any", 1, g);
        p.AddMoney(o.pay * AHDB.CU, p.transform.position);
        foreach (var b in o.bonus) p.bag.Add(b.id, b.n);
        p.AddMastery(p.profMain, o.mxp);
        p.GainXp(AHJson.S(AHJson.O(AHPlayer.Profs, p.profMain), "skill"), Mathf.RoundToInt(o.mxp / 6f), false);
        g.ui.Banner(TierName(o.tier) + " order delivered", o.client + " pays " + AHItems.MoneyText(o.pay * AHDB.CU));
        int ra = Rank(p);
        if (ra > rb) g.ui.Toast("The Guild now counts you as " + RankName[ra] + ". +" + ra * 10 + "% pay on every order." + (ra == 2 ? " You get a fourth order slot." : ra == 3 ? " Legendary commissions can now find you." : ""), 5f);
        p.orders[i] = New(p) ?? o;
        p.bag.Touch(); g.MarkDirty();
    }
}

public partial class AHUI
{
    long rerollAt;

    public void OpenOrders() { wkMode = "orders"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderOrders(AHPlayer p)
    {
        AHOrders.Tick(g);
        int rk = AHOrders.Rank(p);
        string focus = p.profMain != null ? AHJson.S(AHJson.O(AHPlayer.Profs, p.profMain), "name", p.profMain) : null;
        wkTitle.text = "Work orders" + (focus != null ? " · " + focus + "s’ guild" : "");
        wkHint.text = focus == null ? "Take up a profession and choose your ★ focus: its guild sends you orders." :
            "Guild rank: " + AHOrders.RankName[rk] + " (" + p.guildRep + " reputation) · " + p.ordersDone + " delivered · " + AHOrders.Slots(p) + " order slots. Only goods you gathered or made yourself count.";
        var rows = new List<Action<int>>();
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (int i = 0; i < p.orders.Count; i++)
        {
            var o = p.orders[i]; int ii = i;
            var lines = new List<string>();
            foreach (var it in o.items) { int u = AHOrders.Usable(p, it.id, it.q); lines.Add(Bad(u + "/" + it.n + " " + AHOrders.QualName(it.id, it.q), u >= it.n) + (AHOrders.Have(p, it.id, it.q) > u ? " <color=#9a9080>(bought goods don’t count)</color>" : "")); }
            string bonus = ""; foreach (var b in o.bonus) { var bi = AHItems.Get(b.id); bonus += " + " + b.n + " " + (bi != null ? bi.name.ToLowerInvariant() : b.id); }
            string left = o.due > 0 ? " · " + Mins(o.due - now) + " left" : "";
            rows.Add(slot => Row(slot, TierNameTag(o.tier) + " " + o.client + left, Color.white, string.Join(" · ", lines.ToArray()),
                "Pays " + AHItems.MoneyText(o.pay * AHDB.CU) + bonus + " · " + o.mxp.ToString("#,0") + " mastery XP · +" + o.rep + " reputation",
                new WkBtn { label = "Deliver", on = AHOrders.Ready(p, o), col = Go, act = () => { AHOrders.Deliver(g, ii); RenderWork(); } }));
        }
        if (p.profMain != null)
            rows.Add(slot => Row(slot, "New common orders", new Color(1f, 0.8f, 0.45f), "Swap your common orders for new ones (once every 10 minutes)", "",
                new WkBtn { label = "Reroll", on = now - rerollAt > 600000, col = Plain, act = () => { rerollAt = now; for (int i = 0; i < p.orders.Count; i++) if (p.orders[i].tier == "common") p.orders[i] = AHOrders.New(p) ?? p.orders[i]; g.MarkDirty(); RenderWork(); } }));
        rows.Add(slot => Row(slot, "Back to professions", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "prof"; wkProf = null; wkPageI = 0; RenderWork(); } }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    static string TierNameTag(string t) { return "<color=#" + ColorUtility.ToHtmlStringRGB(AHOrders.TierCol(t)) + ">" + AHOrders.TierName(t) + "</color>"; }
}
