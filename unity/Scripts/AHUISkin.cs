// Ashen Hollow: the HUD's look, in the style of the big Eastern mobile MMOs: gold-rimmed round buttons with a dark
// lacquer face, a framed portrait with the health and mana bars beside it, ornate gold-edged panels for windows and
// tags, a quest tracker on the left with an AUTO button, and a large round attack button ringed by the skills.
// Everything is drawn in code (no image files): ApplySkin() runs once after the HUD is built and restyles it.
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    Sprite ornRing, ornDisc, panel9, tag9, bar9, glow;
    RectTransform questPanel, autoBtn; Image autoImg; Text autoText, panelTag;
    static readonly Color Gold = new Color(1f, 0.82f, 0.42f), GoldDeep = new Color(0.62f, 0.42f, 0.16f);

    // ---------- the drawn pieces ----------
    static Sprite MakeOrnRing(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - r, y + 0.5f - r); float d = p.magnitude / r, a = Mathf.Atan2(p.y, p.x);
                // a gold band from 0.84 to 0.98 of the radius, lit from the top left, with dark lines either side
                float band = Mathf.Clamp01((d - 0.83f) * 60f) * Mathf.Clamp01((0.985f - d) * 60f);
                float lit = 0.5f + 0.5f * Vector2.Dot(p.normalized, new Vector2(-0.6f, 0.8f));
                float bevel = Mathf.Sin(Mathf.Clamp01((d - 0.83f) / 0.155f) * Mathf.PI);
                Color gc = Color.Lerp(new Color(0.45f, 0.27f, 0.08f), new Color(1f, 0.9f, 0.55f), Mathf.Clamp01(lit * 0.7f + bevel * 0.45f));
                float dark = Mathf.Clamp01(1f - Mathf.Abs(d - 0.815f) * 90f) + Mathf.Clamp01(1f - Mathf.Abs(d - 0.99f) * 90f);
                Color c = gc; float alpha = band;
                if (dark > 0f) { c = Color.Lerp(c, new Color(0.15f, 0.08f, 0.03f), dark); alpha = Mathf.Max(alpha, dark * Mathf.Clamp01((0.997f - d) * 200f)); }
                // four studs at the compass points
                for (int k = 0; k < 4; k++)
                {
                    float ang = k * Mathf.PI * 0.5f + Mathf.PI * 0.25f; Vector2 s = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r * 0.905f;
                    float sd = (p - s).magnitude / (r * 0.06f);
                    if (sd < 1f) { c = Color.Lerp(new Color(1f, 0.95f, 0.75f), new Color(0.7f, 0.15f, 0.12f), Mathf.Clamp01(sd * 1.4f - 0.2f)); alpha = 1f; }
                }
                px[y * n + x] = new Color(c.r, c.g, c.b, alpha);
            }
        tex.SetPixels(px); tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
    static Sprite MakeOrnDisc(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - r, y + 0.5f - r); float d = p.magnitude / r;
                float a = Mathf.Clamp01((0.9f - d) * r * 0.5f);
                // lacquer: lighter in the middle and toward the top, a soft dark edge
                float v = Mathf.Lerp(1f, 0.55f, d * d) * (0.85f + 0.15f * (p.y / r));
                px[y * n + x] = new Color(v, v, v, a);
            }
        tex.SetPixels(px); tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
    // a 9-sliced panel: dark translucent face, a gold double edge and a little diamond in each corner
    static Sprite MakePanel(int n, int border, Color face0, Color face1, float alpha)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int e = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
                Color c = Color.Lerp(face0, face1, y / (float)n); float a = alpha;
                if (e <= 1) { c = new Color(0.12f, 0.07f, 0.03f); a = 1f; }
                else if (e <= 4) { float t = (e - 2) / 2f; c = Color.Lerp(new Color(1f, 0.88f, 0.5f), new Color(0.6f, 0.4f, 0.14f), t); a = 1f; }
                else if (e == 6) { c = new Color(0.85f, 0.65f, 0.3f); a = 0.8f; }
                // the corner diamonds
                foreach (var cx in new[] { 0, n - 1 }) foreach (var cy in new[] { 0, n - 1 })
                    {
                        float dd = Mathf.Abs(x - (cx == 0 ? 7 : n - 8)) + Mathf.Abs(y - (cy == 0 ? 7 : n - 8));
                        if (dd < 5f) { c = Color.Lerp(new Color(1f, 0.95f, 0.7f), new Color(0.75f, 0.2f, 0.12f), dd / 5f); a = 1f; }
                    }
                px[y * n + x] = new Color(c.r, c.g, c.b, a);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
    static Sprite MakeGlow(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude / r; px[y * n + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 2f)); }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    RectTransform FindUI(string name) { var t = FindDeep(transform, name); return t as RectTransform; }
    static Transform FindDeep(Transform t, string name) { if (t.name == name) return t; foreach (Transform c in t) { var r = FindDeep(c, name); if (r != null) return r; } return null; }

    // a round HUD button: lacquer face in its colour, the gold ring over it
    void SkinRound(RectTransform b, float size, Color face)
    {
        if (b == null) return;
        var im = b.GetComponent<Image>(); if (im != null) { im.sprite = ornDisc; im.color = face; }
        b.sizeDelta = new Vector2(size, size);
        var rim = b.Find("Rim") as RectTransform;
        if (rim == null) rim = Img("Rim", b, ornRing, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size, Color.white);
        var ri = rim.GetComponent<Image>(); ri.sprite = ornRing; ri.color = Color.white; rim.sizeDelta = Vector2.one * size * 1.08f;
        rim.SetAsLastSibling();
        foreach (var t in b.GetComponentsInChildren<Text>(true)) { t.color = new Color(1f, 0.95f, 0.82f, t.color.a); t.SetAllDirty(); t.transform.SetAsLastSibling(); }
    }

    void ApplySkin()
    {
        ornRing = MakeOrnRing(160); ornDisc = MakeOrnDisc(128); glow = MakeGlow(64);
        panel9 = MakePanel(48, 14, new Color(0.11f, 0.06f, 0.06f), new Color(0.05f, 0.035f, 0.05f), 0.9f);
        tag9 = MakePanel(32, 9, new Color(0.35f, 0.08f, 0.07f), new Color(0.18f, 0.05f, 0.05f), 0.92f);
        bar9 = MakePanel(24, 6, new Color(0.05f, 0.03f, 0.03f), new Color(0.02f, 0.02f, 0.02f), 0.85f);

        // ---- the round buttons along the top and the side ----
        var lacquer = new Color(0.55f, 0.16f, 0.13f);
        foreach (var n in new[] { "ClassBtn", "BagBtn", "PetsBtn", "MenuBtn" }) SkinRound(FindUI(n), 70f, lacquer);
        SkinRound(FindUI("RideBtn"), 64f, new Color(0.55f, 0.38f, 0.18f));
        SkinRound(FindUI("PotHeal"), 62f, new Color(0.75f, 0.12f, 0.12f));
        SkinRound(FindUI("PotMana"), 62f, new Color(0.18f, 0.3f, 0.8f));
        SkinRound(dodgeBtn, 86f, new Color(0.2f, 0.42f, 0.8f));
        for (int i = 0; i < 6; i++) if (spellBtns[i] != null) { SkinRound(spellBtns[i].rt, 76f, new Color(0.3f, 0.2f, 0.25f)); if (spellBtns[i].cool != null) spellBtns[i].cool.transform.SetSiblingIndex(spellBtns[i].rt.Find("Rim").GetSiblingIndex()); }

        // ---- the attack button: a big round seal instead of the diamond ----
        atkBtn.localRotation = Quaternion.identity; atkBtn.sizeDelta = new Vector2(128, 128);
        atkImg.sprite = ornDisc; atkImg.color = new Color(0.62f, 0.12f, 0.1f);
        foreach (Transform c in atkBtn) c.gameObject.SetActive(false);
        Img("Rim", atkBtn, ornRing, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140), Color.white);
        var atkGlow = Img("Glow", atkBtn, glow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100), new Color(1f, 0.6f, 0.3f, 0.35f)); atkGlow.SetSiblingIndex(0);
        var atkLab = FindUI("AttackLabel"); if (atkLab != null) { var t = atkLab.GetComponent<Text>(); t.text = "ATTACK"; t.fontSize = 22; t.fontStyle = FontStyle.Bold; t.color = new Color(1f, 0.92f, 0.7f); atkLab.SetAsLastSibling(); }

        // ---- the joystick: a gold ring and a lacquer knob ----
        var jb = joyBase.GetComponent<Image>(); jb.sprite = ornRing; jb.color = new Color(1f, 1f, 1f, 0.7f);
        var jk = joyKnob.GetComponent<Image>(); jk.sprite = ornDisc; jk.color = new Color(0.62f, 0.2f, 0.14f, 0.95f);
        Img("Rim", joyKnob, ornRing, new Vector2(0.5f, 0.5f), Vector2.zero, joyKnob.sizeDelta * 1.05f, Color.white);

        // ---- the portrait corner: a round frame with the class sign, the bars beside it in gold frames ----
        var top = FindUI("TopLeft");
        if (top != null)
        {
            foreach (RectTransform c in top) c.anchoredPosition += new Vector2(104f, 0f);
            var por = Img("Portrait", top, ornDisc, new Vector2(0, 1), new Vector2(48, -50), new Vector2(92, 92), new Color(0.35f, 0.12f, 0.1f));
            portraitSign = Center(Label(por, "Sign", "", 44, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(92, 60), Gold));
            portraitSign.fontStyle = FontStyle.Bold;
            Img("Rim", por, ornRing, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 104), Color.white);
            var lvBadge = Img("LvBadge", por, tag9, new Vector2(0.5f, 0f), new Vector2(0, -2), new Vector2(64, 26), Color.white);
            lvBadge.GetComponent<Image>().type = Image.Type.Sliced;
            portraitLv = Center(Label(lvBadge, "Lv", "1", 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 26), Gold)); portraitLv.fontStyle = FontStyle.Bold;
            foreach (var bn in new[] { "HPBg", "ManaBg", "XPBg", "HungerBg" })
            {
                var bg = top.Find(bn) as RectTransform; if (bg == null) continue;
                var bi = bg.GetComponent<Image>(); if (bi == null) bi = bg.gameObject.AddComponent<Image>();
                bi.sprite = bar9; bi.type = Image.Type.Sliced; bi.color = Color.white; bi.raycastTarget = false;
                bg.sizeDelta = new Vector2(Mathf.Min(bg.sizeDelta.x, 330f), bg.sizeDelta.y + 4f);
                var fill = bg.Find(bn.Replace("Bg", "")) as RectTransform; if (fill != null) fill.sizeDelta = bg.sizeDelta - new Vector2(10, 8);
            }
            var shb = top.Find("ShieldBg") as RectTransform; var hpb = top.Find("HPBg") as RectTransform;
            if (shb != null && hpb != null) { shb.sizeDelta = hpb.sizeDelta; var sf = shb.Find("Shield") as RectTransform; if (sf != null) sf.sizeDelta = hpb.sizeDelta - new Vector2(10, 8); }
            foreach (var tn in new[] { "HPText", "ManaText" }) { var t = top.Find(tn) as RectTransform; if (t != null) t.sizeDelta = new Vector2(330, t.sizeDelta.y + 4); }
            var hgT = top.Find("HungerText") as RectTransform; var hgB = top.Find("HungerBg") as RectTransform; if (hgT != null && hgB != null) hgT.anchoredPosition = new Vector2(hgB.anchoredPosition.x + hgB.sizeDelta.x + 8f, hgT.anchoredPosition.y);
            var lvT = top.Find("Level") as RectTransform; if (lvT != null) lvT.gameObject.SetActive(false);   // the level sits on the portrait now
            hpFill.color = new Color(0.86f, 0.16f, 0.14f); manaFill.color = new Color(0.22f, 0.5f, 1f); xpFill.color = new Color(1f, 0.78f, 0.25f);
            nameText.color = new Color(1f, 0.93f, 0.78f); nameText.fontStyle = FontStyle.Bold;
        }

        // ---- the quest tracker: a gold-edged panel on the left with AUTO ----
        questPanel = Img("QuestPanel", transform, panel9, new Vector2(0, 1), new Vector2(178, -252), new Vector2(340, 98), Color.white);
        questPanel.GetComponent<Image>().type = Image.Type.Sliced;
        var tagRt = Img("Tag", questPanel, tag9, new Vector2(0, 1), new Vector2(40, -18), new Vector2(64, 24), Color.white);
        tagRt.GetComponent<Image>().type = Image.Type.Sliced;
        panelTag = Center(Label(tagRt, "T", "MAIN", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 24), Gold)); panelTag.fontStyle = FontStyle.Bold;
        foreach (var t in new[] { trackTitle, trackLine })
        {
            t.transform.SetParent(questPanel, false);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = t.rectTransform.pivot = new Vector2(0, 1);
            t.alignment = TextAnchor.UpperLeft; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        }
        trackTitle.rectTransform.anchoredPosition = new Vector2(78, -7); trackTitle.rectTransform.sizeDelta = new Vector2(176, 24); trackTitle.fontSize = 17; trackTitle.color = Gold;
        trackLine.rectTransform.anchoredPosition = new Vector2(14, -36); trackLine.rectTransform.sizeDelta = new Vector2(312, 56); trackLine.fontSize = 16;
        autoBtn = Img("AutoBtn", questPanel, tag9, new Vector2(1, 1), new Vector2(-40, -18), new Vector2(68, 26), Color.white);
        autoImg = autoBtn.GetComponent<Image>(); autoImg.type = Image.Type.Sliced;
        autoText = Center(Label(autoBtn, "T", "AUTO", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(68, 26), Gold)); autoText.fontStyle = FontStyle.Bold;
        taps.Insert(0, new TapBtn { rt = autoBtn, act = () => AHAuto.Toggle(g) });
        taps.Add(new TapBtn { rt = questPanel, act = () => { if (g.quests.Current != null) OpenQuest(); } });
        RefreshAuto();

        // ---- the chips move to the right under the top buttons, as gold-edged tags ----
        foreach (var n in new[] { "DailyChip", "EvChip" })
        {
            var c = FindUI(n); if (c == null) continue;
            var im = c.GetComponent<Image>(); im.sprite = tag9; im.type = Image.Type.Sliced; im.color = Color.white;
            c.anchorMin = c.anchorMax = new Vector2(1, 1); c.anchoredPosition = new Vector2(-118, n == "DailyChip" ? -118 : -156); c.sizeDelta = new Vector2(196, 32);
        }
        var buffs = FindUI("Buffs"); if (buffs != null) buffs.anchoredPosition = new Vector2(22, -186);

        // ---- the action button and the windows ----
        if (talkBtn != null) { var ti = talkBtn.GetComponent<Image>(); ti.sprite = tag9; ti.type = Image.Type.Sliced; ti.color = new Color(0.55f, 1f, 0.6f); }
        foreach (var n in new[] { "BagWindow", "DialogWindow", "WorkPanel", "CreatorPanel" })
        {
            var w = FindUI(n); if (w == null) continue;
            var im = w.GetComponent<Image>(); im.sprite = panel9; im.type = Image.Type.Sliced; im.color = Color.white;
        }
        if (bannerTitle != null) { bannerTitle.color = Gold; var o = bannerTitle.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.25f, 0.1f, 0.02f, 0.9f); o.effectDistance = new Vector2(2, -2); }
    }
    Text portraitSign, portraitLv;

    // the AUTO button lights up while auto quest runs
    public void RefreshAuto()
    {
        if (autoImg == null) return;
        autoImg.color = AHAuto.On ? new Color(0.5f, 1f, 0.55f) : Color.white;
        autoText.text = AHAuto.On ? "AUTO ●" : "AUTO";
        autoText.color = AHAuto.On ? new Color(0.85f, 1f, 0.85f) : Gold;
    }
    // called every frame from the HUD update
    void TickSkin()
    {
        var p = g.player; if (p == null || p.cls == null) return;
        if (portraitSign != null) { portraitSign.text = p.cls.name.Substring(0, 1); portraitSign.color = Color.Lerp(p.cls.color, Gold, 0.35f); }
        if (portraitLv != null) portraitLv.text = p.level.ToString();
        if (panelTag != null) panelTag.text = g.quests.Current != null ? "MAIN" : "ROAD";
        if (autoBtn != null && autoBtn.gameObject.activeSelf != (g.quests.Current != null)) autoBtn.gameObject.SetActive(g.quests.Current != null);
        if (AHAuto.On && autoText != null) autoText.color = Color.Lerp(new Color(0.6f, 1f, 0.6f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f));
    }
}
