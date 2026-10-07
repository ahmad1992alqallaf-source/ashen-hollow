// Ashen Hollow: the weekly cook-off, beside the fishing derby. Every dish you cook well scores points (bigger fish are
// worth more) and a run of dishes without burning one adds a streak bonus; a burnt dish ends the streak. Your week's
// score goes on the board against the Hollow's cooks. Beat the leader for the weekly prize. MENU → Cook-off.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHCookOff
{
    static string Week() { var d = DateTime.Now.Date; int back = ((int)d.DayOfWeek + 6) % 7; return d.AddDays(-back).ToString("yyyy-MM-dd"); }
    const string ScoreKey = "ah_cook_score", WeekKey = "ah_cook_week", StreakKey = "ah_cook_streak", PrizeKey = "ah_cook_prize";
    static readonly Dictionary<string, int> Pts = new Dictionary<string, int>
    {
        { "meat", 2 }, { "trout", 3 }, { "eel", 4 }, { "salmon", 5 }, { "tuna", 8 }, { "lavaeel", 10 }, { "swordfish", 12 },
    };
    public static readonly string[] Rivals = { "Baker Maudie", "Chef Rosamund", "Innkeeper Hob", "Granny Wren", "Little Tess" };
    public static int RivalScore(int i) { var r = new System.Random(Week().GetHashCode() + i * 613); int[] cap = { 260, 210, 160, 110, 40 }; return (int)(cap[i] * (0.55 + r.NextDouble() * 0.45)); }
    static void Roll() { if (AHPrefs.GetString(WeekKey, "") != Week()) { AHPrefs.SetString(WeekKey, Week()); AHPrefs.SetInt(ScoreKey, 0); AHPrefs.SetInt(StreakKey, 0); } }
    public static int Score { get { Roll(); return AHPrefs.GetInt(ScoreKey, 0); } }
    public static int Streak { get { Roll(); return AHPrefs.GetInt(StreakKey, 0); } }
    public static bool Prized { get { return AHPrefs.GetString(PrizeKey, "") == Week(); } }
    public static int Leader { get { int m = 0; for (int i = 0; i < Rivals.Length; i++) m = Mathf.Max(m, RivalScore(i)); return m; } }

    // editor test: cook swordfish (no fish used up) until you pass the week's leader
    public static void TestWin(AHGame g) { for (int i = 0; i < 400 && Score <= Leader; i++) OnCook(g, "swordfish", false); }
    public static void OnCook(AHGame g, string dish, bool burnt)
    {
        Roll();
        if (burnt) { if (Streak >= 3) AHChat.Add("system", "Cook-off: burnt! Your streak of " + Streak + " ends."); AHPrefs.SetInt(StreakKey, 0); return; }
        int pts; if (!Pts.TryGetValue(dish, out pts)) pts = 2;
        int st = Streak + 1; AHPrefs.SetInt(StreakKey, st);
        int bonus = Mathf.Min(5, st / 5);   // +1 a dish for every 5 in a row, up to +5
        int before = Score, now = before + pts + bonus; AHPrefs.SetInt(ScoreKey, now);
        if (st % 10 == 0) AHChat.Add("system", "Cook-off: " + st + " dishes without burning one! +" + bonus + " a dish.");
        if (now > Leader && before <= Leader && !Prized)
        {
            AHPrefs.SetString(PrizeKey, Week());
            var p = g.player; int L = Mathf.Max(1, p.level);
            p.AddMoney((long)(L * 120) * AHDB.CU, p.transform.position);
            if (AHItems.Get("mystery_sack") != null) p.bag.Add("mystery_sack");
            g.ui.Banner("Cook-off champion!", now + " points leads the week · prize paid");
        }
    }
}

public partial class AHUI
{
    public void OpenCookOff() { wkMode = "cookoff"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderCookOff(AHPlayer p)
    {
        wkTitle.text = "Cook-off";
        wkHint.text = "Every dish you cook well scores (bigger fish score more); 5 in a row without burning adds +1 a dish. Beat the leader for the weekly prize" + (AHCookOff.Prized ? " (already won this week)." : ".");
        var list = new List<KeyValuePair<string, int>>();
        for (int i = 0; i < AHCookOff.Rivals.Length; i++) list.Add(new KeyValuePair<string, int>(AHCookOff.Rivals[i], AHCookOff.RivalScore(i)));
        list.Add(new KeyValuePair<string, int>("\u0001" + (string.IsNullOrEmpty(p.heroName) ? "You" : p.heroName), AHCookOff.Score));
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        var rows = new List<Action<int>>();
        for (int i = 0; i < list.Count; i++)
        {
            bool mine = list[i].Key.Length > 0 && list[i].Key[0] == (char)1; string nm = mine ? list[i].Key.Substring(1) : list[i].Key; int rank = i + 1, sc = list[i].Value;
            rows.Add(s => Row(s, "#" + rank + "  " + nm + (mine ? "  (you)" : ""), mine ? new Color(1f, 0.82f, 0.4f) : new Color(1f, 0.7f, 0.45f), sc + " points" + (mine ? " · streak " + AHCookOff.Streak : ""), ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
