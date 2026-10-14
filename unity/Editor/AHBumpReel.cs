// Ashen Hollow (editor test): "Test: Walk Into Things" (shift+alt+I). Picks six solid things in the land you are in
// (crags, walls, houses, rocks: AHSolidGrid) and walks the hero straight at each for four seconds, filmed through the
// game's own camera (so you see whether the hero stops at it and whether the camera stays out of it).
// Frames: HeroShots/bump/<n>/f_00000.jpg (960x540, 30 a second), zipped to HeroShots/bump/<area>.zip;
// HeroShots/bump/<area>.txt says how close the hero got to each.
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHBumpReel
{
    const int W = 960, H = 540, Fps = 30;
    [MenuItem("Ashen Hollow/Test: Walk Into Things #&i")]
    static void Run() { var g = AHGame.I; if (Application.isPlaying && g != null && g.player != null) g.StartCoroutine(Reel(g)); }

    static IEnumerator Reel(AHGame g)
    {
        string root = Path.Combine(Application.dataPath, "../HeroShots/bump"); Directory.CreateDirectory(root);
        foreach (var d in Directory.GetDirectories(root)) Directory.Delete(d, true);
        var p = g.player; var rt = new RenderTexture(W, H, 24); var grab = new Texture2D(W, H, TextureFormat.RGB24, false);
        var targets = AHSolidGrid.Targets(6, 9f, AHGame.AreaId.GetHashCode());
        // HeroShots/bump_wild.txt there: walk at the wild land's own rocks and broken pillars instead (AHWilds)
        if (File.Exists(Path.Combine(Application.dataPath, "../HeroShots/bump_wild.txt")))
        {
            targets = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Vector3, Vector3>>();
            var rnd = new System.Random(7);
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (targets.Count >= 6 || (go.name != "Wild ruin" && go.name != "Wild rock")) continue;
                if (rnd.Next(3) != 0) continue;
                var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                if (bb.size.y < 0.6f) continue;
                float a = (float)rnd.NextDouble() * 6.28f; Vector3 c = bb.center; c.y = g.player.transform.position.y;
                var from = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Mathf.Max(bb.size.x, bb.size.z) * 0.5f + 5f);
                targets.Add(new System.Collections.Generic.KeyValuePair<Vector3, Vector3>(g.Resolve(from, 0.4f), c));
            }
        }
        var log = new System.Text.StringBuilder(); int n = 0;
        foreach (var t in targets)
        {
            n++; string sd = Path.Combine(root, n.ToString("00")); Directory.CreateDirectory(sd);
            p.transform.position = t.Key; Vector3 dir = t.Value - t.Key; dir.y = 0f; dir.Normalize();
            p.transform.rotation = Quaternion.LookRotation(dir);
            g.camYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            for (int k = 0; k < 20; k++) yield return null;
            Time.captureFramerate = Fps; float closest = 99f;
            for (int f = 0; f < 120; f++)
            {
                Vector3 cf = g.CamForward(), cr = g.CamRight();
                AHInput.TestMove = f < 110 ? new Vector2(Vector3.Dot(dir, cr), Vector3.Dot(dir, cf)) : Vector2.zero;
                yield return new WaitForEndOfFrame();
                Vector3 hp = p.transform.position; hp.y = t.Value.y; closest = Mathf.Min(closest, (hp - t.Value).magnitude);
                g.cam.targetTexture = rt; g.cam.Render(); g.cam.targetTexture = null;
                RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(false); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(sd, "f_" + f.ToString("00000") + ".jpg"), grab.EncodeToJPG(85));
            }
            Time.captureFramerate = 0; AHInput.TestMove = Vector2.zero;
            log.AppendLine(n + ": solid cell at " + t.Value.ToString("F1") + ", hero got to " + closest.ToString("F2") + " m of it" + (AHSolidGrid.Blocked(p.transform.position, 0f) ? "  INSIDE IT" : ""));
        }
        Object.Destroy(rt); Object.Destroy(grab);
        File.WriteAllText(Path.Combine(root, AHGame.AreaId + ".txt"), log.ToString());
        string zp = Path.Combine(root, AHGame.AreaId + ".zip"); if (File.Exists(zp)) File.Delete(zp);
        string tmp = Path.Combine(Application.dataPath, "../HeroShots/bump_zip_tmp"); if (Directory.Exists(tmp)) Directory.Delete(tmp, true); Directory.CreateDirectory(tmp);
        foreach (var d in Directory.GetDirectories(root)) { string to = Path.Combine(tmp, Path.GetFileName(d)); Directory.CreateDirectory(to); foreach (var f in Directory.GetFiles(d)) File.Copy(f, Path.Combine(to, Path.GetFileName(f))); }
        System.IO.Compression.ZipFile.CreateFromDirectory(tmp, zp, System.IO.Compression.CompressionLevel.Fastest, false);
        Directory.Delete(tmp, true);
        Debug.Log("Ashen Hollow: walk-into test done (HeroShots/bump)\n" + log);
    }
}
