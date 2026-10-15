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
        if (AHGame.I != null && AHGame.I.player != null) k *= AHArtisan.SellBonus(AHGame.I.player);   // the workshop's shop counter
        return Math.Max(1, (long)Math.Floor(AHComp.Value(id) * AHDB.CU * k));
    }
}

public partial class AHUI
{
    string shopId; bool shopSell; float sellOldArm;
    RectTransform rideBtn, petsBtn; Text rideText;

    void BuildShopHud()
    {
        petsBtn = Img("PetsBtn", transform, circle, new Vector2(1, 1), new Vector2(-236, -55), new Vector2(76, 76), new Color(0.2f, 0.14f, 0.1f, 0.9f));
        Img("Rim", petsBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 76), new Color(0.85f, 0.6f, 0.25f, 1f));
        Center(Label(petsBtn, "T", "PETS", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(76, 30), Color.white));
        taps.Add(new TapBtn { rt = petsBtn, act = OpenComp });
        // the store's tag (moved under the Daily and event tags by the skin)
        var storeChip = Img("StoreChip", transform, white, new Vector2(1, 1), new Vector2(-150, -168), new Vector2(150, 34), new Color(0.32f, 0.22f, 0.06f, 0.92f));
        Center(Label(storeChip, "T", "STORE", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 30), new Color(1f, 0.85f, 0.4f)));
        taps.Add(new TapBtn { rt = storeChip, act = OpenStore });
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
        // gear repair: any shopkeeper mends everything you wear
        if (AHWear.WornCount(p) > 0)
        {
            long rc = AHWear.RepairCost(p); int lo = AHWear.LowestCondition(p);
            rows.Add(slot => Row(slot, "Repair all your gear", lo <= 30 ? new Color(1f, 0.55f, 0.45f) : new Color(0.75f, 0.9f, 1f), "Your most worn piece is at " + lo + "%" + (lo <= 0 ? " (broken: no stats)" : lo <= 30 ? " (worn: half stats)" : ""), "",
                new WkBtn { label = "Repair · " + AHItems.MoneyText(rc, 2), on = p.bag.money >= rc, col = Go, act = () => { p.bag.money -= rc; AHWear.RepairAll(g, p, false); RenderWork(); } }));
        }
        if (!shopSell)
        {
            var stock = AHJson.A(sh, "stock");
            if (stock != null) foreach (var o in stock)
                {
                    string id = (string)o; long pr = AHShops.PriceBuy(id);
                    string name, l2; Color c = Color.white; bool owned = false;
                    if (id.StartsWith("mount:")) { string k = id.Substring(6); var m = AHJson.O(AHComp.Mounts, k); name = AHComp.MountName(k); c = new Color(1f, 0.62f, 0.24f); owned = p.mounts.Contains(k); l2 = AHJson.S(m, "blurb") + " +" + Mathf.RoundToInt(((float)AHJson.N(m, "speed", 1.5) - 1f) * 100f) + "% speed"; }
                    else if (id.StartsWith("pet:") && AHComp.Retired.Contains(id.Substring(4))) continue;   // (put away for now)
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
            // old gear in one go: pieces no better than what you wear (sets, legendaries and masterworks are kept)
            var old = AHExtras.OldGear(p);
            if (old.Count > 0)
            {
                long tot = 0; foreach (var id in old) tot += AHShops.PriceSell(shopId, id) * p.bag.Count(id);
                rows.Insert(1, slot => Row(slot, "Sell old gear (" + old.Count + " pieces)", new Color(1f, 0.85f, 0.5f), "Gear no better than what you wear · " + AHItems.MoneyText(tot), "Set pieces, legendaries and masterworks are kept.",
                    new WkBtn { label = sellOldArm > Time.unscaledTime ? "Tap to confirm" : "Sell all", on = true, col = Go, act = () => {
                        if (sellOldArm < Time.unscaledTime) { sellOldArm = Time.unscaledTime + 3f; RenderWork(); return; }
                        sellOldArm = 0f; long got = 0; foreach (var id in AHExtras.OldGear(p)) { int n = p.bag.Count(id); got += AHShops.PriceSell(shopId, id) * n; p.bag.Take(id, n); }
                        p.bag.money += got; p.bag.Touch(); Toast("Sold your old gear for " + AHItems.MoneyText(got) + "."); g.MarkDirty(); RenderWork(); } }));
            }
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

    // ---------- a pet's skills, in numbers (from the companions page) ----------
    string petInfo;
    // TESTING: every mount in the game, each with a Ride button (AHComp.TestMounts)
    void RenderMountTest(AHPlayer p)
    {
        wkTitle.text = "All mounts (testing)";
        wkHint.text = "Tap Ride to try any mount. Nothing is bought or learned, and getting off gives you back your own mount. You can’t mount up in a fight, in a dungeon or in the dark.";
        var rows = new List<Action<int>>();
        rows.Add(slot => Row(slot, "Back to companions", Color.white, "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "comp"; wkPageI = 0; RenderWork(); } }));
        var all = AHComp.Mounts;
        if (all != null)
            foreach (var kv in all)
            {
                string k = kv.Key; var m = AHJson.O(all, k);
                bool riding = p.mounted && p.mountSel == k, own = p.mounts.Contains(k);
                float sp = (float)AHJson.N(m, "speed", 1.5);
                rows.Add(slot => Row(slot, AHComp.MountName(k) + (riding ? "  <color=#9be37a>riding</color>" : own ? "  <color=#c8b89a>yours</color>" : ""), new Color(1f, 0.62f, 0.24f),
                    "+" + Mathf.RoundToInt((sp - 1f) * 100f) + "% speed · " + k, AHJson.S(m, "blurb"),
                    riding ? new WkBtn { label = "Get off", on = true, col = Plain, act = () => { AHComp.Dismount(g, false); RenderWork(); } }
                           : new WkBtn { label = "Ride", on = true, col = Go, act = () => { ShowWork(false); AHComp.Try(g, k); } }));
            }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    // TESTING: every pet in the game, each with a Call button (AHComp.TestMounts)
    void RenderPetTest(AHPlayer p)
    {
        wkTitle.text = "All pets (testing)";
        wkHint.text = "Tap Call to bring out any pet and watch it idle, walk, run and fly. Nothing is bought or learned.";
        var rows = new List<Action<int>>();
        rows.Add(slot => Row(slot, "Back to companions", Color.white, "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "comp"; wkPageI = 0; RenderWork(); } }));
        var all = AHComp.Pets;
        if (all != null)
            foreach (var kv in all)
            {
                string k = kv.Key; bool outNow = p.pet == k, own = p.pets.Contains(k);
                rows.Add(slot => Row(slot, AHComp.PetName(k) + (outNow ? "  <color=#9be37a>out</color>" : own ? "  <color=#c8b89a>yours</color>" : ""), new Color(1f, 0.62f, 0.24f), k, AHJson.S(AHJson.O(all, k), "blurb"),
                    new WkBtn { label = outNow ? "Put away" : "Call", on = true, col = outNow ? Plain : Go, act = () => { p.pet = outNow ? null : k; AHComp.SpawnPet(g); RenderWork(); } }));
            }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void RenderPetInfo(AHPlayer p)
    {
        string k = petInfo; if (k == null) { wkMode = "comp"; RenderComp(p); return; }
        long done, need; int L; AHComp.Bar(p, k, false, out done, out need, out L);
        var kind = AHJson.A(AHDB.Table("companions", "PET_KIND"), k);
        int stage = AHComp.EvoStage(p, k);
        wkTitle.text = AHComp.PetName(k) + " · Lv " + L;
        wkHint.text = (AHEggs.StatsOn ? "A pet helps with skills learnt as it levels, its own knack, and its stats (more with every star and level; only the pet that is out counts). " : "A pet helps with skills learnt as it levels and its own knack. ") + (kind != null && kind.Count > 1 ? "This one’s gift: " + kind[1] + ". " : "") + "At level 20 every skill is 50% stronger.";
        var rows = new List<Action<int>>();
        rows.Add(slot => Row(slot, AHComp.PetName(k), new Color(1f, 0.62f, 0.24f),
            "Level " + L + " · " + (need > 0 ? done.ToString("#,0") + " / " + need.ToString("#,0") + " XP to the next" : "max level") + (stage > 0 ? " · " + AHComp.EvoName[stage - 1] : ""),
            AHJson.S(AHJson.O(AHComp.Pets, k), "blurb"),
            new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "comp"; wkPageI = 0; RenderWork(); } }));
        string rar = AHEggs.Gilded(p, k) ? "legendary" : AHEggs.Rarity(k);
        if (AHEggs.StatsOn) rows.Add(slot => Row(slot, AHEggs.StarsText(AHEggs.Stars(p, k)) + "  " + AHEggs.RarityTag(rar) + (AHEggs.Gilded(p, k) ? " · Gilded" : ""), Color.white,
            AHEggs.StatsLine(p, k), AHEggs.StatsHelp(p, k)));
        else rows.Add(slot => Row(slot, AHEggs.RarityTag(rar) + (AHEggs.Gilded(p, k) ? " · Gilded" : "") + (AHEggs.StoreOnly(k) ? " · from the Mystic egg" : ""), Color.white, "A companion: it doesn't change your stats", ""));
        rows.Add(slot => Row(slot, "Knack: " + AHPetSkills.SkillName(k), new Color(0.62f, 0.85f, 1f), "Used by itself in a fight when it is ready", AHEggs.StatsOn ? "More stars come from hatching this pet again; at five, Essence instead." : ""));
        foreach (var d in AHComp.PetDetails(p, k))
        {
            var dd = d; bool got = dd[2] == "1";
            rows.Add(slot => Row(slot, (got ? "" : "<color=#7a7068>") + dd[0] + (got ? "" : "</color>"), got ? new Color(0.62f, 0.9f, 0.55f) : new Color(1f, 1f, 1f, 0.6f),
                got ? dd[3] : "Learnt at level " + dd[1] + ": " + dd[3], got ? "Learnt at level " + dd[1] : ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
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
        if (AHComp.TestMounts)
            rows.Add(slot => Row(slot, "All mounts (testing)", new Color(0.62f, 0.85f, 1f), "Ride any mount in the game to try it", "Nothing is bought or learned: getting off gives you back your own mount.",
                new WkBtn { label = "Open", on = true, col = Go, act = () => { wkMode = "mounttest"; wkPageI = 0; RenderWork(); } }));
        if (AHComp.TestMounts)
            rows.Add(slot => Row(slot, "All pets (testing)", new Color(0.62f, 0.85f, 1f), "Call any pet in the game to see it move", "Nothing is bought or learned: a pet you don't own goes away when you log out.",
                new WkBtn { label = "Open", on = true, col = Go, act = () => { wkMode = "pettest"; wkPageI = 0; RenderWork(); } }));
        // the egg satchel: eggs found in the world, growing as you work
        AHEggs.EnsureStars(p);
        var es = AHEggs.S(p);
        if (AHComp.TestMounts)
            rows.Add(slot => Row(slot, "Test egg (testing)", new Color(0.62f, 0.85f, 1f), "Puts this land's egg in your satchel, to try hatching", "This row goes away before release.",
                new WkBtn { label = "Egg", on = es.eggs.Count < AHEggs.Satchel, col = Go, act = () => { AHEggs.Add(g, AHEggs.LandEgg(AHGame.AreaId)); RenderWork(); } }));
        rows.Add(slot => Row(slot, "Egg satchel · " + es.eggs.Count + " / " + AHEggs.Satchel + (es.essence > 0 ? "  <color=#c07aff>" + es.essence.ToString("#,0") + " Essence</color>" : ""), new Color(1f, 0.85f, 0.4f),
            "Beasts and gathering drop the egg of the land you are in; beasts slain after dark, a Void egg.", "Each egg grows from the work that matches it. A grown egg hatches a pet: a new one, or Essence for one you have."));
        int awayN = AHExpeditions.L(p).Count, homeN = AHExpeditions.L(p).FindAll(x => AHExpeditions.Back(x)).Count;
        rows.Add(slot => Row(slot, "Pet expeditions" + (homeN > 0 ? "  <color=#9be37a>" + homeN + " back home</color>" : ""), new Color(1f, 0.85f, 0.4f),
            "Send a pet on a trip for materials, pet XP and rare finds · " + awayN + " / " + AHExpeditions.Slots(p) + " away", "",
            new WkBtn { label = "Open", on = true, col = homeN > 0 ? Go : Plain, act = OpenExpeditions }));
        for (int ei = 0; ei < es.eggs.Count; ei++)
        {
            int ii = ei; var e = es.eggs[ei]; bool ready = AHEggs.Ready(e); long wc = AHEggs.WarmCost(e);
            rows.Add(slot => Row(slot, "<color=" + AHEggs.EggHex(e.k) + ">" + AHEggs.EggName(e.k) + "</color>" + (ready ? "  <color=#9be37a>ready to hatch</color>" : ""), Color.white,
                ready ? "Tap Hatch to see what comes out" : e.v.ToString("#,0") + " / " + AHEggs.Need(e.k).ToString("#,0") + " warmth · grows from " + AHEggs.Grows(e.k), "",
                ready ? new WkBtn { label = "Hatch", on = true, col = Go, act = () => { AHEggs.Hatch(g, ii); RenderWork(); } }
                      : new WkBtn { label = "Warm " + wc + " " + AHStore.Currency, on = p.prog.crowns >= wc, col = Plain, act = () => { AHEggs.WarmWithCrowns(g, ii); RenderWork(); } },
                !ready && es.essence > 0 ? new WkBtn { label = "Essence", on = true, col = Plain, act = () => { AHEggs.WarmWithEssence(g, ii); RenderWork(); } } : null,
                new WkBtn { label = "Leave", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { AHEggs.Discard(g, ii); RenderWork(); } }));
        }
        var skills = AHDB.List("companions", "PET_SKILLS");
        foreach (var k in p.pets)
            if (!AHComp.Retired.Contains(k))
        {
            string kk = k; long done, need; int L; AHComp.Bar(p, k, false, out done, out need, out L);
            var sk = new List<string>();
            if (skills != null) foreach (var o in skills) { var r = o as List<object>; int l = (int)(double)r[0]; sk.Add(L >= l ? (string)r[1] : "<color=#7a7068>" + r[1] + "</color>"); }
            var kind = AHJson.A(AHDB.Table("companions", "PET_KIND"), k);
            bool outNow = p.pet == k;
            rows.Add(slot => Row(slot, (AHEggs.Gilded(p, kk) ? "<color=#ffc040>Gilded</color> " : "") + AHComp.PetName(kk) + " · Lv " + L + (AHEggs.StatsOn ? "  " + AHEggs.StarsText(AHEggs.Stars(p, kk)) : "") + (outNow ? "  <color=#9be37a>out</color>" : ""), new Color(1f, 0.62f, 0.24f),
                (need > 0 ? done.ToString("#,0") + " / " + need.ToString("#,0") + " XP" : "Max level") + (AHEggs.StatsOn ? " · " + AHEggs.StatsLine(p, kk) : (AHEggs.StoreOnly(kk) ? " · store pet" : "")), string.Join(" · ", sk.ToArray()),
                new WkBtn { label = "Skills", on = true, col = Plain, act = () => { petInfo = kk; wkMode = "petinfo"; wkPageI = 0; RenderWork(); } },
                outNow ? new WkBtn { label = "Feed", on = true, col = Plain, act = () => { AHComp.Treat(g); RenderWork(); } } : null,
                AHExpeditions.Away(p, kk) != null ? new WkBtn { label = "Away", on = false, col = Plain, act = () => { } }
                    : new WkBtn { label = outNow ? "Put away" : "Call", on = true, col = outNow ? Plain : Go, act = () => { p.pet = outNow ? null : kk; AHComp.SpawnPet(g); g.MarkDirty(); RenderWork(); } }));
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
