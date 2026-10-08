// Ashen Hollow: a boss's entrance. The first time a boss of the Deep, a dungeon or the raid turns on you, the world
// slows almost to a stop, black cinema bars slide in, the camera swings round to the boss's face and its name and
// title rise up between the bars; two and a half seconds later the fight is on.
using UnityEngine;
using UnityEngine.UI;

public class AHBossIntro : MonoBehaviour
{
    static AHBossIntro I;
    AHMob m; float t, prevScale = 1f; RectTransform top, bot; Text title, sub; CanvasGroup words;
    Vector3 camFrom; Quaternion camFromR; bool haveFrom;
    const float Len = 2.6f;
    public static bool Active { get { return I != null; } }

    public static void Play(AHGame g, AHMob boss, string name, string line)
    {
        if (I != null || boss == null || g == null || g.cam == null || AHCine.Active)
        { Debug.Log("Ashen Hollow: boss intro skipped (" + (I != null ? "already playing" : boss == null ? "no boss" : g == null || g.cam == null ? "no camera" : "story scene playing") + ")"); return; }
        Debug.Log("Ashen Hollow: boss intro for " + name);
        var go = new GameObject("Boss intro"); I = go.AddComponent<AHBossIntro>(); I.m = boss; I.Build(name, line);
        I.prevScale = Time.timeScale > 0.2f ? Time.timeScale : 1f; Time.timeScale = 0.1f;
        AHSound.Play("level"); AHJuice.Shake(0.15f, 0.4f);
    }

    void Build(string name, string line)
    {
        var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 400;
        var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1600, 900); sc.matchWidthOrHeight = 0.5f;
        top = Bar(new Vector2(0f, 1f)); bot = Bar(new Vector2(0f, 0f));
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var wg = new GameObject("Words", typeof(RectTransform)); wg.transform.SetParent(transform, false); words = wg.AddComponent<CanvasGroup>(); words.alpha = 0f;
        var wr = (RectTransform)wg.transform; wr.anchorMin = new Vector2(0f, 0f); wr.anchorMax = new Vector2(1f, 0f); wr.pivot = new Vector2(0.5f, 0f); wr.anchoredPosition = new Vector2(0, 20); wr.sizeDelta = new Vector2(0, 110);
        title = Txt(wr, name.ToUpperInvariant(), font, 46, new Color(1f, 0.82f, 0.45f), new Vector2(0, 56)); title.fontStyle = FontStyle.Bold;
        sub = Txt(wr, line ?? "", font, 22, new Color(1f, 1f, 1f, 0.85f), new Vector2(0, 14)); sub.fontStyle = FontStyle.Italic;
    }
    RectTransform Bar(Vector2 anchorY)
    {
        var o = new GameObject("Bar", typeof(RectTransform), typeof(Image)); o.transform.SetParent(transform, false);
        var r = (RectTransform)o.transform; r.anchorMin = new Vector2(0f, anchorY.y); r.anchorMax = new Vector2(1f, anchorY.y); r.pivot = new Vector2(0.5f, anchorY.y); r.sizeDelta = new Vector2(0, 0);
        o.GetComponent<Image>().color = Color.black; return r;
    }
    Text Txt(RectTransform p, string s, Font f, int size, Color c, Vector2 pos)
    {
        var o = new GameObject("T", typeof(RectTransform), typeof(Text)); o.transform.SetParent(p, false);
        var r = (RectTransform)o.transform; r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(1f, 0f); r.pivot = new Vector2(0.5f, 0f); r.anchoredPosition = pos; r.sizeDelta = new Vector2(0, size + 12);
        var tx = o.GetComponent<Text>(); tx.font = f; tx.fontSize = size; tx.color = c; tx.alignment = TextAnchor.MiddleCenter; tx.text = s;
        var ol = o.AddComponent<Outline>(); ol.effectColor = new Color(0f, 0f, 0f, 0.9f); ol.effectDistance = new Vector2(2f, -2f);
        return tx;
    }

    void Update()
    {
        t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);   // a long frame (a loading hitch, the editor's menu) must not skip the whole entrance
        float inK = Mathf.SmoothStep(0f, 1f, t / 0.35f), outK = Mathf.SmoothStep(0f, 1f, (Len - t) / 0.35f), k = Mathf.Min(inK, outK);
        float bar = 120f * k; top.sizeDelta = new Vector2(0, bar); bot.sizeDelta = new Vector2(0, bar);
        words.alpha = Mathf.Clamp01((t - 0.35f) / 0.4f) * outK;
        if (t >= Len || m == null || m.dead) End();
    }
    void End()
    {
        if (I == this) I = null;
        if (Time.timeScale < 0.2f) Time.timeScale = prevScale;
        Destroy(gameObject);
    }

    // the camera: swings from where it was to the boss's face, then pushes in slowly
    public static bool Drive(Camera cam)
    {
        if (I == null || I.m == null) return false;
        var b = I.m.transform; float h = Mathf.Max(1.6f, I.m.height);
        if (!I.haveFrom) { I.camFrom = cam.transform.position; I.camFromR = cam.transform.rotation; I.haveFrom = true; }
        Vector3 face = b.position + Vector3.up * h * 0.75f;
        Vector3 fw = b.forward; fw.y = 0f; if (fw.sqrMagnitude < 0.01f) fw = Vector3.forward; fw.Normalize();
        float push = Mathf.Lerp(1f, 0.85f, I.t / Len);
        Vector3 want = face + fw * h * 1.6f * push + Vector3.Cross(Vector3.up, fw) * h * 0.35f - Vector3.up * h * 0.15f;
        float s = Mathf.SmoothStep(0f, 1f, Mathf.Min(I.t / 0.45f, (Len - I.t) / 0.45f));
        cam.transform.position = Vector3.Lerp(I.camFrom, want, s);
        cam.transform.rotation = Quaternion.Slerp(I.camFromR, Quaternion.LookRotation(face - want), s);
        return true;
    }
}
