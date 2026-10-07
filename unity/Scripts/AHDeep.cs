// Ashen Hollow: the Ashen Deep. A dungeon that is never the same twice: five floors of rooms joined by halls, laid out
// fresh for every run. Clear a floor (every foe on it) and the stairs open to the next. Rooms hold packs of foes with a
// Deep-touched elite, fire vents that flare in turn, or a treasure chest. The fifth floor is the boss's hall, and the boss
// changes every Monday. Its first defeat each week gives the Nightglass costume (once), a rare recipe scroll and
// enhancement stones. Foes grow with your level and with each floor. Your fastest full clear is kept as a record.
// Open from the MENU (The Ashen Deep) from level 5. Die and you wake at the floor's start; the run goes on.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHDeep
{
    public const string Area = "delve";
    public const int Floors = 5, MinLevel = 5;
    const string RunKey = "ah_deep_run", BestKey = "ah_deep_best", ClearsKey = "ah_deep_clears", WeekKey = "ah_deep_week", FromKey = "ah_deep_from", ChestKey = "ah_deep_chest";

    // ---- the run: "seed|floor|start ticks" ----
    public static bool InRun { get { return AHPrefs.GetString(RunKey, "") != ""; } }
    public static int Seed { get { return Part(0); } }
    public static int Floor { get { return Mathf.Max(1, Part(1)); } }
    static int Part(int i) { var s = AHPrefs.GetString(RunKey, "").Split('|'); int v; return s.Length > i && int.TryParse(s[i], out v) ? v : 0; }
    static long StartTicks { get { var s = AHPrefs.GetString(RunKey, "").Split('|'); long v; return s.Length > 2 && long.TryParse(s[2], out v) ? v : DateTime.Now.Ticks; } }
    static void SetRun(int seed, int floor, long start) { AHPrefs.SetString(RunKey, seed + "|" + floor + "|" + start); AHPrefs.Save(); }
    public static float Elapsed { get { return (float)TimeSpan.FromTicks(DateTime.Now.Ticks - StartTicks).TotalSeconds; } }
    public static int Best { get { return AHPrefs.GetInt(BestKey, 0); } }
    public static int Clears { get { return AHPrefs.GetInt(ClearsKey, 0); } }
    public static string Clock(float s) { int t = Mathf.Max(0, Mathf.RoundToInt(s)); return (t / 60) + ":" + (t % 60).ToString("00"); }

    static string Week() { var d = DateTime.Now.Date; int back = ((int)d.DayOfWeek + 6) % 7; return d.AddDays(-back).ToString("yyyy-MM-dd"); }
    static int WeekNo() { var d = DateTime.Now.Date; int back = ((int)d.DayOfWeek + 6) % 7; return (int)(d.AddDays(-back) - new DateTime(2026, 1, 5)).TotalDays / 7; }
    public static bool BossLooted { get { return AHPrefs.GetString(WeekKey, "") == Week(); } }

    // ---- this week's master of the Deep ----
    public class BossDef { public string id, name, model, line; public Color glow; public float size = 1f, reach = 1.6f; }
    public static readonly BossDef[] Bosses =
    {
        new BossDef { id = "h_golem",  name = "The Ashen Colossus",       model = "Web/mGolem",      line = "Stone that remembers the fire", glow = new Color(1f, 0.45f, 0.2f), size = 1.7f, reach = 1.8f },
        new BossDef { id = "h_nyx",    name = "Umbravex, the Deep Wyrm",   model = "Web/mDragonHQ",   line = "Wings that swallow the light", glow = new Color(0.6f, 0.35f, 1f), size = 1.2f, reach = 2.4f },
        new BossDef { id = "fwarlord", name = "Morvane, the Bone King",    model = "Web/mSkeletonHQ", line = "He rules what the Deep has taken", glow = new Color(0.55f, 0.9f, 1f), size = 2.3f, reach = 1.4f },
        new BossDef { id = "m_hound",  name = "Gloomjaw, Hound of the Deep", model = "Web/mCerberus", line = "Three heads, one hunger", glow = new Color(1f, 0.3f, 0.3f), size = 2.6f, reach = 1.7f },
    };
    public static BossDef WeekBoss { get { return Bosses[((WeekNo() % Bosses.Length) + Bosses.Length) % Bosses.Length]; } }

    // the foes of the Deep: existing beasts (for their bodies), with stats made here
    static readonly string[][] Pool =
    {
        new[] { "fskel", "Web/mSkeletonHQ", "Hollow skeleton" }, new[] { "drowned", "Web/mSkeletonHQ", "Deep drowned" },
        new[] { "w_cultist", "Mobs/w_cultist", "Ash cultist" }, new[] { "f_cultist", "Mobs/f_cultist", "Cinder acolyte" },
        new[] { "riftling", "Mobs/riftling", "Riftling" }, new[] { "h_shade", "Web/mdlWolf", "Shade hound" },
        new[] { "golem", "Web/mGolem", "Deep golem" }, new[] { "t_scorp", "Mobs/t_scorp", "Ash scorpion" },
    };

    // ---- the layout of a floor (the same for the same run and floor) ----
    public class Room { public Rect r; public string role; public bool traps; public int cx, cz; public Vector2 C { get { return r.center; } } }
    public const float Cell = 36f, Edge = 10f; public const int Grid = 4;
    public static List<Room> Layout(int seed, int floor, out List<Rect> halls)
    {
        var rnd = new System.Random(seed * 31 + floor * 7919);
        int n = floor >= Floors ? 6 : Mathf.Min(5 + floor, 8);
        List<Vector2Int> path = null;
        for (int tries = 0; tries < 200 && path == null; tries++)
        {
            var p = new List<Vector2Int> { new Vector2Int(rnd.Next(Grid), rnd.Next(Grid)) };
            while (p.Count < n)
            {
                var last = p[p.Count - 1]; var opts = new List<Vector2Int>();
                foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var q = last + d; if (q.x < 0 || q.y < 0 || q.x >= Grid || q.y >= Grid || p.Contains(q)) continue; opts.Add(q);
                }
                if (opts.Count == 0) break;
                p.Add(opts[rnd.Next(opts.Count)]);
            }
            if (p.Count == n) path = p;
        }
        var rooms = new List<Room>();
        for (int i = 0; i < path.Count; i++)
        {
            bool boss = floor >= Floors && i == path.Count - 1;
            float w = boss ? 30f : 16f + rnd.Next(8), h = boss ? 30f : 16f + rnd.Next(8);
            Vector2 c = new Vector2(Edge + path[i].x * Cell + Cell / 2f, Edge + path[i].y * Cell + Cell / 2f);
            rooms.Add(new Room { r = new Rect(c.x - w / 2f, c.y - h / 2f, w, h), role = "fight", cx = path[i].x, cz = path[i].y });
        }
        rooms[0].role = "start"; rooms[rooms.Count - 1].role = floor >= Floors ? "boss" : "stairs";
        rooms[1 + rnd.Next(rooms.Count - 2)].role = "treasure";
        foreach (var r in rooms) if (r.role == "fight" && floor >= 2 && rnd.NextDouble() < 0.4) r.traps = true;
        halls = new List<Rect>();
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2 a = rooms[i - 1].C, b = rooms[i].C; const float hw = 2.6f;
            if (Mathf.Abs(a.y - b.y) < 0.1f) halls.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x), a.y - hw, Mathf.Max(a.x, b.x), a.y + hw));
            else halls.Add(Rect.MinMaxRect(a.x - hw, Mathf.Min(a.y, b.y), a.x + hw, Mathf.Max(a.y, b.y)));
        }
        return rooms;
    }
    public static Vector2 StartOf(int seed, int floor) { List<Rect> h; return Layout(seed, floor, out h)[0].C; }

    // ---- going in, down and out ----
    public static void Start(AHGame g)
    {
        var p = g.player; if (p == null) return;
        if (p.level < MinLevel) { g.ui.Toast("The Ashen Deep opens to heroes of level " + MinLevel + " and up.", 3f); return; }
        if (AHGame.AreaId == Area) { g.ui.Toast("You are already in the Deep.", 2f); return; }
        Vector2 w = g.ToWeb(p.transform.position);
        AHPrefs.SetString(FromKey, AHGame.AreaId + "|" + w.x + "|" + w.y);
        int seed = UnityEngine.Random.Range(1, 999999);
        SetRun(seed, 1, DateTime.Now.Ticks);
        AHPrefs.SetString(ChestKey, "");
        var s = StartOf(seed, 1);
        g.Travel(Area, s.x, s.y, Mathf.PI / 2f);
    }

    public static void Down(AHGame g)
    {
        int f = Floor + 1; if (f > Floors) return;
        SetRun(Seed, f, StartTicks); AHPrefs.SetString(ChestKey, "");
        var s = StartOf(Seed, f);
        g.Travel(Area, s.x, s.y, Mathf.PI / 2f);
    }

    public static void Leave(AHGame g)
    {
        AHPrefs.DeleteKey(RunKey); AHPrefs.SetString(ChestKey, ""); AHPrefs.Save();
        var s = AHPrefs.GetString(FromKey, "").Split('|'); float x, z;
        if (s.Length == 3 && s[0] != Area && float.TryParse(s[1], out x) && float.TryParse(s[2], out z)) g.Travel(s[0], x, z, Mathf.PI / 2f);
        else g.Travel("meadow", g.data != null && AHGame.AreaId == "meadow" ? g.data.spawn.x : 0f, 0f, Mathf.PI / 2f);
    }

    public static bool ChestTaken(int room) { return ("," + AHPrefs.GetString(ChestKey, "") + ",").Contains("," + room + ","); }
    public static void TakeChest(int room) { AHPrefs.SetString(ChestKey, AHPrefs.GetString(ChestKey, "") + "," + room); }

    // ---- the foes ----
    static readonly HashSet<AHMob> foes = new HashSet<AHMob>();
    static AHMob boss;
    public static int Left { get { int n = 0; foreach (var m in foes) if (m != null && !m.dead) n++; return n; } }
    public static bool IsBoss(AHMob m) { return m != null && m == boss; }
    public static void Reset() { foes.Clear(); boss = null; brain = null; }
    public static void Forget(AHMob m) { foes.Remove(m); if (m == boss) boss = null; }

    public static AHMob Spawn(AHGame g, int pick, Vector3 at, bool elite, int floor)
    {
        var e = Pool[pick % Pool.Length]; int L = Mathf.Max(1, g.player.level);
        float k = 1f + 0.14f * (floor - 1);
        var t = new AHMobType
        {
            id = e[0], model = e[1], name = (elite ? "Deep-touched " : "") + e[2], lvl = L + (floor - 1) / 2,
            hp = Mathf.RoundToInt(9f * Mathf.Pow(L, 1.3f) * k * (elite ? 2.4f : 1f)),
            dmg = Mathf.RoundToInt((3f + 1.25f * L) * (1f + 0.08f * (floor - 1)) * (elite ? 1.3f : 1f)),
            xp = Mathf.RoundToInt(L * 14 * k * (elite ? 3f : 1f)), gold = L * (elite ? 8 : 3),
            speed = 125f * AHDB.S, radius = 15f * AHDB.S, aggro = 280f * AHDB.S, atkCd = 1.5f, respawn = 1e9f, noSkin = true, elite = elite,
        };
        var d = AHJson.O(AHDB.Mobs, e[0]);
        if (d != null) { t.radius = (float)AHJson.N(d, "r", 15) * AHDB.S; t.speed = Mathf.Min(150f, (float)AHJson.N(d, "speed", 125)) * AHDB.S; }
        var m = AHMob.Create(g, t, at); g.mobs.Add(m); foes.Add(m);
        return m;
    }

    public static AHMob SpawnBoss(AHGame g, Vector3 at)
    {
        var b = WeekBoss; int L = Mathf.Max(1, g.player.level);
        var d = AHJson.O(AHDB.Mobs, b.id);
        var t = new AHMobType
        {
            id = b.id, model = b.model, name = b.name, lvl = L + 3,
            hp = Mathf.RoundToInt(9f * Mathf.Pow(L, 1.3f) * 1.56f * 26f), dmg = Mathf.RoundToInt((3f + 1.25f * L) * 1.32f * 1.6f),
            xp = L * 320, gold = L * 60, speed = 64f * AHDB.S, radius = (float)AHJson.N(d, "r", 40) * AHDB.S, aggro = 520f * AHDB.S,
            atkCd = 2f, respawn = 1e9f, noSkin = true, elite = true,
        };
        t.radius = Mathf.Max(t.radius, b.reach);
        boss = AHMob.Create(g, t, at); g.mobs.Add(boss); foes.Add(boss);
        // the master of the Deep stands far bigger than its kin
        boss.transform.localScale *= b.size;
        return boss;
    }

    // the boss's fight: fire under you every 6 s, a ring around itself every 14 s, two waves of the Deep's foes
    class Brain { public bool engaged, add1, add2; public float aT = 4f, bT = 10f; }
    static Brain brain;
    public static bool Think(AHGame g, AHMob m, float dt)
    {
        if (brain == null) brain = new Brain();
        var b = brain; float f = m.hp / m.type.hp, hit = Mathf.Round(m.type.dmg * 1.4f);
        if (!b.engaged) { b.engaged = true; g.ui.Banner(m.type.name, WeekBoss.line); }
        if (!b.add1 && f < 0.6f) { b.add1 = true; Adds(g, m, 2); }
        if (!b.add2 && f < 0.3f) { b.add2 = true; Adds(g, m, 3); }
        b.aT -= dt; b.bT -= dt;
        if (b.aT <= 0f) { b.aT = f < 0.3f ? 4.5f : 6f; AHDungeon.Telegraph(g.player.transform.position, 2.4f, 1.4f, hit, m); return false; }
        if (b.bT <= 0f)
        {
            b.bT = 14f; AHDungeon.Telegraph(m.transform.position, m.type.radius + 6f, 1.9f, Mathf.Round(hit * 1.3f), m);
            m.Windup(1.9f); g.ui.Toast(m.type.name + " gathers its strength! Get away!", 2.5f);
            return true;
        }
        return false;
    }
    static void Adds(AHGame g, AHMob m, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float a = UnityEngine.Random.value * 6.28f;
            Vector3 at = g.Resolve(m.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (m.type.radius + 4f), 0.6f);
            var x = Spawn(g, UnityEngine.Random.Range(0, Pool.Length), at, false, Floor); x.add = true; x.Engage();
            AHFx.Pop(at + Vector3.up * 0.6f, 1.6f, WeekBoss.glow);
        }
        g.ui.Toast("The Deep sends more of its own!", 2.5f);
    }

    public static void OnMobDown(AHGame g, AHMob m)
    {
        if (AHGame.AreaId != Area || !foes.Contains(m)) return;
        var p = g.player;
        if (m == boss) { BossDown(g, m); return; }
        int left = Left;
        if (left == 0)
        {
            int L = Mathf.Max(1, p.level);
            p.AddMoney((long)(L * 25 * Floor) * AHDB.CU, m.transform.position);
            g.ui.Banner("Floor " + Floor + " cleared", Floor < Floors ? "The stairs down are open · " + Clock(Elapsed) : "Only the master of the Deep remains");
            AHSound.Play("level");
            if (AHDeepView.I != null) AHDeepView.I.Opened();
            AHStory.Event(g, "deepfloor");
        }
        else if (left <= 3) g.ui.Toast(left + (left == 1 ? " foe is" : " foes are") + " left on this floor.", 2f);
    }

    static void BossDown(AHGame g, AHMob m)
    {
        var p = g.player; int L = Mathf.Max(1, p.level); bool first = !BossLooted;
        float time = Elapsed; int best = Best; bool record = best == 0 || time < best;
        AHPrefs.SetInt(ClearsKey, Clears + 1);
        if (record) AHPrefs.SetInt(BestKey, Mathf.RoundToInt(time));
        foreach (var q in new List<AHMob>(g.mobs)) if (q != null && q.add && !q.dead) q.Hurt(999999, null, true);
        for (int k = 0; k < 4; k++) AHFx.Ring(m.transform.position, 1f, 3f + k, WeekBoss.glow, 1.2f + k * 0.3f);
        g.ui.Banner(m.type.name + " falls!", "The Deep is cleared in " + Clock(time) + (record ? " · a new record!" : " · your best is " + Clock(best)));
        if (first)
        {
            AHPrefs.SetString(WeekKey, Week());
            if (!p.prog.costumes.Contains("night")) { p.prog.costumes.Add("night"); g.ui.Banner("Nightglass", "A costume only the Deep gives · wear it in the Wardrobe"); }
            AHRareRecipes.Give(g, p, m.type.name);
            p.bag.Add("enh_stone", 3); if (UnityEngine.Random.value < 0.3f) p.bag.Add("lucky_charm");
            p.AddMoney((long)(L * 300) * AHDB.CU, m.transform.position);
        }
        else p.AddMoney((long)(L * 80) * AHDB.CU, m.transform.position);
        AHSound.Play("level");
        if (AHDeepView.I != null) AHDeepView.I.Opened();
        AHStory.Event(g, "deepfloor"); AHStory.Event(g, "deepboss");
        g.SaveProgress();
    }

    // a treasure chest: money, and sometimes a stone or a recipe
    public static void OpenChest(AHGame g, Vector3 at)
    {
        var p = g.player; int L = Mathf.Max(1, p.level);
        p.AddMoney((long)(L * 40 * Floor) * AHDB.CU, at);
        string got = "";
        if (UnityEngine.Random.value < 0.35f) { p.bag.Add("enh_stone"); got = " and an enhancement stone"; }
        if (UnityEngine.Random.value < 0.12f) { AHRareRecipes.Give(g, p, "a chest in the Deep"); }
        g.ui.Toast("The chest holds coin" + got + ".", 3f);
        AHSound.Play("coin");
        g.SaveProgress();
    }
}

// one floor of the Deep, built in code: stone floors and walls, torches, props, foes, traps, chests and the stairs
public class AHDeepView : MonoBehaviour
{
    public static AHDeepView I;
    AHGame g; List<AHDeep.Room> rooms; List<Rect> halls; int floor;
    readonly List<Transform> vents = new List<Transform>(); float ventT; int ventI;
    GameObject stairsGlow; bool open, wasDead;
    // a preview (the test camera): built far from the hero, nothing to walk on, no foes awake
    bool preview; int pSeed, pFloor; public readonly List<GameObject> Extra = new List<GameObject>();
    public static AHDeepView Preview(AHGame g, int seed, int floor)
    {
        var v = new GameObject("Deep preview").AddComponent<AHDeepView>(); v.g = g; v.preview = true; v.pSeed = seed; v.pFloor = floor;
        v.Build(); return v;
    }
    void Block(Vector3 at, float r) { if (!preview) g.AddBlocker(at, r); }
    void Spot(AHSpot s) { if (!preview) AHGather.Spots.Add(s); }
    GameObject Gate(Vector3 at, Color c)
    {
        if (!preview) return AHDungeon.Portal(g, at, 0f, c);
        // the preview marks a portal with a glowing ring (a real portal would block the ground where the hero is)
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(go.GetComponent<Collider>()); go.transform.SetParent(transform, false);
        go.transform.position = at + Vector3.up * 0.05f; go.transform.localScale = new Vector3(2.4f, 0.05f, 2.4f); go.GetComponent<Renderer>().sharedMaterial = Mat(c, 0.2f, true);
        return go;
    }
    void Foe(AHMob m) { if (!preview || m == null) return; m.enabled = false; g.mobs.Remove(m); AHDeep.Forget(m); Extra.Add(m.gameObject); }
    static Material stone, wall, trim, ember;

    public static void Setup(AHGame g)
    {
        AHDeep.Reset();
        if (AHGame.AreaId == "meadow") { Entrance(g); return; }
        if (AHGame.AreaId != AHDeep.Area) return;
        if (!AHDeep.InRun) { g.StartCoroutine(Bounce(g)); return; }   // a run that already ended: back up top
        I = new GameObject("Ashen Deep").AddComponent<AHDeepView>(); I.g = g; I.Build();
    }
    // the way in: a violet portal at the edge of Hollow Meadow's village
    static void Entrance(AHGame g)
    {
        if (g.data == null || g.data.spawn == null) return;
        Vector3 at = g.Resolve(g.W(g.data.spawn.x - 11f, g.data.spawn.z - 9f), 1.4f);
        AHDungeon.Portal(g, at, 0f, new Color(0.6f, 0.4f, 1f));
        AHGather.Spots.Add(new AHSpot { kind = "use", type = "deep_in", name = "Enter the Ashen Deep", pos = at, r = 0.5f, reach = 3f, use = () =>
        {
            if (g.player.level < AHDeep.MinLevel) { g.ui.Toast("The Ashen Deep opens to heroes of level " + AHDeep.MinLevel + " and up.", 3f); return; }
            AHDeep.Start(g);
        } });
    }
    static System.Collections.IEnumerator Bounce(AHGame g) { yield return new WaitForSeconds(0.5f); AHDeep.Leave(g); }

    Vector3 W(Vector2 v) { return g.W(v.x, v.y); }
    Rect U(Rect r) { Vector3 a = g.W(r.xMin, r.yMin), b = g.W(r.xMax, r.yMax); return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z)); }

    static Material Mat(Color c, float smooth = 0.08f, bool glow = false)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth);
        if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2.2f); }
        return m;
    }

    void Build()
    {
        floor = preview ? pFloor : AHDeep.Floor; int seed = preview ? pSeed : AHDeep.Seed; rooms = AHDeep.Layout(seed, floor, out halls);
        if (stone == null) { stone = Mat(new Color(0.2f, 0.18f, 0.17f)); wall = Mat(new Color(0.27f, 0.24f, 0.22f)); trim = Mat(new Color(0.14f, 0.12f, 0.11f)); ember = Mat(new Color(1f, 0.42f, 0.12f), 0.2f, true); }
        if (!preview)
        {
            g.SetDark(new Color(0.06f, 0.045f, 0.05f));
            // where you may walk: the rooms and the halls between them
            foreach (var r in rooms) g.AddFloor(U(r.r));
            foreach (var h in halls) g.AddFloor(U(h));
            // you wake at this floor's start if you fall
            if (g.data != null && g.data.spawn != null) { g.data.spawn.x = rooms[0].C.x; g.data.spawn.z = rooms[0].C.y; }
        }
        BuildStone();
        var rnd = new System.Random(seed * 13 + floor);
        Color flame = AHDeep.WeekBoss.glow;
        for (int i = 0; i < rooms.Count; i++) Dress(i, rooms[i], rnd, flame);
        if (!preview) StartCoroutine(Hello());
    }

    System.Collections.IEnumerator Hello()
    {
        yield return new WaitForSeconds(1.2f);
        if (floor < AHDeep.Floors) g.ui.Banner("The Ashen Deep · floor " + floor + " of " + AHDeep.Floors, "Clear every foe to open the stairs · " + AHDeep.Clock(AHDeep.Elapsed));
        else g.ui.Banner("The Ashen Deep · the last floor", AHDeep.WeekBoss.name + " waits below · " + AHDeep.Clock(AHDeep.Elapsed));
    }

    // the floors and walls as two meshes; a wall runs along every edge of a room or hall unless open floor lies beyond
    void BuildStone()
    {
        var fl = new List<CombineInstance>(); var wl = new List<CombineInstance>();
        var quad = Prim(PrimitiveType.Cube);
        foreach (var r in rooms) fl.Add(Slab(U(r.r), 0.02f, quad));
        foreach (var h in halls) fl.Add(Slab(U(h), 0f, quad));
        var all = new List<Rect>(); foreach (var r in rooms) all.Add(U(r.r)); foreach (var h in halls) all.Add(U(h));
        const float step = 2f, hgt = 3.4f, th = 0.7f;
        foreach (var R in all)
        {
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side < 2; float len = alongX ? R.width : R.height; int n = Mathf.Max(1, Mathf.CeilToInt(len / step)); float seg = len / n;
                for (int i = 0; i < n; i++)
                {
                    float t = (i + 0.5f) * seg; Vector3 p, outN;
                    if (side == 0) { p = new Vector3(R.xMin + t, 0, R.yMin); outN = Vector3.back; }
                    else if (side == 1) { p = new Vector3(R.xMin + t, 0, R.yMax); outN = Vector3.forward; }
                    else if (side == 2) { p = new Vector3(R.xMin, 0, R.yMin + t); outN = Vector3.left; }
                    else { p = new Vector3(R.xMax, 0, R.yMin + t); outN = Vector3.right; }
                    Vector3 beyond = p + outN * 0.6f; bool open = false;
                    foreach (var o in all) if (o.Contains(new Vector2(beyond.x, beyond.z))) { open = true; break; }
                    if (open) continue;
                    var c = p + outN * th / 2f + Vector3.up * hgt / 2f;
                    var size = alongX ? new Vector3(seg + th, hgt, th) : new Vector3(th, hgt, seg + th);
                    wl.Add(new CombineInstance { mesh = quad, transform = Matrix4x4.TRS(c, Quaternion.identity, size) });
                }
            }
        }
        Mesh(fl, stone, "Deep floor"); Mesh(wl, wall, "Deep walls");
        // KayKit's rocky floor tiles over each room
        var tile = AHDungeonDress.P("floor_tile_large_rocks");
        if (tile != null)
        {
            var probe = Instantiate(tile); float tw = 4f; var rs = probe.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0) { Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); tw = Mathf.Max(0.5f, bb.size.x); }
            Destroy(probe);
            foreach (var room in rooms)
            {
                var R = U(room.r); int nx = Mathf.Max(1, Mathf.FloorToInt(R.width / 4f)), nz = Mathf.Max(1, Mathf.FloorToInt(R.height / 4f));
                float sx = R.width / nx, sz = R.height / nz;
                for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++)
                {
                    var t = Instantiate(tile, transform, false); foreach (var c in t.GetComponentsInChildren<Collider>(true)) Destroy(c);
                    t.transform.position = new Vector3(R.xMin + (i + 0.5f) * sx, 0.03f, R.yMin + (j + 0.5f) * sz);
                    t.transform.localScale = new Vector3(sx / tw, 1f, sz / tw); t.transform.rotation = Quaternion.Euler(0, ((i + j) % 4) * 90f, 0);
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.sharedMaterials = Darken(r.sharedMaterials); }
                }
            }
        }
    }
    // the tiles' pale stone, darkened to the Deep's ash-grey (one copy of each material)
    static readonly Dictionary<Material, Material> dark = new Dictionary<Material, Material>();
    static Material[] Darken(Material[] ms)
    {
        var o = new Material[ms.Length];
        for (int i = 0; i < ms.Length; i++)
        {
            var m = ms[i]; if (m == null) continue; Material d;
            if (!dark.TryGetValue(m, out d) || d == null)
            {
                d = new Material(m); var k = new Color(0.36f, 0.32f, 0.31f);
                foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (d.HasProperty(pr)) { var c = d.GetColor(pr); d.SetColor(pr, new Color(c.r * k.r, c.g * k.g, c.b * k.b, c.a)); }
                dark[m] = d;
            }
            o[i] = d;
        }
        return o;
    }
    static Mesh cubeMesh;
    static Mesh Prim(PrimitiveType t) { if (cubeMesh == null) { var go = GameObject.CreatePrimitive(t); cubeMesh = go.GetComponent<MeshFilter>().sharedMesh; Destroy(go); } return cubeMesh; }
    CombineInstance Slab(Rect r, float y, Mesh m) { return new CombineInstance { mesh = m, transform = Matrix4x4.TRS(new Vector3(r.center.x, y - 0.05f, r.center.y), Quaternion.identity, new Vector3(r.width, 0.1f, r.height)) }; }
    void Mesh(List<CombineInstance> parts, Material mat, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(parts.ToArray(), true, true);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = true;
    }

    GameObject Kit(string n, Vector3 at, float yaw, float k)
    {
        var pf = AHDungeonDress.P(n); if (pf == null) return null;
        var go = Instantiate(pf, transform, false); foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
        go.transform.position = at; go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * k; AHModel.SetShadows(go);
        return go;
    }

    void Dress(int idx, AHDeep.Room room, System.Random rnd, Color flame)
    {
        Rect R = U(room.r); Vector3 c = new Vector3(R.center.x, 0, R.center.y);
        // pillars in the corners and two torches on opposite walls
        foreach (var cn in new[] { new Vector3(R.xMin + 1.4f, 0, R.yMin + 1.4f), new Vector3(R.xMax - 1.4f, 0, R.yMin + 1.4f), new Vector3(R.xMin + 1.4f, 0, R.yMax - 1.4f), new Vector3(R.xMax - 1.4f, 0, R.yMax - 1.4f) })
            if (Kit(room.role == "boss" ? "pillar_decorated" : "pillar", cn, rnd.Next(4) * 90f, 0.8f) != null) Block(cn, 0.7f);
        Vector3 tA = new Vector3(c.x, 0, R.yMax - 0.9f), tB = new Vector3(c.x, 0, R.yMin + 0.9f);
        if (!InHall(tA)) AHDungeonDress.Torch(transform, tA, Vector3.forward, flame);
        if (!InHall(tB)) AHDungeonDress.Torch(transform, tB, Vector3.back, flame);
        // rubble, barrels and crates by the walls
        string[] kit = { "barrel_small_stack", "crates_stacked", "rubble_half", "rubble_large", "box_stacked", "sword_shield_broken", "candle_triple" };
        float[] kk = { 0.55f, 0.5f, 0.4f, 0.4f, 0.55f, 0.7f, 0.8f };
        int props = 2 + rnd.Next(3);
        for (int i = 0; i < props; i++)
        {
            int k = rnd.Next(kit.Length); bool xs = rnd.Next(2) == 0;
            Vector3 at = xs ? new Vector3(rnd.Next(2) == 0 ? R.xMin + 1.2f : R.xMax - 1.2f, 0, Mathf.Lerp(R.yMin + 3f, R.yMax - 3f, (float)rnd.NextDouble()))
                            : new Vector3(Mathf.Lerp(R.xMin + 3f, R.xMax - 3f, (float)rnd.NextDouble()), 0, rnd.Next(2) == 0 ? R.yMin + 1.2f : R.yMax - 1.2f);
            if (InHall(at)) continue;
            if (Kit(kit[k], at, (float)rnd.NextDouble() * 360f, kk[k]) != null) Block(at, 0.6f);
        }
        switch (room.role)
        {
            case "start":
                {
                    Vector3 at = c + new Vector3(0, 0, R.height * 0.3f);
                    Gate(at, new Color(1f, 0.62f, 0.28f));
                    Spot(new AHSpot { kind = "use", type = "deep_out", name = "Leave the Deep", pos = at, r = 0.5f, reach = 3f, use = () => AHDeep.Leave(g) });
                    break;
                }
            case "stairs":
            case "boss":
                {
                    Vector3 at = c + new Vector3(0, 0, -R.height * 0.3f);
                    stairsGlow = Gate(at, room.role == "boss" ? new Color(1f, 0.75f, 0.35f) : new Color(0.6f, 0.4f, 1f));
                    if (stairsGlow != null && !preview) stairsGlow.SetActive(false);
                    Spot(new AHSpot { kind = "use", type = "deep_down", name = room.role == "boss" ? "Return to the surface" : "Stairs down", pos = at, r = 0.5f, reach = 3f, use = () => Stairs(room.role == "boss") });
                    if (room.role == "boss") Foe(AHDeep.SpawnBoss(g, c + new Vector3(0, 0, R.height * 0.15f)));
                    else Pack(room, rnd);
                    break;
                }
            case "treasure":
                {
                    Vector3 at = c; int i2 = idx;
                    if (!AHDeep.ChestTaken(i2))
                    {
                        var ch = Kit("chest", at, 180f, 0.9f);
                        AHSpot spot = null;
                        spot = new AHSpot { kind = "use", type = "deep_chest", name = "Open the chest", pos = at, r = 0.5f, reach = 3f };
                        spot.use = () => { if (AHDeep.ChestTaken(i2)) return; AHDeep.TakeChest(i2); AHDeep.OpenChest(g, at); AHGather.Spots.Remove(spot); if (ch != null) ch.transform.localScale *= 0.7f; };
                        Spot(spot); Block(at, 0.7f);
                        Kit("coin_stack_large", at + new Vector3(1.3f, 0, 0), 0f, 0.8f);
                    }
                    if (rnd.NextDouble() < 0.6) Pack(room, rnd, 2);
                    break;
                }
            default: Pack(room, rnd); break;
        }
        if (room.traps)
        {
            int n = 4 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                Vector3 at = new Vector3(Mathf.Lerp(R.xMin + 3f, R.xMax - 3f, (float)rnd.NextDouble()), 0, Mathf.Lerp(R.yMin + 3f, R.yMax - 3f, (float)rnd.NextDouble()));
                var v = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(v.GetComponent<Collider>());
                v.name = "Vent"; v.transform.SetParent(transform, false); v.transform.position = at + Vector3.up * 0.03f; v.transform.localScale = new Vector3(1.1f, 0.03f, 1.1f);
                v.GetComponent<Renderer>().sharedMaterial = ember; vents.Add(v.transform);
            }
        }
    }

    bool InHall(Vector3 p) { foreach (var h in halls) { var u = U(h); u.xMin -= 1f; u.xMax += 1f; u.yMin -= 1f; u.yMax += 1f; if (u.Contains(new Vector2(p.x, p.z))) return true; } return false; }

    void Pack(AHDeep.Room room, System.Random rnd, int max = 0)
    {
        Rect R = U(room.r); int n = max > 0 ? max : 2 + rnd.Next(2) + Mathf.Min(2, floor / 2);
        int kind = rnd.Next(64);
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * 6.28f + (float)rnd.NextDouble();
            Vector3 at = new Vector3(R.center.x + Mathf.Cos(a) * R.width * 0.22f, 0, R.center.y + Mathf.Sin(a) * R.height * 0.22f);
            Foe(AHDeep.Spawn(g, kind + (rnd.NextDouble() < 0.35 ? rnd.Next(8) : 0), preview ? at : g.Resolve(at, 0.6f), i == 0 && max == 0, floor));
        }
    }

    void Stairs(bool last)
    {
        if (!open) { int left = AHDeep.Left; g.ui.Toast(last ? "The master of the Deep still stands." : "The stairs are sealed. " + left + (left == 1 ? " foe remains" : " foes remain") + " on this floor.", 2.5f); return; }
        if (last) AHDeep.Leave(g); else AHDeep.Down(g);
    }

    public void Opened()
    {
        open = true; if (stairsGlow != null) stairsGlow.SetActive(true);
    }

    void Update()
    {
        var p = g.player; if (p == null || preview) return;
        if (!open && AHDeep.Left == 0 && Time.timeSinceLevelLoad > 3f) Opened();   // a floor cleared before (you came back to it)
        // fire vents flare one after another
        if (vents.Count > 0)
        {
            ventT -= Time.deltaTime;
            if (ventT <= 0f)
            {
                ventT = 0.9f; var v = vents[ventI++ % vents.Count];
                if ((v.position - p.transform.position).sqrMagnitude < 30f * 30f)
                {
                    float dmg = Mathf.Round((3f + 1.25f * Mathf.Max(1, p.level)) * 1.4f);
                    AHDungeon.Telegraph(v.position, 1.9f, 1.3f, dmg, null);
                }
            }
        }
        wasDead = p.dead;
    }
}
