// Ashen Hollow: the Dungeon Finder, as in the web game (v67, dfQueue / dfGoNow / dfStart / roleFx / dfHealPulse / dfOnKill).
// Pick a role (tank, healer or damage) and a dungeon or the raid. In the web game other heroes queued for the same
// dungeon are matched; here, as in the web game when no one else is online, every open role is filled by a follower
// (a sellsword) and you go straight in. Your own sellswords wait outside and come back when the run ends.
// Tanks and healers get a Satchel of Helpful Goods after the boss; damage dealers get a smaller clear bonus.
// Also the MENU hub on the HUD: the finder, companions, work orders, the homestead.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHFinder
{
    public const string Raid = "r_throne";
    // web ROLES: name, what it does, the follower that fills it, its mark
    public static readonly string[] RoleIds = { "tank", "healer", "damage" };
    public static string RoleName(string r) { return r == "tank" ? "Tank" : r == "healer" ? "Healer" : "Damage"; }
    public static string RoleText(string r)
    {
        return r == "tank" ? "Takes the hits: +30% HP, 20% less damage taken, 15% less damage dealt"
            : r == "healer" ? "Heals the group every few seconds: 20% less damage dealt, +25% healing"
            : "Deals the damage: +15% damage";
    }
    static string Follower(string r) { return r == "tank" ? "knight" : r == "healer" ? "cleric" : "ranger"; }

    const string RoleKey = "ah_df_role", RunKey = "ah_df", RunsKey = "ah_df_runs";
    public static string Pick = "";   // the role chosen in the window (empty: by class)
    public static string Run = "", Role = "";   // the group run you are in
    static List<string> saved = new List<string>();
    static bool done;
    static float healT = 3f;

    public static int Runs { get { return AHPrefs.GetInt(RunsKey, 0); } }
    public static string DefaultRole(AHPlayer p) { return p.cls.id == "warrior" || p.cls.id == "warden" ? "tank" : p.cls.id == "priest" || p.cls.id == "druid" || p.cls.id == "shaman" ? "healer" : "damage"; }
    public static string ChosenRole(AHPlayer p) { if (Pick == "") Pick = AHPrefs.GetString(RoleKey, ""); return Pick != "" ? Pick : DefaultRole(p); }
    public static void SetRole(string r) { Pick = r; AHPrefs.SetString(RoleKey, r); }
    public static bool In { get { return Run != "" && AHGame.AreaId == AreaOf(Run); } }
    static string AreaOf(string id) { return id == Raid ? AHRaid.Area : id; }
    static int Size(string id) { return id == Raid ? 8 : 4; }
    public static string NameOf(string id) { return id == Raid ? "The Ember Throne" : AHJson.S(AHDungeon.Def(id), "name", id); }

    // web dfCanQueue
    public static bool CanQueue(AHGame g, string id, out string why)
    {
        why = "";
        if (id == Raid) { if (g.player.level < AHRaid.MinLevel) { why = "Level " + AHRaid.MinLevel + "+"; return false; } return true; }
        return AHDungeon.Status(g, id, out why);
    }

    // web dfFollowersFor: one of each open role first, at most three
    static List<string> FollowersFor(string role, string id)
    {
        var need = id == Raid ? new Dictionary<string, int> { { "tank", 2 }, { "healer", 2 }, { "damage", 4 } } : new Dictionary<string, int> { { "tank", 1 }, { "healer", 1 }, { "damage", 2 } };
        need[role] = Mathf.Max(0, need[role] - 1);
        var out1 = new List<string>();
        for (int i = 0; i < 8; i++) foreach (var r in RoleIds) if (need[r] > i) out1.Add(Follower(r));
        return out1.GetRange(0, Mathf.Min(out1.Count, Mathf.Min(3, Size(id) - 1)));
    }

    // web dfGoNow / dfStart: no one else is queued, so followers fill the group and in you go
    public static void Go(AHGame g, string id)
    {
        var p = g.player; string why;
        if (In) { g.ui.Toast("You are already in a group run."); return; }
        if (!CanQueue(g, id, out why)) { g.ui.Toast(why); return; }
        if (p.dead) return;
        if (id != Raid && !AHDungeon.CanEnter(g, id)) return;
        if (Run == "") saved = new List<string>(p.party);
        Run = id; Role = ChosenRole(p); done = false;
        p.party.Clear(); p.party.AddRange(FollowersFor(Role, id));
        Store();
        p.Recalc(); p.hp = p.maxHp;
        g.SaveProgress();
        if (id == Raid) AHRaid.Enter(g);
        else
        {
            var z = AHJson.O(AHDungeon.Def(id), "z");
            g.Travel(id, ((float)AHJson.N(z, "x") + 500) * AHDB.S, ((float)AHJson.N(z, "y") + 700) * AHDB.S, 0f);
        }
    }

    static void Store() { AHPrefs.SetString(RunKey, Run == "" ? "" : Run + "|" + Role + "|" + (done ? "1" : "0") + "|" + string.Join(",", saved.ToArray())); }
    static void Restore()
    {
        var s = AHPrefs.GetString(RunKey, "").Split('|');
        if (s.Length < 4 || s[0] == "") { Run = ""; return; }
        Run = s[0]; Role = s[1]; done = s[2] == "1";
        saved = new List<string>(); foreach (var k in s[3].Split(',')) if (k != "" && AHJson.Has(AHComp.Mercs, k)) saved.Add(k);
    }

    // every area: still in the run's area? if not, the run is over and your own sellswords come back (web dfEnd)
    public static void Setup(AHGame g)
    {
        Restore();
        if (Run == "") return;
        var p = g.player;
        if (AHGame.AreaId != AreaOf(Run)) { End(g, "Your group run of " + NameOf(Run) + " is over. Your own companions are back."); return; }
        p.party.Clear(); p.party.AddRange(FollowersFor(Role, Run));
        p.Recalc();
        var fol = p.party.ConvertAll(k => AHComp.MercName(k));
        g.ui.Banner(NameOf(Run) + " · group", "1 hero + " + fol.Count + " follower" + (fol.Count != 1 ? "s" : "") + " · you are the " + RoleName(Role));
        g.ui.Toast("Followers: " + string.Join(", ", fol.ToArray()) + ".", 4f);
    }

    public static void End(AHGame g, string msg)
    {
        if (Run == "") return;
        var p = g.player;
        p.party.Clear(); foreach (var k in saved) if (p.party.Count < AHComp.PartyMax) p.party.Add(k);
        Run = ""; Role = ""; saved.Clear(); Store();
        p.Recalc();
        if (msg != null) g.ui.Toast(msg, 4f);
        g.MarkDirty();
    }

    // leave the group from the menu: walk out the dungeon's way out (or the raid's)
    public static void Leave(AHGame g)
    {
        if (!In) { End(g, "You left the group."); return; }
        if (Run == Raid) { AHRaid.Leave(g); return; }
        foreach (var s in AHGather.Spots)
            if (s.kind == "gate" && s.gate != null && s.gate.to != AHGame.AreaId && string.IsNullOrEmpty(s.gate.dung)) { g.Travel(s.gate.to, s.gate.tx, s.gate.tz, s.gate.f); return; }
        End(g, "You left the group.");
    }

    // ---------- roles (web roleFx / dfTakenK) ----------
    public static void Apply(AHStats s)
    {
        if (!In) return;
        if (Role == "tank") s.dmg -= 0.15f;
        else if (Role == "healer") { s.dmg -= 0.2f; s.heal += 0.25f; }
        else s.dmg += 0.15f;
    }
    public static float HpK { get { return In && Role == "tank" ? 1.3f : 1f; } }
    public static float TakenK(AHGame g)
    {
        if (!In) return 1f;
        if (Role == "tank") return 0.8f;
        foreach (var a in AHComp.Allies) if (a != null && !a.dead && a.id == "knight" && Vector3.Distance(a.transform.position, g.player.transform.position) < 700f * AHDB.S) return 0.75f;
        return 1f;
    }

    // web dfHealPulse: the healer mends the most hurt of the group every 3 s
    public static void Tick(AHGame g, float dt)
    {
        if (!In || Role != "healer" || g.player == null || g.player.dead) return;
        healT -= dt; if (healT > 0f) return; healT = 3f;
        var p = g.player; float best = p.hp / p.maxHp; AHAlly who = null;
        foreach (var a in AHComp.Allies) if (a != null && !a.dead && a.hp / a.maxHp < best) { best = a.hp / a.maxHp; who = a; }
        if (best >= 0.92f) return;
        float k = 0.12f;
        if (who == null) p.Heal(p.maxHp * k);
        else
        {
            float n = Mathf.Round(who.maxHp * k * (1f + p.stat.heal)); who.hp = Mathf.Min(who.maxHp, who.hp + n);
            g.ui.Float(who.transform.position + Vector3.up * 2.2f, "+" + n, new Color(0.54f, 0.94f, 0.54f));
            AHFx.Pillar(who.transform.position, 0.6f, 2.2f, new Color(0.5f, 1f, 0.5f, 0.5f), 0.6f);
        }
    }

    // web dfOnKill: the boss of the run fell
    public static void OnMobDown(AHGame g, AHMob m)
    {
        if (!In || done || m.add) return;
        bool boss = Run == Raid ? m.type.id == AHRaid.Boss : m.type.id == AHDungeon.BossOf(Run);
        if (!boss) return;
        done = true; Store();
        var p = g.player; float tier = Run == Raid ? 4f : (float)AHJson.N(AHDungeon.Def(Run), "tier", 1);
        if (Role != "damage")
        {
            long c = (long)Mathf.Round(600f * tier * tier) * AHDB.CU; string[] gems = { "sapphire", "ruby", "emerald", "diamond" }; string gm = gems[UnityEngine.Random.Range(0, 4)];
            p.AddMoney(c, m.transform.position); p.bag.Add(gm, 2); p.bag.Add("enh_stone", 2);
            var gi = AHItems.Get(gm);
            g.ui.Toast("Satchel of Helpful Goods! Thanks for queueing as " + RoleName(Role) + ": " + AHItems.MoneyText(c) + ", 2 " + (gi != null ? gi.name : gm) + "s and 2 enhancement stones.", 6f);
        }
        else
        {
            long c = (long)Mathf.Round(200f * tier * tier) * AHDB.CU;
            p.AddMoney(c, m.transform.position);
            g.ui.Toast("Group clear bonus: " + AHItems.MoneyText(c) + ".", 4f);
        }
        AHPrefs.SetInt(RunsKey, Runs + 1);
        g.SaveProgress();
    }
}

public partial class AHUI
{
    RectTransform menuBtn;

    void BuildMenuHud()
    {
        menuBtn = Img("MenuBtn", transform, circle, new Vector2(1, 1), new Vector2(-322, -55), new Vector2(76, 76), new Color(0.2f, 0.14f, 0.1f, 0.9f));
        Img("Rim", menuBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 76), new Color(0.85f, 0.6f, 0.25f, 1f));
        Center(Label(menuBtn, "T", "MENU", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(76, 30), Color.white));
        taps.Add(new TapBtn { rt = menuBtn, act = OpenHub });
    }

    // ---------- the hub ----------
    public void OpenHub() { wkMode = "hub"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderHub(AHPlayer p)
    {
        wkTitle.text = "Menu";
        wkHint.text = "Level " + p.level + " " + p.cls.name + " · " + AHItems.MoneyText(p.bag.money) + (AHFinder.In ? " · in a group run of " + AHFinder.NameOf(AHFinder.Run) + " as " + AHFinder.RoleName(AHFinder.Role) : "");
        var rows = new List<Action<int>>();
        // the map has its own button on the top row now; the class can be changed here
        rows.Add(s => Row(s, "Settings", new Color(0.8f, 0.8f, 1f), "Sound, graphics, camera, your account", "",
            new WkBtn { label = "Open", on = true, col = Go, act = OpenSettings }));
        rows.Add(s => Row(s, "Emotes", new Color(1f, 0.55f, 0.9f), "Wave, bow, dance, sit and more", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenEmotes }));
        rows.Add(s => Row(s, "Artisans' Guild", new Color(0.6f, 0.9f, 0.5f), p.path == "artisan" ? AHArtisan.Rank(p) + " · the Saga, commissions, the Masterwork Contest, your workshop" : "Commissions, the Masterwork Contest and workshop upgrades", "",
            new WkBtn { label = "Open", on = true, col = p.path == "artisan" ? Go : Plain, act = () => OpenArtisan() }));
        rows.Add(s => Row(s, "Library, tournament, helpers", new Color(0.56f, 0.85f, 1f), "Lore pages · the weekend tournament · helpers who work while you are away", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = () => OpenWorld() }));
        rows.Add(s => Row(s, "Loadouts and bag", new Color(0.61f, 0.89f, 1f), "Gear sets in one tap · " + p.bag.SlotsMax + " bag slots · weapon mastery", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenLoadouts }));
        rows.Add(s => Row(s, "Change class", p.cls.color, "Now: " + p.cls.name + " · switch to another class", "",
            new WkBtn { label = "Choose", on = true, col = Plain, act = () => { ShowWork(false); ShowPicker(true); } }));
        rows.Add(s => Row(s, "Class: " + AHEvo.Title(p), p.cls.color, "Evolution at 30 and 60 · talents (" + (AHEvo.Points(p) - AHEvo.Spent(p)) + " free) · spellbook", "",
            new WkBtn { label = "Open", on = true, col = AHEvo.Points(p) > AHEvo.Spent(p) || (p.level >= 30 && AHEvo.CP(p).path == null) || (p.level >= 60 && AHEvo.CP(p).form == null) ? Go : Plain, act = () => OpenClassWin() }));
        rows.Add(s => Row(s, "Dungeon Finder", new Color(1f, 0.8f, 0.45f), "Pick a role and a dungeon or the raid; followers fill the open roles.", "Group runs completed: " + AHFinder.Runs,
            new WkBtn { label = "Open", on = true, col = Go, act = OpenFinder }));
        rows.Add(s => Row(s, "Story: The Ashen King", new Color(1f, 0.75f, 0.4f), AHStory.Done(p) ? "Done · you are Kingsbane" : "Chapter " + (p.prog.storyCh + 1) + " of " + AHStory.Chapters.Length + " · " + AHStory.Chapters[Mathf.Min(p.prog.storyCh, AHStory.Chapters.Length - 1)].name, AHStory.Status(p),
            new WkBtn { label = "Open", on = true, col = AHStory.Done(p) ? Plain : Go, act = OpenStory }));
        rows.Add(s => Row(s, "Fishing derby", new Color(0.62f, 0.85f, 1f), AHDerby.Best > 0f ? "Your best this week: " + AHDerby.Best.ToString("0.00") + " kg" : "Every fish is weighed · beat the week's leader for a prize", "",
            new WkBtn { label = "Board", on = true, col = Plain, act = OpenDerby }));
        rows.Add(s => Row(s, "Cook-off", new Color(1f, 0.7f, 0.45f), "This week: " + AHCookOff.Score + " points · streak " + AHCookOff.Streak + " · beat the leader for a prize", "",
            new WkBtn { label = "Board", on = true, col = Plain, act = OpenCookOff }));
        rows.Add(s => Row(s, "Talent sets", p.cls.color, "Save up to 3 sets of talents and switch in one tap", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenTalSets }));
        rows.Add(s => Row(s, "Tales of the townsfolk", new Color(1f, 0.85f, 0.6f), "Baker Maudie, Woodcarver Ansel and Little Tess each have a story", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenTales }));
        rows.Add(s => Row(s, "The Ashen Deep", new Color(0.75f, 0.55f, 1f),
            AHDeep.InRun ? "You are on floor " + AHDeep.Floor + " of " + AHDeep.Floors + " · " + AHDeep.Clock(AHDeep.Elapsed) : "This week: " + AHDeep.WeekBoss.name + " · " + AHDeep.WeekTwist.name,
            (AHDeep.Best > 0 ? "Best clear " + AHDeep.Clock(AHDeep.Best) + " · " : "") + AHDeep.Clears + " clears · " + (AHDeep.BossLooted ? "weekly reward taken" : "weekly reward waiting"),
            new WkBtn { label = AHDeep.InRun ? "In a run" : "Enter", on = !AHDeep.InRun && p.level >= AHDeep.MinLevel && !AHFinder.In, col = Go, act = () => { ShowWork(false); AHDeep.Start(g); } },
            new WkBtn { label = "Records", on = true, col = Plain, act = OpenDeepHall }));
        if (AHFinder.Run != "")
            rows.Add(s => Row(s, "Leave the group", new Color(1f, 0.55f, 0.45f), AHFinder.NameOf(AHFinder.Run) + " · " + AHFinder.RoleName(AHFinder.Role), "Your own companions come back when you leave.",
                new WkBtn { label = "Leave", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { ShowWork(false); AHFinder.Leave(g); } }));
        rows.Add(s => Row(s, "Daily goals", new Color(1f, 0.85f, 0.5f), "Daily goals, weekly challenges, the login calendar · " + p.prog.daily.marks + " marks", "",
            new WkBtn { label = "Open", on = true, col = Go, act = OpenDaily }));
        rows.Add(s => Row(s, "Deeds", new Color(1f, 0.8f, 0.45f), p.prog.ach.Count + " / " + AHAch.All.Count + " achievements" + (AHAch.Title(p) != null ? " · title “" + AHAch.Title(p) + "”" : ""), "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenDeeds }));
        { var F = AHFest.Now(); if (F != null) rows.Add(s => Row(s, F.name, F.col, F.blurb, "Tokens: " + AHFest.State(p, F).tok + " · the stall is by the waystone in Ashen Hollow and Varrow", new WkBtn { label = "Open", on = true, col = Go, act = OpenFest })); }
        rows.Add(s => Row(s, "Friends", new Color(1f, 0.6f, 0.75f), AHFriends.Count(p, 1) + " friends · " + p.prog.letters.FindAll(l => !l.taken).Count + " letters to open · today: " + AHFriends.Today.name, "",
            new WkBtn { label = "Open", on = true, col = p.prog.letters.Exists(l => !l.taken) ? Go : Plain, act = () => OpenFriends(p.prog.letters.Exists(l => !l.taken) ? "letters" : "list") }));
        rows.Add(s => Row(s, "Reputation", new Color(0.56f, 0.85f, 1f), "Your standing with the seven towns.", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenRep }));
        rows.Add(s => Row(s, "Enhance gear", new Color(0.61f, 0.89f, 1f), "Raise weapons and armor to +9 · " + p.bag.Count("enh_stone") + " stones · " + p.bag.Count("lucky_charm") + " lucky charms", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenEnh }));
        rows.Add(s => Row(s, "Stats", new Color(1f, 0.8f, 0.45f), "Damage, crit, evasion, speed and their caps", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenStats }));
        rows.Add(s => Row(s, "Gem sockets", new Color(0.61f, 0.89f, 1f), p.prog.gems.Count + " gems set", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenGems }));
        rows.Add(s => Row(s, "Collection", new Color(0.56f, 0.85f, 1f), p.prog.cards.Count + " monster cards · licenses", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenCollection }));
        rows.Add(s => Row(s, "Pet battles", new Color(1f, 0.62f, 0.24f), "Turn-based battles with your pets · " + p.prog.pbTamers.Count + "/6 tamers beaten", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenPetBattles }));
        rows.Add(s => Row(s, "Wardrobe", new Color(0.85f, 0.55f, 1f), AHWardrobe.CosmeticCount(p) + " cosmetics · " + p.prog.coll.Count + " pieces collected · " + p.prog.outfits.Count + " outfits", "Choose what each slot shows. Stats stay with your gear.",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenWardrobe }));
        rows.Add(s => Row(s, "Companions", new Color(1f, 0.62f, 0.24f), p.pets.Count + " pets · " + p.mounts.Count + " mounts · " + p.party.Count + " in your party", "",
            new WkBtn { label = "Open", on = true, col = Plain, act = OpenComp }));
        rows.Add(s => Row(s, "Work orders", new Color(1f, 0.8f, 0.45f), p.profMain == null ? "Take up a profession first" : p.orders.Count + " orders · guild rank " + AHOrders.RankName[AHOrders.Rank(p)], "",
            new WkBtn { label = "Open", on = p.profMain != null, col = Plain, act = OpenOrders }));
        rows.Add(s => Row(s, "Your homestead", new Color(0.6f, 0.9f, 0.5f), p.home == null ? "Buy a deed from a Land Agent in Varrow or Ashen Hollow." : AHGame.AreaId == AHHome.Area ? "You are home." : "Travel home: fields, pens, house and stall.", "",
            new WkBtn { label = "Go home", on = p.home != null && AHGame.AreaId != AHHome.Area && !AHFinder.In, col = Go, act = () => { ShowWork(false); AHHome.TravelHome(g); } }));
        rows.Add(s => Row(s, "Change hero / Log out", new Color(1f, 0.6f, 0.75f), "Play another hero on this device, or make a new one", "",
            new WkBtn { label = "Log out", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { ShowWork(false); g.Logout(); } }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    // ---------- web finderHtml ----------
    public void OpenFinder() { wkMode = "finder"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderFinder(AHPlayer p)
    {
        string role = AHFinder.ChosenRole(p);
        wkTitle.text = "Dungeon Finder";
        wkHint.text = "Role: " + AHFinder.RoleName(role) + ". " + AHFinder.RoleText(role) + ". Open roles are filled by followers, so you can always go. Tanks and healers get a Satchel of Helpful Goods after the boss.";
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, "Your role: " + AHFinder.RoleName(role), new Color(1f, 0.8f, 0.45f), "Tap a role to switch. Group runs completed: " + AHFinder.Runs, "",
            new WkBtn { label = "Tank", on = true, col = role == "tank" ? Go : Plain, act = () => { AHFinder.SetRole("tank"); RenderWork(); } },
            new WkBtn { label = "Healer", on = true, col = role == "healer" ? Go : Plain, act = () => { AHFinder.SetRole("healer"); RenderWork(); } },
            new WkBtn { label = "Damage", on = true, col = role == "damage" ? Go : Plain, act = () => { AHFinder.SetRole("damage"); RenderWork(); } }));
        var l = AHDB.List("events", "DUNGEONS");
        if (l != null)
            foreach (var d in l)
            {
                string id = AHJson.S(d, "id"), why; bool ok = AHFinder.CanQueue(g, id, out why);
                string lv = AHJson.S(d, "lv", "1"); int lo; int.TryParse(lv.Split('–', '-')[0], out lo);
                var bm = AHJson.O(AHDB.Mobs, AHDungeon.BossOf(id));
                rows.Add(s => Row(s, AHJson.S(d, "name", id) + "  <color=#c8b68a>Level " + lv + "</color>", Color.white,
                    ok ? AHJson.S(bm, "name", "") + (p.level < lo ? " · <color=#ff8a7a>above your level</color>" : "") : "<color=#ff8a7a>" + why + "</color>", "",
                    new WkBtn { label = "Go now", on = ok && !AHFinder.In, col = Go, act = () => { ShowWork(false); AHFinder.Go(g, id); } }));
            }
        {
            string why; bool ok = AHFinder.CanQueue(g, AHFinder.Raid, out why);
            rows.Add(s => Row(s, "Raid: The Ember Throne  <color=#c8b68a>Level 50–90</color>", new Color(1f, 0.55f, 0.3f),
                ok ? "Vaelor, the Ember King · up to 8: 2 tanks, 2 healers, 4 damage" : "<color=#ff8a7a>" + why + "</color>",
                AHRaid.Looted ? "<color=#ff8a7a>Looted this week · resets on Monday</color>" : "Kingsflame set: shoulders, legs, boots, cape · once a week",
                new WkBtn { label = "Go now", on = ok && !AHFinder.In, col = Go, act = () => { ShowWork(false); AHFinder.Go(g, AHFinder.Raid); } }));
        }
        rows.Add(s => Row(s, "Back", new Color(1f, 1f, 1f, 0.7f), "", "", new WkBtn { label = "Menu", on = true, col = Plain, act = OpenHub }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
