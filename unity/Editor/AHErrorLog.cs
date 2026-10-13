// Ashen Hollow (editor): every error, exception and failed assertion the game throws while playing in the editor is
// written to HeroShots/errors.txt, once each (with how many times it happened, where it first happened and its stack),
// so a long test run leaves a list of bugs to fix. "Test: Clear Error Log" starts the list afresh.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AHErrorLog
{
    class E { public string msg, stack, area; public int n; public double first; }
    static readonly Dictionary<string, E> seen = new Dictionary<string, E>();
    static double lastWrite;
    static string FilePath { get { var d = Path.Combine(Application.dataPath, "../HeroShots"); Directory.CreateDirectory(d); return Path.Combine(d, "errors.txt"); } }

    static AHErrorLog()
    {
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        EditorApplication.update -= Flush; EditorApplication.update += Flush;
    }

    static void OnLog(string msg, string stack, LogType type)
    {
        if (type == LogType.Log || type == LogType.Warning || !Application.isPlaying) return;
        if (msg.Contains("Mismatched state in DefaultScenario")) return;   // (an editor quirk on stopping, not the game)
        string key = msg.Length > 200 ? msg.Substring(0, 200) : msg;
        string top = stack ?? ""; int nl = top.IndexOf('\n'); if (nl > 0) top = top.Substring(0, nl); key += "|" + top;
        E e; if (!seen.TryGetValue(key, out e)) { e = new E { msg = msg, stack = stack, area = AHGame.AreaId, first = EditorApplication.timeSinceStartup }; seen[key] = e; }
        e.n++;
    }

    static void Flush()
    {
        if (seen.Count == 0 || EditorApplication.timeSinceStartup - lastWrite < 5) return;
        lastWrite = EditorApplication.timeSinceStartup;
        var sb = new StringBuilder();
        foreach (var e in seen.Values) { sb.AppendLine("=== x" + e.n + " in " + e.area + ": " + e.msg); sb.AppendLine((e.stack ?? "").Trim()); sb.AppendLine(); }
        File.WriteAllText(FilePath, sb.ToString());
    }

    [MenuItem("Ashen Hollow/Test: Clear Error Log")]
    static void Clear() { seen.Clear(); if (File.Exists(FilePath)) File.Delete(FilePath); }
}
