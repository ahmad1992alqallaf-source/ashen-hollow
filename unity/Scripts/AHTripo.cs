// Ashen Hollow: gear made with Tripo from our own concept art (Resources/AH/Models/Tripo, kept on the PC: model files
// are not pushed). Each model was shrunk to game size and given a glow map for its lava seams before it came in.
//  - Hold: a weapon or shield in a hand, sized in metres, held at its grip (or a shield by the middle of its back)
//  - Helm: a helmet on the VRoid head, in place of the outfit's own helm piece (which still hides the hair under it)
using UnityEngine;

public static class AHTripo
{
    const string Dir = "AH/Models/Tripo/";

    public static bool Has(string name) { return Resources.Load<GameObject>(Dir + name) != null; }

    // a prop in the hand: long axis +y, held 'grip' of the way up from its bottom end; a shield (shield=true) is held
    // by the middle of its back. 'len' is its longest side in metres.
    public static Transform Hold(Transform hand, string res, string name, Quaternion q, Vector3 p, float len, float grip, bool shield)
    {
        var pf = Resources.Load<GameObject>(Dir + res); if (pf == null || hand == null) return null;
        var root = new GameObject("Weapon_" + name).transform; root.SetParent(hand, false);
        root.localPosition = p; root.localRotation = q;
        Vector3 hs = hand.lossyScale; root.localScale = new Vector3(1f / Mathf.Max(1e-5f, hs.x), 1f / Mathf.Max(1e-5f, hs.y), 1f / Mathf.Max(1e-5f, hs.z));
        var m = Object.Instantiate(pf, root, false); m.name = "Model"; m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
        var b = Bounds(m.transform, root); float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (big > 1e-5f) m.transform.localScale *= len / big;
        b = Bounds(m.transform, root);
        Vector3 at = shield ? new Vector3(b.center.x, b.center.y, b.min.z) : new Vector3(b.center.x, b.min.y + b.size.y * grip, b.center.z);
        m.transform.localPosition -= at;
        Finish(m, hand.gameObject.layer);
        return root;
    }

    // the Warden's horned lava helm on the head stand of a worn outfit; the outfit's own helm piece is hidden
    // (it stays, so the hair under it stays hidden). Sized to the hero: the male head is the measure.
    public static GameObject Helm(GameObject worn, Transform rig, string res)
    {
        var pf = Resources.Load<GameObject>(Dir + res); if (pf == null || worn == null) return null;
        Transform head = null;
        foreach (var t in worn.GetComponentsInChildren<Transform>(true)) if (t.name == "stand_Head") { head = t; break; }
        if (head == null) return null;
        foreach (Transform inst in worn.transform)
            if (inst.name.EndsWith(":Head")) foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        var link = rig.GetComponent<AHVRoidLink>();
        Transform body = link != null && link.Body != null ? link.Body : rig;
        Vector3 fwd = body.forward; fwd.y = 0f; fwd.Normalize();
        // how big this hero's head is next to the male hero's (head joint 1.571 m above the feet)
        float k = 1f; var an = body.GetComponent<Animator>();
        if (an != null && an.isHuman) { var hb = an.GetBoneTransform(HumanBodyBones.Head); if (hb != null) k = Mathf.Clamp((hb.position.y - body.position.y) / (1.571f * Mathf.Max(0.01f, body.lossyScale.y)), 0.7f, 1.3f); }
        var root = new GameObject("TripoHelm").transform;
        root.position = head.position; root.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        var m = Object.Instantiate(pf, root, false); m.name = "Model"; m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
        var b = Bounds(m.transform, root);
        if (b.size.y > 1e-5f) m.transform.localScale *= HelmHeight * k / b.size.y;
        b = Bounds(m.transform, root);
        // the helm's middle over the head joint, its bottom edge (the gorget) a little under it
        m.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
        m.transform.localPosition += new Vector3(0f, HelmDrop * k, HelmFwd * k);
        root.SetParent(head, true);
        Finish(m, rig.gameObject.layer);
        return root.gameObject;
    }
    // the helm's size and seat on the head, in metres for the male hero (the female is scaled by her head height)
    public static float HelmHeight = 0.39f, HelmDrop = -0.07f, HelmFwd = 0.01f;

    static void Finish(GameObject m, int layer)
    {
        foreach (var t in m.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        foreach (var r in m.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.enabled = true; }
    }

    // the meshes' bounds in another transform's space
    static Bounds Bounds(Transform t, Transform space)
    {
        bool any = false; var b = new Bounds();
        foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue; var mb = mf.sharedMesh.bounds; var to = space.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = to.MultiplyPoint3x4(c); if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
        }
        foreach (var smr in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null) continue; var mb = smr.sharedMesh.bounds; var to = space.worldToLocalMatrix * smr.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = to.MultiplyPoint3x4(c); if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
        }
        return b;
    }
}
