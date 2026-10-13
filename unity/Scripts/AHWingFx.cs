// Ashen Hollow: the crest wings' own magic, rising off the feathers as they move (through the shared spark system,
// AHSpark). Each kind has its look, and all of it grows when the hero runs and more when the wings beat in the air; a
// downstroke throws off a burst.
//  - ember (Emberplume): embers drifting up from the plumes, sparks shaken out on each beat
//  - frost (Glacierfan): snow sifting down, a glitter of frost on each beat
//  - storm (Thunderfork): crackling sparks, and now and then a fork of lightning jumping across the wing
//  - nature (Grovebloom): leaves and the odd blossom drifting down, swaying as they fall
//  - shadow (Nightveil): violet smoke curling off the membrane and trailing behind
//  - celestial (Dawnfeather): golden motes rising, and a shimmer of light on each beat
using UnityEngine;

public class AHWingFx : MonoBehaviour
{
    public string kind = "ember";
    AHWingRig rig; float acc; int beats; LineRenderer bolt; float boltOff, nextBolt;
    static Material boltMat;

    void Start() { rig = GetComponent<AHWingRig>(); nextBolt = Time.time + Random.Range(1f, 3f); }

    static float R(float a, float b) { return Random.Range(a, b); }
    Vector3 P()
    {
        var s = rig.Samples[Random.Range(0, rig.Samples.Count)]; var b = rig.Bone(s.Key);
        return b != null ? b.TransformPoint(s.Value) : transform.position;
    }
    static Color Mix(Color a, Color b) { return Color.Lerp(a, b, Random.value); }

    void LateUpdate()
    {
        if (rig == null || rig.Samples.Count == 0 || rig.Bone(0) == null) return;
        // nothing while the wing is hidden (the look editor's pages, a costume, the hero not drawn)
        var r = GetComponentInChildren<SkinnedMeshRenderer>(); if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) return;
        float dt = Time.deltaTime; if (dt <= 0f) return;
        float act = 1f + rig.Run * 1.6f + rig.Air * 3f + rig.Open * 0.8f;
        acc += Rate * act * dt;
        int guard = 0; while (acc >= 1f && guard++ < 20) { acc -= 1f; One(false); }
        if (rig.Beats != beats) { beats = rig.Beats; if (rig.Air > 0.3f || rig.Open > 0.5f) for (int i = 0; i < Burst; i++) One(true); }
        if (kind == "storm") Lightning(dt);
        Trail();
    }

    // a streak of the wing's own colour off its tip while it sweeps through the air (running, flying, a stretch)
    TrailRenderer trail;
    void Trail()
    {
        if (trail == null)
        {
            var mat = AHGame.LoadMat("AH/Materials/Spark", "AshenHollow/Spark"); if (mat == null) return;
            var go = new GameObject("WingTrail"); go.transform.SetParent(rig.Bone(2), false);
            trail = go.AddComponent<TrailRenderer>(); trail.sharedMaterial = mat; trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false; trail.minVertexDistance = 0.04f;
            Color a, b; float w = 0.1f, life = 0.35f;
            switch (kind)
            {
                case "ember": a = new Color(1.9f, 0.8f, 0.2f, 1f); b = new Color(1.4f, 0.25f, 0.05f, 0f); break;
                case "frost": a = new Color(1.3f, 1.7f, 2.2f, 0.9f); b = new Color(0.8f, 1.2f, 2f, 0f); life = 0.45f; break;
                case "storm": a = new Color(1.5f, 2.3f, 3.2f, 1f); b = new Color(0.6f, 1.2f, 2.4f, 0f); w = 0.06f; life = 0.22f; break;
                case "nature": a = new Color(0.6f, 1.2f, 0.4f, 0.7f); b = new Color(0.4f, 0.9f, 0.3f, 0f); w = 0.08f; break;
                case "shadow": a = new Color(0.6f, 0.25f, 1.1f, 0.9f); b = new Color(0.25f, 0.08f, 0.5f, 0f); w = 0.16f; life = 0.5f; break;
                default: a = new Color(1.9f, 1.6f, 0.9f, 1f); b = new Color(1.6f, 1.3f, 0.6f, 0f); break;
            }
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) }, new[] { new GradientAlphaKey(a.a, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g; trail.time = life;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, w), new Keyframe(1f, 0f));
        }
        trail.emitting = rig.Air > 0.35f || rig.Run > 0.5f || rig.Open > 0.6f;
    }

    float Rate { get { switch (kind) { case "ember": return 11f; case "frost": return 8f; case "storm": return 7f; case "nature": return 3.5f; case "shadow": return 10f; default: return 8f; } } }
    int Burst { get { switch (kind) { case "ember": return 10; case "storm": return 8; case "nature": return 4; case "shadow": return 6; default: return 8; } } }

    void One(bool beat)
    {
        Vector3 p = P(), out_ = (p - rig.Bone(0).position); out_.y = 0f; out_ = out_.sqrMagnitude > 1e-4f ? out_.normalized : Vector3.zero;
        Vector3 back = -rig.transform.root.forward;
        switch (kind)
        {
            case "ember":
                if (beat) AHSpark.Emit(p, out_ * R(0.8f, 2f) + Random.onUnitSphere * 0.6f + Vector3.up * 0.4f, R(0.04f, 0.07f), R(0.35f, 0.7f), Mix(new Color(2f, 0.85f, 0.2f), new Color(1.7f, 0.42f, 0.08f)));
                else AHSpark.Emit(p, new Vector3(R(-0.15f, 0.15f), R(0.5f, 1.2f), R(-0.15f, 0.15f)), R(0.04f, 0.085f), R(0.8f, 1.5f), Mix(new Color(1.8f, 0.7f, 0.15f), new Color(1.5f, 0.32f, 0.06f)));
                break;
            case "frost":
                if (beat) AHSpark.Emit(p, Random.onUnitSphere * R(0.6f, 1.4f), R(0.03f, 0.05f), R(0.4f, 0.7f), Mix(new Color(2.2f, 2.4f, 2.8f), new Color(1.2f, 1.8f, 2.8f)));
                else AHSpark.Emit(p, new Vector3(R(-0.12f, 0.12f), R(-0.45f, -0.2f), R(-0.12f, 0.12f)), R(0.035f, 0.07f), R(1.4f, 2.2f), Mix(new Color(1.3f, 1.7f, 2.4f), new Color(1.9f, 2f, 2.3f)));
                break;
            case "storm":
                AHSpark.Emit(p, Random.onUnitSphere * R(beat ? 1.5f : 0.6f, beat ? 3f : 1.6f), R(0.025f, 0.05f), R(0.12f, 0.3f), Mix(new Color(1.2f, 2.3f, 3.4f), new Color(2.6f, 2.8f, 3.2f)));
                break;
            case "nature":
                {
                    bool bloom = Random.value < 0.18f;
                    Color c = bloom ? Mix(new Color(1.7f, 0.85f, 1.15f), new Color(1.9f, 1.6f, 1.7f)) : Mix(new Color(0.45f, 1.15f, 0.3f), new Color(0.95f, 1.3f, 0.35f));
                    AHSpark.Emit(p, new Vector3(R(-0.35f, 0.35f), beat ? R(-0.1f, 0.4f) : R(-0.35f, -0.15f), R(-0.35f, 0.35f)) + (beat ? out_ * 0.8f : Vector3.zero), R(0.05f, 0.085f), R(1.8f, 2.8f), c);
                }
                break;
            case "shadow":
                AHSpark.Emit(p, back * R(0.15f, 0.45f) + new Vector3(R(-0.1f, 0.1f), R(-0.05f, 0.25f), R(-0.1f, 0.1f)) + (beat ? out_ * R(0.6f, 1.2f) : Vector3.zero), R(0.08f, 0.15f), R(0.9f, 1.6f), Mix(new Color(0.75f, 0.3f, 1.4f), new Color(0.35f, 0.12f, 0.75f)));
                break;
            default:   // celestial
                if (beat) AHSpark.Emit(p, out_ * R(0.5f, 1.2f) + Vector3.up * R(0.2f, 0.6f), R(0.04f, 0.07f), R(0.6f, 1f), Mix(new Color(2f, 1.75f, 1.1f), new Color(1.9f, 1.4f, 0.5f)));
                else AHSpark.Emit(p, new Vector3(R(-0.08f, 0.08f), R(0.15f, 0.45f), R(-0.08f, 0.08f)), R(0.035f, 0.065f), R(1.4f, 2.2f), Mix(new Color(1.8f, 1.4f, 0.55f), new Color(1.8f, 1.75f, 1.4f)));
                break;
        }
    }

    // a fork of lightning jumping between two points of the wing: a jagged line, there for a blink
    void Lightning(float dt)
    {
        if (bolt == null)
        {
            var go = new GameObject("Lightning"); go.transform.SetParent(transform, false);
            bolt = go.AddComponent<LineRenderer>(); bolt.positionCount = 8; bolt.useWorldSpace = true; bolt.widthMultiplier = 0.022f;
            bolt.numCapVertices = 2; bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; bolt.receiveShadows = false;
            if (boltMat == null) { boltMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); boltMat.SetColor("_BaseColor", new Color(1.6f, 2.6f, 4f)); }
            bolt.sharedMaterial = boltMat; bolt.enabled = false;
        }
        if (bolt.enabled) { boltOff -= dt; if (boltOff <= 0f) bolt.enabled = false; return; }
        if (Time.time < nextBolt) return;
        nextBolt = Time.time + (rig.Air > 0.3f ? R(0.25f, 0.8f) : rig.Run > 0.3f ? R(0.7f, 1.6f) : R(1.4f, 3.6f));
        Vector3 a = P(), b = P(); if ((a - b).sqrMagnitude < 0.04f) b = rig.Bone(2).position;
        Vector3 side = Vector3.Cross(b - a, Vector3.up).normalized; float len = (b - a).magnitude;
        for (int i = 0; i < 8; i++)
        {
            float u = i / 7f; Vector3 q = Vector3.Lerp(a, b, u);
            if (i > 0 && i < 7) q += (side * R(-1f, 1f) + Vector3.up * R(-1f, 1f)) * len * 0.12f;
            bolt.SetPosition(i, q);
        }
        bolt.widthMultiplier = R(0.015f, 0.03f); bolt.enabled = true; boltOff = R(0.06f, 0.12f);
        AHSpark.Burst(b, new Color(1.6f, 2.6f, 3.6f), 6, 1.6f, 0.25f, 0.05f);
    }
}
