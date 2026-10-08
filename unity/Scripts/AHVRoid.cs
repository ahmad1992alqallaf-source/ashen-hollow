// Ashen Hollow: VRoid heroes. A hero made in VRoid Studio (exported as VRM and brought in by UniVRM to
// Resources/AH/VRoid) takes the place of the old body. The old rig stays as the hero's invisible "puppeteer": it
// keeps playing all the game's animations, and every frame its pose is copied onto the VRoid body through Unity's
// humanoid muscle space (so the two skeletons can be built differently). Gear the wardrobe puts on the old bones
// (helms, pauldrons, boots, weapons...) is moved onto the matching VRoid bones the moment it appears.
// Turn it off with PlayerPrefs "ah_vroid" = 0 (or when there is no VRoid model for that body).
using System.Collections.Generic;
using UnityEngine;

public static class AHVRoid
{
    static readonly Dictionary<string, Avatar> srcAvatars = new Dictionary<string, Avatar>();

    public static GameObject Last;   // the VRoid body the last Link made (an old one may still be in the holder, being destroyed)
    public static bool On { get { return AHPrefs.GetInt("ah_vroid", 1) == 1; } }

    // which VRoid model a hero uses (null: none yet)
    public static GameObject ModelFor(ClassDef cls, AHLook look)
    {
        if (!On) return null;
        bool fem = look != null && look.sex == "f";
        GameObject pf = null;
        // the hairstyle chosen in the look editor: each has its own VRoid build (hero_m_ponytail, hero_f_braid...)
        string hv = HairVariant(look != null ? look.hair : null);
        if (hv != null) pf = Resources.Load<GameObject>("AH/VRoid/hero_" + (fem ? "f_" : "m_") + hv);
        if (pf == null && cls != null) pf = Resources.Load<GameObject>("AH/VRoid/hero_" + (fem ? "f_" : "m_") + cls.id);   // a class's own look, if made
        if (pf == null) pf = Resources.Load<GameObject>("AH/VRoid/hero_" + (fem ? "f" : "m"));
        return pf;
    }

    static string HairVariant(string hair)
    {
        switch (hair)
        {
            case "pony": return "ponytail";
            case "mohawk": return "short";   // shaved sides and a crest (Style)
            case "short": case "spiky": case "shaved": case "bald": case "bob": return "short";
            case "long": case "mane": return "long";
            case "braid": return "braid";
            case "bun": return "short";   // the high bun: the short cut with a topknot on top (Style)
            case "twin": return "twin";
        }
        return null;
    }

    // the Quaternius skeleton (Unreal mannequin names) as a Unity humanoid
    static readonly string[,] Map =
    {
        { "Hips", "pelvis" }, { "Spine", "spine_01" }, { "Chest", "spine_02" }, { "UpperChest", "spine_03" }, { "Neck", "neck_01" }, { "Head", "Head" },
        { "LeftShoulder", "clavicle_l" }, { "LeftUpperArm", "upperarm_l" }, { "LeftLowerArm", "lowerarm_l" }, { "LeftHand", "hand_l" },
        { "RightShoulder", "clavicle_r" }, { "RightUpperArm", "upperarm_r" }, { "RightLowerArm", "lowerarm_r" }, { "RightHand", "hand_r" },
        { "LeftUpperLeg", "thigh_l" }, { "LeftLowerLeg", "calf_l" }, { "LeftFoot", "foot_l" }, { "LeftToes", "ball_l" },
        { "RightUpperLeg", "thigh_r" }, { "RightLowerLeg", "calf_r" }, { "RightFoot", "foot_r" }, { "RightToes", "ball_r" },
        { "Left Thumb Proximal", "thumb_01_l" }, { "Left Thumb Intermediate", "thumb_02_l" }, { "Left Thumb Distal", "thumb_03_l" },
        { "Left Index Proximal", "index_01_l" }, { "Left Index Intermediate", "index_02_l" }, { "Left Index Distal", "index_03_l" },
        { "Left Middle Proximal", "middle_01_l" }, { "Left Middle Intermediate", "middle_02_l" }, { "Left Middle Distal", "middle_03_l" },
        { "Left Ring Proximal", "ring_01_l" }, { "Left Ring Intermediate", "ring_02_l" }, { "Left Ring Distal", "ring_03_l" },
        { "Left Little Proximal", "pinky_01_l" }, { "Left Little Intermediate", "pinky_02_l" }, { "Left Little Distal", "pinky_03_l" },
        { "Right Thumb Proximal", "thumb_01_r" }, { "Right Thumb Intermediate", "thumb_02_r" }, { "Right Thumb Distal", "thumb_03_r" },
        { "Right Index Proximal", "index_01_r" }, { "Right Index Intermediate", "index_02_r" }, { "Right Index Distal", "index_03_r" },
        { "Right Middle Proximal", "middle_01_r" }, { "Right Middle Intermediate", "middle_02_r" }, { "Right Middle Distal", "middle_03_r" },
        { "Right Ring Proximal", "ring_01_r" }, { "Right Ring Intermediate", "ring_02_r" }, { "Right Ring Distal", "ring_03_r" },
        { "Right Little Proximal", "pinky_01_r" }, { "Right Little Intermediate", "pinky_02_r" }, { "Right Little Distal", "pinky_03_r" },
    };

    // built once per body, from the prefab's own rest pose (a T-pose)
    static Avatar SourceAvatar(string body, Transform inst)
    {
        Avatar av; if (srcAvatars.TryGetValue(body, out av) && av != null) return av;
        var pf = Resources.Load<GameObject>("AH/Models/Web/" + body); if (pf == null) return null;
        var names = new HashSet<string>(); foreach (var t in pf.GetComponentsInChildren<Transform>(true)) names.Add(t.name);
        var human = new List<HumanBone>();
        for (int i = 0; i < Map.GetLength(0); i++)
        {
            if (!names.Contains(Map[i, 1])) continue;
            var hb = new HumanBone { humanName = Map[i, 0], boneName = Map[i, 1] }; hb.limit.useDefaultValues = true; human.Add(hb);
        }
        var skel = new List<SkeletonBone>();
        foreach (var t in pf.GetComponentsInChildren<Transform>(true))
            skel.Add(new SkeletonBone { name = t == pf.transform ? inst.name : t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
        var hd = new HumanDescription
        {
            human = human.ToArray(), skeleton = skel.ToArray(), upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
            armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false
        };
        av = AvatarBuilder.BuildHumanAvatar(inst.gameObject, hd);
        if (av == null || !av.isValid || !av.isHuman) { Debug.LogWarning("Ashen Hollow: could not build a humanoid for " + body); av = null; }
        else av.name = body + "_human";
        srcAvatars[body] = av; return av;
    }

    // tint the VRoid materials by kind (VRoid names them ..._HAIR, ..._SKIN, ..._CLOTH); a plain colour dyes the
    // texture: a dark hair colour gives dark hair, the skin tone only nudges (the face is painted)
    static readonly Dictionary<Material, Color> shade0 = new Dictionary<Material, Color>();
    // the skin tone, strong enough that Deep and Ebony read as dark and Porcelain stays fair, never brighter than the paint
    public static Color SkinTint(Color skin)
    {
        Color k = Color.Lerp(Color.white, skin * 1.12f, 0.8f);
        k = new Color(Mathf.Min(1f, k.r), Mathf.Min(1f, k.g), Mathf.Min(1f, k.b), 1f);
        return k * 0.94f;
    }

    public static void Dye(GameObject v, Color hair, Color skin, Color cloth)
    {
        if (shade0.Count > 400) { var dead = new List<Material>(); foreach (var km in shade0.Keys) if (km == null) dead.Add(km); foreach (var km in dead) shade0.Remove(km); }
        foreach (var r in v.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials; bool ch = false;
            foreach (var m in mats)
            {
                if (m == null) continue; string n = m.name.ToUpperInvariant(); Color k;
                if (n.Contains("_HAIR")) k = Color.Lerp(hair * 1.6f, Color.white, 0.15f);
                else if (n.Contains("_SKIN") || n.Contains("_FACE")) k = SkinTint(skin);
                else if (n.Contains("_CLOTH")) k = Color.Lerp(Color.white, cloth * 1.5f, 0.6f) * 1.3f;   // the robe is mid grey: a lighter dye, brightened, keeps it light
                else continue;
                k.a = 1f;
                foreach (var pr in new[] { "_Color", "_BaseColor" }) if (m.HasProperty(pr)) { var c0 = m.GetColor(pr); m.SetColor(pr, new Color(k.r, k.g, k.b, c0.a)); }
                // the skin's shadow side takes the tone too, or a dark skin keeps pale pink shadows
                if ((n.Contains("_SKIN") || n.Contains("_FACE")) && m.HasProperty("_ShadeColor"))
                {
                    Color s0; if (!shade0.TryGetValue(m, out s0)) { s0 = m.GetColor("_ShadeColor"); shade0[m] = s0; }   // dyed again and again: start from the first
                    m.SetColor("_ShadeColor", new Color(s0.r * k.r, s0.g * k.g, s0.b * k.b, s0.a));
                }
                ch = true;
            }
            if (ch) r.materials = mats;
        }
    }

    // the face and the finishing touches the mirror sets: eye shape, brows and expression (VRoid face blend shapes), eye
    // colour, pointed or elven ears, and the hairstyles VRoid has no preset for (a topknot, a mohawk crest, spikes)
    public static void Style(GameObject v, AHLook look, Color hair, Color skin, Color eye, bool eyeSet)
    {
        if (v == null || look == null) return;
        foreach (var smr in v.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var m = smr.sharedMesh; if (m == null || m.blendShapeCount == 0) continue;
            System.Action<string, float> bs = (n, w) => { for (int i = 0; i < m.blendShapeCount; i++) if (m.GetBlendShapeName(i).EndsWith(n)) { smr.SetBlendShapeWeight(i, w); return; } };
            switch (look.eyeShape) { case "round": bs("Fcl_EYE_Surprised", 30f); break; case "narrow": bs("Fcl_EYE_Fun", 35f); break; case "wide": bs("Fcl_EYE_Surprised", 55f); break; case "sharp": bs("Fcl_EYE_Angry", 50f); break; }
            switch (look.brow) { case "arched": bs("Fcl_BRW_Surprised", 30f); break; case "fierce": bs("Fcl_BRW_Angry", 75f); break; case "soft": bs("Fcl_BRW_Sorrow", 35f); break; case "thick": bs("Fcl_BRW_Angry", 25f); break; }
            switch (look.mouth) { case "smile": bs("Fcl_MTH_Fun", 45f); break; case "smirk": bs("Fcl_MTH_Fun", 25f); bs("Fcl_MTH_Up", 25f); break; case "grin": bs("Fcl_MTH_Joy", 55f); break; case "frown": bs("Fcl_MTH_Angry", 45f); break; }
        }
        if (eyeSet) TintIris(v, eye);
        var an = v.GetComponent<Animator>(); var head = an != null ? an.GetBoneTransform(HumanBodyBones.Head) : null; if (head == null) return;
        Bounds b; if (!HeadBox(v.GetComponentsInChildren<SkinnedMeshRenderer>(true), head, out b)) return;
        float w = Mathf.Max(b.size.x, 0.12f);
        Vector3 up = v.transform.up, fw = v.transform.forward, rt = v.transform.right;
        Vector3 top = new Vector3(b.center.x, b.max.y, b.center.z);
        Color hk = Color.Lerp(hair * 1.3f, Color.white, 0.08f); hk.a = 1f;
        var hm = Mat(hk, 0.35f); var dark = Mat(hk * 0.6f, 0.2f); var gold = Mat(new Color(0.85f, 0.68f, 0.3f), 0.7f);
        System.Func<Vector3, Vector3, Quaternion, Material, Transform> piece = (at, size, rot, mat) =>
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(go.GetComponent<Collider>());
            go.name = "HairExtra"; go.transform.position = at; go.transform.rotation = rot; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat; go.transform.SetParent(head, true); return go.transform;
        };
        Quaternion face = Quaternion.LookRotation(fw, up);
        if (look.hair == "bun")
        {
            // a high topknot: a round bun on the crown, tied with a band and pinned with a gold pin
            Vector3 c = top + up * 0.06f * w - fw * 0.12f * w;
            piece(c, new Vector3(0.38f, 0.32f, 0.38f) * w, face, hm);
            piece(c - up * 0.14f * w, new Vector3(0.3f, 0.06f, 0.3f) * w, face, dark);
            piece(c + up * 0.02f * w, new Vector3(0.62f, 0.035f, 0.035f) * w, face * Quaternion.Euler(0f, 30f, 8f), gold);
        }
        else if (look.hair == "mohawk")
        {
            // shaved sides (the hair hidden, the scalp left) and a crest from brow to nape
            foreach (var r in v.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Hair")) r.enabled = false;
            for (int i = 0; i < 7; i++)
            {
                float t = -1f + i / 3f;   // -1 at the front, +1 at the back
                Vector3 at = top - fw * t * 0.42f * w + up * (0.08f - t * t * 0.22f) * w;
                piece(at, new Vector3(0.1f, 0.34f * (1f - 0.25f * Mathf.Abs(t)), 0.26f) * w, face * Quaternion.Euler(t * 35f, 0f, 0f), hm);
            }
        }
        else if (look.hair == "spiky")
        {
            // sharp tapered spikes in three rings, standing up and out from inside the hair (the face kept clear)
            float[] rr = { 0.36f, 0.22f, 0.08f }, lift = { -0.24f, -0.13f, -0.08f }, lean = { 1.25f, 0.7f, 0.2f }, len = { 0.34f, 0.4f, 0.38f }, rad = { 0.075f, 0.08f, 0.085f };
            var sm = Mat(hk * 0.78f, 0.3f);   // a shade darker: lit spikes otherwise read paler than the painted hair
            int[] cnt = { 13, 8, 4 };
            for (int ring = 0; ring < 3; ring++)
                for (int i = 0; i < cnt[ring]; i++)
                {
                    float a = (i + ring * 0.37f) * Mathf.PI * 2f / cnt[ring]; Vector3 dir = rt * Mathf.Cos(a) + fw * Mathf.Sin(a) * 0.85f;
                    if (Vector3.Dot(dir, fw) > 0.5f && ring == 0) continue;
                    Vector3 at = top + up * lift[ring] * w + dir * rr[ring] * w - fw * 0.07f * w;
                    Vector3 way = (up + dir * lean[ring] - fw * 0.25f).normalized;
                    float k = 0.85f + 0.3f * Mathf.Abs(Mathf.Sin(a * 3f + ring));
                    var sp = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(sp.GetComponent<Collider>());
                    sp.GetComponent<MeshFilter>().sharedMesh = Spike(); sp.name = "HairExtra";
                    sp.transform.position = at; sp.transform.rotation = Quaternion.FromToRotation(Vector3.up, way);
                    sp.transform.localScale = new Vector3(rad[ring], len[ring] * k, rad[ring]) * w;
                    sp.GetComponent<Renderer>().sharedMaterial = sm; sp.transform.SetParent(head, true);
                }
        }
        if (look.ears == "pointed" || look.ears == "elven")
        {
            // ears that sweep up and back from the side of the head, in the face's own tone
            float L = look.ears == "elven" ? 0.42f : 0.26f;
            Color sk = new Color(1f, 0.88f, 0.8f) * SkinTint(skin); sk.a = 1f;
            var em = new Material(Shader.Find("Universal Render Pipeline/Unlit")); em.SetColor("_BaseColor", sk);   // flat, like the VRoid face
            foreach (int sx in new[] { -1, 1 })
            {
                Vector3 root = new Vector3(b.center.x, b.center.y, b.center.z) + rt * sx * 0.47f * w - up * 0.12f * w - fw * 0.05f * w;
                Vector3 dirE = (rt * sx * 0.75f + up * 0.6f - fw * 0.35f).normalized;
                piece(root + dirE * L * 0.45f * w, new Vector3(0.09f, L, 0.2f) * w, Quaternion.LookRotation(Vector3.Cross(dirE, rt * sx).normalized, dirE), em);
            }
        }
    }

    // eye colour: VRoid paints the iris into the face's texture, so the iris area of that texture (found from the
    // triangles that follow the eye bones) is recoloured: its hue becomes the chosen colour, highlights stay
    static readonly Dictionary<string, Texture2D> irisTex = new Dictionary<string, Texture2D>();
    static void TintIris(GameObject v, Color eye)
    {
        foreach (var smr in v.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var m = smr.sharedMesh; if (m == null || smr.bones == null) continue;
            var eyeBone = new bool[smr.bones.Length]; bool any = false;
            for (int i = 0; i < smr.bones.Length; i++) if (smr.bones[i] != null && smr.bones[i].name.Contains("FaceEye")) { eyeBone[i] = true; any = true; }
            if (!any) continue;
            var uv = m.uv; var bw = m.boneWeights; if (uv == null || uv.Length != m.vertexCount || bw.Length != m.vertexCount) continue;
            var mats = smr.materials; bool changed = false;
            for (int sub = 0; sub < m.subMeshCount && sub < mats.Length; sub++)
            {
                var tri = m.GetTriangles(sub); var rects = new List<Rect>();
                for (int t = 0; t + 2 < tri.Length; t += 3)
                {
                    bool all = true; for (int k = 0; k < 3; k++) { var w0 = bw[tri[t + k]]; if (!(eyeBone[w0.boneIndex0] && w0.weight0 > 0.5f)) { all = false; break; } }
                    if (!all) continue;
                    Vector2 a = uv[tri[t]], b2 = uv[tri[t + 1]], c = uv[tri[t + 2]];
                    var r = Rect.MinMaxRect(Mathf.Min(a.x, Mathf.Min(b2.x, c.x)), Mathf.Min(a.y, Mathf.Min(b2.y, c.y)), Mathf.Max(a.x, Mathf.Max(b2.x, c.x)), Mathf.Max(a.y, Mathf.Max(b2.y, c.y)));
                    bool merged = false;
                    for (int q = 0; q < rects.Count; q++) if (rects[q].Overlaps(r)) { var o = rects[q]; rects[q] = Rect.MinMaxRect(Mathf.Min(o.xMin, r.xMin), Mathf.Min(o.yMin, r.yMin), Mathf.Max(o.xMax, r.xMax), Mathf.Max(o.yMax, r.yMax)); merged = true; break; }
                    if (!merged) rects.Add(r);
                }
                if (rects.Count == 0 || mats[sub] == null) continue;
                var src = mats[sub].mainTexture; if (src == null) continue;
                string key = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(src) + "|" + ColorUtility.ToHtmlStringRGB(eye);
                Texture2D tex;
                if (!irisTex.TryGetValue(key, out tex) || tex == null)
                {
                    var rtx = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(src, rtx); var prev = RenderTexture.active; RenderTexture.active = rtx;
                    tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true); tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                    RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rtx);
                    float th, ts, tv; Color.RGBToHSV(eye, out th, out ts, out tv);
                    var px = tex.GetPixels32();
                    foreach (var r in rects)
                    {
                        int x0 = Mathf.Clamp(Mathf.FloorToInt(r.xMin * tex.width) - 2, 0, tex.width - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(r.xMax * tex.width) + 2, 0, tex.width - 1);
                        int y0 = Mathf.Clamp(Mathf.FloorToInt(r.yMin * tex.height) - 2, 0, tex.height - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(r.yMax * tex.height) + 2, 0, tex.height - 1);
                        for (int y = y0; y <= y1; y++)
                            for (int x = x0; x <= x1; x++)
                            {
                                int i = y * tex.width + x; Color c = px[i]; float h, sa, va; Color.RGBToHSV(c, out h, out sa, out va);
                                if (sa < 0.12f || va < 0.06f) continue;   // the white highlights and the black pupil stay
                                Color n = Color.HSVToRGB(th, Mathf.Clamp01(Mathf.Max(sa, ts * 0.85f)), Mathf.Clamp01(va * Mathf.Lerp(0.85f, 1.25f, tv)));
                                n.a = c.a; px[i] = n;
                            }
                    }
                    tex.SetPixels32(px); tex.Apply(true); tex.wrapMode = src.wrapMode; tex.filterMode = src.filterMode;
                    irisTex[key] = tex;
                }
                mats[sub].mainTexture = tex; changed = true;
            }
            if (changed) smr.materials = mats;
        }
    }

    // the class look: a sash in the class's colour over the robe's belt, tied at the hip with two trailing ribbons
    public static void ClassSash(GameObject v, Color c)
    {
        if (v == null) return;
        var an = v.GetComponent<Animator>(); if (an == null) return;
        var spine = an.GetBoneTransform(HumanBodyBones.Spine); var hips = an.GetBoneTransform(HumanBodyBones.Hips); if (spine == null || hips == null) return;
        float y = Mathf.Lerp(hips.position.y, spine.position.y, 0.75f) + 0.02f;
        // how far the body (with its robe) reaches at that height
        Vector3 ctr = new Vector3(hips.position.x, y, hips.position.z); Vector3 rt = v.transform.right, fw = v.transform.forward;
        float sx = 0.13f, sz = 0.1f; var baked = new Mesh();
        foreach (var smr in v.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.name.Contains("Hair") || smr.name.Contains("Face")) continue;
            smr.BakeMesh(baked, true); var vs = baked.vertices; var M = smr.transform.localToWorldMatrix;
            for (int i = 0; i < vs.Length; i += 3) { var p = M.MultiplyPoint3x4(vs[i]); if (Mathf.Abs(p.y - y) > 0.035f) continue; Vector3 d = p - ctr; float ax = Mathf.Abs(Vector3.Dot(d, rt)), az = Mathf.Abs(Vector3.Dot(d, fw)); if (ax < 0.3f && az < 0.3f) { sx = Mathf.Max(sx, ax); sz = Mathf.Max(sz, az); } }
        }
        Object.Destroy(baked);
        Color k = c; k.a = 1f; var mat = Mat(k, 0.25f); var dark = Mat(k * 0.7f, 0.2f);
        var band = new GameObject("ClassSash"); band.transform.position = ctr; band.transform.rotation = Quaternion.LookRotation(fw, Vector3.up);
        band.AddComponent<MeshFilter>().sharedMesh = Band(sx + 0.012f, sz + 0.012f, 0.07f, 28);
        var mr = band.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        band.transform.SetParent(spine, true);
        // the knot at the left hip and two ribbons hanging from it
        Vector3 knot = ctr - rt * (sx + 0.01f) + fw * 0.02f;
        System.Func<Vector3, Vector3, Vector3, Material, Transform> box = (at, size, eul, m) =>
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(go.GetComponent<Collider>()); go.name = "ClassSashRibbon";
            go.transform.position = at; go.transform.rotation = Quaternion.LookRotation(fw, Vector3.up) * Quaternion.Euler(eul); go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m; go.transform.SetParent(hips, true); return go.transform;
        };
        box(knot, new Vector3(0.05f, 0.06f, 0.05f), Vector3.zero, dark);
        box(knot - Vector3.up * 0.2f - rt * 0.012f, new Vector3(0.012f, 0.38f, 0.06f), new Vector3(0f, 0f, -6f), mat);
        box(knot - Vector3.up * 0.16f + fw * 0.03f - rt * 0.008f, new Vector3(0.012f, 0.3f, 0.05f), new Vector3(8f, 0f, -3f), mat);
    }
    // an open band (no caps) round the waist
    static Mesh Band(float rx, float rz, float h, int seg)
    {
        var vs = new List<Vector3>(); var tr = new List<int>();
        for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2f / seg; float x = Mathf.Cos(a) * rx, z = Mathf.Sin(a) * rz; vs.Add(new Vector3(x, -h / 2f, z)); vs.Add(new Vector3(x, h / 2f, z)); }
        for (int i = 0; i < seg; i++) { int a = i * 2; tr.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2, a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
        var m = new Mesh { name = "sash" }; m.SetVertices(vs); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    static readonly Dictionary<int, Material> extraMats = new Dictionary<int, Material>();
    static Material Mat(Color c, float smooth)
    {
        int key = (Mathf.RoundToInt(c.r * 255f) << 16) | (Mathf.RoundToInt(c.g * 255f) << 8) | Mathf.RoundToInt(c.b * 255f);
        Material m; if (extraMats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        extraMats[key] = m; return m;
    }

    // swap the hero's look for the VRoid body; returns false (and changes nothing) if it can't
    public static bool Link(GameObject rig, Transform holder, string body, GameObject model)
    {
        if (rig == null || model == null) return false;
        var arm = rig.transform.Find("Armature"); if (arm == null) return false;
        var av = SourceAvatar(body, arm); if (av == null) return false;
        var v = Object.Instantiate(model, holder, false); v.name = "VRoid"; Last = v;
        v.transform.localPosition = Vector3.zero; v.transform.localRotation = Quaternion.identity;
        var an = v.GetComponent<Animator>(); if (an == null || an.avatar == null || !an.avatar.isHuman) { Object.Destroy(v); return false; }
        an.enabled = false;   // posed by the link, not by its own animator
        // the hair's spring bones are switched off: driven by a pose copied from another rig they can run away
        // (strands stretched across the screen, NaN pixels flashing white); the hair stays styled as made
        foreach (var c in v.GetComponentsInChildren<MonoBehaviour>(true)) if (c != null && c.GetType().Name == "VRMSpringBone") c.enabled = false;
        foreach (var smr in v.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.updateWhenOffscreen = true; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
        // the two heads, measured in the rest pose before the old one hides: helms, hoods and hats were shaped for the
        // old head, so they move onto the VRoid head top to top and scale to its width
        Transform qHead = null; foreach (var t in arm.GetComponentsInChildren<Transform>(true)) if (t.name == "Head") { qHead = t; break; }
        var vHead = an.GetBoneTransform(HumanBodyBones.Head);
        Bounds qb = new Bounds(), vb = new Bounds();
        bool headOk = qHead != null && vHead != null
            && HeadBox(rig.GetComponentsInChildren<SkinnedMeshRenderer>(true), qHead, out qb)
            && HeadBox(v.GetComponentsInChildren<SkinnedMeshRenderer>(true), vHead, out vb);
        // the old body vanishes (its bones keep moving)
        // (only the body's own meshes: anything hanging from a bone, like the weapon in the hand, stays)
        var boneNames = new HashSet<string>(); for (int i = 0; i < Map.GetLength(0); i++) boneNames.Add(Map[i, 1]);
        foreach (var r in arm.GetComponentsInChildren<Renderer>(true))
        {
            bool onBone = false; for (var t = r.transform.parent; t != null && t != arm; t = t.parent) if (boneNames.Contains(t.name)) { onBone = true; break; }
            if (!onBone) r.enabled = false;
        }
        var link = rig.AddComponent<AHVRoidLink>();
        link.Setup(arm, av, v.transform, an.avatar);
        if (headOk)
        {
            // the VRoid skull plus its hair is about an eighth wider than the face mesh alone
            // (the old male head is broad, so on the male VRoid head the gear needs a little more size and sits lower)
            bool male = model.name.StartsWith("hero_m");
            float qw = Mathf.Max(qb.size.x, qb.size.z), vw = Mathf.Max(vb.size.x, vb.size.z) * (male ? 1.5f : 1.3f);
            link.SetupHead(qHead, qHead.InverseTransformPoint(new Vector3(qb.center.x, qb.max.y, qb.center.z)),
                           vHead, vHead.InverseTransformPoint(new Vector3(vb.center.x, vb.max.y - (male ? 0.012f : 0.018f), vb.center.z)),
                           qw > 1e-3f ? Mathf.Clamp(vw / qw, 0.6f, 1.8f) : 1f);
        }
        return true;
    }

    // a hair spike: a six-sided cone, base radius 1 at y = 0, tip at y = 1
    static Mesh spike;
    static Mesh Spike()
    {
        if (spike != null) return spike;
        var v = new List<Vector3>(); var t = new List<int>(); const int n = 6;
        var ring = new Vector3[n]; for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n; ring[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); }
        System.Action<Vector3, Vector3, Vector3> tri = (a, b, c) => { int k = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(k); t.Add(k + 1); t.Add(k + 2); };
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            tri(ring[i], Vector3.up, ring[j]); tri(ring[j], Vector3.up, ring[i]);           // the side, both ways (flat-shaded facets)
            tri(ring[j], Vector3.zero, ring[i]); tri(ring[i], Vector3.zero, ring[j]);       // the base
        }
        spike = new Mesh(); spike.SetVertices(v); spike.SetTriangles(t, 0); spike.RecalculateNormals(); spike.RecalculateBounds(); spike.name = "HairSpike";
        return spike;
    }

    // a costume (or the plain clothes worn under gear): the body of another export of the same VRoid hero, moved onto
    // this hero's own skeleton. The same body and the same bones, so it fits exactly and bends with every pose. Bones
    // only the costume has (a coat's tails, a cape) come along, under the same parent bone. The hero keeps their own
    // face and hair; from the hero's old body only the back hair stays, from the costume everything but its hair.
    public static SkinnedMeshRenderer WearBody(GameObject v, GameObject pf, string name)
    {
        if (v == null || pf == null) return null;
        SkinnedMeshRenderer mine = null;
        foreach (var s in v.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.name == "Body") { mine = s; break; }
        if (mine == null) return null;
        var tmp = new GameObject("CostumeTmp"); tmp.SetActive(false);   // built switched off: none of its scripts wake up
        var inst = Object.Instantiate(pf, tmp.transform, false);
        SkinnedMeshRenderer theirs = null;
        foreach (var s in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.name == "Body") { theirs = s; break; }
        if (theirs == null) { Object.Destroy(tmp); return null; }
        var bones = new Dictionary<string, Transform>();
        foreach (var t in v.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(t.name)) bones[t.name] = t;
        var src = theirs.bones; var nb = new Transform[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            var b = src[i]; if (b == null) continue;
            if (!bones.ContainsKey(b.name))
            {
                // the highest bone of this branch the hero lacks moves across whole, with its children
                var top = b; while (top.parent != null && top.parent != inst.transform && !bones.ContainsKey(top.parent.name)) top = top.parent;
                Transform at; if (top.parent == null || !bones.TryGetValue(top.parent.name, out at)) at = mine.rootBone != null ? mine.rootBone : v.transform;
                top.SetParent(at, false);
                foreach (var t in top.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(t.name)) bones[t.name] = t;
            }
            Transform m; nb[i] = bones.TryGetValue(b.name, out m) ? m : null;
        }
        Transform rb = null; if (theirs.rootBone != null) bones.TryGetValue(theirs.rootBone.name, out rb);
        theirs.transform.SetParent(mine.transform.parent, false);
        theirs.bones = nb; theirs.rootBone = rb != null ? rb : mine.rootBone;
        theirs.name = name; theirs.updateWhenOffscreen = true; theirs.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        theirs.sharedMesh = Keep(theirs.sharedMesh, theirs.sharedMaterials, false);
        mine.sharedMesh = Keep(mine.sharedMesh, mine.sharedMaterials, true);
        Object.Destroy(tmp);
        return theirs;
    }

    // a copy of a mesh with only its hair parts (hair = true) or only the rest; made once and reused
    static readonly Dictionary<Mesh, Mesh> keepHair = new Dictionary<Mesh, Mesh>(), keepRest = new Dictionary<Mesh, Mesh>();
    static Mesh Keep(Mesh m, Material[] mats, bool hair)
    {
        if (m == null) return null;
        var cache = hair ? keepHair : keepRest; Mesh c;
        if (cache.TryGetValue(m, out c) && c != null) return c;
        if (keepHair.ContainsValue(m) || keepRest.ContainsValue(m)) return m;   // already a kept copy
        c = Object.Instantiate(m); c.name = m.name + (hair ? "_hair" : "_body");
        for (int i = 0; i < c.subMeshCount && i < mats.Length; i++)
        {
            bool isHair = mats[i] != null && mats[i].name.ToUpperInvariant().Contains("_HAIR");
            if (isHair != hair) c.SetTriangles(new int[0], i);
        }
        cache[m] = c; return c;
    }

    // the head itself (not hair, brows, eyes or a beard): every vertex that mostly follows the head bone, in the world
    static bool HeadBox(SkinnedMeshRenderer[] smrs, Transform head, out Bounds b)
    {
        b = new Bounds(); bool any = false; var baked = new Mesh();
        foreach (var smr in smrs)
        {
            string n = smr.name; if (n.Contains("Hair") || n.Contains("Brow") || n.Contains("Eye") || n.Contains("Beard") || n.StartsWith("Outfit")) continue;
            var sm = smr.sharedMesh; if (sm == null || smr.bones == null) continue;
            int hi = System.Array.IndexOf(smr.bones, head); if (hi < 0) continue;
            var bws = sm.boneWeights; if (bws.Length != sm.vertexCount) continue;
            smr.BakeMesh(baked, true); var vs = baked.vertices; if (vs.Length != bws.Length) continue;
            var M = smr.transform.localToWorldMatrix;
            for (int i = 0; i < vs.Length; i++)
            {
                if (bws[i].boneIndex0 != hi || bws[i].weight0 < 0.6f) continue;
                var p = M.MultiplyPoint3x4(vs[i]);
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
        }
        Object.Destroy(baked);
        return any && b.size.y > 0.05f;
    }
}

[DefaultExecutionOrder(20000)]
public class AHVRoidLink : MonoBehaviour
{
    Transform src, dst; HumanPoseHandler hs, hd; HumanPose pose;
    readonly Dictionary<Transform, Transform> boneMap = new Dictionary<Transform, Transform>();
    readonly HashSet<Transform> skeleton = new HashSet<Transform>();
    int lastKids = -1;
    Vector3 rig0, dst0;   // the old body's rig and the VRoid body start side by side; the VRoid body follows the rig's lift (wading, leaping, the saddle)
    public Transform Body { get { return dst; } }
    public Transform Map(Transform srcBone) { Transform d; return srcBone != null && boneMap.TryGetValue(srcBone, out d) ? d : null; }

    public void Setup(Transform srcRoot, Avatar srcAv, Transform dstRoot, Avatar dstAv)
    {
        src = srcRoot; dst = dstRoot;
        rig0 = transform.localPosition; dst0 = dst.localPosition;
        hs = new HumanPoseHandler(srcAv, srcRoot); hd = new HumanPoseHandler(dstAv, dstRoot);
        // the skeleton is only the body's own bones (a weapon already in the hand is not part of it, and moves)
        var bones = new HashSet<string>(); foreach (var sb in srcAv.humanDescription.skeleton) bones.Add(sb.name);
        foreach (var t in srcRoot.GetComponentsInChildren<Transform>(true)) if (bones.Contains(t.name)) skeleton.Add(t);
        // pair the bones the wardrobe hangs things on
        var dstAnim = dstRoot.GetComponent<Animator>();
        foreach (HumanBodyBones hb in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (hb == HumanBodyBones.LastBone) continue;
            var d = dstAnim.GetBoneTransform(hb); if (d == null) continue;
            string sn = null; string hn = HumanTrait.BoneName[(int)hb];
            foreach (var h in srcAv.humanDescription.human) if (h.humanName == hn) { sn = h.boneName; break; }
            if (sn == null) continue;
            foreach (var t in skeleton) if (t.name == sn) { boneMap[t] = d; break; }
        }
        Copy();
    }

    void Copy()
    {
        if (hs == null) return;
        hs.GetHumanPose(ref pose);
        // a broken frame (the old rig hidden, scaled to nothing or mid-teleport) can give NaN muscles: a NaN pose
        // turns the whole body into NaN pixels that bloom into a white flash, so such frames are skipped
        if (!Ok(pose.bodyPosition) || float.IsNaN(pose.bodyRotation.x) || float.IsNaN(pose.bodyRotation.w)) return;
        if (pose.muscles != null) for (int i = 0; i < pose.muscles.Length; i++) if (float.IsNaN(pose.muscles[i]) || float.IsInfinity(pose.muscles[i])) return;
        hd.SetHumanPose(ref pose);
    }

    Vector3 lastPos;
    void ResetSprings()
    {
        foreach (var c in dst.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (c == null || c.GetType().Name != "VRMSpringBone") continue;
            var m = c.GetType().GetMethod("Setup"); if (m != null) m.Invoke(c, new object[] { true });
        }
    }
    static bool Ok(Vector3 v) { return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)); }

    void LateUpdate()
    {
        if (src == null || dst == null) { Destroy(this); return; }
        // the VRoid body shows and hides with the old one (riding, dying, cutscenes)
        if (dst.gameObject.activeSelf != src.gameObject.activeInHierarchy) dst.gameObject.SetActive(src.gameObject.activeInHierarchy);
        if (!src.gameObject.activeInHierarchy) return;
        // a big jump (entering a land, a teleport) would fling the hair springs and blow them up: reset them
        if ((dst.position - lastPos).sqrMagnitude > 9f) ResetSprings();
        lastPos = dst.position;
        if (dst.parent == transform.parent) dst.localPosition = dst0 + (transform.localPosition - rig0);
        Copy();
        // gear added to the old bones since last frame moves onto the VRoid bones (where it stays)
        int kids = 0; foreach (var kv in boneMap) kids += kv.Key.childCount;
        if (kids == lastKids) return;
        foreach (var kv in boneMap)
        {
            var b = kv.Key;
            for (int i = b.childCount - 1; i >= 0; i--)
            {
                var c = b.GetChild(i); if (skeleton.Contains(c)) continue;
                if (b == qHead && vHead != null && kv.Value == vHead && c.name.StartsWith("Outfit"))
                {
                    // headgear: top of the old head onto the top of the VRoid head, sized to it
                    Vector3 qt = b.TransformPoint(qTop), vt = vHead.TransformPoint(vTop);
                    c.position = vt + (c.position - qt) * headK; c.localScale *= headK;
                    c.SetParent(vHead, true); continue;
                }
                // keep where it sits relative to the bone (not where it was in the world): the VRoid body is
                // slimmer and its arms shorter, so a glove follows the VRoid hand, a pauldron the VRoid shoulder
                Vector3 off = c.position - b.position; float k = Scale(b, kv.Value);
                Transform qc = null, vc = null; foreach (Transform cc in b) if (boneMap.ContainsKey(cc)) { qc = cc; vc = boneMap[cc]; break; }
                if (qc != null && (c.position - qc.position).sqrMagnitude < off.sqrMagnitude)
                    c.position = vc.position + (c.position - qc.position) * k;   // nearer the next joint: keep to it (a boot shaft at the ankle)
                else c.position = kv.Value.position + off * k;
                c.localScale *= Mathf.Lerp(1f, k, 0.8f);
                c.SetParent(kv.Value, true);
            }
        }
        kids = 0; foreach (var kv in boneMap) kids += kv.Key.childCount; lastKids = kids;
    }

    // how much smaller the VRoid body is around this bone (by the length of the limb it starts)
    Transform qHead, vHead; Vector3 qTop, vTop; float headK = 1f;
    public float HeadScale { get { return headK; } }   // how much bigger the VRoid head is than the old one
    public void SetupHead(Transform q, Vector3 qTopLocal, Transform v, Vector3 vTopLocal, float k) { qHead = q; qTop = qTopLocal; vHead = v; vTop = vTopLocal; headK = k; lastKids = -1; }

    float Scale(Transform s, Transform d)
    {
        float k; if (scaleOf.TryGetValue(s, out k)) return k;
        Transform sc = null, dc = null;
        foreach (Transform c in s) if (boneMap.ContainsKey(c)) { sc = c; dc = boneMap[c]; break; }
        k = 1f;
        if (sc != null && dc != null)
        {
            float a = (sc.position - s.position).magnitude, b = (dc.position - d.position).magnitude;
            if (a > 1e-3f) k = Mathf.Clamp(b / a, 0.6f, 1.2f);
        }
        else if (s.parent != null && boneMap.ContainsKey(s.parent))
        {
            float a = (s.position - s.parent.position).magnitude, b = (d.position - boneMap[s.parent].position).magnitude;
            if (a > 1e-3f) k = Mathf.Clamp(b / a, 0.6f, 1.2f);
        }
        scaleOf[s] = k; return k;
    }
    readonly Dictionary<Transform, float> scaleOf = new Dictionary<Transform, float>();

    void OnDestroy() { if (hs != null) hs.Dispose(); if (hd != null) hd.Dispose(); if (dst != null) Destroy(dst.gameObject); }
}
