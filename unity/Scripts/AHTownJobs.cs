// Ashen Hollow: the town jobs board in Hollow Meadow. Three small jobs a day for the townsfolk: bring them what they
// need (only things you gathered or hunted yourself count, nothing bought), or clear the beasts troubling the farms.
// Each pays coin and a little experience, a bit more as you grow. New jobs every day.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHTownJobs
{
    public class Job { public string id, kind, item, giver, en; public int n; public long pay; public int xp; }
    static readonly Job[] Pool =
    {
        new Job { id = "logs", kind = "bring", item = "logs", n = 6, pay = 60, xp = 40, giver = "Woodcarver Ansel", en = "Bring 6 logs for the carver's workbench" },
        new Job { id = "copper", kind = "bring", item = "copper_ore", n = 5, pay = 70, xp = 45, giver = "Smith Brenna", en = "Bring 5 copper ore to the forge" },
        new Job { id = "tin", kind = "bring", item = "tin_ore", n = 5, pay = 70, xp = 45, giver = "Smith Brenna", en = "Bring 5 tin ore to the forge" },
        new Job { id = "trout", kind = "bring", item = "raw_trout", n = 3, pay = 65, xp = 40, giver = "Cook Hobb", en = "Bring 3 raw trout for the kitchen's stew" },
        new Job { id = "hide", kind = "bring", item = "boar_hide", n = 2, pay = 75, xp = 50, giver = "Tanner Rolf", en = "Bring 2 boar hides to the tannery" },
        new Job { id = "sunpetal", kind = "bring", item = "sunpetal", n = 4, pay = 65, xp = 45, giver = "Alchemist Wyn", en = "Bring 4 sunpetals to the apothecary" },
        new Job { id = "boars", kind = "slay", item = "boar", n = 5, pay = 80, xp = 60, giver = "Farmhand Kessa", en = "Drive off 5 boars raiding the farms" },
        new Job { id = "wolves", kind = "slay", item = "wolf", n = 3, pay = 95, xp = 75, giver = "Town guard Aldric", en = "Hunt 3 wolves stalking the roads" },
    };

    static string Today { get { return DateTime.Now.ToString("yyyy-MM-dd"); } }
    // today's three, the same all day
    public static List<Job> Todays()
    {
        var list = new List<Job>(Pool); var r = new System.Random(Today.GetHashCode() ^ 0x5bd1);
        for (int i = list.Count - 1; i > 0; i--) { int j = r.Next(i + 1); var t = list[i]; list[i] = list[j]; list[j] = t; }
        return list.GetRange(0, 3);
    }
    static void Fresh() { if (AHPrefs.GetString("ah_tj_day", "") != Today) { AHPrefs.SetString("ah_tj_day", Today); AHPrefs.SetString("ah_tj_done", ""); foreach (var j in Pool) AHPrefs.SetString("ah_tj_take_" + j.id, ""); } }
    public static bool Done(Job j) { Fresh(); return ("," + AHPrefs.GetString("ah_tj_done", "") + ",").Contains("," + j.id + ","); }
    public static bool Taken(Job j) { Fresh(); return j.kind != "slay" || AHPrefs.GetString("ah_tj_take_" + j.id, "") != ""; }
    public static int Progress(AHPlayer p, Job j)
    {
        if (j.kind == "slay")
        {
            string b = AHPrefs.GetString("ah_tj_take_" + j.id, ""); long b0; if (!long.TryParse(b, out b0)) return 0;
            return (int)Math.Min(j.n, AHProgress.Get(p.prog.kills, j.item) - b0);
        }
        return Mathf.Min(j.n, p.OwnOf(j.item));
    }
    public static long Pay(AHPlayer p, Job j) { return (long)Math.Round(j.pay * (1.0 + p.level * 0.08)) * AHDB.CU; }
    public static int Xp(AHPlayer p, Job j) { return Mathf.RoundToInt(j.xp * (1f + p.level * 0.1f)); }
    public static void Take(AHGame g, Job j)
    {
        Fresh(); if (j.kind != "slay" || Taken(j)) return;
        AHPrefs.SetString("ah_tj_take_" + j.id, AHProgress.Get(g.player.prog.kills, j.item).ToString());
        g.ui.Toast(j.giver + ": “Thank you! Come back to the board when it's done.”", 3f);
    }
    public static void HandIn(AHGame g, Job j)
    {
        var p = g.player; if (Done(j) || !Taken(j) || Progress(p, j) < j.n) return;
        if (j.kind == "bring") p.bag.Take(j.item, j.n);
        long pay = Pay(p, j); p.AddMoney(pay, p.transform.position); p.GainClassXpDirect(Xp(p, j));
        AHPrefs.SetString("ah_tj_done", AHPrefs.GetString("ah_tj_done", "") + "," + j.id);
        AHSound.Play("coin");
        g.ui.Banner("Job done", j.giver + " pays you " + AHItems.MoneyText(pay));
        p.bag.Touch(); g.MarkDirty();
    }
    public static string ItemName(string id) { var it = AHItems.Get(id); return it != null ? it.name : id; }
}

public partial class AHUI
{
    public void OpenJobs() { if (g.player.mounted) AHComp.Dismount(g, true); wkMode = "jobs"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderJobs(AHPlayer p)
    {
        wkTitle.text = "Town jobs";
        wkHint.text = "Small jobs for the people of Hollow Meadow. Three new ones every day. Only things you gathered or hunted yourself count.";
        var rows = new List<Action<int>>();
        foreach (var j in AHTownJobs.Todays())
        {
            var jj = j; bool done = AHTownJobs.Done(j), taken = AHTownJobs.Taken(j); int have = AHTownJobs.Progress(p, j);
            string prog = done ? "Done" : !taken ? "" : Bad(have + " / " + j.n, have >= j.n);
            WkBtn btn = done ? new WkBtn { label = "Done", on = false, col = Plain, act = () => { } }
                : !taken ? new WkBtn { label = "Take", on = true, col = Plain, act = () => { AHTownJobs.Take(g, jj); RenderWork(); } }
                : new WkBtn { label = "Hand in", on = have >= j.n, col = Go, act = () => { AHTownJobs.HandIn(g, jj); RenderWork(); } };
            rows.Add(sl => Row(sl, jj.en, done ? new Color(1f, 1f, 1f, 0.55f) : new Color(0.62f, 0.88f, 0.48f),
                "For " + jj.giver + (prog.Length > 0 ? " · " + prog : ""),
                "Pays " + AHItems.MoneyText(AHTownJobs.Pay(p, jj)) + " · +" + AHTownJobs.Xp(p, jj) + " xp", btn));
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
