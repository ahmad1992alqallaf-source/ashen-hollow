// Ashen Hollow (editor only): the gameplay trailer recorder. "Ashen Hollow/Test: Record Trailer" adds this to the scene;
// it travels from land to land and films a list of shots with its own camera (no buttons or bars in the picture), the
// hero acting each one out: standing by the fire, running the road, fighting whatever is nearest with the class's
// spells. Game time steps exactly 1/30 s a frame while filming, so the film plays smoothly however slow the saving is.
// Frames: HeroShots/trailer/<nn>_<shot>/f_00000.jpg (1280x720). The hero cannot die while filming.
#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

public class AHTrailer : MonoBehaviour
{
    public class Shot
    {
        public string name, area, cam, act; public float secs, a0 = 0f, a1 = 60f, r = 8f, h = 3f;
        public Shot(string name, string area, float secs, string cam, string act) { this.name = name; this.area = area; this.secs = secs; this.cam = cam; this.act = act; }
    }
    // the trailer, in order
    public static Shot[] List =
    {
        new Shot("dawn", "meadow", 5f, "descend", "idle"),
        new Shot("road", "meadow", 4f, "track", "run"),
        new Shot("wings", "meadow", 4.5f, "behind", "run"),
                new Shot("fight", "meadow", 5f, "orbit", "fight") { a0 = -40f, a1 = 50f, r = 7f, h = 2.6f },
        new Shot("sea", "tide", 5f, "vista", "idle") { a0 = 150f, a1 = 205f },
        new Shot("ride", "sands", 3.5f, "ride", "ride"),
        new Shot("lava", "ember", 4.5f, "vista", "run") { a0 = -25f, a1 = 15f },
        new Shot("ember_fight", "ember", 4f, "orbit", "fight") { a0 = 160f, a1 = 220f, r = 6.5f, h = 2.2f },
                new Shot("city", "hc_city", 4f, "track", "run"),
        new Shot("depths", "d_molten", 4.5f, "follow", "run"),
        new Shot("depths_fight", "d_molten", 4f, "orbit", "fight") { a0 = 30f, a1 = 100f, r = 6f, h = 2.4f },
        new Shot("throne", "raid", 6f, "orbit", "fight") { a0 = 200f, a1 = 290f, r = 8f, h = 3.5f },
        new Shot("hero", "meadow", 4.5f, "portrait", "idle") { a0 = -30f, a1 = 30f, r = 3.2f, h = 1.55f },
    };
    public const int W = 1280, H = 720, Fps = 30;
    public static string Only;   // just this shot (by name), for a retake
    public static bool Busy { get { return busy; } }
    static bool busy;

    Camera cam; RenderTexture rt; Texture2D grab; string dir;

    public static void Begin(string root)
    {
        if (busy) return;
        var go = new GameObject("Trailer"); DontDestroyOnLoad(go);
        var t = go.AddComponent<AHTrailer>(); t.dir = root; busy = true;
        t.StartCoroutine(t.Run());
    }

    IEnumerator Run()
    {
        var cg = new GameObject("TrailerCam"); cg.transform.SetParent(transform, false);
        cam = cg.AddComponent<Camera>(); cam.enabled = false; cam.fieldOfView = 42f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 600f;
        var ad = cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); ad.renderPostProcessing = true; ad.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        rt = new RenderTexture(W, H, 24); grab = new Texture2D(W, H, TextureFormat.RGB24, false);
        int n = 0;
        foreach (var s in List)
        {
            n++;
            if (Only != null && System.Array.IndexOf(Only.Split(','), s.name) < 0) continue;   // only.txt: "fight,lava" films just those
            // get there and let the land finish building
            var g = AHGame.I;
            if (AHGame.AreaId != s.area && g != null)
            {
                var ta = Resources.Load<TextAsset>("AH/Areas/" + s.area);
                if (ta == null) { Debug.LogWarning("trailer: no area " + s.area); continue; }
                var d = JsonUtility.FromJson<AHWorldData>(ta.text);
                g.Travel(s.area, d.spawn.x, d.spawn.z, 0f);
                float t0 = Time.realtimeSinceStartup;
                while (AHGame.AreaId != s.area && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            }
            yield return new WaitForSecondsRealtime(9f);
            g = AHGame.I; if (g == null || g.player == null) continue;
            string sd = Path.Combine(dir, n.ToString("00") + "_" + s.name); Directory.CreateDirectory(sd);
            foreach (var f in Directory.GetFiles(sd, "*.jpg")) File.Delete(f);
            yield return Film(g, s, sd);
            Debug.Log("Ashen Hollow: trailer shot " + s.name + " filmed");
        }
        AHInput.TestMove = Vector2.zero; Time.captureFramerate = 0;
        Debug.Log("Ashen Hollow: trailer frames saved to " + Path.GetFullPath(dir));
        busy = false; Destroy(rt); Destroy(gameObject);
    }

    IEnumerator Film(AHGame g, Shot s, string sd)
    {
        var p = g.player; int frames = Mathf.RoundToInt(s.secs * Fps);
        AHMob foe = null;
        if (s.act == "fight")
        {
            // the nearest living enemy; the hero is put a few steps from it, facing it
            // out in the wild if there is anything there (not among the stalls and anvils of a town square)
            float best = 400f;
            foreach (var m in g.mobs) { if (m == null || m.dead || g.InTown(m.transform.position)) continue; float dd = Vector3.Distance(m.transform.position, p.transform.position); if (dd < 25f) continue; if (dd < best) { best = dd; foe = m; } }
            if (foe == null) foreach (var m in g.mobs) { if (m == null || m.dead) continue; float dd = Vector3.Distance(m.transform.position, p.transform.position); if (dd < best) { best = dd; foe = m; } }
            if (foe != null)
            {
                Vector3 to = foe.transform.position - p.transform.position; to.y = 0f; if (to.sqrMagnitude < 0.01f) to = Vector3.forward;
                Vector3 at = g.Resolve(foe.transform.position - to.normalized * 3.5f, 0.4f); at.y = p.transform.position.y;
                p.transform.position = at; p.transform.rotation = Quaternion.LookRotation(to.normalized); p.target = foe;
            }
        }
        // on a mount for a ride shot (and off it again after)
        bool rode = false;
        if (s.act == "ride" && !p.mounted) { AHComp.Mount(g); rode = p.mounted; }
        if (s.act != "ride" && p.mounted) AHComp.Dismount(g, true);
        Vector3 fwd = p.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 start = p.transform.position;
        Time.captureFramerate = Fps;
        for (int f = 0; f < frames; f++)
        {
            float u = frames > 1 ? f / (float)(frames - 1) : 0f, e = u * u * (3f - 2f * u);
            p.hp = p.maxHp; p.mana = p.maxMana; p.dead = false;
            // what the hero does
            Vector3 want = Vector3.zero;
            if (s.act == "run" || s.act == "ride") want = fwd;
            else if (s.act == "fight" && foe != null)
            {
                if (foe.dead) { foe = null; foreach (var m in g.mobs) if (m != null && !m.dead && Vector3.Distance(m.transform.position, p.transform.position) < 25f) { foe = m; p.target = m; break; } }
                if (foe != null)
                {
                    Vector3 to = foe.transform.position - p.transform.position; to.y = 0f;
                    if (to.magnitude > 3f) want = to.normalized;
                    if (f % 24 == 0) p.Cast((f / 24) % Mathf.Max(1, Mathf.Min(4, p.Spells.Length)));
                }
            }
            if (want != Vector3.zero) { Vector3 cf = g.CamForward(), cr = g.CamRight(); AHInput.TestMove = new Vector2(Vector3.Dot(want, cr), Vector3.Dot(want, cf)); }
            else AHInput.TestMove = Vector2.zero;
            yield return new WaitForEndOfFrame();
            // the camera
            Vector3 h = p.transform.position, look = h + Vector3.up * 1.2f, pos;
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            switch (s.cam)
            {
                case "descend": pos = Vector3.Lerp(h - fwd * 45f + Vector3.up * 32f + side * 10f, h - fwd * 6.5f + Vector3.up * 2.6f + side * 2f, e); look = Vector3.Lerp(h + fwd * 12f, h + Vector3.up * 1.3f, e); break;
                case "track": pos = h + side * 4.5f + Vector3.up * 1.7f - fwd * (1.5f - u * 1.5f); look = h + fwd * 2.5f + Vector3.up * 1.2f; break;
                case "ride": pos = h + side * 7.5f + Vector3.up * 2.6f + fwd * (2.5f - u * 2f); look = h + Vector3.up * 1.4f + fwd * 1.2f; break;   // a rider from the side, the whole mount in view
                case "behind": pos = h - fwd * (4.2f - u * 0.8f) + Vector3.up * 2.1f + side * (1.2f - u * 2.4f); look = h + fwd * 3f + Vector3.up * 1.4f; break;   // the wings and the back of the gear
                case "follow": pos = h - fwd * 5.5f + Vector3.up * 2.4f + side * 0.8f; look = h + fwd * 4f + Vector3.up * 1.3f; break;
                case "wide": { float a = Mathf.Lerp(s.a0, s.a1, e) * Mathf.Deg2Rad; pos = h + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * s.r + Vector3.up * s.h; look = h + Vector3.up * 2f; break; }
                case "vista":
                {   // from just behind the hero, out over the land, turning slowly
                    float a = Mathf.Lerp(s.a0, s.a1, e) * Mathf.Deg2Rad; Vector3 dir = fwd * Mathf.Cos(a) + side * Mathf.Sin(a);
                    pos = h - dir * 5.5f + Vector3.up * 3.2f + Vector3.Cross(Vector3.up, dir) * 1.6f; look = h + dir * 35f + Vector3.up * 2f; break;
                }
                case "portrait": { float a = Mathf.Lerp(s.a0, s.a1, e) * Mathf.Deg2Rad; Vector3 face = p.transform.forward; face.y = 0f; face.Normalize(); Vector3 fs = Vector3.Cross(Vector3.up, face); pos = h + (face * Mathf.Cos(a) + fs * Mathf.Sin(a)) * s.r + Vector3.up * s.h; look = h + Vector3.up * 1.35f; break; }
                default:
                {   // orbit round the fight (the hero and the foe both in the picture)
                    Vector3 c = foe != null ? Vector3.Lerp(h, foe.transform.position, 0.4f) : h;
                    float a = Mathf.Lerp(s.a0, s.a1, e) * Mathf.Deg2Rad;
                    pos = c + (fwd * Mathf.Cos(a) + side * Mathf.Sin(a)) * s.r + Vector3.up * s.h; look = c + Vector3.up * 1.3f; break;
                }
            }
            cam.transform.position = pos; cam.transform.LookAt(look);
            if (g.cam != null)
            {   // the game camera's own look (its post-processing, volumes and brightness), only placed elsewhere
                cam.CopyFrom(g.cam); cam.fieldOfView = 42f; cam.nearClipPlane = 0.1f; cam.farClipPlane = Mathf.Max(600f, g.cam.farClipPlane);
                var gd = g.cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); var td = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (gd != null && td != null) { td.renderPostProcessing = true; td.volumeLayerMask = gd.volumeLayerMask; td.volumeTrigger = gd.volumeTrigger != null ? gd.volumeTrigger : g.cam.transform; }
                cam.transform.position = pos; cam.transform.LookAt(look);
            }
            cam.enabled = false; cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; grab.ReadPixels(new Rect(0, 0, W, H), 0, 0); grab.Apply(false); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(sd, "f_" + f.ToString("00000") + ".jpg"), grab.EncodeToJPG(90));
        }
        Time.captureFramerate = 0; AHInput.TestMove = Vector2.zero;
        if (rode && p.mounted) AHComp.Dismount(g, true);
        p.transform.position = start;
    }
}
#endif
