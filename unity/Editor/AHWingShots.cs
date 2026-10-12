// Ashen Hollow (editor test): "Test: Wing Shots" (shift+alt+U). Films the hero's wings from behind and from the side as
// a strip of frames: standing (a stretch is set off), running in place and flying (forced through AHWingRig's test
// switches). HeroShots/wings.jpg: a row each (stand, run, fly), eight frames a row, back view over side view.
using UnityEditor;
using UnityEngine;

public static class AHWingShots
{
    [MenuItem("Ashen Hollow/Test: Wing Shots #&u")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        g.StartCoroutine(Film(g));
    }

    static System.Collections.IEnumerator Film(AHGame g)
    {
        const int W = 300, H = 300, N = 8;
        var sheet = new Texture2D(W * N, H * 6, TextureFormat.RGB24, false);
        var go = new GameObject("WingCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 34f; cam.nearClipPlane = 0.05f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; cam.cullingMask = g.cam.cullingMask; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
        var hero = g.player.transform;
        for (int row = 0; row < 3; row++)
        {
            AHWingRig.TestAir = row == 2 ? 1f : 0f; AHWingRig.TestRun = row == 1 ? 1f : 0f; AHWingRig.TestStretch = row == 0;
            yield return new WaitForSeconds(row == 0 ? 0.3f : 1.2f);
            AHWingRig.TestStretch = false;
            float step = row == 0 ? 0.42f : row == 1 ? 0.12f : 0.09f;
            for (int i = 0; i < N; i++)
            {
                Vector3 at = hero.position + Vector3.up * 1.3f;
                for (int v = 0; v < 2; v++)
                {
                    Vector3 d = v == 0 ? -hero.forward * 6.5f + Vector3.up * 1.6f : (hero.right * 0.9f - hero.forward * 0.6f).normalized * 6.5f + Vector3.up * 1.2f;
                    cam.transform.position = at + d; cam.transform.LookAt(at);
                    cam.Render(); RenderTexture.active = rt; sheet.ReadPixels(new Rect(0, 0, W, H), i * W, (5 - (row * 2 + v)) * H); RenderTexture.active = null;
                }
                yield return new WaitForSeconds(step);
            }
        }
        AHWingRig.TestAir = 0f; AHWingRig.TestRun = 0f; AHWingRig.TestStretch = false;
        sheet.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../HeroShots/wings.jpg"), sheet.EncodeToJPG(85));
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(sheet);
        Debug.Log("Ashen Hollow: wing shots saved (HeroShots/wings.jpg)");
    }
}
