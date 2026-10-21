// Ashen Hollow: bag slots and the tool belt.
//  - Bags: five bag slots beside your backpack. Two are open from the start; the other three are opened with coin
//    (never Crowns). A bag in a slot adds its slots to your backpack's. Shops sell bags up to 16 slots (pouch 8,
//    satchel 12, traveller's pack 16); tailors make them: the Linen bag (16) anyone can sew, the bigger ones only
//    Artisans can make: Woolen 20, Hide 24, Silk-lined 28, Runeweave 32, Master's haversack 36 and the mythical
//    Bottomless bag (44). Adventurers buy those from Artisans at the auction, or ask for one on the Requests board.
//  - Tool belt: five tools that don't take bag space (pickaxe, woodcutter's axe, herb sickle, skinning knife, fishing
//    rod). A better tool gathers faster: bronze +10%, iron +20%, Guildmaster's +35% (made only by Artisans).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHBags
{
    public const int Boxes = 5, FreeBoxes = 2;
    static readonly long[] OpenCost = { 0, 0, 5000, 25000, 100000 };   // copper: 5 silver, 25 silver, 100 silver
    public static readonly string[] ToolKinds = { "pick", "axe", "sickle", "knife", "rod", "torch", "satchel" };
    public static readonly string[] ToolNames = { "Pickaxe", "Axe", "Sickle", "Knife", "Rod", "Torch", "Satchel" };
    public static readonly string[] ArtisanOnly = { "bag_wool", "bag_hide", "bag_silk", "bag_rune", "bag_master", "bag_mythic", "tool_pick_guild", "tool_axe_guild", "tool_sickle_guild", "tool_knife_guild", "tool_rod_guild" };

    static List<string> L(AHPlayer p) { var l = p.prog.bags; while (l.Count < Boxes) l.Add(""); return l; }
    public static int Opened(AHPlayer p) { return Mathf.Clamp(Mathf.Max(FreeBoxes, p.prog.bagOpen), FreeBoxes, Boxes); }
    public static string In(AHPlayer p, int i) { var l = L(p); return i >= 0 && i < l.Count && !string.IsNullOrEmpty(l[i]) ? l[i] : null; }
    public static long Cost(int i) { return i >= 0 && i < OpenCost.Length ? OpenCost[i] * AHDB.CU : 0; }

    public static int Size(string id) { return id == null ? 0 : (int)AHJson.N(AHItems.Raw(id), "bag", 0); }
    public static bool IsBag(string id) { return Size(id) > 0; }
    public static string ToolKind(string id) { return id == null ? null : AHJson.S(AHJson.O(AHItems.Raw(id), "tool"), "kind"); }
    public static bool IsTool(string id) { return ToolKind(id) != null; }
    public static float ToolBonus(string id) { return (float)AHJson.N(AHJson.O(AHItems.Raw(id), "tool"), "k", 0); }

    // the backpack's own slots (and the rows bought with Marks), then each bag's
    public static int Backpack(AHPlayer p) { return AHDB.Slots + p.prog.bagRows * 9; }
    public static int Extra(AHPlayer p) { int n = p.prog.bagRows * 9; for (int i = 0; i < Boxes; i++) n += Size(In(p, i)); return n; }
    public static void Apply(AHPlayer p) { if (p != null && p.prog != null) p.bag.extra = Extra(p); }

    public static bool OpenBox(AHGame g, int i)
    {
        var p = g.player; if (i != Opened(p)) { g.ui.Toast("Open the bag slots in order."); return false; }
        long c = Cost(i); if (p.bag.money < c) { g.ui.Toast("Opening this bag slot costs " + AHItems.MoneyText(c) + "."); return false; }
        p.bag.money -= c; p.prog.bagOpen = i + 1; p.bag.Touch();
        g.ui.Toast("A new bag slot is open. Put a bag in it from your backpack.", 3f); g.MarkDirty();
        return true;
    }

    // a bag from the backpack into a free slot (or in place of the smallest one, if it is bigger)
    public static string Equip(AHGame g, string id)
    {
        var p = g.player; int sz = Size(id); if (sz <= 0 || p.bag.Count(id) <= 0) return null;
        int free = -1, small = -1;
        for (int i = 0; i < Opened(p); i++)
        {
            var b = In(p, i);
            if (b == null) { if (free < 0) free = i; }
            else if (small < 0 || Size(b) < Size(In(p, small))) small = i;
        }
        if (free >= 0) { p.bag.Take(id, 1); L(p)[free] = id; Apply(p); g.MarkDirty(); return null; }
        if (small >= 0 && Size(In(p, small)) < sz)
        {
            string old = In(p, small);
            p.bag.Take(id, 1); L(p)[small] = id; Apply(p);
            if (!p.bag.Add(old, 1)) { L(p)[small] = old; p.bag.Add(id, 1); Apply(p); return "No room to swap the bags."; }
            g.MarkDirty(); return null;
        }
        return Opened(p) < Boxes ? "Every open bag slot is full. Open another one (tap a locked slot)." : "Your bags are all at least this big.";
    }

    // a bag back into the backpack: only if everything still fits without it
    public static string Unequip(AHGame g, int i)
    {
        var p = g.player; string id = In(p, i); if (id == null) return null;
        int after = p.bag.SlotsMax - Size(id);
        int need = p.bag.UsedSlots + (p.bag.Count(id) > 0 ? 0 : 1);
        if (need > after) return "Empty it first: your things wouldn't fit without this bag.";
        L(p)[i] = ""; Apply(p);
        p.bag.Add(id, 1); g.MarkDirty();
        return null;
    }

    // ---------- the tool belt ----------
    public static string Tool(AHPlayer p, string kind) { foreach (var t in p.prog.tools) if (t.k == kind) return string.IsNullOrEmpty(t.v) ? null : t.v; return null; }
    public static void SetToolPublic(AHPlayer p, string kind, string id) { SetTool(p, kind, id); }
    static void SetTool(AHPlayer p, string kind, string id)
    {
        var e = p.prog.tools.Find(t => t.k == kind);
        if (e == null) p.prog.tools.Add(new AHKS { k = kind, v = id ?? "" }); else e.v = id ?? "";
    }
    public static string EquipTool(AHGame g, string id)
    {
        var p = g.player; string kind = ToolKind(id); if (kind == null || p.bag.Count(id) <= 0) return null;
        string old = Tool(p, kind);
        p.bag.Take(id, 1); SetTool(p, kind, id);
        if (old != null && !p.bag.Add(old, 1)) { SetTool(p, kind, old); p.bag.Add(id, 1); return "Make room in your bag for your old tool first."; }
        g.MarkDirty(); return null;
    }
    public static string UnequipTool(AHGame g, string kind)
    {
        var p = g.player; string id = Tool(p, kind); if (id == null) return null;
        if (!p.bag.Add(id, 1)) return p.bag.lastWarn ?? "Your bag is full.";
        SetTool(p, kind, null); g.MarkDirty(); return null;
    }
    // gathering speed from the belt: mine, chop, herb, skin, fish
    public static float SpeedK(AHPlayer p, string gatherKind)
    {
        if (p == null || p.prog == null) return 1f;
        string kind = gatherKind == "mine" ? "pick" : gatherKind == "chop" ? "axe" : gatherKind == "herb" ? "sickle" : gatherKind == "skin" ? "knife" : gatherKind == "fish" ? "rod" : null;
        string id = kind != null ? Tool(p, kind) : null;
        return 1f + (id != null ? ToolBonus(id) : 0f);
    }

    public static string Describe(string id)
    {
        if (IsBag(id)) return "Bag · " + Size(id) + " slots. Put it in a bag slot to carry more." + (Array.IndexOf(ArtisanOnly, id) >= 0 ? " Made only by Artisans." : "");
        if (IsTool(id)) { int i = Array.IndexOf(ToolKinds, ToolKind(id)); return "Tool for the belt (" + (i >= 0 ? ToolNames[i].ToLowerInvariant() : "") + ") · gathers " + Mathf.RoundToInt(ToolBonus(id) * 100f) + "% faster. It doesn't take bag space on the belt."; }
        return null;
    }
}
