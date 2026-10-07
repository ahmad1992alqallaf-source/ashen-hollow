// Ashen Hollow: the Settings window (sound, graphics, camera, interface, account), the Emotes window and its HUD
// button, and the hero select screen (several heroes on one device, log out, change character).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

// the player's settings, shared by every hero on the device
public static class AHSettings
{
    static int I(string k, int d) { return AHPrefs.GetInt("ah_set_" + k, d); }
    static void S(string k, int v) { AHPrefs.SetInt("ah_set_" + k, v); AHPrefs.Save(); }

    public static int Quality { get { return I("quality", 2); } set { S("quality", value); } }   // 0 low, 1 medium, 2 high, 3 custom
    public static int Shadows { get { return I("shadows", 2); } set { S("shadows", value); } }   // off, sharp, soft
    public static int Fps { get { return I("fps", 1); } set { S("fps", value); } }               // 30, 60, as fast as it can
    public static int AA { get { return I("aa", 0); } set { S("aa", value); } }                  // off, FXAA, SMAA
    public static int Post { get { return I("post", 1); } set { S("post", value); } }            // glow and colour grading
    public static int View { get { return I("view", 1); } set { S("view", value); } }            // near, normal, far
    public static int Grass { get { return I("grass", 1); } set { S("grass", value); } }          // light, normal, thick
    public static int Cam { get { return I("cam", 1); } set { S("cam", value); } }                // close, normal, far
    public static int Awake { get { return I("awake", 1); } set { S("awake", value); } }          // keep the screen on
    public static int Tips { get { return I("tips", 1); } set { S("tips", value); } }             // floating words over heads (emotes, loot)

    public static float Reach { get { return View == 0 ? 140f : View == 1 ? 195f : 250f; } }

    public static void Preset(int q)
    {
        Quality = q;
        if (q == 0) { Shadows = 0; AA = 0; Post = 0; View = 0; Grass = 0; Fps = 0; }
        else if (q == 1) { Shadows = 1; AA = 1; Post = 1; View = 1; Grass = 1; Fps = 1; }
        else { Shadows = 2; AA = 2; Post = 1; View = 2; Grass = 2; Fps = 1; }
    }
    public static void Reset()
    {
        foreach (var k in new[] { "quality", "shadows", "fps", "aa", "post", "view", "grass", "cam", "awake", "tips" }) AHPrefs.DeleteKey("ah_set_" + k);
        AHSound.Muted = false; AHSound.MusicVol = 0.5f; AHSound.SfxVol = 0.8f;
    }

    // puts the settings into the running game (each land calls this once it is built)
    public static void Apply(AHGame g, bool camToo = true)
    {
        if (g == null) return;
        var sun = RenderSettings.sun;
        if (sun != null) sun.shadows = Shadows == 0 ? LightShadows.None : Shadows == 1 ? LightShadows.Hard : LightShadows.Soft;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Fps == 0 ? 30 : Fps == 1 ? 60 : 120;
        Screen.sleepTimeout = Awake == 1 ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        if (g.cam != null)
        {
            var cd = g.cam.GetComponent<UniversalAdditionalCameraData>();
            if (cd != null)
            {
                cd.antialiasing = AA == 0 ? AntialiasingMode.None : AA == 1 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cd.renderPostProcessing = Post == 1;
            }
            float reach = View == 0 ? 140f : View == 1 ? 195f : 250f;
            var lc = new float[32]; for (int i = 0; i < 32; i++) lc[i] = reach; lc[AHRange.Layer] = 420f; g.cam.layerCullDistances = lc;
            if (!AHDungeon.IsDungeon(AHGame.AreaId)) { RenderSettings.fogStartDistance = reach * 0.36f; RenderSettings.fogEndDistance = reach - 5f; }
            if (camToo) g.camDistance = Cam == 0 ? 5f : Cam == 1 ? 7f : 10f;
        }
        if (g.grass != null) { g.grass.density = Grass == 0 ? 0.38f : Grass == 1 ? 0.62f : 0.85f; g.grass.radius = Grass == 0 ? 10f : Grass == 1 ? 14f : 18f; }
    }
}

public partial class AHUI
{
    // ---------- Settings ----------
    public void OpenSettings() { wkMode = "settings"; wkPageI = 0; ShowWork(true); RenderWork(); }

    WkBtn Opt(string label, bool sel, Action act) { return new WkBtn { label = label, on = true, col = sel ? Go : Plain, act = () => { act(); AHSettings.Apply(g, false); RenderWork(); } }; }
    WkBtn Pick(string label, int cur, int v, Action<int> set, bool custom = true) { return Opt(label, cur == v, () => { set(v); if (custom) AHSettings.Quality = 3; }); }

    void RenderOptions(AHPlayer p)
    {
        wkTitle.text = "Settings";
        wkHint.text = "Sound, graphics, camera and your account. Changes take effect at once.";
        var rows = new List<Action<int>>();
        Color cS = new Color(0.8f, 0.8f, 1f), cG = new Color(0.61f, 0.89f, 1f), cC = new Color(1f, 0.8f, 0.45f), cA = new Color(1f, 0.6f, 0.75f);
        // sound
        rows.Add(s => Row(s, "Sound", cS, AHSound.Muted ? "All sound is off" : "Music and effects are on", "",
            Opt("On", !AHSound.Muted, () => AHSound.Muted = false), Opt("Off", AHSound.Muted, () => AHSound.Muted = true)));
        rows.Add(s => Row(s, "Music  " + Mathf.RoundToInt(AHSound.MusicVol * 100) + "%", cS, "It follows where you are: town, wild, night, dungeon, battle.", "",
            Opt("−", false, () => AHSound.MusicVol = Mathf.Max(0f, Mathf.Round(AHSound.MusicVol * 10f - 1f) / 10f)),
            Opt("+", false, () => AHSound.MusicVol = Mathf.Min(1f, Mathf.Round(AHSound.MusicVol * 10f + 1f) / 10f))));
        rows.Add(s => Row(s, "Effects  " + Mathf.RoundToInt(AHSound.SfxVol * 100) + "%", cS, "Swings, spells, footsteps and the world around you.", "",
            Opt("−", false, () => { AHSound.SfxVol = Mathf.Max(0f, Mathf.Round(AHSound.SfxVol * 10f - 1f) / 10f); AHSound.Play("swing"); }),
            Opt("+", false, () => { AHSound.SfxVol = Mathf.Min(1f, Mathf.Round(AHSound.SfxVol * 10f + 1f) / 10f); AHSound.Play("swing"); })));
        // graphics
        int q = AHSettings.Quality;
        rows.Add(s => Row(s, "Graphics", cG, q == 3 ? "Custom (your own mix below)" : "Low runs best on older phones; High looks best.", "",
            Opt("Low", q == 0, () => AHSettings.Preset(0)), Opt("Medium", q == 1, () => AHSettings.Preset(1)), Opt("High", q == 2, () => AHSettings.Preset(2))));
        rows.Add(s => Row(s, "Shadows", cG, "Off is fastest; soft shadows look best.", "",
            Pick("Off", AHSettings.Shadows, 0, v => AHSettings.Shadows = v), Pick("Sharp", AHSettings.Shadows, 1, v => AHSettings.Shadows = v), Pick("Soft", AHSettings.Shadows, 2, v => AHSettings.Shadows = v)));
        rows.Add(s => Row(s, "Frame rate", cG, "30 saves battery; 60 is smooth.", "",
            Pick("30", AHSettings.Fps, 0, v => AHSettings.Fps = v), Pick("60", AHSettings.Fps, 1, v => AHSettings.Fps = v), Pick("Max", AHSettings.Fps, 2, v => AHSettings.Fps = v)));
        rows.Add(s => Row(s, "Smooth edges", cG, "Softens jagged edges (anti-aliasing).", "",
            Pick("Off", AHSettings.AA, 0, v => AHSettings.AA = v), Pick("Fast", AHSettings.AA, 1, v => AHSettings.AA = v), Pick("Sharp", AHSettings.AA, 2, v => AHSettings.AA = v)));
        rows.Add(s => Row(s, "Glow and colour", cG, "Bloom on spells and the colour of the light.", "",
            Pick("On", AHSettings.Post, 1, v => AHSettings.Post = v), Pick("Off", AHSettings.Post, 0, v => AHSettings.Post = v)));
        rows.Add(s => Row(s, "View distance", cG, "How far the land is drawn before the haze.", "",
            Pick("Near", AHSettings.View, 0, v => AHSettings.View = v), Pick("Normal", AHSettings.View, 1, v => AHSettings.View = v), Pick("Far", AHSettings.View, 2, v => AHSettings.View = v)));
        rows.Add(s => Row(s, "Grass", cG, "How thick the grass grows around you.", "",
            Pick("Light", AHSettings.Grass, 0, v => AHSettings.Grass = v), Pick("Normal", AHSettings.Grass, 1, v => AHSettings.Grass = v), Pick("Thick", AHSettings.Grass, 2, v => AHSettings.Grass = v)));
        // camera and interface
        rows.Add(s => Row(s, "Camera distance", cC, "How far behind you the camera starts (pinch or scroll to zoom).", "",
            new WkBtn { label = "Close", on = true, col = AHSettings.Cam == 0 ? Go : Plain, act = () => { AHSettings.Cam = 0; AHSettings.Apply(g); zoom = 1f; RenderWork(); } },
            new WkBtn { label = "Normal", on = true, col = AHSettings.Cam == 1 ? Go : Plain, act = () => { AHSettings.Cam = 1; AHSettings.Apply(g); zoom = 1f; RenderWork(); } },
            new WkBtn { label = "Far", on = true, col = AHSettings.Cam == 2 ? Go : Plain, act = () => { AHSettings.Cam = 2; AHSettings.Apply(g); zoom = 1f; RenderWork(); } }));
        rows.Add(s => Row(s, "Floating numbers", cC, "Damage, healing, loot and emotes shown over heads.", "",
            Pick("On", AHSettings.Tips, 1, v => AHSettings.Tips = v, false), Pick("Off", AHSettings.Tips, 0, v => AHSettings.Tips = v, false)));
        rows.Add(s => Row(s, "Keep screen on", cC, "The phone will not dim while you play.", "",
            Pick("On", AHSettings.Awake, 1, v => AHSettings.Awake = v, false), Pick("Off", AHSettings.Awake, 0, v => AHSettings.Awake = v, false)));
        bool vr = AHPrefs.GetInt("ah_vroid", 1) == 1, ds = AHPrefs.GetInt("ah_dreamscape", 1) == 1;
        rows.Add(s => Row(s, "Hero model", cC, vr ? "The new detailed hero" : "The classic hero", "Changing it reloads the land.",
            new WkBtn { label = "New", on = true, col = vr ? Go : Plain, act = () => { if (!vr) { AHPrefs.SetInt("ah_vroid", 1); ShowWork(false); g.Reload(false); } } },
            new WkBtn { label = "Classic", on = true, col = !vr ? Go : Plain, act = () => { if (vr) { AHPrefs.SetInt("ah_vroid", 0); ShowWork(false); g.Reload(false); } } }));
        rows.Add(s => Row(s, "Meadow nature", cC, ds ? "Lush painted trees, bushes and flowers" : "The classic trees", "Changing it reloads the land.",
            new WkBtn { label = "Lush", on = true, col = ds ? Go : Plain, act = () => { if (!ds) { AHPrefs.SetInt("ah_dreamscape", 1); ShowWork(false); g.Reload(false); } } },
            new WkBtn { label = "Classic", on = true, col = !ds ? Go : Plain, act = () => { if (ds) { AHPrefs.SetInt("ah_dreamscape", 0); ShowWork(false); g.Reload(false); } } }));
        // account
        rows.Add(s => Row(s, "Your hero: " + (p.heroName ?? "Hero"), cA, p.cls.name + " · level " + p.level + " · " + AHPrefs.Slots().Count + " of " + AHPrefs.MaxHeroes + " heroes on this device", "",
            new WkBtn { label = "Change hero", on = true, col = Go, act = () => { ShowWork(false); g.Logout(); } }));
        rows.Add(s => Row(s, "Log out", cA, "Saves your hero and goes back to the hero select screen.", "",
            new WkBtn { label = "Log out", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { ShowWork(false); g.Logout(); } }));
        rows.Add(s => Row(s, "This device", cA, "ID " + AHPrefs.DeviceId.Substring(0, 8).ToUpperInvariant(), "Kept for fair play when heroes are stored online."));
        rows.Add(s => Row(s, "Back to defaults", cA, "Every setting above as it first was (your heroes are not touched).", "",
            new WkBtn { label = "Reset", on = true, col = Plain, act = () => { AHSettings.Reset(); AHSettings.Apply(g); RenderWork(); } }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    // ---------- Emotes ----------
    RectTransform emoteBtn;
    void BuildEmoteHud()
    {
        emoteBtn = Img("EmoteBtn", transform, circle, new Vector2(1, 0), new Vector2(-58, 402), new Vector2(64, 64), new Color(0.3f, 0.2f, 0.32f, 0.92f));
        Img("Rim", emoteBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), new Color(1f, 0.8f, 0.45f, 1f));
        Center(Label(emoteBtn, "T", "EMOTE", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 24), Color.white));
        taps.Add(new TapBtn { rt = emoteBtn, act = OpenEmotes });
    }
    // the lightfoot button (unlocked at level 5): three pips show the leaps you have
    RectTransform leapBtn; Image[] leapPips;
    void BuildLeapHud()
    {
        leapBtn = Img("LeapBtn", transform, circle, new Vector2(1, 0), new Vector2(-58, 482), new Vector2(64, 64), new Color(0.16f, 0.3f, 0.4f, 0.92f));
        Img("Rim", leapBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), new Color(0.75f, 0.9f, 1f, 1f));
        Center(Label(leapBtn, "T", "LEAP", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 24), Color.white));
        leapPips = new Image[AHPlayer.LeapMax];
        for (int i = 0; i < leapPips.Length; i++) leapPips[i] = Img("Pip", leapBtn, circle, new Vector2(0.5f, 0f), new Vector2((i - 1) * 14f, -8f), new Vector2(10, 10), Color.white).GetComponent<Image>();
        taps.Add(new TapBtn { rt = leapBtn, act = () => { leapPressed = true; } });
    }
    void LeapHudTick()
    {
        var p = g.player; if (leapBtn == null || p == null) return;
        bool on = p.LeapUnlocked && Modal == 0 && !PhotoOn;
        if (leapBtn.gameObject.activeSelf != on) leapBtn.gameObject.SetActive(on);
        if (!on) return;
        for (int i = 0; i < leapPips.Length; i++) leapPips[i].color = i < p.leapCharges ? new Color(0.75f, 0.95f, 1f) : new Color(1f, 1f, 1f, 0.2f);
    }

    public void OpenEmotes() { wkMode = "emotes"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderEmotes(AHPlayer p)
    {
        wkTitle.text = "Emotes";
        wkHint.text = "Tap one and the window closes so you can watch. Moving or fighting ends it.";
        SetPager(false); wkPage.text = "";
        var groups = new[]
        {
            new[] { "Greetings", "wave", "bow", "yes", "no" },
            new[] { "Fun", "cheer", "dance", "point", "box" },
            new[] { "Rest", "sit", "talk", "fold", "eat" },
        };
        for (int r = 0; r < groups.Length; r++)
        {
            var gr = groups[r]; var btns = new List<WkBtn>();
            for (int i = 1; i < gr.Length; i++)
            {
                var e = AHEmote.Find(gr[i]); if (e == null) continue; string id = e.id;
                btns.Add(new WkBtn { label = e.name, on = true, col = p.emote == id ? Go : Plain, act = () => { ShowWork(false); g.player.Emote(id); } });
            }
            Row(r, gr[0], new Color(1f, 0.8f, 0.45f), "", "", btns.ToArray());
        }
        Row(3, "Photo mode", new Color(0.8f, 0.8f, 1f), "Hide the screen buttons to take a picture (drag to turn the camera)", "",
            new WkBtn { label = "Photo", on = true, col = Go, act = () => PhotoMode(true) });
        if (p.emote != null)
            Row(4, "Stop", new Color(1f, 1f, 1f, 0.7f), "End the emote you are doing", "", new WkBtn { label = "Stop", on = true, col = Plain, act = () => { g.player.StopEmote(); RenderWork(); } });
    }

    // ---------- the hero select screen ----------
    RectTransform selRoot, selList, selView; RawImage selRaw; Text selName, selInfo, selDelT, selMsg;
    int selPick = -1; float selDelArm;
    public bool SelectOpen { get { return selRoot != null && selRoot.gameObject.activeSelf; } }

    public void OpenSelect()
    {
        ShowBag(false); ShowDialog(false); ShowWork(false);
        if (selRoot == null) BuildSelect();
        selRoot.gameObject.SetActive(true); selRoot.SetAsLastSibling();
        Time.timeScale = 0f; ptrs.Clear();
        selPick = AHPrefs.Slot; selDelArm = 0f;
        if (!AHPrefs.Slots().Contains(selPick)) { var l = AHPrefs.Slots(); selPick = l.Count > 0 ? l[0] : -1; }
        RenderSelect();
    }
    void CloseSelect() { if (selRoot != null) selRoot.gameObject.SetActive(false); Time.timeScale = 1f; ptrs.Clear(); AHItemStudio.StopHero(); RemoveTaps("sel"); }

    void BuildSelect()
    {
        selRoot = Box("Select", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        selRoot.anchorMin = Vector2.zero; selRoot.anchorMax = Vector2.one; selRoot.offsetMin = selRoot.offsetMax = Vector2.zero;
        var dim = Img("Dim", selRoot, white, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.025f, 0.02f, 0.82f));
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.offsetMin = dim.offsetMax = Vector2.zero;
        var t = Center(Label(selRoot, "Title", "Choose your hero", 40, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(900, 56), Gold));
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f); t.rectTransform.anchoredPosition = new Vector2(0, -42); t.fontStyle = FontStyle.Bold;
        // the hero standing, on the left
        var stand = Img("Stand", selRoot, panel9 != null ? panel9 : white, new Vector2(0.5f, 0.5f), new Vector2(-265, -15), new Vector2(300, 470), new Color(0.13f, 0.1f, 0.08f, 0.96f));
        if (panel9 != null) stand.GetComponent<Image>().type = Image.Type.Sliced;
        if (glow != null) Img("Glow", stand, glow, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(300, 340), new Color(1f, 0.75f, 0.4f, 0.28f));
        selView = Box("Hero", stand, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(260, 364));
        selRaw = selView.gameObject.AddComponent<RawImage>(); selRaw.raycastTarget = false;
        selName = Center(Label(stand, "Name", "", 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(290, 32), Gold)); selName.fontStyle = FontStyle.Bold;
        selName.rectTransform.anchoredPosition = new Vector2(0, -172);
        selInfo = Center(Label(stand, "Info", "", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(290, 24), new Color(1f, 1f, 1f, 0.75f)));
        selInfo.rectTransform.anchoredPosition = new Vector2(0, -200);
        // the list, on the right
        selList = Box("List", selRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230, 40), new Vector2(620, 460));
        selMsg = Center(Label(selRoot, "Msg", "", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620, 24), new Color(1f, 0.7f, 0.55f)));
        selMsg.rectTransform.anchoredPosition = new Vector2(150, -205);
        selRoot.gameObject.SetActive(false);
    }

    readonly List<GameObject> selItems = new List<GameObject>();
    RectTransform SelBtn(string name, string label, Vector2 pos, Vector2 size, Color c, Action act)
    {
        var b = Img(name, selRoot, white, new Vector2(0.5f, 0.5f), pos, size, c); selItems.Add(b.gameObject);
        Center(Label(b, "T", label, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(size.x, 34), Color.white)).fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = b, layer = 6, group = "sel", act = act });
        return b;
    }

    void RenderSelect()
    {
        foreach (var go in selItems) Destroy(go); selItems.Clear(); RemoveTaps("sel");
        var slots = AHPrefs.Slots();
        if (selPick >= 0 && !slots.Contains(selPick)) selPick = slots.Count > 0 ? slots[0] : -1;
        // the cards: two columns of three
        for (int i = 0; i < AHPrefs.MaxHeroes; i++)
        {
            int col = i % 2, row = i / 2;
            Vector2 pos = new Vector2(20 + col * 262, 160 - row * 94);
            if (i < slots.Count)
            {
                int sl = slots[i]; var h = HeroInfo(sl); var cls = AHClasses.Get(h.cls);
                bool on = sl == selPick;
                var card = Img("Card", selRoot, white, new Vector2(0.5f, 0.5f), pos, new Vector2(252, 86), on ? new Color(0.3f, 0.22f, 0.12f, 1f) : new Color(0.16f, 0.12f, 0.09f, 1f));
                selItems.Add(card.gameObject);
                Img("Stripe", card, white, new Vector2(0f, 0.5f), new Vector2(4, 0), new Vector2(8, 86), cls != null ? cls.color : Color.gray);
                if (on) Img("Rim", card, white, new Vector2(0.5f, 1f), new Vector2(0, -2), new Vector2(252, 4), Gold);
                var nm = Label(card, "N", string.IsNullOrEmpty(h.name) ? "Unnamed hero" : h.name, 20, TextAnchor.UpperLeft, new Vector2(18, -6), new Vector2(230, 26), on ? Gold : Color.white); nm.fontStyle = FontStyle.Bold;
                Label(card, "C", "Level " + h.level + " " + (cls != null ? cls.name : h.cls), 15, TextAnchor.UpperLeft, new Vector2(18, -34), new Vector2(230, 20), new Color(1f, 1f, 1f, 0.8f));
                Label(card, "A", AreaName(h.area) + (h.t > 0 ? " · " + Ago(h.t) : ""), 13, TextAnchor.UpperLeft, new Vector2(18, -58), new Vector2(230, 18), new Color(1f, 1f, 1f, 0.55f));
                taps.Add(new TapBtn { rt = card, layer = 6, group = "sel", act = () => { if (selPick == sl) { PlaySel(); return; } selPick = sl; selDelArm = 0f; selMsg.text = ""; RenderSelect(); } });
            }
            else
            {
                var card = Img("Empty", selRoot, white, new Vector2(0.5f, 0.5f), pos, new Vector2(252, 86), new Color(0.1f, 0.08f, 0.06f, 0.9f));
                selItems.Add(card.gameObject);
                Center(Label(card, "T", i == slots.Count ? "+  New hero" : "Empty", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(252, 30), new Color(1f, 1f, 1f, i == slots.Count ? 0.85f : 0.3f)));
                if (i == slots.Count) taps.Add(new TapBtn { rt = card, layer = 6, group = "sel", act = NewHero });
            }
        }
        // the hero picked
        if (selPick >= 0)
        {
            var h = HeroInfo(selPick); var cls = AHClasses.Get(h.cls);
            selName.text = string.IsNullOrEmpty(h.name) ? "Unnamed hero" : h.name;
            selInfo.text = "Level " + h.level + " " + (cls != null ? cls.name : "") + " · " + AreaName(h.area);
            AHItemStudio.Campfire = true;   // the hero select screen: your hero stands by a campfire
            if (selPick == AHPrefs.Slot && g.player != null) selRaw.texture = AHItemStudio.Hero(g.player);
            else
            {
                AHLook look = null; try { if (!string.IsNullOrEmpty(h.look)) look = JsonUtility.FromJson<AHLook>(h.look); } catch { }
                var gear = new Dictionary<string, string>();
                foreach (var kv in (h.gear ?? "").Split(',')) { int e = kv.IndexOf('='); if (e > 0 && e < kv.Length - 1) gear[kv.Substring(0, e)] = kv.Substring(e + 1); }
                selRaw.texture = AHItemStudio.HeroOf(cls ?? g.player.cls, look, s => { string v; return gear.TryGetValue(s, out v) ? v : null; });
            }
            selRaw.enabled = selRaw.texture != null;
            SelBtn("Play", "Play", new Vector2(20, -140), new Vector2(240, 60), Go, PlaySel);
            selDelT = Center(Label(SelBtn("Del", "", new Vector2(282, -140), new Vector2(240, 60), new Color(0.45f, 0.22f, 0.17f, 1f), DeleteSel), "D", selDelArm > Time.unscaledTime ? "Tap again to delete" : "Delete", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(240, 34), Color.white));
        }
        else { selName.text = "No heroes yet"; selInfo.text = "Make your first hero."; selRaw.enabled = false; SelBtn("New", "Make a hero", new Vector2(150, -140), new Vector2(260, 60), Go, NewHero); }
#if !UNITY_EDITOR
        SelBtn("Quit", "Quit", new Vector2(150, -250), new Vector2(160, 46), Plain, Application.Quit);
#endif
    }

    AHPrefs.Hero HeroInfo(int slot)
    {
        var h = AHPrefs.Info(slot);
        if (slot == AHPrefs.Slot && g.player != null && g.player.cls != null) { h.name = g.player.heroName ?? ""; h.cls = g.player.cls.id; h.level = g.player.level; h.area = AHGame.AreaId; }
        return h;
    }
    static string AreaName(string id)
    {
        if (string.IsNullOrEmpty(id)) return "Hollow Meadow";
        var c = AHDB.List("world", "REGIONS");
        if (c != null) foreach (var o in c) if (AHJson.S(o, "id") == id) return AHJson.S(o, "name", id);
        return char.ToUpperInvariant(id[0]) + id.Substring(1).Replace('_', ' ');
    }
    static string Ago(long t)
    {
        double m = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - t) / 60000.0;
        if (m < 2) return "just now"; if (m < 60) return (int)m + " min ago"; if (m < 48 * 60) return (int)(m / 60) + " h ago"; return (int)(m / 1440) + " days ago";
    }

    void PlaySel()
    {
        if (selPick < 0) return;
        if (selPick == AHPrefs.Slot && g.player != null && AHPrefs.GetString("ah_class", "") != "") { CloseSelect(); g.Welcome(); return; }
        g.SaveProgress(); AHPrefs.Use(selPick); CloseSelect(); g.Reload(false, true);
    }
    void NewHero()
    {
        if (AHPrefs.Slots().Count >= AHPrefs.MaxHeroes) { selMsg.text = "This device holds " + AHPrefs.MaxHeroes + " heroes. Delete one to make room."; return; }
        g.SaveProgress();
        AHPrefs.Use(-1); CloseSelect(); g.Reload(false, true);
    }
    void DeleteSel()
    {
        if (selPick < 0) return;
        if (selDelArm < Time.unscaledTime) { selDelArm = Time.unscaledTime + 3f; selMsg.text = "Deleting a hero cannot be undone."; RenderSelect(); return; }
        int sl = selPick; bool cur = sl == AHPrefs.Slot;
        AHPrefs.Delete(sl); selDelArm = 0f; selMsg.text = "Hero deleted.";
        var l = AHPrefs.Slots(); selPick = l.Count > 0 ? l[0] : -1;
        if (cur) { AHPrefs.Use(selPick >= 0 ? selPick : -1); CloseSelect(); g.Reload(true, true); return; }
        RenderSelect();
    }
}
