// Ashen Hollow: VRoid heroes. Moves exported .vrm files into Resources/AH/VRoid and writes a report of each one
// (humanoid avatar, height, materials and shaders, bone names) to HeroShots/vroid_<name>.txt.
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHVRoidTools
{
    const string Dir = "Assets/AshenHollow/Resources/AH/VRoid", Src = "Assets/AshenHollow/VRoidSource";

    [MenuItem("Ashen Hollow/VRoid: Collect And Report %&v")]
    static void Collect()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/AshenHollow/Resources/AH", "VRoid");
        if (!AssetDatabase.IsValidFolder(Src)) AssetDatabase.CreateFolder("Assets/AshenHollow", "VRoidSource");
        // the .vrm files themselves stay out of Resources (they would bloat the build); what UniVRM made from them
        // (the prefab and its materials, meshes, textures, avatar) goes into Resources so the game can load it
        foreach (var root in new[] { "Assets", Src, Dir })
        {
            foreach (var f in Directory.GetFiles(root, "*.vrm", SearchOption.TopDirectoryOnly))
                Move(f, Src + "/" + Path.GetFileName(f));
            foreach (var f in Directory.GetFiles(root, "*.prefab", SearchOption.TopDirectoryOnly))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (!File.Exists(Path.Combine(Src, n + ".vrm")) && !File.Exists(Path.Combine(root, n + ".vrm"))) continue;
                if (root != Dir) Move(f, Dir + "/" + Path.GetFileName(f));
                foreach (var d in Directory.GetDirectories(root, n + ".*")) if (root != Dir) Move(d, Dir + "/" + Path.GetFileName(d));
            }
        }
        AssetDatabase.Refresh();
        foreach (var f in Directory.GetFiles(Dir, "*.prefab"))
        {
            string path = f.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var sb = new StringBuilder();
            sb.AppendLine("asset " + path + " root " + (go != null ? go.name : "NULL"));
            if (go != null)
            {
                var an = go.GetComponent<Animator>();
                sb.AppendLine("animator " + (an != null) + " avatar " + (an != null && an.avatar != null ? an.avatar.name + " human=" + an.avatar.isHuman + " valid=" + an.avatar.isValid : "none"));
                foreach (var c in go.GetComponents<Component>()) sb.AppendLine("component " + c.GetType().FullName);
                var b = new Bounds(); bool any = false;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var rb = r is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)r).bounds : r.bounds;
                    if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
                    sb.Append("renderer " + r.name + " " + r.GetType().Name + " mats:");
                    foreach (var m in r.sharedMaterials) sb.Append(" [" + (m != null ? m.name + " / " + (m.shader != null ? m.shader.name : "no shader") : "null") + "]");
                    sb.AppendLine();
                }
                sb.AppendLine("bounds " + b);
                if (an != null && an.avatar != null && an.avatar.isHuman)
                    foreach (HumanBodyBones hb in System.Enum.GetValues(typeof(HumanBodyBones)))
                    {
                        if (hb == HumanBodyBones.LastBone) continue;
                        var t = GetBone(go, an.avatar, hb); if (t != null) sb.AppendLine("bone " + hb + " = " + t.name + " pos=" + t.position.ToString("F3"));
                    }
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/vroid_" + Path.GetFileNameWithoutExtension(f) + ".txt"), sb.ToString());
            Debug.Log("Ashen Hollow: VRoid report written for " + path);
        }
    }

    [MenuItem("Ashen Hollow/VRoid: Dump Hero Bones %&2")]
    static void DumpBones()
    {
        var sb = new StringBuilder();
        foreach (var n in new[] { "qMaleRanger", "qMalePeasant", "qFemalePeasant" })
        {
            var go = Resources.Load<GameObject>("AH/Models/Web/" + n); if (go == null) { sb.AppendLine("missing " + n); continue; }
            sb.AppendLine("== " + n);
            Walk(go.transform, 0, sb);
        }
        File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/hero_bones.txt"), sb.ToString());
        Debug.Log("Ashen Hollow: hero bones written");
    }
    static void Walk(Transform t, int d, StringBuilder sb)
    {
        sb.AppendLine(new string(' ', d) + t.name + " lp=" + t.localPosition.ToString("F3") + " lr=" + t.localRotation.eulerAngles.ToString("F1") + " wp=" + t.position.ToString("F3"));
        foreach (Transform c in t) Walk(c, d + 1, sb);
    }

    static void Move(string from, string to)
    {
        from = from.Replace('\\', '/'); if (from == to) return;
        string err = AssetDatabase.MoveAsset(from, to);
        Debug.Log("Ashen Hollow: moved " + from + " -> " + to + (string.IsNullOrEmpty(err) ? "" : " ERROR " + err));
    }

    static Transform GetBone(GameObject root, Avatar av, HumanBodyBones hb)
    {
        string want = null;
        foreach (var h in av.humanDescription.human) if (h.humanName == HumanTrait.BoneName[(int)hb]) { want = h.boneName; break; }
        if (want == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == want) return t;
        return null;
    }

    // a picture of every VRoid build from the front, the side and the back (HeroShots/vr_<name>.png, the three side by side)
    [MenuItem("Ashen Hollow/VRoid: Model Shots %&b")]
    static void Shots()
    {
        Collect();
        var cg = new GameObject("VRShotCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 24f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.68f);
        var lg = new GameObject("VRShotLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.2f; lg.transform.rotation = Quaternion.Euler(30f, 160f, 0f);
        var rt = new RenderTexture(400, 640, 24); cam.targetTexture = rt; var tex = new Texture2D(400, 640, TextureFormat.RGB24, false);
        foreach (var f in Directory.GetFiles(Dir, "hero_*.prefab"))
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/')); if (pf == null) continue;
            var go = (GameObject)Object.Instantiate(pf, new Vector3(0f, -500f, 0f), Quaternion.identity);
            var sheet = new Texture2D(1200, 640, TextureFormat.RGB24, false);
            float[] yaw = { 180f, 270f, 0f };   // VRoid models face +z: the camera looks from the front, the side, the back
            for (int i = 0; i < 3; i++)
            {
                Vector3 c = go.transform.position + Vector3.up * 0.9f;
                cam.transform.position = c + Quaternion.Euler(0f, yaw[i], 0f) * Vector3.back * -5.2f; cam.transform.LookAt(c);
                cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 400, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
                sheet.SetPixels(i * 400, 0, 400, 640, tex.GetPixels());
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/vr_" + Path.GetFileNameWithoutExtension(f) + ".png"), sheet.EncodeToPNG());
            Object.DestroyImmediate(go); Object.DestroyImmediate(sheet);
        }
        cam.targetTexture = null; Object.DestroyImmediate(cg); Object.DestroyImmediate(lg); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        Debug.Log("Ashen Hollow: VRoid model shots saved to HeroShots/vr_*.png");
    }

    // ---- gear on the VRoid body: full outfits on a male and a female hero, from the back, the side and the front
    // (HeroShots/gear_m.png and gear_f.png). Play mode only; the heroes are built far below the world and removed after.
    static readonly string[][] GearSets =
    {
        new[] { "warrior", "steel_helm", "iron_pauldrons", "steel_plate", "steel_gauntlets", "steel_greaves", "iron_boots", "royal_cape" },
        new[] { "mage", "arcane_hood", "arcane_mantle", "arcane_robe", "arcane_gloves", null, "deer_boots", "magma_wings" },
        new[] { "rogue", "shade_cowl", "wolf_mantle", "leather_vest", "boar_gloves", "shadow_greaves", "shadow_boots", "verdant_quiver" },
        new[] { "warrior", "gold_crown", "forge_pauldrons", "forge_plate", "forge_gauntlets", "forge_greaves", "forge_boots", "forge_cape" },
        new[] { "priest", "wolf_hood", "shadow_mantle", "holy_robe", "holy_gloves", null, "bronze_boots", "void_wings" },
        new[] { "ranger", "corsair_hat", "frost_mantle", "corsair_coat", "boar_gloves", "bronze_greaves", "deer_boots", "shadow_coat" },
        new[] { "druid", "wyrm_horns", null, null, null, null, null, null },
    };
    static readonly System.Collections.Generic.List<GameObject> gearStands = new System.Collections.Generic.List<GameObject>();
    static int gearFrame;

    [MenuItem("Ashen Hollow/VRoid: Gear Shots")]
    static void GearShots()
    {
        if (!Application.isPlaying || AHGame.I == null) { Debug.Log("Ashen Hollow: Gear Shots needs Play mode"); return; }
        foreach (var s in gearStands) if (s != null) Object.Destroy(s);
        gearStands.Clear();
        string[] slots = { "head", "shoulders", "chest", "hands", "legs", "feet", "cape" };
        string[] hairs = { "pony", "long", "short", "braid", "bun", "twin", "bald" };
        for (int sx = 0; sx < 2; sx++)
            for (int i = 0; i < GearSets.Length; i++)
            {
                var set = GearSets[i];
                var holder = new GameObject("GearStand_" + sx + "_" + i).transform;
                holder.position = new Vector3(4000f + i * 12f, -900f, sx * 12f);
                var look = new AHLook { sex = sx == 0 ? "m" : "f", hair = hairs[i], hairCol = 3 + i, cloth = i };
                AHAnim a; var rig = AHPeople.BuildHero(holder, AHClasses.Get(set[0]), look, AHGame.I, out a);
                if (rig == null) { Object.Destroy(holder.gameObject); continue; }
                AHWardrobe.DressWith(rig, holder, s => { int k = System.Array.IndexOf(slots, s); return k >= 0 ? set[k + 1] : null; }, () => false);
                if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
                gearStands.Add(holder.gameObject);
            }
        gearFrame = Time.frameCount;
        EditorApplication.update -= GearStep; EditorApplication.update += GearStep;
    }

    static void GearStep()
    {
        if (!Application.isPlaying) { EditorApplication.update -= GearStep; gearStands.Clear(); return; }
        if (Time.frameCount < gearFrame + 20) return;   // the VRoid link moves the gear onto its bones over a few frames
        EditorApplication.update -= GearStep;
        const int L = 29, W = 300, H = 480;
        foreach (var s in gearStands) if (s != null) { foreach (var t in s.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = L; foreach (var r in s.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false; }
        var cg = new GameObject("GearShotCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 26f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.68f); cam.cullingMask = 1 << L; cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
        var lg = new GameObject("GearShotLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; li.cullingMask = 1 << L; lg.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        for (int sx = 0; sx < 2; sx++)
        {
            int n = GearSets.Length; var sheet = new Texture2D(W * 3, H * n, TextureFormat.RGB24, false);
            for (int i = 0; i < n; i++)
            {
                var st = gearStands.Find(o => o != null && o.name == "GearStand_" + sx + "_" + i); if (st == null) continue;
                float[] yaw = { 180f, 270f, 0f };
                for (int v = 0; v < 3; v++)
                {
                    Vector3 c = st.transform.position + Vector3.up * 0.95f;
                    cam.transform.position = c + Quaternion.Euler(0f, yaw[v], 0f) * Vector3.back * -4.6f; cam.transform.LookAt(c);
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                    sheet.SetPixels(v * W, (n - 1 - i) * H, W, H, tex.GetPixels());
                }
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/gear_" + (sx == 0 ? "m" : "f") + ".png"), sheet.EncodeToPNG());
            Object.Destroy(sheet);
        }
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(rt); Object.Destroy(tex);
        foreach (var s in gearStands) if (s != null) Object.Destroy(s);
        gearStands.Clear();
        Debug.Log("Ashen Hollow: gear shots saved to HeroShots/gear_m.png and gear_f.png");
    }

    // the live hero's VRoid materials: name, shader and colours (to see the dye at work)
    [MenuItem("Ashen Hollow/VRoid: Dump Live Materials")]
    static void DumpLive()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var vr = g.player.transform.Find("VRoid"); var sb = new StringBuilder();
        sb.AppendLine("look hair " + g.player.look.hair + " col " + g.player.look.hairCol + " vroid " + (vr != null ? vr.name : "none"));
        if (vr != null)
            foreach (var r in vr.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue; sb.Append(r.name + " | " + m.name + " | " + m.shader.name);
                    foreach (var pr in new[] { "_Color", "_BaseColor", "_ShadeColor", "_MainTex" }) if (m.HasProperty(pr)) sb.Append(" | " + pr + "=" + (pr == "_MainTex" ? (m.GetTexture(pr) != null ? m.GetTexture(pr).name : "null") : m.GetColor(pr).ToString()));
                    sb.AppendLine();
                }
        File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/vroid_live.txt"), sb.ToString());
        Debug.Log("Ashen Hollow: wrote HeroShots/vroid_live.txt");
    }
}
