// Ashen Hollow: friends of the realm and the realm's calendar, as in the web game (v66, FRIENDS / addFriendPts /
// giftValue / giveGift / sendLetter / DAY_BONUS).
// 22 townsfolk can become your friends: chat once a day (+12) and bring one gift a day (+60 for a favourite, +30 for
// something they like, +12 for food, +6 otherwise). Hearts at 50, 150, 300, 500 and 750 points; every heart sends a
// letter with a present. Every weekday has a small festival: a skill learns faster, and at the weekend (the Hearth
// Festival) friendship grows twice as fast.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHFriend { public string n; public int pts, h; public string talk, gift; }
[Serializable] public class AHLetter { public string from, text; public int h; public bool read, taken; public long money; public List<AHStack> items = new List<AHStack>(); }

public static class AHFriends
{
    static readonly int[] HeartAt = { 0, 50, 150, 300, 500, 750 };
    public static Dictionary<string, object> All { get { return AHDB.Table("npcs", "FRIENDS"); } }
    public static bool Is(string name) { return name != null && All != null && All.ContainsKey(name); }
    public static AHFriend St(AHPlayer p, string n) { foreach (var f in p.prog.friends) if (f.n == n) return f; var x = new AHFriend { n = n }; p.prog.friends.Add(x); return x; }
    public static int Hearts(AHPlayer p, string n) { int pts = 0; foreach (var f in p.prog.friends) if (f.n == n) pts = f.pts; int h = 0; while (h < 5 && pts >= HeartAt[h + 1]) h++; return h; }
    public static int Count(AHPlayer p, int lvl) { int c = 0; if (All != null) foreach (var k in All.Keys) if (Hearts(p, k) >= lvl) c++; return c; }
    public static string HeartStr(int h) { return "<color=#ff7aa8>" + new string('♥', h) + "</color><color=#7a6a70>" + new string('♡', 5 - h) + "</color>"; }

    // web DAY_BONUS
    public class DayB { public string name, blurb; public float fr = 1f, all; public string[] skills = new string[0]; public float k; }
    static readonly DayB[] Days =
    {
        new DayB { name = "Hearth Festival", blurb = "Sunday: friendship grows twice as fast and every skill learns 10% faster.", fr = 2f, all = 0.1f },
        new DayB { name = "Woodcutters’ Day", blurb = "Monday: Woodcutting learns 30% faster.", skills = new[] { "woodcutting" }, k = 0.3f },
        new DayB { name = "Miners’ Day", blurb = "Tuesday: Mining and Smithing learn 30% faster.", skills = new[] { "mining", "smithing" }, k = 0.3f },
        new DayB { name = "Anglers’ Day", blurb = "Wednesday: Fishing and Cooking learn 30% faster.", skills = new[] { "fishing", "cooking" }, k = 0.3f },
        new DayB { name = "Harvest Day", blurb = "Thursday: Farming and Herblore learn 30% faster.", skills = new[] { "farming", "herblore" }, k = 0.3f },
        new DayB { name = "Crafters’ Day", blurb = "Friday: Tailoring, Jewelcrafting and Skinning learn 30% faster.", skills = new[] { "tailoring", "jewelcrafting", "skinning" }, k = 0.3f },
        new DayB { name = "Hearth Festival", blurb = "Saturday: friendship grows twice as fast and every skill learns 10% faster.", fr = 2f, all = 0.1f },
    };
    public static DayB Today { get { return Days[(int)DateTime.Now.DayOfWeek]; } }
    public static float DayXp(string skill) { var b = Today; if (skill == "attack") return 1f; return 1f + b.all + (Array.IndexOf(b.skills, skill) >= 0 ? b.k : 0f); }

    static string Day { get { return DateTime.Now.ToString("yyyy-MM-dd"); } }

    public static void AddPts(AHGame g, string n, int pts)
    {
        var p = g.player; var st = St(p, n); int h0 = Hearts(p, n); float mult = Today.fr;
        int add = Mathf.RoundToInt(pts * mult);
        st.pts = Mathf.Min(HeartAt[5], st.pts + add);
        int h1 = Hearts(p, n);
        g.ui.Float(p.transform.position + Vector3.up * 2.4f, "+" + add + " friendship", new Color(1f, 0.6f, 0.75f));
        if (h1 > h0)
        {
            g.ui.Banner(n + " · " + new string('♥', h1), h1 == 5 ? "Best friends!" : "Friendship grew");
            for (int h = h0 + 1; h <= h1; h++) if (h > st.h) { Letter(g, n, h); st.h = h; }
            g.quests.Event("friend", "any", 1, g);
        }
        g.MarkDirty();
    }

    static void Letter(AHGame g, string n, int h)
    {
        var F = AHJson.O(All, n); string voice = AHJson.S(F, "voice", n), sig = voice == "the cat" ? "(a pawprint)" : voice;
        string text = h == 3 ? AHJson.S(F, "letter") : h == 5 ? AHJson.S(F, "best") : h == 1 ? "Thank you for stopping by. It is nice to have someone who listens. — " + sig : h == 2 ? "I keep a little something aside for when you visit. Here it is early. — " + sig : "You are always welcome. I mean that. — " + sig;
        string gift = AHJson.S(F, "gift");
        var l = new AHLetter { from = n, h = h, text = text };
        int[] gn = { 0, 2, 3, 4, 5, 6 }; long[] mo = { 0, 0, 300, 800, 2000, 5000 };
        if (AHItems.Get(gift) != null) l.items.Add(new AHStack { id = gift, n = gn[h] });
        if (h == 3) l.items.Add(new AHStack { id = "big_potion", n = 2 });
        if (h == 4) l.items.Add(new AHStack { id = "mystery_sack", n = 1 });
        if (h == 5) { l.items.Add(new AHStack { id = "diamond", n = 1 }); l.items.Add(new AHStack { id = "mystery_sack", n = 2 }); }
        l.money = mo[h];
        var L = g.player.prog.letters; L.Insert(0, l);
        while (L.Count > 40) { int i = L.FindLastIndex(x => x.taken); if (i < 0) break; L.RemoveAt(i); }
        g.ui.Toast("A letter from " + n + " arrived. Open Menu → Friends to read it.", 4f);
    }

    // web friendTalk: one chat a day
    public static void Talk(AHGame g, string n)
    {
        var st = St(g.player, n);
        if (st.talk != Day) { st.talk = Day; AddPts(g, n, 12); }
    }

    // web giftValue
    public static int GiftValue(string n, string id, out string k)
    {
        var F = AHJson.O(All, n); string b = id.EndsWith("_fine") ? id.Substring(0, id.Length - 5) : id.EndsWith("_master") ? id.Substring(0, id.Length - 7) : id.EndsWith("_mw") ? id.Substring(0, id.Length - 3) : id;
        var loves = AHJson.A(F, "loves"); var likes = AHJson.A(F, "likes"); var it = AHItems.Get(id);
        if (loves != null && loves.Contains(b)) { k = "love"; return 60; }
        if (likes != null && likes.Contains(b)) { k = "like"; return 30; }
        if (it != null && (it.IsFood || it.meal)) { k = "ok"; return 12; }
        k = "meh"; return 6;
    }
    public static bool Giftable(AHPlayer p, string id) { var it = AHItems.Get(id); return it != null && !it.cosmetic && !it.IsGear && !id.EndsWith("_seed") && p.bag.Count(id) > 0; }
    public static string Give(AHGame g, string n, string id)
    {
        var p = g.player; var st = St(p, n);
        if (!Is(n) || st.gift == Day || !p.bag.Take(id)) return null;
        st.gift = Day; string k; int pts = GiftValue(n, id, out k);
        var F = AHJson.O(All, n); bool cat = AHJson.S(F, "voice") == "the cat"; string nm = AHItems.Get(id).name;
        string line = k == "love" ? (cat ? "(The cat purrs so hard it falls over.)" : "Oh! " + nm + "! You remembered. This is my favourite.")
            : k == "like" ? (cat ? "(The cat sniffs it, approves, and walks off with it.)" : nm + "? How kind. Thank you.")
            : (cat ? "(The cat looks at it, then at you. It accepts, for now.)" : "Oh, for me? That is sweet of you.");
        AddPts(g, n, pts); g.quests.Event("gift", "any", 1, g); g.SaveProgress();
        return line;
    }
    public static void TakeLetter(AHGame g, AHLetter l)
    {
        var p = g.player; if (l.taken) return;
        var ids = new List<string>(); foreach (var s in l.items) ids.Add(s.id);
        if (!p.bag.Fits(ids)) { g.ui.Toast("Your bag is full. Make room first."); return; }
        foreach (var s in l.items) p.bag.Add(s.id, s.n);
        if (l.money > 0) p.AddMoney(l.money * AHDB.CU, p.transform.position);
        l.taken = true; g.SaveProgress();
    }
}

public partial class AHUI
{
    string friendSel, friendLine, friendTab = "list";
    public void OpenFriend(string n, string line) { friendSel = n; friendLine = line; wkMode = "friend"; wkPageI = 0; ShowWork(true); RenderWork(); }
    public void OpenFriends(string tab = "list") { friendTab = tab; wkMode = "friends"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderFriend(AHPlayer p)
    {
        string n = friendSel; var F = AHJson.O(AHFriends.All, n); var st = AHFriends.St(p, n); int h = AHFriends.Hearts(p, n);
        int[] at = { 0, 50, 150, 300, 500, 750 }; int next = at[Mathf.Min(5, h + 1)];
        wkTitle.text = n + "  " + AHFriends.HeartStr(h);
        var lv = AHJson.A(F, "loves"); var lk = AHJson.A(F, "likes");
        Func<List<object>, string> names = l => { var o = new List<string>(); if (l != null) foreach (var x in l) { var it = AHItems.Get((string)x); o.Add(it != null ? it.name : (string)x); } return string.Join(", ", o.ToArray()); };
        wkHint.text = (string.IsNullOrEmpty(friendLine) ? "" : friendLine + "  ") + (h >= 5 ? "Best friends." : "Friendship " + st.pts + " / " + next + ".") + " Loves: " + names(lv) + "." + (AHFriends.Today.fr > 1f ? " Hearth Festival today: friendship counts double!" : "");
        var rows = new List<Action<int>>();
        bool giver = g.quests.Current != null && g.quests.NpcName == n;
        if (giver) rows.Add(s => Row(s, "Quest: " + g.quests.Current.name, new Color(1f, 0.82f, 0.3f), "They have something for you.", "", new WkBtn { label = "Quest", on = true, col = Go, act = () => { ShowWork(false); OpenQuest(); } }));
        if (st.gift == DateTime.Now.ToString("yyyy-MM-dd")) rows.Add(s => Row(s, "You already gave a gift today", new Color(1f, 1f, 1f, 0.7f), "Come back tomorrow.", ""));
        else
        {
            var ids = new List<string>(); foreach (var id in p.bag.order) if (AHFriends.Giftable(p, id)) ids.Add(id);
            ids.Sort((a, b) => { string k; return AHFriends.GiftValue(n, b, out k).CompareTo(AHFriends.GiftValue(n, a, out k)); });
            foreach (var id in ids)
            {
                string ii = id, k; int pts = AHFriends.GiftValue(n, id, out k); var it = AHItems.Get(id);
                rows.Add(s => Row(s, it.name + (k == "love" ? "  <color=#ff7aa8>Loves it</color>" : k == "like" ? "  <color=#ffb0c8>Likes it</color>" : ""), AHItems.Quality(it), "You have " + p.bag.Count(ii) + " · +" + pts + " friendship", "",
                    new WkBtn { label = "Give", on = true, col = k == "love" ? Go : Plain, act = () => { var line = AHFriends.Give(g, n, ii); if (line != null) friendLine = line; RenderWork(); } }));
            }
            if (ids.Count == 0) rows.Add(s => Row(s, "Nothing to give", new Color(1f, 1f, 1f, 0.7f), "Food, fish, flowers and crafted goods make good gifts.", ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void RenderFriends(AHPlayer p)
    {
        var rows = new List<Action<int>>();
        int unread = p.prog.letters.FindAll(l => !l.taken).Count;
        rows.Add(s => Row(s, "Friends · Letters" + (unread > 0 ? " (" + unread + ")" : ""), new Color(1f, 0.6f, 0.75f), "Today: " + AHFriends.Today.name + ". " + AHFriends.Today.blurb, "",
            new WkBtn { label = "Friends", on = true, col = friendTab == "list" ? Go : Plain, act = () => OpenFriends("list") },
            new WkBtn { label = "Letters", on = true, col = friendTab == "letters" ? Go : Plain, act = () => OpenFriends("letters") }));
        if (friendTab == "list")
        {
            wkTitle.text = "Friends · " + AHFriends.Count(p, 1) + " friends · " + AHFriends.Count(p, 5) + " best friends";
            wkHint.text = "Talk to people every day and bring them gifts. Each heart sends you a letter with a present.";
            if (AHFriends.All != null)
                foreach (var kv in AHFriends.All)
                {
                    string n = kv.Key; int h = AHFriends.Hearts(p, n); var st = AHFriends.St(p, n);
                    var lv = AHJson.A(kv.Value, "loves"); var o = new List<string>(); if (lv != null) foreach (var x in lv) { var it = AHItems.Get((string)x); o.Add(it != null ? it.name.ToLowerInvariant() : (string)x); }
                    var town = AHRep.Get(AHJson.S(kv.Value, "town"));
                    rows.Add(s => Row(s, n + "  " + AHFriends.HeartStr(h), Color.white, (town != null ? town.name + " · " : "") + "loves " + string.Join(", ", o.ToArray()) + (st.gift == DateTime.Now.ToString("yyyy-MM-dd") ? " · gift given today" : ""), ""));
                }
        }
        else
        {
            wkTitle.text = "Letters";
            wkHint.text = "Letters from your friends, newest first. Each brings a present.";
            foreach (var l in p.prog.letters)
            {
                var ll = l; var parts = new List<string>(); foreach (var st in l.items) { var it = AHItems.Get(st.id); if (it != null) parts.Add(st.n + "× " + it.name); }
                if (l.money > 0) parts.Add(AHItems.MoneyText(l.money * AHDB.CU));
                rows.Add(s => Row(s, "From " + ll.from + "  " + AHFriends.HeartStr(ll.h), new Color(1f, 0.85f, 0.7f), ll.text, parts.Count > 0 ? "Enclosed: " + string.Join(", ", parts.ToArray()) : "",
                    parts.Count > 0 ? new WkBtn { label = ll.taken ? "Taken" : "Take", on = !ll.taken, col = Go, act = () => { AHFriends.TakeLetter(g, ll); RenderWork(); } } : null));
                l.read = true;
            }
            if (p.prog.letters.Count == 0) rows.Add(s => Row(s, "No letters yet", new Color(1f, 1f, 1f, 0.7f), "Make friends and they will write to you.", ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
