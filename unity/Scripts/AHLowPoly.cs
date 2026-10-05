// Ashen Hollow: lighter shapes for phones. Unity's built-in sphere is 768 triangles and its capsule 832; the game uses
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
            bool off = (r.bounds.center - c).sqrMagnitude > f2;
            if (r.forceRenderingOff != off) r.forceRenderingOff = off;
        }
    }
}
