// Ashen Hollow: plays a model's animation clips by name, with short cross-fades.
// Works whether the glTF importer made Mecanim clips (played through Playables) or legacy clips.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AHAnim
{
    readonly List<AnimationClip> clips = new List<AnimationClip>();
    readonly List<AnimationClipPlayable> plays = new List<AnimationClipPlayable>();
    Animation legacy;
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    float[] weights;
    int cur = -1;
    bool curLoop = true;
    const float Fade = 0.15f;

    public bool HasClips { get { return clips.Count > 0; } }
    // each entry's name and the window of its clip it plays (a long single take can be split into named parts)
    readonly List<string> names = new List<string>();
    readonly List<float> t0s = new List<float>(), t1s = new List<float>();

    // split: [name, start s, end s] parts cut from the model's first clip (web nlSplit, at 30 fps)
    public AHAnim(GameObject root, AnimationClip[] found, List<object> split = null)
    {
        var all = new List<AnimationClip>();
        if (found != null) foreach (var c in found) if (c != null && !all.Contains(c)) all.Add(c);
        var segN = new List<string>(); var segA = new List<float>(); var segB = new List<float>(); var segC = new List<AnimationClip>();
        foreach (var c in all) { segN.Add(c.name); segA.Add(0f); segB.Add(c.length); segC.Add(c); }
        if (split != null && all.Count > 0)
            foreach (var o in split)
            {
                var row = o as List<object>; if (row == null || row.Count < 3) continue;
                segN.Add((string)row[0]); segA.Add((float)(double)row[1]); segB.Add(Mathf.Min((float)(double)row[2], all[0].length)); segC.Add(all[0]);
            }
        if (all.Count == 0)
        {
            // clips may already sit on a legacy Animation component
            legacy = root.GetComponentInChildren<Animation>();
            if (legacy != null) foreach (AnimationState s in legacy) if (s.clip != null) clips.Add(s.clip);
            if (legacy != null) legacy.playAutomatically = false;
            return;
        }
        bool anyLegacy = false;
        foreach (var c in all) if (c.legacy) anyLegacy = true;
        if (anyLegacy)
        {
            legacy = root.GetComponent<Animation>();
            if (legacy == null) legacy = root.AddComponent<Animation>();
            legacy.playAutomatically = false;
            foreach (var c in all)
            {
                if (!c.legacy) continue;
                if (legacy.GetClip(c.name) == null) legacy.AddClip(c, c.name);
                clips.Add(c);
            }
            return;
        }
        Animator animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("AshenHollowAnim");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        mixer = AnimationMixerPlayable.Create(graph, segC.Count);
        output.SetSourcePlayable(mixer);
        for (int i = 0; i < segC.Count; i++)
        {
            var p = AnimationClipPlayable.Create(graph, segC[i]);
            graph.Connect(p, 0, mixer, i);
            mixer.SetInputWeight(i, 0f);
            clips.Add(segC[i]); names.Add(segN[i]); t0s.Add(segA[i]); t1s.Add(segB[i]);
            plays.Add(p);
        }
        weights = new float[segC.Count];
        graph.Play();
    }

    // other names for clips (the game asks for KayKit names; a Quaternius model maps them to its own)
    public Dictionary<string, string> alias;

    int Find(string name)
    {
        if (string.IsNullOrEmpty(name)) return -1;
        string al; if (alias != null && alias.TryGetValue(name, out al)) name = al;
        string n = name.ToLowerInvariant();
        // named parts first (exact), then clip names
        for (int i = names.Count - 1; i >= 0; i--) if (names[i].ToLowerInvariant() == n) return i;
        for (int i = 0; i < clips.Count; i++) if (clips[i].name.ToLowerInvariant() == n) return i;
        for (int i = 0; i < clips.Count; i++) if (clips[i].name.ToLowerInvariant().Contains(n)) return i;
        return -1;
    }

    public bool Has(string name) { return Find(name) >= 0; }

    public float Length(string name)
    {
        int i = Find(name);
        if (i < 0) return 0f;
        return i < t1s.Count ? t1s[i] - t0s[i] : clips[i].length;
    }

    // hold one frame of a clip (a sleeping skeleton's pose): frac 0..1 of the clip or part
    public void Hold(string name, float frac)
    {
        int i = Find(name);
        if (i < 0 || legacy != null || !graph.IsValid() || i >= t0s.Count) return;
        cur = i; curLoop = false;
        plays[i].SetTime(t0s[i] + (t1s[i] - t0s[i]) * Mathf.Min(frac, 0.995f));
        plays[i].SetSpeed(0);
    }

    // play a clip; restart forces it from the beginning even if it is already playing
    public bool Play(string name, bool loop, float speed = 1f, bool restart = false)
    {
        int i = Find(name);
        if (i < 0) return false;
        if (legacy == null && i >= t0s.Count) return false;
        if (legacy != null)
        {
            string cn = clips[i].name;
            AnimationState st = legacy[cn];
            if (st == null) return false;
            st.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            st.speed = speed;
            if (restart || !legacy.IsPlaying(cn)) { st.time = 0f; legacy.CrossFade(cn, Fade); }
            return true;
        }
        if (!graph.IsValid()) return false;
        if (i != cur || restart)
        {
            plays[i].SetTime(t0s[i]);
            cur = i;
            curLoop = loop;
        }
        plays[i].SetSpeed(speed);
        return true;
    }

    // call once per frame
    public void Tick(float dt)
    {
        if (legacy != null || !graph.IsValid() || cur < 0) return;
        for (int i = 0; i < weights.Length; i++)
        {
            float target = i == cur ? 1f : 0f;
            weights[i] = Mathf.MoveTowards(weights[i], target, dt / Fade);
            mixer.SetInputWeight(i, weights[i]);
        }
        double t = plays[cur].GetTime();
        double a = t0s[cur], b = t1s[cur], len = b - a;
        if (len > 0 && t > b)
        {
            if (curLoop) plays[cur].SetTime(a + (t - a) % len);
            // stop a hair before the end: a clip imported as looping samples its end as its first frame, which stood
            // dead beasts back up
            else { plays[cur].SetTime(System.Math.Max(a, b - 1.0 / 60.0)); plays[cur].SetSpeed(0); }
        }
    }

    public void Dispose()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
