// Ashen Hollow (editor test): "Test: Weapon Grip Shots". Every class's weapon (and off-hand piece) in the hero's hands:
// the class's own weapon, then each collection's (draconic, demonic, lava, fossil, starfallen). Each hero is posed
// standing, running and mid-attack and photographed close on the hands, so a weapon held by its blade, floating off the
// fist, pointing the wrong way or sunk through the arm shows. One sheet a collection,
// HeroShots/grips/grip_<collection>.png: a row a class (warrior, warden, mage, priest, rogue, ranger, druid, shaman);
// columns: weapon hand idle front, idle side, running, attack; the off hand (shield, book, second dagger). Play mode only; nothing is given to the hero.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHGripShots
{
    static readonly string[] Colls = { "base", "draconic", "demonic", "lava", "fossil", "starfallen" };
    static readonly string[] Classes = { "warrior", "warden", "mage", "priest", "rogue", "ranger", "druid", "shaman" };
    const int L = 29, S = 300;
    static int ci, pose, frame; static string dir;
    static readonly List<GameObject> stands = new List<GameObject>();
    static readonly List<AHAnim> anims = new List<AHAnim>();
    static readonly List<string> clsOf = new List<string>();
    static Texture2D sheet; static string only;

    [MenuItem("Ashen Hollow/Test: Weapon Grip Shots #&g")]
    static void Run()
    {
        if (!Application.isPlaying || AHGame.I == null) { Debug.Log("Ashen Hollow: Grip Shots needs Play mode"); return; }
        dir = Path.Combine(Application.dataPath, "../HeroShots/grips"); Directory.CreateDirectory(dir);
        // trial settings (HeroShots/grips/tune.txt, lines like "staff=60", "fist=-0.9", "only=lava")
        only = null; string tf = Path.Combine(dir, "tune.txt");
        if (File.Exists(tf)) foreach (var line in File.ReadAllLines(tf))
        {
            var kv = line.Split('='); if (kv.Length != 2) continue; string k = kv[0].Trim(), v = kv[1].Trim(); float f;
            if (k == "only") only = v.Length > 0 ? v : null;
            else if (float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
            { if (k == "staff") AHPeople.StaffTilt = f; else if (k == "fist") AHVRoidLink.FistCurl = f; else if (k == "thumb") AHVRoidLink.ThumbCurl = f; }
        }
        ci = -1; Next();
    }

    static void Clear()
    {
        foreach (var a in anims) if (a != null) a.Dispose();
        foreach (var s in stands) if (s != null) Object.Destroy(s);
        stands.Clear(); anims.Clear(); clsOf.Clear();
    }

    static void Next()
    {
        Clear();
        ci++;
        while (ci < Colls.Length && only != null && Colls[ci] != only) ci++;
        if (ci >= Colls.Length) { Debug.Log("Ashen Hollow: grip shots saved to " + Path.GetFullPath(dir)); return; }
        string coll = Colls[ci]; int n = 0;
        foreach (var cls in Classes)
        {
            var holder = new GameObject("GripStand_" + n).transform; holder.position = new Vector3(6000f + n * 8f, -900f, 0f); n++;
            var look = new AHLook { sex = "m", hair = "short", hairCol = 2, cloth = 0 };
            AHAnim a = null; GameObject rig = null;
            try { rig = AHPeople.BuildHero(holder, AHClasses.Get(cls), look, AHGame.I, out a); } catch (System.Exception e) { Debug.LogWarning("grip shots: " + e.Message); }
            if (rig == null) { Object.Destroy(holder.gameObject); stands.Add(null); anims.Add(null); clsOf.Add(cls); continue; }
            if (coll != "base")
            {
                string wid = coll + "_" + cls + "_weapon";
                if (AHItems.Get(wid) != null) AHWardrobe.DressWith(rig, holder, slot => slot == "weapon" ? wid : null, () => false, "m", cls);
            }
            stands.Add(holder.gameObject); anims.Add(a); clsOf.Add(cls);
        }
        sheet = new Texture2D(S * 5, S * Classes.Length, TextureFormat.RGB24, false);
        var px = sheet.GetPixels(); for (int i = 0; i < px.Length; i++) px[i] = new Color(0.2f, 0.2f, 0.22f); sheet.SetPixels(px);
        pose = 0; frame = Time.frameCount;
        EditorApplication.update -= Step; EditorApplication.update += Step;
    }

    // the clip and moment for each column (the attack is the class's own swing or shot)
    static void Pose(AHAnim a, string cls, int p)
    {
        if (a == null) return;
        bool ranged = cls == "mage" || cls == "priest" || cls == "ranger" || cls == "druid" || cls == "shaman";
        string atk = cls == "ranger" && a.Has("Pistol_Shoot") ? "Pistol_Shoot" : ranged ? "Shoot" : a.Has("Sword_Regular_A") ? "Sword_Regular_A" : "1H_Melee_Attack";
        if (!a.Has(atk)) atk = "1H_Melee_Attack";
        if (p <= 1) a.Hold("Idle", 0.3f); else if (p == 2) a.Hold("Running_A", 0.25f); else a.Hold(atk, 0.4f);
        a.Tick(10f);
    }

    static void Step()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Step; stands.Clear(); anims.Clear(); return; }
        for (int i = 0; i < anims.Count; i++) Pose(anims[i], clsOf[i], pose);
        if (Time.frameCount < frame + 5) return;
        Shoot(pose);
        pose = pose == 0 ? 2 : pose + 1;   // the side view (1) is taken with the front one
        frame = Time.frameCount;
        if (pose <= 3) return;
        EditorApplication.update -= Step;
        sheet.Apply(); File.WriteAllBytes(Path.Combine(dir, "grip_" + Colls[ci] + ".png"), sheet.EncodeToPNG()); Object.Destroy(sheet);
        Next();
    }

    static void Shoot(int p)
    {
        var cg = new GameObject("GripCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.02f; cam.farClipPlane = 30f; cam.cullingMask = 1 << L;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.68f);
        var lg = new GameObject("GripLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; li.cullingMask = 1 << L; lg.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        var lg2 = new GameObject("GripFill"); var l2 = lg2.AddComponent<Light>(); l2.type = LightType.Directional; l2.intensity = 0.7f; l2.cullingMask = 1 << L; lg2.transform.rotation = Quaternion.Euler(25f, -30f, 0f);
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt;
        for (int i = 0; i < stands.Count; i++)
        {
            var st = stands[i]; if (st == null) continue;
            for (int j = 0; j < stands.Count; j++) if (stands[j] != null) foreach (var t in stands[j].GetComponentsInChildren<Transform>(true)) t.gameObject.layer = j == i ? L : 0;
            foreach (var r in st.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false;   // far-off figures are culled
            // the hands that hold something (the weapons move onto the VRoid body's hands): right and left by name
            Transform hr = null, hl = null;
            foreach (var t in st.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Weapon_") || t.parent == null) continue;
                string pn = t.parent.name; bool left = pn == "hand_l" || pn.Contains("L_Hand") || pn.Contains("LeftHand") || pn.Contains("Left");
                if (left) hl = t.parent; else hr = t.parent;
            }
            if (hr == null) foreach (var t in st.GetComponentsInChildren<Transform>(true)) if (t.name == "hand_r") hr = t;
            var fwd = st.transform.forward; var right = st.transform.right;
            // columns: the weapon hand from the front three-quarters and from its own side (idle), the weapon hand
            // running, mid-attack; the off hand (idle) shares the side view's frame when it holds something
            var shots = new List<KeyValuePair<Transform, Vector3>>();
            if (p == 0) { shots.Add(new KeyValuePair<Transform, Vector3>(hr, (fwd * 0.8f + right * 0.55f + Vector3.up * 0.15f).normalized)); shots.Add(new KeyValuePair<Transform, Vector3>(hr, (right + fwd * 0.15f + Vector3.up * 0.1f).normalized)); }
            else shots.Add(new KeyValuePair<Transform, Vector3>(hr, (fwd * 0.55f + right * 0.8f + Vector3.up * 0.25f).normalized));
            for (int v = 0; v < shots.Count; v++)
            {
                var hand = shots[v].Key; if (hand == null) continue;
                Bounds b = new Bounds(hand.position, Vector3.one * 0.15f);
                foreach (Transform c in hand) if (c.name.StartsWith("Weapon_")) foreach (var r in c.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                float size = Mathf.Clamp(b.size.magnitude, 0.35f, 2.2f);
                cam.transform.position = b.center + shots[v].Value * (size * 1.7f + 0.25f); cam.transform.LookAt(b.center); cam.Render();
                int col = p == 0 ? v : p;
                RenderTexture.active = rt; sheet.ReadPixels(new Rect(0, 0, S, S), col * S, (Classes.Length - 1 - i) * S); RenderTexture.active = null;
            }
            if (p == 0 && hl != null)
            {   // the off hand, into its own sheet column 4
                bool any = false; Bounds b = new Bounds(hl.position, Vector3.one * 0.15f);
                foreach (Transform c in hl) if (c.name.StartsWith("Weapon_")) foreach (var r in c.GetComponentsInChildren<Renderer>()) { b.Encapsulate(r.bounds); any = true; }
                if (any)
                {
                    float size = Mathf.Clamp(b.size.magnitude, 0.35f, 2.2f);
                    cam.transform.position = b.center + (fwd * 0.8f - right * 0.55f + Vector3.up * 0.15f).normalized * (size * 1.7f + 0.25f); cam.transform.LookAt(b.center); cam.Render();
                    RenderTexture.active = rt; sheet.ReadPixels(new Rect(0, 0, S, S), 4 * S, (Classes.Length - 1 - i) * S); RenderTexture.active = null;
                }
            }
        }
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(lg2); Object.Destroy(rt);
    }
}
