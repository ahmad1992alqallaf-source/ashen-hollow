// Ashen Hollow: the look of the picture. A global post-processing volume built in code: a neutral tone curve (so
// bright skies and spells roll off softly instead of clipping), a gentle bloom on things that really glow, a little
// more contrast and colour, cooler shadows and warmer highlights, and a soft vignette. Lands can tint it (a dungeon
// colder, the ember lands warmer). It follows the "Glow and colour" setting (off on the lowest quality).
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AHGrade
{
    public static Volume V;
    static VolumeProfile prof; static ColorAdjustments ca; static ShadowsMidtonesHighlights smh; static Bloom bloom; static Vignette vig; static LiftGammaGain lgg;

    public static void Setup(AHGame g)
    {
        if (V == null)
        {
            var go = new GameObject("AH Grade"); Object.DontDestroyOnLoad(go);
            V = go.AddComponent<Volume>(); V.isGlobal = true; V.priority = 50f;
            prof = ScriptableObject.CreateInstance<VolumeProfile>(); V.sharedProfile = prof;
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
            bloom = prof.Add<Bloom>(true); bloom.threshold.Override(1.0f); bloom.intensity.Override(0.5f); bloom.scatter.Override(0.62f); bloom.highQualityFiltering.Override(false);
            ca = prof.Add<ColorAdjustments>(true); ca.postExposure.Override(0.12f); ca.contrast.Override(16f); ca.saturation.Override(14f);
            smh = prof.Add<ShadowsMidtonesHighlights>(true);
            lgg = prof.Add<LiftGammaGain>(true); lgg.lift.Override(new Vector4(1f, 1f, 1.02f, -0.06f)); lgg.gamma.Override(new Vector4(1f, 1f, 1f, -0.03f));   // deeper blacks: the haze lifts off the picture
            vig = prof.Add<Vignette>(true); vig.intensity.Override(0.24f); vig.smoothness.Override(0.42f); vig.color.Override(new Color(0.05f, 0.03f, 0.06f));
        }
        Mood(AHGame.AreaId);
        V.weight = 1f;
    }

    // a land's own light: cool and dim below ground, warm in the ember lands, crisp in the frost
    static void Mood(string area)
    {
        if (smh == null) return;
        Vector4 sh = new Vector4(0.96f, 0.98f, 1.06f, -0.03f), hi = new Vector4(1.05f, 1.01f, 0.95f, 0.02f);   // cool shadows, warm highlights
        float sat = 22f, con = 24f, exp = 0.1f;
        if (AHDungeon.IsDungeon(area) || area == AHDeep.Area) { sh = new Vector4(0.94f, 0.96f, 1.1f, -0.05f); sat = 10f; con = 26f; exp = 0.2f; }
        else if (area == "ember") { hi = new Vector4(1.1f, 1f, 0.9f, 0.03f); sat = 26f; }
        else if (area == "frost") { sh = new Vector4(0.95f, 1f, 1.1f, -0.02f); hi = new Vector4(1f, 1.02f, 1.05f, 0.02f); sat = 12f; }
        smh.shadows.Override(sh); smh.highlights.Override(hi);
        ca.saturation.Override(sat); ca.contrast.Override(con); ca.postExposure.Override(exp);
    }

    // test tools: switch the whole look off and on (to compare)
    public static void Enable(bool on) { if (V != null) V.weight = on ? 1f : 0f; }

    // ambient occlusion (soft contact shadows in creases, under eaves and round feet): a renderer feature added once by
    // Ashen Hollow → Tools: Graphics Upgrade; switched with the quality setting here (it costs too much on the lowest)
    public static void AO(bool on)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; if (urp == null) return;
        var f = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var list = f != null ? f.GetValue(urp) as ScriptableRendererData[] : null; if (list == null) return;
        foreach (var d in list) if (d != null) foreach (var rf in d.rendererFeatures) if (rf != null && rf.GetType().Name == "ScreenSpaceAmbientOcclusion") rf.SetActive(on);
    }
}
