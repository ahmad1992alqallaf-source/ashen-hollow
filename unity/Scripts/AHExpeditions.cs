// Ashen Hollow: pet expeditions. Send a pet you own (not the one at your side) on a trip of 1, 4 or 8 hours to a land
// you have reached; it comes back with that land's materials, pet XP, and now and then a dye, a costume pattern, a gem,
// a mystery sack or a rare recipe scroll. Longer trips bring more and have better odds of a rare find. A pet away
// can't be called until it is back. Two pets can be away at once (three for an Artisan). Time runs while the game is
// closed. Opened from Companions (Menu -> Companions -> Pet expeditions). Nothing here can be bought with Crowns.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHExped { public string pet, land; public long start, end; public int hours; public bool told; }

public static class AHExpeditions
{
    public class Land { public string id, name, blurb; public int lvl; public string[] items; }
    public static readonly Land[] Lands =
    {
        new Land { id = "wood",  name = "Whisperwood",    lvl = 1,  blurb = "logs, herbs, honey and feathers", items = new[] { "logs", "pine_logs", "sunpetal", "mireroot", "honey", "feather" } },
        new Land { id = "hills", name = "Copper Hills",   lvl = 1,  blurb = "copper, tin and iron ore",         items = new[] { "copper_ore", "tin_ore", "copper_ore", "tin_ore", "iron_ore" } },
        new Land { id = "fields", name = "Meadow Farms",  lvl = 5,  blurb = "wool, cotton and flax",            items = new[] { "wool", "cotton", "flax" } },
        new Land { id = "shore", name = "Silver Shore",   lvl = 8,  blurb = "fish, coconuts and pearls",        items = new[] { "raw_trout", "raw_salmon", "raw_trout", "coconut", "pearl", "sea_silk" } },
        new Land { id = "ember", name = "Ember Wastes",   lvl = 16, blurb = "obsidian, ember hides and magma",  items = new[] { "obsidian_shard", "ember_hide", "salamander_scale", "obsidian_shard", "magma_core", "gold_ore" } },
        new Land { id = "deep",  name = "The Hollow Deep", lvl = 25, blurb = "bone dust, void dust and relics", items = new[] { "bone_dust", "void_dust", "bone_dust", "ancient_relic" } },
    };
    public static readonly int[] Hours = { 1, 4, 8 };
    static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }

    public static List<AHExped> L(AHPlayer p) { if (p.prog.exped == null) p.prog.exped = new List<AHExped>(); return p.prog.exped; }
    public static int Slots(AHPlayer p) { return p.path == "artisan" ? 3 : 2; }
    public static AHExped Away(AHPlayer p, string pet) { return p == null || p.prog == null ? null : L(p).Find(e => e.pet == pet); }
    public static bool Back(AHExped e) { return NowS >= e.end; }
    public static long Left(AHExped e) { return Math.Max(0, e.end - NowS); }
    public static Land LandOf(string id) { return Array.Find(Lands, x => x.id == id); }
    public static string LeftText(long s) { long m = (s + 59) / 60; return m >= 60 ? (m / 60) + "h " + (m % 60) + "m" : m + "m"; }

    public static bool Send(AHGame g, string pet, string land, int hours)
    {
        var p = g.player; var l = L(p); var ld = LandOf(land);
        if (ld == null || !p.pets.Contains(pet) || Away(p, pet) != null) return false;
        if (l.Count >= Slots(p)) { g.ui.Toast("All your expedition slots are in use. Wait for a pet to come home."); return false; }
        if (p.level < ld.lvl) { g.ui.Toast(ld.name + " opens at level " + ld.lvl + "."); return false; }
        if (p.pet == pet) { p.pet = null; AHComp.SpawnPet(g); }   // the pet at your side leaves your side
        l.Add(new AHExped { pet = pet, land = land, hours = hours, start = NowS, end = NowS + hours * 3600L });
        g.ui.Toast(AHComp.PetName(pet) + " sets off for " + ld.name + ". Back in " + hours + (hours > 1 ? " hours." : " hour."), 3f);
        g.MarkDirty();
        return true;
    }

    // a pet coming home says so once, even with the panel closed
    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        foreach (var e in L(p))
            if (!e.told && Back(e)) { e.told = true; var ld = LandOf(e.land); g.ui.Toast(AHComp.PetName(e.pet) + " is back from " + (ld != null ? ld.name : "its trip") + ". Collect its finds in Companions.", 4f); g.MarkDirty(); }
    }

    static void Put(Dictionary<string, int> got, string id, int n) { if (id == null || n <= 0 || AHItems.Get(id) == null) return; int c; got.TryGetValue(id, out c); got[id] = c + n; }

    public static void Collect(AHGame g, AHExped e)
    {
        var p = g.player; if (!L(p).Contains(e) || !Back(e)) return;
        var ld = LandOf(e.land); int lv = AHComp.PetLv(p, e.pet);
        var got = new Dictionary<string, int>();
        // finds: about 4 an hour, a little more from a seasoned pet
        if (ld != null)
        {
            int n = Mathf.RoundToInt(e.hours * 4f * (1f + lv / 20f) * (0.85f + UnityEngine.Random.value * 0.3f));
            for (int i = 0; i < n; i++) Put(got, ld.items[UnityEngine.Random.Range(0, ld.items.Length)], 1);
        }
        // rare finds, better on a long trip
        float hk = e.hours / 8f;
        if (UnityEngine.Random.value < 0.08f + 0.3f * hk) Put(got, AHFashion.Dyes[UnityEngine.Random.Range(0, AHFashion.Dyes.Length)].id, 1);
        if (UnityEngine.Random.value < 0.05f + 0.15f * hk) { string[] gems = { "sapphire", "ruby", "emerald" }; Put(got, gems[UnityEngine.Random.Range(0, 3)], 1); }
        if (UnityEngine.Random.value < 0.04f + 0.12f * hk) Put(got, "mystery_sack", 1);
        if (UnityEngine.Random.value < 0.01f + 0.07f * hk) { string[] pats = { "pattern_ember", "pattern_sage", "pattern_crimson", "pattern_duelist" }; Put(got, pats[UnityEngine.Random.Range(0, pats.Length)], 1); }
        if (e.hours >= 4 && UnityEngine.Random.value < 0.06f * hk) Put(got, AHRareRecipes.ScrollId, 1);
        var said = new List<string>(); bool full = false;
        foreach (var kv in got)
        {
            if (p.bag.Add(kv.Key, kv.Value)) said.Add(kv.Value + "× " + AHItems.Get(kv.Key).name);
            else { full = true; if (AHAuction.S != null) AHAuction.MailAdd(kv.Key, kv.Value); }
        }
        // pet XP for the traveller (not the pet at your side)
        long x; p.petXp.TryGetValue(e.pet, out x); p.petXp[e.pet] = x + e.hours * 120L;
        int lv2 = AHComp.PetLv(p, e.pet);
        L(p).Remove(e);
        g.ui.Banner(AHComp.PetName(e.pet) + " is home", said.Count > 0 ? string.Join(", ", said.ToArray()) : "It brought nothing this time");
        if (full) g.ui.Toast("Your bag was full: the rest waits in your auction mail at an Auctioneer.", 4f);
        if (lv2 > lv) g.ui.Toast(AHComp.PetName(e.pet) + " is now level " + lv2 + ".", 3f);
        p.bag.Touch(); g.MarkDirty();
    }
}

public partial class AHUI
{
    string expPet;
    public void OpenExpeditions() { wkMode = "exped"; expPet = null; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderExpeditions(AHPlayer p)
    {
        AHExpeditions.Tick(g);
        var l = AHExpeditions.L(p);
        wkTitle.text = expPet == null ? "Pet expeditions" : "Send " + AHComp.PetName(expPet);
        wkHint.text = expPet == null
            ? "Send a pet on a trip; it comes back with materials, pet XP and sometimes something rare. " + l.Count + " / " + AHExpeditions.Slots(p) + " away" + (p.path == "artisan" ? " (Artisans get a third slot)." : ".")
            : "Choose where to go and for how long. Longer trips bring more, with better odds of a rare find. Time runs while the game is closed.";
        var rows = new List<Action<int>>();
        if (expPet == null)
        {
            foreach (var e in l)
            {
                var ee = e; var ld = AHExpeditions.LandOf(e.land); bool back = AHExpeditions.Back(e);
                rows.Add(s => Row(s, AHComp.PetName(ee.pet) + " · " + (ld != null ? ld.name : ee.land), new Color(1f, 0.62f, 0.24f),
                    back ? "<color=#9be37a>Back home</color>" : "Away · back in " + AHExpeditions.LeftText(AHExpeditions.Left(ee)), ld != null ? "Finds " + ld.blurb : "",
                    new WkBtn { label = "Collect", on = back, col = Go, act = () => { AHExpeditions.Collect(g, ee); RenderWork(); } }));
            }
            foreach (var k in p.pets)
            {
                if (AHComp.Retired.Contains(k) || AHExpeditions.Away(p, k) != null) continue;
                string kk = k;
                rows.Add(s => Row(s, AHComp.PetName(kk) + " · Lv " + AHComp.PetLv(p, kk) + (p.pet == kk ? "  <color=#9be37a>out</color>" : ""), Color.white,
                    "Ready to travel", "",
                    new WkBtn { label = "Send", on = l.Count < AHExpeditions.Slots(p), col = Plain, act = () => { expPet = kk; wkPageI = 0; RenderWork(); } }));
            }
            if (p.pets.Count == 0) rows.Add(s => Row(s, "No pets yet", new Color(1f, 1f, 1f, 0.7f), "Buy one at a menagerie, or hatch an egg.", ""));
            rows.Add(s => Row(s, "Back to companions", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "comp"; wkPageI = 0; RenderWork(); } }));
        }
        else
        {
            foreach (var ld in AHExpeditions.Lands)
            {
                var land = ld; bool open = p.level >= ld.lvl;
                var btns = new List<WkBtn>();
                foreach (int h in AHExpeditions.Hours)
                {
                    int hh = h;
                    btns.Add(new WkBtn { label = h + "h", on = open, col = h == 8 ? Go : Plain, act = () => { if (AHExpeditions.Send(g, expPet, land.id, hh)) expPet = null; wkPageI = 0; RenderWork(); } });
                }
                rows.Add(s => Row(s, land.name, open ? new Color(1f, 0.85f, 0.55f) : new Color(1f, 1f, 1f, 0.5f), open ? "Finds " + land.blurb : "Opens at level " + land.lvl, "", btns.ToArray()));
            }
            rows.Add(s => Row(s, "Choose another pet", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { expPet = null; wkPageI = 0; RenderWork(); } }));
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
