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
    static readonly string[] Roots = { "Rebuilt props", "Rebuilt boxes", "Stations", "Stalls and wells", "Ruins", "Graves", "Tents", "Crystals", "Palms", "Cacti", "Bones", "Coral", "Mushrooms", "Treasure", "Bank vault", "Village", "Statue garden", "Training yard", "Fossils", "Ships", "Ore rocks", "Ore", "City buildings", "Castle", "Palace", "Great skull", "Dragon nest", "Arena", "BountyBoard", "Kingsbane statue", "Statue", "Waystone", "Inn sign", "Homestead",
        // the dungeons: their standing pillars and the barrels, racks and statues along the walls
        "Dungeon dressing", "The Sunken Forge", "The Hollow Below", "Frostpeak Caverns", "The Molten Depths", "Tomb of the Sand King", "The Mire Warrens" };
    static readonly string[] Open = { "Bridge", "bridge", "Rug", "Carpet", "Arch", "arch", "Awning", "Gate", "Portal", "Door", "Path", "Road", "Plank", "Dock", "Pier", "Stairs", "Step", "Banner", "Flag", "Lamp", "Lantern", "Sign", "Fence", "Rope", "qd_wall", "qd_column", "qd_banner", "torch", "Torch", "web", "Web" };
    public static int Last;

    public static IEnumerator Later(AHGame g) { yield return null; yield return null; Run(g); }

    public static void Run(AHGame g)
    {
        if (g == null || AHGame.AreaId == "abyss") { Last = 0; return; }
        float gy = g.W(0f, 0f).y;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AH_GROUND")) { gy = r.bounds.max.y; break; }
        // plants and crystals standing on the bare ground that ended up hanging in the air (a Sunscar cactus floated
        // knee-high over the sand, its shadow on the ground beside it): set back down
        int sat = 0;
        foreach (var pn in new[] { "Cacti", "Palms", "Crystals", "Ferns", "Mushrooms", "Bones", "Coral" })
        {
            var pr = GameObject.Find(pn); if (pr == null) continue;
            foreach (Transform t in pr.transform)
            {
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                float lo = float.MaxValue; foreach (var r in rs) lo = Mathf.Min(lo, r.bounds.min.y);
                float up = lo - gy; if (up > 0.15f && up < 1.2f) { t.position -= Vector3.up * up; sat++; }
            }
        }
        if (sat > 0) Debug.Log("Ashen Hollow: " + sat + " plants and crystals set down on the ground");
        var keep = new List<Vector3>();
        foreach (var s in AHGather.Spots) if (s.kind == "gate" || s.kind == "door" || s.kind == "exit" || (s.kind == "use" && (s.name ?? "").Contains("House"))) keep.Add(s.pos);   // ways in and out (a waystone, a furnace or a chest is walked up to, not through)
        if (g.data != null && g.data.exits != null) foreach (var e in g.data.exits) keep.Add(e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) / 2f, (e.z0 + e.z1) / 2f));
        int n = 0;
        var roots = new List<GameObject>();
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!go.gameObject.activeInHierarchy || System.Array.IndexOf(Roots, go.name) < 0) continue;
            bool inner = false; for (var a = go.parent; a != null; a = a.parent) if (System.Array.IndexOf(Roots, a.name) >= 0) { inner = true; break; }
            if (!inner) roots.Add(go.gameObject);
        }
        foreach (var root in roots)
        {
            var units = new List<Transform>();
            // one thing made of parts (a waystone, a statue) when it all stands in one small place; otherwise a
            // collection (the stations, the tents, the props of a land), each child its own thing
            var all = root.GetComponentsInChildren<Renderer>(); bool one = all.Length > 0 && all.Length < 40;
            if (one) { Bounds rb = all[0].bounds; foreach (var r in all) rb.Encapsulate(r.bounds); one = Mathf.Max(rb.size.x, rb.size.z) <= 8f; }
            if (one) units.Add(root.transform);
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
                // a gate post stands beside the way through it: solid, but only its own thin footprint
                bool post = u.name.Contains("post") || u.name.Contains("Post");
                if (atWay && post && foot < 2.5f) { g.AddBlocker(c, thin * 0.4f); n++; continue; }
                if (atWay) continue;
                // something you use stands in it (an oven, a furnace, a stall's trader): stay well inside its reach
                float reach = float.MaxValue; var uses = new List<AHSpot>();
                foreach (var s in AHGather.Spots)
                {
                    if (s.kind == "chop") continue;
                    if (s.pos.x > b.min.x - 0.5f && s.pos.x < b.max.x + 0.5f && s.pos.z > b.min.z - 0.5f && s.pos.z < b.max.z + 0.5f) { reach = Mathf.Min(reach, s.reach); uses.Add(s); }
                }
                bool box = false;
                if (reach < float.MaxValue) g.AddBlocker(c, Mathf.Min(thin * 0.42f, Mathf.Max(0.3f, reach * 0.55f)));
                else if (foot < thin * 1.8f) g.AddBlocker(c, thin * 0.42f);
                else { g.AddBlockBox(b, 0.25f); box = true; }
                // never close in something you use: with no ground left to stand on within its reach, the blocker goes again
                bool shut = false; foreach (var s in uses) if (!Standable(g, s.pos, s.reach, gy)) { shut = true; break; }
                if (shut) { var rr = new Rect(c.x - 0.1f, c.z - 0.1f, 0.2f, 0.2f); if (box) g.RemoveBoxesIn(new Rect(b.min.x - 0.01f, b.min.z - 0.01f, b.size.x + 0.02f, b.size.z + 0.02f)); else g.RemoveBlockersIn(rr); continue; }
                n++;
            }
        }
        // a crag with no blocker at its heart (two in Vale Keep you could walk straight into): its solid core blocks
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (go.name != "Mountains" || !go.gameObject.activeInHierarchy) continue;
            foreach (Transform m in go)
            {
                if (!m.gameObject.activeInHierarchy || m.name != "Mountain") continue;
                bool any = false; Bounds b = new Bounds();
                foreach (var r in m.GetComponentsInChildren<Renderer>()) { if (!r.enabled) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (!any || b.size.y < 2f || b.min.y > gy + 0.5f) continue;
                Vector3 c = new Vector3(b.center.x, gy, b.center.z);
                if (!g.InArea(c) || g.Blocked(c, 0.05f)) continue;
                g.AddBlocker(c, Mathf.Min(b.size.x, b.size.z) * 0.3f); n++;
            }
        }
        // everything you use must be usable from somewhere you can stand: a fishing spot far out in the water, or a tent
        // house whose own walls fill its reach, gets a reach that comes to the nearest open ground
        int longer = 0;
        foreach (var sp in AHGather.Spots)
        {
            if (sp.kind == "chop" || sp.reach <= 0.5f || Standable(g, sp.pos, sp.reach, gy)) continue;
            for (float d = sp.reach + 0.5f; d <= sp.reach + 10f; d += 0.5f)
            {
                bool hit = false;
                for (int k = 0; k < 24 && !hit; k++)
                {
                    float a = k * Mathf.PI / 12f; var q = sp.pos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d; q.y = gy;
                    if (g.InArea(q) && !g.Blocked(q, 0.3f)) hit = true;
                }
                if (hit) { sp.reach = d + 0.8f; longer++; break; }
            }
        }
        if (longer > 0) Debug.Log("Ashen Hollow: " + longer + " things you use given a longer reach (no ground to stand on near them)");
        Last = n;
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " solid things given blockers (you could walk through them)");
    }

    static bool Standable(AHGame g, Vector3 at, float reach, float gy)
    {
        for (int k = 0; k < 16; k++)
        {
            float a = k * Mathf.PI / 8f;
            foreach (float rr in new[] { reach * 0.55f, reach * 0.85f, reach * 0.98f })
            {
                var q = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr; q.y = gy;
                if (g.InArea(q) && !g.Blocked(q, 0.3f)) return true;
            }
        }
        return false;
    }

    static bool OpenName(string s) { if (s.Contains("post") || s.Contains("Post")) return false; foreach (var o in Open) if (s.Contains(o)) return true; return false; }
}
