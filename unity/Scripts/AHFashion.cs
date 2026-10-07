// Ashen Hollow: make your own fashion. Tailors make dyes at the loom (from herbs, gems and cloth) and costume patterns
// (from cloth, hides and dye). Read a pattern to add its costume to your Wardrobe, or sell it at a market stall, since
// dyes and patterns are tailor's goods like any other. In the Wardrobe each costume you own can be dyed: one dye colours
// its cloth for good (until you dye it again or wash it back to its own colours, which is free).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHFashion
{
    public class Dye { public string id, name; public Color c; }
    public static readonly Dye[] Dyes =
    {
        new Dye { id = "dye_crimson", name = "Crimson", c = new Color(0.78f, 0.16f, 0.22f) }, new Dye { id = "dye_azure", name = "Azure", c = new Color(0.25f, 0.5f, 0.9f) },
        new Dye { id = "dye_emerald", name = "Emerald", c = new Color(0.18f, 0.68f, 0.38f) }, new Dye { id = "dye_gold", name = "Gold", c = new Color(0.9f, 0.7f, 0.2f) },
        new Dye { id = "dye_violet", name = "Violet", c = new Color(0.55f, 0.3f, 0.85f) }, new Dye { id = "dye_snow", name = "Snow", c = new Color(0.95f, 0.96f, 0.98f) },
        new Dye { id = "dye_ember", name = "Ember", c = new Color(1f, 0.42f, 0.16f) }, new Dye { id = "dye_shadow", name = "Shadow", c = new Color(0.24f, 0.24f, 0.3f) },
    };
    public static Dye GetDye(string id) { foreach (var d in Dyes) if (d.id == id) return d; return null; }

    // the loom's new recipes (tailoring)
    class R { public string outId; public int lvl, xp; public string[] mats; public int[] n; }
    static readonly R[] Recipes =
    {
        new R { outId = "dye_gold",    lvl = 1,  xp = 25,  mats = new[] { "sunpetal" }, n = new[] { 3 } },
        new R { outId = "dye_snow",    lvl = 3,  xp = 35,  mats = new[] { "cotton_cloth" }, n = new[] { 2 } },
        new R { outId = "dye_shadow",  lvl = 4,  xp = 40,  mats = new[] { "boar_hide", "linen" }, n = new[] { 2, 1 } },
        new R { outId = "dye_emerald", lvl = 5,  xp = 50,  mats = new[] { "mireroot" }, n = new[] { 3 } },
        new R { outId = "dye_crimson", lvl = 8,  xp = 70,  mats = new[] { "ruby", "linen" }, n = new[] { 1, 1 } },
        new R { outId = "dye_azure",   lvl = 10, xp = 85,  mats = new[] { "frostbloom" }, n = new[] { 3 } },
        new R { outId = "dye_violet",  lvl = 12, xp = 100, mats = new[] { "sapphire", "ruby" }, n = new[] { 1, 1 } },
        new R { outId = "dye_ember",   lvl = 13, xp = 110, mats = new[] { "emberthorn" }, n = new[] { 3 } },
        new R { outId = "pattern_ember",   lvl = 6,  xp = 180, mats = new[] { "linen", "boar_hide", "dye_crimson" }, n = new[] { 4, 3, 1 } },
        new R { outId = "pattern_sage",    lvl = 10, xp = 260, mats = new[] { "cotton_cloth", "linen", "dye_azure", "dye_gold" }, n = new[] { 4, 4, 1, 1 } },
        new R { outId = "pattern_crimson", lvl = 14, xp = 380, mats = new[] { "wolf_pelt", "linen", "dye_crimson" }, n = new[] { 4, 6, 2 } },
        new R { outId = "pattern_duelist", lvl = 16, xp = 480, mats = new[] { "cotton_cloth", "ruby", "dye_crimson", "dye_gold" }, n = new[] { 6, 1, 1, 1 } },
    };
    public static void AddTo(string station, List<AHRecipe> l)
    {
        if (station != "loom") return;
        foreach (var d in Recipes)
        {
            if (AHItems.Get(d.outId) == null) continue;
            var r = new AHRecipe { station = station, outId = d.outId, skill = "tailoring", lvl = d.lvl, xp = d.xp, n = 1 };
            for (int i = 0; i < d.mats.Length; i++) if (AHItems.Get(d.mats[i]) != null) r.mats.Add(new KeyValuePair<string, int>(d.mats[i], d.n[i]));
            l.Add(r);
        }
    }

    // patterns: read one to own its costume
    public static bool IsPattern(string id) { return id != null && id.StartsWith("pattern_"); }
    public static void ReadPattern(AHGame g, string id)
    {
        var p = g.player; string cos = id.Substring(8); var d = AHCostumes.Get(cos); if (p == null || d == null) return;
        if (AHCostumes.Owns(p, cos)) { g.ui.Toast("You already have " + d.name + ". Sell the pattern at a market stall.", 3f); return; }
        if (!p.bag.Take(id)) return;
        p.prog.costumes.Add(cos);
        g.ui.Banner(d.name, "Costume made! Wear it in the Wardrobe");
        AHSound.Play("level"); p.bag.Touch(); g.MarkDirty();
    }

    // dye on each costume: "costume=dye"
    public static string DyeOf(AHPlayer p, string costume)
    {
        if (p == null || p.prog == null || costume == null) return null;
        foreach (var e in p.prog.cosDye) if (e.k == costume) return e.v;
        return null;
    }
    public static void SetDye(AHPlayer p, string costume, string dye)
    {
        p.prog.cosDye.RemoveAll(e => e.k == costume);
        if (!string.IsNullOrEmpty(dye)) p.prog.cosDye.Add(new AHKS { k = costume, v = dye });
    }

    // colour a costume's cloth: the dye over its own painted texture (shading and stitching show through)
    public static void Tint(SkinnedMeshRenderer smr, string dye)
    {
        var d = GetDye(dye); if (smr == null || d == null) return;
        var mats = smr.materials; bool ch = false;
        foreach (var m in mats)
        {
            if (m == null) continue; string n = m.name.ToUpperInvariant();
            if (n.Contains("_SKIN") || n.Contains("_HAIR") || n.Contains("_FACE") || n.Contains("_EYE")) continue;
            float lum = 0.299f * d.c.r + 0.587f * d.c.g + 0.114f * d.c.b;
            // a light dye lifts a dark cloth, a dark one deepens a light cloth
            Color k = lum > 0.8f ? Color.Lerp(Color.white, d.c, 0.5f) * 1.35f : Color.Lerp(Color.white, d.c * 1.5f, 0.8f);
            foreach (var pr in new[] { "_Color", "_BaseColor" }) if (m.HasProperty(pr)) { var c0 = m.GetColor(pr); m.SetColor(pr, new Color(k.r, k.g, k.b, c0.a)); }
            if (m.HasProperty("_ShadeColor")) { var s0 = m.GetColor("_ShadeColor"); m.SetColor("_ShadeColor", new Color(s0.r * k.r, s0.g * k.g, s0.b * k.b, s0.a)); }
            ch = true;
        }
        if (ch) smr.materials = mats;
    }
}

public partial class AHUI
{
    string dyeFor;   // the costume being dyed (the "dye" page)
    public void OpenDye(string costume) { dyeFor = costume; wkMode = "dye"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderDye(AHPlayer p)
    {
        var cd = AHCostumes.Get(dyeFor); if (cd == null) { OpenWardrobe(); return; }
        string cur = AHFashion.DyeOf(p, dyeFor); var curD = AHFashion.GetDye(cur);
        wkTitle.text = "Dye: " + cd.name;
        wkHint.text = "One dye colours this costume's cloth for good. Tailors make dyes at the loom. Now: " + (curD != null ? curD.name : "its own colours") + ".";
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, "Back to the Wardrobe", new Color(1f, 0.85f, 0.55f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = OpenWardrobe }));
        rows.Add(s => Row(s, "Its own colours", new Color(0.9f, 0.9f, 0.9f), "Wash the dye out (free).", "",
            new WkBtn { label = cur == null ? "Now" : "Wash", on = cur != null, col = Plain, act = () => { AHFashion.SetDye(p, dyeFor, null); p.CheckOutfit(); g.SaveProgress(); RenderWork(); } }));
        foreach (var d in AHFashion.Dyes)
        {
            var dd = d; int have = p.bag.Count(d.id);
            rows.Add(s => Row(s, d.name + (cur == d.id ? "  · now" : ""), d.c, have > 0 ? "You have " + have + "." : "None in your bag · make it at the loom or buy it at a stall.", "",
                new WkBtn { label = "Use", on = have > 0 && cur != d.id, col = Go, act = () => { if (!p.bag.Take(dd.id)) return; AHFashion.SetDye(p, dyeFor, dd.id); p.bag.Touch(); p.CheckOutfit(); g.SaveProgress(); Toast(cd.name + " dyed " + dd.name.ToLowerInvariant() + "."); RenderWork(); } }));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
