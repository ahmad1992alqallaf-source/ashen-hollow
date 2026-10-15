// Ashen Hollow: class pets. Each class has pets that suit it; with one of them out, from hero level 45 and pet level 5,
// it gives a small passive bonus. A pet gives nothing to a class it doesn't suit.
//   Healers (Priest, Druid, Shaman): Slime, Pumpkin slime     +healing
//   Ranger: Bee                                               +critical chance
//   Rogue: Fox, Night prowler (store)                         +evasion
//   Tanks (Warrior, Warden): Toad, Rock golem (store)         +max health
//   Mage: Spellbook, Spirit wisp (store), Flame hatchling (store)  +damage
//   (the Snow fox stands in as the mage's world pet until the Spellbook's model is in the game)
// Fair by design: every class has a world pet, and a store pet gives exactly what a world pet of the same kind gives
// (2% at pet level 5, rising to 3% at pet level 20). Only a Gilded pet, which can't be bought (it hatches from a world
// egg's legendary roll), gives +1% more. Pets' other stats stay off (eggs.json STATS false).
using System.Collections.Generic;
using UnityEngine;

public static class AHPetPassive
{
    public const int HeroLv = 45, PetLv = 5;
    public class Kind { public string stat, name; public string[] classes, pets; }
    public static readonly Kind[] Kinds =
    {
        new Kind { stat = "heal",  name = "healing",         classes = new[] { "priest", "druid", "shaman" }, pets = new[] { "slime", "pumpkin_slime" } },
        new Kind { stat = "crit",  name = "critical chance", classes = new[] { "ranger" },                     pets = new[] { "bee" } },
        new Kind { stat = "evade", name = "evasion",         classes = new[] { "rogue" },                      pets = new[] { "fox", "shadow_cat" } },
        new Kind { stat = "hpK",   name = "max health",      classes = new[] { "warrior", "warden" },          pets = new[] { "toad", "rock_golem" } },
        new Kind { stat = "dmg",   name = "damage",          classes = new[] { "mage" },                       pets = new[] { "spell_book", "spirit_wisp", "phoenix_chick" } },
    };
    public static Kind Of(string pet)
    {
        foreach (var k in Kinds) if (System.Array.IndexOf(k.pets, pet) >= 0) return k;
        // until the Spellbook can be found, the Snow fox is the mage's pet
        if (pet == "snow_fox" && (AHComp.Pets == null || !AHComp.Pets.ContainsKey("spell_book"))) return Kinds[Kinds.Length - 1];
        return null;
    }
    public static bool Suits(Kind k, string cls) { return k != null && System.Array.IndexOf(k.classes, cls) >= 0; }

    // the bonus a pet gives at its level (0..1), whoever has it
    public static float Value(AHPlayer p, string pet)
    {
        int lv = AHComp.PetLv(p, pet); if (lv < PetLv) return 0f;
        float v = 0.02f + 0.01f * Mathf.Clamp01((lv - PetLv) / (float)(AHComp.PetMax - PetLv));
        if (AHEggs.Gilded(p, pet)) v += 0.01f;   // earned only, never sold
        return v;
    }

    public static bool Active(AHPlayer p)
    {
        return p != null && p.pet != null && p.cls != null && p.level >= HeroLv && Suits(Of(p.pet), p.cls.id) && AHExpeditions.Away(p, p.pet) == null;
    }

    // added to the hero's stats in Recalc
    public static void Apply(AHPlayer p, AHStats s)
    {
        if (!Active(p)) return;
        var k = Of(p.pet); float v = Value(p, p.pet); if (v <= 0f) return;
        switch (k.stat)
        {
            case "heal": s.heal += v; break;
            case "crit": s.crit += v; break;
            case "evade": s.evade += v; break;
            case "hpK": s.hpK += v; break;
            case "dmg": s.dmg += v; break;
        }
    }

    static string ClassNames(Kind k)
    {
        var l = new List<string>();
        foreach (var c in k.classes) { var d = AHJson.O(AHDB.Classes, c); l.Add(AHJson.S(d, "name", c)); }
        return string.Join(", ", l.ToArray());
    }

    // one line for the pet list: what it gives, to whom, and whether it is on
    public static string Line(AHPlayer p, string pet)
    {
        var k = Of(pet); if (k == null) return "";
        int lv = AHComp.PetLv(p, pet); float v = Value(p, pet);
        string pct = v > 0f ? "+" + Mathf.RoundToInt(v * 100f) + "% " : "+2% to 3% ";
        if (!Suits(k, p.cls != null ? p.cls.id : ""))
            return "Class pet for " + ClassNames(k) + ": " + pct + k.name;
        if (p.level < HeroLv) return "Your class pet: " + pct + k.name + " from hero level " + HeroLv;
        if (lv < PetLv) return "Your class pet: " + pct + k.name + " from pet level " + PetLv;
        return "<color=#9be37a>Your class pet: " + pct + k.name + (p.pet == pet ? " (on)" : " when out") + "</color>";
    }
}
