// Ashen Hollow (editor): keyboard shortcuts for test menu items that have none (so they can be run without the menus)
using UnityEditor;

public static class AHQuick
{
    [MenuItem("Ashen Hollow/Quick/All Monsters Check %&#a")]
    static void AllMobs() { EditorApplication.ExecuteMenuItem("Ashen Hollow/Test: All Monsters Check"); }
    [MenuItem("Ashen Hollow/Quick/Beast and Mount Parade %&#b")]
    static void Parade() { EditorApplication.ExecuteMenuItem("Ashen Hollow/Test: Beast and Mount Parade"); }
}
