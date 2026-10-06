// Ashen Hollow: the web game's data (Resources/AH/Data/*.json), loaded once and shared by every system.
// Units in the data are web world units: metres = units * 0.04 (S).
using System.Collections.Generic;
using UnityEngine;

public static class AHDB
{
    public const float S = 0.04f;   // metres per web unit

    static readonly Dictionary<string, object> files = new Dictionary<string, object>();

    // the whole parsed file (one object whose keys are the web game's table names)
    public static object File(string name)
    {
        object o;
        if (files.TryGetValue(name, out o)) return o;
        var ta = Resources.Load<TextAsset>("AH/Data/" + name);
        if (ta == null) { Debug.LogError("Ashen Hollow: Resources/AH/Data/" + name + ".json is missing"); files[name] = null; return null; }
        o = AHJson.Parse(ta.text);
        files[name] = o;
        return o;
    }

    // one table from a file, e.g. Table("items", "ITEMS")
    public static Dictionary<string, object> Table(string file, string table) { return AHJson.O(File(file), table); }
    public static List<object> List(string file, string table) { return AHJson.A(File(file), table); }

    // ---------- common tables ----------
    public static Dictionary<string, object> Items { get { return Table("items", "ITEMS"); } }
    public static Dictionary<string, object> Mobs { get { return Table("monsters", "MOBS"); } }
    public static Dictionary<string, object> Classes { get { return Table("classes", "CLASSES"); } }
    public static object Rules { get { return File("rules"); } }

    public static int Slots { get { return (int)AHJson.N(Rules, "SLOTS", 28); } }
    public static long CU { get { return (long)AHJson.N(Rules, "CU", 1000); } }

    // "#rrggbb" or a number like 0xrrggbb, as written in the web game
    public static Color Col(object v, Color def)
    {
        if (v is string)
        {
            Color c;
            return ColorUtility.TryParseHtmlString((string)v, out c) ? c : def;
        }
        if (v is double) return AHGame.Hex((int)(double)v);
        return def;
    }

    // ---------- levels (web: XP_AT for skills up to MAX_LV, CL_AT for class levels up to 90) ----------
    static int[] xpAt, clAt;
    public static int[] XpAt { get { if (xpAt == null) xpAt = Ints(AHJson.A(Rules, "XP_AT")); return xpAt; } }
    public static int[] ClAt { get { if (clAt == null) clAt = Ints(List("classes", "CL_AT")); return clAt; } }
    public static int MaxSkillLv { get { return (int)AHJson.N(Rules, "MAX_LV", 20); } }
    public const int ClMax = 90;

    static int[] Ints(List<object> l)
    {
        if (l == null) return new[] { 0, 0 };
        var a = new int[l.Count];
        for (int i = 0; i < a.Length; i++) a[i] = l[i] is double ? (int)(double)l[i] : 0;
        return a;
    }

    // web: levelOf(xp) for skills
    public static int SkillLevel(long xp)
    {
        var t = XpAt; int L = 1, max = MaxSkillLv;
        while (L < max && L + 1 < t.Length && xp >= t[L + 1]) L++;
        return L;
    }

    // web: classLv for the class XP
    public static int ClassLevel(long xp)
    {
        var t = ClAt; int L = 1;
        while (L < ClMax && L + 1 < t.Length && xp >= t[L + 1]) L++;
        return L;
    }

    // ---------- profession mastery (web M_AT up to M_MAX 50, PROF_RANKS) ----------
    static int[] mAt;
    public static int[] MAt { get { if (mAt == null) mAt = Ints(AHJson.A(Rules, "M_AT")); return mAt; } }
    public const int MMax = 50;
    public static int MasteryLevel(long xp)
    {
        var t = MAt; int L = 1;
        while (L < MMax && L + 1 < t.Length && xp >= t[L + 1]) L++;
        return L;
    }
    public static string Rank(int L)
    {
        string r = "Apprentice";
        var rk = AHJson.A(Rules, "PROF_RANKS");
        if (rk != null) foreach (var o in rk) { var row = o as List<object>; if (row != null && row.Count > 1 && L >= (double)row[0]) r = (string)row[1]; }
        return r;
    }

    public static string SkillName(string skill) { return AHJson.S(AHJson.O(Rules, "SKILL_NAME"), skill, skill); }
}
