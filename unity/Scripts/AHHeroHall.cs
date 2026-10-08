// Ashen Hollow: the hero hall, the "Choose your hero" screen. Instead of a picture on a card over the dimmed game,
// the whole screen becomes a small night scene: a clearing in a pine wood under the stars and a full moon, a campfire
// crackling with embers rising, and your hero standing by it, lit warm by the fire on one side and by a rim of their
// class's colour from behind. The camera drifts slowly. The game's own buttons and bars are put away while it shows.
using System.Collections.Generic;
using UnityEngine;

public class AHHeroHall : MonoBehaviour
{
    public const int Layer = 28;
    static AHHeroHall I;
    Camera cam; Transform root, heroHold, fireT; Light fireLight, rim; Transform[] flames; GameObject hero; string heroSig;
    float t, sNear; Camera worldCam; readonly List<GameObject> hiddenUi = new List<GameObject>();
    static readonly Vector3 At = new Vector3(0f, -3000f, 0f);

    // ---- open, show a hero, close ----
    public static void Open(AHGame g, Transform keepUi)
    {
        if (I == null) { var go = new GameObject("Hero Hall"); I = go.AddComponent<AHHeroHall>(); I.Build(); }
        I.gameObject.SetActive(true);
        I.worldCam = g.cam; if (g.cam != null) g.cam.enabled = false;   // the land is not drawn behind the hall
        // the game's buttons and bars go away; only the select screen stays
        I.hiddenUi.Clear();
        if (keepUi != null && keepUi.parent != null)
            foreach (Transform c in keepUi.parent) if (c != keepUi && c.gameObject.activeSelf) { c.gameObject.SetActive(false); I.hiddenUi.Add(c.gameObject); }
    }
    public static void Close()
    {
        if (I == null) return;
        foreach (var o in I.hiddenUi) if (o != null) o.SetActive(true); I.hiddenUi.Clear();
        if (I.worldCam != null) I.worldCam.enabled = true;
        if (I.hero != null) Destroy(I.hero); I.hero = null; I.heroSig = null; Spin = 0f; Near = 0f; Shift = 0f;
        I.gameObject.SetActive(false);
    }
    public static bool Showing { get { return I != null && I.gameObject.activeSelf; } }
    // making a hero here: drag to turn them (degrees), and how close the camera has come to the face (0 far .. 1 near)
    public static float Spin, Near, Shift;   // Shift 1: the creator's wider panel, so the hero stands further left

    // the hero to stand by the fire (the one picked in the list)
    public static void Show(ClassDef cls, AHLook look, System.Func<string, string> shown, AHPlayer p)
    {
        if (I == null || cls == null) return; look = look ?? new AHLook();
        string sig = cls.id + "|" + JsonUtility.ToJson(look); foreach (var sl in AHItems.GearSlots) sig += "|" + (shown(sl) ?? "");
        if (p != null) sig += "|" + AHCostumes.Worn(p);
        if (I.hero != null && I.heroSig == sig) return;
        if (I.hero != null) Destroy(I.hero);
        var holder = new GameObject("Hero").transform; holder.SetParent(I.heroHold, false);
        AHAnim a; GameObject rig = null;
        try { rig = AHPeople.BuildHero(holder, cls, look, AHGame.I, out a); } catch { a = null; }
        if (rig == null) { Destroy(holder.gameObject); return; }
        string cos = p != null ? AHCostumes.Worn(p) : null;
        if (cos != null && AHCostumes.Apply(rig, cos, look, AHFashion.DyeOf(p, cos))) AHWardrobe.DressWith(rig, holder, s => null, () => false);
        else { if (shown("chest") != null) AHCostumes.Apply(rig, AHCostumes.Under, look); AHWardrobe.DressWith(rig, holder, shown, () => false, look != null && look.sex == "f" ? "f" : "m", cls.id); }
        if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
        I.hero = holder.gameObject; I.heroSig = sig; SetLayer(holder, Layer);
        I.rim.color = Color.Lerp(cls.color, Color.white, 0.25f);
    }

    // ---- the scene ----
    static Shader lit, unlit;
    static Material M(Color c, bool glow = false)
    {
        if (lit == null) { lit = Shader.Find("Universal Render Pipeline/Lit"); unlit = Shader.Find("Universal Render Pipeline/Unlit"); }
        var m = new Material(glow ? unlit : lit); m.SetColor("_BaseColor", c); if (!glow) m.SetFloat("_Smoothness", 0.1f); return m;
    }
    Transform Prim(PrimitiveType pt, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 rot = default(Vector3))
    {
        var o = GameObject.CreatePrimitive(pt); Destroy(o.GetComponent<Collider>()); o.transform.SetParent(parent, false);
        o.transform.localPosition = pos; o.transform.localScale = scale; o.transform.localRotation = Quaternion.Euler(rot); o.GetComponent<Renderer>().sharedMaterial = m; return o.transform;
    }

    void Build()
    {
        root = new GameObject("Hall").transform; root.SetParent(transform, false); root.position = At;
        // the camera: its own, drawing only the hall, under the night sky colour
        var cg = new GameObject("HallCam"); cg.transform.SetParent(transform, false); cam = cg.AddComponent<Camera>();
        cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.13f, 0.17f, 0.3f); cam.fieldOfView = 34f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f; cam.depth = 50;
        var cd = cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); cd.renderPostProcessing = true; cd.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        // ground: a clearing of dark grass, a worn earth patch round the fire
        Prim(PrimitiveType.Cylinder, root, new Vector3(0, -0.05f, 0), new Vector3(60f, 0.05f, 60f), M(new Color(0.07f, 0.1f, 0.06f)));
        Prim(PrimitiveType.Cylinder, root, new Vector3(-1.05f, -0.03f, 1.0f), new Vector3(3.4f, 0.04f, 3.4f), M(new Color(0.16f, 0.12f, 0.08f)));
        // the moon and its cold light
        Prim(PrimitiveType.Sphere, root, new Vector3(-4f, 11f, -55f), Vector3.one * 4.5f, M(new Color(1.6f, 1.55f, 1.35f), true));
        var moon = new GameObject("Moonlight").AddComponent<Light>(); moon.transform.SetParent(root, false); moon.type = LightType.Directional; moon.color = new Color(0.55f, 0.65f, 1f); moon.intensity = 0.8f; moon.cullingMask = 1 << Layer; moon.shadows = LightShadows.Soft; moon.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
        // the pine wood round the clearing, dark against the sky
        var dark = M(new Color(0.02f, 0.035f, 0.03f)); var bark = M(new Color(0.08f, 0.06f, 0.05f)); var rnd = new System.Random(11);
        for (int i = 0; i < 46; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 9f + (float)rnd.NextDouble() * 16f; if (Mathf.Sin(a) > 0.55f && r < 14f) r += 6f;   // keep the view toward the camera open
            float h = 5f + (float)rnd.NextDouble() * 5f; var p = new Vector3(Mathf.Cos(a) * r, 0f, -Mathf.Abs(Mathf.Sin(a)) * r * 0.8f - 2f);
            var tree = new GameObject("Pine").transform; tree.SetParent(root, false); tree.localPosition = p;
            Prim(PrimitiveType.Cylinder, tree, new Vector3(0, h * 0.15f, 0), new Vector3(0.25f, h * 0.15f, 0.25f), bark);
            for (int k = 0; k < 4; k++) { float y = h * (0.25f + k * 0.18f), w = h * (0.32f - k * 0.065f); var c = new GameObject("Cone").transform; c.SetParent(tree, false); c.localPosition = new Vector3(0, y, 0); c.localScale = new Vector3(w, h * 0.3f, w); c.gameObject.AddComponent<MeshFilter>().sharedMesh = Cone(); c.gameObject.AddComponent<MeshRenderer>().sharedMaterial = dark; }
        }
        // a log to sit on, and a pack leaning on it
        Prim(PrimitiveType.Cylinder, root, new Vector3(-2.9f, 0.18f, -0.4f), new Vector3(0.36f, 0.8f, 0.36f), M(new Color(0.24f, 0.15f, 0.08f)), new Vector3(0, -30f, 90f));
        Prim(PrimitiveType.Cube, root, new Vector3(-2.5f, 0.32f, 0.1f), new Vector3(0.36f, 0.5f, 0.26f), M(new Color(0.3f, 0.2f, 0.12f)), new Vector3(0, 20f, 8f));
        // the campfire: a ring of stones, crossed logs, flames, embers rising and a warm flickering light
        fireT = new GameObject("Campfire").transform; fireT.SetParent(root, false); fireT.localPosition = new Vector3(-1.05f, 0f, 1.0f);   // on the hero's right as you look (the camera looks down -z: +x is the left of the picture)
        var stone = M(new Color(0.3f, 0.29f, 0.28f)); var wood = M(new Color(0.2f, 0.12f, 0.07f));
        for (int i = 0; i < 10; i++) { float a = i * Mathf.PI * 0.2f; Prim(PrimitiveType.Sphere, fireT, new Vector3(Mathf.Cos(a) * 0.55f, 0.05f, Mathf.Sin(a) * 0.55f), new Vector3(0.22f, 0.14f, 0.2f), stone); }
        for (int i = 0; i < 4; i++) Prim(PrimitiveType.Cylinder, fireT, new Vector3(0, 0.1f, 0), new Vector3(0.1f, 0.38f, 0.1f), wood, new Vector3(75f, i * 45f, 0));
        Color[] fc = { new Color(2.2f, 0.55f, 0.12f), new Color(2.4f, 1.0f, 0.22f), new Color(2.5f, 1.7f, 0.6f) }; flames = new Transform[7];
        for (int i = 0; i < flames.Length; i++)
        {
            var fo = new GameObject("Flame"); fo.transform.SetParent(fireT, false); fo.AddComponent<MeshFilter>().sharedMesh = Cone();
            var fr = fo.AddComponent<MeshRenderer>(); fr.sharedMaterial = M(fc[i % 3], true); fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; flames[i] = fo.transform;
        }
        fireLight = new GameObject("FireLight").AddComponent<Light>(); fireLight.transform.SetParent(fireT, false); fireLight.transform.localPosition = new Vector3(0, 0.7f, 0); fireLight.type = LightType.Point; fireLight.color = new Color(1f, 0.55f, 0.22f); fireLight.range = 9f; fireLight.intensity = 3f; fireLight.shadows = LightShadows.Soft; fireLight.cullingMask = 1 << Layer;
        Embers(); Stars();
        // the hero's place, and the rim of class colour behind
        heroHold = new GameObject("HeroPlace").transform; heroHold.SetParent(root, false); heroHold.localPosition = Vector3.zero; heroHold.localRotation = Quaternion.Euler(0f, -16f, 0f);   // turned a little toward the fire
        rim = new GameObject("Rim").AddComponent<Light>(); rim.transform.SetParent(root, false); rim.transform.localPosition = new Vector3(0.9f, 2.4f, -2.2f); rim.transform.LookAt(root.position + Vector3.up * 1.1f);
        rim.type = LightType.Spot; rim.spotAngle = 45f; rim.range = 8f; rim.intensity = 6f; rim.cullingMask = 1 << Layer;
        // a soft cool fill from the front, so the face reads in the dark
        var fill = new GameObject("Fill").AddComponent<Light>(); fill.transform.SetParent(root, false); fill.transform.localPosition = new Vector3(0.6f, 1.8f, 3.2f); fill.transform.LookAt(root.position + Vector3.up * 1.2f);
        fill.type = LightType.Spot; fill.spotAngle = 50f; fill.range = 8f; fill.intensity = 2.2f; fill.color = new Color(0.75f, 0.82f, 1f); fill.cullingMask = 1 << Layer;
        SetLayer(transform, Layer);
    }

    static Mesh cone;
    static Mesh Cone()
    {
        if (cone != null) return cone; var v = new List<Vector3>(); var tr = new List<int>(); const int n = 12;
        v.Add(new Vector3(0, 1, 0)); for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); }
        for (int i = 1; i <= n; i++) { tr.AddRange(new[] { 0, i + 1, i }); tr.AddRange(new[] { 0, i, i + 1 }); }   // both sides
        cone = new Mesh(); cone.SetVertices(v); cone.SetTriangles(tr, 0); cone.RecalculateNormals(); cone.RecalculateBounds(); return cone;
    }
    static Texture2D dot;
    static Material DotMat(Color c)
    {
        if (dot == null)
        {
            dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) { float u = (x + 0.5f) / 16f - 1f, w = (y + 0.5f) / 16f - 1f, a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + w * w)); dot.SetPixel(x, y, new Color(1, 1, 1, a * a)); }
            dot.Apply();
        }
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); m.SetColor("_BaseColor", c); m.SetTexture("_BaseMap", dot);
        m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.2f); m.EnableKeyword("_ALPHATEST_ON"); return m;
    }
    void Embers()
    {
        var go = new GameObject("Embers"); go.transform.SetParent(fireT, false); go.transform.localPosition = new Vector3(0, 0.3f, 0);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.useUnscaledTime = true; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f); main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f); main.maxParticles = 120;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.5f, 1.1f, 0.3f), new Color(2.5f, 0.6f, 0.15f)); main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 26f; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 18f; sh.radius = 0.2f; sh.rotation = new Vector3(-90f, 0, 0);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.8f;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = DotMat(Color.white); ps.Play();
    }
    void Stars()
    {
        var go = new GameObject("Stars"); go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(0, 30f, -80f);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.useUnscaledTime = true; main.loop = false; main.startLifetime = 1e6f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f); main.maxParticles = 400;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.4f, 1.4f, 1.6f), new Color(0.9f, 0.95f, 1.3f));
        var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 380) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(220f, 70f, 2f);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = DotMat(Color.white); ps.Play();
    }

    static void SetLayer(Transform t, int l) { foreach (var c in t.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = l; }

    // ---- every frame (the game is paused under the select screen: unscaled time) ----
    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime; t += dt;
        for (int i = 0; i < flames.Length; i++)
        {
            // tongues of flame round the centre: the inner ones taller and paler, each licking up and down on its own
            float a = i * 2.4f, ring = i == 0 ? 0f : 0.09f + (i % 2) * 0.05f, k = i == 0 ? 1f : 0.55f + (i % 3) * 0.12f;
            float fl = 1f + Mathf.Sin(t * (8f + i * 2.3f)) * 0.22f + Mathf.Sin(t * (13f + i * 3.1f)) * 0.1f;
            flames[i].localPosition = new Vector3(Mathf.Cos(a) * ring + Mathf.Sin(t * 5f + i) * 0.015f, 0.06f, Mathf.Sin(a) * ring + Mathf.Cos(t * 4f + i) * 0.015f);
            flames[i].localScale = new Vector3(0.2f * k, 0.62f * k * fl, 0.2f * k);
            flames[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 3f + i * 2f) * 6f, i * 40f, Mathf.Cos(t * 2.5f + i) * 6f);
        }
        fireLight.intensity = 2.6f + Mathf.PerlinNoise(t * 6f, 0f) * 1.6f;
        if (hero != null && Mathf.Repeat(t, 1f) < dt * 1.5f) SetLayer(hero.transform, Layer);   // the VRoid body may join late
        // the camera drifts slowly round the hero and fire; the hero sits in the left part of the picture
        heroHold.localRotation = Quaternion.Slerp(heroHold.localRotation, Quaternion.Euler(0f, -16f + Spin, 0f), 1f - Mathf.Exp(-dt * 12f));
        sNear = Mathf.Lerp(sNear, Near, 1f - Mathf.Exp(-dt * 6f));
        float yaw = Mathf.Sin(t * 0.12f) * 9f * (1f - sNear), dolly = Mathf.Sin(t * 0.09f) * 0.35f * (1f - sNear);
        // the hero at the left, the fire beside them, the list over the right; closer in, the face
        Vector3 focus = At + Vector3.Lerp(new Vector3(-1.45f - 0.55f * Shift, 1.0f, 0f), new Vector3(-0.62f, 1.42f, 0f), sNear);
        Vector3 off = Quaternion.Euler(0f, yaw, 0f) * new Vector3(Mathf.Lerp(0.3f, 0.05f, sNear), Mathf.Lerp(0.55f, 0.12f, sNear), Mathf.Lerp(7.2f, 2.9f, sNear) + dolly);
        cam.transform.position = focus + off; cam.transform.LookAt(focus);
    }
}
