// Ashen Hollow: what a kill gives you, as in the web game (killMob): XP for Attack and your class,
// item drops rolled from the beast's drop table, and money straight into your purse.
// Hides and meat are not dropped: you skin the body (the action button) - see AHPlayer.Interact.
using System.Collections.Generic;
using UnityEngine;

public static class AHLoot
{
    // web killXpMult: beasts far below you give little, tougher ones a bonus
    public static float KillXpMult(int mobLvl, int classLv)
    {
        int gap = mobLvl - Mathf.Min(classLv, 52);
        if (gap <= -12) return 0.2f;
        if (gap < 0) return Mathf.Max(0.2f, 1f + gap * 0.07f);
        return 1f + Mathf.Min(gap, 10) * 0.04f;
    }

    // a random piece of a collection (draconic, lava, demonic, fossil) made for this class: <coll>_<class>_<slot>
    public static string CollectionPiece(string coll, string cls)
    {
        var pool = new List<string>(); string pre = coll + "_" + cls + "_";
        var items = AHDB.Items;
        if (items != null) foreach (var k in items.Keys) if (k.StartsWith(pre)) pool.Add(k);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    public static void OnKill(AHGame g, AHMob m, AHPlayer p)
    {
        var t = m.type;
        Vector3 at = m.transform.position;
        // the training island: a kill counts for the lesson and nothing else (no XP, loot or money)
        if (AHTraining.OnIsland)
        {
            AHTraining.OnKill(g, m);
            if (g.ui != null) { g.ui.Float(at + Vector3.up * 2.2f, "Training: no XP", new Color(0.65f, 0.9f, 1f)); g.ui.Toast(t.name + " defeated."); }
            return;
        }

        // XP (web: gainXp('attack', xp * killXpMult, 'kill'))
        int kxp = Mathf.Max(1, Mathf.RoundToInt(t.xp * KillXpMult(t.lvl, p.level)));
        p.GainXp("attack", kxp, true);
        if (g.ui != null) g.ui.Float(at + Vector3.up * 2.2f, "+" + kxp + " XP", new Color(0.65f, 0.9f, 1f));   // so you know it died

        // drops (web rollDrops: each [id, chance]): they wait on the body until you open it (or come straight to the bag
        // with auto pickup: a pet of three stars or more, or the VIP month)
        var drops = new List<string>();
        if (t.drops != null)
            foreach (var o in t.drops)
            {
                var row = o as List<object>;
                if (row == null || row.Count < 2 || !(row[0] is string)) continue;
                string id = (string)row[0];
                double ch = (double)row[1]; if (ch < 0.1) ch *= AHComp.Lucky(p);   // your pet's Lucky skill
                if (Random.value >= ch) continue;
                if (id.StartsWith("mount:") || id.StartsWith("pet:"))
                {
                    // a rare mount or pet (web: straight into your stable)
                    bool mnt = id.StartsWith("mount:"); string k = id.Substring(mnt ? 6 : 4);
                    var own = mnt ? p.mounts : p.pets;
                    if (!own.Contains(k)) { own.Add(k); if (mnt && p.mountSel == null) p.mountSel = k; if (g.ui != null) g.ui.Banner(mnt ? AHComp.MountName(k) : AHComp.PetName(k), mnt ? "Rare mount! Tap RIDE" : "A new companion! See PETS"); g.MarkDirty(); AHAnnounce.Rare(g, p, mnt ? AHComp.MountName(k) : AHComp.PetName(k), "legendary"); }
                    continue;
                }
                if (AHItems.Get(id) != null) drops.Add(id);
            }
        // recipes: bosses and the toughest beasts drop them now and then (AHRecipeDrops)
        string rec = AHRecipeDrops.Roll(g, m, p); if (rec != null) drops.Add(rec);
        // a collection boss: a piece of its armour made for your class every time, now and then a second
        if (!string.IsNullOrEmpty(t.classSet))
            for (int n = 0; n < (Random.value < 0.3f ? 2 : 1); n++)
            {
                string id = CollectionPiece(t.classSet, p.cls.id);
                if (id != null && AHItems.Get(id) != null) drops.Add(id);
            }
        // money (web: ((gold + lvl*2 + rand*lvl*2) * CU + rand*CU) bronze)
        long cu = AHDB.CU;
        long b = (long)System.Math.Round(((t.gold + t.lvl * 2 + Mathf.Floor(Random.value * t.lvl * 2)) * (double)cu + Mathf.Floor(Random.value * cu)) * AHComp.Fetch(p));
        AHComp.OnKill(g, m);
        AHEggs.OnKill(g, m);
        AHRep.OnKill(g, m);
        AHEnh.OnKill(g, m);
        AHPower.OnKill(g, m);
        AHEvo.OnKill(g, m);
        AHKQ.OnKill(m);
        AHEvents.OnKill(g, m);
        AHPass2.OnKill(p, m);   // the Battle Pass's Fight line
        if (AutoLoot(p)) { Give(g, p, drops, at); p.AddMoney(b, at); }
        else
        {
            m.loot.AddRange(drops); m.lootMoney = b; m.KeepForLoot(); AHLootGlow.Refresh(m);
        }

        if (g.ui != null) g.ui.Toast(t.name + " defeated." + (m.HasLoot ? " Tap LOOT to open the body." : t.noSkin ? "" : " Tap the big button to skin it."));
    }

    // auto pickup: a pet of three stars or more at your side, or the VIP month
    public static bool AutoLoot(AHPlayer p)
    {
        if (p == null) return false;
        if (AHVip.Active(p)) return true;
        return !string.IsNullOrEmpty(p.pet) && AHEggs.Stars(p, p.pet) >= 3;
    }

    // items into the bag (what does not fit stays behind with a word)
    public static List<string> Give(AHGame g, AHPlayer p, List<string> ids, Vector3 at)
    {
        var got = new List<string>(); var left = new List<string>();
        foreach (var id in ids)
        {
            var d = AHItems.Get(id); if (d == null) continue;
            if (p.bag.Add(id))
            {
                got.Add(d.name);
                if (!string.IsNullOrEmpty(d.set) || AHGrades.Of(id) >= 3) { if (g.ui != null) g.ui.Banner(d.name, AHItems.WhoUses(id)); }
                AHAnnounce.Item(g, p, d);
            }
            else left.Add(id);
        }
        if (left.Count > 0 && g.ui != null) g.ui.Toast(p.bag.lastWarn ?? "Your bag is full.");
        if (got.Count > 0 && g.ui != null)
        {
            g.ui.Float(at + Vector3.up * 2.8f, string.Join(" · ", got.ToArray()), new Color(1f, 0.85f, 0.42f));
            g.ui.Toast("Loot: " + string.Join(", ", got.ToArray()) + ".");
        }
        return left;
    }

    // take everything from a body (the loot window's "Take all", and auto pickup on a body killed before it)
    public static void TakeAll(AHGame g, AHPlayer p, AHMob m)
    {
        if (m == null) return;
        var left = Give(g, p, new List<string>(m.loot), m.transform.position);
        m.loot.Clear(); m.loot.AddRange(left);
        if (m.lootMoney > 0) { p.AddMoney(m.lootMoney, m.transform.position); m.lootMoney = 0; }
        m.Looted(); g.MarkDirty();
    }
    public static void TakeOne(AHGame g, AHPlayer p, AHMob m, int i)
    {
        if (m == null || i < 0 || i >= m.loot.Count) return;
        string id = m.loot[i];
        var left = Give(g, p, new List<string> { id }, m.transform.position);
        if (left.Count == 0) m.loot.RemoveAt(i);
        m.Looted(); g.MarkDirty();
    }
    public static void TakeMoney(AHGame g, AHPlayer p, AHMob m)
    {
        if (m == null || m.lootMoney <= 0) return;
        p.AddMoney(m.lootMoney, m.transform.position); m.lootMoney = 0; m.Looted();
    }
}
