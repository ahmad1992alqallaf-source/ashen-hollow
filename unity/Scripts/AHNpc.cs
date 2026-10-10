// Ashen Hollow: the townsfolk of Ashen Hollow from npcs.json (VILLAGERS with town 'ashen') and Captain Mara
// (npcs.json NPC), the quest-giver in the plaza. Web rules: talkTo cycles a villager's lines, they turn to face
// you for 4 s, wanderers stroll around home; reach is 70 units (villagers) and 75 (Mara).
// They are Quaternius townsfolk assembled like the web game's qAssemble: one outfit, a head, hair and
// eyebrows, all bound to the outfit's skeleton, with the shared animations from qAnims.
using System.Collections.Generic;
using UnityEngine;

public class AHNpc : MonoBehaviour
{
    public string npcName;
    public string[] lines = new string[0];
    public bool isCaptain, guild, agent, barber, auction, kq, market, wander;
    public string sail;   // web v.sail: this captain takes you over the water
    public float reach;            // metres
    public float radius;
    public float height = 1.8f;
    public bool Walking { get; private set; }

    AHGame g;
    AHAnim anim;
    Vector3 home, target;
    float face, wt, talkT = -99f, seqT;
    int li = -1, seqStep;
    string[] seq;

    public static readonly List<AHNpc> All = new List<AHNpc>();

    // who wears what (web NL_PEOPLE outfits; the web's own townsfolk are block figures)
    class Outfit { public string body; public string[] hair; public uint hairC; public bool hideHood; public float h = 1.8f; public string[] seq; public string[] kit; public string wm, main, off; public string beast; }
    static Outfit OutfitFor(string name)
    {
        switch (name)
        {
            case "Captain Mara": return new Outfit { body = "qFemalePeasant", hair = new[] { "Hair_Long", "Eyebrows_Female" }, hairC = 0x3a2416, h = 1.82f, seq = new[] { "Idle_FoldArms_Loop" } };
            case "Farmer Tobin": return new Outfit { body = "qMalePeasant", hair = new[] { "Hair_Buzzed", "Hair_Beard", "Eyebrows_Regular" }, hairC = 0x5a3a20, seq = new[] { "Farm_Harvest", "Farm_Watering", "Idle_FoldArms_Loop" } };
            case "Guild Clerk Mina": return new Outfit { body = "qFemalePeasant", hair = new[] { "Hair_Buns", "Eyebrows_Female" }, hairC = 0x1e1612, h = 1.72f, seq = new[] { "Idle_Loop" } };
            case "Baker Maudie": return new Outfit { body = "qFemalePeasant", hair = new[] { "Hair_Buns", "Eyebrows_Female" }, hairC = 0xc8c0b0, h = 1.68f, seq = new[] { "Idle_Loop", "Consume" } };
            case "Woodcarver Ansel": return new Outfit { body = "qMalePeasant", hair = new[] { "Hair_SimpleParted", "Hair_Beard", "Eyebrows_Regular" }, hairC = 0x6a4a2a, seq = new[] { "TreeChopping_Loop", "Idle_Loop" } };
            case "Arena crier Bess": return new Outfit { body = "qFemalePeasant", hair = new[] { "Hair_Long", "Eyebrows_Female" }, hairC = 0x8a4a22, seq = new[] { "Idle_Talking_Loop", "Yes" } };
            case "Little Tess": return new Outfit { body = "qFemalePeasant", hair = new[] { "Hair_Buns", "Eyebrows_Female" }, hairC = 0xc89a50, h = 1.2f, seq = new[] { "Idle_Loop" } };
            case "Barber Finn": return new Outfit { body = "qMalePeasant", hair = new[] { "Hair_SimpleParted", "Eyebrows_Regular" }, hairC = 0x1e1612, seq = new[] { "Idle_Loop" } };
            case "Auctioneer Wren": return new Outfit { body = "qMaleRanger", hair = new[] { "Hair_SimpleParted", "Eyebrows_Regular" }, hairC = 0x3a2416, hideHood = true, seq = new[] { "Idle_Loop", "Yes" } };
            case "Market Warden": return new Outfit { body = "qMaleRanger", hair = new[] { "Hair_Beard", "Eyebrows_Regular" }, hairC = 0x6a4a2a, seq = new[] { "Idle_Shield_Loop" } };
        }
        return null;
    }

    // ---------- everyone else: dressed for their work and their town ----------
    // kit order: head, shoulders, chest, hands, legs, feet, cape (item ids from items.json, drawn by AHWardrobe)
    static readonly string[] Women = { "Tamsin", "Ilsa", "Olga", "Nia", "Hilde", "Kessa", "Nami", "Isolde", "Ulla", "Sefa", "Inga", "Elara", "Hilda", "Mina", "Elowen", "Maudie", "Bess", "Tess", "Lyra", "Wren", "Lira", "Mara", "Maren", "witch" };
    static bool Has(string n, params string[] w) { if (n == null) return false; foreach (var x in w) if (n.IndexOf(x, System.StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; }
    static readonly uint[] HairCols = { 0x1e1612, 0x3a2416, 0x5a3a20, 0x6a4a2a, 0x8a4a22, 0xc89a50, 0x9a9088, 0xd8d0c4, 0x2a2a2a };
    static string[] Armour(string town)
    {
        switch (town)
        {
            case "varrow": return new[] { "steel_helm", "steel_pauldrons", "steel_plate", "steel_gauntlets", "steel_greaves", "steel_boots", "tabard_varrow" };
            case "highcairn": return new[] { "iron_helm", "wolf_mantle", "iron_plate", "iron_gauntlets", "iron_greaves", "iron_boots", "tabard_highcairn" };
            case "mirewatch": return new[] { "bronze_helm_mw", "bronze_pauldrons_mw", "bronze_plate_mw", "bronze_gauntlets_mw", "bronze_greaves_mw", "bronze_boots_mw", "tabard_mirewatch" };
            case "sunspire": return new[] { "bronze_helm", "sun_mantle", "bronze_plate", "bronze_gauntlets", "bronze_greaves", "bronze_boots", "tabard_sunspire" };
            case "cinderhold": return new[] { "obsidian_helm", "obsidian_pauldrons", "obsidian_plate", "obsidian_gauntlets", "obsidian_greaves", "obsidian_boots", "tabard_cinderhold" };
            case "coralport": return new[] { "tide_head", "tide_shoulders", "tide_chest", "tide_hands", "tide_legs", "tide_feet", "tabard_coralport" };
            case "ashen": return new[] { "iron_helm", "iron_pauldrons", "iron_plate", "iron_gauntlets", "iron_greaves", "iron_boots", "tabard_ashen" };
        }
        return new[] { "iron_helm", "iron_pauldrons", "iron_plate", "iron_gauntlets", "iron_greaves", "iron_boots", "wool_cloak" };
    }
    static Outfit Dressed(string name, object raw)
    {
        var o = OutfitFor(name);
        string town = raw != null ? AHJson.S(raw, "town") : "ashen";
        int hsh = 17; foreach (char c in name) hsh = hsh * 31 + c; if (raw != null) hsh = hsh * 7 + (int)AHJson.N(raw, "x");
        hsh &= 0x7fffffff;
        bool woman = false; foreach (var w in Women) if (name.Contains(w)) woman = true;
        if (name == "Gate guard" || name == "Market Warden") woman = hsh % 3 == 0;
        bool guard = raw != null && AHJson.B(raw, "guard");
        if (o == null)
        {
            o = new Outfit { body = woman ? "qFemalePeasant" : "qMalePeasant", h = (woman ? 1.68f : 1.76f) + (hsh % 9) * 0.012f, seq = new[] { "Idle_Loop" } };
            string[] mh = { "Hair_SimpleParted", "Hair_Buzzed", "Hair_Long" }, fh = { "Hair_Long", "Hair_Buns" };
            var hair = new List<string> { woman ? fh[hsh % 2] : mh[hsh % 3] };
            if (!woman && hsh % 5 < 2) hair.Add("Hair_Beard");
            hair.Add(woman ? "Eyebrows_Female" : "Eyebrows_Regular");
            o.hair = hair.ToArray(); o.hairC = HairCols[(hsh / 7) % HairCols.Length];
        }
        string[] K = null;
        if (Has(name, "cat")) { o.beast = "b_cat"; return o; }
        if (name == "Captain Mara") { K = Armour("ashen"); K[0] = null; o.body = "qFemalePeasant"; o.wm = "Knight_t"; o.main = "1H_Sword"; o.kit = K; o.seq = new[] { "Sword_Idle", "Idle_Loop" }; return o; }
        if (Has(name, "vizier")) { K = new[] { "dunescroll_head", "sun_mantle", "dunescroll_chest", "dunescroll_hands", null, "silk_boots", "desert_veil" }; o.seq = new[] { "Idle_FoldArms_Loop" }; }
        else if (guard || Has(name, "Guard", "Sergeant", "Gate warden", "Road warden"))
        {
            K = Armour(town); o.body = woman ? "qFemalePeasant" : "qMaleRanger"; o.hideHood = true; o.wm = "Knight_t";
            if (Has(name, "Sergeant")) { o.main = "2H_Sword"; K[0] = "royal_helm"; } else { o.main = "1H_Sword"; o.off = "Round_Shield"; }
            if (Has(name, "Road warden")) { K[0] = "wolf_hood"; K[6] = "wool_cloak"; }
            o.seq = new[] { "Sword_Idle", "Idle_Shield_Loop" };
        }
        else if (name == "Market Warden") { K = new[] { "guild_helm", "kf_warrior_shoulders", "guild_plate", "iron_gauntlets", null, "iron_boots", Has(town, "ashen") ? "tabard_ashen" : "tabard_" + town }; o.wm = "Knight_t"; o.main = "1H_Sword"; o.off = "Round_Shield"; }
        else if (Has(name, "Captain") && raw != null) { K = new[] { "captain_hat", null, "captain_coat", "boar_gloves", null, "corsair_boots", "captain_cape" }; o.body = woman ? "qFemalePeasant" : "qMaleRanger"; o.hideHood = true; o.wm = "Knight_t"; o.main = "1H_Sword"; o.seq = new[] { "Idle_FoldArms_Loop", "Idle_Loop" }; }
        else if (Has(name, "Ferryman", "Bargeman", "Old salt", "Dockhand", "Fisher")) { K = new[] { hsh % 2 == 0 ? "corsair_hat" : null, null, hsh % 2 == 0 ? "corsair_coat" : "hide_jerkin", null, null, "corsair_boots", null }; o.seq = new[] { "Idle_Loop", "Interact" }; }
        else if (Has(name, "Pearl diver")) K = new[] { "seasilk_hood", null, "seasilk_robe", null, null, null, "reef_cape" };
        else if (Has(name, "Castaway")) K = new[] { null, null, "linen_robe", null, null, null, null };
        else if (Has(name, "Caravan")) { K = new[] { "duneshot_head", null, "duneshot_chest", null, null, "deer_boots", "desert_veil" }; o.seq = new[] { "Idle_Talking_Loop", "Idle_Loop" }; }
        else if (Has(name, "Merchant", "Auctioneer")) { K = new[] { "harvest_hat", "silk_mantle", "silk_robe", null, null, "silk_boots", null }; o.seq = new[] { "Idle_Talking_Loop", "Yes", "Idle_Loop" }; }
        else if (Has(name, "Jarl")) { K = new[] { "gold_crown", "wolf_mantle", "iron_plate", "iron_gauntlets", null, "iron_boots", "frost_cloak" }; o.body = "qMaleRanger"; o.hideHood = true; o.h = 1.92f; o.wm = "Barbarian_t"; o.main = "2H_Axe"; o.seq = new[] { "Idle_FoldArms_Loop" }; }
        else if (Has(name, "Skald")) { K = new[] { "wolf_hood", "wolf_mantle", "hide_jerkin", null, null, "deer_boots", "frost_cloak" }; o.seq = new[] { "Idle_Talking_Loop", "Dance_Loop" }; }
        else if (Has(name, "Elder")) { K = new[] { null, "wolf_mantle", "linen_robe", null, null, null, "wool_cloak" }; o.seq = new[] { "Idle_FoldArms_Loop" }; }
        else if (Has(name, "Archivist", "Registrar", "Clerk")) { K = new[] { null, null, "linen_robe", null, null, null, "guild_cape" }; }
        else if (Has(name, "Guildmaster")) { K = new[] { "guild_helm", "kf_warrior_shoulders", "guild_plate", "iron_gauntlets", "iron_greaves", "iron_boots", "guild_cape" }; o.body = "qMaleRanger"; o.hideHood = true; o.wm = "Knight_t"; o.main = "2H_Sword"; o.seq = new[] { "Idle_FoldArms_Loop" }; }
        else if (Has(name, "Trial-master")) { K = new[] { "bronze_helm", "bronze_pauldrons", "bronze_plate", "bronze_gauntlets", "bronze_greaves", "bronze_boots", "kf_warrior_cape" }; o.body = "qMaleRanger"; o.hideHood = true; o.wm = "Barbarian_t"; o.main = "2H_Axe"; o.seq = new[] { "Idle_FoldArms_Loop" }; }
        else if (Has(name, "Herald")) { K = new[] { "harvest_hat", null, "leather_vest", null, null, "royal_boots", "tabard_varrow" }; o.seq = new[] { "Idle_Talking_Loop" }; }
        else if (Has(name, "Bard")) { K = new[] { null, "bloom_mantle", "silk_robe", null, null, "silk_boots", "royal_cape" }; o.seq = new[] { "Dance_Loop", "Idle_Talking_Loop" }; }
        else if (Has(name, "Ash-seer")) { K = new[] { "cinder_hood", "cinder_mantle", "cinder_robe", "cinder_gloves", null, "cinder_boots", "ash_mantle" }; o.seq = new[] { "Spell_Simple_Idle_Loop" }; }
        else if (Has(name, "witch")) { K = new[] { "mirecall_head", null, "mirecall_chest", "mirecall_hands", null, null, null }; o.wm = "Mage_t"; o.main = "1H_Wand"; o.seq = new[] { "Spell_Simple_Idle_Loop", "Idle_Loop" }; }
        else if (Has(name, "Gravewarden")) { K = new[] { "shade_cowl", null, "holy_robe", "holy_gloves", null, "holy_boots", "holy_cape" }; o.seq = new[] { "Idle_Lantern_Loop" }; }
        else if (Has(name, "Lantern")) { K = new[] { "lantern_hood", null, "hide_jerkin", null, null, "deer_boots", "wool_cloak" }; o.seq = new[] { "Idle_Lantern_Loop" }; }
        else if (Has(name, "Guide")) { K = new[] { "verdant_hood", "verdant_mantle", "verdant_vest", "verdant_gloves", null, "verdant_boots", "verdant_quiver" }; o.hideHood = true; }
        else if (Has(name, "Snake charmer")) { K = new[] { "dunescroll_head", null, "oasis_chest", null, null, null, null }; o.seq = new[] { "Sitting_Idle_Loop" }; }
        else if (Has(name, "Forge-master")) { K = new[] { null, null, "leather_vest", "forge_gauntlets", null, "forge_boots", null }; o.wm = "Barbarian_t"; o.main = "1H_Axe"; o.seq = new[] { "Interact", "Idle_FoldArms_Loop" }; }
        else if (Has(name, "Digmaster")) { K = new[] { "iron_helm", null, "leather_vest", "boar_gloves", null, "deer_boots", null }; o.seq = new[] { "Farm_Harvest", "Idle_Loop" }; }
        else if (Has(name, "Trapper")) { K = new[] { "wolf_hood", "wolf_mantle", "hide_jerkin", "boar_gloves", null, "deer_boots", "wolf_cloak" }; o.wm = "Rogue_t"; o.main = "2H_Crossbow"; }
        else if (Has(name, "Scout", "Ash-walker")) { K = Has(name, "Ash") ? new[] { "ashwood_head", "ashwood_shoulders", "ashwood_chest", "ashwood_hands", null, "ashwood_feet", "ashwood_cape" } : new[] { "frosthunt_head", null, "frosthunt_chest", "frosthunt_hands", null, "deer_boots", "verdant_quiver" }; o.hideHood = true; o.wm = "Rogue_t"; o.main = "2H_Crossbow"; }
        else if (Has(name, "pilgrim")) { K = new[] { "cotton_hood", null, "linen_robe", null, null, null, "wool_cloak" }; o.seq = new[] { "Idle_Loop" }; }
        else if (Has(name, "Farmer", "Peat cutter")) { K = new[] { Has(name, "Peat") ? "winter_hat" : "harvest_hat", null, "hide_jerkin", "boar_gloves", null, "deer_boots", null }; }
        else if (Has(name, "Woodcarver")) K = new[] { null, null, "leather_vest", "boar_gloves", null, "deer_boots", null };
        else if (Has(name, "Land Agent")) K = new[] { "harvest_hat", null, "silk_robe", null, null, "silk_boots", null };
        else if (Has(name, "Barber")) K = new[] { null, null, "linen_robe", null, null, null, null };
        else if (Has(name, "Baker")) K = new[] { null, null, "linen_robe", null, null, null, null };
        o.kit = K;
        // someone with a one-handed weapon does not fold their arms (the blade would lie across their belly)
        if (o.main != null && o.main.StartsWith("1H") && o.seq != null)
            for (int i = 0; i < o.seq.Length; i++) if (o.seq[i] == "Idle_FoldArms_Loop") o.seq[i] = "Sword_Idle";
        return o;
    }

    // ---------- the town ----------
    // the area models still hold the old box-built people the web game drew (a body, a head, a spear, all little
    // blocks). Our real townsfolk now stand on the same spots, so the two overlapped: hide the old figure wherever a
    // townsperson stands.
    public static void HideOldFigures(AHGame game)
    {
        var w = game.World; if (w == null || All.Count == 0) return;
        int n = 0;
        foreach (var r in w.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled) continue;
            var b = r.bounds;
            if (b.size.y > 2.4f || Mathf.Max(b.size.x, b.size.z) > 1.4f) continue;   // only person-sized bits
            foreach (var p in All)
            {
                if (p == null) continue;
                Vector3 d = b.center - p.home; d.y = 0f;
                if (d.magnitude < 0.75f && b.min.y < p.home.y + 2.3f) { r.enabled = false; n++; break; }
            }
        }
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " old box townsfolk pieces hidden");
    }

    public static void SpawnTown(AHGame game)
    {
        All.Clear();
        if (AHGame.AreaId == AHDeep.Area) return;   // the Deep's map overlaps a town's on the world grid: no townsfolk down there
        var npc = AHDB.Table("npcs", "NPC");
        if (npc != null && game.InArea(game.W((float)AHJson.N(npc, "x") * AHDB.S, (float)AHJson.N(npc, "y") * AHDB.S)))
        {
            var m = Create(game, AHJson.S(npc, "name", "Captain Mara"), (float)AHJson.N(npc, "x"), (float)AHJson.N(npc, "y"), Mathf.PI / 2f, (float)AHJson.N(npc, "r", 16));
            m.isCaptain = true; m.reach = 75f * AHDB.S;
            m.lines = new[] { "Ashen Hollow is safer with you here, hero." };
        }
        var vs = AHDB.List("npcs", "VILLAGERS");
        if (vs == null) return;
        foreach (var o in vs)
        {
            if (!game.InArea(game.W((float)AHJson.N(o, "x") * AHDB.S, (float)AHJson.N(o, "y") * AHDB.S))) continue;
            var v = Create(game, AHJson.S(o, "name", "Villager"), (float)AHJson.N(o, "x"), (float)AHJson.N(o, "y"), (float)AHJson.N(o, "face", Mathf.PI / 2f), (float)AHJson.N(o, "r", 14), o);
            var ls = AHJson.A(o, "lines");
            if (ls != null) { v.lines = new string[ls.Count]; for (int i = 0; i < ls.Count; i++) v.lines[i] = ls[i] as string; }
            v.guild = AHJson.B(o, "guild"); v.agent = AHJson.B(o, "agent"); v.barber = AHJson.B(o, "barber");
            v.auction = AHJson.B(o, "auction"); v.kq = AHJson.B(o, "kq"); v.wander = AHJson.B(o, "wander");
            v.market = AHJson.Has(o, "mk");
            v.sail = AHJson.S(o, "sail");
            if (v.sail != null && !AHJson.Has(AHJson.O(AHDB.Rules, "SAIL"), v.sail)) v.sail = null;
            v.reach = 70f * AHDB.S;
        }
    }

    // web x, y in units
    static AHNpc Create(AHGame game, string name, float x, float y, float face, float r, object raw = null)
    {
        Vector3 pos = game.W(x * AHDB.S, y * AHDB.S);
        var go = new GameObject(name);
        go.transform.position = pos;
        var n = go.AddComponent<AHNpc>();
        n.g = game; n.npcName = name; n.home = pos; n.target = pos; n.face = face; n.radius = r * AHDB.S;
        go.transform.rotation = game.Face(n.WebDir(face));
        var o = Dressed(name, raw);
        n.seq = o.seq; n.height = o.h;
        GameObject model = null;
        if (o.beast != null)
        {
            // an animal among the townsfolk (Granny Wormwood's cat)
            model = AHModel.Spawn(go.transform, "Beasts/" + o.beast, 0.75f, true, 0f, out n.anim);
            if (n.anim != null) n.anim.alias = new Dictionary<string, string> { { "Walk_Loop", "Walk" }, { "Idle_Talking_Loop", "Idle" }, { "Idle_Loop", "Idle" } };
            n.seq = new[] { "Idle", "Idle_2", "Eating" }; n.height = 0.5f;
        }
        else
        try
        {
            model = Assemble(go.transform, o, game, out n.anim);
            // a whole outfit made for their trade (Resources/AH/Models/Outfits/tqo_npc_<role>), head to toe, in place of
            // the body and the loose kit; only what they carry stays
            string suit = model != null ? Suit(name, raw, o) : null;
            bool suited = false;
            if (suit != null && AHQOutfit.Wear(model, suit) != null)
            {
                suited = true;
                foreach (var rr in model.GetComponentsInChildren<Renderer>(true))
                {
                    bool keep = false; for (var p = rr.transform; p != null && p != model.transform; p = p.parent) if (p.name.StartsWith("Weapon_")) { keep = true; break; }
                    if (!keep) rr.enabled = false;
                }
            }
            // their clothes and gear, drawn the same way as the hero's (AHWardrobe) and the sellswords' weapons
            if (model != null && o.kit != null && !suited)
            {
                var kit = o.kit; string[] slots = AHWardrobe.Slots;
                AHWardrobe.DressWith(model, go.transform, sl => { int i = System.Array.IndexOf(slots, sl); return i >= 0 && i < kit.Length && kit[i] != null && AHItems.Get(kit[i]) != null ? kit[i] : null; }, () => n.Walking);
            }
            if (model != null && o.wm != null) AHPeople.Arm(model, o.wm, o.main, o.off, game);
        }
        catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: could not dress " + name + " (" + e.Message + ")"); }
        if (model == null) model = AHModel.Spawn(go.transform, "Rogue_t", o.h, false, game.heroYawFix, out n.anim);
        AHModel.SetShadows(model);
        All.Add(n);
        return n;
    }

    // which trade's outfit a townsperson wears (null: their own look, kept for the named characters)
    static string Suit(string name, object raw, Outfit o)
    {
        if (name == "Captain Mara" || Has(name, "Jarl", "Guildmaster", "Trial-master", "witch", "Skald", "Scout", "Ash-walker", "Guide", "Snake charmer", "vizier", "Little Tess")) return null;
        bool woman = o.body != null && o.body.Contains("Female");
        bool guard = raw != null && AHJson.B(raw, "guard");
        string r;
        if (guard || Has(name, "Guard", "Sergeant", "Gate warden", "Road warden", "Market Warden")) r = woman ? "guard_f" : "guard";
        else if (woman) r = "villager_f";
        else if (Has(name, "Merchant", "Auctioneer", "Land Agent", "Caravan")) r = "merchant";
        else if (Has(name, "Farmer", "Peat cutter", "Woodcarver", "Digmaster", "Trapper", "Forge-master")) r = "farmer";
        else if (Has(name, "Ferryman", "Bargeman", "Old salt", "Dockhand", "Fisher", "Pearl diver", "Castaway") || (Has(name, "Captain") && raw != null)) r = "sailor";
        else if (Has(name, "Archivist", "Registrar", "Clerk", "Elder", "pilgrim", "Herald", "Bard", "Ash-seer", "Gravewarden", "Lantern", "Barber")) r = "scholar";
        else r = "villager_m";
        string path = "tqo_npc_" + r;
        return Resources.Load<GameObject>("AH/Models/Outfits/" + path) != null ? path : null;
    }

    Vector3 WebDir(float a) { return g.W(Mathf.Cos(a), Mathf.Sin(a)) - g.W(0f, 0f); }

    // web qAssemble: outfit + head + hair/eyebrows on the outfit's skeleton
    static GameObject Assemble(Transform holder, Outfit o, AHGame game, out AHAnim anim)
    {
        anim = null;
        var bodyPrefab = Resources.Load<GameObject>("AH/Models/Web/" + o.body);
        if (bodyPrefab == null) { Debug.LogWarning("Ashen Hollow: model AH/Models/Web/" + o.body + " not found"); return null; }
        // the clips in qAnims are keyed from above the skeleton ("Armature/root/..."), so the outfit's
        // Armature goes inside a rig object that carries the animator
        var rig = new GameObject("Rig");
        rig.transform.SetParent(holder, false);
        rig.transform.localRotation = Quaternion.Euler(0, game.ModelYaw + game.heroYawFix, 0);
        var inst = Object.Instantiate(bodyPrefab, rig.transform, false);
        inst.name = "Armature";
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);

        // every bone of the outfit's skeleton, by name
        var bones = new Dictionary<string, Transform>();
        foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(t.name)) bones[t.name] = t;
        Transform host = null;
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { host = smr.transform.parent; break; }
        if (host == null) host = inst.transform;

        var parts = new List<string> { "qHead_" + (o.body.Contains("Female") ? "Female" : "Male") };
        foreach (var h in o.hair) parts.Add("q" + h);
        foreach (var part in parts)
        {
            var pf = Resources.Load<GameObject>("AH/Models/Web/" + part);
            if (pf == null) continue;
            var pi = Object.Instantiate(pf);
            foreach (var smr in pi.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var nb = smr.bones;
                for (int i = 0; i < nb.Length; i++) { Transform t; if (nb[i] != null && bones.TryGetValue(nb[i].name, out t)) nb[i] = t; }
                Transform rb = null;
                if (smr.rootBone != null) bones.TryGetValue(smr.rootBone.name, out rb);
                Vector3 lp = smr.transform.localPosition; Quaternion lr = smr.transform.localRotation; Vector3 ls = smr.transform.localScale;
                smr.transform.SetParent(host, false);
                smr.transform.localPosition = lp; smr.transform.localRotation = lr; smr.transform.localScale = ls;
                smr.bones = nb;
                if (rb != null) smr.rootBone = rb;
            }
            Object.Destroy(pi);
        }

        Color hc = new Color(((o.hairC >> 16) & 255) / 255f, ((o.hairC >> 8) & 255) / 255f, (o.hairC & 255) / 255f);
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            if (o.hideHood && r.name.Contains("Hood")) { r.enabled = false; continue; }
            var smr = r as SkinnedMeshRenderer; if (smr != null) smr.updateWhenOffscreen = true;
            var mats = r.materials; bool changed = false;
            foreach (var m in mats)
            {
                if (m == null || !m.name.Contains("Hair")) continue;
                if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", hc);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hc);
                changed = true;
            }
            if (changed) r.materials = mats;
        }

        // size and feet on the ground
        var rs = new List<Renderer>();
        foreach (var r in inst.GetComponentsInChildren<Renderer>(false)) if (r.enabled) rs.Add(r);
        if (rs.Count > 0)
        {
            Bounds b = rs[0].bounds; for (int i = 1; i < rs.Count; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y > 1e-3f) rig.transform.localScale = Vector3.one * (o.h / b.size.y);
            b = rs[0].bounds; for (int i = 1; i < rs.Count; i++) b.Encapsulate(rs[i].bounds);
            rig.transform.position += Vector3.up * (holder.position.y - b.min.y);
        }
        var clips = Resources.LoadAll<AnimationClip>("AH/Models/Web/qAnims");
        if (clips.Length == 0) Debug.LogWarning("Ashen Hollow: no animations found in AH/Models/Web/qAnims");
        anim = new AHAnim(rig, clips);
        return rig;
    }

    void OnDestroy() { All.Remove(this); if (anim != null) anim.Dispose(); }

    public float DistTo(Vector3 p) { Vector3 d = p - transform.position; d.y = 0; return d.magnitude; }

    // what the action button says next to them (web labelFor 'villager' / 'npc')
    public string Label
    {
        get
        {
            if (isCaptain) return "Talk";
            if (sail != null) return "Sail";
            if (market) return "Market"; if (kq) return "Kingdom Quests"; if (agent) return "Land & seeds";
            if (guild) return "Guild"; if (auction) return "Auction"; if (barber) return "Barber";
            return "Talk";
        }
    }

    // web talkTo: the next line, said in the chat; they look at you for 4 s
    public string Talk()
    {
        talkT = Time.time + 4f;
        if (lines.Length == 0) return null;
        li = (li + 1) % lines.Length;
        return npcName + ": “" + lines[li] + "”";
    }

    void Update()
    {
        if (g == null || g.player == null) return;
        float dt = Time.deltaTime;
        if (anim != null) anim.Tick(dt);
        Vector3 pp = g.player.transform.position;
        float pd = DistTo(pp);
        bool near = talkT > Time.time || pd < 90f * AHDB.S;
        bool moving = false;
        Vector3 want = WebDir(face);
        if (near) want = pp - transform.position;
        else if (wander)
        {
            // web: a new spot within 150 units of home every 3–7 s, walked at 55 units a second
            wt -= dt;
            if (wt <= 0f) { wt = 3f + Random.value * 4f; float a = Random.value * 6.28f, r = Random.value * 150f * AHDB.S; target = home + WebDir(a).normalized * r; }
            Vector3 to = target - transform.position; to.y = 0f;
            if (to.magnitude > 8f * AHDB.S && wt < 2.6f)
            {
                Vector3 np = transform.position + to.normalized * 55f * AHDB.S * dt;
                if (!g.Blocked(np, radius)) { transform.position = np; moving = true; want = to; }
                else wt = 0f;
            }
        }
        want.y = 0f;
        if (want.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(want), 1f - Mathf.Exp(-dt * 6f));

        Walking = moving;
        if (anim == null) return;
        if (moving) { anim.Play("Walk_Loop", true); return; }
        if (near) { anim.Play("Idle_Talking_Loop", true); return; }
        seqT += dt;
        if (seqT > 4.5f) { seqT = 0f; seqStep++; }
        if (seq == null || seq.Length == 0 || !anim.Play(seq[seqStep % seq.Length], true)) anim.Play("Idle_Loop", true);
    }

    // the one standing nearest the hero within reach, or null
    public static AHNpc Nearest(Vector3 p)
    {
        AHNpc best = null; float bd = float.MaxValue;
        // closest person in reach, but the one you face and the one with your quest come first
        var g = AHGame.I; var pl = g != null ? g.player : null;
        Vector3 fwd = pl != null ? pl.transform.forward : Vector3.zero; fwd.y = 0;
        string qn = g != null && g.quests != null ? g.quests.NpcName : null;
        foreach (var n in All)
        {
            float d = n.DistTo(p); if (d >= n.reach) continue;
            float score = d;
            Vector3 to = n.transform.position - p; to.y = 0;
            if (fwd.sqrMagnitude > 0.01f && to.sqrMagnitude > 0.01f && Vector3.Dot(fwd.normalized, to.normalized) < 0.3f) score *= 1.8f;
            if (qn != null && n.npcName == qn) score *= 0.6f;
            if (score < bd) { bd = score; best = n; }
        }
        return best;
    }

    public static AHNpc Find(string name)
    {
        foreach (var n in All) if (n.npcName == name) return n;
        return null;
    }
}
