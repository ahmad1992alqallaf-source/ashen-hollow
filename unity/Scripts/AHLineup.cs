// Ashen Hollow (editor test): every class, man and woman, in its starter outfit, standing idle in a row far above
// the land; after a moment each one is photographed from the front three-quarters and the side into
// HeroShots/lineup_<class>_<sex>.png, then they are all removed.
using System.Collections.Generic;
using UnityEngine;

public class AHLineup : MonoBehaviour
{
    static readonly string[] Classes = { "warrior", "mage", "priest", "rogue", "ranger", "druid", "shaman" };
    readonly List<Transform> holders = new List<Transform>();
    readonly List<AHAnim> anims = new List<AHAnim>();
    readonly List<string> names = new List<string>();
    float t;

    public static bool Feet;   // close-ups of the feet while running (to check boots)
    public static void Run(AHGame g, bool feet = false)
    {
        Feet = feet; var go = new GameObject("Lineup"); go.AddComponent<AHLineup>().Build(g);
    }

    void Build(AHGame g)
    {
        Vector3 at = g.player.transform.position + Vector3.up * 400f;
        int n = 0;
        foreach (var id in Classes)
            foreach (var sex in new[] { "m", "f" })
            {
                var cls = AHClasses.Get(id); if (cls == null) continue;
                var look = new AHLook { sex = sex, hair = sex == "f" ? "long" : "short", brow = "arched" };
                var h = new GameObject("Hero_" + id + "_" + sex).transform; h.SetParent(transform, false);
                h.position = at + Vector3.right * (n++ * 6f); h.rotation = Quaternion.identity;
                AHAnim a; var rig = AHPeople.BuildHero(h, cls, look, g, out a);
                if (rig == null) { Destroy(h.gameObject); continue; }
                var kit = AHPlayer.Starter(id);
                AHWardrobe.DressWith(rig, h, s => { string v; return kit.TryGetValue(s, out v) && AHItems.Get(v ?? "") != null ? v : null; }, () => false);
                if (a != null) a.Play(Feet ? "Running_A" : "Idle", true);
                holders.Add(h); anims.Add(a); names.Add(id + "_" + sex);
                if (sex == "m" && (id == "ranger" || id == "mage"))
                {
                    var sb = new System.Text.StringBuilder(id + " materials: ");
                    foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
                        if (r.name != "Piece") foreach (var m in r.sharedMaterials) if (m != null) sb.Append(r.name + "/" + m.name + "[" + m.shader.name + "] " + (m.HasProperty("baseColorFactor") ? ColorUtility.ToHtmlStringRGB(m.GetColor("baseColorFactor")) : "-") + (m.HasProperty("baseColorTexture") && m.GetTexture("baseColorTexture") != null ? " tex" : "") + "; ");
                    System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../HeroShots/mats_" + id + ".txt"), sb.ToString());
                }
            }
    }

    void Update()
    {
        foreach (var a in anims) if (a != null) a.Tick(Time.deltaTime);
        t += Time.deltaTime; if (t < 0.6f) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var go = new GameObject("ShotCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 30f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.66f);
        var rt = new RenderTexture(480, 480, 24); cam.targetTexture = rt;
        var sheet = new Texture2D(480 * 2, 480, TextureFormat.RGB24, false); var tex = new Texture2D(480, 480, TextureFormat.RGB24, false);
        for (int i = 0; i < holders.Count; i++)
        {
            Vector3 c = holders[i].position + Vector3.up * (Feet ? 0.3f : 0.95f);
            float[] yaw = { 30f, 90f };
            for (int v = 0; v < 2; v++)
            {
                Vector3 d = Quaternion.Euler(0, yaw[v], 0) * holders[i].forward;
                cam.transform.position = c + d * (Feet ? 1.6f : 4.2f) + Vector3.up * 0.3f; cam.transform.LookAt(c);
                cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 480, 480), 0, 0); tex.Apply(); RenderTexture.active = null;
                sheet.SetPixels(v * 480, 0, 480, 480, tex.GetPixels());
            }
            sheet.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "lineup_" + names[i] + ".png"), sheet.EncodeToPNG());
        }
        cam.targetTexture = null; Destroy(go); Destroy(rt); Destroy(tex); Destroy(sheet);
        foreach (var a in anims) if (a != null) a.Dispose();
        Debug.Log("Ashen Hollow: class lineup saved (" + holders.Count + " heroes) to " + System.IO.Path.GetFullPath(dir));
        Destroy(gameObject);
    }
}
