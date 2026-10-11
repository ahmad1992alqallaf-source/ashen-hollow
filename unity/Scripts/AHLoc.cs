// Ashen Hollow: the game's language. English is the main language; a player can switch everything to Arabic in
// Settings (text, the townsfolk's lines and, where they are recorded, the voices). The choice is kept on the device.
//
// Arabic on screen: Unity's text draws letters left to right, one by one, without joining them. Arabic letters change
// shape with their neighbours (start, middle, end of a word, or alone) and lines read right to left, so before an
// Arabic line is shown it is "shaped" here: every letter becomes its joined form (Unicode presentation forms), lam and
// alef become one letter, and the line is turned around so it reads right to left; runs of English letters and numbers
// inside it keep their own direction. Long text is broken into lines first (Lines), because Unity would wrap a turned
// line from the wrong end.
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class AHLoc
{
    const string Key = "ah_lang";
    public static string Lang { get { return AHPrefs.GetInt(Key, 0) == 1 ? "ar" : "en"; } set { AHPrefs.SetInt(Key, value == "ar" ? 1 : 0); } }
    public static bool Arabic { get { return Lang == "ar"; } }

    // the text in the player's language (Arabic shaped for the screen); falls back to English
    public static string T(string en, string ar) { return Arabic && !string.IsNullOrEmpty(ar) ? Shape(ar) : en; }
    // a {"en":..,"ar":..} object from a data file
    public static string T(object o) { return o is string ? (string)o : T(AHJson.S(o, "en"), AHJson.S(o, "ar")); }
    public static string Raw(object o) { return o is string ? (string)o : Arabic && AHJson.S(o, "ar") != null ? AHJson.S(o, "ar") : AHJson.S(o, "en"); }

    // ---------- shaping ----------
    // per letter: isolated, final, initial, medial (0 = that form does not exist: the letter does not join forwards)
    static readonly Dictionary<char, char[]> forms = new Dictionary<char, char[]>
    {
        { 'ء', new[] { 'ﺀ', 'ﺀ', '\0', '\0' } },           // hamza
        { 'آ', new[] { 'ﺁ', 'ﺂ', '\0', '\0' } },           // alef madda
        { 'أ', new[] { 'ﺃ', 'ﺄ', '\0', '\0' } },           // alef hamza above
        { 'ؤ', new[] { 'ﺅ', 'ﺆ', '\0', '\0' } },           // waw hamza
        { 'إ', new[] { 'ﺇ', 'ﺈ', '\0', '\0' } },           // alef hamza below
        { 'ئ', new[] { 'ﺉ', 'ﺊ', 'ﺋ', 'ﺌ' } },   // yeh hamza
        { 'ا', new[] { 'ﺍ', 'ﺎ', '\0', '\0' } },           // alef
        { 'ب', new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' } },   // beh
        { 'ة', new[] { 'ﺓ', 'ﺔ', '\0', '\0' } },           // teh marbuta
        { 'ت', new[] { 'ﺕ', 'ﺖ', 'ﺗ', 'ﺘ' } },   // teh
        { 'ث', new[] { 'ﺙ', 'ﺚ', 'ﺛ', 'ﺜ' } },   // theh
        { 'ج', new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' } },   // jeem
        { 'ح', new[] { 'ﺡ', 'ﺢ', 'ﺣ', 'ﺤ' } },   // hah
        { 'خ', new[] { 'ﺥ', 'ﺦ', 'ﺧ', 'ﺨ' } },   // khah
        { 'د', new[] { 'ﺩ', 'ﺪ', '\0', '\0' } },           // dal
        { 'ذ', new[] { 'ﺫ', 'ﺬ', '\0', '\0' } },           // thal
        { 'ر', new[] { 'ﺭ', 'ﺮ', '\0', '\0' } },           // reh
        { 'ز', new[] { 'ﺯ', 'ﺰ', '\0', '\0' } },           // zain
        { 'س', new[] { 'ﺱ', 'ﺲ', 'ﺳ', 'ﺴ' } },   // seen
        { 'ش', new[] { 'ﺵ', 'ﺶ', 'ﺷ', 'ﺸ' } },   // sheen
        { 'ص', new[] { 'ﺹ', 'ﺺ', 'ﺻ', 'ﺼ' } },   // sad
        { 'ض', new[] { 'ﺽ', 'ﺾ', 'ﺿ', 'ﻀ' } },   // dad
        { 'ط', new[] { 'ﻁ', 'ﻂ', 'ﻃ', 'ﻄ' } },   // tah
        { 'ظ', new[] { 'ﻅ', 'ﻆ', 'ﻇ', 'ﻈ' } },   // zah
        { 'ع', new[] { 'ﻉ', 'ﻊ', 'ﻋ', 'ﻌ' } },   // ain
        { 'غ', new[] { 'ﻍ', 'ﻎ', 'ﻏ', 'ﻐ' } },   // ghain
        { 'ف', new[] { 'ﻑ', 'ﻒ', 'ﻓ', 'ﻔ' } },   // feh
        { 'ق', new[] { 'ﻕ', 'ﻖ', 'ﻗ', 'ﻘ' } },   // qaf
        { 'ك', new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' } },   // kaf
        { 'ل', new[] { 'ﻝ', 'ﻞ', 'ﻟ', 'ﻠ' } },   // lam
        { 'م', new[] { 'ﻡ', 'ﻢ', 'ﻣ', 'ﻤ' } },   // meem
        { 'ن', new[] { 'ﻥ', 'ﻦ', 'ﻧ', 'ﻨ' } },   // noon
        { 'ه', new[] { 'ﻩ', 'ﻪ', 'ﻫ', 'ﻬ' } },   // heh
        { 'و', new[] { 'ﻭ', 'ﻮ', '\0', '\0' } },           // waw
        { 'ى', new[] { 'ﻯ', 'ﻰ', '\0', '\0' } },           // alef maksura
        { 'ي', new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' } },   // yeh
        { 'ـ', new[] { 'ـ', 'ـ', 'ـ', 'ـ' } },   // tatweel
    };
    // lam followed by an alef: one letter (isolated, final)
    static readonly Dictionary<char, char[]> lamAlef = new Dictionary<char, char[]>
    {
        { 'آ', new[] { 'ﻵ', 'ﻶ' } }, { 'أ', new[] { 'ﻷ', 'ﻸ' } },
        { 'إ', new[] { 'ﻹ', 'ﻺ' } }, { 'ا', new[] { 'ﻻ', 'ﻼ' } },
    };
    static bool Mark(char c) { return (c >= 'ً' && c <= 'ٟ') || c == 'ٰ'; }   // harakat: dropped (the font draws them badly unjoined)
    static bool JoinsForward(char c) { char[] f; return forms.TryGetValue(c, out f) && f[2] != '\0'; }
    static bool IsArabic(char c) { return (c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= '﻿'); }

    // join the letters (logical order kept)
    static string Join(string s)
    {
        var src = new List<char>(s.Length); foreach (var c in s) if (!Mark(c)) src.Add(c);
        var sb = new StringBuilder(src.Count);
        for (int i = 0; i < src.Count; i++)
        {
            char c = src[i]; char[] f;
            if (!forms.TryGetValue(c, out f)) { sb.Append(c); continue; }
            bool prev = i > 0 && JoinsForward(src[i - 1]);
            char[] la;
            if (c == 'ل' && i + 1 < src.Count && lamAlef.TryGetValue(src[i + 1], out la)) { sb.Append(prev ? la[1] : la[0]); i++; continue; }
            bool next = f[2] != '\0' && i + 1 < src.Count && forms.ContainsKey(src[i + 1]);
            sb.Append(prev && next ? f[3] : prev ? f[1] : next ? f[2] : f[0]);
        }
        return sb.ToString();
    }

    // one line ready for the screen: joined, then turned right to left (English words and numbers keep their direction)
    public static string Shape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        bool any = false; foreach (var c in s) if (IsArabic(c)) { any = true; break; }
        if (!any) return s;
        if (s.IndexOf('\n') >= 0) { var ls = s.Split('\n'); for (int i = 0; i < ls.Length; i++) ls[i] = Shape(ls[i]); return string.Join("\n", ls); }
        string j = Join(s);
        // split into runs: right-to-left (Arabic, spaces, most punctuation) and left-to-right (Latin letters, digits)
        var runs = new List<string>(); var cur = new StringBuilder(); int curDir = -1;
        foreach (var c in j)
        {
            int d = (char.IsLetterOrDigit(c) && !IsArabic(c)) ? 1 : 0;
            if (d != curDir && cur.Length > 0) { runs.Add((curDir == 1 ? "L" : "R") + cur); cur.Length = 0; }
            curDir = d; cur.Append(c);
        }
        if (cur.Length > 0) runs.Add((curDir == 1 ? "L" : "R") + cur);
        var outSb = new StringBuilder(j.Length);
        for (int r = runs.Count - 1; r >= 0; r--)
        {
            string run = runs[r].Substring(1);
            if (runs[r][0] == 'L') { outSb.Append(run); continue; }
            for (int k = run.Length - 1; k >= 0; k--) outSb.Append(Mirror(run[k]));
        }
        return outSb.ToString();
    }
    static char Mirror(char c)
    {
        switch (c) { case '(': return ')'; case ')': return '('; case '[': return ']'; case ']': return '['; case '«': return '»'; case '»': return '«'; case '<': return '>'; case '>': return '<'; }
        return c;
    }

    // break text into lines of at most `width` characters, then shape each (for bubbles and boxes that wrap)
    public static string Lines(string s, int width)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var words = s.Split(' '); var lines = new List<string>(); var cur = new StringBuilder();
        foreach (var w in words)
        {
            if (cur.Length > 0 && cur.Length + 1 + w.Length > width) { lines.Add(cur.ToString()); cur.Length = 0; }
            if (cur.Length > 0) cur.Append(' '); cur.Append(w);
        }
        if (cur.Length > 0) lines.Add(cur.ToString());
        if (Arabic) for (int i = 0; i < lines.Count; i++) lines[i] = Shape(lines[i]);
        return string.Join("\n", lines);
    }
}
