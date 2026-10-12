// Ashen Hollow (editor): "Test: Record Trailer" (shift+alt+T) films the gameplay trailer's shots (AHTrailer) into
// HeroShots/trailer. HeroShots/trailer/only.txt with a shot's name films just that one again.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHTrailerMenu
{
    [MenuItem("Ashen Hollow/Test: Record Trailer #&t")]
    static void Run()
    {
        if (!Application.isPlaying || AHGame.I == null) { Debug.Log("Ashen Hollow: the trailer needs Play mode (in the game)"); return; }
        string dir = Path.Combine(Application.dataPath, "../HeroShots/trailer"); Directory.CreateDirectory(dir);
        string of = Path.Combine(dir, "only.txt"); AHTrailer.Only = File.Exists(of) ? File.ReadAllText(of).Trim() : null; if (AHTrailer.Only == "") AHTrailer.Only = null;
        AHTrailer.Begin(dir);
    }
}
