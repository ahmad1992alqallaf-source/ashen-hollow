// Ashen Hollow (editor test): "Test: Tour Reel" (ctrl+shift+alt+T). Rides the hero along a route and films it through the
// game's own camera, for a video of a land as a player sees it. HeroShots/tour.txt:
//   area sands               the land (travels there first)
//   ride spider              get on this mount (or "walk")
//   hour 9                   time of day
//   yaw 90                   turn the camera to face this way (compass, 0 north) and keep it there
//   at -600 379              stand here to start
//   to -650 380              then run to here (one line per stop)
//   wait 2                   stand still a moment
// Frames: HeroShots/tour/f_00000.jpg (854x480, 30 a second), zipped to HeroShots/tour.zip.
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHTourReel
{
    const int W = 854, H = 480, Fps = 30;
    [MenuItem("Ashen Hollow/Test: Tour Reel %#&t")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string f = Path.Combine(Application.dataPath, "../HeroShots/tour.txt"); if (!File.Exists(f)) return;
        var lines = new List<string[]>(); foreach (var l in File.ReadAllLines(f)) { var w = l.Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries); if (w.Length > 0 && !w[0].StartsWith("#")) lines.Add(w); }
        string area = null; foreach (var w in lines) if (w[0] == "area" && w.Length > 1) area = w[1];
        if (area != null && AHGame.AreaId != area)
        {
            var ta = Resources.Load<TextAsset>("AH/Areas/" + area); if (ta == null) return;
            var d = JsonUtility.FromJson<AHWorldData>(ta.text); double t0 = EditorApplication.timeSinceStartup;
            g.Travel(area, d.spawn.x, d.spawn.z, 0f);
            EditorApplication.CallbackFunction cb = null;
            cb = () => { var gg = AHGame.I; if (!Application.isPlaying) { EditorApplication.update -= cb; return; } if (gg == null || AHGame.AreaId != area || EditorApplication.timeSinceStartup - t0 < 10) return; EditorApplication.update -= cb; gg.StartCoroutine(Reel(gg, lines)); };
            EditorApplication.update += cb;
        }
        else g.StartCoroutine(Reel(g, lines));
    }

    static IEnumerator Reel(AHGame g, List<string[]> lines)
    {
        var ci = CultureInfo.InvariantCulture;
        string dir = Path.Combine(Application.dataPath, "../HeroShots/tour"); if (Directory.Exists(dir)) Directory.Delete(dir, true); Directory.CreateDirectory(dir);
        var p = g.player; var rt = new RenderTexture(W, H, 24); var grab = new Texture2D(W, H, TextureFormat.RGB24, false);
        int frame = 0; float? yaw = null;
        System.Action shoot = () =>
        {
            g.cam.targetTexture = rt; g.cam.Render(); g.cam.targetTexture = null;
            RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(false); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(dir, "f_" + (frame++).ToString("00000") + ".jpg"), grab.EncodeToJPG(80));
        };
        Time.captureFramerate = Fps;
        foreach (var w in lines)
        {
            switch (w[0])
            {
                case "hour": g.SetTimeOfDay(float.Parse(w[1], ci) / 24f); break;
                case "yaw": if (w[1] == "free") yaw = null; else { yaw = float.Parse(w[1], ci); g.camYaw = yaw.Value; } break;
                case "ride":
                    if (w[1] == "walk") { if (p.mounted) AHComp.Dismount(g, true); }
                    else { if (!p.mounts.Contains(w[1])) p.mounts.Add(w[1]); if (p.mounted) AHComp.Dismount(g, true); p.mountSel = w[1]; AHComp.Mount(g); }
                    for (int k = 0; k < 30; k++) yield return null;
                    break;
                case "at":
                    p.transform.position = new Vector3(float.Parse(w[1], ci), p.transform.position.y, float.Parse(w[2], ci));
                    for (int k = 0; k < 30; k++) yield return null;
                    break;
                case "wait":
                    for (int k = 0, n = Mathf.RoundToInt(float.Parse(w[1], ci) * Fps); k < n; k++) { AHInput.TestMove = Vector2.zero; if (yaw.HasValue) g.camYaw = yaw.Value; yield return new WaitForEndOfFrame(); shoot(); }
                    break;
                case "to":
                {
                    Vector3 to = new Vector3(float.Parse(w[1], ci), 0f, float.Parse(w[2], ci)); float stuck = 0f; Vector3 last = p.transform.position;
                    for (int k = 0; k < Fps * 40; k++)
                    {
                        Vector3 d = to - p.transform.position; d.y = 0f; if (d.magnitude < 1.2f) break;
                        Vector3 want = d.normalized, cf = g.CamForward(), cr = g.CamRight();
                        if (yaw.HasValue) g.camYaw = yaw.Value; else g.camYaw = Mathf.LerpAngle(g.camYaw, Mathf.Atan2(want.x, want.z) * Mathf.Rad2Deg, 0.04f);
                        AHInput.TestMove = new Vector2(Vector3.Dot(want, cr), Vector3.Dot(want, cf));
                        yield return new WaitForEndOfFrame(); shoot();
                        // walked into something and not getting anywhere: on to the next stop
                        if ((p.transform.position - last).magnitude < 0.02f) stuck += 1f / Fps; else stuck = 0f; last = p.transform.position;
                        if (stuck > 1.5f) break;
                    }
                    AHInput.TestMove = Vector2.zero;
                    break;
                }
            }
        }
        Time.captureFramerate = 0; AHInput.TestMove = Vector2.zero;
        Object.Destroy(rt); Object.Destroy(grab);
        string zp = Path.Combine(Application.dataPath, "../HeroShots/tour.zip"); if (File.Exists(zp)) File.Delete(zp);
        System.IO.Compression.ZipFile.CreateFromDirectory(dir, zp, System.IO.Compression.CompressionLevel.Fastest, false);
        Debug.Log("Ashen Hollow: tour reel done, " + frame + " frames (HeroShots/tour.zip)");
    }
}
