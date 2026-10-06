// Ashen Hollow: the Dreamscape Nature: Meadows pieces the game uses (Polyart Studio, Unity Asset Store), gathered by
// the editor (Ashen Hollow → Dreamscape: Build Set) into Resources/AH/Dreamscape/set.asset so the game can load them.
using UnityEngine;

public class AHDreamSet : ScriptableObject
{
    public GameObject[] trees, birches, bushes, flowers, flowerFields, grass, rocks, smallRocks, mushrooms;
    public GameObject stump, fallen;
    static AHDreamSet inst; static bool tried;
    public static AHDreamSet Get() { if (!tried) { tried = true; inst = Resources.Load<AHDreamSet>("AH/Dreamscape/set"); } return inst; }
    public static bool UseIn(string area) { return AHPrefs.GetInt("ah_dreamscape", 1) == 1 && Get() != null && (area == "meadow"); }
}
