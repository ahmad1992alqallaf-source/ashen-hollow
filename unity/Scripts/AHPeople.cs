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
        { "Levelup", "Yes" }, { "Ride", "Sitting_Idle_Loop" }, { "Fish", "Idle_Lantern_Loop" },
    };

    public static Color SkinColor(AHLook look) { return LookColor("SKIN", look != null ? look.skin : 1); }
    public static Color OutfitColor(AHLook look) { return look == null ? Color.white : LookColor("CLOTH", look.cloth, 0); }
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
            case "shaman": model = "Barbarian_t"; main = "1H_Axe"; off = null; return;
        }
        model = null; main = null; off = null;
    }

    public static GameObject BuildHero(Transform holder, ClassDef cls, AHLook look, AHGame g, out AHAnim anim)
    {
        anim = null;
        bool fem = look != null && look.sex == "f";
        string body = fem ? "qFemalePeasant" : (cls.id == "warrior" || cls.id == "rogue" || cls.id == "ranger" || cls.id == "shaman") ? "qMaleRanger" : "qMalePeasant";
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
        // the outfit colour as a dye over the outfit's own painted texture: its hue, but never so dark that the
        // stitching, leather and folds of the clothes vanish into black
        float dh, ds, dv; Color.RGBToHSV(Color.Lerp(clothMain, clothHi, 0.6f), out dh, out ds, out dv);
        Color dyeC = Color.HSVToRGB(dh, Mathf.Min(ds * 1.1f, 0.7f), Mathf.Clamp(dv * 2.4f, 0.62f, 0.95f));
        Color clothK = Color.Lerp(Color.white, dyeC, 0.7f) * Color.Lerp(Color.white, cls.tint, 0.2f);
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
        // a VRoid body, if one has been made for this hero (AHVRoid)
        var vm = AHVRoid.ModelFor(cls, look);
        if (vm != null && AHVRoid.Link(rig, holder, body, vm))
        {
            // the look editor's colours dye the VRoid body too: hair, skin and the outfit
            var vr = AHVRoid.Last;
            if (vr != null) AHVRoid.Dye(vr, hair, skin, Color.Lerp(clothMain, clothHi, 0.4f));
            // bald and shaved heads: the short build without its hair
            if (vr != null && (look.hair == "bald" || look.hair == "shaved")) foreach (var r in vr.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Hair")) r.enabled = false;
            // the face (eyes, brows, expression, eye colour, ears) and the topknot, mohawk and spikes
            if (vr != null) AHVRoid.Style(vr, look, hair, skin, look.eye >= 0 ? LookColor("EYE", look.eye) : Color.white, look.eye >= 0);
            if (vr != null && cls != null) AHVRoid.ClassSash(vr, cls.color);   // each class wears its own colour
        }
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
        q = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f); p = new Vector3(palm, 0.085f, 0f);   // through the fist, out of the thumb side
        if (name.Contains("Shield")) { q = new Quaternion(0.7071068f, 0f, 0.7071068f, 0f); p = new Vector3(right ? -0.13f : 0.13f, 0.05f, 0f); return; }
        if (name.Contains("Crossbow")) { q = new Quaternion(0f, -0.7071068f, -0.7071068f, 0f); return; }
        if (name.Contains("Staff")) { q = Quaternion.AngleAxis(-12f, Vector3.right) * q; return; }   // upright from the fist, tipped a little forward
        if (name.Contains("Spellbook")) { p = new Vector3(right ? 0.07f : -0.07f, 0.09f, 0f); return; }   // held at its edge against the palm
    }

    // how long each kind of weapon is in the hand, in metres (the KayKit sets come in different sizes)
    static float TargetLen(string n)
    {
        if (n.Contains("Staff")) return 1.65f;
        if (n.Contains("2H_Sword")) return 1.3f;
        if (n.Contains("Crossbow")) return 0.8f;
        if (n.Contains("Shield")) return 0.62f;
        if (n.Contains("Spellbook")) return 0.32f;
        if (n.Contains("Knife") || n.Contains("Wand")) return 0.45f;
        if (n.Contains("1H_Axe")) return 0.68f;   // the shaman's hand axe: the KayKit head is big
        if (n.Contains("Sword") || n.Contains("Axe") || n.Contains("Mace")) return 0.95f;
        return 0f;
    }

    // a work tool in the right hand: the woodcutter's axe (the KayKit hand axe), a pickaxe or a fishing rod
    static Material toolWood, toolIron, toolLine;
    public static GameObject Tool(GameObject rig, string kind)
    {
        if (rig == null || kind == null) return null;
        Transform hr = Find(rig.transform, "hand_r"); if (hr == null) return null;
        Quaternion q; Vector3 p; Grip("1H_Axe", true, out q, out p);
        if (kind == "axe")
        {
            var pf = Resources.Load<GameObject>("AH/Models/Barbarian_t"); if (pf == null) return null;
            var tmp = Object.Instantiate(pf);
            Grab(tmp.transform, "1H_Axe", hr, q, p);
            Object.Destroy(tmp);
            var w = hr.Find("Weapon_1H_Axe"); if (w == null) return null;
            w.name = "Tool_axe"; w.localScale *= 1.15f; return w.gameObject;
        }
        if (toolWood == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            toolWood = new Material(sh); toolWood.color = new Color(0.45f, 0.3f, 0.17f); toolWood.SetFloat("_Smoothness", 0.25f);
            toolIron = new Material(sh); toolIron.color = new Color(0.55f, 0.57f, 0.6f); toolIron.SetFloat("_Metallic", 0.8f); toolIron.SetFloat("_Smoothness", 0.55f);
            toolLine = new Material(sh); toolLine.color = new Color(0.9f, 0.9f, 0.85f);
        }
        var root = new GameObject("Tool_" + kind).transform; root.SetParent(hr, false);
        root.localPosition = p; root.localRotation = q;
        // sized in metres in the world, whatever the hand bone's scale
        Vector3 hs = hr.lossyScale; root.localScale = new Vector3(1f / Mathf.Max(1e-5f, hs.x), 1f / Mathf.Max(1e-5f, hs.y), 1f / Mathf.Max(1e-5f, hs.z));
        System.Action<PrimitiveType, Vector3, Vector3, Vector3, Material> P = (t, pos, sc, rot, m) =>
        {
            var go = GameObject.CreatePrimitive(t); Object.Destroy(go.GetComponent<Collider>());
            go.name = "Piece"; go.transform.SetParent(root, false); go.transform.localPosition = pos; go.transform.localScale = sc; go.transform.localEulerAngles = rot;
            go.GetComponent<Renderer>().sharedMaterial = m;
        };
        if (kind == "pick")
        {
            P(PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(0.035f, 0.36f, 0.035f), Vector3.zero, toolWood);   // the haft, held near its end
            foreach (int sx in new[] { -1, 1 })
            {
                P(PrimitiveType.Cube, new Vector3(sx * 0.11f, 0.53f, 0), new Vector3(0.22f, 0.05f, 0.05f), new Vector3(0, 0, -sx * 12f), toolIron);
                P(PrimitiveType.Cube, new Vector3(sx * 0.235f, 0.495f, 0), new Vector3(0.07f, 0.03f, 0.03f), new Vector3(0, 0, -sx * 24f), toolIron);   // the points
            }
            P(PrimitiveType.Cube, new Vector3(0, 0.54f, 0), new Vector3(0.07f, 0.08f, 0.07f), Vector3.zero, toolIron);
        }
        else if (kind == "rod")
        {
            P(PrimitiveType.Cylinder, new Vector3(0, 0.7f, 0), new Vector3(0.022f, 0.8f, 0.022f), Vector3.zero, toolWood);
            P(PrimitiveType.Cylinder, new Vector3(0, 1.55f, 0), new Vector3(0.012f, 0.1f, 0.012f), Vector3.zero, toolLine);
            P(PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0.035f), new Vector3(0.05f, 0.012f, 0.05f), new Vector3(90, 0, 0), toolIron);   // the reel
        }
        return root.gameObject;
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
        // every weapon its own true size, whatever hat the KayKit figure it came from wore
        float L = TargetLen(name); var mf = w.GetComponentInChildren<MeshFilter>();
        if (L > 0f && mf != null && mf.sharedMesh != null)
        {
            var mb = mf.sharedMesh.bounds; float len = Mathf.Max(mb.size.x, Mathf.Max(mb.size.y, mb.size.z)) * mf.transform.lossyScale.y;
            if (len > 1e-4f) w.localScale *= L / len;
            // the staff and the book have their origin in the middle: hold the staff low, the book by its edge
            if (name.Contains("Staff")) w.localPosition += w.localRotation * Vector3.up * (mb.extents.y * 0.45f * w.localScale.y);
            if (name.Contains("Spellbook")) w.localPosition += w.localRotation * Vector3.up * (mb.extents.y * 0.8f * w.localScale.y);
        }
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
