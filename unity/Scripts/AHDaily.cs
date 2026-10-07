// Ashen Hollow: daily and weekly goals, the login calendar and Adventurer's Marks, as in the web game
// (v66, pickDailies / dlRefresh / dailyEvent / dlReward / LOGIN_REWARDS / MARKS_SHOP), and the mystery sack (openSack).
// Three daily goals (new each day), three weekly challenges (new each Monday), four monthly feats (new on the 1st), the
// week's event (a different one each week, three tiers of rewards), a seven-day login calendar (missing a day restarts
// the streak), and the marks they pay spent in the Marks shop.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public class AHGoal { public string t, id, label; public int n, prog; public bool claimed; }
[Serializable]
public class AHDailyState
{
    public string day = "", week = "", last = "", claimedDay = "";
    public int streak, marks; public bool bonus;
    public List<AHGoal> tasks = new List<AHGoal>(), wtasks = new List<AHGoal>();
    public string month = "", evWeek = "", ev = "";
    public List<AHGoal> mtasks = new List<AHGoal>(), etasks = new List<AHGoal>();
}

public static class AHDaily
{
    public static string DayKey(DateTime t) { return t.ToString("yyyy-MM-dd"); }
    public static string WeekKey(DateTime t) { var d = t.Date; d = d.AddDays(-(((int)d.DayOfWeek + 6) % 7)); return DayKey(d); }
    public static string MonthKey(DateTime t) { return t.ToString("yyyy-MM"); }

    // ---------- the week's event: one of four, in turn ----------
    public class WeekEvent { public string id, name, blurb; public Color col; public AHGoal[] tiers; }
    public static WeekEvent EventOf(DateTime t, int L)
    {
        int wk = (int)((t.Date - new DateTime(2026, 1, 5)).TotalDays / 7.0); int k = ((wk % 4) + 4) % 4;
        switch (k)
        {
            case 0: return new WeekEvent { id = "hunt", name = "The Great Hunt", blurb = "The guilds pay double bounties this week. Hunt anything.", col = new Color(1f, 0.45f, 0.3f), tiers = new[] { G("kill", "any", 100, "Defeat 100 monsters"), G("kill", "any", 300, "Defeat 300 monsters"), G("kill", "elite", 15, "Defeat 15 elite monsters") } };
            case 1: return new WeekEvent { id = "harvest", name = "Gatherer’s Bounty", blurb = "Merchants are short of everything. Bring it in.", col = new Color(0.55f, 0.9f, 0.45f), tiers = new[] { G("gather", "any", 80, "Gather 80 materials"), G("gather", "any", 250, "Gather 250 materials"), G("craft", "any", 25, "Craft 25 items") } };
            case 2: return new WeekEvent { id = "bosses", name = "Bosshunt", blurb = "The world bosses stir. Bring them down for the realm.", col = new Color(0.8f, 0.5f, 1f), tiers = new[] { G("rare", "any", 2, "Defeat 2 rare monsters"), G("wboss", "any", 1, "Defeat a world boss"), G("wboss", "any", 3, "Defeat 3 world bosses") } };
            default: return new WeekEvent { id = "delve", name = "Delvers’ Week", blurb = "The dungeons are restless. Clear them out.", col = new Color(0.45f, 0.75f, 1f), tiers = L >= 14 ? new[] { G("dclear", "any", 1, "Clear a dungeon"), G("dclear", "any", 4, "Clear 4 dungeons"), G("kill", "elite", 10, "Defeat 10 elite monsters") } : new[] { G("kill", "any", 80, "Defeat 80 monsters"), G("skin", "any", 30, "Skin 30 monsters"), G("kill", "elite", 5, "Defeat 5 elite monsters") } };
        }
    }
    public static Reward EventReward(AHPlayer p, int tier)
    {
        long step = AHHome.LevelStep(p); int L = p.level;
        if (tier == 0) return new Reward { money = (long)Math.Round(200 * (1 + L / 5.0)), xp = (long)Math.Round(step * 0.3), marks = 25 };
        if (tier == 1) return new Reward { money = (long)Math.Round(500 * (1 + L / 5.0)), xp = (long)Math.Round(step * 0.6), marks = 60, items = { new KeyValuePair<string, int>("mystery_sack", 2) } };
        return new Reward { money = (long)Math.Round(900 * (1 + L / 5.0)), xp = step, marks = 120, items = { new KeyValuePair<string, int>("enh_stone", 3), new KeyValuePair<string, int>("ruby", 1) } };
    }
    public static Reward MonthReward(AHPlayer p)
    {
        long step = AHHome.LevelStep(p);
        return new Reward { money = (long)Math.Round(1500 * (1 + p.level / 4.0)), xp = (long)Math.Round(step * 1.5), marks = 200, items = { new KeyValuePair<string, int>("diamond", 1), new KeyValuePair<string, int>("enh_stone", 2) } };
    }
    static readonly string[] Meats = { "raw_beef", "raw_pork", "raw_mutton", "raw_chicken", "raw_duck", "raw_meat" };
    static string Kind(string id)
    {
        if (id.EndsWith("_ore")) return "ore";
        if (id.EndsWith("logs")) return "logs";
        if (id.StartsWith("raw_") && Array.IndexOf(Meats, id) < 0) return "fish";
        if (AHJson.O(AHJson.O(AHDB.Rules, "HERB_T"), id) != null) return "herb";
        var crops = AHDB.Table("home", "CROPS"); if (crops != null) foreach (var kv in crops) if (kv.Key == id || AHJson.S(kv.Value, "id") == id) return "crop";
        return "other";
    }
    static AHGoal G(string t, string id, int n, string label) { return new AHGoal { t = t, id = id, n = n, label = label }; }

    static List<AHGoal> Pick(AHGame g, int L, int n, bool weekly)
    {
        var p = g.player; var pool = new List<AHGoal>();
        if (p.path == "artisan")
        {
            // an Artisan's goals are about making and gathering, never fighting
            if (weekly)
            {
                pool.Add(G("gather", "any", 200, "Gather 200 materials")); pool.Add(G("craft", "any", 40, "Craft 40 items"));
                pool.Add(G("order", "any", 6, "Deliver 6 Guild work orders")); pool.Add(G("craft", "anvil", 10, "Forge 10 things at the anvil"));
                pool.Add(G("craft", "oven", 15, "Bake or cook 15 dishes")); pool.Add(G("gather", "ore", 80, "Mine 80 ore")); pool.Add(G("gather", "logs", 80, "Chop 80 logs"));
            }
            else
            {
                pool.Add(G("gather", "any", 30, "Gather 30 materials")); pool.Add(G("gather", "ore", 15, "Mine 15 ore")); pool.Add(G("gather", "logs", 15, "Chop 15 logs"));
                pool.Add(G("gather", "fish", 12, "Catch 12 fish")); pool.Add(G("gather", "herb", 10, "Pick 10 herbs")); pool.Add(G("craft", "any", 6, "Craft 6 items"));
                pool.Add(G("craft", "furnace", 5, "Smelt 5 bars")); pool.Add(G("craft", "fire", 5, "Cook 5 meals on a fire")); pool.Add(G("craft", "brew", 4, "Brew 4 potions"));
                if (p.home != null) pool.Add(G("gather", "crop", 15, "Harvest 15 crops"));
                if (p.profMain != null) pool.Add(G("order", "any", 1, "Deliver a Guild work order"));
            }
        }
        else if (weekly)
        {
            pool.Add(G("kill", "any", 250, "Defeat 250 monsters")); pool.Add(G("gather", "any", 200, "Gather 200 materials")); pool.Add(G("craft", "any", 40, "Craft 40 items"));
            pool.Add(L >= 14 ? G("dclear", "any", 3, "Clear 3 dungeons") : G("kill", "elite", 5, "Defeat 5 elite monsters"));
            pool.Add(p.profMain != null ? G("order", "any", 6, "Deliver 6 Guild work orders") : G("skin", "any", 60, "Skin 60 monsters"));
        }
        else
        {
            pool.Add(G("kill", "any", 25, "Defeat 25 monsters"));
            var live = new List<AHMobType>();
            foreach (var m in g.mobs) if (m != null && !m.add && !m.type.elite && !m.type.rare && Mathf.Abs(m.type.lvl - L) <= 5 && !live.Contains(m.type)) live.Add(m.type);
            if (live.Count > 0) { var k = live[UnityEngine.Random.Range(0, live.Count)]; pool.Add(G("kill", k.id, 10, "Hunt 10 " + k.name.ToLowerInvariant() + "s")); }
            pool.Add(G("gather", "any", 30, "Gather 30 materials")); pool.Add(G("gather", "ore", 15, "Mine 15 ore")); pool.Add(G("gather", "logs", 15, "Chop 15 logs"));
            pool.Add(G("gather", "fish", 12, "Catch 12 fish")); pool.Add(G("craft", "any", 5, "Craft 5 items")); pool.Add(G("skin", "any", 8, "Skin 8 monsters"));
            if (p.home != null) pool.Add(G("gather", "crop", 15, "Harvest 15 crops"));
            if (p.profMain != null) pool.Add(G("order", "any", 1, "Deliver a Guild work order"));
            if (L >= 14) pool.Add(G("dclear", "any", 1, "Clear a dungeon"));
        }
        var out1 = new List<AHGoal>();
        while (out1.Count < n && pool.Count > 0) { int i = UnityEngine.Random.Range(0, pool.Count); out1.Add(pool[i]); pool.RemoveAt(i); }
        return out1;
    }

    // web dlRefresh
    public static void Refresh(AHGame g)
    {
        var p = g.player; if (p == null || p.cls == null) return;
        var S = p.prog.daily; var now = DateTime.Now; string d = DayKey(now), w = WeekKey(now);
        if (S.day != d) { S.day = d; S.tasks = Pick(g, p.level, 3, false); S.bonus = false; g.MarkDirty(); }
        if (S.week != w) { S.week = w; S.wtasks = Pick(g, p.level, 3, true); g.MarkDirty(); }
        string mo = MonthKey(now);
        if (S.month != mo || S.mtasks == null || S.mtasks.Count == 0)
        {
            S.month = mo; int L = p.level;
            S.mtasks = p.path == "artisan"
                ? new List<AHGoal> { G("gather", "any", 1000, "Gather 1,000 materials"), G("craft", "any", 150, "Craft 150 items"), G("order", "any", 20, "Deliver 20 Guild work orders"), G("craft", "anvil", 40, "Forge 40 things at the anvil") }
                : new List<AHGoal> { G("kill", "any", 1500, "Defeat 1,500 monsters"), G("gather", "any", 800, "Gather 800 materials"), G("wboss", "any", 4, "Defeat 4 world bosses"), L >= 14 ? G("dclear", "any", 12, "Clear 12 dungeons") : G("kill", "elite", 25, "Defeat 25 elite monsters") };
            g.MarkDirty();
        }
        if (S.evWeek != w || S.etasks == null || S.etasks.Count == 0)
        {
            var E = EventOf(now, p.level); S.evWeek = w; S.ev = E.id; S.etasks = new List<AHGoal>(E.tiers);
            // Artisans take part by supplying it: making and gathering instead of fighting
            if (p.path == "artisan" && E.id != "harvest") S.etasks = new List<AHGoal> { G("craft", "any", 30, "Make 30 supplies"), G("gather", "any", 200, "Gather 200 materials"), G("order", "any", 8, "Deliver 8 work orders") };
            g.MarkDirty();
        }
        if (S.last != d) { S.streak = S.last == DayKey(now.AddDays(-1)) ? S.streak + 1 : 1; S.last = d; g.MarkDirty(); }
    }

    // web dailyEvent: everything the quest log hears
    public static void Event(AHGame g, string t, string id, int n)
    {
        var p = g.player; if (p == null || id == null) return;
        AHFest.Event(g, t, n);
        var S = p.prog.daily;
        var all = new List<AHGoal>(S.tasks); all.AddRange(S.wtasks); if (S.mtasks != null) all.AddRange(S.mtasks); if (S.etasks != null) all.AddRange(S.etasks);
        foreach (var q in all)
        {
            if (q.t != t || q.claimed || q.prog >= q.n) continue;
            bool hit = q.id == "any" || q.id == id || (t == "gather" && Kind(id) == q.id) || (q.id == "elite" && AHJson.B(AHJson.O(AHDB.Mobs, id), "elite"));
            if (!hit) continue;
            q.prog = Mathf.Min(q.n, q.prog + n);
            if (q.prog >= q.n) g.ui.Banner("Goal complete", q.label);
        }
    }

    // ---------- rewards ----------
    public class Reward { public long money; public long xp; public int marks; public List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>(); public string name; }
    static Reward R(string name, long money, int marks, params object[] items)
    {
        var r = new Reward { name = name, money = money, marks = marks };
        for (int i = 0; i + 1 < items.Length; i += 2) r.items.Add(new KeyValuePair<string, int>((string)items[i], (int)items[i + 1]));
        return r;
    }
    public static readonly Reward[] Login =
    {
        R("Coins", 300, 0), R("Healing potions", 0, 0, "big_potion", 2), R("A gem", 0, 0, "sapphire", 1), R("Seeds and feed", 0, 0, "wheat_seed", 10, "animal_feed", 10),
        R("Mystery sack", 0, 0, "mystery_sack", 1), R("Elixirs", 0, 0, "elixir_might", 2, "elixir_swift", 2), R("Grand prize", 0, 60, "diamond", 1),
    };
    public static Reward GoalReward(AHPlayer p, bool weekly)
    {
        int L = p.level; long step = AHHome.LevelStep(p);
        return weekly ? new Reward { money = (long)Math.Round(400 * (1 + L / 4.0)), xp = (long)Math.Round(step * 0.6), marks = 50 } : new Reward { money = (long)Math.Round(40.0 * (1 + L)), xp = (long)Math.Round(step * 0.15), marks = 10 };
    }
    public static string Text(Reward r)
    {
        var l = new List<string>();
        if (r.money > 0) l.Add(AHItems.MoneyText(r.money * AHDB.CU));
        if (r.xp > 0) l.Add(r.xp.ToString("#,0") + " XP");
        if (r.marks > 0) l.Add(r.marks + " marks");
        foreach (var kv in r.items) { var it = AHItems.Get(kv.Key); l.Add((kv.Value > 1 ? kv.Value + "× " : "") + (it != null ? it.name : kv.Key)); }
        return string.Join(" · ", l.ToArray());
    }
    public static void Grant(AHGame g, Reward r)
    {
        var p = g.player;
        if (r.money > 0) p.AddMoney(r.money * AHDB.CU, p.transform.position);
        if (r.xp > 0) p.GainClassXpDirect((int)Math.Min(int.MaxValue, r.xp));
        if (r.marks > 0) { p.prog.daily.marks += r.marks; AHExtras.AddXp(p, r.marks); }   // the marks your goals pay also fill the season track
        foreach (var kv in r.items) p.bag.Add(kv.Key, kv.Value);
        g.SaveProgress();
    }

    // web MARKS_SHOP: [id, count, marks]
    public static readonly object[][] Shop =
    {
        new object[] { "big_potion", 3, 12 }, new object[] { "great_mana", 2, 14 }, new object[] { "mystery_sack", 1, 30 }, new object[] { "sapphire", 1, 40 }, new object[] { "ruby", 1, 50 },
        new object[] { "emerald", 1, 60 }, new object[] { "diamond", 1, 250 }, new object[] { "pet:owl", 1, 400 }, new object[] { "mount:direwolf", 1, 900 },
    };

    public static int Ready(AHPlayer p)
    {
        var S = p.prog.daily; int n = S.claimedDay != S.day ? 1 : 0;
        foreach (var q in S.tasks) if (q.prog >= q.n && !q.claimed) n++;
        foreach (var q in S.wtasks) if (q.prog >= q.n && !q.claimed) n++;
        if (S.mtasks != null) foreach (var q in S.mtasks) if (q.prog >= q.n && !q.claimed) n++;
        if (S.etasks != null) foreach (var q in S.etasks) if (q.prog >= q.n && !q.claimed) n++;
        return n;
    }

    // web openSack
    public static void OpenSack(AHGame g)
    {
        var p = g.player; if (!p.bag.Take("mystery_sack")) return;
        float roll = UnityEngine.Random.value;
        if (roll < 0.35f) { long c = (20 + UnityEngine.Random.Range(0, 120)) * AHDB.CU + UnityEngine.Random.Range(0, (int)AHDB.CU); p.AddMoney(c, p.transform.position); g.ui.Toast("The sack held " + AHItems.MoneyText(c) + "!"); }
        else if (roll < 0.7f)
        {
            string[] its = { "hp_potion", "big_potion", "elixir_might", "elixir_swift", "elixir_iron", "stew" }; string it = its[UnityEngine.Random.Range(0, its.Length)];
            if (AHItems.Get(it) == null) it = "hp_potion";
            p.bag.Add(it); g.ui.Toast("The sack held a " + AHItems.Get(it).name + ".");
        }
        else if (roll < 0.9f)
        {
            string got = null; foreach (var k in new[] { "lantern_hood", "bloom_hood", "corsair_hat", "royal_cape", "bloom_cape" }) if (AHItems.Get(k) != null && p.bag.Count(k) == 0 && !p.bag.gear.ContainsValue(k)) { got = k; break; }
            if (got != null && p.bag.Add(got)) g.ui.Banner(AHItems.Get(got).name, "Rare outfit!");
            else { p.AddMoney(60 * AHDB.CU, p.transform.position); g.ui.Toast("The sack held some coins."); }
        }
        else if (!p.pets.Contains("toad") && AHJson.Has(AHComp.Pets, "toad")) { p.pets.Add("toad"); p.pet = "toad"; AHComp.SpawnPet(g); g.ui.Banner("Mire toadling", "A pet hopped out of the sack!"); }
        else { p.bag.Add("diamond"); g.ui.Banner("Diamond", "A diamond at the bottom of the sack!"); }
        g.SaveProgress();
    }

    static float t;
    public static void Tick(AHGame g, float dt) { t -= dt; if (t > 0f) return; t = 5f; Refresh(g); g.ui.RefreshDailyChip(); }
}

public partial class AHUI
{
    RectTransform dailyChip; Text dailyChipText;

    void BuildDailyHud()
    {
        dailyChip = Img("DailyChip", transform, white, new Vector2(0, 1), new Vector2(112, -232), new Vector2(190, 34), new Color(0.2f, 0.14f, 0.1f, 0.85f));
        dailyChipText = Center(Label(dailyChip, "T", "Daily 0/3", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(190, 30), new Color(1f, 0.85f, 0.5f)));
        taps.Add(new TapBtn { rt = dailyChip, act = OpenDaily });
    }
    public void RefreshDailyChip()
    {
        var p = g.player; if (dailyChip == null || p == null || p.cls == null) return;
        var S = p.prog.daily; int done = S.tasks.FindAll(q => q.prog >= q.n).Count, ready = AHDaily.Ready(p);
        dailyChipText.text = "Daily " + done + "/" + S.tasks.Count + (ready > 0 ? " · claim!" : "");
        dailyChip.GetComponent<UnityEngine.UI.Image>().color = ready > 0 ? new Color(0.45f, 0.3f, 0.08f, 0.92f) : new Color(0.2f, 0.14f, 0.1f, 0.85f);
    }

    public void OpenDaily() { AHDaily.Refresh(g); wkMode = "daily"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderDaily(AHPlayer p)
    {
        var S = p.prog.daily; int di = (Mathf.Max(1, S.streak) - 1) % 7; bool canLogin = S.claimedDay != S.day;
        wkTitle.text = "Goals & events";
        var nd = DateTime.Today.AddDays(1) - DateTime.Now;
        wkHint.text = "Adventurer's Marks: " + S.marks + " · login streak: day " + S.streak + " · new goals in " + (int)nd.TotalHours + "h " + nd.Minutes + "m. Missing a day restarts the streak.";
        var rows = new List<Action<int>>();
        var lr = AHDaily.Login[di];
        // the season track
        {
            int tier = AHExtras.Tier(p), cl = AHExtras.Claimed(p); var nx = cl < AHExtras.Tiers ? AHExtras.TierReward(p, cl + 1) : null;
            rows.Add(s => Row(s, AHExtras.SeasonName(DateTime.Now) + " · tier " + tier + " / " + AHExtras.Tiers, new Color(0.56f, 0.85f, 1f),
                (p.prog.seasonXp % AHExtras.TierXp) + " / " + AHExtras.TierXp + " to the next tier · marks from goals fill it", nx != null ? "Tier " + (cl + 1) + ": " + AHDaily.Text(nx) : "Every tier claimed this season",
                nx == null ? null : new WkBtn { label = "Claim", on = cl < tier, col = Go, act = () => { if (AHExtras.ClaimNext(g)) Banner("Season tier " + AHExtras.Claimed(p), "Reward claimed"); RenderWork(); } }));
        }
        rows.Add(s => Row(s, "Login calendar · day " + (di + 1) + " of 7", new Color(1f, 0.8f, 0.45f), lr.name + ": " + AHDaily.Text(lr), "Day 7: Grand prize (a diamond and 60 marks)",
            new WkBtn { label = canLogin ? "Claim" : "Tomorrow", on = canLogin, col = Go, act = () => { if (S.claimedDay == S.day) return; S.claimedDay = S.day; AHDaily.Grant(g, lr); Banner("Day " + (di + 1) + " reward", lr.name);
                // a whole month of days in a row: a big bonus
                if (S.streak > 0 && S.streak % 30 == 0) { AHDaily.Grant(g, new AHDaily.Reward { marks = 150, items = { new KeyValuePair<string, int>("diamond", 2), new KeyValuePair<string, int>("mystery_sack", 3) } }); Banner("30 days in a row!", "Two diamonds, three sacks and 150 marks"); }
                RenderWork(); RefreshDailyChip(); } }));
        Action<List<AHGoal>, bool> list = (tasks, weekly) =>
        {
            foreach (var q in tasks)
            {
                var qq = q; var rw = AHDaily.GoalReward(p, weekly);
                rows.Add(s => Row(s, (weekly ? "Weekly: " : "") + qq.label + (qq.claimed ? "  <color=#9be37a>done</color>" : ""), weekly ? new Color(0.75f, 0.6f, 1f) : Color.white, qq.prog + " / " + qq.n + " · " + AHDaily.Text(rw), "",
                    qq.claimed ? null : new WkBtn { label = "Claim", on = qq.prog >= qq.n, col = Go, act = () => { if (qq.claimed || qq.prog < qq.n) return; qq.claimed = true; AHDaily.Grant(g, rw); RenderWork(); RefreshDailyChip(); } }));
            }
        };
        // the week's event
        {
            var E = AHDaily.EventOf(DateTime.Now, p.level); var ne = DateTime.Today.AddDays(7 - (((int)DateTime.Today.DayOfWeek + 6) % 7)) - DateTime.Now;
            rows.Add(s => Row(s, "Event: " + E.name, E.col, p.path == "artisan" && E.id != "harvest" ? "The fighters need supplies: Artisans earn the event by making and gathering." : E.blurb, "Ends in " + (int)ne.TotalDays + "d " + ne.Hours + "h · a new event every Monday"));
            for (int ti = 0; ti < S.etasks.Count; ti++)
            {
                var qq = S.etasks[ti]; int tier = ti; var rw = AHDaily.EventReward(p, tier);
                rows.Add(s => Row(s, "  Tier " + (tier + 1) + ": " + qq.label + (qq.claimed ? "  <color=#9be37a>done</color>" : ""), E.col, qq.prog + " / " + qq.n + " · " + AHDaily.Text(rw), "",
                    qq.claimed ? null : new WkBtn { label = "Claim", on = qq.prog >= qq.n, col = Go, act = () => { if (qq.claimed || qq.prog < qq.n) return; qq.claimed = true; AHDaily.Grant(g, rw); Banner(E.name, "Tier " + (tier + 1) + " reward"); RenderWork(); RefreshDailyChip(); } }));
            }
        }
        list(S.tasks, false);
        rows.Add(s => Row(s, "Finish all three", Color.white, "Bonus: 20 marks and a mystery sack", "",
            S.bonus ? null : new WkBtn { label = "Claim bonus", on = S.tasks.TrueForAll(q => q.claimed), col = Go, act = () => { if (S.bonus || !S.tasks.TrueForAll(q => q.claimed)) return; S.bonus = true; AHDaily.Grant(g, new AHDaily.Reward { marks = 20, items = { new KeyValuePair<string, int>("mystery_sack", 1) } }); RenderWork(); } }));
        list(S.wtasks, true);
        // this month's feats
        {
            var nm = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1) - DateTime.Now; var mr = AHDaily.MonthReward(p);
            foreach (var q in S.mtasks)
            {
                var qq = q;
                rows.Add(s => Row(s, "Monthly: " + qq.label + (qq.claimed ? "  <color=#9be37a>done</color>" : ""), new Color(1f, 0.75f, 0.3f), qq.prog + " / " + qq.n + " · " + AHDaily.Text(mr), "New feats in " + (int)nm.TotalDays + " days",
                    qq.claimed ? null : new WkBtn { label = "Claim", on = qq.prog >= qq.n, col = Go, act = () => { if (qq.claimed || qq.prog < qq.n) return; qq.claimed = true; AHDaily.Grant(g, mr); Banner("Monthly feat", qq.label); RenderWork(); RefreshDailyChip(); } }));
            }
        }
        foreach (var o in AHDaily.Shop)
        {
            string id = (string)o[0]; int n = (int)o[1], cost = (int)o[2];
            bool own = id.StartsWith("pet:") ? p.pets.Contains(id.Substring(4)) : id.StartsWith("mount:") && p.mounts.Contains(id.Substring(6));
            string name = id.StartsWith("pet:") ? AHComp.PetName(id.Substring(4)) + " (pet)" : id.StartsWith("mount:") ? AHComp.MountName(id.Substring(6)) + " (mount)" : (n > 1 ? n + "× " : "") + (AHItems.Get(id) != null ? AHItems.Get(id).name : id);
            rows.Add(s => Row(s, "Marks shop: " + name, new Color(1f, 0.62f, 0.24f), cost + " marks" + (own ? " · owned" : ""), "",
                new WkBtn { label = "Buy", on = !own && S.marks >= cost, col = Plain, act = () => {
                    if (S.marks < cost) return;
                    if (id.StartsWith("pet:")) { string k = id.Substring(4); if (p.pets.Contains(k)) return; p.pets.Add(k); Banner(AHComp.PetName(k), "New pet!"); }
                    else if (id.StartsWith("mount:")) { string k = id.Substring(6); if (p.mounts.Contains(k)) return; p.mounts.Add(k); if (p.mountSel == null) p.mountSel = k; RefreshRide(); Banner(AHComp.MountName(k), "New mount!"); }
                    else if (!p.bag.Add(id, n)) { Toast(p.bag.lastWarn ?? "Your bag is full."); return; }
                    S.marks -= cost; g.SaveProgress(); RenderWork(); } }));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
