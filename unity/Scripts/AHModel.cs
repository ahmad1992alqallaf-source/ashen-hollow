// Ashen Hollow: loads a model from Resources/AH/Models, sizes it and stands it on the ground
using UnityEngine;
using UnityEngine.Playables;   // (SetTime, SetSourcePlayable)

public static class AHModel
{
    // holder: the object that moves and turns. The model sits inside it, scaled to size metres
    // (its height, or its length for four-legged beasts), facing the holder's forward.
    public static GameObject Spawn(Transform holder, string file, float size, bool byLength, float yawFix, out AHAnim anim, System.Collections.Generic.List<object> split = null)
    {
        anim = null;
        string path = "AH/Models/" + file;
        GameObject prefab = null;
        // an animal, mount or pet with a detailed body made for it (the same skeleton and clips, <name>_hd) wears that one
        if ((file.StartsWith("Beasts/") || file.StartsWith("Comp/")) && !file.EndsWith("_hd")) { prefab = Resources.Load<GameObject>(path + "_hd"); if (prefab != null) path += "_hd"; }
        if (prefab == null) prefab = Resources.Load<GameObject>(path);
        GameObject inst;
        if (prefab == null)
        {
            Debug.LogWarning("Ashen Hollow: model " + path + " not found, using a capsule");
            inst = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Capsule), PrimitiveType.Capsule);
            Object.Destroy(inst.GetComponent<Collider>());
            inst.transform.SetParent(holder, false);
            inst.transform.localPosition = Vector3.up;
            return inst;
        }
        inst = Object.Instantiate(prefab, holder, false);
        inst.name = file;
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.Euler(0, (AHGame.I != null ? AHGame.I.ModelYaw : 0f) + yawFix, 0);
        inst.transform.localScale = Vector3.one;
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
        foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);

        // measure, scale, and put the feet on the ground, as it stands in its idle: some rigs (Sketchfab's) lie on their side
        // at rest and only stand up in their clips, so their skins' own boxes say nothing about where their feet are
        var clipsAll = Resources.LoadAll<AnimationClip>(ClipPath(path));
        AnimationClip pose = null; foreach (var c in clipsAll) if (c != null && c.name == "Idle") { pose = c; break; }
        if (pose == null && clipsAll.Length > 0) pose = clipsAll[0];
        bool posed = false;
        Bounds b = Measure(inst, false);
        if (pose != null)
        {
            if ((Application.isEditor && !PhoneLike) || pose.legacy) { try { pose.SampleAnimation(inst, 0f); } catch { } }   // (a phone can only pose a legacy clip this way)
            else PoseByGraph(inst, pose);   // phones: the same pose through a one-frame animation graph (models whose rest pose lies on its side, the bear, were measured wrong and stood out of sight)
            // only where the skin's box is far off (other models keep the measure their feet were set by, AH/Data/mob_feet)
            Bounds bp = Measure(inst, true);
            if (Mathf.Abs(bp.size.y - b.size.y) > 0.25f * Mathf.Max(b.size.y, 0.01f) || Mathf.Abs(bp.min.y - b.min.y) > 0.25f * Mathf.Max(b.size.y, 0.01f)) { b = bp; posed = true; }
        }
        float have = byLength ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
        if (size > 0f && have > 1e-3f) inst.transform.localScale = Vector3.one * (size / have);
        b = Measure(inst, posed);
        inst.transform.position += Vector3.up * (holder.position.y - b.min.y);
        LastHeight = b.size.y;

        anim = new AHAnim(inst, clipsAll, split);
        return inst;
    }

    public static float LastHeight;   // how tall the last model came out (metres)
    public static bool PhoneLike;     // editor tests: build models (and the meadow's flowers) the way a phone does

    // put a model into the first frame of a (Mecanim) clip at run time on a phone, where SampleAnimation only works for
    // legacy clips: a throwaway playable graph evaluated once
    static void PoseByGraph(GameObject inst, AnimationClip clip)
    {
        try
        {
            var animator = inst.GetComponent<Animator>(); if (animator == null) animator = inst.AddComponent<Animator>();
            animator.applyRootMotion = false;
            var graph = UnityEngine.Playables.PlayableGraph.Create("AshenHollowPose");
            var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "Pose", animator);
            var cp = UnityEngine.Animations.AnimationClipPlayable.Create(graph, clip);
            cp.SetTime(0); output.SetSourcePlayable(cp);
            graph.Evaluate(0f);
            graph.Destroy();
        }
        catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: could not pose " + inst.name + ": " + e.Message); }
    }

    // models that share one skeleton and the very same moves keep the moves in one file only (the others' copies were
    // taken out to make the phone build smaller): each of these plays the clips of the model named after it
    static readonly System.Collections.Generic.Dictionary<string, string> SharedClips = new System.Collections.Generic.Dictionary<string, string>
    {
        { "Comp/mount_goldsteed_hd", "Comp/mount_horse_hd" }, { "Comp/mount_nightmare_hd", "Comp/mount_horse_hd" }, { "Comp/mount_voidwing_hd", "Comp/mount_horse_hd" },
        { "Comp/mount_souleater_blue_hd", "Comp/mount_souleater_black_hd" }, { "Comp/mount_souleater_green_hd", "Comp/mount_souleater_black_hd" }, { "Comp/mount_souleater_red_hd", "Comp/mount_souleater_black_hd" },
        { "Comp/mount_glacierwolf_hd", "Comp/mount_direwolf_hd" }, { "Comp/pet_snow_fox_hd", "Comp/pet_fox_hd" },
        { "Beasts/b_boar_hd", "Beasts/b_alphawolf_hd" }, { "Beasts/b_boarlord_hd", "Beasts/b_alphawolf_hd" }, { "Beasts/b_dm_behemoth", "Beasts/b_alphawolf_hd" },
        { "Beasts/b_dm_hellhound", "Beasts/b_alphawolf_hd" }, { "Beasts/b_goldhorn_hd", "Beasts/b_alphawolf_hd" }, { "Beasts/b_icewolf_hd", "Beasts/b_alphawolf_hd" },
        { "Beasts/b_mistfang_hd", "Beasts/b_alphawolf_hd" }, { "Beasts/b_rimeclaw_hd", "Beasts/b_alphawolf_hd" }, { "Beasts/b_shadowwolf_hd", "Beasts/b_alphawolf_hd" },
        { "Beasts/b_wolf_hd", "Beasts/b_alphawolf_hd" }, { "Beasts/b_tjackal_hd", "Beasts/b_jackal_hd" }, { "Beasts/b_stag_hd", "Beasts/b_doe_hd" },
    };
    public static string ClipPath(string path)
    {
        string f = path.StartsWith("AH/Models/") ? path.Substring(10) : path, from;
        return SharedClips.TryGetValue(f, out from) ? "AH/Models/" + from : path;
    }

    // a model is there in either of its forms: <file> or its detailed <file>_hd (the plain one may have been left out of the build)
    public static bool Exists(string file)
    {
        return Resources.Load<GameObject>("AH/Models/" + file) != null || Resources.Load<GameObject>("AH/Models/" + file + "_hd") != null;
    }

    static Mesh baked;
    static Bounds Measure(GameObject go) { return Measure(go, false); }
    static Bounds Measure(GameObject go, bool posed)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        bool any = false; Bounds b = new Bounds();
        foreach (var r in rs)
        {
            Bounds rb = r.bounds;
            var smr = r as SkinnedMeshRenderer;
            if (posed && smr != null && smr.sharedMesh != null)
            {
                // a skin as it is posed now (its own box is the rest pose's)
                if (baked == null) baked = new Mesh();
                smr.BakeMesh(baked, true);
                var vs = baked.vertices; var m = smr.transform.localToWorldMatrix;
                if (vs.Length > 0)
                {
                    rb = new Bounds(m.MultiplyPoint3x4(vs[0]), Vector3.zero);
                    int step = Mathf.Max(1, vs.Length / 4000);
                    for (int i = step; i < vs.Length; i += step) rb.Encapsulate(m.MultiplyPoint3x4(vs[i]));
                }
            }
            if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
        }
        return b;
    }

    public static void SetShadows(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }
}
