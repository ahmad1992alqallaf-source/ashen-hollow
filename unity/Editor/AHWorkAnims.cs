// Ashen Hollow editor tool: bakes Kevin Iglesias' "Human Crafting Animations" (Unity Asset Store, a Unity humanoid
// rig) onto our hero's own skeleton (the Quaternius one, which the VRoid body follows), as plain transform clips the
// game's AHAnim can play. Ashen Hollow → Tools: Bake Work Animations. Writes Resources/AH/Models/Web/kWorkM and kWorkF
// (man and woman). The pack's files and these baked clips are licensed art: they stay on this PC, out of GitHub.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHWorkAnims
{
    const string Pack = "Assets/Kevin Iglesias/Human Animations/";
    const string OutRoot = "Assets/AshenHollow/Resources/AH/Models/Web/";
    // our name ← the pack's file (in Animations/<Male|Female>/...), {S} = M or F
    static readonly string[,] Clips =
    {
        { "Work_Mine", "Work/Mining/Human{S}@MiningOneHand01_R - Ground.fbx" },
        { "Work_Chop", "Work/Mining/Human{S}@MiningOneHand01_R - Wall.fbx" },
        { "Work_Gather", "Work/Gathering/Human{S}@Gathering01.fbx" },
        { "Work_Gather2", "Work/Gathering/Human{S}@Gathering02.fbx" },
        { "Work_Hammer", "Work/Hammering/Human{S}@HammeringGround01_R - Loop.fbx" },
        { "Work_Farm", "Work/Farming/Human{S}@FarmingWithPlow01_R - Loop.fbx" },
        { "Work_FishCast", "Work/Fishing/Human{S}@FishingThrow01.fbx" },
        { "Work_Fish", "Work/Fishing/Human{S}@Fishing01 - Loop.fbx" },
        { "Work_FishPull", "Work/Fishing/Human{S}@FishingPullOut01.fbx" },
        { "Idle_Work", "Idles/Human{S}@Idle01.fbx" },
    };
    // the Quaternius skeleton (Unreal mannequin names) as a Unity humanoid (the same map AHVRoid uses)
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

    [MenuItem("Ashen Hollow/Tools: Bake Work Animations")]
    static void Bake()
    {
        if (Application.isPlaying) { Debug.Log("Ashen Hollow: stop Play mode first"); return; }
        int made = 0; var log = new System.Text.StringBuilder();
        foreach (var sx in new[] { "M", "F" })
        {
            string srcModel = Pack + "Models/Human" + sx + "_Model.fbx";
            var srcPf = AssetDatabase.LoadAssetAtPath<GameObject>(srcModel);
            var tgtPf = Resources.Load<GameObject>("AH/Models/Web/" + (sx == "M" ? "qMalePeasant" : "qFemalePeasant"));
            if (srcPf == null || tgtPf == null) { Debug.LogWarning("Ashen Hollow: missing " + (srcPf == null ? srcModel : "hero body")); continue; }
            var src = Object.Instantiate(srcPf); src.name = "BakeSrc"; var tgt = Object.Instantiate(tgtPf); tgt.name = "BakeTgt";
            try
            {
                var srcAnim = src.GetComponent<Animator>() ?? src.AddComponent<Animator>();
                var srcAv = srcAnim.avatar; if (srcAv == null || !srcAv.isHuman) { Debug.LogWarning("Ashen Hollow: the pack's model is not a humanoid"); continue; }
                var tgtAv = BuildAvatar(tgt); if (tgtAv == null) { Debug.LogWarning("Ashen Hollow: could not build our hero's humanoid"); continue; }
                var hpSrc = new HumanPoseHandler(srcAv, src.transform); var hpTgt = new HumanPoseHandler(tgtAv, tgt.transform);
                // the bones we write, and their paths under the hero's root
                var bones = new List<Transform>(); var paths = new List<string>();
                foreach (var t in tgt.GetComponentsInChildren<Transform>(true)) { if (t == tgt.transform) continue; bones.Add(t); paths.Add(AnimationUtility.CalculateTransformPath(t, tgt.transform)); }
                var rest = new Dictionary<Transform, Quaternion>(); var restP = new Dictionary<Transform, Vector3>(); foreach (var b in bones) { rest[b] = b.localRotation; restP[b] = b.localPosition; }
                Transform pelvis = null; foreach (var b in bones) if (b.name == "pelvis") pelvis = b;
                // the hero's own clips (qAnims.glb) address the bones from their own root: take the same prefix
                string ours = pelvis != null ? AnimationUtility.CalculateTransformPath(pelvis, tgt.transform) : "root/pelvis", prefix = "", theirs = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/AshenHollow/Resources/AH/Models/Web/qAnims.glb"))
                {
                    var qc = o as AnimationClip; if (qc == null) continue;
                    foreach (var bd in AnimationUtility.GetCurveBindings(qc)) if (bd.path == "pelvis" || bd.path.EndsWith("/pelvis")) { theirs = bd.path; break; }
                    if (theirs != null) break;
                }
                if (theirs != null && theirs.EndsWith(ours)) prefix = theirs.Substring(0, theirs.Length - ours.Length);
                else if (theirs != null && ours.EndsWith(theirs)) { prefix = null; }   // theirs is shorter: cut ours down (handled below)
                log.AppendLine(sx + ": hero clip pelvis path '" + theirs + "', ours '" + ours + "', prefix '" + prefix + "'");
                string cut = theirs != null && prefix == null ? ours.Substring(0, ours.Length - theirs.Length) : "";
                for (int k = 0; k < paths.Count; k++)
                {
                    if (prefix != null) paths[k] = prefix + paths[k];
                    else if (cut.Length > 0 && paths[k].StartsWith(cut)) paths[k] = paths[k].Substring(cut.Length);
                }
                string outDir = OutRoot + "kWork" + sx; Directory.CreateDirectory(outDir);
                for (int i = 0; i < Clips.GetLength(0); i++)
                {
                    string file = Pack + "Animations/" + (sx == "M" ? "Male/" : "Female/") + Clips[i, 1].Replace("{S}", sx);
                    AnimationClip sc = null; foreach (var o in AssetDatabase.LoadAllAssetsAtPath(file)) { var c = o as AnimationClip; if (c != null && !c.name.StartsWith("__preview")) { sc = c; break; } }
                    if (sc == null) { log.AppendLine("missing " + file); continue; }
                    foreach (var b in bones) { b.localRotation = rest[b]; b.localPosition = restP[b]; }
                    var outClip = new AnimationClip { name = Clips[i, 0], frameRate = 30f };   // a Mecanim clip, like the hero's others (a legacy one would switch the whole hero to legacy)
                    int frames = Mathf.Max(2, Mathf.CeilToInt(sc.length * 30f) + 1);
                    var curves = new Dictionary<string, AnimationCurve[]>();
                    var pose = new HumanPose(); Vector3 body0 = Vector3.zero;
                    for (int f = 0; f < frames; f++)
                    {
                        float t = Mathf.Min(sc.length, f / 30f);
                        sc.SampleAnimation(src, t);
                        hpSrc.GetHumanPose(ref pose);
                        if (f == 0) body0 = pose.bodyPosition;
                        pose.bodyPosition = new Vector3(body0.x, pose.bodyPosition.y, body0.z);   // in place: the game moves the hero itself
                        hpTgt.SetHumanPose(ref pose);
                        for (int k = 0; k < bones.Count; k++)
                        {
                            var b = bones[k]; string key = paths[k];
                            AnimationCurve[] cv; if (!curves.TryGetValue(key, out cv)) { cv = new AnimationCurve[7]; for (int z = 0; z < 7; z++) cv[z] = new AnimationCurve(); curves[key] = cv; }
                            var q = b.localRotation; cv[0].AddKey(t, q.x); cv[1].AddKey(t, q.y); cv[2].AddKey(t, q.z); cv[3].AddKey(t, q.w);
                            if (b == pelvis) { var lp = b.localPosition; cv[4].AddKey(t, lp.x); cv[5].AddKey(t, lp.y); cv[6].AddKey(t, lp.z); }
                        }
                    }
                    foreach (var kv in curves)
                    {
                        string[] rp = { "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
                        for (int z = 0; z < 4; z++) outClip.SetCurve(kv.Key, typeof(Transform), rp[z], kv.Value[z]);
                        if (kv.Value[4].length > 0) { outClip.SetCurve(kv.Key, typeof(Transform), "m_LocalPosition.x", kv.Value[4]); outClip.SetCurve(kv.Key, typeof(Transform), "m_LocalPosition.y", kv.Value[5]); outClip.SetCurve(kv.Key, typeof(Transform), "m_LocalPosition.z", kv.Value[6]); }
                    }
                    outClip.EnsureQuaternionContinuity();
                    var st = AnimationUtility.GetAnimationClipSettings(outClip); st.loopTime = Clips[i, 0] != "Work_FishCast" && Clips[i, 0] != "Work_FishPull"; AnimationUtility.SetAnimationClipSettings(outClip, st);
                    string path = outDir + "/" + Clips[i, 0] + ".anim"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(outClip, path); made++;
                }
                hpSrc.Dispose(); hpTgt.Dispose();
            }
            finally { Object.DestroyImmediate(src); Object.DestroyImmediate(tgt); }
        }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("Ashen Hollow: baked " + made + " work animations\n" + log);
    }

    static Avatar BuildAvatar(GameObject inst)
    {
        var names = new HashSet<string>(); foreach (var t in inst.GetComponentsInChildren<Transform>(true)) names.Add(t.name);
        var human = new List<HumanBone>();
        for (int i = 0; i < Map.GetLength(0); i++) { if (!names.Contains(Map[i, 1])) continue; var hb = new HumanBone { humanName = Map[i, 0], boneName = Map[i, 1] }; hb.limit.useDefaultValues = true; human.Add(hb); }
        var skel = new List<SkeletonBone>();
        foreach (var t in inst.GetComponentsInChildren<Transform>(true)) skel.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
        var hd = new HumanDescription { human = human.ToArray(), skeleton = skel.ToArray(), upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f, armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false };
        var av = AvatarBuilder.BuildHumanAvatar(inst, hd);
        return av != null && av.isValid && av.isHuman ? av : null;
    }

    // ---- test: a hero plays each work animation, pictured from the side at three moments (HeroShots/work_anims.png) ----
    static readonly string[] Show = { "Idle", "Work_Chop", "Work_Mine", "Work_Gather", "Work_Hammer", "Work_Farm", "Work_Fish" };
    static bool showRig = false; static float lastTick; static GameObject stand; static AHAnim standAnim; static int si, shot; static double nextAt; static Texture2D sheet; static Camera scam; static RenderTexture srt;
    [MenuItem("Ashen Hollow/Test: Work Animation Shots")]
    static void Shots()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) { Debug.Log("Ashen Hollow: needs Play mode"); return; }
        if (stand != null) Object.Destroy(stand);
        var holder = new GameObject("WorkStand").transform; holder.position = new Vector3(6000f, -900f, 0f);
        var look = g.player.look ?? new AHLook();
        stand = holder.gameObject; AHAnim a; var rig = AHPeople.BuildHero(holder, g.player.cls, look, g, out a); standAnim = a;
        if (rig == null || a == null) { Debug.Log("Ashen Hollow: no stand hero"); return; }
        const int L = 29; foreach (var t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = L;
        var cg = new GameObject("WorkCam"); cg.transform.SetParent(holder, false); scam = cg.AddComponent<Camera>(); scam.fieldOfView = 30f; scam.clearFlags = CameraClearFlags.SolidColor; scam.backgroundColor = new Color(0.55f, 0.6f, 0.68f); scam.cullingMask = 1 << L;
        cg.transform.position = holder.position + new Vector3(4.2f, 1.1f, 0f); cg.transform.LookAt(holder.position + Vector3.up * 0.95f);
        var lg = new GameObject("WorkLight"); lg.transform.SetParent(holder, false); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.cullingMask = 1 << L; li.intensity = 1.3f; lg.transform.rotation = Quaternion.Euler(40f, -60f, 0f);
        srt = new RenderTexture(300, 400, 24); scam.targetTexture = srt;
        sheet = new Texture2D(300 * 3, 400 * Show.Length, TextureFormat.RGB24, false);
        si = 0; shot = -1; lastTick = 0f; nextAt = EditorApplication.timeSinceStartup + 1.0;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (!Application.isPlaying || stand == null) { EditorApplication.update -= Tick; return; }
        float now = (float)EditorApplication.timeSinceStartup; if (lastTick > 0f) standAnim.Tick(Mathf.Min(0.1f, now - lastTick)); lastTick = now;   // the blend weights move only when ticked (the hero does it every frame)
        if (EditorApplication.timeSinceStartup < nextAt) return;
        foreach (var r in stand.GetComponentsInChildren<Renderer>(true)) { r.forceRenderingOff = false; if (showRig) r.enabled = true; }
        if (shot < 0) { bool ok = standAnim.Play(Show[si], true, 1f, true); if (!ok) Debug.Log("Ashen Hollow: no clip " + Show[si]); shot = 0; nextAt = EditorApplication.timeSinceStartup + 0.25; return; }
        scam.Render(); RenderTexture.active = srt; var tex = new Texture2D(300, 400, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 300, 400), 0, 0); tex.Apply(); RenderTexture.active = null;
        sheet.SetPixels(shot * 300, (Show.Length - 1 - si) * 400, 300, 400, tex.GetPixels()); Object.Destroy(tex);
        shot++; nextAt = EditorApplication.timeSinceStartup + 0.3;
        if (shot >= 3) { shot = -1; si++; }
        if (si >= Show.Length)
        {
            EditorApplication.update -= Tick; sheet.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/work_anims.png"), sheet.EncodeToPNG());
            Object.Destroy(sheet); scam.targetTexture = null; Object.Destroy(srt); Object.Destroy(stand); stand = null;
            Debug.Log("Ashen Hollow: work animation shots saved to HeroShots/work_anims.png");
        }
    }
}
