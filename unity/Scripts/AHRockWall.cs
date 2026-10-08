// Ashen Hollow: the Warden's Rockwall. A ridge of stone bursts up a few steps ahead, across the Warden's path, with
// ember glowing in its cracks. While it stands, any enemy that walks into it is shoved back out to its own side and
// held fast for a moment (the spell's status, usually Root). It sinks back into the ground when its time is up.
// Only one wall stands at a time: raising a new one brings the old one down.
using System.Collections.Generic;
using UnityEngine;

public class AHRockWall : MonoBehaviour
{
    static AHRockWall standing;
    AHPlayer owner; SpellDef sp; Vector3 along, across; float half, life, age, tick;
    readonly List<Transform> rocks = new List<Transform>(); readonly List<float> heights = new List<float>();
    readonly Dictionary<AHMob, float> held = new Dictionary<AHMob, float>();
    const float Thick = 1.1f, Rise = 0.35f, Sink = 0.5f;

    public static void Raise(AHPlayer p, SpellDef sp, Vector3 at, Vector3 forward)
    {
        if (standing != null) standing.life = Mathf.Min(standing.life, standing.age + 0.01f);
        var go = new GameObject("RockWall");
        go.transform.position = AHGame.I != null ? AHGame.I.Resolve(at, 0.5f) : at;
        go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        var w = go.AddComponent<AHRockWall>();
        w.owner = p; w.sp = sp; w.life = Mathf.Max(1f, sp.time); w.half = Mathf.Max(1.5f, sp.radius);
        w.across = forward; w.along = Vector3.Cross(Vector3.up, forward);
        w.Build(); standing = w;
        AHFx.Ring(go.transform.position, 0.4f, w.half, new Color(0.75f, 0.55f, 0.35f), 0.5f);
    }

    // jagged stone slabs side by side, leaning a little, with a thin ember seam in each
    void Build()
    {
        var stone = Mat(new Color(0.46f, 0.42f, 0.38f), 0f); var dark = Mat(new Color(0.3f, 0.27f, 0.25f), 0f); var ember = Mat(new Color(1f, 0.45f, 0.12f), 2.2f);
        int n = Mathf.Max(5, Mathf.RoundToInt(half * 2f / 0.75f));
        var rnd = new System.Random(n * 7919 + (int)(Time.time * 10f));
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Lerp(-half, half, (i + 0.5f) / n), h = 1.6f + (float)rnd.NextDouble() * 0.9f - Mathf.Abs(x / half) * 0.5f;
            var slab = new GameObject("Rock").transform; slab.SetParent(transform, false);
            slab.localPosition = new Vector3(x, -h, ((float)rnd.NextDouble() - 0.5f) * 0.3f);
            slab.localRotation = Quaternion.Euler(((float)rnd.NextDouble() - 0.5f) * 14f, ((float)rnd.NextDouble() - 0.5f) * 30f, ((float)rnd.NextDouble() - 0.5f) * 16f);
            Part(slab, PrimitiveType.Cube, new Vector3(0, h * 0.5f, 0), new Vector3(0.85f, h, Thick * 0.8f), Vector3.zero, i % 3 == 1 ? dark : stone);
            Part(slab, PrimitiveType.Cube, new Vector3(0.05f, h * 0.92f, 0f), new Vector3(0.55f, 0.35f, Thick * 0.6f), new Vector3(0, 0, 18f), stone);   // the broken top
            Part(slab, PrimitiveType.Cube, new Vector3(0.1f, h * 0.5f, Thick * 0.41f), new Vector3(0.05f, h * 0.7f, 0.02f), new Vector3(0, 0, 8f), ember);  // the seam, front
            Part(slab, PrimitiveType.Cube, new Vector3(-0.1f, h * 0.45f, -Thick * 0.41f), new Vector3(0.05f, h * 0.6f, 0.02f), new Vector3(0, 0, -6f), ember);
            rocks.Add(slab); heights.Add(h);
        }
        var li = new GameObject("Glow").AddComponent<Light>(); li.transform.SetParent(transform, false); li.transform.localPosition = new Vector3(0, 0.8f, 0);
        li.type = LightType.Point; li.color = new Color(1f, 0.55f, 0.2f); li.range = half * 1.6f; li.intensity = 1.2f; li.shadows = LightShadows.None;
    }

    void Update()
    {
        age += Time.deltaTime;
        // rising, standing, sinking
        float up = age < Rise ? Mathf.SmoothStep(0f, 1f, age / Rise) : age > life ? 1f - Mathf.Clamp01((age - life) / Sink) : 1f;
        for (int i = 0; i < rocks.Count; i++) { var p = rocks[i].localPosition; p.y = -heights[i] * (1f - up); rocks[i].localPosition = p; }
        if (age > life + Sink) { if (standing == this) standing = null; Destroy(gameObject); return; }
        if (age > life || age < Rise * 0.5f) return;
        tick -= Time.deltaTime; if (tick > 0f) return; tick = 0.15f;
        var g = AHGame.I; if (g == null) return;
        Vector3 c = transform.position;
        foreach (var m in g.mobs)
        {
            if (m == null || m.dead) continue;
            Vector3 d = m.transform.position - c; d.y = 0f;
            float a = Vector3.Dot(d, along), b = Vector3.Dot(d, across), r = m.type.radius;
            if (Mathf.Abs(a) > half + r * 0.5f || Mathf.Abs(b) > Thick * 0.5f + r) continue;
            // in the wall: out to the side it came from, and held there (once a second at most)
            float side = b >= 0f ? 1f : -1f;
            Vector3 to = c + along * a + across * side * (Thick * 0.5f + r + 0.15f); to.y = m.transform.position.y;
            m.transform.position = g.Resolve(to, r);
            float last; held.TryGetValue(m, out last);
            if (Time.time - last > 1f && sp.status != AHStatus.None && owner != null)
            {
                held[m] = Time.time;
                m.AddStatus(sp.status, sp.statusTime, owner.Power * 0.2f, owner);
                AHFx.Pop(m.transform.position + Vector3.up * 0.6f, 0.9f, new Color(0.8f, 0.6f, 0.4f));
            }
        }
    }

    static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
    static Material Mat(Color c, float glow)
    {
        int key = Mathf.RoundToInt(c.r * 255) << 16 | Mathf.RoundToInt(c.g * 255) << 8 | Mathf.RoundToInt(c.b * 255) | (glow > 0f ? 1 << 24 : 0);
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.15f);
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
        mats[key] = m; return m;
    }
    static void Part(Transform parent, PrimitiveType t, Vector3 pos, Vector3 size, Vector3 rot, Material m)
    {
        var go = GameObject.CreatePrimitive(t); Object.Destroy(go.GetComponent<Collider>()); go.name = "Piece";
        go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = size; go.transform.localEulerAngles = rot;
        var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
}
