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
            case "snap":
                {
                    if (!Application.isPlaying) return "not playing";
                    Directory.CreateDirectory(Dir); ScreenCapture.CaptureScreenshot(Path.Combine(Dir, (a.Length > 0 ? a : "snap") + ".png")); return "ok";
                }
            case "aerial":
                {
                    // aerial <height> <back> <yaw>: the camera held over Hollow Meadow's town, looking at its middle
                    if (!Application.isPlaying || AHGame.I == null) return "not playing";
                    var f = a.Split(' '); float h = 70f, bk = 60f, yaw = 0f;
                    if (f.Length > 0) float.TryParse(f[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out h);
                    if (f.Length > 1) float.TryParse(f[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out bk);
                    if (f.Length > 2) float.TryParse(f[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out yaw);
                    Vector3 mid = AHHollow.Active && !(f.Length > 3 && f[3] == "hero") ? AHHollow.C : AHGame.I.player.transform.position;
                    AHGame.FreePos = mid + Quaternion.Euler(0f, yaw, 0f) * Vector3.back * bk + Vector3.up * h;
                    AHGame.FreeRot = Quaternion.LookRotation(mid - AHGame.FreePos); AHGame.FreeCam = true; return "ok";
                }
            case "freecam": AHGame.FreeCam = false; return "ok";
            case "show":
                {
                    // show <building id> [in]: the camera on a building's front (or, with the hero inside, over its room)
                    var g = AHGame.I; if (!Application.isPlaying || g == null) return "not playing";
                    var f = a.Split(' '); var hall = AHHollow.Halls.Find(x => x.id == f[0]); if (hall == null) return "no such building";
                    if (f.Length > 1 && f[1] == "in")
                    {
                        Vector3 to = hall.At(0f, hall.d * 0.35f); to.y = g.player.transform.position.y; g.player.transform.position = to;
                        AHGame.FreePos = hall.At(0f, -2.2f) + Vector3.up * 7.5f; AHGame.FreeRot = Quaternion.LookRotation(hall.At(0f, hall.d * 0.62f) - AHGame.FreePos);
                    }
                    else
                    {
                        Vector3 to = hall.Door; to.y = g.player.transform.position.y; g.player.transform.position = to;
                        AHGame.FreePos = hall.At(hall.w * 0.18f, -5.2f) + Vector3.up * 3.6f; AHGame.FreeRot = Quaternion.LookRotation(hall.At(0f, 1f) + Vector3.up * 2.2f - AHGame.FreePos);
                    }
                    AHGame.FreeCam = true; return "ok";
                }
            case "gateview":
                {
                    int n; int.TryParse(a, out n); Vector3 eye, at; if (!AHHollow.GateView(n, out eye, out at)) return "no such gate";
                    AHGame.FreePos = eye; AHGame.FreeRot = Quaternion.LookRotation(at - eye); AHGame.FreeCam = true; return "ok";
                }
            case "roadlamp":
                {
                    int n; int.TryParse(a, out n); if (n >= AHHollow.RoadLampsAt.Count) return "only " + AHHollow.RoadLampsAt.Count + " road lamps";
                    Vector3 lp = AHHollow.RoadLampsAt[n], d = lp - AHHollow.C; d.y = 0; d.Normalize();
                    AHGame.FreePos = lp + d * 9f + Vector3.up * 5f + Vector3.Cross(Vector3.up, d) * 4f; AHGame.FreeRot = Quaternion.LookRotation(lp + Vector3.up - AHGame.FreePos); AHGame.FreeCam = true; return "ok";
                }
            case "go":
                {
                    // go <building id> [in]: the hero to a building's door (or inside it)
                    var g = AHGame.I; if (!Application.isPlaying || g == null) return "not playing";
                    var f = a.Split(' '); var hall = AHHollow.Halls.Find(x => x.id == f[0]); if (hall == null) return "no such building";
                    bool inside = f.Length > 1 && f[1] == "in";
                    Vector3 to = inside ? hall.At(0f, hall.d * 0.3f) : hall.Door - hall.b * 3f; to.y = g.player.transform.position.y;
                    g.player.transform.position = to; g.camYaw = Quaternion.LookRotation(hall.b).eulerAngles.y; AHGame.FreeCam = false;
                    return "ok";
                }
            case "look":
                {
                    var g = AHGame.I; if (!Application.isPlaying || g == null) return "not playing";
                    var f = a.Split(' '); float y, p, d;
                    if (f.Length > 0 && float.TryParse(f[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)) g.camYaw = y;
                    if (f.Length > 1 && float.TryParse(f[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p)) g.camPitch = p;
                    if (f.Length > 2 && float.TryParse(f[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)) g.camDistance = d;
                    return "ok";
                }
        }
        return "unknown command";
    }
}
