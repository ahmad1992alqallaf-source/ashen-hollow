// Ashen Hollow: how each spell looks and sounds.
//  - Icon: a round painted icon per spell (Resources/AH/Icons/Spells/<id>.png, glyphs from game-icons.net, CC BY 3.0).
//  - Element: fire, frost, arcane, storm, holy, nature, star, shadow, poison, blade, earth, arrow, war or wind;
//    it picks the cast and impact sounds (AHSound.Spell).
//  - Pose: the body animation the hero plays when casting, chosen from the spell's kind and the hero's class
//    (sword swings, a spinning slash, a ground slam, a two-handed crossbow shot, a hand thrust, a throw, a roar,
//    kneeling to set a trap or bless the ground, a roll).
using System.Collections.Generic;
using UnityEngine;

public enum SpellEl { Fire, Frost, Arcane, Storm, Holy, Nature, Star, Shadow, Poison, Blade, Earth, Arrow, War, Wind, Heal }

public static class AHSpellLook
{
    static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

    public static Sprite Icon(SpellDef sp)
    {
        if (sp == null) return null;
        Sprite s; if (icons.TryGetValue(sp.id, out s) && s != null) return s;
        // all icons share one atlas (Resources/AH/Icons/spell_icons.png); the index says which cell each spell has
        if (atlas == null && !tried)
        {
            tried = true;
            atlas = Resources.Load<Texture2D>("AH/Icons/spell_icons");
            var ta = Resources.Load<TextAsset>("AH/Icons/spell_icons_index");
            if (atlas == null || ta == null) { Debug.LogWarning("Ashen Hollow: spell icon atlas missing"); atlas = null; }
            else { atlas.wrapMode = TextureWrapMode.Clamp; index = AHJson.Parse(ta.text); }
        }
        if (atlas != null)
        {
            var ids = AHJson.O(index, "ids"); object cell = ids != null ? AHJson.O(ids, sp.id) : null;
            if (cell == null && ids != null) { var d = ids as Dictionary<string, object>; if (d != null && d.ContainsKey(sp.id)) cell = d[sp.id]; }
            if (cell != null)
            {
                int n = (int)System.Convert.ToDouble(cell), cols = (int)AHJson.N(index, "cols", 12);
                float c = (float)AHJson.N(index, "cell", 128);
                float k = atlas.width / (cols * c); c *= k;   // the importer may have scaled the atlas down
                var r = new Rect((n % cols) * c, atlas.height - (n / cols + 1) * c, c, c);
                s = Sprite.Create(atlas, r, new Vector2(0.5f, 0.5f), 100f); s.name = "spell_" + sp.id;
            }
        }
        icons[sp.id] = s; return s;
    }
    static Texture2D atlas; static object index; static bool tried;

    static readonly Dictionary<string, SpellEl> El = new Dictionary<string, SpellEl>();
    static void Set(SpellEl e, params string[] ids) { foreach (var i in ids) El[i] = e; }
    static AHSpellLook()
    {
        Set(SpellEl.Fire, "fireball", "meteor", "pillar", "comet", "bomb", "cataclysm", "flamewave", "firestorm", "explosive");
        Set(SpellEl.Frost, "nova", "icelance", "prison", "blizzard", "zero", "frosttrap");
        Set(SpellEl.Arcane, "missiles", "blink", "barrier", "timewarp", "arcexp", "voidbolt", "rift", "soulharvest");
        Set(SpellEl.Storm, "spark", "chain", "cyclone", "hurricane", "storm");
        Set(SpellEl.Holy, "holyfire", "consecrate", "divine", "smite", "holynova", "chains", "guardian", "penance", "sacred", "wrath",
            "radiance", "sanctuary", "judgment", "aegis");
        Set(SpellEl.Heal, "heal", "renew", "purify", "prayer", "rejuv", "regrowth", "tranquility", "wildgrowth", "secondwind");
        Set(SpellEl.Nature, "dwrath", "roots", "thorns", "barkskin", "grasp", "treants", "wildwrath", "snare");
        Set(SpellEl.Star, "moonfire", "starfall", "starfire");
        Set(SpellEl.Shadow, "shadowstep", "vanish", "cloak", "shadowdance", "deathmark", "smokebomb", "camo", "phantomblades", "evasion", "ambush");
        Set(SpellEl.Poison, "envenom", "garrote", "plague", "swarm");
        Set(SpellEl.Blade, "cleave", "blow", "whirl", "backstab", "fanknives", "bash", "rend", "execute", "bladestorm", "titan", "gouge", "kidney",
            "eviscerate", "bladedance", "assassinate", "thousandcuts", "harvest", "maul");
        Set(SpellEl.Earth, "quake", "leap", "earthsplit", "stampede");
        Set(SpellEl.Arrow, "aimed", "multishot", "concussive", "rapidfire", "volley", "barbed", "killshot", "rainarrows", "piercing", "headshot",
            "arrowstorm", "huntmark", "eagleeye");
        Set(SpellEl.War, "cry", "avatar", "rampage", "bearform", "bearroar", "feralfury", "wolfcall", "packhunt", "unstoppable", "shieldwall",
            "ironwill", "adrenaline");
        Set(SpellEl.Wind, "charge", "disengage");
        // the Warden: stone, its roars, and the ember in the rock
        Set(SpellEl.Earth, "stonehammer", "rockwall", "pebblethrow", "earthengrip", "avalanche", "tremor", "faultline", "rampart", "titanfall", "shieldslam");
        Set(SpellEl.War, "bedrockroar", "stoneskin", "fossilshell", "guardward", "bulwark", "mountain");
        Set(SpellEl.Fire, "emberquake", "emberbrand", "magmaquake", "magmacore", "eruption");
    }

    public static SpellEl Element(SpellDef sp)
    {
        SpellEl e; if (sp != null && El.TryGetValue(sp.id, out e)) return e;
        if (sp == null) return SpellEl.Arcane;
        switch (sp.kind)
        {
            case SpellKind.Heal: case SpellKind.Hot: return SpellEl.Heal;
            case SpellKind.Strike: case SpellKind.Arc: case SpellKind.Whirl: return SpellEl.Blade;
            case SpellKind.Blink: case SpellKind.Leap: case SpellKind.Charge: return SpellEl.Wind;
            case SpellKind.Buff: case SpellKind.Bear: return SpellEl.War;
            case SpellKind.Stealth: return SpellEl.Shadow;
        }
        if (sp.status == AHStatus.Burn) return SpellEl.Fire;
        if (sp.status == AHStatus.Poison) return SpellEl.Poison;
        return SpellEl.Arcane;
    }

    // the cast animation: a key in AHPlayer.Poses, or null for none (blink)
    public static string Pose(ClassDef cls, SpellDef sp)
    {
        string c = cls != null ? cls.id : "";
        bool melee = c == "warrior" || c == "rogue" || c == "warden", ranger = c == "ranger", stone = c == "warden";
        switch (sp.kind)
        {
            case SpellKind.Strike:
                if (ranger) return "shoot";
                if (!melee) return "bolt";
                if (sp.mult >= 2.5f) return c == "rogue" ? "slash2" : "heavy";
                return c == "rogue" ? "stab" : "slash";
            case SpellKind.Arc: return melee ? "slash2" : "push";
            case SpellKind.Nova:
                if (c == "warrior" || stone) return "slam";
                if (ranger) return "kneel";
                return "burst";
            case SpellKind.Whirl: return melee ? "spin" : "roar";
            case SpellKind.Bolt: case SpellKind.Multi: return ranger ? "shoot" : "bolt";
            case SpellKind.GroundAt:
                if (c == "warrior" || stone) return "slam";
                if (sp.id == "snare") return "kneel";
                if (ranger) return "shoot";
                if (sp.id == "bomb" || sp.id == "plague") return "throw";
                return "push";
            case SpellKind.Consecrate: return "kneel";
            case SpellKind.Charge: return "dash";
            case SpellKind.Blink: return null;
            case SpellKind.StepBehind: return "stab";
            case SpellKind.Leap: return "hop";
            case SpellKind.Heal: case SpellKind.Hot: return "raise";
            case SpellKind.Shield: return c == "warrior" || stone ? "push" : "raise";
            case SpellKind.Taunt: return "roar";
            case SpellKind.Wall: return "slam";
            case SpellKind.Fortify: return "raise";
            case SpellKind.Invuln: return c == "rogue" ? "roll" : "raise";
            case SpellKind.Buff: case SpellKind.Bear: return "roar";
            case SpellKind.Stealth: return sp.id == "smokebomb" ? "throw" : "roll";
        }
        return "bolt";
    }
}

// test helper (Ashen Hollow > Test: Cast Pose Snapshots): plays every cast animation on the hero and photographs it
// from the front partway through, into one sheet (HeroShots/poses.png)
public class AHPoseShots : MonoBehaviour
{
    public string dir;
    System.Collections.IEnumerator Start()
    {
        var p = AHGame.I != null ? AHGame.I.player : null; if (p == null) { Destroy(this); yield break; }
        var keys = new List<string>(AHPlayer.PoseKeys);
        const int S = 300, cols = 6; int rows = (keys.Count + cols - 1) / cols;
        var sheet = new Texture2D(S * cols, S * rows, TextureFormat.RGB24, false);
        var go = new GameObject("PoseCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 32f; cam.nearClipPlane = 0.05f;
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt; var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        for (int i = 0; i < keys.Count; i++)
        {
            p.PlayPose(keys[i]);
            yield return new WaitForSeconds(0.28f);
            var tr = p.transform; Vector3 c = tr.position + Vector3.up * 1.0f;
            cam.transform.position = c + tr.forward * 3.6f + tr.right * 1.6f + Vector3.up * 0.4f; cam.transform.LookAt(c);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, S, S), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels((i % cols) * S, (rows - 1 - i / cols) * S, S, S, tex.GetPixels());
            yield return new WaitForSeconds(0.9f);
        }
        sheet.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "poses.png"), sheet.EncodeToPNG());
        cam.targetTexture = null; Destroy(go); Destroy(rt); Destroy(tex); Destroy(sheet);
        Debug.Log("Cast pose snapshots: " + string.Join(", ", keys.ToArray()));
        Destroy(this);
    }
}
