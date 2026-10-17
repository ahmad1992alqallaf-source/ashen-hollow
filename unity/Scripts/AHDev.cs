// Ashen Hollow: everything that makes a build ready for friends to test.
//  · AHDev     — developer mode. Always on in the Unity editor. On a phone it is off, so testers never see the testing
//                rows (all mounts, all pets, free Crowns...). Tap the version button in Settings 7 times quickly to
//                turn it on for that phone (and off again the same way).
//  · AHBuildInfo — which build this is (written by Ashen Hollow > Build Android APK into Resources/AH/Data/build.txt).
//  · AHErrors  — keeps the last errors the game hit (and every crash-like exception in a small file), so a tester's
//                feedback carries them.
//  · AHFeedback — the note a tester sends: their words, the screenshot, the device, where they were and the errors.
//                On a phone it opens Android's share sheet (WhatsApp, Gmail, Telegram...); in the editor it saves to
//                Builds/feedback/.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class AHDev
{
    const string Key = "ah_dev_mode";
    public static bool On { get { return Application.isEditor || PlayerPrefs.GetInt(Key, 0) == 1; } }

    static int taps; static float lastTap;
    // the version button: 7 quick taps switch developer mode on this phone
    public static string Tap()
    {
        if (Time.unscaledTime - lastTap > 1.5f) taps = 0;
        lastTap = Time.unscaledTime; taps++;
        if (Application.isEditor) return taps >= 7 ? "Developer mode is always on in the editor" : null;
        if (taps >= 7)
        {
            taps = 0; bool now = PlayerPrefs.GetInt(Key, 0) == 0;
            PlayerPrefs.SetInt(Key, now ? 1 : 0); PlayerPrefs.Save();
            return now ? "Developer mode on: the testing rows are back" : "Developer mode off";
        }
        return taps >= 4 ? (7 - taps) + " more taps…" : null;
    }
}

public static class AHBuildInfo
{
    static bool read; static int code; static string date = "";
    static void Read()
    {
        if (read) return; read = true;
        var ta = Resources.Load<TextAsset>("AH/Data/build");
        if (ta == null) return;
        var parts = ta.text.Trim().Split('|');
        if (parts.Length > 0) int.TryParse(parts[0], out code);
        if (parts.Length > 1) date = parts[1];
    }
    public static int Code { get { Read(); return code; } }
    public static string Date { get { Read(); return date; } }
    public static string Version { get { return Application.version; } }
    // "0.9.12 (build 12) · tester"
    public static string Label { get { return Version + (Code > 0 ? " (build " + Code + ")" : "") + (Application.isEditor ? " · editor" : " · tester"); } }
}

public static class AHErrors
{
    public struct Entry { public string when, kind, msg, where; }
    static readonly List<Entry> last = new List<Entry>();
    static readonly object gate = new object();
    public static int Count;   // this session
    // read once on the main thread: the log can arrive from other threads, where Unity's own values may not be read
    static string FilePath = "", version = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        FilePath = Path.Combine(Application.persistentDataPath, "ah_errors.txt"); version = Application.version;
        Application.logMessageReceivedThreaded -= OnLog;
        Application.logMessageReceivedThreaded += OnLog;
    }

    static void OnLog(string msg, string stack, LogType type)
    {
        if (type == LogType.Log || type == LogType.Warning) return;
        var e = new Entry { when = DateTime.Now.ToString("HH:mm:ss"), kind = type.ToString(), msg = msg ?? "", where = FirstLine(stack) };
        lock (gate)
        {
            Count++;
            // the same error every frame counts once in the list
            if (last.Count > 0 && last[last.Count - 1].msg == e.msg) return;
            last.Add(e); if (last.Count > 30) last.RemoveAt(0);
        }
        if (type == LogType.Exception && FilePath.Length > 0)
            try
            {
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + version + " " + e.msg + " @ " + e.where + "\n";
                var fi = new FileInfo(FilePath);
                if (fi.Exists && fi.Length > 40000) File.WriteAllText(FilePath, "");   // kept small
                File.AppendAllText(FilePath, line);
            }
            catch { }
    }

    static string FirstLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int i = s.IndexOf('\n'); return (i < 0 ? s : s.Substring(0, i)).Trim();
    }

    public static string Recent(int n)
    {
        var sb = new StringBuilder();
        lock (gate)
            for (int i = Mathf.Max(0, last.Count - n); i < last.Count; i++)
                sb.AppendLine(last[i].when + " " + last[i].kind + ": " + Cut(last[i].msg, 220) + (last[i].where.Length > 0 ? "  @ " + Cut(last[i].where, 120) : ""));
        return sb.ToString();
    }
    // exceptions from earlier sessions (a crash ends a session before anyone can send feedback)
    public static string Earlier(int lines)
    {
        try
        {
            if (FilePath.Length == 0 || !File.Exists(FilePath)) return "";
            var all = File.ReadAllLines(FilePath); var sb = new StringBuilder();
            for (int i = Mathf.Max(0, all.Length - lines); i < all.Length; i++) sb.AppendLine(Cut(all[i], 260));
            return sb.ToString();
        }
        catch { return ""; }
    }
    static string Cut(string s, int n) { return s.Length <= n ? s : s.Substring(0, n) + "…"; }
}

public static class AHFeedback
{
    public static readonly string[] Kinds = { "Bug", "Idea", "Balance", "Looks" };

    public static string Report(AHGame g, string kind, string note)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ashen Hollow feedback · " + kind);
        sb.AppendLine(string.IsNullOrEmpty(note) ? "(no note)" : note);
        sb.AppendLine();
        sb.AppendLine("Version " + AHBuildInfo.Label + (AHBuildInfo.Date.Length > 0 ? " · built " + AHBuildInfo.Date : ""));
        sb.AppendLine("Device " + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem);
        sb.AppendLine("Memory " + SystemInfo.systemMemorySize + " MB · " + SystemInfo.graphicsDeviceName + " · screen " + Screen.width + "x" + Screen.height + " · " + Mathf.RoundToInt(1f / Mathf.Max(0.001f, Time.smoothDeltaTime)) + " fps");
        var p = g != null ? g.player : null;
        if (p != null)
        {
            var pos = p.transform.position;
            sb.AppendLine("Hero " + (string.IsNullOrEmpty(p.heroName) ? "?" : p.heroName) + " · " + p.cls.name + " level " + p.level + " · path " + p.path);
            sb.AppendLine("Area " + AHGame.AreaId + " at " + pos.x.ToString("0") + ", " + pos.y.ToString("0") + ", " + pos.z.ToString("0"));
        }
        sb.AppendLine("Device ID " + AHPrefs.DeviceId.Substring(0, 8).ToUpperInvariant() + " · playing for " + Mathf.RoundToInt(Time.realtimeSinceStartup / 60f) + " min");
        string now = AHErrors.Recent(10), before = AHErrors.Earlier(5);
        if (now.Length > 0) { sb.AppendLine(); sb.AppendLine("Errors this session (" + AHErrors.Count + "):"); sb.Append(now); }
        if (before.Length > 0) { sb.AppendLine(); sb.AppendLine("Earlier crashes:"); sb.Append(before); }
        return sb.ToString();
    }

    // sends the note and the picture; returns where it went (a line for the toast)
    public static string Send(string text, byte[] png)
    {
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        if (Application.isEditor || Application.platform != RuntimePlatform.Android)
        {
            string dir = Application.isEditor ? Path.Combine(Application.dataPath, "../Builds/feedback") : Path.Combine(Application.persistentDataPath, "feedback");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "feedback_" + stamp + ".txt"), text);
            if (png != null) File.WriteAllBytes(Path.Combine(dir, "feedback_" + stamp + ".png"), png);
            return "Saved to " + (Application.isEditor ? "Builds/feedback" : dir);
        }
        return ShareAndroid(text, png, "ashen_feedback_" + stamp + ".png");
    }

    static string ShareAndroid(string text, byte[] png, string name)
    {
        try
        {
            using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var act = up.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject uri = null;
                if (png != null)
                    try { uri = SavePicture(act, png, name); }
                    catch (Exception e) { Debug.LogWarning("Ashen Hollow: feedback picture not saved: " + e.Message); }
                var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("setType", uri != null ? "image/png" : "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", "Ashen Hollow feedback");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text);
                if (uri != null)
                {
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
                    intent.Call<AndroidJavaObject>("addFlags", 1);   // FLAG_GRANT_READ_URI_PERMISSION
                }
                using (var ic = new AndroidJavaClass("android.content.Intent"))
                {
                    var chooser = ic.CallStatic<AndroidJavaObject>("createChooser", intent, "Send your feedback");
                    act.Call("startActivity", chooser);
                }
                return uri != null ? "Pick WhatsApp, Gmail or any app to send it" : "Pick an app to send it (the picture could not be added)";
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Ashen Hollow: feedback share failed: " + e.Message);
            GUIUtility.systemCopyBuffer = text;
            return "Sharing failed: the note is copied, paste it in a message";
        }
    }

    // the screenshot goes into the phone's Pictures/AshenHollow (Android 10 and newer need no permission for this)
    static AndroidJavaObject SavePicture(AndroidJavaObject act, byte[] png, string name)
    {
        var resolver = act.Call<AndroidJavaObject>("getContentResolver");
        var values = new AndroidJavaObject("android.content.ContentValues");
        values.Call("put", "_display_name", name);
        values.Call("put", "mime_type", "image/png");
        values.Call("put", "relative_path", "Pictures/AshenHollow");
        AndroidJavaObject where;
        using (var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media")) where = media.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
        var uri = resolver.Call<AndroidJavaObject>("insert", where, values);
        if (uri == null) return null;
        using (var os = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
        {
            os.Call("write", (sbyte[])(Array)png);
            os.Call("close");
        }
        return uri;
    }
}
