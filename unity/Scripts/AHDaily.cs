// Ashen Hollow: daily and weekly goals, the login calendar and Adventurer's Marks, as in the web game
// (v66, pickDailies / dlRefresh / dailyEvent / dlReward / LOGIN_REWARDS / MARKS_SHOP), and the mystery sack (openSack).
// Three daily goals (new each day), three weekly challenges (new each Monday), a seven-day login calendar (missing a day
// restarts the streak), and the marks they pay spent in the Marks shop.
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
}

public static class AHDaily
{
    public static string DayKey(DateTime t) { return t.ToString("yyyy-MM-dd"); }
    public static string WeekKey(DateTime t) { var d = t.Date; d = d.AddDays(-(((int)d.DayOfWeek + 6) % 7)); return DayKey(d); }
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
        if (weekly)
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
        if (S.last != d) { S.streak = S.last == DayKey(now.AddDays(-1)) ? S.streak + 1 : 1; S.last = d; g.MarkDirty(); }
    }

    // web dailyEvent: everything the quest log hears
    public static void Event(AHGame g, string t, string id, int n)
    {
        var p = g.player; if (p == null || id == null) return;
        AHFest.Event(g, t, n);
        var S = p.prog.daily;
        var all = new List<AHGoal>(S.tasks); all.AddRange(S.wtasks);
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
        if (r.marks > 0) p.prog.daily.marks += r.marks;
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
        wkTitle.text = "Daily goals";
        var nd = DateTime.Today.AddDays(1) - DateTime.Now;
        wkHint.text = "Adventurer's Marks: " + S.marks + " · login streak: day " + S.streak + " · new goals in " + (int)nd.TotalHours + "h " + nd.Minutes + "m. Missing a day restarts the streak.";
        var rows = new List<Action<int>>();
        var lr = AHDaily.Login[di];
        rows.Add(s => Row(s, "Login calendar · day " + (di + 1) + " of 7", new Color(1f, 0.8f, 0.45f), lr.name + ": " + AHDaily.Text(lr), "Day 7: Grand prize (a diamond and 60 marks)",
            new WkBtn { label = canLogin ? "Claim" : "Tomorrow", on = canLogin, col = Go, act = () => { if (S.claimedDay == S.day) return; S.claimedDay = S.day; AHDaily.Grant(g, lr); Banner("Day " + (di + 1) + " reward", lr.name); RenderWork(); RefreshDailyChip(); } }));
        Action<List<AHGoal>, bool> list = (tasks, weekly) =>
        {
            foreach (var q in tasks)
            {
                var qq = q; var rw = AHDaily.GoalReward(p, weekly);
                rows.Add(s => Row(s, (weekly ? "Weekly: " : "") + qq.label + (qq.claimed ? "  <color=#9be37a>done</color>" : ""), weekly ? new Color(0.75f, 0.6f, 1f) : Color.white, qq.prog + " / " + qq.n + " · " + AHDaily.Text(rw), "",
                    qq.claimed ? null : new WkBtn { label = "Claim", on = qq.prog >= qq.n, col = Go, act = () => { if (qq.claimed || qq.prog < qq.n) return; qq.claimed = true; AHDaily.Grant(g, rw); RenderWork(); RefreshDailyChip(); } }));
            }
        };
        list(S.tasks, false);
        rows.Add(s => Row(s, "Finish all three", Color.white, "Bonus: 20 marks and a mystery sack", "",
            S.bonus ? null : new WkBtn { label = "Claim bonus", on = S.tasks.TrueForAll(q => q.claimed), col = Go, act = () => { if (S.bonus || !S.tasks.TrueForAll(q => q.claimed)) return; S.bonus = true; AHDaily.Grant(g, new AHDaily.Reward { marks = 20, items = { new KeyValuePair<string, int>("mystery_sack", 1) } }); RenderWork(); } }));
        list(S.wtasks, true);
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
