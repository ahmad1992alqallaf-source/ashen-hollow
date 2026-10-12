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
    float sx = 1f; Func<bool> moving; Transform hero; bool dragon;
    Transform b0, b1, b2; Quaternion r0, r1, r2;
    bool built; Vector3 lastPos; float lastYaw; Vector3 vel; float yawRate;
    float run, air, walk, stretchT = -1f, nextStretch, ph, breathe;
    // spring state for each joint: [flap, lift, fold] angles and their velocities
    readonly float[] ang = new float[9], spd = new float[9];
    public static float TestAir, TestRun; public static bool TestStretch;   // tests: force a flight, a run, a stretch

    public void Setup(float side, Func<bool> mv, Transform who, bool isDragon)
    {
        sx = side; moving = mv; hero = who; dragon = isDragon;
        nextStretch = Time.time + UnityEngine.Random.Range(5f, 10f); ph = UnityEngine.Random.value * 6.28f;
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
        b0 = new GameObject("WingShoulder").transform; b0.SetParent(transform, false);
        b1 = new GameObject("WingWrist").transform; b1.SetParent(b0, false); b1.localPosition = Vector3.Scale(e, new Vector3(span, Mathf.Abs(ps.y), Mathf.Abs(ps.z)));
        b2 = new GameObject("WingTip").transform; b2.SetParent(b1, false); b2.localPosition = Vector3.Scale(t - e, new Vector3(span, Mathf.Abs(ps.y), Mathf.Abs(ps.z)));
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
            var mats = mr.sharedMaterials; var shadow = mr.shadowCastingMode;
            DestroyImmediate(mr); DestroyImmediate(mf);
            var smr = p.gameObject.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = mesh; smr.bones = bones; smr.rootBone = b0; smr.sharedMaterials = mats;
            smr.updateWhenOffscreen = true; smr.shadowCastingMode = shadow;
        }
        if (hero != null) { lastPos = hero.position; lastYaw = hero.eulerAngles.y; }
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
        if (TestStretch && stretchT < 0f) stretchT = 0f;   // (the test turns it off again)
        walk = Mathf.Lerp(walk, mv && flat < 4.2f ? 1f : 0f, k);
        run = Mathf.Lerp(run, (mv && flat >= 4.2f) || TestRun > 0f ? 1f : 0f, k);
        air = Mathf.Lerp(air, Mathf.Abs(vel.y) > 2.6f ? 1f : 0f, 1f - Mathf.Exp(-dt * (Mathf.Abs(vel.y) > 2.6f ? 4f : 1.5f)));
        if (TestAir > 0f) air = Mathf.Lerp(air, 1f, 1f - Mathf.Exp(-dt * 4f));   // (a slope walked up is not flight)
        // a stretch now and then while standing
        float stretch = 0f;
        if (!mv && air < 0.2f && TestRun <= 0f)
        {
            if (stretchT < 0f && Time.time > nextStretch) stretchT = 0f;
            if (stretchT >= 0f)
            {
                stretchT += dt; const float dur = 3.4f;
                stretch = Mathf.Sin(Mathf.Clamp01(stretchT / dur) * Mathf.PI);
                if (stretchT > dur) { stretchT = -1f; nextStretch = Time.time + UnityEngine.Random.Range(9f, 16f); }
            }
        }
        else { stretchT = -1f; nextStretch = Mathf.Max(nextStretch, Time.time + 4f); }
        // the beat: quick while flying, slow beats inside a stretch, a slow breath otherwise
        float open = Mathf.Clamp01(Mathf.Max(air, Mathf.Max(stretch, run * 0.55f)));
        float rate = Mathf.Lerp(0.22f, 1.6f, air) + stretch * 0.45f + run * 0.5f;
        ph += dt * rate * 6.2832f;
        breathe += dt * 0.35f * 6.2832f;
        // downstroke quick, upstroke slow: a skewed wave
        Func<float, float> stroke = p => { float s = Mathf.Sin(p); return s > 0f ? Mathf.Pow(s, 0.7f) : -Mathf.Pow(-s, 1.4f) * 0.8f; };
        float beatAmp = air * 38f + stretch * 22f + run * 6f;
        float flap0 = stroke(ph) * beatAmp, flap1 = stroke(ph - 0.9f) * beatAmp * 0.7f, flap2 = stroke(ph - 1.8f) * beatAmp * 0.55f;
        // the wind of running lifts the trailing tips and makes them flutter
        float flutter = run * (Mathf.Sin(Time.time * 17f + sx) * 3f + Mathf.Sin(Time.time * 29f) * 1.5f);
        // the walk's rock, with the steps
        float rock = walk * Mathf.Sin(Time.time * 7.5f) * 3.5f;
        // lag: turning swings the wings the other way, speeding up sweeps them back
        float lag = Mathf.Clamp(-yawRate * 0.04f, -14f, 14f) * sx + flat * 1.2f;
        float br = Mathf.Sin(breathe) * 2.5f * (1f - open);
        // targets (degrees): flap (about the wing's up), lift (about its normal), fold (the wrist and tip folding down)
        float fold = Mathf.Lerp(1f, 0f, open);
        Spring(0, flap0 + lag * 0.5f + rock + fold * 22f, air > 0.3f ? 4.5f : 2.2f, dt);
        Spring(1, Mathf.Lerp(-10f, 12f, open) + br + rock * 0.5f, 2.0f, dt);
        Spring(2, 0f, 1f, dt);
        Spring(3, flap1 + lag * 0.4f, air > 0.3f ? 4f : 2.4f, dt);
        Spring(4, -fold * 48f + br * 0.6f + run * 10f, 2.2f, dt);
        Spring(5, 0f, 1f, dt);
        Spring(6, flap2 + lag * 0.3f + flutter, air > 0.3f ? 3.6f : 3.2f, dt);
        Spring(7, -fold * 34f + run * 8f + flutter * 0.4f, 2.6f, dt);
        Spring(8, 0f, 1f, dt);
        // apply, mirrored for the left wing (flap about the wing's up axis, lift and fold about its normal)
        b0.localRotation = r0 * Quaternion.Euler(0f, sx * ang[0], sx * ang[1]);
        b1.localRotation = r1 * Quaternion.Euler(0f, sx * ang[3], sx * ang[4]);
        b2.localRotation = r2 * Quaternion.Euler(0f, sx * ang[6], sx * ang[7]);
    }
}
