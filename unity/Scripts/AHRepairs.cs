// Ashen Hollow: repairs by Artisans. Shopkeepers mend gear for coin (AHWear); the guild's Artisans do it for less.
//  - An Adventurer can send worn gear to the Artisans' Guild from anywhere: 70% of the shop's price.
//  - An Artisan mends their own gear at an anvil for 40% (less as their mender rank grows), and earns Smithing XP.
//  - An Artisan can also take repair orders: three a day, other heroes' worn pieces (a breastplate, a bow, a helm).
//    Mending one at an anvil uses bars or leather, and pays coin and Smithing XP.
//  Mender ranks (by pieces mended): Novice, Mender (10), Restorer (40), Master mender (100); each rank takes a little off
//  your own repairs and adds to what orders pay.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHRepairOrder { public string who, piece, mat; public int n; public long pay; public int xp; public bool done; }
[Serializable] public class AHRepairState { public int mended; public string day = ""; public List<AHRepairOrder> orders = new List<AHRepairOrder>(); }

public static class AHRepairs
{
    public static AHRepairState S(AHPlayer p) { if (p.prog.repairs == null) p.prog.repairs = new AHRepairState(); return p.prog.repairs; }
    public static readonly string[] Ranks = { "Novice", "Mender", "Restorer", "Master mender" };
    static readonly int[] RankAt = { 0, 10, 40, 100 };
    public static int RankI(AHPlayer p) { int m = S(p).mended, r = 0; for (int i = 0; i < RankAt.Length; i++) if (m >= RankAt[i]) r = i; return r; }
    public static string Rank(AHPlayer p) { return Ranks[RankI(p)]; }
    public static int NextAt(AHPlayer p) { int r = RankI(p); return r + 1 < RankAt.Length ? RankAt[r + 1] : 0; }

    public static float GuildK = 0.7f;
    public static float OwnK(AHPlayer p) { return 0.4f - 0.05f * RankI(p); }
    public static long Cost(AHPlayer p, float k) { long c = AHWear.RepairCost(p); return c <= 0 ? 0 : Math.Max(AHDB.CU / 10, (long)Math.Round(c * k)); }

    public static bool AtAnvil(AHPlayer p)
    {
        foreach (var s in AHGather.Spots) if (s.kind == "anvil" && (s.pos - p.transform.position).sqrMagnitude < 7f * 7f) return true;
        return false;
    }

    // an Adventurer's gear, mended by the guild
    public static void Guild(AHGame g)
    {
        var p = g.player; long c = Cost(p, GuildK); if (c <= 0) { g.ui.Toast("Nothing you wear needs mending."); return; }
        if (p.bag.money < c) { g.ui.Toast("The guild asks " + AHItems.MoneyText(c) + "."); return; }
        p.bag.money -= c; AHWear.RepairAll(g, p, false);
        g.ui.Toast("The Artisans' Guild mended your gear for " + AHItems.MoneyText(c) + ".", 3f);
    }

    // an Artisan mends their own gear at an anvil
    public static void Own(AHGame g)
    {
        var p = g.player; int n = AHWear.WornCount(p); long c = Cost(p, OwnK(p));
        if (n <= 0) { g.ui.Toast("Nothing you wear needs mending."); return; }
        if (!AtAnvil(p)) { g.ui.Toast("Stand by an anvil to mend your gear."); return; }
        if (p.bag.money < c) { g.ui.Toast("The materials cost " + AHItems.MoneyText(c) + "."); return; }
        p.bag.money -= c; AHWear.RepairAll(g, p, false);
        Mended(g, p, n, 25 * n);
    }

    static void Mended(AHGame g, AHPlayer p, int n, int xp)
    {
        int r0 = RankI(p); S(p).mended += n;
        p.GainXp("smithing", xp, false);
        if (RankI(p) > r0) g.ui.Banner("Mender rank: " + Rank(p), "Your own repairs cost less, and repair orders pay more");
        g.MarkDirty();
    }

    // ---------- repair orders (Artisans) ----------
    static readonly string[] Who = { "Sir Brannoc", "Captain Ysolde", "Tamsin the scout", "Old Garrow", "Lady Vessa", "Hedric the hunter", "Brother Amos", "Kael Varn" };
    static readonly string[,] Pieces = { { "dented breastplate", "iron_bar" }, { "cracked helm", "iron_bar" }, { "split bow", "logs" }, { "torn cloak", "linen" }, { "worn boots", "boar_hide" }, { "notched sword", "iron_bar" }, { "frayed robe", "linen" }, { "bent shield", "bronze_bar" } };
    static string Today { get { return DateTime.UtcNow.ToString("yyyy-MM-dd"); } }

    public static List<AHRepairOrder> Orders(AHPlayer p)
    {
        var s = S(p);
        if (s.day != Today)
        {
            s.day = Today; s.orders.Clear();
            var rnd = new System.Random(Today.GetHashCode() ^ (p.heroName ?? "").GetHashCode());
            for (int i = 0; i < 3; i++)
            {
                int k = rnd.Next(Pieces.GetLength(0)); string mat = Pieces[k, 1];
                if (AHItems.Get(mat) == null) mat = "iron_bar";
                int n = 1 + rnd.Next(3);
                long pay = (long)Math.Round((AHComp.Value(mat) * n * 2.2f + 40f) * (1f + 0.1f * RankI(p))) * AHDB.CU / 10;
                s.orders.Add(new AHRepairOrder { who = Who[rnd.Next(Who.Length)], piece = Pieces[k, 0], mat = mat, n = n, pay = Math.Max(AHDB.CU, pay), xp = 40 + 20 * n });
            }
        }
        return s.orders;
    }

    public static void Fill(AHGame g, AHRepairOrder o)
    {
        var p = g.player; if (o.done) return;
        if (p.path != "artisan") { g.ui.Toast("Only Artisans take repair orders."); return; }
        if (!AtAnvil(p)) { g.ui.Toast("Stand by an anvil to mend it."); return; }
        if (p.bag.Count(o.mat) < o.n) { g.ui.Toast("You need " + o.n + "× " + AHItems.Get(o.mat).name + "."); return; }
        p.bag.Take(o.mat, o.n); o.done = true;
        p.AddMoney(o.pay, p.transform.position);
        AHSound.Play("anvil");
        Mended(g, p, 1, o.xp);
        g.ui.Toast("You mended " + o.who + "'s " + o.piece + ". Paid " + AHItems.MoneyText(o.pay) + ".", 3.5f);
        p.bag.Touch();
    }
}

public partial class AHUI
{
    public void OpenRepairs() { wkMode = "repairs"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderRepairs(AHPlayer p)
    {
        var s = AHRepairs.S(p); var rows = new List<Action<int>>(); bool art = p.path == "artisan"; int worn = AHWear.WornCount(p);
        wkTitle.text = art ? "Repairs · " + AHRepairs.Rank(p) : "Guild repairs";
        wkHint.text = art ? "Mend your own gear at an anvil for less than a shop, and take other heroes' repair orders. Every piece mended counts towards your mender rank (" + s.mended + (AHRepairs.NextAt(p) > 0 ? " / " + AHRepairs.NextAt(p) : "") + ")."
                          : "The Artisans' Guild mends your gear for less than a shopkeeper, from anywhere. Your gear: lowest condition " + AHWear.LowestCondition(p) + "%.";
        long shop = AHWear.RepairCost(p);
        if (art)
        {
            long c = AHRepairs.Cost(p, AHRepairs.OwnK(p)); bool anvil = AHRepairs.AtAnvil(p);
            rows.Add(sl => Row(sl, "Mend your own gear", new Color(1f, 0.85f, 0.55f), worn > 0 ? worn + " worn piece" + (worn > 1 ? "s" : "") + " · materials " + AHItems.MoneyText(c) + " (a shop asks " + AHItems.MoneyText(shop) + ")" : "Nothing you wear needs mending",
                anvil ? "At an anvil" : "Stand by an anvil", new WkBtn { label = "Mend", on = worn > 0 && anvil && p.bag.money >= c, col = Go, act = () => { AHRepairs.Own(g); RenderWork(); } }));
            foreach (var o in AHRepairs.Orders(p))
            {
                var oo = o;
                rows.Add(sl => Row(sl, oo.who + "'s " + oo.piece, oo.done ? new Color(0.62f, 0.9f, 0.55f) : Color.white,
                    oo.done ? "<color=#9be37a>Mended</color>" : "Needs " + oo.n + "× " + AHItems.Get(oo.mat).name + " (you have " + p.bag.Count(oo.mat) + ") · pays " + AHItems.MoneyText(oo.pay) + " and " + oo.xp + " Smithing XP", "",
                    oo.done ? null : new WkBtn { label = "Mend", on = anvil && p.bag.Count(oo.mat) >= oo.n, col = Go, act = () => { AHRepairs.Fill(g, oo); RenderWork(); } }));
            }
            rows.Add(sl => Row(sl, "New orders every day", new Color(1f, 1f, 1f, 0.6f), "", ""));
        }
        else
        {
            long c = AHRepairs.Cost(p, AHRepairs.GuildK);
            rows.Add(sl => Row(sl, "Send your gear to the guild", new Color(1f, 0.85f, 0.55f), worn > 0 ? worn + " worn piece" + (worn > 1 ? "s" : "") + " · " + AHItems.MoneyText(c) + " (a shop asks " + AHItems.MoneyText(shop) + ")" : "Nothing you wear needs mending", "",
                new WkBtn { label = "Repair", on = worn > 0 && p.bag.money >= c, col = Go, act = () => { AHRepairs.Guild(g); RenderWork(); } }));
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
