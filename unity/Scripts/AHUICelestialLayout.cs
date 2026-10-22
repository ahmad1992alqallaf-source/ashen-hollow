// Ashen Hollow: the play screen laid out as the Celestial Glass design (the "B-HUD" board of the design canvas).
// Measured from the 1040 x 480 design and scaled to the HUD's 620-unit height (x 1.29):
//  - top left: a glass player card (round portrait with a gold level badge, name in Marcellus and the class at the
//    right, a thin health bar and a thinner mana bar, "HP · Food · XP %" under them); the land and the purse on one
//    line beneath; then the quest with no panel: a gold star, its name in gold Marcellus, a sky-blue AUTO pill, the
//    step, and a sky-blue line saying where to go and how far;
//  - the story line in a glass pill along the top;
//  - top right: a rounded-square minimap with the land and the time on a strip at its foot, a column of small round
//    glass buttons with line icons beside it (map, menu, pets, store, Battle Pass), Daily and the event under it;
//  - the guide: three glowing chevrons on the ground ahead of the hero, pointing the way, with the goal and distance;
//  - a body with loot: a glass "Loot" pill over it in its best item's colour (tap to open it when close);
//  - bottom: a thin glass ring for the joystick, the chat as a glass pill, and on the right the big sky-glass attack
//    seal (a sword, or the action's word) with the skills on an arc round it, dodge, the potions and the bag.
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    const float K = 620f / 480f;
    static Sprite cgCard, cgPillSky, cgPillGlass, cgMask, cgRim, cgAtk;
    static readonly System.Collections.Generic.Dictionary<string, Sprite> cgIcons = new System.Collections.Generic.Dictionary<string, Sprite>();
    Text cgClass, cgXp, cgGuideLine, cgMapStrip, lootPillName; RectTransform cgSword, lootPill, cgStar; Image lootPillImg;

    public static Sprite CGIcon(string n)
    {
        Sprite s; if (cgIcons.TryGetValue(n, out s)) return s;
        var t = Resources.Load<Texture2D>("AH/UI/CG/cg_" + n);
        s = t != null ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f) : null;
        cgIcons[n] = s; return s;
    }
    // a rounded shape, solid white (for masks) or an outline only
    static Sprite CGShape(int n, float radius, bool outline)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[n * n]; float h = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - h) - (h - radius), qy = Mathf.Abs(y + 0.5f - h) - (h - radius);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float a = Mathf.Clamp01(0.5f - d);
                if (outline) a *= Mathf.Clamp01(1f - Mathf.Abs(d + 3f) / 3f) + Mathf.Clamp01(1f - Mathf.Abs(d + 9f) / 6f) * 0.25f;
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        int b = Mathf.CeilToInt(radius + 6f);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 400f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }
    // the attack seal: sky glass, bright at the top, deep blue below, a white rim and a soft halo
    static Sprite CGAttackDisc(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - r, y + 0.5f - r) / r; float d = p.magnitude;
                float t = Mathf.Clamp01(new Vector2(p.x, p.y - 0.4f).magnitude / 1.3f);
                Color c = Color.Lerp(new Color(0.5f, 0.88f, 1f, 0.6f), new Color(0.08f, 0.16f, 0.31f, 0.72f), t);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) / 0.025f);
                float halo = d > 0.8f ? Mathf.Pow(Mathf.Clamp01(1f - (d - 0.8f) / 0.2f), 2f) * 0.45f : 0f;
                if (d <= 0.8f) { c = Color.Lerp(c, new Color(0.86f, 0.96f, 1f, 1f), rim); }
                else c = new Color(0.5f, 0.88f, 1f, Mathf.Max(halo, rim));
                px[y * n + x] = c;
            }
        tex.SetPixels(px); tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    static Vector2 M(float x, float y) { return new Vector2(x * K, y * K); }   // design px -> HUD units
    static void Shade(Text t) { if (t == null || t.GetComponent<Shadow>() != null) return; var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0f, 0f, 0f, 0.75f); s.effectDistance = new Vector2(1f, -1.5f); }
    static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) { if (r == null) return; r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size; }
    Text CGText(Transform parent, string name, int size, TextAnchor a, Vector2 pos, Vector2 box, Color c) { var t = Label(parent, name, "", size, a, pos, box, c); t.font = MainFont; Shade(t); return t; }

    // a small round glass button with a line icon (the label is put away)
    void CGRoundIcon(RectTransform b, float size, string icon, Color face)
    {
        if (b == null) return;
        b.sizeDelta = new Vector2(size, size); b.localScale = Vector3.one;
        var im = b.GetComponent<Image>(); if (im != null) { im.sprite = ornDisc; im.color = face; }
        var rim = b.Find("Rim") as RectTransform; if (rim != null) rim.sizeDelta = Vector2.one * size * 1.06f;
        var ic = CGIcon(icon);
        if (ic != null)
        {
            foreach (var t in b.GetComponentsInChildren<Text>(true)) if (t.name == "T" || t.name == "DodgeLabel") t.gameObject.SetActive(false);
            var old = b.Find("CGIcon"); if (old != null) Destroy(old.gameObject);
            var ir = Img("CGIcon", b, ic, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.46f, new Color(0.9f, 0.97f, 1f, 1f));
            ir.GetComponent<Image>().preserveAspect = true;
        }
    }

    void CelestialLayout()
    {
        if (cgCard == null)
        {
            cgCard = CGGlass(160, 40f, 0.55f, 0.9f, new Color(0.05f, 0.08f, 0.15f));
            cgPillGlass = CGGlass(96, 44f, 0.6f, 0.9f, new Color(0.05f, 0.08f, 0.15f));
            cgPillSky = CGGlass(96, 44f, 0.3f, 1f, new Color(0.35f, 0.75f, 0.95f));
            cgMask = CGShape(128, 40f, false); cgRim = CGShape(128, 40f, true); cgAtk = CGAttackDisc(256);
        }
        Color glassFace = new Color(0.05f, 0.08f, 0.15f, 0.6f);

        // ---- the player card ----
        var top = FindUI("TopLeft");
        if (top != null)
        {
            top.localScale = Vector3.one; Place(top, new Vector2(0, 1), new Vector2(0, 1), M(14, -12), new Vector2(338, 93));
            var card = Img("Card", top, cgCard, new Vector2(0, 1), new Vector2(169, -46.5f), new Vector2(338, 93), Color.white);
            card.GetComponent<Image>().type = Image.Type.Sliced; card.SetAsFirstSibling();
            var por = top.Find("Portrait") as RectTransform;
            if (por != null)
            {
                Place(por, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(16 + 33.5f, -46.5f), new Vector2(67, 67));
                var pi = por.GetComponent<Image>(); pi.sprite = ornDisc; pi.color = new Color(0.22f, 0.38f, 0.62f, 0.95f); pi.name = "CGPortrait";
                var pr = por.Find("Rim") as RectTransform; if (pr != null) pr.sizeDelta = new Vector2(72, 72);
                if (portraitSign != null) { portraitSign.font = TitleFont; portraitSign.fontStyle = FontStyle.Normal; portraitSign.fontSize = 30; portraitSign.rectTransform.sizeDelta = new Vector2(67, 50); }
                var lb = por.Find("LvBadge") as RectTransform;
                if (lb != null)
                {
                    Place(lb, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26, -26), new Vector2(26, 26));
                    var li = lb.GetComponent<Image>(); li.sprite = circle; li.type = Image.Type.Simple; li.color = CGGold;
                    if (portraitLv != null) { portraitLv.color = new Color(0.1f, 0.1f, 0.16f); portraitLv.fontSize = 13; portraitLv.rectTransform.sizeDelta = new Vector2(26, 22); foreach (var o in portraitLv.GetComponents<Shadow>()) Destroy(o); }
                }
            }
            if (nameText != null) { nameText.font = TitleFont; nameText.fontStyle = FontStyle.Normal; nameText.fontSize = 19; nameText.color = Color.white; nameText.rectTransform.anchoredPosition = new Vector2(96, -11); nameText.rectTransform.sizeDelta = new Vector2(160, 24); nameText.horizontalOverflow = HorizontalWrapMode.Overflow; }
            cgClass = CGText(top, "Class", 14, TextAnchor.UpperRight, new Vector2(96, -14), new Vector2(228, 20), new Color(0.74f, 0.83f, 0.93f));
            foreach (var bn in new[] { "HPBg", "ShieldBg", "ManaBg", "XPBg", "HungerBg" })
            {
                var bg = top.Find(bn) as RectTransform; if (bg == null) continue;
                float h = bn == "HPBg" || bn == "ShieldBg" ? 10f : bn == "ManaBg" ? 7f : 0f;
                bg.anchoredPosition = new Vector2(96, bn == "ManaBg" ? -53f : -38f); bg.sizeDelta = new Vector2(228, h);
                var bi = bg.GetComponent<Image>(); if (bi != null) { bi.sprite = cgMask; bi.type = Image.Type.Sliced; bi.color = new Color(1f, 1f, 1f, 0.12f); }
                var fill = bg.Find(bn.Replace("Bg", "")) as RectTransform; if (fill != null) fill.sizeDelta = new Vector2(228, h);
                if (h == 0f) bg.gameObject.SetActive(false);
            }
            if (hpFill != null) hpFill.color = new Color(1f, 0.45f, 0.45f);
            if (manaFill != null) manaFill.color = new Color(0.42f, 0.75f, 1f);
            if (shieldFill != null) shieldFill.color = new Color(0.6f, 0.9f, 1f, 0.6f);
            if (hpText != null) { hpText.alignment = TextAnchor.MiddleLeft; hpText.fontSize = 12; hpText.color = new Color(0.74f, 0.83f, 0.93f); hpText.rectTransform.anchoredPosition = new Vector2(96, -64); hpText.rectTransform.sizeDelta = new Vector2(150, 18); hpText.horizontalOverflow = HorizontalWrapMode.Overflow; Shade(hpText); }
            if (manaText != null) manaText.color = new Color(1f, 1f, 1f, 0f);
            if (hungerText != null) { hungerText.alignment = TextAnchor.MiddleCenter; hungerText.fontSize = 12; hungerText.rectTransform.anchoredPosition = new Vector2(96 + 60, -64); hungerText.rectTransform.sizeDelta = new Vector2(110, 18); }
            cgXp = CGText(top, "XpPct", 12, TextAnchor.MiddleRight, new Vector2(96, -64), new Vector2(228, 18), new Color(0.74f, 0.83f, 0.93f));
            // the land and the purse, one line under the card
            if (clockText != null) { clockText.font = TitleFont; clockText.fontSize = 15; clockText.color = Color.white; clockText.rectTransform.anchoredPosition = new Vector2(8, -101); clockText.rectTransform.sizeDelta = new Vector2(220, 22); Shade(clockText); }
            if (goldText != null) { goldText.fontSize = 14; goldText.fontStyle = FontStyle.Bold; goldText.color = CGGold; goldText.rectTransform.anchoredPosition = new Vector2(180, -102); goldText.rectTransform.sizeDelta = new Vector2(220, 22); Shade(goldText); }
        }
        if (buffText != null) { buffText.fontSize = 13; buffText.rectTransform.anchoredPosition = M(16, -122); Shade(buffText); }

        // ---- the quest, no panel ----
        if (questPanel != null)
        {
            Place(questPanel, new Vector2(0, 1), new Vector2(0, 1), M(18, -134), new Vector2(320, 84));
            questPanel.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var tag = questPanel.Find("Tag"); if (tag != null) tag.gameObject.SetActive(false);
            cgStar = Img("Star", questPanel, CGIcon("star"), new Vector2(0, 1), new Vector2(9, -12), new Vector2(18, 18), CGGold);
            if (trackTitle != null) { trackTitle.font = TitleFont; trackTitle.fontStyle = FontStyle.Normal; trackTitle.fontSize = 18; trackTitle.color = CGGold; trackTitle.rectTransform.anchoredPosition = new Vector2(26, -1); trackTitle.rectTransform.sizeDelta = new Vector2(220, 24); Shade(trackTitle); }
            if (trackLine != null) { trackLine.fontSize = 15; trackLine.color = new Color(0.93f, 0.96f, 1f); trackLine.rectTransform.anchoredPosition = new Vector2(26, -28); trackLine.rectTransform.sizeDelta = new Vector2(290, 22); Shade(trackLine); }
            cgGuideLine = CGText(questPanel, "Guide", 14, TextAnchor.UpperLeft, new Vector2(26, -50), new Vector2(290, 20), new Color(0.62f, 0.9f, 1f));
            if (autoBtn != null)
            {
                Place(autoBtn, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(283, -12), new Vector2(58, 28));
                autoImg.sprite = cgPillSky; autoImg.type = Image.Type.Sliced;
                if (autoText != null) { autoText.fontSize = 12; autoText.rectTransform.sizeDelta = new Vector2(58, 24); }
            }
        }

        // ---- the story pill ----
        if (pill != null)
        {
            Place(pill, new Vector2(0, 1), new Vector2(0, 1), M(300, -14), new Vector2(400, 39));
            var pim = pill.GetComponent<Image>(); pim.sprite = cgPillGlass; pim.type = Image.Type.Sliced; pim.color = Color.white;
            var e = pill.Find("Edge"); if (e != null) e.gameObject.SetActive(false);
            if (pillT != null) { pillT.fontSize = 15; pillT.color = new Color(0.93f, 0.96f, 1f); }
        }

        // ---- the minimap: a rounded square at the top right ----
        if (mmRoot != null)
        {
            Place(mmRoot, new Vector2(1, 1), new Vector2(1, 1), M(-14, -12), new Vector2(170, 170));
            var ri = mmRoot.GetComponent<Image>(); ri.sprite = cgRim; ri.type = Image.Type.Sliced; ri.color = new Color(0.75f, 0.88f, 1f, 0.85f);
            var face = mmRoot.Find("Face") as RectTransform;
            if (face != null)
            {
                face.sizeDelta = new Vector2(164, 164);
                var fi = face.GetComponent<Image>(); fi.sprite = cgMask; fi.type = Image.Type.Sliced; fi.color = new Color(0.23f, 0.33f, 0.38f, 1f);
                var gl = face.Find("Glass"); if (gl != null) gl.gameObject.SetActive(false);
                var strip = Img("Strip", face, white, new Vector2(0.5f, 0f), new Vector2(0, 11), new Vector2(164, 22), new Color(0.05f, 0.08f, 0.15f, 0.62f));
                cgMapStrip = Center(Label(strip, "T", "", 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(164, 20), new Color(0.93f, 0.96f, 1f)));
                cgMapStrip.fontStyle = FontStyle.Bold;
            }
            var halo = Img("Halo", mmRoot, cgRim, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(184, 184), new Color(0.5f, 0.88f, 1f, 0.28f));
            halo.GetComponent<Image>().type = Image.Type.Sliced; halo.SetAsFirstSibling();
        }

        // ---- the column of small round buttons beside the minimap ----
        string[][] col = { new[] { "MapBtn", "map" }, new[] { "MenuBtn", "menu" }, new[] { "PetsBtn", "pets" }, new[] { "StoreBtn", "store" }, new[] { "PassBtn", "pass" } };
        if (FindUI("PassBtn") == null)
        {
            var pb = Img("PassBtn", transform, ornDisc, new Vector2(1, 1), Vector2.zero, new Vector2(49, 49), glassFace);
            Img("Rim", pb, ornRing, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52), Color.white);
            taps.Add(new TapBtn { rt = pb, act = OpenPass });
        }
        // they slide away to the right behind the minimap (and back) with the little glass tab under them
        slide.Clear(); slideHome.Clear();
        for (int i = 0; i < col.Length; i++)
        {
            var b = FindUI(col[i][0]); if (b == null) continue;
            CGRoundIcon(b, 49f, col[i][1], glassFace);
            Place(b, new Vector2(1, 1), new Vector2(0.5f, 0.5f), M(-(158 + 19), -(14 + 19 + i * 46)), new Vector2(49, 49));
            slide.Add(b); slideHome[b] = b.anchoredPosition;
        }
        if (slideBtn != null)
        {
            slideBtn.gameObject.SetActive(true);
            Place(slideBtn, new Vector2(1, 1), new Vector2(0.5f, 0.5f), M(-(158 + 19), -(14 + 19 + col.Length * 46 - 8)), new Vector2(40, 26));
            var si = slideBtn.GetComponent<Image>(); si.sprite = cgPillGlass != null ? cgPillGlass : si.sprite; si.type = Image.Type.Sliced; si.color = Color.white;
            var sr = slideBtn.Find("Rim"); if (sr != null) sr.gameObject.SetActive(false);
            if (slideText != null) { slideText.fontSize = 14; slideText.color = new Color(0.93f, 0.96f, 1f); slideText.rectTransform.sizeDelta = new Vector2(40, 24); }
            if (!taps.Exists(t => t.rt == slideBtn)) taps.Insert(0, new TapBtn { rt = slideBtn, act = () => { slideHidden = !slideHidden; try { AHPrefs.SetInt("ah_set_hud_tucked", slideHidden ? 1 : 0); } catch { } } });
        }
        // Daily and the event, plain words under the minimap
        int ci = 0;
        foreach (var n in new[] { "DailyChip", "EvChip" })
        {
            var c = FindUI(n); if (c == null) continue;
            Place(c, new Vector2(1, 1), new Vector2(1, 1), M(-14, -152 - ci * 16), new Vector2(170, 20)); ci++;
            var im = c.GetComponent<Image>(); if (im != null) im.color = new Color(1f, 1f, 1f, 0f);
            foreach (var t in c.GetComponentsInChildren<Text>(true)) { t.fontSize = 13; t.alignment = TextAnchor.MiddleRight; t.rectTransform.sizeDelta = new Vector2(170, 20); Shade(t); }
        }

        // ---- the joystick: a thin glass ring and a glass knob ----
        if (joyBase != null)
        {
            Place(joyBase, Vector2.zero, new Vector2(0.5f, 0.5f), M(94, 86), new Vector2(150, 150));
            var jb = joyBase.GetComponent<Image>(); jb.sprite = ring; jb.type = Image.Type.Simple; jb.color = new Color(0.75f, 0.88f, 1f, 0.55f);
            var fill = Img("Fill", joyBase, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(146, 146), new Color(0.05f, 0.08f, 0.15f, 0.22f));
            fill.SetAsFirstSibling();
        }
        if (joyKnob != null)
        {
            joyKnob.sizeDelta = new Vector2(59, 59);
            var jk = joyKnob.GetComponent<Image>(); jk.sprite = ornDisc; jk.color = new Color(0.12f, 0.2f, 0.32f, 0.75f);
            var kr = joyKnob.Find("Rim") as RectTransform; if (kr != null) kr.sizeDelta = new Vector2(64, 64);
        }

        // ---- the right hand: attack seal, skills on an arc, dodge, potions, bag ----
        Vector2 atk = new Vector2(-108f, 98f);
        if (atkBtn != null)
        {
            Place(atkBtn, new Vector2(1, 0), new Vector2(0.5f, 0.5f), atk, new Vector2(126, 126));
            atkImg.sprite = cgAtk; atkImg.color = Color.white;
            foreach (Transform c in atkBtn) if (c.name == "Rim" || c.name == "Glow") c.gameObject.SetActive(false);
            cgSword = Img("Sword", atkBtn, CGIcon("sword"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52), Color.white);
            cgSword.GetComponent<Image>().preserveAspect = true;
            var al = FindUI("AttackLabel"); if (al != null) { al.anchorMin = al.anchorMax = new Vector2(1, 0); al.anchoredPosition = atk; }
            if (atkLabelText != null) { atkLabelText.color = Color.white; Shade(atkLabelText); }
        }
        float[] ang = { 205f, 168f, 131f, 94f, 57f };
        for (int i = 0; i < 6; i++)
        {
            var sb = spellBtns[i]; if (sb == null) continue;
            Vector2 pos = i < 5 ? atk + new Vector2(Mathf.Cos(ang[i] * Mathf.Deg2Rad), Mathf.Sin(ang[i] * Mathf.Deg2Rad)) * 112f : new Vector2(-60f, 276f);
            sb.rt.anchoredPosition = pos; sb.rt.sizeDelta = new Vector2(67, 67);
            sb.bg.sprite = ornDisc; sb.bg.color = glassFace;
            var r = sb.rt.Find("Rim") as RectTransform; if (r != null) r.sizeDelta = new Vector2(71, 71);
            if (sb.cool != null) sb.cool.rectTransform.sizeDelta = new Vector2(63, 63);
            if (sb.icon != null) sb.icon.rectTransform.sizeDelta = new Vector2(58, 58);
        }
        var dodge = dodgeBtn; if (dodge != null) { CGRoundIcon(dodge, 59f, "dodge", glassFace); Place(dodge, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-290f, 40f), new Vector2(59, 59)); }
        foreach (var kv in new[] { new object[] { "PotHeal", new Vector2(-186f, 268f), new Color(1f, 0.54f, 0.54f) }, new object[] { "PotMana", new Vector2(-245f, 243f), new Color(0.56f, 0.76f, 1f) } })
        {
            var b = FindUI((string)kv[0]); if (b == null) continue;
            b.sizeDelta = new Vector2(52, 52); b.anchoredPosition = (Vector2)kv[1];
            var im = b.GetComponent<Image>(); im.sprite = ornDisc; im.color = glassFace;
            var r = b.Find("Rim") as RectTransform; if (r != null) r.sizeDelta = new Vector2(55, 55);
            var cool = b.Find("Cool") as RectTransform; if (cool != null) cool.sizeDelta = new Vector2(48, 48);
            var t = b.Find("T"); if (t != null) t.gameObject.SetActive(false);
            var ic = Img("CGIcon", b, CGIcon("potion"), new Vector2(0.5f, 0.5f), new Vector2(0, 3), new Vector2(24, 24), (Color)kv[2]); ic.GetComponent<Image>().preserveAspect = true;
            var nT = b.Find("N") as RectTransform; if (nT != null) { nT.anchoredPosition = new Vector2(14, -14); var tt = nT.GetComponent<Text>(); tt.fontSize = 13; tt.fontStyle = FontStyle.Bold; tt.color = Color.white; Shade(tt); }
        }
        var bag = FindUI("BagBtn"); if (bag != null) { CGRoundIcon(bag, 57f, "bag", glassFace); Place(bag, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-46f, 338f), new Vector2(57, 57)); }
        var leap = FindUI("LeapBtn"); if (leap != null) { CGRoundIcon(leap, 52f, "leap", glassFace); Place(leap, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-46f, 410f), new Vector2(52, 52)); }
        var emo = FindUI("EmoteBtn"); if (emo != null) { CGRoundIcon(emo, 44f, "emote", glassFace); Place(emo, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-352f, 52f), new Vector2(44, 44)); }
        var ride = FindUI("RideBtn"); if (ride != null) { CGRoundIcon(ride, 52f, "ride", glassFace); ride.localScale = Vector3.one; ride.anchoredPosition = M(178, 34); }
        if (modeBtn != null) { modeBtn.anchorMin = modeBtn.anchorMax = new Vector2(1, 0); modeBtn.anchoredPosition = new Vector2(-300f, 122f); if (modeGlow != null) { modeGlow.rectTransform.anchorMin = modeGlow.rectTransform.anchorMax = new Vector2(1, 0); modeGlow.rectTransform.anchoredPosition = new Vector2(-300f, 122f); } }

        // ---- the chat, a glass pill along the bottom ----
        if (chatBox != null)
        {
            var ci2 = chatBox.GetComponent<Image>(); ci2.sprite = cgPillGlass; ci2.type = Image.Type.Sliced; ci2.color = Color.white;
            Place(chatBox, Vector2.zero, new Vector2(0f, 0.5f), new Vector2(172 * K, 10 * K + 17), new Vector2(430, 34));
            var hide = chatBox.Find("Hide") as RectTransform; if (hide != null) { var hi = hide.GetComponent<Image>(); hi.sprite = circle; hi.color = new Color(1f, 1f, 1f, 0.12f); hide.sizeDelta = new Vector2(24, 24); hide.anchoredPosition = new Vector2(-18, 0); }
            if (chatText != null) { chatText.fontSize = 14; chatText.rectTransform.sizeDelta = new Vector2(380, 28); }
        }
        if (chatPill != null) { var cp = chatPill.GetComponent<Image>(); cp.sprite = cgPillGlass; cp.type = Image.Type.Sliced; cp.color = Color.white; }

        // ---- the hero's name over the head, in Marcellus ----
        if (heroPlateText != null) { heroPlateText.font = TitleFont; heroPlateText.fontStyle = FontStyle.Normal; heroPlateText.fontSize = 16; heroPlateText.color = Color.white; Shade(heroPlateText); }

        // ---- the loot pill over a body ----
        lootPill = Img("LootPill", transform, cgPillGlass, new Vector2(0, 0), Vector2.zero, new Vector2(96, 38), Color.white);
        lootPillImg = lootPill.GetComponent<Image>(); lootPillImg.type = Image.Type.Sliced;
        var lic = Img("Icon", lootPill, CGIcon("loot"), new Vector2(0f, 0.5f), new Vector2(22, 0), new Vector2(19, 19), new Color(0.62f, 0.82f, 1f)); lic.GetComponent<Image>().preserveAspect = true;
        var ltx = Center(Label(lootPill, "T", "Loot", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(60, 30), new Color(0.89f, 0.95f, 1f))); ltx.fontStyle = FontStyle.Bold; ltx.rectTransform.anchoredPosition = new Vector2(12, 0);
        lootPillName = Center(Label(lootPill, "Name", "", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(220, 20), new Color(0.84f, 0.9f, 0.97f))); lootPillName.rectTransform.anchoredPosition = new Vector2(0, 30); Shade(lootPillName);
        taps.Insert(0, new TapBtn { rt = lootPill, act = () => { var m = lootPillMob; if (m == null || !m.HasLoot) return; float d = Vector3.Distance(m.transform.position, g.player.transform.position); if (d <= 3.6f) OpenLoot(m); else Toast("Walk up to the body to loot it.", 1.4f); } });
        lootPill.gameObject.SetActive(false);
        CelestialBag();   // the bag as the design (AHUICelestialBag)
        CelestialWindows();   // the windows' frames
        PlusLayout();         // the quest card, the timer pill, the menu badge, the layout editor (AHUIPlus)
    }
    AHMob lootPillMob;

    // after the HUD's own update each frame, so its words are the last ones written
    void LateUpdate() { CelestialTick(); CelestialBagTick(); LootAllTick(); PlusTick(); }

    // every frame: the words that follow the game, the loot pill, the attack seal's sword
    void CelestialTick()
    {
        var p = g.player; if (p == null || p.cls == null) return;
        if (nameText != null) nameText.text = string.IsNullOrEmpty(p.heroName) ? p.cls.name : p.heroName;
        if (cgClass != null) cgClass.text = AHEvo.Title(p);
        if (cgXp != null) cgXp.text = "XP " + Mathf.FloorToInt(p.XpFrac * 100f) + "%";
        if (hpText != null && !hpText.text.StartsWith("HP")) { string ht = hpText.text; int cut = ht.IndexOf("  ("); if (cut > 0) ht = ht.Substring(0, cut); hpText.text = "HP " + ht; }
        if (clockText != null && goldText != null) goldText.rectTransform.anchoredPosition = new Vector2(8 + clockText.preferredWidth + 12f, -102);
        if (cgMapStrip != null) cgMapStrip.text = (g.data != null ? g.data.region : "") + " · " + System.DateTime.Now.ToString("HH:mm");
        // the seal: a sword when it attacks, the word when it does something else
        if (cgSword != null && atkLabelText != null)
        {
            bool attack = atkLabelText.text == "ATTACK";
            if (cgSword.gameObject.activeSelf != attack) cgSword.gameObject.SetActive(attack);
            atkLabelText.enabled = !attack;
        }
        // the quest's way and distance under the step
        if (cgGuideLine != null) cgGuideLine.text = hasQ ? "↗ " + (qLabel ?? "Quest") + " · " + Mathf.RoundToInt(Vector3.Distance(new Vector3(qAt.x, 0, qAt.z), new Vector3(p.transform.position.x, 0, p.transform.position.z))) + " m" : "";
        if (cgStar != null) cgStar.gameObject.SetActive(g.quests.Current != null);
        // the loot pill over the nearest body with loot (within 14 m, on screen)
        if (lootPill != null)
        {
            AHMob best = null; float bd = 14f;
            if (Modal == 0 && !PhotoOn) foreach (var m in g.mobs) { if (m == null || !m.HasLoot) continue; float d = Vector3.Distance(m.transform.position, p.transform.position); if (d < bd) { bd = d; best = m; } }
            var cam = Camera.main; bool on = best != null && cam != null;
            Vector3 sp = Vector3.zero;
            if (on) { sp = cam.WorldToScreenPoint(best.transform.position + Vector3.up * 1.3f); on = sp.z > 0f; }
            if (lootPill.gameObject.activeSelf != on) lootPill.gameObject.SetActive(on);
            lootPillMob = best;
            if (on)
            {
                Vector2 lp; RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)lootPill.parent, sp, null, out lp);
                var pr = (RectTransform)lootPill.parent;
                lootPill.anchorMin = lootPill.anchorMax = new Vector2(0.5f, 0.5f); lootPill.anchoredPosition = lp + pr.rect.size * (pr.pivot - new Vector2(0.5f, 0.5f));
                Color q = AHLootGlow.Best(best);
                lootPillImg.color = Color.Lerp(Color.white, q, 0.35f);
                lootPillName.text = best.type.name;
            }
        }
    }
}
