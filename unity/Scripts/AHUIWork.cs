// Ashen Hollow: the work windows, as in the web game: the crafting list at the anvil, loom, cauldron and
// jeweler's bench (web openCraft / renderCraft), and the Guild's profession panel (web openProf / renderProf):
// take up or set aside professions, choose your focus, see mastery, perks and specialisation, change your path.
// Built in code like the rest of the HUD; works by touch and keyboard (Esc closes, arrow keys turn pages).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RectTransform wkRoot, wkPanel, wkList;
    Text wkTitle, wkHint, wkPage;
    readonly List<GameObject> wkItems = new List<GameObject>();
    string wkMode;            // "craft" or "prof"
    AHSpot wkStation;
    string wkProf;            // profession shown in detail, or null for the list
    int wkPageI;
    const float WkW = 820f, WkH = 580f, RowH = 66f;
    const int RowsPerPage = 6;
    public bool WorkOpen { get { return wkRoot != null && wkRoot.gameObject.activeSelf; } }

    void BuildWork()
    {
        wkRoot = Box("Work", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        wkRoot.anchorMin = Vector2.zero; wkRoot.anchorMax = Vector2.one; wkRoot.offsetMin = wkRoot.offsetMax = Vector2.zero;
        var dim = wkRoot.gameObject.AddComponent<Image>(); dim.sprite = null; dim.color = new Color(0f, 0f, 0f, 0.45f); dim.raycastTarget = false;
        Img("WorkBack", wkRoot, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WkW - 16, WkH - 16), new Color(0.085f, 0.065f, 0.05f, 1f));   // so the world never shows through the window
        wkPanel = Img("WorkPanel", wkRoot, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WkW, WkH), new Color(0.1f, 0.075f, 0.055f, 1f));
        Img("Edge", wkPanel, white, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(WkW, 6), new Color(0.85f, 0.65f, 0.3f, 1f));
        wkTitle = Label(wkPanel, "Title", "", 28, TextAnchor.UpperLeft, new Vector2(22, -14), new Vector2(640, 38), new Color(1f, 0.85f, 0.45f));
        wkTitle.fontStyle = FontStyle.Bold;
        wkHint = Label(wkPanel, "Hint", "", 16, TextAnchor.UpperLeft, new Vector2(22, -52), new Vector2(WkW - 44, 44), new Color(1f, 1f, 1f, 0.7f));
        wkHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        wkList = Box("List", wkPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18, -104), new Vector2(WkW - 36, RowH * RowsPerPage));
        var close = Img("Close", wkPanel, white, new Vector2(1f, 1f), new Vector2(-36, -32), new Vector2(48, 48), new Color(0.55f, 0.18f, 0.14f, 1f));
        Center(Label(close, "X", "X", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(48, 40), Color.white));
        taps.Add(new TapBtn { rt = close, layer = 5, act = () => ShowWork(false) });
        var prev = Img("Prev", wkPanel, white, new Vector2(0.5f, 0f), new Vector2(-150, 38), new Vector2(150, 50), new Color(0.3f, 0.22f, 0.17f, 1f));
        Center(Label(prev, "T", "◀ Prev", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 36), Color.white));
        taps.Add(new TapBtn { rt = prev, layer = 5, act = () => { wkPageI--; RenderWork(); } });
        var next = Img("Next", wkPanel, white, new Vector2(0.5f, 0f), new Vector2(150, 38), new Vector2(150, 50), new Color(0.3f, 0.22f, 0.17f, 1f));
        Center(Label(next, "T", "Next ▶", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 36), Color.white));
        taps.Add(new TapBtn { rt = next, layer = 5, act = () => { wkPageI++; RenderWork(); } });
        wkPage = Center(Label(wkPanel, "Page", "", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(140, 30), new Color(1f, 1f, 1f, 0.75f)));
        wkPage.rectTransform.anchorMin = wkPage.rectTransform.anchorMax = new Vector2(0.5f, 0f); wkPage.rectTransform.anchoredPosition = new Vector2(0, 38);
        wkRoot.gameObject.SetActive(false);
    }

    public void ShowWork(bool on)
    {
        if (!on) AHItemStudio.StopHero();
        if (wkRoot == null) return;
        if (on) { ShowBag(false); ShowDialog(false); }
        wkRoot.gameObject.SetActive(on);
        wkRoot.SetAsLastSibling();
        ptrs.Clear();
    }

    int workShutFrame = -1;

    void WorkKeys()
    {
        if (!WorkOpen) return;
        // M closes the map again, B closes any window (Escape too)
        if ((wkMode == "map" && AHInput.MapKey()) || AHInput.BagKey()) { ShowWork(false); workShutFrame = Time.frameCount; return; }
        if (AHInput.BackKey()) { if (wkMode == "prof" && wkProf != null) { wkProf = null; wkPageI = 0; RenderWork(); } else ShowWork(false); }
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.leftArrowKey.wasPressedThisFrame) { wkPageI--; RenderWork(); }
        if (kb != null && kb.rightArrowKey.wasPressedThisFrame) { wkPageI++; RenderWork(); }
#endif
    }

    // web openCraft
    public void OpenCraft(AHSpot station)
    {
        wkMode = "craft"; wkStation = station; wkPageI = 0;
        ShowWork(true);
        RenderWork();
    }

    // web openProf (from the Guild Clerk)
    public void OpenProf()
    {
        wkMode = "prof"; wkProf = null; wkPageI = 0;
        ShowWork(true);
        RenderWork();
    }

    class WkBtn { public string label; public bool on; public Color col; public Action act; }
    Sprite rowIcon; bool rowIconDim; ItemDef rowItem;

    void ClearRows()
    {
        foreach (var go in wkItems) Destroy(go);
        wkItems.Clear();
        RemoveTaps("work");
    }

    // a row with an item's icon at its left
    void RowI(ItemDef it, int slot, string title, Color tc, string line2, string line3, params WkBtn[] btns) { rowItem = it; Row(slot, title, tc, line2, line3, btns); }

    // up to three even lines of text in one row (a story passage), with no title above them
    void TextRow(int slot, string a, string b, string c)
    {
        float y = -slot * RowH;
        var bg = Img("Row", wkList, white, new Vector2(0f, 1f), new Vector2((WkW - 36) / 2f, y - RowH / 2f + 2), new Vector2(WkW - 36, RowH - 6), new Color(0.13f, 0.1f, 0.08f, 1f));
        wkItems.Add(bg.gameObject);
        string[] ls = { a, b, c };
        for (int i = 0; i < 3; i++) if (!string.IsNullOrEmpty(ls[i])) Label(bg, "L" + i, "<i>" + ls[i] + "</i>", 15, TextAnchor.UpperLeft, new Vector2(12f, -6f - i * 19f), new Vector2(WkW - 60, 20), new Color(1f, 0.95f, 0.85f, 0.9f));
    }

    void Row(int slot, string title, Color tc, string line2, string line3, params WkBtn[] btns)
    {
        float y = -slot * RowH;
        var bg = Img("Row", wkList, white, new Vector2(0f, 1f), new Vector2((WkW - 36) / 2f, y - RowH / 2f + 2), new Vector2(WkW - 36, RowH - 6), new Color(0.16f, 0.12f, 0.09f, 1f));
        wkItems.Add(bg.gameObject);
        // an optional icon at the row's left (spells); set rowIcon just before calling Row
        float tx = 12f;
        if (rowIcon != null) { var ic = Img("Icon", bg, rowIcon, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(54, 54), rowIconDim ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white); ic.GetComponent<Image>().preserveAspect = true; tx = 72f; }
        else if (rowItem != null)   // an item: its coloured circle with its picture
        {
            var gs = AHItemIcons.Best(rowItem); bool full = AHItemIcons.Full(rowItem, gs);
            var ic = Img("Item", bg, circle, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(52, 52), full ? new Color(0.2f, 0.15f, 0.11f, 1f) : new Color(rowItem.color.r * 0.85f, rowItem.color.g * 0.85f, rowItem.color.b * 0.85f, 1f));
            if (gs != null) { var gl = Img("Glyph", ic, gs, new Vector2(0.5f, 0.5f), Vector2.zero, full ? new Vector2(50, 50) : new Vector2(36, 36), Color.white); gl.GetComponent<Image>().preserveAspect = true; }
            // gear: tap its picture to look at it in its own window (and try it on)
            if (rowItem.IsGear)
            {
                var it = rowItem;
                var tag = Img("View", ic, white, new Vector2(0.5f, 0f), new Vector2(0, 2), new Vector2(40, 15), new Color(0.1f, 0.3f, 0.5f, 0.95f));
                Center(Label(tag, "T", "VIEW", 10, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40, 14), Color.white));
                taps.Add(new TapBtn { rt = ic, layer = 5, group = "work", act = () => OpenInspect(it) });
            }
            tx = 72f;
        }
        rowIcon = null; rowIconDim = false; rowItem = null;
        var t = Label(bg, "Name", title, 19, TextAnchor.UpperLeft, new Vector2(tx, -4), new Vector2(520, 24), tc); t.fontStyle = FontStyle.Bold;
        if (!string.IsNullOrEmpty(line2)) Label(bg, "L2", line2, 15, TextAnchor.UpperLeft, new Vector2(tx, -26), new Vector2(560 - (tx - 12f), 20), new Color(1f, 1f, 1f, 0.8f));
        if (!string.IsNullOrEmpty(line3)) Label(bg, "L3", line3, 15, TextAnchor.UpperLeft, new Vector2(tx, -44), new Vector2(560 - (tx - 12f), 20), new Color(1f, 1f, 1f, 0.65f));
        float x = -12f;
        for (int i = btns.Length - 1; i >= 0; i--)
        {
            var b = btns[i];
            if (b == null) continue;
            float w = Mathf.Max(96f, b.label.Length * 11f + 24f);
            var br = Img("Btn", bg, white, new Vector2(1f, 0.5f), new Vector2(x - w / 2f, 0), new Vector2(w, 44), b.on ? b.col : new Color(0.28f, 0.25f, 0.22f, 1f));
            Center(Label(br, "T", b.label, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(w, 32), b.on ? Color.white : new Color(1f, 1f, 1f, 0.45f)));
            x -= w + 8f;
            var act = b.act; bool on = b.on;
            taps.Add(new TapBtn { rt = br, layer = 5, group = "work", act = () => { if (on && act != null) act(); } });
        }
    }

    static readonly Color Go = new Color(0.22f, 0.5f, 0.26f, 1f), Plain = new Color(0.35f, 0.27f, 0.2f, 1f);
    static string Bad(string s, bool ok) { return ok ? s : "<color=#ff8a7a>" + s + "</color>"; }

    void RenderWork()
    {
        if (!WorkOpen) return;
        ClearRows(); SetPager(true);
        if (wkMode != "stats") AHItemStudio.StopHero();
        var p = g.player;
        if (wkMode == "craft") RenderCraft(p);
        else if (wkMode == "shop") RenderShop(p);
        else if (wkMode == "bank") RenderBank(p);
        else if (wkMode == "comp") RenderComp(p);
        else if (wkMode == "ah") RenderAH(p);
        else if (wkMode == "orders") RenderOrders(p);
        else if (wkMode == "farm") RenderFarm(p);
        else if (wkMode == "hub") RenderHub(p);
        else if (wkMode == "finder") RenderFinder(p);
        else if (wkMode == "board") RenderBoard(p);
        else if (wkMode == "rep") RenderRepList(p);
        else if (wkMode == "deeds") RenderDeeds(p);
        else if (wkMode == "daily") RenderDaily(p);
        else if (wkMode == "enh") RenderEnh(p);
        else if (wkMode == "events") RenderEvents(p);
        else if (wkMode == "ways") RenderWays(p);
        else if (wkMode == "map") RenderMap(p);
        else if (wkMode == "coll") RenderCollection(p);
        else if (wkMode == "gems") RenderGems(p);
        else if (wkMode == "stats") RenderStats(p);
        else if (wkMode == "class") RenderClass(p);
        else if (wkMode == "friend") RenderFriend(p);
        else if (wkMode == "friends") RenderFriends(p);
        else if (wkMode == "kq") RenderKQ(p);
        else if (wkMode == "fest") RenderFest(p);
        else if (wkMode == "pb" || wkMode == "pbfight") RenderPetBattles(p);
        else if (wkMode == "mq") RenderMarket(p);
        else if (wkMode == "night") RenderNight(p);
        else if (wkMode == "ward") RenderWardrobe(p);
        else if (wkMode == "house") RenderHouse(p);
        else if (wkMode == "settings") RenderOptions(p);
        else if (wkMode == "emotes") RenderEmotes(p);
        else if (wkMode == "art") RenderArtisan(p);
        else if (wkMode == "loadouts") RenderLoadouts(p);
        else if (wkMode == "world") RenderRealm(p);
        else RenderProf(p);
    }

    int Paged(int count)
    {
        int pages = Mathf.Max(1, (count + RowsPerPage - 1) / RowsPerPage);
        wkPageI = Mathf.Clamp(wkPageI, 0, pages - 1);
        wkPage.text = pages > 1 ? (wkPageI + 1) + " / " + pages : "";
        return wkPageI * RowsPerPage;
    }

    // ---------- web renderCraft ----------
    void RenderCraft(AHPlayer p)
    {
        string kind = wkStation.kind;
        wkTitle.text = AHGather.StationName(kind);
        wkHint.text = AHGather.StationHint(kind);
        var list = AHGather.Recipes(kind);
        int from = Paged(list.Count);
        for (int i = from; i < Mathf.Min(list.Count, from + RowsPerPage); i++)
        {
            var r = list[i]; var it = AHItems.Get(r.outId);
            bool lvOk = p.Skill(r.skill) >= r.lvl && AHGather.MasteryOk(p, r) && (!AHArtisan.Only(r.outId) || p.path == "artisan"), ok = lvOk && AHGather.HasMats(p, r);
            string l2 = it.slot != null ? AHItems.StatLine(it) + " · " + Bad(AHItems.WhoUses(r.outId), AHItems.CanUse(r.outId, p.cls.id)) : (it.note ?? "");
            if (r.n > 1) l2 += " · makes " + r.n;
            var mats = new List<string>();
            foreach (var m in r.mats) { var md = AHItems.Get(m.Key); int have = p.bag.Count(m.Key); mats.Add(Bad(have + "/" + m.Value + " " + (md != null ? md.name : m.Key), have >= m.Value)); }
            string lv = AHDB.SkillName(r.skill) + " " + r.lvl + (r.masteryProf != null ? " · " + AHJson.S(AHJson.O(AHPlayer.Profs, r.masteryProf), "name", r.masteryProf) + " mastery " + r.masteryLv : "");
            if (AHArtisan.Only(r.outId)) lv += " · Artisans only";
            string l3 = string.Join(" · ", mats.ToArray()) + " · " + Bad(lv, lvOk);
            var rr = r;
            rowItem = it;
            Row(i - from, it.name, AHItems.Quality(it), l2, l3, new WkBtn
            {
                label = "Make", on = ok, col = Go,
                act = () => { ShowWork(false); AHGather.Start(p, wkStation, rr); }
            });
        }
    }

    // ---------- web renderProf ----------
    void RenderProf(AHPlayer p)
    {
        var all = AHPlayer.Profs as Dictionary<string, object>;
        if (all == null) return;
        if (wkProf != null) { RenderProfDetail(p, wkProf); return; }
        wkTitle.text = p.profs.Count > 0 ? "Your professions" : "Choose a profession";
        wkHint.text = "Professions you take up learn 25% faster, earn mastery and unlock perks. Your ★ focus is the guild whose work orders you take. Taking up or setting aside is free; mastery is never lost.";
        var keys = new List<string>(all.Keys);
        // the first row: your guild's work orders
        keys.Insert(0, "__orders");
        int count = keys.Count + 1;
        int from = Paged(count);
        for (int i = from; i < Mathf.Min(count, from + RowsPerPage); i++)
        {
            if (i == keys.Count)
            {
                bool art = p.path == "artisan";
                string pn = art ? "Artisan" : "Adventurer";
                var pl = AHJson.A(AHDB.Table("paths", "PATHS_LIFE"), art ? "artisan" : "adventurer");
                if (pl != null && pl.Count > 0) pn = pl[0] as string;
                string pd = pl != null && pl.Count > 1 ? pl[1] as string : "";
                Row(i - from, "Your path: " + pn, new Color(1f, 0.8f, 0.45f), pd, "", new WkBtn { label = art ? "Become an Adventurer" : "Become an Artisan", on = true, col = Plain, act = () => SwitchPath(p) });
                continue;
            }
            if (keys[i] == "__orders")
            {
                int ready = 0; foreach (var o in p.orders) if (AHOrders.Ready(p, o)) ready++;
                Row(i - from, "Work orders", new Color(1f, 0.8f, 0.45f), p.profMain == null ? "Take up a profession first" : p.orders.Count + " orders · " + ready + " ready to deliver · guild rank " + AHOrders.RankName[AHOrders.Rank(p)], "",
                    new WkBtn { label = "Orders", on = p.profMain != null, col = Go, act = () => OpenOrders() });
                continue;
            }
            string k = keys[i]; var pr = all[k];
            string name = AHJson.S(pr, "name", k), skill = AHJson.S(pr, "skill");
            bool has = p.profs.Contains(k); int L = p.MasteryLv(k);
            string tag = k == p.profMain ? "  <color=#ffb04a>★ focus</color>" : has ? "  <color=#9be37a>yours</color>" : "";
            string l2 = AHDB.Rank(L) + " · Mastery " + L + " · " + AHDB.SkillName(skill) + " " + p.Skill(skill) + (p.profSpec.ContainsKey(k) ? " · " + SpecName(pr, p.profSpec[k]) : "");
            string l3 = AHJson.S(pr, "blurb", "");
            string kk = k;
            Row(i - from, name + tag, Color.white, l2, l3,
                new WkBtn { label = "Perks", on = true, col = Plain, act = () => { wkProf = kk; wkPageI = 0; RenderWork(); } },
                has && k != p.profMain ? new WkBtn { label = "Focus", on = true, col = Plain, act = () => { p.profMain = kk; Toast("Your work orders now come from the " + name.ToLowerInvariant() + "s’ guild."); g.MarkDirty(); RenderWork(); } } : null,
                has ? new WkBtn { label = "Set aside", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => DropProf(p, kk, name) }
                    : new WkBtn { label = "Take up", on = true, col = Go, act = () => TakeProf(p, kk, name, skill) });
        }
    }

    static string SpecName(object pr, string id) { var a = AHJson.A(AHJson.O(pr, "specs"), id); return a != null && a.Count > 0 ? a[0] as string : id; }

    void RenderProfDetail(AHPlayer p, string k)
    {
        var pr = AHJson.O(AHPlayer.Profs, k);
        string name = AHJson.S(pr, "name", k), skill = AHJson.S(pr, "skill");
        int L = p.MasteryLv(k); long xp; p.profXp.TryGetValue(k, out xp);
        var at = AHDB.MAt; long lo = L < at.Length ? at[L] : 0, hi = L + 1 < at.Length ? at[L + 1] : lo;
        wkTitle.text = AHDB.Rank(L) + " " + name;
        wkHint.text = "Mastery " + L + (L >= AHDB.MMax ? " · max" : " · " + (xp - lo).ToString("#,0") + " / " + (hi - lo).ToString("#,0") + " mastery XP") + "\n" + AHDB.SkillName(skill) + " " + p.Skill(skill) + " · " + (p.profs.Contains(k) ? "+25% " + AHDB.SkillName(skill) + " XP · mastery grows with every " + AHDB.SkillName(skill).ToLowerInvariant() + " action" : "take it up at the Guild to earn mastery");
        var rows = new List<Action<int>>();
        var perks = AHJson.A(pr, "perks");
        if (perks != null) foreach (var o in perks)
            {
                var row = o as List<object>; if (row == null || row.Count < 2) continue;
                int pl = (int)(double)row[0]; string txt = (string)row[1];
                rows.Add(slot => Row(slot, "Mastery " + pl + (L >= pl ? "  <color=#9be37a>✓</color>" : "  <color=#9a9080>locked</color>"), L >= pl ? Color.white : new Color(0.7f, 0.66f, 0.6f), txt, ""));
            }
        var specs = AHJson.O(pr, "specs") as Dictionary<string, object>;
        string chosen; p.profSpec.TryGetValue(k, out chosen);
        if (specs != null) foreach (var kv in specs)
            {
                var a = kv.Value as List<object>; string sn = a != null && a.Count > 0 ? a[0] as string : kv.Key, st = a != null && a.Count > 1 ? a[1] as string : "";
                string id = kv.Key;
                rows.Add(slot => Row(slot, "Specialisation: " + sn + (chosen == id ? "  <color=#9be37a>chosen</color>" : ""), new Color(1f, 0.8f, 0.45f), st, "Unlocks at mastery 15",
                    chosen != null ? null : new WkBtn
                    {
                        label = "Choose", on = L >= 15 && p.profs.Contains(k), col = Go,
                        act = () => { p.profSpec[k] = id; Banner(sn, "Specialisation chosen"); g.MarkDirty(); RenderWork(); }
                    }));
            }
        rows.Add(slot => Row(slot, "Back to all professions", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkProf = null; wkPageI = 0; RenderWork(); } }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void TakeProf(AHPlayer p, string k, string name, string skill)
    {
        if (p.profs.Contains(k)) return;
        p.profs.Add(k);
        if (p.profMain == null) p.profMain = k;
        Banner(name, p.profs.Count > 1 ? "Profession " + p.profs.Count + " taken up" : "Your first profession");
        Toast("You take up " + name.ToLowerInvariant() + " work. " + AHDB.SkillName(skill) + " now learns 25% faster.", 3f);
        g.quests.RoadCheck(g);
        g.MarkDirty();
        RenderWork();
    }

    void DropProf(AHPlayer p, string k, string name)
    {
        p.profs.Remove(k);
        if (p.profMain == k) p.profMain = p.profs.Count > 0 ? p.profs[0] : null;
        Toast("You set " + name.ToLowerInvariant() + " aside. Your mastery is kept.", 3f);
        g.MarkDirty();
        RenderWork();
    }

    // web: change path at a Guild Registrar; peace follows the path
    void SwitchPath(AHPlayer p)
    {
        p.path = p.path == "artisan" ? "adventurer" : "artisan";
        p.peace = p.path == "artisan" ? 1 : 0;
        Banner(p.path == "artisan" ? "The Artisan’s life" : "The Adventurer’s life", p.path == "artisan" ? "You level up through your professions" : "You level up by fighting");
        g.quests.RoadCheck(g);
        g.MarkDirty();
        RenderWork();
    }
}
