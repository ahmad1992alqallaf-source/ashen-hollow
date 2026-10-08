// Ashen Hollow editor tools for the picture:
//  Tools: Graphics Upgrade — adds ambient occlusion to the URP renderer(s) once, and sets softer, longer shadows for
//    PC (phones still cap them in AHGame.PhoneQuality).
//  Test: Graphics Before-After — renders the game camera twice (the new look off, then on) to HeroShots/gfx_off.png and
//    gfx_on.png, without the HUD, to compare.
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AHGraphicsTools
{
    [MenuItem("Ashen Hollow/Tools: Graphics Upgrade")]
    static void Upgrade()
    {
        if (Application.isPlaying) { Debug.Log("Ashen Hollow: stop Play mode first"); return; }
        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        var log = new System.Text.StringBuilder();
        var assets = new System.Collections.Generic.List<UniversalRenderPipelineAsset>();
        if (urp != null) assets.Add(urp);
        for (int q = 0; q < QualitySettings.names.Length; q++) { var a = QualitySettings.GetRenderPipelineAssetAt(q) as UniversalRenderPipelineAsset; if (a != null && !assets.Contains(a)) assets.Add(a); }
        var aoType = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
        foreach (var a in assets)
        {
            var f = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            var list = f != null ? f.GetValue(a) as ScriptableRendererData[] : null;
            if (list != null && aoType != null)
                foreach (var d in list)
                {
                    if (d == null) continue; bool has = false; foreach (var rf in d.rendererFeatures) if (rf != null && rf.GetType() == aoType) has = true;
                    if (has) { log.AppendLine(d.name + ": ambient occlusion already there"); continue; }
                    var feat = (ScriptableRendererFeature)ScriptableObject.CreateInstance(aoType); feat.name = "AH Ambient Occlusion";
                    AssetDatabase.AddObjectToAsset(feat, d);
                    d.rendererFeatures.Add(feat);
                    var mapF = typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (mapF != null) { var map = mapF.GetValue(d) as System.Collections.Generic.List<long>; long id; string guid; if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out guid, out id)) map.Add(id); }
                    d.SetDirty(); EditorUtility.SetDirty(d); log.AppendLine(d.name + ": ambient occlusion added");
                }
            a.shadowDistance = Mathf.Max(a.shadowDistance, 60f); a.shadowCascadeCount = Mathf.Max(a.shadowCascadeCount, 2);
            EditorUtility.SetDirty(a); log.AppendLine(a.name + ": shadows " + a.shadowDistance + " m, " + a.shadowCascadeCount + " cascades");
        }
        if (aoType == null) log.AppendLine("ambient occlusion type not found in this URP version");
        AssetDatabase.SaveAssets();
        Debug.Log("Ashen Hollow: graphics upgrade\n" + log);
    }

    [MenuItem("Ashen Hollow/Test: Graphics Before-After")]
    static void BeforeAfter()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.cam == null) return;
        string dir = Path.Combine(Application.dataPath, "../HeroShots"); Directory.CreateDirectory(dir);
        Shot(g.cam, false, Path.Combine(dir, "gfx_off.png"));
        Shot(g.cam, true, Path.Combine(dir, "gfx_on.png"));
        Debug.Log("Ashen Hollow: graphics shots saved (gfx_off.png, gfx_on.png)");
    }
    static void Shot(Camera cam, bool on, string path)
    {
        AHGrade.Enable(on); AHGrade.AO(on);
        var rt = new RenderTexture(1280, 720, 24); var was = cam.targetTexture; cam.targetTexture = rt;
        cam.Render(); cam.targetTexture = was;
        RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
        File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.Destroy(tex); rt.Release(); UnityEngine.Object.Destroy(rt);
        AHGrade.Enable(true); AHGrade.AO(true);
    }
}
