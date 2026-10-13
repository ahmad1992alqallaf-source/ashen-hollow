// Ashen Hollow (editor test): "Test: Bug Sweep" (shift+alt+X). Goes through every area and in each one:
//  - opens every window of the game in turn (and pages through it), noting any that throws
//  - sets the hero down at 40 spots across the land, as moving there would (Resolve), noting any left in deep water,
//    outside the land or on top of something solid
// HeroShots/sweep.txt lists what went wrong; errors thrown on their own go to HeroShots/errors.txt (AHErrorLog).
// HeroShots/sweep_only.txt ("meadow,tide") limits the areas.
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHSweep
{
    static readonly string[] Areas = { "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead", "abyss" };
    static readonly string[] Modes = { "ah", "art", "bank", "board", "class", "coll", "comp", "cookoff", "daily", "deeds", "deephall", "derby", "dye", "emotes", "enh", "events", "fest", "finder", "friends", "furnish", "gems", "house", "hub", "kq", "map", "mq", "night", "orders", "pb", "rep", "settings", "stats", "story", "tales", "talsets", "ward", "ways", "craft", "farm", "loadouts", "prof", "world", "petinfo" };
    static StringBuilder log;

    [MenuItem("Ashen Hollow/Test: Bug Sweep #&x")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string[] list = Areas; string only = Path.Combine(Application.dataPath, "../HeroShots/sweep_only.txt");
        if (File.Exists(only)) { var l = File.ReadAllText(only).Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries); if (l.Length > 0) list = l; }
        log = new StringBuilder();
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            var gg = AHGame.I;
            if (!Application.isPlaying || gg == null) { EditorApplication.update -= cb; Save(); return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 9)) { if (t - t0 > 60) { log.AppendLine(want + ": could not get there"); want = null; } return; }
            if (want != null) { Windows(gg); Walk(gg); Save(); }
            i++;
            if (i >= list.Length) { EditorApplication.update -= cb; Save(); Debug.Log("Ashen Hollow: bug sweep done (HeroShots/sweep.txt)"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = list[i]; t0 = t;
            if (AHGame.AreaId != want) gg.Travel(want, d.spawn.x, d.spawn.z, 0f); else t0 -= 9;
        };
        EditorApplication.update += cb;
    }

    static void Save() { if (log != null) File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/sweep.txt"), log.ToString()); }

    static void Try(string what, System.Action a)
    {
        try { a(); }
        catch (System.Exception e)
        {
            var x = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
            string st = x.StackTrace ?? ""; var lines = st.Split('\n');
            log.AppendLine(AHGame.AreaId + ": " + what + " THREW " + x.GetType().Name + ": " + x.Message + "\n    " + string.Join("\n    ", lines, 0, Mathf.Min(4, lines.Length)).Trim());
        }
    }

    static void Windows(AHGame g)
    {
        var ui = g.ui; if (ui == null) return; var T = typeof(AHUI);
        var fMode = T.GetField("wkMode", BindingFlags.NonPublic | BindingFlags.Instance);
        var fPage = T.GetField("wkPageI", BindingFlags.NonPublic | BindingFlags.Instance);
        var mRender = T.GetMethod("RenderWork", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var m in Modes)
        {
            Try("window " + m, () =>
            {
                fMode.SetValue(ui, m); fPage.SetValue(ui, 0); ui.ShowWork(true); mRender.Invoke(ui, null);
                for (int k = 0; k < 2; k++) { fPage.SetValue(ui, k + 1); mRender.Invoke(ui, null); }
            });
            Try("close " + m, () => ui.ShowWork(false));
        }
        Try("bag", () => { ui.ShowBag(true); ui.ShowBag(false); });
        Try("quests", () => { ui.OpenQuest(); ui.ShowWork(false); });
        Try("professions", () => { ui.OpenProf(); ui.ShowWork(false); });
        Try("companions", () => { ui.OpenComp(); ui.ShowWork(false); });
        Try("wardrobe", () => { ui.OpenWardrobe(); ui.ShowWork(false); });
        Try("settings", () => { ui.OpenSettings(); ui.ShowWork(false); });
    }

    // well inside the painted sea (not just its shore): wet two metres round
    static bool WetCore(Vector3 q) { return AHSea.Sea(q + new Vector3(2f, 0f, 0f)) && AHSea.Sea(q - new Vector3(2f, 0f, 0f)) && AHSea.Sea(q + new Vector3(0f, 0f, 2f)) && AHSea.Sea(q - new Vector3(0f, 0f, 2f)); }

    static void Walk(AHGame g)
    {
        var R = g.WalkArea; var p = g.player.transform; Vector3 home = p.position; int bad = 0;
        var rnd = new System.Random(AHGame.AreaId.GetHashCode());
        for (int k = 0; k < 40; k++)
        {
            Vector3 want = new Vector3(R.xMin + (float)rnd.NextDouble() * R.width, home.y, R.yMin + (float)rnd.NextDouble() * R.height);
            Vector3 got = Vector3.zero; bool ok = true;
            Try("resolve", () => got = g.Resolve(want, 0.4f));
            if (AHSea.Deep(got)) { bad++; log.AppendLine(AHGame.AreaId + ": walked into deep water at " + got.ToString("F1")); ok = false; }
            if (got.x < R.xMin - 0.1f || got.x > R.xMax + 0.1f || got.z < R.yMin - 0.1f || got.z > R.yMax + 0.1f) { bad++; log.AppendLine(AHGame.AreaId + ": left the land at " + got.ToString("F1")); ok = false; }
            foreach (var b in g.Blocks) if (b.Contains(new Vector2(got.x, got.z)) && b.width > 0.6f && b.height > 0.6f) { bad++; log.AppendLine(AHGame.AreaId + ": stands inside a solid block at " + got.ToString("F1")); ok = false; break; }
            if (!ok) continue;
        }
        p.position = home;
        log.AppendLine(AHGame.AreaId + ": walk check " + (40 - bad) + "/40 fine");
        // the web game's own tile map (where an area has one) against the painted sea: land you could walk on there
        // must not have become deep water here
        var mOk = typeof(AHGame).GetMethod("TilesOk", BindingFlags.NonPublic | BindingFlags.Instance);
        var fT = typeof(AHGame).GetField("tiles", BindingFlags.NonPublic | BindingFlags.Instance);
        if (mOk != null && fT != null && fT.GetValue(g) != null && AHSea.On)
        {
            int land = 0, wet = 0; var sb = new StringBuilder();
            for (float x = R.xMin + 1f; x < R.xMax; x += 2f)
                for (float z = R.yMin + 1f; z < R.yMax; z += 2f)
                {
                    var q = new Vector3(x, 0f, z);
                    if (!(bool)mOk.Invoke(g, new object[] { q, 0.4f })) continue;
                    land++; if (AHSea.Sea(q) && WetCore(q)) { wet++; if (wet <= 12) sb.Append(" " + q.ToString("F0")); }
                }
            // a picture of the two, a pixel a metre, north up: green land on both, red land under the painted sea,
            // blue sea on both, dark the tile map's water on dry painted ground
            int W = Mathf.CeilToInt(R.width), H = Mathf.CeilToInt(R.height); var pic = new Texture2D(W, H, TextureFormat.RGB24, false);
            for (int z = 0; z < H; z++)
                for (int x = 0; x < W; x++)
                {
                    var q = new Vector3(R.xMin + x + 0.5f, 0f, R.yMin + z + 0.5f); bool tl = (bool)mOk.Invoke(g, new object[] { q, 0.2f }), sw = AHSea.Sea(q);
                    pic.SetPixel(x, z, tl ? (sw ? Color.red : new Color(0.3f, 0.8f, 0.3f)) : (sw ? new Color(0.2f, 0.4f, 0.9f) : new Color(0.15f, 0.15f, 0.15f)));
                }
            pic.Apply(); File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/" + AHGame.AreaId + "_tiles.png"), pic.EncodeToPNG()); Object.DestroyImmediate(pic);
            log.AppendLine(AHGame.AreaId + ": tile map land " + land + " spots, " + wet + " of them under the painted sea" + (wet > 0 ? ":" + sb : ""));
        }
    }
}
