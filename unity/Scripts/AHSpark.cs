// Ashen Hollow: sparks. One shared particle system draws the glowing motes that give each kind of spell its own
// signature: trails behind bolts, a slash of sparks for strikes, a ring blown outward by novas, embers falling on a
// ground blast, motes spiralling up around a heal, smoke for a vanish, sparks on every hit and a golden fountain on
// a level-up. The older rings, discs and pillars (AHFx) stay; these are layered on top.
using UnityEngine;

public static class AHSpark
{
    static ParticleSystem ps;
    static ParticleSystem.EmitParams ep;

    static ParticleSystem PS
    {
        get
        {
            if (ps != null) return ps;
            var mat = AHGame.LoadMat("AH/Materials/Spark", "AshenHollow/Spark");
            if (mat == null) mat = AHFx.Mat;
            if (mat == null) return null;
            var go = new GameObject("Sparks");
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = 4000; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f; main.startLifetime = 1f; main.gravityModifier = 0f; main.scalingMode = ParticleSystemScalingMode.Shape;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.limit = 40f; lv.drag = 1.6f; lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = false;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.15f, 1f), new Keyframe(1, 0f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = gr;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.minParticleSize = 0f; r.maxParticleSize = 0.5f;
            ps.Play();
            return ps;
        }
    }

    public static void Emit(Vector3 pos, Vector3 vel, float size, float life, Color c)
    {
        var s = PS; if (s == null) return;
        ep.position = pos; ep.velocity = vel; ep.startSize = size * 1.7f; ep.startLifetime = life * 1.2f; ep.startColor = c; ep.applyShapeToPosition = false;
        s.Emit(ep, 1);
    }
    static float R(float a, float b) { return Random.Range(a, b); }

    // all directions
    public static void Burst(Vector3 pos, Color c, int n, float speed, float life = 0.6f, float size = 0.16f, float up = 0f)
    {
        for (int i = 0; i < n; i++) Emit(pos, Random.onUnitSphere * speed * R(0.4f, 1f) + Vector3.up * up, size * R(0.6f, 1.3f), life * R(0.6f, 1.2f), c);
    }
    // flat and outward, like a shock wave
    public static void Ring(Vector3 pos, Color c, int n, float speed, float life = 0.55f, float size = 0.18f)
    {
        for (int i = 0; i < n; i++) { float a = (i + R(0f, 0.6f)) * Mathf.PI * 2f / n; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); Emit(pos + d * 0.3f, d * speed * R(0.8f, 1.1f) + Vector3.up * R(0f, 0.8f), size * R(0.7f, 1.2f), life * R(0.8f, 1.1f), c); }
    }
    // motes rising out of a disc on the ground
    public static void Rise(Vector3 pos, float radius, Color c, int n, float speed = 2.2f, float life = 1f, float size = 0.14f)
    {
        for (int i = 0; i < n; i++) { var o = Random.insideUnitCircle * radius; Emit(pos + new Vector3(o.x, R(0f, 0.4f), o.y), new Vector3(0f, speed * R(0.6f, 1.2f), 0f), size * R(0.6f, 1.3f), life * R(0.7f, 1.2f), c); }
    }
    // a spiral climbing around a point (heals, buffs)
    public static void Spiral(Vector3 pos, float radius, Color c, int n, float height = 2.2f)
    {
        for (int i = 0; i < n; i++)
        {
            float a = i * 0.9f, h = (float)i / n;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var tng = new Vector3(-d.z, 0f, d.x);
            Emit(pos + d * radius + Vector3.up * h * 0.6f, Vector3.up * height * R(0.8f, 1.2f) + tng * 1.6f, 0.15f * R(0.7f, 1.2f), 0.9f * R(0.8f, 1.2f), c);
        }
    }
    // a sweep of sparks in front (slashes)
    public static void Arc(Vector3 pos, Vector3 fwd, Color c, int n, float radius, float spread = 70f)
    {
        fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; fwd.Normalize();
        for (int i = 0; i < n; i++)
        {
            float ang = Mathf.Lerp(-spread, spread, (i + R(0f, 1f)) / n);
            var d = Quaternion.Euler(0f, ang, 0f) * fwd;
            Emit(pos + d * radius * R(0.6f, 1f) + Vector3.up * R(-0.2f, 0.3f), d * R(2f, 5f) + Vector3.up * R(0f, 1.5f), 0.15f * R(0.7f, 1.3f), 0.4f * R(0.7f, 1.2f), c);
        }
    }
    // embers falling onto a spot (ground blasts), timed to land after 'time' seconds
    public static void Fall(Vector3 pos, float radius, Color c, int n, float time = 0.8f, float height = 7f)
    {
        for (int i = 0; i < n; i++) { var o = Random.insideUnitCircle * radius * 0.6f; float h = height * R(0.8f, 1.2f); Emit(pos + new Vector3(o.x, h, o.y), new Vector3(0f, -h / time, 0f), 0.2f * R(0.7f, 1.3f), time * R(0.9f, 1.05f), c); }
    }
    // a few motes left behind something moving (bolts, dashes)
    public static void Trail(Vector3 pos, Color c, float size = 0.14f, int n = 2)
    {
        for (int i = 0; i < n; i++) Emit(pos + Random.insideUnitSphere * size * 0.6f, Random.insideUnitSphere * 0.6f, size * R(0.6f, 1.2f), R(0.2f, 0.4f), c);
    }

    static Color Hot(Color c) { return Color.Lerp(c, Color.white, 0.25f); }

    // the signature of a spell, by its kind (called right after it is cast)
    public static void ForSpell(AHPlayer p, SpellDef sp, AHMob t)
    {
        if (p == null || sp == null) return;
        Vector3 me = p.transform.position, chest = me + Vector3.up * 1.1f; Color c = sp.color, h = Hot(c);
        Vector3 tp = t != null ? t.transform.position + Vector3.up * 0.8f : chest + p.transform.forward * 2f;
        switch (sp.kind)
        {
            case SpellKind.Strike: Burst(tp, h, 18, 4.5f, 0.45f, 0.14f); Arc(tp, p.transform.forward, c, 10, 0.6f, 50f); break;
            case SpellKind.Arc: Arc(chest - Vector3.up * 0.2f, p.transform.forward, h, 26, sp.radius * 0.8f, 75f); break;
            case SpellKind.Nova: Ring(me + Vector3.up * 0.4f, h, 48, sp.radius * 2.2f, 0.5f, 0.2f); Burst(chest, c, 16, 3f, 0.5f); break;
            case SpellKind.Whirl: Ring(me + Vector3.up * 0.9f, h, 22, 5f, 0.35f); break;
            case SpellKind.Bolt: case SpellKind.Multi: Burst(p.Hand, h, 10, 2.5f, 0.3f, 0.12f); break;
            case SpellKind.GroundAt: Rise(tp - Vector3.up * 0.8f, sp.radius * 0.8f, c, 24, 1.2f, 0.8f, 0.12f); Fall(tp - Vector3.up * 0.8f, sp.radius, h, 30, 0.8f); break;
            case SpellKind.Consecrate: Rise(me, sp.radius, c, 40, 1.6f, 1.4f, 0.14f); break;
            case SpellKind.Charge: case SpellKind.Leap: for (int i = 0; i < 12; i++) Trail(me + Vector3.up * R(0.3f, 1.4f) + Random.insideUnitSphere * 0.4f, c, 0.18f, 1); Ring(me + Vector3.up * 0.2f, c, 18, 4f, 0.4f); break;
            case SpellKind.Blink: case SpellKind.StepBehind: Burst(chest, h, 26, 3.5f, 0.45f, 0.14f); break;
            case SpellKind.Heal: case SpellKind.Hot: Spiral(me, 0.75f, h, 34); Rise(me, 0.9f, c, 16, 2.4f, 1.1f); break;
            case SpellKind.Shield: case SpellKind.Invuln: Burst(chest, h, 30, 2.2f, 0.7f, 0.15f); Spiral(me, 0.9f, c, 20, 1.6f); break;
            case SpellKind.Buff: Ring(me + Vector3.up * 0.2f, h, 30, 4.5f, 0.5f); Rise(me, 0.7f, c, 20, 3.2f, 0.8f); break;
            case SpellKind.Stealth: Burst(chest, new Color(0.35f, 0.28f, 0.45f, 0.8f), 40, 1.6f, 0.9f, 0.3f, 0.6f); break;
            case SpellKind.Bear: Burst(chest, h, 40, 3.5f, 0.6f, 0.2f, 1f); break;
            default: Burst(chest, h, 16, 3f, 0.5f); break;
        }
    }

    public static void LevelUp(Vector3 at) { Rise(at, 0.9f, new Color(1f, 0.82f, 0.35f), 60, 3.6f, 1.4f, 0.16f); Ring(at + Vector3.up * 0.2f, new Color(1f, 0.9f, 0.55f), 40, 5f, 0.6f); }
}
