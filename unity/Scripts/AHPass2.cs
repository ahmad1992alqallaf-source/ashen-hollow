// Ashen Hollow: the Battle Pass, two lines side by side for a month at a time.
//  - Fight line: filled by what you defeat (more for elites, rare beasts and bosses).
//  - Craft line: filled by gathering, fishing and making things (more for better grades).
// Thirty tiers on each. Every tier has a free reward (money, sacks, materials, lures, recipe scrolls: things the world
// gives anyway). The Premium pass (bought once a month with Crowns in the store) adds a second reward on each tier,
// and those are looks only (weapon skins, trails, hit effects, name colours) or a few Crowns back. Never power.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHPass2
{
    public const int Tiers = 30, TierXp = 150;
    public const long PremiumPrice = 950;

    static string Key() { return DateTime.UtcNow.ToString("yyyy-MM"); }
    static void Fresh(AHPlayer p)
    {
        var s = p.prog; string k = Key();
        if (s.passKey == k) return;
        s.passKey = k; s.passXp = 0; s.passXpC = 0; s.passClaimedF = 0; s.passClaimedC = 0; s.passPremF = 0; s.passPremC = 0; s.passPremium = 0;
    }
    public static string MonthName { get { return DateTime.UtcNow.ToString("MMMM"); } }
    public static int DaysLeft { get { var n = DateTime.UtcNow; return DateTime.DaysInMonth(n.Year, n.Month) - n.Day + 1; } }

    public static int Xp(AHPlayer p, bool craft) { Fresh(p); return craft ? p.prog.passXpC : p.prog.passXp; }
    public static int Tier(AHPlayer p, bool craft) { return Mathf.Min(Tiers, Xp(p, craft) / TierXp); }
    public static bool Premium(AHPlayer p) { Fresh(p); return p.prog.passPremium == 1; }

    static void Add(AHPlayer p, bool craft, int n)
    {
        if (p == null || n <= 0 || AHTraining.NoXp) return;
        Fresh(p); int before = Tier(p, craft);
        if (craft) p.prog.passXpC += n; else p.prog.passXp += n;
        int after = Tier(p, craft);
        if (after > before && AHGame.I != null && AHGame.I.ui != null) AHGame.I.ui.Toast("Battle Pass · " + (craft ? "Craft" : "Fight") + " tier " + after + " reached. Claim it in Menu → Battle Pass.", 3f);
    }
    public static void OnKill(AHPlayer p, AHMob m)
    {
        if (m == null || m.type == null) return;
        bool boss = AHJson.B(AHJson.O(AHDB.Mobs, m.type.id), "boss") || AHDeep.IsBoss(m) || AHStory.IsBoss(m);
        Add(p, false, boss ? 60 : m.type.elite ? 15 : m.type.rare ? 20 : 4);
    }
    // a gathering or crafting action (grade: the grade of what came out)
    public static void OnWork(AHPlayer p, int grade = 0) { Add(p, true, 4 + grade * 4); }

    // ---------- rewards ----------
    public static AHDaily.Reward Free(AHPlayer p, bool craft, int tier)
    {
        var r = new AHDaily.Reward { money = (long)Math.Round(60 * (1 + p.level / 6.0) * (1 + tier / 12.0)) };
        if (!craft)
        {
            if (tier % 10 == 0) r.items.Add(new KeyValuePair<string, int>(AHRareRecipes.ScrollId, 1));
            else if (tier % 5 == 0) r.items.Add(new KeyValuePair<string, int>("mystery_sack", 2));
            else if (tier % 3 == 0) r.items.Add(new KeyValuePair<string, int>("big_potion", 2));
        }
        else
        {
            if (tier % 10 == 0) r.items.Add(new KeyValuePair<string, int>(AHRareRecipes.ScrollId, 1));
            else if (tier % 5 == 0) r.items.Add(new KeyValuePair<string, int>(tier >= 20 ? "iron_bar_g2" : "bronze_bar_g2", 3));
            else if (tier % 3 == 0) r.items.Add(new KeyValuePair<string, int>("fishing_lure", 2));
            else if (tier % 2 == 0) r.items.Add(new KeyValuePair<string, int>(tier >= 15 ? "iron_ore_g1" : "copper_ore_g1", 4));
        }
        r.items.RemoveAll(kv => AHItems.Get(kv.Key) == null);
        return r;
    }
    // premium: a look every fifth tier (one you don't own yet, if any are left), else a few Crowns
    public static string PremiumLook(AHPlayer p, bool craft, int tier)
    {
        if (tier % 5 != 0) return null;
        var pool = AHLooks.All.FindAll(l => l.how == "store" || l.how == "pass");
        if (pool.Count == 0) return null;
        int i = (tier / 5 - 1) * 2 + (craft ? 1 : 0);
        return pool[i % pool.Count].id;
    }
    public static int PremiumCrowns(int tier) { return tier % 5 == 0 ? 0 : 15; }
    public static string FreeText(AHPlayer p, bool craft, int tier)
    {
        var r = Free(p, craft, tier); var parts = new List<string> { AHItems.MoneyText(r.money * AHDB.CU) };
        foreach (var kv in r.items) parts.Add(kv.Value + "× " + AHItems.Get(kv.Key).name);
        return string.Join(", ", parts.ToArray());
    }
    public static string PremText(AHPlayer p, bool craft, int tier)
    {
        string lk = PremiumLook(p, craft, tier); var l = AHLooks.Get(lk);
        if (l != null) return "Look: " + l.name + (p.prog.looks.Contains(l.id) ? " (owned: 60 Crowns instead)" : "");
        return PremiumCrowns(tier) + " " + AHStore.Currency;
    }

    static bool Bit(int mask, int tier) { return (mask & (1 << tier)) != 0; }
    public static bool ClaimedFree(AHPlayer p, bool craft, int tier) { Fresh(p); return Bit(craft ? p.prog.passClaimedC : p.prog.passClaimedF, tier); }
    public static bool ClaimedPrem(AHPlayer p, bool craft, int tier) { Fresh(p); return Bit(craft ? p.prog.passPremC : p.prog.passPremF, tier); }
    public static int Ready(AHPlayer p)
    {
        int n = 0;
        foreach (bool c in new[] { false, true })
            for (int t = 1; t <= Tier(p, c); t++) { if (!ClaimedFree(p, c, t)) n++; if (Premium(p) && !ClaimedPrem(p, c, t)) n++; }
        return n;
    }

    public static void ClaimFree(AHGame g, AHPlayer p, bool craft, int tier)
    {
        if (tier < 1 || tier > Tier(p, craft) || ClaimedFree(p, craft, tier)) return;
        var r = Free(p, craft, tier);
        if (!p.bag.Fits(r.items.ConvertAll(kv => kv.Key))) { g.ui.Toast("Your bag is full."); return; }
        if (craft) p.prog.passClaimedC |= 1 << tier; else p.prog.passClaimedF |= 1 << tier;
        AHDaily.Grant(g, r); AHSound.Play("coin");
    }
    public static void ClaimPrem(AHGame g, AHPlayer p, bool craft, int tier)
    {
        if (!Premium(p) || tier < 1 || tier > Tier(p, craft) || ClaimedPrem(p, craft, tier)) return;
        if (craft) p.prog.passPremC |= 1 << tier; else p.prog.passPremF |= 1 << tier;
        var l = AHLooks.Get(PremiumLook(p, craft, tier));
        if (l != null && !p.prog.looks.Contains(l.id)) { p.prog.looks.Add(l.id); g.ui.Banner(l.name, "A new look · wear it in Menu → Looks"); }
        else { long c = l != null ? 60 : PremiumCrowns(tier); p.prog.crowns += c; g.ui.Toast("+" + c + " " + AHStore.Currency, 2f); }
        AHSound.Play("loot"); g.MarkDirty();
    }
    public static bool BuyPremium(AHGame g, AHPlayer p)
    {
        if (Premium(p) || p.prog.crowns < PremiumPrice) return false;
        p.prog.crowns -= PremiumPrice; p.prog.passPremium = 1;
        g.ui.Banner("Premium Battle Pass", MonthName + " · looks on every tier of both lines");
        AHSound.Play("level"); g.MarkDirty();
        return true;
    }
}

public partial class AHUI
{
    bool passCraft;
    public void OpenPass() { wkMode = "pass"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderPass(AHPlayer p)
    {
        bool c = passCraft; int tier = AHPass2.Tier(p, c), xp = AHPass2.Xp(p, c);
        wkTitle.text = "Battle Pass · " + AHPass2.MonthName + (AHPass2.Premium(p) ? " · Premium" : "");
        wkHint.text = (c ? "Craft line: gathering, fishing and making things fill it." : "Fight line: what you defeat fills it (bosses most).") + " Tier " + tier + " / " + AHPass2.Tiers
            + (tier < AHPass2.Tiers ? " · " + (xp % AHPass2.TierXp) + " / " + AHPass2.TierXp + " to the next" : "") + " · " + AHPass2.DaysLeft + " days left. Premium rewards are looks only.";
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, c ? "Craft line" : "Fight line", c ? new Color(0.55f, 0.95f, 0.6f) : new Color(1f, 0.6f, 0.45f), "Fight tier " + AHPass2.Tier(p, false) + " · Craft tier " + AHPass2.Tier(p, true), AHPass2.Premium(p) ? "Premium is yours this month" : "Premium: " + AHStore.Amount(AHPass2.PremiumPrice) + " · looks on every tier",
            new WkBtn { label = c ? "Fight line" : "Craft line", on = true, col = Plain, act = () => { passCraft = !passCraft; wkPageI = 0; RenderWork(); } },
            AHPass2.Premium(p) ? null : new WkBtn { label = "Premium", on = p.prog.crowns >= AHPass2.PremiumPrice, col = new Color(0.55f, 0.35f, 0.7f, 1f), act = () => { AHPass2.BuyPremium(g, p); RenderWork(); } }));
        for (int t = 1; t <= AHPass2.Tiers; t++)
        {
            int tt = t; bool reached = t <= tier;
            bool cf = AHPass2.ClaimedFree(p, c, t), cp = AHPass2.ClaimedPrem(p, c, t), prem = AHPass2.Premium(p);
            rows.Add(s => Row(s, "Tier " + tt + (reached ? "" : "  <color=#8a8070>locked</color>"), reached ? new Color(1f, 0.85f, 0.45f) : new Color(0.7f, 0.66f, 0.6f),
                "Free: " + AHPass2.FreeText(p, c, tt) + (cf ? "  <color=#9be37a>✓</color>" : ""),
                "<color=#c9a6ff>Premium: " + AHPass2.PremText(p, c, tt) + "</color>" + (cp ? "  <color=#9be37a>✓</color>" : ""),
                new WkBtn { label = cf ? "Taken" : "Free", on = reached && !cf, col = Go, act = () => { AHPass2.ClaimFree(g, p, c, tt); RenderWork(); } },
                new WkBtn { label = cp ? "Taken" : "Premium", on = reached && prem && !cp, col = new Color(0.55f, 0.35f, 0.7f, 1f), act = () => { AHPass2.ClaimPrem(g, p, c, tt); RenderWork(); } }));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
