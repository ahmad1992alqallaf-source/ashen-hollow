// Ashen Hollow (editor test): "Test: Wing Shots" (shift+alt+U). Films the hero's wings from behind and from the side as
// a strip of frames: standing (a stretch is set off), running in place and flying (forced through AHWingRig's test
// switches). HeroShots/wings.jpg: a row each (stand, run, fly), eight frames a row, back view over side view.
// HeroShots/wings_only.txt ("wing_emberplume,wing_nightveil", or "crest" for all six crest wings) puts each of those on
// in turn and films it: HeroShots/wings_<id>.jpg.
using UnityEditor;
using UnityEngine;

public static class AHWingShots
{
    static bool close;
    public static readonly string[] Crest = { "wing_emberplume", "wing_glacierfan", "wing_thunderfork", "wing_grovebloom", "wing_nightveil", "wing_dawnfeather" };
    [MenuItem("Ashen Hollow/Test: Wing Shots #&u")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string only = System.IO.Path.Combine(Application.dataPath, "../HeroShots/wings_only.txt");
        string[] list = null; close = false;
        if (System.IO.File.Exists(only))
        {
            var l = System.IO.File.ReadAllText(only).Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            // "close": filmed near, frame after frame, to judge the movement
            close = System.Array.IndexOf(l, "close") >= 0; if (close) l = System.Array.FindAll(l, x => x != "close");
            if (l.Length == 1 && l[0] == "crest") l = Crest;
            if (l.Length > 0) list = l;
        }
        g.StartCoroutine(list == null ? Film(g, "wings") : Each(g, list));
    }

    static System.Collections.IEnumerator Each(AHGame g, string[] list)
    {
        var p = g.player; string was = AHWardrobe.Cos(p, "cape");
        foreach (var id in list)
        {
            AHWardrobe.Collect(p, id); AHWardrobe.SetCos(p, "cape", id); p.CheckOutfit();
            yield return new WaitForSeconds(2.5f);
            yield return Film(g, "wings_" + id);
        }
        AHWardrobe.SetCos(p, "cape", was); p.CheckOutfit();
        Debug.Log("Ashen Hollow: wing shots done (" + list.Length + " wings)");
    }

    static System.Collections.IEnumerator Film(AHGame g, string name)
    {
        const int W = 300, H = 300, N = 8;
        var sheet = new Texture2D(W * N, H * 6, TextureFormat.RGB24, false);
        var go = new GameObject("WingCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 34f; cam.nearClipPlane = 0.05f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; cam.cullingMask = g.cam.cullingMask; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
        var hero = g.player.transform;
        // turned so nothing stands between the camera behind and the hero (a house behind filled the back view)
        {
            Vector3 at0 = hero.position + Vector3.up * 1.3f; float best = -1f; Quaternion turn = hero.rotation;
            for (int a = 0; a < 360; a += 30)
            {
                var q = Quaternion.Euler(0f, hero.eulerAngles.y + a, 0f); Vector3 back = q * Vector3.back, side = (q * Vector3.right * 0.9f - q * Vector3.forward * 0.6f).normalized;
                float c = 0f; RaycastHit h;
                c += Physics.Raycast(at0, (back * 6.5f + Vector3.up * 1.6f).normalized, out h, 8f, ~0, QueryTriggerInteraction.Ignore) ? h.distance : 8f;
                c += Physics.Raycast(at0, (side * 6.5f + Vector3.up * 1.2f).normalized, out h, 8f, ~0, QueryTriggerInteraction.Ignore) ? h.distance : 8f;
                if (c > best + 0.01f) { best = c; turn = q; }
            }
            hero.rotation = turn;
        }
        for (int row = 0; row < 3; row++)
        {
            AHWingRig.TestAir = row == 2 ? 1f : 0f; AHWingRig.TestRun = row == 1 ? 1f : 0f; AHWingRig.TestStretch = row == 0;
            yield return new WaitForSeconds(row == 0 ? 0.3f : 1.2f);
            AHWingRig.TestStretch = false;
            float step = row == 0 ? 0.42f : row == 1 ? 0.12f : 0.09f; if (close) step = row == 0 ? 0.4f : 0.075f;
            for (int i = 0; i < N; i++)
            {
                Vector3 at = hero.position + Vector3.up * 1.3f;
                for (int v = 0; v < 2; v++)
                {
                    Vector3 d = v == 0 ? -hero.forward * (close ? 3.2f : 5f) + Vector3.up * (close ? 1.6f : 3f) : (hero.right * 0.9f - hero.forward * 0.6f).normalized * (close ? 3.6f : 6.5f) + Vector3.up * (close ? 0.6f : 1.2f);
                    cam.transform.position = at + d; cam.transform.LookAt(at);
                    cam.Render(); RenderTexture.active = rt; sheet.ReadPixels(new Rect(0, 0, W, H), i * W, (5 - (row * 2 + v)) * H); RenderTexture.active = null;
                }
                yield return new WaitForSeconds(step);
            }
        }
        AHWingRig.TestAir = 0f; AHWingRig.TestRun = 0f; AHWingRig.TestStretch = false;
        sheet.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../HeroShots/" + name + ".jpg"), sheet.EncodeToJPG(85));
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(sheet);
        Debug.Log("Ashen Hollow: wing shots saved (HeroShots/" + name + ".jpg)");
    }
}
