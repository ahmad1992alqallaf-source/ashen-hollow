// Ashen Hollow: saves the hero between plays as JSON in PlayerPrefs: class, each class's XP, skills, money,
// bag (in order), worn gear, hunger and quests. Version 2 follows the web game's structures (v67).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHStack { public string id; public int n; }
[Serializable] public class AHKV { public string k; public long v; }
[Serializable] public class AHKS { public string k, v; }

[Serializable]
public class AHSaveData
{
    public int version = 2;
    public string cls;
    public long money;
    public float hunger = 100f;
    public List<AHKV> classXp = new List<AHKV>(), banked = new List<AHKV>(), skillXp = new List<AHKV>();
    public List<int> trials = new List<int>();
    public List<AHStack> inv = new List<AHStack>();
    public List<AHKS> gear = new List<AHKS>();
    public string name = "", path = "adventurer";
    public int peace = -1;
    public List<string> profs = new List<string>();
    public string profMain;
    public List<AHKV> profXp = new List<AHKV>();
    public List<AHKS> profSpec = new List<AHKS>();
    public AHLook look = new AHLook();
    public int questI, roadI, roadP;
    public string questState = "offer";
    public List<int> questProg = new List<int>();
    public bool hasPos;
    public string area = "meadow";
    public float x, z, hp = -1f;    // where you stood (web metres) and your health, as the web save keeps P.x, P.y and P.hp
    public long t;                  // when it was saved (unix ms)
    // companions and the bank vault
    public List<string> pets = new List<string>(), mounts = new List<string>(), party = new List<string>();
    public string pet, mountSel;
    public List<AHKV> petXp = new List<AHKV>(), mountXp = new List<AHKV>();
    public List<AHStack> bank = new List<AHStack>();
    public List<AHOrder> orders = new List<AHOrder>();
    public int guildRep, ordersDone;
    public List<AHKV> bought = new List<AHKV>();
    public bool hasHome; public AHHomeState home = new AHHomeState(); public long rested; public bool hasMeal; public AHMealState meal = new AHMealState(); public AHProgress prog = new AHProgress();
}

public static class AHSave
{
    public const string Key = "ah_save";
    public const int Version = 2;

    public static bool Exists { get { return AHPrefs.HasKey(Key); } }

    static List<AHKV> Pack(Dictionary<string, long> d) { var l = new List<AHKV>(); foreach (var kv in d) l.Add(new AHKV { k = kv.Key, v = kv.Value }); return l; }
    static void Unpack(List<AHKV> l, Dictionary<string, long> d) { d.Clear(); if (l != null) foreach (var kv in l) if (!string.IsNullOrEmpty(kv.k)) d[kv.k] = Math.Max(0, kv.v); }

    public static void Save(AHGame g)
    {
        var p = g.player;
        if (p == null || p.cls == null || AHPrefs.Locked) return;
        // a brand-new hero who has not picked a class yet: nothing to keep
        if (!AHPrefs.HasKey("ah_class") && g.ui != null && (g.ui.PickerOpen || g.ui.CreatorOpen)) return;
        var d = new AHSaveData
        {
            cls = p.cls.id, money = p.bag.money, hunger = p.hunger,
            name = p.heroName ?? "", path = p.path, peace = p.peace, profs = new List<string>(p.profs), look = p.look,
            classXp = Pack(p.classXp), banked = Pack(p.banked), skillXp = Pack(p.skillXp), trials = new List<int>(p.trialsDone),
            questI = g.quests.i, questState = g.quests.state, questProg = new List<int>(g.quests.prog ?? new int[0]),
            roadI = g.quests.roadI, roadP = g.quests.roadP,
            hasPos = !p.dead, area = AHGame.AreaId, hp = p.dead ? -1f : p.hp, t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        d.profMain = p.profMain; d.profXp = Pack(p.profXp);
        foreach (var kv in p.profSpec) d.profSpec.Add(new AHKS { k = kv.Key, v = kv.Value });
        if (!p.dead) { var w = g.ToWeb(p.transform.position); d.x = w.x; d.z = w.y; }
        foreach (var id in p.bag.order) d.inv.Add(new AHStack { id = id, n = p.bag.Count(id) });
        foreach (var kv in p.bag.gear) d.gear.Add(new AHKS { k = kv.Key, v = kv.Value });
        d.pets = new List<string>(p.pets); d.mounts = new List<string>(p.mounts); d.party = new List<string>(p.party);
        d.pet = p.pet; d.mountSel = p.mountSel; d.petXp = Pack(p.petXp); d.mountXp = Pack(p.mountXp);
        foreach (var kv in p.bank) if (kv.Value > 0) d.bank.Add(new AHStack { id = kv.Key, n = kv.Value });
        d.orders = p.orders; d.guildRep = p.guildRep; d.ordersDone = p.ordersDone; d.bought = Pack(p.bought);
        d.hasHome = p.home != null; if (p.home != null) d.home = p.home; d.rested = p.rested; d.hasMeal = p.meal != null; if (p.meal != null) d.meal = p.meal; d.prog = p.prog;
        AHPrefs.SetString(Key, JsonUtility.ToJson(d));
        AHPrefs.SetString("ah_class", d.cls);
        AHPrefs.WriteMeta(p); AHPrefs.Register();
        AHPrefs.Save();
    }

    // puts a saved hero back; false if there is no usable save (a new hero)
    public static bool Load(AHGame g)
    {
        if (!Exists) return false;
        AHSaveData d;
        try { d = JsonUtility.FromJson<AHSaveData>(AHPrefs.GetString(Key)); }
        catch (Exception e) { Debug.LogWarning("Ashen Hollow: could not read the save (" + e.Message + "), starting fresh"); return false; }
        if (d == null || d.version != Version) { Debug.Log("Ashen Hollow: an older save from before the web data; starting a fresh hero"); return false; }

        var p = g.player;
        Unpack(d.classXp, p.classXp); Unpack(d.banked, p.banked); Unpack(d.skillXp, p.skillXp);
        p.trialsDone.Clear(); if (d.trials != null) p.trialsDone.AddRange(d.trials);
        var bag = p.bag;
        bag.money = Math.Max(0, d.money);
        bag.inv.Clear(); bag.order.Clear(); bag.gear.Clear();
        if (d.inv != null) foreach (var s in d.inv) if (s != null && s.n > 0 && AHItems.Get(s.id) != null) { bag.inv[s.id] = s.n; bag.order.Add(s.id); }
        if (d.gear != null) foreach (var s in d.gear) { var it = AHItems.Get(s.v); if (it != null && it.slot == s.k) bag.gear[s.k] = s.v; }
        p.hunger = Mathf.Clamp(d.hunger, 0f, 100f);
        p.heroName = d.name ?? ""; p.path = d.path == "artisan" ? "artisan" : "adventurer"; p.peace = d.peace;
        p.profs.Clear(); if (d.profs != null) foreach (var k in d.profs) if (AHJson.Has(AHPlayer.Profs, k) && !p.profs.Contains(k)) p.profs.Add(k);
        p.profMain = d.profMain != null && p.profs.Contains(d.profMain) ? d.profMain : (p.profs.Count > 0 ? p.profs[0] : null);
        Unpack(d.profXp, p.profXp);
        p.profSpec.Clear(); if (d.profSpec != null) foreach (var kv in d.profSpec) if (!string.IsNullOrEmpty(kv.k)) p.profSpec[kv.k] = kv.v;
        p.look = d.look ?? new AHLook();
        p.pets.Clear(); if (d.pets != null) foreach (var k in d.pets) if (AHJson.Has(AHComp.Pets, k) && !p.pets.Contains(k)) p.pets.Add(k);
        p.mounts.Clear(); if (d.mounts != null) foreach (var k in d.mounts) if (AHJson.Has(AHComp.Mounts, k) && !p.mounts.Contains(k)) p.mounts.Add(k);
        p.party.Clear(); if (d.party != null) foreach (var k in d.party) if (AHJson.Has(AHComp.Mercs, k) && !p.party.Contains(k) && p.party.Count < AHComp.PartyMax) p.party.Add(k);
        p.pet = d.pet != null && p.pets.Contains(d.pet) ? d.pet : null;
        p.mountSel = d.mountSel != null && p.mounts.Contains(d.mountSel) ? d.mountSel : (p.mounts.Count > 0 ? p.mounts[0] : null);
        Unpack(d.petXp, p.petXp); Unpack(d.mountXp, p.mountXp);
        p.orders = d.orders ?? new List<AHOrder>(); p.orders.RemoveAll(o => o == null || o.items == null || o.items.Count == 0);
        p.guildRep = Mathf.Max(0, d.guildRep); p.ordersDone = d.ordersDone; Unpack(d.bought, p.bought);
        p.home = d.hasHome && d.home != null ? d.home : null; p.rested = Math.Max(0, d.rested); p.meal = d.hasMeal && d.meal != null && d.meal.until > AHMeal.Now ? d.meal : null; p.prog = d.prog ?? new AHProgress(); p.bag.extra = p.prog.bagRows * 9;
        p.bank.Clear(); if (d.bank != null) foreach (var s in d.bank) if (s != null && s.n > 0 && AHItems.Get(s.id) != null) p.bank[s.id] = s.n;
        var log = g.quests;
        log.i = Mathf.Clamp(d.questI, 0, AHQuests.All.Count);
        log.state = d.questState == "active" || d.questState == "ready" ? d.questState : "offer";
        log.prog = d.questProg != null ? d.questProg.ToArray() : new int[0];
        var cq = log.Current;
        if (cq != null && log.state != "offer" && log.prog.Length != cq.obj.Count) System.Array.Resize(ref log.prog, cq.obj.Count);
        log.roadI = Mathf.Max(0, d.roadI); log.roadP = Mathf.Max(0, d.roadP);
        p.SetClass(p.cls.id);   // web: anything this class can't use goes back to the bag
        p.Recalc();
        p.hp = d.hp > 0f ? Mathf.Min(d.hp, p.maxHp) : p.maxHp; p.mana = p.maxMana;
        // back where you left off, unless that spot is now inside something (web savePos)
        if (d.hasPos && (string.IsNullOrEmpty(d.area) ? "meadow" : d.area) == AHGame.AreaId)
        {
            Vector3 at = g.W(d.x, d.z);
            if (g.InArea(at) && !g.Blocked(at, 0.3f)) p.transform.position = at;
        }
        bag.Touch();
        return true;
    }

    public static void Clear()
    {
        AHTutorial.ClearAll();
        AHPrefs.DeleteKey(Key);
        AHPrefs.DeleteKey("ah_class");
        AHPrefs.DeleteKey("ah_area");
        AHDungeon.ResetAll();
        AHAuction.Clear();
        AHPrefs.DeleteKey("ah_level");
        AHPrefs.DeleteKey("ah_xp");
        // the rest of a hero's story: lands seen, treasure X spots and digs, the Fossil Lands gift, the raid lock,
        // the dungeon finder and the kingdom quests (sound settings stay)
        foreach (var k in new[] { "ah_seen", "ah_digs", "ah_fossil_dig", "ah_fossil_dug", "ah_fossil_gift", "ah_raid_from", "ah_raid_week", "ah_df", "ah_df_role", "ah_df_runs", "ah_kq" })
            AHPrefs.DeleteKey(k);
        foreach (var a in new[] { "meadow", "silkwood", "mire", "vale", "frost", "sands", "isle", "tide", "ember", "fossil" }) AHPrefs.DeleteKey("ah_dig_" + a);
        AHPrefs.Save();
    }
}
