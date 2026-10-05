// Ashen Hollow: better city buildings. The cities' houses, shops, temples and halls come from the web game as simple
// boxes with roofs. Here each one is swapped, on the same lot and facing the same street, for a detailed building from
// KayKit's Medieval Hexagon pack (Kay Lousberg, CC0): homes with one or two floors, a church for the temple, a fortified
// hall, a blacksmith with its furnace, market stalls, a tavern with its great barrel, and an alchemist's tower.
// Each city has its own roof colour. Walls you bump into stay as they were; only the look changes.
// The lots come from Resources/AH/Data/cityhouses.json (exported from the web game's city planner).
using System.Collections.Generic;
using UnityEngine;

public static class AHCity
{
    static readonly Dictionary<string, string> Colour = new Dictionary<string, string>
    {
        { "city", "blue" }, { "hc_city", "blue" }, { "mw_city", "green" }, { "ss_city", "yellow" }, { "ch_city", "red" }, { "co_city", "green" },
        // villages and camps out in the wild
        { "vale", "blue" }, { "meadow", "green" }, { "kingsroad", "blue" }, { "frost", "blue" }, { "mire", "green" }, { "sands", "yellow" }, { "ember", "red" }, { "tide", "green" }, { "isle", "yellow" },
    };
    static object data;
    public static int Swapped { get; private set; }

    public static void Setup(AHGame g, Transform world)
    {
        Swapped = 0;
        string col; if (world == null || !Colour.TryGetValue(AHGame.AreaId, out col)) return;
        Landmarks(g, world, col);
        Pools(g, world);
        HideBlockPeople(world);
        if (data == null) { var ta = Resources.Load<TextAsset>("AH/Data/cityhouses"); if (ta == null) return; data = AHJson.Parse(ta.text); }
        var list = AHJson.A(data, AHGame.AreaId); if (list == null) return;

        // the web houses' meshes: everything whose middle stands inside a lot
        var rs = new List<Renderer>(); var mids = new List<Vector2>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.StartsWith("AH_")) continue;   // ground, water, marks
            var b = r.bounds; if (b.size.y > 16f || Mathf.Max(b.size.x, b.size.z) > 30f) continue;
            rs.Add(r); mids.Add(g.ToWeb(b.center));
        }
        var root = new GameObject("City buildings").transform;
        var hidden = new bool[rs.Count];
        foreach (var o in list)
        {
            float cx = (float)AHJson.N(o, "cx"), cz = (float)AHJson.N(o, "cz"), w = (float)AHJson.N(o, "w"), d = (float)AHJson.N(o, "d");
            string m = AHJson.S(o, "m", "home_A");
            float ground = float.PositiveInfinity; int n = 0;
            for (int i = 0; i < rs.Count; i++)
            {
                if (hidden[i]) continue; var p = mids[i];
                var b = rs[i].bounds; float bf = Mathf.Max(b.size.x, b.size.z);
                float edge = bf < 1.2f ? 1.0f : 0.3f;   // door signs and lamps stick out past the lot's front
                if (Mathf.Abs(p.x - cx) > w / 2f + edge || Mathf.Abs(p.y - cz) > d / 2f + edge) continue;
                if (bf > Mathf.Max(w, d) * 2.1f + 1.5f) continue;   // roofs turned 45 degrees measure 1.4x wider
                ground = Mathf.Min(ground, b.min.y); rs[i].enabled = false; hidden[i] = true; n++;
            }

            // face the door: the models' doors look along +z
            Vector3 centre = g.W(cx, cz); if (!float.IsInfinity(ground) && n > 0) centre.y = ground;
            Vector3 door = AHJson.Has(o, "dx") ? g.W((float)AHJson.N(o, "dx"), (float)AHJson.N(o, "dz")) : g.W(g.data.spawn.x, g.data.spawn.z);   // no door: face the middle of the camp
            Vector3 dir = door - centre; dir.y = 0f; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir = Snap(g, dir);   // square to the streets: no house stands askew
            Vector2 lotAxes = LotAxes(g, dir, w, d);   // x = across the front, y = deep

            // homes become townhouses in the place's own style; the shops, inns, temples and halls stay KayKit's
            GameObject pf = null; bool town = false;
            bool inn = m == "tavern";
            if (m == "home_A" || m == "home_B" || inn)
            {
                string tn = inn ? InnHouse() : TownHouse(o, lotAxes.x, Swapped);
                if (tn != null) { pf = Resources.Load<GameObject>("AH/Models/Town/" + tn); town = pf != null; }
            }
            if (pf == null) pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_" + m + "_" + col);
            if (pf == null) { for (int i = 0; i < rs.Count; i++) if (hidden[i] && !rs[i].enabled && mids[i].x >= cx - w / 2f - 0.3f && mids[i].x <= cx + w / 2f + 0.3f && mids[i].y >= cz - d / 2f - 0.3f && mids[i].y <= cz + d / 2f + 0.3f) rs[i].enabled = true; continue; }
            var go = Object.Instantiate(pf, root, false); go.name = (town ? "Townhouse_" : "Building_") + m;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            // fit the lot: across the street front and back to front, then put it on the ground
            go.transform.localScale = Vector3.one; go.transform.position = Vector3.zero;
            Vector3 ms = MeasureLocal(go);
            if (town)
            {
                // a townhouse fills its lot's front exactly; shallow lots make it shallower, never shorter than a storey looks right
                float tx = lotAxes.x * 0.99f / Mathf.Max(0.1f, ms.x), tz = lotAxes.y * 1.05f / Mathf.Max(0.1f, ms.z);
                go.transform.localScale = new Vector3(tx, Mathf.Clamp((tx + tz) * 0.5f, 0.88f, 1.12f), tz);
            }
            else
            {
            float k = Mathf.Min(lotAxes.x / Mathf.Max(0.1f, ms.x), lotAxes.y / Mathf.Max(0.1f, ms.z)) * 0.96f;
            // a little wider where the lot is wider than the building, so the street front stays closed (up to a third)
            float kx = Mathf.Clamp(lotAxes.x / Mathf.Max(0.1f, ms.x) * 0.94f, k, k * 1.33f);
            go.transform.localScale = new Vector3(kx, k, k);
            }
            Bounds mb = Measure(go);
            go.transform.position = new Vector3(centre.x - (mb.center.x - go.transform.position.x), centre.y - (mb.min.y - go.transform.position.y) - 0.02f, centre.z - (mb.center.z - go.transform.position.z));
            AHModel.SetShadows(go);
            if (inn && town) InnSign(root, centre, dir.normalized, lotAxes, mb2(go));
            Props(root, m, col, centre, dir.normalized, lotAxes, Swapped);
            Swapped++;
        }
        Debug.Log("Ashen Hollow: " + Swapped + " of " + list.Count + " city buildings rebuilt");
        Walls(g, world, col, root);
    }

    // ---------- shop booths and wells ----------
    // The camps' and cities' shop booths were four posts, a back board, a sign and a box of a roof; the Hollow
    // Meadow well (the way down into the Hollow) a grey ring with a cone roof. They become KayKit market stalls,
    // full of crates of produce under striped awnings, facing the square, and a stone well with a tiled roof,
    // bucket and crank, in the area's colour. Their spots (Resources/AH/Data/landmarks.json) were read from the models.
    static object marks;
    public static int Landmarked { get; private set; }
    static void Landmarks(AHGame g, Transform world, string col)
    {
        Landmarked = 0;
        if (marks == null) { var ta = Resources.Load<TextAsset>("AH/Data/landmarks"); if (ta == null) return; marks = AHJson.Parse(ta.text); }
        var here = AHJson.O(marks, AHGame.AreaId); if (here == null) return;
        var booths = AHJson.A(here, "booths"); var wells = AHJson.A(here, "wells");
        var root = new GameObject("Stalls and wells").transform;
        var rs = new List<Renderer>(); var mids = new List<Vector2>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue;
            var b = r.bounds; if (b.max.y - world.position.y > 5f || Mathf.Max(b.size.x, b.size.z) > 8f) continue;
            rs.Add(r); mids.Add(g.ToWeb(b.center));
        }
        Vector3 mid = g.W(g.data.spawn.x, g.data.spawn.z);
        if (booths != null)
            foreach (var o in booths)
            {
                var a = o as List<object>; float x0 = (float)(double)a[0], z0 = (float)(double)a[1], x1 = (float)(double)a[2], z1 = (float)(double)a[3];
                for (int i = 0; i < rs.Count; i++) if (mids[i].x > x0 - 0.7f && mids[i].x < x1 + 0.7f && mids[i].y > z0 - 0.7f && mids[i].y < z1 + 0.7f) rs[i].enabled = false;
                var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_market_" + col); if (pf == null) continue;
                var go = Object.Instantiate(pf, root, false); go.name = "Market stall";
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
                Vector3 centre = g.W((x0 + x1) / 2f, (z0 + z1) / 2f); centre.y = world.position.y;
                Vector3 dir = mid - centre; dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
                go.transform.rotation = Quaternion.LookRotation(dir.normalized);
                go.transform.position = Vector3.zero; go.transform.localScale = Vector3.one;
                Vector3 ms = MeasureLocal(go); Vector2 lot = LotAxes(g, dir, x1 - x0 + 0.5f, z1 - z0 + 0.5f);
                float k = Mathf.Min(lot.x / Mathf.Max(0.1f, ms.x), lot.y / Mathf.Max(0.1f, ms.z));
                go.transform.localScale = Vector3.one * k;
                Bounds mb = Measure(go);
                go.transform.position = new Vector3(centre.x - (mb.center.x - go.transform.position.x), centre.y - (mb.min.y - go.transform.position.y), centre.z - (mb.center.z - go.transform.position.z));
                AHModel.SetShadows(go); Landmarked++;
            }
        if (wells != null)
            foreach (var o in wells)
            {
                var a = o as List<object>; float x = (float)(double)a[0], z = (float)(double)a[1];
                for (int i = 0; i < rs.Count; i++) if ((mids[i] - new Vector2(x, z)).magnitude < 2.4f) rs[i].enabled = false;
                var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_well_" + col); if (pf == null) continue;
                var go = Object.Instantiate(pf, root, false); go.name = "Well";
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
                Vector3 p = g.W(x, z); p.y = world.position.y;
                Vector3 dir = mid - p; dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
                go.transform.position = p; go.transform.rotation = Quaternion.LookRotation(dir.normalized); go.transform.localScale = Vector3.one * 4.6f;
                AHModel.SetShadows(go); Landmarked++;
            }
        var lamps = AHJson.A(here, "lamps");
        if (lamps != null)
            foreach (var o in lamps)
            {
                var a = o as List<object>; float x = (float)(double)a[0], z = (float)(double)a[1];
                for (int i = 0; i < rs.Count; i++) if ((mids[i] - new Vector2(x, z)).magnitude < 0.45f) rs[i].enabled = false;
                var go = AHStations.Lamp(root, Landmarked); Vector3 p = g.W(x, z); p.y = world.position.y;
                go.transform.position = p; go.transform.rotation = Quaternion.Euler(0, (Mathf.Abs(x * 13 + z * 7) % 4) * 90f, 0);
                AHModel.SetShadows(go); Landmarked++;
            }
        var ftns = AHJson.A(here, "fountains");
        if (ftns != null)
            foreach (var o in ftns)
            {
                var a = o as List<object>; float x = (float)(double)a[0], z = (float)(double)a[1], fr = (float)(double)a[2];
                for (int i = 0; i < rs.Count; i++) if ((mids[i] - new Vector2(x, z)).magnitude < fr + 0.3f && rs[i].bounds.size.x < fr * 2.4f) rs[i].enabled = false;
                var go = AHStations.Fountain(root, fr); Vector3 p = g.W(x, z); p.y = world.position.y; go.transform.position = p;
                AHModel.SetShadows(go); Landmarked++;
            }
        Debug.Log("Ashen Hollow: " + Landmarked + " stalls, wells, lamps and fountains rebuilt");
    }

    // ---------- city walls and towers ----------
    // The web walls were long grey slabs with a row of cubes on top, and the towers plain cylinders with cones.
    // In the six cities they become KayKit castle walls (stone courses and battlements) and round watchtowers with
    // the city's roof colour; the two towers beside each gate get the taller kind with a lookout and a pennant.
    // The walls stop you exactly where they did before (the area's boxes); only the look changes.
    public static int WallPieces { get; private set; }
    static void Walls(AHGame g, Transform world, string col, Transform root)
    {
        WallPieces = 0;
        string id = AHGame.AreaId; bool town = id != "city" && !id.EndsWith("_city");
        var wallPf = Resources.Load<GameObject>("AH/Models/KK/kk_wall_straight");
        if (wallPf == null || g.data == null || g.data.boxes == null) return;
        var walls = new List<AHBox>(); foreach (var b in g.data.boxes) if (Mathf.Min(b.w, b.h) < 2f && Mathf.Max(b.w, b.h) > 8f) walls.Add(b);
        if (walls.Count == 0) return;
        var towers = new List<AHCircle>();
        if (g.data.circles != null)
            foreach (var c in g.data.circles)
            {
                if (c.r < 1.7f || c.r > 2.3f) continue;
                foreach (var w in walls) if (c.x > w.x - 2.6f && c.x < w.x + w.w + 2.6f && c.z > w.z - 2.6f && c.z < w.z + w.h + 2.6f) { towers.Add(c); break; }
            }
        var gates = new List<Vector2>(); if (g.data.gates != null) foreach (var gt in g.data.gates) gates.Add(new Vector2(gt.x, gt.z));
        // out in Kingsvale (Varrow seen from outside) only the town's own walls count: those with a tower at an end
        if (town)
        {
            walls.RemoveAll(w => { bool ok = false; foreach (var c in towers) { float ex = Mathf.Clamp(c.x, w.x, w.x + w.w), ez = Mathf.Clamp(c.z, w.z, w.z + w.h); if (new Vector2(c.x - ex, c.z - ez).magnitude < 3f && (Mathf.Abs(c.x - w.x) < 3f || Mathf.Abs(c.x - w.x - w.w) < 3f) && (Mathf.Abs(c.z - w.z) < 3f || Mathf.Abs(c.z - w.z - w.h) < 3f)) ok = true; } return !ok; });
            if (walls.Count == 0) return;
        }
        // gates with no towers of their own (the walls run straight past them): cut an opening in the wall there
        var openGates = new List<Vector2>();
        foreach (var gp in gates) { bool tw = false; foreach (var c in towers) if ((gp - new Vector2(c.x, c.z)).magnitude < 7f) tw = true; if (!tw) openGates.Add(gp); }

        // hide the web walls, battlements and towers
        int hid = 0;
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue;
            var b = r.bounds; if (b.size.y > 16f) continue;
            Vector2 p0 = g.ToWeb(b.min), p1 = g.ToWeb(b.max);
            float x0 = Mathf.Min(p0.x, p1.x), x1 = Mathf.Max(p0.x, p1.x), z0 = Mathf.Min(p0.y, p1.y), z1 = Mathf.Max(p0.y, p1.y);
            bool hide = false;
            foreach (var w in walls) if (x0 >= w.x - 0.45f && x1 <= w.x + w.w + 0.45f && z0 >= w.z - 0.45f && z1 <= w.z + w.h + 0.45f) { hide = true; break; }
            if (!hide) foreach (var c in towers) if (x0 >= c.x - c.r - 1.4f && x1 <= c.x + c.r + 1.4f && z0 >= c.z - c.r - 1.4f && z1 <= c.z + c.r + 1.4f) { hide = true; break; }
            // the old gatehouse: its square towers, the arch, the portcullis, the dark doorway and the banners
            if (!hide)
                foreach (var gp in gates)
                {
                    if (Mathf.Max(Mathf.Abs((x0 + x1) / 2f - gp.x), Mathf.Abs((z0 + z1) / 2f - gp.y)) > 5.5f) continue;
                    float lift = b.min.y - world.position.y, fx = x1 - x0, fz = z1 - z0;
                    if (lift > 2.3f || (fx > 2.5f && fz > 2.5f) || Mathf.Min(fx, fz) < 0.4f) { hide = true; break; }
                }
            if (hide) { r.enabled = false; hid++; }
        }

        float ground = world.position.y;
        Vector3 xw = g.W(1f, 0f) - g.W(0f, 0f), zw = g.W(0f, 1f) - g.W(0f, 0f); xw.y = 0f; zw.y = 0f;
        foreach (var w in walls)
        {
            bool alongX = w.w >= w.h; float L = Mathf.Max(w.w, w.h), thick = Mathf.Min(w.w, w.h);
            Vector3 dir = (alongX ? xw : zw).normalized; float yaw = Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg;
            // the stretches of this wall between gate openings
            float a0 = alongX ? w.x : w.z, line = alongX ? w.z + w.h / 2f : w.x + w.w / 2f;
            var cuts = new List<float>();
            foreach (var gp in openGates)
            {
                float along = alongX ? gp.x : gp.y, across = alongX ? gp.y : gp.x;
                if (Mathf.Abs(across - line) < 6f && along > a0 + 4f && along < a0 + L - 4f) cuts.Add(along);
            }
            cuts.Sort();
            var spans = new List<Vector2>(); float from = a0;
            foreach (var cu in cuts) { spans.Add(new Vector2(from, cu - 3.1f)); from = cu + 3.1f; GateHouse(g, root, wallPf, col, alongX, line, cu, 3.9f, ground); }
            spans.Add(new Vector2(from, a0 + L));
            foreach (var sp in spans)
            {
            float SL = sp.y - sp.x; if (SL < 0.5f) continue;
            int n = Mathf.Max(1, Mathf.RoundToInt(SL / 6.5f)); float s = SL / n;
            for (int i = 0; i < n; i++)
            {
                float u = sp.x + (i + 0.5f) * s;
                float cx = alongX ? u : line, cz = alongX ? line : u;
                var go = Object.Instantiate(wallPf, root, false); go.name = "Wall";
                foreach (var cl in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
                Vector3 p = g.W(cx, cz); p.y = ground;
                go.transform.position = p; go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                go.transform.localScale = new Vector3(s / 2f * 1.03f, 4.1f / 1.1f, (thick + 0.5f) / 0.8f);
                AHModel.SetShadows(go); WallPieces++;
            }
            }
        }
        // gates that sit in a gap between two walls on one line: a gatehouse across the gap
        foreach (var gp in openGates)
        {
            float bestLo = float.NegativeInfinity, bestHi = float.PositiveInfinity; bool bx = false; float bl = 0f;
            foreach (bool ax in new[] { true, false })
                foreach (var w in walls)
                {
                    bool alongX = w.w >= w.h; if (alongX != ax) continue;
                    float line = alongX ? w.z + w.h / 2f : w.x + w.w / 2f, across = alongX ? gp.y : gp.x, along = alongX ? gp.x : gp.y;
                    if (Mathf.Abs(across - line) > 6f) continue;
                    float e0 = alongX ? w.x : w.z, e1 = e0 + (alongX ? w.w : w.h);
                    if (e1 <= along + 0.5f && along - e1 < 8f && e1 > bestLo) { bestLo = e1; bx = ax; bl = line; }
                    if (e0 >= along - 0.5f && e0 - along < 8f && e0 < bestHi) { bestHi = e0; bx = ax; bl = line; }
                }
            if (float.IsInfinity(bestLo) || float.IsInfinity(bestHi) || bestHi - bestLo < 3f || bestHi - bestLo > 15f) continue;
            GateHouse(g, root, wallPf, col, bx, bl, (bestLo + bestHi) / 2f, (bestHi - bestLo) / 2f, ground);
        }
        Vector3 mid = g.W(g.data.spawn.x, g.data.spawn.z);
        foreach (var c in towers)
        {
            bool atGate = false; foreach (var gp in gates) if ((gp - new Vector2(c.x, c.z)).magnitude < 7f) atGate = true;
            var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_tower_" + (atGate ? "B_" : "A_") + col);
            if (pf == null) continue;
            var go = Object.Instantiate(pf, root, false); go.name = atGate ? "Gate tower" : "Wall tower";
            foreach (var cl in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
            Vector3 p = g.W(c.x, c.z); p.y = ground;
            Vector3 face = mid - p; face.y = 0f; if (face.sqrMagnitude < 0.01f) face = Vector3.forward;
            go.transform.position = p; go.transform.rotation = Quaternion.LookRotation(face.normalized);
            go.transform.localScale = Vector3.one * (c.r * 2.3f);
            AHModel.SetShadows(go); WallPieces++;
        }
        // a battlemented lintel across each gate, from one gate tower to the other
        for (int i = 0; i < towers.Count; i++)
            for (int j = i + 1; j < towers.Count; j++)
            {
                var a = towers[i]; var c = towers[j]; Vector2 ab = new Vector2(c.x - a.x, c.z - a.z);
                if (ab.magnitude > 9.5f || ab.magnitude < 3f) continue;
                Vector2 mid2 = new Vector2((a.x + c.x) / 2f, (a.z + c.z) / 2f); bool gate = false;
                foreach (var gp in gates) if ((gp - mid2).magnitude < 4f) gate = true;
                if (!gate) continue;
                Vector3 pa = g.W(a.x, a.z), pc = g.W(c.x, c.z), d = pc - pa; d.y = 0f;
                var go = Object.Instantiate(wallPf, root, false); go.name = "Gate lintel";
                foreach (var cl in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
                Vector3 p = (pa + pc) / 2f; p.y = ground + 4.6f;
                go.transform.position = p; go.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg, 0f);
                go.transform.localScale = new Vector3(d.magnitude / 2f, 1.9f / 1.1f, 2.1f / 0.8f);
                AHModel.SetShadows(go); WallPieces++;
            }
        Debug.Log("Ashen Hollow: city walls rebuilt (" + WallPieces + " pieces, " + hid + " old parts hidden)");
    }

    // a gatehouse over an opening cut in a straight wall: a tall tower each side and a battlemented lintel between
    static void GateHouse(AHGame g, Transform root, GameObject wallPf, string col, bool alongX, float line, float at, float hw, float ground)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_tower_B_" + col);
        Vector3 mid = g.W(g.data.spawn.x, g.data.spawn.z);
        foreach (float sd in new[] { -hw, hw })
        {
            if (pf == null) break;
            float cx = alongX ? at + sd : line, cz = alongX ? line : at + sd;
            var go = Object.Instantiate(pf, root, false); go.name = "Gate tower";
            foreach (var cl in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
            Vector3 p = g.W(cx, cz); p.y = ground;
            Vector3 face = mid - p; face.y = 0f; if (face.sqrMagnitude < 0.01f) face = Vector3.forward;
            go.transform.position = p; go.transform.rotation = Quaternion.LookRotation(face.normalized);
            go.transform.localScale = Vector3.one * 4.4f;
            AHModel.SetShadows(go); WallPieces++;
        }
        Vector3 pa = g.W(alongX ? at - hw : line, alongX ? line : at - hw), pc = g.W(alongX ? at + hw : line, alongX ? line : at + hw), d = pc - pa; d.y = 0f;
        var li = Object.Instantiate(wallPf, root, false); li.name = "Gate lintel";
        foreach (var cl in li.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
        Vector3 q = (pa + pc) / 2f; q.y = ground + 4.6f;
        li.transform.position = q; li.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(-d.z, d.x) * Mathf.Rad2Deg, 0f);
        li.transform.localScale = new Vector3(d.magnitude / 2f, 1.9f / 1.1f, 2.1f / 0.8f);
        AHModel.SetShadows(li); WallPieces++;
    }

    // which townhouse for a home: the place's style, the width class nearest the lot's street front, its storeys
    // (a storey more in the cities now and then, so the streets rise and fall) and one of two looks
    static readonly Dictionary<string, string> TownStyle = new Dictionary<string, string>
    {
        { "city", "varrow" }, { "vale", "varrow" }, { "meadow", "varrow" }, { "kingsroad", "varrow" },
        { "hc_city", "hc" }, { "frost", "hc" }, { "mw_city", "mw" }, { "mire", "mw" },
        { "ch_city", "ch" }, { "ember", "ch" }, { "co_city", "co" }, { "tide", "co" },
        { "ss_city", "ss" }, { "sands", "ss" }, { "isle", "ss" },
    };
    static string TownHouse(object o, float front, int seed)
    {
        string st; if (!TownStyle.TryGetValue(AHGame.AreaId, out st)) return null;
        int wi = front < 5.4f ? 0 : front < 7.1f ? 1 : 2;
        int fl = Mathf.Clamp((int)AHJson.N(o, "fl", 1), 1, 3);
        int h = Mathf.Abs((int)(AHJson.N(o, "cx") * 7.31 + AHJson.N(o, "cz") * 13.7)) + seed;
        if (IsCity(AHGame.AreaId) && AHGame.AreaId != "mw_city" && wi > 0 && fl < 3 && h % 3 != 0) fl++;
        if (wi == 0) fl = Mathf.Min(fl, 2);    // a narrow house of three storeys looks like a tower
        return st + "_" + wi + "_" + fl + "_" + (h / 3 % 2);
    }

    static string InnHouse()
    {
        string st; if (!TownStyle.TryGetValue(AHGame.AreaId, out st)) return null;
        return st + "_2_3_" + (AHGame.AreaId.Length % 2);
    }
    static Bounds mb2(GameObject go) { return Measure(go); }

    // the inn's sign: an iron bracket out from the front wall with a painted board and a gilt tankard
    static void InnSign(Transform root, Vector3 centre, Vector3 dir, Vector2 lot, Bounds b)
    {
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 wall = centre + dir * (lot.y * 0.5f) + right * (lot.x * 0.3f); wall.y = b.min.y + 3.6f;
        var t = new GameObject("Inn sign").transform; t.SetParent(root, false); t.position = wall; t.rotation = Quaternion.LookRotation(dir);
        var iron = Mat("sign_iron", new Color(0.16f, 0.16f, 0.18f)); var wood = Mat("sign_wood", new Color(0.42f, 0.26f, 0.14f));
        var paint = Mat("sign_paint", new Color(0.12f, 0.24f, 0.42f)); var gold = Mat("sign_gold", new Color(0.86f, 0.66f, 0.22f));
        P(t, PrimitiveType.Cube, new Vector3(0, 0, 0.55f), new Vector3(0.06f, 0.06f, 1.1f), iron);
        P(t, PrimitiveType.Cube, new Vector3(0, 0.25f, 0.3f), new Vector3(0.04f, 0.04f, 0.7f), iron, new Vector3(-40f, 0, 0));
        foreach (float z in new[] { 0.35f, 0.95f }) P(t, PrimitiveType.Cube, new Vector3(0, -0.12f, z), new Vector3(0.02f, 0.24f, 0.02f), iron);
        P(t, PrimitiveType.Cube, new Vector3(0, -0.6f, 0.65f), new Vector3(0.07f, 0.72f, 0.92f), wood);
        P(t, PrimitiveType.Cube, new Vector3(0, -0.6f, 0.65f), new Vector3(0.09f, 0.56f, 0.76f), paint);
        foreach (float sx in new[] { -1f, 1f })
        {
            P(t, PrimitiveType.Cylinder, new Vector3(sx * 0.05f, -0.6f, 0.62f), new Vector3(0.22f, 0.012f, 0.22f), gold, new Vector3(0, 0, 90));
            P(t, PrimitiveType.Cube, new Vector3(sx * 0.055f, -0.6f, 0.78f), new Vector3(0.012f, 0.14f, 0.06f), gold);
        }
        AHModel.SetShadows(t.gameObject);
    }

    static Vector3 Snap(AHGame g, Vector3 dir)
    {
        Vector3 ax = g.W(1f, 0f) - g.W(0f, 0f), az = g.W(0f, 1f) - g.W(0f, 0f); ax.y = 0f; az.y = 0f; ax.Normalize(); az.Normalize();
        Vector3 best = dir.normalized; float bd = -2f;
        foreach (var c in new[] { ax, -ax, az, -az }) { float d = Vector3.Dot(c, dir.normalized); if (d > bd) { bd = d; best = c; } }
        return best;
    }

    static readonly Dictionary<string, Material> pmats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, float smooth = 0.2f)
    {
        Material m; if (pmats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = key }; m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.enableInstancing = true;
        pmats[key] = m; return m;
    }
    static GameObject P(Transform par, PrimitiveType t, Vector3 pos, Vector3 size, Material m, Vector3 rot = default(Vector3))
    {
        var o = AHLowPoly.Fix(GameObject.CreatePrimitive(t), t); Object.Destroy(o.GetComponent<Collider>());
        o.transform.SetParent(par, false); o.transform.localPosition = pos; o.transform.localScale = size; o.transform.localRotation = Quaternion.Euler(rot);
        o.GetComponent<Renderer>().sharedMaterial = m; return o;
    }

    // ---------- pools: Mirewatch's black bog pools and Highcairn's frozen pond ----------
    // The web game painted them on the ground only. Now Mirewatch's are dark still water you cannot walk into, with lily
    // pads (some in flower), reeds and bulrushes round the edge and mossy stones; Highcairn's pond is a sheet of glassy
    // ice with cracks, ringed with snow.
    static Mesh disc;
    static Mesh Disc()
    {
        if (disc != null) return disc;
        int n = 48; var v = new List<Vector3> { Vector3.zero }; var uv = new List<Vector2> { new Vector2(0.5f, 0.5f) }; var tri = new List<int>();
        for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f)); uv.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f)); }
        for (int i = 1; i <= n; i++) { tri.Add(0); tri.Add(i + 1); tri.Add(i); }
        disc = new Mesh { name = "Disc" }; disc.SetVertices(v); disc.SetUVs(0, uv); disc.SetTriangles(tri, 0); disc.RecalculateNormals(); disc.RecalculateBounds();
        return disc;
    }
    static void Pools(AHGame g, Transform world)
    {
        string id = AHGame.AreaId; var list = new List<Vector4>(); bool ice = false, dress = false;
        if (id == "mw_city") { list.Add(new Vector4(450, 1750, 300, 190)); list.Add(new Vector4(2050, 1750, 330, 200)); list.Add(new Vector4(2100, 480, 260, 170)); }
        else if (id == "hc_city") { list.Add(new Vector4(2365, 1632, 362, 205)); ice = true; }   // where the ground map paints it
        else if (id == "mire" && g.data.water != null && g.data.water.Length > 0) { foreach (var w in g.data.water) list.Add(new Vector4(w.x, w.z, w.rx, w.rz)); dress = true; }   // Duskmire's own bog pools: dress them
        else return;
        float ox = id == "mw_city" ? 155000f : 150000f;
        float ground = world.position.y; foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("AH_GROUND")) { ground = r.bounds.max.y; break; }
        var root = new GameObject(ice ? "Frozen pond" : "Bog pools").transform;
        var water = ice ? null : AHGame.LoadMat("AH/Materials/Water", "AshenHollow/Water");
        var rnd = new System.Random(id.GetHashCode());
        var extra = new List<AHWaterArea>(g.data.water ?? new AHWaterArea[0]);
        foreach (var p in list)
        {
            float x = (ox + p.x) * AHDB.S, z = p.y * AHDB.S, rx = p.z * AHDB.S, rz = p.w * AHDB.S;
            if (dress) { x = p.x; z = p.y; rx = p.z; rz = p.w; }
            Vector3 c = g.W(x, z); c.y = ground;
            Vector3 ex = g.W(1f, 0f) - g.W(0f, 0f), ez = g.W(0f, 1f) - g.W(0f, 0f);
            float wx = Mathf.Abs(ex.x) * rx + Mathf.Abs(ez.x) * rz, wz = Mathf.Abs(ex.z) * rx + Mathf.Abs(ez.z) * rz;   // world half-sizes
            if (dress)   // the old square lily boards round the pool's edge
                foreach (var r in world.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || r.name.StartsWith("AH_")) continue; var bb = r.bounds; float f = Mathf.Max(bb.size.x, bb.size.z);
                    if (bb.size.y > 0.05f || f < 0.6f || f > 1.5f) continue;
                    float dx = (bb.center.x - c.x) / (wx + 1.5f), dz = (bb.center.z - c.z) / (wz + 1.5f);
                    if (dx * dx + dz * dz < 1f) r.enabled = false;
                }
            if (!dress) {
            var surf = new GameObject(ice ? "Ice" : "Bog water"); surf.transform.SetParent(root, false);
            surf.transform.position = c + Vector3.up * (ice ? 0.05f : 0.04f); surf.transform.localScale = new Vector3(wx * 2f, 1f, wz * 2f);
            surf.AddComponent<MeshFilter>().sharedMesh = Disc(); var mr = surf.AddComponent<MeshRenderer>();
            if (ice) { mr.sharedMaterial = Mat("ice", new Color(0.72f, 0.86f, 0.95f), 0.92f); }
            else if (water != null)
            {
                var m = new Material(water); m.SetColor("_Color", new Color32(0x2a, 0x36, 0x22, 255)); m.SetFloat("_Foam", 0.15f); m.SetFloat("_Mode", 1f); m.SetFloat("_Refract", 0f); m.SetFloat("_Opacity", 0.96f);
                mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (!ice) extra.Add(new AHWaterArea { x = x, z = z, rx = rx * 0.92f, rz = rz * 0.92f });
            }
            // the rim: stones (snow banks round the ice), reeds and bulrushes, lily pads on the water
            var stone = Mat(ice ? "snowbank" : "mossstone", ice ? new Color(0.95f, 0.97f, 1f) : new Color(0.33f, 0.36f, 0.26f), ice ? 0.3f : 0.1f);
            int ring = Mathf.RoundToInt((wx + wz) * 2.2f);
            for (int i = 0; i < ring; i++)
            {
                float a = (i + (float)rnd.NextDouble() * 0.5f) * Mathf.PI * 2f / ring, k = 1.0f + (float)rnd.NextDouble() * 0.06f;
                Vector3 q = c + new Vector3(Mathf.Cos(a) * wx * k, 0.02f, Mathf.Sin(a) * wz * k);
                float s0 = ice ? 0.9f + (float)rnd.NextDouble() * 0.7f : 0.45f + (float)rnd.NextDouble() * 0.45f;
                P(root, PrimitiveType.Sphere, root.InverseTransformPoint(q), new Vector3(s0 * 1.3f, s0 * (ice ? 0.35f : 0.45f), s0), stone, new Vector3(0, a * Mathf.Rad2Deg, 0));
            }
            if (ice)
            {
                var crack = Mat("crack", new Color(0.92f, 0.97f, 1f), 0.6f);
                for (int i = 0; i < 9; i++)
                {
                    float a = (float)rnd.NextDouble() * 360f, L = (wx + wz) * (0.2f + (float)rnd.NextDouble() * 0.35f);
                    Vector3 q = c + new Vector3(((float)rnd.NextDouble() - 0.5f) * wx, 0.065f, ((float)rnd.NextDouble() - 0.5f) * wz);
                    P(root, PrimitiveType.Cube, root.InverseTransformPoint(q), new Vector3(0.05f, 0.005f, L), crack, new Vector3(0, a, 0));
                }
                continue;
            }
            var reed = Mat("reed", new Color(0.24f, 0.36f, 0.16f)); var rush = Mat("bulrush", new Color(0.36f, 0.22f, 0.12f));
            var pad = Mat("lilypad", new Color(0.22f, 0.45f, 0.18f)); var bloom = Mat("lily", new Color(0.98f, 0.78f, 0.88f));
            for (int i = 0; i < ring * 2; i++)
            {
                if (rnd.NextDouble() < 0.35) continue;
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, k = 0.86f + (float)rnd.NextDouble() * 0.1f;
                Vector3 b0 = c + new Vector3(Mathf.Cos(a) * wx * k, 0f, Mathf.Sin(a) * wz * k);
                int n = 3 + rnd.Next(4);
                for (int j = 0; j < n; j++)
                {
                    Vector3 q = b0 + new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.5f, 0f, ((float)rnd.NextDouble() - 0.5f) * 0.5f);
                    float h = 0.8f + (float)rnd.NextDouble() * 0.9f; var tilt = new Vector3(((float)rnd.NextDouble() - 0.5f) * 16f, 0, ((float)rnd.NextDouble() - 0.5f) * 16f);
                    var r = P(root, PrimitiveType.Cylinder, root.InverseTransformPoint(q + Vector3.up * h * 0.5f), new Vector3(0.035f, h * 0.5f, 0.035f), reed, tilt);
                    if (rnd.NextDouble() < 0.4) P(root, PrimitiveType.Capsule, root.InverseTransformPoint(q + Vector3.up * (h * 0.85f)), new Vector3(0.07f, 0.14f, 0.07f), rush, tilt);
                }
            }
            int pads = Mathf.RoundToInt(wx * wz * 0.35f);
            for (int i = 0; i < pads; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, k = Mathf.Sqrt((float)rnd.NextDouble()) * 0.75f;
                Vector3 q = c + new Vector3(Mathf.Cos(a) * wx * k, 0.07f, Mathf.Sin(a) * wz * k);
                float s0 = 0.5f + (float)rnd.NextDouble() * 0.5f;
                P(root, PrimitiveType.Cylinder, root.InverseTransformPoint(q), new Vector3(s0, 0.01f, s0), pad);
                if (rnd.NextDouble() < 0.3) P(root, PrimitiveType.Sphere, root.InverseTransformPoint(q + Vector3.up * 0.07f), new Vector3(0.2f, 0.12f, 0.2f), bloom);
            }
        }
        if (!dress) g.data.water = extra.ToArray();
        AHModel.SetShadows(root.gameObject);
        Debug.Log("Ashen Hollow: " + list.Count + (ice ? " frozen pond" : " bog pools") + " filled");
    }

    // the web game's townsfolk baked into the city models (box bodies with stick limbs, standing by the gates): the
    // living townsfolk and guards walk the streets instead, so these stand-ins go
    static void HideBlockPeople(Transform world)
    {
        int n = 0;
        foreach (var t in world.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("obj")) continue;
            var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length < 3) continue;
            bool bodies = false, small = true;
            foreach (var r in rs)
            {
                var b = r.bounds; if (b.size.y > 1.9f || Mathf.Max(b.size.x, b.size.z) > 1.0f) { small = false; break; }
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                long tri = 0; for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) tri += mf.sharedMesh.GetIndexCount(k) / 3;
                if (tri > 250 && b.size.y > 0.8f) bodies = true;
            }
            if (!small) continue;
            if (!bodies)
            {
                // the plainer folk: a skin-coloured ball of a head on top of a 1.4-2 m stack of small pieces
                Bounds all = rs[0].bounds; foreach (var r in rs) all.Encapsulate(r.bounds);
                if (rs.Length < 5 || all.size.y < 1.3f || all.size.y > 2.1f) continue;
                foreach (var r in rs)
                {
                    var b = r.bounds; var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                    if (b.size.y < 0.18f || b.size.y > 0.42f || Mathf.Abs(b.size.x - b.size.y) > 0.08f || b.max.y < all.max.y - 0.35f) continue;
                    if (mf.sharedMesh.GetIndexCount(0) / 3 < 100) continue;
                    var m = r.sharedMaterial; if (m == null) continue;
                    Color c = m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.black;
                    if (c.r > 0.6f && c.r > c.g && c.g > c.b && c.r - c.b > 0.15f) { bodies = true; break; }
                }
                if (!bodies) continue;
            }
            foreach (var r in rs) r.enabled = false; n++;
        }
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " groups of old block townsfolk hidden");
    }

    static bool IsCity(string id) { return id == "city" || id.EndsWith("_city"); }

    // the walls' outline in world space (x0, z0, x1, z1), from the area's long thin boxes; false when there are none
    static bool WallRect(AHGame g, out Vector4 rect)
    {
        rect = Vector4.zero; if (g.data == null || g.data.boxes == null) return false;
        float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue; int n = 0;
        foreach (var b in g.data.boxes)
        {
            if (Mathf.Min(b.w, b.h) >= 2f || Mathf.Max(b.w, b.h) <= 8f) continue;
            foreach (var c in new[] { g.W(b.x, b.z), g.W(b.x + b.w, b.z + b.h) })
            { x0 = Mathf.Min(x0, c.x); x1 = Mathf.Max(x1, c.x); z0 = Mathf.Min(z0, c.z); z1 = Mathf.Max(z1, c.z); }
            n++;
        }
        if (n < 3) return false;
        rect = new Vector4(x0, z0, x1, z1); return true;
    }

    // cobblestones on the streets and squares inside a city's walls (the ground shader does the laying)
    static Texture2D cobble;
    public static void Pave(AHGame g, Material m)
    {
        if (m == null || !IsCity(AHGame.AreaId)) return;
        if (!m.HasProperty("_PaveMap")) { Debug.Log("Ashen Hollow: ground shader has no paving (" + m.shader.name + ")"); return; }
        Vector4 rect; if (!WallRect(g, out rect)) { Debug.Log("Ashen Hollow: no city walls to pave inside"); return; }
        if (cobble == null) cobble = Resources.Load<Texture2D>("AH/Textures/cobble");
        if (cobble == null) { Debug.Log("Ashen Hollow: no cobble texture"); return; }
        Debug.Log("Ashen Hollow: streets cobbled inside " + rect);
        m.SetTexture("_PaveMap", cobble); m.SetFloat("_PaveScale", 0.16f); m.SetFloat("_PaveAmt", 1f);
        m.SetVector("_PaveRect", new Vector4(rect.x - 0.5f, rect.y - 0.5f, rect.z + 0.5f, rect.w + 0.5f));
    }

    // a wide stretch of the land the city stands in, laid just under its ground: through the gates and over the walls you
    // see fields, sand, snow or ash running off to the horizon instead of a blank edge of the world
    static readonly Dictionary<string, Color> Land = new Dictionary<string, Color>
    {
        { "city", new Color(0.42f, 0.55f, 0.28f) }, { "hc_city", new Color(0.86f, 0.89f, 0.93f) }, { "mw_city", new Color(0.3f, 0.37f, 0.22f) },
        { "ss_city", new Color(0.8f, 0.62f, 0.4f) }, { "ch_city", new Color(0.2f, 0.17f, 0.16f) }, { "co_city", new Color(0.82f, 0.74f, 0.56f) },
    };
    public static void Outskirts(AHGame g, Transform world)
    {
        Color col; if (world == null || !Land.TryGetValue(AHGame.AreaId, out col)) return;
        Renderer ground = null; foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("AH_GROUND")) { ground = r; break; }
        if (ground == null) return;
        var gm = AHGame.LoadMat("AH/Materials/Ground", "AshenHollow/Ground"); if (gm == null) return;
        var white = new Texture2D(4, 4); var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white; white.SetPixels(px); white.Apply();
        gm.SetTexture("_BaseMap", white); gm.SetColor("_Tint", col);
        var b = ground.bounds;
        var go = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Quad), PrimitiveType.Quad); go.name = "Outskirts";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = new Vector3(b.center.x, b.min.y - 0.35f, b.center.z);   // under any sea laid just below the town
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(900f, 900f, 1f);
        var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = gm; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // street life in front of the buildings: crates and sacks at the market, a weapon rack at the smithy, barrels at
    // the tavern, banners at the hall, timber at the stable, and now and then a barrel or a wheelbarrow by a home
    static readonly Dictionary<string, GameObject> propCache = new Dictionary<string, GameObject>();
    const float PropK = 5.2f;
    static void Prop(Transform root, string name, Vector3 at, float yaw)
    {
        GameObject pf; if (!propCache.TryGetValue(name, out pf)) { pf = Resources.Load<GameObject>("AH/Models/KK/kk_" + name); propCache[name] = pf; }
        if (pf == null) return;
        var go = Object.Instantiate(pf, root, false); go.name = "Prop_" + name;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        go.transform.position = at; go.transform.rotation = Quaternion.Euler(0f, yaw, 0f); go.transform.localScale = Vector3.one * PropK;
        AHModel.SetShadows(go);
    }
    static void Props(Transform root, string m, string col, Vector3 centre, Vector3 dir, Vector2 lot, int seed)
    {
        var r = new System.Random(seed * 7919 + m.Length);
        Vector3 right = Vector3.Cross(Vector3.up, dir), front = centre + dir * (lot.y / 2f + 0.55f);
        float yaw = Quaternion.LookRotation(dir).eulerAngles.y;
        System.Func<float, float, Vector3> at = (side, out_) => front + right * side * lot.x * 0.5f + dir * out_;
        System.Func<float> jit = () => (float)(r.NextDouble() * 50 - 25);
        string[] kit;
        switch (m)
        {
            case "market": kit = new[] { "crate_A_big", "sack", "crate_B_small", "barrel" }; break;
            case "blacksmith": kit = new[] { "weaponrack", "bucket_water", "barrel" }; break;
            case "tavern": kit = new[] { "barrel", "barrel", "crate_A_big" }; break;
            case "barracks": Prop(root, "flag_" + col, at(-0.75f, 0f), yaw + 90f); Prop(root, "flag_" + col, at(0.75f, 0f), yaw + 90f); return;
            case "lumbermill": kit = new[] { "resource_lumber", "wheelbarrow" }; break;
            case "tower_A": kit = new[] { "bucket_water", "sack" }; break;
            case "church": return;
            default: { double p = r.NextDouble(); kit = p < 0.18 ? new[] { "barrel" } : p < 0.3 ? new[] { "wheelbarrow" } : p < 0.4 ? new[] { "crate_B_small", "sack" } : null; break; }
        }
        if (kit == null) return;
        for (int i = 0; i < kit.Length; i++)
        {
            float side = (i % 2 == 0 ? -1f : 1f) * (0.55f + 0.15f * (i / 2)), o = 0.1f + 0.45f * (i / 2);
            Prop(root, kit[i], at(side, o), yaw + jit());
        }
    }

    // the first house-for-rent lot of this area: its door and its middle (false when the area has none)
    public static bool RentLot(AHGame g, out Vector3 door, out Vector3 centre)
    {
        door = centre = Vector3.zero;
        if (data == null) { var ta = Resources.Load<TextAsset>("AH/Data/cityhouses"); if (ta == null) return false; data = AHJson.Parse(ta.text); }
        var list = AHJson.A(data, AHGame.AreaId); if (list == null) return false;
        foreach (var o in list)
        {
            if (AHJson.S(o, "use") != "rent" || !AHJson.Has(o, "dx")) continue;
            centre = g.W((float)AHJson.N(o, "cx"), (float)AHJson.N(o, "cz")); door = g.W((float)AHJson.N(o, "dx"), (float)AHJson.N(o, "dz"));
            return true;
        }
        return false;
    }

    // the lot's size across its front and from front to back, whichever way it faces
    static Vector2 LotAxes(AHGame g, Vector3 dir, float w, float d)
    {
        Vector3 xw = g.W(1f, 0f) - g.W(0f, 0f); xw.y = 0f;   // the area's x axis in the world
        float along = Mathf.Abs(Vector3.Dot(dir.normalized, xw.normalized));
        return along > 0.7f ? new Vector2(d, w) : new Vector2(w, d);
    }
    static Vector3 MeasureLocal(GameObject go)
    {
        var q = go.transform.rotation; go.transform.rotation = Quaternion.identity;
        var b = Measure(go); go.transform.rotation = q; return b.size;
    }
    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); return b;
    }
}
