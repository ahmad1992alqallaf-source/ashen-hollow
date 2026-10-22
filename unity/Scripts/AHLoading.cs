// Ashen Hollow: the loading screen between lands. Two to five seconds of the realm's map drifting slowly, the land
// you are going to in large letters, a tip, and a bar filling along the bottom; it fades away once the land is built
// (never sooner than two seconds, so it doesn't flash, and never kept past five once the land is ready).
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class AHLoading : MonoBehaviour
{
    static AHLoading cur;
    Canvas canvas; RawImage bg; Image bar, barBack; Text title, sub, tip; CanvasGroup group;
    float t, readyAt = -1f, minT, fadeT = -1f;
    const float MaxT = 5f;

    static readonly string[] Tips =
    {
        "A body that glows still holds loot. Walk up to it and tap LOOT.",
        "Rich veins shimmer gold. They give better grades of ore and more Mining XP.",
        "Fishing: wait for the bobber to dip, then tap. Big fish fight on the reel: tap in the green.",
        "Schools of fish ripple on the water for a few minutes. Rarer fish bite there.",
        "Each land has its own five fish, from Common to Legendary. Fill the Collection book for rewards.",
        "Better grades of material make a Masterwork likelier. A Legendary one makes it certain.",
        "Recipes above skill 10 are learned at the station, or from scrolls the bosses drop.",
        "Swap to SKILL mode with the button by the seal to chop, mine, fish and pick without fighting.",
        "Eat before a long fight: being well fed helps you heal.",
        "The gold arrow at the top of the screen points to your quest.",
        "Talent boards open as you level: every few levels brings a new point to spend.",
        "Ancient trees glitter at night as well as by day. Their logs are worth the walk.",
    };

    // a line of the realm's lore, now and then in place of a tip
    static readonly string[] Lore =
    {
        "Long before the walls, Hollow Meadow was a ford where traders waited out the floods.",
        "The ash that gave the Hollow its name fell for nine days, the year the mountain woke.",
        "Waystones are older than any kingdom. Nobody remembers who carved the runes.",
        "Miners say the Ashen Deep is never the same twice. They also say it listens.",
        "The old guild hall burned the winter before you came. Its master still keeps her letters.",
        "Golden beasts are said to have grazed where a dragon's hoard was buried.",
        "Every town crier swears the news is true. Most of it even is.",
        "The Fossil Lands were a sea once. Some nights the sand still sounds like waves.",
        "Bards sing that the first rider tamed a wolf with nothing but a loaf of bread.",
        "Captains of the guard pass down one rule: never fight on an empty stomach.",
    };
    // the last look you had at each land, shown the next time you travel there
    static string ShotPath(string id) { return Path.Combine(Application.persistentDataPath, "landshots", id + ".jpg"); }
    static Texture2D LoadShot(string id)
    {
        try { var f = ShotPath(id); if (!File.Exists(f)) return null; var tex = new Texture2D(2, 2, TextureFormat.RGB24, false); tex.LoadImage(File.ReadAllBytes(f)); return tex; } catch { return null; }
    }
    public static IEnumerator TakeShot(AHGame g, string id)
    {
        yield return new WaitForSecondsRealtime(7f);
        if (g == null || AHGame.AreaId != id || Showing || g.player == null || g.player.dead || g.ui == null || g.ui.Modal != 0) yield break;
        string f = ShotPath(id);
        try { if (File.Exists(f) && (System.DateTime.Now - File.GetLastWriteTime(f)).TotalDays < 3) yield break; } catch { }
        var main = Camera.main; if (main == null) yield break;
        var rt = new RenderTexture(640, 360, 24);
        var go = new GameObject("LandShot"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(main); cam.cullingMask &= ~(1 << 5); cam.targetTexture = rt; cam.depth = main.depth - 1;
        cam.enabled = true;   // URP draws a camera only in its own loop
        yield return new WaitForEndOfFrame();
        cam.enabled = false;
        var tex = new Texture2D(640, 360, TextureFormat.RGB24, false);
        var prev = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); tex.Apply(); RenderTexture.active = prev;
        try { Directory.CreateDirectory(Path.GetDirectoryName(f)); File.WriteAllBytes(f, tex.EncodeToJPG(80)); } catch { }
        cam.targetTexture = null; Destroy(go); rt.Release(); Destroy(rt); Destroy(tex);
    }

    public static void Show(string toId)
    {
        if (cur != null) { cur.t = 0f; cur.readyAt = -1f; cur.fadeT = -1f; cur.SetLand(toId); return; }
        var go = new GameObject("AH_Loading"); DontDestroyOnLoad(go);
        cur = go.AddComponent<AHLoading>(); cur.Build(); cur.SetLand(toId);
    }
    // the new land is built
    public static void Ready() { if (cur != null && cur.readyAt < 0f) cur.readyAt = cur.t; }
    public static bool Showing { get { return cur != null; } }

    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1600, 900); sc.matchWidthOrHeight = 0.5f;
        group = gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = true;
        var font = AHUI.MainFont;
        var black = Rect("Black", transform, Vector2.zero, Vector2.one); black.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 1f);
        var bgRt = Rect("Map", transform, new Vector2(-0.1f, -0.1f), new Vector2(1.1f, 1.1f));
        bg = bgRt.gameObject.AddComponent<RawImage>(); bg.texture = Resources.Load<Texture2D>("AH/UI/worldmap"); bg.color = new Color(0.55f, 0.6f, 0.7f, bg.texture != null ? 1f : 0f);
        // a soft dark band behind the words
        var shade = Rect("Shade", transform, new Vector2(0f, 0f), new Vector2(1f, 0.42f)); shade.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.78f);
        title = Txt("Title", font, 64, new Vector2(0.5f, 0.27f), new Color(0.94f, 0.84f, 0.54f)); title.fontStyle = FontStyle.Bold;
        sub = Txt("Sub", font, 24, new Vector2(0.5f, 0.2f), new Color(0.5f, 0.88f, 1f));
        tip = Txt("Tip", font, 21, new Vector2(0.5f, 0.12f), new Color(1f, 1f, 1f, 0.82f));
        var bb = Rect("BarBack", transform, new Vector2(0.2f, 0.05f), new Vector2(0.8f, 0.058f)); barBack = bb.gameObject.AddComponent<Image>(); barBack.color = new Color(1f, 1f, 1f, 0.12f);
        var br = Rect("Bar", bb, Vector2.zero, new Vector2(0f, 1f)); bar = br.gameObject.AddComponent<Image>(); bar.color = new Color(0.5f, 0.88f, 1f, 0.95f);
    }
    static RectTransform Rect(string n, Transform p, Vector2 a0, Vector2 a1)
    {
        var go = new GameObject(n, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(p, false);
        rt.anchorMin = a0; rt.anchorMax = a1; rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
    }
    Text Txt(string n, Font f, int size, Vector2 at, Color c)
    {
        var rt = Rect(n, transform, at, at); rt.sizeDelta = new Vector2(1400, size + 24);
        var t = rt.gameObject.AddComponent<Text>(); t.font = f; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = c; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.raycastTarget = false;
        var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.7f); sh.effectDistance = new Vector2(2, -2);
        return t;
    }
    void SetLand(string id)
    {
        title.text = AHAuto.LandName(id);
        string lv = null;
        foreach (var o in AHDB.List("world", "REGIONS") ?? new System.Collections.Generic.List<object>()) if (AHJson.S(o, "id") == id) lv = AHJson.S(o, "lv", AHJson.S(o, "levels", null));
        sub.text = lv == "safe" ? "A safe city" : lv != null ? "Levels " + lv : "Travelling…";
        tip.text = Random.value < 0.35f ? "<i>" + Lore[Random.Range(0, Lore.Length)] + "</i>" : Tips[Random.Range(0, Tips.Length)];
        // the land's own picture when you have been there before, else the realm's map
        var shot = LoadShot(id);
        if (shot != null) { if (bg.texture != null && bg.texture.name == "landshot") Destroy(bg.texture); shot.name = "landshot"; bg.texture = shot; bg.color = new Color(0.78f, 0.8f, 0.86f, 1f); }
        else { bg.texture = Resources.Load<Texture2D>("AH/UI/worldmap"); bg.color = new Color(0.55f, 0.6f, 0.7f, bg.texture != null ? 1f : 0f); }
        minT = Random.Range(2f, 3f);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime; t += dt;
        // the map drifts and grows a little
        if (bg != null) { float k = 1f + t * 0.025f; bg.rectTransform.localScale = new Vector3(k, k, 1f); bg.rectTransform.anchoredPosition = new Vector2(-t * 6f, t * 3f); }
        float end = readyAt >= 0f ? Mathf.Clamp(Mathf.Max(minT, readyAt + 0.3f), minT, Mathf.Max(MaxT, readyAt + 0.3f)) : MaxT + 30f;
        float fill = readyAt >= 0f ? Mathf.Clamp01(t / end) : Mathf.Clamp01(t / MaxT) * 0.9f;
        bar.rectTransform.anchorMax = new Vector2(fill, 1f);
        if (fadeT < 0f && readyAt >= 0f && t >= end) fadeT = 0f;
        if (fadeT >= 0f)
        {
            fadeT += dt; group.alpha = 1f - Mathf.Clamp01(fadeT / 0.45f);
            if (fadeT >= 0.45f) { if (cur == this) cur = null; Destroy(gameObject); }
        }
    }
}
