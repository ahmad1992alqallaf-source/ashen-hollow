// Ashen Hollow: the water's detail maps (AHWater.shader v3): an ocean-spectrum slope map and a caustic / foam map, made by
// tools/water/make_water_tex.py and kept as PNG bytes (Resources/AH/Textures/water_*.bytes) so they load as linear data,
// tiling, with mipmaps. Set once as global textures for every water surface.
using UnityEngine;

public static class AHWaterTex
{
    static Texture2D w, d;
    public static void Ensure()
    {
        // the maps are made once, but set every time a land loads (a shader re-import clears the globals, and with no
        // domain reload on play a "done" flag outlived them: the sea went flat and pale)
        if (w == null) w = Load("AH/Textures/water_waves");
        if (d == null) d = Load("AH/Textures/water_detail");
        if (w == null || d == null) { Shader.SetGlobalFloat("_AH_WaterTexOn", 0f); return; }
        Shader.SetGlobalTexture("_AH_WaveTex", w); Shader.SetGlobalTexture("_AH_WaterDetail", d);
        Shader.SetGlobalFloat("_AH_WaterTexOn", 1f);
    }
    static Texture2D Load(string res)
    {
        var ta = Resources.Load<TextAsset>(res); if (ta == null) return null;
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
        if (!t.LoadImage(ta.bytes, false)) return null;
        t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4; t.Apply(true, true);
        return t;
    }
}
