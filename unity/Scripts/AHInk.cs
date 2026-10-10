// Ashen Hollow: the anime ink line (AshenHollow/Outline) on armour and weapons made with Tripo, so they match the
// VRoid heroes' cel look. Added as one more material on each renderer, which draws its last mesh part again as the
// line. Thicker for big pieces is not wanted: the line is kept about 1.6 pixels wide on screen.
using UnityEngine;

public static class AHInk
{
    public static bool On = true;
    static Material mat;

    public static void Add(GameObject go, float width = 1.6f)
    {
        if (!On || go == null) return;
        if (mat == null) { mat = AHGame.LoadMat("AH/Materials/Outline", "AshenHollow/Outline"); if (mat == null) { On = false; return; } }
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
            var ms = r.sharedMaterials; bool has = false;
            foreach (var m in ms) if (m == mat) has = true;
            if (has) continue;
            // only meshes with a single part get the line on all of them (it draws the last part only)
            Mesh mesh = r is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)r).sharedMesh : r.GetComponent<MeshFilter>() != null ? r.GetComponent<MeshFilter>().sharedMesh : null;
            if (mesh == null || mesh.subMeshCount != 1 || mesh.vertexCount < 200) continue;
            var nm = new Material[ms.Length + 1]; for (int i = 0; i < ms.Length; i++) nm[i] = ms[i]; nm[ms.Length] = mat;
            r.sharedMaterials = nm;
        }
    }
}
