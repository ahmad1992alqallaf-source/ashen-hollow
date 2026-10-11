// Ashen Hollow (editor test): "Test: Creator Look Shots" (shift+alt+H). The hero as the character creator shows it, in
// each class's starting outfit, with every hairstyle, man and woman: one sheet a class,
// HeroShots/creator/<class>.png, a row a hairstyle; columns: man head front, man head back, woman head front, woman head
// back, man whole, woman whole. For finding hoods that leave a gap or an empty space round the head, hair poking
// through, and odd pieces of the outfits. Play mode only; nothing is given to the hero.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHCreatorShots
{
    static readonly string[] Classes = { "warrior", "warden", "mage", "priest", "rogue", "ranger", "druid", "shaman" };
    static readonly string[] Hairs = { "pony", "short", "long", "braid", "bun", "twin", "bob", "mohawk", "spiky", "mane", "shaved", "bald" };
    const int L = 29, S = 240;
    static int ci, frame; static string dir;
    static readonly List<GameObject> stands = new List<GameObject>();
    static readonly List<AHAnim> anims = new List<AHAnim>();
    public static string Only;

    [MenuItem("Ashen Hollow/Test: Creator Look Shots #&h")]
    static void Run()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        dir = Path.Combine(Application.dataPath, "../HeroShots/creator"); Directory.CreateDirectory(dir);
        string tf = Path.Combine(dir, "only.txt"); Only = File.Exists(tf) ? File.ReadAllText(tf).Trim() : null; if (Only == "") Only = null;
        ci = -1; Next();
    }

    static void Clear()
    {
        // the pose component disposes each hero's animation with it
        foreach (var s in stands) if (s != null) Object.Destroy(s);
        stands.Clear(); anims.Clear();
    }

    static void Next()
    {
        Clear();
        do ci++; while (ci < Classes.Length && Only != null && Classes[ci] != Only);
        if (ci >= Classes.Length) { Debug.Log("Ashen Hollow: creator shots saved to " + Path.GetFullPath(dir)); return; }
        string cls = Classes[ci]; int n = 0;
        foreach (var hair in Hairs)
            foreach (var sex in new[] { "m", "f" })
            {
                var holder = new GameObject("CreatorStand_" + n).transform; holder.position = new Vector3(7000f + n * 6f, -900f, 0f); n++;
                var look = new AHLook { sex = sex, hair = hair, hairCol = 2, cloth = 0, beard = sex == "m" && (hair == "short" || hair == "mane") ? "short" : "none" };
                AHAnim a = null; GameObject rig = null;
                try { rig = AHPeople.BuildHero(holder, AHClasses.Get(cls), look, AHGame.I, out a); } catch (System.Exception e) { Debug.LogWarning("creator shots: " + e.Message); }
                if (rig != null)
                {
                    // as a new hero: the class's own starting outfit, no gear
                    var kit = AHPlayer.Starter(cls); string ks;
                    try { AHWardrobe.DressWith(rig, holder, s => kit.TryGetValue(s, out ks) ? ks : null, () => false, sex, cls); } catch (System.Exception e) { Debug.LogWarning("creator shots dress: " + e.Message); }
                    if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
                }
                stands.Add(holder.gameObject); anims.Add(a);
            }
        frame = Time.frameCount;
        EditorApplication.update -= Step; EditorApplication.update += Step;
    }

    static void Step()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Step; stands.Clear(); anims.Clear(); return; }
        if (Time.frameCount < frame + 40) return;
        EditorApplication.update -= Step;
        Shoot();
        Next();
    }

    static Transform HeadOf(GameObject st)
    {
        var link = st.GetComponentInChildren<AHVRoidLink>(true);
        if (link != null && link.Body != null) { var an = link.Body.GetComponent<Animator>(); if (an != null && an.isHuman) { var h = an.GetBoneTransform(HumanBodyBones.Head); if (h != null) return h; } }
        foreach (var t in st.GetComponentsInChildren<Transform>(true)) if (t.name == "Head") return t;
        return null;
    }

    static void Shoot()
    {
        var sheet = new Texture2D(S * 6, S * Hairs.Length, TextureFormat.RGB24, false);
        var px = sheet.GetPixels(); for (int i = 0; i < px.Length; i++) px[i] = new Color(0.2f, 0.2f, 0.22f); sheet.SetPixels(px);
        var cg = new GameObject("CreatorCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.02f; cam.farClipPlane = 30f; cam.cullingMask = 1 << L;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.68f);
        var lg = new GameObject("CrLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; li.cullingMask = 1 << L; lg.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        var lg2 = new GameObject("CrFill"); var l2 = lg2.AddComponent<Light>(); l2.type = LightType.Directional; l2.intensity = 0.8f; l2.cullingMask = 1 << L; lg2.transform.rotation = Quaternion.Euler(25f, -30f, 0f);
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt;
        for (int i = 0; i < stands.Count; i++)
        {
            var st = stands[i]; if (st == null) continue;
            for (int j = 0; j < stands.Count; j++) if (stands[j] != null) foreach (var t in stands[j].GetComponentsInChildren<Transform>(true)) t.gameObject.layer = j == i ? L : 0;
            foreach (var r in st.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false;
            int row = i / 2, fem = i % 2;
            var head = HeadOf(st); Vector3 hc = head != null ? head.position + Vector3.up * 0.08f : st.transform.position + Vector3.up * 1.6f;
            Vector3 fwd = st.transform.forward, right = st.transform.right;
            var shots = new[] {
                new { at = hc, dir = (fwd * 0.9f + right * 0.35f + Vector3.up * 0.1f).normalized, dist = 1.0f, col = fem * 2 },
                new { at = hc, dir = (-fwd * 0.85f + right * 0.5f + Vector3.up * 0.25f).normalized, dist = 1.0f, col = fem * 2 + 1 },
                new { at = st.transform.position + Vector3.up * 0.95f, dir = (fwd * 0.9f + right * 0.3f + Vector3.up * 0.1f).normalized, dist = 4.0f, col = 4 + fem } };
            foreach (var sh in shots)
            {
                cam.transform.position = sh.at + sh.dir * sh.dist; cam.transform.LookAt(sh.at); cam.Render();
                RenderTexture.active = rt; sheet.ReadPixels(new Rect(0, 0, S, S), sh.col * S, (Hairs.Length - 1 - row) * S); RenderTexture.active = null;
            }
        }
        sheet.Apply(); File.WriteAllBytes(Path.Combine(dir, Classes[ci] + ".png"), sheet.EncodeToPNG());
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(lg2); Object.Destroy(rt); Object.Destroy(sheet);
    }
}
