// Ashen Hollow: costumes (fashion). A costume is a whole outfit made for the hero's own VRoid body, so it fits exactly and
// moves with every pose, like a costume in a big mobile MMO. It changes only how you look: your stats always come from
// your equipped gear. While a costume is on, the gear pieces are not drawn (your weapon still is). Choose "Show gear"
// in the Wardrobe to see your gear again. A hero showing chest armour wears plain close-fitting clothes under it, so
// the armour sits on the body instead of over the starting robe.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHCostumes
{
    public class Def { public string id, name, blurb; public long price; }   // price in copper (0: every hero has it)
    public static readonly Def[] All =
    {
        new Def { id = "shadow",  name = "Ashen Shade",    price = 0,     blurb = "A hooded black long coat over strapped leathers and tall boots." },
        new Def { id = "ember",   name = "Emberfist Garb", price = 8000,  blurb = "A fighting jacket in ember red, frog-buttoned, for those who hit first." },
        new Def { id = "sage",    name = "Skyveil Sage",   price = 12000, blurb = "White and sky-blue robes trimmed in gold, worn by the Hollow's seers." },
        new Def { id = "crimson", name = "Crimson Count",  price = 20000, blurb = "A noble's black coat with a blood-red cape. Old money, older grudges." },
        new Def { id = "duelist", name = "Rose Duelist",   price = 25000, blurb = "A red-and-gold officer's coat, cut for the duelling ring." },
    };
    public const string Under = "under";   // the plain clothes under armour (not a costume you choose)

    public static Def Get(string id) { foreach (var d in All) if (d.id == id) return d; return null; }
    public static bool Owns(AHPlayer p, string id) { var d = Get(id); return d != null && (d.price == 0 || (p != null && p.prog != null && p.prog.costumes.Contains(id))); }
    // the costume shown, or null for "Show gear"
    public static string Worn(AHPlayer p)
    {
        if (p == null || p.prog == null || string.IsNullOrEmpty(p.prog.costume)) return null;
        return Owns(p, p.prog.costume) && Exists(p.look, p.prog.costume) ? p.prog.costume : null;
    }
    public static bool Exists(AHLook look, string id) { return Model(look, id) != null; }
    static GameObject Model(AHLook look, string id)
    {
        bool fem = look != null && look.sex == "f";
        return Resources.Load<GameObject>("AH/VRoid/costume_" + (fem ? "f_" : "m_") + id);
    }

    public static string Buy(AHPlayer p, string id)
    {
        var d = Get(id); if (d == null || p == null) return null;
        if (Owns(p, id)) return "You already have " + d.name + ".";
        if (d.price * AHDB.CU > p.bag.money) return "You need " + AHItems.MoneyText(d.price * AHDB.CU) + " for " + d.name + ".";
        p.bag.money -= d.price * AHDB.CU; p.bag.Touch();
        p.prog.costumes.Add(id); p.prog.costume = id;
        return d.name + " is yours, and you are wearing it.";
    }

    // put a costume (or the under-clothes) on a hero rig built by AHPeople.BuildHero; false if it has no VRoid body
    // or the costume has not been made for that body yet
    public static bool Apply(GameObject rig, string id, AHLook look)
    {
        if (rig == null || id == null) return false;
        var link = rig.GetComponent<AHVRoidLink>(); if (link == null || link.Body == null) return false;
        var pf = Model(look, id); if (pf == null) return false;
        var v = link.Body.gameObject;
        foreach (var s in v.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.name == "CostumeBody" || s.name == "UnderBody") return false;   // already on
        var smr = AHVRoid.WearBody(v, pf, id == Under ? "UnderBody" : "CostumeBody");
        if (smr == null) return false;
        // the class sash belongs to the starting robe
        foreach (var t in v.GetComponentsInChildren<Transform>(true)) if (t.name == "ClassSash" || t.name == "ClassSashRibbon") UnityEngine.Object.Destroy(t.gameObject);
        // the skin takes the hero's tone, as on the rest of the body
        var skin = AHPeople.SkinColor(look);
        var mats = smr.materials; bool ch = false;
        foreach (var m in mats)
        {
            if (m == null || !m.name.ToUpperInvariant().Contains("_SKIN")) continue;
            Color k = AHVRoid.SkinTint(skin);
            foreach (var pr in new[] { "_Color", "_BaseColor" }) if (m.HasProperty(pr)) { var c0 = m.GetColor(pr); m.SetColor(pr, new Color(k.r, k.g, k.b, c0.a)); }
            if (m.HasProperty("_ShadeColor")) { var s0 = m.GetColor("_ShadeColor"); m.SetColor("_ShadeColor", new Color(s0.r * k.r, s0.g * k.g, s0.b * k.b, s0.a)); }
            ch = true;
        }
        if (ch) smr.materials = mats;
        return true;
    }

    public static bool HasCostume(GameObject rig)
    {
        var link = rig != null ? rig.GetComponent<AHVRoidLink>() : null; if (link == null || link.Body == null) return false;
        foreach (var s in link.Body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.name == "CostumeBody") return true;
        return false;
    }
    // the robe is off (a costume or the under-clothes): leg armour can show
    public static bool RobeOff(GameObject rig)
    {
        var link = rig != null ? rig.GetComponent<AHVRoidLink>() : null; if (link == null || link.Body == null) return false;
        foreach (var s in link.Body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.name == "CostumeBody" || s.name == "UnderBody") return true;
        return false;
    }
}

public partial class AHUI
{
    // the Wardrobe's costume rows: the one you show, then each costume to wear or buy
    void CostumeRows(AHPlayer p, List<Action<int>> rows)
    {
        string cur = AHCostumes.Worn(p);
        var cd = cur != null ? AHCostumes.Get(cur) : null;
        rows.Add(s => Row(s, "Costume: " + (cd != null ? cd.name : "Show gear"), new Color(1f, 0.75f, 0.45f),
            cd != null ? "Your gear still gives all its stats; only your weapon shows." : "Your equipped gear shows. Pick a costume below to wear one.", "",
            new WkBtn { label = "Show gear", on = cd != null, col = Plain, act = () => { p.prog.costume = ""; p.CheckOutfit(); g.SaveProgress(); RenderWork(); } }));
        foreach (var d in AHCostumes.All)
        {
            var dd = d; bool own = AHCostumes.Owns(p, d.id), made = AHCostumes.Exists(p.look, d.id), on = cur == d.id;
            string sub = d.blurb + (made ? "" : " (coming soon for this hero)");
            if (own)
                rows.Add(s => Row(s, d.name + (on ? "  · wearing" : ""), new Color(0.95f, 0.8f, 1f), sub, "",
                    new WkBtn { label = on ? "Worn" : "Wear", on = made && !on, col = Go, act = () => { p.prog.costume = dd.id; p.CheckOutfit(); g.SaveProgress(); Toast("Wearing " + dd.name + "."); RenderWork(); } }));
            else
                rows.Add(s => Row(s, d.name, new Color(0.8f, 0.7f, 0.9f), sub, AHItems.MoneyText(d.price * AHDB.CU),
                    new WkBtn { label = "Buy", on = made, col = Go, act = () => { Toast(AHCostumes.Buy(p, dd.id) ?? ""); p.CheckOutfit(); g.SaveProgress(); RenderWork(); } }));
        }
    }
}
