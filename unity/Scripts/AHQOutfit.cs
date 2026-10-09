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
    readonly List<SkinnedMeshRenderer> darkened = new List<SkinnedMeshRenderer>();
    static readonly Color Under = new Color(0.17f, 0.13f, 0.12f, 1f);   // a dark padded under-suit, warm like the basalt plates
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
        int cutTris = 0; bool closed = outfit.name.Contains("tqo_");
        foreach (var s in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = s.sharedMesh; if (mesh == null) continue;
            string nm = s.name.ToLower();
            if (helmet && nm.Contains("hair")) { if (s.enabled) { s.enabled = false; hiddenHair.Add(s); } continue; }
            // a closed great helm (the Warden's lava armour) hides the face too: it would show through the visor
            if (helmet && closed && nm.Contains("face")) { if (s.enabled) { s.enabled = false; hiddenHair.Add(s); } continue; }
            if (!mesh.isReadable || nm.Contains("face")) continue;   // the face stays (a helmet hides it or not)
            s.BakeMesh(baked, true); var m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
            var v = baked.vertices; var hide = new bool[v.Length]; int nh = 0;
            // under a closed suit of plate the body stays inside, darkened to a black under-suit, so no chink between
            // the plates shows the world behind (only the hair, and whatever pokes out through the plates, goes)
            if (closed) { var pb = new MaterialPropertyBlock(); s.GetPropertyBlock(pb); pb.SetColor("_Color", Under); pb.SetColor("_BaseColor", Under); pb.SetColor("_ShadeColor", Under); s.SetPropertyBlock(pb); darkened.Add(s); }
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
                // under a closed suit only what pokes out through the plates goes; what is under them stays, dark
                // (under closed plate only what is actually outside the plate goes: the rest stays as dark padding behind
                // every chink between the plates)
                if (closed) { if (along > 0.004f && dist < Through) { hide[i] = true; nh++; } }
                else if (dist < Close || (along > 0.5f * dist && dist < Through) || along < -0.5f * dist) { hide[i] = true; nh++; }
            }
            // which vertices hang mostly from a forearm, a hand or a finger
            var arm = new bool[v.Length]; var bw = mesh.boneWeights; var sb = s.bones;
            if (bw.Length == v.Length)
                for (int i = 0; i < v.Length; i++)
                {
                    int bi = bw[i].boneIndex0; var bt = bi >= 0 && bi < sb.Length ? sb[bi] : null; if (bt == null) continue;
                    string n = bt.name;
                    // under closed plate the shins, feet and hands are all plate: the body's own there would only peek out
                    // below the boots or past the gauntlets as a dark shape when walking, so it goes
                    if (closed && (n.Contains("LowerLeg") || n.Contains("Foot") || n.Contains("Toe") || n.Contains("Hand") || n.Contains("Thumb") || n.Contains("Index") || n.Contains("Middle") || n.Contains("Ring") || n.Contains("Little"))) { if (!hide[i]) { hide[i] = true; nh++; } continue; }
                    arm[i] = n.Contains("LowerArm") || n.Contains("Hand") || n.Contains("Thumb") || n.Contains("Index") || n.Contains("Middle") || n.Contains("Ring") || n.Contains("Little");
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
                bool cloth = mn.Contains("CLOTH");   // loose VRoid clothes swing out past the plates when walking: they always go
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
        foreach (var d in darkened) if (d != null) d.SetPropertyBlock(null);
        for (int i = 0; i < smrs.Count; i++) { if (smrs[i] != null && smrs[i].sharedMesh == cuts[i]) smrs[i].sharedMesh = originals[i]; if (cuts[i] != null) Destroy(cuts[i]); }
    }
}

// the stand-in bones: placed every frame after the VRoid body has taken the rig's pose
[DefaultExecutionOrder(20010)]
public class AHQOutfitFollow : MonoBehaviour
{
    class S { public Transform stand, src, dst, anchorSrc, anchorDst, kidSrc, kidDst; public bool head; }
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
        // a limb or spine bone: the next joint down the chain, so the outfit's limb can be laid along the VRoid limb
        if (s.dst != null && Limb(src.name))
            foreach (Transform c in src) { var cd = link.Map(c); if (cd != null && !c.name.Contains("twist")) { s.kidSrc = c; s.kidDst = cd; break; } }
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
        // the hidden rig and the VRoid body don't always bend a limb quite alike (a wide stance, a raised arm): swing the
        // stand so the outfit's limb lies along the VRoid limb it covers
        if (s.kidSrc != null && s.kidDst != null)
        {
            Vector3 a = s.kidSrc.position - s.src.position, b = s.kidDst.position - s.dst.position;
            if (a.sqrMagnitude > 1e-6f && b.sqrMagnitude > 1e-6f) s.stand.rotation = Quaternion.FromToRotation(a, b) * s.src.rotation;
        }
        s.stand.localScale = s.src.lossyScale * k;
    }
    static bool Limb(string n) { return n.StartsWith("thigh_") || n.StartsWith("calf_") || n.StartsWith("upperarm_") || n.StartsWith("lowerarm_"); }
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

// which real outfit a hero wears. Each class has its own outfit shape and colours (the chest piece shown must be
// there, and its own colour tints the outfit a little, so every chest piece still looks its own):
//   warrior  the Knight in plate (the Royal Guard tabard: the Knight in cloth), coloured by the metal
//   rogue    a violet leather jerkin over a wizard's wrapped trousers and boots: an assassin's look
//   ranger   the Ranger's leathers, with bracers and belts, in woodland green
//   shaman   a tunic over teal leather sleeves and the Ranger's boots
//   priest   the Noble's coat, in white and gold
//   mage     the Wizard's coat, in arcane blue
//   druid    a simple tunic (the men with leather sleeves), in leaf green
//   warden   the Knight under a cloth tabard, dyed sandstone and ember
// Helms, horns and hoods, and plate or spiked pauldrons, add the matching pieces; any other head, shoulder or cape
// piece stays as the wardrobe draws it.
public static class AHQOutfitPlan
{
    static readonly HashSet<string> Priest = new HashSet<string> { "holy", "dawn", "sunflame", "pearl", "fenlight", "snowlight", "sunpriest", "emberlight", "dawnlight" };
    // the Ranger's leathers in the class colour: violet for rogues, teal for shamans (recoloured copies of the pack)
    // the collections' own armour models (made with Tripo from our concept art), by the chest piece's style
    // ("lava_warrior"): Resources/AH/Models/Outfits/tqo_<collection>_<class>. A class without its own model yet borrows
    // one made for a class in the same armour (the Warden wears the Warrior's plate); with none, the class's usual
    // outfit is worn in the collection's colours.
    static readonly Dictionary<string, string> tqo = new Dictionary<string, string>();
    public static string CollectionModel(string style)
    {
        if (string.IsNullOrEmpty(style) || style.IndexOf('_') < 0) return null;
        string m; if (tqo.TryGetValue(style, out m)) return m;
        string coll = style.Substring(0, style.IndexOf('_')), cls = style.Substring(style.IndexOf('_') + 1);
        string arm = AHJson.S(AHJson.O(AHDB.Rules, "ARMOR_OF"), style, "");
        m = null;
        foreach (var c in new[] { cls, arm == "plate" ? (cls == "warden" ? "warrior" : "warden") : null })
            if (c != null && m == null && Resources.Load<GameObject>("AH/Models/Outfits/tqo_" + coll + "_" + c) != null) m = "tqo_" + coll + "_" + c;
        tqo[style] = m; return m;
    }
    static string RangerOf(string cls) { return cls == "rogue" ? "RangerShade" : cls == "shaman" ? "RangerTide" : "Ranger"; }
    static string Armor(ItemDef d) { return AHJson.S(AHJson.O(AHDB.Rules, "ARMOR_OF"), d.style ?? "", "leather"); }

    // the class's own outfit shape (S is "qoMale_" or "qoFemale_")
    static string Shape(string cls, string S, bool fem, ItemDef chest)
    {
        switch (cls)
        {
            case "warrior": return S + ((chest.style ?? "") == "royal" ? "Knight_Cloth" : "Knight") + ":-Head,-Pauldron";
            case "rogue": return S + "RangerShade:Body,Arms,-Bracer+" + S + "Wizard:Legs,Feet";
            case "ranger": return S + "Ranger:-Head,-Pauldron";
            case "shaman": return S + "Peasant:Body,Legs+" + S + "RangerTide:Arms,Feet";
            case "priest": return S + "Noble:-Head,-Pauldron";
            case "mage": return S + "Wizard";
            case "druid": return fem ? S + "Peasant" : S + "Peasant:-Arms+" + S + "Ranger:Arms,-Bracer";
            case "warden": return S + "KnightClothStone:-Head,-Pauldron";   // armour under a tabard, recoloured sandstone and ember
        }
        // no class given: by the armour itself
        string a = Armor(chest);
        return a == "plate" ? S + "Knight:-Head,-Pauldron" : a == "cloth" ? S + "Wizard" : S + "Ranger:-Head,-Pauldron";
    }
    // the class's colour and how strongly it dyes the outfit
    static Color ClassDye(string cls, out float k)
    {
        switch (cls)
        {
            case "warrior": k = 0f; return Color.white;
            case "rogue": k = 0.2f; return new Color(0.75f, 0.68f, 0.85f);
            case "ranger": k = 0.25f; return new Color(0.55f, 0.75f, 0.45f);
            case "shaman": k = 0.3f; return new Color(0.7f, 0.92f, 0.9f);
            case "priest": k = 0.35f; return new Color(1f, 0.93f, 0.72f);
            case "mage": k = 0.4f; return new Color(0.55f, 0.6f, 1f);
            case "druid": k = 0.4f; return new Color(0.6f, 0.88f, 0.45f);
            case "warden": k = 0.15f; return new Color(0.95f, 0.85f, 0.72f);
        }
        k = 0f; return Color.white;
    }

    // the class's Ashen Hollow crest (a hollow tree in a ring, an ember at its heart), worn as a badge on the chest
    public static string CrestOf(string cls, out float size)
    {
        switch (cls)
        {
            case "warrior": size = 0.13f; return "steel";
            case "priest": size = 0.11f; return "gold";
            case "mage": size = 0.09f; return "arcane";
            case "druid": size = 0.09f; return "leaf";
            case "ranger": size = 0.08f; return "leaf";
            case "shaman": size = 0.09f; return "teal";
            case "rogue": size = 0.07f; return "violet";
            case "warden": size = 0.13f; return "ember";
        }
        size = 0f; return null;
    }

    // the outfit spec for AHQOutfit.Wear (null: no real outfit); 'covered' gets the slots it draws
    public static string Spec(bool fem, string cls, System.Func<string, string> shown, HashSet<string> covered, out Color tint, out float tintK)
    {
        tint = Color.white; tintK = 0f;
        var ch = AHItems.Get(shown("chest") ?? ""); if (ch == null) return null;
        // the Warden's own lava armour (made with Tripo from our concept art), helm and pauldrons included, in its own colours
        var cm = CollectionModel(ch.style);
        if (cm != null) { foreach (var sl in new[] { "chest", "hands", "legs", "feet", "head", "shoulders" }) covered.Add(sl); return cm; }   // the whole suit, helm and all
        string S = fem ? "qoFemale_" : "qoMale_";
        string spec = Shape(cls, S, fem, ch);
        // the class colour, with the chest piece's own colour in it (plate takes the metal's colour)
        float ck; var cc = ClassDye(cls, out ck);
        if (cls == "warrior" || (cls == null && Armor(ch) == "plate")) { tint = ch.color; tintK = 0.3f; }
        else { tint = Color.Lerp(cc, ch.color, ck > 0f ? 0.35f : 1f); tintK = Mathf.Max(ck, 0.3f); }
        if (!string.IsNullOrEmpty(ch.cls)) { tint = ch.color; tintK = 0.5f; }   // a collection piece with no model of its own yet: the class's outfit in the collection's colours
        covered.Add("chest"); covered.Add("hands"); covered.Add("legs"); covered.Add("feet");

        var hd = AHItems.Get(shown("head") ?? "");
        if (hd != null)
        {
            string f = hd.form ?? "", part = null;
            if (f == "helm" || f == "forgehelm") part = S + (cls == "warden" ? "KnightClothStone:Head" : "Knight:Head");   // the Warden's helm is horned stone
            else if (f == "horns") part = S + "Knight_Cloth:Head";
            // the Ranger's green hood suits the leather classes; robed classes keep the wardrobe's hood in the item's colour
            else if ((f == "hood" || f == "cowl" || f == "mhood") && cls != "mage" && cls != "priest" && cls != "druid") part = S + RangerOf(cls) + ":Head";
            if (part != null) { spec += "+" + part; covered.Add("head"); }
        }
        var sh = AHItems.Get(shown("shoulders") ?? "");
        if (sh != null)
        {
            string f = sh.form ?? "", sa = Armor(sh), part = null;
            if (f == "plates" || f == "spikes")
            {
                if (sa == "plate") part = S + (cls == "warden" ? "KnightClothStone" : f == "spikes" ? "Knight_Cloth" : "Knight") + ":Pauldron";
                else if (sa == "leather") part = S + RangerOf(cls) + ":Pauldron";
                else part = S + (Priest.Contains(sh.style ?? "") || cls == "priest" ? "Noble:Lion" : "Noble:Pauldron,-Lion");
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
    public string crest; public float crestSize;
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
        if (worn != null && crest != null && !spec.Contains("tqo_")) Badge(worn, crest, crestSize);   // a collection suit has its own emblem
        // the Warden's horned lava helm (Tripo) over the outfit's stone helm piece
        if (worn != null && spec.Contains("KnightClothStone:Head")) AHTripo.Helm(worn, transform, "warden_helmet");
        if (worn != null && spec.Contains("tqo_") && GetComponent<AHArmSpread>() == null) gameObject.AddComponent<AHArmSpread>();
        enabled = false;
    }

    // the crest badge: a thin metal disc with the crest on its face, on the front of the outfit's chest, following it
    void Badge(GameObject outfit, string name, float size)
    {
        var tex = Resources.Load<Texture2D>("AH/Textures/Crest/ashen_crest_" + name); if (tex == null) return;
        Transform chest = null;
        foreach (var t in outfit.GetComponentsInChildren<Transform>(true)) if (t.name == "stand_spine_03") { chest = t; break; }
        if (chest == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "spine_03") { chest = t; break; }   // the classic body
        if (chest == null) return;
        var link = GetComponent<AHVRoidLink>();
        Vector3 fwd = link != null && link.Body != null ? link.Body.forward : transform.parent != null ? transform.parent.forward : transform.forward;
        fwd.y = 0f; fwd.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, fwd);
        // the front of the chest: the outfit's furthest-forward point near the breastbone
        float best = float.MinValue; Vector3 at = chest.position + fwd * 0.12f; var baked = new Mesh();
        foreach (var smr in outfit.GetComponentsInChildren<SkinnedMeshRenderer>(false))
        {
            smr.BakeMesh(baked, true); var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one); var vs = baked.vertices;
            for (int i = 0; i < vs.Length; i++)
            {
                var p = m.MultiplyPoint3x4(vs[i]); var d = p - chest.position;
                if (Mathf.Abs(Vector3.Dot(d, right)) > 0.05f || d.y < -0.02f || d.y > 0.14f) continue;
                float f = Vector3.Dot(d, fwd); if (f > best) { best = f; at = p; }
            }
        }
        Destroy(baked);
        var root = new GameObject("CrestBadge").transform; root.SetParent(chest, false);
        root.position = at + fwd * 0.008f; root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        // keep it its own size in the world, whatever the stand's scale
        Vector3 ls = chest.lossyScale; root.localScale = new Vector3(1f / Mathf.Max(1e-4f, ls.x), 1f / Mathf.Max(1e-4f, ls.y), 1f / Mathf.Max(1e-4f, ls.z));
        int layer = gameObject.layer;
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(disc.GetComponent<Collider>()); disc.name = "Piece";
        disc.transform.SetParent(root, false); disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); disc.transform.localScale = new Vector3(size * 1.04f, 0.004f, size * 1.04f);
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var dm = new Material(lit); dm.SetColor("_BaseColor", new Color(0.16f, 0.15f, 0.17f)); dm.SetFloat("_Metallic", 0.7f); dm.SetFloat("_Smoothness", 0.5f);
        disc.GetComponent<Renderer>().sharedMaterial = dm; disc.layer = layer;
        var face = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(face.GetComponent<Collider>()); face.name = "Piece";
        face.transform.SetParent(root, false); face.transform.localPosition = new Vector3(0f, 0f, 0.0045f); face.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        face.transform.localScale = new Vector3(size, size, 1f);
        var fm = new Material(lit); fm.SetTexture("_BaseMap", tex); fm.SetColor("_BaseColor", Color.white); fm.SetFloat("_Metallic", 0.6f); fm.SetFloat("_Smoothness", 0.55f);
        fm.SetFloat("_AlphaClip", 1f); fm.SetFloat("_Cutoff", 0.5f); fm.EnableKeyword("_ALPHATEST_ON");
        fm.SetColor("_EmissionColor", new Color(0.25f, 0.18f, 0.1f)); fm.SetTexture("_EmissionMap", tex); fm.EnableKeyword("_EMISSION");
        var fr = face.GetComponent<Renderer>(); fr.sharedMaterial = fm; face.layer = layer;
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

// bulky plate (the Warden's lava armour): the arms are held a little out from the body, after the animation and before
// the VRoid body and the outfit copy the pose, so the gauntlets and arm plates swing clear of the hips and the chest
// instead of sinking into them. The weapons in the hands follow the same arms.
[DefaultExecutionOrder(19990)]
public class AHArmSpread : MonoBehaviour
{
    public float degrees = 15f;
    Transform la, ra, spine;
    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "upperarm_l") la = t; else if (t.name == "upperarm_r") ra = t; else if (t.name == "spine_03") spine = t;
        }
    }
    void LateUpdate()
    {
        if (spine == null) return;
        Spread(la); Spread(ra);
    }
    void Spread(Transform up)
    {
        if (up == null || up.childCount == 0) return;
        Transform low = null; foreach (Transform c in up) if (c.name.StartsWith("lowerarm")) { low = c; break; }
        if (low == null) return;
        Vector3 d = low.position - up.position, o = up.position - spine.position; o -= Vector3.Project(o, transform.up);
        if (d.sqrMagnitude < 1e-6f || o.sqrMagnitude < 1e-6f) return;
        Vector3 axis = Vector3.Cross(d, o); if (axis.sqrMagnitude < 1e-8f) return;
        // only an arm hanging down needs it: one raised to the side for a swing or a cast is already clear
        float hang = Vector3.Dot(d.normalized, -transform.up); if (hang <= 0.2f) return;
        up.rotation = Quaternion.AngleAxis(degrees * Mathf.Clamp01((hang - 0.2f) / 0.5f), axis.normalized) * up.rotation;
    }
}
