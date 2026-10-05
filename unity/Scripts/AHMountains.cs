// Ashen Hollow: better mountains. The hills and peaks around every area were plain seven-sided cones. Each one is now
// a craggy mountain with ridges, gullies and a crooked summit, faced with rock (Idyllic Fantasy Nature rock texture),
// lighter towards the top and darker in the folds. Each keeps the place and size of its cone and the colour of its
// area (green hills, grey-white Frostfang peaks, ash-black Ember crags, sand-brown desert ridges). Three shapes,
// turned at random so no two neighbours look alike.
// Areas that drew their peaks as one batch of cones get them split and reshaped too (lighter mtnlite_* models).
// Models: Resources/AH/Models/Nature/mtn_0..2.glb, mtnlite_0..2.glb (made for Ashen Hollow).
using System.Collections.Generic;
using UnityEngine;

public static class AHMountains
{
    static readonly GameObject[] prefabs = new GameObject[3], lite = new GameObject[3];
    static readonly float[] width = new float[3], liteW = new float[3];
    static Transform root; static Dictionary<Color, Material> mats;

    public static void Setup(AHGame g, Transform world)
    {
        if (world == null) return;
        for (int i = 0; i < 3; i++) if (prefabs[i] == null)
        {
            prefabs[i] = Resources.Load<GameObject>("AH/Models/Nature/mtn_" + i);
            if (prefabs[i] != null) { var mf = prefabs[i].GetComponentInChildren<MeshFilter>(); width[i] = mf != null ? Mathf.Max(mf.sharedMesh.bounds.size.x, mf.sharedMesh.bounds.size.z) : 1.5f; }
        }
        if (prefabs[0] == null) return;
        for (int i = 0; i < 3; i++) if (lite[i] == null)
        {
            lite[i] = Resources.Load<GameObject>("AH/Models/Nature/mtnlite_" + i);
            if (lite[i] != null) { var mf = lite[i].GetComponentInChildren<MeshFilter>(); liteW[i] = mf != null ? Mathf.Max(mf.sharedMesh.bounds.size.x, mf.sharedMesh.bounds.size.z) : 1.5f; }
        }

        mats = new Dictionary<Color, Material>();
        root = null; int n = 0;
        // grey peaks a little steeper than the rest, wearing a separate white cap of snow: one crag, and a snow cap
        // made of the same crag shrunk over its top
        {
            var caps = new List<MeshRenderer>(); var bodies = new List<MeshRenderer>();
            foreach (var r in world.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled || !r.name.StartsWith("obj")) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || mf.sharedMesh.subMeshCount != 1 || mf.sharedMesh.GetIndexCount(0) > 60) continue;
                var b = r.bounds; float h = b.size.y, foot = Mathf.Min(b.size.x, b.size.z); Color cc = Colour(r.sharedMaterial); float lum = (cc.r + cc.g + cc.b) / 3f, sat = Mathf.Abs(cc.r - cc.b);
                if (lum > 0.85f && sat < 0.06f && h > 1.5f) caps.Add(r);
                else if (lum > 0.25f && lum < 0.7f && sat < 0.1f && h >= 6f && foot >= 0.44f * h) bodies.Add(r);
            }
            foreach (var body in bodies)
            {
                var bb = body.bounds; MeshRenderer cap = null;
                foreach (var c in caps) { if (!c.enabled) continue; var cb = c.bounds; Vector2 d = new Vector2(cb.center.x - bb.center.x, cb.center.z - bb.center.z); if (d.magnitude < 2f && cb.min.y > bb.center.y) { cap = c; break; } }
                if (cap == null && Mathf.Min(bb.size.x, bb.size.z) < 0.55f * bb.size.y) continue;   // a lone steep grey thing: leave it
                var all = bb; if (cap != null) all.Encapsulate(cap.bounds);
                var go = Place(prefabs, width, all, Colour(body.sharedMaterial));
                if (cap != null)
                {
                    var snow = Object.Instantiate(go.gameObject, root, false).transform; snow.name = "Snowcap";
                    float f = Mathf.Clamp(cap.bounds.size.y / all.size.y + 0.08f, 0.25f, 0.5f);
                    snow.localScale = new Vector3(go.localScale.x * f * 1.06f, go.localScale.y * f, go.localScale.z * f * 1.06f);
                    snow.position = go.position + Vector3.up * (go.localScale.y * (1f - f) + 0.05f);
                    var sm = SnowMat(go.GetComponentInChildren<Renderer>().sharedMaterial);
                    foreach (var r2 in snow.GetComponentsInChildren<Renderer>(true)) r2.sharedMaterial = sm;
                    cap.enabled = false;
                }
                body.enabled = false; n++;
            }
        }
        foreach (var r in world.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r.enabled || !r.name.StartsWith("obj")) continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var m = mf.sharedMesh; if (m.subMeshCount != 1 || m.GetIndexCount(0) > 120) continue;    // the old cones: a handful of triangles
            var b = r.bounds; float h = b.size.y, foot = Mathf.Min(b.size.x, b.size.z);
            if (foot < 0.55f * h) continue;                                                       // a spire or a post
            if (h < 5f)
            {
                // the small snowy or grey rock spikes along some borders (a six-sided pyramid) are crags too
                Color cc = Colour(r.sharedMaterial); float lum = (cc.r + cc.g + cc.b) / 3f;
                if (h < 3.3f || m.GetIndexCount(0) > 60 || (lum < 0.4f && Mathf.Abs(cc.r - cc.b) > 0.08f)) continue;
            }

            Place(prefabs, width, b, Colour(r.sharedMaterial));
            r.enabled = false; n++;
        }
        // some areas (the cities, the Fossil Lands, Emberreach, your homestead) drew their border peaks and rock columns as
        // one batch of seven-sided cones or columns (63 vertices each, coloured per piece): split it and reshape every one
        int nb = 0;
        if (!AHDungeon.IsDungeon(AHGame.AreaId))
            foreach (var r in world.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled || !r.name.StartsWith("AH_INST")) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var m = mf.sharedMesh;
                if (!m.isReadable || m.vertexCount < 63 || m.vertexCount % 63 != 0) continue;
                var v = m.vertices; var cols = m.colors; if (cols == null || cols.Length != v.Length) continue;
                var tf = r.transform; int count = v.Length / 63; var list = new List<KeyValuePair<Bounds, Color>>();
                bool ok = true;
                for (int k = 0; k < count && ok; k++)
                {
                    Vector3 lo = Vector3.positiveInfinity, hi = Vector3.negativeInfinity; Color c = Color.clear; int top = 0;
                    for (int q = 0; q < 63; q++) { var w = tf.TransformPoint(v[k * 63 + q]); lo = Vector3.Min(lo, w); hi = Vector3.Max(hi, w); c += cols[k * 63 + q]; }
                    for (int q = 0; q < 63; q++) { float y = tf.TransformPoint(v[k * 63 + q]).y; if (y > hi.y - 0.01f) top++; }
                    if (!(top >= 5 && top <= 9) && top != 21) ok = false;   // a seven-sided cone (one summit) or column (flat top)
                    if (hi.y - lo.y < 1.5f || Mathf.Max(hi.x - lo.x, hi.z - lo.z) > 40f) ok = false;
                    var bb = new Bounds((lo + hi) * 0.5f, hi - lo); c /= 63f; c.a = 1f;
                    c = new Color(Mathf.Round(c.r * 20f) / 20f, Mathf.Round(c.g * 20f) / 20f, Mathf.Round(c.b * 20f) / 20f, 1f);
                    list.Add(new KeyValuePair<Bounds, Color>(bb, c));
                }
                if (!ok || list.Count == 0) { Debug.Log("Ashen Hollow: batch " + r.name + " is not all cones"); continue; }
                foreach (var kv in list) Place(lite[0] != null ? lite : prefabs, lite[0] != null ? liteW : width, kv.Key, kv.Value);
                r.enabled = false; nb += list.Count;
            }
        // white snow cones left standing on hills that were not grey (Whitepine's green and grey ridges): each becomes a
        // snow cap on the crag that replaced its hill, or a snowy crag of its own
        int ns = 0;
        if (root != null)
            foreach (var r in world.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled || r.name.StartsWith("AH_")) continue;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || mf.sharedMesh.subMeshCount != 1 || mf.sharedMesh.GetIndexCount(0) > 130) continue;
                var cb = r.bounds; Color cc = Colour(r.sharedMaterial); float lum = (cc.r + cc.g + cc.b) / 3f;
                if (lum < 0.85f || Mathf.Abs(cc.r - cc.b) > 0.06f || cb.size.y < 1.5f || Mathf.Min(cb.size.x, cb.size.z) < 0.5f * cb.size.y && cb.size.y > 6f) continue;
                Transform best = null; Bounds bb = default; float bd = float.MaxValue;
                foreach (Transform m in root)
                {
                    if (m.name != "Mountain") continue; var mr = m.GetComponentInChildren<Renderer>(); if (mr == null) continue;
                    var mb = mr.bounds; float d = new Vector2(mb.center.x - cb.center.x, mb.center.z - cb.center.z).magnitude;
                    if (d < Mathf.Max(mb.extents.x, mb.extents.z) * 0.7f && cb.center.y > mb.center.y && d < bd) { bd = d; best = m; bb = mb; }
                }
                if (best != null)
                {
                    float top = Mathf.Max(bb.max.y, cb.max.y), f = Mathf.Clamp((top - cb.min.y) / Mathf.Max(1f, top - bb.min.y) + 0.08f, 0.22f, 0.5f);
                    var snow = Object.Instantiate(best.gameObject, root, false).transform; snow.name = "Snowcap";
                    float lift = top - bb.max.y;   // the old cone stood taller than the new crag: raise the whole crag to meet it
                    if (lift > 0f) { best.localScale = new Vector3(best.localScale.x, best.localScale.y * (top - bb.min.y) / Mathf.Max(1f, bb.size.y), best.localScale.z); }
                    snow.localScale = new Vector3(best.localScale.x * f * 1.06f, best.localScale.y * f, best.localScale.z * f * 1.06f);
                    snow.position = best.position + Vector3.up * (best.localScale.y * (1f - f) + 0.05f);
                    var sm = SnowMat(best.GetComponentInChildren<Renderer>().sharedMaterial);
                    foreach (var r2 in snow.GetComponentsInChildren<Renderer>(true)) r2.sharedMaterial = sm;
                }
                else Place(prefabs, width, cb, new Color(0.72f, 0.74f, 0.76f));
                r.enabled = false; ns++;
            }
        if (ns > 0) Debug.Log("Ashen Hollow: " + ns + " snow cones made snow caps");
        if (n + nb > 0) Debug.Log("Ashen Hollow: " + (n + nb) + " mountains reshaped" + (nb > 0 ? " (" + nb + " from the batch)" : ""));
    }

    static Transform Place(GameObject[] set, float[] widths, Bounds b, Color col)
    {
        if (root == null) root = new GameObject("Mountains").transform;
        var rnd = new System.Random((int)(b.center.x * 7.3f + b.center.z * 13.1f));
        int k = rnd.Next(3);
        var go = Object.Instantiate(set[k], root, false).transform; go.name = "Mountain";
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        float h = b.size.y, sxz = Mathf.Max(b.size.x, b.size.z) * 1.08f / widths[k];
        go.position = new Vector3(b.center.x, b.min.y - 0.35f, b.center.z);
        go.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        go.localScale = new Vector3(sxz, h * 1.06f + 0.35f, sxz);
        Material mat;
        if (!mats.TryGetValue(col, out mat))
        {
            var src = go.GetComponentInChildren<Renderer>().sharedMaterial;
            mat = new Material(src); mat.enableInstancing = true;
            Color c2 = col * 1.35f; c2.a = 1f;
            if (col.r + col.g + col.b < 0.45f && AHGame.AreaId == "fossil") c2 = Color.Lerp(c2, new Color(0.46f, 0.38f, 0.3f), 0.6f);   // the Fossil Lands' near-black rock spires: weathered brown stone
            foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (mat.HasProperty(pr)) mat.SetColor(pr, c2);
            mats[col] = mat;
        }
        foreach (var r2 in go.GetComponentsInChildren<Renderer>(true)) r2.sharedMaterial = mat;
        AHModel.SetShadows(go.gameObject);
        return go;
    }
    static Material snowMat;
    static Material SnowMat(Material src)
    {
        if (snowMat != null) return snowMat;
        snowMat = new Material(src) { name = "snowcap" }; snowMat.enableInstancing = true;
        foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (snowMat.HasProperty(pr)) snowMat.SetColor(pr, new Color(0.95f, 0.97f, 1f));
        return snowMat;
    }

    static Color Colour(Material m)
    {
        if (m == null) return new Color(0.3f, 0.3f, 0.3f);
        foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) return m.GetColor(pr);
        return new Color(0.3f, 0.3f, 0.3f);
    }
}
