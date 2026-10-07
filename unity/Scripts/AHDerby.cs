// Ashen Hollow: the weekly fishing derby. Every fish you catch has a weight; your heaviest catch of the week goes on the
// derby board against the Hollow's anglers. Beat the week's leader and the derby pays a prize (once a week). The board
// is in MENU → Fishing derby, and each new personal best is called out in the chat.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHDerby
{
    static string Week() { var d = DateTime.Now.Date; int back = ((int)d.DayOfWeek + 6) % 7; return d.AddDays(-back).ToString("yyyy-MM-dd"); }
    const string BestKey = "ah_derby_best", WeekKey = "ah_derby_week", FishKey = "ah_derby_fish", PrizeKey = "ah_derby_prize";
    static readonly Dictionary<string, Vector2> Kg = new Dictionary<string, Vector2>
    {
        { "raw_trout", new Vector2(0.4f, 2.2f) }, { "raw_eel", new Vector2(0.8f, 3.5f) }, { "raw_salmon", new Vector2(1.5f, 6f) },
        { "raw_tuna", new Vector2(6f, 30f) }, { "raw_swordfish", new Vector2(15f, 60f) }, { "raw_lavaeel", new Vector2(3f, 14f) },
    };
    public static readonly string[] Rivals = { "Old Bertram", "Fisher Lisbet", "Silent Odo", "Captain Maren", "Little Tess" };
    // the anglers' catches change every week, seeded by the week
    public static float RivalKg(int i) { var r = new System.Random(Week().GetHashCode() + i * 977); float[] cap = { 9f, 7f, 5.5f, 4f, 1.2f }; return (float)Math.Round(cap[i] * (0.55 + r.NextDouble() * 0.45), 2); }
    static void Roll() { if (AHPrefs.GetString(WeekKey, "") != Week()) { AHPrefs.SetString(WeekKey, Week()); AHPrefs.SetFloat(BestKey, 0f); AHPrefs.SetString(FishKey, ""); } }
    public static float Best { get { Roll(); return AHPrefs.GetFloat(BestKey, 0f); } }
    public static string BestFish { get { Roll(); return AHPrefs.GetString(FishKey, ""); } }
    public static bool Prized { get { return AHPrefs.GetString(PrizeKey, "") == Week(); } }

    public static void OnCatch(AHGame g, string fish)
    {
        Vector2 k; if (!Kg.TryGetValue(fish, out k)) return;
        float r = UnityEngine.Random.value; float kg = (float)Math.Round(Mathf.Lerp(k.x, k.y, r * r * r), 2);   // big ones are rare
        var it = AHItems.Get(fish); string nm = it != null ? it.name.Replace("Raw ", "").ToLowerInvariant() : fish;
        if (kg <= Best) { AHChat.Add("system", "You caught a " + kg.ToString("0.00") + " kg " + nm + "."); return; }
        AHPrefs.SetFloat(BestKey, kg); AHPrefs.SetString(FishKey, nm);
        AHChat.Add("system", "Derby: a new best this week, a " + kg.ToString("0.00") + " kg " + nm + "!");
        float lead = 0f; for (int i = 0; i < Rivals.Length; i++) lead = Mathf.Max(lead, RivalKg(i));
        if (kg > lead && !Prized)
        {
            AHPrefs.SetString(PrizeKey, Week());
            var p = g.player; int L = Mathf.Max(1, p.level);
            p.AddMoney((long)(L * 120) * AHDB.CU, p.transform.position);
            if (AHItems.Get("mystery_sack") != null) p.bag.Add("mystery_sack");
            g.ui.Banner("Derby champion!", "Your " + kg.ToString("0.00") + " kg " + nm + " leads the week · prize paid");
        }
        else g.ui.Toast("Derby: new personal best, " + kg.ToString("0.00") + " kg!", 3f);
    }
}

public partial class AHUI
{
    public void OpenDerby() { wkMode = "derby"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderDerby(AHPlayer p)
    {
        wkTitle.text = "Fishing derby";
        wkHint.text = "Your heaviest catch this week against the Hollow's anglers. Beat the leader for the weekly prize" + (AHDerby.Prized ? " (already won this week)." : ".");
        var list = new List<KeyValuePair<string, float>>();
        for (int i = 0; i < AHDerby.Rivals.Length; i++) list.Add(new KeyValuePair<string, float>(AHDerby.Rivals[i], AHDerby.RivalKg(i)));
        if (AHDerby.Best > 0f) list.Add(new KeyValuePair<string, float>("\u0001" + (string.IsNullOrEmpty(p.heroName) ? "You" : p.heroName), AHDerby.Best));
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        var rows = new List<Action<int>>();
        for (int i = 0; i < list.Count; i++)
        {
            bool mine = list[i].Key.Length > 0 && list[i].Key[0] == (char)1; string nm = mine ? list[i].Key.Substring(1) : list[i].Key; int rank = i + 1; float kg = list[i].Value;
            rows.Add(s => Row(s, "#" + rank + "  " + nm + (mine ? "  (you)" : ""), mine ? new Color(1f, 0.82f, 0.4f) : new Color(0.62f, 0.85f, 1f), kg.ToString("0.00") + " kg" + (mine ? " · " + AHDerby.BestFish : ""), ""));
        }
        if (AHDerby.Best <= 0f) rows.Add(s => Row(s, "No catch yet this week", new Color(1f, 1f, 1f, 0.6f), "Fish anywhere: every catch is weighed.", ""));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
