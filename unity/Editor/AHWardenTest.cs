// Ashen Hollow (editor test): "Test: Warden Skill Check". Play as a Warden. Five harmless stone dummies appear around
// the hero (one right where Rockwall rises); every Warden skill (the five, the ten from the spellbook, the two path
// skills and the four ultimates) is cast in turn, about a second apart, and what each one did
// is written to the console and to HeroShots/warden_check.txt: damage dealt, who was taunted, held, slowed or pulled,
// the stone skin and the wall. The dummies are removed at the end.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHWardenTest
{
    static readonly List<AHMob> dummies = new List<AHMob>();
    static int step; static double next; static StringBuilder log;
    static readonly Dictionary<AHMob, int> hpBefore = new Dictionary<AHMob, int>();
    static readonly Dictionary<AHMob, Vector3> posBefore = new Dictionary<AHMob, Vector3>();
    static readonly List<Vector3> home = new List<Vector3>(); static Vector3 facing;

    [MenuItem("Ashen Hollow/Test: Warden Skill Check")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { Debug.Log("Ashen Hollow: needs Play mode"); return; }
        var p = g.player;
        if (p.cls.id != "warden") { Debug.Log("Ashen Hollow: play as a Warden for this check (now " + p.cls.id + ")"); return; }
        Clear(g);
        Vector3 me = p.transform.position; Vector3 f = p.transform.forward; f.y = 0f; f.Normalize(); Vector3 r = Vector3.Cross(Vector3.up, f);
        Vector3[] at = { me + f * 2.5f, me + f * 4.5f + r * 1.5f, me - r * 3.5f, me + f * 11f, me + f * 3.9f - r * 1.2f };   // close, near, beside, far (Pebble Throw), in the wall's way
        home.Clear(); foreach (var v in at) home.Add(v); facing = f;
        for (int i = 0; i < at.Length; i++)
        {
            var t = new AHMobType { id = "wardentest" + i, model = "Web/mGolem", name = "Stone Dummy " + (i + 1), lvl = 1, hp = 100000, dmg = 0, xp = 0, gold = 0, speed = 1f, radius = 0.6f, aggro = 0f, atkCd = 99f, respawn = 1e9f, noSkin = true };
            var m = AHMob.Create(g, t, g.Resolve(at[i], 0.6f)); g.mobs.Add(m); dummies.Add(m);
        }
        log = new StringBuilder("Warden skill check, level " + p.level + ", power " + p.Power.ToString("0.0") + "\n");
        step = 0; next = EditorApplication.timeSinceStartup + 1.5;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }

    static void Snap()
    {
        hpBefore.Clear(); posBefore.Clear();
        foreach (var m in dummies) if (m != null) { hpBefore[m] = Mathf.RoundToInt(m.hp); posBefore[m] = m.transform.position; }
    }
    static string Report(AHPlayer p)
    {
        var sb = new StringBuilder();
        foreach (var m in dummies)
        {
            if (m == null) continue;
            int dmg = hpBefore[m] - Mathf.RoundToInt(m.hp); float moved = (m.transform.position - posBefore[m]).magnitude;
            sb.Append("   " + m.type.name + ": -" + dmg + (m.Taunted ? ", taunted" : "") + (moved > 0.5f ? ", moved " + moved.ToString("0.0") + " m" : "") + Statuses(m) + "\n");
        }
        sb.Append("   hero: hp " + Mathf.RoundToInt(p.hp) + "/" + Mathf.RoundToInt(p.maxHp) + (p.fortT > 0f ? ", stone skin " + p.fortT.ToString("0.0") + " s (" + Mathf.RoundToInt(p.fortV * 100f) + "% kept off)" : "") + (GameObject.Find("RockWall") != null ? ", a rock wall stands" : "") + "\n");
        return sb.ToString();
    }
    static string Statuses(AHMob m)
    {
        var sb = new StringBuilder();
        foreach (var n in new[] { "slowT", "rootT", "stunT", "burnT", "poisonT", "fearT" })
        {
            var fi = typeof(AHMob).GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (fi != null && fi.FieldType == typeof(float) && (float)fi.GetValue(m) > 0f) sb.Append(", " + n.Substring(0, n.Length - 1) + " " + ((float)fi.GetValue(m)).ToString("0.0") + " s");
        }
        return sb.ToString();
    }

    static readonly string[] Order = { "stonehammer", "emberquake", "bedrockroar", "rockwall", "stoneskin",
        "shieldslam", "pebblethrow", "earthengrip", "guardward", "fossilshell", "avalanche", "tremor", "emberbrand", "faultline", "rampart",
        "bulwark", "magmaquake", "mountain", "titanfall", "magmacore", "eruption" };
    static void Tick()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorApplication.update -= Tick; dummies.Clear(); return; }
        if (EditorApplication.timeSinceStartup < next) return;
        var p = g.player;
        if (step > 0) log.Append(Report(p));
        if (step >= Order.Length) { Finish(g); return; }
        string id = Order[step++];
        // put the skill on the ring for the test if it isn't there (Pebble Throw comes at level 10)
        int slot = -1; for (int i = 0; i < p.Spells.Length; i++) if (p.Spells[i].id == id) slot = i;
        if (slot < 0)
        {
            SpellDef sp = AHEvo.Book("warden").Find(s => s.id == id) ?? AHEvo.Evo(id);
            if (sp == null) { log.Append(id + ": not found\n"); next = EditorApplication.timeSinceStartup + 0.1; return; }
            var arr = p.Spells; slot = arr.Length - 1; arr[slot] = sp;
        }
        for (int i = 0; i < p.cds.Length; i++) p.cds[i] = 0f;
        // every dummy back in its place, unharmed by what came before, the hero facing the same way
        for (int i = 0; i < dummies.Count; i++)
        {
            var m = dummies[i]; if (m == null) continue;
            m.transform.position = g.Resolve(home[i], 0.6f); m.hp = m.type.hp;
            foreach (var n in new[] { "slowT", "rootT", "stunT", "burnT", "poisonT", "fearT" })
            {
                var fi = typeof(AHMob).GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (fi != null && fi.FieldType == typeof(float)) fi.SetValue(m, 0f);
            }
        }
        p.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        if (id == "pebblethrow") p.target = dummies[3];
        else p.target = dummies[0];
        Snap();
        log.Append(id + " (" + p.Spells[slot].name + ", " + p.Spells[slot].kind + "):\n");
        p.Cast(slot);
        next = EditorApplication.timeSinceStartup + (id == "pebblethrow" ? 2.0 : id == "tremor" ? 1.6 : 1.3);
    }

    static void Finish(AHGame g)
    {
        EditorApplication.update -= Tick;
        string text = log.ToString();
        Debug.Log("Ashen Hollow: " + text);
        File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/warden_check.txt"), text);
        Clear(g);
        g.player.RebuildRing();
    }
    static void Clear(AHGame g)
    {
        foreach (var m in dummies) if (m != null) { g.mobs.Remove(m); Object.Destroy(m.gameObject); }
        dummies.Clear();
    }
}
