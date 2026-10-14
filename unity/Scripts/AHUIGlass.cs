// Ashen Hollow: the HUD's bars and tags, sharp and easy to read. The quest tracker, the Daily and event tags and the
// lines under your name were small blurry gold-edged panels with soft doubled text; now they are dark glass panels
// drawn at four times the detail (smooth rounded corners, a fine gold rim, a soft light along the top), and their
// words are set in a rounded bold game font (Nunito, SIL Open Font Licence; kept on the PC like the art, the game
// falls back to the built-in font without it) with a tight dark edge instead of a long drop shadow.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    Font hudFont;
    static Sprite glassPanel, glassTag, glassTagGold;

    // a rounded panel drawn at 4x and shown at 1x (pixels per unit 400): crisp at any screen size
    static Sprite Glass(int n, float radius, Color top, Color bottom, Color rim, float rimW, float alpha)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[n * n]; float h = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // distance to the rounded edge (negative inside)
                float qx = Mathf.Abs(x + 0.5f - h) - (h - radius), qy = Mathf.Abs(y + 0.5f - h) - (h - radius);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float inside = Mathf.Clamp01(0.5f - d);                       // anti-aliased outline of the shape
                float v = y / (float)(n - 1);
                Color face = Color.Lerp(bottom, top, v);
                // a soft sheen along the top third
                face = Color.Lerp(face, Color.white, Mathf.Clamp01((v - 0.62f) / 0.38f) * 0.07f);
                float a = alpha;
                // the rim: a fine bright line just inside the edge, and a darker line inside it
                float rimK = Mathf.Clamp01(1f - Mathf.Abs(d + rimW * 0.5f + 0.5f) / (rimW * 0.5f + 0.5f));
                float darkK = Mathf.Clamp01(1f - Mathf.Abs(d + rimW + 2.5f) / 1.5f) * 0.6f;
                Color c = Color.Lerp(face, Color.black, darkK);
                c = Color.Lerp(c, rim, rimK); a = Mathf.Lerp(a, 1f, Mathf.Max(rimK, darkK * 0.5f));
                px[y * n + x] = new Color(c.r, c.g, c.b, a * inside);
            }
        tex.SetPixels(px); tex.Apply();
        int b = Mathf.CeilToInt(radius + rimW + 4f);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 400f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    // a tight dark edge round the letters (not a long shadow that doubles them)
    static void Crisp(Text t, Font f, int size = 0, bool bold = false)
    {
        if (t == null) return;
        if (f != null) { t.font = f; t.fontStyle = FontStyle.Normal; } else if (bold) t.fontStyle = FontStyle.Bold;
        if (size > 0) t.fontSize = size;
        foreach (var s in t.GetComponents<Shadow>()) Object.Destroy(s);
        var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0f, 0f, 0f, 0.85f); o.effectDistance = new Vector2(1.1f, -1.1f);
    }

    void SkinGlass()
    {
        hudFont = Resources.Load<Font>("AH/Fonts/Nunito-ExtraBold");
        if (glassPanel == null)
        {
            glassPanel = Glass(160, 28f, new Color(0.1f, 0.09f, 0.11f), new Color(0.03f, 0.03f, 0.05f), new Color(0.95f, 0.76f, 0.38f), 3.5f, 0.78f);
            glassTag = Glass(96, 40f, new Color(0.13f, 0.1f, 0.12f), new Color(0.05f, 0.04f, 0.06f), new Color(0.95f, 0.76f, 0.38f), 3f, 0.85f);
            glassTagGold = Glass(96, 40f, new Color(0.62f, 0.16f, 0.12f), new Color(0.38f, 0.07f, 0.06f), new Color(1f, 0.85f, 0.5f), 3f, 0.95f);
        }
        // the quest tracker
        if (questPanel != null)
        {
            var im = questPanel.GetComponent<Image>(); im.sprite = glassPanel; im.type = Image.Type.Sliced; im.color = Color.white;
            questPanel.sizeDelta = new Vector2(290, 72); questPanel.anchoredPosition = new Vector2(150, -208);   // (clear of the buffs line above it)
            Crisp(trackTitle, hudFont, 15, true); Crisp(trackLine, hudFont, 14);
            trackLine.color = new Color(0.86f, 0.95f, 0.8f);
            trackTitle.rectTransform.sizeDelta = new Vector2(168, 22); trackLine.rectTransform.sizeDelta = new Vector2(278, 42);
            var tag = questPanel.Find("Tag"); if (tag != null) { var ti = tag.GetComponent<Image>(); ti.sprite = glassTagGold; ti.type = Image.Type.Sliced; }
            Crisp(panelTag, hudFont, 11, true); panelTag.color = new Color(1f, 0.93f, 0.75f);
            if (autoBtn != null) { autoImg.sprite = glassTag; autoImg.type = Image.Type.Sliced; }
            Crisp(autoText, hudFont, 12, true);
        }
        // the Daily and event tags
        foreach (var n in new[] { "DailyChip", "EvChip" })
        {
            var c = FindUI(n); if (c == null) continue;
            var im = c.GetComponent<Image>(); im.sprite = glassTag; im.type = Image.Type.Sliced; im.color = Color.white;
            c.sizeDelta = new Vector2(190, 28);
            foreach (var t in c.GetComponentsInChildren<Text>(true)) { Crisp(t, hudFont, 14); t.color = n == "DailyChip" ? new Color(1f, 0.88f, 0.55f) : new Color(0.85f, 0.93f, 1f); }
        }
        // the lines under your name: where you are, your purse, your buffs
        Crisp(clockText, hudFont, 0); Crisp(goldText, hudFont, 0); Crisp(buffText, hudFont, 0);
        Crisp(nameText, hudFont, 0); Crisp(lvText, hudFont, 0); Crisp(hpText, hudFont, 0); Crisp(manaText, hudFont, 0); Crisp(hungerText, hudFont, 0);
    }
}
