// Ashen Hollow: dust kicked up by a running mount. Soft clouds that swell, drift up and thin out (one particle system
// for the whole land), instead of the hard-edged balls they used to be.
using UnityEngine;

public static class AHDust
{
    static ParticleSystem ps; static Texture2D soft;

    static ParticleSystem Get()
    {
        if (ps != null) return ps;
        if (soft == null)
        {
            // a soft round puff with a little lumpiness
            const int N = 64; soft = new Texture2D(N, N, TextureFormat.RGBA32, false); soft.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / (N / 2f) - 1f, v = (y + 0.5f) / (N / 2f) - 1f, r = Mathf.Sqrt(u * u + v * v);
                    float lump = 0.85f + 0.15f * Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                    float a = Mathf.Clamp01(1f - r) * lump; a = a * a * (3f - 2f * a);
                    soft.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            soft.Apply();
        }
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var m = new Material(sh); m.SetTexture("_BaseMap", soft); m.SetColor("_BaseColor", Color.white);
        // see-through, blended softly (not cut out)
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_ZWrite", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
        var go = new GameObject("Mount dust");
        ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f); main.startSpeed = 0f; main.maxParticles = 300;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f); main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var em = ps.emission; em.rateOverTime = 0f;
        var shp = ps.shape; shp.enabled = false;
        var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2.2f));
        var col = ps.colorOverLifetime; col.enabled = true;
        var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.y = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);   // (all three the same kind of curve)
        var pr = go.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = m; pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false; pr.sortingFudge = 10f;
        ps.Play();
        return ps;
    }

    // a puff at 'at', tinted (sand, snow, earth), drifting back from the way the mount runs
    public static void Puff(Vector3 at, Vector3 back, Color c, float size = 1f)
    {
        var p = Get(); if (p == null) return;
        var e = new ParticleSystem.EmitParams { position = at, velocity = back * Random.Range(0.4f, 0.9f) + Vector3.up * 0.15f, startColor = c, startSize = Random.Range(0.5f, 0.9f) * size, applyShapeToPosition = false };
        p.Emit(e, 1);
    }
}

// the colour of the ground's dust in each land
public static class AHDustColour
{
    public static Color Of(string area)
    {
        switch (area)
        {
            case "sands": case "scorchwind": case "ss_city": case "isle": return new Color(0.86f, 0.74f, 0.55f, 0.55f);
            case "frost": case "whitepine": case "hc_city": return new Color(0.92f, 0.95f, 1f, 0.6f);
            case "ember": case "ashfall": case "ch_city": return new Color(0.35f, 0.32f, 0.3f, 0.55f);
            case "mire": case "causeway": case "fenwick": return new Color(0.45f, 0.42f, 0.32f, 0.45f);
            default: return new Color(0.62f, 0.55f, 0.42f, 0.45f);
        }
    }
}
