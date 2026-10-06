// Ashen Hollow: the Talents page is a real tree. Tier I talents grow on the trunk and roots; at the fork the trunk
// splits into the class's two paths (the level-30 choice), each branch carrying its three Tier II talents; each branch
// then splits into two twigs, one per form (the level-60 choice), with their Tier III talents in the leaves.
// Branches you did not take wither grey; branches still locked show when they open. Tap a talent to read it on the
// card at the right, then Learn to spend a point. Art: Resources/AH/UI/talent_tree_*.png (tools/talenttree/build.py),
// talent glyphs from game-icons.net (CC BY 3.0).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    string talSel;
    static Sprite ttTrunk, ttLeft, ttRight; static Texture2D ttIcons; static Dictionary<string, object> ttIdx; static int ttCols = 8; static float ttCell = 96f; static bool ttTried;
    static readonly Dictionary<string, Sprite> ttGlyph = new Dictionary<string, Sprite>();

    // where the talents sit on the painted tree (in its 560 x 400 box, from the bottom left)
    static readonly Vector2[] TT1 = { new Vector2(150, 40), new Vector2(410, 40), new Vector2(205, 92), new Vector2(355, 92), new Vector2(222, 146), new Vector2(338, 146), new Vector2(88, 22), new Vector2(472, 22) };
    static readonly Vector2[] TT2 = { new Vector2(244, 200), new Vector2(195, 229), new Vector2(144, 268) };
    static readonly Vector2[][] TT3 = { new[] { new Vector2(99, 314), new Vector2(55, 366) }, new[] { new Vector2(173, 317), new Vector2(205, 370) } };
    const float TreeW = 560f, TreeH = 400f;
    static Vector2 Mir(Vector2 v, bool right) { return right ? new Vector2(TreeW - v.x, v.y) : v; }

    static Sprite TTLoad(string n)
    {
        var t = Resources.Load<Texture2D>("AH/UI/" + n); if (t == null) return null;
        t.wrapMode = TextureWrapMode.Clamp; return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }
    static Sprite TTGlyph(object node) { TTInit(); return TTGlyphOf(node); }
    static void TTInit()
    {
        if (!ttTried)
        {
            ttTried = true; ttTrunk = TTLoad("talent_tree_trunk"); ttLeft = TTLoad("talent_tree_left"); ttRight = TTLoad("talent_tree_right");
            ttIcons = Resources.Load<Texture2D>("AH/UI/talent_icons"); var ta = Resources.Load<TextAsset>("AH/UI/talent_icons_index");
            if (ttIcons != null && ta != null) { var o = AHJson.Parse(ta.text); ttIdx = AHJson.O(o, "index") as Dictionary<string, object>; ttCols = (int)AHJson.N(o, "cols", 8); ttCell = (float)AHJson.N(o, "cell", 96); ttIcons.wrapMode = TextureWrapMode.Clamp; }
        }
    }
    static Sprite TTGlyphOf(object node)
    {
        string key = "lock";
        var per = AHJson.O(node, "per") as Dictionary<string, object>;
        if (per != null) foreach (var k in per.Keys) { key = k; break; }
        Sprite s; if (ttGlyph.TryGetValue(key, out s)) return s;
        object cell = null;
        if (ttIcons != null && ttIdx != null && (ttIdx.TryGetValue(key, out cell) || ttIdx.TryGetValue("lock", out cell)))
        {
            int n = (int)System.Convert.ToDouble(cell); float c = ttCell * ttIcons.width / (ttCols * ttCell);
            s = Sprite.Create(ttIcons, new Rect((n % ttCols) * c, ttIcons.height - (n / ttCols + 1) * c, c, c), new Vector2(0.5f, 0.5f), 100f);
        }
        ttGlyph[key] = s; return s;
    }

    void SetPager(bool on) { if (wkPanel == null) return; foreach (var n in new[] { "Prev", "Next" }) { var t = wkPanel.Find(n); if (t != null) t.gameObject.SetActive(on); } }

    class TTNode { public object node; public string tier, where; public bool open, locked; public Color col; }

    void RenderTalentTree(AHPlayer p, int free)
    {
        SetPager(false); wkPage.text = ""; TTInit();
        var cp = AHEvo.CP(p); var paths = AHEvo.Paths(p.cls.id);
        wkHint.text = "A talent point every 3 levels: " + AHEvo.Points(p) + " earned, " + free + " free. Tap a talent to read it.";
        var box = Box("Tree", wkList, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -RowH), new Vector2(TreeW, TreeH)); wkItems.Add(box.gameObject);
        Color gold = new Color(1f, 0.8f, 0.4f);
        Color[] pc = new Color[2]; bool[] mine = new bool[2], other = new bool[2];
        for (int i = 0; i < 2; i++)
        {
            var pa = paths != null && i < paths.Count ? paths[i] : null;
            pc[i] = pa != null ? AHGame.Hex((int)AHJson.N(pa, "color")) : gold;
            mine[i] = pa != null && cp.path == AHJson.S(pa, "id"); other[i] = cp.path != null && !mine[i];
        }
        // a dusk backdrop with a warm glow behind the crown, then the painting: branches first, the trunk over their roots
        Img("Back", box, panel9 != null ? panel9 : white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TreeW, TreeH), new Color(0.11f, 0.085f, 0.06f, 1f)).GetComponent<Image>().type = panel9 != null ? Image.Type.Sliced : Image.Type.Simple;
        if (glow != null) Img("Halo", box, glow, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(TreeW * 1.1f, TreeH * 0.9f), new Color(1f, 0.78f, 0.4f, 0.22f));
        for (int i = 0; i < 2; i++)
        {
            var sp = i == 0 ? ttLeft : ttRight; if (sp == null) continue;
            var tint = other[i] ? new Color(0.42f, 0.4f, 0.38f, 0.85f) : mine[i] ? Color.Lerp(Color.white, pc[i], 0.25f) : new Color(0.82f, 0.8f, 0.75f, 1f);
            Img("Branch" + i, box, sp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TreeW, TreeH), tint);
        }
        if (ttTrunk != null) Img("Trunk", box, ttTrunk, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TreeW, TreeH), Color.white);
        if (ttTrunk == null) Img("NoArt", box, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(TreeW, TreeH), new Color(0.14f, 0.1f, 0.07f, 1f));

        var all = new List<KeyValuePair<Vector2, TTNode>>();
        // tier I: the trunk and roots
        var t1 = AHJson.A(AHJson.O(AHDB.File("evo"), "TALENTS"), p.cls.id);
        if (t1 != null) for (int i = 0; i < t1.Count && i < TT1.Length; i++) all.Add(new KeyValuePair<Vector2, TTNode>(TT1[i], new TTNode { node = t1[i], tier = "Tier I", where = p.cls.name + " talent", open = true, col = gold }));
        // tier II and III: the two branches and their twigs
        for (int i = 0; i < 2 && paths != null && i < paths.Count; i++)
        {
            var pa = paths[i]; bool right = i == 1; string pname = AHJson.S(pa, "name");
            var tal = AHJson.A(pa, "tal");
            if (tal != null) for (int j = 0; j < tal.Count && j < TT2.Length; j++)
                all.Add(new KeyValuePair<Vector2, TTNode>(Mir(TT2[j], right), new TTNode { node = tal[j], tier = "Tier II", where = pname + " path (level 30)", open = mine[i], locked = !mine[i], col = pc[i] }));
            // the path's name under its branch
            var lp = Mir(new Vector2(92f, 196f), right);
            var pl = Label(box, "Path", pname + (mine[i] ? "" : other[i] ? "" : "\n<size=12>level 30</size>"), 17, TextAnchor.UpperCenter, Vector2.zero, new Vector2(150, 40), other[i] ? new Color(0.6f, 0.56f, 0.5f) : pc[i]);
            pl.fontStyle = FontStyle.Bold; Place(pl.rectTransform, lp + new Vector2(0, 10));
            var forms = AHJson.A(pa, "forms");
            if (forms == null) continue;
            for (int f = 0; f < forms.Count && f < 2; f++)
            {
                var fo = forms[f]; string fid = AHJson.S(fo, "id"); bool fmine = cp.form == fid, fother = cp.form != null && !fmine;
                var ft = AHJson.A(fo, "tal");
                if (ft != null) for (int j = 0; j < ft.Count && j < 2; j++)
                    all.Add(new KeyValuePair<Vector2, TTNode>(Mir(TT3[f][j], right), new TTNode { node = ft[j], tier = "Tier III", where = AHJson.S(fo, "name") + " form (level 60)", open = fmine, locked = !fmine, col = Color.Lerp(pc[i], Color.white, 0.3f) }));
                var lf = Mir(f == 0 ? new Vector2(40f, 282f) : new Vector2(234f, 345f), right);
                var fl = Label(box, "Form", AHJson.S(fo, "name"), 13, TextAnchor.UpperCenter, Vector2.zero, new Vector2(90, 20), other[i] || fother ? new Color(0.6f, 0.56f, 0.5f) : new Color(1f, 0.92f, 0.75f));
                Place(fl.rectTransform, lf + new Vector2(0, 10));
            }
        }
        // pick a talent to show: the one tapped, else the first that can take a point
        if (talSel == null || all.Find(e => AHJson.S(e.Value.node, "id") == talSel).Value == null)
        {
            talSel = null;
            foreach (var e in all) { int r0 = AHEvo.Rank(p, AHJson.S(e.Value.node, "id")); if (e.Value.open && r0 < (int)AHJson.N(e.Value.node, "max", 3)) { talSel = AHJson.S(e.Value.node, "id"); break; } }
            if (talSel == null && all.Count > 0) talSel = AHJson.S(all[0].Value.node, "id");
        }
        TTNode sel = null;
        foreach (var e in all)
        {
            var nd = e.Value; string id = AHJson.S(nd.node, "id"); int r = AHEvo.Rank(p, id), max = (int)AHJson.N(nd.node, "max", 3);
            bool isSel = id == talSel; if (isSel) sel = nd;
            float sz = 46f;
            if (isSel && glow != null) Place(Img("Sel", box, glow, new Vector2(0f, 0f), Vector2.zero, new Vector2(sz * 2f, sz * 2f), new Color(1f, 0.85f, 0.4f, 0.9f)), e.Key);
            else if (r >= max && glow != null) Place(Img("Full", box, glow, new Vector2(0f, 0f), Vector2.zero, new Vector2(sz * 1.6f, sz * 1.6f), new Color(nd.col.r, nd.col.g, nd.col.b, 0.6f)), e.Key);
            Color face = nd.locked ? new Color(0.13f, 0.11f, 0.1f, 1f) : r > 0 ? Color.Lerp(new Color(0.25f, 0.17f, 0.08f), nd.col, 0.45f) : new Color(0.2f, 0.15f, 0.1f, 1f);
            var disc = Img("Talent", box, ornDisc != null ? ornDisc : white, new Vector2(0f, 0f), Vector2.zero, new Vector2(sz, sz), face); Place(disc, e.Key);
            if (ornRing != null) Img("Rim", disc, ornRing, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sz * 1.12f, sz * 1.12f), nd.locked ? new Color(0.45f, 0.42f, 0.4f) : Color.white);
            var gs = TTGlyph(nd.node);
            if (gs != null) { var gi = Img("Glyph", disc, gs, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sz * 0.6f, sz * 0.6f), nd.locked ? new Color(0.42f, 0.4f, 0.38f) : r > 0 ? new Color(1f, 0.93f, 0.7f) : Color.white); gi.GetComponent<Image>().preserveAspect = true; }
            var rk = Label(disc, "Rank", r + "/" + max, 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40, 16), r >= max ? new Color(1f, 0.85f, 0.35f) : r > 0 ? Color.white : new Color(1f, 1f, 1f, nd.locked ? 0.4f : 0.75f));
            rk.fontStyle = FontStyle.Bold; rk.rectTransform.anchorMin = rk.rectTransform.anchorMax = new Vector2(0.5f, 0f); rk.rectTransform.pivot = new Vector2(0.5f, 0.5f); rk.rectTransform.anchoredPosition = new Vector2(0, -6);
            string cid = id; taps.Add(new TapBtn { rt = disc, layer = 5, group = "work", act = () => { talSel = cid; RenderWork(); } });
        }
        TalentCard(p, sel, free);
    }

    // a child placed by the tree's bottom-left coordinates
    static void Place(RectTransform rt, Vector2 at) { rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = at; }

    void TalentCard(AHPlayer p, TTNode nd, int free)
    {
        float x = TreeW + 12f, w = WkW - 36f - x;
        var card = Img("Card", wkList, panel9 != null ? panel9 : white, new Vector2(0f, 1f), Vector2.zero, new Vector2(w, TreeH), new Color(0.2f, 0.15f, 0.11f, 1f));
        card.pivot = new Vector2(0f, 1f); card.anchoredPosition = new Vector2(x, -RowH); wkItems.Add(card.gameObject);
        var ci = card.GetComponent<Image>(); if (panel9 != null) ci.type = Image.Type.Sliced;
        var pts = Label(card, "Points", free + " point" + (free == 1 ? "" : "s") + " free", 17, TextAnchor.UpperCenter, new Vector2(0, -14), new Vector2(w, 24), free > 0 ? new Color(0.6f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.6f));
        pts.fontStyle = FontStyle.Bold;
        if (nd == null) return;
        string id = AHJson.S(nd.node, "id"); int r = AHEvo.Rank(p, id), max = (int)AHJson.N(nd.node, "max", 3);
        var nm = Label(card, "Name", AHJson.S(nd.node, "name"), 22, TextAnchor.UpperCenter, new Vector2(0, -52), new Vector2(w, 30), nd.locked ? new Color(0.7f, 0.66f, 0.6f) : nd.col); nm.fontStyle = FontStyle.Bold;
        Label(card, "Tier", nd.tier + " · " + nd.where, 13, TextAnchor.UpperCenter, new Vector2(10, -84), new Vector2(w - 20, 36), new Color(1f, 1f, 1f, 0.6f)).horizontalOverflow = HorizontalWrapMode.Wrap;
        // rank pips
        float px = w / 2f - (max - 1) * 11f;
        for (int i = 0; i < max; i++)
        {
            var pip = Img("Pip", card, white, new Vector2(0f, 1f), new Vector2(px + i * 22f, -132), new Vector2(12, 12), i < r ? new Color(1f, 0.8f, 0.35f) : new Color(0.35f, 0.28f, 0.22f));
            pip.localRotation = Quaternion.Euler(0, 0, 45);
        }
        var per = Label(card, "Per", "Each rank: " + AHEvo.PassText(AHJson.O(nd.node, "per")), 15, TextAnchor.UpperCenter, new Vector2(12, -154), new Vector2(w - 24, 60), Color.white);
        per.horizontalOverflow = HorizontalWrapMode.Wrap;
        if (r > 0) { var now = Label(card, "Now", "Now: " + AHEvo.PassText(AHJson.O(nd.node, "per"), r), 15, TextAnchor.UpperCenter, new Vector2(12, -214), new Vector2(w - 24, 60), new Color(1f, 0.85f, 0.45f)); now.horizontalOverflow = HorizontalWrapMode.Wrap; }
        bool can = !nd.locked && free > 0 && r < max;
        string lbl = nd.locked ? (nd.tier == "Tier II" ? "Choose this path at level 30" : "Choose this form at level 60") : r >= max ? "Fully learned" : free <= 0 ? "No points free" : "Learn  +1";
        var b = Img("Learn", card, white, new Vector2(0.5f, 0f), new Vector2(0, 104), new Vector2(w - 28, 50), can ? Go : new Color(0.28f, 0.25f, 0.22f, 1f));
        var bl = Center(Label(b, "T", lbl, can ? 20 : 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(w - 28, 40), can ? Color.white : new Color(1f, 1f, 1f, 0.6f))); bl.horizontalOverflow = HorizontalWrapMode.Wrap;
        var node = nd.node; taps.Add(new TapBtn { rt = b, layer = 5, group = "work", act = () => { if (can) { AHEvo.Learn(g, node); AHSound.Play("level"); RenderWork(); } } });
        var rs = Img("Reset", card, white, new Vector2(0.5f, 0f), new Vector2(0, 42), new Vector2(w - 28, 42), AHEvo.Spent(p) > 0 ? Plain : new Color(0.22f, 0.19f, 0.16f, 1f));
        Center(Label(rs, "T", "Reset all (free)", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(w - 28, 34), new Color(1f, 1f, 1f, AHEvo.Spent(p) > 0 ? 0.9f : 0.4f)));
        taps.Add(new TapBtn { rt = rs, layer = 5, group = "work", act = () => { if (AHEvo.Spent(p) > 0) { AHEvo.ResetTalents(g); RenderWork(); } } });
    }
}
