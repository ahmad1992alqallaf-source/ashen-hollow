// Ashen Hollow (editor test): "Test: Pet Reel" (ctrl+shift+alt+K). Every pet is called out in turn and filmed up
// close: first standing (idle), then following the hero at a walk, then at a run, at 30 frames a second (game time
// steps exactly 1/30 s a frame). Frames: HeroShots/pet_reel/<nn>_<pet>/f_00000.jpg (960x540), zipped into
// HeroShots/pet_reel/zips. HeroShots/pet_reel/only.txt ("toad,owl") films just those. Your own pet is put back after.
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHPetReel
{
    const int W = 960, H = 540, Fps = 30;

    [MenuItem("Ashen Hollow/Test: Pet Reel %#&k")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        g.StartCoroutine(Reel(g));
    }

    static IEnumerator Reel(AHGame g)
    {
        string root = Path.Combine(Application.dataPath, "../HeroShots/pet_reel"); Directory.CreateDirectory(root);
        var p = g.player; string was = p.pet; Vector3 home = p.transform.position; Quaternion homeRot = p.transform.rotation;
        if (p.mounted) AHComp.Dismount(g, true);
        // the party and any monsters near by step out of the picture while the pets are filmed
        var hidden = new System.Collections.Generic.List<GameObject>();
        foreach (var a in AHComp.Allies) if (a != null && a.gameObject.activeSelf) { a.gameObject.SetActive(false); hidden.Add(a.gameObject); }
        foreach (var m in g.mobs) if (m != null && m.gameObject.activeSelf && (m.transform.position - home).magnitude < 40f) { m.gameObject.SetActive(false); hidden.Add(m.gameObject); }
        var go = new GameObject("PetReelCam"); var cam = go.AddComponent<Camera>(); cam.enabled = false; cam.fieldOfView = 35f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400f;
        var rt = new RenderTexture(W, H, 24); var grab = new Texture2D(W, H, TextureFormat.RGB24, false);
        string onlyF = Path.Combine(root, "only.txt"); string[] only = File.Exists(onlyF) ? File.ReadAllText(onlyF).Split(new[] { ',', ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries) : new string[0];
        int n = 0;
        foreach (var k in AHComp.Pets.Keys)
        {
            n++;
            if (only.Length > 0 && System.Array.IndexOf(only, k) < 0) continue;
            p.transform.position = home; p.transform.rotation = homeRot;
            p.pet = k; AHComp.SpawnPet(g);
            yield return new WaitForSecondsRealtime(1.5f);
            var pet = GameObject.Find("Pet"); if (pet == null) continue;
            string sd = Path.Combine(root, n.ToString("00") + "_" + k); Directory.CreateDirectory(sd);
            foreach (var f in Directory.GetFiles(sd, "*.jpg")) File.Delete(f);
            Vector3 fwd = p.transform.forward; fwd.y = 0f; fwd.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, fwd);
            Time.captureFramerate = Fps; int frames = 150;
            for (int f = 0; f < frames; f++)
            {
                p.hp = p.maxHp;
                // standing for a while, then a walk, then a run
                float sp = f < 50 ? 0f : f < 100 ? 0.3f : 1f;
                Vector3 want = fwd * sp; Vector3 cf = g.CamForward(), cr = g.CamRight();
                AHInput.TestMove = new Vector2(Vector3.Dot(want, cr), Vector3.Dot(want, cf));
                yield return new WaitForEndOfFrame();
                // the pet's size and where it is now (it may perch on the shoulder or fly overhead)
                var b = new Bounds(pet.transform.position, Vector3.zero); bool any = false;
                foreach (var r in pet.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                float size = Mathf.Max(0.4f, b.size.magnitude);
                if (g.cam != null) { cam.CopyFrom(g.cam); cam.fieldOfView = 35f; }
                cam.transform.position = b.center + side * size * 2.6f + Vector3.up * size * 0.7f + fwd * size * 0.9f;
                cam.transform.LookAt(b.center);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(false); RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(sd, "f_" + f.ToString("00000") + ".jpg"), grab.EncodeToJPG(88));
            }
            Time.captureFramerate = 0; AHInput.TestMove = Vector2.zero;
            Debug.Log("Ashen Hollow: pet reel " + k);
        }
        foreach (var h in hidden) if (h != null) h.SetActive(true);
        p.pet = was; AHComp.SpawnPet(g); p.transform.position = home; p.transform.rotation = homeRot;
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(grab); Object.Destroy(go);
        string zd = Path.Combine(root, "zips"); Directory.CreateDirectory(zd);
        foreach (var sd in Directory.GetDirectories(root))
        {
            string nm = Path.GetFileName(sd); if (nm == "zips") continue;
            if (only.Length > 0 && System.Array.IndexOf(only, nm.Substring(3)) < 0) continue;
            string zp = Path.Combine(zd, nm + ".zip"); if (File.Exists(zp)) File.Delete(zp);
            System.IO.Compression.ZipFile.CreateFromDirectory(sd, zp, System.IO.Compression.CompressionLevel.NoCompression, false);
        }
        Debug.Log("Ashen Hollow: pet reel done (HeroShots/pet_reel)");
    }
}
