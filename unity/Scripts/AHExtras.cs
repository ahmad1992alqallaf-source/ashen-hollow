// Ashen Hollow: quality-of-life and long-term goals.
//  - bag rows bought with Adventurer's Marks (up to three more rows of nine);
//  - gear loadouts: save what you wear in a slot and put it all back on in one tap;
//  - sorting the bag, and selling your old gear (pieces worse than what you wear) at a shop in one go;
//  - salvaging gear you don't need back into some of its materials;
//  - the season track: thirty tiers of rewards a season (three months), earned with the marks your goals pay;
//  - weapon mastery: kills with your class's weapon light it with a glow (blue, violet, then gold).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHLoadout { public string name; public List<AHKS> gear = new List<AHKS>(); }

public static class AHExtras
{
    // ---------- bag rows ----------
    public const int MaxRows = 3;
    public static int RowCost(AHPlayer p) { return 150 * (1 << p.prog.bagRows); }   // 150, 300, 600 marks
    public static bool BuyRow(AHGame g)
    {
        var p = g.player; if (p.prog.bagRows >= MaxRows) return false;
        int c = RowCost(p); if (p.prog.daily.marks < c) return false;
        p.prog.daily.marks -= c; p.prog.bagRows++; p.bag.extra = p.prog.bagRows * 9;
        g.SaveProgress();
        return true;
    }

    // ---------- loadouts ----------
    public const int Loadouts = 3;
    public static AHLoadout Slot(AHPlayer p, int i)
    {
        while (p.prog.loadouts.Count < Loadouts) p.prog.loadouts.Add(new AHLoadout { name = "Set " + (p.prog.loadouts.Count + 1) });
        return p.prog.loadouts[i];
    }
    public static void Save(AHPlayer p, int i)
    {
        var L = Slot(p, i); L.gear.Clear();
        foreach (var s in AHItems.GearSlots) { string id = p.bag.Worn(s); if (id != null) L.gear.Add(new AHKS { k = s, v = id }); }
    }
    // puts the set on; returns how many pieces were missing (sold, dropped, in the bank)
    public static int Wear(AHPlayer p, int i)
    {
        var L = Slot(p, i); int missing = 0;
        foreach (var kv in L.gear)
        {
            if (p.bag.Worn(kv.k) == kv.v) continue;
            if (p.bag.Count(kv.v) > 0) p.bag.Equip(kv.v, p.cls.id); else missing++;
        }
        return missing;
    }
    public static string Describe(AHPlayer p, int i)
    {
        var L = Slot(p, i); if (L.gear.Count == 0) return "Empty: wear what you like, then Save";
        var names = new List<string>();
        foreach (var kv in L.gear) { var it = AHItems.Get(kv.v); if (it != null && (kv.k == "weapon" || kv.k == "chest" || kv.k == "head")) names.Add(it.name); }
        return L.gear.Count + " pieces" + (names.Count > 0 ? ": " + string.Join(", ", names.ToArray()) : "");
    }

    // ---------- sorting ----------
    static int Rank(ItemDef d)
    {
        if (d == null) return 9;
        if (d.IsGear) return 0;
        if (d.potion) return 1;
        if (d.IsFood) return 2;
        if (d.id == "enh_stone" || d.id == "lucky_charm" || d.id == "mystery_sack" || AHPower.GemText(d.id) != "" || d.id.StartsWith("card_") || d.reins != null || AHTreasure.IsMap(d.id)) return 3;
        return 4;   // materials
    }
    public static void Sort(AHBag bag)
    {
        var l = new List<string>(bag.order);
        l.Sort((a, b) =>
        {
            var A = AHItems.Get(a); var B = AHItems.Get(b);
            int r = Rank(A).CompareTo(Rank(B)); if (r != 0) return r;
            if (A != null && B != null && A.IsGear && B.IsGear) { int sa = Array.IndexOf(AHItems.GearSlots, A.slot), sb = Array.IndexOf(AHItems.GearSlots, B.slot); if (sa != sb) return sa.CompareTo(sb); }
            return string.Compare(A != null ? A.name : a, B != null ? B.name : b, StringComparison.OrdinalIgnoreCase);
        });
        bag.order.Clear(); bag.order.AddRange(l); bag.Touch();
    }

    // ---------- old gear ----------
    // pieces in the bag that are no better than what you wear in their slot (or that your class can't use);
    // set pieces, legendary, masterwork and cosmetic items are always kept
    public static List<string> OldGear(AHPlayer p)
    {
        var l = new List<string>();
        foreach (var id in p.bag.order)
        {
            var d = AHItems.Get(id); if (d == null || !d.IsGear || d.cosmetic || p.bag.Count(id) <= 0) continue;
            if (d.rarity == "set" || d.rarity == "legend" || d.rarity == "masterwork" || d.rarity == "rep" || id.EndsWith("_mw")) continue;
            var w = AHItems.Get(p.bag.Worn(d.slot)); if (w == null) continue;
            if (!AHItems.CanUse(id, p.cls.id) || AHUI.GearScore(d) <= AHUI.GearScore(w)) l.Add(id);
        }
        return l;
    }

    // ---------- salvage ----------
    public static AHRecipe RecipeOf(string id)
    {
        string b = id.EndsWith("_mw") ? id.Substring(0, id.Length - 3) : id;
        foreach (var st in new[] { "anvil", "loom", "jewel" }) foreach (var r in AHGather.Recipes(st)) if (r.outId == b) return r;
        return null;
    }
    public static bool CanSalvage(AHPlayer p, ItemDef d) { return d != null && d.IsGear && !d.cosmetic && p.bag.Count(d.id) > 0 && RecipeOf(d.id) != null; }
    public static string Salvage(AHGame g, ItemDef d)
    {
        var p = g.player; var r = RecipeOf(d.id); if (r == null || !p.bag.Take(d.id)) return null;
        var got = new List<string>();
        foreach (var m in r.mats)
        {
            int n = Mathf.Max(1, m.Value / 2) + (d.id.EndsWith("_mw") ? 1 : 0);
            if (p.bag.Add(m.Key, n)) got.Add(n + "× " + AHItems.Get(m.Key).name);
        }
        g.MarkDirty();
        return got.Count > 0 ? "Salvaged " + d.name + ": " + string.Join(", ", got.ToArray()) : "Salvaged " + d.name + ".";
    }

    // ---------- the season track ----------
    public const int Tiers = 30, TierXp = 100;
    public static string SeasonKey(DateTime t) { return t.Year + "-S" + ((t.Month - 1) / 3 + 1); }
    public static string SeasonName(DateTime t) { string[] n = { "Season of Frost", "Season of Bloom", "Season of Embers", "Season of Harvest" }; return n[(t.Month - 1) / 3]; }
    static void Fresh(AHPlayer p) { string k = SeasonKey(DateTime.Now); if (p.prog.seasonKey != k) { p.prog.seasonKey = k; p.prog.seasonXp = 0; p.prog.seasonClaimed = 0; } }
    public static void AddXp(AHPlayer p, int n) { if (p == null || n <= 0) return; Fresh(p); p.prog.seasonXp += n; }
    public static int Tier(AHPlayer p) { Fresh(p); return Mathf.Min(Tiers, p.prog.seasonXp / TierXp); }
    public static int Claimed(AHPlayer p) { Fresh(p); return p.prog.seasonClaimed; }
    public static AHDaily.Reward TierReward(AHPlayer p, int tier)   // tier 1..30
    {
        var r = new AHDaily.Reward { money = (long)Math.Round(150 * (1 + p.level / 5.0) * (1 + tier / 10.0)) };
        if (tier == 30) { r.items.Add(new KeyValuePair<string, int>("diamond", 3)); r.items.Add(new KeyValuePair<string, int>("lucky_charm", 3)); r.marks = 200; }
        else if (tier == 20) { r.items.Add(new KeyValuePair<string, int>("diamond", 1)); r.items.Add(new KeyValuePair<string, int>("enh_stone", 4)); }
        else if (tier == 10) { r.items.Add(new KeyValuePair<string, int>("ruby", 1)); r.items.Add(new KeyValuePair<string, int>("enh_stone", 3)); }
        else if (tier % 5 == 0) { r.items.Add(new KeyValuePair<string, int>("mystery_sack", 2)); }
        else if (tier % 2 == 0) { r.items.Add(new KeyValuePair<string, int>("big_potion", 2)); }
        else r.marks = 15;
        return r;
    }
    public static bool ClaimNext(AHGame g)
    {
        var p = g.player; int next = Claimed(p) + 1;
        if (next > Tier(p)) return false;
        p.prog.seasonClaimed = next;
        AHDaily.Grant(g, TierReward(p, next));
        return true;
    }

    // ---------- weapon mastery ----------
    public static readonly int[] MasteryAt = { 100, 500, 2000 };
    public static readonly Color[] MasteryCol = { new Color(0.4f, 0.75f, 1f), new Color(0.75f, 0.45f, 1f), new Color(1f, 0.8f, 0.3f) };
    public static readonly string[] MasteryName = { "Tempered", "Runed", "Legendary" };
    public static long Kills(AHPlayer p) { return p.cls == null ? 0 : AHProgress.Get(p.prog.weaponKills, p.cls.id); }
    public static int MasteryTier(AHPlayer p) { long k = Kills(p); int t = 0; foreach (var a in MasteryAt) if (k >= a) t++; return t; }
    public static void OnKill(AHPlayer p)
    {
        if (p == null || p.cls == null) return;
        int before = MasteryTier(p);
        AHProgress.Add(p.prog.weaponKills, p.cls.id, 1);
        int after = MasteryTier(p);
        if (after > before)
        {
            p.ApplyWeaponGlow();
            if (AHGame.I != null && AHGame.I.ui != null) AHGame.I.ui.Banner(MasteryName[after - 1] + " weapon", "Your weapon mastery glows brighter");
            AHSound.Play("level");
        }
    }
}

// a soft light on the weapon of a hero whose weapon mastery has grown
public class AHWeaponGlow : MonoBehaviour
{
    public Color col; float t;
    Light li;
    void Start()
    {
        li = gameObject.AddComponent<Light>(); li.type = LightType.Point; li.range = 1.4f; li.intensity = 1.2f; li.color = col; li.shadows = LightShadows.None;
    }
    void Update()
    {
        t += Time.deltaTime;
        if (li != null) li.intensity = 1f + Mathf.Sin(t * 2.4f) * 0.35f;
        if (Mathf.Repeat(t, 0.25f) < Time.deltaTime) AHSpark.Trail(transform.position, new Color(col.r, col.g, col.b, 0.6f), 0.08f, 1);
    }
}

public partial class AHUI
{
    // ---------- loadouts and bag rows (Menu) ----------
    public void OpenLoadouts() { wkMode = "loadouts"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderLoadouts(AHPlayer p)
    {
        wkTitle.text = "Gear loadouts";
        wkHint.text = "Save what you wear now in a set, and put a whole set back on in one tap. The pieces must be in your bag.";
        var rows = new List<Action<int>>();
        for (int i = 0; i < AHExtras.Loadouts; i++)
        {
            int ii = i; var L = AHExtras.Slot(p, i);
            rows.Add(s => Row(s, L.name, new Color(1f, 0.8f, 0.45f), AHExtras.Describe(p, ii), "",
                new WkBtn { label = "Save", on = true, col = Plain, act = () => { AHExtras.Save(p, ii); g.MarkDirty(); Toast(L.name + " saved."); RenderWork(); } },
                new WkBtn { label = "Wear", on = L.gear.Count > 0, col = Go, act = () => { int miss = AHExtras.Wear(p, ii); RefreshClassLook(); Toast(miss > 0 ? miss + " pieces were not in your bag." : L.name + " on."); g.MarkDirty(); RenderWork(); } }));
        }
        int cost = AHExtras.RowCost(p);
        rows.Add(s => Row(s, "Bigger bag · " + p.bag.SlotsMax + " slots", new Color(0.61f, 0.89f, 1f), p.prog.bagRows >= AHExtras.MaxRows ? "Your bag is as big as it gets." : "Nine more slots for " + cost + " Adventurer's Marks (you have " + p.prog.daily.marks + ")", p.prog.bagRows < AHExtras.MaxRows ? "The land reloads to stitch the new row in." : "",
            p.prog.bagRows >= AHExtras.MaxRows ? null : new WkBtn { label = "Buy row", on = p.prog.daily.marks >= cost, col = Go, act = () => { if (AHExtras.BuyRow(g)) { ShowWork(false); Toast("Your bag grows by nine slots."); g.Reload(false); } } }));
        long k = AHExtras.Kills(p); int mt = AHExtras.MasteryTier(p);
        rows.Add(s => Row(s, "Weapon mastery: " + (mt > 0 ? AHExtras.MasteryName[mt - 1] : "none yet"), mt > 0 ? AHExtras.MasteryCol[mt - 1] : Color.white, k.ToString("#,0") + " kills as " + p.cls.name + (mt < 3 ? " · next glow at " + AHExtras.MasteryAt[mt].ToString("#,0") : " · fully mastered"), ""));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
    // the hero's look after gear changed outside the bag window
    void RefreshClassLook() { if (BagOpen) RefreshBag(); }
}

public partial class AHUI
{
    // ---------- photo mode: the HUD tucks away, the camera stays free; a button takes the picture ----------
    readonly List<GameObject> photoHidden = new List<GameObject>();
    RectTransform photoBar;
    public bool PhotoOn { get { return photoBar != null && photoBar.gameObject.activeSelf; } }
    public void PhotoMode(bool on)
    {
        if (on == PhotoOn) return;
        if (on)
        {
            ShowWork(false); ShowBag(false);
            photoHidden.Clear();
            foreach (Transform c in transform) if (c.gameObject.activeSelf && (photoBar == null || c != photoBar)) { photoHidden.Add(c.gameObject); c.gameObject.SetActive(false); }
            if (photoBar == null)
            {
                photoBar = Box("PhotoBar", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16, -16), new Vector2(300, 60));
                var shot = Img("Shot", photoBar, circle, new Vector2(0f, 0.5f), new Vector2(40, 0), new Vector2(60, 60), new Color(0.95f, 0.95f, 0.95f, 0.9f));
                Center(Label(shot, "T", "SNAP", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(60, 24), new Color(0.15f, 0.12f, 0.1f)));
                taps.Add(new TapBtn { rt = shot, act = Snap });
                var ex = Img("Exit", photoBar, white, new Vector2(1f, 0.5f), new Vector2(-80, 0), new Vector2(150, 50), new Color(0.2f, 0.14f, 0.1f, 0.85f));
                Center(Label(ex, "T", "EXIT PHOTO", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 30), Color.white));
                taps.Add(new TapBtn { rt = ex, act = () => PhotoMode(false) });
            }
            photoBar.gameObject.SetActive(true); photoBar.SetAsLastSibling();
        }
        else
        {
            photoBar.gameObject.SetActive(false);
            foreach (var go in photoHidden) if (go != null) go.SetActive(true);
            photoHidden.Clear();
        }
    }
    void Snap()
    {
        string name = "AshenHollow_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        photoBar.gameObject.SetActive(false);
        StartCoroutine(SnapLater(name));
    }
    System.Collections.IEnumerator SnapLater(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(name);
        yield return null;
        photoBar.gameObject.SetActive(true);
        AHSound.Play("coin");
        Debug.Log("Ashen Hollow: photo saved as " + System.IO.Path.Combine(Application.persistentDataPath, name));
    }
}
