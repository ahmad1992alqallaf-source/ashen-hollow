// Ashen Hollow: the "Celestial Glass" look (FIX_LIST 8, style B). Frosted see-through glass panels in deep night blue,
// thin silver lines, a sky-blue glow (#7fe0ff), soft gold highlights (#f0d58a), rounded shapes; Marcellus for titles
// and Manrope for text (both SIL Open Font Licence, in Resources/AH/Fonts). Drawn in code like the rest of the HUD:
//  - the round buttons are glass discs under a silver ring with a faint sky-blue inner light (their old colours are
//    kept as a tint, so ATTACK is still warm and DODGE still blue);
//  - windows, tags and bars are rounded glass with a silver hairline and a sheen along the top;
//  - every window is restyled after it is drawn: the old brown rows and buttons become glass, the gold edge a sky-blue
//    hairline, the gold words soft gold.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    public static readonly Color CGSky = new Color(0.498f, 0.878f, 1f), CGGold = new Color(0.941f, 0.835f, 0.541f), CGSilver = new Color(0.86f, 0.9f, 0.96f);
    static Sprite cgRound, cgPill, cgRow;
    readonly HashSet<Image> cgDone = new HashSet<Image>();

    // a rounded glass shape drawn at 4x (pixels per unit 400), sliced: night-blue face, a sheen along the top, a silver
    // hairline and a faint sky-blue line inside it
    static Sprite CGGlass(int n, float radius, float alpha, float rimA) { return CGGlass(n, radius, alpha, rimA, Color.white); }
    // baked: the face colour drawn in (for pieces the old skin shows untinted, such as the windows and tags)
    static Sprite CGGlass(int n, float radius, float alpha, float rimA, Color baked)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[n * n]; float h = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - h) - (h - radius), qy = Mathf.Abs(y + 0.5f - h) - (h - radius);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float inside = Mathf.Clamp01(0.5f - d), v = y / (float)(n - 1);
                Color face = Color.Lerp(new Color(0.85f, 0.9f, 1f), Color.white, v) * baked;   // tinted by the Image colour (or baked)
                face.a = 1f;
                float a = alpha * (0.88f + 0.12f * v);
                face = Color.Lerp(face, Color.white, Mathf.Clamp01((v - 0.7f) / 0.3f) * 0.25f);
                float rimK = Mathf.Clamp01(1f - Mathf.Abs(d + 2f) / 2f) * rimA;               // the silver hairline
                float skyK = Mathf.Clamp01(1f - Mathf.Abs(d + 6f) / 2.5f) * 0.35f * rimA;     // the sky-blue line inside it
                bool white = baked.r > 0.9f;
                Color c = Color.Lerp(face, white ? new Color(1.6f, 1.7f, 1.8f) : CGSilver, rimK);
                c = Color.Lerp(c, white ? new Color(0.9f, 1.4f, 1.8f) : CGSky, skyK);
                a = Mathf.Lerp(a, 1f, Mathf.Max(rimK, skyK * 0.6f));
                px[y * n + x] = new Color(c.r, c.g, c.b, a * inside);
            }
        tex.SetPixels(px); tex.Apply();
        int b = Mathf.CeilToInt(radius + 8f);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 400f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }
    // the silver ring over a round button, with a soft sky-blue light just inside it
    static Sprite CGRing(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - r, y + 0.5f - r); float d = p.magnitude / r;
                float lit = 0.5f + 0.5f * Vector2.Dot(p.normalized, new Vector2(-0.6f, 0.8f));
                float band = Mathf.Clamp01((d - 0.905f) * 70f) * Mathf.Clamp01((0.975f - d) * 70f);
                Color silver = Color.Lerp(new Color(0.55f, 0.62f, 0.72f), new Color(0.98f, 1f, 1f), lit);
                float inner = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) / 0.06f), 2f) * 0.55f;   // sky-blue light inside the ring
                Color c = band > inner ? silver : new Color(0.5f, 0.88f, 1f);
                px[y * n + x] = new Color(c.r, c.g, c.b, Mathf.Max(band, inner));
            }
        tex.SetPixels(px); tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
    // a frosted glass disc (tinted by the Image colour): clearer at the middle, a sheen at the top
    static Sprite CGDisc(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - r, y + 0.5f - r); float d = p.magnitude / r;
                float a = Mathf.Clamp01((0.93f - d) * r * 0.5f) * Mathf.Lerp(0.78f, 0.95f, d);
                float v = Mathf.Lerp(0.8f, 1f, d) * (0.9f + 0.1f * (p.y / r));
                float sheen = Mathf.Clamp01(1f - new Vector2(p.x / r * 1.2f, (p.y / r - 0.55f) * 2.6f).magnitude) * 0.35f;
                px[y * n + x] = new Color(Mathf.Min(1f, v + sheen), Mathf.Min(1f, v + sheen), Mathf.Min(1f, v + sheen), a);
            }
        tex.SetPixels(px); tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // the face of a round button: night glass carrying a little of its old colour
    static Color CGFace(Color face) { var c = Color.Lerp(new Color(0.07f, 0.12f, 0.2f), face, 0.32f); c.a = 0.88f; return c; }

    void CelestialSprites()
    {
        ornRing = CGRing(160); ornDisc = CGDisc(128);
        panel9 = CGGlass(160, 30f, 0.9f, 1f, new Color(0.06f, 0.1f, 0.17f)); tag9 = CGGlass(96, 40f, 0.9f, 0.9f, new Color(0.1f, 0.18f, 0.28f)); bar9 = CGGlass(64, 14f, 0.82f, 0.7f, new Color(0.03f, 0.05f, 0.09f));
        cgRound = CGGlass(160, 26f, 1f, 0.9f); cgPill = CGGlass(96, 44f, 1f, 1f); cgRow = CGGlass(128, 18f, 1f, 0.6f);
    }

    // old colour -> glass piece. kind: 0 recolour only, 1 window, 2 row, 3 chosen row, 4 main button (sky glass),
    // 5 plain button (glass), 6 a button you can't press, 7 close / set aside (rose glass), 8 hidden
    struct CGMap { public Color from, to; public int kind; }
    static readonly CGMap[] CGTable =
    {
        new CGMap { from = new Color(0.1f, 0.075f, 0.055f), kind = 1 },                                              // a window
        new CGMap { from = new Color(0.085f, 0.065f, 0.05f), kind = 8 },                                             // its backing (the glass covers it)
        new CGMap { from = new Color(0.85f, 0.65f, 0.3f), to = new Color(0.5f, 0.88f, 1f, 0.55f), kind = 0 },          // the gold edge: a sky-blue hairline
        new CGMap { from = new Color(0.16f, 0.12f, 0.09f), kind = 2 },                                              // a row
        new CGMap { from = new Color(0.13f, 0.1f, 0.08f), kind = 2 },
        new CGMap { from = new Color(0.13f, 0.1f, 0.075f), kind = 2 },
        new CGMap { from = new Color(0.15f, 0.115f, 0.085f), kind = 2 },
        new CGMap { from = new Color(0.32f, 0.24f, 0.14f), kind = 3 },                                              // a chosen row
        new CGMap { from = new Color(0.22f, 0.5f, 0.26f), kind = 4 },                                               // Go
        new CGMap { from = new Color(0.35f, 0.27f, 0.2f), kind = 5 },                                               // Plain
        new CGMap { from = new Color(0.3f, 0.22f, 0.17f), kind = 5 },                                               // Prev / Next
        new CGMap { from = new Color(0.3f, 0.24f, 0.18f), kind = 5 },
        new CGMap { from = new Color(0.22f, 0.16f, 0.11f), kind = 5 },                                              // tabs and side buttons
        new CGMap { from = new Color(0.28f, 0.25f, 0.22f), kind = 6 },                                              // a button you can't press
        new CGMap { from = new Color(0.55f, 0.18f, 0.14f), kind = 7 },                                              // Close
        new CGMap { from = new Color(0.45f, 0.22f, 0.17f), kind = 7 },                                              // Set aside
        new CGMap { from = new Color(0.42f, 0.18f, 0.14f), kind = 7 },
        new CGMap { from = new Color(0.45f, 0.15f, 0.12f), kind = 7 },
    };
    static Sprite cgWin, cgRowB, cgRowSel, cgBtnGo, cgBtnPlain, cgBtnRose;
    static bool Near(Color a, Color b) { return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.03f; }

    // restyle everything under root (after a window is drawn, and the HUD once)
    void Celestial(Transform root)
    {
        if (root == null || cgRound == null) return;
        if (cgWin == null)
        {
            cgWin = CGGlass(160, 40f, 0.9f, 1f, new Color(0.06f, 0.1f, 0.18f));
            cgRowB = CGGlass(128, 22f, 0.72f, 0.55f, new Color(0.11f, 0.17f, 0.27f));
            cgRowSel = CGGlass(128, 22f, 0.9f, 1f, new Color(0.2f, 0.36f, 0.52f));
            cgBtnGo = CGGlass(96, 44f, 0.85f, 1f, new Color(0.3f, 0.56f, 0.82f));
            cgBtnPlain = CGGlass(96, 44f, 0.7f, 0.9f, new Color(0.14f, 0.21f, 0.32f));
            cgBtnRose = CGGlass(96, 44f, 0.8f, 0.9f, new Color(0.45f, 0.22f, 0.32f));
        }
        foreach (var im in root.GetComponentsInChildren<Image>(true))
        {
            if (im.sprite == null)
            {
                foreach (var m in CGTable)
                {
                    if (!Near(im.color, m.from)) continue;
                    float a = im.color.a;
                    switch (m.kind)
                    {
                        case 0: im.color = m.to; break;
                        case 8: im.color = new Color(0f, 0f, 0f, 0f); break;
                        default:
                            im.sprite = m.kind == 1 ? cgWin : m.kind == 2 ? cgRowB : m.kind == 3 ? cgRowSel : m.kind == 4 ? cgBtnGo : m.kind == 7 ? cgBtnRose : cgBtnPlain;
                            im.type = Image.Type.Sliced; im.color = new Color(1f, 1f, 1f, m.kind == 6 ? 0.45f : 1f);
                            break;
                    }
                    break;
                }
            }
            else if (im.sprite == ornDisc && im.color.a > 0.5f && cgDone.Add(im)) im.color = CGFace(im.color);
        }
        foreach (var t in root.GetComponentsInChildren<Text>(true))
        {
            if (t.font == null || t.font.name == "LegacyRuntime" || t.font.name.StartsWith("Nunito") || t.font.name.StartsWith("Cinzel")) t.font = MainFont;
            Color c = t.color;
            if (c.r > 0.95f && c.g > 0.72f && c.g < 0.9f && c.b > 0.25f && c.b < 0.6f) t.color = new Color(CGGold.r, CGGold.g, CGGold.b, c.a);   // the old gold words
        }
    }
    // the windows' frames, once: deep night dim behind, white Marcellus titles, a round glass close button
    void CelestialWindows()
    {
        foreach (var root in new[] { wkRoot, dlgRoot })
        {
            if (root == null) continue;
            var dim = root.GetComponent<Image>(); if (dim != null) dim.color = new Color(0.03f, 0.05f, 0.1f, 0.8f);
        }
        if (wkTitle != null) { wkTitle.font = TitleFont; wkTitle.fontStyle = FontStyle.Normal; wkTitle.color = Color.white; wkTitle.fontSize = 28; }
        if (wkHint != null) wkHint.color = new Color(0.74f, 0.83f, 0.93f);
        if (dlgName != null) { dlgName.font = TitleFont; dlgName.fontStyle = FontStyle.Normal; dlgName.color = Color.white; }
        if (dlgQuest != null) { dlgQuest.font = TitleFont; dlgQuest.fontStyle = FontStyle.Normal; dlgQuest.color = CGGold; }
        if (wkPanel != null)
        {
            var cl = wkPanel.Find("Close") as RectTransform;
            if (cl != null)
            {
                cl.sizeDelta = new Vector2(44, 44);
                var ci = cl.GetComponent<Image>(); ci.sprite = ornDisc; ci.type = Image.Type.Simple; ci.color = new Color(0.05f, 0.08f, 0.15f, 0.7f);
                Img("Rim", cl, ornRing, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(47, 47), Color.white);
                foreach (var t in cl.GetComponentsInChildren<Text>(true)) t.gameObject.SetActive(false);
                Img("CGIcon", cl, CGIcon("close"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18), Color.white);
            }
            var e = wkPanel.Find("Edge"); if (e != null) e.gameObject.SetActive(false);
        }
    }
    void CelestialTitle(Text t) { if (t == null) return; t.font = TitleFont; t.fontStyle = FontStyle.Normal; t.color = CGGold; }
}
