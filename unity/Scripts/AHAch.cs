// Ashen Hollow: achievements and titles (Deeds), as in the web game (v66, ACH / checkAch / titleText).
// Deeds unlock as you play (checked every 1.5 s); each one gives a title you can wear under your name.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHAch
{
    public class Deed { public string id, name, desc, title; public Func<AHPlayer, bool> test; }
    static List<Deed> all;
    static long K(AHPlayer p, string t) { return AHProgress.Get(p.prog.kills, t); }
    static long Total(AHPlayer p) { long n = 0; foreach (var e in p.prog.kills) n += e.v; return n; }
    static bool Found(AHPlayer p, string area) { return p.prog.found.Contains(area); }

    public static List<Deed> All
    {
        get
        {
            if (all != null) return all;
            all = new List<Deed>();
            Action<string, string, string, string, Func<AHPlayer, bool>> A = (id, n, d, t, f) => all.Add(new Deed { id = id, name = n, desc = d, title = t, test = f });
            A("firstblood", "First Blood", "Defeat your first monster.", "the Brave", p => Total(p) >= 1);
            A("hunter100", "Seasoned Hunter", "Defeat 100 monsters.", "the Relentless", p => Total(p) >= 100);
            A("sailor", "Across the Sea", "Set foot on Dragonscale Isle.", "the Seafarer", p => Found(p, "isle"));
            A("forge", "Forgebreaker", "Defeat Forgemaster Grull.", "Forgebreaker", p => K(p, "grull") >= 1);
            A("queen", "Queenslayer", "Defeat Sand Queen Ssera.", "Queenslayer", p => K(p, "sandqueen") >= 1);
            A("tyrant", "Tyrant’s End", "Defeat the Tyrant king.", "Primal Hunter", p => K(p, "tyrant") >= 1);
            A("dragon", "Dragonslayer", "Defeat Ignirax the Ashen Wyrm.", "Dragonslayer", p => K(p, "wyrm") >= 1);
            A("dinos", "Dino Hunter", "Defeat 25 dinosaurs.", "the Dino Hunter", p => K(p, "raptor") + K(p, "hornback") + K(p, "pterra") + K(p, "tyrant") >= 25);
            A("beast", "Beastmaster", "Own 3 pets.", "Beastmaster", p => p.pets.Count >= 3);
            A("rider", "Saddled Up", "Own a mount.", "the Rider", p => p.mounts.Count >= 1);
            A("party", "Band of Heroes", "Travel with a full party.", "Captain", p => p.party.Count >= AHComp.PartyMax);
            A("rich", "Silver Tongue", "Hold 1 silver in your purse.", "the Wealthy", p => p.bag.money >= 1000000L);
            A("rich2", "Golden Touch", "Hold 1 gold in your purse.", "Tycoon", p => p.bag.money >= 1000000000L);
            A("artisan", "Artisan", "Reach level 20 in any gathering or crafting skill.", "Artisan", p => { foreach (var kv in p.skillXp) if (kv.Key != "attack" && p.Skill(kv.Key) >= 20) return true; return false; });
            A("waywalker", "Waywalker", "Attune every waystone.", "Waywalker", p => AHWays.Ways != null && AHWays.KnownCount(p) >= AHWays.Ways.Count);
            A("treasure", "Treasure Hunter", "Find every hidden treasure chest.", "the Lucky", p => { var ts = AHWays.Treasures; if (ts == null) return false; foreach (var t in ts) if (!p.prog.found.Contains(AHJson.S(t, "id"))) return false; return true; });
            A("neighbour", "Good Neighbour", "Reach three hearts with three friends.", "the Good Neighbour", p => AHFriends.Count(p, 3) >= 3);
            A("beloved", "Beloved", "Become best friends (five hearts) with ten people.", "the Beloved", p => AHFriends.Count(p, 5) >= 10);
            A("fest_harvest", "The Harvester", "Earn 100 tokens at the Harvest Fair.", "the Harvester", p => p.prog.fests.Exists(f => f.id == "harvest" && f.earned >= 100));
            A("fest_lantern", "Lantern-bearer", "Earn 100 tokens during Lantern Nights.", "the Lantern-bearer", p => p.prog.fests.Exists(f => f.id == "lantern" && f.earned >= 100));
            A("fest_winter", "The Merry", "Earn 100 tokens at the Winter Feast.", "the Merry", p => p.prog.fests.Exists(f => f.id == "winter" && f.earned >= 100));
            A("tamer", "Master Tamer", "Beat all six pet tamers.", "Master Tamer", p => p.prog.pbTamers.Count >= 6);
            A("fashion", "Fashion Icon", "Collect 12 cosmetic pieces.", "the Fabulous", p => AHWardrobe.CosmeticCount(p) >= 12);
            A("merchant", "Merchant", "Earn 1 gold from your market stalls.", "the Merchant", p => { long e = 0; foreach (var s in p.prog.stalls) e += s.earned; return e >= 1000000; });
            A("bounty", "Bounty Hunter", "Complete 10 bounties.", "Bounty Hunter", p => p.prog.Stat("bounties") >= 10);
            A("islander", "Island Hopper", "Reach Coralport in the Tidewake Isles.", "the Islander", p => Found(p, "co_city"));
            A("pirates", "Pirate Hunter", "Defeat Captain Saltbeard.", "the Pirate Hunter", p => K(p, "saltbeard") >= 1);
            A("tidequeen", "Queen of the Deep", "Defeat Thalassa, the Tide Queen.", "Tidebreaker", p => K(p, "thalassa") >= 1);
            A("emberwalker", "Emberwalker", "Reach Cinderhold in Emberreach.", "the Emberwalker", p => Found(p, "ch_city"));
            A("moltenking", "Kingslayer", "Defeat Pyraxis, the Molten King.", "Kingslayer", p => K(p, "pyraxis") >= 1);
            A("homebody", "Home Sweet Home", "Build every comfort at your homestead.", "the Homebody", p => { var cs = AHHome.Comforts; if (p.home == null || cs == null) return false; foreach (var c in cs) if (!p.home.comf.Contains(AHJson.S(c, "id"))) return false; return true; });
            A("throne", "Throne Breaker", "Defeat Vaelor in the Ember Throne raid.", "Throne Breaker", p => K(p, AHRaid.Boss) >= 1);
            A("delver", "Delver", "Complete 5 Dungeon Finder runs.", "the Delver", p => AHFinder.Runs >= 5);
            A("wellfed", "Gourmand", "Eat a masterwork meal.", "the Gourmand", p => p.prog.Stat("master_meals") >= 1);
            A("artisan_road", "Master Artisan", "Walk the whole Artisan’s Road.", "Master Artisan", p => { var r = AHQuestLog.Road; return r != null && AHGame.I != null && AHGame.I.quests.roadI >= r.Count; });
            foreach (var f in AHRep.Factions) { var ff = f; A("exalted_" + f.id, "Exalted: " + f.name, "Reach Exalted with " + f.name + ".", "Champion of " + f.name, p => AHRep.Rank(p, ff.id) >= 4); }
            var profs = AHPlayer.Profs as Dictionary<string, object>;
            string[] rk = { "Journeyman", "Expert", "Master", "Grandmaster", "Legendary" }; int[] lv = { 10, 20, 30, 40, 50 };
            if (profs != null)
                foreach (var kv in profs)
                {
                    string k = kv.Key, pn = AHJson.S(kv.Value, "name", k);
                    for (int i = 0; i < rk.Length; i++) { int L = lv[i]; A("prof_" + k + "_" + L, rk[i] + " " + pn, "Reach " + pn + " Mastery " + L + ".", rk[i] + " " + pn, p => p.MasteryLv(k) >= L); }
                }
            return all;
        }
    }
    public static Deed Get(string id) { return All.Find(d => d.id == id); }
    public static string Title(AHPlayer p) { var d = p.prog.title != null ? Get(p.prog.title) : null; return d != null && p.prog.ach.Contains(d.id) ? d.title : null; }

    static float t;
    public static void Tick(AHGame g, float dt)
    {
        var p = g.player; if (p == null || p.cls == null) return;
        if (!p.prog.found.Contains(AHGame.AreaId)) { p.prog.found.Add(AHGame.AreaId); g.MarkDirty(); }
        t -= dt; if (t > 0f) return; t = 1.5f;
        foreach (var a in All)
            if (!p.prog.ach.Contains(a.id) && a.test(p))
            {
                p.prog.ach.Add(a.id);
                g.ui.Banner(a.name, "Achievement unlocked");
                g.ui.Toast("New title unlocked: “" + a.title + "”. Pick it in Menu → Deeds.", 4f);
                g.SaveProgress();
                break;
            }
    }
}

public partial class AHUI
{
    public void OpenDeeds() { wkMode = "deeds"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderDeeds(AHPlayer p)
    {
        var deeds = AHAch.All;
        wkTitle.text = "Deeds · " + p.prog.ach.Count + " / " + deeds.Count;
        string cur = AHAch.Title(p);
        wkHint.text = "Every deed gives a title you can wear under your name." + (cur != null ? " Wearing: “" + cur + "”." : "");
        var rows = new List<Action<int>>();
        var order = new List<AHAch.Deed>(deeds.FindAll(d => p.prog.ach.Contains(d.id))); order.AddRange(deeds.FindAll(d => !p.prog.ach.Contains(d.id)));
        foreach (var d in order)
        {
            var dd = d; bool got = p.prog.ach.Contains(d.id), on = p.prog.title == d.id;
            rows.Add(s => Row(s, (got ? "" : "<color=#7a7068>") + dd.name + (got ? "" : "</color>"), got ? new Color(1f, 0.8f, 0.45f) : new Color(0.6f, 0.55f, 0.5f), dd.desc, "Title: “" + dd.title + "”" + (on ? "  <color=#9be37a>worn</color>" : ""),
                got ? new WkBtn { label = on ? "Remove" : "Wear title", on = true, col = on ? Plain : Go, act = () => { p.prog.title = on ? null : dd.id; RefreshClass(); g.MarkDirty(); RenderWork(); } } : null));
        }
        rows.Add(s => Row(s, "Back", new Color(1f, 1f, 1f, 0.7f), "", "", new WkBtn { label = "Menu", on = true, col = Plain, act = OpenHub }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
