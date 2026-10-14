// Ashen Hollow: the camera inside a tree. When the camera ends up inside a tree's crown (standing right under a tree,
// or turning the view round by one) that crown stops being drawn (it still casts its shade) until the camera is out of
// it again, so the screen never fills with leaves. Only crowns: houses, walls and rocks keep the camera out instead.
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(31000)]   // after the camera is placed
public class AHCrownFade : MonoBehaviour
{
    readonly Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode> hidden = new Dictionary<Renderer, UnityEngine.Rendering.ShadowCastingMode>();
    readonly List<Renderer> back = new List<Renderer>();
    public static void Ensure(AHGame g) { if (g.GetComponent<AHCrownFade>() == null) g.gameObject.AddComponent<AHCrownFade>(); }

    void LateUpdate()
    {
        var g = AHGame.I; if (g == null || g.cam == null) return;
        Vector3 c = g.cam.transform.position;
        var inNow = new HashSet<Renderer>();
        foreach (var t in AHForest.Trees)
        {
            if (t == null || t.crown == null) continue;
            Vector3 d = t.pos - c; d.y = 0f; if (d.sqrMagnitude > 100f) continue;
            foreach (var r in t.crown.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds; b.Expand(0.6f);
                if (b.Contains(c)) inNow.Add(r);
            }
        }
        foreach (var r in inNow)
            if (!hidden.ContainsKey(r)) { hidden[r] = r.shadowCastingMode; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; }
        back.Clear();
        foreach (var kv in hidden) if (kv.Key == null || !inNow.Contains(kv.Key)) back.Add(kv.Key);
        foreach (var r in back) { if (r != null) r.shadowCastingMode = hidden[r]; hidden.Remove(r); }
    }
}
