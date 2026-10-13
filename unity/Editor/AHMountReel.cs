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
        foreach (var k in AHComp.Mounts.Keys)
        {
            n++;
            if (!p.mounts.Contains(k)) p.mounts.Add(k);
            if (p.mounted) AHComp.Dismount(g, true);
            p.transform.position = home; p.transform.rotation = homeRot;
            p.mountSel = k; AHComp.Mount(g);
            if (!p.mounted) { names.AppendLine(k + "\t(could not ride here)"); continue; }
            names.AppendLine(n.ToString("00") + "_" + k + "\t" + AHComp.MountName(k));
            yield return new WaitForSecondsRealtime(1.2f);
            string sd = Path.Combine(root, n.ToString("00") + "_" + k); Directory.CreateDirectory(sd);
            foreach (var f in Directory.GetFiles(sd, "*.jpg")) File.Delete(f);
            Vector3 fwd = p.transform.forward; fwd.y = 0f; fwd.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, fwd);
            bool big = false; foreach (var r in p.GetComponentsInChildren<Renderer>()) if (r.bounds.size.y > 3.2f) big = true;
            float back = big ? 8.5f : 6f;
            Time.captureFramerate = Fps; int frames = 115;
            for (int f = 0; f < frames; f++)
            {
                p.hp = p.maxHp;
                // walk a moment, then gallop
                Vector3 want = fwd * (f < 25 ? 0.45f : 1f); Vector3 cf = g.CamForward(), cr = g.CamRight();
                AHInput.TestMove = new Vector2(Vector3.Dot(want, cr), Vector3.Dot(want, cf));
                yield return new WaitForEndOfFrame();
                Vector3 h = p.transform.position;
                if (g.cam != null) { cam.CopyFrom(g.cam); cam.fieldOfView = 40f; }
                cam.transform.position = h + side * back + Vector3.up * (big ? 2.6f : 1.9f) + fwd * 1.2f; cam.transform.LookAt(h + Vector3.up * (big ? 1.6f : 1.2f) + fwd * 0.6f);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(false); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(sd, "f_" + f.ToString("00000") + ".jpg"), grab.EncodeToJPG(88));
            }
            Time.captureFramerate = 0; AHInput.TestMove = Vector2.zero;
            Debug.Log("Ashen Hollow: mount reel " + k);
        }
        if (p.mounted) AHComp.Dismount(g, true);
        p.mountSel = was; p.transform.position = home; p.transform.rotation = homeRot;
        File.WriteAllText(Path.Combine(root, "names.txt"), names.ToString());
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(grab); Object.Destroy(go);
        // zips, to carry the frames off the PC in a few files
        string zd = Path.Combine(root, "zips"); Directory.CreateDirectory(zd);
        foreach (var sd in Directory.GetDirectories(root))
        {
            string nm = Path.GetFileName(sd); if (nm == "zips") continue;
            string zp = Path.Combine(zd, nm + ".zip"); if (File.Exists(zp)) File.Delete(zp);
            System.IO.Compression.ZipFile.CreateFromDirectory(sd, zp, System.IO.Compression.CompressionLevel.NoCompression, false);
        }
        Debug.Log("Ashen Hollow: mount reel done (HeroShots/mount_reel)");
    }
}
