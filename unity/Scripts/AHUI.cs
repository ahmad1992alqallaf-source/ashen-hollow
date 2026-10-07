// Ashen Hollow: the on-screen controls and HUD, built in code (no EventSystem needed).
// Left thumb: joystick. Right: attack diamond, five spells around it, and dodge.
// Drag anywhere else to turn the camera, pinch to zoom.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI : MonoBehaviour
{
    // read by the game each frame
    [HideInInspector] public Vector2 stick;
    [HideInInspector] public bool attackHeld, dodgePressed, leapPressed;
    [HideInInspector] public readonly bool[] spellPressed = new bool[6];
    [HideInInspector] public Vector2 camDelta;
    [HideInInspector] public float zoom = 1f;
    public bool PickerOpen { get { return picker != null && picker.gameObject.activeSelf; } }
    public bool BagOpen { get { return bagRoot != null && bagRoot.gameObject.activeSelf; } }
    // which window is in front: 0 none (the game), 1 class picker, 2 bag, 3 talking, 4 making your hero, 5 crafting / professions
    public int Modal { get { return AHCine.Active ? 8 : ChatOpen ? 9 : InspectOpen ? 7 : SelectOpen ? 6 : CreatorOpen ? 4 : WorkOpen ? 5 : PickerOpen ? 1 : BagOpen ? 2 : DialogOpen ? 3 : 0; } }

    AHGame g;
    Canvas canvas;
    Font font;
    Sprite circle, ring, white;
    RectTransform joyBase, joyKnob, atkBtn, dodgeBtn, picker;
    Image hpFill, xpFill, atkImg, shieldFill, manaFill, hungerFill;
    Text hpText, manaText, lvText, nameText, bannerTitle, bannerSub, clockText, toastText, goldText, hungerText;
    CanvasGroup bannerGroup, toastGroup;
    float bannerT, toastT, toastShown;
    readonly List<KeyValuePair<string, float>> toastQ = new List<KeyValuePair<string, float>>();

    class SpellBtn { public RectTransform rt; public Image bg, cool, icon; public Text label, secs, name; public float flashT; }
    readonly SpellBtn[] spellBtns = new SpellBtn[6];

    class TapBtn { public RectTransform rt; public Action act; public int layer; public string group; }
    void RemoveTaps(string group) { taps.RemoveAll(t => t.group == group); }
    readonly List<TapBtn> taps = new List<TapBtn>();

    enum Role { Joy, Attack, Dodge, Cam, Spell, Tap, None }
    class Ptr { public Role role; public Vector2 start, last; public int index; public TapBtn tap; public bool dragged; }
    readonly Dictionary<int, Ptr> ptrs = new Dictionary<int, Ptr>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> gone = new List<int>();

    class Plate { public RectTransform rt; public Text label; public Image fill; }
    readonly List<Plate> plates = new List<Plate>();
    class Floater { public RectTransform rt; public Text text; public Vector3 world; public float t; }
    readonly List<Floater> floaters = new List<Floater>();
    int nextFloat;
    float pinchLast;

    const float JoyR = 70f;   // in canvas units (reference 1100 x 620)
    static readonly Vector2 AtkPos = new Vector2(-175, 160);

    public static AHUI Create(AHGame game)
    {
        var go = new GameObject("HUD");
        var ui = go.AddComponent<AHUI>();
        ui.g = game;
        ui.Build();
        ui.WrapSafe();
        return ui;
    }

    // ---------- the phone's safe area ----------
    // Everything the HUD drew goes into one box that matches the screen's safe area, so nothing sits under a
    // camera hole, a notch or rounded corners. Full-screen layers (the fade, dimmers) still reach the edges.
    RectTransform safeRt; Rect safeNow; readonly List<RectTransform> fullLayers = new List<RectTransform>();
    void WrapSafe()
    {
        var go = new GameObject("SafeArea", typeof(RectTransform));
        safeRt = (RectTransform)go.transform; safeRt.SetParent(transform, false);
        var kids = new List<Transform>(); foreach (Transform c in transform) if (c != safeRt) kids.Add(c);
        foreach (var c in kids)
        {
            var rt = c as RectTransform; c.SetParent(safeRt, false);
            if (rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && rt.offsetMin == Vector2.zero && rt.offsetMax == Vector2.zero) fullLayers.Add(rt);
        }
        safeNow = new Rect(-1, -1, 0, 0); FitSafe();
    }
    void FitSafe()
    {
        if (safeRt == null) return;
        // wide phones scale the HUD by height; near-square screens (unfolded phones, tablets) by width, so the HUD
        // keeps its layout instead of piling up in the middle
        var sc = GetComponent<CanvasScaler>();
        if (sc != null && Screen.height > 0)
        {
            float a = (float)Screen.width / Screen.height, m = Mathf.InverseLerp(1.3f, 1.75f, a);
            if (Mathf.Abs(sc.matchWidthOrHeight - m) > 0.001f) { sc.matchWidthOrHeight = m; safeNow = new Rect(-1, -1, 0, 0); }
        }
        Rect sa = Screen.safeArea; if (sa == safeNow || Screen.width <= 0 || Screen.height <= 0) return;
        safeNow = sa;
        Vector2 size = new Vector2(Screen.width, Screen.height);
        safeRt.anchorMin = new Vector2(sa.xMin / size.x, sa.yMin / size.y);
        safeRt.anchorMax = new Vector2(sa.xMax / size.x, sa.yMax / size.y);
        safeRt.offsetMin = safeRt.offsetMax = Vector2.zero;
        float k = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        foreach (var rt in fullLayers)
        {
            if (rt == null) continue;
            rt.offsetMin = new Vector2(-sa.xMin, -sa.yMin) / k;
            rt.offsetMax = new Vector2(size.x - sa.xMax, size.y - sa.yMax) / k;
        }
    }

    // ---------- building ----------
    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1100, 620);
        scaler.matchWidthOrHeight = 1f;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        circle = MakeCircle(128, 0f);
        ring = MakeCircle(128, 10f);
        white = MakeWhite();

        // portrait corner: class, level, health, shield and experience
        var top = Box("TopLeft", transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -16), new Vector2(400, 120));
        nameText = Label(top, "Name", "Warrior", 26, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(300, 34), Color.white);
        lvText = Label(top, "Level", "Lv 1", 24, TextAnchor.UpperRight, new Vector2(0, 0), new Vector2(370, 34), new Color(1f, 0.85f, 0.4f));
        hpFill = Bar(top, "HP", new Vector2(0, -38), new Vector2(370, 28), new Color(0.82f, 0.13f, 0.16f));
        shieldFill = Bar(top, "Shield", new Vector2(0, -38), new Vector2(370, 28), new Color(0.5f, 0.8f, 1f, 0.55f), false);
        hpText = Label(top, "HPText", "100 / 100", 19, TextAnchor.MiddleCenter, new Vector2(0, -38), new Vector2(370, 28), Color.white);
        manaFill = Bar(top, "Mana", new Vector2(0, -69), new Vector2(370, 18), new Color(0.25f, 0.5f, 1f));
        manaText = Label(top, "ManaText", "", 14, TextAnchor.MiddleCenter, new Vector2(0, -69), new Vector2(370, 18), Color.white);
        xpFill = Bar(top, "XP", new Vector2(0, -90), new Vector2(370, 10), new Color(0.95f, 0.72f, 0.2f));
        hungerFill = Bar(top, "Hunger", new Vector2(0, -102), new Vector2(260, 8), new Color(0.9f, 0.55f, 0.25f));
        // the food bar says what it is, and turns into a warning when hunger stops your health coming back
        hungerText = Label(top, "HungerText", "Food", 14, TextAnchor.MiddleLeft, new Vector2(268, -97), new Vector2(110, 18), new Color(1f, 0.8f, 0.55f));
        clockText = Label(top, "Clock", "", 19, TextAnchor.UpperLeft, new Vector2(0, -114), new Vector2(370, 26), new Color(1f, 1f, 1f, 0.8f));
        goldText = Label(top, "Gold", "", 19, TextAnchor.UpperLeft, new Vector2(0, -138), new Vector2(370, 26), new Color(1f, 0.84f, 0.35f));

        // class button, top right
        var clsBtn = Img("ClassBtn", transform, circle, new Vector2(1, 1), new Vector2(-60, -55), new Vector2(76, 76), new Color(0.2f, 0.14f, 0.1f, 0.9f));
        Img("Rim", clsBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 76), new Color(0.85f, 0.6f, 0.25f, 1f));
        Center(Label(clsBtn, "T", "CLASS", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(76, 30), Color.white));
        taps.Add(new TapBtn { rt = clsBtn, act = () => ShowPicker(true) });

        // bag button, next to it
        var bagBtn = Img("BagBtn", transform, circle, new Vector2(1, 1), new Vector2(-148, -55), new Vector2(76, 76), new Color(0.2f, 0.14f, 0.1f, 0.9f));
        Img("Rim", bagBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 76), new Color(0.85f, 0.6f, 0.25f, 1f));
        Center(Label(bagBtn, "T", "BAG", 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(76, 30), Color.white));
        taps.Add(new TapBtn { rt = bagBtn, act = () => ShowBag(true) });

        // joystick, bottom left
        joyBase = Img("JoyBase", transform, ring, new Vector2(0, 0), new Vector2(170, 155), new Vector2(JoyR * 2.3f, JoyR * 2.3f), new Color(1f, 0.85f, 0.5f, 0.55f));
        joyKnob = Img("JoyKnob", joyBase, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(JoyR * 1.05f, JoyR * 1.05f), new Color(0.85f, 0.65f, 0.3f, 0.9f));

        // attack diamond, spells around it, dodge in the corner
        atkBtn = Img("Attack", transform, white, new Vector2(1, 0), AtkPos, new Vector2(112, 112), new Color(0.2f, 0.14f, 0.1f, 0.85f));
        atkBtn.localRotation = Quaternion.Euler(0, 0, 45);
        atkImg = atkBtn.GetComponent<Image>();
        var atkRim = Img("Rim", atkBtn, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100), new Color(0.85f, 0.6f, 0.25f, 0.9f));
        Img("Inner", atkRim, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88), new Color(0.22f, 0.15f, 0.1f, 1f));
        var atkLabel = Label(transform, "AttackLabel", "ATTACK", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(140, 40), Color.white);
        Anchor(atkLabel.rectTransform, new Vector2(1, 0), AtkPos);
        for (int i = 0; i < 6; i++)
        {
            float a = (98f + i * 33f) * Mathf.Deg2Rad;   // far enough apart that a thumb never hits the neighbour
            Vector2 pos = i < 5 ? AtkPos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 146f : new Vector2(-140f, 44f);   // the sixth: the ultimate (level 60 form)
            var b = new SpellBtn();
            b.rt = Img("Spell" + i, transform, circle, new Vector2(1, 0), pos, new Vector2(76, 76), new Color(0.3f, 0.2f, 0.15f, 0.92f));
            b.bg = b.rt.GetComponent<Image>();
            // the spell's painted icon sits inside the gold rim, under the cooldown shade
            b.icon = Img("Icon", b.rt, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70), Color.white).GetComponent<Image>();
            b.icon.preserveAspect = true; b.icon.enabled = false;
            Img("Rim", b.rt, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 76), new Color(0.9f, 0.7f, 0.35f, 1f));
            var cool = Img("Cool", b.rt, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72), new Color(0f, 0f, 0f, 0.62f));
            b.cool = cool.GetComponent<Image>();
            b.cool.type = Image.Type.Filled; b.cool.fillMethod = Image.FillMethod.Radial360; b.cool.fillOrigin = (int)Image.Origin360.Top; b.cool.fillClockwise = false;
            b.label = Center(Label(b.rt, "Abbr", "", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(76, 30), Color.white));
            b.label.fontStyle = FontStyle.Bold;
            b.secs = Center(Label(b.rt, "Secs", "", 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(76, 40), new Color(1f, 0.95f, 0.8f)));
            // the spell's name, shown above the button for a moment when you cast it
            b.name = Center(Label(b.rt, "Name", "", 17, TextAnchor.MiddleCenter, new Vector2(0, 58), new Vector2(200, 26), new Color(1f, 0.93f, 0.75f, 0f)));
            b.name.rectTransform.anchoredPosition = new Vector2(0, 58); b.name.fontStyle = FontStyle.Bold;
            spellBtns[i] = b;
            if (i == 5) b.rt.gameObject.SetActive(false);
        }
        dodgeBtn = Img("Dodge", transform, circle, new Vector2(1, 0), new Vector2(-62, 62), new Vector2(88, 88), new Color(0.2f, 0.45f, 0.85f, 0.9f));
        Center(Label(dodgeBtn, "DodgeLabel", "DODGE", 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(88, 40), Color.white));

        // banner and short messages, centre
        var ban = Box("Banner", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(900, 140));
        bannerGroup = ban.gameObject.AddComponent<CanvasGroup>();
        bannerTitle = Label(ban, "Title", "", 56, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(900, 80), new Color(0.95f, 0.6f, 0.25f));
        bannerTitle.fontStyle = FontStyle.Bold;
        bannerSub = Label(ban, "Sub", "", 24, TextAnchor.MiddleCenter, new Vector2(0, -78), new Vector2(900, 40), Color.white);
        foreach (var t in new[] { bannerTitle, bannerSub }) { t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f); t.rectTransform.pivot = new Vector2(0.5f, 1f); }
        bannerGroup.alpha = 0f;
        var toast = Box("Toast", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(600, 40));
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastText = Center(Label(toast, "Text", "", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1000, 40), new Color(1f, 0.9f, 0.7f)));
        toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
        toastGroup.alpha = 0f;

        // name plates over beasts, and floating numbers
        for (int i = 0; i < 12; i++)
        {
            var rt = Box("Plate" + i, transform, new Vector2(0, 0), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200, 40));
            var lab = Label(rt, "Name", "", 18, TextAnchor.LowerCenter, new Vector2(0, 0), new Vector2(200, 24), Color.white);
            lab.rectTransform.anchorMin = lab.rectTransform.anchorMax = new Vector2(0.5f, 1f); lab.rectTransform.pivot = new Vector2(0.5f, 1f);
            var bg = Img("Bg", rt, white, new Vector2(0.5f, 0f), new Vector2(0, 4), new Vector2(84, 8), new Color(0, 0, 0, 0.6f));
            var fill = Img("Fill", bg, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 8), new Color(0.85f, 0.2f, 0.2f)).GetComponent<Image>();
            fill.sprite = white; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            plates.Add(new Plate { rt = rt, label = lab, fill = fill });
            rt.gameObject.SetActive(false);
        }
        for (int i = 0; i < 16; i++)
        {
            var t = Label(transform, "Float" + i, "", 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(240, 44), Color.white);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = Vector2.zero;
            t.gameObject.SetActive(false);
            floaters.Add(new Floater { rt = t.rectTransform, text = t });
        }

        BuildQuestUI();
        BuildPotions();
        BuildBag();
        BuildPicker();
        BuildCreator();
        BuildWork();
        BuildShopHud();
        BuildMenuHud();
        BuildEmoteHud();
        BuildLeapHud(); BuildPetHud(); BuildExtraHud(); BuildChat(); BuildCombo();
        BuildDailyHud();
        BuildEvHud();
        ApplySkin();
        RefreshClass();
        if (g.player != null) g.player.bag.Changed += RefreshBag;
    }

    // ---------- class picker ----------
    void BuildPicker()
    {
        picker = Box("ClassPicker", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        picker.anchorMin = Vector2.zero; picker.anchorMax = Vector2.one; picker.offsetMin = picker.offsetMax = Vector2.zero;
        var shade = picker.gameObject.AddComponent<Image>(); shade.sprite = white; shade.color = new Color(0.04f, 0.03f, 0.03f, 0.88f); shade.raycastTarget = false;
        var title = Center(Label(picker, "Title", "Choose your class", 46, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(900, 70), new Color(0.95f, 0.65f, 0.3f)));
        title.rectTransform.anchoredPosition = new Vector2(0, 230);
        title.fontStyle = FontStyle.Bold;
        for (int i = 0; i < AHClasses.All.Length; i++)
        {
            ClassDef c = AHClasses.All[i];
            int per = AHClasses.All.Length > 6 ? 4 : 3, col = i % per, row = i / per;
            Vector2 pos = new Vector2((col - (per - 1) * 0.5f) * (per > 3 ? 250f : 300f), 80f - row * 190f);
            var card = Img("Card_" + c.id, picker, white, new Vector2(0.5f, 0.5f), pos, new Vector2(per > 3 ? 236 : 280, 170), new Color(0.16f, 0.11f, 0.08f, 0.96f));
            Img("Stripe", card, white, new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(per > 3 ? 228 : 272, 8), c.color);
            var n = Center(Label(card, "Name", c.name, 34, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(230, 50), Color.white));
            n.rectTransform.anchoredPosition = new Vector2(0, 30); n.fontStyle = FontStyle.Bold;
            var r = Center(Label(card, "Role", c.role, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(260, 60), new Color(1f, 0.9f, 0.75f)));
            r.rectTransform.anchoredPosition = new Vector2(0, -20); r.horizontalOverflow = HorizontalWrapMode.Wrap;
            var sp = Center(Label(card, "Spells", c.spells[0].name + " · " + c.spells[1].name, 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(260, 30), new Color(1f, 1f, 1f, 0.6f)));
            sp.rectTransform.anchoredPosition = new Vector2(0, -62);
            string id = c.id;
            taps.Add(new TapBtn { rt = card, layer = 1, act = () => { g.ChooseClass(id); ShowPicker(false); } });
        }
        picker.gameObject.SetActive(false);
    }

    public void ShowPicker(bool on)
    {
        if (on) { ShowBag(false); ShowDialog(false); }
        picker.gameObject.SetActive(on);
        picker.SetAsLastSibling();
        Time.timeScale = on ? 0f : 1f;
        ptrs.Clear();
    }

    // a cast: the button pops and the spell's name floats up above it
    public void SpellFlash(int i, SpellDef sp)
    {
        if (i < 0 || i >= spellBtns.Length || spellBtns[i] == null) return;
        spellBtns[i].flashT = 1.4f; spellBtns[i].name.text = sp.name;
    }

    public void RefreshClass()
    {
        var p = g.player;
        if (p == null || p.cls == null) return;
        string ttl0 = AHEvo.Title(p);
        nameText.text = string.IsNullOrEmpty(p.heroName) ? ttl0 : p.heroName + "  ·  " + ttl0;
        var sps0 = p.Spells; spellBtns[5].rt.gameObject.SetActive(sps0.Length > 5);
        for (int i = 0; i < 6; i++)
        {
            if (i >= sps0.Length) continue;
            var sp = sps0[i];
            var ic = AHSpellLook.Icon(sp);
            spellBtns[i].icon.sprite = ic; spellBtns[i].icon.enabled = ic != null;
            spellBtns[i].label.text = ic != null ? "" : sp.abbr;
            Color c = sp.color * 0.55f; c.a = 0.95f;
            spellBtns[i].bg.color = c;
        }
    }

    // ---------- small builders ----------
    static Sprite MakeCircle(int size, float ringWidth)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - 1f - d);
                if (ringWidth > 0f) a *= Mathf.Clamp01(d - (r - 1f - ringWidth));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite MakeWhite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
    }

    static Text Center(Text t)
    {
        Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero);
        return t;
    }

    RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    RectTransform Img(string name, Transform parent, Sprite sp, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
    {
        var rt = Box(name, parent, anchor, new Vector2(0.5f, 0.5f), pos, size);
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = sp == white ? null : sp;   // plain panels draw best with no sprite at all (the tiny white texture drew faintly)
        im.color = c;
        im.raycastTarget = false;
        return rt;
    }

    Text Label(Transform parent, string name, string s, int size, TextAnchor align, Vector2 pos, Vector2 box, Color c)
    {
        var rt = Box(name, parent, new Vector2(0, 1), new Vector2(0, 1), pos, box);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = c;
        t.text = s;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.8f);
        sh.effectDistance = new Vector2(2, -2);
        return t;
    }

    Image Bar(Transform parent, string name, Vector2 pos, Vector2 size, Color c, bool withBg = true)
    {
        var bg = Box(name + "Bg", parent, new Vector2(0, 1), new Vector2(0, 1), pos, size);
        if (withBg) { var bgi = bg.gameObject.AddComponent<Image>(); bgi.sprite = white; bgi.color = new Color(0, 0, 0, 0.55f); bgi.raycastTarget = false; }
        var fill = Box(name, bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(4, 4));
        var im = fill.gameObject.AddComponent<Image>();
        im.sprite = white; im.color = c; im.raycastTarget = false;
        im.type = Image.Type.Filled; im.fillMethod = Image.FillMethod.Horizontal; im.fillOrigin = 0;
        return im;
    }

    // ---------- a black screen while the next area loads ----------
    RectTransform fadeRt;
    public void Fade(bool on)
    {
        if (fadeRt == null)
        {
            fadeRt = Box("Fade", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            fadeRt.anchorMin = Vector2.zero; fadeRt.anchorMax = Vector2.one; fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
            var im = fadeRt.gameObject.AddComponent<Image>(); im.sprite = null; im.color = Color.black; im.raycastTarget = false;
            Center(Label(fadeRt, "T", "Travelling…", 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(600, 50), new Color(1f, 0.85f, 0.55f)));
        }
        fadeRt.gameObject.SetActive(on);
        fadeRt.SetAsLastSibling();
    }

    // ---------- messages ----------
    public void Banner(string title, string sub)
    {
        if (title != null && (title.StartsWith("Level ") || sub == "Achievement unlocked" || title.StartsWith("Success") || sub == "Level up" || title.Contains("cleared") || title.StartsWith("Victory"))) AHSound.Play("level");
        else if (title != null && (title.StartsWith("Wave ") || title.Contains("wakes") || title.Contains("enraged") || sub == "Rare monster" || title == "World boss")) AHSound.Play("roar");
        else AHSound.Play("loot");
        AHChat.Add(title != null && (title.StartsWith("Chapter") || title == "Kingsbane") ? "story" : "system", title + (string.IsNullOrEmpty(sub) ? "" : " · " + sub));
        if (bannerTitle == null) return;
        bannerTitle.text = title;
        bannerSub.text = sub;
        bannerT = 3.2f;
    }

    public string lastToast;   // for tests
    public void Toast(string s, float time = 1.4f)
    {
        lastToast = s; AHChat.FromToast(s);
        if (toastText == null || string.IsNullOrEmpty(s)) return;
        float t = Mathf.Max(time, 1.4f + s.Length * 0.025f);
        // a message that has only just appeared is not wiped out: the new one waits its turn (at most 3 wait)
        if (toastT > 0f && toastShown < 1.2f && toastText.text != s)
        {
            if (toastQ.Count >= 3) toastQ.RemoveAt(0);
            toastQ.Add(new KeyValuePair<string, float>(s, t));
            return;
        }
        toastText.text = s; toastT = t; toastShown = 0f;
    }

    public void Float(Vector3 world, string s, Color c)
    {
        if (AHSettings.Tips == 0) return;
        var f = floaters[nextFloat];
        nextFloat = (nextFloat + 1) % floaters.Count;
        f.world = world + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0, UnityEngine.Random.Range(-0.3f, 0.3f));
        f.t = 0f;
        f.text.text = s;
        f.text.color = c;
        f.rt.gameObject.SetActive(true);
    }

    // ---------- every frame ----------
    void Update()
    {
        FitSafe();
        ReadPointers();
        TickSkin();
        float dt = Time.unscaledDeltaTime;
        var p = g.player;
        if (p != null && p.cls != null)
        {
            hpFill.fillAmount = p.maxHp > 0 ? p.hp / p.maxHp : 0f;
            shieldFill.fillAmount = p.maxHp > 0 ? Mathf.Clamp01(p.shield / p.maxHp) : 0f;
            hpText.text = Mathf.CeilToInt(p.hp) + " / " + Mathf.CeilToInt(p.maxHp);
            bool usesMana = p.cls.mana;
            if (manaFill.transform.parent.gameObject.activeSelf != usesMana) { manaFill.transform.parent.gameObject.SetActive(usesMana); manaText.gameObject.SetActive(usesMana); }
            if (usesMana) { manaFill.fillAmount = p.maxMana > 0 ? p.mana / p.maxMana : 0f; manaText.text = Mathf.FloorToInt(p.mana) + " / " + Mathf.CeilToInt(p.maxMana); }
            xpFill.fillAmount = p.XpFrac;
            lvText.text = "Lv " + p.level;
            goldText.text = AHItems.MoneyText(p.bag.money, 2);
            hungerFill.fillAmount = p.hunger / 100f;
            bool starving = p.hunger < 25f;
            hungerText.text = starving ? "Hungry! Eat" : "Food " + Mathf.RoundToInt(p.hunger) + "%";
            hungerText.color = starving ? Color.Lerp(new Color(1f, 0.3f, 0.25f), Color.white, Mathf.PingPong(Time.unscaledTime * 2f, 1f) * 0.5f) : new Color(1f, 0.8f, 0.55f);
            hungerFill.color = starving ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.9f, 0.55f, 0.25f);
            if (starving && p.hp < p.maxHp) hpText.text += "  (hungry: no healing)";
            clockText.text = g.data.region + " · " + (g.Dark ? "Underground" : g.IsNight ? "Night" : g.DayLight < 0.6f ? "Dusk" : "Day");
            var sps = p.Spells;
            for (int i = 0; i < 6 && i < sps.Length; i++)
            {
                float cd = AHEvo.Cd(p, sps[i]), left = Mathf.Max(0f, p.cds[i]);
                spellBtns[i].cool.fillAmount = cd > 0f ? left / cd : 0f;
                spellBtns[i].secs.text = left > 0f ? Mathf.CeilToInt(left).ToString() : "";
                spellBtns[i].label.enabled = left <= 0f;
                bool dry = usesMana && p.mana < sps[i].cost;
                spellBtns[i].label.color = dry ? new Color(0.55f, 0.65f, 1f, 0.8f) : Color.white;
                // the icon dims while cooling down and turns a cold grey-blue when you lack the mana
                spellBtns[i].icon.color = dry ? new Color(0.45f, 0.55f, 0.85f, 1f) : left > 0f ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white;
                var sb = spellBtns[i];
                if (sb.flashT > 0f)
                {
                    sb.flashT -= dt; float k = Mathf.Clamp01(sb.flashT / 1.4f);
                    sb.rt.localScale = Vector3.one * (1f + 0.14f * Mathf.Max(0f, (k - 0.75f) * 4f));
                    var nc = sb.name.color; nc.a = Mathf.Clamp01(k * 2.2f); sb.name.color = nc;
                    sb.name.rectTransform.anchoredPosition = new Vector2(0, 58 + (1f - k) * 14f);
                    if (sb.flashT <= 0f) { sb.rt.localScale = Vector3.one; nc.a = 0f; sb.name.color = nc; }
                }
            }
        }
        atkImg.color = attackHeld ? new Color(0.5f, 0.3f, 0.15f, 0.95f) : new Color(0.2f, 0.14f, 0.1f, 0.85f);

        if (bannerT > 0f) { bannerT -= dt; bannerGroup.alpha = Mathf.Clamp01(bannerT / 0.6f) * Mathf.Clamp01((3.2f - bannerT) / 0.3f); }
        else bannerGroup.alpha = 0f;
        if (toastT <= 0f && toastQ.Count > 0) { var nx = toastQ[0]; toastQ.RemoveAt(0); toastText.text = nx.Key; toastT = nx.Value; toastShown = 0f; }
        if (toastT > 0f) { toastT -= dt; toastShown += dt; toastGroup.alpha = Mathf.Clamp01(toastT / 0.4f); } else toastGroup.alpha = 0f;

        UpdateQuestUI();
        UpdatePotions();
        ShopKeys();
        if (Modal == 0) AHTutorial.Tick(g, this, dt);
        InspectTick(); UpgradeTick(dt); LeapHudTick(); PetHudTick(); ExtraHudTick(dt); ChatTick(dt); ComboTick();
        Camera cam = g.cam;
        int n = 0;
        if (cam != null && p != null && !PickerOpen)
        {
            foreach (var m in g.mobs)
            {
                if (n >= plates.Count) break;
                if (m.dead) continue;
                Vector3 wp = m.transform.position + Vector3.up * (m.type.model == "Wolf_t" ? 1.5f : 1.6f);
                if ((wp - p.transform.position).sqrMagnitude > 18f * 18f) continue;
                Vector3 sp = cam.WorldToScreenPoint(wp);
                if (sp.z <= 0f || UnderHud(sp)) continue;
                var pl = plates[n++];
                pl.rt.gameObject.SetActive(true);
                pl.rt.position = sp;
                pl.label.text = m.Label;
                pl.label.color = m == p.target ? new Color(1f, 0.5f, 0.4f) : m.type.aggro > 0f ? new Color(1f, 0.75f, 0.45f) : Color.white;
                pl.fill.fillAmount = m.hp / Mathf.Max(1f, m.type.hp);
            }
        }
        for (int i = n; i < plates.Count; i++) if (plates[i].rt.gameObject.activeSelf) plates[i].rt.gameObject.SetActive(false);

        foreach (var f in floaters)
        {
            if (!f.rt.gameObject.activeSelf) continue;
            f.t += dt;
            if (f.t > 1.1f || cam == null) { f.rt.gameObject.SetActive(false); continue; }
            Vector3 sp = cam.WorldToScreenPoint(f.world + Vector3.up * f.t * 0.9f);
            if (sp.z <= 0f) { f.rt.gameObject.SetActive(false); continue; }
            f.rt.position = sp;
            var c = f.text.color; c.a = Mathf.Clamp01((1.1f - f.t) / 0.4f); f.text.color = c;
        }
    }

    bool Inside(RectTransform rt, Vector2 screen, float pad)
    {
        Vector3[] cs = new Vector3[4];
        rt.GetWorldCorners(cs);   // in screen pixels for an overlay canvas
        float x0 = Mathf.Min(cs[0].x, cs[2].x) - pad, x1 = Mathf.Max(cs[0].x, cs[2].x) + pad;
        float y0 = Mathf.Min(cs[0].y, cs[2].y) - pad, y1 = Mathf.Max(cs[0].y, cs[2].y) + pad;
        return screen.x >= x0 && screen.x <= x1 && screen.y >= y0 && screen.y <= y1;
    }

    Role Claim(AHPointer q, Ptr pt, float k)
    {
        int modal = Modal;
        foreach (var t in taps)
            if (t.layer == modal && t.rt != null && t.rt.gameObject.activeInHierarchy && Inside(t.rt, q.pos, 4f * k)) { pt.tap = t; return Role.Tap; }
        // making your hero: a finger on the hero (off the panel) turns them, two fingers zoom in and out
        if (modal == 4 && crPanel != null && !Inside(crPanel, q.pos, 0f)) return Role.Cam;
        if (modal != 0) return Role.None;
        if (q.id == -2 || PhotoOn) return Role.Cam;
        for (int i = 0; i < 6; i++)
            if (spellBtns[i].rt.gameObject.activeSelf && Inside(spellBtns[i].rt, q.pos, 4f * k)) { pt.index = i; spellPressed[i] = true; return Role.Spell; }
        if (Inside(atkBtn, q.pos, 24f * k)) return Role.Attack;
        if (Inside(dodgeBtn, q.pos, 16f * k)) { dodgePressed = true; return Role.Dodge; }
        if (q.pos.x < Screen.width * 0.42f && q.pos.y < Screen.height * 0.5f && !HasRole(Role.Joy)) return Role.Joy;
        return Role.Cam;
    }

    void ReadPointers()
    {
        if (CreatorOpen) { CreatorKeys(); }
        else if (ChatOpen) { }   // typing in the chat: no hotkeys
        else if (WorkOpen) WorkKeys();
        else if (AHInput.BagKey() && !PickerOpen && Time.frameCount != workShutFrame) ShowBag(!BagOpen);
        if (AHInput.BackKey() && BagOpen) ShowBag(false);
        if (AHInput.BackKey() && DialogOpen) ShowDialog(false);
        if (AHInput.TalkKey() && Modal == 0) OnAction();
        float k = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        var list = AHInput.Pointers();
        seen.Clear();
        dodgePressed = false; leapPressed = false;
        for (int i = 0; i < 6; i++) spellPressed[i] = false;
        camDelta = Vector2.zero;
        zoom = 1f;
        Vector2 joyCentre = joyBase.position;

        foreach (var q in list)
        {
            seen.Add(q.id);
            Ptr pt;
            if (!ptrs.TryGetValue(q.id, out pt))
            {
                pt = new Ptr { start = q.pos, last = q.pos };
                pt.role = Claim(q, pt, k);
                ptrs[q.id] = pt;
            }
            if (pt.role == Role.Cam) camDelta += (q.pos - pt.last) / k;
            // a finger dragged over a scrolling list scrolls it, and then doesn't count as a tap
            if ((q.pos - pt.start).magnitude > 14f * k) pt.dragged = true;
            if (CreatorOpen && Inside(crBody, pt.start, 0f)) CrScrollBy((q.pos.y - pt.last.y) / k);
            pt.last = q.pos;
        }

        // pinch with two camera fingers
        Vector2 pa = Vector2.zero, pb = Vector2.zero; int nCam = 0;
        foreach (var q in list)
        {
            Ptr pt = ptrs[q.id];
            if (pt.role != Role.Cam || q.id < 0) continue;
            if (nCam == 0) pa = q.pos; else if (nCam == 1) pb = q.pos;
            nCam++;
        }
        if (nCam >= 2)
        {
            float now = Vector2.Distance(pa, pb);
            if (pinchLast > 0f && now > 1f) zoom = pinchLast / now;
            pinchLast = now;
            camDelta = Vector2.zero;
        }
        else pinchLast = 0f;
        float wheel = AHInput.Wheel();
        if (Mathf.Abs(wheel) > 0.01f && Modal == 0) zoom *= Mathf.Pow(0.9f, wheel);
        if (Mathf.Abs(wheel) > 0.01f && CreatorOpen)
        {
            if (Inside(crPanel, AHInput.MousePos(), 0f)) CrScrollBy(-wheel * 40f);
            else zoom *= Mathf.Pow(0.8f, wheel);
        }

        // released fingers: taps fire when lifted over their button
        gone.Clear();
        foreach (var kv in ptrs) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
        foreach (var id in gone)
        {
            Ptr pt = ptrs[id];
            ptrs.Remove(id);
            if (pt.role == Role.Tap && pt.tap != null && !(pt.dragged && pt.tap.group == "cr") && Inside(pt.tap.rt, pt.last, 8f * k)) pt.tap.act();
        }

        // joystick
        stick = Vector2.zero;
        attackHeld = false;
        foreach (var kv in ptrs)
        {
            if (kv.Value.role == Role.Attack) attackHeld = true;
            if (kv.Value.role != Role.Joy) continue;
            Vector2 d = (kv.Value.last - joyCentre) / k;
            if (d.magnitude > JoyR) d = d.normalized * JoyR;
            stick = d / JoyR;
            joyKnob.anchoredPosition = d;
        }
        if (!HasRole(Role.Joy)) joyKnob.anchoredPosition = Vector2.Lerp(joyKnob.anchoredPosition, Vector2.zero, 0.35f);
    }

    // ---------- bag window (web: Bag + Gear): worn gear on the left, the 28-slot bag on the right ----------
    RectTransform bagRoot, bagWin;
    class BagSlot { public RectTransform rt; public Image bg, icon, glyph, frame; public Text abbr, count, hint; }
    BagSlot[] bagSlots;
    readonly Dictionary<string, BagSlot> gearSlots = new Dictionary<string, BagSlot>();
    Text bagGold, bagInfo, bagInfoSub, gearStats, bagActText, bagDropText; RectTransform bagAct, bagDrop; bool dropAsk;
    string bagSel, gearSel;
    const float WinW = 860f, WinH = 540f;

    BagSlot MakeSlot(string name, Vector2 pos, float size, int layer, Action act)
    {
        var b = new BagSlot();
        b.rt = Img(name, bagWin, white, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size), new Color(0.2f, 0.15f, 0.11f, 1f));
        b.bg = b.rt.GetComponent<Image>();
        b.icon = Img("Icon", b.rt, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.78f, size * 0.78f), Color.white).GetComponent<Image>();
        b.glyph = Img("Glyph", b.rt, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.7f, size * 0.7f), Color.white).GetComponent<Image>();
        b.glyph.preserveAspect = true; b.glyph.enabled = false;
        b.abbr = Center(Label(b.rt, "Abbr", "", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size, 30), Color.white));
        b.abbr.fontStyle = FontStyle.Bold;
        b.count = Label(b.rt, "Count", "", 15, TextAnchor.LowerRight, new Vector2(0, 0), new Vector2(size - 4, size - 2), new Color(1f, 0.95f, 0.8f));
        b.hint = Center(Label(b.rt, "Hint", "", 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size, 24), new Color(1f, 1f, 1f, 0.35f)));
        taps.Add(new TapBtn { rt = b.rt, layer = layer, act = act });
        return b;
    }

    void BuildBag()
    {
        bagRoot = Box("Bag", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        bagRoot.anchorMin = Vector2.zero; bagRoot.anchorMax = Vector2.one; bagRoot.offsetMin = bagRoot.offsetMax = Vector2.zero;
        var dim = bagRoot.gameObject.AddComponent<Image>(); dim.sprite = white; dim.color = new Color(0f, 0f, 0f, 0.5f); dim.raycastTarget = false;
        bagWin = Img("BagWindow", bagRoot, white, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(WinW, WinH), new Color(0.1f, 0.075f, 0.055f, 1f));
        Img("Edge", bagWin, white, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(WinW, 6), new Color(0.85f, 0.6f, 0.25f, 1f));
        var title = Label(bagWin, "Title", "Bag", 30, TextAnchor.UpperLeft, new Vector2(24, -14), new Vector2(300, 44), new Color(0.95f, 0.65f, 0.3f));
        // sort the bag: gear, potions, food, valuables, then materials
        var sortBtn = Img("Sort", bagWin, white, new Vector2(0f, 1f), new Vector2(150, -36), new Vector2(84, 34), new Color(0.3f, 0.24f, 0.18f, 1f));
        Center(Label(sortBtn, "T", "SORT", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(84, 26), Color.white));
        taps.Add(new TapBtn { rt = sortBtn, layer = 2, act = () => { AHExtras.Sort(g.player.bag); bagSel = null; RefreshBag(); } });
        title.fontStyle = FontStyle.Bold;
        bagGold = Label(bagWin, "Money", "", 22, TextAnchor.UpperRight, new Vector2(WinW - 400, -20), new Vector2(310, 34), new Color(1f, 0.84f, 0.35f));
        var close = Img("Close", bagWin, circle, new Vector2(1, 1), new Vector2(-36, -34), new Vector2(52, 52), new Color(0.45f, 0.15f, 0.12f, 1f));
        Center(Label(close, "X", "X", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(52, 40), Color.white));
        taps.Add(new TapBtn { rt = close, layer = 2, act = () => ShowBag(false) });

        // worn gear: the web game's ten slots, two columns
        var worn = Label(bagWin, "Worn", "Gear", 20, TextAnchor.UpperCenter, new Vector2(30, -62), new Vector2(150, 28), new Color(1f, 0.85f, 0.55f));
        worn.fontStyle = FontStyle.Bold;
        for (int k = 0; k < AHItems.GearSlots.Length; k++)
        {
            string slot = AHItems.GearSlots[k];
            int col = k / 5, row = k % 5;
            var b = MakeSlot("Gear_" + slot, new Vector2(-370 + col * 72, 150 - row * 70), 62, 2, () => OnGearSlot(slot));
            b.hint.text = AHItems.SlotName(slot);
            gearSlots[slot] = b;
        }
        gearStats = Label(bagWin, "Stats", "", 15, TextAnchor.UpperLeft, new Vector2(22, -470), new Vector2(190, 66), new Color(1f, 0.9f, 0.7f));
        gearStats.horizontalOverflow = HorizontalWrapMode.Wrap;

        // the bag: 7 x 4 slots, or 9 x 5 (and more rows, smaller) as it grows
        int n = AHDB.Slots;
        bagSlots = new BagSlot[n];
        int bcols = n <= 28 ? 7 : 9, brows = (n + bcols - 1) / bcols;
        float step = Mathf.Min(n <= 28 ? 76f : 60f, 320f / brows), size = step - 6f;
        float bx0 = 32f - (bcols - 1) * step * 0.5f, by0 = 54f + (brows - 1) * step * 0.5f;
        for (int i = 0; i < n; i++)
        {
            int col = i % bcols, row = i / bcols, idx = i;
            bagSlots[i] = MakeSlot("Slot" + i, new Vector2(bx0 + col * step, by0 - row * step), size, 2, () => OnBagSlot(idx));
        }
        bagInfo = Label(bagWin, "Info", "", 21, TextAnchor.UpperLeft, new Vector2(200, -396), new Vector2(420, 30), Color.white);
        bagInfo.fontStyle = FontStyle.Bold;
        bagInfoSub = Label(bagWin, "InfoSub", "", 16, TextAnchor.UpperLeft, new Vector2(200, -424), new Vector2(420, 110), new Color(1f, 0.9f, 0.75f, 0.95f));
        bagInfoSub.horizontalOverflow = HorizontalWrapMode.Wrap; bagInfoSub.verticalOverflow = VerticalWrapMode.Overflow; bagInfoSub.supportRichText = true;
        // what you can do with the chosen item: a big button (equip, eat, drink, use, take off) and Drop
        bagAct = Img("Act", bagWin, white, new Vector2(1f, 1f), new Vector2(-110, -420), new Vector2(170, 50), new Color(0.22f, 0.5f, 0.26f, 1f));
        bagActText = Center(Label(bagAct, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(170, 36), Color.white)); bagActText.fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = bagAct, layer = 2, act = BagAction });
        bagDrop = Img("Drop", bagWin, white, new Vector2(1f, 1f), new Vector2(-110, -478), new Vector2(170, 42), new Color(0.42f, 0.18f, 0.14f, 1f));
        bagDropText = Center(Label(bagDrop, "T", "Drop", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(170, 32), Color.white));
        taps.Add(new TapBtn { rt = bagDrop, layer = 2, act = BagDrop });
        bagRoot.gameObject.SetActive(false);
    }

    public void ShowBag(bool on)
    {
        if (bagRoot == null) return;
        bagRoot.gameObject.SetActive(on);
        bagRoot.SetAsLastSibling();
        bagSel = null; gearSel = null;
        ptrs.Clear();
        RefreshBag();
    }

    // web: tap once to read about it, tap again to eat it or put it on
    void OnBagSlot(int i)
    {
        var p = g.player; var bag = p.bag;
        string id = i < bag.order.Count ? bag.order[i] : null;
        gearSel = null; dropAsk = false;
        if (id == null) { bagSel = null; RefreshBag(); return; }
        var d = AHItems.Get(id);
        if (bagSel == id && d != null)
        {
            if (d.IsFood) { p.Eat(id); bagSel = bag.Count(id) > 0 ? id : null; }
            else if (d.potion) { p.Drink(id); bagSel = bag.Count(id) > 0 ? id : null; }
            else if (id == "mystery_sack") { AHDaily.OpenSack(g); bagSel = bag.Count(id) > 0 ? id : null; }
            else if (id == AHRareRecipes.ScrollId) { AHRareRecipes.Read(g); bagSel = bag.Count(id) > 0 ? id : null; }
            else if (AHFashion.IsPattern(id)) { AHFashion.ReadPattern(g, id); bagSel = bag.Count(id) > 0 ? id : null; }
            else if (AHTreasure.IsMap(id)) { AHTreasure.Read(g, id); bagSel = bag.Count(id) > 0 ? id : null; }
            else if (d.reins != null) { if (p.mounts.Contains(d.reins)) Toast("You already know that mount. Sell the reins to someone who wants it."); else if (bag.Take(id)) { p.mounts.Add(d.reins); p.mountSel = d.reins; RefreshRide(); Banner(AHComp.MountName(d.reins), "Mount learned! Tap RIDE"); AHSound.Play("level"); g.MarkDirty(); } bagSel = bag.Count(id) > 0 ? id : null; }
            else if (id.StartsWith("card_")) { if (!AHPower.AddCard(g, id)) Toast("That card is already in your collection. Sell or trade the spare."); bagSel = bag.Count(id) > 0 ? id : null; }
            else if (d.IsGear) { string msg = bag.Equip(id, p.cls.id); if (msg != null) Toast(msg); bagSel = null; }
        }
        else bagSel = id;
        RefreshBag();
    }

    // tap a worn piece once to read about it, again to take it off
    void OnGearSlot(string slot)
    {
        var bag = g.player.bag;
        bagSel = null;
        if (bag.Worn(slot) == null) { gearSel = null; RefreshBag(); return; }
        if (gearSel == slot) { string msg = bag.Unequip(slot); if (msg != null) Toast(msg); gearSel = null; }
        else gearSel = slot;
        RefreshBag();
    }

    // the big button does what a second tap on the item does
    void BagAction()
    {
        var bag = g.player.bag;
        if (bagSel != null) { int i = bag.order.IndexOf(bagSel); if (i >= 0) OnBagSlot(i); }
        else if (gearSel != null) OnGearSlot(gearSel);
    }

    // Drop asks once ("Drop 1?"), then throws one away
    void BagDrop()
    {
        var p = g.player; var bag = p.bag;
        if (bagSel == null || bag.Count(bagSel) <= 0) return;
        if (!dropAsk) { dropAsk = true; RefreshBag(); return; }
        var d = AHItems.Get(bagSel);
        if (bag.Take(bagSel)) { Toast("Dropped " + (d != null ? d.name : bagSel) + "."); g.MarkDirty(); }
        dropAsk = false;
        if (bag.Count(bagSel) <= 0) bagSel = null;
        RefreshBag();
    }

    // gear against what you wear in that slot: "+3 attack · −2 armor"
    static string Compare(ItemDef a, ItemDef worn)
    {
        if (worn == null) return "<color=#8fe08a>Empty slot: all gain.</color>";
        var parts = new List<string>();
        Action<float, string, bool> add = (v, what, pct) =>
        {
            if (Mathf.Abs(v) < 0.0001f) return;
            string n = pct ? Mathf.RoundToInt(v * 100) + "%" : Mathf.RoundToInt(v).ToString();
            parts.Add((v > 0 ? "<color=#8fe08a>+" : "<color=#ff8a7a>−") + n.TrimStart('-') + " " + what + "</color>");
        };
        add(a.atk - worn.atk, "attack", false); add(a.def - worn.def, "armor", false); add(a.hp - worn.hp, "max HP", false);
        add(a.dmg - worn.dmg, "damage", true); add(a.cdr - worn.cdr, "cooldowns", true); add(a.heal - worn.heal, "healing", true);
        return parts.Count == 0 ? "Same as what you wear." : string.Join(" · ", parts.ToArray());
    }

    static void PaintSlot(BagSlot b, ItemDef d, int n, bool selected)
    {
        b.icon.enabled = d != null;
        var gs = AHItemIcons.Best(d);   // gear: its photograph; everything else: its own picture
        if (d != null) b.icon.color = AHItemIcons.Full(d, gs) ? new Color(0.16f + d.color.r * 0.12f, 0.12f + d.color.g * 0.1f, 0.09f + d.color.b * 0.1f, 0.9f) : new Color(d.color.r * 0.85f, d.color.g * 0.85f, d.color.b * 0.85f, 1f);
        if (b.glyph != null)
        {
            b.glyph.enabled = gs != null; b.glyph.sprite = gs;
            float k = AHItemIcons.Full(d, gs) ? 0.94f : 0.7f;   // its own picture fills the socket
            b.glyph.rectTransform.sizeDelta = b.rt.sizeDelta * k;
        }
        b.abbr.text = d != null && gs == null ? d.Abbr : "";
        b.abbr.color = d != null && (d.color.r + d.color.g + d.color.b) > 2.1f ? new Color(0.15f, 0.1f, 0.08f) : Color.white;
        b.count.text = n > 1 ? n.ToString("#,0") : "";
        if (b.hint != null) b.hint.enabled = d == null;
        if (b.frame != null)
        {
            // the socket stays dark; the frame shows the item's quality, and glows gold when chosen
            b.bg.color = selected ? new Color(1f, 0.85f, 0.55f, 1f) : Color.white;
            var q = d != null ? AHItems.Quality(d) : new Color(0.8f, 0.65f, 0.4f);
            b.frame.color = selected ? new Color(1f, 0.85f, 0.4f, 1f) : d != null ? new Color(q.r, q.g, q.b, 0.95f) : new Color(0.8f, 0.65f, 0.4f, 0.25f);
        }
        else b.bg.color = selected ? new Color(0.55f, 0.38f, 0.18f, 1f) : d != null ? Color.Lerp(new Color(0.2f, 0.15f, 0.11f, 1f), AHItems.Quality(d), 0.18f) : new Color(0.2f, 0.15f, 0.11f, 1f);
    }

    public void RefreshBag()
    {
        if (bagWin == null || g.player == null) return;
        var p = g.player;
        var bag = p.bag;
        bagGold.text = AHItems.MoneyText(bag.money, 3);
        for (int i = 0; i < bagSlots.Length; i++)
        {
            string id = i < bag.order.Count ? bag.order[i] : null;
            PaintSlot(bagSlots[i], AHItems.Get(id), bag.Count(id), id != null && id == bagSel);
        }
        foreach (var kv in gearSlots) PaintSlot(kv.Value, AHItems.Get(bag.Worn(kv.Key)), 1, kv.Key == gearSel);
        var s = p.stat;
        gearStats.text = "Attack +" + s.atk + "   Armor " + s.def + " (−" + Mathf.RoundToInt(p.ArmorCut * 100) + "%)\nMax HP " + Mathf.CeilToInt(p.maxHp) + "   Attack lv " + p.Skill("attack") + "\nSkinning lv " + p.Skill("skinning");

        ItemDef sd = null; int n = 0; string text = "", act = null;
        if (bagSel != null && bag.Count(bagSel) > 0)
        {
            sd = AHItems.Get(bagSel); n = bag.Count(bagSel);
            if (sd.IsGear)
            {
                var wornD = AHItems.Get(bag.Worn(sd.slot));
                text = AHItems.SlotName(sd.slot) + " · " + AHItems.WhoUses(sd.id) + "\n" + AHItems.StatLine(sd) + SetLine(sd, bag)
                    + "\nVs " + (wornD != null ? wornD.name : "nothing worn") + ": " + Compare(sd, wornD)
                    + (AHItems.CanUse(sd.id, p.cls.id) ? "" : "\n<color=#ff8a7a>Your class can’t use this.</color>");
                act = AHItems.CanUse(sd.id, p.cls.id) ? "Equip" : null;
            }
            else if (sd.IsFood) { text = "Restores " + sd.foodHp + " HP and " + sd.foodHunger + " hunger." + (sd.meal ? " Well fed: " + AHMeal.Describe(sd.id) + "." : ""); act = "Eat"; }
            else if (sd.potion) { text = PotionText(sd.id); act = "Drink"; }
            else if (sd.id.StartsWith("card_")) { bool has = p.prog.cards.Contains(AHJson.S(AHItems.Raw(sd.id), "card")); text = "Monster card. " + (has ? "Already in your collection: sell or trade it." : "Add it to your collection: +1% damage against this monster."); act = has ? null : "Collect"; }
            else if (sd.id == "enh_stone") text = "Used to enhance weapons and armor from +1 to +9 (Menu → Enhance gear).";
            else if (sd.id == "lucky_charm") text = "Use one while enhancing: if the attempt fails, the item keeps its level.";
            else if (AHPower.GemText(sd.id) != "") text = "Gem · fits a gear socket: " + AHPower.GemText(sd.id) + " (Menu → Gem sockets).";
            else if (sd.id == "mystery_sack") { text = sd.note ?? "What could be inside?"; act = "Open"; }
            else if (AHFashion.IsPattern(sd.id)) { var pc = AHCostumes.Get(sd.id.Substring(8)); text = (sd.note ?? "") + (pc != null && AHCostumes.Owns(p, pc.id) ? " You already have this costume." : ""); act = "Read"; }
            else if (sd.id == AHRareRecipes.ScrollId) { text = (sd.note ?? "") + " You know " + AHRareRecipes.KnownCount(p) + " of " + AHRareRecipes.Total + "."; act = "Read"; }
            else if (AHTreasure.IsMap(sd.id)) { text = sd.note ?? "A treasure map."; act = "Read"; }
            else if (sd.reins != null) { text = sd.note ?? "Reins: learn this mount."; act = "Learn"; }
            else text = sd.note ?? "";
        }
        else if (gearSel != null && bag.Worn(gearSel) != null)
        {
            sd = AHItems.Get(bag.Worn(gearSel)); n = 1;
            text = AHItems.SlotName(gearSel) + " · worn\n" + AHItems.StatLine(sd) + SetLine(sd, bag); act = "Take off";
        }
        if (sd != null && AHArtisan.Made(p, sd.id) > 0) text += "\n<color=#c9e0a0>Maker's mark: " + (string.IsNullOrEmpty(p.heroName) ? "you" : p.heroName) + " (" + AHArtisan.Made(p, sd.id) + " made)</color>";
        bagAct.gameObject.SetActive(sd != null && act != null);
        if (act != null) bagActText.text = act;
        bagDrop.gameObject.SetActive(bagSel != null && sd != null);
        if (bagSel == null) dropAsk = false;
        bagDropText.text = dropAsk ? "Drop 1? Tap again" : "Drop";
        RefreshPreview(sd);
        if (sd == null) { bagInfo.text = "Tap an item"; bagInfo.color = Color.white; bagInfoSub.text = bag.UsedSlots + " of " + bag.SlotsMax + " slots used. Tap an item to see what it does."; }
        else
        {
            bagInfo.text = sd.name + (n > 1 ? "  ×" + n : "");
            bagInfo.color = AHItems.Quality(sd);
            bagInfoSub.text = text;
        }
    }

    static string PotionText(string id)
    {
        var pot = AHJson.O(AHJson.O(AHDB.Items, id), "potion");
        if (AHJson.Has(pot, "heal")) return "Restores " + (int)AHJson.N(pot, "heal") + " HP (20 s cooldown).";
        if (AHJson.Has(pot, "mana")) return "Restores " + (int)AHJson.N(pot, "mana") + " mana (15 s cooldown).";
        string b = AHJson.S(pot, "buff");
        return b != null ? AHPlayer.ElixText(b) + "." : "";
    }

    // ---------- quick potions (web btnHeal / btnMana) ----------
    RectTransform potHeal, potMana;
    Text potHealN, potManaN;
    Image potHealCool, potManaCool;
    Text buffText;

    void BuildPotions()
    {
        potHeal = Img("PotHeal", transform, circle, new Vector2(1, 0), new Vector2(-58, 170), new Vector2(66, 66), new Color(0.55f, 0.12f, 0.12f, 0.92f));
        Img("Rim", potHeal, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66, 66), new Color(1f, 0.55f, 0.5f, 1f));
        potHealCool = Img("Cool", potHeal, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 62), new Color(0f, 0f, 0f, 0.6f)).GetComponent<Image>();
        potHealCool.type = Image.Type.Filled; potHealCool.fillMethod = Image.FillMethod.Radial360; potHealCool.fillOrigin = (int)Image.Origin360.Top; potHealCool.fillClockwise = false;
        Center(Label(potHeal, "T", "HP", 16, TextAnchor.MiddleCenter, new Vector2(0, 0), new Vector2(66, 26), Color.white)).rectTransform.anchoredPosition = new Vector2(0, 8);
        potHealN = Center(Label(potHeal, "N", "0", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(66, 24), new Color(1f, 0.9f, 0.8f)));
        potHealN.rectTransform.anchoredPosition = new Vector2(0, -12);
        taps.Add(new TapBtn { rt = potHeal, act = () => g.player.QuickPotion(false) });

        potMana = Img("PotMana", transform, circle, new Vector2(1, 0), new Vector2(-58, 246), new Vector2(66, 66), new Color(0.14f, 0.24f, 0.6f, 0.92f));
        Img("Rim", potMana, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66, 66), new Color(0.55f, 0.7f, 1f, 1f));
        potManaCool = Img("Cool", potMana, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 62), new Color(0f, 0f, 0f, 0.6f)).GetComponent<Image>();
        potManaCool.type = Image.Type.Filled; potManaCool.fillMethod = Image.FillMethod.Radial360; potManaCool.fillOrigin = (int)Image.Origin360.Top; potManaCool.fillClockwise = false;
        Center(Label(potMana, "T", "MP", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(66, 26), Color.white)).rectTransform.anchoredPosition = new Vector2(0, 8);
        potManaN = Center(Label(potMana, "N", "0", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(66, 24), new Color(0.85f, 0.9f, 1f)));
        potManaN.rectTransform.anchoredPosition = new Vector2(0, -12);
        taps.Add(new TapBtn { rt = potMana, act = () => g.player.QuickPotion(true) });

        // elixirs running, under the bars
        buffText = Label(transform, "Buffs", "", 16, TextAnchor.UpperLeft, new Vector2(22, -194), new Vector2(420, 24), new Color(1f, 0.85f, 0.5f));
    }

    void UpdatePotions()
    {
        var p = g.player; if (p == null || potHeal == null) return;
        if (Modal == 0 && AHInput.PotionKey(false)) p.QuickPotion(false);
        if (Modal == 0 && AHInput.PotionKey(true)) p.QuickPotion(true);
        int hn = p.PotionCount(false), mn = p.PotionCount(true);
        potHealN.text = hn.ToString(); potManaN.text = mn.ToString();
        float now = Time.time;
        potHealCool.fillAmount = Mathf.Clamp01((p.potCd - now) / 20f);
        potManaCool.fillAmount = Mathf.Clamp01((p.manaCd - now) / 15f);
        bool um = p.maxMana > 0f;
        if (potMana.gameObject.activeSelf != um) potMana.gameObject.SetActive(um);
        var sb = new System.Text.StringBuilder();
        foreach (var kv in p.elix) if (kv.Value > now) { if (sb.Length > 0) sb.Append("  ·  "); sb.Append(ElixName(kv.Key)).Append(' ').Append(Mathf.CeilToInt((kv.Value - now) / 60f)).Append('m'); }
        if (AHMeal.Active(p)) { if (sb.Length > 0) sb.Append("  ·  "); sb.Append(p.meal.name).Append(" ").Append(Mathf.CeilToInt((p.meal.until - AHMeal.Now) / 60000f)).Append("m"); }
        if (p.rested > 0) { if (sb.Length > 0) sb.Append("  ·  "); sb.Append("Rested · 2× XP"); }
        buffText.text = sb.ToString();
    }

    static string ElixName(string k) { return k == "might" ? "Might" : k == "swift" ? "Swiftness" : k == "iron" ? "Ironskin" : k == "dragon" ? "Dragonfire" : k == "ward" ? "Fire ward" : k == "titan" ? "Titan" : k == "raid" ? "Raider" : k; }

    static string SetLine(ItemDef d, AHBag bag)
    {
        if (d.set == null) return "";
        int have; bag.SetCounts().TryGetValue(d.set, out have);
        return "\nSet: " + AHItems.SetName(d.set) + " (" + have + " worn)";
    }

    // ---------- quests: the action button, name townPlates and quest marks, the quest window and the tracker ----------
    RectTransform talkBtn, dlgRoot, heroPlate;
    Text talkText, heroPlateText;
    Text dlgName, dlgQuest, dlgBody, dlgObj, dlgReward, dlgPrimaryText;
    Text trackTitle, trackLine;
    Action dlgPrimary;
    class NpcPlateUI { public AHNpc n; public RectTransform rt; public Text name, mark; }
    readonly List<NpcPlateUI> townPlates = new List<NpcPlateUI>();

    void BuildQuestUI()
    {
        // the big action button (web btnMain): Talk, Guild, Skin boar, ... depending on what is next to you
        talkBtn = Img("Action", transform, white, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(270, 58), new Color(0.2f, 0.45f, 0.25f, 1f));
        talkText = Center(Label(talkBtn, "T", "TALK", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(270, 40), Color.white));
        talkText.fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = talkBtn, act = OnAction });
        talkBtn.gameObject.SetActive(false);

        // a name over every townsperson, with the gold ! or ? over whoever has your quest
        foreach (var n in AHNpc.All)
        {
            var pl = new NpcPlateUI { n = n };
            pl.rt = Box("Plate " + n.npcName, transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300, 90));
            pl.mark = Label(pl.rt, "Mark", "", 54, TextAnchor.LowerCenter, new Vector2(0, 26), new Vector2(300, 64), new Color(1f, 0.85f, 0.2f));
            pl.mark.fontStyle = FontStyle.Bold;
            pl.name = Label(pl.rt, "Name", n.npcName, 19, TextAnchor.LowerCenter, new Vector2(0, 0), new Vector2(300, 26), n.isCaptain ? new Color(1f, 0.85f, 0.45f) : new Color(0.75f, 1f, 0.7f));
            foreach (var t in new[] { pl.mark, pl.name }) { t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0f); t.rectTransform.pivot = new Vector2(0.5f, 0f); }
            pl.rt.gameObject.SetActive(false);
            townPlates.Add(pl);
        }
        // your own name over your head
        heroPlate = Box("HeroPlate", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300, 28));
        heroPlateText = Label(heroPlate, "Name", "", 19, TextAnchor.LowerCenter, Vector2.zero, new Vector2(300, 26), new Color(0.85f, 0.92f, 1f));
        heroPlateText.rectTransform.anchorMin = heroPlateText.rectTransform.anchorMax = heroPlateText.rectTransform.pivot = new Vector2(0.5f, 0f);
        heroPlate.gameObject.SetActive(false);

        // quest tracker, top right under the buttons
        trackTitle = Label(transform, "TrackTitle", "", 20, TextAnchor.UpperRight, Vector2.zero, new Vector2(460, 28), new Color(1f, 0.8f, 0.35f));
        trackTitle.fontStyle = FontStyle.Bold;
        trackTitle.rectTransform.anchorMin = trackTitle.rectTransform.anchorMax = trackTitle.rectTransform.pivot = new Vector2(1, 1);
        trackTitle.rectTransform.anchoredPosition = new Vector2(-22, -104);
        trackLine = Label(transform, "TrackLine", "", 18, TextAnchor.UpperRight, Vector2.zero, new Vector2(460, 26), Color.white);
        trackLine.horizontalOverflow = HorizontalWrapMode.Wrap;
        trackLine.rectTransform.anchorMin = trackLine.rectTransform.anchorMax = trackLine.rectTransform.pivot = new Vector2(1, 1);
        trackLine.rectTransform.anchoredPosition = new Vector2(-22, -132);

        // the quest window (web questPanel)
        dlgRoot = Box("Dialog", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        dlgRoot.anchorMin = Vector2.zero; dlgRoot.anchorMax = Vector2.one; dlgRoot.offsetMin = dlgRoot.offsetMax = Vector2.zero;
        var dim = dlgRoot.gameObject.AddComponent<Image>(); dim.sprite = null; dim.color = new Color(0f, 0f, 0f, 0.45f); dim.raycastTarget = false;
        var win = Img("DialogWindow", dlgRoot, white, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(720, 470), new Color(0.1f, 0.075f, 0.055f, 1f));
        Img("Edge", win, white, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(720, 6), new Color(0.85f, 0.65f, 0.3f, 1f));
        dlgName = Label(win, "Name", "", 28, TextAnchor.UpperLeft, new Vector2(24, -16), new Vector2(560, 40), new Color(1f, 0.85f, 0.45f));
        dlgName.fontStyle = FontStyle.Bold;
        dlgQuest = Label(win, "Quest", "", 23, TextAnchor.UpperLeft, new Vector2(24, -58), new Vector2(672, 32), new Color(1f, 0.8f, 0.35f));
        dlgQuest.fontStyle = FontStyle.Bold;
        dlgBody = Label(win, "Body", "", 20, TextAnchor.UpperLeft, new Vector2(24, -94), new Vector2(672, 130), Color.white);
        dlgBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        dlgObj = Label(win, "Objectives", "", 20, TextAnchor.UpperLeft, new Vector2(24, -232), new Vector2(672, 90), new Color(0.85f, 0.85f, 0.8f));
        dlgObj.supportRichText = true;
        dlgReward = Label(win, "Reward", "", 18, TextAnchor.UpperLeft, new Vector2(24, -326), new Vector2(672, 50), new Color(1f, 0.84f, 0.35f));
        dlgReward.horizontalOverflow = HorizontalWrapMode.Wrap;
        var ok = Img("Primary", win, white, new Vector2(0.5f, 0f), new Vector2(-120, 46), new Vector2(220, 60), new Color(0.22f, 0.5f, 0.26f, 1f));
        dlgPrimaryText = Center(Label(ok, "T", "", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(220, 40), Color.white));
        dlgPrimaryText.fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = ok, layer = 3, act = () => { if (dlgPrimary != null) dlgPrimary(); } });
        var close = Img("Close", win, white, new Vector2(0.5f, 0f), new Vector2(120, 46), new Vector2(220, 60), new Color(0.3f, 0.22f, 0.17f, 1f));
        Center(Label(close, "T", "Close", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(220, 40), Color.white));
        taps.Add(new TapBtn { rt = close, layer = 3, act = () => ShowDialog(false) });
        dlgRoot.gameObject.SetActive(false);
    }

    public bool DialogOpen { get { return dlgRoot != null && dlgRoot.gameObject.activeSelf; } }

    public void ShowDialog(bool on)
    {
        if (dlgRoot == null) return;
        dlgRoot.gameObject.SetActive(on);
        dlgRoot.SetAsLastSibling();
        ptrs.Clear();
    }

    // web: Reward: 60 Woodcutting XP · 1 copper · 2× Grilled Trout
    string RewardText(QuestDef q)
    {
        var parts = new List<string>();
        if (q.xpSkill != null) parts.Add(q.xp + " " + AHDB.SkillName(q.xpSkill) + " XP");
        if (q.gold > 0) parts.Add(AHItems.MoneyText((long)q.gold * AHDB.CU));
        var its = new List<string>();
        foreach (var kv in q.items)
        {
            var d = AHItems.Get(AHQuests.RewardItem(kv.Key, g.player.cls.id));
            if (d != null) its.Add((kv.Value > 1 ? kv.Value + "× " : "") + d.name);
        }
        if (its.Count > 0) parts.Add(string.Join(", ", its.ToArray()));
        return "Reward: " + string.Join(" · ", parts.ToArray());
    }

    string ObjText(QuestDef q, bool zero)
    {
        var log = g.quests; var sb = new System.Text.StringBuilder();
        for (int k = 0; k < q.obj.Count; k++)
        {
            int have = zero ? 0 : log.Prog(k); var o = q.obj[k];
            bool done = have >= o.n;
            sb.Append(done ? "<color=#8fe08a>✓ " : "• ").Append(o.label).Append("  <b>").Append(have).Append('/').Append(o.n).Append("</b>").Append(done ? "</color>" : "").Append('\n');
        }
        return sb.ToString();
    }

    // floating names stay off the hero panel (top left) and the minimap and tracker (top right)
    static bool UnderHud(Vector3 sp)
    {
        float w = Screen.width, h = Screen.height;
        if (sp.y > h * 0.50f && sp.x < w * 0.37f) return true;   // name, bars, money, daily and event chips
        if (sp.y > h * 0.68f && sp.x > w * 0.56f) return true;   // the top buttons and the quest tracker
        return false;
    }

    AHNpc NearNpc()
    {
        var p = g.player;
        if (p == null || p.dead) return null;
        return AHNpc.Nearest(p.transform.position);
    }

    public void OnAction()
    {
        if (g.player != null && g.player.Busy) return;
        var n = NearNpc();
        // people win over things (a shop door, a bank, a chest) unless the thing is much closer
        var s = g.player != null ? AHGather.Nearest(g.player.transform.position) : null;
        if (n != null && s != null && s.kind == "use")
        {
            Vector3 pp = g.player.transform.position;
            float ds = (s.pos - pp).magnitude, dn = n.DistTo(pp);
            if (n.npcName != g.quests.NpcName && ds < dn * 0.5f) n = null;
        }
        if (n != null) { g.TalkTo(n); return; }
        if (g.player != null) g.player.Interact();
    }

    // web openQuest: who you are talking to, the quest, its objectives and reward, and one button
    public void OpenQuest()
    {
        ShowBag(false);
        var log = g.quests;
        var p = g.player;
        var q = log.Current;
        var who = q != null ? AHNpc.Find(log.NpcName) : AHNpc.Find(AHQuests.NpcName);
        dlgName.text = who != null ? who.npcName : AHQuests.NpcName;
        dlgObj.text = ""; dlgReward.text = "";
        if (q == null)
        {
            dlgQuest.text = "";
            dlgBody.text = "You have done everything I asked, hero. Ashen Hollow is safer thanks to you. More adventures are coming.";
            dlgPrimaryText.text = "Farewell";
            dlgPrimary = () => ShowDialog(false);
            ShowDialog(true);
            return;
        }
        string st = log.state;
        if (who != null && who.DistTo(p.transform.position) > 260f * AHDB.S)
        {
            dlgQuest.text = q.name;
            dlgBody.text = st == "offer" ? "Your next task waits with " + who.npcName + " in Ashen Hollow." : st == "ready" ? "Take the good news to " + who.npcName + " in Ashen Hollow." : q.text;
            dlgObj.text = ObjText(q, st == "offer");
            dlgPrimaryText.text = "Close";
            dlgPrimary = () => ShowDialog(false);
            ShowDialog(true);
            return;
        }
        dlgQuest.text = q.name;
        dlgBody.text = st == "ready" ? "You did it. Here is your reward, as promised." : q.text;
        dlgObj.text = ObjText(q, st == "offer");
        dlgReward.text = RewardText(q);
        dlgPrimaryText.text = st == "offer" ? "Accept" : st == "ready" ? "Claim reward" : "On my way";
        dlgPrimary = () =>
        {
            if (st == "offer") { log.Accept(); Toast("Quest accepted: " + q.name, 3f); ShowDialog(false); return; }
            if (st == "ready")
            {
                string msg = log.Claim(p, g);
                if (msg == null) { ShowDialog(false); return; }
                Toast(msg, 3f);
                if (msg.StartsWith("Make room")) return;
                if (log.state == "offer" && log.Current != null && msg.StartsWith("Quest complete")) { OpenQuest(); return; }
                ShowDialog(false);
                return;
            }
            ShowDialog(false);
        };
        ShowDialog(true);
    }

    static readonly Color markGold = new Color(1f, 0.85f, 0.2f), greyMark = new Color(0.75f, 0.75f, 0.75f, 0.9f);

    void UpdateQuestUI()
    {
        var p = g.player;
        var log = g.quests;
        // the action button: talk to someone, or skin the body at your feet
        string act = null;
        if (p != null && !p.dead)
        {
            var n = NearNpc();
            var us = AHGather.Nearest(p.transform.position);
            if (n != null && us != null && us.kind == "use" && (us.pos - p.transform.position).sqrMagnitude < (n.transform.position - p.transform.position).sqrMagnitude) n = null;
            if (p.Busy) act = p.ActionName.ToUpperInvariant() + "… " + Mathf.RoundToInt(p.ActionFrac * 100) + "%";
            else if (n != null) act = n.Label.ToUpperInvariant();
            else
            {
                var sk = p.SkinTarget();
                if (sk != null) act = "SKIN " + sk.type.name.ToUpperInvariant();
                else { var gs = AHGather.Nearest(p.transform.position); if (gs != null) act = AHGather.Label(gs).ToUpperInvariant(); else if (AHGather.CanLight(p)) act = "LIGHT FIRE"; }
            }
        }
        talkBtn.gameObject.SetActive(Modal == 0 && act != null);
        if (act != null) talkText.text = act;

        // names and quest marks over the townsfolk
        string qn = log.Current != null ? log.NpcName : null;
        foreach (var pl in townPlates)
        {
            bool show = false;
            if (pl.n != null && g.cam != null && p != null && Modal == 0)
            {
                Vector3 wp = pl.n.transform.position + Vector3.up * (pl.n.height + 0.25f);
                Vector3 sp = g.cam.WorldToScreenPoint(wp);
                if (sp.z > 0f && !UnderHud(sp) && (wp - p.transform.position).sqrMagnitude < 26f * 26f)
                {
                    show = true;
                    pl.rt.position = sp;
                    if (qn != null && qn == pl.n.npcName)
                    {
                        pl.mark.text = log.state == "offer" ? "!" : "?";
                        pl.mark.color = log.state == "active" ? greyMark : markGold;
                    }
                    else if (AHTales.HasMark(p, pl.n.npcName)) { pl.mark.text = "!"; pl.mark.color = markGold; }   // a townsfolk tale
                    else pl.mark.text = "";
                }
            }
            if (pl.rt.gameObject.activeSelf != show) pl.rt.gameObject.SetActive(show);
        }
        bool hs = false;
        if (p != null && g.cam != null && Modal == 0 && !string.IsNullOrEmpty(p.heroName))
        {
            Vector3 sp = g.cam.WorldToScreenPoint(p.transform.position + Vector3.up * 2.2f);
            if (sp.z > 0f) { hs = true; heroPlate.position = sp; heroPlateText.text = p.heroName + (AHAch.Title(p) != null ? "\n<size=13><color=#ffd27a>“" + AHAch.Title(p) + "”</color></size>" : ""); heroPlateText.verticalOverflow = VerticalWrapMode.Overflow; }
        }
        if (heroPlate.gameObject.activeSelf != hs) heroPlate.gameObject.SetActive(hs);

        // the tracker (web renderTracker)
        string title;
        string line = p != null ? log.Tracker(p, out title) : (title = "");
        trackTitle.text = title;
        trackLine.text = line;
        trackLine.color = log.state == "ready" ? new Color(0.6f, 1f, 0.55f) : Color.white;
    }

    bool HasRole(Role r)
    {
        foreach (var kv in ptrs) if (kv.Value.role == r) return true;
        return false;
    }
}
