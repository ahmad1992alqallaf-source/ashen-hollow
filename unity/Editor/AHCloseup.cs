// Ashen Hollow (editor test): "Test: Monster Close-up". A monster (its id in HeroShots/show_id.txt, the Hellforged
// Knight when there is none) stood high above the world on its own, posed in its idle, walk and attack clips and
// photographed from the front three-quarters and the side of each: HeroShots/closeup_<id>.png (a row a clip). For
// checking a model and whatever it holds without the land getting in the way.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHCloseup
{
    [MenuItem("Ashen Hollow/Test: Monster Close-up #&c")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = Path.Combine(Application.dataPath, "../HeroShots"); Directory.CreateDirectory(dir);
        string idf = Path.Combine(dir, "show_id.txt");
        string ids = File.Exists(idf) ? File.ReadAllText(idf).Trim() : "dm_knight"; if (ids.Length == 0) ids = "dm_knight";
        foreach (var id in ids.Split(new[] { ',', '\n', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)) Shoot(g, id.Trim(), dir);
    }

    // one clip a few frames: hold the pose, let the animation graph show it, then photograph
    static void Shoot(AHGame g, string id, string dir)
    {
        if (AHJson.O(AHDB.Mobs, id) == null) { Debug.Log("Ashen Hollow: close-up: no monster " + id); return; }
        var t = new AHMobType { id = id, name = AHJson.S(AHJson.O(AHDB.Mobs, id), "name", id), hp = 10, lvl = 1, radius = 0.5f };
        Vector3 at = g.player.transform.position + Vector3.up * 400f;
        var m = AHMob.Create(g, t, at); m.enabled = false;
        m.transform.position = at; m.transform.rotation = Quaternion.identity;
        string[] clips = { "Idle", "Walk", "Attack", "Attack2" }; float[] at01 = { 0.3f, 0.3f, 0.45f, 0.5f };
        const int S = 480;
        var sheet = new Texture2D(S * 2, S * clips.Length, TextureFormat.RGB24, false);
        int c = 0, frames = 0, last = -1;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            if (!Application.isPlaying || m == null) { EditorApplication.update -= cb; return; }
            if (m.anim != null) { m.anim.Hold(clips[c], at01[c]); m.anim.Tick(10f); }
            if (Time.frameCount == last) return; last = Time.frameCount;
            if (++frames < 4) return;
            frames = 0;
            var go = new GameObject("CloseCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.7f, 0.76f);
            var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt;
            bool fog = RenderSettings.fog; RenderSettings.fog = false;
            var rs = m.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
                var views = new[] { (Vector3.forward * 0.8f + Vector3.right * 0.5f + Vector3.up * 0.2f).normalized, (Vector3.right + Vector3.up * 0.12f).normalized };
                for (int v = 0; v < 2; v++)
                {
                    cam.transform.position = b.center + views[v] * (size * 2.0f + 0.6f); cam.transform.LookAt(b.center); cam.Render();
                    RenderTexture.active = rt; sheet.ReadPixels(new Rect(0, 0, S, S), v * S, (clips.Length - 1 - c) * S); RenderTexture.active = null;
                }
                var big = new System.Text.StringBuilder();
                foreach (var r in rs) { var rb = r.bounds; if (Mathf.Max(rb.size.x, rb.size.y, rb.size.z) > 3f) big.Append(" | " + r.transform.parent?.name + "/" + r.name + " " + rb.size.ToString("F1") + " at " + (rb.center - m.transform.position).ToString("F1")); }
                Debug.Log("Ashen Hollow: close-up " + id + " " + clips[c] + ": " + b.size.ToString("F2") + big);
            }
            RenderSettings.fog = fog; cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt);
            if (++c < clips.Length) return;
            EditorApplication.update -= cb;
            sheet.Apply(); File.WriteAllBytes(Path.Combine(dir, "closeup_" + id + ".png"), sheet.EncodeToPNG());
            Object.Destroy(sheet); Object.Destroy(m.gameObject);
        };
        EditorApplication.update += cb;
    }
}
