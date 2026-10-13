// Ashen Hollow (editor test): "Test: Hero Close-up Sheet" (ctrl+shift+alt+9): the hero from the front, three-quarter,
// side and back, close, in one picture: HeroShots/hero_close_<n>.jpg (n counts up, so before/after pictures sit side
// by side).
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHHeroClose
{
    static int n;
    [MenuItem("Ashen Hollow/Test: Hero Close-up Sheet %#&9")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var hero = g.player.transform; const int S = 512;
        var go = new GameObject("CloseCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 32f; cam.nearClipPlane = 0.05f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; cam.cullingMask = g.cam.cullingMask; }
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt; var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        var sheet = new Texture2D(S * 4, S, TextureFormat.RGB24, false); float[] yaw = { 0f, 40f, 90f, 180f };
        for (int i = 0; i < 4; i++)
        {
            Vector3 d = Quaternion.Euler(0, yaw[i], 0) * hero.forward, at = hero.position + Vector3.up * 1.0f;
            go.transform.position = at + d * 3.3f + Vector3.up * 0.2f; go.transform.LookAt(at);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, S, S), 0, 0); RenderTexture.active = null;
            sheet.SetPixels(i * S, 0, S, S, tex.GetPixels());
        }
        sheet.Apply(); string dir = Path.Combine(Application.dataPath, "../HeroShots"); Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "hero_close_" + System.DateTime.Now.ToString("HHmmss") + ".jpg"), sheet.EncodeToJPG(90));
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet);
        Debug.Log("Ashen Hollow: hero close-up sheet " + (n - 1));
    }
}
