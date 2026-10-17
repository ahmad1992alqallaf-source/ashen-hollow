// Ashen Hollow test: the tester pack (ctrl+shift+alt+F7, in Play mode). Developer mode, the version, the error log,
// the feedback note (saved to Builds/feedback in the editor) and the two windows, with pictures in HeroShots/tester/.
// Writes HeroShots/testerpack.txt.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHTesterPackTest
{
    static List<string> log; static string dir; static int step; static double nextAt;

    [MenuItem("Ashen Hollow/Test: Tester Pack Check %#&F7")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { Debug.Log("Ashen Hollow: press Play first"); return; }
        dir = Path.Combine(Application.dataPath, "../HeroShots/tester"); Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
        log = new List<string>(); step = 0; nextAt = EditorApplication.timeSinceStartup + 0.3;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }

    static void Ok(bool b, string what) { log.Add((b ? "OK   " : "FAIL ") + what); }
    static void Shot(string n) { ScreenCapture.CaptureScreenshot(Path.Combine(dir, n + ".png")); }
    static string Mode(AHUI ui) { var f = typeof(AHUI).GetField("wkMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); return f != null ? (string)f.GetValue(ui) : "?"; }

    static void Tick()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < nextAt) return;
        double wait = 1.0;
        try
        {
            switch (step)
            {
                case 0:
                    Ok(AHDev.On, "developer mode is on in the editor");
                    Ok(AHComp.TestMounts && (AHStore.Test || !AHJson.B(AHDB.File("store"), "TEST")), "the testing rows follow developer mode");
                    Ok(!string.IsNullOrEmpty(AHBuildInfo.Version), "version label: " + AHBuildInfo.Label);
                    Ok(Resources.Load<TextAsset>("AH/Data/whatsnew") != null, "whatsnew.txt is in the game");
                    int before = AHErrors.Count; Debug.LogError("Ashen Hollow test: a pretend error for the feedback note");
                    Ok(AHErrors.Count == before + 1 && AHErrors.Recent(3).Contains("pretend error"), "the error log caught an error");
                    string rep = AHFeedback.Report(g, "Bug", "test note from the tester pack check");
                    Ok(rep.Contains("test note") && rep.Contains(SystemInfo.deviceModel) && rep.Contains("Area " + AHGame.AreaId) && rep.Contains("pretend error"), "the feedback note has the words, device, place and errors");
                    log.Add("INFO note:\n" + rep);
                    g.ui.OpenSettings(); wait = 1.0; break;
                case 1: Shot("1_settings"); Ok(Mode(g.ui) == "settings", "Settings opens with the tester rows"); g.ui.OpenWhatsNew(); break;
                case 2: Shot("2_whatsnew"); Ok(Mode(g.ui) == "whatsnew", "What's new opens"); g.ui.OpenFeedback(); wait = 1.5; break;
                case 3:
                    Shot("3_feedback"); Ok(Mode(g.ui) == "feedback" && g.ui.WorkOpen, "Send feedback opens after taking the picture");
                    var png = typeof(AHUI).GetField("fbPng", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(g.ui) as byte[];
                    Ok(png != null && png.Length > 1000, "the screen picture was taken (" + (png != null ? png.Length / 1024 : 0) + " KB)");
                    string fbDir = Path.Combine(Application.dataPath, "../Builds/feedback");
                    int n0 = Directory.Exists(fbDir) ? Directory.GetFiles(fbDir).Length : 0;
                    string where = AHFeedback.Send(AHFeedback.Report(g, "Idea", "saved by the tester pack check"), png);
                    int n1 = Directory.Exists(fbDir) ? Directory.GetFiles(fbDir).Length : 0;
                    Ok(n1 >= n0 + 2, "the note and picture were saved: " + where);
                    g.ui.ShowWork(false); break;
                default:
                    EditorApplication.update -= Tick;
                    File.WriteAllLines(Path.Combine(dir, "../testerpack.txt"), log.ToArray());
                    Debug.Log("Ashen Hollow: tester pack check done · " + log.FindAll(l => l.StartsWith("FAIL")).Count + " fails · HeroShots/testerpack.txt");
                    return;
            }
        }
        catch (Exception e) { log.Add("FAIL step " + step + " threw " + e.GetType().Name + ": " + e.Message); Debug.LogException(e); }
        step++; nextAt = EditorApplication.timeSinceStartup + wait;
    }
}
