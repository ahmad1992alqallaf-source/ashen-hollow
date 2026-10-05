// Ashen Hollow: the people among the monsters (road bandits and their chief, Tidewake pirates and Captain Saltbeard,
// the frost and toad cultists, the High Priest of the tomb) were stick figures built from cylinders in the web game.
// Here they get the same rigged, animated bodies as the hero and the sellswords: a Quaternius body and face, a KayKit
// weapon in hand, and their kit drawn from the game's own items (hoods, coats, tricorns, robes, plate and cloaks),
// with proper idle, walk, run, attack, hit and death animations.
using System.Collections.Generic;
using UnityEngine;

public static class AHMobPeople
{
    class Look
    {
        public string body, wm, main, off; public string[] parts; public int hair; public string[] kit; public bool caster, hood;
    }
    // kit order: head, shoulders, chest, hands, legs, feet, cape
    static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
    {
        { "bandit", new Look { body = "qMaleRanger", hood = true, parts = new[] { "qHead_Male", "qHair_Beard", "qEyebrows_Regular" }, hair = 0x3a2416, wm = "Knight_t", main = "1H_Sword", off = null,
            kit = new[] { null, null, "leather_vest", "boar_gloves", null, "deer_boots", "wolf_cloak" } } },
        { "banditchief", new Look { body = "qMaleRanger", parts = new[] { "qHead_Male", "qHair_Buzzed", "qHair_Beard", "qEyebrows_Regular" }, hair = 0x5a2a12, wm = "Barbarian_t", main = "2H_Axe", off = null,
            kit = new[] { "bronze_helm", "bronze_pauldrons", "bronze_plate", "boar_gloves", null, "deer_boots", "wolf_cloak" } } },
        { "pirate", new Look { body = "qMalePeasant", parts = new[] { "qHead_Male", "qHair_SimpleParted", "qHair_Beard", "qEyebrows_Regular" }, hair = 0x2a1a10, wm = "Knight_t", main = "1H_Sword", off = null,
            kit = new[] { "corsair_hat", null, "corsair_coat", null, null, "shadow_boots", null } } },
        { "saltbeard", new Look { body = "qMaleRanger", parts = new[] { "qHead_Male", "qHair_Long", "qHair_Beard", "qEyebrows_Regular" }, hair = 0xe8e0d0, wm = "Knight_t", main = "1H_Sword", off = "1H_Sword_Offhand",
            kit = new[] { "captain_hat", null, "captain_coat", null, null, "shadow_boots", "captain_cape" } } },
        { "f_cultist", new Look { body = "qMalePeasant", parts = new[] { "qHead_Male", "qEyebrows_Regular" }, hair = 0xd0d8e0, wm = "Mage_t", main = "2H_Staff", off = null, caster = true,
            kit = new[] { "frost_hood", "frost_mantle", "arcane_robe", null, null, null, null } } },
        { "w_cultist", new Look { body = "qMalePeasant", parts = new[] { "qHead_Male", "qEyebrows_Regular" }, hair = 0x3a3a1a, wm = "Mage_t", main = "1H_Wand", off = "Spellbook", caster = true,
            kit = new[] { "raptor_hood", "verdant_mantle", "mossweave_chest", null, null, null, null } } },
        { "t_priest", new Look { body = "qMalePeasant", parts = new[] { "qHead_Male", "qHair_Beard", "qEyebrows_Regular" }, hair = 0x1a1410, wm = "Mage_t", main = "2H_Staff", off = null, caster = true,
            kit = new[] { "gold_crown", "sun_mantle", "sunpriest_chest", null, null, null, "desert_veil" } } },
    };
    static readonly string[] Slots = { "head", "shoulders", "chest", "hands", "legs", "feet", "cape" };

    // this beast's person (its own id, or the id its model comes from), or null
    static Look Find(string id)
    {
        Look l; int n = 0;
        while (id != null && n++ < 6)
        {
            if (Looks.TryGetValue(id, out l)) return l;
            id = AHJson.S(AHJson.O(AHDB.Mobs, id), "model");
        }
        return null;
    }
    public static bool Is(string id) { return Find(id) != null; }

    public static GameObject Build(Transform holder, AHMobType t, AHGame g, out AHAnim anim)
    {
        anim = null; var L = Find(t.id); if (L == null) return null;
        AHAnim a; var rig = AHPeople.Assemble(holder, L.body, new List<string>(L.parts), g, out a);
        if (rig == null) return null;
        anim = a;
        if (anim != null)
            anim.alias = new Dictionary<string, string>
            {
                { "Idle", L.caster ? "Spell_Simple_Idle_Loop" : "Sword_Idle" }, { "Walk", "Walk_Loop" }, { "Gallop", "Jog_Fwd_Loop" },
                { "Attack", L.caster ? "Spell_Simple_Shoot" : "Sword_Attack" }, { "HitReact", "Hit_Chest" }, { "Death", "Death01" },
            };
        Color hc = AHGame.Hex(L.hair);
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.Contains("Hood") && !L.hood) r.enabled = false;
            var mats = r.materials; bool ch = false;
            foreach (var m in mats) if (m != null && m.name.Contains("Hair")) { foreach (var pr in new[] { "baseColorFactor", "_BaseColor" }) if (m.HasProperty(pr)) m.SetColor(pr, hc.linear * 1.6f); ch = true; }
            if (ch) r.materials = mats;
        }
        AHPeople.Arm(rig, L.wm, L.main, L.off, g);
        var kit = L.kit;
        AHWardrobe.DressWith(rig, holder, s => { int i = System.Array.IndexOf(Slots, s); return i >= 0 && kit[i] != null && AHItems.Get(kit[i]) != null ? kit[i] : null; }, () => false);
        float k = (float)AHJson.N(AHJson.O(AHDB.Mobs, t.id), "scale", 1);
        if (k != 1f) rig.transform.localScale *= k;
        return rig;
    }
}
