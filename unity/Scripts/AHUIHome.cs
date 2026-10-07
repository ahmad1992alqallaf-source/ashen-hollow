// Ashen Hollow: the homestead windows, as in the web game (renderFarm): the Land Agent (deed, seeds, farm supplies,
// travel home), each plot (build, plant, water, bone meal, compost, harvest; pens: collect, feed, buy, butcher),
// the house (upgrade, bed, storage chest, comforts) and the market stall.
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class AHUI
{
    string farmCtx, farmPlot; bool farmConfirm;
    static readonly KeyValuePair<string, int>[] FarmSupply = { new KeyValuePair<string, int>("animal_feed", 8), new KeyValuePair<string, int>("bone_meal", 15), new KeyValuePair<string, int>("compost", 15) };

    public void OpenFarmAgent() { if (g.player.mounted) AHComp.Dismount(g, true); wkMode = "farm"; farmCtx = "agent"; wkPageI = 0; ShowWork(true); RenderWork(); }
    public void OpenFarmPlot(string id) { wkMode = "farm"; farmCtx = "plot"; farmPlot = id; farmConfirm = false; wkPageI = 0; AHHome.Tick(g); ShowWork(true); RenderWork(); }

    void FarmChanged() { var p = g.player; p.bag.Touch(); g.MarkDirty(); if (AHHomeView.I != null) AHHomeView.I.Refresh(); RenderWork(); }

    void RenderFarm(AHPlayer p)
    {
        var H = p.home; var rows = new List<Action<int>>(); long CU = AHDB.CU;
        var HOME = AHHome.HOME;
        if (farmCtx == "agent")
        {
            wkTitle.text = "Land & farm supplies";
            wkHint.text = "You have " + AHItems.MoneyText(p.bag.money) + " · Farming " + p.Skill("farming") + ". Crops grow in real time, even while you are away.";
            long deed = (long)AHJson.N(HOME, "deed", 5000); int deedLv = (int)AHJson.N(HOME, "deedLv", 10);
            if (H != null) rows.Add(slot => Row(slot, "Your homestead", new Color(1f, 0.8f, 0.45f), "You own land. " + AHJson.S(AHHome.Tier(H.tier), "name") + " · Farming " + p.Skill("farming"), "",
                new WkBtn { label = "Travel there", on = true, col = Go, act = () => { ShowWork(false); AHHome.TravelHome(g); } }));
            else rows.Add(slot => Row(slot, "Homestead deed · " + AHItems.MoneyText(deed * CU), new Color(1f, 0.8f, 0.45f), "Your own land with a tent, two fields, room for a house, animal pens and a kitchen.", "",
                new WkBtn
                {
                    label = p.level < deedLv ? "Level " + deedLv + " needed" : "Buy deed", on = p.level >= deedLv && p.bag.money >= deed * CU, col = Go,
                    act = () => { p.bag.money -= deed * CU; p.home = new AHHomeState { tier = 1 }; p.home.built.Add("f1"); p.home.built.Add("f2"); p.home.built.Add("house"); Banner("You own land!", "Your Homestead"); Toast("The deed is yours. Tap Travel there to go home.", 3f); FarmChanged(); }
                }));
            var crops = AHDB.Table("home", "CROPS");
            if (crops != null) foreach (var c in crops.Values)
                {
                    string id = AHJson.S(c, "id") + "_seed"; long pr = (long)AHJson.N(c, "seed", 6); var it = AHItems.Get(id); if (it == null) continue;
                    rows.Add(slot => Row(slot, it.name + "  <color=#9a9080>Farming " + AHJson.N(c, "lvl", 1) + "</color>", Color.white, AHItems.MoneyText(pr * CU) + " each · ripe in " + AHJson.N(c, "min", 6) + " min · you have " + p.bag.Count(id), "",
                        new WkBtn { label = "Buy 1", on = p.bag.money >= pr * CU, col = Plain, act = () => BuyFarm(p, id, 1, pr) },
                        new WkBtn { label = "Buy 5", on = p.bag.money >= pr * 5 * CU, col = Go, act = () => BuyFarm(p, id, 5, pr) }));
                }
            foreach (var sp in FarmSupply)
            {
                string id = sp.Key; long pr = sp.Value; var it = AHItems.Get(id); if (it == null) continue;
                rows.Add(slot => Row(slot, it.name, Color.white, AHItems.MoneyText(pr * CU) + " each · " + (it.note ?? "") + " · you have " + p.bag.Count(id), "",
                    new WkBtn { label = "Buy 1", on = p.bag.money >= pr * CU, col = Plain, act = () => BuyFarm(p, id, 1, pr) },
                    new WkBtn { label = "Buy 5", on = p.bag.money >= pr * 5 * CU, col = Go, act = () => BuyFarm(p, id, 5, pr) }));
            }
        }
        else
        {
            var pl = AHHome.Plot(farmPlot); string kind = AHJson.S(pl, "kind"), pname = AHJson.S(pl, "name");
            wkTitle.text = kind == "house" ? "Your house" : pname;
            wkHint.text = "You have " + AHItems.MoneyText(p.bag.money) + " · Farming " + p.Skill("farming");
            if (H == null) rows.Add(slot => Row(slot, "No deed yet", new Color(1f, 1f, 1f, 0.7f), "Buy a homestead deed from a Land Agent (Varrow or Ashen Hollow) to build here.", ""));
            else if (kind == "house") HouseRows(p, rows);
            else if (!AHHome.Built(H, farmPlot)) BuildRows(p, pl, rows);
            else if (farmPlot == "stall") StallRows(p, rows);
            else if (kind == "field") FieldRows(p, pl, rows);
            else if (kind == "pen") PenRows(p, pl, rows);
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void BuyFarm(AHPlayer p, string id, int n, long pr)
    {
        if (p.bag.money < pr * n * AHDB.CU) return;
        if (!p.bag.Add(id, n)) { Toast(p.bag.lastWarn ?? "Your bag is full."); return; }
        p.bag.money -= pr * n * AHDB.CU; p.NoteBought(id, n);
        Toast("Bought " + n + " " + AHItems.Get(id).name.ToLowerInvariant() + ".");
        FarmChanged();
    }

    void BuildRows(AHPlayer p, object pl, List<Action<int>> rows)
    {
        var H = p.home; int tier = (int)AHJson.N(pl, "tier", 1); bool ok = H.tier >= tier; var cost = AHJson.O(pl, "cost");
        string kind = AHJson.S(pl, "kind"), id = AHJson.S(pl, "id");
        string what = kind == "field" ? "Plough a field for crops." : kind == "pen" ? "A home for " + AHJson.S(AHHome.Animal(AHJson.S(pl, "animal")), "name").ToLowerInvariant() + "s. " + AnimalBlurb(AHJson.S(pl, "animal")) : AHGather.StationHint(id);
        rows.Add(slot => Row(slot, "Build: " + AHJson.S(pl, "name"), new Color(1f, 0.8f, 0.45f), what, ok ? AHHome.CostText(p, cost) : "<color=#ff8a7a>Needs a " + AHJson.S(AHHome.Tier(tier), "name") + " first</color>",
            new WkBtn
            {
                label = "Build", on = ok && AHHome.CanPay(p, cost), col = Go,
                act = () => { if (!AHHome.Pay(p, cost)) return; H.built.Add(id); if (kind == "pen") AHHome.Pen(H, id); p.GainXp("farming", 60 + (int)(AHJson.N(cost, "money", 0) / 4), false); Banner(AHJson.S(pl, "name"), "Built"); FarmChanged(); }
            }));
    }

    void HouseRows(AHPlayer p, List<Action<int>> rows)
    {
        var H = p.home; var T0 = AHHome.Tier(H.tier); var nx = AHHome.Tier(H.tier + 1);
        rows.Add(slot => Row(slot, AHJson.S(T0, "name"), new Color(1f, 0.8f, 0.45f), AHJson.S(T0, "blurb"), "Pens hold up to " + AHHome.Cap(p) + " animals · Comfort " + AHHome.ComfortPts(p) + " · rested XP " + p.rested.ToString("#,0") + " / " + AHHome.RestCap(p).ToString("#,0")));
        AHTrophies.Backfill(p);
        int hl = AHTrophies.Level(p), nxt = AHTrophies.NextAt(p);
        rows.Add(slot => Row(slot, "Homestead level " + hl + " / 10", new Color(0.6f, 0.9f, 0.5f), "Renown " + AHTrophies.Renown(p) + (nxt > 0 ? " · next level at " + nxt : " · the finest homestead in the realm") + " (house, buildings, comforts and trophies)", AHTrophies.Perks(p)));
        var tl = p.prog.trophies;
        rows.Add(slot => Row(slot, "Trophies · " + tl.Count, Gold, tl.Count == 0 ? "Beat a boss, an elite or a rare beast to win its trophy." : "Each one adds renown." + (H.comf.Contains("trophies") ? " Your last eight stand on the Trophy stand." : " Build the Trophy stand to show them off."), ""));
        for (int ti = tl.Count - 1; ti >= 0; ti--) { var tt = tl[ti]; rows.Add(slot => Row(slot, "  " + AHTrophies.Name(tt), new Color(1f, 0.85f, 0.5f), "", "")); }
        if (nx != null)
        {
            var rq = AHJson.O(nx, "req"); int rc = (int)AHJson.N(rq, "cls", 0), rf = (int)AHJson.N(rq, "farming", 0);
            bool rqOk = p.level >= rc && p.Skill("farming") >= rf; var cost = AHJson.O(nx, "cost");
            rows.Add(slot => Row(slot, "Upgrade to " + AHJson.S(nx, "name"), Color.white, AHJson.S(nx, "blurb"), AHHome.CostText(p, cost) + (rc > 0 ? " · " + Bad("Level " + rc, p.level >= rc) + " · " + Bad("Farming " + rf, p.Skill("farming") >= rf) : ""),
                new WkBtn { label = "Build", on = rqOk && AHHome.CanPay(p, cost), col = Go, act = () => { if (!AHHome.Pay(p, cost)) return; H.tier++; p.GainXp("farming", H.tier == 2 ? 300 : 900, false); Banner(AHJson.S(nx, "name"), "Your new home"); FarmChanged(); } }));
        }
        rows.Add(slot => Row(slot, "Inside your house", new Color(1f, 0.8f, 0.45f), "Walk in, and furnish it: " + AHInterior.Pts(p) + " comfort from furniture so far.", "",
            new WkBtn { label = "Enter", on = true, col = Go, act = () => { ShowWork(false); AHInterior.Enter(g); } },
            new WkBtn { label = "Furnish", on = true, col = Plain, act = OpenFurnish }));
        if (H.tier >= 2)
        {
            long left = AHHome.SleepCd(p) - (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - H.sleepAt);
            rows.Add(slot => Row(slot, "Bed", Color.white, left > 0 ? "Rested again in " + Mins(left) + "." : "Sleep to fill your rested XP (double XP from kills) and restore HP.", "",
                new WkBtn { label = "Sleep", on = left <= 0, col = Go, act = () => { H.sleepAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); p.rested = AHHome.RestCap(p); p.hp = p.maxHp; p.hunger = Mathf.Max(p.hunger, 60f); Banner("Well rested", "Rested XP is full"); Fade(true); Invoke("FadeOff", 0.7f); FarmChanged(); } }));
            rows.Add(slot => Row(slot, "Storage chest", Color.white, "Opens your bank vault from home.", "", new WkBtn { label = "Open", on = true, col = Plain, act = () => OpenBank() }));
            rows.Add(slot => Row(slot, "Mirror", Color.white, "Restyle your hair and face for free. Body, name and gender changes cost the same as at a barber.", "", new WkBtn { label = "Look", on = true, col = Plain, act = () => OpenCreator("mirror") }));
        }
        var cs = AHHome.Comforts;
        if (cs != null) foreach (var c in cs)
            {
                string id = AHJson.S(c, "id"); bool have = H.comf.Contains(id); var cost = AHJson.O(c, "cost");
                rows.Add(slot => Row(slot, AHJson.S(c, "name") + "  <color=#9a9080>+" + AHJson.N(c, "pts", 1) + " comfort</color>" + (have ? "  <color=#9be37a>built</color>" : ""), Color.white, AHJson.S(c, "blurb"), have ? "" : AHHome.CostText(p, cost),
                    have ? null : new WkBtn { label = "Build", on = H.tier >= 1 && AHHome.CanPay(p, cost), col = Go, act = () => { if (!AHHome.Pay(p, cost)) return; H.comf.Add(id); Banner(AHJson.S(c, "name"), "Comfort " + AHHome.ComfortPts(p)); FarmChanged(); } }));
            }
    }
    void FadeOff() { Fade(false); }

    void FieldRows(AHPlayer p, object pl, List<Action<int>> rows)
    {
        var H = p.home; string id = AHJson.S(pl, "id"); var f = AHHome.Field(H, id); int flv = p.Skill("farming");
        if (f == null)
        {
            var crops = AHDB.Table("home", "CROPS");
            if (crops != null) foreach (var c in crops.Values)
                {
                    string cid = AHJson.S(c, "id"), sid = cid + "_seed"; int n = p.bag.Count(sid); if (n <= 0) continue; int lv = (int)AHJson.N(c, "lvl", 1);
                    rows.Add(slot => Row(slot, AHJson.S(c, "name") + "  <color=#9a9080>Farming " + lv + "</color>", Color.white, n + " seeds · ripe in " + AHJson.N(c, "min", 6) + " min (watered) · about " + AHJson.N(c, "yld", 4) + " crops", "",
                        new WkBtn
                        {
                            label = "Plant", on = flv >= lv, col = Go,
                            act = () => { if (!p.bag.Take(sid)) return; H.fields.Add(new AHField { id = id, crop = cid, prog = 0, last = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }); p.GainXp("farming", (int)AHJson.N(c, "xp", 10), false); Toast("You plant " + AHJson.S(c, "name").ToLowerInvariant() + ". Water it at the well for full speed.", 3f); FarmChanged(); }
                        }));
                }
            if (rows.Count == 0) rows.Add(slot => Row(slot, "No seeds in your bag", new Color(1f, 1f, 1f, 0.7f), "Buy seeds from a Land Agent in Varrow or Ashen Hollow.", ""));
            return;
        }
        var cr = AHHome.Crop(f.crop); double left = (1 - f.prog) * AHHome.CropDur(p, f);
        rows.Add(slot => Row(slot, AHJson.S(cr, "name"), Color.white, f.prog >= 1f ? "<color=#9be37a>Ready to harvest!</color>" : Mathf.FloorToInt(f.prog * 100) + "% grown · " + Mins((long)left) + " left" + (f.water ? " · watered" : " · <color=#ff8a7a>dry: growing at half speed</color>") + (f.bm ? " · bone meal" : "") + (f.comp ? " · compost" : ""), "",
            f.prog >= 1f ? new WkBtn { label = "Harvest", on = true, col = Go, act = () => { AHHome.Harvest(g, f); FarmChanged(); } } : null));
        if (f.prog < 1f)
        {
            rows.Add(slot => Row(slot, "Water it", Color.white, "Fetch water from the well. Watered crops grow twice as fast.", "", new WkBtn { label = "Water", on = !f.water, col = Go, act = () => { AHHome.Tick(g); f.water = true; p.GainXp("farming", 6, false); Toast("Watered. It grows twice as fast now."); FarmChanged(); } }));
            rows.Add(slot => Row(slot, "Bone meal", Color.white, "30% faster growth · you have " + p.bag.Count("bone_meal"), "", new WkBtn { label = "Spread", on = !f.bm && p.bag.Count("bone_meal") > 0, col = Plain, act = () => { if (!p.bag.Take("bone_meal")) return; AHHome.Tick(g); f.bm = true; p.GainXp("farming", 8, false); FarmChanged(); } }));
            rows.Add(slot => Row(slot, "Compost", Color.white, "+2 crops at harvest · you have " + p.bag.Count("compost"), "", new WkBtn { label = "Spread", on = !f.comp && p.bag.Count("compost") > 0, col = Plain, act = () => { if (!p.bag.Take("compost")) return; f.comp = true; p.GainXp("farming", 8, false); FarmChanged(); } }));
        }
    }

    static string AnimalBlurb(string k)
    {
        var A = AHHome.Animal(k); var prod = AHJson.A(A, "prod"); var l = new List<string>();
        if (prod != null) foreach (var o in prod) { var r = o as List<object>; var it = AHItems.Get((string)r[0]); string nm = it != null ? it.name.ToLowerInvariant() : (string)r[0]; l.Add((double)r[1] < 1 ? "sometimes " + nm : nm); }
        return "Gives " + string.Join(", ", l.ToArray()) + " every " + AHJson.N(A, "every", 10) + " min" + (AHJson.A(A, "feed") != null ? " while fed" : "") + ".";
    }

    void PenRows(AHPlayer p, object pl, List<Action<int>> rows)
    {
        var H = p.home; string id = AHJson.S(pl, "id"), ak = AHJson.S(pl, "animal"); var A = AHHome.Animal(ak); var pen = AHHome.Pen(H, id); int cap = AHHome.Cap(p);
        string an = AHJson.S(A, "name"); var feed = AHJson.A(A, "feed"); long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string feedTxt = feed != null ? (pen.n > 0 ? (pen.fed > now ? "Fed for " + Mins(pen.fed - now) + ". " : "<color=#ff8a7a>Hungry: they only produce while fed.</color> ") : "") + "Eats: " + string.Join(", ", feed.ConvertAll(o => { var it = AHItems.Get((string)o); return it != null ? it.name.ToLowerInvariant() : (string)o; }).ToArray()) + "." : "";
        rows.Add(slot => Row(slot, an + "s: " + pen.n + " / " + cap, new Color(1f, 0.8f, 0.45f), AnimalBlurb(ak), feedTxt));
        var prod = AHJson.A(A, "prod");
        rows.Add(slot => Row(slot, "Collect", Color.white, pen.ready > 0 ? pen.ready + " ready" : "Nothing to collect yet.", "",
            new WkBtn
            {
                label = "Collect", on = pen.ready > 0, col = Go,
                act = () =>
                {
                    AHHome.Tick(g); if (pen.ready <= 0) return;
                    var got = new Dictionary<string, int>();
                    for (int i = 0; i < pen.ready; i++) foreach (var o in prod) { var r = o as List<object>; if (UnityEngine.Random.value < (double)r[1]) { int c; got.TryGetValue((string)r[0], out c); got[(string)r[0]] = c + 1; } }
                    if (!p.bag.Fits(got.Keys)) { Toast("Your bag is full. Make room, then collect."); return; }
                    var l = new List<string>(); foreach (var kv in got) { p.bag.Add(kv.Key, kv.Value); l.Add(kv.Value + " " + AHItems.Get(kv.Key).name.ToLowerInvariant()); }
                    p.GainXp("farming", (int)AHJson.N(A, "xp", 8) * pen.ready, false); pen.ready = 0;
                    Toast("Collected " + (l.Count > 0 ? string.Join(", ", l.ToArray()) : "nothing this time") + ".", 3f); FarmChanged();
                }
            }));
        if (feed != null)
        {
            int have = 0; foreach (var o in feed) have += p.bag.Count((string)o);
            rows.Add(slot => Row(slot, "Feed them", Color.white, "One feed per animal feeds the pen for an hour (up to 3 hours). You have " + have + " they will eat.", "",
                new WkBtn
                {
                    label = "Feed all", on = pen.n > 0 && have > 0, col = Plain,
                    act = () =>
                    {
                        AHHome.Tick(g); int fed = 0;
                        foreach (var o in feed) while (fed < pen.n && p.bag.Count((string)o) > 0) { p.bag.Take((string)o); fed++; }
                        if (fed == 0) return; long t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); float fk = p.Perk("farmer", 20) ? 2f : 1f;
                        pen.fed = Math.Min(t + (long)(3 * 3600000L * fk), Math.Max(t, pen.fed) + (long)(3600000L * fk * fed / pen.n)); pen.last = t;
                        p.GainXp("farming", 4 * fed, false); Toast("You feed " + fed + " " + an.ToLowerInvariant() + (fed > 1 ? "s" : "") + "."); FarmChanged();
                    }
                }));
        }
        long price = (long)AHJson.N(A, "price", 150);
        rows.Add(slot => Row(slot, "Buy a " + an.ToLowerInvariant(), Color.white, AHItems.MoneyText(price * AHDB.CU), "",
            new WkBtn { label = "Buy", on = pen.n < cap && p.bag.money >= price * AHDB.CU, col = Go, act = () => { AHHome.Tick(g); p.bag.money -= price * AHDB.CU; pen.n++; if (pen.fed == 0) pen.fed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600000L; Toast("A " + an.ToLowerInvariant() + " joins your farm."); FarmChanged(); } }));
        var bu = AHJson.A(A, "butcher");
        if (bu != null)
        {
            var bl = bu.ConvertAll(o => { var r = o as List<object>; return (int)(double)r[1] + " " + AHItems.Get((string)r[0]).name.ToLowerInvariant(); });
            rows.Add(slot => Row(slot, "Butcher one", Color.white, "Gives " + string.Join(", ", bl.ToArray()) + ".", "",
                new WkBtn
                {
                    label = farmConfirm ? "Tap again" : "Butcher", on = pen.n > 0, col = new Color(0.45f, 0.22f, 0.17f, 1f),
                    act = () =>
                    {
                        if (!farmConfirm) { farmConfirm = true; RenderWork(); return; }
                        farmConfirm = false; var ids = bu.ConvertAll(o => (string)((List<object>)o)[0]);
                        if (!p.bag.Fits(ids)) { Toast("Your bag is full. Make room before butchering."); return; }
                        foreach (var o in bu) { var r = o as List<object>; p.bag.Add((string)r[0], (int)(double)r[1]); }
                        AHHome.Tick(g); pen.n--; pen.ready = Math.Min(pen.ready, pen.n * 3); p.GainXp("farming", (int)AHJson.N(A, "xp", 8) * 2, false); FarmChanged();
                    }
                }));
        }
    }

    void StallRows(AHPlayer p, List<Action<int>> rows)
    {
        var H = p.home;
        rows.Add(slot => Row(slot, "Takings", new Color(1f, 0.84f, 0.35f), AHItems.MoneyText(H.stallCoins) + " waiting · " + H.stallSold + " sold", "Buyers pay 50% more than a shop for things you made or grew. One sale about every 3 minutes, even while you are away.",
            new WkBtn { label = "Collect", on = H.stallCoins > 0, col = Go, act = () => { p.AddMoney(H.stallCoins, p.transform.position); Toast("You collect " + AHItems.MoneyText(H.stallCoins) + "."); H.stallCoins = 0; FarmChanged(); } }));
        foreach (var s in H.stall)
        {
            var it = AHItems.Get(s.id); if (it == null) continue; var ss = s;
            rows.Add(slot => Row(slot, ss.n + "× " + it.name + " on the stall", AHItems.Quality(it), AHItems.MoneyText(AHHome.StallPrice(ss.id), 2) + " each", "",
                new WkBtn { label = "Take back", on = true, col = Plain, act = () => { if (!p.bag.Add(ss.id, ss.n)) { Toast(p.bag.lastWarn ?? "Your bag is full."); return; } H.stall.Remove(ss); FarmChanged(); } }));
        }
        if (H.stall.Count < 6)
            foreach (var id in p.bag.order)
            {
                int n = p.OwnOf(id); var it = AHItems.Get(id); if (n <= 0 || it == null || it.slot != null) continue; string iid = id;
                rows.Add(slot => Row(slot, n + "× " + it.name, AHItems.Quality(it), "Sells for " + AHItems.MoneyText(AHHome.StallPrice(iid), 2) + " each", "",
                    new WkBtn { label = "Put on stall", on = true, col = Go, act = () => { int k = p.OwnOf(iid); if (k <= 0 || H.stall.Count >= 6) return; p.bag.Take(iid, k); if (H.stall.Count == 0) H.stallLast = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); H.stall.Add(new AHStack { id = iid, n = k }); FarmChanged(); } }));
            }
    }
}
