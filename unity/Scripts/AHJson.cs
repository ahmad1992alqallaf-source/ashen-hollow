// Ashen Hollow: a small JSON reader for the web game's data files (JsonUtility cannot read keyed tables).
// Objects become Dictionary<string, object>, arrays List<object>, numbers double, plus string, bool and null.
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class AHJson
{
    public static object Parse(string s)
    {
        int i = 0;
        object v = Value(s, ref i);
        return v;
    }

    static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

    static object Value(string s, ref int i)
    {
        Ws(s, ref i);
        if (i >= s.Length) return null;
        char c = s[i];
        if (c == '{') return Obj(s, ref i);
        if (c == '[') return Arr(s, ref i);
        if (c == '"') return Str(s, ref i);
        if (c == 't') { i += 4; return true; }
        if (c == 'f') { i += 5; return false; }
        if (c == 'n') { i += 4; return null; }
        return Num(s, ref i);
    }

    static Dictionary<string, object> Obj(string s, ref int i)
    {
        var d = new Dictionary<string, object>();
        i++;
        while (true)
        {
            Ws(s, ref i);
            if (s[i] == '}') { i++; return d; }
            string k = Str(s, ref i);
            Ws(s, ref i); i++;   // :
            d[k] = Value(s, ref i);
            Ws(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == '}') { i++; return d; }
        }
    }

    static List<object> Arr(string s, ref int i)
    {
        var a = new List<object>();
        i++;
        while (true)
        {
            Ws(s, ref i);
            if (s[i] == ']') { i++; return a; }
            a.Add(Value(s, ref i));
            Ws(s, ref i);
            if (s[i] == ',') { i++; continue; }
            if (s[i] == ']') { i++; return a; }
        }
    }

    static string Str(string s, ref int i)
    {
        var sb = new StringBuilder();
        i++;   // opening quote
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') break;
            if (c != '\\') { sb.Append(c); continue; }
            char e = s[i++];
            switch (e)
            {
                case 'n': sb.Append('\n'); break;
                case 't': sb.Append('\t'); break;
                case 'r': sb.Append('\r'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'u': sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber)); i += 4; break;
                default: sb.Append(e); break;
            }
        }
        return sb.ToString();
    }

    static object Num(string s, ref int i)
    {
        int st = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
    }

    // ---------- small helpers for reading the parsed tree ----------
    public static Dictionary<string, object> O(object o, string key)
    {
        var d = o as Dictionary<string, object>;
        object v;
        return d != null && d.TryGetValue(key, out v) ? v as Dictionary<string, object> : null;
    }
    public static List<object> A(object o, string key)
    {
        var d = o as Dictionary<string, object>;
        object v;
        return d != null && d.TryGetValue(key, out v) ? v as List<object> : null;
    }
    public static string S(object o, string key, string def = null)
    {
        var d = o as Dictionary<string, object>;
        object v;
        return d != null && d.TryGetValue(key, out v) && v is string ? (string)v : def;
    }
    public static double N(object o, string key, double def = 0)
    {
        var d = o as Dictionary<string, object>;
        object v;
        if (d == null || !d.TryGetValue(key, out v) || v == null) return def;
        if (v is double) return (double)v;
        if (v is bool) return (bool)v ? 1 : 0;
        return def;
    }
    public static bool B(object o, string key)
    {
        var d = o as Dictionary<string, object>;
        object v;
        if (d == null || !d.TryGetValue(key, out v) || v == null) return false;
        if (v is bool) return (bool)v;
        if (v is double) return (double)v != 0;
        return v is string ? (string)v != "" : true;
    }
    public static bool Has(object o, string key) { var d = o as Dictionary<string, object>; return d != null && d.ContainsKey(key) && d[key] != null; }
}
