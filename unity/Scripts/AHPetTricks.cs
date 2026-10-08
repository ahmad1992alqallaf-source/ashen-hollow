// Ashen Hollow: pet tricks. Your pet learns a trick as it levels (Sit, Spin, Beg, Play dead, Hop, Dance, Backflip).
// Ask for one from the Emotes window (the "Pet tricks" row), or just stand about: a happy pet shows off on its own
// when you have been still for a while out of a fight. Hearts float up when it is done, and a trick now and then
// gives the pet a little experience.
// The moves are made by tilting, turning and lifting the pet's whole model, so every pet can do them (the flying
// ones too, in the air), whatever its skeleton.
using System.Collections.Generic;
using UnityEngine;

public static class AHPetTricks
{
    public class Trick { public string id, name, verb; public int lv; public float len; }
    public static readonly Trick[] All =
    {
        new Trick { id = "sit",   name = "Sit",       verb = "sits nicely",        lv = 1,  len = 2.4f },
        new Trick { id = "spin",  name = "Spin",      verb = "spins round",        lv = 3,  len = 1.2f },
        new Trick { id = "beg",   name = "Beg",       verb = "begs for a treat",   lv = 5,  len = 2.6f },
        new Trick { id = "dead",  name = "Play dead", verb = "plays dead",         lv = 8,  len = 3.2f },
        new Trick { id = "hop",   name = "Hop",       verb = "hops for joy",       lv = 11, len = 1.3f },
        new Trick { id = "dance", name = "Dance",     verb = "dances",             lv = 14, len = 3.4f },
        new Trick { id = "flip",  name = "Backflip",  verb = "does a backflip!",   lv = 18, len = 1.3f },
    };
    static float xpReady;

    public static List<Trick> Known(AHPlayer p)
    {
        var l = new List<Trick>(); if (p == null || p.pet == null) return l;
        int lv = AHComp.PetLv(p, p.pet); foreach (var t in All) if (lv >= t.lv) l.Add(t);
        return l;
    }
    public static Trick Next(AHPlayer p)
    {
        if (p == null || p.pet == null) return null; int lv = AHComp.PetLv(p, p.pet);
        foreach (var t in All) if (lv < t.lv) return t; return null;
    }
    public static Trick Find(string id) { foreach (var t in All) if (t.id == id) return t; return null; }

    // ask for a trick (null: one it knows, at random)
    public static void Do(AHGame g, string id)
    {
        var p = g != null ? g.player : null; if (p == null || p.pet == null) return;
        var known = Known(p); if (known.Count == 0) return;
        var t = id != null ? Find(id) : known[Random.Range(0, known.Count)];
        if (t == null || !known.Contains(t)) return;
        var f = Object.FindAnyObjectByType<AHPetFollow>(); if (f == null) return;
        f.StartTrick(t);
        AHChat.Add("system", AHComp.PetName(p.pet) + " " + t.verb + ".");
    }

    // a trick done: hearts, and now and then a little experience
    public static void Done(AHGame g, Vector3 at)
    {
        Hearts(at);
        if (Time.time >= xpReady) { xpReady = Time.time + 90f; AHComp.PetGain(g, 12f); }
    }

    // a few small hearts rising and fading
    static Material heartMat;
    public static void Hearts(Vector3 at)
    {
        var go = new GameObject("PetHearts"); go.transform.position = at;
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = false; main.duration = 0.4f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f); main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.35f, 0.5f), new Color(1f, 0.6f, 0.72f));
        main.gravityModifier = -0.15f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.playOnAwake = false;
        var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 7) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.25f; sh.rotation = new Vector3(-90f, 0f, 0f);
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
        var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Billboard;
        if (heartMat == null)
        {
            var s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s != null) { heartMat = new Material(s); heartMat.SetColor("_BaseColor", Color.white); heartMat.SetTexture("_BaseMap", Heart()); heartMat.SetFloat("_AlphaClip", 1f); heartMat.SetFloat("_Cutoff", 0.4f); heartMat.EnableKeyword("_ALPHATEST_ON"); heartMat.SetFloat("_Cull", 0f); }
        }
        if (heartMat != null) r.sharedMaterial = heartMat;
        ps.Play(); Object.Destroy(go, 2.5f);
    }
    static Texture2D Heart()
    {
        const int N = 32; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float u = ((x + 0.5f) / N * 2f - 1f) * 1.25f, v = ((y + 0.5f) / N * 2f - 1f) * 1.25f + 0.25f;
            float a = u * u + v * v - 1f; float f = a * a * a - u * u * v * v * v;   // the heart curve
            t.SetPixel(x, y, new Color(1f, 1f, 1f, f <= 0f ? 1f : 0f));
        }
        t.Apply(); return t;
    }

    // the move itself: how far up, and how turned, the model is at time u (0..1) of the trick
    public static void Pose(Trick t, float u, float time, float h, out Vector3 lift, out Vector3 euler)
    {
        float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.15f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.15f));   // into it and out of it
        float s = Mathf.SmoothStep(0f, 1f, u);
        lift = Vector3.zero; euler = Vector3.zero;
        switch (t.id)
        {
            case "sit": euler.x = -24f * e; lift.y = -0.03f * e; break;
            case "spin": euler.y = 360f * s; lift.y = Mathf.Sin(u * Mathf.PI) * 0.12f; break;
            case "beg": euler.x = -52f * e; lift.y = (0.06f + Mathf.Abs(Mathf.Sin(time * 9f)) * 0.04f) * e; break;
            case "dead": euler.z = 88f * e; lift.y = h * 0.16f * e + (u > 0.8f ? Mathf.Sin((u - 0.8f) / 0.2f * Mathf.PI) * 0.25f : 0f); break;
            case "hop": { float k = Mathf.Repeat(u * 2f, 1f); lift.y = Mathf.Sin(k * Mathf.PI) * 0.55f; euler.y = 30f * Mathf.Sin(u * Mathf.PI * 2f); break; }
            case "dance": euler.y = Mathf.Sin(time * 8f) * 28f * e; euler.z = Mathf.Sin(time * 8f + 1.2f) * 12f * e; lift.y = Mathf.Abs(Mathf.Sin(time * 8f)) * 0.14f * e; break;
            case "flip": lift.y = Mathf.Sin(u * Mathf.PI) * 0.95f; euler.x = -360f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.85f, u)); break;
        }
    }
}
