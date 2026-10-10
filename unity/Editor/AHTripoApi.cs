// Ashen Hollow (editor tool): talks to the Tripo API with the key the user saved in Keys/tripo.txt (outside Assets, so
// it is never built into the game or pushed anywhere). "Test: Tripo API Check" only asks for the account's credit
// balance, which costs nothing, and writes what came back to HeroShots/tripo_check.txt (the key itself is never written).
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class AHTripoApi
{
    static string ProjectDir { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }

    public static string Key(string who = "tripo")
    {
        foreach (var n in new[] { who + ".txt", who + ".txt.txt", who })
        {
            string p = Path.Combine(ProjectDir, "Keys", n);
            if (File.Exists(p)) { string k = File.ReadAllText(p).Trim().Trim('"', '\'', ' '); if (k.Length > 0) return k; }
        }
        return null;
    }

    [MenuItem("Ashen Hollow/Test: Tripo API Check %&j")]
    static void Check()
    {
        string key = Key(); string outp = Path.Combine(ProjectDir, "HeroShots", "tripo_check.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outp));
        if (key == null) { File.WriteAllText(outp, "no key found in Keys/tripo.txt\n"); Debug.Log("Ashen Hollow: no Tripo key found"); return; }
        string mkey = Key("meshy");
        var log = new StringBuilder();
        log.AppendLine("tripo key: " + key.Length + " characters; meshy key: " + (mkey == null ? "none" : mkey.Length + " characters"));
        var reqs = new List<UnityWebRequest>();
        System.Action<string, string> ask = (u, k) => { var r = UnityWebRequest.Get(u); r.SetRequestHeader("Authorization", "Bearer " + k); r.timeout = 25; r.SendWebRequest(); reqs.Add(r); };
        ask("https://api.tripo3d.ai/v2/openapi/user/balance", key);
        if (mkey != null) ask("https://api.meshy.ai/openapi/v1/balance", mkey);
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            foreach (var r in reqs) if (!r.isDone) return;
            EditorApplication.update -= cb;
            foreach (var r in reqs)
            {
                string body = r.downloadHandler != null ? r.downloadHandler.text : "";
                if (body != null) { body = body.Replace(key, "<key>"); if (mkey != null) body = body.Replace(mkey, "<key>"); }
                log.AppendLine(r.url + "  ->  HTTP " + r.responseCode + "  " + (r.error ?? "") + "\n" + body + "\n");
                r.Dispose();
            }
            File.WriteAllText(outp, log.ToString());
            Debug.Log("Ashen Hollow: Tripo API check written to " + outp);
        };
        EditorApplication.update += cb;
    }
}
