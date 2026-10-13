// Ashen Hollow (editor tests) for the companions:
//  - "Test: Mount Shots" (shift+alt+M): the hero rides every mount in turn; each filmed standing and galloping on the
//    spot, from the side, the front and behind: HeroShots/mounts/<mount>.jpg, and how high the hero sits over the
//    saddle in mounts.txt
//  - "Test: Pet Shots" (shift+alt+P): every pet called out in turn and filmed with the hero standing and running in
//    place: HeroShots/pets/<pet>.jpg
// The hero's own pet and mount are put back afterwards.
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHCompShots
{
    static string Dir(string sub) { var d = Path.Combine(Application.dataPath, "../HeroShots/" + sub); Directory.CreateDirectory(d); return d; }

    [MenuItem("Ashen Hollow/Test: Mount Shots #&m")]
    static void Mounts() { var g = AHGame.I; if (Application.isPlaying && g != null && g.player != null) g.StartCoroutine(MountRun(g)); }

    [MenuItem("Ashen Hollow/Test: Pet Shots #&p")]
    static void Pets() { var g = AHGame.I; if (Application.isPlaying && g != null && g.player != null) g.StartCoroutine(PetRun(g)); }

    static IEnumerator MountRun(AHGame g)
    {
        var p = g.player; string was = p.mountSel; bool wasOn = p.mounted;
        var log = new StringBuilder();
        foreach (var k in AHComp.Mounts.Keys)
        {
            if (!p.mounts.Contains(k)) p.mounts.Add(k);
            if (p.mounted) AHComp.Dismount(g, true);
            p.mountSel = k; AHComp.Mount(g);
            if (!p.mounted) { log.AppendLine(k + "\tcould not mount here"); continue; }
            yield return new WaitForSeconds(1.2f);
            var shots = new Texture2D[6];
            for (int pass = 0; pass < 2; pass++)
            {
                AHMountGait.TestRun = pass == 1; yield return new WaitForSeconds(0.8f);
                Shoot(p.transform, 2.6f, 1.1f, shots, pass * 3);
            }
            AHMountGait.TestRun = false;
            // the hero over the saddle: the lowest point of the rider against the top of the mount's back
            float riderLow = float.MaxValue, mountTop = float.MinValue;
            foreach (var r in p.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer) continue;
                bool onMount = p.mountGo != null && r.transform.IsChildOf(p.mountGo.transform);
                if (onMount) mountTop = Mathf.Max(mountTop, r.bounds.center.y);
                else riderLow = Mathf.Min(riderLow, r.bounds.min.y);
            }
            log.AppendLine(k + "\tseat " + AHComp.Seat(k).ToString("F2") + "\trider lowest " + (riderLow - p.transform.position.y).ToString("F2") + "\tmount middle " + (mountTop - p.transform.position.y).ToString("F2"));
            Save(Path.Combine(Dir("mounts"), k + ".jpg"), shots, 3);
        }
        if (p.mounted) AHComp.Dismount(g, true);
        p.mountSel = was; if (wasOn) AHComp.Mount(g);
        File.WriteAllText(Path.Combine(Dir("mounts"), "mounts.txt"), log.ToString());
        Debug.Log("Ashen Hollow: mount shots done (HeroShots/mounts)");
    }

    static IEnumerator PetRun(AHGame g)
    {
        var p = g.player; string was = p.pet;
        if (p.mounted) AHComp.Dismount(g, true);
        foreach (var k in AHComp.Pets.Keys)
        {
            if (!p.pets.Contains(k)) p.pets.Add(k);
            p.pet = k; AHComp.SpawnPet(g);
            yield return new WaitForSeconds(1.5f);
            var shots = new Texture2D[6];
            Shoot(p.transform, 2.4f, 1.3f, shots, 0);
            yield return new WaitForSeconds(0.6f);
            Shoot(p.transform, 1.6f, 1.45f, shots, 3);   // close
            Save(Path.Combine(Dir("pets"), k + ".jpg"), shots, 3);
        }
        p.pet = was; AHComp.SpawnPet(g);
        Debug.Log("Ashen Hollow: pet shots done (HeroShots/pets)");
    }

    // three views (side, front three-quarter, behind) into shots[at..at+2]
    static void Shoot(Transform hero, float dist, float h, Texture2D[] shots, int at)
    {
        const int S = 420;
        var go = new GameObject("CompCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 40f; cam.nearClipPlane = 0.03f;
        var gc = AHGame.I.cam; if (gc != null) { cam.clearFlags = gc.clearFlags; cam.backgroundColor = gc.backgroundColor; cam.cullingMask = gc.cullingMask; }
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt;
        Vector3 c = hero.position + Vector3.up * h;
        Vector3[] dirs = { hero.right, (hero.forward + hero.right * 0.6f).normalized, (-hero.forward + hero.right * 0.25f).normalized };
        for (int i = 0; i < 3; i++)
        {
            go.transform.position = c + dirs[i] * dist + Vector3.up * 0.25f; go.transform.LookAt(c);
            cam.Render(); RenderTexture.active = rt;
            var t = new Texture2D(S, S, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, S, S), 0, 0); t.Apply(); RenderTexture.active = null;
            shots[at + i] = t;
        }
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(go);
    }

    static void Save(string path, Texture2D[] shots, int cols)
    {
        int S = shots[0].width, rows = (shots.Length + cols - 1) / cols;
        var sheet = new Texture2D(S * cols, S * rows, TextureFormat.RGB24, false);
        for (int i = 0; i < shots.Length; i++) if (shots[i] != null) { sheet.SetPixels((i % cols) * S, (rows - 1 - i / cols) * S, S, S, shots[i].GetPixels()); Object.Destroy(shots[i]); }
        sheet.Apply(); File.WriteAllBytes(path, sheet.EncodeToJPG(85)); Object.Destroy(sheet);
    }
}
