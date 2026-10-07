// Ashen Hollow: dungeons, as in the web game (makeDungeon, dungeonTick, dbossThink, dMobDown, enterDungeon,
// lootChest) and the Sunken Forge (bossThink, spawnAdds).
//  - Doors into the dungeons stand in the regions (events.json DUNGEONS ent, PORTALS): a portal you walk up to.
//    The Hollow below the meadow well waits for its story quest; a cleared dungeon is sealed for 20 minutes.
//  - Inside: four rooms; each iron gate opens once the room behind it is cleared. A mini-boss guards room 2.
//  - The boss warns before it strikes: red circles on the ground fill up, then anything inside is hit.
//    Two circles under you every 5 s, a ring of eight around it every 9 s, a great slam below 65% health,
//    helpers at 60% and 30%, and faster attacks once enraged (below 30%).
//  - The boss leaves a chest: a piece of this dungeon's set for your class, and gems. The first clear pays a bonus.
// Beasts that slam (golems, mini-bosses) warn the same way wherever they are.
using System.Collections.Generic;
using UnityEngine;

public class AHDungeon : MonoBehaviour
{
    public static AHDungeon I;
    const float DLockMin = 20f;
    static readonly Color Red = new Color(1f, 0.16f, 0.1f);

    AHGame g;
    object def;          // events.json DUNGEONS entry (null in the Sunken Forge)
    string id;
    float tier = 1f, zx, zy, tick;
    readonly List<Rect> rooms = new List<Rect>();        // web units, from the zone's corner (DROOM)
    readonly List<Rect> doors = new List<Rect>();
    readonly List<GameObject> doorModels = new List<GameObject>();
    readonly List<bool> open = new List<bool>();
    readonly Dictionary<AHMob, Brain> brains = new Dictionary<AHMob, Brain>();

    class Brain { public bool engaged, add1, add2; public float aT, bT, cT, slamT = 2.5f; }

    // ---------- the tables ----------
    public static object Def(string id)
    {
        var l = AHDB.List("events", "DUNGEONS");
        if (l != null) foreach (var o in l) if (AHJson.S(o, "id") == id) return o;
        return null;
    }
    public static bool IsDungeon(string id) { return id == "forge" || Def(id) != null; }
    static string Col(object def, int i) { var a = AHJson.A(def, i == 0 ? "boss" : "mini"); return a != null && a.Count > 0 ? (string)a[0] : null; }
    static long NowMs { get { return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
    static string Key(string id) { return "ah_dung_" + id; }
    // saved per dungeon: "clears|cleared at (ms)"
    static void Rec(string id, out int clears, out long at)
    {
        clears = 0; at = 0;
        var s = AHPrefs.GetString(Key(id), "").Split('|');
        if (s.Length == 2) { int.TryParse(s[0], out clears); long.TryParse(s[1], out at); }
    }
    public static void ResetAll()
    {
        AHPrefs.DeleteKey(Key("forge"));
        var l = AHDB.List("events", "DUNGEONS");
        if (l != null) foreach (var o in l) AHPrefs.DeleteKey(Key(AHJson.S(o, "id")));
    }

    public static string BossOf(string id) { var d = Def(id); return d != null ? Col(d, 0) : id == "forge" ? "grull" : null; }
    // web dfCanQueue: the same checks, quietly
    public static bool Status(AHGame g, string id, out string why)
    {
        why = ""; var d = Def(id);
        if (d == null) return true;
        string gate = AHJson.S(d, "gate");
        if (!string.IsNullOrEmpty(gate))
        {
            var qs = AHDB.List("quests", "QUESTS"); int qi = -1;
            if (qs != null) for (int i = 0; i < qs.Count; i++) if (AHJson.S(qs[i], "name") == gate) { qi = i; break; }
            if (qi >= 0 && g.quests.i < qi) { why = "Locked by the story"; return false; }
        }
        int clears; long at; Rec(id, out clears, out at);
        double left = DLockMin * 60000.0 - (NowMs - at);
        if (at > 0 && left > 0) { why = "Sealed · " + Mathf.CeilToInt((float)(left / 60000.0)) + " min"; return false; }
        return true;
    }

    // web enterDungeon's checks: the story gate (the Hollow) and the seal after a clear
    public static bool CanEnter(AHGame g, string id)
    {
        var d = Def(id);
        if (d == null) return true;
        string gate = AHJson.S(d, "gate");
        if (!string.IsNullOrEmpty(gate))
        {
            var qs = AHDB.List("quests", "QUESTS"); int qi = -1;
            if (qs != null) for (int i = 0; i < qs.Count; i++) if (AHJson.S(qs[i], "name") == gate) { qi = i; break; }
            if (qi >= 0 && g.quests.i < qi) { g.ui.Toast(AHJson.S(d, "gateMsg", "The way is sealed."), 4f); return false; }
        }
        int clears; long at; Rec(id, out clears, out at);
        double left = DLockMin * 60000.0 - (NowMs - at);
        if (at > 0 && left > 0) { g.ui.Toast(AHJson.S(d, "name") + " is sealed for " + Mathf.CeilToInt((float)(left / 60000.0)) + " min.", 3f); return false; }
        string lv = AHJson.S(d, "lv", "1"); int lo; int.TryParse(lv.Split('–', '-')[0], out lo);
        if (g.player.level < lo) g.ui.Toast("You are level " + g.player.level + ". " + AHJson.S(d, "name") + " is for level " + lv + ". This will be very hard.", 4f);
        return true;
    }

    // ---------- set up: doors in the regions, and the dungeon itself ----------
    public static void Setup(AHGame g, Transform world)
    {
        I = null;
        float S = AHDB.S;
        // the dungeon doors that stand in this area
        var l = AHDB.List("events", "DUNGEONS");
        if (l != null)
            foreach (var d in l)
            {
                var ent = AHJson.O(d, "ent"); var z = AHJson.O(d, "z");
                if (ent == null || z == null) continue;
                Vector3 p = g.W((float)AHJson.N(ent, "x") * S, (float)AHJson.N(ent, "y") * S);
                if (!g.InArea(p)) continue;
                string id = AHJson.S(d, "id");
                var gt = new AHGate { x = (float)AHJson.N(ent, "x") * S, z = (float)AHJson.N(ent, "y") * S, r = 110 * S, to = id, tx = ((float)AHJson.N(z, "x") + 500) * S, tz = ((float)AHJson.N(z, "y") + 700) * S, f = 0f, label = "Enter " + AHJson.S(d, "name"), name = AHJson.S(d, "name"), dung = id };
                AHGather.Spots.Add(new AHSpot { kind = "gate", type = id, name = gt.label, pos = p, r = 0.5f, reach = gt.r, gate = gt });
                Portal(g, p, 0.4f, PortalColor(id));
            }
        // the burning portal to the Sunken Forge (web PORTALS[0])
        var ps = AHDB.List("events", "PORTALS");
        if (ps != null && ps.Count > 1)
        {
            var q = ps[0]; var to = AHJson.O(q, "to");
            Vector3 p = g.W((float)AHJson.N(q, "x") * S, (float)AHJson.N(q, "y") * S);
            if (g.InArea(p))
            {
                var gt = new AHGate { x = (float)AHJson.N(q, "x") * S, z = (float)AHJson.N(q, "y") * S, r = 80 * S, to = "forge", tx = (float)AHJson.N(to, "x") * S, tz = (float)AHJson.N(to, "y") * S, f = 0f, label = "Enter the Sunken Forge", name = "Sunken Forge", dung = "forge" };
                AHGather.Spots.Add(new AHSpot { kind = "gate", type = "forge", name = gt.label, pos = p, r = 0.5f, reach = gt.r, gate = gt });
                Portal(g, p, -0.6f, AHGame.Hex(0xff6a1c));
            }
        }
        if (!IsDungeon(AHGame.AreaId)) return;
        var dd = new GameObject("Dungeon").AddComponent<AHDungeon>();
        dd.Init(g, world);
    }

    static Color PortalColor(string id)
    {
        switch (id) { case "d_warrens": return AHGame.Hex(0x6aff6a); case "d_frost": return AHGame.Hex(0x9fe4ff); case "d_tomb": return AHGame.Hex(0xffd23a); case "d_molten": return AHGame.Hex(0xff5a1c); case "d_hollow": return AHGame.Hex(0xb08aff); }
        return AHGame.Hex(0x66b8ff);
    }

    // web buildPortal: two stone posts, a lintel and a glowing door between them (turned like the web's rotation.y)
    static Material stone, stoneDk;
    public static GameObject Portal(AHGame g, Vector3 at, float webYaw, Color c)
    {
        if (stone == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            stone = new Material(sh); stone.SetColor("_BaseColor", AHGame.Hex(0x4a423c).linear); stone.SetFloat("_Smoothness", 0.1f);
            stoneDk = new Material(sh); stoneDk.SetColor("_BaseColor", AHGame.Hex(0x2d2624).linear); stoneDk.SetFloat("_Smoothness", 0.1f);
        }
        var root = new GameObject("Portal");
        root.transform.position = at;
        // the web's local x axis, after its turn, in Unity
        Vector3 right = g.W(Mathf.Cos(webYaw), -Mathf.Sin(webYaw)) - g.W(0f, 0f); right.y = 0; right.Normalize();
        root.transform.rotation = Quaternion.LookRotation(new Vector3(-right.z, 0f, right.x), Vector3.up);
        System.Action<Vector3, Vector3, Material> box = (pos, size, m) =>
        {
            var b = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube);
            Object.Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(root.transform, false); b.transform.localPosition = pos; b.transform.localScale = size;
            b.GetComponent<Renderer>().sharedMaterial = m;
        };
        box(new Vector3(-1.3f, 1.6f, 0), new Vector3(0.5f, 3.2f, 0.5f), stone);
        box(new Vector3(1.3f, 1.6f, 0), new Vector3(0.5f, 3.2f, 0.5f), stone);
        box(new Vector3(0, 3.3f, 0), new Vector3(3.3f, 0.5f, 0.6f), stoneDk);
        foreach (float s in new[] { -1f, 1f })
        {
            var sp = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Capsule), PrimitiveType.Capsule); Object.Destroy(sp.GetComponent<Collider>());
            sp.transform.SetParent(root.transform, false); sp.transform.localPosition = new Vector3(s * 1.35f, 3.75f, 0); sp.transform.localScale = new Vector3(0.22f, 0.4f, 0.22f);
            sp.transform.localRotation = Quaternion.Euler(0, 0, -s * 17f); sp.GetComponent<Renderer>().sharedMaterial = stoneDk;
        }
        // the glowing door, both faces
        foreach (float yaw in new[] { 0f, 180f })
        {
            var disc = AHFx.Keep(PrimitiveType.Quad, new Color(c.r, c.g, c.b, 0.8f), 2f);
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = new Vector3(0, 1.55f, 0); disc.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            disc.transform.localScale = new Vector3(2.3f, 3.0f, 1f);
        }
        var lg = new GameObject("Glow"); lg.transform.SetParent(root.transform, false); lg.transform.localPosition = new Vector3(0, 1.6f, 0);
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = c; li.range = 7f; li.intensity = 2.2f; li.shadows = LightShadows.None;
        root.AddComponent<AHPortalSpin>();
        g.AddBlocker(root.transform.position + right * 1.3f, 0.35f);
        g.AddBlocker(root.transform.position - right * 1.3f, 0.35f);
        return root;
    }

    void Init(AHGame game, Transform world)
    {
        I = this; g = game; id = AHGame.AreaId; def = Def(id);
        float S = AHDB.S;
        if (def != null)
        {
            tier = (float)AHJson.N(def, "tier", 1);
            var z = AHJson.O(def, "z"); zx = (float)AHJson.N(z, "x"); zy = (float)AHJson.N(z, "y");
            var dr = AHDB.List("events", "DROOM");
            if (dr != null) foreach (var o in dr) { var r = o as List<object>; rooms.Add(new Rect((float)(double)r[0], (float)(double)r[1], (float)(double)r[2], (float)(double)r[3])); }
            // the iron gates: solid until their room is cleared
            if (g.data.dgates != null)
                for (int i = 0; i < g.data.dgates.Length; i++)
                {
                    var b = g.data.dgates[i];
                    var rc = g.MapRect(b.x, b.z, b.w, b.h); doors.Add(rc); g.doors.Add(rc); open.Add(false);
                    Transform t = world != null ? FindDeep(world, "AH_DGATE_" + i) : null;
                    doorModels.Add(t != null ? t.gameObject : null);
                }
            // the way out (web D.exit portal)
            var ex = AHJson.O(def, "exit");
            if (ex != null) Portal(g, g.W((float)AHJson.N(ex, "x") * S, (float)AHJson.N(ex, "y") * S), Mathf.PI / 2f, PortalColor(id));
            Color around = AHDB.Col(((Dictionary<string, object>)def)["around"], AHGame.Hex(0x141a10));
            g.SetDark(around);
            // torches in each room's corners
            foreach (var r in rooms)
                foreach (var c in new[] { new Vector2(r.xMin + 40, r.yMin + 40), new Vector2(r.xMax - 40, r.yMin + 40), new Vector2(r.xMin + 40, r.yMax - 40), new Vector2(r.xMax - 40, r.yMax - 40) })
                    Torch(g.W((zx + c.x) * S, (zy + c.y) * S), 0.6f);
        }
        else
        {
            // the Sunken Forge: torches on its pillars (web DPILLARS)
            g.SetDark(AHGame.Hex(0x0e0b0a));
            var ps = AHDB.List("events", "PORTALS");
            if (ps != null && ps.Count > 1) Portal(g, g.W((float)AHJson.N(ps[1], "x") * S, (float)AHJson.N(ps[1], "y") * S), Mathf.PI / 2f, AHGame.Hex(0x66b8ff));
            var pil = new List<Vector2> { new Vector2(12150, 490), new Vector2(12150, 730), new Vector2(13150, 490), new Vector2(13150, 730) };
            for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 2f + 0.5f; pil.Add(new Vector2(13830 + Mathf.Cos(a) * 250, 610 + Mathf.Sin(a) * 230)); }
            foreach (var p in pil) Torch(g.W(p.x * S, p.y * S) + Vector3.up * 3.5f, 0.8f);
        }
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), name); if (r != null) return r; }
        return null;
    }

    // a flame with a flickering light (web buildFire)
    static void Torch(Vector3 at, float size)
    {
        var f = AHFx.Keep(PrimitiveType.Sphere, new Color(1f, 0.55f, 0.15f, 0.9f), 0f);
        f.transform.position = at + Vector3.up * 0.35f * size; f.transform.localScale = new Vector3(0.35f, 0.55f, 0.35f) * size;
        var li = f.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.62f, 0.3f); li.range = 9f * size + 3f; li.intensity = 2.4f; li.shadows = LightShadows.None;
        f.AddComponent<AHFlicker>();
    }

    // ---------- rooms and gates (web dungeonTick) ----------
    int RoomOf(Vector3 p)
    {
        Vector2 w = g.ToWeb(p) / AHDB.S;
        float u = w.x - zx, v = w.y - zy;
        for (int i = 0; i < rooms.Count; i++) if (rooms[i].Contains(new Vector2(u, v))) return i;
        return -1;
    }

    void Update()
    {
        if (g == null || g.player == null) return;
        tick += Time.deltaTime; if (tick < 0.4f) return; tick = 0f;
        for (int i = 0; i < doors.Count; i++)
        {
            if (open[i]) continue;
            bool alive = false;
            foreach (var m in g.mobs) if (!m.dead && !m.add && RoomOf(m.Home) == i + 1) { alive = true; break; }
            if (alive) continue;
            open[i] = true; g.doors.Remove(doors[i]);
            if (doorModels[i] != null) doorModels[i].SetActive(false);
            g.ui.Banner("The gate opens", i == doors.Count - 1 ? "The boss waits beyond" : "Room " + (i + 2) + " of 4");
        }
    }

    // ---------- warnings on the ground (web telegraph) ----------
    public static void Telegraph(Vector3 at, float r, float dur, float dmg, AHMob src)
    {
        var go = new GameObject("Telegraph");
        go.transform.position = new Vector3(at.x, 0f, at.z);
        go.AddComponent<AHTelegraph>().Setup(r, dur, dmg, src);
    }

    // ---------- the beasts' special attacks: called from AHMob while it chases ----------
    public static bool Think(AHMob m, float dt, float dist)
    {
        if (m.type.id == AHRaid.Boss) return AHRaid.Think(AHGame.I, m, dt);
        if (AHDeep.IsBoss(m)) return AHDeep.Think(AHGame.I, m, dt);
        if (AHStory.IsBoss(m)) return AHStory.Think(AHGame.I, m, dt);
        var d = AHJson.O(AHDB.Mobs, m.type.id);
        if (I != null)
        {
            if (I.def != null && m.type.id == Col(I.def, 0)) return I.BossThink(m, dt);
            if (I.def == null && m.type.id == "grull") return I.GrullThink(m, dt);
        }
        if (!AHJson.B(d, "slam")) return false;
        // web d.slam: a heavy blow in front, with a warning circle
        Brain b = SlamBrain(m);
        b.slamT -= dt;
        if (b.slamT <= 0f && dist < 140f * AHDB.S)
        {
            Telegraph(m.transform.position + m.transform.forward * 36f * AHDB.S, 75f * AHDB.S, 1.1f, 22f, m);
            m.Windup(1.1f); b.slamT = 4.5f; return true;
        }
        return false;
    }

    static readonly Dictionary<AHMob, Brain> slamBrains = new Dictionary<AHMob, Brain>();
    static Brain SlamBrain(AHMob m)
    {
        Brain b; if (!slamBrains.TryGetValue(m, out b) || b == null) slamBrains[m] = b = new Brain();
        if (slamBrains.Count > 200) { var dead = new List<AHMob>(); foreach (var k in slamBrains.Keys) if (k == null) dead.Add(k); foreach (var k in dead) slamBrains.Remove(k); }
        return b;
    }

    Brain BrainOf(AHMob m) { Brain b; if (!brains.TryGetValue(m, out b)) brains[m] = b = new Brain(); return b; }

    List<string> Lines() { var l = new List<string>(); var a = AHJson.A(def, "bossLines"); if (a != null) foreach (var o in a) l.Add((string)o); while (l.Count < 3) l.Add(""); return l; }

    // web dbossThink
    bool BossThink(AHMob m, float dt)
    {
        var b = BrainOf(m); var lines = Lines(); float S = AHDB.S;
        if (!b.engaged) { b.engaged = true; b.aT = 3f; b.bT = 7f; b.cT = 11f; g.ui.Banner(m.type.name, lines[0]); }
        float f = m.hp / m.type.hp, fast = f < 0.3f ? 0.65f : 1f;
        string shortName = m.type.name.Split(',')[0];
        if (f < 0.3f && !m.enraged) { m.enraged = true; g.ui.Banner(shortName + " is enraged", "Attacks come faster"); }
        if (!b.add1 && f < 0.6f) { b.add1 = true; g.ui.Toast(lines[2], 3f); Adds(m, AHJson.S(def, "add"), 2, 70f); }
        if (!b.add2 && f < 0.3f) { b.add2 = true; Adds(m, AHJson.S(def, "add"), 3, 70f); }
        b.aT -= dt; b.bT -= dt; b.cT -= dt;
        float hit = Mathf.Round(m.type.dmg * 1.6f);
        Vector3 pp = g.player.transform.position, mp = m.transform.position;
        if (b.aT <= 0f)
        {
            b.aT = 5f * fast;
            for (int k = 0; k < 2; k++) Telegraph(pp + new Vector3(Random.Range(-80f, 80f), 0, Random.Range(-80f, 80f)) * S, 75f * S, 1.5f, hit, m);
            g.ui.Toast(lines[1], 2.5f);
            return false;
        }
        if (b.bT <= 0f)
        {
            b.bT = 9f * fast;
            float rr = m.type.radius + 130f * S;
            for (int k = 0; k < 8; k++) { float a = k / 8f * Mathf.PI * 2f; Telegraph(mp + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rr, 75f * S, 1.4f, hit, m); }
            m.Windup(1.4f); return true;
        }
        if (f < 0.65f && b.cT <= 0f)
        {
            b.cT = 13f * fast;
            Telegraph(mp, m.type.radius + 170f * S, 1.7f, Mathf.Round(hit * 1.2f), m);
            m.Windup(1.7f); g.ui.Toast(shortName + " gathers power! Get away!", 2.5f);
            return true;
        }
        return false;
    }

    // web bossThink: Forgemaster Grull slams where you stand and calls imps from the forge
    bool GrullThink(AHMob m, float dt)
    {
        var b = BrainOf(m);
        if (!b.engaged) { b.engaged = true; b.slamT = 3f; g.ui.Toast("Forgemaster Grull: \"Who dares enter my forge?\"", 3f); }
        float f = m.hp / m.type.hp;
        if (!b.add1 && f < 0.7f) { b.add1 = true; g.ui.Toast("Grull calls imps from the forge!", 2.5f); Adds(m, "imp", 2, 90f); }
        if (!b.add2 && f < 0.35f) { b.add2 = true; g.ui.Toast("Grull calls imps from the forge!", 2.5f); Adds(m, "imp", 2, 90f); }
        if (!m.enraged && f < 0.3f) { m.enraged = true; g.ui.Banner("Grull is enraged", "His slams come faster"); }
        b.slamT -= dt;
        if (b.slamT <= 0f)
        {
            float dur = m.enraged ? 1.0f : 1.4f;
            Telegraph(g.player.transform.position, 100f * AHDB.S, dur, 34f, m);
            m.Windup(dur); b.slamT = m.enraged ? 3.8f : 5.5f;
            return true;
        }
        return false;
    }

    // web boss2Adds / spawnAdds: helpers appear around the boss and come straight for you
    void Adds(AHMob boss, string type, int n, float dist)
    {
        if (string.IsNullOrEmpty(type)) return;
        AHMobType t = null;
        foreach (var o in g.mobs) if (o.type.id == type) { t = o.type; break; }
        if (t == null) return;
        Vector3 back = -boss.transform.forward;
        for (int k = 0; k < n; k++)
        {
            float a = (k - (n - 1) / 2f) * 1.2f;
            Vector3 dir = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0) * back;
            Vector3 at = g.Resolve(boss.transform.position + dir * (boss.type.radius + dist * AHDB.S), t.radius);
            var m = AHMob.Create(g, t, at);
            m.add = true; m.Engage();
            g.mobs.Add(m);
            AHFx.Pop(at + Vector3.up * 0.6f, 1.6f, new Color(1f, 0.5f, 0.2f));
        }
    }

    // ---------- a beast fell (web dMobDown, and Grull's chest) ----------
    public static void OnMobDown(AHGame g, AHMob m)
    {
        AHFinder.OnMobDown(g, m);
        if (m.type.id == AHRaid.Boss && !m.add) { AHRaid.BossDown(g, m); return; }
        if (I == null || m.add) return;
        I.MobDown(m);
    }

    void MobDown(AHMob m)
    {
        var p = g.player;
        if (def == null)
        {
            if (m.type.id != "grull") return;
            var pool = AHJson.A(AHJson.O(AHDB.Rules, "CLASS_DROPS"), p.cls.id);
            Chest(m.transform.position, pool, "iron_bar", 3);
            g.ui.Banner("Victory", "Forgemaster Grull is defeated");
            g.ui.Toast("A chest appeared where Grull fell. Open it for your loot.", 4f);
            return;
        }
        string[] gems = { "sapphire", "ruby", "emerald" };
        var sets = AHJson.A(AHJson.O(def, "sets"), p.cls.id);
        if (m.type.id == Col(def, 0))
        {
            g.quests.Event("dclear", id, 1, g);
            Chest(m.transform.position, sets, gems[Random.Range(0, 3)], 1 + Mathf.FloorToInt(tier));
            int clears; long at; Rec(id, out clears, out at);
            clears++; AHPrefs.SetString(Key(id), clears + "|" + NowMs);
            string name = AHJson.S(def, "name");
            if (clears == 1)
            {
                long bonus = (long)Mathf.Round(2000f * tier * tier) * AHDB.CU;
                p.AddMoney(bonus, m.transform.position);
                g.ui.Toast("First clear of " + name + "! Bonus " + AHItems.MoneyText(bonus) + ".", 4f);
            }
            g.ui.Banner(name + " cleared!", "Open the chest for your set piece");
            for (int k = 0; k < 4; k++) AHFx.Ring(m.transform.position, 0.5f, 2f + k, new Color(1f, 0.85f, 0.4f), 1.2f + k * 0.3f);
            g.ui.Toast("The way out is back at the entrance. The dungeon seals for " + (int)DLockMin + " minutes.", 4f);
            g.SaveProgress();
        }
        else if (m.type.id == Col(def, 1) || m.type.elite)
        {
            string gem = gems[Random.Range(0, 3)];
            p.bag.Add(gem);
            if (sets != null && sets.Count > 0 && Random.value < 0.15f)
            {
                string pid = (string)sets[Random.Range(0, sets.Count)];
                if (p.bag.Add(pid)) { var it = AHItems.Get(pid); g.ui.Banner(it != null ? it.name : pid, "Mini-boss drop!"); }
            }
            var gi = AHItems.Get(gem);
            g.ui.Toast(m.type.name + " dropped a " + (gi != null ? gi.name.ToLowerInvariant() : gem) + ".", 3f);
        }
    }

    // ---------- the chest (web spawnChest / lootChest) ----------
    void Chest(Vector3 at, List<object> pool, string extra, int extraN)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        Material wood = new Material(sh), gold = new Material(sh);
        wood.SetColor("_BaseColor", AHGame.Hex(0x5a3a22).linear); gold.SetColor("_BaseColor", AHGame.Hex(0xe0b040).linear); gold.SetFloat("_Metallic", 0.8f); gold.SetFloat("_Smoothness", 0.6f);
        var root = new GameObject("Chest"); root.transform.position = g.Resolve(at, 0.6f);
        root.transform.rotation = g.Face(g.player.transform.position - root.transform.position);
        System.Action<Transform, Vector3, Vector3, Material> box = (par, pos, size, mt) =>
        {
            var b = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube); Object.Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(par, false); b.transform.localPosition = pos; b.transform.localScale = size; b.GetComponent<Renderer>().sharedMaterial = mt;
        };
        float k = 1.4f; Transform lid = null;
        // the boss's chest: KayKit's gold-banded treasure chest (KayKit Dungeon Remastered, CC0), its lid hinged at the back
        var cpf = Resources.Load<GameObject>("AH/Models/KK/kk_dg_chest_gold");
        if (cpf != null)
        {
            var ch = Object.Instantiate(cpf, root.transform, false); ch.transform.localScale = Vector3.one * 0.85f;
            foreach (var cl in ch.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
            foreach (var t in ch.GetComponentsInChildren<Transform>(true)) if (t.name.Contains("lid")) lid = t;
            AHModel.SetShadows(ch);
            if (lid == null) Object.Destroy(ch);
        }
        if (lid == null)
        {
            box(root.transform, new Vector3(0, 0.25f, 0) * k, new Vector3(0.9f, 0.5f, 0.6f) * k, wood);
            box(root.transform, new Vector3(0, 0.5f, 0) * k, new Vector3(0.94f, 0.06f, 0.64f) * k, gold);
            box(root.transform, new Vector3(0, 0.26f, 0) * k, new Vector3(0.1f, 0.52f, 0.64f) * k, gold);
            lid = new GameObject("Lid").transform; lid.SetParent(root.transform, false); lid.localPosition = new Vector3(0, 0.52f, -0.3f) * k;
            box(lid, new Vector3(0, 0.1f, 0.3f) * k, new Vector3(0.9f, 0.2f, 0.6f) * k, wood);
            box(lid, new Vector3(0, 0.2f, 0.3f) * k, new Vector3(0.94f, 0.04f, 0.64f) * k, gold);
        }
        var lg = new GameObject("Glow"); lg.transform.SetParent(root.transform, false); lg.transform.localPosition = Vector3.up * 1.2f;
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.75f, 0.3f); li.range = 6f; li.intensity = 2.5f;
        AHFx.Ring(root.transform.position, 0.3f, 2.4f, new Color(1f, 0.8f, 0.3f), 1.2f);
        AHSpot spot = null;
        spot = new AHSpot { kind = "use", name = "Open chest", pos = root.transform.position, r = 0.6f, reach = 2.6f };
        spot.use = () =>
        {
            var p = g.player;
            if (p.bag.UsedSlots > p.bag.SlotsMax - 3) { g.ui.Toast("Make room in your bag (3 free slots) to open the chest."); return; }
            var drops = new List<string>(); if (pool != null) foreach (var o in pool) drops.Add((string)o);
            var got = new List<string>();
            if (drops.Count > 0)
            {
                var unowned = drops.FindAll(x => p.bag.Count(x) == 0 && !p.bag.gear.ContainsValue(x));
                var from = unowned.Count > 0 ? unowned : drops;
                got.Add(from[Random.Range(0, from.Count)]);
                if (Random.value < 0.3f) got.Add(drops[Random.Range(0, drops.Count)]);
            }
            foreach (var x in got) p.bag.Add(x);
            p.bag.Add(extra, extraN);
            AHGather.Spots.Remove(spot);
            lid.localRotation = Quaternion.Euler(-70f, 0, 0);
            AHFx.Ring(root.transform.position, 0.3f, 2f, new Color(1f, 0.85f, 0.4f), 1f);
            var names = got.ConvertAll(x => { var it = AHItems.Get(x); return it != null ? it.name : x; });
            var ei = AHItems.Get(extra);
            if (got.Count > 0) { var first = AHItems.Get(got[0]); g.ui.Banner(names[0], first != null && first.set != null ? AHItems.SetName(first.set) + " set piece" : "Treasure"); }
            g.ui.Toast("Loot: " + (names.Count > 0 ? string.Join(", ", names.ToArray()) + " and " : "") + extraN + " × " + (ei != null ? ei.name : extra) + ". Equip them in Gear.", 5f);
            g.SaveProgress();
        };
        AHGather.Spots.Add(spot);
    }

    void OnDestroy() { if (I == this) I = null; }
}

// the red circle: fills up, then hits whoever stands inside
public class AHTelegraph : MonoBehaviour
{
    float r, dur, dmg, t;
    AHMob src;
    GameObject grow;

    public void Setup(float r, float dur, float dmg, AHMob src)
    {
        this.r = r; this.dur = dur; this.dmg = dmg; this.src = src;
        var fill = AHFx.Keep(PrimitiveType.Quad, new Color(1f, 0.16f, 0.1f, 0.12f), 2f);
        fill.transform.SetParent(transform, false); fill.transform.localPosition = Vector3.up * 0.07f; fill.transform.localScale = Vector3.one * r * 2f;
        var edge = AHFx.Keep(PrimitiveType.Quad, new Color(1f, 0.2f, 0.1f, 0.95f), 1f);
        edge.transform.SetParent(transform, false); edge.transform.localPosition = Vector3.up * 0.08f; edge.transform.localScale = Vector3.one * r * 2f;
        grow = AHFx.Keep(PrimitiveType.Quad, new Color(1f, 0.25f, 0.1f, 0.32f), 2f);
        grow.transform.SetParent(transform, false); grow.transform.localPosition = Vector3.up * 0.09f; grow.transform.localScale = Vector3.one * 0.01f;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / dur);
        grow.transform.localScale = Vector3.one * Mathf.Max(0.01f, r * 2f * k);
        if (t < dur) return;
        var g = AHGame.I;
        if (g != null && g.player != null && !g.player.dead)
        {
            Vector3 d = g.player.transform.position - transform.position; d.y = 0;
            if (d.magnitude < r + AHPlayer.Radius) g.player.Hurt(dmg, src);
        }
        AHFx.Ring(transform.position, r * 0.6f, r * 1.1f, new Color(1f, 0.45f, 0.2f, 0.8f), 0.35f);
        Destroy(gameObject);
    }
}

public class AHFlicker : MonoBehaviour
{
    Light li; float baseI, seed;
    void Start() { li = GetComponent<Light>(); baseI = li != null ? li.intensity : 1f; seed = Random.value * 10f; }
    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * 3.2f);
        if (li != null) li.intensity = baseI * (0.75f + 0.5f * n);
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x * (1.4f + 0.4f * n), transform.localScale.z);
    }
}

public class AHPortalSpin : MonoBehaviour
{
    Light li; float baseI;
    void Start() { li = GetComponentInChildren<Light>(); baseI = li != null ? li.intensity : 1f; }
    void Update() { if (li != null) li.intensity = baseI * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.3f)); }
}
