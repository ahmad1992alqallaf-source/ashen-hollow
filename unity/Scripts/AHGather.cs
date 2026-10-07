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
        // city gates
        if (game.data.gates != null)
            foreach (var gt in game.data.gates)
                Spots.Add(new AHSpot { kind = "gate", type = gt.to, name = gt.label, pos = game.W(gt.x, gt.z), r = 0.5f, reach = gt.r, gate = gt });
    }

    static AHSpot Spot(string kind, string type, object o, float reachUnits)
    {
        float S = AHDB.S;
        Vector3 p = g.W((float)AHJson.N(o, "x") * S, (float)AHJson.N(o, "y") * S);
        if (!g.InArea(p)) return null;
        var s = new AHSpot { kind = kind, type = type, pos = p, r = (float)AHJson.N(o, "r", 16) * S, reach = reachUnits * S };
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
            case "mine": return "Mine " + s.name.Split(' ')[0].ToLowerInvariant();
            case "chop": return "Chop " + s.name;
            case "fish": return "Fish";
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
            case "fire": if (RawToCook(p) == null) warn = "Nothing to cook. Catch fish or hunt for raw meat."; break;
            case "furnace": if (SmeltRecipe(p) == null) warn = "Need 1 copper + 1 tin ore (or iron ore at Smithing 10)."; break;
        }
        if (warn != null && loud && g.ui != null) g.ui.Toast(warn);
        return warn == null;
    }

    static readonly string[] Meats = { "raw_beef", "raw_pork", "raw_mutton", "raw_chicken", "raw_duck" };
    public static string RawToCook(AHPlayer p)
    {
        var b = p.bag; int c = p.Skill("cooking");
        if (b.Count("raw_lavaeel") > 0 && c >= 18) return "raw_lavaeel";
        if (b.Count("raw_swordfish") > 0 && c >= 16) return "raw_swordfish";
        if (b.Count("raw_tuna") > 0 && c >= 12) return "raw_tuna";
        if (b.Count("raw_salmon") > 0 && c >= 10) return "raw_salmon";
        if (b.Count("raw_eel") > 0 && c >= 5) return "raw_eel";
        if (b.Count("raw_trout") > 0) return "raw_trout";
        if (b.Count("raw_meat") > 0) return "raw_meat";
        foreach (var k in Meats) if (b.Count(k) > 0) return k;
        return null;
    }

    public static string SmeltRecipe(AHPlayer p)
    {
        if (p.bag.Count("iron_ore") > 0 && p.Skill("smithing") >= 10) return "iron";
        if (p.bag.Count("copper_ore") > 0 && p.bag.Count("tin_ore") > 0) return "bronze";
        return null;
    }

    public static bool HasMats(AHPlayer p, AHRecipe r) { foreach (var m in r.mats) if (p.bag.Count(m.Key) < m.Value) return false; return true; }
    public static bool MasteryOk(AHPlayer p, AHRecipe r) { return r.masteryProf == null || (p.profs.Contains(r.masteryProf) && p.MasteryLv(r.masteryProf) >= r.masteryLv); }
    public static bool CanMake(AHPlayer p, AHRecipe r) { return (!r.rare || AHRareRecipes.Known(p, r.outId)) && p.Skill(r.skill) >= r.lvl && MasteryOk(p, r) && HasMats(p, r) && (!AHArtisan.Only(r.outId) || p.path == "artisan"); }

    // ---------- the action you are doing (web P.action) ----------
    public static AHSpot actSpot;
    public static AHRecipe actRecipe;
    static float actT, actDur;
    public static bool Busy { get { return actSpot != null; } }
    public static float Frac { get { return actSpot != null && actDur > 0f ? Mathf.Clamp01(actT / actDur) : 0f; } }
    public static void Cancel() { actSpot = null; actRecipe = null; }

    // web profSpeed
    static float Speed(AHPlayer p, string kind)
    {
        float m = kind == "chop" || kind == "mine" || kind == "fish" || kind == "herb" || kind == "skin" ? 1f + AHMeal.Fx(p, "gather") : 1f;
        m *= AHWeather.GatherK(kind);   // rain: the fish bite and the herbs open
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
        actDur = (r != null ? 2.2f : Dur(s.kind)) / Speed(p, s.kind);
        // the woodcutter and the miner swing their tools; the herbalist kneels to pick; the angler casts a line
        if (r != null) p.BeginWork(s.pos, s.kind == "anvil" || s.kind == "furnace" || s.kind == "jewel" ? "Work_Hammer|Interact" : "Interact");
        else p.BeginWork(s.pos, s.kind == "chop" ? "Work_Chop|Chop" : s.kind == "mine" ? "Work_Mine|Chop" : s.kind == "herb" || s.kind == "bog" || s.kind == "reef" ? "Work_Gather|Harvest" : s.kind == "fish" || s.kind == "lava" || s.kind == "sea" ? "Work_Fish|Fish" : "Interact",
            s.kind == "chop" ? "axe" : s.kind == "mine" ? "pick" : s.kind == "fish" || s.kind == "lava" || s.kind == "sea" ? "rod" : null);
    }

    // web: lighting your own campfire in the wild with logs
    public static bool CanLight(AHPlayer p) { return !p.dead && !AHInterior.Inside && !g.InTown(p.transform.position) && (p.bag.Count("logs") > 0 || p.bag.Count("pine_logs") > 0); }
    public static void StartLight(AHPlayer p)
    {
        if (g.InTown(p.transform.position)) { g.ui.Toast("Use the town campfire. You can light your own in the wild."); return; }
        if (p.bag.Count("logs") <= 0 && p.bag.Count("pine_logs") <= 0) { g.ui.Toast("You need logs. Chop a tree first."); return; }
        var spot = new AHSpot { kind = "light", pos = p.transform.position + p.transform.forward * 34f * AHDB.S, reach = 1f };
        actSpot = spot; actRecipe = null; actT = 0f; actDur = Dur("light");
    }

    // call every frame from the player; moving stops the work
    public static void Update(AHPlayer p, float dt, bool moving)
    {
        if (actSpot == null) return;
        if (moving || p.dead) { Cancel(); return; }
        Vector3 dd = actSpot.pos - p.transform.position; dd.y = 0;
        if (actSpot.kind != "light" && dd.magnitude > actSpot.reach + 1f) { Cancel(); return; }   // pushed or carried away
        actT += dt;
        if (actT < actDur) return;
        var s = actSpot; var r = actRecipe;
        actSpot = null; actRecipe = null;
        bool again = Finish(p, s, r);
        if (again && CanDo(p, s, false)) { actSpot = s; actRecipe = null; actT = 0f; actDur = Dur(s.kind) / Speed(p, s.kind); }
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
                    string log = AHJson.S(tr, "log", "logs");
                    if (!bag.Add(log)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); return false; } AHComp.Forage(g, log);
                    p.GainXp("woodcutting", (int)AHJson.N(tr, "xp", 25), false);
                    g.quests.Event("gather", log, 1, g);
                    if (Random.value < AHJson.N(tr, "fell", 0.35)) { s.until = now + 22f; SetFelled(s, true); ui.Toast("The " + s.name + " falls. It grows back soon."); return false; }
                    return true;
                }
            case "herb":
                {
                    var h = AHJson.O(Rule("HERB_T"), s.type);
                    if (!bag.Add(s.type)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); return false; } AHComp.Forage(g, s.type);
                    p.GainXp("herblore", (int)AHJson.N(h, "xp", 14), false);
                    if (p.Perk("alchemist", 20) && Random.value < 0.3f) bag.Add(s.type);
                    s.until = now + 45f;
                    g.quests.Event("gather", s.type, 1, g);
                    return false;
                }
            case "mine":
                {
                    var rk = AHJson.O(Rule("ROCKS"), s.type);
                    string ore = AHJson.S(rk, "ore", "copper_ore");
                    if (!bag.Add(ore)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); return false; } AHComp.Forage(g, ore);
                    p.GainXp("mining", (int)AHJson.N(rk, "xp", 18), false);
                    s.until = now + 7f * (p.Spec("miner", "delve") ? 0.5f : 1f);
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
                    return false;
                }
            case "fish":
                {
                    int fl = p.Skill("fishing") + (p.Spec("fisher", "angler") ? 4 : 0);
                    string kind = s.type, fish = "raw_trout"; int fxp = 20;
                    if (kind == "lava") { if (fl < 18) { ui.Toast("Lava eels need Fishing 18."); return false; } fish = "raw_lavaeel"; fxp = 120; }
                    else if (kind == "reef" && fl >= 16 && Random.value < 0.55f) { fish = "raw_swordfish"; fxp = 90; }
                    else if ((kind == "sea" || kind == "reef") && fl >= 12 && Random.value < 0.55f) { fish = "raw_tuna"; fxp = 60; }
                    else if (kind == "bog" && fl >= 5 && Random.value < 0.6f) { fish = "raw_eel"; fxp = 35; }
                    else if (fl >= 10 && Random.value < 0.5f) { fish = "raw_salmon"; fxp = 45; }
                    if (!bag.Add(fish)) { ui.Toast(bag.lastWarn ?? "Your bag is full."); return false; } AHComp.Forage(g, fish);
                    p.GainXp("fishing", fxp, false);
                    g.Ripple(s.pos, 1.2f);
                    g.quests.Event("gather", fish, 1, g);
                    AHDerby.OnCatch(g, fish);   // weighed for the weekly derby
                    // web fishBonus
                    float dbl = p.Perk("fisher", 30) ? 0.35f : p.Perk("fisher", 10) ? 0.2f : 0f;
                    if (dbl > 0f && Random.value < dbl) { bag.Add(fish); ui.Float(p.transform.position + Vector3.up * 2.6f, "Double catch!", new Color(0.62f, 0.89f, 1f)); }
                    float tk = p.Spec("fisher", "treasure") ? 3f : 1f;
                    if (p.Perk("fisher", 20) && Random.value < 0.03f * tk && AHItems.Get("mystery_sack") != null && bag.Add("mystery_sack")) ui.Banner("Something heavy…", "You reeled in a mystery sack");
                    return true;
                }
            case "fire":
                {
                    string raw = RawToCook(p); if (raw == null) return false;
                    int req = raw == "raw_salmon" ? 10 : raw == "raw_tuna" ? 12 : raw == "raw_eel" ? 5 : raw == "raw_swordfish" ? 16 : raw == "raw_lavaeel" ? 18 : 1;
                    float burn = Mathf.Max(0.03f, 0.34f - (p.Skill("cooking") - req) * 0.035f);
                    string cooked = raw == "raw_trout" ? "trout" : raw == "raw_salmon" ? "salmon" : raw == "raw_tuna" ? "tuna" : raw == "raw_eel" ? "eel" : raw == "raw_swordfish" ? "swordfish" : raw == "raw_lavaeel" ? "lavaeel" : "meat";
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
                    if (!bag.Fits(new[] { bar }) && (r == "iron" ? bag.Count("iron_ore") > 1 : bag.Count("copper_ore") > 1 && bag.Count("tin_ore") > 1)) { ui.Toast("Your bag is full. Make room to keep smelting."); return false; }
                    if (r == "iron") { bag.Take("iron_ore"); bag.Add("iron_bar"); p.GainXp("smithing", 30, false); }
                    else { bag.Take("copper_ore"); bag.Take("tin_ore"); bag.Add("bronze_bar"); p.GainXp("smithing", 15, false); }
                    if (p.Perk("smith", 30) && Random.value < 0.25f) bag.Add(bar);
                    ui.Toast("You smelt a " + AHItems.Get(bar).name.ToLowerInvariant() + ".");
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
                    foreach (var m in rec.mats) bag.Take(m.Key, m.Value);
                    CraftOut(p, rec, s.kind);
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

    static void CraftOut(AHPlayer p, AHRecipe r, string kind)
    {
        var bag = p.bag; var ui = g.ui;
        var it0 = AHItems.Get(r.outId);
        string outId = r.outId; int n = r.n; bool mw = false; int mq = 0;
        if (it0.meal) outId = AHMeal.Out(p, r, out mq);
        else if (it0.slot != null && AHItems.Get(r.outId + "_mw") != null && Random.value < MwChance(p, r, kind) * AHArtisan.MwBonus(p, kind)) { outId = r.outId + "_mw"; mw = true; }
        float dbl = 0f;
        if (kind == "oven") dbl = AHMeal.Double(p, r);
        if (kind == "brew" && p.profs.Contains("alchemist")) dbl = p.Perk("alchemist", 30) ? 0.4f : p.Perk("alchemist", 5) ? 0.2f : 0f;
        dbl += AHArtisan.DoubleBonus(p, kind);
        bool twice = dbl > 0f && Random.value < dbl; if (twice) n *= 2;
        if (!bag.Add(outId, n)) { foreach (var m in r.mats) bag.Add(m.Key, m.Value); ui.Toast(bag.lastWarn ?? "Your bag is full."); return; }
        if (((kind == "anvil" && p.Perk("smith", 5)) || (kind == "loom" && p.Perk("tailor", 5)) || (kind == "jewel" && p.Perk("jeweler", 5))) && Random.value < 0.15f)
        {
            var k = r.mats[Random.Range(0, r.mats.Count)].Key; bag.Add(k, 1);
            ui.Toast("Careful work: you saved 1 " + AHItems.Get(k).name.ToLowerInvariant() + ".");
        }
        p.GainXp(r.skill, r.xp, false);
        AHArtisan.OnCraft(p, outId, n);
        g.quests.Event("craft", kind, 1, g);
        var o = AHItems.Get(outId);
        if (o.slot != null) ui.Toast("You made " + o.name + ". Open the bag to wear it.", 3f);
        else ui.Toast("You made " + (n > 1 ? n + "× " : "") + o.name + "." + (twice ? " Double batch!" : ""), 3f);
        if (mw) ui.Banner(o.name, "Masterwork! +30% stats");
        if (mq > 0) ui.Banner(o.name, mq == 2 ? "Masterwork meal!" : "Fine meal");
    }
}
