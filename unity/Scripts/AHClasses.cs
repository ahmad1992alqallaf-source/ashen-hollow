// Ashen Hollow: the six classes and their first five spells, matching the web game
using UnityEngine;

public enum AHStatus { None, Burn, Poison, Slow, Root, Stun, Fear }

public enum SpellKind
{
    Strike,      // one target in reach
    Arc,         // everything in front of you
    Nova,        // everything around you
    Whirl,       // around you, several times
    Bolt,        // a homing shot at the target
    Multi,       // shots at up to three targets
    GroundAt,    // a blast where the target stands, after a moment
    Consecrate,  // holy ground under you that keeps burning
    Charge,      // rush to the target and hit it
    Blink,       // jump forward
    StepBehind,  // appear behind the target
    Leap,        // jump back, away from danger
    Heal, Hot, Shield, Invuln, Buff, Stealth, Bear,
    Totem        // the shaman's totems: planted at your feet, pulsing heal, war-fury or fire (AHTotem)
}

public class SpellDef
{
    public string id, name, abbr;
    public SpellKind kind;
    public float cd, mult = 1f, radius = 3f, range = 3f, value, time, cost;
    public AHStatus status;
    public string desc; public int lvl = 1;   // spellbook: description and the level it unlocks
    public SpellDef Scale(float k) { var c = (SpellDef)MemberwiseClone(); c.mult *= k; return c; }
    public float statusTime;
    public Color color = Color.white;

    public SpellDef(string id, string name, string abbr, SpellKind kind, float cd)
    { this.id = id; this.name = name; this.abbr = abbr; this.kind = kind; this.cd = cd; }
    public SpellDef M(float m) { mult = m; return this; }
    public SpellDef R(float r) { radius = r; return this; }
    public SpellDef Rg(float r) { range = r; return this; }
    public SpellDef V(float v) { value = v; return this; }
    public SpellDef T(float t) { time = t; return this; }
    public SpellDef S(AHStatus s, float t) { status = s; statusTime = t; return this; }
    public SpellDef C(int hex) { color = AHGame.Hex(hex); return this; }
}

public class ClassDef
{
    public string id, name, role, model;
    public float hp = 100f, range = 2.4f, atkCd = 0.75f, atkMult = 1f;
    public bool ranged, mana;
    public Color color = Color.white, tint = Color.white;
    public SpellDef[] spells;
}

public static class AHClasses
{
    public static readonly ClassDef[] All;

    static AHClasses()
    {
        All = new[]
        {
            new ClassDef
            {
                id = "warrior", name = "Warrior", role = "Frontline melee", model = "Knight_t", hp = 100, color = AHGame.Hex(0xff2d3d),
                spells = new[]
                {
                    new SpellDef("cleave", "Cleave", "CLV", SpellKind.Arc, 5).M(1.6f).R(3.2f).C(0xff6b5a),
                    new SpellDef("blow", "Crushing Blow", "BLW", SpellKind.Strike, 7).M(2.6f).Rg(3f).S(AHStatus.Stun, 1.5f).C(0xffd36a),
                    new SpellDef("charge", "Charge", "CHG", SpellKind.Charge, 10).M(1.2f).Rg(10f).S(AHStatus.Stun, 1f).C(0xff6b5a),
                    new SpellDef("whirl", "Whirlwind", "WHL", SpellKind.Whirl, 12).M(0.9f).R(3.4f).C(0xffb08a),
                    new SpellDef("cry", "War Cry", "CRY", SpellKind.Buff, 25).V(0.3f).T(10f).C(0xff2d3d),
                }
            },
            new ClassDef
            {
                id = "mage", name = "Mage", role = "Ranged magic", model = "Mage_t", hp = 80, ranged = true, mana = true, range = 11f, atkMult = 0.9f, color = AHGame.Hex(0x7fd4ff),
                spells = new[]
                {
                    new SpellDef("fireball", "Fireball", "FIR", SpellKind.Bolt, 4).M(2.2f).Rg(12f).S(AHStatus.Burn, 3f).C(0xff7a2a),
                    new SpellDef("nova", "Frost Nova", "NOV", SpellKind.Nova, 12).M(0.8f).R(4.5f).S(AHStatus.Root, 3f).C(0x7fd4ff),
                    new SpellDef("blink", "Blink", "BLK", SpellKind.Blink, 8).V(7f).C(0x9b5cff),
                    new SpellDef("meteor", "Meteor", "MET", SpellKind.GroundAt, 16).M(3f).R(3.5f).Rg(13f).S(AHStatus.Burn, 3f).C(0xff7a2a),
                    new SpellDef("barrier", "Mana Barrier", "BAR", SpellKind.Shield, 25).V(0.45f).T(10f).C(0x7fd4ff),
                }
            },
            new ClassDef
            {
                id = "priest", name = "Priest", role = "Healer and holy magic", model = "Mage_t", hp = 90, ranged = true, mana = true, range = 10f, atkMult = 0.85f, color = AHGame.Hex(0xffd66a), tint = new Color(1f, 0.95f, 0.8f),
                spells = new[]
                {
                    new SpellDef("heal", "Heal", "HEA", SpellKind.Heal, 5).V(0.3f).C(0x9be37a),
                    new SpellDef("holyfire", "Holy Fire", "HOL", SpellKind.Bolt, 6).M(1.9f).Rg(11f).S(AHStatus.Burn, 3f).C(0xffd66a),
                    new SpellDef("renew", "Renew", "REN", SpellKind.Hot, 15).V(0.45f).T(10f).C(0x9be37a),
                    new SpellDef("consecrate", "Consecration", "CON", SpellKind.Consecrate, 14).M(0.55f).R(4f).T(5f).C(0xffd66a),
                    new SpellDef("divine", "Divine Shield", "DIV", SpellKind.Invuln, 30).T(3f).C(0xffe28a),
                }
            },
            new ClassDef
            {
                id = "rogue", name = "Rogue", role = "Fast blades, poison and shadows", model = "Rogue_t", hp = 90, atkCd = 0.5f, atkMult = 0.7f, color = AHGame.Hex(0xb58cff),
                spells = new[]
                {
                    new SpellDef("backstab", "Backstab", "BST", SpellKind.Strike, 5).M(2.8f).Rg(3f).C(0xb58cff),
                    new SpellDef("envenom", "Envenom", "ENV", SpellKind.Strike, 9).M(1f).Rg(3f).S(AHStatus.Poison, 6f).C(0x7ad05a),
                    new SpellDef("shadowstep", "Shadowstep", "STP", SpellKind.StepBehind, 11).M(1f).Rg(11f).C(0x6a4a9a),
                    new SpellDef("fanknives", "Fan of Knives", "FAN", SpellKind.Nova, 11).M(1.2f).R(4f).C(0xd8d0e8),
                    new SpellDef("vanish", "Vanish", "VAN", SpellKind.Stealth, 26).T(4f).C(0x6a4a9a),
                }
            },
            new ClassDef
            {
                id = "ranger", name = "Ranger", role = "Bows, traps and a sharp eye", model = "Rogue_t", hp = 85, ranged = true, range = 13f, atkMult = 0.82f, color = AHGame.Hex(0x9be37a), tint = new Color(0.75f, 0.95f, 0.7f),
                spells = new[]
                {
                    new SpellDef("aimed", "Aimed Shot", "AIM", SpellKind.Bolt, 5).M(2.6f).Rg(14f).C(0xf2e2b0),
                    new SpellDef("multishot", "Multi-Shot", "MUL", SpellKind.Multi, 8).M(1.2f).Rg(13f).C(0xf2e2b0),
                    new SpellDef("disengage", "Disengage", "DIS", SpellKind.Leap, 10).V(6f).R(3.5f).S(AHStatus.Slow, 3f).C(0x9be37a),
                    new SpellDef("snare", "Snare Trap", "TRP", SpellKind.GroundAt, 14).M(0.8f).R(3f).Rg(12f).S(AHStatus.Root, 3f).C(0x8a6a44),
                    new SpellDef("huntmark", "Hunter's Mark", "MRK", SpellKind.Buff, 20).V(0.25f).T(12f).C(0xff5a3a),
                }
            },
            new ClassDef
            {
                id = "druid", name = "Druid", role = "Nature magic, healing and shapeshifting", model = "Barbarian_t", hp = 95, ranged = true, mana = true, range = 10.5f, atkMult = 0.85f, color = AHGame.Hex(0x7ad05a), tint = new Color(0.8f, 1f, 0.75f),
                spells = new[]
                {
                    new SpellDef("dwrath", "Wrath", "WRA", SpellKind.Bolt, 4).M(2f).Rg(11f).C(0x9be37a),
                    new SpellDef("roots", "Entangling Roots", "ROO", SpellKind.Strike, 12).M(0.6f).Rg(11f).S(AHStatus.Root, 4f).C(0x5a8a3a),
                    new SpellDef("rejuv", "Rejuvenation", "REJ", SpellKind.Hot, 12).V(0.4f).T(10f).C(0x9be37a),
                    new SpellDef("moonfire", "Moonfire", "MOO", SpellKind.Bolt, 8).M(1f).Rg(12f).S(AHStatus.Burn, 6f).C(0xb58cff),
                    new SpellDef("bearform", "Bear Form", "BEA", SpellKind.Bear, 30).V(0.25f).T(15f).C(0x8a5a3a),
                }
            },
            new ClassDef
            {
                // totems carry the fight: heal and war-fury for you and your party; agility (evasion), not shields, keeps
                // the shaman alive, so it heals well without becoming unkillable
                id = "shaman", name = "Shaman", role = "Totems, healing and the storm", model = "Barbarian_t", hp = 92, ranged = true, mana = true, range = 10.5f, atkMult = 0.85f, color = AHGame.Hex(0x3fc8d8), tint = new Color(0.75f, 0.95f, 1f),
                spells = new[]
                {
                    new SpellDef("lbolt", "Lightning Bolt", "LBO", SpellKind.Bolt, 4).M(2f).Rg(11f).C(0x8ad8ff),
                    new SpellDef("healtotem", "Healing Totem", "HTO", SpellKind.Totem, 14).V(0.05f).R(6f).T(12f).C(0x7ae0a0),
                    new SpellDef("wartotem", "War Totem", "WTO", SpellKind.Totem, 20).V(0.25f).R(7f).T(12f).C(0xff8a4a),
                    new SpellDef("chainheal", "Chain Heal", "CHH", SpellKind.Heal, 9).V(0.2f).C(0x7ae0a0),
                    new SpellDef("earthshock", "Earth Shock", "ESH", SpellKind.Strike, 8).M(1.4f).Rg(11f).S(AHStatus.Slow, 3f).C(0xc8a060),
                }
            },
        };
        // web v67: only classes without noMana use mana (Mage, Priest, Druid); spellCost = round(10 + cd * 1.6)
        foreach (var c in All)
        {
            var d = AHJson.O(AHDB.Classes, c.id);
            if (d != null) c.mana = !AHJson.B(d, "noMana");
            if (c.mana) foreach (var s in c.spells) s.cost = Mathf.Round(10f + s.cd * 1.6f);
        }
    }

    public static ClassDef Get(string id)
    {
        foreach (var c in All) if (c.id == id) return c;
        return All[0];
    }
}
