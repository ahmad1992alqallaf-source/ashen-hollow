// Ashen Hollow test: "Test: Armour Check" (ctrl+shift+alt+F10) photographs one collection's suits as the game wears
// them (the Collection Suit Shots, for the collection named in Only below); used to compare suits before and after
// they were made lighter for phones. Pictures go to HeroShots/suits.
using System.Reflection;
using UnityEditor;

public static class AHArmourShots
{
    public static string Only = "demonic";
    [MenuItem("Ashen Hollow/Test: Armour Check %#&F10")]
    static void Run()
    {
        AHSuitShots.Only = Only;
        typeof(AHSuitShots).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
}
