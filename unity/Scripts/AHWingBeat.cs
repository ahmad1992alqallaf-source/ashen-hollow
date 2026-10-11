// Ashen Hollow: wings that beat. A winged monster rigged by Meshy has no wing bones; tools/animals/wingbones.py gives
// it two (WingL, WingR, at the shoulder blades) and this moves them on top of whatever the clips do: a slow rise and
// fall while it stands, a stronger, quicker beat while it moves or fights. The beat turns each wing about the
// monster's own forward axis, the two in mirror, so it works whatever axes the bones came in with.
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class AHWingBeat : MonoBehaviour
{
    Transform root; Transform[] wings; Quaternion[] rest; float[] side;
    Vector3 last; float beat, phase;
    public float amp = 8f, rate = 0.7f, lift = 3f;
    public static float Gain = 1f;   // tests: 0 holds the wings still

    public static void Attach(GameObject model, Transform root)
    {
        if (model == null) return;
        Transform l = null, r = null;
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) { if (t.name == "WingL") l = t; else if (t.name == "WingR") r = t; }
        if (l == null || r == null) return;
        var f = model.AddComponent<AHWingBeat>(); f.root = root; f.wings = new[] { l, r };
        f.rest = new[] { l.localRotation, r.localRotation }; f.side = new float[2];
        f.phase = Random.value * 6.28f; f.last = root.position;
    }

    void LateUpdate()
    {
        if (root == null || wings == null) return;
        float dt = Time.deltaTime; if (dt <= 0f) return;
        // moving (or lunging) beats harder and faster; standing settles back to a slow breath
        float speed = (root.position - last).magnitude / dt; last = root.position;
        beat = Mathf.MoveTowards(beat, speed > 0.3f ? 1f : 0f, dt * 2f);
        phase += dt * Mathf.Lerp(rate, rate * 2.2f, beat) * 6.2832f;
        float a = (lift + Mathf.Sin(phase) * Mathf.Lerp(amp, amp * 1.75f, beat)) * Gain;
        Vector3 fwd = root.forward, right = root.right;
        for (int i = 0; i < 2; i++)
        {
            var w = wings[i]; if (w == null) continue;
            w.localRotation = rest[i];
            if (side[i] == 0f) side[i] = Vector3.Dot(w.position - root.position, right) >= 0f ? 1f : -1f;
            w.rotation = Quaternion.AngleAxis(a * side[i], fwd) * w.rotation;   // +: the wing tip rises
        }
    }
}
