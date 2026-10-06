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

    public static bool On { get { return PlayerPrefs.GetInt("ah_vroid", 1) == 1; } }

    // which VRoid model a hero uses (null: none yet)
    public static GameObject ModelFor(ClassDef cls, AHLook look)
    {
        if (!On) return null;
        bool fem = look != null && look.sex == "f";
        GameObject pf = null;
        if (cls != null) pf = Resources.Load<GameObject>("AH/VRoid/hero_" + (fem ? "f_" : "m_") + cls.id);   // a class's own look, if made
        if (pf == null) pf = Resources.Load<GameObject>("AH/VRoid/hero_" + (fem ? "f" : "m"));
        return pf;
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
    public static void Dye(GameObject v, Color hair, Color skin, Color cloth)
    {
        foreach (var r in v.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials; bool ch = false;
            foreach (var m in mats)
            {
                if (m == null) continue; string n = m.name.ToUpperInvariant(); Color k;
                if (n.Contains("_HAIR")) k = Color.Lerp(hair * 1.6f, Color.white, 0.15f);
                else if (n.Contains("_SKIN") || n.Contains("_FACE")) k = Color.Lerp(Color.white, skin * 1.25f, 0.45f);
                else if (n.Contains("_CLOTH")) k = Color.Lerp(Color.white, cloth * 1.4f, 0.75f);
                else continue;
                k.a = 1f;
                foreach (var pr in new[] { "_Color", "_BaseColor" }) if (m.HasProperty(pr)) { var c0 = m.GetColor(pr); m.SetColor(pr, new Color(k.r, k.g, k.b, c0.a)); }
                ch = true;
            }
            if (ch) r.materials = mats;
        }
    }

    // swap the hero's look for the VRoid body; returns false (and changes nothing) if it can't
    public static bool Link(GameObject rig, Transform holder, string body, GameObject model)
    {
        if (rig == null || model == null) return false;
        var arm = rig.transform.Find("Armature"); if (arm == null) return false;
        var av = SourceAvatar(body, arm); if (av == null) return false;
        var v = Object.Instantiate(model, holder, false); v.name = "VRoid";
        v.transform.localPosition = Vector3.zero; v.transform.localRotation = Quaternion.identity;
        var an = v.GetComponent<Animator>(); if (an == null || an.avatar == null || !an.avatar.isHuman) { Object.Destroy(v); return false; }
        an.enabled = false;   // posed by the link, not by its own animator
        // the hair's spring bones are switched off: driven by a pose copied from another rig they can run away
        // (strands stretched across the screen, NaN pixels flashing white); the hair stays styled as made
        foreach (var c in v.GetComponentsInChildren<MonoBehaviour>(true)) if (c != null && c.GetType().Name == "VRMSpringBone") c.enabled = false;
        foreach (var smr in v.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { smr.updateWhenOffscreen = true; smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
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
        return true;
    }
}

[DefaultExecutionOrder(20000)]
public class AHVRoidLink : MonoBehaviour
{
    Transform src, dst; HumanPoseHandler hs, hd; HumanPose pose;
    readonly Dictionary<Transform, Transform> boneMap = new Dictionary<Transform, Transform>();
    readonly HashSet<Transform> skeleton = new HashSet<Transform>();
    int lastKids = -1;
    public Transform Body { get { return dst; } }

    public void Setup(Transform srcRoot, Avatar srcAv, Transform dstRoot, Avatar dstAv)
    {
        src = srcRoot; dst = dstRoot;
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
                // keep where it sits relative to the bone (not where it was in the world): the VRoid body is
                // slimmer and its arms shorter, so a glove follows the VRoid hand, a pauldron the VRoid shoulder
                Vector3 off = c.position - b.position; float k = Scale(b, kv.Value);
                c.position = kv.Value.position + off * k; c.localScale *= Mathf.Lerp(1f, k, 0.8f);
                c.SetParent(kv.Value, true);
            }
        }
        kids = 0; foreach (var kv in boneMap) kids += kv.Key.childCount; lastKids = kids;
    }

    // how much smaller the VRoid body is around this bone (by the length of the limb it starts)
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
