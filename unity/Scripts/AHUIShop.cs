// Ashen Hollow: shops, the bank vault and the companions window, as in the web game (v67):
//  - Shops (economy.json SHOPS, world.json DOORS): walk up to a shop's door and press the big button. Buy at
//    1.5 × the item's value (items.json VALUE); sell your bag for 40% (22% for crafted gear; a shop pays 85% back
//    on its own goods, and more for the trade goods it wants). Taverns rent beds and hire out sellswords,
//    stables sell mounts, menageries sell pets.
//  - The bank vault (world.json BANKS): deposit and withdraw whole stacks; it never fills up.
//  - Companions (PETS button): your pets (call, put away, feed for XP), mounts (choose, ride) and party.
//  - RIDE button (or R): get on your chosen mount.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHShops
{
    public static Dictionary<string, object> Shops { get { return AHDB.Table("economy", "SHOPS"); } }
    public const float Markup = 1.5f, Resale = 0.85f;

    // shop doors and bank vaults in this area
    public static void Setup(AHGame g)
    {
        float S = AHDB.S;
        var doors = AHDB.List("world", "DOORS");
        if (doors != null)
            foreach (var d in doors)
            {
                string id = AHJson.S(d, "shop"); var sh = AHJson.O(Shops, id); if (sh == null) continue;
                Vector3 p = g.W((float)AHJson.N(d, "x") * S, (float)AHJson.N(d, "y") * S);
                if (!g.InArea(p)) continue;
                string sid = id;
                AHGather.Spots.Add(new AHSpot { kind = "use", type = "shop", name = "Enter " + AHJson.S(sh, "name", id), pos = p, r = 0.5f, reach = 70f * S, use = () => g.ui.OpenShop(sid) });
            }
        var banks = AHDB.List("world", "BANKS");
        if (banks != null)
            foreach (var b in banks)
            {
                Vector3 p = g.W((float)AHJson.N(b, "x") * S, (float)AHJson.N(b, "y") * S);
                if (!g.InArea(p)) continue;
                AHGather.Spots.Add(new AHSpot { kind = "use", type = "bank", name = "Bank vault", pos = p, r = 0.5f, reach = 80f * S, use = () => g.ui.OpenBank() });
                AHScenery.Bank(g, p);
            }
    }

    static bool Sold(string id)
    {
        foreach (var kv in Shops) { var st = AHJson.A(kv.Value, "stock"); if (st != null && st.Contains(id)) return true; }
        return false;
    }

    // web priceBuy (no town reputation discount yet)
    public static long PriceBuy(string id)
    {
        double v;
        if (id.StartsWith("mount:")) v = AHJson.N(AHJson.O(AHComp.Mounts, id.Substring(6)), "price", 1000);
        else if (id.StartsWith("merc:")) v = AHJson.N(AHJson.O(AHComp.Mercs, id.Substring(5)), "price", 1000);
        else if (id.StartsWith("pet:")) v = AHJson.N(AHJson.O(AHComp.Pets, id.Substring(4)), "price", 500);
        else if (id == "svc:rest") v = 45;
        else v = AHComp.Value(id) * Markup;
        v *= 1f - AHRep.ShopDiscount();
        return (long)Math.Round(v * AHDB.CU);
    }

    // web priceSell / sellK
    public static long PriceSell(string shop, string id)
    {
        var it = AHItems.Get(id);
        float k = Sold(id) ? Markup * Resale : it != null && it.rarity == "crafted" ? 0.22f : 0.4f;
        var prem = AHJson.A(AHJson.O(Shops, shop), "premium");
        if (prem != null && prem.Contains(id)) k = Mathf.Max(k, 0.8f);
        return Math.Max(1, (long)Math.Floor(AHComp.Value(id) * AHDB.CU * k));
    }
}

public partial class AHUI
{
    string shopId; bool shopSell;
    RectTransform rideBtn, petsBtn; Text rideText;

    void BuildShopHud()
    {
        petsBtn = Img("PetsBtn", transform, circle, new Vector2(1, 1), new Vector2(-236, -55), new Vector2(76, 76), new Color(0.2f, 0.14f, 0.1f, 0.9f));
        Img("Rim", petsBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 76), new Color(0.85f, 0.6f, 0.25f, 1f));
        Center(Label(petsBtn, "T", "PETS", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(76, 30), Color.white));
        taps.Add(new TapBtn { rt = petsBtn, act = OpenComp });
        rideBtn = Img("RideBtn", transform, circle, new Vector2(1, 0), new Vector2(-58, 322), new Vector2(66, 66), new Color(0.45f, 0.32f, 0.16f, 0.92f));
        Img("Rim", rideBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66, 66), new Color(1f, 0.8f, 0.45f, 1f));
        rideText = Center(Label(rideBtn, "T", "RIDE", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(66, 26), Color.white));
        taps.Add(new TapBtn { rt = rideBtn, act = () => AHComp.ToggleRide(g) });
        RefreshRide();
    }

    public void RefreshRide()
    {
        if (rideBtn == null || g.player == null) return;
        rideBtn.gameObject.SetActive(g.player.mountSel != null);
        rideText.text = g.player.mounted ? "WALK" : "RIDE";
    }

    void ShopKeys()
    {
        if (Modal == 0 && AHInput.RideKey()) AHComp.ToggleRide(g);
        if (Modal == 0 && AHInput.MapKey() && Time.frameCount != workShutFrame) OpenMap();
    }

    // ---------- shops (web openShop / renderShop / buy / sell) ----------
    public void OpenShop(string id)
    {
        if (g.player.mounted) AHComp.Dismount(g, true);
        wkMode = "shop"; shopId = id; shopSell = false; wkPageI = 0;
        var sh = AHJson.O(AHShops.Shops, id);
        Banner(AHJson.S(sh, "name", id), AHJson.S(sh, "keeper") + " · " + AHJson.S(sh, "role"));
        ShowWork(true);
        RenderWork();
    }

    void RenderShop(AHPlayer p)
    {
        var sh = AHJson.O(AHShops.Shops, shopId);
        wkTitle.text = AHJson.S(sh, "name", shopId);
        wkHint.text = AHJson.S(sh, "keeper") + " · “" + AHJson.S(sh, "greet") + "”\nYour purse: " + AHItems.MoneyText(p.bag.money) + (shopSell ? " · equipped gear is safe, only your bag is listed" : "");
        var rows = new List<Action<int>>();
        rows.Add(slot => Row(slot, shopSell ? "Selling" : "Buying", new Color(1f, 0.8f, 0.45f), shopSell ? "Tap Sell to sell one, Sell all for the stack." : "Tap a price to buy.", "",
            new WkBtn { label = "Buy", on = shopSell, col = Plain, act = () => { shopSell = false; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Sell", on = !shopSell, col = Plain, act = () => { shopSell = true; wkPageI = 0; RenderWork(); } }));
        if (!shopSell)
        {
            var stock = AHJson.A(sh, "stock");
            if (stock != null) foreach (var o in stock)
                {
                    string id = (string)o; long pr = AHShops.PriceBuy(id);
                    string name, l2; Color c = Color.white; bool owned = false;
                    if (id.StartsWith("mount:")) { string k = id.Substring(6); var m = AHJson.O(AHComp.Mounts, k); name = AHComp.MountName(k); c = new Color(1f, 0.62f, 0.24f); owned = p.mounts.Contains(k); l2 = AHJson.S(m, "blurb") + " +" + Mathf.RoundToInt(((float)AHJson.N(m, "speed", 1.5) - 1f) * 100f) + "% speed"; }
                    else if (id.StartsWith("pet:")) { string k = id.Substring(4); name = AHComp.PetName(k); c = new Color(1f, 0.62f, 0.24f); owned = p.pets.Contains(k); l2 = AHJson.S(AHJson.O(AHComp.Pets, k), "blurb"); }
                    else if (id.StartsWith("merc:")) { string k = id.Substring(5); var m = AHJson.O(AHComp.Mercs, k); name = AHComp.MercName(k); c = new Color(0.62f, 0.85f, 1f); owned = p.party.Contains(k); l2 = AHJson.S(m, "role") + ". " + AHJson.S(m, "blurb"); }
                    else if (id == "svc:rest") { name = "Rest at the inn"; l2 = "A warm bed: full HP and hunger"; }
                    else { var it = AHItems.Get(id); if (it == null) continue; name = it.name; c = AHItems.Quality(it); owned = it.cosmetic && AHWardrobe.Has(p, id); l2 = it.slot != null ? AHItems.StatLine(it) + " · " + Bad(AHItems.WhoUses(id), AHItems.CanUse(id, p.cls.id)) : (it.note ?? ""); }
                    string iid = id; var rit = id.Contains(":") ? null : AHItems.Get(id);
                    rows.Add(slot => { rowItem = rit; Row(slot, name + (owned ? "  <color=#9be37a>owned</color>" : ""), c, l2, "",
                        id.StartsWith("merc:") && owned
                            ? new WkBtn { label = "Dismiss", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { p.party.Remove(iid.Substring(5)); AHComp.SpawnAllies(g); Toast(AHComp.MercName(iid.Substring(5)) + " leaves your party."); g.MarkDirty(); RenderWork(); } }
                            : new WkBtn { label = owned ? "Owned" : (iid.StartsWith("merc:") ? "Hire · " : "") + AHItems.MoneyText(pr, 2), on = !owned && p.bag.money >= pr, col = Go, act = () => Buy(p, iid, pr) }); });
                }
        }
        else
        {
            foreach (var id in p.bag.order)
            {
                int n = p.bag.Count(id); var it = AHItems.Get(id); if (n <= 0 || it == null) continue;
                long pr = AHShops.PriceSell(shopId, id); string iid = id;
                var prem = AHJson.A(sh, "premium"); bool extra = prem != null && prem.Contains(id);
                rows.Add(slot => { rowItem = it; Row(slot, it.name + " ×" + n, AHItems.Quality(it), AHItems.MoneyText(pr, 2) + " each" + (extra ? " · <color=#ffd23a>this shop pays extra</color>" : ""), "",
                    new WkBtn { label = "Sell 1", on = true, col = Go, act = () => Sell(p, iid, false) },
                    n > 1 ? new WkBtn { label = "Sell all", on = true, col = Plain, act = () => Sell(p, iid, true) } : null); });
            }
            if (rows.Count == 1) rows.Add(slot => Row(slot, "Your bag is empty", new Color(1f, 1f, 1f, 0.6f), "", ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void Buy(AHPlayer p, string id, long pr)
    {
        if (p.bag.money < pr) { Toast("Not enough money. You need " + AHItems.MoneyText(pr - p.bag.money) + " more."); return; }
        if (id.StartsWith("mount:"))
        {
            string k = id.Substring(6); if (p.mounts.Contains(k)) return;
            p.bag.money -= pr; p.mounts.Add(k); p.mountSel = k; Banner(AHComp.MountName(k), "New mount · tap RIDE"); RefreshRide();
        }
        else if (id.StartsWith("merc:"))
        {
            string k = id.Substring(5); if (p.party.Contains(k)) return;
            if (p.party.Count >= AHComp.PartyMax) { Toast("Your party is full (" + AHComp.PartyMax + "). Dismiss someone first."); return; }
            p.bag.money -= pr; p.party.Add(k); AHComp.SpawnAllies(g);
            Banner(AHComp.MercName(k), AHJson.S(AHJson.O(AHComp.Mercs, k), "role") + " joined your party");
        }
        else if (id == "svc:rest") { p.bag.money -= pr; p.hp = p.maxHp; p.hunger = 100f; Toast("You sleep soundly at the inn. Fully restored."); }
        else if (id.StartsWith("pet:"))
        {
            string k = id.Substring(4); if (p.pets.Contains(k)) return;
            p.bag.money -= pr; p.pets.Add(k); p.pet = k; AHComp.SpawnPet(g);
            Banner(AHComp.PetName(k), "New companion"); Toast("Your new pet follows you. Change pets with the PETS button.", 3f);
        }
        else
        {
            var cdef = AHItems.Get(id); if (cdef != null && cdef.cosmetic && AHWardrobe.Has(p, id)) return;
            if (!p.bag.Add(id)) { Toast(p.bag.lastWarn ?? "Your bag is full."); return; }
            p.bag.money -= pr; p.NoteBought(id, 1);
            Toast("Bought " + AHItems.Get(id).name + " for " + AHItems.MoneyText(pr) + ".");
        }
        p.bag.Touch(); g.MarkDirty(); RenderWork();
        g.quests.Event("buy", "any", 1, g);
    }

    void Sell(AHPlayer p, string id, bool all)
    {
        int n = all ? p.bag.Count(id) : 1; if (n <= 0) return;
        long gold = AHShops.PriceSell(shopId, id) * n;
        p.bag.Take(id, n); p.bag.money += gold; p.bag.Touch();
        Toast("Sold " + (n > 1 ? n + "× " : "") + AHItems.Get(id).name + " for " + AHItems.MoneyText(gold) + ".");
        g.MarkDirty(); RenderWork();
    }

    // ---------- the bank vault (web renderBank / bankMove) ----------
    public void OpenBank()
    {
        if (g.player.mounted) AHComp.Dismount(g, true);
        wkMode = "bank"; wkPageI = 0;
        ShowWork(true);
        RenderWork();
    }

    void RenderBank(AHPlayer p)
    {
        wkTitle.text = "Bank vault";
        int vaultN = 0; foreach (var kv in p.bank) if (kv.Value > 0) vaultN++;
        wkHint.text = "Your bag: " + p.bag.UsedSlots + " / " + p.bag.SlotsMax + " slots · vault: " + vaultN + " kinds of things, it never fills up. Deposit or withdraw whole stacks.";
        var rows = new List<Action<int>>();
        rows.Add(slot => Row(slot, "Deposit all materials", new Color(1f, 0.8f, 0.45f), "Everything in your bag that isn’t gear, food or a potion", "",
            new WkBtn { label = "Deposit all", on = true, col = Go, act = () => { foreach (var id in new List<string>(p.bag.order)) { var it = AHItems.Get(id); if (it != null && it.slot == null && !it.IsFood && !it.potion) BankMove(p, id, true); } RenderWork(); } }));
        foreach (var kv in new List<KeyValuePair<string, int>>(p.bank))
        {
            if (kv.Value <= 0) continue; var it = AHItems.Get(kv.Key); if (it == null) continue; string id = kv.Key; int n = kv.Value;
            rows.Add(slot => { rowItem = it; Row(slot, it.name + " ×" + n, AHItems.Quality(it), "In the vault", "", new WkBtn { label = "Withdraw", on = true, col = Plain, act = () => { BankMove(p, id, false); RenderWork(); } }); });
        }
        foreach (var id in new List<string>(p.bag.order))
        {
            int n = p.bag.Count(id); var it = AHItems.Get(id); if (n <= 0 || it == null) continue; string iid = id;
            rows.Add(slot => { rowItem = it; Row(slot, it.name + " ×" + n, AHItems.Quality(it), "In your bag", "", new WkBtn { label = "Deposit", on = true, col = Go, act = () => { BankMove(p, iid, true); RenderWork(); } }); });
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void BankMove(AHPlayer p, string id, bool toBank)
    {
        if (toBank)
        {
            int n = p.bag.Count(id); if (n <= 0) return;
            p.bag.Take(id, n); int have; p.bank.TryGetValue(id, out have); p.bank[id] = have + n;
        }
        else
        {
            int n; if (!p.bank.TryGetValue(id, out n) || n <= 0) return;
            if (p.bag.Count(id) == 0 && p.bag.UsedSlots >= p.bag.SlotsMax) { Toast("Your bag is full."); return; }
            p.bank.Remove(id); p.bag.Add(id, n);
        }
        p.bag.Touch(); g.MarkDirty();
    }

    // ---------- companions (web compHtml): pets, mounts and your party ----------
    public void OpenComp()
    {
        wkMode = "comp"; wkPageI = 0;
        ShowWork(true);
        RenderWork();
    }

    void RenderComp(AHPlayer p)
    {
        wkTitle.text = "Companions";
        wkHint.text = "Pets level up when you win fights with them out, or when you feed them. Mounts level up as you ride. Buy pets at a menagerie, mounts at a stable, and hire sellswords at a tavern.";
        var rows = new List<Action<int>>();
        var skills = AHDB.List("companions", "PET_SKILLS");
        foreach (var k in p.pets)
        {
            string kk = k; long done, need; int L; AHComp.Bar(p, k, false, out done, out need, out L);
            var sk = new List<string>();
            if (skills != null) foreach (var o in skills) { var r = o as List<object>; int l = (int)(double)r[0]; sk.Add(L >= l ? (string)r[1] : "<color=#7a7068>" + r[1] + "</color>"); }
            var kind = AHJson.A(AHDB.Table("companions", "PET_KIND"), k);
            bool outNow = p.pet == k;
            rows.Add(slot => Row(slot, AHComp.PetName(kk) + " · Lv " + L + (outNow ? "  <color=#9be37a>out</color>" : ""), new Color(1f, 0.62f, 0.24f),
                (need > 0 ? done.ToString("#,0") + " / " + need.ToString("#,0") + " XP" : "Max level") + (kind != null && kind.Count > 1 ? " · " + kind[1] : ""), string.Join(" · ", sk.ToArray()),
                outNow ? new WkBtn { label = "Feed", on = true, col = Plain, act = () => { AHComp.Treat(g); RenderWork(); } } : null,
                new WkBtn { label = outNow ? "Put away" : "Call", on = true, col = outNow ? Plain : Go, act = () => { p.pet = outNow ? null : kk; AHComp.SpawnPet(g); g.MarkDirty(); RenderWork(); } }));
        }
        foreach (var k in p.mounts)
        {
            string kk = k; long done, need; int L; AHComp.Bar(p, k, true, out done, out need, out L);
            bool sel = p.mountSel == k;
            float sp = (float)AHJson.N(AHJson.O(AHComp.Mounts, k), "speed", 1.5) * AHComp.MountBonus(p, k);
            rows.Add(slot => Row(slot, AHComp.MountName(kk) + " · Lv " + L + (sel ? "  <color=#9be37a>chosen</color>" : ""), new Color(1f, 0.62f, 0.24f),
                "+" + Mathf.RoundToInt((sp - 1f) * 100f) + "% speed · " + (need > 0 ? done.ToString("#,0") + " / " + need.ToString("#,0") + " XP" : "Max level"), AHJson.S(AHJson.O(AHComp.Mounts, kk), "blurb"),
                sel ? new WkBtn { label = p.mounted ? "Get off" : "Ride", on = true, col = Go, act = () => { ShowWork(false); AHComp.ToggleRide(g); } }
                    : new WkBtn { label = "Choose", on = true, col = Plain, act = () => { if (p.mounted) AHComp.Dismount(g, true); p.mountSel = kk; RefreshRide(); g.MarkDirty(); RenderWork(); } }));
        }
        foreach (var k in p.party)
        {
            string kk = k; AHAlly a = AHComp.Allies.Find(x => x != null && x.id == k);
            var m = AHJson.O(AHComp.Mercs, k);
            rows.Add(slot => Row(slot, AHComp.MercName(kk), new Color(0.62f, 0.85f, 1f), AHJson.S(m, "role") + (a != null ? " · " + (a.dead ? "down" : Mathf.RoundToInt(a.hp) + " / " + Mathf.RoundToInt(a.maxHp) + " HP") : ""), AHJson.S(m, "blurb"),
                new WkBtn { label = "Dismiss", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { p.party.Remove(kk); AHComp.SpawnAllies(g); Toast(AHComp.MercName(kk) + " leaves your party."); g.MarkDirty(); RenderWork(); } }));
        }
        if (rows.Count == 0) rows.Add(slot => Row(slot, "No companions yet", new Color(1f, 1f, 1f, 0.7f), "The Menagerie in Sunspire and Morwen’s Curios in Mirewatch sell pets; Varrow Stables sell horses.", "Sellswords drink at the Salty Trout in Varrow."));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
