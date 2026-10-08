// Ashen Hollow: real outfits (Quaternius "Modular Character Outfits - Fantasy", CC0). The outfits are skinned to the
// same skeleton as the hero's hidden rig (root, pelvis, spine_01... Head), so an outfit can be worn by giving each of
// its bones a stand-in that follows the hero:
//  - with the classic body, the stand-in is simply the rig's own bone;
//  - with a VRoid body (slimmer, with a bigger head), each stand-in sits on the matching VRoid joint, turns exactly
//    as the hidden rig's bone turns, and is scaled to the VRoid body's size (the head pieces to the VRoid head).
// The outfit models live in Resources/AH/Models/Outfits (kept on the PC: model files are not pushed).
using System.Collections.Generic;
using UnityEngine;

public static class AHQOutfit
{
    // put an outfit on a hero; returns the worn object (destroy it to take it off).
    // The spec is one outfit model, or several joined with '+', each optionally narrowed to its pieces whose names
    // contain a word ("qoMale_Ranger:Arms") or leave it out ("qoMale_Peasant:-Arms").
    public static GameObject Wear(GameObject rig, string spec)
    {
        if (rig == null || string.IsNullOrEmpty(spec)) return null;
        var ubc = new Dictionary<string, Transform>();
        foreach (var t in rig.GetComponentsInChildren<Transform>(true)) if (!ubc.ContainsKey(t.name)) ubc[t.name] = t;
        if (!ubc.ContainsKey("pelvis")) { Debug.Log("Ashen Hollow: this hero has no outfit skeleton"); return null; }
        var link = rig.GetComponent<AHVRoidLink>();

        var go = new GameObject("QOutfit_" + spec); go.transform.SetParent(rig.transform.parent, false); go.layer = rig.layer;
        var follow = go.AddComponent<AHQOutfitFollow>(); follow.Setup(rig.transform, ubc, link);
        var smrs = new List<SkinnedMeshRenderer>();
        foreach (var part in spec.Split('+'))
        {
            // "model:Word1,Word2,-Word3": only pieces naming one of the words (if any are given), never those naming a -word
            string model = part; var inc = new List<string>(); var exc = new List<string>(); int c = part.IndexOf(':');
            if (c > 0) { model = part.Substring(0, c); foreach (var w in part.Substring(c + 1).Split(',')) if (w.StartsWith("-")) exc.Add(w.Substring(1)); else if (w.Length > 0) inc.Add(w); }
            var pf = Resources.Load<GameObject>("AH/Models/Outfits/" + model);
            if (pf == null) { Debug.Log("Ashen Hollow: no outfit model " + model); continue; }
            var inst = Object.Instantiate(pf, go.transform, false); inst.name = part;
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // the noble's crown is left off (crowns are for kings, and it sits too high on the VRoid heads)
                string n = smr.name; bool want = !n.Contains("Crown") && (inc.Count == 0 || inc.Exists(w => n.Contains(w))) && !exc.Exists(w => n.Contains(w));
                if (!want) smr.gameObject.SetActive(false); else smrs.Add(smr);
            }
            // the outfit's own armature is no longer used
            foreach (Transform ch in inst.transform) if (ch.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) ch.gameObject.SetActive(false);
        }
        if (smrs.Count == 0) { Object.Destroy(go); return null; }
        foreach (var smr in smrs)
        {
            var bs = smr.bones; var nb = new Transform[bs.Length];
            for (int i = 0; i < bs.Length; i++)
            {
                Transform u; if (bs[i] == null || !ubc.TryGetValue(bs[i].name, out u)) { nb[i] = bs[i]; continue; }
                nb[i] = follow.Stand(u);
            }
            smr.bones = nb;
            Transform ru; if (smr.rootBone != null && ubc.TryGetValue(smr.rootBone.name, out ru)) smr.rootBone = follow.Stand(ru);
            smr.updateWhenOffscreen = true;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            int layer = rig.layer; smr.gameObject.layer = layer;
        }
        // the VRoid body's own skin and clothes that would poke through the outfit are cut away while it is worn
        if (link != null && link.Body != null) { Transform qh; ubc.TryGetValue("Head", out qh); go.AddComponent<AHQOutfitHide>().Apply(link.Body, go, qh != null ? link.Map(qh) : null); }
        return go;
    }
}

// cuts the VRoid body's triangles that sit under (or just through) an outfit; puts them back when the outfit comes off
public class AHQOutfitHide : MonoBehaviour
{
    readonly List<SkinnedMeshRenderer> smrs = new List<SkinnedMeshRenderer>();
    readonly List<Mesh> originals = new List<Mesh>(), cuts = new List<Mesh>();
    readonly List<SkinnedMeshRenderer> hiddenHair = new List<SkinnedMeshRenderer>();
    readonly List<Renderer> hiddenBits = new List<Renderer>();
    const float Cell = 0.05f, Reach = 0.10f, Through = 0.07f, Close = 0.03f, Snug = 0.07f;

    public void Apply(Transform body, GameObject outfit, Transform head)
    {
        // the outfit's surface, in world space, in a grid
        var grid = new Dictionary<Vector3Int, List<int>>(); var pts = new List<Vector3>(); var nrm = new List<Vector3>();
        var baked = new Mesh();
        foreach (var o in outfit.GetComponentsInChildren<SkinnedMeshRenderer>(false))
        {
            o.BakeMesh(baked, true); var m = Matrix4x4.TRS(o.transform.position, o.transform.rotation, Vector3.one);
            var v = baked.vertices; var n = baked.normals;
            for (int i = 0; i < v.Length; i++)
            {
                var p = m.MultiplyPoint3x4(v[i]); pts.Add(p); nrm.Add(n.Length > i ? m.MultiplyVector(n[i]).normalized : Vector3.up);
                var k = Key(p); List<int> l; if (!grid.TryGetValue(k, out l)) grid[k] = l = new List<int>(); l.Add(pts.Count - 1);
            }
        }
        // a helmet (outfit reaching well above the head bone) hides the hair: a ponytail would poke through it
        bool helmet = false;
        if (head != null) { float top = head.position.y + 0.16f * head.lossyScale.y; foreach (var p in pts) if (p.y > top) { helmet = true; break; } }
        // the forearms (elbow to the hand's middle), for keeping a VRoid sleeve that is snug on them
        var segs = new List<Vector3>(); var an = body.GetComponent<Animator>();
        if (an != null && an.isHuman)
            foreach (var side in new[] { new[] { HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal }, new[] { HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal } })
            {
                Transform a = an.GetBoneTransform(side[0]), h = an.GetBoneTransform(side[1]), f = an.GetBoneTransform(side[2]);
                if (a == null || h == null) continue; segs.Add(a.position); segs.Add(f != null ? f.position : h.position);
            }
        int cutTris = 0;
        foreach (var s in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = s.sharedMesh; if (mesh == null) continue;
            string nm = s.name.ToLower();
            if (helmet && nm.Contains("hair")) { if (s.enabled) { s.enabled = false; hiddenHair.Add(s); } continue; }
            if (!mesh.isReadable || nm.Contains("face")) continue;   // the face stays (a helmet hides it or not)
            s.BakeMesh(baked, true); var m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
            var v = baked.vertices; var hide = new bool[v.Length]; int nh = 0;
            for (int i = 0; i < v.Length; i++)
            {
                var p = m.MultiplyPoint3x4(v[i]); var k = Key(p); float best = Reach * Reach; int bi = -1;
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                {
                    List<int> l; if (!grid.TryGetValue(new Vector3Int(k.x + x, k.y + y, k.z + z), out l)) continue;
                    foreach (int j in l) { float d = (pts[j] - p).sqrMagnitude; if (d < best) { best = d; bi = j; } }
                }
                if (bi < 0) continue;
                // right at the surface, poking out along it, or just under it: hidden; past a sleeve's or a boot's edge (sideways from it) stays
                float dist = Mathf.Sqrt(best), along = Vector3.Dot(p - pts[bi], nrm[bi]);
                if (dist < Close || (along > 0.5f * dist && dist < Through) || along < -0.5f * dist) { hide[i] = true; nh++; }
            }
            // which vertices hang mostly from a forearm, a hand or a finger
            var arm = new bool[v.Length]; var bw = mesh.boneWeights; var sb = s.bones;
            if (bw.Length == v.Length)
                for (int i = 0; i < v.Length; i++)
                {
                    int bi = bw[i].boneIndex0; var bt = bi >= 0 && bi < sb.Length ? sb[bi] : null; if (bt == null) continue;
                    string n = bt.name; arm[i] = n.Contains("LowerArm") || n.Contains("Hand") || n.Contains("Thumb") || n.Contains("Index") || n.Contains("Middle") || n.Contains("Ring") || n.Contains("Little");
                    // only the snug part of a sleeve: a wide bell cuff would hang off the hand
                    if (arm[i] && segs.Count > 0)
                    {
                        var p = m.MultiplyPoint3x4(v[i]); float near = float.MaxValue;
                        for (int g = 0; g + 1 < segs.Count; g += 2) near = Mathf.Min(near, SegDist(p, segs[g], segs[g + 1]));
                        if (near > Snug) arm[i] = false;
                    }
                }
            var cut = Instantiate(mesh); cut.name = mesh.name + " (under outfit)";
            var smats = s.sharedMaterials;
            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                var t = mesh.GetTriangles(sm); var keep = new List<int>(t.Length);
                // the VRoid clothes (a long coat, skirts, shoes) go altogether: the outfit replaces them; so does hair under a helmet
                string mn = sm < smats.Length && smats[sm] != null ? smats[sm].name.ToUpper() : "";
                if (helmet && mn.Contains("HAIR")) { cutTris += t.Length / 3; cut.SetTriangles(keep, sm); continue; }
                bool cloth = mn.Contains("CLOTH");
                for (int i = 0; i < t.Length; i += 3)
                {
                    // the VRoid clothes go, except on the forearms and hands (often there is no skin under a VRoid sleeve):
                    // there they show under a short outfit sleeve, like an undershirt, and are trimmed like the skin
                    if (cloth && !(arm[t[i]] && arm[t[i + 1]] && arm[t[i + 2]])) { cutTris++; continue; }
                    int h = (hide[t[i]] ? 1 : 0) + (hide[t[i + 1]] ? 1 : 0) + (hide[t[i + 2]] ? 1 : 0);
                    if (h >= 1) { cutTris++; continue; }
                    keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
                }
                cut.SetTriangles(keep, sm);
            }
            smrs.Add(s); originals.Add(mesh); cuts.Add(cut); s.sharedMesh = cut;
        }
        Destroy(baked);
        // the class sash and its ribbons sit over the old clothes
        foreach (var t in body.GetComponentsInChildren<Transform>(true))
            if (t.name == "ClassSash" || t.name == "ClassSashRibbon") { var r = t.GetComponent<Renderer>(); if (r != null && r.enabled) { r.enabled = false; hiddenBits.Add(r); } }
        Debug.Log("Ashen Hollow: outfit hides " + cutTris + " body triangles on " + smrs.Count + " meshes" + (helmet ? ", hair under the helmet" : ""));
    }
    static float SegDist(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a; float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); return (p - (a + ab * t)).magnitude;
    }
    static Vector3Int Key(Vector3 p) { return new Vector3Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell), Mathf.FloorToInt(p.z / Cell)); }

    void OnDestroy()
    {
        foreach (var h in hiddenHair) if (h != null) h.enabled = true;
        foreach (var b in hiddenBits) if (b != null) b.enabled = true;
        for (int i = 0; i < smrs.Count; i++) { if (smrs[i] != null && smrs[i].sharedMesh == cuts[i]) smrs[i].sharedMesh = originals[i]; if (cuts[i] != null) Destroy(cuts[i]); }
    }
}

// the stand-in bones: placed every frame after the VRoid body has taken the rig's pose
[DefaultExecutionOrder(20010)]
public class AHQOutfitFollow : MonoBehaviour
{
    class S { public Transform stand, src, dst, anchorSrc, anchorDst; public bool head; }
    readonly Dictionary<Transform, S> map = new Dictionary<Transform, S>();
    readonly List<S> order = new List<S>();
    Transform rig, standRoot; AHVRoidLink link; Dictionary<string, Transform> ubc;
    float bodyK = 1f; Renderer[] rends; bool shown = true;

    public void Setup(Transform rigRoot, Dictionary<string, Transform> bones, AHVRoidLink l)
    {
        rig = rigRoot; ubc = bones; link = l;
        standRoot = new GameObject("Stand-ins").transform; standRoot.SetParent(transform, false);
        // the VRoid body's size against the rig's: pelvis-to-head height
        if (link != null && link.Body != null)
        {
            Transform qp = bones["pelvis"], qh; bones.TryGetValue("Head", out qh);
            Transform vp = link.Map(qp), vh = qh != null ? link.Map(qh) : null;
            if (vp != null && vh != null && qh != null) { float a = (qh.position - qp.position).magnitude, b = (vh.position - vp.position).magnitude; if (a > 0.01f) bodyK = Mathf.Clamp(b / a, 0.6f, 1.4f); }
        }
    }

    public Transform Stand(Transform src)
    {
        S s; if (map.TryGetValue(src, out s)) return s.stand;
        if (link == null) return src;   // the classic body: the rig's own bones
        s = new S { src = src, stand = new GameObject("stand_" + src.name).transform, head = src.name == "Head" };
        s.stand.SetParent(standRoot, false);
        s.dst = link.Map(src);
        if (s.dst == null)
        {
            // not a humanoid bone (root, finger tips, toe tips): keep its offset from the nearest one that is
            for (var p = src.parent; p != null && p != rig; p = p.parent) { var d = link.Map(p); if (d != null) { s.anchorSrc = p; s.anchorDst = d; break; } }
        }
        map[src] = s; order.Add(s);
        Place(s);
        return s.stand;
    }

    void Place(S s)
    {
        float k = bodyK * (s.head ? HeadK() : 1f);
        if (s.dst != null) s.stand.position = s.dst.position;
        else if (s.anchorDst != null) s.stand.position = s.anchorDst.position + (s.src.position - s.anchorSrc.position) * bodyK;
        else s.stand.position = link.Body != null ? link.Body.position + (s.src.position - rig.position) * bodyK : s.src.position;
        s.stand.rotation = s.src.rotation;
        s.stand.localScale = s.src.lossyScale * k;
    }
    float HeadK() { return link != null ? Mathf.Max(1f, link.HeadScale / Mathf.Max(0.3f, bodyK)) : 1f; }

    void LateUpdate()
    {
        if (rig == null) { Destroy(gameObject); return; }
        if (link == null) return;
        // shown and hidden with the body
        bool on = link.Body != null && link.Body.gameObject.activeInHierarchy;
        if (rends == null) rends = GetComponentsInChildren<Renderer>(true);
        if (on != shown) { shown = on; foreach (var r in rends) if (r != null) r.enabled = on; }
        foreach (var s in order) Place(s);
    }
}

// which real outfit a hero's shown gear becomes. The chest piece picks the whole outfit (its body, sleeves, legs and
// boots always come together, so nothing is ever left bare): plate is the Knight (the Royal Guard tabard the Knight in
// cloth), leather the Ranger, and cloth the Noble for priests' vestments, a peasant's tunic for druids' robes and the
// Wizard for the rest. Helms, horns and hoods, and plate or spiked pauldrons, add the matching pieces; any other head,
// shoulder or cape piece stays as the wardrobe draws it.
public static class AHQOutfitPlan
{
    static readonly HashSet<string> Priest = new HashSet<string> { "holy", "dawn", "sunflame", "pearl", "fenlight", "snowlight", "sunpriest", "emberlight", "dawnlight" };
    static readonly HashSet<string> Druid = new HashSet<string> { "grove", "cinderbloom", "emberoot", "kelpheart", "mossweave", "snowbark", "oasis", "ashgrove", "rootsong" };
    static string Armor(ItemDef d) { return AHJson.S(AHJson.O(AHDB.Rules, "ARMOR_OF"), d.style ?? "", "leather"); }

    // the outfit spec for AHQOutfit.Wear (null: no real outfit); 'covered' gets the slots it draws
    public static string Spec(bool fem, System.Func<string, string> shown, HashSet<string> covered, out Color tint, out float tintK)
    {
        tint = Color.white; tintK = 0f;
        var ch = AHItems.Get(shown("chest") ?? ""); if (ch == null) return null;
        string S = fem ? "qoFemale_" : "qoMale_", a = Armor(ch), st = ch.style ?? "", spec;
        if (a == "plate") spec = S + (st == "royal" ? "Knight_Cloth" : "Knight") + ":-Head,-Pauldron";
        else if (a == "cloth") spec = Priest.Contains(st) ? S + "Noble:-Head,-Pauldron" : Druid.Contains(st) ? (fem ? S + "Peasant" : S + "Peasant:-Arms+" + S + "Ranger:Arms") : S + "Wizard";
        else spec = S + "Ranger:-Head,-Pauldron";
        tint = ch.color; tintK = a == "plate" ? 0.15f : 0.35f;
        covered.Add("chest"); covered.Add("hands"); covered.Add("legs"); covered.Add("feet");

        var hd = AHItems.Get(shown("head") ?? "");
        if (hd != null)
        {
            string f = hd.form ?? "", part = null;
            if (f == "helm" || f == "forgehelm") part = S + "Knight:Head";
            else if (f == "horns") part = S + "Knight_Cloth:Head";
            else if (f == "hood" || f == "cowl" || f == "mhood") part = S + "Ranger:Head";
            if (part != null) { spec += "+" + part; covered.Add("head"); }
        }
        var sh = AHItems.Get(shown("shoulders") ?? "");
        if (sh != null)
        {
            string f = sh.form ?? "", sa = Armor(sh), part = null;
            if (f == "plates" || f == "spikes")
            {
                if (sa == "plate") part = S + (f == "spikes" ? "Knight_Cloth" : "Knight") + ":Pauldron";
                else if (sa == "leather") part = S + "Ranger:Pauldron";
                else part = S + (Priest.Contains(sh.style ?? "") ? "Noble:Lion" : "Noble:Pauldron,-Lion");
            }
            if (part != null) { spec += "+" + part; covered.Add("shoulders"); }
        }
        return spec;
    }
}

// puts a real outfit on a freshly built hero once its body is ready (the VRoid body is linked a few frames after the
// hero is built; a classic body wears it on its own rig)
public class AHQOutfitAuto : MonoBehaviour
{
    public string spec; public Color tint = Color.white; public float tintK;
    GameObject worn; int frames;

    void Update()
    {
        if (worn != null) return;
        frames++;
        var link = GetComponent<AHVRoidLink>();
        bool ready = link != null && link.Body != null && frames >= 3;
        if (!ready && frames < 30) return;
        worn = AHQOutfit.Wear(gameObject, spec);
        if (worn != null && tintK > 0f) Tint(worn, tint, tintK);
        enabled = false;
    }
    void OnDestroy() { if (worn != null) Destroy(worn); }

    // the gear's own colour, lightly over the outfit's
    static void Tint(GameObject go, Color c, float k)
    {
        var mul = Color.Lerp(Color.white, new Color(c.r, c.g, c.b, 1f), k);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.materials;
            foreach (var m in ms)
            {
                if (m == null) continue;
                foreach (var prop in new[] { "baseColorFactor", "_BaseColor", "_Color" })
                    if (m.HasProperty(prop)) { m.SetColor(prop, m.GetColor(prop) * mul); break; }
            }
            r.materials = ms;
        }
    }
}
