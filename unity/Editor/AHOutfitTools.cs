// Ashen Hollow: "VRoid: Outfit Shots" puts every Quaternius outfit (Knight, Knight in cloth, Noble, Peasant, Ranger,
// Wizard) on a male and a female VRoid hero and photographs each from the front, the side and the back
// (HeroShots/outfits_m.png and outfits_f.png). Play mode only.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHOutfitTools
{
    static readonly string[] Outfits = { "Knight", "Knight_Cloth", "Noble", "Peasant", "Ranger", "Wizard" };
    static readonly string[] Classes = { "warrior", "warrior", "priest", "druid", "ranger", "mage" };
    static readonly List<GameObject> stands = new List<GameObject>();
    static int frame, stage;

    [MenuItem("Ashen Hollow/VRoid: Outfit Shots")]
    static void OutfitShots()
    {
        if (!Application.isPlaying || AHGame.I == null) { Debug.Log("Ashen Hollow: Outfit Shots needs Play mode"); return; }
        foreach (var s in stands) if (s != null) Object.Destroy(s);
        stands.Clear();
        string[] hairs = { "pony", "short", "long", "braid", "bun", "twin" };
        for (int sx = 0; sx < 2; sx++)
            for (int i = 0; i < Outfits.Length; i++)
            {
                var holder = new GameObject("OutfitStand_" + sx + "_" + i).transform;
                holder.position = new Vector3(4400f + i * 12f, -900f, sx * 12f);
                var look = new AHLook { sex = sx == 0 ? "m" : "f", hair = hairs[i], hairCol = 2 + i, skin = (i * 2) % 9 };
                AHAnim a; var rig = AHPeople.BuildHero(holder, AHClasses.Get(Classes[i]), look, AHGame.I, out a);
                if (rig == null) { Object.Destroy(holder.gameObject); continue; }
                if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
                stands.Add(holder.gameObject);
            }
        frame = Time.frameCount; stage = 0;
        EditorApplication.update -= Step; EditorApplication.update += Step;
    }

    static void Step()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Step; stands.Clear(); return; }
        if (stage == 0)
        {
            if (Time.frameCount < frame + 15) return;   // the VRoid bodies are set up over a few frames
            foreach (var st in stands)
            {
                if (st == null) continue;
                var p = st.name.Split('_'); int sx = int.Parse(p[1]), i = int.Parse(p[2]);
                var link = st.GetComponentInChildren<AHVRoidLink>(true);
                GameObject rig = link != null ? link.gameObject : null;
                if (rig == null) foreach (var t in st.GetComponentsInChildren<Transform>(true)) if (t.name == "pelvis") { var r = t; while (r.parent != null && r.parent != st.transform) r = r.parent; rig = r.gameObject; break; }
                foreach (var t in st.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Outfit_")) t.gameObject.SetActive(false);
                var worn = AHQOutfit.Wear(rig, "qo" + (sx == 0 ? "Male_" : "Female_") + Outfits[i]);
                Debug.Log("Ashen Hollow: outfit stand " + st.name + " " + (worn != null ? "dressed" : "FAILED") + (link != null ? " (VRoid)" : " (classic)"));
            }
            stage = 1; frame = Time.frameCount; return;
        }
        if (Time.frameCount < frame + 10) return;
        EditorApplication.update -= Step;
        const int L = 29, W = 300, H = 480;
        foreach (var s in stands) if (s != null) { foreach (var t in s.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = L; foreach (var r in s.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false; }
        // the outfits sit beside their stands (under the rig's parent), so take those too
        var cg = new GameObject("OutfitShotCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 26f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.68f); cam.cullingMask = 1 << L; cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
        var lg = new GameObject("OutfitShotLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; li.cullingMask = 1 << L; lg.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        int n = Outfits.Length;
        for (int sx = 0; sx < 2; sx++)
        {
            var sheet = new Texture2D(W * n, H * 3, TextureFormat.RGB24, false);
            for (int i = 0; i < n; i++)
            {
                var st = stands.Find(o => o != null && o.name == "OutfitStand_" + sx + "_" + i); if (st == null) continue;
                float[] yaw = { 180f, 270f, 0f };
                for (int v = 0; v < 3; v++)
                {
                    Vector3 c = st.transform.position + Vector3.up * 0.95f;
                    cam.transform.position = c + Quaternion.Euler(0f, yaw[v], 0f) * Vector3.back * -4.6f; cam.transform.LookAt(c);
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                    sheet.SetPixels(i * W, (2 - v) * H, W, H, tex.GetPixels());
                }
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/outfits_" + (sx == 0 ? "m" : "f") + ".png"), sheet.EncodeToPNG());
            Object.Destroy(sheet);
        }
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(rt); Object.Destroy(tex);
        foreach (var s in stands) if (s != null) Object.Destroy(s);
        stands.Clear();
        Debug.Log("Ashen Hollow: outfit shots saved to HeroShots/outfits_m.png and outfits_f.png");
    }
}
