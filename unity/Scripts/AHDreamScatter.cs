// Ashen Hollow: Dreamscape meadows. In lands that use the Dreamscape Nature: Meadows pack (AHDreamSet.UseIn), the
// open ground gets the pack's bushes, flowers, grass tufts, mushrooms and stones: thick round bushes and flowers at the
// foot of the trees, drifts of grass and flower fields across the fields, rocks here and there. Nothing is put on
// the paths you walk (anything Blocked, a village lot, a road tile), in water, or too close to the hero's start.
using System.Collections.Generic;
using UnityEngine;

public static class AHDreamScatter
{
    public static void Setup(AHGame g, Transform world)
    {
        if (world == null || !AHDreamSet.UseIn(AHGame.AreaId)) return;
        var d = AHDreamSet.Get(); if (d == null) return;
        var root = new GameObject("DreamScatter").transform;
        var rnd = new System.Random(AHGame.AreaId.GetHashCode() ^ 0x5eed);
        float y = world.position.y; var r = g.AreaRect;
        float areaM2 = r.width * r.height;
        int placed = 0;
        // around the trees: bushes and flowers, the way woods thicken at their edges
        foreach (var t in AHForest.Trees)
        {
            if (rnd.NextDouble() < 0.45) placed += Put(g, root, d.bushes, Around(t.pos, 2.5f, 5f, rnd), y, 0.32f, 0.5f, rnd, 1.6f) ? 1 : 0;
            if (rnd.NextDouble() < 0.35) placed += Put(g, root, d.flowers, Around(t.pos, 2f, 4.5f, rnd), y, 0.9f, 1.4f, rnd, 0.6f) ? 1 : 0;
            if (rnd.NextDouble() < 0.12) placed += Put(g, root, d.mushrooms, Around(t.pos, 1.2f, 2.5f, rnd), y, 0.8f, 1.3f, rnd, 0.4f) ? 1 : 0;
        }
        // across the open fields: grass drifts, flower fields, a few bushes and stones
        int nGrass = Mathf.Clamp((int)(areaM2 / 35f), 120, 1400), nFlow = nGrass / 4, nRock = Mathf.Clamp((int)(areaM2 / 900f), 10, 70);
        for (int i = 0; i < nGrass; i++) placed += Put(g, root, d.grass, Rand(r, rnd), y, 0.9f, 1.6f, rnd, 0.8f) ? 1 : 0;
        for (int i = 0; i < nFlow; i++) placed += Put(g, root, d.flowerFields, Rand(r, rnd), y, 0.8f, 1.3f, rnd, 1.2f) ? 1 : 0;
        for (int i = 0; i < nRock; i++) placed += (rnd.NextDouble() < 0.3 ? Put(g, root, d.rocks, Rand(r, rnd), y, 0.3f, 0.5f, rnd, 2f) : Put(g, root, d.smallRocks, Rand(r, rnd), y, 0.8f, 1.5f, rnd, 1f)) ? 1 : 0;
        AHModel.SetShadows(root.gameObject);
        // grass and flowers cast no shadows (they are many and small)
        foreach (var rr in root.GetComponentsInChildren<Renderer>(true)) if (rr.bounds.size.y < 1.2f) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Debug.Log("Ashen Hollow: Dreamscape scatter " + placed + " pieces in " + AHGame.AreaId);
    }

    static Vector3 Around(Vector3 c, float a, float b, System.Random rnd)
    {
        float ang = (float)rnd.NextDouble() * 6.283f, dist = a + (float)rnd.NextDouble() * (b - a);
        return c + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
    }
    static Vector3 Rand(Rect r, System.Random rnd) { return new Vector3(r.xMin + (float)rnd.NextDouble() * r.width, 0f, r.yMin + (float)rnd.NextDouble() * r.height); }

    static bool Put(AHGame g, Transform root, GameObject[] arr, Vector3 p, float y, float s0, float s1, System.Random rnd, float clear)
    {
        if (arr == null || arr.Length == 0) return false;
        var pf = arr[rnd.Next(arr.Length)]; if (pf == null) return false;
        p.y = y;
        if (!g.InArea(p) || g.Blocked(p, clear) || AHVillage.InLots(p)) return false;
        if (g.player != null) { Vector3 dd = g.player.transform.position - p; dd.y = 0; if (dd.sqrMagnitude < 9f) return false; }
        var go = Object.Instantiate(pf, root, false);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        go.transform.position = p; go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        go.transform.localScale = pf.transform.localScale * (s0 + (float)rnd.NextDouble() * (s1 - s0));
        return true;
    }
}
