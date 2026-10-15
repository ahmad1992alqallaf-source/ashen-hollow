// Ashen Hollow: the store (the STORE tag under the top buttons, or Menu -> Store). It sells looks only, never power:
// mounts that run at a riding horse's pace (companions.json "store": true) and painted feathered wings (items.json
// "store": true). Nothing sold here drops in the world.
//  - Paid for in Crowns (p.prog.crowns), the store's own currency. Crowns will be bought with real money through
//    Google Play / the App Store; until that is connected the packs show "coming soon".
//  - While store.json "TEST" is true a free "test Crowns" row is shown. Turn it off before release.
//  - Every item can be tried first: a mount is ridden for a try (AHComp.Try), wings are shown on you while the
//    store is open (AHWardrobe.Preview).
//  - A mount whose model is not in the game yet is left out of the list.
//  - Data: Resources/AH/Data/store.json (ITEMS: id "mount:<key>" or an item id, price, tag; PACKS: crowns, price, bonus).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHStore
{
    public static string Currency { get { return AHJson.S(AHDB.File("store"), "CURRENCY", "Crowns"); } }
    public static bool Test { get { return AHJson.B(AHDB.File("store"), "TEST"); } }
    public static List<object> Items { get { return AHJson.A(AHDB.File("store"), "ITEMS") ?? new List<object>(); } }
    public static List<object> Packs { get { return AHJson.A(AHDB.File("store"), "PACKS") ?? new List<object>(); } }

    public static bool IsMount(string id) { return id != null && id.StartsWith("mount:"); }
    public static string MountKey(string id) { return id.Substring(6); }

    // a store mount is listed only once its model is in the game
    public static bool Ready(string id)
    {
        if (!IsMount(id)) return AHItems.Get(id) != null;
        string k = MountKey(id);
        if (AHJson.O(AHComp.Mounts, k) == null) return false;
        return Resources.Load<GameObject>("AH/Models/Comp/mount_" + k + "_hd") != null || Resources.Load<GameObject>("AH/Models/Comp/mount_" + k) != null;
    }
    public static bool Owned(AHPlayer p, string id) { return IsMount(id) ? p.mounts.Contains(MountKey(id)) : AHWardrobe.Has(p, id); }
    public static string Name(string id) { if (IsMount(id)) return AHComp.MountName(MountKey(id)); var d = AHItems.Get(id); return d != null ? d.name : id; }
    public static string Amount(long n) { return n.ToString("#,0") + " " + Currency; }

    // spends the Crowns and hands the item over; false if it can't be bought
    public static bool Buy(AHGame g, string id, long price)
    {
        var p = g.player; if (p == null || Owned(p, id) || price < 0 || p.prog.crowns < price) return false;
        if (IsMount(id))
        {
            string k = MountKey(id);
            if (p.mounted) AHComp.Dismount(g, true);
            p.prog.crowns -= price; p.mounts.Add(k); p.mountSel = k;
        }
        else
        {
            if (!AHWardrobe.Collect(p, id)) return false;
            p.prog.crowns -= price;
        }
        g.SaveProgress();
        return true;
    }
}

public partial class AHUI
{
    public void OpenStore()
    {
        wkMode = "store"; wkPageI = 0;
        ShowWork(true);
        RenderWork();
    }

    // the wings tried on in the store come off when the window closes
    void EndStorePreview()
    {
        if (AHWardrobe.Preview == null) return;
        AHWardrobe.Preview = null;
        if (g != null && g.player != null) g.player.CheckOutfit();
    }

    void RenderStore(AHPlayer p)
    {
        string cur = AHStore.Currency;
        wkTitle.text = "Store · " + AHStore.Amount(p.prog.crowns);
        wkHint.text = "Everything here is looks only: store mounts run at a riding horse’s pace, and pets don’t change your stats. Mystic egg pets are found only in the Mystic egg.";
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, "Your " + cur + ": " + p.prog.crowns.ToString("#,0"), new Color(1f, 0.85f, 0.4f), "Get more " + cur + " to spend in the store", "",
            new WkBtn { label = "Get " + cur, on = true, col = Go, act = () => { EndStorePreview(); wkMode = "storepacks"; wkPageI = 0; RenderWork(); } }));
        rows.Add(s => Row(s, "Looks  <color=#9be37a>New</color>", new Color(1f, 0.62f, 0.85f), "Weapon skins, hit effects, footstep trails and name colours", "Some are earned in the world, some are here. Try them on first.",
            new WkBtn { label = "Open", on = true, col = Go, act = () => { EndStorePreview(); OpenLooks(); } }));
        // the Mystic egg: hatches at once, one of the store's own pets (found nowhere else); shown once one is in the game
        if (!AHEggs.MysticReady) rows.Add(s => Row(s, AHJson.S(AHEggs.Mystic, "name", "Mystic egg") + "  <color=#c8b89a>coming soon</color>", new Color(0.75f, 0.55f, 1f), "Store-only pets: Rock golem, Spirit wisp, Night prowler, Flame hatchling", "Looks only, like every pet."));
        else
        {
            var es = AHEggs.S(p); long p1 = AHEggs.MysticPrice(1), p10 = AHEggs.MysticPrice(10);
            int pe = (int)AHJson.N(AHEggs.Mystic, "pity_epic", 30), pl = (int)AHJson.N(AHEggs.Mystic, "pity_legendary", 90);
            rows.Add(s => Row(s, AHJson.S(AHEggs.Mystic, "name", "Mystic egg") + "  <color=#9be37a>New</color>", new Color(0.75f, 0.55f, 1f),
                AHEggs.OddsLine(), "Epic or better within " + (pe - es.pityEpic) + " · legendary (Gilded) within " + (pl - es.pityLeg) + " · ten for " + AHStore.Amount(p10),
                new WkBtn { label = "1 · " + p1.ToString("#,0"), on = p.prog.crowns >= p1, col = Go, act = () => MysticBuy(1) },
                new WkBtn { label = "10 · " + p10.ToString("#,0"), on = p.prog.crowns >= p10, col = Go, act = () => MysticBuy(10) }));
        }
        foreach (var o in AHStore.Items)
        {
            var r = o as Dictionary<string, object>; if (r == null) continue;
            string id = AHJson.S(r, "id"); if (string.IsNullOrEmpty(id) || !AHStore.Ready(id)) continue;
            long price = (long)AHJson.N(r, "price"); string tag = AHJson.S(r, "tag");
            bool mount = AHStore.IsMount(id), own = AHStore.Owned(p, id), afford = p.prog.crowns >= price;
            string name = AHStore.Name(id) + (string.IsNullOrEmpty(tag) || own ? "" : "  <color=#9be37a>" + tag + "</color>") + (own ? "  <color=#c8b89a>yours</color>" : "");
            string l2 = (mount ? "Mount" : "Wings") + " · looks only · " + (own ? "owned" : Bad(AHStore.Amount(price), afford));
            string l3 = mount ? AHJson.S(AHJson.O(AHComp.Mounts, AHStore.MountKey(id)), "blurb") : "Painted feathered wings that beat as you run and fly.";
            Color tc = mount ? new Color(1f, 0.62f, 0.24f) : new Color(0.85f, 0.55f, 1f);
            var itm = mount ? null : AHItems.Get(id);
            // try, then buy (or use, once it is yours)
            WkBtn tryB, mainB;
            if (mount)
            {
                string k = AHStore.MountKey(id);
                tryB = own ? null : new WkBtn { label = "Try", on = true, col = Plain, act = () => { EndStorePreview(); ShowWork(false); AHComp.Try(g, k); } };
                mainB = own ? new WkBtn { label = p.mounted && p.mountSel == k ? "Riding" : "Ride", on = !(p.mounted && p.mountSel == k), col = Go, act = () => { EndStorePreview(); ShowWork(false); if (p.mounted) AHComp.Dismount(g, true); p.mountSel = k; RefreshRide(); AHComp.Mount(g); g.MarkDirty(); } }
                            : new WkBtn { label = "Buy", on = afford, col = Go, act = () => StoreBuy(id, price) };
            }
            else
            {
                bool trying = AHWardrobe.Preview == id, wearing = AHWardrobe.Cos(p, "cape") == id;
                tryB = own ? null : new WkBtn { label = trying ? "Take off" : "Try on", on = true, col = Plain, act = () => { AHWardrobe.Preview = trying ? null : id; p.CheckOutfit(); if (!trying) Toast("Showing them on you. They come off when you close the store."); RenderWork(); } };
                mainB = own ? new WkBtn { label = wearing ? "Wearing" : "Wear", on = !wearing, col = Go, act = () => { EndStorePreview(); AHWardrobe.SetCos(p, "cape", id); p.CheckOutfit(); g.SaveProgress(); RenderWork(); } }
                            : new WkBtn { label = "Buy", on = afford, col = Go, act = () => StoreBuy(id, price) };
            }
            rows.Add(s => RowI(itm, s, name, tc, l2, l3, tryB, mainB));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    List<string> mysticLines = new List<string>();
    void MysticBuy(int n)
    {
        EndStorePreview();
        var l = AHEggs.BuyMystic(g, n); if (l.Count == 0) return;
        mysticLines = l; wkMode = "mystic"; wkPageI = 0; RenderWork();
    }
    void RenderMystic(AHPlayer p)
    {
        wkTitle.text = "Your Mystic egg" + (mysticLines.Count > 1 ? "s" : "");
        wkHint.text = "The new pets wait on the Companions page. A pet you had already gained a star (Essence at five stars).";
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, "Back to the store", Color.white, "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "store"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Companions", on = true, col = Go, act = () => { wkMode = "comp"; wkPageI = 0; RenderWork(); } }));
        foreach (var line in mysticLines) { string ln = line; int cut = ln.IndexOf(" — "); rows.Add(s => Row(s, cut > 0 ? ln.Substring(0, cut) : ln, ln.StartsWith("Gilded") ? new Color(1f, 0.78f, 0.3f) : new Color(1f, 0.62f, 0.24f), cut > 0 ? ln.Substring(cut + 3) : "", "")); }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void StoreBuy(string id, long price)
    {
        var p = g.player;
        if (p.prog.crowns < price) { Toast("Not enough " + AHStore.Currency + "."); return; }
        if (!AHStore.Buy(g, id, price)) { Toast("You can’t buy that right now."); return; }
        if (AHWardrobe.Preview == id) AHWardrobe.Preview = null;
        if (AHStore.IsMount(id)) { RefreshRide(); Banner(AHStore.Name(id), "New mount · tap RIDE"); }
        else { AHWardrobe.SetCos(p, "cape", id); p.CheckOutfit(); g.SaveProgress(); Banner(AHStore.Name(id), "Yours now · wearing them"); }
        AHSound.Play("level");
        RenderWork();
    }

    void RenderStorePacks(AHPlayer p)
    {
        string cur = AHStore.Currency;
        wkTitle.text = "Get " + cur + " · you have " + p.prog.crowns.ToString("#,0");
        wkHint.text = cur + " are spent in the store on mounts, wings and Mystic eggs. Buying them with real money comes soon.";
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, "Back to the store", Color.white, "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "store"; wkPageI = 0; RenderWork(); } }));
        if (AHStore.Test)
        {
            long grant = (long)AHJson.N(AHDB.File("store"), "TEST_GRANT", 1000);
            rows.Add(s => Row(s, "Test " + cur + " (testing only)", new Color(0.62f, 0.85f, 1f), "+" + grant.ToString("#,0") + " " + cur + " for free, to try the store", "This row goes away before the game is released.",
                new WkBtn { label = "Take " + grant.ToString("#,0"), on = true, col = Go, act = () => { p.prog.crowns += grant; g.SaveProgress(); Toast("+" + AHStore.Amount(grant)); RenderWork(); } }));
        }
        foreach (var o in AHStore.Packs)
        {
            var r = o as Dictionary<string, object>; if (r == null) continue;
            long n = (long)AHJson.N(r, "crowns"); string price = AHJson.S(r, "price"), bonus = AHJson.S(r, "bonus");
            rows.Add(s => Row(s, n.ToString("#,0") + " " + cur + (string.IsNullOrEmpty(bonus) ? "" : "  <color=#9be37a>" + bonus + " extra</color>"), new Color(1f, 0.85f, 0.4f), price, "Coming soon",
                new WkBtn { label = price, on = false, col = Go, act = () => { } }));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
