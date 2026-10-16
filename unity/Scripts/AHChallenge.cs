// Ashen Hollow: the weekly challenge. Each week (from Monday) has its own twist. Take the challenge, then clear any
// dungeon you are ready for while it is on: the twist holds inside the dungeon, and beating the boss counts the week as
// won. Winning gives Challenge marks and, over the weeks, looks of the Gauntlet (a weapon skin, a trail, a name colour):
// looks only, never power. One win counts per week; the twist turns off by itself when you leave the dungeon or when the
// week ends.
//   Swift foes: they move 35% faster.          Iron hides: they take 40% less damage.
//   Dry flasks: no potions or food inside.     Glass cannon: you hit 30% harder and take 60% more.
//   Frenzy: they hit 40% harder.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable] public class AHChallengeState { public string week = ""; public bool taken, won; public int wins, marks; }

public static class AHChallenge
{
    public class Twist { public string id, name, desc; }
    public static readonly Twist[] Twists =
    {
        new Twist { id = "swift", name = "Swift foes",   desc = "Foes move 35% faster" },
        new Twist { id = "iron",  name = "Iron hides",   desc = "Foes take 40% less damage" },
        new Twist { id = "dry",   name = "Dry flasks",   desc = "No potions or food inside" },
        new Twist { id = "glass", name = "Glass cannon", desc = "You hit 30% harder and take 60% more damage" },
        new Twist { id = "frenzy", name = "Frenzy",      desc = "Foes hit 40% harder" },
    };
    public static string WeekKey { get { var d = DateTime.UtcNow; int w = ISOWeek.GetWeekOfYear(d); return ISOWeek.GetYear(d) + "-W" + w; } }
    public static Twist ThisWeek { get { var d = DateTime.UtcNow; int w = ISOWeek.GetWeekOfYear(d) + ISOWeek.GetYear(d) * 53; return Twists[((w % Twists.Length) + Twists.Length) % Twists.Length]; } }

    public static AHChallengeState S(AHPlayer p)
    {
        if (p.prog.challenge == null) p.prog.challenge = new AHChallengeState();
        var s = p.prog.challenge; if (s.week != WeekKey) { s.week = WeekKey; s.taken = false; s.won = false; }
        return s;
    }
    public static int Wins(AHPlayer p) { return p == null || p.prog == null ? 0 : S(p).wins; }

    // the twist holds now: taken this week, not yet won, and you are inside a dungeon
    static string Live
    {
        get
        {
            var g = AHGame.I; var p = g != null ? g.player : null; if (p == null || p.prog == null) return null;
            var s = S(p); if (!s.taken || s.won || !AHDungeon.IsDungeon(AHGame.AreaId)) return null;
            return ThisWeek.id;
        }
    }
    public static float MobSpeedK() { return Live == "swift" ? 1.35f : 1f; }
    public static float MobTakenK() { return Live == "iron" ? 0.6f : 1f; }
    public static float YouDealK() { return Live == "glass" ? 1.3f : 1f; }
    public static float YouTakenK() { var l = Live; return l == "glass" ? 1.6f : l == "frenzy" ? 1.4f : 1f; }
    public static bool NoFlasks(AHGame g) { if (Live != "dry") return false; if (g != null && g.ui != null) g.ui.Toast("Dry flasks: no potions or food in this week's challenge."); return true; }

    public static void Take(AHGame g)
    {
        var p = g.player; if (p.path == "artisan") { g.ui.Toast("The weekly challenge is for Adventurers."); return; }
        var s = S(p); if (s.won) return;
        s.taken = !s.taken;
        g.ui.Toast(s.taken ? "Challenge on: " + ThisWeek.name + ". " + ThisWeek.desc + " in any dungeon until you beat its boss." : "Challenge off.", 4f);
        g.MarkDirty();
    }

    // from AHDungeon: a dungeon boss fell
    public static void OnBoss(AHGame g)
    {
        var p = g.player; if (p == null || Live == null) return;
        var s = S(p); s.won = true; s.wins++; s.marks += 3;
        var got = new List<string>();
        foreach (var d in AHLooks.All) if (d.how == "challenge" && s.wins >= d.n && !p.prog.looks.Contains(d.id)) { p.prog.looks.Add(d.id); got.Add(d.name); }
        g.ui.Banner("Weekly challenge won!", ThisWeek.name + " · 3 Challenge marks" + (got.Count > 0 ? " · new look: " + string.Join(", ", got.ToArray()) : ""));
        g.MarkDirty();
    }

    public static string Status(AHPlayer p)
    {
        var s = S(p);
        return ThisWeek.name + " · " + (s.won ? "won this week" : s.taken ? "on: clear a dungeon" : "not taken") + " · " + s.wins + " won";
    }
}

public partial class AHUI
{
    public void OpenChallenge() { wkMode = "challenge"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderChallenge(AHPlayer p)
    {
        var s = AHChallenge.S(p); var t = AHChallenge.ThisWeek; var rows = new List<Action<int>>();
        wkTitle.text = "Weekly challenge";
        wkHint.text = "A new twist every Monday. Take the challenge, then clear any dungeon with the twist on. Rewards are looks only. Challenge marks: " + s.marks + ".";
        rows.Add(sl => Row(sl, "This week: " + t.name, new Color(1f, 0.6f, 0.35f), t.desc + ", in any dungeon until you beat its boss", s.won ? "<color=#9be37a>Won this week</color>" : s.taken ? "On: go and clear a dungeon" : "",
            s.won ? null : new WkBtn { label = s.taken ? "Turn off" : "Take it", on = p.path != "artisan", col = s.taken ? Plain : Go, act = () => { AHChallenge.Take(g); RenderWork(); } }));
        foreach (var d in AHLooks.All)
        {
            if (d.how != "challenge") continue;
            var dd = d; bool own = p.prog.looks.Contains(d.id);
            rows.Add(sl => Row(sl, dd.name + " · " + (dd.kind == "skin" ? "weapon skin" : dd.kind == "trail" ? "trail" : dd.kind == "hit" ? "hit effect" : "name colour"), own ? new Color(0.62f, 0.9f, 0.55f) : dd.col,
                own ? "Yours · wear it in Menu → Looks" : "Win " + dd.n + " weekly challenge" + (dd.n > 1 ? "s" : "") + " (" + s.wins + " so far)", dd.desc));
        }
        rows.Add(sl => Row(sl, "The twists", new Color(1f, 1f, 1f, 0.7f), string.Join(" · ", Array.ConvertAll(AHChallenge.Twists, x => x.name)), "One each week, in turn"));
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
