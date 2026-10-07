// Ashen Hollow test tool: every gear set in the game, worn by the class it is made for (told by the set's weapon:
// blades for the warrior, fire staffs for the mage, maces for the priest, daggers for the rogue, bows for the ranger,
// leaf staffs for the druid; the Kingsflame sets by their class). One picture sheet per class in HeroShots/:
// sets_<class>.png, a row per set: man front, man back, woman front, woman back. sets_index.txt names the rows.
// Play mode only; nothing is given to the hero.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHSetShots
{
    static readonly string[] Classes = { "warrior", "mage", "priest", "rogue", "ranger", "druid" };
    static readonly string[] Slots = { "head", "shoulders", "chest", "hands", "legs", "feet", "cape" };
    static int ci, frame; static List<string> rows; static readonly List<GameObject> stands = new List<GameObject>();
    static Dictionary<string, Dictionary<string, string>> sets; static System.Text.StringBuilder index;

    static string ClassOf(string set, Dictionary<string, string> pieces)
    {
        if (set.StartsWith("kf_")) return set.Substring(3);
        string w; if (!pieces.TryGetValue("weapon", out w)) return "warrior";   // the starter Shadowguard set
        var it = AHItems.Get(w); string f = it != null ? it.form : "";
        switch (f)
        {
            case "cleaver": case "sword": return "warrior";
            case "magmastaff": case "staff": return "mage";
            case "sunmace": case "sceptre": case "mace": return "priest";
            case "dagger": case "kris": return "rogue";
            case "bow": case "longbow": return "ranger";
            default: return "druid";
        }
    }

    [MenuItem("Ashen Hollow/Test: Set Shots (every set, by class)")]
    static void Run()
    {
        if (!Application.isPlaying || AHGame.I == null) { Debug.Log("Ashen Hollow: Set Shots needs Play mode"); return; }
        sets = new Dictionary<string, Dictionary<string, string>>(); var order = new List<string>();
        foreach (var kv in AHDB.Items)
        {
            var it = AHItems.Get(kv.Key); if (it == null || string.IsNullOrEmpty(it.set) || it.slot == null) continue;
            Dictionary<string, string> d; if (!sets.TryGetValue(it.set, out d)) { d = new Dictionary<string, string>(); sets[it.set] = d; order.Add(it.set); }
            if (!d.ContainsKey(it.slot)) d[it.slot] = kv.Key;
        }
        byClass = new Dictionary<string, List<string>>(); foreach (var c in Classes) byClass[c] = new List<string>();
        foreach (var s in order) { string c = ClassOf(s, sets[s]); if (byClass.ContainsKey(c)) byClass[c].Add(s); }
        index = new System.Text.StringBuilder(); ci = -1;
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "../HeroShots"));
        Next();
    }
    static Dictionary<string, List<string>> byClass;

    static void Next()
    {
        foreach (var s in stands) if (s != null) Object.Destroy(s); stands.Clear();
        ci++;
        if (ci >= Classes.Length)
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/sets_index.txt"), index.ToString());
            Debug.Log("Ashen Hollow: set shots saved to HeroShots/sets_*.png");
            return;
        }
        string cls = Classes[ci]; rows = byClass[cls];
        index.AppendLine(cls + ": " + string.Join(", ", rows.ToArray()));
        for (int i = 0; i < rows.Count; i++)
            for (int sx = 0; sx < 2; sx++)
            {
                var set = sets[rows[i]];
                var holder = new GameObject("SetStand_" + i + "_" + sx).transform; holder.position = new Vector3(5000f + i * 8f, -900f, sx * 8f);
                var look = new AHLook { sex = sx == 0 ? "m" : "f", hair = sx == 0 ? "short" : "pony", hairCol = 2, cloth = 0 };
                AHAnim a; GameObject rig = null;
                try { rig = AHPeople.BuildHero(holder, AHClasses.Get(cls), look, AHGame.I, out a); } catch { a = null; }
                if (rig == null) { Object.Destroy(holder.gameObject); continue; }
                if (set.ContainsKey("chest")) AHCostumes.Apply(rig, AHCostumes.Under, look);   // as the game does: fitted clothes under chest armour
                AHWardrobe.DressWith(rig, holder, slot => { string id; return set.TryGetValue(slot, out id) ? id : null; }, () => false);
                if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
                stands.Add(holder.gameObject);
            }
        frame = Time.frameCount;
        EditorApplication.update -= Step; EditorApplication.update += Step;
    }

    static void Step()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Step; stands.Clear(); return; }
        if (Time.frameCount < frame + 25) return;   // gear settles onto the bones over a few frames
        EditorApplication.update -= Step;
        const int L = 29, W = 220, H = 340;
        foreach (var s in stands) if (s != null) { foreach (var t in s.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = L; foreach (var r in s.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false; }
        var cg = new GameObject("SetShotCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 26f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.68f); cam.cullingMask = 1 << L; cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f;
        var lg = new GameObject("SetShotLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; li.cullingMask = 1 << L; lg.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        var lg2 = new GameObject("SetShotFill"); var l2 = lg2.AddComponent<Light>(); l2.type = LightType.Directional; l2.intensity = 0.6f; l2.cullingMask = 1 << L; lg2.transform.rotation = Quaternion.Euler(30f, -30f, 0f);
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        int n = Mathf.Max(1, rows.Count); var sheet = new Texture2D(W * 4, H * n, TextureFormat.RGB24, false);
        var fill = new Color[W * 4 * H * n]; for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0.3f, 0.3f, 0.32f); sheet.SetPixels(fill);
        for (int i = 0; i < rows.Count; i++)
            for (int sx = 0; sx < 2; sx++)
            {
                var st = stands.Find(o => o != null && o.name == "SetStand_" + i + "_" + sx); if (st == null) continue;
                float[] yaw = { 0f, 180f };   // front, then back
                for (int v = 0; v < 2; v++)
                {
                    Vector3 c = st.transform.position + Vector3.up * 0.95f;
                    cam.transform.position = c + Quaternion.Euler(0f, yaw[v], 0f) * Vector3.back * -4.6f; cam.transform.LookAt(c);
                    foreach (var r in st.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false;   // the far-away culling must not hide the stand
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                    sheet.SetPixels((sx * 2 + v) * W, (n - 1 - i) * H, W, H, tex.GetPixels());
                }
            }
        sheet.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/sets_" + Classes[ci] + ".png"), sheet.EncodeToPNG());
        Object.Destroy(sheet); cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(lg2); Object.Destroy(rt); Object.Destroy(tex);
        Next();
    }
}
