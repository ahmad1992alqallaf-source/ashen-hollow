// Ashen Hollow: cosmetic wings that move like living wings rather than a board turning on a hinge. Each wing gets a
// little skeleton (shoulder, elbow/wrist, and the fingers or primaries out to the tip) and its meshes are skinned to
// it, so it bends. Its motion is layered and springy:
//  - standing: folded against the back, breathing slowly, now and then a stretch (unfold, two slow beats, fold away)
//  - walking: held folded, rocking a little with the steps
//  - running: half open and swept back, the tips trailing and fluttering in the wind
//  - in the air (jumping, falling, a leap): spread wide and beating, the downstroke quick and the upstroke slow, the
//    bend travelling out along the wing (the wrist after the shoulder, the tip after the wrist)
//  - turning or stopping hard: the wings lag behind and settle
// Every joint follows its target through a soft spring, so nothing snaps from one pose to the next.
using System;
using System.Collections.Generic;
using UnityEngine;

public class AHWingRig : MonoBehaviour
{
    float sx = 1f; Func<bool> moving; Transform hero; bool dragon; AHPlayer rider;
    Transform b0, b1, b2; Quaternion r0, r1, r2;
    bool built; Vector3 lastPos; float lastYaw; Vector3 vel; float yawRate;
    float run, air, walk;
    // the pair's shared rhythm (one beat, one glide, one stretch for both wings; worked out once a frame)
    class Pace { public int frame = -1; public float stretchT = -1f, nextStretch, ph, breathe, glideT, nextGlide, glide, shakeT = -1f, nextShake, stretch, shake; }
    Pace P;
    // spring state for each joint: [flap, lift, fold] angles and their velocities
    readonly float[] ang = new float[9], spd = new float[9];
    public static float TestAir, TestRun; public static bool TestStretch;   // tests: force a flight, a run, a stretch
    // a modelled wing (the crest wings): its wrist and tip are found on the mesh rather than assumed
    public bool fromMesh;
    // how this kind of wing moves: "feather" (the default), "membrane" (bat and dragon wings: slow, deep, a long wave
    // through them), "crystal" (stiff, quick, with a shiver), "leaf" (soft and swaying)
    public string style = "feather";
    float seed, flare, prevAir;
    // for the wing's effects (AHWingFx): its bones, how it is moving, the beat, and points on its surface (each held by
    // the bone that carries it most, in that bone's space)
    public Transform Bone(int i) { return i == 0 ? b0 : i == 1 ? b1 : b2; }
    public float Air { get { return air; } }
    public float Run { get { return run; } }
    public float Open { get; private set; }
    public float Stroke { get; private set; }
    public int Beats { get; private set; }
    public readonly List<KeyValuePair<int, Vector3>> Samples = new List<KeyValuePair<int, Vector3>>();

    public void Setup(float side, Func<bool> mv, Transform who, bool isDragon)
    {
        sx = side; moving = mv; hero = who; dragon = isDragon;
        if (isDragon) style = "membrane";
        seed = UnityEngine.Random.Range(0f, 100f);
        // the other wing's rhythm, if it is already made
        if (transform.parent != null) foreach (var o in transform.parent.GetComponentsInChildren<AHWingRig>(true)) if (o != this && o.P != null) { P = o.P; break; }
        if (P == null) P = new Pace { nextStretch = Time.time + UnityEngine.Random.Range(5f, 10f), ph = UnityEngine.Random.value * 6.28f, nextShake = Time.time + UnityEngine.Random.Range(4f, 9f), nextGlide = Time.time + 2f };
    }

    // the joints, along the span (+x in the wing's own space, mirrored by the meshes' negative x scale on the left)
    Vector3 Elbow { get { return dragon ? new Vector3(0.42f, 0.34f, 0f) : new Vector3(0.45f, 0.2f * Mathf.Sin(0.45f * Mathf.PI * 0.75f) - 0.018f, 0f); } }
    Vector3 Tip { get { return dragon ? new Vector3(0.78f, 0.3f, -0.02f) : new Vector3(0.78f, 0.2f * Mathf.Sin(0.78f * Mathf.PI * 0.75f) - 0.03f, 0f); } }

    void Build()
    {
        built = true;
        var pieces = new List<Transform>();
        foreach (Transform c in transform) if (c.name == "Piece" && c.GetComponent<MeshFilter>() != null) pieces.Add(c);
        if (pieces.Count == 0) { enabled = false; return; }
        // the wing's size: the first piece's scale (all pieces share it), its sign giving the side
        Vector3 ps = pieces[0].localScale; float span = Mathf.Abs(ps.x);
        Vector3 e = Elbow, t = Tip; e.x *= Mathf.Sign(ps.x); t.x *= Mathf.Sign(ps.x);
        Vector3 ap = new Vector3(span, Mathf.Abs(ps.y), Mathf.Abs(ps.z));
        if (fromMesh)
        {
            // along the top (leading) edge of the wing: the wrist half way out, the tip near the end
            var mv = pieces[0].GetComponent<MeshFilter>().sharedMesh.vertices;
            e = Edge(mv, 0.45f); t = Edge(mv, 0.8f); ap = ps;
        }
        b0 = new GameObject("WingShoulder").transform; b0.SetParent(transform, false);
        b1 = new GameObject("WingWrist").transform; b1.SetParent(b0, false); b1.localPosition = Vector3.Scale(e, ap);
        b2 = new GameObject("WingTip").transform; b2.SetParent(b1, false); b2.localPosition = Vector3.Scale(t - e, ap);
        r0 = b0.localRotation; r1 = b1.localRotation; r2 = b2.localRotation;
        var bones = new[] { b0, b1, b2 };
        foreach (var p in pieces)
        {
            var mf = p.GetComponent<MeshFilter>(); var mr = p.GetComponent<MeshRenderer>(); var src = mf.sharedMesh; if (src == null) continue;
            var mesh = Instantiate(src); mesh.name = src.name + " (wing)";
            var vs = mesh.vertices; var bw = new BoneWeight[vs.Length];
            for (int i = 0; i < vs.Length; i++)
            {
                // how far out along the span (in the wing's own units, whatever side)
                float u = Mathf.Abs(vs[i].x);
                float w0 = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.42f, u));
                float w2 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.88f, u));
                float w1 = Mathf.Max(0f, 1f - w0 - w2);
                float s = w0 + w1 + w2;
                // Unity wants the weights heaviest first
                var ws = new[] { new KeyValuePair<int, float>(0, w0 / s), new KeyValuePair<int, float>(1, w1 / s), new KeyValuePair<int, float>(2, w2 / s) };
                System.Array.Sort(ws, (a, b) => b.Value.CompareTo(a.Value));
                bw[i] = new BoneWeight { boneIndex0 = ws[0].Key, weight0 = ws[0].Value, boneIndex1 = ws[1].Key, weight1 = ws[1].Value, boneIndex2 = ws[2].Key, weight2 = ws[2].Value };
            }
            mesh.boneWeights = bw;
            var bp = new Matrix4x4[3]; for (int i = 0; i < 3; i++) bp[i] = bones[i].worldToLocalMatrix * p.localToWorldMatrix;
            mesh.bindposes = bp;
            // points on it for the effects to rise from
            for (int n = 0; n < 48 && vs.Length > 0; n++)
            {
                int i = UnityEngine.Random.Range(0, vs.Length);
                Samples.Add(new KeyValuePair<int, Vector3>(bw[i].boneIndex0, bp[bw[i].boneIndex0].MultiplyPoint3x4(vs[i])));
            }
            var mats = mr.sharedMaterials; var shadow = mr.shadowCastingMode;
            DestroyImmediate(mr); DestroyImmediate(mf);
            var smr = p.gameObject.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = mesh; smr.bones = bones; smr.rootBone = b0; smr.sharedMaterials = mats;
            smr.updateWhenOffscreen = true; smr.shadowCastingMode = shadow;
        }
        if (hero != null) { lastPos = hero.position; lastYaw = hero.eulerAngles.y; }
    }

    // a point on the wing's top edge 'u' of the way out (mesh units; the side and the axes as the mesh has them)
    static Vector3 Edge(Vector3[] v, float u)
    {
        var ys = new List<float>(); float sx = 0f, n = 0f;
        foreach (var p in v) if (Mathf.Abs(Mathf.Abs(p.x) - u) < 0.05f) { ys.Add(p.y); sx += p.x; n++; }
        if (n < 3) return new Vector3(u, 0f, 0f);
        ys.Sort(); float top = ys[Mathf.Min(ys.Count - 1, (int)(ys.Count * 0.75f))];
        float z = 0f, m = 0f; foreach (var p in v) if (Mathf.Abs(Mathf.Abs(p.x) - u) < 0.05f && p.y > top - 0.08f) { z += p.z; m++; }
        return new Vector3(sx / n, top, m > 0 ? z / m : 0f);
    }

    // a soft spring toward the target (a touch under-damped: it settles with a small overshoot, like a real wing)
    void Spring(int i, float target, float freq, float dt)
    {
        float w = 6.2832f * freq, z = 0.62f;
        spd[i] += (-(2f * z * w) * spd[i] - w * w * (ang[i] - target)) * dt;
        ang[i] += spd[i] * dt;
    }

    void LateUpdate()
    {
        if (!built) { Build(); if (!enabled) return; }
        float dt = Mathf.Min(Time.deltaTime, 0.05f); if (dt <= 0f || b0 == null) return;
        // how the hero is moving
        if (hero != null)
        {
            Vector3 v = (hero.position - lastPos) / dt; lastPos = hero.position;
            vel = Vector3.Lerp(vel, v, 1f - Mathf.Exp(-dt * 8f));
            float y = hero.eulerAngles.y; yawRate = Mathf.Lerp(yawRate, Mathf.DeltaAngle(lastYaw, y) / dt, 1f - Mathf.Exp(-dt * 6f)); lastYaw = y;
        }
        float flat = new Vector2(vel.x, vel.z).magnitude;
        bool mv = moving != null && moving();
        float k = 1f - Mathf.Exp(-dt * 3f);
        // in the saddle the wings stay folded on the back (the mount's speed and bounce are not the hero running or
        // flying), so they do not spread across the mount or into a winged mount's own wings
        if (rider == null && hero != null) rider = hero.GetComponentInParent<AHPlayer>();
        bool riding = rider != null && rider.mounted;
        if (riding) { mv = true; flat = 0f; }
        walk = Mathf.Lerp(walk, mv && flat < 4.2f ? 1f : 0f, k);
        run = Mathf.Lerp(run, (mv && flat >= 4.2f) || TestRun > 0f ? 1f : 0f, k);
        air = Mathf.Lerp(air, !riding && Mathf.Abs(vel.y) > 2.6f ? 1f : 0f, 1f - Mathf.Exp(-dt * (Mathf.Abs(vel.y) > 2.6f ? 4f : 1.5f)));
        if (TestAir > 0f) air = Mathf.Lerp(air, 1f, 1f - Mathf.Exp(-dt * 4f));   // (a slope walked up is not flight)

        // ---- what makes it alive rather than a machine ----
        // each kind of wing has its own build
        float pRate = 1.6f, pAmp = 38f, pBend = 1f, pK = 1f, pShiver = 0f, pSway = 0f;
        switch (style)
        {
            case "membrane": pRate = 1.1f; pAmp = 46f; pBend = 1.35f; pK = 0.8f; break;
            case "crystal": pRate = 1.8f; pAmp = 30f; pBend = 0.6f; pK = 1.3f; pShiver = 1f; break;
            case "leaf": pRate = 1.3f; pAmp = 34f; pBend = 1.3f; pK = 0.75f; pSway = 1f; break;
        }
        float t = Time.time + seed;
        // slow wandering noise: each wing its own small wobbles (N); the pair's beat wanders together (Nb)
        Func<float, float, float> N = (f, o) => (Mathf.PerlinNoise(t * f, o + seed) - 0.5f) * 2f;
        Func<float, float, float> Nb = (f, o) => (Mathf.PerlinNoise(Time.time * f, o) - 0.5f) * 2f;
        float rateK = 1f + Nb(0.3f, 1f) * 0.12f, ampK = 1f + Nb(0.4f, 2f) * 0.15f;
        // the pair's rhythm, once a frame: stretches, ruffles, glides and the beat itself
        if (P.frame != Time.frameCount)
        {
            P.frame = Time.frameCount;
            if (TestStretch && P.stretchT < 0f) P.stretchT = 0f;   // (the test turns it off again)
            // a stretch now and then while standing
            P.stretch = 0f;
            if (!mv && air < 0.2f && TestRun <= 0f)
            {
                if (P.stretchT < 0f && Time.time > P.nextStretch) P.stretchT = 0f;
                if (P.stretchT >= 0f)
                {
                    P.stretchT += dt; const float dur = 3.4f;
                    P.stretch = Mathf.Sin(Mathf.Clamp01(P.stretchT / dur) * Mathf.PI);
                    if (P.stretchT > dur) { P.stretchT = -1f; P.nextStretch = Time.time + UnityEngine.Random.Range(9f, 16f); }
                }
            }
            else { P.stretchT = -1f; P.nextStretch = Mathf.Max(P.nextStretch, Time.time + 4f); }
            // in the air the beats are broken by glides: the wings held wide, riding the air, then beating again
            if (air > 0.5f)
            {
                if (P.glideT > 0f) P.glideT -= dt;
                else if (Time.time > P.nextGlide) { P.glideT = UnityEngine.Random.Range(0.9f, 1.8f); P.nextGlide = Time.time + P.glideT + UnityEngine.Random.Range(2.5f, 5f); }
            }
            else { P.glideT = 0f; P.nextGlide = Mathf.Max(P.nextGlide, Time.time + 1.5f); }
            P.glide = Mathf.Lerp(P.glide, P.glideT > 0f ? 1f : 0f, 1f - Mathf.Exp(-dt * 3f));
            // standing, now and then the feathers are shaken out (a quick ruffle that dies away)
            P.shake = 0f;
            if (!mv && air < 0.2f && P.stretchT < 0f && P.shakeT < 0f && Time.time > P.nextShake) P.shakeT = 0f;
            if (P.shakeT >= 0f)
            {
                P.shakeT += dt; const float sd = 0.75f;
                P.shake = Mathf.Sin(P.shakeT * 52f) * Mathf.Sin(Mathf.Clamp01(P.shakeT / sd) * Mathf.PI);
                if (P.shakeT > sd) { P.shakeT = -1f; P.nextShake = Time.time + UnityEngine.Random.Range(7f, 14f); }
            }
            float r0_ = Mathf.Lerp(0.22f, pRate, air) * rateK * (1f - P.glide * 0.85f) + P.stretch * 0.45f + run * 0.5f;
            P.ph += dt * r0_ * 6.2832f;
            P.breathe += dt * 0.35f * 6.2832f;
        }
        float stretch = P.stretch, glide = P.glide, shake = P.shake, ph = P.ph, breathe = P.breathe;
        // touching down: the wings flare open to brake, then settle
        if (prevAir > 0.6f && air < prevAir - 0.01f && Mathf.Abs(vel.y) < 1.2f) flare = 1f;
        flare = Mathf.MoveTowards(flare, 0f, dt * 1.3f); prevAir = air;

        // the beat: quick while flying, slow beats inside a stretch, a slow breath otherwise
        float open = Mathf.Clamp01(Mathf.Max(air, Mathf.Max(stretch, Mathf.Max(run * 0.55f, flare * 0.85f))));
        // downstroke quick, upstroke slow: a skewed wave
        Func<float, float> stroke = p => { float sn = Mathf.Sin(p); return sn > 0f ? Mathf.Pow(sn, 0.7f) : -Mathf.Pow(-sn, 1.4f) * 0.8f; };
        float beatAmp = (air * pAmp * (1f - glide * 0.85f) + stretch * 5f + run * 6f) * ampK;
        float st = stroke(ph); if (st > 0f && Stroke <= 0f) Beats++; Stroke = st; Open = open;
        // the beat is the wing rising and falling (seen from behind, as a real wing beats), with a smaller sweep forward
        // on the downstroke and back on the upstroke; the bend travels out along the wing, so the wrist follows the
        // shoulder and the tip follows the wrist: the tip trails as the wing rises and whips through as it comes down
        float wl = 0.9f * pBend;
        float s0 = st, s1 = stroke(ph - wl), s2 = stroke(ph - 2f * wl);
        float lift0 = -s0 * beatAmp, lift1 = -(s1 - s0) * beatAmp * 0.55f, lift2 = -(s2 - s1) * beatAmp * 0.45f;
        float swing0 = -s0 * beatAmp * 0.28f, swing1 = -(s1 - s0) * beatAmp * 0.18f, swing2 = -(s2 - s1) * beatAmp * 0.15f;
        // on the upstroke the outer wing half folds (less air against it); on the downstroke it spreads full
        float upF = air * (1f - glide) * Mathf.Clamp01(-Mathf.Sin(ph - wl * 0.5f));
        // and it turns about its length: the leading edge down as it beats down, up as it rises
        float twist = Mathf.Cos(ph) * beatAmp * 0.3f;
        // gliding: held wide in a shallow V, small steering corrections
        float gAdj = glide * N(0.8f, 5f) * 5f;
        // a living wing is never quite still
        float m0 = N(0.25f, 10f) * 2.2f, m1 = N(0.35f, 20f) * 2.6f, m2 = N(0.5f, 30f) * 3.4f;
        float shiver = pShiver * (Mathf.Sin(t * 41f) * 0.6f + Mathf.Sin(t * 67f) * 0.4f) * (0.5f + run + air * 1.5f + stretch * 1.5f);
        float sway = pSway * Mathf.Sin(t * 0.9f) * 3f;
        // the wind of running lifts the trailing tips and makes them flutter
        float flutter = run * (Mathf.Sin(t * 17f) * 3f + Mathf.Sin(t * 29f) * 1.5f);
        // the walk's rock, with the steps
        float rock = walk * Mathf.Sin(Time.time * 7.5f) * 3.5f;
        // lag: turning swings the wings the other way, speeding up sweeps them back
        float lag = Mathf.Clamp(-yawRate * 0.04f, -14f, 14f) * sx + flat * 1.2f;
        float br = Mathf.Sin(breathe) * 2.5f * (1f - open);
        // a stretch: the wings rise and reach forward, open full, quiver at the top, then settle back
        float reach = stretch * stretch;
        // targets (degrees): swing (about the wing's up: forward and back), lift (about its normal: up and down),
        // twist (about its length); folded at rest (swept back, the wrist and tip folded down)
        float fold = Mathf.Lerp(1f, 0f, open);
        float fa = air > 0.3f ? 4.5f : 2.2f;
        Spring(0, swing0 + lag * 0.5f + rock + fold * 22f - reach * 14f + m0 + gAdj, fa * pK, dt);
        Spring(1, lift0 + Mathf.Lerp(-10f, 8f, open) + air * 6f + reach * 22f + br + rock * 0.5f + m0 * 0.6f + glide * 7f + shake * 3f, fa * pK, dt);
        Spring(2, twist * 0.4f + N(0.3f, 40f) * 1.5f, 3f * pK, dt);
        Spring(3, swing1 + lag * 0.4f + m1 + sway * 0.5f, (air > 0.3f ? 4f : 2.4f) * pK, dt);
        Spring(4, lift1 - fold * 48f - upF * 16f + br * 0.6f + run * 10f + reach * 6f + m1 * 0.7f + shake * 6f + shiver * 0.4f, (air > 0.3f ? 4f : 2.2f) * pK, dt);
        Spring(5, twist * 0.7f, 3.4f * pK, dt);
        Spring(6, swing2 + lag * 0.3f + flutter + m2 + shiver + sway, (air > 0.3f ? 3.6f : 3.2f) * pK, dt);
        Spring(7, lift2 - fold * 34f - upF * 12f + run * 8f + flutter * 0.4f + shake * 9f + shiver * 0.6f + m2 * 0.5f, (air > 0.3f ? 3.8f : 2.6f) * pK, dt);
        Spring(8, twist + flutter * 0.5f, 3.8f * pK, dt);
        // apply, mirrored for the left wing (flap about the wing's up axis, lift and fold about its normal; the twist
        // about its length is the same both sides)
        b0.localRotation = r0 * Quaternion.Euler(ang[2], sx * ang[0], sx * ang[1]);
        b1.localRotation = r1 * Quaternion.Euler(ang[5], sx * ang[3], sx * ang[4]);
        b2.localRotation = r2 * Quaternion.Euler(ang[8], sx * ang[6], sx * ang[7]);
    }
}
