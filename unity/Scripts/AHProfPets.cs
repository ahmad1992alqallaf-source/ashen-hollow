// Ashen Hollow: profession pets. Little helpers earned by working a gathering skill, not bought or hatched:
//   Mining: the Drill mole (and, rarely, the Drill python)    Woodcutting: the Lumber beaver
//   Fishing: the River otter                                   Herblore: the Herb hedgehog
// Each turns up now and then while you gather (from skill 3; a little more often for an Artisan), and comes to you for
// sure once the skill reaches 12. The python is rare: only for miners of skill 15 or more who already have the mole.
// For an Artisan, a profession pet that is out brings what its trade gathers by itself, every few minutes (sooner as
// it levels): ore, logs, fish or herbs fit for your skill, with a little skill XP. For an Adventurer it is a companion
// to collect and show off, with no help in a fight and no gathering (looks only). They never learn the fighting
// skills (guard, mend…) and have no class bonus.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHProfPets
{
    public class Tier { public string item; public int lvl, xp; public Tier(string i, int l, int x) { item = i; lvl = l; xp = x; } }
    public class Def { public string id, skill, verb, needs; public int minLv = 3, sureLv = 12; public float chance = 1f / 450f; public Tier[] tiers; }
    public static readonly Def[] Defs =
    {
        new Def { id = "mole", skill = "mining", verb = "dug up", tiers = new[] { new Tier("copper_ore", 1, 18), new Tier("tin_ore", 1, 18), new Tier("iron_ore", 10, 40), new Tier("gold_ore", 15, 70) } },
        new Def { id = "drill_python", skill = "mining", verb = "drilled out", needs = "mole", minLv = 15, sureLv = 0, chance = 1f / 1500f,
            tiers = new[] { new Tier("copper_ore", 1, 18), new Tier("iron_ore", 10, 40), new Tier("gold_ore", 15, 70), new Tier("obsidian_shard", 18, 120) } },
        new Def { id = "beaver", skill = "woodcutting", verb = "gnawed down", tiers = new[] { new Tier("logs", 1, 25), new Tier("pine_logs", 10, 50) } },
        new Def { id = "otter", skill = "fishing", verb = "caught", tiers = new[] { new Tier("raw_trout", 1, 20), new Tier("raw_salmon", 10, 45), new Tier("raw_tuna", 12, 60), new Tier("raw_swordfish", 16, 90) } },
        new Def { id = "hedgehog", skill = "herblore", verb = "found", tiers = new[] { new Tier("sunpetal", 1, 14), new Tier("mireroot", 5, 24), new Tier("frostbloom", 10, 38), new Tier("emberthorn", 13, 48), new Tier("dragonfern", 16, 62), new Tier("ashbloom", 19, 90) } },
    };
    public static Def Of(string pet) { return pet == null ? null : Array.Find(Defs, d => d.id == pet); }
    public static bool Is(string pet) { return Of(pet) != null; }
    // how tall each one stands next to the hero (0: as the model is)
    public static float Height(string pet) { return pet == "mole" ? 0.75f : pet == "drill_python" ? 0.6f : pet == "beaver" ? 0.8f : pet == "otter" ? 0.95f : pet == "hedgehog" ? 0.55f : 0f; }
    static string SkillName(string s) { return char.ToUpperInvariant(s[0]) + s.Substring(1); }
    static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }

    // seconds between hauls: 10 minutes, a quarter of a minute less per pet level (5 at the least)
    public static int Every(AHPlayer p, string pet) { return Mathf.Max(300, 600 - 15 * AHComp.PetLv(p, pet)); }
    public static bool Helps(AHPlayer p) { return p != null && p.path == "artisan" && Is(p.pet) && AHExpeditions.Away(p, p.pet) == null; }
    public static string HelpText(string pet)
    {
        var d = Of(pet); if (d == null) return "";
        return "Profession pet (" + SkillName(d.skill) + "). For an Artisan it brings " + Brings(d) + " by itself every few minutes; for an Adventurer it is a companion to collect (looks only).";
    }
    static string Brings(Def d) { return d.skill == "mining" ? "ore" : d.skill == "woodcutting" ? "logs" : d.skill == "fishing" ? "fish" : "herbs"; }
    public static string Where(Def d)
    {
        return d.sureLv > 0 ? "Turns up while " + Doing(d.skill) + " from " + SkillName(d.skill) + " " + d.minLv + "; yours for sure at " + SkillName(d.skill) + " " + d.sureLv
                            : "A rare find while " + Doing(d.skill) + " at " + SkillName(d.skill) + " " + d.minLv + "+, once you have the " + AHComp.PetName(d.needs);
    }
    static string Doing(string s) { return s == "mining" ? "mining" : s == "woodcutting" ? "chopping trees" : s == "fishing" ? "fishing" : "picking herbs"; }

    // from AHGather, after each thing gathered with that skill
    public static void OnGather(AHGame g, string skill)
    {
        var p = g.player; if (p == null || AHComp.Pets == null) return;
        foreach (var d in Defs)
        {
            if (d.skill != skill || p.pets.Contains(d.id) || !AHComp.Pets.ContainsKey(d.id)) continue;   // (shown only once its model is in)
            int lv = p.Skill(skill); if (lv < d.minLv) continue;
            if (d.needs != null && !p.pets.Contains(d.needs)) continue;
            bool sure = d.sureLv > 0 && lv >= d.sureLv;
            if (!sure && UnityEngine.Random.value >= d.chance * (p.path == "artisan" ? 1.5f : 1f)) continue;
            p.pets.Add(d.id);
            g.ui.Banner(AHComp.PetName(d.id) + " found you!", "It follows you now. Call it from Pets." + (p.path == "artisan" ? " Out at your side, it gathers " + Brings(d) + " for you." : ""));
            AHSound.Play("level");
            g.MarkDirty();
            return;
        }
    }

    // every few seconds (AHGame): an Artisan's profession pet brings its haul when the time comes round
    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null || p.prog == null || p.dead || !Helps(p)) return;
        long now = NowS;
        if (p.prog.profPetAt <= 0 || p.prog.profPetPet != p.pet) { p.prog.profPetAt = now + Every(p, p.pet); p.prog.profPetPet = p.pet; return; }
        if (now < p.prog.profPetAt) return;
        p.prog.profPetAt = now + Every(p, p.pet);
        Haul(g, p, Of(p.pet));
    }

    public static void Haul(AHGame g, AHPlayer p, Def d)
    {
        if (d == null) return;
        int lv = p.Skill(d.skill);
        var ok = new List<Tier>(); foreach (var t in d.tiers) if (t.lvl <= lv && AHItems.Get(t.item) != null) ok.Add(t);
        if (ok.Count == 0) return;
        // mostly the best two things your skill allows
        var tr = ok.Count > 1 && UnityEngine.Random.value < 0.7f ? ok[ok.Count - 1 - UnityEngine.Random.Range(0, Mathf.Min(2, ok.Count))] : ok[UnityEngine.Random.Range(0, ok.Count)];
        int plv = AHComp.PetLv(p, p.pet), n = 1 + (plv >= 8 ? 1 : 0) + (plv >= 16 ? 1 : 0);
        bool mail = !p.bag.Add(tr.item, n);
        if (mail && AHAuction.S != null) AHAuction.MailAdd(tr.item, n);
        p.GainXp(d.skill, Mathf.Max(1, tr.xp * n / 2), false);
        long x; p.petXp.TryGetValue(p.pet, out x); p.petXp[p.pet] = x + 40;
        string what = n + "× " + AHItems.Get(tr.item).name;
        g.ui.Toast("Your " + AHComp.PetName(p.pet) + " " + d.verb + " " + what + (mail ? " (bag full: sent to your auction mail)" : "") + ".", 3.5f);
        if (g.player != null) g.ui.Float(g.player.transform.position + Vector3.up * 2.4f, "+" + what, new Color(0.79f, 0.9f, 0.66f));
        p.bag.Touch(); g.MarkDirty();
    }

    public static string NextText(AHPlayer p)
    {
        if (!Helps(p)) return null;
        long left = Math.Max(0, p.prog.profPetAt - NowS);
        return p.prog.profPetPet == p.pet && p.prog.profPetAt > 0 ? "next haul in " + AHExpeditions.LeftText(left) : "first haul in about " + (Every(p, p.pet) / 60) + "m";
    }
}

public partial class AHUI
{
    public void OpenProfPets() { wkMode = "profpets"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderProfPets(AHPlayer p)
    {
        wkTitle.text = "Profession pets";
        wkHint.text = p.path == "artisan"
            ? "Little helpers earned by gathering. Out at your side, each brings what its trade gathers by itself every few minutes (sooner as it levels)."
            : "Little helpers earned by gathering. For an Adventurer they are companions to collect; in an Artisan's hands they gather by themselves.";
        var rows = new List<System.Action<int>>();
        foreach (var d in AHProfPets.Defs)
        {
            var dd = d; bool shown = AHComp.Pets != null && AHComp.Pets.ContainsKey(d.id), own = p.pets.Contains(d.id), outNow = p.pet == d.id;
            string name = shown || own ? AHComp.PetName(d.id) : "A " + d.skill + " pet (coming soon)";
            rows.Add(s => Row(s, name + (outNow ? "  <color=#9be37a>out</color>" : own ? "  <color=#c8b89a>yours</color>" : ""), own ? new Color(1f, 0.62f, 0.24f) : new Color(1f, 1f, 1f, 0.6f),
                own ? (outNow && AHProfPets.Helps(p) ? "Gathering for you · " + AHProfPets.NextText(p) : AHProfPets.HelpText(dd.id)) : AHProfPets.Where(dd),
                own ? "" : "Your " + char.ToUpperInvariant(dd.skill[0]) + dd.skill.Substring(1) + ": " + p.Skill(dd.skill),
                own ? new WkBtn { label = outNow ? "Put away" : "Call", on = AHExpeditions.Away(p, dd.id) == null, col = outNow ? Plain : Go, act = () => { p.pet = outNow ? null : dd.id; AHComp.SpawnPet(g); g.MarkDirty(); RenderWork(); } } : null));
        }
        rows.Add(s => Row(s, "Back to companions", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "comp"; wkPageI = 0; RenderWork(); } }));
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
