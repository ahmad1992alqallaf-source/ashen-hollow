// Ashen Hollow: items from items.json (838 of them) and the hero's bag, gear and money, with the web game's rules:
// 28 bag slots (one per kind of item, stacks have no limit), 10 gear slots, which class can use what,
// set bonuses, and money kept as one number of bronze (1 copper = 1,000 bronze).
using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemDef
{
    public string id, name, icon, slot, style, form, rarity, set, note;
    public Color color = Color.white;
    public int atk, def, hp;
    public float dmg, cdr, heal;
    public bool cosmetic, fuel, potion, meal;
    public string reins;   // reins of a rare mount: use them to learn it
    public int enh; public string baseId;   // enhanced gear: "+N" of a base piece
    public int foodHp, foodHunger;
    public bool IsGear { get { return slot != null; } }
    public object MemberwiseCopy() { return MemberwiseClone(); }
    public bool IsFood { get { return foodHp > 0 || foodHunger > 0; } }

    // a short label for the round icon (the web game draws SVG icons; here it is letters)
    public string Abbr
    {
        get
        {
            var words = name.Replace("’", "").Replace("'", "").Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2) return (words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, Math.Min(2, words[words.Length - 1].Length))).ToUpperInvariant();
            return name.Substring(0, Math.Min(3, name.Length)).ToUpperInvariant();
        }
    }
}

public class AHStats { public int atk, def, hp; public float dmg, cdr, heal, crit, evade, aspd, hpK, manaK; }

public static class AHItems
{
    public static readonly string[] GearSlots = { "head", "shoulders", "chest", "hands", "legs", "feet", "cape", "weapon", "ring", "amulet" };
    static readonly Dictionary<string, string> slotName = new Dictionary<string, string>
    {
        { "head", "Head" }, { "shoulders", "Shoulders" }, { "chest", "Chest" }, { "hands", "Hands" }, { "legs", "Legs" },
        { "feet", "Feet" }, { "cape", "Cape" }, { "weapon", "Weapon" }, { "ring", "Ring" }, { "amulet", "Amulet" },
    };
    public static string SlotName(string s) { string n; return s != null && slotName.TryGetValue(s, out n) ? n : s; }

    static Dictionary<string, ItemDef> cache;

    public static object Raw(string id) { object r = null; if (id != null && AHDB.Items != null && !AHDB.Items.TryGetValue(id, out r) && id.Length > 2 && id[id.Length - 2] == '+') AHDB.Items.TryGetValue(id.Substring(0, id.Length - 2), out r); return r; }

    public static ItemDef Get(string id)
    {
        if (id == null) return null;
        if (cache == null) cache = new Dictionary<string, ItemDef>();
        ItemDef d;
        if (cache.TryGetValue(id, out d)) return d;
        var items = AHDB.Items;
        object raw;
        if (items == null || !items.TryGetValue(id, out raw))
        {
            // web ensureItem: "+N" versions of a piece of gear are made when needed
            d = null;
            int n; ItemDef b;
            if (id.Length > 2 && id[id.Length - 2] == '+' && int.TryParse(id.Substring(id.Length - 1), out n) && n >= 1 && n <= 9 && (b = Get(id.Substring(0, id.Length - 2))) != null && b.IsGear && !b.cosmetic && b.enh == 0)
            {
                d = (ItemDef)b.MemberwiseCopy();
                d.id = id; d.name = "+" + n + " " + b.name; d.enh = n; d.baseId = b.id;
                d.atk = EnhStat(b.atk, n, false); d.def = EnhStat(b.def, n, false); d.hp = EnhStat(b.hp, n, true);
            }
            cache[id] = d; return d;
        }
        d = new ItemDef
        {
            id = id,
            name = AHJson.S(raw, "name", id),
            icon = AHJson.S(raw, "icon"),
            slot = AHJson.S(raw, "slot"),
            style = AHJson.S(raw, "style"),
            form = AHJson.S(raw, "form"),
            rarity = AHJson.S(raw, "rarity"),
            set = AHJson.S(raw, "set"),
            color = AHDB.Col(AHJson.Has(raw, "c") ? ((Dictionary<string, object>)raw)["c"] : null, Color.white),
            cosmetic = AHJson.B(raw, "cosmetic"),
            fuel = AHJson.B(raw, "fuel"),
            potion = AHJson.Has(raw, "potion"),
            meal = AHJson.Has(raw, "meal"),
            reins = AHJson.S(raw, "reins"),
        };
        var st = AHJson.O(raw, "stats");
        if (st != null)
        {
            d.atk = (int)AHJson.N(st, "atk"); d.def = (int)AHJson.N(st, "def"); d.hp = (int)AHJson.N(st, "hp");
            d.dmg = (float)AHJson.N(st, "dmg"); d.cdr = (float)AHJson.N(st, "cdr"); d.heal = (float)AHJson.N(st, "heal");
        }
        var food = AHJson.O(raw, "food");
        if (food != null) { d.foodHp = (int)AHJson.N(food, "hp"); d.foodHunger = (int)AHJson.N(food, "hunger"); }
        d.note = AHJson.S(AHJson.O(AHDB.Rules, "ITEM_NOTE"), id);
        cache[id] = d;
        return d;
    }

    static int EnhStat(int v, int n, bool hp) { return v > 0 ? Mathf.Max(Mathf.RoundToInt(v * (1f + 0.08f * n)), v + Mathf.CeilToInt(n * (hp ? 1.5f : 0.5f))) : v; }

    static readonly string[] RareMats = { "sapphire", "ruby", "emerald", "pearl", "truffle", "magma_core", "kraken_ink", "drake_wing", "wyrm_wings", "wyrm_horns", "dragon_scale", "tyrant_tooth", "queen_stinger", "pyrax_heart", "enh_stone" };
    // ---------- web: qualityOf / qColor ----------
    public static Color Quality(ItemDef d)
    {
        string q = "common";
        if (d == null) q = "common";
        else if (d.cosmetic) q = "cosmetic";
        else if (d.id == "burnt") q = "poor";
        else if (d.rarity == "legend" || d.id == "diamond" || d.id == "void_shard") q = "legendary";
        else if (d.rarity == "set" || d.id == "lucky_charm") q = "epic";
        else if (d.rarity == "masterwork" || Array.IndexOf(RareMats, d.id) >= 0) q = "rare";
        else if (d.rarity == "crafted" || d.meal || d.potion) q = "uncommon";
        var row = AHJson.A(AHDB.Table("items", "QUALITY"), q);
        return row != null && row.Count > 1 ? AHDB.Col(row[1], Color.white) : Color.white;
    }

    // ---------- web: canUse / whoUses (armour type and weapon type decide the class) ----------
    public static bool CanUse(string id, string cls)
    {
        var it = Get(id);
        if (it == null || !it.IsGear) return false;
        if (it.cosmetic) return true;
        if (it.slot == "weapon") return AHJson.S(AHJson.O(AHDB.Rules, "WEAPON_CLASS"), it.form ?? "") == cls;
        string armor = AHJson.S(AHJson.O(AHDB.Rules, "ARMOR_OF"), it.style ?? "", "leather");
        var who = AHJson.A(AHJson.O(AHDB.Rules, "ARMOR_CLASSES"), armor);
        return who != null && who.Contains(cls);
    }

    public static string WhoUses(string id)
    {
        var it = Get(id);
        if (it == null || !it.IsGear) return "";
        if (it.cosmetic) return "Cosmetic · all classes";
        if (it.slot == "ring" || it.slot == "amulet") return "Jewelry · all classes";
        if (it.slot == "weapon")
        {
            string c = AHJson.S(AHJson.O(AHDB.Rules, "WEAPON_CLASS"), it.form ?? "", "");
            return AHJson.S(AHJson.O(AHDB.Classes, c), "name", c) + " weapon";
        }
        string armor = AHJson.S(AHJson.O(AHDB.Rules, "ARMOR_OF"), it.style ?? "", "leather");
        return armor == "plate" ? "Plate · Warrior" : armor == "cloth" ? "Cloth · Mage, Priest and Druid" : "Leather · all classes";
    }

    // web: statLine
    public static string StatLine(ItemDef d)
    {
        if (d == null) return "";
        var parts = new List<string>();
        if (d.atk != 0) parts.Add("+" + d.atk + " attack");
        if (d.def != 0) parts.Add("+" + d.def + " armor");
        if (d.hp != 0) parts.Add("+" + d.hp + " max HP");
        if (d.dmg != 0) parts.Add("+" + Mathf.RoundToInt(d.dmg * 100) + "% damage");
        if (d.cdr != 0) parts.Add(Mathf.RoundToInt(d.cdr * 100) + "% shorter cooldowns");
        if (d.heal != 0) parts.Add("+" + Mathf.RoundToInt(d.heal * 100) + "% healing");
        return string.Join(" · ", parts.ToArray());
    }

    public static string SetName(string set) { return AHJson.S(AHJson.O(AHJson.O(AHDB.Rules, "SETS"), set), "name", set); }

    // ---------- web: money is one whole number of bronze ----------
    static readonly string[] coinName = { "diamond", "platinum", "gold", "silver", "copper", "bronze" };
    static readonly double[] coinValue = { 1e16, 1e12, 1e9, 1e6, 1e3, 1 };
    public static string MoneyText(long b, int max = 2)
    {
        if (b <= 0) return "0 bronze";
        var parts = new List<string>();
        double left = b;
        for (int i = 0; i < coinName.Length && parts.Count < max; i++)
        {
            double q = Math.Floor(left / coinValue[i]);
            if (q >= 1) { parts.Add(q.ToString("#,0") + " " + coinName[i]); left -= q * coinValue[i]; }
        }
        return string.Join(" ", parts.ToArray());
    }
}

// the hero's things: bag (28 kinds of item), worn gear, money
public class AHBag
{
    public readonly List<string> order = new List<string>();          // bag order, as items arrived
    public readonly Dictionary<string, int> inv = new Dictionary<string, int>();
    public readonly Dictionary<string, string> gear = new Dictionary<string, string>();
    public long money;                                                 // bronze
    public event Action Changed;
    public static Func<string, int, bool> CosHook;
    public string lastWarn;

    public int SlotsMax { get { return AHDB.Slots; } }
    public void Touch() { if (Changed != null) Changed(); }

    public int Count(string id) { int n; return id != null && inv.TryGetValue(id, out n) ? n : 0; }
    public int UsedSlots { get { int u = 0; foreach (var kv in inv) if (kv.Value > 0) u++; return u; } }

    // web: bagFits: room for these kinds of item (ones already in the bag need no new slot)
    public bool Fits(IEnumerable<string> ids)
    {
        var need = new HashSet<string>();
        foreach (var id in ids) { var d = AHItems.Get(id); if (d != null && !d.cosmetic && Count(id) <= 0) need.Add(id); }
        return UsedSlots + need.Count <= SlotsMax;
    }

    // web: addItem
    public bool Add(string id, int n = 1)
    {
        if (AHItems.Get(id) == null || n <= 0) return false;
        if (CosHook != null && CosHook(id, n)) { Touch(); return true; }   // cosmetics go to the Wardrobe
        if (Count(id) <= 0 && UsedSlots >= SlotsMax) { lastWarn = "Your bag is full (" + SlotsMax + " slots)."; return false; }
        inv[id] = Count(id) + n;
        if (!order.Contains(id)) order.Add(id);
        Touch();
        return true;
    }

    // web: takeItem
    public bool Take(string id, int n = 1)
    {
        if (Count(id) < n) return false;
        inv[id] -= n;
        if (inv[id] <= 0) { inv.Remove(id); order.Remove(id); }
        Touch();
        return true;
    }

    public string Worn(string slot) { string id; return gear.TryGetValue(slot, out id) ? id : null; }

    // web: equipGear. Returns a message for the player.
    public string Equip(string id, string cls)
    {
        var it = AHItems.Get(id);
        if (it == null || !it.IsGear) return null;
        if (!AHItems.CanUse(id, cls)) return "Your class can’t use " + it.name + ".";
        string old = Worn(it.slot);
        if (old != null && Count(old) <= 0 && Count(id) > 1 && UsedSlots >= SlotsMax) return "Your bag is full.";
        if (!Take(id)) return null;
        gear[it.slot] = id;
        if (old != null) Add(old);
        Touch();
        return "Equipped " + it.name + ".";
    }

    // web: unequipGear
    public string Unequip(string slot)
    {
        string id = Worn(slot);
        if (id == null) return null;
        if (Count(id) <= 0 && UsedSlots >= SlotsMax) return "Your bag is full.";
        gear.Remove(slot);
        Add(id);
        return "Took off " + AHItems.Get(id).name + ".";
    }

    // web: setCounts
    public Dictionary<string, int> SetCounts()
    {
        var n = new Dictionary<string, int>();
        foreach (var s in AHItems.GearSlots)
        {
            var d = AHItems.Get(Worn(s));
            if (d != null && d.set != null) n[d.set] = (n.ContainsKey(d.set) ? n[d.set] : 0) + 1;
        }
        return n;
    }

    // web: recalcStats (gear and set bonuses; talents, cards and gems come later)
    public AHStats Stats()
    {
        var s = new AHStats();
        foreach (var slot in AHItems.GearSlots)
        {
            var d = AHItems.Get(Worn(slot));
            if (d == null) continue;
            s.atk += d.atk; s.def += d.def; s.hp += d.hp; s.dmg += d.dmg; s.cdr += d.cdr; s.heal += d.heal;
        }
        var sets = AHJson.O(AHDB.Rules, "SETS");
        foreach (var kv in SetCounts())
        {
            var bon = AHJson.A(AHJson.O(sets, kv.Key), "bonuses");
            if (bon == null) continue;
            foreach (var b in bon)
            {
                var row = b as List<object>;
                if (row == null || row.Count < 3 || kv.Value < (double)row[0]) continue;
                var fx = row[2];
                s.hp += (int)AHJson.N(fx, "hp"); s.dmg += (float)AHJson.N(fx, "dmg"); s.cdr += (float)AHJson.N(fx, "cdr");
                s.heal += (float)AHJson.N(fx, "heal"); s.crit += (float)AHJson.N(fx, "crit");
            }
        }
        return s;
    }
}
