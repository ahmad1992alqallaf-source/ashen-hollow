// Ashen Hollow: emotes. Some play a clip from the hero animation set (dance, sit, nod, talk, folded arms, eat,
// point, shadowbox); the others (wave, bow, cheer, shake your head) bend the bones on top of the standing pose.
// They run on the hero's own skeleton before the VRoid body copies it, so both looks show them.
// Moving, attacking, dodging, mounting or working ends an emote; a looping one (dance, sit...) lasts until then.
using System.Collections.Generic;
using UnityEngine;

public static class AHEmote
{
    public class Def { public string id, name, verb, clip, proc; public bool loop; public float time, speed = 1f; public Color col; }
    public static readonly List<Def> All = new List<Def>
    {
        new Def { id = "wave",  name = "Wave",      verb = "waves",              clip = "Idle", proc = "wave",  time = 2.6f, col = new Color(1f, 0.85f, 0.5f) },
        new Def { id = "bow",   name = "Bow",       verb = "bows",               clip = "Idle", proc = "bow",   time = 2.2f, col = new Color(1f, 0.85f, 0.5f) },
        new Def { id = "yes",   name = "Nod",       verb = "nods",               clip = "Yes",  time = 1.6f, col = new Color(0.6f, 0.95f, 0.6f) },
        new Def { id = "no",    name = "No",        verb = "shakes their head",  clip = "Idle", proc = "no",    time = 1.6f, col = new Color(1f, 0.6f, 0.5f) },
        new Def { id = "cheer", name = "Cheer",     verb = "cheers!",            clip = "Idle", proc = "cheer", time = 2.4f, col = new Color(1f, 0.8f, 0.3f) },
        new Def { id = "dance", name = "Dance",     verb = "dances",             clip = "Dance_Loop", loop = true, col = new Color(1f, 0.55f, 0.9f) },
        new Def { id = "point", name = "Point",     verb = "points",             clip = "Spell_Simple_Shoot", time = 1.3f, speed = 0.8f, col = new Color(0.7f, 0.85f, 1f) },
        new Def { id = "box",   name = "Shadowbox", verb = "shadowboxes",        clip = "Punch_Jab", loop = true, speed = 0.9f, col = new Color(1f, 0.6f, 0.4f) },
        new Def { id = "sit",   name = "Sit",       verb = "sits down",          clip = "Sitting_Idle_Loop", proc = "sit", loop = true, col = new Color(0.7f, 0.9f, 0.7f) },
        new Def { id = "talk",  name = "Talk",      verb = "chats",              clip = "Idle_Talking_Loop", loop = true, col = new Color(0.85f, 0.85f, 1f) },
        new Def { id = "fold",  name = "Arms folded", verb = "waits",            clip = "Idle_FoldArms_Loop", loop = true, col = new Color(0.85f, 0.85f, 1f) },
        new Def { id = "eat",   name = "Snack",     verb = "has a snack",        clip = "Consume", time = 2.2f, col = new Color(1f, 0.8f, 0.5f) },
    };
    public static Def Find(string id) { return string.IsNullOrEmpty(id) ? null : All.Find(e => e.id == id); }
}

// bends the bones for the procedural emotes (after the animation, before the VRoid body copies the pose)
[DefaultExecutionOrder(19000)]
public class AHEmotePose : MonoBehaviour
{
    public string proc; public float t, w, target;
    Transform root, pelvis, spine1, spine2, neck, head, uaR, laR, hR, uaL, laL, hL, thL, caL, ftL, thR, caR, ftR;
    GameObject boundTo;

    void Bind(GameObject model)
    {
        boundTo = model; root = transform;
        var all = model.GetComponentsInChildren<Transform>(true);
        System.Func<string, Transform> F = n => { foreach (var x in all) if (x.name == n) return x; return null; };
        pelvis = F("pelvis"); spine1 = F("spine_01"); spine2 = F("spine_02"); neck = F("neck_01"); head = F("Head") ?? F("head");
        uaR = F("upperarm_r"); laR = F("lowerarm_r"); hR = F("hand_r"); uaL = F("upperarm_l"); laL = F("lowerarm_l"); hL = F("hand_l");
        thL = F("thigh_l"); caL = F("calf_l"); ftL = F("foot_l"); thR = F("thigh_r"); caR = F("calf_r"); ftR = F("foot_r");
    }

    public void Begin(GameObject model, string p) { if (model != boundTo) Bind(model); proc = p; t = 0f; target = 1f; }
    public void End() { target = 0f; }

    void LateUpdate()
    {
        if (string.IsNullOrEmpty(proc) || boundTo == null) return;
        float dt = Time.deltaTime; t += dt;
        w = Mathf.MoveTowards(w, target, dt * 4f);
        if (w <= 0f && target <= 0f) { proc = null; return; }
        Vector3 up = Vector3.up, fwd = root.forward, right = root.right;
        switch (proc)
        {
            case "wave":
            {
                float s = Side(uaR, right);
                Aim(uaR, laR, up * 0.8f + right * s * 0.7f + fwd * 0.2f, w);
                Aim(laR, hR, up + right * s * (0.15f + 0.45f * Mathf.Sin(t * 9f)) + fwd * 0.12f, w);
                break;
            }
            case "cheer":
            {
                float hop = Mathf.Abs(Mathf.Sin(t * 5.5f));
                foreach (var arm in new[] { new[] { uaR, laR, hR }, new[] { uaL, laL, hL } })
                {
                    float s = Side(arm[0], right);
                    Aim(arm[0], arm[1], up + right * s * 0.4f + fwd * 0.15f, w);
                    Aim(arm[1], arm[2], up + right * s * (0.1f + 0.2f * hop) + fwd * 0.1f, w);
                }
                if (pelvis != null) pelvis.position += up * hop * 0.07f * w;
                break;
            }
            case "bow":
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.55f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 1.4f) / 0.6f)) * w;
                if (spine1 != null) spine1.rotation = Quaternion.AngleAxis(24f * k, right) * spine1.rotation;
                if (spine2 != null) spine2.rotation = Quaternion.AngleAxis(16f * k, right) * spine2.rotation;
                if (neck != null) neck.rotation = Quaternion.AngleAxis(12f * k, right) * neck.rotation;
                // the right hand to the chest
                if (uaR != null) { Aim(uaR, laR, -up + fwd * 0.35f, k); Aim(laR, hR, -right * Side(uaR, right) + fwd * 0.4f + up * 0.25f, k); }
                break;
            }
            case "no":
                if (head != null) head.rotation = Quaternion.AngleAxis(Mathf.Sin(t * 11f) * 24f * w * Mathf.Clamp01(1.6f - t), up) * head.rotation;
                break;
            case "sit":
            {
                // down to the ground, knees up, feet flat in front
                float ground = root.position.y;
                if (pelvis != null)
                {
                    float hip = pelvis.position.y - ground, H = head != null ? head.position.y - ground : hip * 2f;
                    pelvis.position += up * (ground + H * 0.1f - pelvis.position.y) * w;
                    if (hip <= 0f) break;
                }
                foreach (var leg in new[] { new[] { thL, caL, ftL }, new[] { thR, caR, ftR } })
                {
                    if (leg[0] == null) continue;
                    float s = Side(leg[0], right);
                    Aim(leg[0], leg[1], fwd + up * 0.55f + right * s * 0.3f, w);
                    Aim(leg[1], leg[2], -up + fwd * 0.3f + right * s * 0.05f, w);
                }
                // hands resting on the knees
                foreach (var arm in new[] { new[] { uaR, laR, hR }, new[] { uaL, laL, hL } })
                {
                    if (arm[0] == null) continue;
                    float s = Side(arm[0], right);
                    Aim(arm[0], arm[1], -up * 0.7f + fwd * 0.6f + right * s * 0.15f, w);
                    Aim(arm[1], arm[2], fwd + right * s * 0.05f - up * 0.1f, w);
                }
                break;
            }
        }
    }

    float Side(Transform a, Vector3 right)
    {
        if (a == null) return 1f;
        Vector3 c = pelvis != null ? pelvis.position : root.position;
        return Vector3.Dot(a.position - c, right) >= 0f ? 1f : -1f;
    }

    // turn bone a so that its child b lies along dir (blended by k)
    static void Aim(Transform a, Transform b, Vector3 dir, float k)
    {
        if (a == null || b == null || k <= 0f) return;
        Vector3 cur = b.position - a.position;
        if (cur.sqrMagnitude < 1e-8f || dir.sqrMagnitude < 1e-8f) return;
        Quaternion q = Quaternion.FromToRotation(cur, dir.normalized) * a.rotation;
        a.rotation = Quaternion.Slerp(a.rotation, q, Mathf.Clamp01(k));
    }
}
