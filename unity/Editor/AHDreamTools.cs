// Ashen Hollow: gathers the Dreamscape Meadows prefabs the game uses into Resources/AH/Dreamscape/set.asset.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AHDreamTools
{
    const string Root = "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Prefabs/";
    [MenuItem("Ashen Hollow/Dreamscape: Build Set %&3")]
    static void Build()
    {
        var s = ScriptableObject.CreateInstance<AHDreamSet>();
        s.trees = Load("Trees/Prefab_TreeLarge_0", 1, 4);
        s.birches = Load("Trees/Prefab_Birch_0", 1, 5);
        s.bushes = Load("Foliage/Prefab_Bush_0", 1, 9);
        s.flowers = Load("Foliage/Prefab_Flower_0", 1, 4);
        s.flowerFields = Load("Foliage/Prefab_FlowerField_0", 1, 2);
        s.mushrooms = Load("Foliage/Prefab_Mushroom_0", 1, 6);
        s.grass = new[] { L("Grass/Prefab_Grass_Group_01"), L("Grass/Prefab_Grass_Group_02"), L("Grass/Prefab_Grass_01"), L("Grass/Prefab_Grass_02"), L("Grass/Prefab_Grass_03") };
        s.rocks = Load("Rocks/Prefab_RocksRound_0", 1, 3);
        s.smallRocks = new[] { L("Rocks/Prefab_SmallRock_01"), L("Rocks/Prefab_SmallRock_02"), L("Rocks/Prefab_SmallRock_03"), L("Rocks/Prefab_StoneSmall_01"), L("Rocks/Prefab_StoneSmall_02"), L("Rocks/Prefab_StoneSmall_03") };
        s.stump = L("Props/Prefab_TreeStump_01"); s.fallen = L("Props/Prefab_TreeFallen");
        if (!AssetDatabase.IsValidFolder("Assets/AshenHollow/Resources/AH/Dreamscape")) AssetDatabase.CreateFolder("Assets/AshenHollow/Resources/AH", "Dreamscape");
        AssetDatabase.CreateAsset(s, "Assets/AshenHollow/Resources/AH/Dreamscape/set.asset");
        AssetDatabase.SaveAssets();
        // a report of what each piece is made of (size, triangles, shaders) to HeroShots/dreamscape.txt
        var sb = new System.Text.StringBuilder();
        foreach (var arr in new[] { s.trees, s.birches, s.bushes, s.flowers, s.flowerFields, s.mushrooms, s.grass, s.rocks, s.smallRocks, new[] { s.stump, s.fallen } })
            foreach (var go in arr)
            {
                if (go == null) { sb.AppendLine("MISSING"); continue; }
                int tris = 0; var b = new Bounds(); bool any = false; var shaders = new HashSet<string>();
                var lod = go.GetComponent<LODGroup>();
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null) tris += (int)mf.sharedMesh.GetIndexCount(0) / 3;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); foreach (var m in r.sharedMaterials) if (m != null && m.shader != null) shaders.Add(m.shader.name + (m.shader.isSupported ? "" : " UNSUPPORTED")); }
                sb.AppendLine(go.name + " size " + b.size.ToString("F2") + " tris(all LODs) " + tris + " lods " + (lod != null ? lod.lodCount : 0) + " shaders " + string.Join(", ", shaders));
            }
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../HeroShots/dreamscape.txt"), sb.ToString());
        Debug.Log("Ashen Hollow: Dreamscape set built");
    }
    static GameObject L(string p) { var g = AssetDatabase.LoadAssetAtPath<GameObject>(Root + p + ".prefab"); if (g == null) Debug.LogWarning("Ashen Hollow: missing " + p); return g; }
    static GameObject[] Load(string p, int a, int b) { var l = new List<GameObject>(); for (int i = a; i <= b; i++) { var g = L(p + i); if (g != null) l.Add(g); } return l.ToArray(); }
}
