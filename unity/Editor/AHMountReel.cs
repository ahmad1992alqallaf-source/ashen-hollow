// Ashen Hollow (editor test): "Test: Mount Ride Reel" (ctrl+shift+alt+R). The hero rides every mount in turn, galloping
// in a straight line for a few seconds, filmed from the side at 30 frames a second (game time steps exactly 1/30 s a
// frame, so the gait plays as it does in the game). Frames: HeroShots/mount_reel/<nn>_<mount>/f_00000.jpg (960x540),
// each mount's frames also zipped into HeroShots/mount_reel/zips. names.txt lists the mounts' names in order.
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHMountReel
{
    const int W = 960, H = 540, Fps = 30;

    [MenuItem("Ashen Hollow/Test: Mount Ride Reel %#&r")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        g.StartCoroutine(Reel(g));
    }

    static IEnumerator Reel(AHGame g)
    {
        string root = Path.Combine(Application.dataPath, "../HeroShots/mount_reel"); Directory.CreateDirectory(root);
        var p = g.player; string was = p.mountSel; Vector3 home = p.transform.position; Quaternion homeRot = p.transform.rotation;
        var go = new GameObject("ReelCam"); var cam = go.AddComponent<Camera>(); cam.enabled = false; cam.fieldOfView = 40f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 600f;
        var rt = new RenderTexture(W, H, 24); var grab = new Texture2D(W, H, TextureFormat.RGB24, false);
        var names = new StringBuilder(); int n = 0;
        // HeroShots/mount_reel/only.txt ("horse,phoenix") films just those
        string onlyF = Path.Combine(root, "only.txt"); string[] only = File.Exists(onlyF) ? File.ReadAllText(onlyF).Split(new[] { ',', ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries) : new string[0];
        foreach (var k in AHComp.Mounts.Keys)
        {
            n++;
            if (only.Length > 0 && System.Array.IndexOf(only, k) < 0) continue;
            if (!p.mounts.Contains(k)) p.mounts.Add(k);
            if (p.mounted) AHComp.Dismount(g, true);
            p.transform.position = home; p.transform.rotation = homeRot;
            p.mountSel = k; AHComp.Mount(g);
            if (!p.mounted) { names.AppendLine(k + "\t(could not ride here)"); continue; }
            names.AppendLine(n.ToString("00") + "_" + k + "\t" + AHComp.MountName(k));
            yield return new WaitForSecondsRealtime(2.2f);   // standing: the mount learns its back and seats the rider
            string sd = Path.Combine(root, n.ToString("00") + "_" + k); Directory.CreateDirectory(sd);
            foreach (var f in Directory.GetFiles(sd, "*.jpg")) File.Delete(f);
            Vector3 fwd = p.transform.forward; fwd.y = 0f; fwd.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, fwd);
            bool big = false; foreach (var r in p.GetComponentsInChildren<Renderer>()) if (r.bounds.size.y > 3.2f) big = true;
            float back = big ? 8.5f : 6f;
            // a still from in front, a little to one side: how the rider sits astride
            {
                Vector3 h0 = p.transform.position; if (g.cam != null) { cam.CopyFrom(g.cam); } cam.fieldOfView = 35f;
                cam.transform.position = h0 + fwd * (big ? 6f : 4.5f) + side * 1.6f + Vector3.up * 1.8f; cam.transform.LookAt(h0 + Vector3.up * 1.4f);
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(false); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(sd, "front.jpg"), grab.EncodeToJPG(90));
            }
            Time.captureFramerate = Fps; int frames = 190; Vector3 sc = Vector3.zero; float sd2 = 0f;
            for (int f = 0; f < frames; f++)
            {
                p.hp = p.maxHp;
                // a slow walk first, then full speed
                Vector3 want = fwd * (f < 80 ? 0.22f : 1f); Vector3 cf = g.CamForward(), cr = g.CamRight();
                AHInput.TestMove = new Vector2(Vector3.Dot(want, cr), Vector3.Dot(want, cf));
                yield return new WaitForEndOfFrame();
                if (f == 60 || f == 170) { var gt = p.GetComponentInChildren<AHMountGait>(); if (gt != null) Debug.Log("Ashen Hollow: mount gait " + k + " frame " + f + ": " + gt.State); }
                Vector3 h = p.transform.position;
                if (g.cam != null) { cam.CopyFrom(g.cam); cam.fieldOfView = 40f; }
                // framed on the mount and rider as they are now (a flier up in the air included), eased so it doesn't jump
                Bounds bb = new Bounds(h + Vector3.up, Vector3.one); foreach (var r in p.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is ParticleSystemRenderer)) bb.Encapsulate(r.bounds);
                float want2 = Mathf.Max(bb.size.y, Mathf.Max(bb.size.x, bb.size.z) * 0.8f) * 1.35f + 2.2f; Vector3 wc = bb.center - h;
                if (f == 0) { sc = wc; sd2 = want2; } else { sc = Vector3.Lerp(sc, wc, 0.12f); sd2 = Mathf.Lerp(sd2, want2, 0.08f); }
                cam.transform.position = h + sc + side * sd2 + Vector3.up * sd2 * 0.18f; cam.transform.LookAt(h + sc);
                cam.nearClipPlane = sd2 * 0.55f;   // (a wall, rock or tree between the camera and the mount is cut away)
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(false); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(sd, "f_" + f.ToString("00000") + ".jpg"), grab.EncodeToJPG(88));
            }
            Time.captureFramerate = 0; AHInput.TestMove = Vector2.zero;
            Debug.Log("Ashen Hollow: mount reel " + k);
        }
        if (p.mounted) AHComp.Dismount(g, true);
        p.mountSel = was; p.transform.position = home; p.transform.rotation = homeRot;
        if (only.Length == 0) File.WriteAllText(Path.Combine(root, "names.txt"), names.ToString());
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(grab); Object.Destroy(go);
        // zips, to carry the frames off the PC in a few files
        string zd = Path.Combine(root, "zips"); Directory.CreateDirectory(zd);
        foreach (var sd in Directory.GetDirectories(root))
        {
            string nm = Path.GetFileName(sd); if (nm == "zips") continue;
            if (only.Length > 0 && System.Array.IndexOf(only, nm.Substring(3)) < 0) continue;
            string zp = Path.Combine(zd, nm + ".zip"); if (File.Exists(zp)) File.Delete(zp);
            System.IO.Compression.ZipFile.CreateFromDirectory(sd, zp, System.IO.Compression.CompressionLevel.NoCompression, false);
        }
        Debug.Log("Ashen Hollow: mount reel done (HeroShots/mount_reel)");
    }
}
