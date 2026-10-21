// Ashen Hollow (editor): commands from a file, so tests can be started without clicking through the long menu.
// Put HeroShots/cmd.txt next to the Assets folder with one command per line; the editor runs them once and deletes
// the file, and writes HeroShots/cmd_done.txt (what ran, and any error):
//   play                     - enter play mode
//   stop                     - leave play mode
//   hero <n>                 - on the hero select screen, play the n-th saved hero (0 = the first)
//   menu Ashen Hollow/...    - run any menu item by its path
//   check                    - Ashen Hollow > Test: Round Check
//   travel <area id>         - go to a land
//   time <0..1>              - time of day (0.3 day, 0.8 night)
//   refresh                  - reimport changed files (scripts compile after)
//   wait <seconds>           - hold the rest of the list (play mode loading, a land loading...)
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AHRemote
{
    static double next;
    static AHRemote()
    {
        EditorApplication.update -= Poll; EditorApplication.update += Poll;
        // the queue outlives the script reload that entering play mode brings (SessionState lasts the editor session)
        string q = SessionState.GetString("ah_remote_q", ""); foreach (var l in q.Split('\n')) if (l.Trim().Length > 0) queue.Enqueue(l.Trim());
        log.Append(SessionState.GetString("ah_remote_log", ""));
        waitUntil = EditorApplication.timeSinceStartup + Mathf.Max(0f, SessionState.GetFloat("ah_remote_wait", 0f));
    }
    static void Keep()
    {
        SessionState.SetString("ah_remote_q", string.Join("\n", queue.ToArray()));
        SessionState.SetString("ah_remote_log", log.ToString());
        SessionState.SetFloat("ah_remote_wait", (float)(waitUntil - EditorApplication.timeSinceStartup));
    }
    static string Dir { get { return Path.Combine(Application.dataPath, "../HeroShots"); } }

    static readonly System.Collections.Generic.Queue<string> queue = new System.Collections.Generic.Queue<string>();
    static readonly StringBuilder log = new StringBuilder();
    static double waitUntil;
    static void Poll()
    {
        double now = EditorApplication.timeSinceStartup;
        // the queue: one command per tick, "wait s" holds it (the queue lives in the editor, through entering play mode)
        if (queue.Count > 0 && now >= waitUntil && !EditorApplication.isCompiling)
        {
            string l = queue.Dequeue();
            if (l.StartsWith("wait ")) { double s; double.TryParse(l.Substring(5), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out s); waitUntil = now + s; log.AppendLine(l); }
            else { try { log.AppendLine(l + " -> " + Run(l)); } catch (System.Exception e) { log.AppendLine(l + " -> ERROR " + e.Message); } }
            Write(); Keep();
        }
        if (now < next) return;
        next = now + 1.0;
        string f = Path.Combine(Dir, "cmd.txt");
        if (!File.Exists(f)) return;
        string[] lines; try { lines = File.ReadAllLines(f); File.Delete(f); } catch { return; }
        log.Length = 0; log.AppendLine(System.DateTime.Now.ToString("HH:mm:ss"));
        foreach (var raw in lines) { string l = raw.Trim(); if (l.Length > 0 && !l.StartsWith("#")) queue.Enqueue(l); }
        Write(); Keep();
    }
    static void Write() { try { Directory.CreateDirectory(Dir); File.WriteAllText(Path.Combine(Dir, "cmd_done.txt"), log.ToString() + (queue.Count > 0 ? "(" + queue.Count + " to go)\n" : "(done)\n")); } catch { } }

    static string Run(string l)
    {
        int sp = l.IndexOf(' '); string c = sp < 0 ? l : l.Substring(0, sp), a = sp < 0 ? "" : l.Substring(sp + 1).Trim();
        switch (c)
        {
            case "play": EditorApplication.isPlaying = true; return "ok";
            case "stop": EditorApplication.isPlaying = false; return "ok";
            case "refresh": AssetDatabase.Refresh(); return "ok";
            case "menu": return EditorApplication.ExecuteMenuItem(a) ? "ok" : "no such menu item";
            case "check": return EditorApplication.ExecuteMenuItem("Ashen Hollow/Test: Round Check") ? "ok" : "no menu";
            case "hero":
                {
                    if (!Application.isPlaying) return "not playing";
                    int n; int.TryParse(a, out n);
                    return AHUI.TestPlayHero(n) ? "ok" : "no hero select screen";
                }
            case "travel":
                {
                    var g = AHGame.I; if (!Application.isPlaying || g == null) return "not playing";
                    var ta = Resources.Load<TextAsset>("AH/Areas/" + a); if (ta == null) return "no such land";
                    var d = JsonUtility.FromJson<AHWorldData>(ta.text); g.Travel(a, d.spawn.x, d.spawn.z, 0f); return "ok";
                }
            case "time":
                {
                    var g = AHGame.I; if (!Application.isPlaying || g == null) return "not playing";
                    float t; float.TryParse(a, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out t); g.SetTimeOfDay(t); return "ok";
                }
        }
        return "unknown command";
    }
}
