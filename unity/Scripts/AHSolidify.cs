// Ashen Hollow: nothing solid you can walk through. Once a land is built, every standing thing among the rebuilt
// props, work stations, stalls, ruins, graves, tents, crystals and the like that still has no blocker under it (you could
// walk straight through a furnace, a loom, a gate post, a market stall) gets one: a round one for a round or squat thing,
// a box for a long one, a little inside its outline so you can still walk up to it and use it. Things you walk on or
// under (bridges, rugs, arches, awnings) and anything at a way out are left alone.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AHSolidify
{
    static readonly string[] Roots = { "Rebuilt props", "Rebuilt boxes", "Stations", "Stalls and wells", "Ruins", "Graves", "Tents", "Crystals", "Palms", "Cacti", "Bones", "Coral", "Mushrooms", "Treasure", "Bank vault", "Village", "Statue garden", "Training yard", "Fossils", "Ships", "Ore rocks", "Ore", "City buildings", "Castle", "Palace", "Great skull", "Dragon nest", "Arena", "BountyBoard", "Kingsbane statue", "Statue", "Waystone", "Inn sign", "Homestead" };
    static readonly string[] Open = { "Bridge", "bridge", "Rug", "Carpet", "Arch", "arch", "Awning", "Gate", "Portal", "Door", "Path", "Road", "Plank", "Dock", "Pier", "Stairs", "Step", "Banner", "Flag", "Lamp", "Lantern", "Sign", "Fence", "Rope" };
    public static int Last;

    public static IEnumerator Later(AHGame g) { yield return null; yield return null; Run(g); }

    public static void Run(AHGame g)
    {
        if (g == null || AHGame.AreaId == "abyss") { Last = 0; return; }
        float gy = g.W(0f, 0f).y;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AH_GROUND")) { gy = r.bounds.max.y; break; }
        var keep = new List<Vector3>();
        foreach (var s in AHGather.Spots) if (s.kind == "gate" || s.kind == "door" || s.kind == "use" || s.kind == "exit") keep.Add(s.pos);
        if (g.data != null && g.data.exits != null) foreach (var e in g.data.exits) keep.Add(e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) / 2f, (e.z0 + e.z1) / 2f));
        int n = 0;
        foreach (var rn in Roots)
        {
            var root = GameObject.Find(rn); if (root == null) continue;
            var units = new List<Transform>();
            if (root.GetComponentsInChildren<Renderer>().Length < 40 && root.transform.childCount < 8) units.Add(root.transform);   // one thing made of parts
            else foreach (Transform c in root.transform) units.Add(c);
            foreach (var u in units)
            {
                if (!u.gameObject.activeInHierarchy || OpenName(u.name)) continue;
                if (u.name.Length >= 2 && u.name[0] == 'f' && char.IsDigit(u.name[1])) continue;   // the homestead's crop fields: walked into
                bool any = false; Bounds b = new Bounds();
                foreach (var rd in u.GetComponentsInChildren<Renderer>())
                {
                    if (!rd.enabled || rd is ParticleSystemRenderer || rd is SkinnedMeshRenderer || OpenName(rd.name)) continue;
                    if (!any) { b = rd.bounds; any = true; } else b.Encapsulate(rd.bounds);
                }
                if (!any) continue;
                float foot = Mathf.Max(b.size.x, b.size.z), thin = Mathf.Min(b.size.x, b.size.z);
                // standing things you would bump into: tall enough, on the ground, not too big to be a place
                if (b.size.y < 0.9f || foot < 0.5f || foot > 12f || b.min.y > gy + 0.5f) continue;
                Vector3 c = new Vector3(b.center.x, gy, b.center.z), pv = new Vector3(u.position.x, gy, u.position.z);
                if (g.Blocked(c, 0.05f) || g.Blocked(pv, 0.05f) || !g.InArea(c)) continue;
                bool atWay = false; foreach (var k in keep) { Vector3 d = k - c; d.y = 0f; if (d.magnitude < foot * 0.5f + 2.5f) { atWay = true; break; } }
                if (atWay) continue;
                // something you use stands in it (an oven, a furnace, a stall's trader): stay well inside its reach
                float reach = float.MaxValue;
                foreach (var s in AHGather.Spots)
                {
                    if (s.kind == "chop") continue;
                    if (s.pos.x > b.min.x - 0.5f && s.pos.x < b.max.x + 0.5f && s.pos.z > b.min.z - 0.5f && s.pos.z < b.max.z + 0.5f) reach = Mathf.Min(reach, s.reach);
                }
                if (reach < float.MaxValue) g.AddBlocker(c, Mathf.Min(thin * 0.42f, Mathf.Max(0.3f, reach * 0.55f)));
                else if (foot < thin * 1.8f) g.AddBlocker(c, thin * 0.42f);
                else g.AddBlockBox(b, 0.25f);
                n++;
            }
        }
        Last = n;
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " solid things given blockers (you could walk through them)");
    }

    static bool OpenName(string s) { if (s.Contains("post") || s.Contains("Post")) return false; foreach (var o in Open) if (s.Contains(o)) return true; return false; }
}
