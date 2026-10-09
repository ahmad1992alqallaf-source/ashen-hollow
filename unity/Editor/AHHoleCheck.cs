// Ashen Hollow (editor test): "Test: See-Through Check". Photographs only the hero (nothing else drawn) on a bright
// magenta background from 12 directions at two heights, standing and at two moments of a run, to
// HeroShots/holes/<pose>_<pitch>_<yaw>.png. Any magenta inside the hero's outline is a hole you can see through.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHHoleCheck
{
    const int Layer = 31, Size = 512;
    [MenuItem("Ashen Hollow/Test: See-Through Check %&o")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = Path.Combine(Application.dataPath, "../HeroShots/holes"); Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
        Shoot(g, dir, "stand");
        AHInput.TestMove = new Vector2(0f, 1f); double t0 = EditorApplication.timeSinceStartup; int n = 0;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            double t = EditorApplication.timeSinceStartup - t0;
            if (n < 2 && t > 1.0 + n * 0.21) { n++; Shoot(g, dir, "run" + n); }
            if (n >= 2 || t > 4 || !Application.isPlaying) { AHInput.TestMove = Vector2.zero; EditorApplication.update -= cb; Debug.Log("Ashen Hollow: see-through shots saved to HeroShots/holes"); }
        };
        EditorApplication.update += cb;
    }

    static void Shoot(AHGame g, string dir, string pose)
    {
        var hero = g.player.transform;
        var keep = new List<KeyValuePair<GameObject, int>>();
        foreach (var r in hero.GetComponentsInChildren<Renderer>(false))
        {
            if (r is ParticleSystemRenderer) continue;
            keep.Add(new KeyValuePair<GameObject, int>(r.gameObject, r.gameObject.layer)); r.gameObject.layer = Layer;
        }
        var go = new GameObject("HoleCam"); var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(1f, 0f, 1f, 1f); cam.cullingMask = 1 << Layer;
        cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f;
        var rt = new RenderTexture(Size, Size, 24); cam.targetTexture = rt; var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
        Vector3 at = hero.position + Vector3.up * 1.0f;
        foreach (float pitch in new[] { 8f, 38f })
            for (int i = 0; i < 12; i++)
            {
                float yaw = i * 30f;
                Vector3 h = Quaternion.Euler(0f, yaw, 0f) * hero.forward; Vector3 d = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, h)) * h;
                cam.transform.position = at + d * 4.4f; cam.transform.LookAt(at);
                cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, pose + "_" + (int)pitch + "_" + ((int)yaw).ToString("000") + ".png"), tex.EncodeToPNG());
            }
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(tex);
        foreach (var kv in keep) if (kv.Key != null) kv.Key.layer = kv.Value;
    }
}
