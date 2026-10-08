// Ashen Hollow: lighter shapes for phones (plus the shown-monster helper for the editor tests)
// thousands of little balls (lamp curls, flower heads, berries, rivets, gear knobs), so every sphere and capsule made
// in code is swapped for one of about 160 triangles that looks the same at game distance. AHCull stops drawing the
// rigged townsfolk and beasts far down the street (past where the fog hides them anyway).
using System.Collections.Generic;
using UnityEngine;

public static class AHLowPoly
{
    static Mesh sphere, capsule;
    public static Mesh Sphere { get { if (sphere == null) sphere = Make(false); return sphere; } }
    public static Mesh Capsule { get { if (capsule == null) capsule = Make(true); return capsule; } }

    // a UV ball of radius 0.5 (or a capsule 2 tall, 0.5 round, like Unity's)
    static Mesh Make(bool cap)
    {
        int seg = 12, rings = cap ? 10 : 8; var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= rings; i++)
        {
            float la = Mathf.PI * (i / (float)rings - 0.5f);
            for (int k = 0; k <= seg; k++)
            {
                float lo = k * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo));
                var p = d * 0.5f; if (cap) p.y += i > rings / 2 ? 0.5f : i < rings / 2 ? -0.5f : 0f;
                v.Add(p); n.Add(d); uv.Add(new Vector2(k / (float)seg, i / (float)rings));
            }
        }
        for (int i = 0; i < rings; i++) for (int k = 0; k < seg; k++) { int p = i * (seg + 1) + k, q = p + seg + 1; t.AddRange(new[] { p, q, q + 1, p, q + 1, p + 1 }); }
        var m = new Mesh { name = cap ? "Capsule" : "Sphere" }; m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds(); m.RecalculateTangents();
        return m;
    }

    // after GameObject.CreatePrimitive: put the light mesh in
    public static GameObject Fix(GameObject o, PrimitiveType t)
    {
        if (o == null || (t != PrimitiveType.Sphere && t != PrimitiveType.Capsule)) return o;
        var mf = o.GetComponent<MeshFilter>(); if (mf != null) mf.sharedMesh = t == PrimitiveType.Sphere ? Sphere : Capsule;
        return o;
    }
    // the mesh to use for a primitive kind (helpers that only want the mesh)
    public static Mesh For(PrimitiveType t, Mesh builtIn) { return t == PrimitiveType.Sphere ? Sphere : t == PrimitiveType.Capsule ? Capsule : builtIn; }
}

// stops drawing far-off rigged figures (townsfolk, beasts, companions) and wakes them as you come near
public class AHCull : MonoBehaviour
{
    public float far = 58f;
    readonly List<SkinnedMeshRenderer> list = new List<SkinnedMeshRenderer>();
    float scan, check;
    void Update()
    {
        var g = AHGame.I; if (g == null || g.cam == null) return;
        scan -= Time.deltaTime; check -= Time.deltaTime;
        if (scan <= 0f)
        {
            scan = 3f; list.Clear();
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) list.AddRange(go.GetComponentsInChildren<SkinnedMeshRenderer>(false));
        }
        if (check > 0f) return;
        check = 0.25f;
        Vector3 c = g.cam.transform.position; Transform hero = g.player != null ? g.player.transform : null; float f2 = far * far;
        foreach (var r in list)
        {
            if (r == null) continue;
            if (hero != null && r.transform.IsChildOf(hero)) { r.forceRenderingOff = false; continue; }
            if (r.gameObject.layer == AHItemStudio.Layer || r.gameObject.layer == AHHeroHall.Layer) { r.forceRenderingOff = false; continue; }   // studio stand-ins and the hero hall (far from the land)   // the picture studio's stand-ins (far above the land)
            bool off = (r.bounds.center - c).sqrMagnitude > f2;
            if (r.forceRenderingOff != off) r.forceRenderingOff = off;
        }
    }
}

// keeps a shown monster idling (its own AI is off)
public class AHShowAnim : MonoBehaviour
{
    [System.NonSerialized] public AHAnim anim; [System.NonSerialized] public string shotName; float t; bool shot;
    void Update()
    {
        t += Time.deltaTime;
        if (anim != null && anim.HasClips) { anim.Play(t % 8f < 5.5f ? "Idle" : "Walk", true); anim.Tick(Time.deltaTime); }
        if (!shot && t > 0.8f && shotName != null) { shot = true; Snap(); }
    }
    // a picture of it from the front three-quarters and the side, into HeroShots/<shotName>.png (editor tests)
    void Snap()
    {
        var rs = GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return;
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
        const int S = 640; var go = new GameObject("ShowCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.7f, 0.76f);
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt; var tex = new Texture2D(S * 2, S, TextureFormat.RGB24, false);
        var views = new[] { (transform.forward * 0.8f + transform.right * 0.5f + Vector3.up * 0.25f).normalized, (transform.right + Vector3.up * 0.15f).normalized };
        for (int i = 0; i < 2; i++)
        {
            cam.transform.position = b.center + views[i] * (size * 2.0f + 0.6f); cam.transform.LookAt(b.center); cam.Render();
            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, S, S), i * S, 0); RenderTexture.active = null;
        }
        tex.Apply();
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, shotName + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; Destroy(go); Destroy(rt); Destroy(tex);
    }
}
