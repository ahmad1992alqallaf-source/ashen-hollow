// Ashen Hollow (editor test): "Test: Solid Map Tour" (shift+alt+O). Visits each land and photographs it from straight
// above with what you cannot walk into painted red over it (AHSolidGrid), so walls, crags and houses you could still walk
// through show up as places left unpainted. HeroShots/solid/<area>.jpg. HeroShots/solid_only.txt ("sands,meadow")
// limits it to those lands; "Test: Solid Map Here" does just the land you are in.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHSolidShots
{
    static readonly string[] Areas = { "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead" };
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/solid"); Directory.CreateDirectory(d); return d; } }

    [MenuItem("Ashen Hollow/Test: Solid Map Tour #&o")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string[] list = Areas; string only = Path.Combine(Application.dataPath, "../HeroShots/solid_only.txt");
        if (File.Exists(only)) { var l = File.ReadAllText(only).Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries); if (l.Length > 0) list = l; }
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            var gg = AHGame.I;
            if (!Application.isPlaying || gg == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 9)) { if (t - t0 > 60) want = null; return; }
            if (want != null) Shoot(gg);
            i++;
            if (i >= list.Length) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: solid map tour done (HeroShots/solid)"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = list[i]; t0 = t;
            if (AHGame.AreaId != want) gg.Travel(want, d.spawn.x, d.spawn.z, 0f); else t0 -= 9;
        };
        EditorApplication.update += cb;
    }

    [MenuItem("Ashen Hollow/Test: Solid Map Here")]
    static void Here() { var g = AHGame.I; if (Application.isPlaying && g != null) Shoot(g); }

    static void Shoot(AHGame g)
    {
        var pic = AHSolidGrid.Picture(); if (pic == null) { Debug.Log("Ashen Hollow: no solid map in " + AHGame.AreaId); return; }
        var E = AHSolidGrid.Extent; float gy = g.player.transform.position.y;
        int W = 1600, H = Mathf.Clamp(Mathf.RoundToInt(W * E.height / E.width), 200, 2400);
        var go = new GameObject("SolidCam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.2f; cam.farClipPlane = 2000f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; cam.cullingMask = g.cam.cullingMask; }
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; cam.orthographic = true; cam.aspect = (float)W / H;
        cam.orthographicSize = E.height / 2f;
        go.transform.position = new Vector3(E.center.x, gy + 400f, E.center.y); go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.Render(); RenderTexture.active = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
        RenderSettings.fog = fog;
        var px = tex.GetPixels();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var c = pic.GetPixelBilinear((x + 0.5f) / W, (y + 0.5f) / H);
                if (c.r > 0.5f) px[y * W + x] = Color.Lerp(px[y * W + x], new Color(1f, 0.1f, 0.1f), 0.45f);
            }
        // where the hero stands: a white dot
        Vector3 hp = g.player.transform.position; int hx = Mathf.RoundToInt((hp.x - E.xMin) / E.width * W), hz = Mathf.RoundToInt((hp.z - E.yMin) / E.height * H);
        for (int dy = -5; dy <= 5; dy++) for (int dx = -5; dx <= 5; dx++) { int X = hx + dx, Y = hz + dy; if (X >= 0 && Y >= 0 && X < W && Y < H && dx * dx + dy * dy <= 25) px[Y * W + X] = Color.white; }
        tex.SetPixels(px); tex.Apply();
        File.WriteAllBytes(Path.Combine(Dir, AHGame.AreaId + ".jpg"), tex.EncodeToJPG(85));
        cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
        Debug.Log("Ashen Hollow: solid map " + AHGame.AreaId + " (" + AHSolidGrid.Things + " things, " + AHSolidGrid.Unread + " unreadable)");
    }
}
