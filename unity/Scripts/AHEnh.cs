// Ashen Hollow: enhancement (+1 to +9), as in the web game (v66, ENH_CHANCE / ENH_STONES / enhFee / doEnhance / stoneDrop).
// Weapons and armor can be raised to +9 with enhancement stones and a coin fee: +8% to their attack, armor and HP a level.
// +1 and +2 always work; from +3 it can fail; from +6 a failure drops the piece one level, unless a Lucky charm is used.
// Stones drop from monsters (more from elites and bosses); bosses sometimes drop Lucky charms.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHEnh
{
    public const int Max = 9;
    static readonly float[] Chance = { 1f, 1f, 1f, 0.95f, 0.85f, 0.75f, 0.6f, 0.45f, 0.35f, 0.25f };   // chance to reach +N (index N)
    public static float ChanceTo(int nx) { return nx >= 0 && nx < Chance.Length ? Chance[nx] : 0f; }
    public static int Stones(int nx) { return nx <= 3 ? 1 : nx <= 6 ? 2 : 3; }
    public static int Of(string id) { var it = AHItems.Get(id); return it != null ? it.enh : 0; }
    public static string Base(string id) { var it = AHItems.Get(id); return it != null && it.enh > 0 ? it.baseId : id; }
    public static string Id(string b, int n) { return n > 0 ? b + "+" + n : b; }
    public static long Fee(string id) { return Math.Max(20, (long)Math.Round(AHComp.Value(Base(id)) * 0.08 * (Of(id) + 1))) * AHDB.CU; }
    public static bool Can(ItemDef it) { return it != null && it.IsGear && !it.cosmetic; }

    // web doEnhance. slot: the gear slot when the piece is worn, else null (from the bag)
    public static void Do(AHGame g, string id, string slot, bool charm)
    {
        var p = g.player; var bag = p.bag; int n = Of(id), nx = n + 1; string b = Base(id);
        if (n >= Max || (slot == null && bag.Count(id) <= 0) || (slot != null && bag.Worn(slot) != id)) return;
        int need = Stones(nx); long fee = Fee(id);
        if (bag.Count("enh_stone") < need || bag.money < fee || (charm && bag.Count("lucky_charm") <= 0)) return;
        bag.Take("enh_stone", need); bag.money -= fee; if (charm && nx >= 6) bag.Take("lucky_charm");
        bool ok = UnityEngine.Random.value < ChanceTo(nx);
        int to = ok ? nx : nx >= 6 && !charm ? n - 1 : n;
        string nid = id;
        if (to != n)
        {
            nid = Id(b, to);
            if (slot != null) { bag.gear[slot] = nid; }
            else { bag.Take(id); bag.Add(nid); }
            bag.Touch();
            p.Recalc();
        }
        var it = AHItems.Get(nid);
        if (ok) { g.ui.Banner("Success! +" + nx, it.name); g.ui.Toast("Enhancement worked: " + it.name + "." + (nx == 7 && it.slot == "weapon" ? " Your weapon begins to glow!" : ""), 4f); AHFx.Ring(p.transform.position, 0.5f, 2.2f, new Color(0.6f, 0.9f, 1f), 0.8f); }
        else { g.ui.Banner("Enhancement failed", to < n ? "It slipped back to +" + to : "The item is unchanged"); g.ui.Toast(to < n ? "The enhancement failed and your item dropped to +" + to + ". A Lucky charm would have saved it." : "The enhancement failed. Only the stones and coins were lost.", 4f); }
        g.SaveProgress();
    }

    // web stoneDrop
    public static void OnKill(AHGame g, AHMob m)
    {
        if (m.add) return;
        var t = m.type; var p = g.player;
        bool dboss = AHDungeon.Def(AHGame.AreaId) != null && t.id == AHDungeon.BossOf(AHGame.AreaId);
        bool boss = dboss || t.id == "grull" || t.id == AHRaid.Boss || (t.elite && t.respawn > 600f);
        int n = boss ? 2 + UnityEngine.Random.Range(0, 3) : t.elite ? (UnityEngine.Random.value < 0.35f ? 1 : 0) : (UnityEngine.Random.value < 0.012f + t.lvl * 0.0004f ? 1 : 0);
        if (n > 0 && p.bag.Add("enh_stone", n)) g.ui.Float(m.transform.position + Vector3.up * 3.1f, "+" + n + " Enhancement stone", new Color(0.61f, 0.89f, 1f));
        if (boss && UnityEngine.Random.value < 0.25f && p.bag.Add("lucky_charm")) g.ui.Banner("Lucky charm!", "Protects an enhancement from failing");
    }
}

public partial class AHUI
{
    bool enhCharm;
    public void OpenEnh() { wkMode = "enh"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderEnh(AHPlayer p)
    {
        var bag = p.bag;
        int stones = bag.Count("enh_stone"), charms = bag.Count("lucky_charm");
        if (charms == 0) enhCharm = false;
        wkTitle.text = "Enhance gear";
        wkHint.text = "Stones: " + stones + " · Lucky charms: " + charms + (enhCharm ? " (one will be used from +6)" : "") + " · " + AHItems.MoneyText(bag.money) + ". +1 and +2 always work; from +6 a failure drops the piece one level unless a Lucky charm protects it.";
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, "Lucky charm: " + (enhCharm ? "on" : "off"), new Color(1f, 0.82f, 0.23f), "Protects a +6 to +9 attempt from dropping a level.", "Dropped by dungeon bosses, the raid and world bosses.",
            new WkBtn { label = enhCharm ? "Don't use" : "Use charm", on = charms > 0, col = enhCharm ? Go : Plain, act = () => { enhCharm = !enhCharm; RenderWork(); } }));
        var list = new List<KeyValuePair<string, string>>();   // id, slot (null: in the bag)
        foreach (var sl in AHItems.GearSlots) { string w = bag.Worn(sl); if (AHEnh.Can(AHItems.Get(w))) list.Add(new KeyValuePair<string, string>(w, sl)); }
        foreach (var id in bag.order) if (AHEnh.Can(AHItems.Get(id))) list.Add(new KeyValuePair<string, string>(id, null));
        foreach (var kv in list)
        {
            string id = kv.Key, slot = kv.Value; var it = AHItems.Get(id); int n = it.enh, nx = n + 1;
            if (n >= AHEnh.Max)
            {
                rows.Add(s => RowI(it, s, it.name + (slot != null ? "  <color=#9be37a>worn</color>" : ""), AHItems.Quality(it), AHItems.StatLine(it), "Already +9, as strong as it can be."));
                continue;
            }
            var nextIt = AHItems.Get(AHEnh.Id(AHEnh.Base(id), nx));
            int need = AHEnh.Stones(nx); long fee = AHEnh.Fee(id); float ch = AHEnh.ChanceTo(nx);
            bool ok = stones >= need && bag.money >= fee && (!enhCharm || charms > 0);
            rows.Add(s => RowI(it, s, it.name + (slot != null ? "  <color=#9be37a>worn</color>" : ""), AHItems.Quality(it),
                "→ +" + nx + ": " + AHItems.StatLine(nextIt), Mathf.RoundToInt(ch * 100) + "% · " + Bad(need + " stone" + (need > 1 ? "s" : ""), stones >= need) + " · " + Bad(AHItems.MoneyText(fee), bag.money >= fee) + (nx >= 6 ? (enhCharm ? " · protected" : " · <color=#ff8a7a>can drop a level</color>") : ""),
                new WkBtn { label = "+" + nx, on = ok, col = Go, act = () => { AHEnh.Do(g, id, slot, enhCharm && nx >= 6); RefreshBag(); RenderWork(); } }));
        }
        if (list.Count == 0) rows.Add(s => Row(s, "No gear to enhance", new Color(1f, 1f, 1f, 0.7f), "Weapons and armor, worn or in your bag, show up here.", ""));
        rows.Add(s => Row(s, "Back", new Color(1f, 1f, 1f, 0.7f), "", "", new WkBtn { label = "Menu", on = true, col = Plain, act = OpenHub }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
