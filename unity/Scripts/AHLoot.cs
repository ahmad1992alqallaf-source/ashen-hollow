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

    public static void OnKill(AHGame g, AHMob m, AHPlayer p)
    {
        var t = m.type;
        Vector3 at = m.transform.position;

        // XP (web: gainXp('attack', xp * killXpMult, 'kill'))
        p.GainXp("attack", Mathf.Max(1, Mathf.RoundToInt(t.xp * KillXpMult(t.lvl, p.level))), true);

        // drops (web rollDrops: each [id, chance])
        var got = new List<string>();
        if (t.drops != null)
            foreach (var o in t.drops)
            {
                var row = o as List<object>;
                if (row == null || row.Count < 2 || !(row[0] is string)) continue;
                string id = (string)row[0];
                if (Random.value >= (double)row[1]) continue;
                if (id.StartsWith("mount:") || id.StartsWith("pet:"))
                {
                    // a rare mount or pet (web: straight into your stable)
                    bool mnt = id.StartsWith("mount:"); string k = id.Substring(mnt ? 6 : 4);
                    var own = mnt ? p.mounts : p.pets;
                    if (!own.Contains(k)) { own.Add(k); if (mnt && p.mountSel == null) p.mountSel = k; if (g.ui != null) g.ui.Banner(mnt ? AHComp.MountName(k) : AHComp.PetName(k), mnt ? "Rare mount! Tap RIDE" : "A new companion! See PETS"); g.MarkDirty(); }
                    continue;
                }
                var d = AHItems.Get(id);
                if (d != null && p.bag.Add(id)) got.Add(d.name);
                else if (d != null && g.ui != null) g.ui.Toast(p.bag.lastWarn ?? "Your bag is full.");
            }
        if (got.Count > 0 && g.ui != null)
        {
            g.ui.Float(at + Vector3.up * 2.8f, string.Join(" · ", got.ToArray()), new Color(1f, 0.85f, 0.42f));
            g.ui.Toast("Loot: " + string.Join(", ", got.ToArray()) + ".");
        }

        // money (web: ((gold + lvl*2 + rand*lvl*2) * CU + rand*CU) bronze)
        long cu = AHDB.CU;
        long b = (long)System.Math.Round(((t.gold + t.lvl * 2 + Mathf.Floor(Random.value * t.lvl * 2)) * (double)cu + Mathf.Floor(Random.value * cu)) * AHComp.Fetch(p));
        AHComp.OnKill(g, m);
        AHRep.OnKill(g, m);
        AHEnh.OnKill(g, m);
        AHPower.OnKill(g, m);
        AHEvo.OnKill(g, m);
        AHKQ.OnKill(m);
        AHEvents.OnKill(g, m);
        p.AddMoney(b, at);

        if (g.ui != null) g.ui.Toast(t.name + " defeated." + (t.noSkin ? "" : " Tap the big button to skin it."));
    }
}
