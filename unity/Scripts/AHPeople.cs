// Ashen Hollow: the hero built from Quaternius parts (web qAssemble), following the character creator's look:
// an outfit body by sex and class, a head, hair, beard and brows bound to the body's skeleton, skin, hair and
// outfit colours (web LOOK), height and build, the class weapon taken from the KayKit models and held in the
// right hand, and the shared animations from qAnims (KayKit animation names are mapped to them).
using System.Collections.Generic;
using UnityEngine;

public static class AHPeople
{
    const string Dir = "AH/Models/Web/";
    static bool loggedClips;

    // KayKit clip names the game asks for -> the Quaternius clip
    public static readonly Dictionary<string, string> HeroAlias = new Dictionary<string, string>
    {
        { "Idle", "Idle_Loop" }, { "Running_A", "Jog_Fwd_Loop" }, { "Walking_A", "Walk_Loop" },
        { "1H_Melee_Attack", "Sword_Attack" }, { "Shoot", "Spell_Simple_Shoot" }, { "Cheer", "Spell_Simple_Shoot" },
        { "Hit_A", "Hit_Chest" }, { "Death_A", "Death01" }, { "Chop", "TreeChopping_Loop" }, { "Harvest", "Farm_Harvest" },
        { "Levelup", "Yes" }, { "Ride", "Sitting_Idle_Loop" },
    };

    static Color LookColor(string list, int i, int field = 0)
    {
        var l = AHJson.A(AHJson.O(AHDB.Rules, "LOOK"), list);
        if (l == null || l.Count == 0) return Color.white;
        var row = l[Mathf.Clamp(i, 0, l.Count - 1)] as List<object>;
        return row != null && row.Count > field ? AHGame.Hex((int)(double)row[field]) : Color.white;
    }

    static string HairPart(string h)
    {
        switch (h)
        {
            case "bald": return null;
            case "shaved": return "qHair_Buzzed";
            case "bun": return "qHair_Buns";
            case "short": case "bob": case "spiky": case "mohawk": return "qHair_SimpleParted";
        }
        return "qHair_Long";   // pony, long, braid, twin, mane
    }

    // the class weapon: KayKit model and the node names to borrow (main hand, off hand)
    static void WeaponFor(string cls, out string model, out string main, out string off)
    {
        switch (cls)
        {
            case "warrior": model = "Knight_t"; main = "1H_Sword"; off = "Round_Shield"; return;
            case "mage": model = "Mage_t"; main = "2H_Staff"; off = null; return;
            case "priest": model = "Mage_t"; main = "1H_Wand"; off = "Spellbook"; return;
            case "rogue": model = "Rogue_t"; main = "Knife"; off = "Knife_Offhand"; return;
            case "ranger": model = "Rogue_t"; main = "2H_Crossbow"; off = null; return;
            case "druid": model = "Mage_t"; main = "2H_Staff"; off = null; return;
        }
        model = null; main = null; off = null;
    }

    public static GameObject BuildHero(Transform holder, ClassDef cls, AHLook look, AHGame g, out AHAnim anim)
    {
        anim = null;
        bool fem = look != null && look.sex == "f";
        string body = fem ? "qFemalePeasant" : (cls.id == "warrior" || cls.id == "rogue" || cls.id == "ranger") ? "qMaleRanger" : "qMalePeasant";
        var parts = new List<string> { fem ? "qHead_Female" : "qHead_Male" };
        string hp = HairPart(look.hair); if (hp != null) parts.Add(hp);
        if (!fem && look.beard != null && look.beard != "none") parts.Add("qHair_Beard");
        if (look.brow != "none") parts.Add(fem ? "qEyebrows_Female" : "qEyebrows_Regular");

        var rig = Assemble(holder, body, parts, g, out anim);
        if (rig == null) return null;
        if (anim != null) anim.alias = HeroAlias;

        // hide the ranger hood unless you are a rogue (and always with a hairstyle that would poke through)
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Hood") && cls.id != "rogue") r.enabled = false;

        // colours: skin, hair, outfit
        Color skin = LookColor("SKIN", look.skin), hair = LookColor("HAIR", look.hairCol);
        Color clothMain = LookColor("CLOTH", look.cloth, 0), clothHi = LookColor("CLOTH", look.cloth, 1);
        Color skinRef = LookColor("SKIN", 1);
        Color skinK = new Color(skin.r / Mathf.Max(0.05f, skinRef.r), skin.g / Mathf.Max(0.05f, skinRef.g), skin.b / Mathf.Max(0.05f, skinRef.b));
        Color clothK = Color.Lerp(Color.white, Color.Lerp(clothMain, clothHi, 0.6f) * 2.4f, 0.5f) * Color.Lerp(Color.white, cls.tint, 0.5f);
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials; bool ch = false;
            foreach (var m in mats)
            {
                if (m == null) continue;
                Color k = Color.white; bool set = false;
                if (m.name.Contains("Hair")) { k = hair.linear * 1.6f; set = true; }
                else if (m.name.Contains("Superhero") || m.name.Contains("Regular_Male")) k = skinK.linear;
                else if (m.name.Contains("Peasant") || m.name.Contains("Ranger")) k = clothK.linear;
                else continue;
                Color baseC = m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : Color.white;
                Color c = set ? new Color(k.r, k.g, k.b, baseC.a) : new Color(baseC.r * k.r, baseC.g * k.g, baseC.b * k.b, baseC.a);
                if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", c);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                ch = true;
            }
            if (ch) r.materials = mats;
        }

        // the weapon from the KayKit set, held in the right hand (and an off-hand item in the left)
        string wm, main, off; WeaponFor(cls.id, out wm, out main, out off);
        if (wm != null) Arm(rig, wm, main, off, g);
        return rig;
    }

    public static void Arm(GameObject rig, string wm, string main, string off, AHGame g)
    {
        var pf = Resources.Load<GameObject>("AH/Models/" + wm);
        if (pf == null) return;
        var tmp = Object.Instantiate(pf);
        // size the KayKit hero to 1.8 m like ours, so the weapon comes out the right size
        var rs = tmp.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y > 1e-3f) tmp.transform.localScale = Vector3.one * (1.8f / b.size.y);
        }
        Transform hr = Find(rig.transform, "hand_r"), hl = Find(rig.transform, "hand_l");
        Quaternion q; Vector3 p;
        Grip(main, true, out q, out p); Grab(tmp.transform, main, hr, q, p);
        if (off != null) { Grip(off, false, out q, out p); Grab(tmp.transform, off, hl, q, p); }
        Object.Destroy(tmp);
    }

    // how each kind of weapon sits in the hand, worked out from the Quaternius hand bones (as glTFast imports them:
    // fingers along +y, the thumb and index side along +z, the palm toward +x on the right hand and -x on the left):
    // blades, axes, maces and wands are held in a fist and leave it on the thumb side, edge toward the knuckles;
    // a staff stands up from the fist, leaning a little forward; a crossbow is held by its grip like a pistol;
    // a shield is strapped to the back of the forearm with its face outward. KayKit weapons have their grip at the
    // origin and the blade or haft along +y (a crossbow points along +z, a shield faces +z).
    public static void Grip(string name, bool right, out Quaternion q, out Vector3 p)
    {
        float palm = right ? 0.035f : -0.035f;
        if (name.Contains("Shield")) { q = new Quaternion(0.7071068f, 0f, 0.7071068f, 0f); p = new Vector3(right ? -0.09f : 0.09f, 0.05f, 0f); return; }
        if (name.Contains("Crossbow")) { q = new Quaternion(0f, -0.7071068f, -0.7071068f, 0f); p = new Vector3(palm, 0.085f, 0f); return; }
        if (name.Contains("Staff")) { q = new Quaternion(0.976296f, 0f, 0f, 0.2164396f); p = new Vector3(palm, 0.085f, 0f); return; }
        q = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f); p = new Vector3(palm, 0.085f, 0f);
    }

    static void Grab(Transform src, string name, Transform hand, Quaternion rot, Vector3 offset)
    {
        if (hand == null || name == null) return;
        var w = Find(src, name);
        if (w == null) return;
        Vector3 ws = w.lossyScale;
        w.SetParent(hand, true);
        w.localPosition = offset;
        w.localRotation = rot;
        // keep its size in the world
        Vector3 hs = hand.lossyScale;
        w.localScale = new Vector3(ws.x / Mathf.Max(1e-5f, hs.x), ws.y / Mathf.Max(1e-5f, hs.y), ws.z / Mathf.Max(1e-5f, hs.z));
        foreach (var r in w.GetComponentsInChildren<Renderer>(true)) { r.enabled = true; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
        w.gameObject.SetActive(true);
        w.name = "Weapon_" + name;
    }

    public static Transform Find(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++) { var r = Find(t.GetChild(i), name); if (r != null) return r; }
        return null;
    }

    // web qAssemble: an outfit plus parts, all on the outfit's skeleton; sized to 1.8 m, feet on the ground
    public static GameObject Assemble(Transform holder, string body, List<string> parts, AHGame game, out AHAnim anim)
    {
        anim = null;
        var bodyPrefab = Resources.Load<GameObject>(Dir + body);
        if (bodyPrefab == null) return null;
        var rig = new GameObject("Rig");
        rig.transform.SetParent(holder, false);
        rig.transform.localRotation = Quaternion.Euler(0, game.ModelYaw + game.heroYawFix, 0);
        var inst = Object.Instantiate(bodyPrefab, rig.transform, false);
        inst.name = "Armature";
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        var bones = new Dictionary<string, Transform>();
        foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(t.name)) bones[t.name] = t;
        Transform host = null;
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { host = smr.transform.parent; break; }
        if (host == null) host = inst.transform;
        foreach (var part in parts)
        {
            var pf = Resources.Load<GameObject>(Dir + part);
            if (pf == null) continue;
            var pi = Object.Instantiate(pf);
            foreach (var smr in pi.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var nb = smr.bones;
                for (int i = 0; i < nb.Length; i++) { Transform t; if (nb[i] != null && bones.TryGetValue(nb[i].name, out t)) nb[i] = t; }
                Transform rb = null;
                if (smr.rootBone != null) bones.TryGetValue(smr.rootBone.name, out rb);
                Vector3 lp = smr.transform.localPosition; Quaternion lr = smr.transform.localRotation; Vector3 ls = smr.transform.localScale;
                smr.transform.SetParent(host, false);
                smr.transform.localPosition = lp; smr.transform.localRotation = lr; smr.transform.localScale = ls;
                smr.bones = nb;
                if (rb != null) smr.rootBone = rb;
            }
            Object.Destroy(pi);
        }
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
        var rs = new List<Renderer>();
        foreach (var r in inst.GetComponentsInChildren<Renderer>(false)) if (r.enabled && !r.name.Contains("Hood")) rs.Add(r);
        if (rs.Count > 0)
        {
            Bounds b = rs[0].bounds; for (int i = 1; i < rs.Count; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y > 1e-3f) rig.transform.localScale = Vector3.one * (1.8f / b.size.y);
            b = rs[0].bounds; for (int i = 1; i < rs.Count; i++) b.Encapsulate(rs[i].bounds);
            rig.transform.position += Vector3.up * (holder.position.y - b.min.y);
        }
        // qAnims2: the combat set (sword cuts, spin, slam, roll, crossbow shot, throw, roar, kneel) from Quaternius UAL 1 and 2
        var clipList = new List<AnimationClip>(Resources.LoadAll<AnimationClip>(Dir + "qAnims"));
        clipList.AddRange(Resources.LoadAll<AnimationClip>(Dir + "qAnims2"));
        if (!loggedClips) { loggedClips = true; Debug.Log("Ashen Hollow: hero animations " + clipList.Count + " (combat set " + Resources.LoadAll<AnimationClip>(Dir + "qAnims2").Length + ")"); }
        anim = new AHAnim(rig, clipList.ToArray());
        return rig;
    }
}
