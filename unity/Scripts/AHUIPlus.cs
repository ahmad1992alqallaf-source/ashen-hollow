// Ashen Hollow: the interface, round two.
//  - The talk box: talking to someone opens a glass box at the bottom of the screen with their picture, name and
//    trade, and what they say (written out letter by letter), with Next and Goodbye. It folds away when you walk off.
//  - A cleaner screen: the quest sits on a glass card that folds to one line (the little arrow on it); Daily and the
//    event timers share one glass pill under the minimap; the MENU button carries a green badge counting what waits.
//  - The boss bar: a wide bar across the top with the boss's name while you fight one.
//  - The reward chest: claiming a reward opens a chest and shows what is in it, one thing at a time.
//  - The character screen: your hero in the middle with the ten gear slots round them (each ringed in its rarity's
//    colour; tap one to look at it), your gear score and main stats, and tabs for Stats, Talents and Looks.
//  - Moving the buttons: Settings → Edit HUD layout. Drag the joystick, the fighting buttons, the minimap column, the
//    quest card or the chat where you like them, make the buttons bigger or smaller, Reset or Done.
//  - Arabic: with Arabic chosen, windows are laid out right to left (mirrored, the words kept readable and right-aligned).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    // =====================================================================================================
    // ---------- the talk box ----------
    // =====================================================================================================
    RectTransform tbRoot, tbNext; Text tbName, tbRole, tbLine; Image tbIcon; AHNpc tbNpc; string tbFull = ""; float tbShown, tbUntil;
    public bool TalkOpen { get { return tbRoot != null && tbRoot.gameObject.activeSelf; } }

    void BuildTalkBox()
    {
        tbRoot = Img("TalkBox", transform, cgCard != null ? cgCard : white, new Vector2(0.5f, 0f), new Vector2(0f, 262f), new Vector2(560, 136), Color.white);
        if (cgCard != null) tbRoot.GetComponent<Image>().type = Image.Type.Sliced;
        var por = Img("Portrait", tbRoot, ornDisc != null ? ornDisc : circle, new Vector2(0f, 0.5f), new Vector2(64f, 4f), new Vector2(96, 96), new Color(0.12f, 0.2f, 0.32f, 0.95f));
        Img("Rim", por, ring != null ? ring : circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(102, 102), new Color(1f, 0.82f, 0.45f, 0.9f));
        var ic = Img("Icon", por, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 62), Color.white); tbIcon = ic.GetComponent<Image>(); tbIcon.preserveAspect = true;
        tbName = Label(tbRoot, "Name", "", 22, TextAnchor.UpperLeft, new Vector2(124f, -10f), new Vector2(300, 28), CGGold); tbName.font = TitleFont; Shade(tbName);
        tbRole = Label(tbRoot, "Role", "", 13, TextAnchor.UpperRight, new Vector2(330f, -14f), new Vector2(210, 20), new Color(0.74f, 0.83f, 0.93f));
        tbLine = Label(tbRoot, "Line", "", 17, TextAnchor.UpperLeft, new Vector2(124f, -42f), new Vector2(420, 58), Color.white);
        tbLine.horizontalOverflow = HorizontalWrapMode.Wrap; Shade(tbLine);
        tbNext = Img("Next", tbRoot, cgPillSky != null ? cgPillSky : white, new Vector2(1f, 0f), new Vector2(-170f, 22f), new Vector2(110, 32), Color.white);
        if (cgPillSky != null) tbNext.GetComponent<Image>().type = Image.Type.Sliced;
        Center(Label(tbNext, "T", AHLoc.T("Next ▸", "التالي"), 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(110, 28), Color.white));
        var bye = Img("Bye", tbRoot, cgPillGlass != null ? cgPillGlass : white, new Vector2(1f, 0f), new Vector2(-60f, 22f), new Vector2(100, 32), Color.white);
        if (cgPillGlass != null) bye.GetComponent<Image>().type = Image.Type.Sliced;
        Center(Label(bye, "T", AHLoc.T("Goodbye", "وداعًا"), 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(100, 28), Color.white));
        taps.Insert(0, new TapBtn { rt = tbNext, act = () => { if (tbShown < tbFull.Length) tbShown = tbFull.Length; else if (tbNpc != null) g.TalkTo(tbNpc); } });
        taps.Insert(0, new TapBtn { rt = bye, act = () => tbRoot.gameObject.SetActive(false) });
        tbRoot.gameObject.SetActive(false);
    }

    // someone says something to you ("Name: “words”" or just the words)
    public void TalkLine(AHNpc n, string line)
    {
        if (n == null || string.IsNullOrEmpty(line)) return;
        if (tbRoot == null) BuildTalkBox();
        string words = line; int q = line.IndexOf(": “"); if (q > 0) words = line.Substring(q + 3).TrimEnd('”');
        tbNpc = n; tbFull = words; tbShown = 0f; tbUntil = Time.unscaledTime + Mathf.Clamp(words.Length * 0.06f + 5f, 6f, 14f);
        tbName.text = n.npcName;
        tbRole.text = RoleText(n);
        var sp = HubIcon(RoleIcon(n)); tbIcon.sprite = sp; tbIcon.enabled = sp != null;
        tbNext.gameObject.SetActive(n.lines != null && n.lines.Length > 1);
        tbRoot.gameObject.SetActive(true); tbRoot.SetAsLastSibling();
        AHChat.FromToast(line);
        Mirror(tbRoot, AHLoc.Arabic);
    }
    static string RoleText(AHNpc n)
    {
        string s = n.npcName;
        if (n.isCaptain) return "Captain of the guard";
        if (n.market) return "Market warden"; if (n.guild) return "Guild"; if (n.auction) return "Auction house"; if (n.barber) return "Barber"; if (n.agent) return "Land agent"; if (n.kq) return "Arena";
        string r = AHLife.Role(n);
        return r == "guard" ? "Town guard" : r == "merchant" ? "Trader" : r == "child" ? "Child of the town" : n.label != null ? n.label : "Townsfolk";
    }
    static string RoleIcon(AHNpc n)
    {
        string s = n.npcName ?? "";
        string[][] by = {
            new[] { "Smith", "iron_bar" }, new[] { "Cook", "stew" }, new[] { "Baker", "bread" }, new[] { "Alchemist", "hp_potion" }, new[] { "Herbwife", "sunpetal" }, new[] { "Tanner", "boar_hide" },
            new[] { "Gemcutter", "ruby" }, new[] { "Woodcarver", "logs" }, new[] { "Bard", "lucky_charm" }, new[] { "Card sharp", "card_wolf" }, new[] { "Ottar", "mystery_sack" }, new[] { "Brann", "boar_gloves" },
            new[] { "Stable", "reins_horse" }, new[] { "Race", "reins_horse" }, new[] { "Arena", "guild_cape" }, new[] { "crier", "recipe_scroll" }, new[] { "Captain", "royal_helm" }, new[] { "Farmer", "wheat" },
            new[] { "Barber", "silk_hood" }, new[] { "Auctioneer", "gold_crown" }, new[] { "Clerk", "recipe_scroll" }, new[] { "Fishmonger", "trout" }, new[] { "Grocer", "pie" }, new[] { "Peddler", "bag_satchel" },
            new[] { "knight", "steel_helm" }, new[] { "Pip", "bread" }, new[] { "Tess", "bread" }, new[] { "Merchant", "bag_satchel" }, new[] { "Patron", "stew" }, new[] { "Warden", "steel_helm" } };
        foreach (var b in by) if (s.IndexOf(b[0], StringComparison.OrdinalIgnoreCase) >= 0) return b[1];
        string r = AHLife.Role(n);
        return r == "guard" ? "steel_helm" : r == "merchant" ? "bag_satchel" : r == "child" ? "bread" : "wool_cloak";
    }
    void TalkBoxTick()
    {
        if (!TalkOpen) return;
        if (Modal != 0) { tbRoot.gameObject.SetActive(false); return; }
        tbShown = Mathf.Min(tbFull.Length, tbShown + Time.unscaledDeltaTime * 55f);
        tbLine.text = tbFull.Substring(0, Mathf.FloorToInt(tbShown));
        bool far = tbNpc == null || g.player == null || tbNpc.DistTo(g.player.transform.position) > 7f;
        if (far || Time.unscaledTime > tbUntil) tbRoot.gameObject.SetActive(false);
    }

    // =====================================================================================================
    // ---------- a cleaner screen: the quest card, the timer pill, the menu badge ----------
    // =====================================================================================================
    RectTransform qCard, qFold, timerPill, menuBadge; Text menuBadgeN; bool qFolded; float badgeT;
    void PlusLayout()
    {
        try
        {
            if (questPanel != null && qCard == null)
            {
                qCard = Img("QCard", questPanel, cgCard != null ? cgCard : white, new Vector2(0f, 1f), new Vector2(170f, -46f), new Vector2(348, 96), new Color(1f, 1f, 1f, 0.82f));
                if (cgCard != null) qCard.GetComponent<Image>().type = Image.Type.Sliced;
                qCard.SetAsFirstSibling(); qCard.GetComponent<Image>().raycastTarget = false;
                qFold = Img("Fold", questPanel, circle, new Vector2(0f, 1f), new Vector2(330f, -46f), new Vector2(26, 26), new Color(0.5f, 0.75f, 1f, 0.35f));
                var ft = Center(Label(qFold, "T", "▴", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(26, 24), Color.white)); ft.name = "FT";
                taps.Insert(0, new TapBtn { rt = qFold, act = () => { qFolded = !qFolded; try { AHPrefs.SetInt("ah_hud_qfold", qFolded ? 1 : 0); } catch { } FoldQuest(); } });
                try { qFolded = AHPrefs.GetInt("ah_hud_qfold", 0) == 1; } catch { }
                FoldQuest();
            }
            if (timerPill == null)
            {
                var dc = FindUI("DailyChip");
                if (dc != null)
                {
                    timerPill = Img("TimerPill", transform, cgPillGlass != null ? cgPillGlass : white, new Vector2(1f, 1f), new Vector2(-104f, -217f), new Vector2(184, 48), Color.white);
                    if (cgPillGlass != null) timerPill.GetComponent<Image>().type = Image.Type.Sliced;
                    timerPill.GetComponent<Image>().raycastTarget = false;
                    timerPill.SetSiblingIndex(dc.GetSiblingIndex());
                }
            }
            var mb = FindUI("MenuBtn");
            if (mb != null && menuBadge == null)
            {
                menuBadge = Img("Badge", mb, circle, new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(22, 22), new Color(0.3f, 0.78f, 0.36f, 1f));
                menuBadgeN = Center(Label(menuBadge, "N", "", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(22, 20), Color.white)); menuBadgeN.fontStyle = FontStyle.Bold;
                menuBadge.gameObject.SetActive(false);
            }
            BuildQuick(new Color(0.05f, 0.08f, 0.15f, 0.6f));
            BuildMail();
            HudLayoutCapture();
        }
        catch (Exception e) { Debug.LogWarning("Ashen Hollow: interface extras (" + e.Message + ")"); }
    }
    void FoldQuest()
    {
        if (qCard == null) return;
        qCard.sizeDelta = qFolded ? new Vector2(348, 34) : new Vector2(348, 96);
        qCard.anchoredPosition = qFolded ? new Vector2(170f, -15f) : new Vector2(170f, -46f);
        qFold.anchoredPosition = qFolded ? new Vector2(330f, -15f) : new Vector2(330f, -46f);
        var ft = qFold.Find("FT"); if (ft != null) ft.GetComponent<Text>().text = qFolded ? "▾" : "▴";
        if (trackLine != null) trackLine.gameObject.SetActive(!qFolded);
        if (cgGuideLine != null) cgGuideLine.gameObject.SetActive(!qFolded);
    }

    void PlusTick()
    {
        TalkBoxTick();
        BossBarTick();
        RewardTick();
        HudEditTick();
        QuickTick();
        MailTick();
        TipTick();
        if (menuBadge != null && (badgeT -= Time.unscaledDeltaTime) <= 0f && g.player != null && g.player.cls != null)
        {
            badgeT = 2f; int n = 0;
            try { foreach (var e in HubEntries(g.player)) if (e.ready) n++; } catch { }
            menuBadge.gameObject.SetActive(n > 0); menuBadgeN.text = n.ToString();
        }
    }

    // =====================================================================================================
    // ---------- the boss bar ----------
    // =====================================================================================================
    RectTransform bossRoot, bossFill, bossLag; Text bossName, bossPct; AHMob bossMob; float bossLagK = 1f;
    static bool IsBig(AHMob m)
    {
        if (m == null || m.type == null) return false;
        var d = AHJson.O(AHDB.Mobs, m.type.id);
        return m.type.elite || m.golden || AHJson.B(d, "boss") || AHJson.B(d, "dboss") || AHJson.B(d, "world") || AHEvents.IsWorldBoss(m.type.id);
    }
    void BossBarTick()
    {
        var p = g.player; if (p == null) return;
        AHMob best = null; float bd = 38f;
        if (Modal == 0)
            foreach (var m in g.mobs)
            {
                if (m == null || m.dead || !IsBig(m)) continue;
                float d = Vector3.Distance(m.transform.position, p.transform.position);
                if (d < bd && (m.Chasing || p.target == m)) { bd = d; best = m; }
            }
        if (best == null) { if (bossRoot != null && bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(false); bossMob = null; return; }
        if (bossRoot == null)
        {
            bossRoot = Box("BossBar", transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(540, 52));
            bossName = Label(bossRoot, "Name", "", 20, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(540, 26), CGGold); bossName.font = TitleFont; Shade(bossName);
            bossName.rectTransform.anchorMin = bossName.rectTransform.anchorMax = new Vector2(0.5f, 1f); bossName.rectTransform.pivot = new Vector2(0.5f, 1f); bossName.rectTransform.anchoredPosition = Vector2.zero;
            var bg = Img("Bg", bossRoot, cgMask != null ? cgMask : white, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(520, 16), new Color(0.05f, 0.06f, 0.1f, 0.85f));
            if (cgMask != null) bg.GetComponent<Image>().type = Image.Type.Sliced;
            bossLag = Img("Lag", bg, white, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(516, 12), new Color(1f, 0.85f, 0.5f, 0.85f)); bossLag.pivot = new Vector2(0f, 0.5f); bossLag.anchoredPosition = new Vector2(2f, 0f);
            bossFill = Img("Fill", bg, white, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(516, 12), new Color(0.9f, 0.25f, 0.22f, 1f)); bossFill.pivot = new Vector2(0f, 0.5f); bossFill.anchoredPosition = new Vector2(2f, 0f);
            bossPct = Center(Label(bg, "Pct", "", 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(520, 16), Color.white)); Shade(bossPct);
        }
        if (bossMob != best) { bossMob = best; bossLagK = 1f; }
        float k = Mathf.Clamp01(best.hp / Mathf.Max(1f, best.type.hp));
        bossLagK = Mathf.MoveTowards(bossLagK, k, Time.unscaledDeltaTime * 0.35f); if (bossLagK < k) bossLagK = k;
        bossFill.sizeDelta = new Vector2(516f * k, 12); bossLag.sizeDelta = new Vector2(516f * bossLagK, 12);
        bossName.text = best.Label;
        bossPct.text = Mathf.CeilToInt(best.hp).ToString("#,0") + " / " + best.type.hp.ToString("#,0") + "  ·  " + Mathf.CeilToInt(k * 100f) + "%";
        if (!bossRoot.gameObject.activeSelf) bossRoot.gameObject.SetActive(true);
    }

    // =====================================================================================================
    // ---------- the reward chest ----------
    // =====================================================================================================
    RectTransform rwRoot, rwChest, rwList; Text rwTitle, rwHint; float rwT; readonly List<KeyValuePair<Sprite, string>> rwItems = new List<KeyValuePair<Sprite, string>>(); int rwShown;
    public bool RewardOpen { get { return rwRoot != null && rwRoot.gameObject.activeSelf; } }
    public void RewardChest(string title, long money, long xp, int marks, List<KeyValuePair<string, int>> items)
    {
        rwItems.Clear();
        if (money > 0) rwItems.Add(new KeyValuePair<Sprite, string>(HubIcon("gold_crown"), AHItems.MoneyText(money * AHDB.CU)));
        if (xp > 0) rwItems.Add(new KeyValuePair<Sprite, string>(CGIcon("star"), xp.ToString("#,0") + " XP"));
        if (marks > 0) rwItems.Add(new KeyValuePair<Sprite, string>(HubIcon("recipe_scroll"), marks + " marks"));
        if (items != null) foreach (var kv in items) { var it = AHItems.Get(kv.Key); if (it != null) rwItems.Add(new KeyValuePair<Sprite, string>(AHItemIcons.Best(it), (kv.Value > 1 ? kv.Value + "× " : "") + it.name)); }
        if (rwItems.Count == 0) return;
        if (rwRoot == null)
        {
            rwRoot = Box("Reward", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            rwRoot.anchorMin = Vector2.zero; rwRoot.anchorMax = Vector2.one; rwRoot.offsetMin = rwRoot.offsetMax = Vector2.zero;
            var dim = rwRoot.gameObject.AddComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.55f);
            var card = Img("Card", rwRoot, cgCard != null ? cgCard : white, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(560, 420), Color.white);
            if (cgCard != null) card.GetComponent<Image>().type = Image.Type.Sliced;
            rwTitle = Label(card, "Title", "", 26, TextAnchor.UpperCenter, new Vector2(0f, -16f), new Vector2(520, 34), CGGold); rwTitle.font = TitleFont;
            rwTitle.rectTransform.anchorMin = rwTitle.rectTransform.anchorMax = new Vector2(0.5f, 1f); rwTitle.rectTransform.pivot = new Vector2(0.5f, 1f); rwTitle.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            rwChest = Img("Chest", card, white, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(110, 110), Color.white); rwChest.GetComponent<Image>().preserveAspect = true;
            rwList = Box("List", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(500, 200));
            rwHint = Label(card, "Hint", "Tap to continue", 15, TextAnchor.LowerCenter, new Vector2(0f, 14f), new Vector2(520, 22), new Color(1f, 1f, 1f, 0.6f));
            rwHint.rectTransform.anchorMin = rwHint.rectTransform.anchorMax = new Vector2(0.5f, 0f); rwHint.rectTransform.pivot = new Vector2(0.5f, 0f); rwHint.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            taps.Add(new TapBtn { rt = rwRoot, layer = 11, act = () => { if (rwShown < rwItems.Count) { rwShown = rwItems.Count; DrawRewards(); } else rwRoot.gameObject.SetActive(false); } });
        }
        rwTitle.text = title;
        rwChest.GetComponent<Image>().sprite = HubIcon("mystery_sack");
        foreach (Transform c in rwList) Destroy(c.gameObject);
        rwShown = 0; rwT = 0f;
        rwRoot.gameObject.SetActive(true); rwRoot.SetAsLastSibling(); ptrs.Clear();
        AHSound.Play("loot");
    }
    void DrawRewards()
    {
        foreach (Transform c in rwList) Destroy(c.gameObject);
        int n = Mathf.Min(rwShown, rwItems.Count), cols = n > 4 ? 2 : 1;
        for (int i = 0; i < n; i++)
        {
            float x = cols == 1 ? 0f : (i % 2 == 0 ? -125f : 125f), y = -(cols == 1 ? i : i / 2) * 44f - 22f;
            var row = Img("R", rwList, cgPillGlass != null ? cgPillGlass : white, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(cols == 1 ? 420 : 240, 38), Color.white);
            if (cgPillGlass != null) row.GetComponent<Image>().type = Image.Type.Sliced;
            if (rwItems[i].Key != null) { var ic = Img("I", row, rwItems[i].Key, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(30, 30), Color.white); ic.GetComponent<Image>().preserveAspect = true; }
            var t = Label(row, "T", rwItems[i].Value, 16, TextAnchor.MiddleLeft, new Vector2(46f, -4f), new Vector2((cols == 1 ? 420 : 240) - 54, 30), Color.white); Fit(t, 11);
        }
    }
    void RewardTick()
    {
        if (!RewardOpen) return;
        rwT += Time.unscaledDeltaTime;
        float bounce = rwT < 0.6f ? 1f + Mathf.Sin(rwT / 0.6f * Mathf.PI * 3f) * 0.12f * (1f - rwT / 0.6f) : 1f;
        rwChest.localScale = Vector3.one * bounce;
        int want = rwT < 0.6f ? 0 : Mathf.Min(rwItems.Count, 1 + Mathf.FloorToInt((rwT - 0.6f) / 0.35f));
        if (want != rwShown) { rwShown = want; DrawRewards(); AHSound.Play("coin"); }
    }

    // =====================================================================================================
    // ---------- the character screen ----------
    // =====================================================================================================
    public void OpenChar() { wkMode = "char"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void CharTabs(string on)
    {
        string[][] tabs = { new[] { "char", AHLoc.T("Gear", "العتاد") }, new[] { "stats", AHLoc.T("Stats", "الإحصاءات") }, new[] { "tal", AHLoc.T("Talents", "المواهب") }, new[] { "looks", AHLoc.T("Looks", "المظهر") } };
        float x = WkW - 36f - 4f * 118f;
        for (int i = 0; i < tabs.Length; i++)
        {
            string id = tabs[i][0];
            var b = Img("Tab", wkPanel, white, new Vector2(0f, 1f), new Vector2(x + 18f + i * 118f + 56f, -70f), new Vector2(112, 36), id == on ? new Color(0.32f, 0.24f, 0.14f, 1f) : new Color(0.22f, 0.16f, 0.11f, 1f));
            wkItems.Add(b.gameObject);
            Center(Label(b, "T", tabs[i][1], 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(112, 30), id == on ? CGGold : Color.white));
            taps.Add(new TapBtn { rt = b, layer = 5, group = "work", act = () => { if (id == "char") OpenChar(); else if (id == "stats") { wkMode = "stats"; wkPageI = 0; RenderWork(); } else if (id == "tal") OpenClassWin("tal"); else OpenWardrobe(); } });
        }
    }
    void RenderChar(AHPlayer p)
    {
        SetPager(false); wkPage.text = "";
        wkTitle.text = (string.IsNullOrEmpty(p.heroName) ? "Hero" : p.heroName) + " · " + AHEvo.Title(p) + " · level " + p.level;
        wkHint.text = "";
        CharTabs("char");
        float W = WkW - 36f, H = RowH * RowsPerPage;
        // the hero in the middle, on a glowing stand
        var stand = Img("Stand", wkList, white, new Vector2(0.5f, 1f), new Vector2(0f, -H / 2f), new Vector2(300, H - 4f), new Color(0.16f, 0.12f, 0.09f, 1f));
        wkItems.Add(stand.gameObject);
        if (glow != null) Img("Glow", stand, glow, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(300, 320), new Color(AHEvo.PathColor(p).r, AHEvo.PathColor(p).g, AHEvo.PathColor(p).b, 0.35f));
        var view = Box("Hero", stand, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(260, H - 70f));
        var raw = view.gameObject.AddComponent<RawImage>(); raw.raycastTarget = false; AHItemStudio.Campfire = false; raw.texture = AHItemStudio.Hero(p);
        // gear score and the main numbers under the hero
        float score = 0f; foreach (var s in AHItems.GearSlots) score += GearScore(AHItems.Get(p.bag.Worn(s)));
        var gs = Label(stand, "Score", "Gear score " + Mathf.RoundToInt(score).ToString("#,0"), 18, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(300, 24), CGGold); gs.font = TitleFont;
        gs.rectTransform.anchorMin = gs.rectTransform.anchorMax = new Vector2(0.5f, 0f); gs.rectTransform.pivot = new Vector2(0.5f, 0f); gs.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        var st = Label(stand, "Stats", "HP " + Mathf.RoundToInt(p.maxHp) + "  ·  Dmg +" + Mathf.RoundToInt(p.stat.dmg * 100f) + "%  ·  Crit " + Mathf.RoundToInt(p.stat.crit * 100f) + "%", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(300, 20), new Color(1f, 1f, 1f, 0.75f));
        st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0.5f, 0f); st.rectTransform.pivot = new Vector2(0.5f, 0f); st.rectTransform.anchoredPosition = new Vector2(0f, 10f);
        // the ten slots, five a side
        string[] names = { "Head", "Shoulders", "Chest", "Hands", "Legs", "Feet", "Cape", "Weapon", "Ring", "Amulet" };
        for (int i = 0; i < AHItems.GearSlots.Length && i < 10; i++)
        {
            string slot = AHItems.GearSlots[i]; var it = AHItems.Get(p.bag.Worn(slot));
            bool left = i < 5; int r = i % 5;
            float sx = left ? 0f : W - 1f, sy = -8f - r * (H - 8f) / 5f;
            var cell = Img("Slot", wkList, white, new Vector2(0f, 1f), Vector2.zero, new Vector2((W - 300f) / 2f - 14f, (H - 8f) / 5f - 8f), new Color(0.16f, 0.12f, 0.09f, 1f));
            cell.pivot = new Vector2(left ? 0f : 1f, 1f); cell.anchoredPosition = new Vector2(sx, sy); wkItems.Add(cell.gameObject);
            Color q = it != null ? AHItems.Quality(it) : new Color(1f, 1f, 1f, 0.25f);
            var ringR = Img("Ring", cell, circle, new Vector2(left ? 0f : 1f, 0.5f), new Vector2(left ? 36f : -36f, 0f), new Vector2(60, 60), new Color(q.r, q.g, q.b, it != null ? 0.95f : 0.3f));
            Img("In", ringR, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52), new Color(0.07f, 0.09f, 0.15f, 1f));
            var sp = it != null ? AHItemIcons.Best(it) : null;
            if (sp != null) { var ic = Img("Icon", ringR, sp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44), Color.white); ic.GetComponent<Image>().preserveAspect = true; }
            float tx = left ? 74f : -74f; var al = left ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            float cw = (W - 300f) / 2f - 14f - 84f;
            var n1 = Label(cell, "Slot", names[i], 12, al, new Vector2(left ? tx : -(cw + 74f) + cw, -8f), new Vector2(cw, 16), new Color(0.74f, 0.83f, 0.93f));
            var n2 = Label(cell, "Item", it != null ? it.name : "—", 15, al, new Vector2(left ? tx : -(cw + 74f) + cw, -26f), new Vector2(cw, 34), it != null ? q : new Color(1f, 1f, 1f, 0.4f)); n2.horizontalOverflow = HorizontalWrapMode.Wrap; Fit(n2, 11);
            if (!left) { foreach (var t in new[] { n1, n2 }) { t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(1f, 1f); t.rectTransform.pivot = new Vector2(1f, 1f); t.rectTransform.anchoredPosition = new Vector2(tx, t == n1 ? -8f : -26f); } }
            if (it != null) { var itt = it; taps.Add(new TapBtn { rt = cell, layer = 5, group = "work", act = () => OpenInspect(itt) }); }
        }
    }

    // =====================================================================================================
    // ---------- moving the buttons: the HUD layout editor ----------
    // =====================================================================================================
    class HudGroup { public string id, name; public List<RectTransform> rts = new List<RectTransform>(); public Dictionary<RectTransform, Vector2> home = new Dictionary<RectTransform, Vector2>(); public bool scales; public RectTransform frame; }
    readonly List<HudGroup> hudGroups = new List<HudGroup>();
    bool hudEdit; RectTransform hudEditRoot; HudGroup hudDrag; Vector2 hudDragLast; float hudScale = 1f;
    public bool HudEditing { get { return hudEdit; } }

    void HudLayoutCapture()
    {
        hudGroups.Clear();
        Func<string, string, bool, string[], HudGroup> G = (id, name, scales, names) =>
        {
            var gr = new HudGroup { id = id, name = name, scales = scales };
            foreach (var n in names) { var r = FindUI(n); if (r != null && !gr.rts.Contains(r)) gr.rts.Add(r); }
            hudGroups.Add(gr); return gr;
        };
        var joy = G("joy", "Joystick", true, new string[0]); if (joyBase != null) joy.rts.Add(joyBase);
        var fight = G("fight", "Fighting buttons", true, new[] { "PotHeal", "PotMana", "PotFood", "PotBuff", "BagBtn", "LeapBtn", "EmoteBtn", "AttackLabel" });
        if (atkBtn != null) fight.rts.Add(atkBtn); if (dodgeBtn != null) fight.rts.Add(dodgeBtn); if (modeBtn != null) fight.rts.Add(modeBtn); if (modeGlow != null) fight.rts.Add(modeGlow.rectTransform);
        for (int i = 0; i < 6; i++) if (spellBtns[i] != null) fight.rts.Add(spellBtns[i].rt);
        var top = G("top", "Minimap and buttons", false, new[] { "MapBtn", "MenuBtn", "PetsBtn", "StoreBtn", "PassBtn", "DailyChip", "EvChip" });
        if (mmRoot != null) top.rts.Add(mmRoot); if (slideBtn != null) top.rts.Add(slideBtn); if (timerPill != null) top.rts.Add(timerPill);
        var quest = G("quest", "Quest card", false, new[] { "MailBtn" }); if (questPanel != null) quest.rts.Add(questPanel);
        var chat = G("chat", "Chat", false, new string[0]); if (chatBox != null) chat.rts.Add(chatBox);
        var ride = G("ride", "Ride button", true, new[] { "RideBtn" });
        foreach (var gr in hudGroups) foreach (var r in gr.rts) gr.home[r] = r.anchoredPosition;
        try { hudScale = AHPrefs.GetFloat("ah_hud_scale", 1f); } catch { hudScale = 1f; }
        HudApply();
    }
    Vector2 HudOff(string id) { try { return new Vector2(AHPrefs.GetFloat("ah_hud_" + id + "_x", 0f), AHPrefs.GetFloat("ah_hud_" + id + "_y", 0f)); } catch { return Vector2.zero; } }
    void HudApply()
    {
        foreach (var gr in hudGroups)
        {
            Vector2 off = HudOff(gr.id);
            foreach (var r in gr.rts)
            {
                if (r == null) continue;
                Vector2 h; if (!gr.home.TryGetValue(r, out h)) continue;
                r.anchoredPosition = h + off;
                if (slideHome.ContainsKey(r)) slideHome[r] = h + off;
                if (gr.scales && r != modeGlow?.rectTransform) r.localScale = Vector3.one * hudScale;
            }
        }
    }
    public void OpenHudEdit()
    {
        ShowWork(false); hudEdit = true;
        if (hudEditRoot == null)
        {
            hudEditRoot = Box("HudEdit", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            hudEditRoot.anchorMin = Vector2.zero; hudEditRoot.anchorMax = Vector2.one; hudEditRoot.offsetMin = hudEditRoot.offsetMax = Vector2.zero;
            var bar = Img("Bar", hudEditRoot, cgCard != null ? cgCard : white, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(520, 120), Color.white);
            if (cgCard != null) bar.GetComponent<Image>().type = Image.Type.Sliced;
            var t = Label(bar, "T", "Drag the glowing frames to move the buttons", 17, TextAnchor.UpperCenter, new Vector2(0f, -10f), new Vector2(500, 24), CGGold);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f); t.rectTransform.pivot = new Vector2(0.5f, 1f); t.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            string[] labels = { "Smaller", "Bigger", "Reset", "Done" };
            for (int i = 0; i < 4; i++)
            {
                int k = i; var b = Img("B", bar, cgPillSky != null ? cgPillSky : white, new Vector2(0.5f, 0f), new Vector2(-186f + i * 124f, 30f), new Vector2(112, 40), Color.white);
                if (cgPillSky != null) b.GetComponent<Image>().type = Image.Type.Sliced;
                Center(Label(b, "T", labels[i], 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(112, 34), Color.white));
                taps.Add(new TapBtn { rt = b, layer = 12, act = () => HudEditButton(k) });
            }
        }
        foreach (var gr in hudGroups)
        {
            if (gr.frame == null)
            {
                gr.frame = Img("Frame " + gr.id, hudEditRoot, cgRim != null ? cgRim : white, new Vector2(0f, 0f), Vector2.zero, new Vector2(10, 10), new Color(0.5f, 0.88f, 1f, 0.9f));
                if (cgRim != null) gr.frame.GetComponent<Image>().type = Image.Type.Sliced;
                gr.frame.pivot = Vector2.zero;
                var lt = Label(gr.frame, "L", gr.name, 14, TextAnchor.UpperCenter, Vector2.zero, new Vector2(200, 20), Color.white); Shade(lt);
                lt.rectTransform.anchorMin = lt.rectTransform.anchorMax = new Vector2(0.5f, 1f); lt.rectTransform.pivot = new Vector2(0.5f, 0f); lt.rectTransform.anchoredPosition = new Vector2(0f, 2f);
            }
        }
        hudEditRoot.gameObject.SetActive(true); hudEditRoot.SetAsLastSibling(); ptrs.Clear();
    }
    void HudEditButton(int k)
    {
        if (k == 0 || k == 1) { hudScale = Mathf.Clamp(hudScale + (k == 1 ? 0.06f : -0.06f), 0.76f, 1.36f); try { AHPrefs.SetFloat("ah_hud_scale", hudScale); } catch { } HudApply(); }
        else if (k == 2) { foreach (var gr in hudGroups) { try { AHPrefs.SetFloat("ah_hud_" + gr.id + "_x", 0f); AHPrefs.SetFloat("ah_hud_" + gr.id + "_y", 0f); } catch { } } hudScale = 1f; try { AHPrefs.SetFloat("ah_hud_scale", 1f); } catch { } HudApply(); }
        else { hudEdit = false; hudEditRoot.gameObject.SetActive(false); Toast("Your layout is saved.", 2f); }
    }
    Rect HudBounds(HudGroup gr)
    {
        bool any = false; Rect r = new Rect(); var cs = new Vector3[4];
        foreach (var t in gr.rts)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            t.GetWorldCorners(cs);
            float x0 = Mathf.Min(cs[0].x, cs[2].x), x1 = Mathf.Max(cs[0].x, cs[2].x), y0 = Mathf.Min(cs[0].y, cs[2].y), y1 = Mathf.Max(cs[0].y, cs[2].y);
            if (!any) { r = Rect.MinMaxRect(x0, y0, x1, y1); any = true; } else r = Rect.MinMaxRect(Mathf.Min(r.xMin, x0), Mathf.Min(r.yMin, y0), Mathf.Max(r.xMax, x1), Mathf.Max(r.yMax, y1));
        }
        return r;
    }
    void HudEditTick()
    {
        if (!hudEdit || hudEditRoot == null) return;
        float k = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        // the frames follow their groups
        foreach (var gr in hudGroups)
        {
            if (gr.frame == null) continue;
            var b = HudBounds(gr); bool on = b.width > 1f;
            gr.frame.gameObject.SetActive(on); if (!on) continue;
            gr.frame.anchoredPosition = new Vector2(b.xMin / k - 6f, b.yMin / k - 6f); gr.frame.sizeDelta = new Vector2(b.width / k + 12f, b.height / k + 12f);
        }
        // a finger on a frame drags it
        var list = AHInput.Pointers();
        if (list.Count == 0) { if (hudDrag != null) { var o = HudOff(hudDrag.id); try { AHPrefs.SetFloat("ah_hud_" + hudDrag.id + "_x", o.x); } catch { } } hudDrag = null; return; }
        var q = list[0];
        if (hudDrag == null)
        {
            foreach (var gr in hudGroups) { var b = HudBounds(gr); if (b.width > 1f && b.Contains(q.pos)) { hudDrag = gr; hudDragLast = q.pos; break; } }
            return;
        }
        Vector2 d = (q.pos - hudDragLast) / k; hudDragLast = q.pos;
        if (d.sqrMagnitude < 1e-4f) return;
        var off = HudOff(hudDrag.id) + d;
        try { AHPrefs.SetFloat("ah_hud_" + hudDrag.id + "_x", off.x); AHPrefs.SetFloat("ah_hud_" + hudDrag.id + "_y", off.y); } catch { }
        HudApply();
    }

    // =====================================================================================================
    // ---------- Arabic: windows laid out right to left ----------
    // =====================================================================================================
    // the panel's contents mirrored: every box moved to the other side of its parent, every line set against the other edge;
    // pictures and words are never turned back to front, only moved
    readonly HashSet<RectTransform> mirrored = new HashSet<RectTransform>();
    static TextAnchor FlipAnchor(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft: return TextAnchor.UpperRight; case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight; case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
            case TextAnchor.UpperRight: return TextAnchor.UpperLeft; case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft; case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
        }
        return a;
    }
    static void Reflect(RectTransform r)
    {
        var par = r.parent as RectTransform; if (par == null) return;
        float cx = par.rect.center.x, w = r.rect.width, k = 0.5f - r.pivot.x;
        float c = r.localPosition.x + k * w * r.localScale.x;
        var lp = r.localPosition; lp.x = 2f * cx - c - k * w * r.localScale.x; r.localPosition = lp;
        var t = r.GetComponent<Text>(); if (t != null) t.alignment = FlipAnchor(t.alignment);
    }
    void Mirror(RectTransform root, bool on)
    {
        if (root == null) return;
        mirrored.RemoveWhere(x => x == null);
        foreach (var r in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (r == root) continue;
            bool was = mirrored.Contains(r);
            if (was == on) continue;
            Reflect(r);
            if (on) mirrored.Add(r); else mirrored.Remove(r);
        }
    }
    public void CloseOverlays()
    {
        if (rwRoot != null) rwRoot.gameObject.SetActive(false);
        if (hudEdit) { hudEdit = false; if (hudEditRoot != null) hudEditRoot.gameObject.SetActive(false); }
        if (tbRoot != null) tbRoot.gameObject.SetActive(false);
    }
    void MirrorWork()
    {
        if (wkList == null) return;
        bool on = AHLoc.Arabic && wkMode != "map";
        Mirror(wkList, on);
        foreach (var t in new[] { wkTitle, wkHint }) if (t != null) t.alignment = on ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
    }
}
