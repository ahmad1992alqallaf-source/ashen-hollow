// Ashen Hollow: the minimap and the quest guide.
//  - The minimap: a round frosted-glass window on the left under the quest panel, turning with the camera (what is
//    ahead of you is up). The land is drawn from one picture taken from high above when you arrive. On it: you (a
//    white arrow), the quest's goal (a gold star; at the rim when it is further than the map shows), people (white),
//    quest givers (gold), beasts close by (red), loot (its quality colour), ore (copper-brown, a rich vein gold),
//    herbs (green), fishing spots (blue, a school bright), and the roads out (green arrows at the edge).
//    Tap it to open the full map.
//  - The guide arrow: at the top of the screen, a gold arrow points the way to the quest's goal, with how far it is.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RectTransform mmRoot, mmSpin, mmDots, mmMe, guideRt, guideArrow; RawImage mmMap; Text guideText;
    Texture2D mmTex; Vector3 mmC; float mmS, mmTick;
    const float MmD = 164f, MmRange = 45f;   // diameter on screen, metres from the centre to the rim
    static readonly Vector2 MmPos = new Vector2(98f, -318f);
    readonly System.Collections.Generic.List<RectTransform> mmPool = new System.Collections.Generic.List<RectTransform>();
    int mmUsed; bool hasQ, qRoad; Vector3 qAt; string qLabel;
    static Sprite diamond, star, arrowSp;

    void BuildMinimap()
    {
        mmRoot = Img("Minimap", transform, circle, new Vector2(0f, 1f), MmPos, new Vector2(MmD + 8f, MmD + 8f), new Color(0.75f, 0.85f, 0.95f, 0.55f));   // the silver rim
        var face = Img("Face", mmRoot, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MmD, MmD), new Color(0.06f, 0.1f, 0.16f, 0.92f));
        var mask = face.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
        mmSpin = Box("Spin", face, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MmD * 1.6f, MmD * 1.6f));
        var mapGo = new GameObject("Map", typeof(RectTransform)); var mrt = (RectTransform)mapGo.transform; mrt.SetParent(mmSpin, false);
        mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = mrt.offsetMax = Vector2.zero;
        mmMap = mapGo.AddComponent<RawImage>(); mmMap.raycastTarget = false; mmMap.color = new Color(1f, 1f, 1f, 0f);
        mmDots = Box("Dots", mmSpin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        // a soft sky-blue inner glow at the rim, and the north tick
        Img("Glass", face, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MmD, MmD), new Color(0.5f, 0.88f, 1f, 0.35f));
        if (arrowSp == null) { diamond = MakeShape(0); star = MakeShape(1); arrowSp = MakeShape(2); }
        mmMe = Img("Me", face, arrowSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18), Color.white);
        taps.Add(new TapBtn { rt = mmRoot, act = () => OpenMap() });
        // the guide arrow, top middle under the story line
        // the guide: glowing chevrons on the ground ahead of the hero (placed every frame), the goal and distance beside
        guideRt = Box("Guide", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
        var cgl = Img("Glow", guideRt, glowSprite(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 130), new Color(0.5f, 0.88f, 1f, 0.35f));
        guideArrow = Img("Arrow", guideRt, CGIcon("chevrons") ?? arrowSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 74), new Color(0.82f, 0.96f, 1f, 1f));
        guideArrow.GetComponent<Image>().preserveAspect = true; cgl.SetParent(guideArrow, true);
        guideText = Label(guideRt, "T", "", 15, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(260, 22), new Color(0.84f, 0.96f, 1f));
        guideText.fontStyle = FontStyle.Bold; guideText.rectTransform.anchorMin = guideText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); guideText.rectTransform.pivot = new Vector2(0f, 0.5f);
        var sh = guideText.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
        guideRt.gameObject.SetActive(false);
        StartCoroutine(SnapMinimap());
    }

    // one picture of the land from high above (like the big map, but kept for the minimap)
    IEnumerator SnapMinimap()
    {
        yield return new WaitForSeconds(1.2f);
        if (g == null || g.data == null || g.data.bounds == null) yield break;
        Vector3 o = g.W(g.data.bounds.x0, g.data.bounds.z0), e = g.W(g.data.bounds.x1, g.data.bounds.z1);
        mmC = (o + e) * 0.5f; mmS = Mathf.Max(Mathf.Abs(e.x - o.x), Mathf.Abs(e.z - o.z)) * 1.02f;
        int res = Mathf.Clamp(Mathf.RoundToInt(mmS * 2.5f), 256, 1024);
        var rt = new RenderTexture(res, res, 24); var go = new GameObject("MinimapCamera"); var cam = go.AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.32f, 0.4f);
        cam.targetTexture = rt; cam.enabled = false; cam.nearClipPlane = 1f; cam.farClipPlane = 1200f; cam.cullingMask = ~(1 << 5);
        cam.transform.position = new Vector3(mmC.x, 400f, mmC.z); cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        cam.aspect = 1f; cam.orthographicSize = mmS * 0.5f;
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        cam.enabled = true;   // URP draws a camera only in its own loop (Camera.Render gives nothing)
        yield return new WaitForEndOfFrame();
        cam.enabled = false; RenderSettings.fog = fog;
        mmTex = new Texture2D(res, res, TextureFormat.RGB24, false); mmTex.wrapMode = TextureWrapMode.Clamp;
        var prev = RenderTexture.active; RenderTexture.active = rt; mmTex.ReadPixels(new Rect(0, 0, res, res), 0, 0); RenderTexture.active = prev;
        // lift the dark (a night picture still reads), a little cooler, like glass
        var px = mmTex.GetPixels32();
        for (int i = 0; i < px.Length; i++)
        {
            Color c = px[i]; float l = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f, k = l < 0.35f ? 0.35f / Mathf.Max(0.04f, l) : 1f; k = Mathf.Min(k, 2.6f);
            c = new Color(Mathf.Clamp01(c.r * k * 0.95f), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k * 1.08f));
            px[i] = c;
        }
        mmTex.SetPixels32(px); mmTex.Apply();
        cam.targetTexture = null; Destroy(go); rt.Release(); Destroy(rt);
        mmMap.texture = mmTex; mmMap.color = new Color(0.9f, 0.95f, 1f, 0.9f);
    }

    RectTransform MmDot(Sprite sp, float size, Color c)
    {
        RectTransform d;
        if (mmUsed < mmPool.Count) d = mmPool[mmUsed];
        else { d = Img("Dot", mmDots, sp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), c); mmPool.Add(d); }
        mmUsed++;
        var im = d.GetComponent<Image>(); im.sprite = sp == white ? null : sp; im.color = c; d.sizeDelta = new Vector2(size, size);
        d.gameObject.SetActive(true);
        return d;
    }

    void MinimapTick(float dt)
    {
        if (mmRoot == null) return;
        var p = g.player;
        bool on = p != null && Modal == 0 && !PhotoOn && !AHInterior.Inside;
        if (mmRoot.gameObject.activeSelf != on) mmRoot.gameObject.SetActive(on);
        if (!on) { if (guideRt != null) guideRt.gameObject.SetActive(false); return; }
        var cam = Camera.main; float yaw = cam != null ? cam.transform.eulerAngles.y : g.camYaw;
        Vector3 pp = p.transform.position;
        float ppm = (MmD * 0.5f) / MmRange;   // pixels per metre
        // the land picture under you
        if (mmTex != null)
        {
            float span = MmRange * 1.6f;
            float u = (pp.x - (mmC.x - mmS * 0.5f)) / mmS, v = (pp.z - (mmC.z - mmS * 0.5f)) / mmS, w = span * 2f / mmS;
            mmMap.uvRect = new Rect(u - w * 0.5f, v - w * 0.5f, w, w);
        }
        mmSpin.localEulerAngles = new Vector3(0f, 0f, yaw);
        mmMe.localEulerAngles = new Vector3(0f, 0f, yaw - p.transform.eulerAngles.y);
        // the marks (every few frames)
        mmTick -= dt;
        if (mmTick <= 0f)
        {
            mmTick = 0.25f; mmUsed = 0;
            hasQ = AHAuto.QuestTarget(g, p, out qAt, out qLabel, out qRoad);
            float r2 = MmRange * MmRange * 1.1f;
            foreach (var s in AHGather.Spots)
            {
                if (s.wander) continue;   // gone, coming back somewhere else
                Vector3 d = s.pos - pp; d.y = 0f; if (d.sqrMagnitude > r2) continue;
                Color c; Sprite sp = diamond; float sz = 8f;
                switch (s.kind)
                {
                    case "mine": c = s.rich ? new Color(1f, 0.84f, 0.3f) : new Color(0.85f, 0.55f, 0.35f); sz = s.rich ? 11f : 8f; if (s.until > Time.time) c.a = 0.35f; break;
                    case "herb": c = new Color(0.5f, 0.95f, 0.45f); if (s.until > Time.time) c.a = 0.35f; break;
                    case "fish": c = AHFishing.School(s) ? new Color(0.6f, 1f, 1f) : new Color(0.4f, 0.75f, 1f); sz = AHFishing.School(s) ? 11f : 8f; break;
                    case "chop": if (!s.rich) continue; c = new Color(1f, 0.84f, 0.3f); sz = 9f; break;
                    case "gate": c = new Color(0.55f, 1f, 0.6f); sp = arrowSp; sz = 12f; break;
                    default: c = new Color(0.85f, 0.85f, 0.95f, 0.8f); sp = circle; sz = 6f; break;   // a station
                }
                var dot = MmDot(sp, sz, c); dot.anchoredPosition = new Vector2(d.x, d.z) * ppm; dot.localEulerAngles = new Vector3(0, 0, -yaw);
            }
            foreach (var n in AHNpc.All)
            {
                if (n == null) continue; Vector3 d = n.transform.position - pp; d.y = 0f; if (d.sqrMagnitude > r2) continue;
                var dot = MmDot(circle, 7f, new Color(1f, 1f, 1f, 0.9f)); dot.anchoredPosition = new Vector2(d.x, d.z) * ppm;
            }
            foreach (var m in g.mobs)
            {
                if (m == null) continue; Vector3 d = m.transform.position - pp; d.y = 0f; if (d.sqrMagnitude > r2) continue;
                if (m.HasLoot) { var ld = MmDot(star, 11f, AHLootGlow.Best(m)); ld.anchoredPosition = new Vector2(d.x, d.z) * ppm; ld.localEulerAngles = new Vector3(0, 0, -yaw); continue; }
                if (m.dead) continue;
                var dot = MmDot(circle, m.type.elite || m.type.rare ? 8f : 6f, m.Chasing ? new Color(1f, 0.25f, 0.2f) : new Color(0.95f, 0.45f, 0.35f, 0.85f)); dot.anchoredPosition = new Vector2(d.x, d.z) * ppm;
            }
            // the roads out, at the rim when far
            if (g.data.exits != null) foreach (var e in g.data.exits)
                {
                    if (e == null) continue;
                    Vector3 at = e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) * 0.5f, (e.z0 + e.z1) * 0.5f); Vector3 d = at - pp; d.y = 0f;
                    Vector2 v2 = new Vector2(d.x, d.z) * ppm; if (v2.magnitude > MmD * 0.47f) v2 = v2.normalized * MmD * 0.47f;
                    var dot = MmDot(arrowSp, 11f, new Color(0.55f, 1f, 0.6f, 0.9f)); dot.anchoredPosition = v2; dot.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(-d.x, d.z) * Mathf.Rad2Deg);
                }
            // the quest goal, on top of everything; pinned to the rim when far
            if (hasQ)
            {
                Vector3 d = qAt - pp; d.y = 0f; Vector2 v2 = new Vector2(d.x, d.z) * ppm; bool far = v2.magnitude > MmD * 0.45f;
                if (far) v2 = v2.normalized * MmD * 0.45f;
                var dot = MmDot(star, far ? 14f : 16f, new Color(1f, 0.85f, 0.35f)); dot.anchoredPosition = v2; dot.localEulerAngles = new Vector3(0, 0, -yaw);
            }
            for (int i = mmUsed; i < mmPool.Count; i++) if (mmPool[i].gameObject.activeSelf) mmPool[i].gameObject.SetActive(false);
        }
        // the guide: chevrons on the ground a few steps ahead of the hero, turned toward the goal
        Vector3 gd = qAt - pp; gd.y = 0f; float gdist = gd.magnitude;
        bool gOn = hasQ && !AHFishing.Active && gdist > 4f && cam != null;
        Vector3 s0 = Vector3.zero, s1 = Vector3.zero;
        if (gOn)
        {
            Vector3 dir = gd / gdist, a0 = pp + dir * 1.6f + Vector3.up * 0.15f, a1 = pp + dir * 3.2f + Vector3.up * 0.15f;
            s0 = cam.WorldToScreenPoint(a0); s1 = cam.WorldToScreenPoint(a1);
            gOn = s0.z > 0f && s1.z > 0f;
        }
        if (guideRt.gameObject.activeSelf != gOn) guideRt.gameObject.SetActive(gOn);
        if (gOn)
        {
            var par = (RectTransform)guideRt.parent; Vector2 l0, l1;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(par, (s0 + s1) * 0.5f, null, out l0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(par, s1, null, out l1);
            Vector2 off = par.rect.size * (par.pivot - new Vector2(0.5f, 0.5f));
            guideRt.anchoredPosition = l0 + off;
            Vector2 sd = (Vector2)(s1 - s0);
            guideArrow.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(-sd.x, sd.y) * Mathf.Rad2Deg);
            guideArrow.GetComponent<Image>().color = new Color(0.82f, 0.96f, 1f, 0.75f + 0.25f * Mathf.PingPong(Time.unscaledTime * 1.5f, 1f));
            guideText.rectTransform.anchoredPosition = new Vector2(28f, -30f);
            guideText.text = (qLabel ?? "Quest") + " · " + Mathf.RoundToInt(gdist) + " m";
        }
    }

    static Sprite glowSp; static Sprite glowSprite()
    {
        if (glowSp != null) return glowSp;
        const int N = 64; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); var px = new Color32[N * N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float d = new Vector2(x + 0.5f - N / 2f, y + 0.5f - N / 2f).magnitude / (N / 2f); px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Pow(Mathf.Clamp01(1f - d), 2f) * 255)); }
        tex.SetPixels32(px); tex.Apply(); glowSp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f); return glowSp;
    }

    // 0: a diamond, 1: a five-point star, 2: an arrow pointing up
    static Sprite MakeShape(int kind)
    {
        const int N = 64; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float fx = (x + 0.5f) / N * 2f - 1f, fy = (y + 0.5f) / N * 2f - 1f, a = 0f;
                if (kind == 0) a = Mathf.Clamp01((0.9f - (Mathf.Abs(fx) + Mathf.Abs(fy))) * 20f);
                else if (kind == 1)
                {
                    float r = Mathf.Sqrt(fx * fx + fy * fy), th = Mathf.Atan2(fy, fx) + Mathf.PI / 2f;
                    float k = Mathf.Cos(Mathf.Repeat(th, 2f * Mathf.PI / 5f) - Mathf.PI / 5f);
                    float edge = Mathf.Lerp(0.42f, 0.95f, Mathf.Pow(Mathf.Clamp01((k - 0.809f) / 0.191f), 1.2f));
                    a = Mathf.Clamp01((edge - r) * 20f);
                }
                else
                {
                    // a chevron arrow: a triangle tip with a notch
                    float tip = 0.9f, baseY = -0.75f;
                    float t = (tip - fy) / (tip - baseY); float half = t * 0.78f;
                    bool inTri = fy <= tip && fy >= baseY && Mathf.Abs(fx) <= half;
                    float notch = baseY + 0.55f * (1f - Mathf.Abs(fx) / 0.78f);
                    a = inTri && fy > notch ? 1f : 0f;
                }
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }
}
