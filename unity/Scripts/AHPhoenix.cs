// Ashen Hollow: the Ashen Phoenix, a rare mount. A great bird of living flame that you ride on its back: it hovers
// above the ground, beats its wings (faster when you run), sheds embers behind it and lights the ground around it.
// It is the fastest mount in the realm. It drops (rarely) from Emberback the world boss and from Vaelor in the
// Ember Throne raid; you can also see it in the Varrow Stables' list of rare mounts once you own it.
using System;
using UnityEngine;

public static class AHPhoenix
{
    public const string Id = "phoenix";
    public const float Seat = 0.72f;
    static Shader lit; static Mesh cone;

    static Mesh Cone()
    {
        if (cone != null) return cone;
        // two-sided: the outside and the inside each get their own vertices, so both are lit properly
        int seg = 16; var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
        for (int side = 0; side < 2; side++)
        {
            int b = v.Count; v.Add(new Vector3(0, 1, 0));
            for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2 / seg; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); }
            for (int i = 1; i <= seg; i++) { if (side == 0) t.AddRange(new[] { b, b + i + 1, b + i }); else t.AddRange(new[] { b, b + i, b + i + 1 }); }
        }
        cone = new Mesh(); cone.SetVertices(v); cone.SetTriangles(t, 0); cone.RecalculateNormals(); cone.RecalculateBounds(); return cone;
    }
    static Material Mat(Color c, float glow, float metal = 0f)
    {
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", 0.35f);
        if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
        return m;
    }
    static Transform Prim(Transform parent, PrimitiveType pt, Vector3 pos, Vector3 size, Vector3 rot, Material m)
    {
        var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); UnityEngine.Object.Destroy(o.GetComponent<Collider>()); o.name = "Phoenix";
        o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localRotation = Quaternion.Euler(rot); o.transform.localScale = size;
        o.GetComponent<Renderer>().sharedMaterial = m; return o.transform;
    }
    static Transform ConeAt(Transform parent, Vector3 pos, Vector3 size, Vector3 rot, Material m)
    {
        var o = new GameObject("Phoenix"); o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localRotation = Quaternion.Euler(rot); o.transform.localScale = size;
        o.AddComponent<MeshFilter>().sharedMesh = Cone(); o.AddComponent<MeshRenderer>().sharedMaterial = m; return o.transform;
    }

    static void Piece(Transform parent, Mesh mesh, Vector3 pos, Vector3 scale, Material m)
    {
        var o = new GameObject("Phoenix"); o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
        o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = m;
    }

    // builds the bird under 'holder' (a child of the hero, facing the hero's forward)
    public static void Build(Transform holder, AHPlayer p)
    {
        // the phoenix model (NORBERTO-3D, CC-BY 4.0) when it is there: a jewel-green firebird with a flaming tail
        if (Resources.Load<GameObject>("AH/Models/Comp/mount_phoenix") != null)
        {
            var mb = new GameObject("PhoenixBody").transform; mb.SetParent(holder, false);
            AHAnim a; var inst = AHModel.Spawn(mb, "Comp/mount_phoenix", 3.2f, true, 0f, out a);
            if (a != null && a.HasClips) a.Play("Take 001", true, 1f);
            var lg0 = new GameObject("PhoenixLight"); lg0.transform.SetParent(mb, false); lg0.transform.localPosition = new Vector3(0, 0.6f, -0.6f);
            var li0 = lg0.AddComponent<Light>(); li0.type = LightType.Point; li0.color = new Color(1f, 0.55f, 0.2f); li0.range = 6f; li0.intensity = 1.1f; li0.shadows = LightShadows.None;
            mb.gameObject.AddComponent<AHPhoenixRide>().Setup(p, mb, a, li0);
            return;
        }
        var body = new GameObject("PhoenixBody").transform; body.SetParent(holder, false);
        Color red = new Color(0.85f, 0.18f, 0.05f), orange = new Color(1f, 0.45f, 0.08f), gold = new Color(1f, 0.78f, 0.25f), pale = new Color(1f, 0.92f, 0.6f);
        Material mRed = Mat(red, 0.22f), mOr = Mat(orange, 0.38f), mGold = Mat(gold, 0.35f, 0.4f), mPale = Mat(pale, 0.55f), mEye = Mat(new Color(0.05f, 0.03f, 0.02f), 0f), mBeak = Mat(new Color(0.95f, 0.75f, 0.3f), 0.3f, 0.6f);

        // body, chest, neck and head
        Prim(body, PrimitiveType.Sphere, new Vector3(0, 0.95f, -0.1f), new Vector3(0.75f, 0.62f, 1.35f), Vector3.zero, mRed);
        Prim(body, PrimitiveType.Sphere, new Vector3(0, 1.05f, 0.5f), new Vector3(0.74f, 0.74f, 0.78f), Vector3.zero, mOr);
        Prim(body, PrimitiveType.Sphere, new Vector3(0, 0.82f, 0.35f), new Vector3(0.5f, 0.4f, 0.6f), Vector3.zero, mGold);   // golden breast
        Prim(body, PrimitiveType.Cylinder, new Vector3(0, 1.38f, 0.78f), new Vector3(0.32f, 0.22f, 0.32f), new Vector3(30, 0, 0), mOr);
        Prim(body, PrimitiveType.Sphere, new Vector3(0, 1.62f, 0.95f), new Vector3(0.38f, 0.36f, 0.42f), Vector3.zero, mRed);
        ConeAt(body, new Vector3(0, 1.58f, 1.12f), new Vector3(0.12f, 0.26f, 0.12f), new Vector3(100, 0, 0), mBeak);
        foreach (int sx in new[] { -1, 1 })
        {
            Prim(body, PrimitiveType.Sphere, new Vector3(sx * 0.15f, 1.67f, 1.08f), Vector3.one * 0.06f, Vector3.zero, mEye);
            Prim(body, PrimitiveType.Sphere, new Vector3(sx * 0.17f, 1.68f, 1.07f), Vector3.one * 0.03f, Vector3.zero, mPale);
        }
        for (int i = 0; i < 4; i++) ConeAt(body, new Vector3(0, 1.75f, 0.9f - i * 0.1f), new Vector3(0.07f, 0.34f - i * 0.04f, 0.025f), new Vector3(-30 - i * 16, 0, 0), i % 2 == 0 ? mGold : mPale);   // crest

        // the saddle: a small leather seat with gold trim
        Prim(body, PrimitiveType.Cube, new Vector3(0, 1.25f, -0.05f), new Vector3(0.5f, 0.08f, 0.55f), Vector3.zero, Mat(new Color(0.35f, 0.18f, 0.08f), 0f));
        Prim(body, PrimitiveType.Cube, new Vector3(0, 1.3f, 0.2f), new Vector3(0.52f, 0.12f, 0.06f), Vector3.zero, mGold);

        // tucked golden legs
        foreach (int sx in new[] { -1, 1 }) ConeAt(body, new Vector3(sx * 0.2f, 0.62f, 0.1f), new Vector3(0.12f, 0.35f, 0.12f), new Vector3(-150, 0, 0), mBeak);

        // the tail: long flame feathers fanning back
        var tail = new GameObject("Tail").transform; tail.SetParent(body, false); tail.localPosition = new Vector3(0, 1.0f, -0.75f);
        for (int i = -3; i <= 3; i++)
            ConeAt(tail, Vector3.zero, new Vector3(0.28f, 2.1f - Mathf.Abs(i) * 0.22f, 0.04f), new Vector3(-104 + Mathf.Abs(i) * 6f, i * 9f, 0), Mathf.Abs(i) % 2 == 0 ? mOr : mPale);

        // the wings: an arm from the shoulder with long burning feathers hanging from it; the roots flap
        var wings = new Transform[2];
        for (int s = 0; s < 2; s++)
        {
            int sx = s == 0 ? -1 : 1;
            var root = new GameObject("Wing").transform; root.SetParent(body, false); root.localPosition = new Vector3(sx * 0.32f, 1.18f, 0.25f);
            // feathered wings (the same shapes as the wings cosmetic, much larger): burning orange flight feathers over a
            // golden under-layer, red coverts and a gold leading edge, swept a little back
            var wg = new GameObject("WingShape").transform; wg.SetParent(root, false); wg.localRotation = Quaternion.Euler(0, sx * 14f, 0);
            Vector3 ws = new Vector3(sx * 1.75f, 1.75f, 1.75f);
            Piece(wg, AHWardrobe.WingFeathers, new Vector3(0, 0, 0.02f), new Vector3(sx * 1.55f, 1.45f, 1.55f), mGold);
            Piece(wg, AHWardrobe.WingFeathers, Vector3.zero, ws, mOr);
            Piece(wg, AHWardrobe.WingCoverts, new Vector3(0, 0, -0.012f), ws, mRed);
            Piece(wg, AHWardrobe.WingArm, new Vector3(0, 0, -0.02f), ws, mBeak);
            wings[s] = root;
        }

        var lg = new GameObject("PhoenixLight"); lg.transform.SetParent(body, false); lg.transform.localPosition = new Vector3(0, 0.6f, 0);
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.55f, 0.2f); li.range = 6f; li.intensity = 1.1f; li.shadows = LightShadows.None;

        foreach (var r in body.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        body.gameObject.AddComponent<AHPhoenixFly>().Setup(p, body, wings, tail, li);
    }
}

// wing beats, hovering, a flickering glow and a trail of embers
public class AHPhoenixFly : MonoBehaviour
{
    AHPlayer p; Transform body, tail; Transform[] wings; Light li; float t, flap; Vector3 last;
    public void Setup(AHPlayer pl, Transform b, Transform[] w, Transform tl, Light l) { p = pl; body = b; wings = w; tail = tl; li = l; last = transform.position; }
    void LateUpdate()
    {
        if (p == null) return;
        float dt = Time.deltaTime; t += dt;
        bool moving = p.Moving;
        flap += dt * (moving ? 7.5f : 3.2f);
        float beat = Mathf.Sin(flap), up = Mathf.Max(0f, -Mathf.Cos(flap));
        float hover = 0.35f + Mathf.Sin(t * 1.7f) * 0.06f + up * (moving ? 0.07f : 0.04f);
        body.localPosition = new Vector3(0, hover, 0);
        body.localRotation = Quaternion.Euler(moving ? 8f : 0f, 0, 0);
        p.seatY = AHPhoenix.Seat + hover;
        for (int s = 0; s < 2; s++) { int sx = s == 0 ? -1 : 1; wings[s].localRotation = Quaternion.Euler(0, sx * (moving ? 18f : 6f), -sx * beat * (moving ? 38f : 26f)); }
        tail.localRotation = Quaternion.Euler(Mathf.Sin(t * 2.3f) * 5f, Mathf.Sin(t * 1.3f) * 6f, 0);
        li.intensity = 1.05f + Mathf.Sin(t * 13f) * 0.15f + Mathf.Sin(t * 7.3f) * 0.12f;
        // embers: from the tail always, from the wing tips on each downbeat, more when running
        Vector3 tailTip = tail.position - transform.forward * 1.1f;
        if (moving || Time.frameCount % 3 == 0) AHSpark.Trail(tailTip + UnityEngine.Random.insideUnitSphere * 0.3f, new Color(1f, 0.55f + UnityEngine.Random.value * 0.3f, 0.15f), 0.16f, moving ? 2 : 1);
        if (moving && beat < -0.9f) foreach (var w in wings) AHSpark.Trail(w.position + w.right * (w.localPosition.x > 0 ? 1.6f : -1.6f) - w.up * 0.4f, new Color(1f, 0.8f, 0.35f), 0.14f, 2);
        if (UnityEngine.Random.value < dt * 2f) AHSpark.Emit(transform.position + Vector3.up * 0.2f + UnityEngine.Random.insideUnitSphere * 0.6f, Vector3.up * 0.8f, 0.12f, 1f, new Color(1f, 0.45f, 0.1f));
    }
}

// riding the phoenix model: it hovers and bobs, leans into the run, beats faster while moving, glows and sheds embers
public class AHPhoenixRide : MonoBehaviour
{
    public const float Seat = 0.82f;
    AHPlayer p; Transform body; AHAnim anim; Light li; float t; bool wasMoving;
    public void Setup(AHPlayer pl, Transform b, AHAnim a, Light l) { p = pl; body = b; anim = a; li = l; }
    void LateUpdate()
    {
        if (p == null) return;
        float dt = Time.deltaTime; t += dt; bool moving = p.Moving;
        if (anim != null && moving != wasMoving) { anim.Play("Take 001", true, moving ? 1.8f : 1f); wasMoving = moving; }
        float hover = 0.4f + Mathf.Sin(t * 1.7f) * 0.07f;
        body.localPosition = new Vector3(0, hover, 0);
        body.localRotation = Quaternion.Euler(moving ? 6f : 0f, 0, Mathf.Sin(t * 0.9f) * 2f);
        p.seatY = Seat + hover;
        li.intensity = 1.05f + Mathf.Sin(t * 13f) * 0.15f + Mathf.Sin(t * 7.3f) * 0.12f;
        Vector3 tail = transform.position - transform.forward * 1.6f + Vector3.up * 0.8f;
        if (moving || Time.frameCount % 3 == 0) AHSpark.Trail(tail + UnityEngine.Random.insideUnitSphere * 0.4f, new Color(1f, 0.55f + UnityEngine.Random.value * 0.3f, 0.15f), 0.16f, moving ? 2 : 1);
    }
    void OnDestroy() { if (anim != null) anim.Dispose(); }
}
