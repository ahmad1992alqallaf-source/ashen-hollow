// Ashen Hollow (editor test): "Test: Collection Suit Shots". Every collection suit made so far (Tripo models in
// Resources/AH/Models/Outfits/tqo_<collection>_<class>[_f]) worn by its class, man and woman, with the whole set and
// its weapon, as the game dresses a hero. Each hero is photographed standing (front, back) and mid-run (side, back
// three-quarters), once lit for looking at and once on bright magenta with nothing else drawn (any magenta inside the
// outline is a hole you can see through). Saved to HeroShots/suits/<collection>_<class>_<sex>_<view>[_m].png.
// Play mode only; nothing is given to the hero.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHSuitShots
{
    static readonly string[] Colls = { "draconic", "demonic", "lava", "fossil" };
    static readonly string[] Classes = { "warrior", "warden", "mage", "priest", "rogue", "ranger", "druid", "shaman" };
    static readonly string[] Slots = { "head", "shoulders", "chest", "hands", "legs", "feet", "cape", "weapon" };
    const int L = 29, W = 360, H = 520;
    static int ci, frame, phase; static string dir;
    static readonly List<GameObject> stands = new List<GameObject>();
    static readonly List<AHAnim> anims = new List<AHAnim>();
    static readonly List<string> names = new List<string>();

    public static string Only;   // one collection only (null: all)

    [MenuItem("Ashen Hollow/Test: Collection Suit Shots %&p")]
    static void Run()
    {
        if (!Application.isPlaying || AHGame.I == null) { Debug.Log("Ashen Hollow: Suit Shots needs Play mode"); return; }
        dir = Path.Combine(Application.dataPath, "../HeroShots/suits"); Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
        ci = -1; Next();
    }

    static bool HasSuit(string coll, string cls)
    {
        foreach (var s in new[] { "", "_f" }) if (Resources.Load<GameObject>("AH/Models/Outfits/tqo_" + coll + "_" + cls + s) != null) return true;
        if (cls == "warden") return HasSuit(coll, "warrior");   // the Warden wears the Warrior's plate with its own hammer
        return false;
    }

    static void Clear()
    {
        foreach (var a in anims) if (a != null) a.Dispose();
        foreach (var s in stands) if (s != null) Object.Destroy(s);
        stands.Clear(); anims.Clear(); names.Clear();
    }

    static void Next()
    {
        Clear();
        while (true)
        {
            ci++;
            if (ci >= Colls.Length) { Debug.Log("Ashen Hollow: suit shots saved to " + Path.GetFullPath(dir)); return; }
            if (Only != null && Colls[ci] != Only) continue;
            bool any = false; foreach (var c in Classes) if (HasSuit(Colls[ci], c)) any = true;
            if (any) break;
        }
        string coll = Colls[ci]; int n = 0;
        foreach (var cls in Classes)
        {
            if (!HasSuit(coll, cls)) continue;
            foreach (var sex in new[] { "m", "f" })
            {
                var set = new Dictionary<string, string>();
                foreach (var sl in Slots) { string id = coll + "_" + cls + "_" + sl; if (AHItems.Get(id) != null) set[sl] = id; }
                var holder = new GameObject("SuitStand_" + n).transform; holder.position = new Vector3(5000f + n * 6f, -900f, 0f); n++;
                var look = new AHLook { sex = sex, hair = sex == "m" ? "short" : "pony", hairCol = 2, cloth = 0 };
                AHAnim a = null; GameObject rig = null;
                try { rig = AHPeople.BuildHero(holder, AHClasses.Get(cls), look, AHGame.I, out a); } catch (System.Exception e) { Debug.LogWarning("suit shots: " + e.Message); }
                if (rig == null) { Object.Destroy(holder.gameObject); continue; }
                if (set.ContainsKey("chest")) AHCostumes.Apply(rig, AHCostumes.Under, look);
                AHWardrobe.DressWith(rig, holder, slot => { string id; return set.TryGetValue(slot, out id) ? id : null; }, () => false, sex, cls);
                if (a != null) a.Play("Idle", true);
                stands.Add(holder.gameObject); anims.Add(a); names.Add(coll + "_" + cls + "_" + sex);
            }
        }
        frame = Time.frameCount; phase = 0;
        EditorApplication.update -= Step; EditorApplication.update += Step;
    }

    static void Step()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Step; stands.Clear(); anims.Clear(); return; }
        foreach (var a in anims) if (a != null) a.Tick(1f / 60f);
        if (phase == 0 && Time.frameCount >= frame + 45) { Shoot(new[] { 0f, 180f }, new[] { "front", "back" }); foreach (var a in anims) if (a != null) a.Play("Running_A", true); frame = Time.frameCount; phase = 1; return; }
        if (phase == 1 && Time.frameCount >= frame + 14) { Shoot(new[] { 90f, 215f }, new[] { "runside", "runback" }); EditorApplication.update -= Step; Next(); }
    }

    static void Shoot(float[] yaws, string[] views)
    {
        var cg = new GameObject("SuitShotCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 26f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 30f; cam.cullingMask = 1 << L;
        var lg = new GameObject("SuitShotLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; li.cullingMask = 1 << L; lg.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        var lg2 = new GameObject("SuitShotFill"); var l2 = lg2.AddComponent<Light>(); l2.type = LightType.Directional; l2.intensity = 0.7f; l2.cullingMask = 1 << L; lg2.transform.rotation = Quaternion.Euler(25f, -30f, 0f);
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        for (int i = 0; i < stands.Count; i++)
        {
            var st = stands[i]; if (st == null) continue;
            // only this stand on the shot layer
            for (int j = 0; j < stands.Count; j++) if (stands[j] != null) foreach (var t in stands[j].GetComponentsInChildren<Transform>(true)) t.gameObject.layer = j == i ? L : 0;
            foreach (var r in st.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false;
            Vector3 c = st.transform.position + Vector3.up * 0.95f;
            for (int v = 0; v < yaws.Length; v++)
            {
                cam.transform.position = c + Quaternion.Euler(0f, yaws[v], 0f) * st.transform.forward * 4.4f + Vector3.up * 0.25f; cam.transform.LookAt(c);
                foreach (var mag in new[] { false, true })
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = mag ? new Color(1f, 0f, 1f) : new Color(0.55f, 0.6f, 0.68f);
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, names[i] + "_" + views[v] + (mag ? "_m" : "") + ".png"), tex.EncodeToPNG());
                }
            }
        }
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(lg2); Object.Destroy(rt); Object.Destroy(tex);
    }
}
