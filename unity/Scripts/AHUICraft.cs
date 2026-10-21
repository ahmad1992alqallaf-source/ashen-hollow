// Ashen Hollow: the crafting window, World of Warcraft style. The station's recipes down the left, coloured by how
// much they still teach you (orange: always skill-up, yellow: likely, green: seldom, grey: never much; red: beyond
// you), with how many you can make now. Tap one and the right side shows it: the piece and its stats, what it needs,
// the materials you hold (and how many of them are of a better grade), a switch to spend your best grades first
// (better grades make a Masterwork likelier; a Legendary one makes it certain), and Create / Create all with a count.
// Recipes you haven't learned are listed dim: learn them from the station's book once your skill is there, or from
// a recipe scroll.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    AHRecipe craftSel; int craftN = 1; bool craftOnlyCan;
    const int CraftRows = 8; const float CraftRowH = 44f, CraftLeftW = 336f;

    static readonly Color[] DiffCol = { new Color(1f, 0.5f, 0.25f), new Color(1f, 0.95f, 0.3f), new Color(0.35f, 0.85f, 0.35f), new Color(0.6f, 0.6f, 0.6f) };
    static Color RecipeCol(AHPlayer p, AHRecipe r)
    {
        if (!AHRecipeDrops.Known(p, r)) return new Color(0.55f, 0.5f, 0.45f);
        int d = AHGather.Diff(p, r);
        return d < 0 ? new Color(0.95f, 0.35f, 0.3f) : DiffCol[Mathf.Clamp(d, 0, 3)];
    }
    static int CanMakeN(AHPlayer p, AHRecipe r)
    {
        int n = 9999;
        foreach (var m in r.mats) n = Mathf.Min(n, AHGrades.CountAll(p.bag, m.Key) / Mathf.Max(1, m.Value));
        return r.mats.Count == 0 ? 0 : n;
    }

    RectTransform CBtn(Transform parent, string label, Vector2 pos, float w, float h, Color col, bool on, System.Action act)
    {
        var br = Img("Btn", parent, white, new Vector2(0f, 1f), pos, new Vector2(w, h), on ? col : new Color(0.28f, 0.25f, 0.22f, 1f));
        Center(Label(br, "T", label, 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(w, h - 8), on ? Color.white : new Color(1f, 1f, 1f, 0.45f)));
        taps.Add(new TapBtn { rt = br, layer = 5, group = "work", act = () => { if (on && act != null) act(); } });
        return br;
    }

    void RenderCraft(AHPlayer p)
    {
        string kind = wkStation.kind;
        wkTitle.text = AHGather.StationName(kind) + " · " + AHDB.SkillName(StationSkill(kind)) + " " + p.Skill(StationSkill(kind));
        wkHint.text = AHGather.StationHint(kind);
        var list = AHGather.Recipes(kind).FindAll(x => (!x.rare || AHRareRecipes.Known(p, x.outId)) && (!craftOnlyCan || AHGather.CanMake(p, x)));
        if (craftSel == null || !list.Contains(craftSel)) { craftSel = null; foreach (var r in list) if (AHGather.CanMake(p, r)) { craftSel = r; break; } if (craftSel == null && list.Count > 0) craftSel = list[0]; craftN = 1; }

        // the list
        int pages = Mathf.Max(1, (list.Count + CraftRows - 1) / CraftRows);
        wkPageI = Mathf.Clamp(wkPageI, 0, pages - 1); wkPage.text = pages > 1 ? (wkPageI + 1) + " / " + pages : "";
        int from = wkPageI * CraftRows;
        for (int i = from; i < Mathf.Min(list.Count, from + CraftRows); i++)
        {
            var r = list[i]; var it = AHItems.Get(r.outId); int slot = i - from;
            bool sel = r == craftSel, known = AHRecipeDrops.Known(p, r);
            var bg = Img("CRow", wkList, white, new Vector2(0f, 1f), new Vector2(CraftLeftW / 2f, -slot * CraftRowH - CraftRowH / 2f), new Vector2(CraftLeftW, CraftRowH - 4), sel ? new Color(0.32f, 0.24f, 0.14f, 1f) : new Color(0.15f, 0.115f, 0.085f, 1f));
            wkItems.Add(bg.gameObject);
            int can = known ? CanMakeN(p, r) : 0;
            string nm = it.name + (can > 0 && p.Skill(r.skill) >= r.lvl ? "  <color=#ffffff>[" + can + "]</color>" : "") + (!known ? "  <color=#b8a37a><size=13>learn</size></color>" : "");
            var t = Label(bg, "N", nm, 17, TextAnchor.MiddleLeft, new Vector2(12, 0), new Vector2(CraftLeftW - 20, CraftRowH - 4), RecipeCol(p, r)); Fit(t, 12);
            var rr = r;
            taps.Add(new TapBtn { rt = bg, layer = 5, group = "work", act = () => { craftSel = rr; craftN = 1; RenderWork(); } });
        }
        if (list.Count == 0) wkItems.Add(Label(wkList, "Empty", craftOnlyCan ? "Nothing you can make right now." : "No recipes here.", 17, TextAnchor.UpperLeft, new Vector2(12, -8), new Vector2(CraftLeftW, 30), new Color(1f, 1f, 1f, 0.6f)).gameObject);
        // the filter switch under the list
        var fl = CBtn(wkList, craftOnlyCan ? "Showing: can make" : "Showing: all", new Vector2(CraftLeftW / 2f, -CraftRows * CraftRowH - 26), CraftLeftW, 36, Plain, true, () => { craftOnlyCan = !craftOnlyCan; wkPageI = 0; RenderWork(); });
        wkItems.Add(fl.gameObject);

        // the detail
        float RW = (WkW - 36f) - CraftLeftW - 12f, RH = CraftRows * CraftRowH + 40f;
        var pane = Img("CDetail", wkList, white, new Vector2(0f, 1f), new Vector2(CraftLeftW + 12f + RW / 2f, -RH / 2f), new Vector2(RW, RH), new Color(0.13f, 0.1f, 0.075f, 1f));
        wkItems.Add(pane.gameObject);
        var sr = craftSel; if (sr == null) return;
        var si = AHItems.Get(sr.outId); bool kn = AHRecipeDrops.Known(p, sr);
        // the icon
        var gs = AHItemIcons.Best(si); bool full = AHItemIcons.Full(si, gs);
        var ic = Img("Icon", pane, circle, new Vector2(0f, 1f), new Vector2(46, -46), new Vector2(68, 68), full ? new Color(0.2f, 0.15f, 0.11f, 1f) : new Color(si.color.r * 0.85f, si.color.g * 0.85f, si.color.b * 0.85f, 1f));
        if (gs != null) { var gl = Img("Glyph", ic, gs, new Vector2(0.5f, 0.5f), Vector2.zero, full ? new Vector2(66, 66) : new Vector2(46, 46), Color.white); gl.GetComponent<Image>().preserveAspect = true; }
        if (si.IsGear) { var it2 = si; taps.Add(new TapBtn { rt = ic, layer = 5, group = "work", act = () => OpenInspect(it2) }); }
        var nmT = Label(pane, "Name", si.name + (sr.n > 1 ? "  ×" + sr.n : ""), 22, TextAnchor.UpperLeft, new Vector2(90, -12), new Vector2(RW - 100, 30), AHItems.Quality(si)); nmT.fontStyle = FontStyle.Bold; Fit(nmT, 14);
        string l2 = si.slot != null ? AHItems.StatLine(si) + " · " + Bad(AHItems.WhoUses(sr.outId), AHItems.CanUse(sr.outId, p.cls.id)) : (si.note ?? "");
        if (sr.rare) l2 = "<color=#e8c26a>Rare recipe</color> · " + l2;
        var l2T = Label(pane, "L2", l2, 15, TextAnchor.UpperLeft, new Vector2(90, -44), new Vector2(RW - 100, 40), new Color(1f, 1f, 1f, 0.8f)); l2T.horizontalOverflow = HorizontalWrapMode.Wrap;
        bool lvOk = p.Skill(sr.skill) >= sr.lvl && AHGather.MasteryOk(p, sr) && (!AHArtisan.Only(sr.outId) || p.path == "artisan");
        string req = "Requires " + AHDB.SkillName(sr.skill) + " " + sr.lvl + (sr.masteryProf != null ? " · " + AHJson.S(AHJson.O(AHPlayer.Profs, sr.masteryProf), "name", sr.masteryProf) + " mastery " + sr.masteryLv : "") + (AHArtisan.Only(sr.outId) ? " · Artisans only" : "");
        int dif = AHGather.Diff(p, sr);
        string teach = dif < 0 ? "" : dif == 0 ? "  <color=#ff8040>· full skill-ups</color>" : dif == 1 ? "  <color=#f2f24d>· good skill-ups</color>" : dif == 2 ? "  <color=#59d959>· few skill-ups</color>" : "  <color=#999999>· little left to learn</color>";
        Fit(Label(pane, "Req", Bad(req, lvOk) + teach, 15, TextAnchor.UpperLeft, new Vector2(16, -96), new Vector2(RW - 28, 22), Color.white), 11);
        Label(pane, "MH", "Materials", 16, TextAnchor.UpperLeft, new Vector2(16, -124), new Vector2(200, 22), new Color(1f, 0.85f, 0.45f)).fontStyle = FontStyle.Bold;
        int row = 0;
        foreach (var m in sr.mats)
        {
            var md = AHItems.Get(m.Key); int have = AHGrades.CountAll(p.bag, m.Key);
            string extra = "";
            if (AHGrades.Gradable(m.Key))
            {
                var parts = new List<string>();
                for (int k = 4; k >= 1; k--) { int c = p.bag.Count(AHGrades.Id(m.Key, k)); if (c > 0) { var gd = AHItems.Get(AHGrades.Id(m.Key, k)); parts.Add("<color=#" + ColorUtility.ToHtmlStringRGB(AHItems.Quality(gd)) + ">" + c + " " + gd.name.Split(' ')[0] + "</color>"); } }
                if (parts.Count > 0) extra = "  (" + string.Join(", ", parts.ToArray()) + ")";
            }
            Fit(Label(pane, "M" + row, Bad(have + " / " + m.Value, have >= m.Value) + "  " + (md != null ? md.name : m.Key) + extra, 16, TextAnchor.UpperLeft, new Vector2(24, -150 - row * 24), new Vector2(RW - 40, 24), Color.white), 11);
            row++;
        }
        float by = -RH + 34f;
        if (!kn)
        {
            bool can = AHRecipeDrops.CanTrain(p, sr); long cost = AHRecipeDrops.TrainCost(sr);
            Fit(Label(pane, "Learn", can ? "Learn it from the " + AHGather.StationName(kind).ToLowerInvariant() + "’s book for " + AHItems.MoneyText(cost) + ", or from a recipe scroll."
                : "Not learned yet. Reach " + AHDB.SkillName(sr.skill) + " " + sr.lvl + " to learn it here, or find its recipe scroll (bosses drop them).", 15, TextAnchor.UpperLeft, new Vector2(16, by + 70), new Vector2(RW - 28, 40), new Color(1f, 0.9f, 0.7f)), 11);
            CBtn(pane, "Learn · " + AHItems.MoneyText(cost), new Vector2(RW / 2f, by), 260, 46, Go, can && p.bag.money >= cost, () => { AHRecipeDrops.Train(g, p, sr); RenderWork(); });
            return;
        }
        // spend best grades first
        CBtn(pane, AHGrades.UseBest ? "Best grades first: ON" : "Best grades first: OFF", new Vector2(130, by + 56), 240, 36, AHGrades.UseBest ? new Color(0.45f, 0.3f, 0.6f, 1f) : Plain, true, () => { AHGrades.UseBest = !AHGrades.UseBest; RenderWork(); });
        int canN = CanMakeN(p, sr); bool ok = lvOk && canN > 0;
        craftN = Mathf.Clamp(craftN, 1, Mathf.Max(1, canN));
        CBtn(pane, "−", new Vector2(30, by), 44, 46, Plain, ok && craftN > 1, () => { craftN--; RenderWork(); });
        Center(Label(pane, "Cnt", craftN.ToString(), 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(50, 40), Color.white)).rectTransform.anchoredPosition = new Vector2(-RW / 2f + 82, by + RH / 2f);
        CBtn(pane, "+", new Vector2(134, by), 44, 46, Plain, ok && craftN < canN, () => { craftN++; RenderWork(); });
        CBtn(pane, "Create", new Vector2(RW - 230, by), 130, 46, Go, ok, () => MakeN(p, sr, craftN));
        CBtn(pane, "Create all", new Vector2(RW - 84, by), 140, 46, Go, ok && canN > 1, () => MakeN(p, sr, canN));
    }

    void MakeN(AHPlayer p, AHRecipe r, int n)
    {
        ShowWork(false);
        AHGather.Start(p, wkStation, r);
        AHGather.craftLeft = Mathf.Max(0, n - 1);
    }

    static string StationSkill(string kind)
    {
        foreach (var r in AHGather.Recipes(kind)) if (r.skill != null) return r.skill;
        return kind == "anvil" ? "smithing" : kind == "loom" ? "tailoring" : kind == "brew" ? "herblore" : "crafting";
    }
}
