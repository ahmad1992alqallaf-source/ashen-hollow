// Ashen Hollow: the Artisan's own game. Artisans (the path of makers and gatherers, left alone by monsters) get:
//  - the Artisan's Saga, a story in five chapters after (and alongside) the Artisan's Road: the burned guild hall of
//    Ashen Hollow, Master Oren's lost recipe book, the rival crafter Corvin Vale of Varrow, rebuilding the hall stone by
//    stone, and the Masterwork that makes you Grand Artisan;
//  - a weekly commission (a noble's or a captain's order of three things to make, paid well);
//  - the monthly Masterwork Contest (hand in your finest piece; the judges rank it against the realm's best);
//  - a rank (Apprentice, Journeyman, Expert, Master, Grand Artisan) and a maker's mark on what you craft;
//  - workshop upgrades bought with coin: the forge, the loom, the kitchen and the shop counter.
// Their daily, weekly and monthly goals are about making and gathering (AHDaily picks them by path).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AHArtisanState
{
    public int sagaI, sagaP, hall;            // the saga step, its progress, and how far the guild hall is rebuilt (0..4)
    public string comWeek = ""; public List<AHStack> com = new List<AHStack>(); public bool comDone;
    public string contestMonth = ""; public int contestScore, contestRank; public string contestItem = ""; public bool contestClaimed;
    public int forge, loom, kitchen, counter;   // workshop upgrade levels (0..3)
    public List<AHKV> made = new List<AHKV>();  // what you have made, by item (your maker's mark)
}

public static class AHArtisan
{
    // ---------- the saga ----------
    public class Step { public int ch; public string t, id, label, story; public int n; public int pay; }
    static Step S(int ch, string t, string id, int n, string label, string story, int pay = 60) { return new Step { ch = ch, t = t, id = id, n = n, label = label, story = story, pay = pay }; }

    public static readonly string[] Chapters = { "The Ashes", "The Lost Recipe Book", "The Rival", "Rebuilding the Hall", "The Masterwork" };
    public static readonly List<Step> Saga = new List<Step>
    {
        // I. The Ashes
        S(0, "talkguild", null, 1, "Speak with the Guild Clerk in Ashen Hollow",
            "The old guild hall of Ashen Hollow burned the winter before you came. Its last master, Mirelle Ashby, survived, but her hands shake too much to hold a hammer. The Guild Clerk keeps her letters. Go and hear them."),
        S(0, "gather", "any", 30, "Gather 30 materials for the cold forge",
            "Mirelle's letter: ‘A forge that has gone cold must be fed before it is lit. Bring me what the land gives: ore, wood, fibre, herbs. Thirty loads, apprentice.’"),
        S(0, "craft", "furnace", 5, "Smelt 5 bars at a furnace",
            "‘Good. Now show me your hands are steady. Five bars, clean, no slag.’"),
        S(0, "give", "logs", 20, "Bring 20 logs to stoke the guild forge",
            "‘The forge in the ruins still stands. Twenty logs and it breathes again.’", 120),
        // II. The Lost Recipe Book
        S(1, "page", null, 5, "Find 5 pages of Master Oren's recipe book (while gathering and crafting)",
            "With the forge alight, Mirelle shows you a charred binding: Master Oren's book, the guild's greatest recipes. The fire scattered its pages to the wind. Gatherers find them in odd places: under roots, in ore seams, in fishing nets."),
        S(1, "craft", "any", 15, "Craft 15 things from Oren's methods",
            "The pages are half-burned but the methods are clear. Mirelle wants to see them worked, not read."),
        S(1, "order", "any", 3, "Deliver 3 guild work orders",
            "‘A guild is its customers. Take orders and fill them. Word will spread that Ashen Hollow makes again.’", 150),
        // III. The Rival
        S(2, "craft", "any", 20, "Out-make Corvin Vale: craft 20 things",
            "Word spreads to Varrow. Corvin Vale, who sells cheap cursed blades and cracked pots, does not like competition. He boasts that no one in Ashen Hollow can make anything worth buying."),
        S(2, "givefine", null, 1, "Bring a Fine or Masterwork piece to the judges",
            "Corvin challenges the guild to a contest before the merchants of Varrow. Bring them your best work: a Fine meal, a Masterwork weapon or armor."),
        S(2, "order", "any", 5, "Win back Corvin's customers: deliver 5 work orders",
            "The merchants laugh at Corvin's cracked wares beside yours. His customers start coming to you.", 250),
        // IV. Rebuilding the Hall
        S(3, "give", "logs", 40, "Hall, stage 1: 40 logs for the frame",
            "Mirelle has a plan drawn on the back of Oren's book: a new guild hall, bigger than the old. It starts with the timber frame."),
        S(3, "give", "copper_ore", 30, "Hall, stage 2: 30 copper ore for the roof",
            "The frame stands. A copper roof, Mirelle says, so it never burns again."),
        S(3, "give", "iron_bar", 10, "Hall, stage 3: 10 iron bars for the doors and fittings",
            "Iron for the great doors and the anvils inside."),
        S(3, "give", "linen", 10, "Hall, stage 4: 10 linen for the banners",
            "Banners over the doors, so travellers know the guild is back.", 400),
        // V. The Masterwork
        S(4, "mastery", null, 20, "Reach mastery 20 in any profession (Expert)",
            "The hall is rebuilt. Mirelle is old and wants to pass the guild to someone. But the guild's law is old too: its master must make a Masterwork the whole realm will remember."),
        S(4, "givemw", null, 1, "Make and bring a Masterwork piece",
            "‘Not good. Not fine. A Masterwork. Show me.’"),
        S(4, "give", "diamond", 1, "Bring a diamond for the guild crown",
            "The guild's crown has been lost since the fire. Mirelle asks you to make a new one, set with a diamond."),
        S(4, "give", "gold_ore", 10, "Bring 10 gold ore for the crown",
            "‘Gold for the band. Then it is yours to wear.’", 1000),
    };

    public static AHArtisanState St(AHPlayer p) { if (p.prog.art == null) p.prog.art = new AHArtisanState(); return p.prog.art; }
    public static Step Current(AHPlayer p) { var s = St(p); return s.sagaI < Saga.Count ? Saga[s.sagaI] : null; }
    public static bool SagaDone(AHPlayer p) { return St(p).sagaI >= Saga.Count; }

    // ---------- the rank ----------
    public static string Rank(AHPlayer p)
    {
        if (SagaDone(p)) return "Grand Artisan";
        int m = p.BestMastery();
        return m >= 30 ? "Master" : m >= 20 ? "Expert" : m >= 10 ? "Journeyman" : "Apprentice";
    }

    // everything the quest log hears (gathering, crafting, orders, talking to the guild)
    public static void Event(AHGame g, string t, string id, int n)
    {
        var p = g.player; if (p == null || p.path != "artisan") return;
        var st = St(p); var s = Current(p); if (s == null) return;
        bool hit = false;
        if (s.t == "talkguild" && t == "talkguild") { st.sagaP = 1; hit = true; }
        else if (s.t == "page" && (t == "gather" || t == "craft") && UnityEngine.Random.value < 0.08f)
        {
            st.sagaP++; hit = true;
            if (g.ui != null) g.ui.Banner("A page of Oren's book", "Page " + st.sagaP + " of " + s.n);
        }
        else if (s.t == t && (s.id == null || s.id == "any" || s.id == id || (t == "gather" && AHQuests.GatherKind(id) == s.id))) { st.sagaP += n; hit = true; }
        if (hit) Check(g);
    }

    // a step done: pay, tell the story on, and move to the next
    public static void Check(AHGame g)
    {
        var p = g.player; var st = St(p); var s = Current(p); if (s == null) return;
        if (s.t == "mastery") st.sagaP = p.BestMastery();
        if (st.sagaP < s.n) { g.MarkDirty(); return; }
        long c = (long)(s.pay * AHDB.CU * (1 + Mathf.FloorToInt(p.level / 10f)));
        p.AddMoney(c, p.transform.position);
        p.GainClassXpDirect(Mathf.RoundToInt(120 + p.level * p.level * 2.5f));
        if (s.ch == 3) st.hall = Mathf.Min(4, st.hall + 1);
        int ch = s.ch;
        st.sagaI++; st.sagaP = 0;
        var nx = Current(p);
        if (nx == null)
        {
            p.bag.Add("diamond", 1); p.bag.Add("enh_stone", 5);
            if (g.ui != null) g.ui.Banner("Grand Artisan", "Mirelle sets the crown on your head. The guild is yours.");
            AHSound.Play("level");
        }
        else if (nx.ch != ch)
        {
            p.bag.Add("mystery_sack", 1); p.bag.Add("enh_stone", 2);
            if (g.ui != null) g.ui.Banner("Chapter " + (ch + 1) + " complete", Chapters[ch] + " · next: " + Chapters[nx.ch]);
            AHSound.Play("level");
        }
        else if (g.ui != null) g.ui.Banner("Artisan's Saga", s.label.Split(':')[0] + " · " + AHItems.MoneyText(c));
        g.SaveProgress();
        if (nx != null && nx.t == "mastery") Check(g);   // already reached
    }

    // hand things in (the give steps)
    public static bool CanGive(AHPlayer p, Step s, out string id)
    {
        id = null; if (s == null) return false;
        if (s.t == "give") { id = s.id; return p.bag.Count(s.id) >= s.n - St(p).sagaP; }
        if (s.t == "givefine" || s.t == "givemw")
            foreach (var k in p.bag.order)
                if (p.bag.Count(k) > 0 && (k.EndsWith("_mw") || k.EndsWith("_master") || (s.t == "givefine" && k.EndsWith("_fine")))) { id = k; return true; }
        return false;
    }
    public static void Give(AHGame g)
    {
        var p = g.player; var st = St(p); var s = Current(p); string id;
        if (!CanGive(p, s, out id)) return;
        int need = s.t == "give" ? s.n - st.sagaP : 1;
        if (!p.bag.Take(id, need)) return;
        st.sagaP += need;
        Check(g);
    }

    // the best potions in the realm are an Artisan's secret: raid supplies and the Titan's elixir
    public static readonly string[] OnlyIds = { "titan_elixir", "raid_flask" };
    public static bool Only(string id) { return Array.IndexOf(OnlyIds, id) >= 0; }

    // ---------- crafting: the maker's mark and the workshop ----------
    public static void OnCraft(AHPlayer p, string outId, int n)
    {
        if (p == null || outId == null) return;
        AHProgress.Add(St(p).made, outId, n);
    }
    public static long Made(AHPlayer p, string id) { return p == null || id == null ? 0 : AHProgress.Get(St(p).made, id); }
    // the forge, loom and kitchen raise the chance of a masterwork / a double batch; the counter pays more at shops
    public static float MwBonus(AHPlayer p, string kind)
    {
        var s = St(p); int lv = kind == "anvil" || kind == "jewel" ? s.forge : kind == "loom" ? s.loom : 0;
        return 1f + lv * 0.15f;
    }
    public static float DoubleBonus(AHPlayer p, string kind) { return kind == "oven" || kind == "brew" || kind == "fire" ? St(p).kitchen * 0.06f : 0f; }
    public static float SellBonus(AHPlayer p) { return 1f + St(p).counter * 0.05f; }

    public class Upgrade { public string id, name, what; }
    public static readonly Upgrade[] Workshop =
    {
        new Upgrade { id = "forge", name = "Forge", what = "+15% masterwork chance at the anvil and the jeweler's bench, per level" },
        new Upgrade { id = "loom", name = "Loom", what = "+15% masterwork chance at the loom, per level" },
        new Upgrade { id = "kitchen", name = "Kitchen", what = "+6% chance of a double batch when cooking and brewing, per level" },
        new Upgrade { id = "counter", name = "Shop counter", what = "Shops pay 5% more for what you sell, per level" },
    };
    public static int Lv(AHPlayer p, string id) { var s = St(p); return id == "forge" ? s.forge : id == "loom" ? s.loom : id == "kitchen" ? s.kitchen : s.counter; }
    public static long Cost(AHPlayer p, string id) { int lv = Lv(p, id); return (long)(2000 * Math.Pow(3, lv)) * AHDB.CU; }
    public static bool Buy(AHGame g, string id)
    {
        var p = g.player; var s = St(p); int lv = Lv(p, id); if (lv >= 3) return false;
        long c = Cost(p, id); if (p.bag.money < c) return false;
        p.bag.money -= c;
        if (id == "forge") s.forge++; else if (id == "loom") s.loom++; else if (id == "kitchen") s.kitchen++; else s.counter++;
        g.SaveProgress(); return true;
    }

    // ---------- the weekly commission ----------
    static readonly string[] Patrons = { "Lady Wren of Varrow", "Captain Holt of the Tidewake", "The Sunscar Caravan", "Abbot Fennick", "The Frostpeak Wardens", "Baron Ashcombe" };
    public static string Patron() { return Patrons[(int)((DateTime.Today - new DateTime(2026, 1, 5)).TotalDays / 7) % Patrons.Length]; }
    public static void RefreshCommission(AHPlayer p)
    {
        var s = St(p); string w = AHDaily.WeekKey(DateTime.Now);
        if (s.comWeek == w && s.com.Count > 0) return;
        s.comWeek = w; s.comDone = false; s.com.Clear();
        // three things you can make now, from the stations of your professions
        var can = new List<AHRecipe>();
        foreach (var st in new[] { "anvil", "loom", "brew", "oven", "jewel", "mill", "dairy", "spin" })
            foreach (var r in AHGather.Recipes(st))
                if (r.skill != null && p.Skill(r.skill) >= r.lvl && AHItems.Get(r.outId) != null) can.Add(r);
        var rnd = new System.Random(w.GetHashCode() ^ (p.heroName ?? "").GetHashCode());
        while (s.com.Count < 3 && can.Count > 0)
        {
            var r = can[rnd.Next(can.Count)]; can.Remove(r);
            if (s.com.Exists(x => x.id == r.outId)) continue;
            var it = AHItems.Get(r.outId);
            s.com.Add(new AHStack { id = r.outId, n = it.slot != null ? 1 : Mathf.Clamp(6 - r.lvl / 6, 2, 6) });
        }
    }
    public static bool CommissionReady(AHPlayer p) { var s = St(p); return !s.comDone && s.com.Count > 0 && s.com.TrueForAll(x => p.bag.Count(x.id) >= x.n); }
    public static AHDaily.Reward CommissionReward(AHPlayer p)
    {
        long step = AHHome.LevelStep(p);
        return new AHDaily.Reward { money = (long)Math.Round(700 * (1 + p.level / 4.0)), xp = (long)Math.Round(step * 0.8), marks = 60, items = { new KeyValuePair<string, int>("mystery_sack", 1) } };
    }
    public static void DeliverCommission(AHGame g)
    {
        var p = g.player; var s = St(p); if (!CommissionReady(p)) return;
        foreach (var x in s.com) p.bag.Take(x.id, x.n);
        s.comDone = true;
        AHDaily.Grant(g, CommissionReward(p));
        if (g.ui != null) g.ui.Banner("Commission delivered", Patron() + " is pleased");
        AHSound.Play("coin");
    }

    // ---------- the monthly Masterwork Contest ----------
    static readonly string[] Rivals = { "Corvin Vale", "Hesta Brightanvil", "Old Pellam", "Saoirse of the Loom", "Dunmore Kettle", "Ysolde Gemhand", "Brannoc the Younger" };
    static int RecipeLv(string id)
    {
        foreach (var st in new[] { "anvil", "loom", "brew", "oven", "jewel", "mill", "dairy", "spin" })
            foreach (var r in AHGather.Recipes(st)) if (r.outId == id) return Mathf.Max(1, r.lvl);
        return 0;
    }
    static string BaseOf(string id) { return id.EndsWith("_mw") ? id.Substring(0, id.Length - 3) : id.EndsWith("_master") ? id.Substring(0, id.Length - 7) : id.EndsWith("_fine") ? id.Substring(0, id.Length - 5) : id; }
    // a piece's score: how hard it is to make, times its quality
    public static int Score(string id)
    {
        int lv = RecipeLv(BaseOf(id)); if (lv <= 0) return 0;
        float q = id.EndsWith("_mw") || id.EndsWith("_master") ? 2f : id.EndsWith("_fine") ? 1.4f : 1f;
        return Mathf.RoundToInt((10 + lv * 4) * q);
    }
    public static string BestEntry(AHPlayer p)
    {
        string best = null; int bs = 0;
        foreach (var k in p.bag.order) { if (p.bag.Count(k) <= 0) continue; int sc = Score(k); if (sc > bs) { bs = sc; best = k; } }
        return best;
    }
    // the realm's other entries this month (the same for everyone: seeded by the month)
    public static List<KeyValuePair<string, int>> Field(AHPlayer p)
    {
        var rnd = new System.Random(AHDaily.MonthKey(DateTime.Now).GetHashCode());
        int top = 40 + Mathf.Clamp(p.BestMastery(), 0, 40) * 4;
        var l = new List<KeyValuePair<string, int>>();
        foreach (var n in Rivals) l.Add(new KeyValuePair<string, int>(n, Mathf.RoundToInt(top * (0.45f + (float)rnd.NextDouble() * 0.6f))));
        return l;
    }
    public static void RefreshContest(AHPlayer p)
    {
        var s = St(p); string mo = AHDaily.MonthKey(DateTime.Now);
        if (s.contestMonth != mo) { s.contestMonth = mo; s.contestScore = 0; s.contestRank = 0; s.contestItem = ""; s.contestClaimed = false; }
    }
    public static void Enter(AHGame g)
    {
        var p = g.player; var s = St(p); RefreshContest(p);
        if (s.contestScore > 0) return;
        string id = BestEntry(p); if (id == null) { if (g.ui != null) g.ui.Toast("Make something worth judging first: a weapon, armor, a meal or a potion."); return; }
        int sc = Score(id);
        if (!p.bag.Take(id)) return;
        s.contestItem = id; s.contestScore = sc;
        int rank = 1; foreach (var kv in Field(p)) if (kv.Value > sc) rank++;
        s.contestRank = rank;
        if (g.ui != null) g.ui.Banner("Masterwork Contest", "The judges place your " + AHItems.Get(id).name + " " + Ordinal(rank) + " of " + (Rivals.Length + 1));
        g.SaveProgress();
    }
    public static string Ordinal(int n) { return n + (n == 1 ? "st" : n == 2 ? "nd" : n == 3 ? "rd" : "th"); }
    public static AHDaily.Reward ContestReward(AHPlayer p, int rank)
    {
        long step = AHHome.LevelStep(p);
        if (rank == 1) return new AHDaily.Reward { money = (long)Math.Round(2500 * (1 + p.level / 4.0)), xp = step * 2, marks = 300, items = { new KeyValuePair<string, int>("diamond", 1), new KeyValuePair<string, int>("enh_stone", 4), new KeyValuePair<string, int>("lucky_charm", 2) } };
        if (rank <= 3) return new AHDaily.Reward { money = (long)Math.Round(1200 * (1 + p.level / 4.0)), xp = step, marks = 150, items = { new KeyValuePair<string, int>("ruby", 1), new KeyValuePair<string, int>("enh_stone", 2) } };
        return new AHDaily.Reward { money = (long)Math.Round(400 * (1 + p.level / 4.0)), xp = step / 3, marks = 50 };
    }
    public static void ClaimContest(AHGame g)
    {
        var p = g.player; var s = St(p); if (s.contestScore <= 0 || s.contestClaimed) return;
        s.contestClaimed = true;
        AHDaily.Grant(g, ContestReward(p, s.contestRank));
    }
}

public partial class AHUI
{
    string artTab = "story";
    public void OpenArtisan(string tab = null) { if (tab != null) artTab = tab; wkMode = "art"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderArtisan(AHPlayer p)
    {
        var st = AHArtisan.St(p); AHArtisan.RefreshCommission(p); AHArtisan.RefreshContest(p);
        wkTitle.text = "Artisans' Guild";
        wkHint.text = "Rank: " + AHArtisan.Rank(p) + " · best mastery " + p.BestMastery() + " · guild hall " + st.hall + "/4 rebuilt" + (p.path != "artisan" ? " · <color=#ffb070>the Saga is for Artisans (change path at a Guild Registrar)</color>" : "");
        var rows = new List<Action<int>>();
        Color cA = new Color(1f, 0.8f, 0.45f);
        rows.Add(s => Row(s, "Guild", cA, "", "",
            new WkBtn { label = "Saga", on = true, col = artTab == "story" ? Go : Plain, act = () => { artTab = "story"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Commission", on = true, col = artTab == "com" ? Go : Plain, act = () => { artTab = "com"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Contest", on = true, col = artTab == "contest" ? Go : Plain, act = () => { artTab = "contest"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Workshop", on = true, col = artTab == "shop" ? Go : Plain, act = () => { artTab = "shop"; wkPageI = 0; RenderWork(); } }));
        if (artTab == "story")
        {
            var s0 = AHArtisan.Current(p);
            if (s0 == null) rows.Add(s => Row(s, "The Saga is complete", Gold, "You are the Grand Artisan of Ashen Hollow.", "The guild hall stands and its crown is yours."));
            else
            {
                string id; bool give = AHArtisan.CanGive(p, s0, out id);
                bool isGive = s0.t == "give" || s0.t == "givefine" || s0.t == "givemw";
                rows.Add(s => Row(s, "Chapter " + (s0.ch + 1) + ": " + AHArtisan.Chapters[s0.ch], new Color(1f, 0.7f, 0.35f), "Step " + (AHArtisan.Saga.FindAll(x => x.ch == s0.ch).IndexOf(s0) + 1) + " of " + AHArtisan.Saga.FindAll(x => x.ch == s0.ch).Count, ""));
                // the story, wrapped over a few rows
                foreach (var line in Wrap(s0.story, 92)) { var ln = line; rows.Add(s => Row(s, "", Color.white, "<i>" + ln + "</i>", "")); }
                rows.Add(s => Row(s, s0.label, Color.white, (s0.t == "mastery" ? p.BestMastery() : st.sagaP) + " / " + s0.n + (isGive && id != null ? " · you have " + p.bag.Count(id) : ""), "",
                    isGive ? new WkBtn { label = "Hand in", on = give && p.path == "artisan", col = Go, act = () => { AHArtisan.Give(g); RenderWork(); } } : null));
            }
            for (int c = 0; c < AHArtisan.Chapters.Length; c++)
            {
                int cc = c; bool done = st.sagaI >= AHArtisan.Saga.Count || (AHArtisan.Current(p) != null && AHArtisan.Current(p).ch > c);
                rows.Add(s => Row(s, (done ? "✓ " : "") + "Chapter " + (cc + 1) + ": " + AHArtisan.Chapters[cc], done ? new Color(0.6f, 0.9f, 0.55f) : new Color(1f, 1f, 1f, 0.55f), "", ""));
            }
        }
        else if (artTab == "com")
        {
            var rw = AHArtisan.CommissionReward(p);
            rows.Add(s => Row(s, "This week: " + AHArtisan.Patron(), new Color(0.75f, 0.6f, 1f), st.comDone ? "Delivered. A new commission comes on Monday." : "Make these and deliver them together.", AHDaily.Text(rw),
                st.comDone ? null : new WkBtn { label = "Deliver", on = AHArtisan.CommissionReady(p), col = Go, act = () => { AHArtisan.DeliverCommission(g); RenderWork(); } }));
            foreach (var x in st.com) { var xx = x; var it = AHItems.Get(xx.id); rows.Add(s => RowI(it, s, (it != null ? it.name : xx.id) + " × " + xx.n, Color.white, "You have " + p.bag.Count(xx.id), "")); }
            if (st.com.Count == 0) rows.Add(s => Row(s, "No commission yet", Color.white, "Raise a crafting skill (smithing, tailoring, cooking, herblore, jewelcrafting or farming) to get one.", ""));
        }
        else if (artTab == "contest")
        {
            var nm = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1) - DateTime.Now;
            string best = AHArtisan.BestEntry(p);
            rows.Add(s => Row(s, "The Masterwork Contest", Gold, "Once a month: hand in your finest made piece. Harder recipes and Fine or Masterwork quality score higher.", "Closes in " + (int)nm.TotalDays + " days"));
            if (st.contestScore <= 0)
                rows.Add(s => Row(s, best != null ? "Your best entry: " + AHItems.Get(best).name : "Nothing worth judging yet", Color.white, best != null ? "Score " + AHArtisan.Score(best) + " · it is handed to the judges" : "Make weapons, armor, meals or potions", "",
                    new WkBtn { label = "Enter", on = best != null, col = Go, act = () => { AHArtisan.Enter(g); RenderWork(); } }));
            else
            {
                var rw = AHArtisan.ContestReward(p, st.contestRank);
                rows.Add(s => Row(s, "You placed " + AHArtisan.Ordinal(st.contestRank) + " with " + AHItems.Get(st.contestItem).name, st.contestRank == 1 ? Gold : Color.white, "Score " + st.contestScore + " · " + AHDaily.Text(rw), "",
                    st.contestClaimed ? null : new WkBtn { label = "Claim", on = true, col = Go, act = () => { AHArtisan.ClaimContest(g); RenderWork(); } }));
            }
            var field = AHArtisan.Field(p); field.Sort((a, b) => b.Value.CompareTo(a.Value));
            foreach (var kv in field) { var k2 = kv; rows.Add(s => Row(s, k2.Key, new Color(1f, 1f, 1f, 0.75f), "Score " + k2.Value, "")); }
        }
        else
        {
            foreach (var u in AHArtisan.Workshop)
            {
                var uu = u; int lv = AHArtisan.Lv(p, uu.id); long c = AHArtisan.Cost(p, uu.id);
                rows.Add(s => Row(s, uu.name + " · level " + lv + "/3", new Color(0.6f, 0.9f, 0.5f), uu.what, lv >= 3 ? "Fully upgraded" : "Next: " + AHItems.MoneyText(c),
                    lv >= 3 ? null : new WkBtn { label = "Upgrade", on = p.bag.money >= c, col = Go, act = () => { if (AHArtisan.Buy(g, uu.id)) { AHSound.Play("anvil"); Banner(uu.name, "Upgraded to level " + AHArtisan.Lv(p, uu.id)); } RenderWork(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    static List<string> Wrap(string s, int w)
    {
        var l = new List<string>(); var cur = "";
        foreach (var word in (s ?? "").Split(' '))
        {
            if (cur.Length + word.Length + 1 > w) { l.Add(cur); cur = word; }
            else cur = cur.Length == 0 ? word : cur + " " + word;
        }
        if (cur.Length > 0) l.Add(cur);
        return l;
    }
}
