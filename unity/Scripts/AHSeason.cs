// Ashen Hollow: the seasons, by the real calendar. In the open lands: falling leaves in autumn (September to November),
// snow in winter (December to February), drifting blossom in spring (March to May) and fireflies on summer nights
// (June to August). Never underground. The chat says which season it is when you arrive somewhere.
using UnityEngine;

public class AHSeason : MonoBehaviour
{
    public static string Now { get { int m = System.DateTime.Now.Month; return m == 12 || m <= 2 ? "winter" : m <= 5 ? "spring" : m <= 8 ? "summer" : "autumn"; } }
    public static string Name { get { switch (Now) { case "winter": return "Winter"; case "spring": return "Spring"; case "summer": return "Summer"; default: return "Autumn"; } } }
    AHGame g; ParticleSystem ps; bool nightOnly;

    public static void Setup(AHGame g)
    {
        string a = AHGame.AreaId;
        if (AHDungeon.IsDungeon(a) || a == AHDeep.Area || a == "raid" || a == "tutorial") return;
        var s = new GameObject("Season").AddComponent<AHSeason>(); s.g = g; s.Build();
        string line = Now == "autumn" ? "Autumn in the Hollow: the leaves are turning and falling." : Now == "winter" ? "Winter in the Hollow: snow on the wind." : Now == "spring" ? "Spring in the Hollow: blossom on the air." : "Summer in the Hollow: fireflies come out at night.";
        AHChat.Add("system", line);
    }

    void Build()
    {
        ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 500; main.playOnAwake = true;
        var em = ps.emission; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(34f, 1f, 34f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        var rot = ps.rotationOverLifetime; var noise = ps.noise;
        Color a, b;
        switch (Now)
        {
            case "winter":
                a = new Color(1f, 1f, 1f, 0.9f); b = new Color(0.85f, 0.9f, 1f, 0.8f);
                main.startLifetime = 9f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f); main.startSpeed = 0f; main.gravityModifier = 0.03f;
                em.rateOverTime = 70f; vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f); vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.8f); vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
                noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.3f;
                break;
            case "spring":
                a = new Color(1f, 0.72f, 0.85f, 0.95f); b = new Color(1f, 0.92f, 0.95f, 0.9f);
                main.startLifetime = 10f; main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f); main.startSpeed = 0f;
                em.rateOverTime = 22f; vel.x = new ParticleSystem.MinMaxCurve(0.4f, 1.1f); vel.y = new ParticleSystem.MinMaxCurve(-0.7f, -0.4f); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f); noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.25f;
                break;
            case "summer":
                a = new Color(1f, 0.95f, 0.45f, 1f); b = new Color(0.8f, 1f, 0.4f, 1f); nightOnly = true;
                main.startLifetime = 5f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f); main.startSpeed = 0f;
                em.rateOverTime = 14f; sh.scale = new Vector3(26f, 3f, 26f); vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.15f); vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                noise.enabled = true; noise.strength = 0.8f; noise.frequency = 0.5f;
                var col = ps.colorOverLifetime; col.enabled = true; var grad = new Gradient();
                grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.2f, 0.6f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
                col.color = grad;
                break;
            default:   // autumn leaves
                a = new Color(0.95f, 0.45f, 0.12f, 1f); b = new Color(0.85f, 0.7f, 0.15f, 1f);
                main.startLifetime = 9f; main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f); main.startSpeed = 0f;
                em.rateOverTime = 26f; vel.x = new ParticleSystem.MinMaxCurve(0.3f, 1f); vel.y = new ParticleSystem.MinMaxCurve(-1.1f, -0.6f); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f); noise.enabled = true; noise.strength = 0.7f; noise.frequency = 0.3f;
                break;
        }
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var r = GetComponent<ParticleSystemRenderer>();
        var sp = Shader.Find("Sprites/Default");
        var sh2 = sp != null ? null : Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sp != null) { var m = new Material(sp); m.mainTexture = Dot(Now == "autumn" || Now == "spring"); r.sharedMaterial = m; }
        else if (sh2 != null)
        {
            var m = new Material(sh2); m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BaseMap", Dot(Now == "autumn" || Now == "spring"));
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
            r.sharedMaterial = m;
        }
        r.renderMode = ParticleSystemRenderMode.Billboard;
        ps.Play();
    }

    // a soft round dot (snow, fireflies) or a leaf/petal shape, drawn once
    static Texture2D Dot(bool leaf)
    {
        const int N = 32; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f, a;
            if (leaf) { float w = 0.55f * (1f - v * v); a = Mathf.Clamp01((w - Mathf.Abs(u)) * 8f) * Mathf.Clamp01((1f - Mathf.Abs(v)) * 6f); if (Mathf.Abs(u) < 0.05f) a *= 0.7f; }
            else { float d = Mathf.Sqrt(u * u + v * v); a = Mathf.Clamp01(1f - d); a *= a; }
            t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        t.Apply(); return t;
    }

    void LateUpdate()
    {
        if (g == null || g.player == null || ps == null) return;
        transform.position = g.player.transform.position + Vector3.up * (nightOnly ? 1.5f : 11f);
        if (nightOnly)
        {
            var em = ps.emission; em.enabled = g.IsNight;
        }
    }
}
