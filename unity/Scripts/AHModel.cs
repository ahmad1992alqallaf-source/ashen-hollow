// Ashen Hollow: loads a model from Resources/AH/Models, sizes it and stands it on the ground
using UnityEngine;

public static class AHModel
{
    // holder: the object that moves and turns. The model sits inside it, scaled to size metres
    // (its height, or its length for four-legged beasts), facing the holder's forward.
    public static GameObject Spawn(Transform holder, string file, float size, bool byLength, float yawFix, out AHAnim anim, System.Collections.Generic.List<object> split = null)
    {
        anim = null;
        string path = "AH/Models/" + file;
        GameObject prefab = Resources.Load<GameObject>(path);
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

        // measure, scale, and put the feet on the ground
        Bounds b = Measure(inst);
        float have = byLength ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
        if (size > 0f && have > 1e-3f) inst.transform.localScale = Vector3.one * (size / have);
        b = Measure(inst);
        inst.transform.position += Vector3.up * (holder.position.y - b.min.y);
        LastHeight = b.size.y;

        anim = new AHAnim(inst, Resources.LoadAll<AnimationClip>(path), split);
        return inst;
    }

    public static float LastHeight;   // how tall the last model came out (metres)

    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
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
