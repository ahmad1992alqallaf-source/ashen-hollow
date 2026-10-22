// Ashen Hollow: the ranked arena. Arena master Varek in Hollow Meadow's Barracks & Arena sends you into the Proving
// Grounds against rival heroes of the realm: a one-on-one duel, or a team skirmish (you and your party against as many
// rivals). Each fight moves your arena rating (it starts at 1000; beating a stronger rival gains more), and the rating
// sets your rank: Bronze, Silver, Gold, Platinum, Diamond, Champion. A season lasts a calendar month; when a new one
// starts your rating is pulled halfway back to 1000 and your best rank of the last season pays a chest. The best rank
// you ever reach unlocks a title (Silver Gladiator ... Arena Champion). Rewards are coins, stones and titles only.
// (Until heroes can meet online, the rivals are the realm's own fighters, matched to your rating.)
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHRanked
{
    public static readonly int[] TierAt = { 0, 1100, 1250, 1400, 1550, 1700 };
    public static readonly string[] Tiers = { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Champion" };
    public static readonly Color[] TierCol = { new Color(0.8f, 0.55f, 0.35f), new Color(0.8f, 0.85f, 0.9f), new Color(1f, 0.8f, 0.3f), new Color(0.55f, 0.95f, 0.9f), new Color(0.6f, 0.75f, 1f), new Color(1f, 0.45f, 0.35f) };
    static readonly string[] Months = { "Frost", "Thaw", "Bloom", "Rain", "Sun", "Ember", "Harvest", "Ash", "Mist", "Gale", "Dusk", "Hearth" };

    public class Rival { public string name, cls; public int rating; }
    static readonly string[] Names = { "Kael Swift", "Brynja Ironhand", "Oren the Grey", "Sefa Nightbloom", "Dagny Stormborn", "Tarik Sand", "Lio Ashveil", "Mirela Frost", "Bram Oakshield", "Yusra Dawn", "Corvin Black", "Edda Quickstep", "Halvar Stone", "Nadia Ember", "Ivo Thorn", "Zara Moonglade" };
    static readonly string[] Classes = { "warrior", "mage", "rogue", "priest" };

    // ---------- your standing ----------
    static string SeasonId { get { return DateTime.Now.ToString("yyyy-MM"); } }
    public static string SeasonName { get { return "Season of " + Months[DateTime.Now.Month - 1] + " " + DateTime.Now.Year; } }
    public static int Rating { get { NewSeason(); return AHPrefs.GetInt("ah_rk_rating", 1000); } }
    public static int Wins { get { return AHPrefs.GetInt("ah_rk_w", 0); } }
    public static int Losses { get { return AHPrefs.GetInt("ah_rk_l", 0); } }
    public static int SeasonBest { get { return AHPrefs.GetInt("ah_rk_best", 1000); } }
    public static int Tier(int r) { int t = 0; for (int i = 0; i < TierAt.Length; i++) if (r >= TierAt[i]) t = i; return t; }
    public static string TierText(int r) { int t = Tier(r); return "<color=#" + ColorUtility.ToHtmlStringRGB(TierCol[t]) + ">" + Tiers[t] + "</color>"; }
    static void NewSeason()
    {
        string s = AHPrefs.GetString("ah_rk_season", "");
        if (s == SeasonId) return;
        if (s != "")
        {
            // last season's chest, by the best rank reached in it
            int best = AHPrefs.GetInt("ah_rk_best", 1000), t = Tier(best);
            AHPrefs.SetInt("ah_rk_chest", t + 1);
            int r = AHPrefs.GetInt("ah_rk_rating", 1000); AHPrefs.SetInt("ah_rk_rating", (r + 1000) / 2);
        }
        AHPrefs.SetString("ah_rk_season", SeasonId); AHPrefs.SetInt("ah_rk_w", 0); AHPrefs.SetInt("ah_rk_l", 0);
        AHPrefs.SetInt("ah_rk_best", AHPrefs.GetInt("ah_rk_rating", 1000));
    }
    public static int ChestTier { get { NewSeason(); return AHPrefs.GetInt("ah_rk_chest", 0) - 1; } }
    public static void ClaimChest(AHGame g)
    {
        int t = ChestTier; if (t < 0) return; var p = g.player; int L = Mathf.Max(1, p.level);
        long coins = (long)((80 + L * 30) * (1 + t)) * AHDB.CU; p.AddMoney(coins, p.transform.position);
        string got = AHItems.MoneyText(coins);
        if (t >= 1 && p.bag.Add("enh_stone", 2 + t * 2)) got += " · " + (2 + t * 2) + " enhancement stones";
        if (t >= 2 && p.bag.Add("mystery_sack", t - 1)) got += " · " + (t - 1) + " mystery sack" + (t > 2 ? "s" : "");
        if (t >= 4 && p.bag.Add("lucky_charm")) got += " · a Lucky charm";
        AHPrefs.SetInt("ah_rk_chest", 0);
        AHSound.Play("loot"); g.ui.Banner(Tiers[t] + " season chest", got); p.bag.Touch(); g.MarkDirty();
    }

    // today's challengers, near your rating
    public static List<Rival> Challengers()
    {
        int R = Rating; var rnd = new System.Random((DateTime.Now.ToString("yyyy-MM-dd") + R / 50).GetHashCode());
        var l = new List<Rival>(); int[] off = { -70, 10, 95 };
        var used = new HashSet<int>();
        for (int i = 0; i < 3; i++)
        {
            int n; do n = rnd.Next(Names.Length); while (used.Contains(n)); used.Add(n);
            l.Add(new Rival { name = Names[n], cls = Classes[rnd.Next(Classes.Length)], rating = Mathf.Max(800, R + off[i] + rnd.Next(-15, 16)) });
        }
        return l;
    }

    // ---------- a fight ----------
    static readonly List<AHMob> foes = new List<AHMob>();
    static float t; static bool over, started;
    static int vsRating; static string vsName;

    public static void Begin(AHGame g, Rival r, bool team)
    {
        var p = g.player; if (p.dead) return;
        if (p.level < 5) { g.ui.Toast("The ranked arena is for level 5 and up."); return; }
        if (team && p.party.Count == 0) { g.ui.Toast("A team skirmish needs at least one companion in your party."); return; }
        AHPrefs.SetString("ah_rk_vs", r.name + "|" + r.cls + "|" + r.rating);
        AHKQ.GoIn(g, team ? "team" : "duel");
    }

    static AHMobType Type(string cls, string name, int L, int rating, bool team)
    {
        string id = "rival_" + cls;
        var all = AHDB.Mobs;
        if (all != null && !all.ContainsKey(id)) { var c = new Dictionary<string, object>(); var bd = AHJson.O(AHDB.Mobs, "bandit") as Dictionary<string, object>; if (bd != null) foreach (var kv in bd) c[kv.Key] = kv.Value; c["name"] = name; c["drops"] = new List<object>(); c["noSkin"] = true; all[id] = c; }
        float k = Mathf.Clamp(1f + (rating - 1000) / 900f, 0.7f, 1.9f) * (team ? 0.75f : 1f);
        bool caster = cls == "mage" || cls == "priest";
        return new AHMobType { id = id, name = name, model = "Rogue_t", lvl = L, hp = Mathf.RoundToInt((160 + 10 * L) * k), dmg = Mathf.RoundToInt((3 + 0.6f * L) * k * (caster ? 1.15f : 1f)), xp = 20 + L * 10, gold = 0,
            speed = (cls == "rogue" ? 150 : 125) * AHDB.S, radius = 15 * AHDB.S, aggro = 900 * AHDB.S, atkCd = cls == "warrior" ? 1.5f : cls == "rogue" ? 1.0f : 1.3f, respawn = 1e9f, noSkin = true, lunge = cls == "rogue" };
    }

    public static void Setup(AHGame g)
    {
        foes.Clear(); t = 0f; over = false; started = false;
        var s = AHPrefs.GetString("ah_rk_vs", "").Split('|');
        vsName = s.Length > 0 && s[0] != "" ? s[0] : "a rival"; string cls = s.Length > 1 ? s[1] : "warrior"; int.TryParse(s.Length > 2 ? s[2] : "1000", out vsRating);
        foreach (var sp in AHGather.Spots) if (sp.kind == "gate" && sp.gate != null && sp.gate.label == "Leave the arena") sp.use = () => { if (!over) End(g, false, "You left the fight"); else AHKQ.Leave(g); };
        bool team = AHKQ.Mode == "team";
        int n = team ? Mathf.Clamp(1 + g.player.party.Count, 2, 3) : 1;
        var rnd = new System.Random(vsName.GetHashCode());
        for (int i = 0; i < n; i++)
        {
            string c = i == 0 ? cls : Classes[rnd.Next(Classes.Length)];
            string nm = i == 0 ? vsName : Names[rnd.Next(Names.Length)];
            var tp = Type(c, nm, g.player.level, vsRating, team);
            Vector3 at = g.Resolve(AHKQ.Spot(g, (i - (n - 1) / 2f) * 110f, -260f), tp.radius);
            var m = AHMob.Create(g, tp, at); m.add = true; g.mobs.Add(m); foes.Add(m);
            m.transform.rotation = Quaternion.LookRotation(Flat(g.player.transform.position - at));
        }
        g.ui.Banner(team ? "Team skirmish" : "Ranked duel", "vs " + vsName + " · " + TierText(vsRating) + " " + vsRating + " · get ready!");
    }
    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward; }

    public static void Tick(AHGame g, float dt)
    {
        t += dt; var p = g.player;
        if (!over)
        {
            if (!started && t > 3.5f) { started = true; foreach (var m in foes) if (m != null) m.Engage(); g.ui.Banner("Fight!", ""); AHSound.Play("roar"); }
            if (p.dead) { End(g, false, "You fell"); return; }
            bool all = foes.Count > 0; foreach (var m in foes) if (m != null && !m.dead) all = false;
            if (all) { End(g, true, "Victory!"); return; }
            if (t > 183.5f) { End(g, false, "Time is up"); return; }
        }
        g.ui.SetKQText(over ? "Ranked fight over" : !started ? "Ranked · " + Mathf.CeilToInt(3.5f - t) + "…" : "Ranked · vs " + vsName + " · " + AHEvents.Fmt(Mathf.Max(0f, 183.5f - t)));
    }

    static void End(AHGame g, bool won, string head)
    {
        if (over) return; over = true;
        var p = g.player; int R = Rating;
        float expect = 1f / (1f + Mathf.Pow(10f, (vsRating - R) / 400f));
        int delta = Mathf.RoundToInt(32f * ((won ? 1f : 0f) - expect)); if (won) delta = Mathf.Max(delta, 6); else delta = Mathf.Min(delta, -4);
        int nr = Mathf.Max(600, R + delta);
        AHPrefs.SetInt("ah_rk_rating", nr);
        AHPrefs.SetInt(won ? "ah_rk_w" : "ah_rk_l", (won ? Wins : Losses) + 1);
        if (nr > AHPrefs.GetInt("ah_rk_best", 1000)) AHPrefs.SetInt("ah_rk_best", nr);
        long peak = AHProgress.Get(p.prog.stats, "arena_peak"); if (peak < nr) p.prog.Bump("arena_peak", nr - peak);
        if (won) p.prog.Bump("arena_wins");
        int L = Mathf.Max(1, p.level);
        long coins = (long)(won ? 25 + L * 10 : 8 + L * 3) * AHDB.CU; p.AddMoney(coins, p.transform.position); p.GainClassXpDirect(won ? 60 + L * 15 : 20 + L * 5);
        bool up = Tier(nr) > Tier(R), down = Tier(nr) < Tier(R);
        g.ui.Banner(head, "Rating " + R + " → " + nr + " (" + (delta >= 0 ? "+" : "") + delta + ") · " + TierText(nr) + (up ? " · rank up!" : down ? "" : "") + " · " + AHItems.MoneyText(coins));
        if (up) { AHSound.Play("level"); AHSpark.Burst(p.transform.position + Vector3.up * 1.5f, TierCol[Tier(nr)], 40, 4f, 1.2f, 0.15f, 2f); }
        foreach (var m in foes) if (m != null && !m.dead) m.Hurt(999999, null, true);
        g.SaveProgress();
        g.StartCoroutine(BackLater(g, won ? 3.5f : 4f));
    }
    static System.Collections.IEnumerator BackLater(AHGame g, float s) { yield return new WaitForSeconds(s); if (AHGame.AreaId == AHKQ.Area) AHKQ.Leave(g); }
}

public partial class AHUI
{
    public void OpenRanked() { if (g.player.mounted) AHComp.Dismount(g, true); wkMode = "ranked"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderRanked(AHPlayer p)
    {
        int R = AHRanked.Rating;
        wkTitle.text = "Ranked arena";
        wkHint.text = AHRanked.SeasonName + " · your rating " + R + " · " + AHRanked.TierText(R) + " · " + AHRanked.Wins + " won, " + AHRanked.Losses + " lost this season · season best " + AHRanked.SeasonBest;
        var rows = new List<Action<int>>();
        if (AHRanked.ChestTier >= 0)
            rows.Add(sl => Row(sl, "Last season's chest: " + AHRanked.Tiers[AHRanked.ChestTier], AHRanked.TierCol[AHRanked.ChestTier], "Your best rank last season pays out.", "", new WkBtn { label = "Claim", on = true, col = Go, act = () => { AHRanked.ClaimChest(g); RenderWork(); } }));
        foreach (var r in AHRanked.Challengers())
        {
            var rr = r;
            rows.Add(sl => Row(sl, rr.name + " · " + char.ToUpper(rr.cls[0]) + rr.cls.Substring(1), Color.white, AHRanked.TierText(rr.rating) + " · rating " + rr.rating, "1 vs 1 duel · three minutes",
                new WkBtn { label = "Duel", on = !p.dead, col = Go, act = () => { ShowWork(false); AHRanked.Begin(g, rr, false); } }));
        }
        var lead = AHRanked.Challengers()[1];
        rows.Add(sl => Row(sl, "Team skirmish", new Color(0.56f, 0.85f, 1f), p.party.Count > 0 ? "You and your party against " + Mathf.Clamp(1 + p.party.Count, 2, 3) + " rivals led by " + lead.name : "Take at least one companion in your party", "",
            new WkBtn { label = "Fight", on = p.party.Count > 0 && !p.dead, col = Go, act = () => { ShowWork(false); AHRanked.Begin(g, lead, true); } }));
        rows.Add(sl => Row(sl, "Ranks", new Color(1f, 1f, 1f, 0.8f), "Silver 1100 · Gold 1250 · Platinum 1400 · Diamond 1550 · Champion 1700", "Your best rank ever unlocks its title; each season's best rank pays a chest."));
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
