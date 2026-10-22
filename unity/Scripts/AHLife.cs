// Ashen Hollow: a living town. The townsfolk notice you and greet you as you pass (the words change with the time of
// day, the weather, your class, how far you have come, and who they are: a guard, a trader, a child); traders and
// guards call out on their own; and two of them standing together will talk to each other while you are near enough
// to hear. Each line shows as a speech bubble over the speaker and, when its recording exists, is heard in their own
// voice (AH/Voices/<en|ar>/<voice>/<line id>; a missing recording just leaves the bubble). Lines come from
// AH/Data/life.json in English and Arabic; the language follows the player's setting (AHLoc).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AHLife : MonoBehaviour
{
    static AHLife inst;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (inst != null) return; var go = new GameObject("Life"); DontDestroyOnLoad(go); inst = go.AddComponent<AHLife>(); }

    object data;
    readonly Dictionary<AHNpc, float> greeted = new Dictionary<AHNpc, float>();
    readonly Dictionary<AHNpc, float> busy = new Dictionary<AHNpc, float>();   // speaking or in a talk until
    float tick, nextGreet, nextCall = 8f, nextTalk = 6f;
    bool talking;
    string area;

    // ---------- who is who ----------
    public static bool Child(AHNpc n) { return n.height < 1.4f || n.npcName.Contains("Little"); }
    static bool Any(string s, params string[] w) { foreach (var x in w) if (s.IndexOf(x, System.StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; }
    public static string Role(AHNpc n)
    {
        if (Child(n)) return "child";
        if (n.isCaptain || Any(n.npcName, "Guard", "Sentry", "Watch", "Warden", "Captain", "Soldier")) return "guard";
        if (n.market || n.auction || Any(n.npcName, "Merchant", "Trader", "Vendor", "Baker", "crier", "Peddler", "Fishmonger", "Grocer")) return "merchant";
        return "any";
    }
    // the voice they always speak with: men m1..m4, women f1..f4, children c1/c2 (fixed per name)
    public static string VoiceOf(AHNpc n)
    {
        int h = 0; foreach (var c in n.npcName) h = h * 31 + c; h = Mathf.Abs(h);
        if (Child(n)) return "c" + (1 + h % 2);
        return (AHNpc.IsWoman(n.npcName) ? "f" : "m") + (1 + h % 4);
    }

    void Update()
    {
        var g = AHGame.I; if (g == null || g.player == null || g.cam == null) { HideAll(); return; }
        if (area != AHGame.AreaId) { area = AHGame.AreaId; greeted.Clear(); busy.Clear(); talking = false; StopAllCoroutines(); HideAll(); }
        if (data == null) { var ta = Resources.Load<TextAsset>("AH/Data/life.json"); if (ta == null) ta = Resources.Load<TextAsset>("AH/Data/life"); if (ta == null) return; data = AHJson.Parse(ta.text); }
        tick -= Time.deltaTime; if (tick > 0f) return; tick = 0.4f;
        float now = Time.time; Vector3 pp = g.player.transform.position;

        // greetings: someone you walk past, now and then (not the same person again for a while)
        if (now >= nextGreet)
            foreach (var n in AHNpc.All)
            {
                if (n == null || !n.isActiveAndEnabled || Busy(n)) continue;
                float d = n.DistTo(pp); if (d > 5.5f || d < 0.6f) continue;
                float last; if (greeted.TryGetValue(n, out last) && now - last < 120f) continue;
                greeted[n] = now;
                if (Random.value > 0.9f) continue;   // now and then someone is too busy to look up
                var line = PickGreeting(g, n); if (line == null) continue;
                Say(n, line, 4.2f); nextGreet = now + 4f;
                break;
            }

        // calls: a trader crying their wares, a guard on watch, children at play
        if (now >= nextCall)
        {
            nextCall = now + Random.Range(14f, 26f);
            var cands = new List<AHNpc>();
            foreach (var n in AHNpc.All) if (n != null && n.isActiveAndEnabled && !Busy(n) && n.DistTo(pp) < 24f && n.DistTo(pp) > 4f && CallList(n) != null) cands.Add(n);
            if (cands.Count > 0) { var n = cands[Random.Range(0, cands.Count)]; var l = CallList(n); Say(n, l[Random.Range(0, l.Count)], 3.2f); }
        }

        // talks: two people standing together within earshot
        if (!talking && now >= nextTalk)
        {
            nextTalk = now + Random.Range(10f, 18f);
            AHNpc a = null, b = null; float best = float.MaxValue;
            foreach (var x in AHNpc.All)
            {
                if (x == null || !x.isActiveAndEnabled || Busy(x) || x.isCaptain) continue;
                float dx = x.DistTo(pp); if (dx > 18f) continue;
                foreach (var y in AHNpc.All)
                {
                    if (y == null || y == x || !y.isActiveAndEnabled || Busy(y) || y.isCaptain) continue;
                    float dy = y.DistTo(pp), dd = x.DistTo(y.transform.position); if (dy > 18f || dd > 4.5f || dd < 0.5f) continue;
                    float s = dx + dy + Random.value * 4f; if (s < best) { best = s; a = x; b = y; }
                }
            }
            if (a != null) { var t = PickTalk(a); if (t != null) StartCoroutine(Talk(a, b, t)); }
        }
    }

    // test tools: the nearest of the townsfolk greets you now; the nearest two talk now
    public static void TestGreet()
    {
        var g = AHGame.I; if (inst == null || g == null || g.player == null || inst.data == null) return;
        AHNpc best = null; float bd = float.MaxValue; foreach (var n in AHNpc.All) { if (n == null) continue; float d = n.DistTo(g.player.transform.position); if (d < bd) { bd = d; best = n; } }
        if (best == null) return; var line = inst.PickGreeting(g, best); if (line != null) inst.Say(best, line, 5f);
        inst.nextTalk = 0f;
    }
    bool Busy(AHNpc n) { float t; return busy.TryGetValue(n, out t) && t > Time.time; }
    object Get(string k) { return AHJson.O(data, k); }
    List<object> CallList(AHNpc n) { var l = AHJson.A(Get("call"), Role(n)); return l != null && l.Count > 0 ? l : null; }

    object PickGreeting(AHGame g, AHNpc n)
    {
        var G = Get("greet"); var p = g.player; string role = Role(n);
        var keys = new List<string>();
        if (role != "any" && Random.value < 0.45f) keys.Add(role);
        if (AHWeather.Raining && Random.value < 0.5f) keys.Add("rain");
        if (AHWeather.Now == AHWeather.Kind.Snow && Random.value < 0.5f) keys.Add("snow");
        if (AHWeather.Now == AHWeather.Kind.Fog && Random.value < 0.4f) keys.Add("fog");
        if (p.cls != null && Random.value < 0.35f) keys.Add(p.cls.id);
        if (p.level >= 40 && Random.value < 0.2f) keys.Add("strong");
        if (Random.value < 0.4f) keys.Add(g.IsNight ? "night" : g.DayLight < 0.5f ? (Morning(g) ? "morning" : "evening") : "morning");
        keys.Add("any");
        foreach (var k in keys)
        {
            if (k == "morning" && (g.IsNight || !Morning(g))) continue;
            var l = AHJson.A(G, k); if (l != null && l.Count > 0) return l[Random.Range(0, l.Count)];
        }
        return null;
    }
    // the sun rising (morning) or setting (evening): light that is growing
    float lastLight = -1f; bool rising = true;
    bool Morning(AHGame g) { if (lastLight >= 0f && Mathf.Abs(g.DayLight - lastLight) > 0.001f) rising = g.DayLight > lastLight; lastLight = g.DayLight; return rising; }

    object PickTalk(AHNpc a)
    {
        var all = AHJson.A(data, "talk"); if (all == null) return null;
        string role = Role(a); var fit = new List<object>();
        foreach (var t in all) { string w = AHJson.S(t, "who", "any"); if (w == "any" || w == role) fit.Add(t); }
        return fit.Count > 0 ? fit[Random.Range(0, fit.Count)] : null;
    }

    IEnumerator Talk(AHNpc a, AHNpc b, object t)
    {
        talking = true;
        var lines = AHJson.A(t, "lines"); float total = lines.Count * 3.4f + 1f;
        busy[a] = busy[b] = Time.time + total;
        a.ChatWith(b, total); b.ChatWith(a, total);
        for (int i = 0; i < lines.Count; i++)
        {
            var who = i % 2 == 0 ? a : b; if (who == null) break;
            Say(who, lines[i], 3.1f);
            yield return new WaitForSeconds(3.4f);
        }
        talking = false;
    }

    // ---------- speaking: a bubble and, when recorded, the voice ----------
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    // a line written in code (AHTownLife: the crier's news, the stall keepers), in English and Arabic
    public static void Speak(AHNpc n, string en, string ar, float time)
    {
        if (inst == null || n == null) return;
        inst.Say(n, new Dictionary<string, object> { { "en", en }, { "ar", ar ?? en } }, time);
    }
    void Say(AHNpc n, object line, float time)
    {
        busy[n] = Mathf.Max(busy.ContainsKey(n) ? busy[n] : 0f, Time.time + time);
        string text = AHLoc.Arabic ? AHJson.S(line, "ar") ?? AHJson.S(line, "en") : AHJson.S(line, "en");
        Bubble(n, text, time);
        string id = AHJson.S(line, "id"); if (id == null || AHSound.Muted) return;
        string path = "AH/Voices/" + AHLoc.Lang + "/" + VoiceOf(n) + "/" + id;
        AudioClip c; if (!clips.TryGetValue(path, out c)) { c = Resources.Load<AudioClip>(path); clips[path] = c; }
        if (c == null) return;
        var src = n.GetComponent<AudioSource>(); if (src == null) { src = n.gameObject.AddComponent<AudioSource>(); src.spatialBlend = 0.9f; src.minDistance = 3f; src.maxDistance = 26f; src.rolloffMode = AudioRolloffMode.Linear; src.playOnAwake = false; }
        src.volume = AHSound.SfxVol; src.clip = c; src.Play();
    }

    // ---------- bubbles ----------
    class B { public RectTransform rt; public Text txt; public Image bg; public AHNpc who; public float until; }
    readonly List<B> bubbles = new List<B>();
    Canvas canvas; Font font;
    void Bubble(AHNpc n, string text, float time)
    {
        if (canvas == null)
        {
            var go = new GameObject("Bubbles"); go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;   // under the game's own panels
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1100, 620); sc.matchWidthOrHeight = 1f;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        B b = null; foreach (var x in bubbles) if (x.who == n || x.until < Time.time) { b = x; break; }
        if (b == null)
        {
            b = new B(); var go = new GameObject("Bubble", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
            b.rt = (RectTransform)go.transform; b.rt.pivot = new Vector2(0.5f, 0f);
            b.bg = go.AddComponent<Image>(); b.bg.sprite = Round(); b.bg.type = Image.Type.Sliced; b.bg.color = new Color(0.08f, 0.07f, 0.06f, 0.82f); b.bg.raycastTarget = false;
            var tg = new GameObject("Text", typeof(RectTransform)); tg.transform.SetParent(go.transform, false);
            b.txt = tg.AddComponent<Text>(); b.txt.font = font; b.txt.fontSize = 15; b.txt.color = new Color(1f, 0.95f, 0.85f); b.txt.alignment = TextAnchor.MiddleCenter; b.txt.raycastTarget = false;
            var trt = (RectTransform)tg.transform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(9, 5); trt.offsetMax = new Vector2(-9, -5);
            bubbles.Add(b);
        }
        b.who = n; b.until = Time.time + time;
        string shown = AHLoc.Lines(text, 30);
        b.txt.text = shown;
        int lines = 1, longest = 0, cur = 0; foreach (var c in shown) { if (c == '\n') { lines++; cur = 0; } else { cur++; if (cur > longest) longest = cur; } }
        b.rt.sizeDelta = new Vector2(Mathf.Clamp(longest * 7.6f + 22f, 60f, 260f), lines * 18f + 12f);
        b.rt.gameObject.SetActive(true);
    }

    // a soft rounded box for the bubbles (white, tinted by the image colour; nine-sliced so it keeps its corners)
    static Sprite round;
    static Sprite Round()
    {
        if (round != null) return round;
        const int N = 32, R = 10; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float dx = Mathf.Max(0f, Mathf.Max(R - x - 0.5f, x + 0.5f - (N - R))), dy = Mathf.Max(0f, Mathf.Max(R - y - 0.5f, y + 0.5f - (N - R)));
            float d = Mathf.Sqrt(dx * dx + dy * dy); t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(R - d)));
        }
        t.Apply();
        round = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        return round;
    }

    void LateUpdate()
    {
        var g = AHGame.I; if (g == null || g.cam == null || canvas == null) return;
        float k = canvas.transform.localScale.x <= 0f ? 1f : 1f / canvas.transform.localScale.x;
        foreach (var b in bubbles)
        {
            if (b.who == null || b.until < Time.time) { if (b.rt.gameObject.activeSelf) b.rt.gameObject.SetActive(false); continue; }
            Vector3 w = b.who.transform.position + Vector3.up * (b.who.height + 1.05f);   // above their name
            Vector3 s = g.cam.WorldToScreenPoint(w);
            float dist = Vector3.Distance(g.cam.transform.position, w);
            bool show = s.z > 0f && dist < 30f;
            if (b.rt.gameObject.activeSelf != show) b.rt.gameObject.SetActive(show);
            if (!show) continue;
            b.rt.anchoredPosition = Vector2.zero; b.rt.anchorMin = b.rt.anchorMax = Vector2.zero;
            // kept clear of the hero card and quest card (top left) and the minimap and its buttons (top right):
            // a bubble that would sit on them drops just below
            Vector2 at = new Vector2(s.x, s.y) * k, sz = b.rt.sizeDelta;
            float W = Screen.width * k, H = Screen.height * k;
            at.x = Mathf.Clamp(at.x, sz.x * 0.5f + 6f, W - sz.x * 0.5f - 6f);
            float top = at.y + sz.y, l = at.x - sz.x * 0.5f, r = at.x + sz.x * 0.5f;
            if (top > H - 250f && (l < 420f || r > W - 340f)) at.y = Mathf.Min(at.y, H - 250f - sz.y);
            b.rt.anchoredPosition = at;
            float fade = Mathf.Clamp01((b.until - Time.time) * 3f);
            b.bg.color = new Color(0.08f, 0.07f, 0.06f, 0.78f * fade); var tc = b.txt.color; tc.a = fade; b.txt.color = tc;
        }
    }
    void HideAll() { foreach (var b in bubbles) if (b.rt != null) b.rt.gameObject.SetActive(false); }
}
