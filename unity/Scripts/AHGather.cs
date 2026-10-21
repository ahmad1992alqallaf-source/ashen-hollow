// Ashen Hollow: gathering and the town's work stations, with the web game's rules (v67, interact / finishAction):
// chop trees (world_trees.json), mine rocks, pick herbs, fish (world.json rocks, HERBS, fishSpots), cook at a
// fire, smelt at the furnace, and make things at the anvil, loom, cauldron and jeweler's bench (rules.json RECIPES).
// Also professions: the ones you practise learn 25% faster and earn mastery (perks at 5/10/20/30/40).
using System.Collections.Generic;
using UnityEngine;

public class AHSpot
{
    public string kind;      // chop, mine, herb, fish, fire, furnace, anvil, loom, brew, jewel
    public string type;      // tree/pine, copper/tin/iron, sunpetal..., or the station's name
    public string name;
    public Vector3 pos;
    public float r;          // metres (the web r)
    public float until;      // Time.time when it is back (trees, rocks, herbs)
    public float reach;      // metres
    public bool temp;        // your own campfire
    public GameObject tempGo;
    // looks
    public GameObject fx;    // ore specks / herb flowers (hidden while spent)
    public Renderer body;    // the rock (greyed while spent)
    public Color bodyCol;
    public int tree = -1;    // index into the tree vertex groups
    public int forest = -1;  // index into AHForest.Trees (the new trees)
    public float lastRipple;
    public AHGate gate;
    public System.Action use;   // something you use with the action button (a dungeon chest)
    public bool rich;        // a rich vein (rocks) or an ancient tree: better grades, more XP, a golden shimmer
    // World of Warcraft style nodes: a rock holds a few ores, then it is gone and comes back a minute or so later
    // somewhere else nearby (herbs the same with one picking)
    public Transform look, look2;   // the rock (or plant) and its ore crystals, moved together
    public bool wander; public Vector3 home;             // gone, waiting to come back somewhere else
    public int charges = -1;        // ores left in this rock (-1: not counted yet)
}

public class AHRecipe
{
    public string station, outId, skill, masteryProf;
    public bool rare;   // a rare recipe: learned from a scroll (AHRareRecipes)
    public int lvl, xp, n = 1, masteryLv;
    public List<KeyValuePair<string, int>> mats = new List<KeyValuePair<string, int>>();
}

public static class AHGather
{
    public static readonly List<AHSpot> Spots = new List<AHSpot>();
    static AHGame g;

    // ---------- web tables ----------
    static object Rule(string k) { return AHJson.O(AHDB.Rules, k); }
    public static float Dur(string kind) { return (float)AHJson.N(Rule("DUR"), kind, 2f); }

    static Dictionary<string, List<AHRecipe>> recipes;
    public static List<AHRecipe> Recipes(string station)
    {
        if (recipes == null)
        {
            recipes = new Dictionary<string, List<AHRecipe>>();
            var all = AHJson.O(AHDB.Rules, "RECIPES") as Dictionary<string, object>;
            if (all != null)
                foreach (var kv in all)
                {
                    var l = new List<AHRecipe>();
                    var arr = kv.Value as List<object>;
                    if (arr != null)
                        foreach (var o in arr)
                        {
                            var r = new AHRecipe { station = kv.Key, outId = AHJson.S(o, "out"), skill = AHJson.S(o, "skill"), lvl = (int)AHJson.N(o, "lvl", 1), xp = (int)AHJson.N(o, "xp"), n = (int)AHJson.N(o, "n", 1) };
                            var m = AHJson.O(o, "mats") as Dictionary<string, object>;
                            if (m != null) foreach (var mk in m) r.mats.Add(new KeyValuePair<string, int>(mk.Key, (int)(double)mk.Value));
                            var ms = AHJson.O(o, "mastery");
                            if (ms != null) { r.masteryProf = AHJson.S(ms, "prof"); r.masteryLv = (int)AHJson.N(ms, "lv"); }
                            if (AHItems.Get(r.outId) != null && r.skill != null) l.Add(r);
                        }
                    AHRareRecipes.AddTo(kv.Key, l);
                    AHFashion.AddTo(kv.Key, l);
                    recipes[kv.Key] = l;
                }
        }
        List<AHRecipe> res;
        return recipes.TryGetValue(station, out res) ? res : new List<AHRecipe>();
    }

    public static string StationName(string kind)
    {
        switch (kind)
        {
            case "anvil": return "Anvil"; case "loom": return "Loom"; case "brew": return "Brewing cauldron";
            case "jewel": return "Jeweler’s bench"; case "furnace": return "Furnace"; case "fire": return "Campfire";
        }
        return AHJson.S(AHJson.O(AHDB.Table("home", "FARM_ST"), kind), "name", kind);
    }

    public static string StationHint(string kind)
    {
        switch (kind)
        {
            case "brew": return "Brew potions and elixirs from herbs you pick in the wild. Herblore rises as you pick and brew.";
            case "anvil": return "Smith weapons and armor from bars. Smelt ore into bars at the furnace.";
            case "jewel": return "Cut gems and set them into rings and amulets.";
        }
        var fs = AHJson.O(AHDB.Table("home", "FARM_ST"), kind);
        if (fs != null && kind != "loom") return AHJson.S(fs, "hint", "");
        return "Tailor leather and fur gear from hides, pelts and fangs you skin.";
    }

    // ---------- the meadow's spots ----------
    public static void Build(AHGame game, Transform world)
    {
        g = game;
        Spots.Clear();
        float S = AHDB.S;
        // group objects of the world model (rocks and herbs are a body + a group of specks/flowers)
        var objs = new List<Transform>();
        if (world != null) foreach (var t in world.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("obj") && t.childCount >= 2) objs.Add(t);

        var w = AHDB.File("world");
        var rocks = AHJson.A(w, "rocks");
        if (rocks != null) foreach (var o in rocks)
            {
                var s = Spot("mine", AHJson.S(o, "type", "copper"), o, 62f);
                if (s == null) continue;
                s.name = AHJson.S(AHJson.O(Rule("ROCKS"), s.type), "name", "Rock");
                LinkLook(s, objs);
            }
        var herbs = AHJson.A(w, "HERBS");
        if (herbs != null) foreach (var o in herbs)
            {
                var s = Spot("herb", AHJson.S(o, "type", "sunpetal"), o, 60f);
                if (s == null) continue;
                var it = AHItems.Get(s.type); s.name = it != null ? it.name : s.type;
                LinkLook(s, objs);
            }
        var fish = AHJson.A(w, "fishSpots");
        if (fish != null) foreach (var o in fish) { var s = Spot("fish", AHJson.S(o, "kind", "pond"), o, 80f); if (s != null) s.name = "Fishing spot"; }
        var st = AHJson.A(w, "stations");
        if (st != null) foreach (var o in st)
            {
                string k = AHJson.S(o, "kind");
                if (k != "fire" && k != "furnace" && k != "anvil" && k != "loom" && k != "brew" && k != "jewel") continue;
                var s = Spot(k, k, o, 68f);
                if (s != null) s.name = AHJson.S(o, "name", StationName(k));
            }
        var trees = AHJson.A(AHDB.File("world_trees"), "trees");
        if (trees != null) foreach (var o in trees)
            {
                var a = o as List<object>;
                if (a == null || a.Count < 4) continue;
                float x = (float)(double)a[0], y = (float)(double)a[1], r = (float)(double)a[2];
                Vector3 p = g.W(x * S, y * S);
                if (!g.InArea(p)) continue;
                var s = new AHSpot { kind = "chop", type = (string)a[3], pos = p, r = r * S, reach = (30f + r) * S };
                s.name = AHJson.S(AHJson.O(Rule("TREES"), s.type), "name", "tree");
                Spots.Add(s);
            }
        LinkTrees(world);
        MarkRich();
        // city gates
        if (game.data.gates != null)
            foreach (var gt in game.data.gates)
                Spots.Add(new AHSpot { kind = "gate", type = gt.to, name = gt.label, pos = game.W(gt.x, gt.z), r = 0.5f, reach = gt.r, gate = gt });
    }

    // about one rock in nine is a rich vein and one tree in twelve an ancient one (the same ones every visit)
    static void MarkRich()
    {
        foreach (var s in Spots)
        {
            if (s.kind != "mine" && s.kind != "chop") continue;
            int h = Mathf.Abs((Mathf.RoundToInt(s.pos.x * 7.3f) * 73856093) ^ (Mathf.RoundToInt(s.pos.z * 7.3f) * 19349663) ^ (AHGame.AreaId ?? "").GetHashCode());
            s.rich = h % (s.kind == "mine" ? 9 : 12) == 0;
            if (s.rich) s.name = (s.kind == "mine" ? "Rich " : "Ancient ") + char.ToLowerInvariant(s.name[0]) + s.name.Substring(1);
        }
    }
    public static float RichK(AHSpot s) { return s != null && s.rich ? 3f : 1f; }

    // ---------- nodes that move (World of Warcraft style) ----------
    static void Deplete(AHSpot s, float wait)
    {
        s.until = Time.time + wait; s.charges = -1;
        if (s.look == null) return;   // an old-style spot: it just greys out and comes back where it was
        s.wander = true;
        s.look.gameObject.SetActive(false); if (s.look2 != null) s.look2.gameObject.SetActive(false);
        AHSpark.Burst(s.pos + Vector3.up * 0.4f, new Color(0.8f, 0.75f, 0.65f, 0.8f), 14, 2f, 0.7f, 0.1f, 1.2f);
    }
    // back somewhere else: near a place where this kind already grows, on open ground (not a road, water, a tree,
    // a house), clear of other nodes, fires and stations, and out of the hero's sight-line close by
    static void Relocate(AHGame game, AHSpot s)
    {
        s.wander = false; s.until = 0f;
        var homes = new List<Vector3>();
        foreach (var o in Spots) if (o.kind == s.kind && o.type == s.type) homes.Add(o.home);
        if (homes.Count == 0) homes.Add(s.home);
        Vector3 hero = game.player != null ? game.player.transform.position : Vector3.one * 1e6f, at = s.home; bool found = false;
        for (int tries = 0; tries < 60 && !found; tries++)
        {
            Vector3 h = homes[Random.Range(0, homes.Count)];
            float a = Random.value * Mathf.PI * 2f, r = Random.Range(3f, 24f);
            Vector3 c = new Vector3(h.x + Mathf.Cos(a) * r, s.pos.y, h.z + Mathf.Sin(a) * r);
            if (!game.InArea(c) || game.InTown(c) || game.Blocked(c, 1.3f) || AHNav.IsRoad(game, c) || AHSea.Sea(c, 1.5f)) continue;
            Vector3 dh = c - hero; dh.y = 0f; if (dh.sqrMagnitude < 14f * 14f) continue;
            bool clear = true;
            foreach (var o in Spots) { if (o == s) continue; Vector3 d = o.pos - c; d.y = 0f; if (d.sqrMagnitude < (o.kind == "chop" ? 3f * 3f : 6f * 6f)) { clear = false; break; } }
            if (!clear) continue;
            at = c; found = true;
        }
        Vector3 delta = at - s.pos; delta.y = 0f;
        game.MoveBlocker(s.pos, at);
        s.pos += delta;
        if (s.look != null) { s.look.position += delta; s.look.gameObject.SetActive(true); }
        if (s.look2 != null) { s.look2.position += delta; s.look2.gameObject.SetActive(true); }
        if (s.kind == "mine") { bool wasRich = s.rich; s.rich = Random.value < 0.11f; if (s.rich != wasRich) s.name = s.rich ? "Rich " + char.ToLowerInvariant(s.name[0]) + s.name.Substring(1) : s.name.Replace("Rich ", "").Substring(0, 1).ToUpperInvariant() + s.name.Replace("Rich ", "").Substring(1); }
    }

    static AHSpot Spot(string kind, string type, object o, float reachUnits)
    {
        float S = AHDB.S;
        Vector3 p = g.W((float)AHJson.N(o, "x") * S, (float)AHJson.N(o, "y") * S);
        if (!g.InArea(p)) return null;
        var s = new AHSpot { kind = kind, type = type, pos = p, home = p, r = (float)AHJson.N(o, "r", 16) * S, reach = reachUnits * S };
        Spots.Add(s);
        return s;
    }

    static void LinkLook(AHSpot s, List<Transform> objs)
    {
        Transform best = null; float bd = 0.5f;
        foreach (var t in objs) { Vector3 d = t.position - s.pos; d.y = 0; if (d.magnitude < bd) { bd = d.magnitude; best = t; } }
        if (best == null) return;
        for (int i = 0; i < best.childCount; i++)
        {
            var c = best.GetChild(i);
            if (c.childCount >= 3) s.fx = c.gameObject;
            else if (s.body == null) { s.body = c.GetComponent<Renderer>(); if (s.body != null) s.bodyCol = s.body.sharedMaterial != null && s.body.sharedMaterial.HasProperty("baseColorFactor") ? s.body.sharedMaterial.GetColor("baseColorFactor") : Color.grey; }
        }
    }

    // ---------- trees: the world model has all trees baked into a few meshes; each tree's vertices are
    // found once so a felled tree can shrink to a stump (web setTreeMatrix) and grow back ----------
    class TreeMesh { public Mesh mesh; public Vector3[] orig, cur; public Transform tf; }
    static readonly List<TreeMesh> treeMeshes = new List<TreeMesh>();
    // per tree: per mesh, which vertices and whether it is the trunk
    class TreePart { public int mesh; public int[] idx; public bool trunk; public float baseY; public Vector3 centre; }
    static readonly List<List<TreePart>> treeParts = new List<List<TreePart>>();
    static bool treesDirty;

    static void LinkTrees(Transform world)
    {
        treeMeshes.Clear(); treeParts.Clear();
        if (world == null) return;
        var trees = new List<AHSpot>();
        foreach (var s in Spots) if (s.kind == "chop") trees.Add(s);
        if (trees.Count == 0) return;
        // a grid of trees, 4 m cells
        var grid = new Dictionary<long, List<int>>();
        for (int i = 0; i < trees.Count; i++) { long k = Cell(trees[i].pos); List<int> l; if (!grid.TryGetValue(k, out l)) grid[k] = l = new List<int>(); l.Add(i); }
        var parts = new List<Dictionary<int, List<int>>>();   // per tree: mesh -> vertices
        for (int i = 0; i < trees.Count; i++) parts.Add(new Dictionary<int, List<int>>());
        foreach (var mf in world.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mf.name.StartsWith("AH_INST") || mf.sharedMesh == null) continue;
            if (!mf.sharedMesh.isReadable) { Debug.LogWarning("Ashen Hollow: tree mesh " + mf.name + " is not readable; felled trees will not shrink"); continue; }
            var mesh = Object.Instantiate(mf.sharedMesh);
            mf.sharedMesh = mesh;
            var tm = new TreeMesh { mesh = mesh, orig = mesh.vertices, tf = mf.transform };
            tm.cur = (Vector3[])tm.orig.Clone();
            int mi = treeMeshes.Count; treeMeshes.Add(tm);
            for (int v = 0; v < tm.orig.Length; v++)
            {
                Vector3 wp = tm.tf.TransformPoint(tm.orig[v]);
                int bi = -1; float bd = 3.2f;
                long c = Cell(wp); int cx = (int)(c >> 32), cz = (int)(c & 0xffffffff);
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                    {
                        List<int> l; if (!grid.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                        foreach (int ti in l) { Vector3 d = wp - trees[ti].pos; d.y = 0; float m = d.magnitude; if (m < bd) { bd = m; bi = ti; } }
                    }
                if (bi < 0) continue;
                List<int> vl; if (!parts[bi].TryGetValue(mi, out vl)) parts[bi][mi] = vl = new List<int>(); vl.Add(v);
            }
        }
        for (int i = 0; i < trees.Count; i++)
        {
            var list = new List<TreePart>();
            foreach (var kv in parts[i])
            {
                var tm = treeMeshes[kv.Key];
                float minY = float.MaxValue; Vector3 sum = Vector3.zero;
                foreach (int v in kv.Value) { minY = Mathf.Min(minY, tm.orig[v].y); sum += tm.orig[v]; }
                list.Add(new TreePart { mesh = kv.Key, idx = kv.Value.ToArray(), baseY = minY, centre = sum / kv.Value.Count, trunk = minY - tm.tf.InverseTransformPoint(trees[i].pos).y < 0.4f });
            }
            trees[i].tree = treeParts.Count; treeParts.Add(list);
        }
    }

    static long Key(int x, int z) { return ((long)x << 32) | (uint)z; }
    static long Cell(Vector3 p) { return Key(Mathf.FloorToInt(p.x / 4f), Mathf.FloorToInt(p.z / 4f)); }

    static void SetFelled(AHSpot s, bool felled)
    {
        if (AHForest.SetFelled(s, felled)) return;
        if (s.tree < 0 || s.tree >= treeParts.Count) return;
        foreach (var part in treeParts[s.tree])
        {
            var tm = treeMeshes[part.mesh];
            foreach (int v in part.idx)
            {
                Vector3 o = tm.orig[v];
                if (!felled) tm.cur[v] = o;
                else if (part.trunk) tm.cur[v] = new Vector3(o.x, part.baseY + (o.y - part.baseY) * 0.2f, o.z);
                else tm.cur[v] = part.centre;   // the crown is gone
            }
        }
        treesDirty = true;
    }

    // ---------- every frame: things grow back, the spent look, ripples on fishing spots ----------
    static float regrowT;
    public static void Tick(AHGame game)
    {
        regrowT -= Time.deltaTime;
        if (regrowT > 0f) return;
        regrowT = 0.5f;
        float now = Time.time;
        var p = game.player;
        for (int i = Spots.Count - 1; i >= 0; i--)
        {
            var s = Spots[i];
            if (s.temp && now > s.until) { if (s.tempGo != null) Object.Destroy(s.tempGo); Spots.RemoveAt(i); continue; }
            bool spent = s.until > now;
            if (s.fx != null && s.fx.activeSelf == spent) s.fx.SetActive(!spent);
            if (s.body != null)
            {
                var mpb = new MaterialPropertyBlock();
                Color c = spent ? s.bodyCol * 0.55f : s.bodyCol; c.a = 1f;
                mpb.SetColor("baseColorFactor", c); mpb.SetColor("_BaseColor", c);
                s.body.SetPropertyBlock(spent ? mpb : null);
            }
            if (s.kind == "chop" && s.until != 0f && s.until <= now) { s.until = 0f; SetFelled(s, false); }
            if (s.wander && s.until <= now) Relocate(game, s);
            if (s.rich && !spent && p != null && (s.pos - p.transform.position).sqrMagnitude < 1600f)
                AHSpark.Burst(s.pos + Vector3.up * (s.kind == "chop" ? 1.6f : 0.5f) + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f)), new Color(1f, 0.85f, 0.4f, 0.9f), 3, 0.5f, 1.1f, 0.07f, 1.2f);
            if (s.kind == "fish" && p != null && (s.pos - p.transform.position).sqrMagnitude < 900f && now - s.lastRipple > 1.4f)
            {
                s.lastRipple = now + Random.value * 0.6f;
                AHFx.Ring(s.pos, 0.25f, 0.9f, new Color(0.92f, 0.96f, 1f, 0.6f), 1.3f);
            }
        }
        if (treesDirty) { treesDirty = false; foreach (var tm in treeMeshes) { tm.mesh.vertices = tm.cur; tm.mesh.RecalculateBounds(); } }
    }

    // ---------- web interactTarget: the nearest thing you can work on ----------
    public static AHSpot Nearest(Vector3 at)
    {
        AHSpot best = null; float bd = float.MaxValue, now = Time.time;
        foreach (var s in Spots)
        {
            if ((s.kind == "chop" || s.kind == "mine" || s.kind == "herb") && s.until > now) continue;
            if (AHInterior.Inside && s.type != "house") continue;   // in your house: only the house's own spots
            Vector3 d = s.pos - at; d.y = 0; float m = d.magnitude;
            if (m < s.reach && m < bd) { bd = m; best = s; }
        }
        return best;
    }

    // web labelFor
    public static string Label(AHSpot s)
    {
        switch (s.kind)
        {
            case "mine": return "Mine " + (s.rich ? "rich " + s.name.Split(' ')[1].ToLowerInvariant() : s.name.Split(' ')[0].ToLowerInvariant());
            case "chop": return "Chop " + s.name;
            case "fish": return AHFishing.School(s) ? "Fish the school" : "Fish";
            case "fire": return "Cook";
            case "furnace": return "Smelt";
            case "anvil": return "Smith";
            case "loom": return "Tailor";
            case "brew": return "Brew";
            case "jewel": return "Jeweler";
            case "herb": return "Pick " + s.name.ToLowerInvariant();
            case "gate": return s.name;
            case "use": return s.name;
            case "oven": return "Cook"; case "mill": return "Mill"; case "dairy": return "Dairy"; case "spin": return "Spin"; case "compost": return "Compost";
        }
        return "Use";
    }

    // ---------- web canDo ----------
    public static bool CanDo(AHPlayer p, AHSpot s, bool loud)
    {
        string warn = null;
        switch (s.kind)
        {
            case "herb": { int req = (int)AHJson.N(AHJson.O(Rule("HERB_T"), s.type), "req", 1); if (p.Skill("herblore") < req) warn = "Requires Herblore " + req + " to pick " + s.name.ToLowerInvariant() + "."; break; }
            case "mine": { int req = (int)AHJson.N(AHJson.O(Rule("ROCKS"), s.type), "req", 1); if (p.Skill("mining") < req) warn = "Requires Mining " + req + " to mine " + s.name.ToLowerInvariant() + "."; break; }
            case "chop": { int req = (int)AHJson.N(AHJson.O(Rule("TREES"), s.type), "req", 1); if (p.Skill("woodcutting") < req) warn = "Requires Woodcutting " + req + " to chop pines."; break; }
            case "fish": { int fl = p.Skill("fishing") + (p.Spec("fisher", "angler") ? 4 : 0), need = AHFish.Of(AHGame.AreaId)[0].lvl; if (s.type == "lava") need = Mathf.Max(need, 18); if (fl < need) warn = "Requires Fishing " + need + " to fish here."; break; }
            case "fire": if (RawToCook(p) == null) warn = "Nothing to cook. Catch fish or hunt for raw meat."; break;
            case "furnace": if (SmeltRecipe(p) == null) warn = "Need 1 copper + 1 tin ore (or iron ore at Smithing 10)."; break;
        }
        if (warn != null && loud && g.ui != null) g.ui.Toast(warn);
        return warn == null;
    }

    static readonly string[] Meats = { "raw_beef", "raw_pork", "raw_mutton", "raw_chicken", "raw_duck" };
    public static string RawToCook(AHPlayer p)
    {
        var b = p.bag;
        string fish = AHFish.BestRaw(p); if (fish != null) return fish;   // the best fish this cook can manage
        if (b.Count("raw_meat") > 0) return "raw_meat";
        foreach (var k in Meats) if (b.Count(k) > 0) return k;
        return null;
    }

    public static string SmeltRecipe(AHPlayer p)
    {
        if (AHGrades.CountAll(p.bag, "iron_ore") > 0 && p.Skill("smithing") >= 10) return "iron";
        if (AHGrades.CountAll(p.bag, "copper_ore") > 0 && AHGrades.CountAll(p.bag, "tin_ore") > 0) return "bronze";
        return null;
    }

    public static bool HasMats(AHPlayer p, AHRecipe r) { foreach (var m in r.mats) if (AHGrades.CountAll(p.bag, m.Key) < m.Value) return false; return true; }
    public static bool MasteryOk(AHPlayer p, AHRecipe r) { return r.masteryProf == null || (p.profs.Contains(r.masteryProf) && p.MasteryLv(r.masteryProf) >= r.masteryLv); }
    public static bool CanMake(AHPlayer p, AHRecipe r) { return (!r.rare || AHRareRecipes.Known(p, r.outId)) && AHRecipeDrops.Known(p, r) && p.Skill(r.skill) >= r.lvl && MasteryOk(p, r) && HasMats(p, r) && (!AHArtisan.Only(r.outId) || p.path == "artisan"); }

    // ---------- the action you are doing (web P.action) ----------
    public static AHSpot actSpot;
    public static AHRecipe actRecipe;
    static float actT, actDur;
    public static bool Busy { get { return actSpot != null; } }
    public static float Frac { get { return AHFishing.Active ? AHFishing.Frac : actSpot != null && actDur > 0f ? Mathf.Clamp01(actT / actDur) : 0f; } }
    public static int craftLeft;   // "Create" more than one: how many are still to make after this one
    public static void Cancel() { actSpot = null; actRecipe = null; craftLeft = 0; if (AHFishing.Active) AHFishing.Stop(); }

    // ---------- the gathering knack (a timing mini-game) ----------
    // While you chop, mine, fish or pick herbs, a gold mark sits somewhere on the work bar. Tap the action button as the
    // bar crosses it: a perfect strike finishes the work at once and brings one more of what you gather (the gold
    // mark is a little wider for the skilled). One try per swing; a miss just carries on as usual.
    public static float knackAt, knackW; public static bool knackTried, knackHit, perfect;
    public static string knackSay; public static float knackSayT;
    public static bool KnackOn { get { return actSpot != null && actRecipe == null && IsGather(actSpot.kind) && !AHFishing.Active; } }
    public static bool IsGather(string kind) { return kind == "chop" || kind == "mine" || kind == "fish" || kind == "herb"; }
    static string SkillOf(string kind) { return kind == "chop" ? "woodcutting" : kind == "mine" ? "mining" : kind == "fish" ? "fishing" : "herblore"; }
    static void NewKnack(AHPlayer p)
    {
        knackTried = false; knackHit = false;
        if (actSpot == null || !IsGather(actSpot.kind)) return;
        knackW = 0.12f + 0.004f * p.Skill(SkillOf(actSpot.kind));
        knackAt = Random.Range(0.42f, 0.86f - knackW * 0.5f);
    }
    public static void Strike(AHPlayer p)
    {
        if (AHFishing.Active) { AHFishing.Tap(p); return; }
        if (!KnackOn || knackTried) return;
        knackTried = true; float f = Frac;
        if (Mathf.Abs(f - knackAt) <= knackW * 0.5f)
        {
            knackHit = true; perfect = true; actT = actDur;   // done at once
            knackSay = "Perfect!"; knackSayT = 1.2f; AHSound.Play("coin");
        }
        else { knackSay = f < knackAt ? "Too soon" : "Too late"; knackSayT = 0.9f; }
    }
    // a perfect strike's extra (AHComp.Forage, for the thing just gathered)
    public static bool TakePerfect() { bool b = perfect; perfect = false; return b; }

    // web profSpeed
    static float Speed(AHPlayer p, string kind)
    {
        float m = kind == "chop" || kind == "mine" || kind == "fish" || kind == "herb" || kind == "skin" ? 1f + AHMeal.Fx(p, "gather") : 1f;
        m *= AHWeather.GatherK(kind);   // rain: the fish bite and the herbs open
        m *= AHBags.SpeedK(p, kind);    // the tool on your belt
        if (kind == "mine") return m * (p.Perk("miner", 5) ? 1.15f : 1f) * (p.Perk("miner", 40) ? 1.2f : 1f);
        if (kind == "fish") return m * (p.Perk("fisher", 5) ? 1.15f : 1f) * (p.Perk("fisher", 40) ? 1.2f : 1f);
        return m;
    }

    public static void Start(AHPlayer p, AHSpot s, AHRecipe r = null)
    {
        if (s == null) return;
        if (s.use != null) { s.use(); return; }
        if (s.kind == "gate" && s.gate.dung != null && !AHDungeon.CanEnter(g, s.gate.dung)) return;
        if (s.kind == "gate") { g.Travel(s.gate.to, s.gate.tx, s.gate.tz, s.gate.f); return; }
        if (r == null && (s.kind == "anvil" || s.kind == "loom" || s.kind == "brew" || s.kind == "jewel" || s.kind == "oven" || s.kind == "mill" || s.kind == "dairy" || s.kind == "spin" || s.kind == "compost")) { g.ui.OpenCraft(s); return; }
        if (r == null && !CanDo(p, s, true)) return;
        actSpot = s; actRecipe = r; actT = 0f;
        if (r == null && AHFishing.IsFishing(s)) { actDur = 1e9f; AHFishing.Begin(p, s); return; }   // World of Warcraft fishing: cast, bobber, bite, hook
        actDur = (r != null ? 2.2f : Dur(s.kind)) / Speed(p, s.kind);
        NewKnack(p);
        // the woodcutter and the miner swing their tools; the herbalist kneels to pick; the angler casts a line
        if (r != null) p.BeginWork(s.pos, s.kind == "anvil" || s.kind == "furnace" || s.kind == "jewel" ? "Work_Hammer|Interact" : "Interact");
        else p.BeginWork(s.pos, s.kind == "chop" ? "Work_Chop|Chop" : s.kind == "mine" ? "Work_Mine|Chop" : s.kind == "herb" || s.kind == "bog" || s.kind == "reef" ? "Work_Gather|Harvest" : s.kind == "fish" || s.kind == "lava" || s.kind == "sea" ? "Work_Fish|Fish" : "Interact",
            s.kind == "chop" ? "axe" : s.kind == "mine" ? "pick" : s.kind == "fish" || s.kind == "lava" || s.kind == "sea" ? "rod" : null);
    }

    static bool FireSpotOk(Vector3 c)
    {
        if (g.Blocked(c, 0.5f)) return false;
        foreach (var o in Spots)
        {
            if (o.wander) continue;
            Vector3 d = o.pos - c; d.y = 0f;
            float keep = o.kind == "chop" ? 2.2f : o.kind == "fish" ? 1.5f : 2.6f;
            if (d.sqrMagnitude < keep * keep) return false;
        }
        return true;
    }

    // web: lighting your own campfire in the wild with logs
    public static bool CanLight(AHPlayer p) { return !p.dead && !AHInterior.Inside && !g.InTown(p.transform.position) && (p.bag.Count("logs") > 0 || p.bag.Count("pine_logs") > 0); }
    public static void StartLight(AHPlayer p)
    {
        if (g.InTown(p.transform.position)) { g.ui.Toast("Use the town campfire. You can light your own in the wild."); return; }
        if (p.bag.Count("logs") <= 0 && p.bag.Count("pine_logs") <= 0) { g.ui.Toast("You need logs. Chop a tree first."); return; }
        // a fire needs open ground: never on a rock, a tree, a herb, a fishing spot or a station
        Vector3 at = Vector3.zero; bool ok = false;
        foreach (float turn in new[] { 0f, 50f, -50f, 100f, -100f, 180f })
        {
            Vector3 c = p.transform.position + Quaternion.Euler(0f, turn, 0f) * p.transform.forward * 34f * AHDB.S;
            if (FireSpotOk(c)) { at = c; ok = true; break; }
        }
        if (!ok) { g.ui.Toast("No room for a fire here. Step away from the rocks, trees and plants."); return; }
        var spot = new AHSpot { kind = "light", pos = at, reach = 1f };
        actSpot = spot; actRecipe = null; actT = 0f; actDur = Dur("light");
    }

    // call every frame from the player; moving stops the work
    public static void Update(AHPlayer p, float dt, bool moving)
    {
        if (actSpot == null) return;
        if (moving || p.dead) { Cancel(); return; }
        Vector3 dd = actSpot.pos - p.transform.position; dd.y = 0;
        if (actSpot.kind != "light" && dd.magnitude > actSpot.reach + 1f) { Cancel(); return; }   // pushed or carried away
        if (AHFishing.Active) { AHFishing.Tick(p, dt); return; }
        actT += dt;
        Feel(p, actSpot, actT - dt, actT);
        if (actT < actDur) return;
        var s = actSpot; var r = actRecipe;
        actSpot = null; actRecipe = null;
        bool again = Finish(p, s, r);
        perfect = false;
        if (again && CanDo(p, s, false)) { actSpot = s; actRecipe = null; actT = 0f; actDur = Dur(s.kind) / Speed(p, s.kind); NewKnack(p); }
        else if (r != null && craftLeft > 0 && CanMake(p, r)) { craftLeft--; actSpot = s; actRecipe = r; actT = 0f; actDur = 2.2f / Speed(p, s.kind); }
        else craftLeft = 0;
    }

    // the feel of the work, in time with the swing: wood chips fly from the axe, sparks and grit from the pick, leaves
    // and glints as you pick herbs, rings spreading round your line while you fish
    static bool Crossed(float a, float b, float period, float at) { return Mathf.Floor((a - at) / period) != Mathf.Floor((b - at) / period) && b > at; }
    static void Feel(AHPlayer p, AHSpot s, float a, float b)
    {
        if (s == null || actRecipe != null) return;
        Vector3 toMe = p.transform.position - s.pos; toMe.y = 0f; toMe = toMe.sqrMagnitude > 0.01f ? toMe.normalized : Vector3.forward;
        switch (s.kind)
        {
            case "chop":
                if (Crossed(a, b, 1f, 0.45f)) { Vector3 at = s.pos + Vector3.up * 0.9f + toMe * 0.35f; AHSpark.Burst(at, new Color(0.62f, 0.42f, 0.22f), 10, 3.2f, 0.6f, 0.07f, 1.6f); AHSpark.Burst(at, new Color(0.9f, 0.75f, 0.5f), 4, 2f, 0.4f, 0.05f, 1f); AHJuice.Shake(0.03f, 0.08f); }
                break;
            case "mine":
                if (Crossed(a, b, 1f, 0.5f)) { Vector3 at = s.pos + Vector3.up * 0.35f + toMe * 0.45f; AHSpark.Burst(at, new Color(1f, 0.85f, 0.4f), 12, 4f, 0.35f, 0.05f, 1.2f); AHSpark.Burst(at, new Color(0.55f, 0.55f, 0.58f), 8, 2.4f, 0.6f, 0.08f, 1.4f); AHJuice.Shake(0.035f, 0.08f); }
                break;
            case "herb": case "bog": case "reef":
                if (Crossed(a, b, 0.8f, 0.3f)) { Vector3 at = s.pos + Vector3.up * 0.25f; AHSpark.Burst(at, new Color(0.55f, 1f, 0.5f), 6, 1.2f, 0.7f, 0.06f, 1.4f); AHSpark.Burst(at, new Color(1f, 1f, 0.7f), 3, 0.8f, 0.8f, 0.04f, 1.8f); }
                break;
            case "fish": case "sea": case "lava":
                if (Crossed(a, b, 2.2f, 0.2f)) { g.Ripple(s.pos, 1f); AHSpark.Ring(s.pos + Vector3.up * 0.05f, s.kind == "lava" ? new Color(1f, 0.5f, 0.15f) : new Color(0.75f, 0.9f, 1f), 10, 1.1f, 0.6f, 0.08f); }
                break;
        }
    }

    // web finishAction. Returns true to keep going (chopping, fishing, cooking, smelting repeat)
    static bool Finish(AHPlayer p, AHSpot s, AHRecipe rec)
    {
        var bag = p.bag; var ui = g.ui; float now = Time.time;
        AHSound.Play(s.kind == "chop" ? "chop" : s.kind == "mine" ? "mine" : s.kind == "fish" ? "splash" : s.kind == "anvil" || s.kind == "furnace" || s.kind == "jewel" ? "anvil" : s.kind == "fire" || s.kind == "oven" || s.kind == "light" ? "fire" : s.kind == "brew" ? "splash" : "step");
        switch (s.kind)
        {
            case "chop":
                {
                    var tr = AHJson.O(Rule("TREES"), s.type);
                    string log = AHJson.S(tr, "log", "logs"); int gr;
                    string glog = Graded(p, log, "woodcutting", (int)AHJson.N(tr, "req", 1), s, out gr);
                    if (!bag.Add(glog)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); return false; } AHComp.Forage(g, log); GradeFx(p, glog, gr);
                    p.GainXp("woodcutting", Mathf.RoundToInt((int)AHJson.N(tr, "xp", 25) * (s.rich ? 1.5f : 1f)), false); AHProfPets.OnGather(g, "woodcutting");
                    g.quests.Event("gather", log, 1, g);
                    if (Random.value < AHJson.N(tr, "fell", 0.35)) { s.until = now + Random.Range(60f, 90f); SetFelled(s, true); ui.Toast("The " + s.name + " falls. It grows back soon."); return false; }
                    return true;
                }
            case "herb":
                {
                    var h = AHJson.O(Rule("HERB_T"), s.type);
                    int gr; string gh = Graded(p, s.type, "herblore", (int)AHJson.N(h, "req", 1), s, out gr);
                    if (!bag.Add(gh)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); return false; } AHComp.Forage(g, s.type); GradeFx(p, gh, gr);
                    p.GainXp("herblore", (int)AHJson.N(h, "xp", 14), false); AHProfPets.OnGather(g, "herblore");
                    if (p.Perk("alchemist", 20) && Random.value < 0.3f) bag.Add(s.type);
                    Deplete(s, Random.Range(60f, 90f));   // picked: it grows again a minute or so later, somewhere nearby
                    g.quests.Event("gather", s.type, 1, g);
                    return false;
                }
            case "mine":
                {
                    var rk = AHJson.O(Rule("ROCKS"), s.type);
                    string ore = AHJson.S(rk, "ore", "copper_ore"); int gr;
                    string gore = Graded(p, ore, "mining", (int)AHJson.N(rk, "req", 1), s, out gr);
                    if (!bag.Add(gore)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); return false; } AHComp.Forage(g, ore); GradeFx(p, gore, gr);
                    p.GainXp("mining", Mathf.RoundToInt((int)AHJson.N(rk, "xp", 18) * (s.rich ? 1.5f : 1f)), false); AHProfPets.OnGather(g, "mining");
                    // a rock holds 2-4 ores (a rich vein 4-6); the last one takes it away, to come back elsewhere
                    if (s.charges < 0) s.charges = s.rich ? Random.Range(4, 7) : Random.Range(2, 5);
                    s.charges--;
                    bool gone = s.charges <= 0;
                    if (gone) Deplete(s, p.Spec("miner", "delve") ? Random.Range(45f, 65f) : Random.Range(60f, 90f));
                    g.quests.Event("gather", ore, 1, g);
                    // web mineBonus: double ore, and now and then a gem
                    float dbl = p.Perk("miner", 30) ? 0.35f : p.Perk("miner", 10) ? 0.2f : 0f;
                    if (dbl > 0f && Random.value < dbl) { bag.Add(ore); ui.Float(p.transform.position + Vector3.up * 2.6f, "Double ore!", new Color(1f, 0.84f, 0.42f)); }
                    float gem = (0.005f + (p.Perk("miner", 20) ? 0.02f : 0f)) * (p.Spec("miner", "prospect") ? 2.5f : 1f);
                    if (Random.value < gem)
                    {
                        float x = Random.value; string gid = x < 0.45f ? "sapphire" : x < 0.75f ? "ruby" : x < 0.95f ? "emerald" : "diamond";
                        if (bag.Add(gid)) ui.Banner(AHItems.Get(gid).name, "You found a gem!");
                    }
                    if (gone) { ui.Toast("The vein is spent. Another will turn up somewhere nearby.", 2f); return false; }
                    return true;   // keep swinging while ore is left
                }
            case "fish": return false;   // fishing runs on its own (AHFishing), it lands its catch with GrantFish
            case "fire":
                {
                    string raw = RawToCook(p); if (raw == null) return false;
                    int req = AHFish.Get(raw) != null ? AHFish.CookReq(raw) : 1;
                    float burn = Mathf.Max(0.03f, 0.34f - (p.Skill("cooking") - req) * 0.035f);
                    string cooked = AHFish.Cooked(raw) ?? "meat";
                    bool burnt = Random.value < burn; string outId = burnt ? "burnt" : cooked;
                    if (!bag.Fits(new[] { outId }) && bag.Count(raw) > 1) { ui.Toast("Your bag is full. Make room to keep cooking."); return false; }
                    g.quests.Event("craft", "fire", 1, g);
                    bag.Take(raw);
                    if (burnt) { bag.Add("burnt"); ui.Toast("You burned the " + AHItems.Get(raw).name.Replace("Raw ", "") + "."); }
                    else { bag.Add(cooked); p.GainXp("cooking", 21 + req * 3, false); }
                    AHCookOff.OnCook(g, cooked, burnt);   // scored for the weekly cook-off
                    return RawToCook(p) != null;
                }
            case "furnace":
                {
                    string r = SmeltRecipe(p); if (r == null) return false;
                    string bar = r == "iron" ? "iron_bar" : "bronze_bar";
                    if (!bag.Fits(new[] { bar }) && (r == "iron" ? AHGrades.CountAll(bag, "iron_ore") > 1 : AHGrades.CountAll(bag, "copper_ore") > 1 && AHGrades.CountAll(bag, "tin_ore") > 1)) { ui.Toast("Your bag is full. Make room to keep smelting."); return false; }
                    // the bar keeps the best grade of the ore that went in
                    int gs, best, best2;
                    if (r == "iron") { AHGrades.TakeAll(bag, "iron_ore", 1, out gs, out best); p.GainXp("smithing", 30, false); }
                    else { AHGrades.TakeAll(bag, "copper_ore", 1, out gs, out best); AHGrades.TakeAll(bag, "tin_ore", 1, out gs, out best2); best = Mathf.Max(best, best2); p.GainXp("smithing", 15, false); }
                    string gbar = AHGrades.Gradable(bar) ? AHGrades.Id(bar, best) : bar;
                    bag.Add(gbar); GradeFx(p, gbar, best);
                    if (p.Perk("smith", 30) && Random.value < 0.25f) bag.Add(gbar);
                    ui.Toast("You smelt " + AHItems.Get(gbar).name.ToLowerInvariant() + ".");
                    g.quests.Event("craft", "furnace", 1, g);
                    return SmeltRecipe(p) != null;
                }
            case "light":
                {
                    string log = bag.Count("logs") > 0 ? "logs" : "pine_logs";
                    if (!bag.Take(log)) return false;
                    LightFire(s.pos);
                    p.GainXp("woodcutting", 10, false);
                    ui.Toast("You light a campfire. Cook on it while it burns.");
                    return false;
                }
            default:
                {
                    if (rec == null || !CanMake(p, rec)) return false;
                    bool frees = false; foreach (var m in rec.mats) if (bag.Count(m.Key) == m.Value) frees = true;
                    if (!frees && bag.UsedSlots >= bag.SlotsMax) { ui.Toast("Your bag is full."); return false; }
                    int gsum = 0, units = 0, gbest = 0;
                    foreach (var m in rec.mats) { int a, b; AHGrades.TakeAll(bag, m.Key, m.Value, out a, out b); if (AHGrades.Gradable(m.Key)) { gsum += a; units += m.Value; gbest = Mathf.Max(gbest, b); } }
                    CraftOut(p, rec, s.kind, AHGrades.MwBoost(gsum, units, gbest));
                    return false;
                }
        }
    }

    // web: a campfire of your own lasts 90 s
    static void LightFire(Vector3 at)
    {
        var go = new GameObject("Your campfire");
        go.transform.position = at;
        AHModel.SetShadows(AHStations.Campfire(go.transform, 0.85f, Random.Range(1, 9999)));
        Spots.Add(new AHSpot { kind = "fire", type = "fire", name = "Your campfire", pos = at, r = 0.5f, reach = 68f * AHDB.S, temp = true, tempGo = go, until = Time.time + 90f });
    }

    // web mwChance / craftOut
    static float MwChance(AHPlayer p, AHRecipe r, string kind)
    {
        var it = AHItems.Get(r.outId); bool wep = it.slot == "weapon";
        string ty = AHJson.S(AHJson.O(AHDB.Rules, "ARMOR_OF"), it.style ?? "", "leather");
        if (kind == "anvil" && p.Perk("smith", 10)) { float c = 0.06f + p.MasteryLv("smith") * 0.004f + (p.Perk("smith", 40) ? 0.12f : 0f); if ((wep && p.Spec("smith", "weapon")) || (!wep && p.Spec("smith", "armor"))) c *= 2f; return c; }
        if (kind == "jewel" && p.Perk("jeweler", 10)) { float c = 0.06f + p.MasteryLv("jeweler") * 0.004f + (p.Perk("jeweler", 40) ? 0.12f : 0f); if ((it.slot == "ring" && p.Spec("jeweler", "rings")) || (it.slot == "amulet" && p.Spec("jeweler", "amulets"))) c *= 2f; return c; }
        if (kind == "loom" && p.Perk("tailor", 10)) { float c = 0.06f + p.MasteryLv("tailor") * 0.004f + (p.Perk("tailor", 30) ? 0.08f : 0f) + (p.Perk("tailor", 40) ? 0.12f : 0f); if ((ty == "leather" && p.Spec("tailor", "leather")) || (ty == "cloth" && p.Spec("tailor", "cloth"))) c *= 2f; return c; }
        return 0f;
    }

    // World of Warcraft colours for a recipe by your skill: orange (always teaches), yellow, green (half), grey (little)
    public static int Diff(AHPlayer p, AHRecipe r) { int d = p.Skill(r.skill) - r.lvl; return d < 0 ? -1 : d < 5 ? 0 : d < 10 ? 1 : d < 15 ? 2 : 3; }
    public static float DiffXp(int diff) { return diff <= 0 ? 1f : diff == 1 ? 0.85f : diff == 2 ? 0.5f : 0.25f; }

    // a gathering roll: the plain thing or one of its grades (better above the node's level, on a rich node)
    static string Graded(AHPlayer p, string baseId, string skill, int req, AHSpot s, out int grade)
    {
        grade = AHGrades.Gradable(baseId) ? AHGrades.Roll(p, p.Skill(skill), req, RichK(s)) : 0;
        return AHGrades.Id(baseId, grade);
    }
    public static void GradeFx(AHPlayer p, string id, int grade)
    {
        AHCollection.Add(g, p, id);
        if (grade <= 0) return;
        var d = AHItems.Get(id); if (d == null) return;
        if (g.ui != null) g.ui.Float(p.transform.position + Vector3.up * 2.9f, d.name + "!", AHItems.Quality(d));
        if (grade >= 2) AHSound.Play("loot");
        if (grade >= 2) AHSpark.Burst(p.transform.position + Vector3.up * 1.4f, AHItems.Quality(d), 14, 2f, 0.8f, 0.08f, 1.5f);
        AHAnnounce.Item(g, p, d, "gathered");
    }

    // a fish on the bank (from AHFishing). false: it could not be kept (bag full) and fishing stops
    public static bool GrantFish(AHPlayer p, AHSpot s, string fish, int fxp, int tier)
    {
        var bag = p.bag; var ui = g.ui;
        if (!bag.Add(fish)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); Cancel(); return false; }
        AHComp.Forage(g, fish); AHSound.Play("splash");
        p.GainXp("fishing", fxp, false); AHProfPets.OnGather(g, "fishing");
        g.Ripple(s.pos, 1.2f);
        g.quests.Event("gather", fish, 1, g);
        AHDerby.OnCatch(g, fish);   // weighed for the weekly derby
        AHCollection.Add(g, p, fish);
        var d = AHItems.Get(fish);
        if (d != null) ui.Float(p.transform.position + Vector3.up * 2.6f, d.name, tier > 0 ? AHItems.Quality(d) : new Color(0.62f, 0.89f, 1f));
        if (d != null) AHAnnounce.Rare(g, p, d.name, AHFish.TierName[Mathf.Clamp(tier, 0, 4)], "caught");
        float dbl = p.Perk("fisher", 30) ? 0.35f : p.Perk("fisher", 10) ? 0.2f : 0f;
        if (tier < 2 && dbl > 0f && Random.value < dbl) { bag.Add(fish); ui.Float(p.transform.position + Vector3.up * 3f, "Double catch!", new Color(0.62f, 0.89f, 1f)); }
        float tk = p.Spec("fisher", "treasure") ? 3f : 1f;
        if (p.Perk("fisher", 20) && Random.value < 0.03f * tk && AHItems.Get("mystery_sack") != null && bag.Add("mystery_sack")) ui.Banner("Something heavy…", "You reeled in a mystery sack");
        return true;
    }

    static void CraftOut(AHPlayer p, AHRecipe r, string kind, float mwBoost = 0f)
    {
        var bag = p.bag; var ui = g.ui;
        var it0 = AHItems.Get(r.outId);
        string outId = r.outId; int n = r.n; bool mw = false; int mq = 0;
        if (it0.meal) outId = AHMeal.Out(p, r, out mq);
        else if (it0.slot != null && AHItems.Get(r.outId + "_mw") != null && Random.value < MwChance(p, r, kind) * AHArtisan.MwBonus(p, kind) + mwBoost) { outId = r.outId + "_mw"; mw = true; }
        float dbl = 0f;
        if (kind == "oven") dbl = AHMeal.Double(p, r);
        if (kind == "brew" && p.profs.Contains("alchemist")) dbl = p.Perk("alchemist", 30) ? 0.4f : p.Perk("alchemist", 5) ? 0.2f : 0f;
        dbl += AHArtisan.DoubleBonus(p, kind);
        bool twice = dbl > 0f && Random.value < dbl; if (twice) n *= 2;
        if (!bag.Add(outId, n)) { craftLeft = 0; foreach (var m in r.mats) bag.Add(m.Key, m.Value); ui.Toast(bag.lastWarn ?? "Your bag is full."); return; }
        if (((kind == "anvil" && p.Perk("smith", 5)) || (kind == "loom" && p.Perk("tailor", 5)) || (kind == "jewel" && p.Perk("jeweler", 5))) && Random.value < 0.15f)
        {
            var k = r.mats[Random.Range(0, r.mats.Count)].Key; bag.Add(k, 1);
            ui.Toast("Careful work: you saved 1 " + AHItems.Get(k).name.ToLowerInvariant() + ".");
        }
        p.GainXp(r.skill, Mathf.Max(1, Mathf.RoundToInt(r.xp * DiffXp(Diff(p, r)))), false);
        AHArtisan.OnCraft(p, outId, n);
        AHTwoPart.OnMade(g, outId);
        g.quests.Event("craft", kind, 1, g);
        var o = AHItems.Get(outId);
        if (o.slot != null) ui.Toast("You made " + o.name + ". Open the bag to wear it.", 3f);
        else ui.Toast("You made " + (n > 1 ? n + "× " : "") + o.name + "." + (twice ? " Double batch!" : ""), 3f);
        if (mw) ui.Banner(o.name, "Masterwork! +30% stats");
        if (mq > 0) ui.Banner(o.name, mq == 2 ? "Masterwork meal!" : "Fine meal");
    }
}
