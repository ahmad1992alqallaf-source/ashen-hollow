// Ashen Hollow (editor): "VRoid: Fitting Shots". Straight-on (orthographic, no perspective) pictures of the two
// fitting bodies (costume_m_under and costume_f_under) from the front, the right side and the back, at an exact
// scale of 500 pixels per metre with the ground on the bottom edge, so a 10 cm grid can be drawn over them later and
// every measurement on the picture is true. Saved as HeroShots/fit_<m|f>_<front|side|back>.png.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHFittingShots
{
    const float PxPerM = 500f, ViewH = 2.0f;
    const string Dir = "Assets/AshenHollow/Resources/AH/VRoid/";

    [MenuItem("Ashen Hollow/VRoid: Fitting Shots")]
    static void Run()
    {
        var cg = new GameObject("FitShotCam"); var cam = cg.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = ViewH * 0.5f; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.93f, 0.93f, 0.95f); cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
        var lg = new GameObject("FitShotLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.1f; lg.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
        foreach (var who in new[] { "m", "f" })
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "costume_" + who + "_under.prefab");
            if (pf == null) { Debug.Log("Ashen Hollow: no costume_" + who + "_under.prefab"); continue; }
            Vector3 at = new Vector3(0f, -600f, 0f);
            var go = (GameObject)Object.Instantiate(pf, at, Quaternion.identity);
            foreach (var an in go.GetComponentsInChildren<Animator>()) an.enabled = false;
            string[] names = { "front", "side", "back" };
            Vector3[] from = { Vector3.forward, Vector3.right, Vector3.back };   // VRoid faces +z; "side" looks at the hero's right side... from +x
            for (int i = 0; i < 3; i++)
            {
                int w = Mathf.RoundToInt((i == 1 ? 0.8f : 1.9f) * PxPerM), h = Mathf.RoundToInt(ViewH * PxPerM);
                var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.aspect = (float)w / h;
                Vector3 c = at + Vector3.up * ViewH * 0.5f;
                cam.transform.position = c + from[i] * 8f; cam.transform.rotation = Quaternion.LookRotation(-from[i], Vector3.up);
                cam.Render();
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/fit_" + who + "_" + names[i] + ".png"), tex.EncodeToPNG());
                cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            }
            Object.DestroyImmediate(go);
        }
        Object.DestroyImmediate(cg); Object.DestroyImmediate(lg);
        Debug.Log("Ashen Hollow: fitting shots saved to HeroShots/fit_*.png (500 px per metre, ground at the bottom)");
    }
}
