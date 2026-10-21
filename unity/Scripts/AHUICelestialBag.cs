// Ashen Hollow: the bag, laid out as the Celestial Glass design (the "B-Bag" board of the design canvas):
//  - a header: "Bag" in Marcellus, a glass pill of tabs (All, Gear, Materials, Food & potions, Recipes), the purse,
//    how full the bag is, and a round close button;
//  - left, a glass panel with your gear in two columns round a figure (the chosen piece shows on it), your name and
//    gear score at the top and Power, Armour, Health and Crit along the bottom;
//  - middle, the grid of glass sockets: each item with a rarity dot at its corner and a glow of its rarity at its foot,
//    the chosen one ringed in white; your bags and the tool belt under it; the rarity key and Sort at the bottom;
//  - right, the chosen item: its picture in a glowing frame, its name in Marcellus and its rarity, what it does, and a
//    big sky-glass button for what you can do with it (Equip, Eat, Learn...) over an outlined Drop button.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    string bagTab = "all"; List<int> bagFiltered;
    static Sprite cgSlotSp, cgSlotGlow, cgPillAct, cgPillLine;
    const float BW = 1060f, BH = 590f;
    readonly List<KeyValuePair<string, RectTransform>> bagTabs = new List<KeyValuePair<string, RectTransform>>();
    Text cgBagCount, cgGearName, cgGearScore, cgInfoRarity; Text[] cgStatV; RectTransform cgFigure, cgInfoIcon; Image cgInfoGlyph, cgInfoFrame;

    static Vector2 BC(float x, float y) { return new Vector2(x - BW * 0.5f, BH * 0.5f - y); }   // top-left design coords -> centre-anchored

    bool TabHas(ItemDef d)
    {
        if (d == null) return false;
        bool rec = d.id == AHRareRecipes.ScrollId || AHRecipeDrops.IsScroll(d.id) || AHFashion.IsPattern(d.id);
        switch (bagTab)
        {
            case "gear": return d.IsGear;
            case "food": return d.IsFood || d.potion;
            case "rec": return rec;
            case "mat": return !d.IsGear && !d.IsFood && !d.potion && !rec;
        }
        return true;
    }

    // a glass socket: a rarity dot at its corner, a glow of its rarity at its foot, a white ring when chosen
    static void CGPaintSlot(BagSlot b, ItemDef d, bool selected)
    {
        if (cgSlotSp == null || b == null || b.rt == null) return;
        Color q = d != null ? AHItems.Quality(d) : Color.clear;
        b.bg.sprite = cgSlotSp; b.bg.type = Image.Type.Sliced; b.bg.color = d != null ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.45f);
        if (b.frame != null) { b.frame.sprite = cgSlotGlow; b.frame.type = Image.Type.Sliced; b.frame.color = d != null ? new Color(q.r, q.g, q.b, 0.85f) : Color.clear; b.frame.rectTransform.sizeDelta = b.rt.sizeDelta; }
        if (d != null && b.icon != null) { var c = b.icon.color; b.icon.color = new Color(c.r, c.g, c.b, AHItemIcons.Full(d, AHItemIcons.Best(d)) ? 0f : 0.25f); }
        var dot = b.rt.Find("Dot"); if (dot != null) { dot.gameObject.SetActive(d != null); if (d != null) dot.GetComponent<Image>().color = q; }
        var sel = b.rt.Find("Sel"); if (sel != null) sel.gameObject.SetActive(selected);
        if (b.count != null) { b.count.color = Color.white; b.count.fontStyle = FontStyle.Bold; }
    }

    static Sprite CGGlowFoot(int n, float radius)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n]; float h = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - h) - (h - radius), qy = Mathf.Abs(y + 0.5f - h) - (h - radius);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float a = Mathf.Clamp01(0.5f - d) * Mathf.Pow(Mathf.Clamp01(1f - y / (n * 0.5f)), 1.6f) * 0.9f;
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        int b = Mathf.CeilToInt(radius + 4f);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 400f, 0, SpriteMeshType.FullRect, new Vector4(b, n / 2, b, b));
    }

    RectTransform BagBtn(string name, string label, Vector2 tl, Vector2 size, Sprite sp, Color col, int fs, System.Action act)
    {
        var r = Img(name, bagWin, sp, new Vector2(0.5f, 0.5f), BC(tl.x + size.x * 0.5f, tl.y + size.y * 0.5f), size, col);
        r.GetComponent<Image>().type = Image.Type.Sliced;
        var t = Center(Label(r, "T", label, fs, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size.x, size.y - 4), new Color(0.93f, 0.96f, 1f))); t.fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = r, layer = 2, act = act });
        return r;
    }

    void CelestialBag()
    {
        if (bagWin == null) return;
        if (cgSlotSp == null)
        {
            cgSlotSp = CGGlass(96, 22f, 0.5f, 0.55f, new Color(0.1f, 0.14f, 0.22f));
            cgSlotGlow = CGGlowFoot(96, 22f);
            cgPillAct = CGGlass(96, 44f, 0.75f, 1f, new Color(0.3f, 0.55f, 0.82f));
            cgPillLine = CGShape(96, 44f, true);
        }
        var dim = bagRoot.GetComponent<Image>(); if (dim != null) dim.color = new Color(0.03f, 0.05f, 0.1f, 0.94f);
        var bagPanel = CGGlass(160, 40f, 0.78f, 0.9f, new Color(0.06f, 0.1f, 0.18f));
        bagWin.sizeDelta = new Vector2(BW, BH); bagWin.anchoredPosition = Vector2.zero;
        var wi = bagWin.GetComponent<Image>(); wi.color = new Color(1f, 1f, 1f, 0f);
        foreach (var nm0 in new[] { "Edge", "Worn" }) { var t = bagWin.Find(nm0); if (t != null) t.gameObject.SetActive(false); }
        if (gearStats != null) gearStats.gameObject.SetActive(false);
        // the three glass panels
        System.Action<string, float, float, float, float> panel = (nm, x, y, w, h) =>
        {
            var r = bagWin.Find(nm) as RectTransform;
            if (r == null) { r = Img(nm, bagWin, bagPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one, Color.white); r.SetSiblingIndex(0); }
            var im = r.GetComponent<Image>(); im.sprite = bagPanel; im.type = Image.Type.Sliced; im.color = Color.white;
            r.anchoredPosition = BC(x + w * 0.5f, y + h * 0.5f); r.sizeDelta = new Vector2(w, h);
        };
        panel("GearInset", 16, 64, 260, 516); panel("BagInset", 290, 64, 490, 516); panel("InfoInset", 794, 64, 250, 516);

        // ---- the header ----
        var title = bagWin.Find("Title") as RectTransform;
        if (title != null) { var tt = title.GetComponent<Text>(); tt.font = TitleFont; tt.fontStyle = FontStyle.Normal; tt.fontSize = 31; tt.color = Color.white; foreach (var o in title.GetComponents<Shadow>()) Destroy(o); title.anchoredPosition = new Vector2(20, -8); title.sizeDelta = new Vector2(100, 44); }
        var tabBack = Img("Tabs", bagWin, cgPillGlass, new Vector2(0.5f, 0.5f), BC(110 + 238, 30), new Vector2(476, 42), Color.white); tabBack.GetComponent<Image>().type = Image.Type.Sliced;
        string[][] tabs = { new[] { "all", "All", "54" }, new[] { "gear", "Gear", "72" }, new[] { "mat", "Materials", "104" }, new[] { "food", "Food & potions", "138" }, new[] { "rec", "Recipes", "92" } };
        float tx = 116; bagTabs.Clear();
        foreach (var t in tabs)
        {
            string id = t[0]; float w = float.Parse(t[2]);
            var b = BagBtn("Tab_" + id, t[1], new Vector2(tx, 14), new Vector2(w, 32), cgPillSky, Color.white, 13, () => { bagTab = id; bagSel = null; RefreshBag(); });
            bagTabs.Add(new KeyValuePair<string, RectTransform>(id, b)); tx += w + 4;
        }
        if (bagGold != null) { bagGold.rectTransform.anchoredPosition = new Vector2(BW - 330, -18); bagGold.rectTransform.sizeDelta = new Vector2(190, 26); bagGold.fontSize = 16; bagGold.fontStyle = FontStyle.Bold; bagGold.color = CGGold; bagGold.alignment = TextAnchor.MiddleRight; }
        cgBagCount = Label(bagWin, "Used", "", 15, TextAnchor.MiddleRight, new Vector2(BW - 134, -18), new Vector2(70, 26), new Color(0.74f, 0.83f, 0.93f));
        var close = bagWin.Find("Close") as RectTransform;
        if (close != null)
        {
            close.anchorMin = close.anchorMax = new Vector2(1, 1); close.anchoredPosition = new Vector2(-38, -31); close.sizeDelta = new Vector2(44, 44);
            var ci = close.GetComponent<Image>(); ci.sprite = ornDisc; ci.color = new Color(0.05f, 0.08f, 0.15f, 0.6f);
            var rim = close.Find("Rim") as RectTransform; if (rim != null) rim.sizeDelta = new Vector2(47, 47);
            foreach (var t in close.GetComponentsInChildren<Text>(true)) t.gameObject.SetActive(false);
            Img("CGIcon", close, CGIcon("close"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18), Color.white);
        }

        // ---- left: the gear round the figure ----
        cgGearName = Label(bagWin, "GearName", "", 15, TextAnchor.UpperLeft, new Vector2(32, -80), new Vector2(130, 22), Color.white); cgGearName.font = TitleFont;
        cgGearScore = Label(bagWin, "GearScore", "", 12, TextAnchor.UpperRight, new Vector2(140, -81), new Vector2(122, 22), new Color(0.74f, 0.83f, 0.93f)); cgGearScore.supportRichText = true;
        var fig = CGIcon("hero"); cgFigure = Img("Figure", bagWin, fig, new Vector2(0.5f, 0.5f), BC(146, 268), new Vector2(104, 218), Color.white); cgFigure.GetComponent<Image>().preserveAspect = true;
        cgFigure.SetSiblingIndex(3);
        for (int k = 0; k < AHItems.GearSlots.Length; k++)
        {
            BagSlot b; if (!gearSlots.TryGetValue(AHItems.GearSlots[k], out b)) continue;
            int col = k / 5, row = k % 5;
            b.rt.anchoredPosition = BC(col == 0 ? 58 : 234, 132 + row * 64); b.rt.sizeDelta = new Vector2(54, 54);
            CGSlotParts(b, 54);
        }
        var pv = bagWin.Find("Preview") as RectTransform; if (pv != null) { pv.anchoredPosition = BC(146, 262); pv.sizeDelta = new Vector2(150, 150); }
        var pg = bagWin.Find("PreviewGlow") as RectTransform; if (pg != null) { pg.anchoredPosition = BC(146, 262); pg.sizeDelta = new Vector2(190, 190); }
        cgStatV = new Text[4]; string[] stn = { "Power", "Armour", "Health", "Crit" };
        for (int i = 0; i < 4; i++)
        {
            float cx = 16 + 65f * (i + 0.5f);
            cgStatV[i] = Label(bagWin, "StatV" + i, "", 19, TextAnchor.UpperCenter, new Vector2(cx - 34, -508), new Vector2(68, 24), Color.white); cgStatV[i].fontStyle = FontStyle.Bold;
            Label(bagWin, "StatN" + i, stn[i], 12, TextAnchor.UpperCenter, new Vector2(cx - 34, -534), new Vector2(68, 18), new Color(0.74f, 0.83f, 0.93f));
        }

        // ---- middle: the grid, the bags, the key ----
        int n = bagSlots.Length, cols = 9; float step = (490f - 28f) / cols, size = step - 6f;
        for (int i = 0; i < n; i++)
        {
            int c = i % cols, r = i / cols;
            bagSlots[i].rt.anchoredPosition = BC(290 + 14 + step * (c + 0.5f), 64 + 14 + step * (r + 0.5f)); bagSlots[i].rt.sizeDelta = new Vector2(size, size);
            CGSlotParts(bagSlots[i], size);
        }
        float gridBottom = 64 + 14 + step * Mathf.CeilToInt(n / (float)cols);
        for (int k = 0; k < boxBtns.Count; k++)
        {
            var bt = boxBtns[k]; float bw = (490f - 28f - 6f * (boxBtns.Count - 1)) / boxBtns.Count;
            bt.anchoredPosition = BC(304 + k * (bw + 6) + bw * 0.5f, gridBottom + 30); bt.sizeDelta = new Vector2(bw, 40);
            var im = bt.GetComponent<Image>(); im.sprite = cgPillGlass; im.type = Image.Type.Sliced;
            if (k < boxTexts.Count) { boxTexts[k].fontSize = 11; boxTexts[k].rectTransform.sizeDelta = new Vector2(bw, 38); }
        }
        string[] rn = { "Common", "Uncommon", "Rare", "Epic", "Legendary" }; string[] rq = { "common", "uncommon", "rare", "epic", "legendary" };
        float lx = 306;
        for (int i = 0; i < 5; i++)
        {
            var row = AHJson.A(AHDB.Table("items", "QUALITY"), rq[i]); Color qc = row != null && row.Count > 1 ? AHDB.Col(row[1], Color.white) : Color.white;
            Img("KeyDot" + i, bagWin, circle, new Vector2(0.5f, 0.5f), BC(lx + 4, 552), new Vector2(8, 8), qc);
            Label(bagWin, "Key" + i, rn[i], 12, TextAnchor.MiddleLeft, new Vector2(lx + 12, -543), new Vector2(80, 18), new Color(0.74f, 0.83f, 0.93f));
            lx += 14 + rn[i].Length * 6.6f + 12;
        }
        var sort = bagWin.Find("Sort") as RectTransform;
        if (sort != null)
        {
            sort.anchorMin = sort.anchorMax = new Vector2(0.5f, 0.5f); sort.anchoredPosition = BC(734, 552); sort.sizeDelta = new Vector2(64, 30);
            var si = sort.GetComponent<Image>(); si.sprite = cgPillGlass; si.type = Image.Type.Sliced; si.color = Color.white;
            foreach (var t in sort.GetComponentsInChildren<Text>(true)) { t.text = "Sort"; t.fontSize = 13; t.fontStyle = FontStyle.Bold; }
        }

        // ---- right: the chosen item ----
        cgInfoFrame = Img("InfoFrame", bagWin, cgSlotSp, new Vector2(0.5f, 0.5f), BC(840, 112), new Vector2(60, 60), Color.white).GetComponent<Image>(); cgInfoFrame.type = Image.Type.Sliced;
        cgInfoIcon = cgInfoFrame.rectTransform;
        cgInfoGlyph = Img("Glyph", cgInfoIcon, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46), Color.white).GetComponent<Image>(); cgInfoGlyph.preserveAspect = true;
        if (bagInfo != null) { bagInfo.font = TitleFont; bagInfo.fontStyle = FontStyle.Normal; bagInfo.fontSize = 17; bagInfo.rectTransform.anchoredPosition = new Vector2(880, -82); bagInfo.rectTransform.sizeDelta = new Vector2(152, 44); bagInfo.horizontalOverflow = HorizontalWrapMode.Wrap; bagInfo.verticalOverflow = VerticalWrapMode.Overflow; }
        cgInfoRarity = Label(bagWin, "Rarity", "", 11, TextAnchor.UpperLeft, new Vector2(880, -128), new Vector2(152, 16), Color.white); cgInfoRarity.fontStyle = FontStyle.Bold;
        Img("InfoLine", bagWin, white, new Vector2(0.5f, 0.5f), BC(919, 156), new Vector2(218, 1), new Color(0.75f, 0.88f, 1f, 0.25f));
        if (bagInfoSub != null) { bagInfoSub.fontSize = 13; bagInfoSub.color = new Color(0.93f, 0.96f, 1f); bagInfoSub.rectTransform.anchoredPosition = new Vector2(810, -166); bagInfoSub.rectTransform.sizeDelta = new Vector2(218, 300); }
        if (bagAct != null)
        {
            bagAct.anchorMin = bagAct.anchorMax = new Vector2(0.5f, 0.5f); bagAct.anchoredPosition = BC(919, 496); bagAct.sizeDelta = new Vector2(218, 46);
            var ai = bagAct.GetComponent<Image>(); ai.sprite = cgPillAct; ai.type = Image.Type.Sliced; ai.color = Color.white;
            if (bagActText != null) { bagActText.fontSize = 16; bagActText.color = Color.white; }
        }
        if (bagDrop != null)
        {
            bagDrop.anchorMin = bagDrop.anchorMax = new Vector2(0.5f, 0.5f); bagDrop.anchoredPosition = BC(919, 548); bagDrop.sizeDelta = new Vector2(218, 38);
            var di = bagDrop.GetComponent<Image>(); di.sprite = cgPillLine; di.type = Image.Type.Sliced; di.color = new Color(0.75f, 0.88f, 1f, 0.6f);
            if (bagDropText != null) { bagDropText.fontSize = 13; bagDropText.color = new Color(0.93f, 0.96f, 1f); }
        }
    }

    // the dot, the white ring and the foot glow of a socket
    void CGSlotParts(BagSlot b, float size)
    {
        if (b.rt.Find("Dot") == null) { var d = Img("Dot", b.rt, circle, new Vector2(0f, 1f), new Vector2(8, -8), new Vector2(7, 7), Color.white); d.gameObject.SetActive(false); }
        if (b.rt.Find("Sel") == null)
        {
            var s = Img("Sel", b.rt, cgPillLine, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 6, size + 6), Color.white);
            s.GetComponent<Image>().sprite = CGShape(96, 22f, true); s.GetComponent<Image>().type = Image.Type.Sliced; s.gameObject.SetActive(false);
        }
        if (b.hint != null) { b.hint.fontSize = 9; b.hint.color = new Color(0.74f, 0.83f, 0.93f, 0.55f); }
        if (b.count != null) { b.count.fontSize = 12; Shade(b.count); }
    }

    // while the bag is open: the tabs, the counts, the stats and the chosen item's frame
    void CelestialBagTick()
    {
        if (!BagOpen || cgBagCount == null) return;
        var p = g.player; if (p == null) return;
        foreach (var kv in bagTabs)
        {
            bool on = kv.Key == bagTab; var im = kv.Value.GetComponent<Image>(); im.color = on ? Color.white : new Color(1f, 1f, 1f, 0f);
            var t = kv.Value.GetComponentInChildren<Text>(); if (t != null) t.color = on ? Color.white : new Color(0.74f, 0.83f, 0.93f);
        }
        cgBagCount.text = p.bag.UsedSlots + " / " + p.bag.SlotsMax;
        cgGearName.text = string.IsNullOrEmpty(p.heroName) ? p.cls.name : p.heroName;
        var s = p.stat; int score = s.atk * 2 + s.def + Mathf.RoundToInt(p.maxHp / 10f);
        cgGearScore.text = "Gear score <color=#f0d58a><b><size=15>" + score + "</size></b></color>";
        if (cgStatV != null) { cgStatV[0].text = s.atk.ToString(); cgStatV[1].text = s.def.ToString(); cgStatV[2].text = Mathf.CeilToInt(p.maxHp).ToString(); cgStatV[3].text = Mathf.RoundToInt(s.crit * 100f) + "%"; }
        // the figure gives way to the chosen piece's picture
        if (cgFigure != null && preview != null) cgFigure.gameObject.SetActive(!preview.enabled);
        // the chosen item's frame
        ItemDef d = bagSel != null && p.bag.Count(bagSel) > 0 ? AHItems.Get(bagSel) : gearSel != null ? AHItems.Get(p.bag.Worn(gearSel)) : null;
        bool on2 = d != null; if (cgInfoIcon.gameObject.activeSelf != on2) cgInfoIcon.gameObject.SetActive(on2);
        cgInfoRarity.text = "";
        if (on2)
        {
            Color q = AHItems.Quality(d); var gs = AHItemIcons.Best(d);
            cgInfoGlyph.sprite = gs; cgInfoGlyph.enabled = gs != null; cgInfoFrame.color = Color.Lerp(Color.white, q, 0.5f);
            string qn = AHItems.QualityName(d); cgInfoRarity.text = qn.ToUpperInvariant(); cgInfoRarity.color = q;
        }
        bagInfo.rectTransform.anchoredPosition = on2 ? new Vector2(880, -82) : new Vector2(810, -82);
    }
}
