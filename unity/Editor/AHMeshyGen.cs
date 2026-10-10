// Ashen Hollow (editor tool): makes 3D models (and missing concept pictures) with the Meshy API, using the key the user
// saved in Keys/meshy.txt (outside Assets: never built into the game or pushed anywhere).
// "Make: Meshy Models From Job List" reads Incoming/meshy_jobs.txt, one job per line:
//     3D model:  name | multi or single | pose (t-pose or none) | picture;picture;...      -> Incoming/<name>.glb
//     picture:   name | img | out picture | reference;reference;... | what to draw           -> Incoming/Concepts/<out picture>
// (all pictures relative to Incoming/Concepts). A job whose pictures are not there yet waits for the jobs that draw
// them. Progress, credits used and any errors go to HeroShots/meshy_status.txt. Started tasks are remembered in
// Library/meshy_tasks.txt, so a script reload carries on waiting for them instead of paying for them again. A job whose
// result already exists is skipped.
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

[InitializeOnLoad]
public static class AHMeshyGen
{
    const string Api = "https://api.meshy.ai/openapi/v1/";
    static string Dir { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
    static string Incoming { get { return Path.Combine(Dir, "Incoming"); } }
    static string Concepts { get { return Path.Combine(Incoming, "Concepts"); } }
    static string StatePath { get { return Path.Combine(Dir, "Library", "meshy_tasks.txt"); } }
    static string StatusPath { get { return Path.Combine(Dir, "HeroShots", "meshy_status.txt"); } }

    class Job
    {
        public string name, mode, pose, outPic, prompt; public string[] pics = new string[0];
        public string id, state = "new", note = ""; public int progress, credits; public UnityWebRequest req; public double next;
        public bool Img { get { return mode == "img"; } }
        public string Result { get { return Img ? Path.Combine(Concepts, outPic) : Path.Combine(Incoming, name + ".glb"); } }
    }
    static readonly List<Job> jobs = new List<Job>();
    static bool running; static double lastStatus;

    static AHMeshyGen() { EditorApplication.delayCall += Resume; }

    // carry on with tasks started before a script reload (the rest of the list is read again from the job file)
    static void Resume()
    {
        if (!File.Exists(StatePath) || File.ReadAllText(StatePath).Trim().Length == 0) return;
        Run();
    }

    [MenuItem("Ashen Hollow/Make: Meshy Models From Job List %&#m")]
    static void Run()
    {
        if (running) { Debug.Log("Ashen Hollow: Meshy jobs are already running"); return; }
        string list = Path.Combine(Incoming, "meshy_jobs.txt");
        if (!File.Exists(list)) { Debug.Log("Ashen Hollow: no Incoming/meshy_jobs.txt"); return; }
        var known = new Dictionary<string, string>();
        if (File.Exists(StatePath)) foreach (var l in File.ReadAllLines(StatePath)) { var p = l.Split('|'); if (p.Length >= 3) known[p[0]] = p[2]; }
        jobs.Clear();
        foreach (var raw in File.ReadAllLines(list))
        {
            var line = raw.Trim(); if (line.Length == 0 || line.StartsWith("#")) continue;
            var p = line.Split('|'); if (p.Length < 4) continue;
            var j = new Job { name = p[0].Trim(), mode = p[1].Trim() };
            if (j.Img) { if (p.Length < 5) continue; j.outPic = p[2].Trim(); j.pics = p[3].Split(';'); j.prompt = p[4].Trim(); }
            else { j.pose = p[2].Trim(); j.pics = p[3].Split(';'); }
            for (int i = 0; i < j.pics.Length; i++) j.pics[i] = j.pics[i].Trim();
            if (File.Exists(j.Result)) continue;
            string id; if (known.TryGetValue(j.name, out id)) { j.id = id; j.state = "waiting"; }
            jobs.Add(j);
        }
        Debug.Log("Ashen Hollow: " + jobs.Count + " Meshy jobs to do");
        if (jobs.Count > 0) { running = true; EditorApplication.update -= Tick; EditorApplication.update += Tick; }
    }

    static string Endpoint(Job j) { return Api + (j.Img ? "image-to-image" : j.mode == "multi" ? "multi-image-to-3d" : "image-to-3d"); }

    static string DataUri(string rel)
    {
        string p = Path.Combine(Concepts, rel);
        string mime = rel.ToLower().EndsWith(".png") ? "image/png" : "image/jpeg";
        return "data:" + mime + ";base64," + System.Convert.ToBase64String(File.ReadAllBytes(p));
    }
    static string Esc(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\""); }

    static string Body(Job j)
    {
        var b = new StringBuilder("{");
        if (j.Img)
        {
            b.Append("\"ai_model\":\"nano-banana-pro\",\"prompt\":\"").Append(Esc(j.prompt)).Append("\",\"reference_image_urls\":[");
            for (int i = 0; i < j.pics.Length && i < 5; i++) { if (i > 0) b.Append(','); b.Append('"').Append(DataUri(j.pics[i])).Append('"'); }
            b.Append("],\"aspect_ratio\":\"1:1\"}");
            return b.ToString();
        }
        if (j.mode == "multi")
        {
            b.Append("\"image_urls\":[");
            for (int i = 0; i < j.pics.Length && i < 4; i++) { if (i > 0) b.Append(','); b.Append('"').Append(DataUri(j.pics[i])).Append('"'); }
            b.Append("],");
        }
        else b.Append("\"image_url\":\"").Append(DataUri(j.pics[0])).Append("\",");
        // "t-pose+remesh": Meshy rebuilds the mesh itself at a game-friendly size (for very heavy, ragged cloth that
        // would tear when shrunk here)
        bool remesh = j.pose.Contains("remesh"); string pose = j.pose.Replace("+remesh", "");
        b.Append("\"ai_model\":\"latest\",\"should_texture\":true,\"texture_resolution\":\"2k\",\"enable_pbr\":false,\"target_formats\":[\"glb\"]");
        b.Append(remesh ? ",\"should_remesh\":true,\"topology\":\"triangle\",\"target_polycount\":45000" : ",\"should_remesh\":false");
        if (pose == "t-pose" || pose == "a-pose") b.Append(",\"pose_mode\":\"").Append(pose).Append('"');
        b.Append('}');
        return b.ToString();
    }

    static UnityWebRequest Req(string method, string url, string key, string body)
    {
        var r = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 120 };
        if (body != null) { r.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)); r.SetRequestHeader("Content-Type", "application/json"); }
        r.SetRequestHeader("Authorization", "Bearer " + key);
        r.SendWebRequest(); return r;
    }

    static void SaveState()
    {
        var sb = new StringBuilder();
        foreach (var j in jobs) if (!string.IsNullOrEmpty(j.id) && j.state != "done" && j.state != "failed") sb.AppendLine(j.name + "|" + j.mode + "|" + j.id);
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)); File.WriteAllText(StatePath, sb.ToString());
    }

    static void WriteStatus()
    {
        var sb = new StringBuilder(); int spent = 0, done = 0;
        foreach (var j in jobs) { spent += j.credits; if (j.state == "done") done++; sb.AppendLine(j.name + ": " + j.state + (j.progress > 0 && j.state != "done" ? " " + j.progress + "%" : "") + (j.credits > 0 ? " (" + j.credits + " credits)" : "") + (j.note.Length > 0 ? "  " + j.note : "")); }
        sb.AppendLine(done + " of " + jobs.Count + " done, credits used: " + spent);
        Directory.CreateDirectory(Path.GetDirectoryName(StatusPath)); File.WriteAllText(StatusPath, sb.ToString());
    }

    static bool Ready(Job j) { foreach (var p in j.pics) if (!File.Exists(Path.Combine(Concepts, p))) return false; return true; }

    static void Tick()
    {
        string key = AHTripoApi.Key("meshy"); if (key == null) { Debug.Log("Ashen Hollow: no Meshy key"); EditorApplication.update -= Tick; running = false; return; }
        double now = EditorApplication.timeSinceStartup; bool busy = false, changed = false, active = false; int creating = 0;
        foreach (var j in jobs) if (j.req != null && j.state == "creating") creating++;
        foreach (var j in jobs)
        {
            if (j.state == "done" || j.state == "failed") continue; busy = true;
            if (j.state != "new") active = true;
            if (j.req != null)
            {
                if (!j.req.isDone) continue;
                var r = j.req; j.req = null; changed = true;
                string txt = r.downloadHandler is DownloadHandlerBuffer ? r.downloadHandler.text : "";
                if (j.state == "creating")
                {
                    creating--;
                    var m = Regex.Match(txt ?? "", "\"result\"\\s*:\\s*\"([^\"]+)\"");
                    if (r.responseCode == 200 || r.responseCode == 202) { if (m.Success) { j.id = m.Groups[1].Value; j.state = "waiting"; j.note = ""; SaveState(); } else { j.state = "failed"; j.note = "no task id: " + Short(txt); } }
                    else if (r.responseCode == 429 || r.responseCode >= 500 || r.responseCode == 0) { j.state = "new"; j.next = now + 30; j.note = "busy, trying again (HTTP " + r.responseCode + ")"; }
                    else { j.state = "failed"; j.note = "HTTP " + r.responseCode + " " + Short(txt); }
                }
                else if (j.state == "waiting")
                {
                    if (r.responseCode != 200) { j.note = "HTTP " + r.responseCode; j.next = now + 20; r.Dispose(); continue; }
                    string st = Field(txt, "status"); int.TryParse(Num(txt, "progress"), out j.progress); int.TryParse(Num(txt, "consumed_credits"), out j.credits);
                    if (st == "SUCCEEDED")
                    {
                        Match g = j.Img ? Regex.Match(txt, "\"image_urls\"\\s*:\\s*\\[\\s*\"([^\"]+)\"") : Regex.Match(txt, "\"model_urls\"\\s*:\\s*\\{[^}]*?\"glb\"\\s*:\\s*\"([^\"]+)\"");
                        if (!g.Success) { j.state = "failed"; j.note = "no result file"; }
                        else
                        {
                            string url = Regex.Unescape(g.Groups[1].Value); string tmp = j.Result + ".part";
                            Directory.CreateDirectory(Path.GetDirectoryName(tmp));
                            j.req = new UnityWebRequest(url, "GET") { downloadHandler = new DownloadHandlerFile(tmp) { removeFileOnAbort = true }, timeout = 600 };
                            j.req.SendWebRequest(); j.state = "downloading";
                        }
                    }
                    else if (st == "FAILED" || st == "CANCELED") { j.state = "failed"; j.note = Short(Field(txt, "message")); SaveState(); }
                    else j.next = now + 15;
                }
                else if (j.state == "downloading")
                {
                    string tmp = j.Result + ".part", fin = j.Result;
                    if (r.responseCode == 200 && File.Exists(tmp)) { if (File.Exists(fin)) File.Delete(fin); File.Move(tmp, fin); j.state = "done"; SaveState(); Debug.Log("Ashen Hollow: Meshy result saved: " + j.name); }
                    else { j.state = "waiting"; j.next = now + 20; j.note = "download failed, retrying"; }
                }
                r.Dispose(); continue;
            }
            if (now < j.next) continue;
            if (j.state == "new")
            {
                if (creating >= 2 || !Ready(j)) continue;   // a couple at a time, and only once its pictures are there
                try { j.req = Req("POST", Endpoint(j), key, Body(j)); j.state = "creating"; creating++; active = true; }
                catch (System.Exception e) { j.state = "failed"; j.note = e.Message; }
                changed = true;
            }
            else if (j.state == "waiting") j.req = Req("GET", Endpoint(j) + "/" + j.id, key, null);
        }
        // nothing on the way and the rest still waiting for pictures that will never come: give up on those
        if (busy && !active)
        {
            bool any = false; foreach (var j in jobs) if (j.state == "new" && (Ready(j) || now < j.next)) any = true;
            if (!any) { foreach (var j in jobs) if (j.state == "new") { j.state = "failed"; j.note = "its pictures were not made"; } changed = true; busy = false; }
        }
        if (changed || now - lastStatus > 20) { WriteStatus(); lastStatus = now; }
        if (!busy) { WriteStatus(); SaveState(); EditorApplication.update -= Tick; running = false; Debug.Log("Ashen Hollow: Meshy jobs finished"); }
    }

    static string Short(string s) { if (string.IsNullOrEmpty(s)) return ""; s = s.Replace("\n", " "); return s.Length > 200 ? s.Substring(0, 200) : s; }
    static string Field(string txt, string f) { var m = Regex.Match(txt ?? "", "\"" + f + "\"\\s*:\\s*\"([^\"]*)\""); return m.Success ? m.Groups[1].Value : ""; }
    static string Num(string txt, string f) { var m = Regex.Match(txt ?? "", "\"" + f + "\"\\s*:\\s*(\\d+)"); return m.Success ? m.Groups[1].Value : "0"; }
}
