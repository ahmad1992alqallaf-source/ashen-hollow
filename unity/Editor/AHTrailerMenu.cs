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

    // each filmed shot packed into one zip (HeroShots/trailer/zips/<shot>.zip), to carry the frames off the PC in a few files
    [MenuItem("Ashen Hollow/Test: Pack Trailer Frames #&k")]
    static void Pack()
    {
        string dir = Path.Combine(Application.dataPath, "../HeroShots/trailer"), zd = Path.Combine(dir, "zips"); Directory.CreateDirectory(zd);
        int n = 0;
        foreach (var sd in Directory.GetDirectories(dir))
        {
            string name = Path.GetFileName(sd); if (name == "zips") continue;
            string zp = Path.Combine(zd, name + ".zip"); if (File.Exists(zp)) File.Delete(zp);
            System.IO.Compression.ZipFile.CreateFromDirectory(sd, zp, System.IO.Compression.CompressionLevel.NoCompression, false); n++;
        }
        Debug.Log("Ashen Hollow: " + n + " trailer shots packed into " + Path.GetFullPath(zd));
    }
}
