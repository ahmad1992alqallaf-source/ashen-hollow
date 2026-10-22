// Ashen Hollow: the MENU, sorted. Instead of one long list of everything, the menu opens on eight picture tiles:
// Hero, Adventure, Events & fun, Crafting & trade, Companions & home, Collections, Store & pass, Friends & settings.
// Each tile opens its own page of smaller tiles (a picture, a name, one line of where you are). A green badge on a
// tile counts what is waiting for you inside (rewards to claim, talent points, quests to hand in), and the tiles with
// something waiting glow. "◀ Menu" (or Esc) goes back to the eight.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    class HubE { public string cat, title, line, icon; public Color col = Color.white; public bool ready, on = true; public Action act; }
    class HubCat { public string id, name, blurb, icon, ar, arB; public Color col; }
    static readonly HubCat[] HubCats =
    {
        new HubCat { id = "hero", name = "Hero", ar = "بطلك", arB = "الإحصاءات · الفئة والمواهب · العتاد · المظهر", blurb = "Stats · class & talents · gear · looks", icon = "steel_helm", col = new Color(1f, 0.8f, 0.45f) },
        new HubCat { id = "adv", name = "Adventure", ar = "المغامرة", arB = "القصة · الزنزانات · الأعماق · الأهداف اليومية", blurb = "Story · dungeons · the Deep · daily goals", icon = "steel_sword", col = new Color(1f, 0.6f, 0.4f) },
        new HubCat { id = "fun", name = "Events & fun", ar = "الفعاليات والمرح", arB = "مسابقة الصيد · الطبخ · البطولة · المهرجانات", blurb = "Derby · cook-off · tournament · festivals", icon = "lucky_charm", col = new Color(0.62f, 0.85f, 1f) },
        new HubCat { id = "craft", name = "Crafting & trade", ar = "الحِرف والتجارة", arB = "الحرفيون · أوامر العمل · القوافل · الإصلاح", blurb = "Artisans · work orders · caravans · repairs", icon = "tool_pick_bronze", col = new Color(0.6f, 0.9f, 0.5f) },
        new HubCat { id = "home", name = "Companions & home", ar = "الرفاق والمنزل", arB = "الحيوانات والمطايا · معارك الحيوانات · المزرعة", blurb = "Pets & mounts · pet battles · homestead", icon = "reins_horse", col = new Color(1f, 0.62f, 0.3f) },
        new HubCat { id = "coll", name = "Collections", ar = "المجموعات", arB = "الإنجازات · كتاب الوحوش · البطاقات · السمعة", blurb = "Deeds · monster book · cards · reputation", icon = "card_wolf", col = new Color(0.56f, 0.85f, 1f) },
        new HubCat { id = "store", name = "Store & pass", ar = "المتجر والتذكرة", arB = "متجر المظهر فقط · تذكرة المعركة · كبار الشخصيات", blurb = "Looks-only store · Battle Pass · VIP", icon = "gold_crown", col = new Color(1f, 0.85f, 0.4f) },
        new HubCat { id = "acct", name = "Friends & settings", ar = "الأصدقاء والإعدادات", arB = "الأصدقاء والرسائل · الإعدادات · الخروج", blurb = "Friends & letters · settings · log out", icon = "cg:chat", col = new Color(1f, 0.6f, 0.75f) },
    };
    string hubCat, hubFrom;   // hubFrom: the category a window was opened from (its "◀ Back" returns there)
    // a window opened from the menu: a "◀ Back" at its bottom left, back to the category
    void HubBack()
    {
        if (hubFrom == null || wkMode == "hub" || wkMode == "hubcat") return;
        var back = Img("Back", wkPanel, white, new Vector2(0f, 0f), new Vector2(92f, 38f), new Vector2(150, 50), new Color(0.22f, 0.16f, 0.11f, 1f));
        wkItems.Add(back.gameObject);
        Center(Label(back, "T", AHLoc.T("◀ Back", "رجوع ▶"), 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 36), Color.white));
        string cat = hubFrom;
        taps.Add(new TapBtn { rt = back, layer = 5, group = "work", act = () => { hubCat = cat; hubFrom = null; wkMode = "hubcat"; wkPageI = 0; ShowWork(true); RenderWork(); } });
    }
    public void TestTile(string title) { var e = HubEntries(g.player).Find(x => x.title == title); if (e == null || e.act == null) return; hubFrom = e.cat; e.act(); if (!WorkOpen) hubFrom = null; }
    public void OpenHubCat(string id) { hubCat = id; wkMode = "hubcat"; wkPageI = 0; ShowWork(true); RenderWork(); }

    List<HubE> HubEntries(AHPlayer p)
    {
        var l = new List<HubE>();
        Action<string, string, string, string, Color, bool, bool, Action> E = (cat, title, line, icon, col, ready, on, act) => l.Add(new HubE { cat = cat, title = title, line = line, icon = icon, col = col, ready = ready, on = on, act = act });
        Color W = Color.white;
        // ---- hero ----
        int free = AHEvo.Points(p) - AHEvo.Spent(p); bool evoDue = (p.level >= 30 && AHEvo.CP(p).path == null) || (p.level >= 60 && AHEvo.CP(p).form == null);
        E("hero", "Character", "Your gear on your hero · gear score", "steel_plate", new Color(1f, 0.8f, 0.45f), false, true, OpenChar);
        E("hero", "Stats", "Damage, crit, evasion, speed", "iron_sword", W, false, true, OpenStats);
        E("hero", "Class & talents", AHEvo.Title(p) + (free > 0 ? " · <color=#9be37a>" + free + " talent points</color>" : " · evolution · spellbook"), "runed_staff", p.cls.color, free > 0 || evoDue, true, () => OpenClassWin());
        E("hero", "Talent sets", "Save up to 3 talent sets", "blessed_mace", W, false, true, OpenTalSets);
        E("hero", "Change class", "Now: " + p.cls.name, "silk_mantle", p.cls.color, false, true, () => { ShowWork(false); ShowPicker(true); });
        E("hero", "Enhance gear", p.bag.Count("enh_stone") + " stones · " + p.bag.Count("lucky_charm") + " charms", "enh_stone", W, false, true, OpenEnh);
        E("hero", "Gem sockets", p.prog.gems.Count + " gems set", "ruby", W, false, true, OpenGems);
        E("hero", "Loadouts & bag", "Gear sets · " + p.bag.SlotsMax + " bag slots", "bag_pack", W, false, true, OpenLoadouts);
        E("hero", "Wardrobe", AHWardrobe.CosmeticCount(p) + " cosmetics · " + p.prog.outfits.Count + " outfits", "silk_robe", W, false, true, OpenWardrobe);
        E("hero", "Looks", "Weapon skins, effects, trails", "lantern_hood", W, false, true, OpenLooks);
        E("hero", "Emotes", "Wave, bow, dance, sit…", "cg:emote", W, false, true, OpenEmotes);
        // ---- adventure ----
        E("adv", "Story", AHStory.Done(p) ? "Done · Kingsbane" : "Chapter " + (p.prog.storyCh + 1) + " of " + AHStory.Chapters.Length, "fossil_map", new Color(1f, 0.75f, 0.4f), !AHStory.Done(p), true, OpenStory);
        E("adv", "Daily wheel", AHFun4.WheelReady ? "<color=#9be37a>A free spin today!</color>" : "Spun today · back tomorrow", "lucky_charm", new Color(1f, 0.85f, 0.45f), AHFun4.WheelReady, true, OpenWheel);
        E("adv", "Weekly legend", AHFun4.LegendName + (AHFun4.LegendDown ? " · your best " + AHDeep.Clock(AHFun4.LegendBest) : " · in " + AHAuto.LandName(AHFun4.LegendArea(p))), "dragon_scale", new Color(0.8f, 0.55f, 1f), false, true, OpenLegend);
        { int br = AHFun3.Ready(p); E("adv", "Bounty board", br > 0 ? "<color=#9be37a>" + br + " to claim</color>" : "Three named beasts a day", "card_wolf", new Color(1f, 0.7f, 0.4f), br > 0, true, OpenBounty); }
        E("adv", "Daily goals", (AHDaily.Ready(p) > 0 ? "<color=#9be37a>" + AHDaily.Ready(p) + " to claim</color> · " : "") + p.prog.daily.marks + " marks · login calendar", "mystery_sack", new Color(1f, 0.85f, 0.5f), AHDaily.Ready(p) > 0, true, OpenDaily);
        E("adv", "Dungeon Finder", AHFinder.Runs + " group runs", "iron_mace", W, false, true, OpenFinder);
        if (AHDeep.InRun) E("adv", "The Ashen Deep", "Floor " + AHDeep.Floor + " of " + AHDeep.Floors + " · " + AHDeep.Clock(AHDeep.Elapsed), "dragon_scale", new Color(0.75f, 0.55f, 1f), false, true, OpenDeepHall);
        else E("adv", "Enter the Ashen Deep", "This week: " + AHDeep.WeekBoss.name, "dragon_scale", new Color(0.75f, 0.55f, 1f), !AHDeep.BossLooted && p.level >= AHDeep.MinLevel, p.level >= AHDeep.MinLevel && !AHFinder.In, () => { ShowWork(false); AHDeep.Start(g); });
        E("adv", "Deep records", (AHDeep.Best > 0 ? "Best " + AHDeep.Clock(AHDeep.Best) + " · " : "") + AHDeep.Clears + " clears", "frost_cloak", W, false, true, OpenDeepHall);
        E("adv", "Weekly challenge", AHChallenge.Status(p), "royal_helm", new Color(1f, 0.6f, 0.35f), !AHChallenge.S(p).won && p.path != "artisan", true, OpenChallenge);
        E("adv", "Ranked arena", AHRanked.TierText(AHRanked.Rating) + " · rating " + AHRanked.Rating, "guild_cape", W, AHRanked.ChestTier >= 0, true, OpenRanked);
        E("adv", "Two-part quest", AHTwoPart.Name, "recipe_scroll", new Color(1f, 0.75f, 0.4f), AHTwoPart.S(p).done && !AHTwoPart.S(p).claimed || !AHTwoPart.S(p).started && AHTwoPart.Open(p), true, OpenTwoPart);
        E("adv", "Tales of the townsfolk", "Maudie, Ansel and Little Tess", "bread", W, false, true, OpenTales);
        if (AHFinder.Run != "") E("adv", "Leave the group", AHFinder.NameOf(AHFinder.Run) + " · " + AHFinder.RoleName(AHFinder.Role), "cg:close", new Color(1f, 0.55f, 0.45f), false, true, () => { ShowWork(false); AHFinder.Leave(g); });
        // ---- events & fun ----
        { var F = AHFest.Now(); if (F != null) E("fun", F.name, "Tokens: " + AHFest.State(p, F).tok, "lucky_charm", F.col, true, true, OpenFest); }
        E("fun", "Fishing journal", "Every water's five fish · when they bite", "tuna", new Color(0.55f, 0.85f, 1f), false, true, OpenFishJournal);
        E("fun", "Photo album", AHFun3.Album(p).Count + " photos · fine views to snap", "fossil_map", new Color(0.7f, 0.85f, 1f), false, true, OpenAlbum);
        E("fun", "Fishing derby", AHDerby.Best > 0f ? "Your best: " + AHDerby.Best.ToString("0.00") + " kg" : "Every fish is weighed", "tuna", W, false, true, OpenDerby);
        E("fun", "Cook-off", AHCookOff.Score + " points · streak " + AHCookOff.Streak, "stew", W, false, true, OpenCookOff);
        E("fun", "Library & tournament", "Lore pages · weekend tournament · helpers", "logs", W, false, true, () => OpenWorld());
        E("home", "Pet battles", p.prog.pbTamers.Count + " / 6 tamers beaten", "egg", W, false, true, OpenPetBattles);
        // ---- crafting & trade ----
        int mReady = AHMastery.ReadyCount(p);
        E("craft", "Artisans' Guild", p.path == "artisan" ? AHArtisan.Rank(p) : "Commissions · contest · workshop", "tabard_ashen", new Color(0.6f, 0.9f, 0.5f), false, true, () => OpenArtisan());
        E("craft", "Work orders", p.profMain == null ? "Take up a profession first" : p.orders.Count + " orders · " + AHOrders.RankName[AHOrders.Rank(p)], "iron_bar", W, false, p.profMain != null, OpenOrders);
        E("craft", "Gathering mastery", mReady > 0 ? "<color=#9be37a>" + mReady + " to hand in at the Guild</color>" : "Tiers and tool marks", "tool_pick_bronze", W, mReady > 0, true, () => OpenMastery(false));
        E("craft", p.path == "artisan" ? "Repairs" : "Guild repairs", "Lowest condition " + AHWear.LowestCondition(p) + "%", "copper_ore", W, AHWear.LowestCondition(p) <= 30, true, OpenRepairs);
        E("craft", "Caravans", "Send goods · guard caravans", "bag_satchel", W, false, true, OpenCaravans);
        E("craft", "Test kitchen", AHKitchen.S(p).found.Count + " / " + AHKitchen.Sigs.Length + " recipes found", "pie", W, false, true, OpenKitchen);
        E("craft", "Requests board", "Renown " + AHRequests.State(p).renown, "boar_hide", W, false, true, OpenRequests);
        E("craft", "Town projects", "Help rebuild Ashen Hollow", "wheat", W, false, true, OpenTown);
        // ---- companions & home ----
        E("home", "Companions", p.pets.Count + " pets · " + p.mounts.Count + " mounts · " + p.party.Count + " in party", "reins_horse", W, false, true, OpenComp);
        E("home", "Apprentice", AHApprentice.Status(p), "wolf_hood", new Color(0.62f, 0.9f, 0.55f), AHApprentice.GiftReady(p) || (!AHApprentice.S(p).active && AHApprentice.Open(p)), true, OpenApprentice);
        E("home", "Your homestead", p.home == null ? "Buy a deed from a Land Agent" : AHGame.AreaId == AHHome.Area ? "You are home" : "Travel home", "wheat_seed", new Color(0.6f, 0.9f, 0.5f), false, p.home != null && AHGame.AreaId != AHHome.Area && !AHFinder.In, () => { ShowWork(false); AHHome.TravelHome(g); });
        // ---- collections ----
        E("coll", "Deeds & titles", p.prog.ach.Count + " / " + AHAch.All.Count + (AHAch.Title(p) != null ? " · “" + AHAch.Title(p) + "”" : ""), "gold_crown", new Color(1f, 0.8f, 0.45f), false, true, OpenDeeds);
        E("coll", "Monster book", AHMonsterBook.Pages(p) + " / " + AHMonsterBook.All.Count + " pages", "card_boar", W, false, true, OpenMonsterBook);
        E("coll", "Monster cards", p.prog.cards.Count + " cards · licenses", "card_wolf", W, false, true, OpenCollection);
        E("coll", "Collection book", AHCollection.Filled(p) + " / " + AHCollection.Pages.Count + " pages", "raw_trout", W, AHCollection.Ready(p) > 0, true, OpenCatchBook);
        E("coll", "Reputation", "Your standing with the seven towns", "guild_cape", W, false, true, OpenRep);
        // ---- store & pass ----
        E("store", "Store", AHStore.Amount(p.prog.crowns) + " · looks only", "diamond", new Color(1f, 0.85f, 0.4f), false, true, OpenStore);
        E("store", "Battle Pass", "Fight " + AHPass2.Tier(p, false) + " · craft " + AHPass2.Tier(p, true), "sun_cape", new Color(0.79f, 0.65f, 1f), AHPass2.Ready(p) > 0, true, OpenPass);
        E("store", "VIP", AHVip.Describe(p), "sapphire", new Color(1f, 0.85f, 0.4f), false, true, () => OpenStore());
        // ---- friends & settings ----
        bool letters = p.prog.letters.Exists(x => !x.taken);
        { int mn = AHMail.Count(p); E("acct", "Mail", mn > 0 ? "<color=#9be37a>" + mn + " waiting</color>" : "Rewards and letters", "cg:chat", new Color(1f, 0.85f, 0.55f), mn > 0, true, OpenMail); }
        E("acct", "Friends", AHFriends.Count(p, 1) + " friends · " + p.prog.letters.FindAll(x => !x.taken).Count + " letters", "cg:chat", new Color(1f, 0.6f, 0.75f), letters, true, () => OpenFriends(letters ? "letters" : "list"));
        E("acct", "Settings", "Sound, graphics, camera, account", "cg:menu", new Color(0.8f, 0.8f, 1f), false, true, OpenSettings);
        if (AHComp.TestMounts) E("acct", "Bags and tools (testing)", "One of every bag and tool", "bag_pouch", W, false, true, () => { foreach (var id in new[] { "bag_pouch", "bag_pack", "bag_master", "bag_mythic", "tool_pick_iron", "tool_axe_guild", "tool_rod_bronze" }) p.bag.Add(id, 1); Toast("Test bags and tools are in your bag."); g.MarkDirty(); });
        E("acct", "Change hero / Log out", "Play another hero on this device", "cg:close", new Color(1f, 0.6f, 0.75f), false, true, () => { ShowWork(false); g.Logout(); });
        return l;
    }

    Sprite HubIcon(string id)
    {
        if (id == null) return null;
        if (id.StartsWith("cg:")) return CGIcon(id.Substring(3));
        var it = AHItems.Get(id); return it != null ? AHItemIcons.Best(it) : null;
    }

    // the eight
    void RenderHub(AHPlayer p)
    {
        hubCat = null;
        wkTitle.text = AHLoc.T("Menu", "القائمة");
        wkHint.text = (string.IsNullOrEmpty(p.heroName) ? "" : p.heroName + " · ") + "level " + p.level + " " + p.cls.name + " · " + AHItems.MoneyText(p.bag.money) + (AHFinder.In ? " · in a group run of " + AHFinder.NameOf(AHFinder.Run) : "");
        wkPage.text = ""; SetPager(false);
        var all = HubEntries(p);
        float W = WkW - 36f, H = RowH * RowsPerPage, gap = 12f, tw = (W - gap * 3f) / 4f, th = (H - gap) / 2f;
        for (int i = 0; i < HubCats.Length; i++)
        {
            var c = HubCats[i]; int ready = all.FindAll(e => e.cat == c.id && e.ready).Count;
            float x = i % 4 * (tw + gap), y = -(i / 4) * (th + gap);
            var bg = Img("Cat", wkList, white, new Vector2(0f, 1f), new Vector2(x + tw / 2f, y - th / 2f), new Vector2(tw, th), ready > 0 ? new Color(0.32f, 0.24f, 0.14f, 1f) : new Color(0.16f, 0.12f, 0.09f, 1f));
            wkItems.Add(bg.gameObject);
            var ring = Img("Ring", bg, circle, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(96, 96), new Color(c.col.r * 0.35f, c.col.g * 0.35f, c.col.b * 0.35f, 0.95f));
            Img("Rim", ring, this.ring != null ? this.ring : circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100), new Color(c.col.r, c.col.g, c.col.b, 0.8f));
            var sp = HubIcon(c.icon); if (sp != null) { var ic = Img("Icon", ring, sp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70), Color.white); ic.GetComponent<Image>().preserveAspect = true; }
            var t = Label(bg, "Name", AHLoc.T(c.name, c.ar), 20, TextAnchor.MiddleCenter, new Vector2(0f, -122f), new Vector2(tw - 12f, 28), c.col); t.fontStyle = FontStyle.Bold; Fit(t, 14);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f); t.rectTransform.pivot = new Vector2(0.5f, 0.5f); t.rectTransform.anchoredPosition = new Vector2(0f, -124f);
            var b2 = Label(bg, "Blurb", AHLoc.T(c.blurb, c.arB), 13, TextAnchor.UpperCenter, new Vector2(0f, -150f), new Vector2(tw - 18f, 38), new Color(1f, 1f, 1f, 0.7f));
            b2.rectTransform.anchorMin = b2.rectTransform.anchorMax = new Vector2(0.5f, 1f); b2.rectTransform.pivot = new Vector2(0.5f, 1f); b2.rectTransform.anchoredPosition = new Vector2(0f, -140f);
            b2.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (ready > 0) Badge(bg, ready, new Vector2(-18f, -18f));
            string id = c.id;
            taps.Add(new TapBtn { rt = bg, layer = 5, group = "work", act = () => { hubCat = id; wkMode = "hubcat"; wkPageI = 0; RenderWork(); } });
        }
    }
    void Badge(RectTransform parent, int n, Vector2 at)
    {
        var d = Img("Badge", parent, circle, new Vector2(1f, 1f), at, new Vector2(30, 30), new Color(0.3f, 0.75f, 0.35f, 1f));
        Center(Label(d, "N", n.ToString(), 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(30, 26), Color.white)).fontStyle = FontStyle.Bold;
    }

    // one of the eight, opened
    void RenderHubCat(AHPlayer p)
    {
        var c = Array.Find(HubCats, x => x.id == hubCat); if (c == null) { wkMode = "hub"; RenderHub(p); return; }
        wkTitle.text = AHLoc.T(c.name, c.ar);
        wkHint.text = AHLoc.T(c.blurb, c.arB);
        var list = HubEntries(p).FindAll(e => e.cat == c.id);
        const int cols = 3, rows = 4, per = cols * rows;
        int pages = Mathf.Max(1, (list.Count + per - 1) / per); wkPageI = Mathf.Clamp(wkPageI, 0, pages - 1);
        wkPage.text = pages > 1 ? (wkPageI + 1) + " / " + pages : ""; SetPager(pages > 1);
        float W = WkW - 36f, H = RowH * RowsPerPage, gap = 10f, tw = (W - gap * (cols - 1)) / cols, th = (H - gap * (rows - 1)) / rows;
        for (int k = 0; k < per && wkPageI * per + k < list.Count; k++)
        {
            var e = list[wkPageI * per + k];
            float x = k % cols * (tw + gap), y = -(k / cols) * (th + gap);
            var bg = Img("Tile", wkList, white, new Vector2(0f, 1f), new Vector2(x + tw / 2f, y - th / 2f), new Vector2(tw, th), e.ready ? new Color(0.32f, 0.24f, 0.14f, 1f) : new Color(0.16f, 0.12f, 0.09f, 1f));
            wkItems.Add(bg.gameObject);
            var ring = Img("Ring", bg, circle, new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(66, 66), new Color(e.col.r * 0.32f, e.col.g * 0.32f, e.col.b * 0.32f, e.on ? 0.95f : 0.5f));
            var sp = HubIcon(e.icon); if (sp != null) { var ic = Img("Icon", ring, sp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48, 48), e.on ? Color.white : new Color(1f, 1f, 1f, 0.45f)); ic.GetComponent<Image>().preserveAspect = true; }
            var t = Label(bg, "Name", e.title, 18, TextAnchor.UpperLeft, new Vector2(84f, -10f), new Vector2(tw - 96f, 24), e.on ? e.col : new Color(1f, 1f, 1f, 0.45f)); t.fontStyle = FontStyle.Bold; Fit(t, 12);
            var l2 = Label(bg, "Line", e.line ?? "", 14, TextAnchor.UpperLeft, new Vector2(84f, -36f), new Vector2(tw - 96f, 46), new Color(1f, 1f, 1f, e.on ? 0.72f : 0.4f));
            l2.horizontalOverflow = HorizontalWrapMode.Wrap; l2.verticalOverflow = VerticalWrapMode.Truncate; l2.resizeTextForBestFit = true; l2.resizeTextMinSize = 11; l2.resizeTextMaxSize = 14;
            if (e.ready) Badge(bg, 1, new Vector2(-12f, -12f));
            var act = e.act; bool on = e.on; string cat = c.id;
            taps.Add(new TapBtn { rt = bg, layer = 5, group = "work", act = () => { if (!on || act == null) return; hubFrom = cat; act(); if (!WorkOpen) hubFrom = null; } });
        }
        // back to the eight
        var back = Img("Back", wkPanel, white, new Vector2(0f, 0f), new Vector2(92f, 38f), new Vector2(150, 50), new Color(0.22f, 0.16f, 0.11f, 1f));
        wkItems.Add(back.gameObject);
        Center(Label(back, "T", AHLoc.T("◀ Menu", "القائمة ▶"), 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 36), Color.white));
        taps.Add(new TapBtn { rt = back, layer = 5, group = "work", act = () => { wkMode = "hub"; wkPageI = 0; RenderWork(); } });
    }
}
