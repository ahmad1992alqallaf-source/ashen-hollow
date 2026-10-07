// Ashen Hollow: every piece of gear gets its own picture. A little photo studio far above the land (layer 29) dresses
// a stand-in of your hero in just that one piece and photographs it close up, so a Kingsflame helm, a Wolf hood and a
// Bronze helm each show their own shape and colour in the bag instead of sharing one drawn glyph. Pictures are made a
// couple per frame, the first time an item is seen, and kept.
// Inspecting gear in the bag shows it big: your hero turning slowly on a stand, wearing the piece, in a glow of its
// quality colour (AHUI bag).
using System.Collections.Generic;
using UnityEngine;

public class AHItemStudio : MonoBehaviour
{
    public const int Layer = 29;
    static AHItemStudio inst;
    static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
    static readonly List<string> todo = new List<string>();
    static readonly HashSet<string> queued = new HashSet<string>();
    public static System.Action Changed;

    Camera cam; RenderTexture iconRt, bigRt; Texture2D read; Light key;
    Transform iconStage, viewStage; GameObject viewRig; string viewId; float spin;

    static AHItemStudio Get()
    {
        if (inst != null) return inst;
        var g = AHGame.I; if (g == null || g.player == null) return null;
        var go = new GameObject("ItemStudio"); inst = go.AddComponent<AHItemStudio>(); inst.Setup(); return inst;
    }

    void Setup()
    {
        transform.position = new Vector3(0f, 900f, 0f);
        iconStage = new GameObject("IconStage").transform; iconStage.SetParent(transform, false);
        viewStage = new GameObject("ViewStage").transform; viewStage.SetParent(transform, false); viewStage.localPosition = new Vector3(30f, 0f, 0f);
        var cg = new GameObject("StudioCam"); cg.transform.SetParent(transform, false);
        cam = cg.AddComponent<Camera>(); cam.enabled = false; cam.cullingMask = 1 << Layer; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f); cam.fieldOfView = 26f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 40f;
        iconRt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32); iconRt.antiAliasing = 2;
        bigRt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); bigRt.antiAliasing = 2;
        read = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        // a warm key light from the front left (the sun lights the studio too)
        var lg = new GameObject("Key"); lg.transform.SetParent(transform, false); lg.transform.rotation = Quaternion.Euler(30f, -35f, 0f);
        key = lg.AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.25f; key.color = new Color(1f, 0.92f, 0.82f); key.cullingMask = 1 << Layer; key.shadows = LightShadows.None;
    }

    void Awake()
    {
        // a soft fill from the front right, so the dark cloth reads
        var fg = new GameObject("Fill"); fg.transform.SetParent(transform, false); fg.transform.rotation = Quaternion.Euler(15f, 140f, 0f);
        var f = fg.AddComponent<Light>(); f.type = LightType.Directional; f.intensity = 0.6f; f.color = new Color(0.8f, 0.88f, 1f); f.cullingMask = 1 << Layer; f.shadows = LightShadows.None;
    }

    static bool Pictured(ItemDef d) { return d != null && d.IsGear && d.slot != "ring" && d.slot != "amulet"; }

    // the picture for an item, or null while it is still being made (the bag shows the glyph meanwhile)
    public static Sprite Icon(ItemDef d)
    {
        if (!Pictured(d)) return null;
        Sprite s; if (icons.TryGetValue(d.id, out s)) return s;
        if (queued.Add(d.id)) todo.Add(d.id);
        Get(); return null;
    }

    // the big turning view of a piece (shown while it is selected in the bag)
    public static Texture View(ItemDef d)
    {
        var st = Get(); if (st == null || !Pictured(d)) { if (st != null) st.ClearView(); return null; }
        if (st.viewId != d.id) st.BuildView(d);
        return st.bigRt;
    }
    public static void StopView() { if (inst != null) inst.ClearView(); }
    void ClearView() { if (viewRig != null) Destroy(viewRig); viewRig = null; viewId = null; }

    GameObject Dress(Transform stage, ItemDef d, bool iconOnly)
    {
        var g = AHGame.I; var p = g.player;
        var holder = new GameObject("Stand_" + d.id).transform; holder.SetParent(stage, false); holder.localRotation = Quaternion.identity;
        AHAnim a; var rig = AHPeople.BuildHero(holder, p.cls, p.look ?? new AHLook(), g, out a);
        if (rig == null) { Destroy(holder.gameObject); return null; }
        if (d.slot != "weapon") foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) if (r.transform.name.StartsWith("Weapon_") || (r.transform.parent != null && r.transform.parent.name.StartsWith("Weapon_"))) r.enabled = false;
        AHWardrobe.DressWith(rig, holder, s => s == d.slot ? d.id : null, () => false);
        if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
        if (iconOnly) Isolate(holder.gameObject, d);
        SetLayer(holder, Layer);
        return holder.gameObject;
    }
    // for the bag picture only the piece itself is shown: no body (unless the piece is cloth on the body, like a vest
    // or leggings), and only the right one of a pair (a glove, a boot, a knife)
    static void Isolate(GameObject root, ItemDef d)
    {
        bool any = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            bool mine = false, left = false; var t = r.transform;
            while (t != null && t != root.transform)
            {
                if (t.name.StartsWith("Outfit_") || (d.slot == "weapon" && t.name.StartsWith("Weapon_"))) mine = true;
                if (t.name.EndsWith("_l") || t.name.Contains("Offhand")) left = true;
                t = t.parent;
            }
            if (mine && left && (d.slot == "hands" || d.slot == "feet" || d.slot == "weapon" || d.slot == "shoulders")) mine = false;
            r.enabled = mine; any |= mine;
        }
        if (any) return;
        // nothing drawn of its own: show the clothes it dyes on the body
        string[] keep = d.slot == "legs" ? new[] { "Legs" } : d.slot == "chest" ? new[] { "Body" } : d.slot == "hands" ? new[] { "Arms" } : new[] { "Body" };
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { bool k = false; foreach (var w in keep) if (r.name.Contains(w)) k = true; r.enabled = k; }
    }

    static void SetLayer(Transform t, int l) { t.gameObject.layer = l; foreach (Transform c in t) SetLayer(c, l); }

    // where to look: the piece's own parts (or the weapon in the hand)
    static bool Focus(GameObject root, ItemDef d, out Bounds b)
    {
        b = new Bounds(); bool any = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled) continue;
            bool mine = false; var t = r.transform;
            while (t != null && t != root.transform) { if (t.name.StartsWith("Outfit_") || (d.slot == "weapon" && t.name.StartsWith("Weapon_"))) { mine = true; break; } t = t.parent; }
            if (!mine && root.transform.parent == inst.iconStage) mine = true;   // icons: whatever is still shown
            if (!mine) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return any;
    }
    void Aim(GameObject root, ItemDef d, float yaw, float pad)
    {
        Bounds b;
        if (!Focus(root, d, out b))
        {
            // pieces drawn as cloth on the body itself (a vest, leggings): look at that part of the body
            string bn = d.slot == "head" ? "Head" : d.slot == "shoulders" || d.slot == "cape" ? "spine_03" : d.slot == "chest" ? "spine_02" : d.slot == "hands" || d.slot == "weapon" ? "hand_r" : d.slot == "legs" ? "calf_l" : d.slot == "feet" ? "foot_l" : "spine_02";
            Transform bt = null; foreach (var t in root.GetComponentsInChildren<Transform>()) if (t.name == bn) { bt = t; break; }
            b = new Bounds(bt != null ? bt.position : root.transform.position + Vector3.up, Vector3.one * (d.slot == "chest" || d.slot == "legs" ? 0.6f : 0.4f));
        }
        float r = Mathf.Max(0.1f, Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z))) * pad;   // fill the frame
        bool back = d.slot == "cape";
        Vector3 dir = Quaternion.Euler(0f, (back ? 180f : 0f) + yaw, 0f) * root.transform.forward; dir.y = 0f; dir.Normalize();
        float dist = r / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        cam.transform.position = b.center + dir * dist + Vector3.up * dist * 0.18f;
        cam.transform.LookAt(b.center);
    }

    int busyFrames;
    void LateUpdate()
    {
        RenderHero();
        // the turning view
        if (viewRig != null)
        {
            spin += Time.unscaledDeltaTime * 30f;
            cam.targetTexture = bigRt; Aim(viewRig, AHItems.Get(viewId), spin, 1.6f); cam.Render();
        }
        // one new icon every other frame
        if (todo.Count == 0) return;
        if (++busyFrames % 2 != 0) return;
        if (pending == null)
        {
            var d = AHItems.Get(todo[0]); todo.RemoveAt(0);
            if (d == null) return;
            pending = Dress(iconStage, d, true); pendingId = d.id; return;   // pose settles on the next frame
        }
        var dd = AHItems.Get(pendingId);
        cam.targetTexture = iconRt; Aim(pending, dd, 28f, 1.12f); cam.Render();
        var was = RenderTexture.active; RenderTexture.active = iconRt;
        var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); tex.Apply(); RenderTexture.active = was;
        icons[pendingId] = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
#if UNITY_EDITOR
        try { System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../HeroShots/icon_" + pendingId + ".png"), tex.EncodeToPNG()); } catch { }
#endif
        Destroy(pending); pending = null; pendingId = null;
        if (Changed != null) Changed();
    }
    GameObject pending; string pendingId;

    void BuildView(ItemDef d) { ClearView(); viewRig = Dress(viewStage, d, false); viewId = d.id; spin = 0f; }

    // ---- your whole hero, standing in all its gear (the Stats window) ----
    RenderTexture heroRt; GameObject heroRig; string heroSig; Transform heroStage; float heroT;
    public static Texture Hero(AHPlayer p)
    {
        if (p == null) return null;
        return HeroOf(p.cls, p.look, s => AHWardrobe.Shown(p, s));
    }
    // any hero, from its class, look and what each slot shows (the hero select screen shows heroes not being played)
    public static Texture HeroOf(ClassDef cls, AHLook look, System.Func<string, string> shown)
    {
        var st = Get(); if (st == null || cls == null) return null;
        look = look ?? new AHLook();
        string sig = cls.id + "|" + JsonUtility.ToJson(look);
        foreach (var sl in AHItems.GearSlots) sig += "|" + (shown(sl) ?? "");
        if (st.heroRig == null || st.heroSig != sig) st.BuildHero(cls, look, shown, sig);
        return st.heroRt;
    }
    public static void StopHero() { if (inst != null && inst.heroRig != null) { Destroy(inst.heroRig); inst.heroRig = null; inst.heroSig = null; } }
    void BuildHero(ClassDef cls, AHLook look, System.Func<string, string> shown, string sig)
    {
        if (heroRig != null) Destroy(heroRig);
        if (heroRt == null) { heroRt = new RenderTexture(320, 448, 24, RenderTextureFormat.ARGB32); heroRt.antiAliasing = 4; }
        if (heroStage == null) { heroStage = new GameObject("HeroStage").transform; heroStage.SetParent(transform, false); heroStage.localPosition = new Vector3(60f, 0f, 0f); }
        var holder = new GameObject("Hero").transform; holder.SetParent(heroStage, false);
        AHAnim a; var rig = AHPeople.BuildHero(holder, cls, look, AHGame.I, out a);
        if (rig == null) { Destroy(holder.gameObject); return; }
        AHWardrobe.DressWith(rig, holder, shown, () => false);
        if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
        SetLayer(holder, Layer);
        heroRig = holder.gameObject; heroSig = sig; heroT = 0f;
    }
    void RenderHero()
    {
        if (heroRig == null) return;
        heroT += Time.unscaledDeltaTime;
        // the VRoid body and gear moved onto it can join the stand after building: keep the whole stand on the studio layer
        if (Mathf.Repeat(heroT, 1f) < Time.unscaledDeltaTime * 1.5f) SetLayer(heroRig.transform, Layer);
        heroRig.transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(heroT * 0.5f) * 28f, 0f);   // faces you, turning a little to each side
        var b = new Bounds(heroRig.transform.position + Vector3.up * 0.9f, Vector3.one * 0.5f); bool any = false;
        // framed on the body (wings, capes and big hats may run off the edge)
        foreach (var r in heroRig.GetComponentsInChildren<SkinnedMeshRenderer>(false))
        {
            if (!r.enabled || r.forceRenderingOff) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) foreach (var r in heroRig.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        float h = Mathf.Clamp(b.size.y, 1.2f, 3.2f) * 1.12f;
        Vector3 c = new Vector3(heroRig.transform.position.x, b.min.y + h * 0.48f, heroRig.transform.position.z);
        float dist = (h * 0.5f) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        cam.targetTexture = heroRt; cam.transform.position = c + heroStage.forward * dist + Vector3.up * dist * 0.08f; cam.transform.LookAt(c); cam.Render();
    }
}

// keeps a studio stand-in's pose (its animation graph is held on a frame of Idle)
public class AHStudioPose : MonoBehaviour
{
    public AHAnim anim;
    void Update() { if (anim != null) anim.Tick(Time.unscaledDeltaTime); }
    void OnDestroy() { if (anim != null) anim.Dispose(); }
}
