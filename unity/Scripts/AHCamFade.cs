// Ashen Hollow: nothing stands between the camera and you. A tree crown, a roof or a big rock that the camera ends up
// inside, or that hides the hero from the camera, is not drawn until the view is clear again (the camera already
// steps in front of trees it can see coming; this is for when you stand right under one).
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(31000)]   // after the camera is placed (and shaken)
public class AHCamFade : MonoBehaviour
{
    readonly List<Renderer> cands = new List<Renderer>();
    readonly HashSet<Renderer> hidden = new HashSet<Renderer>();
    // no flicker: a tree goes only after it has blocked the view for a moment, and comes back only once the view has
    // been clear for a while (walking past a trunk used to make trees blink in and out)
    readonly Dictionary<Renderer, float> since = new Dictionary<Renderer, float>(), clearSince = new Dictionary<Renderer, float>();
    const float HideAfter = 0.18f, ShowAfter = 0.7f;
    // a hidden tree still casts its shade (only its picture goes), so the light on the hero doesn't jump as it hides
    readonly Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> modes = new Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();
    void Hide(Renderer r) { if (!modes.ContainsKey(r)) modes[r] = r.shadowCastingMode; if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; else r.forceRenderingOff = true; }
    void Show(Renderer r) { UnityEngine.Rendering.ShadowCastingMode m; if (modes.TryGetValue(r, out m)) { r.shadowCastingMode = m; modes.Remove(r); } r.forceRenderingOff = false; }
    string area; float scanAt;

    public static void Ensure(AHGame g) { if (g.GetComponent<AHCamFade>() == null) g.gameObject.AddComponent<AHCamFade>(); }

    void Scan(AHGame g)
    {
        cands.Clear();
        Transform hero = g.player != null ? g.player.transform : null;
        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r == null || !r.enabled || (hero != null && r.transform.IsChildOf(hero))) continue;
            var b = r.bounds; Vector3 s = b.size;
            if (s.y < 1.6f || s.x > 24f || s.z > 24f) continue;   // small things never block; the ground and huge meshes are left alone
            if (r.gameObject.layer == AHRange.Layer) continue;
            cands.Add(r);
        }
    }

    void LateUpdate()
    {
        var g = AHGame.I; if (g == null || g.cam == null || g.player == null) return;
        if (area != AHGame.AreaId) { area = AHGame.AreaId; Restore(); cands.Clear(); scanAt = Time.time + 2f; }
        if (scanAt > 0f && Time.time >= scanAt) { scanAt = 0f; Scan(g); }
        if (cands.Count == 0 || AHInterior.Inside) { if (hidden.Count > 0) Restore(); return; }
        Vector3 cp = g.cam.transform.position, head = g.player.transform.position + Vector3.up * 1.4f;
        Vector3 d = head - cp; float len = d.magnitude; if (len < 0.01f) return; var ray = new Ray(cp, d / len);
        var now = new HashSet<Renderer>();
        foreach (var r in cands)
        {
            if (r == null) continue;
            var b = r.bounds; Vector3 c = b.center;
            if ((c.x - cp.x) * (c.x - cp.x) + (c.z - cp.z) * (c.z - cp.z) > 400f) continue;   // only what is near the camera
            b.Expand(0.3f);
            float hit;
            bool block = b.Contains(cp) || (b.IntersectRay(ray, out hit) && hit < len - 0.6f);
            if (block) now.Add(r);
        }
        float t = Time.time;
        foreach (var r in now) { if (!since.ContainsKey(r)) since[r] = t; clearSince.Remove(r); }
        var drop = new List<Renderer>(); foreach (var kv in since) if (!now.Contains(kv.Key)) drop.Add(kv.Key);
        foreach (var r in drop) { since.Remove(r); if (hidden.Contains(r) && !clearSince.ContainsKey(r)) clearSince[r] = t; }
        foreach (var r in now) if (!hidden.Contains(r) && t - since[r] >= HideAfter && r != null) { Hide(r); hidden.Add(r); }
        var back = new List<Renderer>();
        foreach (var r in hidden) { float c; if (r == null || (clearSince.TryGetValue(r, out c) && t - c >= ShowAfter)) back.Add(r); }
        foreach (var r in back) { if (r != null) Show(r); hidden.Remove(r); clearSince.Remove(r); }
    }

    void Restore() { foreach (var r in hidden) if (r != null) Show(r); hidden.Clear(); modes.Clear(); since.Clear(); clearSince.Clear(); }
}
