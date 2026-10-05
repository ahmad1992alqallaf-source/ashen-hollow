// Ashen Hollow: simple glowing spell effects (rings, discs, pillars, orbs) and homing shots
using System;
using UnityEngine;

public static class AHFx
{
    static Material mat;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int ModeId = Shader.PropertyToID("_Mode");

    public static Material Mat
    {
        get
        {
            if (mat == null) mat = AHGame.LoadMat("AH/Materials/Fx", "AshenHollow/Fx");
            return mat;
        }
    }

    static GameObject Prim(PrimitiveType t, string name)
    {
        var go = AHLowPoly.Fix(GameObject.CreatePrimitive(t), t);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.Destroy(col);
        var r = go.GetComponent<Renderer>();
        if (Mat != null) r.sharedMaterial = Mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    public static void Paint(GameObject go, Color c, float mode)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor(ColorId, c);
        mpb.SetFloat(ModeId, mode);
        r.SetPropertyBlock(mpb);
    }

    // a flat ring that grows and fades (mode 1), or a soft disc (mode 2)
    public static GameObject Ring(Vector3 pos, float r0, float r1, Color c, float life, bool disc = false)
    {
        var go = Prim(PrimitiveType.Quad, disc ? "FxDisc" : "FxRing");
        go.transform.position = pos + Vector3.up * 0.06f;
        go.transform.rotation = Quaternion.Euler(90, 0, 0);
        Paint(go, c, disc ? 2f : 1f);
        var l = go.AddComponent<AHFxLife>();
        l.Setup(life, Vector3.one * r0 * 2f, Vector3.one * r1 * 2f, c, disc ? 2f : 1f);
        return go;
    }

    // a column of light (heals, buffs)
    public static GameObject Pillar(Vector3 pos, float radius, float height, Color c, float life)
    {
        var go = Prim(PrimitiveType.Cylinder, "FxPillar");
        go.transform.position = pos + Vector3.up * height * 0.5f;
        Paint(go, c, 0f);
        var l = go.AddComponent<AHFxLife>();
        l.Setup(life, new Vector3(radius * 2f, height * 0.5f, radius * 2f), new Vector3(radius * 0.6f, height * 0.6f, radius * 0.6f), c, 0f);
        return go;
    }

    // a quick glowing pop
    public static GameObject Pop(Vector3 pos, float size, Color c, float life = 0.35f)
    {
        var go = Prim(PrimitiveType.Sphere, "FxPop");
        go.transform.position = pos;
        Paint(go, c, 0f);
        var l = go.AddComponent<AHFxLife>();
        l.Setup(life, Vector3.one * size * 0.4f, Vector3.one * size, c, 0f);
        return go;
    }

    // an orb that does not fade on its own (shields, the target ring)
    public static GameObject Keep(PrimitiveType t, Color c, float mode)
    {
        var go = Prim(t, "FxKeep");
        Paint(go, c, mode);
        if (t == PrimitiveType.Quad) go.transform.rotation = Quaternion.Euler(90, 0, 0);
        return go;
    }

    // a homing shot from 'from' to the beast; onHit runs when it lands
    public static void Shoot(Vector3 from, AHMob target, Color c, float size, float speed, Action<AHMob> onHit)
    {
        var go = Prim(PrimitiveType.Sphere, "FxShot");
        go.transform.position = from;
        go.transform.localScale = Vector3.one * size;
        Paint(go, c, 0f);
        var s = go.AddComponent<AHShot>();
        s.target = target; s.speed = speed; s.onHit = onHit; s.color = c;
    }

    // a spell's shot shaped by its element: arrow spells fly as real crossbow bolts, frost as a long ice shard,
    // everything else as a glowing orb with a white-hot core
    static Material wood, steel, feather;
    static Material Lit(ref Material m, Color c, float smooth, float metal)
    {
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); }
        return m;
    }
    static GameObject Child(GameObject parent, PrimitiveType t, Vector3 pos, Vector3 scale, Vector3 rot)
    {
        var o = Prim(t, "ShotPart"); o.transform.SetParent(parent.transform, false);
        o.transform.localPosition = pos; o.transform.localScale = scale; o.transform.localRotation = Quaternion.Euler(rot);
        return o;
    }
    public static void Shoot(Vector3 from, AHMob target, Color c, float size, float speed, SpellEl el, Action<AHMob> onHit)
    {
        var go = new GameObject("FxShot"); go.transform.position = from;
        if (target != null) go.transform.rotation = Quaternion.LookRotation((target.transform.position + Vector3.up * 0.7f - from).normalized);
        var s = go.AddComponent<AHShot>(); s.target = target; s.speed = speed; s.onHit = onHit; s.color = c; s.orient = true;
        if (el == SpellEl.Arrow)
        {
            // shaft, steel head, two fletchings; a faint coloured streak follows it
            var sh = Child(go, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.035f, 0.32f, 0.035f), new Vector3(90, 0, 0));
            sh.GetComponent<Renderer>().sharedMaterial = Lit(ref wood, new Color(0.45f, 0.3f, 0.16f), 0.3f, 0f);
            var hd = Child(go, PrimitiveType.Cube, new Vector3(0, 0, 0.34f), new Vector3(0.07f, 0.07f, 0.14f), new Vector3(0, 0, 45));
            hd.GetComponent<Renderer>().sharedMaterial = Lit(ref steel, new Color(0.75f, 0.77f, 0.8f), 0.7f, 0.8f);
            foreach (float r in new[] { 0f, 90f })
            {
                var f = Child(go, PrimitiveType.Cube, new Vector3(0, 0, -0.26f), new Vector3(0.12f, 0.01f, 0.12f), new Vector3(0, 0, r));
                f.GetComponent<Renderer>().sharedMaterial = Lit(ref feather, new Color(0.85f, 0.2f, 0.15f), 0.2f, 0f);
            }
            s.trail = 0.35f;
        }
        else
        {
            bool shard = el == SpellEl.Frost;
            var orb = Child(go, PrimitiveType.Sphere, Vector3.zero, shard ? new Vector3(size * 0.7f, size * 0.7f, size * 2.4f) : Vector3.one * size, Vector3.zero);
            Paint(orb, c, 0f);
            var core = Child(go, PrimitiveType.Sphere, Vector3.zero, shard ? new Vector3(size * 0.35f, size * 0.35f, size * 1.8f) : Vector3.one * size * 0.5f, Vector3.zero);
            Paint(core, Color.Lerp(c, Color.white, 0.75f), 0f);
            s.trail = 1f; s.spin = el == SpellEl.Arcane || el == SpellEl.Star || el == SpellEl.Shadow;
        }
    }
}

public class AHFxLife : MonoBehaviour
{
    float life, t, mode;
    Vector3 s0, s1;
    Color c;

    public void Setup(float life, Vector3 s0, Vector3 s1, Color c, float mode)
    {
        this.life = Mathf.Max(0.05f, life); this.s0 = s0; this.s1 = s1; this.c = c; this.mode = mode;
        transform.localScale = s0;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        float e = 1f - (1f - k) * (1f - k);
        transform.localScale = Vector3.LerpUnclamped(s0, s1, e);
        Color cc = c; cc.a = c.a * (1f - k);
        AHFx.Paint(gameObject, cc, mode);
    }
}

public class AHShot : MonoBehaviour
{
    public AHMob target;
    public float speed = 18f;
    public Action<AHMob> onHit;
    public Color color;
    public bool orient, spin; public float trail = 1f;
    float age;

    void Update()
    {
        age += Time.deltaTime;
        if (target == null || target.dead || age > 3f) { Destroy(gameObject); return; }
        Vector3 aim = target.transform.position + Vector3.up * 0.7f;
        Vector3 d = aim - transform.position;
        float step = speed * Time.deltaTime;
        if (d.magnitude <= step + 0.2f)
        {
            AHFx.Pop(aim, 0.9f, color);
            AHSpark.Burst(aim, Color.Lerp(color, Color.white, 0.3f), 14, 4f, 0.4f, 0.13f);
            if (onHit != null) onHit(target);
            Destroy(gameObject);
            return;
        }
        transform.position += d.normalized * step;
        if (orient) transform.rotation = Quaternion.LookRotation(d.normalized) * (spin ? Quaternion.Euler(0, 0, age * 720f) : Quaternion.identity);
        float sz = orient ? 0.3f * trail : transform.localScale.x;
        if (trail > 0.5f || Time.frameCount % 2 == 0) AHSpark.Trail(transform.position, color, sz * 0.8f, trail > 0.5f ? 2 : 1);
        if (trail > 0.5f && Time.frameCount % 3 == 0) AHFx.Pop(transform.position, sz * 1.6f, new Color(color.r, color.g, color.b, 0.5f), 0.25f);
    }
}
