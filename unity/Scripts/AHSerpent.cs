// Ashen Hollow: the Tide serpent, built in code. A long scaled body of tapering segments that rolls along in
// humps like a sea serpent of old charts, a head raised high on an S-shaped neck with a snout, glowing eyes, swept
// horns and a fin crest, and a red-orange fin ridge down its back. AHSerpentBody moves the segments every frame:
// the humps travel back along the body (faster while it moves) and the head sways, rears up before it strikes
// and drops to the ground when it dies.
using System.Collections.Generic;
using UnityEngine;

public static class AHSerpent
{
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string k, Color c, float smooth, float glow = 0f)
    {
        Material m; if (mats.TryGetValue(k, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "serpent_" + k };
        m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c.linear * glow); }
        mats[k] = m; return m;
    }
    static Mesh sph, cone;
    static Mesh Sph { get { if (sph == null) { var g = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Sphere), PrimitiveType.Sphere); sph = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); } return sph; } }
    static Mesh Cone
    {
        get
        {
            if (cone != null) return cone;
            // a fin or horn: a flattened cone pointing up +Y, base on y = 0
            var v = new List<Vector3>(); var t = new List<int>(); int n = 10;
            v.Add(new Vector3(0, 1, 0));
            for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2 / n; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); }
            for (int i = 1; i <= n; i++) t.AddRange(new[] { 0, i + 1, i });
            cone = new Mesh(); cone.SetVertices(v); cone.SetTriangles(t, 0); cone.RecalculateNormals(); cone.RecalculateBounds(); return cone;
        }
    }
    static Transform Part(Transform parent, Mesh mesh, Material m, Vector3 pos, Vector3 scale, Vector3 rot)
    {
        var go = new GameObject("Part"); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = scale; go.transform.localRotation = Quaternion.Euler(rot);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m;
        return go.transform;
    }

    // length: nose to tail tip, in metres
    public static GameObject Build(Transform parent, float length, string id)
    {
        if (length <= 0f) length = 7f;
        var root = new GameObject("Serpent"); root.transform.SetParent(parent, false);
        Material scale = Mat("scale", new Color(0.24f, 0.62f, 0.66f), 0.6f), dark = Mat("dark", new Color(0.16f, 0.46f, 0.54f), 0.55f),
                 belly = Mat("belly", new Color(0.92f, 0.86f, 0.62f), 0.35f), fin = Mat("fin", new Color(0.85f, 0.3f, 0.16f), 0.3f),
                 eye = Mat("eye", new Color(1f, 0.85f, 0.2f), 0.8f, 2.2f), horn = Mat("horn", new Color(0.86f, 0.82f, 0.7f), 0.4f);
        var body = root.AddComponent<AHSerpentBody>();
        int n = 18; float seg = length / (n + 2f), r0 = length * 0.045f;
        body.segs = new Transform[n]; body.seg = seg; body.r0 = r0;
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)(n - 1);
            float r = r0 * Mathf.Lerp(1f, 0.25f, k * k) * (i < 3 ? 0.8f + i * 0.07f : 1f);
            var s = new GameObject("Seg" + i).transform; s.SetParent(root.transform, false);
            Part(s, Sph, i % 2 == 0 ? scale : dark, Vector3.zero, new Vector3(r * 2.1f, r * 2f, seg * 2.6f), Vector3.zero);
            Part(s, Sph, belly, new Vector3(0, -r * 0.4f, 0), new Vector3(r * 1.75f, r * 1.3f, seg * 2.45f), Vector3.zero);
            if (i > 0 && i < n - 1) Part(s, Cone, fin, new Vector3(0, r * 0.75f, 0), new Vector3(r * 0.18f, r * (i % 2 == 0 ? 1.2f : 0.8f), seg * 0.9f), new Vector3(-12f, 0, 0));
            if (i == n - 1) Part(s, Cone, fin, new Vector3(0, 0, -seg * 0.6f), new Vector3(r * 0.25f, r * 4f, seg * 1.4f), new Vector3(-90f, 0, 0));   // tail fin
            body.segs[i] = s;
        }
        // the head: a long skull, snout, jaw, eyes, swept horns and a fin crest
        var h = new GameObject("Head").transform; h.SetParent(root.transform, false); body.head = h;
        float hr = r0 * 1.15f;
        Part(h, Sph, scale, new Vector3(0, 0, hr * 0.4f), new Vector3(hr * 1.9f, hr * 1.5f, hr * 2.6f), Vector3.zero);
        Part(h, Sph, dark, new Vector3(0, -hr * 0.05f, hr * 1.6f), new Vector3(hr * 1.2f, hr * 0.85f, hr * 1.8f), Vector3.zero);
        body.jaw = Part(h, Sph, belly, new Vector3(0, -hr * 0.5f, hr * 1.1f), new Vector3(hr * 1.3f, hr * 0.5f, hr * 2.1f), Vector3.zero);
        foreach (float sx in new[] { -1f, 1f })
        {
            Part(h, Sph, eye, new Vector3(sx * hr * 0.72f, hr * 0.35f, hr * 0.95f), Vector3.one * hr * 0.42f, Vector3.zero);
            Part(h, Cone, horn, new Vector3(sx * hr * 0.55f, hr * 0.55f, hr * 0.1f), new Vector3(hr * 0.3f, hr * 2.0f, hr * 0.3f), new Vector3(-62f, 0, sx * -18f));
            Part(h, Cone, fin, new Vector3(sx * hr * 0.95f, 0, -hr * 0.1f), new Vector3(hr * 0.12f, hr * 1.3f, hr * 1.2f), new Vector3(-70f, 0, sx * -70f));   // gill frills
        }
        Part(h, Cone, fin, new Vector3(0, hr * 0.7f, -hr * 0.3f), new Vector3(hr * 0.15f, hr * 1.6f, hr * 1.8f), new Vector3(-25f, 0, 0));
        foreach (var rr in root.GetComponentsInChildren<MeshRenderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        body.Pose(0f, 0f, 0f);
        AHModel.LastHeight = r0 * 9f;
        return root;
    }
}

public class AHSerpentBody : MonoBehaviour
{
    public Transform[] segs; public Transform head, jaw; public float seg, r0;
    float t, speedK, rear; Vector3 last; bool init;
    readonly List<Vector3> pts = new List<Vector3>();

    void Update()
    {
        var mob = GetComponentInParent<AHMob>();
        Vector3 p = transform.position; float v = init ? (p - last).magnitude / Mathf.Max(Time.deltaTime, 1e-4f) : 0f; last = p; init = true;
        speedK = Mathf.Lerp(speedK, Mathf.Clamp01(v / 3f), 1f - Mathf.Exp(-Time.deltaTime * 4f));
        bool dead = mob != null && mob.dead;
        t += Time.deltaTime * (dead ? 0f : 1.2f + speedK * 3.2f);
        Pose(t, speedK, dead ? -1f : 0f);
    }

    // lay the body along a travelling wave: humps rise and fall behind a raised neck and head
    public void Pose(float time, float move, float mode)
    {
        if (segs == null) return;
        bool dead = mode < 0f;
        pts.Clear();
        float z = 0f;
        for (int i = 0; i <= segs.Length; i++)
        {
            float k = i / (float)segs.Length;
            float y, x;
            if (dead) { y = r0 * 0.6f; x = Mathf.Sin(i * 0.5f) * r0 * 1.5f; }
            else
            {
                // neck: rises from the first humps up to the head; body: humps travelling back
                float neck = Mathf.Clamp01(1f - i / 4f);
                float hump = Mathf.Max(0f, Mathf.Sin(time * 1.6f - i * 0.75f)) * r0 * (2.2f + move) * (1f - k * 0.6f);
                y = r0 * 0.9f + hump * (1f - neck) + neck * neck * r0 * (6.5f + Mathf.Sin(time * 0.9f) * 0.8f);
                x = Mathf.Sin(time * 1.1f - i * 0.5f) * r0 * (0.9f + move * 0.6f) * k;
            }
            pts.Add(new Vector3(x, y, z));
            z -= seg * (i < 4 && !dead ? 0.8f : 1f);
        }
        for (int i = 0; i < segs.Length; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            segs[i].localPosition = (a + b) * 0.5f;
            Vector3 d = a - b; if (d.sqrMagnitude > 1e-6f) segs[i].localRotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
        // the head looks forward and a little down from the top of the neck; the jaw gapes now and then
        if (head != null)
        {
            head.localPosition = pts[0] + new Vector3(0, r0 * 0.4f, seg * 0.3f);
            float sway = dead ? 0f : Mathf.Sin(time * 0.8f) * 12f;
            head.localRotation = Quaternion.Euler(dead ? 0f : 18f + Mathf.Sin(time * 1.3f) * 4f, sway, 0f);
        }
        if (jaw != null)
        {
            float gape = dead ? 18f : Mathf.Max(0f, Mathf.Sin(time * 0.7f) - 0.6f) * 45f;
            jaw.localRotation = Quaternion.Euler(gape, 0, 0);
        }
    }
}

// the Sea Serpent model (Sammy The Citipati, CC-BY 4.0) is rigged but has no animations: this moves its bones.
// Waves run back along the body and the sixteen tail bones, the long neck sways and bobs, the head looks about,
// and the four flippers paddle; all faster while it travels, and it all goes limp when it dies.
public class AHSerpentRig : MonoBehaviour
{
    class Bone { public Transform t; public Quaternion rest; public Vector3 up, side; public float k; }
    readonly List<Bone> spine = new List<Bone>(), neck = new List<Bone>(), flips = new List<Bone>();
    Bone head; float time, moveK; Vector3 last; bool init; AHMob mob;

    Bone Make(Transform t, float k)
    {
        // the axes to bend about, worked out from the model's own up and right, so any bone orientation works
        return new Bone { t = t, rest = t.localRotation, up = Quaternion.Inverse(t.rotation) * transform.up, side = Quaternion.Inverse(t.rotation) * transform.right, k = k };
    }
    bool ready;
    void Start() { Init(); }
    // tests: set up now and hold the pose at a given moment
    public void PoseAt(float t, float move) { Init(); time = t; moveK = move; Apply(false); }
    public void Init()
    {
        if (ready) return; ready = true;
        mob = GetComponentInParent<AHMob>();
        var all = GetComponentsInChildren<Transform>(true);
        var tails = new SortedDictionary<int, Transform>(); var necks = new SortedDictionary<int, Transform>();
        foreach (var t in all)
        {
            string n = t.name.ToLowerInvariant();
            int us = n.IndexOf('_'); string b = us > 0 ? n.Substring(0, us) : n;
            int num;
            if (b.StartsWith("tail") && int.TryParse(b.Substring(4), out num)) tails[num] = t;
            else if (b.StartsWith("neck") && int.TryParse(b.Substring(4), out num)) necks[num] = t;
            else if (b.StartsWith("abdomen") && int.TryParse(b.Substring(7), out num)) tails[-10 + num] = t;
            else if (b == "head") head = null;
            if (b == "head") head = Make(t, 1f);
            if (b.EndsWith("flipper")) flips.Add(Make(t, b.StartsWith("l") ? 1f : -1f));
        }
        int i = 0; foreach (var kv in tails) spine.Add(Make(kv.Value, i++));
        i = 0; foreach (var kv in necks) neck.Add(Make(kv.Value, i++));
        last = transform.position;
    }
    void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector3 p = transform.position; float v = (p - last).magnitude / Mathf.Max(dt, 1e-4f); last = p;
        moveK = Mathf.Lerp(moveK, Mathf.Clamp01(v / 3f), 1f - Mathf.Exp(-dt * 4f));
        bool dead = mob != null && mob.dead;
        if (!dead) time += dt * (1f + moveK * 1.6f);
        Apply(dead);
    }
    void Apply(bool dead)
    {
        if (!ready) return;
        float limp = dead ? 0f : 1f;
        // the body and tail: a side-to-side wave travelling back, bigger toward the tip
        for (int i = 0; i < spine.Count; i++)
        {
            var b = spine[i]; float k = (i + 1f) / spine.Count;
            float yaw = Mathf.Sin(time * 2.2f - i * 0.45f) * (3f + 9f * k) * (0.6f + moveK) * limp;
            float pitch = Mathf.Sin(time * 1.4f - i * 0.3f) * 2.5f * k * limp;
            b.t.localRotation = b.rest * Quaternion.AngleAxis(yaw, b.up) * Quaternion.AngleAxis(pitch, b.side);
        }
        // the neck sways and bobs; the head looks about and dips toward its prey when it strikes
        for (int i = 0; i < neck.Count; i++)
        {
            var b = neck[i];
            float yaw = Mathf.Sin(time * 0.9f - i * 0.35f) * 4.5f * limp, pitch = Mathf.Sin(time * 1.3f - i * 0.4f) * 3f * limp;
            if (dead) pitch = 9f;   // slumps
            b.t.localRotation = b.rest * Quaternion.AngleAxis(yaw, b.up) * Quaternion.AngleAxis(pitch, b.side);
        }
        if (head != null) head.t.localRotation = head.rest * Quaternion.AngleAxis(Mathf.Sin(time * 0.7f) * 14f * limp, head.up) * Quaternion.AngleAxis(Mathf.Sin(time * 1.1f) * 6f * limp, head.side);
        // the flippers paddle in turn
        for (int i = 0; i < flips.Count; i++)
        {
            var b = flips[i];
            float a = Mathf.Sin(time * 3f + i * 1.6f) * 22f * (0.5f + moveK) * limp;
            b.t.localRotation = b.rest * Quaternion.AngleAxis(a, b.side) * Quaternion.AngleAxis(a * 0.5f * b.k, b.up);
        }
    }
}
