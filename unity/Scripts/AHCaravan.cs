// Ashen Hollow: caravans. Both paths, one road.
//  - Send a caravan (best for Artisans): load up to three stacks of goods you gathered or made and send them to a town
//    that wants them. A town pays 30-60% over the goods' worth for what it is short of today. The road takes 30, 60 or
//    120 minutes, and bandits may strike (a 25% chance to lose a third of the cargo). Lower the risk by hiring a guard
//    (10% of the cargo's worth: 10% risk) or by riding ahead and clearing the road yourself (defeat 15 monsters while
//    it travels: no risk). Artisans pay half for guards.
//  - Guard contracts (best for Adventurers): other heroes' caravans need guards. Take one and defeat its number of
//    monsters before the caravan arrives; you are paid on arrival (Adventurers 25% more), with Renown.
// Opened from the menu (Menu -> Caravans).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHCaravanRun { public string town; public List<AHStack> cargo = new List<AHStack>(); public long start, end, worth; public float mult; public bool guard; public long kills0; public bool told; }
[Serializable] public class AHGuardJob { public string who, town; public int need; public long pay, start, end, kills0; public bool taken; }
[Serializable]
public class AHCaravanState
{
    public List<AHCaravanRun> runs = new List<AHCaravanRun>(); public List<AHGuardJob> jobs = new List<AHGuardJob>();
    public List<AHStack> load = new List<AHStack>(); public int sent, guarded; public long earned;
}

public static class AHCaravan
{
    public class Town { public string id, name; public int mins; }
    public static readonly Town[] Towns =
    {
        new Town { id = "varrow", name = "Varrow", mins = 30 }, new Town { id = "mirewatch", name = "Mirewatch", mins = 60 },
        new Town { id = "coralport", name = "Coralport", mins = 60 }, new Town { id = "highcairn", name = "Highcairn", mins = 120 },
        new Town { id = "cinderhold", name = "Cinderhold", mins = 120 },
    };
    static readonly string[] Cats = { "mats", "food", "potions", "gear" };
    static readonly string[] CatNames = { "materials", "food", "potions", "gear" };
    static readonly string[] Who = { "Hollis", "Mirelle", "Dunstan", "Yara Oakhand", "Fenwick", "Liora", "Garrick", "Nessa", "Old Tam", "Corin" };
    public const int MaxRuns = 2, MaxStacks = 3, ClearKills = 15;
    public const float Risk = 0.25f, GuardRisk = 0.1f, Loss = 1f / 3f;
    static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
    static int DayN { get { return (int)(NowS / 86400); } }

    public static AHCaravanState S(AHPlayer p) { if (p.prog.caravan == null) p.prog.caravan = new AHCaravanState(); return p.prog.caravan; }
    public static Town TownOf(string id) { return Array.Find(Towns, t => t.id == id); }
    public static long Kills(AHPlayer p) { long n = 0; foreach (var kv in p.prog.kills) n += kv.v; return n; }

    // what a town is short of today, and how much over worth it pays
    static int H(string s) { int h = 17; foreach (char c in s) h = unchecked(h * 31 + c); return h & 0x7fffffff; }
    public static int WantI(Town t) { return (H(t.id) ^ (DayN * 7919)) % Cats.Length; }
    public static string WantName(Town t) { return CatNames[WantI(t)]; }
    public static float Mult(Town t) { return 1.3f + ((H(t.id) + DayN * 31) & 0x7fffffff) % 4 * 0.1f; }
    public static string Cat(string id) { string c = AHAuction.Cat(id); return c == "rare" ? "mats" : c; }
    public static bool Wanted(Town t, string id) { return Cat(id) == Cats[WantI(t)]; }

    public static long Worth(List<AHStack> l) { long v = 0; foreach (var s in l) v += AHComp.Value(s.id) * s.n; return v; }
    public static long GuardCost(AHPlayer p, long worth) { return (long)Math.Ceiling(worth * 0.1 * (p.path == "artisan" ? 0.5 : 1.0)); }

    // ---------- loading and sending ----------
    public static void Load(AHGame g, string id)
    {
        var p = g.player; var s = S(p);
        var st = s.load.Find(x => x.id == id);
        if (st != null) { s.load.Remove(st); return; }
        if (s.load.Count >= MaxStacks) { g.ui.Toast("A cart holds three kinds of goods. Take one off first."); return; }
        int n = p.OwnOf(id); if (n <= 0) return;
        s.load.Add(new AHStack { id = id, n = n });
    }

    public static void Send(AHGame g, Town t, bool guard)
    {
        var p = g.player; var s = S(p);
        if (s.runs.Count >= MaxRuns) { g.ui.Toast("Two caravans on the road at once. Wait for one to arrive."); return; }
        s.load.RemoveAll(x => p.OwnOf(x.id) <= 0);
        foreach (var x in s.load) x.n = Mathf.Min(x.n, p.OwnOf(x.id));
        if (s.load.Count == 0) { g.ui.Toast("Load some goods first."); return; }
        long worth = Worth(s.load), gc = guard ? GuardCost(p, worth) * AHDB.CU : 0;
        if (gc > p.bag.money) { g.ui.Toast("A guard costs " + AHItems.MoneyText(gc) + "."); return; }
        p.bag.money -= gc;
        float wantedShare = 0f; foreach (var x in s.load) if (Wanted(t, x.id)) wantedShare += AHComp.Value(x.id) * x.n;
        float mult = 1f + (Mult(t) - 1f) * (worth > 0 ? wantedShare / worth : 0f) + 0.1f;   // the road always pays a little (+10%)
        var run = new AHCaravanRun { town = t.id, start = NowS, end = NowS + t.mins * 60L, worth = worth, mult = mult, guard = guard, kills0 = Kills(p) };
        foreach (var x in s.load) { p.bag.Take(x.id, x.n); run.cargo.Add(new AHStack { id = x.id, n = x.n }); }
        s.load.Clear(); s.runs.Add(run); s.sent++;
        g.ui.Toast("The caravan rolls out for " + t.name + ". " + (guard ? "A guard rides with it." : "Clear " + ClearKills + " monsters while it travels to keep the road safe."), 4f);
        p.bag.Touch(); g.MarkDirty();
    }

    public static int Cleared(AHPlayer p, AHCaravanRun r) { return (int)Math.Min(ClearKills, Kills(p) - r.kills0); }

    // ---------- guard contracts ----------
    static AHGuardJob NewJob(AHPlayer p)
    {
        var t = Towns[UnityEngine.Random.Range(0, Towns.Length)];
        int need = Mathf.Clamp(6 + t.mins / 15, 6, 16);
        return new AHGuardJob { who = Who[UnityEngine.Random.Range(0, Who.Length)], town = t.id, need = need, pay = (long)Math.Round(need * (12 + p.level * 3.0)), start = 0, end = 0 };
    }
    public static void TakeJob(AHGame g, AHGuardJob j)
    {
        var p = g.player; if (j.taken) return;
        if (S(p).jobs.Exists(x => x.taken)) { g.ui.Toast("One guard contract at a time."); return; }
        var t = TownOf(j.town); j.taken = true; j.start = NowS; j.end = NowS + (t != null ? t.mins : 30) * 60L; j.kills0 = Kills(p);
        g.ui.Toast("You ride with " + j.who + "’s caravan to " + (t != null ? t.name : j.town) + ". Defeat " + j.need + " monsters before it arrives.", 4f);
        g.MarkDirty();
    }
    public static int JobProg(AHPlayer p, AHGuardJob j) { return j.taken ? (int)Math.Min(j.need, Kills(p) - j.kills0) : 0; }
    public static long JobPay(AHPlayer p, AHGuardJob j) { return p.path == "adventurer" ? (long)Math.Round(j.pay * 1.25) : j.pay; }

    // ---------- arrivals ----------
    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        var s = S(p); long now = NowS; bool changed = false;
        for (int i = 0; i < s.runs.Count; i++)
        {
            var r = s.runs[i]; if (now < r.end) continue;
            s.runs.RemoveAt(i); i--; changed = true;
            var t = TownOf(r.town); string tn = t != null ? t.name : r.town;
            bool safe = Cleared(p, r) >= ClearKills;
            float risk = safe ? 0f : r.guard ? GuardRisk : Risk;
            var rnd = new System.Random((int)(r.start ^ r.worth));
            bool hit = rnd.NextDouble() < risk;
            long pay = (long)Math.Round(r.worth * r.mult * (hit ? 1f - Loss : 1f));
            p.AddMoney(pay * AHDB.CU, p.transform.position); s.earned += pay;
            foreach (var c in r.cargo) AHArtisan.OnSoldAbroad(g, c.id, c.n);
            g.ui.Banner("Caravan arrived in " + tn, (hit ? "Bandits took a third, but it sold for " : "Sold for ") + AHItems.MoneyText(pay * AHDB.CU));
            if (safe) g.ui.Toast("You cleared the road: not a bandit in sight.", 3f);
        }
        // contracts on offer: three; a taken one ends when its caravan arrives
        for (int i = 0; i < s.jobs.Count; i++)
        {
            var j = s.jobs[i]; if (!j.taken || now < j.end) continue;
            s.jobs.RemoveAt(i); i--; changed = true;
            var t = TownOf(j.town); string tn = t != null ? t.name : j.town;
            if (JobProg(p, j) >= j.need)
            {
                long pay = JobPay(p, j); p.AddMoney(pay * AHDB.CU, p.transform.position); s.guarded++;
                int ren = p.path == "adventurer" ? 3 : 2; AHRequests.State(p).renown += ren;
                p.GainClassXpDirect(Mathf.RoundToInt(pay * 0.8f));
                g.ui.Banner(j.who + "’s caravan is safe in " + tn, "You are paid " + AHItems.MoneyText(pay * AHDB.CU) + " · +" + ren + " renown");
            }
            else g.ui.Toast(j.who + "’s caravan reached " + tn + ", but bandits got to it. No pay this time.", 4f);
        }
        while (s.jobs.FindAll(x => !x.taken).Count < 3) { s.jobs.Add(NewJob(p)); changed = true; }
        if (changed) g.MarkDirty();
    }
}

public partial class AHUI
{
    string cvTab = "send", cvTown;
    public void OpenCaravans() { wkMode = "caravan"; cvTab = "send"; cvTown = null; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderCaravans(AHPlayer p)
    {
        AHCaravan.Tick(g);
        var s = AHCaravan.S(p); long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rows = new List<Action<int>>();
        wkTitle.text = cvTab == "send" ? "Caravans · send goods" : "Caravans · guard contracts";
        wkHint.text = cvTab == "send"
            ? "Load up to three kinds of goods you gathered or made and send them where they are wanted. Bandits may strike: hire a guard" + (p.path == "artisan" ? " (half price for Artisans)" : "") + ", or clear " + AHCaravan.ClearKills + " monsters while it travels."
            : "Other heroes' caravans need guards. Defeat the monsters before the caravan arrives to be paid" + (p.path == "adventurer" ? " (Adventurers get 25% more)." : ".");
        bool loading = cvTab == "send" && cvTown != null;
        if (!loading) rows.Add(r => Row(r, "", Color.white, "", "",
            new WkBtn { label = "Send goods", on = true, col = cvTab == "send" ? Go : Plain, act = () => { cvTab = "send"; cvTown = null; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Guard", on = true, col = cvTab == "guard" ? Go : Plain, act = () => { cvTab = "guard"; wkPageI = 0; RenderWork(); } }));
        if (cvTab == "send")
        {
            foreach (var run in s.runs)
            {
                if (loading) break;
                var rr = run; var t = AHCaravan.TownOf(run.town);
                rows.Add(r => Row(r, "On the road to " + (t != null ? t.name : rr.town), new Color(1f, 0.8f, 0.45f),
                    "Arrives in " + AHExpeditions.LeftText(Math.Max(0, rr.end - now)) + " · worth " + AHItems.MoneyText(rr.worth * AHDB.CU) + " · pays about " + AHItems.MoneyText((long)(rr.worth * rr.mult) * AHDB.CU),
                    rr.guard ? "A guard rides with it" : "Road cleared: " + AHCaravan.Cleared(p, rr) + " / " + AHCaravan.ClearKills + " monsters"));
            }
            var cv = cvTown != null ? AHCaravan.TownOf(cvTown) : null;
            if (cv == null)
            {
                foreach (var t in AHCaravan.Towns)
                {
                    var tt = t;
                    rows.Add(r => Row(r, tt.name, Color.white, "Short of " + AHCaravan.WantName(tt) + " today: pays +" + Mathf.RoundToInt((AHCaravan.Mult(tt) - 1f) * 100f) + "% for them", tt.mins + " minutes on the road",
                        new WkBtn { label = "Load", on = s.runs.Count < AHCaravan.MaxRuns, col = Go, act = () => { cvTown = tt.id; wkPageI = 0; RenderWork(); } }));
                }
            }
            else
            {
                long worth = AHCaravan.Worth(s.load); long gc = AHCaravan.GuardCost(p, worth) * AHDB.CU;
                var names = new List<string>(); foreach (var x in s.load) names.Add(x.n + "× " + AHRequests.ItemName(x.id));
                Action<int> head = r => Row(r, "To " + cv.name + " · short of " + AHCaravan.WantName(cv), new Color(1f, 0.8f, 0.45f), names.Count > 0 ? string.Join(", ", names.ToArray()) : "Load goods below", "Worth " + AHItems.MoneyText(worth * AHDB.CU),
                    new WkBtn { label = "Send", on = s.load.Count > 0, col = Go, act = () => { AHCaravan.Send(g, cv, false); cvTown = null; RenderWork(); } },
                    new WkBtn { label = "+ guard " + AHItems.MoneyText(gc, 1), on = s.load.Count > 0 && p.bag.money >= gc, col = Plain, act = () => { AHCaravan.Send(g, cv, true); cvTown = null; RenderWork(); } },
                    new WkBtn { label = "Back", on = true, col = Plain, act = () => { cvTown = null; wkPageI = 0; RenderWork(); } });
                // goods you can load: what you gathered or made yourself, most valuable first
                var ids = new List<string>(); foreach (var kv in p.bag.inv) if (kv.Value > 0 && p.OwnOf(kv.Key) > 0 && AHItems.Get(kv.Key) != null && !AHItems.Get(kv.Key).cosmetic && AHAuction.Allowed(kv.Key) && !p.bag.gear.ContainsValue(kv.Key)) ids.Add(kv.Key);   // trade goods only (what the auction takes): no scrolls, maps or keys
                ids.Sort((a, b) => (AHComp.Value(b) * p.OwnOf(b)).CompareTo(AHComp.Value(a) * p.OwnOf(a)));
                // the Send row heads every page, so the cart can be sent from any page of goods
                int gi = 0;
                if (ids.Count == 0) rows.Add(head);
                foreach (var id in ids)
                {
                    if (gi++ % (RowsPerPage - 1) == 0) rows.Add(head);
                    string ii = id; bool on = s.load.Exists(x => x.id == ii); bool want = AHCaravan.Wanted(cv, ii);
                    rows.Add(r => Row(r, AHRequests.ItemName(ii) + (want ? "  <color=#9be37a>wanted</color>" : "") + (on ? "  <color=#ffd86a>loaded</color>" : ""), Color.white,
                        p.OwnOf(ii) + " yours · worth " + AHItems.MoneyText(AHComp.Value(ii) * p.OwnOf(ii) * AHDB.CU), "",
                        new WkBtn { label = on ? "Unload" : "Load", on = on || s.load.Count < AHCaravan.MaxStacks, col = on ? Plain : Go, act = () => { AHCaravan.Load(g, ii); RenderWork(); } }));
                }
            }
        }
        else
        {
            foreach (var j in s.jobs)
            {
                var jj = j; var t = AHCaravan.TownOf(j.town); string tn = t != null ? t.name : j.town;
                rows.Add(r => Row(r, jj.who + "’s caravan to " + tn, jj.taken ? new Color(1f, 0.8f, 0.45f) : Color.white,
                    jj.taken ? "Defeated " + AHCaravan.JobProg(p, jj) + " / " + jj.need + " · arrives in " + AHExpeditions.LeftText(Math.Max(0, jj.end - now)) : "Defeat " + jj.need + " monsters in " + (t != null ? t.mins : 30) + " minutes",
                    "Pays " + AHItems.MoneyText(AHCaravan.JobPay(p, jj) * AHDB.CU) + (p.path == "adventurer" ? " <color=#c9e0a0>(+25% for your path)</color>" : "") + " · renown",
                    jj.taken ? null : new WkBtn { label = "Take", on = !s.jobs.Exists(x => x.taken), col = Go, act = () => { AHCaravan.TakeJob(g, jj); RenderWork(); } }));
            }
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
