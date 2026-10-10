// Ashen Hollow: inside your house. "Go inside" (on the house's window at your homestead) takes you into a real room:
// plank floor, plaster-and-oak walls, windows of warm light, and a door back out. The world outside is put away while
// you are in, and the wall between you and the camera steps aside so you can always see the room.
// Furnish it from the Furnish spot by the door (or the house window): a woven rug, a feather bed (sleep here for full
// rested XP), an oak table, a stone fireplace with a real fire, a bookshelf, a wardrobe (opens your Wardrobe), a storage
// chest (your bank), potted plants and a painting of the Hollow. Every piece adds comfort, like the comforts outside:
// more rested XP and a shorter wait between sleeps.
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class AHInterior
{
    public class Piece { public string id, name, blurb; public long cost; public int pts; }
    public static readonly Piece[] Pieces =
    {
        new Piece { id = "rug", name = "Woven rug", blurb = "A thick red-and-gold rug for the middle of the room.", cost = 300, pts = 1 },
        new Piece { id = "bed", name = "Feather bed", blurb = "Sleep in your own bed: full rested XP and HP.", cost = 1500, pts = 2 },
        new Piece { id = "table", name = "Oak table", blurb = "A round oak table with two stools and a candle.", cost = 600, pts = 1 },
        new Piece { id = "fire", name = "Stone fireplace", blurb = "A crackling fire against the back wall.", cost = 2500, pts = 2 },
        new Piece { id = "shelf", name = "Bookshelf", blurb = "Tall shelves of books and keepsakes.", cost = 900, pts = 1 },
        new Piece { id = "wardrobe", name = "Wardrobe", blurb = "Change your costume and dye at home.", cost = 1200, pts = 1 },
        new Piece { id = "chest", name = "Storage chest", blurb = "Your bank vault, at the foot of the bed.", cost = 800, pts = 1 },
        new Piece { id = "plants", name = "Potted plants", blurb = "Two leafy pots by the door.", cost = 200, pts = 1 },
        new Piece { id = "painting", name = "Painting of the Hollow", blurb = "Ashen Hollow at sunset, in a gilt frame.", cost = 1000, pts = 1 },
    };
    public static Piece Get(string id) { foreach (var p in Pieces) if (p.id == id) return p; return null; }
    public static bool Has(AHPlayer p, string id) { return preview || (p.home != null && p.home.furn != null && p.home.furn.Contains(id)); }
    public static int Pts(AHPlayer p) { int n = 0; if (p.home != null && p.home.furn != null) foreach (var x in Pieces) if (p.home.furn.Contains(x.id)) n += x.pts; return n; }
    public static object Cost(Piece x) { return new Dictionary<string, object> { { "money", (double)x.cost } }; }

    public static float W = 10f, D = 8f, H = 3f;   // the room's size: your house, or the guild hall (AHGuildHall.cs)
    public static bool Inside { get { return root != null; } }
    public static bool InGuild { get { return root != null && guild; } }
    static bool guild;
    static bool preview;
    static Transform root; static Vector3 c, outside; static float outYaw;
    static readonly List<GameObject> hidden = new List<GameObject>();
    static readonly List<AHSpot> spots = new List<AHSpot>();
    static readonly List<KeyValuePair<Transform, Vector3>> walls = new List<KeyValuePair<Transform, Vector3>>();

    public static void Setup(AHGame g) { root = null; preview = false; guild = false; hidden.Clear(); spots.Clear(); walls.Clear(); }

    // the room keeps you inside its walls (instead of the land's rules)
    public static Vector3 Clamp(Vector3 p, float r)
    {
        // furniture you can't walk through (pushed toward the room, never through a wall), then the walls
        foreach (var b in Blocks()) p = Push(b, p, r);
        p.x = Mathf.Clamp(p.x, c.x - W / 2 + r + 0.15f, c.x + W / 2 - r - 0.15f);
        p.z = Mathf.Clamp(p.z, c.z - D / 2 + r + 0.15f, c.z + D / 2 - r - 0.15f);
        p.y = 0f;
        return p;
    }
    static IEnumerable<Rect> Blocks()
    {
        var pl = AHGame.I != null ? AHGame.I.player : null; if (pl == null) yield break;
        if (guild) { foreach (var b in GuildBlocks()) yield return b; yield break; }
        if (Has(pl, "bed")) yield return R(-3.2f, 2.5f, 1.8f, 2.6f);
        if (Has(pl, "table")) yield return R(2.6f, 0.6f, 1.3f, 1.3f);
        if (Has(pl, "fire")) yield return R(0f, 3.55f, 2.2f, 0.9f);
        if (Has(pl, "shelf")) yield return R(4.6f, 2.2f, 0.8f, 2f);
        if (Has(pl, "wardrobe")) yield return R(-4.6f, -0.6f, 0.8f, 1.6f);
        if (Has(pl, "chest")) yield return R(-3.2f, 0.75f, 1.1f, 0.6f);
    }
    static Rect R(float x, float z, float w, float d) { return new Rect(c.x + x - w / 2, c.z + z - d / 2, w, d); }
    static Vector3 Push(Rect b, Vector3 p, float r)
    {
        if (p.x < b.xMin - r || p.x > b.xMax + r || p.z < b.yMin - r || p.z > b.yMax + r) return p;
        float l = p.x - (b.xMin - r), rr = (b.xMax + r) - p.x, d = p.z - (b.yMin - r), u = (b.yMax + r) - p.z;
        // a side against a wall is no way out
        float x0 = c.x - W / 2 + r + 0.15f, x1 = c.x + W / 2 - r - 0.15f, z0 = c.z - D / 2 + r + 0.15f, z1 = c.z + D / 2 - r - 0.15f;
        if (b.xMin - r < x0) l = 1e9f; if (b.xMax + r > x1) rr = 1e9f; if (b.yMin - r < z0) d = 1e9f; if (b.yMax + r > z1) u = 1e9f;
        float m = Mathf.Min(Mathf.Min(l, rr), Mathf.Min(d, u)); if (m >= 1e8f) return p;
        if (m == l) p.x = b.xMin - r; else if (m == rr) p.x = b.xMax + r; else if (m == d) p.z = b.yMin - r; else p.z = b.yMax + r;
        return p;
    }

    // ---------- in and out ----------
    public static void Enter(AHGame g, bool asPreview = false)
    {
        var p = g.player; if (p == null || Inside) return;
        if (!asPreview && p.home == null) { g.ui.Toast("You need a house first."); return; }
        if (!asPreview && AHGame.AreaId != AHHome.Area) { g.ui.Toast("Go home to your homestead to step inside your house."); return; }
        guild = false; W = 10f; D = 8f; H = 3f;
        GoIn(g, asPreview);
        AHChat.Add("system", preview ? "Inside your house (preview: every piece shown, nothing bought)." : "You step inside your house.");
    }
    static void GoIn(AHGame g, bool asPreview)
    {
        var p = g.player;
        preview = asPreview;
        if (p.mounted) AHComp.Dismount(g, true);
        outside = p.transform.position; outYaw = g.camYaw;
        c = new Vector3(outside.x, 0f, outside.z);
        g.ui.Fade(true);
        // put the world away
        hidden.Clear();
        foreach (var top in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!top.activeSelf || Keep(g, top)) continue;
            top.SetActive(false); hidden.Add(top);
        }
        Build(g);
        if (root != null) AHBevel.DressRoom(root);   // grain on the wood, weave on the cloth, stone on the hearth
        p.transform.position = c + new Vector3(0f, 0f, -D / 2 + 1.2f); p.transform.rotation = Quaternion.identity;
        g.camYaw = 0f;
        g.ui.Invoke("FadeOff", 0.5f);
    }
    static bool Keep(AHGame g, GameObject top)
    {
        if (top == g.gameObject || (g.player != null && top == g.player.transform.root.gameObject) || (g.cam != null && top == g.cam.transform.root.gameObject)) return true;
        if (g.ui != null && top == g.ui.transform.root.gameObject) return true;
        if (top.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) return true;
        var li = top.GetComponent<Light>(); if (li != null && li.type == LightType.Directional) return true;
        if (top.name == "Cutscene") return true;
        return false;
    }

    public static void Leave(AHGame g)
    {
        if (!Inside) return;
        g.ui.Fade(true);
        foreach (var s in spots) AHGather.Spots.Remove(s); spots.Clear(); walls.Clear();
        UnityEngine.Object.Destroy(root.gameObject); root = null;
        foreach (var h in hidden) if (h != null) h.SetActive(true); hidden.Clear();
        var p = g.player; p.transform.position = outside; g.camYaw = outYaw; preview = false; guild = false;
        g.ui.Invoke("FadeOff", 0.5f);
    }

    // after buying a piece: build the room again with it
    public static void Refresh(AHGame g) { if (!Inside) return; foreach (var s in spots) AHGather.Spots.Remove(s); spots.Clear(); walls.Clear(); UnityEngine.Object.Destroy(root.gameObject); Build(g); }

    // the wall between the camera and you steps aside
    public static void Tick(AHGame g)
    {
        if (!Inside || g.cam == null) return;
        // indoors the sun only comes in through the windows: the day's light is turned well down (the day cycle sets
        // it again every frame, so this never adds up)
        if (RenderSettings.sun != null) RenderSettings.sun.intensity *= 0.35f;
        RenderSettings.ambientSkyColor *= 0.6f; RenderSettings.ambientEquatorColor *= 0.6f;
        Vector3 cp = g.cam.transform.position;
        foreach (var w in walls) { bool show = Vector3.Dot(cp - w.Key.position, w.Value) < 0f; if (w.Key.gameObject.activeSelf != show) w.Key.gameObject.SetActive(show); }
    }

    // ---------- the room ----------
    static Shader lit; static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
    static Material M(int hex, float smooth = 0.15f)
    {
        Material m; if (mats.TryGetValue(hex, out m) && m != null) return m;
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(lit); m.SetColor("_BaseColor", new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f)); m.SetFloat("_Smoothness", smooth);
        mats[hex] = m; return m;
    }
    static Material Glow(Color col)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", col); return m;
    }
    static Transform Box(Transform t, Vector3 at, Vector3 size, int hex, float yaw = 0f, PrimitiveType kind = PrimitiveType.Cube)
    {
        var go = GameObject.CreatePrimitive(kind); UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(t, false); go.transform.localPosition = at; go.transform.localScale = size; go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.GetComponent<Renderer>().sharedMaterial = M(hex); return go.transform;
    }
    const int Oak = 0x7a5232, DarkOak = 0x4e3420, Plaster = 0xd9cdb4, Stone = 0x8a8278, Gold = 0xc9a24a, Red = 0x9c2f2a;

    static void Build(AHGame g)
    {
        if (guild) { BuildGuild(g); return; }
        var p = g.player;
        root = new GameObject("House interior").transform; root.position = c;
        // darkness all round (an inside-out box) and a floor under it all
        var shell = new GameObject("Shell"); shell.transform.SetParent(root, false); shell.transform.localScale = new Vector3(60f, 30f, 60f); shell.transform.localPosition = new Vector3(0, 6f, 0);
        shell.AddComponent<MeshFilter>().sharedMesh = InsideOutCube(); shell.AddComponent<MeshRenderer>().sharedMaterial = Glow(new Color(0.05f, 0.04f, 0.035f));
        // floor: oak planks in two tones
        for (int i = 0; i < 10; i++) Box(root, new Vector3(-W / 2 + 0.5f + i, -0.05f, 0f), new Vector3(0.98f, 0.1f, D), i % 2 == 0 ? 0x8a5e38 : 0x7d5431);
        // walls: plaster above oak panelling, beams at the corners
        Wall(new Vector3(0, 0, D / 2), new Vector3(W, H, 0.2f), Vector3.forward, false);
        Wall(new Vector3(0, 0, -D / 2), new Vector3(W, H, 0.2f), Vector3.back, true);
        Wall(new Vector3(W / 2, 0, 0), new Vector3(0.2f, H, D), Vector3.right, false);
        Wall(new Vector3(-W / 2, 0, 0), new Vector3(0.2f, H, D), Vector3.left, false);
        foreach (var x in new[] { -W / 2, W / 2 }) foreach (var z in new[] { -D / 2, D / 2 }) Box(root, new Vector3(x, H / 2, z), new Vector3(0.3f, H, 0.3f), DarkOak);
        // a warm light for the whole room
        Lamp(root, new Vector3(0f, 2.6f, 0f), new Color(1f, 0.82f, 0.6f), 12f, 1.3f);

        if (Has(p, "rug")) { Box(root, new Vector3(0f, 0.01f, 0.2f), new Vector3(4f, 0.02f, 2.6f), Red); Box(root, new Vector3(0f, 0.02f, 0.2f), new Vector3(3.4f, 0.02f, 2f), 0xb8862f); Box(root, new Vector3(0f, 0.03f, 0.2f), new Vector3(3f, 0.02f, 1.6f), Red); }
        if (Has(p, "bed"))
        {
            var b = Group("Bed", new Vector3(-3.2f, 0f, 2.5f));
            Box(b, new Vector3(0, 0.25f, 0), new Vector3(1.7f, 0.35f, 2.4f), DarkOak); Box(b, new Vector3(0, 0.5f, -0.05f), new Vector3(1.55f, 0.18f, 2.2f), 0xeee6d6);
            Box(b, new Vector3(0, 0.62f, -0.35f), new Vector3(1.56f, 0.08f, 1.5f), 0x3d5a8a); Box(b, new Vector3(0, 0.66f, 0.85f), new Vector3(1.1f, 0.14f, 0.38f), 0xffffff);
            Box(b, new Vector3(0, 0.75f, 1.2f), new Vector3(1.8f, 1.3f, 0.12f), DarkOak); Box(b, new Vector3(0, 0.35f, -1.2f), new Vector3(1.8f, 0.6f, 0.1f), DarkOak);
            Spot(g, "Bed", b.position, () => Sleep(g));
        }
        if (Has(p, "chest"))
        {
            var t = Group("Chest", new Vector3(-3.2f, 0f, 0.75f));
            Box(t, new Vector3(0, 0.28f, 0), new Vector3(1f, 0.55f, 0.55f), Oak); Box(t, new Vector3(0, 0.6f, 0), new Vector3(1.04f, 0.12f, 0.59f), DarkOak);
            foreach (float s in new[] { -0.35f, 0.35f }) Box(t, new Vector3(s, 0.33f, 0), new Vector3(0.06f, 0.62f, 0.6f), Gold);
            Spot(g, "Storage chest", t.position, () => { if (preview) g.ui.Toast("Your bank vault opens here."); else g.ui.OpenBank(); });
        }
        if (Has(p, "table"))
        {
            var t = Group("Table", new Vector3(2.6f, 0f, 0.6f));
            Box(t, new Vector3(0, 0.75f, 0), new Vector3(1.2f, 0.06f, 1.2f), Oak, 0f, PrimitiveType.Cylinder); Box(t, new Vector3(0, 0.38f, 0), new Vector3(0.18f, 0.37f, 0.18f), DarkOak, 0f, PrimitiveType.Cylinder);
            foreach (var s in new[] { new Vector3(-0.95f, 0, 0.1f), new Vector3(0.9f, 0, -0.2f) }) Box(t, s + new Vector3(0, 0.25f, 0), new Vector3(0.45f, 0.25f, 0.45f), DarkOak, 0f, PrimitiveType.Cylinder);
            Box(t, new Vector3(0.1f, 0.88f, 0.1f), new Vector3(0.08f, 0.1f, 0.08f), 0xf2ead2, 0f, PrimitiveType.Cylinder);
            var fl = Box(t, new Vector3(0.1f, 1.02f, 0.1f), new Vector3(0.05f, 0.08f, 0.05f), 0, 0f, PrimitiveType.Sphere); fl.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 0.8f, 0.35f));
            Lamp(t, new Vector3(0.1f, 1.2f, 0.1f), new Color(1f, 0.75f, 0.4f), 4f, 0.8f);
        }
        if (Has(p, "fire"))
        {
            var t = Group("Fireplace", new Vector3(0f, 0f, D / 2 - 0.45f));
            Box(t, new Vector3(0, 0.75f, 0), new Vector3(2.2f, 1.5f, 0.8f), Stone); Box(t, new Vector3(0, 1.55f, -0.05f), new Vector3(2.4f, 0.14f, 0.9f), DarkOak);
            Box(t, new Vector3(0, 2.3f, 0.1f), new Vector3(1.2f, 1.4f, 0.6f), Stone);
            Box(t, new Vector3(0, 0.55f, -0.38f), new Vector3(1.2f, 0.9f, 0.1f), 0x1a1410);
            var fire = Box(t, new Vector3(0, 0.38f, -0.42f), new Vector3(0.8f, 0.45f, 0.1f), 0, 0f, PrimitiveType.Sphere); fire.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 0.55f, 0.15f));
            for (int i = 0; i < 3; i++) Box(t, new Vector3(-0.3f + i * 0.3f, 0.18f, -0.45f), new Vector3(0.12f, 0.12f, 0.6f), DarkOak, 90f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 20f * (i - 1), 0f);
            var lg = Lamp(t, new Vector3(0, 0.8f, -1f), new Color(1f, 0.55f, 0.25f), 9f, 2f); lg.AddComponent<AHFlicker>();
        }
        if (Has(p, "shelf"))
        {
            var t = Group("Bookshelf", new Vector3(W / 2 - 0.4f, 0f, 2.2f)); t.localRotation = Quaternion.Euler(0, -90f, 0);
            Box(t, new Vector3(0, 1.1f, 0), new Vector3(1.9f, 2.2f, 0.5f), DarkOak);
            var rnd = new System.Random(7); int[] cols = { 0x7a2a2a, 0x2a4a7a, 0x3a6a3a, 0x8a6a2a, 0x5a3a6a };
            for (int sh = 0; sh < 4; sh++) { float y = 0.25f + sh * 0.52f; Box(t, new Vector3(0, y - 0.04f, 0.02f), new Vector3(1.8f, 0.05f, 0.48f), Oak); float x = -0.8f; while (x < 0.8f) { float bw = 0.07f + (float)rnd.NextDouble() * 0.07f, bh = 0.3f + (float)rnd.NextDouble() * 0.14f; Box(t, new Vector3(x + bw / 2, y + bh / 2, 0.05f), new Vector3(bw * 0.9f, bh, 0.36f), cols[rnd.Next(cols.Length)]); x += bw; } }
        }
        if (Has(p, "wardrobe"))
        {
            var t = Group("Wardrobe", new Vector3(-W / 2 + 0.4f, 0f, -0.6f)); t.localRotation = Quaternion.Euler(0, 90f, 0);
            Box(t, new Vector3(0, 1.1f, 0), new Vector3(1.5f, 2.2f, 0.65f), Oak); Box(t, new Vector3(0, 2.25f, 0), new Vector3(1.6f, 0.1f, 0.72f), DarkOak);
            Box(t, new Vector3(0, 1.1f, 0.33f), new Vector3(0.02f, 2.1f, 0.02f), DarkOak); foreach (float s in new[] { -0.08f, 0.08f }) Box(t, new Vector3(s, 1.15f, 0.35f), new Vector3(0.04f, 0.12f, 0.04f), Gold);
            Spot(g, "Wardrobe", t.position, () => { if (preview) g.ui.Toast("Your Wardrobe opens here."); else g.ui.OpenWardrobe(); });
        }
        if (Has(p, "plants"))
            foreach (float s in new[] { -1.6f, 1.6f })
            {
                var t = Group("Plant", new Vector3(s, 0f, -D / 2 + 0.5f));
                Box(t, new Vector3(0, 0.22f, 0), new Vector3(0.45f, 0.22f, 0.45f), 0xa0522d, 0f, PrimitiveType.Cylinder);
                for (int i = 0; i < 5; i++) { float a = i * 72f * Mathf.Deg2Rad; Box(t, new Vector3(Mathf.Cos(a) * 0.15f, 0.65f + (i % 2) * 0.12f, Mathf.Sin(a) * 0.15f), new Vector3(0.35f, 0.4f, 0.35f), i % 2 == 0 ? 0x3f7a35 : 0x4f8f40, 0f, PrimitiveType.Sphere); }
            }
        if (Has(p, "painting"))
        {
            var t = Group("Painting", new Vector3(2.6f, 1.75f, D / 2 - 0.12f));
            Box(t, Vector3.zero, new Vector3(1.6f, 1.1f, 0.06f), Gold);
            Box(t, new Vector3(0, 0.2f, -0.035f), new Vector3(1.4f, 0.5f, 0.02f), 0xe89a55); Box(t, new Vector3(0, -0.22f, -0.035f), new Vector3(1.4f, 0.42f, 0.02f), 0x4a6a3a);
            Box(t, new Vector3(0.3f, 0.08f, -0.045f), new Vector3(0.3f, 0.3f, 0.02f), 0xffd27a, 0f, PrimitiveType.Sphere);
            Box(t, new Vector3(-0.35f, -0.05f, -0.045f), new Vector3(0.22f, 0.34f, 0.02f), 0x6a4a3a);
        }
        // the door, and the Furnish spot beside it
        var door = Group("Door", new Vector3(0f, 0f, -D / 2 + 0.12f));
        foreach (var w in walls) if (w.Value == Vector3.back) door.SetParent(w.Key, true);
        Box(door, new Vector3(0, 1.05f, 0), new Vector3(1.2f, 2.1f, 0.1f), DarkOak); Box(door, new Vector3(0.4f, 1f, -0.07f), new Vector3(0.08f, 0.08f, 0.06f), Gold);
        Box(root, new Vector3(0f, 0.015f, -D / 2 + 0.55f), new Vector3(1.3f, 0.03f, 0.6f), 0x6a5030);   // a doormat marks the way out
        Spot(g, "Leave house", door.position + Vector3.forward * 0.3f, () => Leave(g));
        Spot(g, "Furnish", c + new Vector3(-1.3f, 0f, -D / 2 + 0.8f), () => g.ui.OpenFurnish());
        Tick(g);
    }

    static Transform Group(string n, Vector3 at) { var t = new GameObject(n).transform; t.SetParent(root, false); t.localPosition = at; return t; }
    static void Wall(Vector3 at, Vector3 size, Vector3 outward, bool door)
    {
        var w = new GameObject("Wall").transform; w.SetParent(root, false); w.localPosition = at;
        bool alongX = size.x > size.z; float len = alongX ? size.x : size.z, th = alongX ? size.z : size.x;
        Action<float, float, float, float, int> seg = (from, to, y0, y1, hex) =>
        {
            float mid = (from + to) / 2, l = to - from; if (l <= 0.01f) return;
            Box(w, alongX ? new Vector3(mid, (y0 + y1) / 2, 0) : new Vector3(0, (y0 + y1) / 2, mid), alongX ? new Vector3(l, y1 - y0, th) : new Vector3(th, y1 - y0, l), hex);
        };
        int low = guild ? Stone : Oak; float dw = guild ? 1.1f : 0.7f, dh = guild ? 3f : 2.2f;   // the guild hall: stone below, a tall double door
        if (door) { seg(-len / 2, -dw, 0, 1.0f, low); seg(dw, len / 2, 0, 1.0f, low); seg(-len / 2, -dw, 1.0f, H, Plaster); seg(dw, len / 2, 1.0f, H, Plaster); seg(-dw, dw, dh, H, Plaster); }
        else { seg(-len / 2, len / 2, 0, 1.0f, low); seg(-len / 2, len / 2, 1.0f, H, Plaster); }
        seg(-len / 2, len / 2, 0.98f, 1.06f, DarkOak); seg(-len / 2, len / 2, H - 0.12f, H, DarkOak);
        // a window of warm light on the long walls
        if (!door)
        {
            var win = new GameObject("Window").transform; win.SetParent(w, false);
            Vector3 inward = -outward * (th / 2 + 0.01f);
            // the hall: tall windows high up between the banners; the house: two (one on the short walls)
            float wy = guild ? 3.4f : 1.9f, ww = guild ? 0.8f : 0.9f, wh = guild ? 1.6f : 0.9f;
            float[] offs = guild ? (alongX ? new[] { -4f, 4f } : new[] { -4f, 0f, 4f }) : alongX ? new[] { -3f, 3f } : new[] { 0f };
            foreach (float off in offs)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); UnityEngine.Object.Destroy(q.GetComponent<Collider>()); q.transform.SetParent(win, false);
                q.transform.localPosition = (alongX ? new Vector3(off, wy, 0) : new Vector3(0, wy, off)) + inward; q.transform.localScale = new Vector3(ww, wh, 1f);
                q.transform.localRotation = Quaternion.LookRotation(outward); q.GetComponent<Renderer>().sharedMaterial = Glow(guild ? new Color(0.75f, 0.85f, 1f) : new Color(1f, 0.85f, 0.55f));
                Box(win, (alongX ? new Vector3(off, wy, 0) : new Vector3(0, wy, off)) + inward * 1.5f, alongX ? new Vector3(0.06f, wh + 0.05f, 0.04f) : new Vector3(0.04f, wh + 0.05f, 0.06f), DarkOak);
                Box(win, (alongX ? new Vector3(off, wy, 0) : new Vector3(0, wy, off)) + inward * 1.5f, alongX ? new Vector3(ww + 0.05f, 0.06f, 0.04f) : new Vector3(0.04f, 0.06f, ww + 0.05f), DarkOak);
                if (guild) Box(win, (alongX ? new Vector3(off, wy - wh / 2 - 0.06f, 0) : new Vector3(0, wy - wh / 2 - 0.06f, off)) + inward * 2f, alongX ? new Vector3(ww + 0.25f, 0.1f, 0.2f) : new Vector3(0.2f, 0.1f, ww + 0.25f), Stone);   // a stone sill
            }
        }
        walls.Add(new KeyValuePair<Transform, Vector3>(w, outward));
    }
    static GameObject Lamp(Transform t, Vector3 at, Color col, float range, float k)
    {
        var lg = new GameObject("Light"); lg.transform.SetParent(t, false); lg.transform.localPosition = at;
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = col; li.range = range; li.intensity = k; li.shadows = LightShadows.None; return lg;
    }
    static void Spot(AHGame g, string name, Vector3 at, Action use)
    {
        var s = new AHSpot { kind = "use", type = "house", name = name, pos = at, r = 0.5f, reach = 1.9f, use = use };
        AHGather.Spots.Add(s); spots.Add(s);
    }
    static Mesh cubeIn;
    static Mesh InsideOutCube()
    {
        if (cubeIn != null) return cubeIn;
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube); var src = tmp.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.Destroy(tmp);
        cubeIn = UnityEngine.Object.Instantiate(src); var tri = cubeIn.triangles; for (int i = 0; i < tri.Length; i += 3) { int a = tri[i]; tri[i] = tri[i + 1]; tri[i + 1] = a; }
        cubeIn.triangles = tri; var n = cubeIn.normals; for (int i = 0; i < n.Length; i++) n[i] = -n[i]; cubeIn.normals = n; return cubeIn;
    }

    static void Sleep(AHGame g)
    {
        var p = g.player; if (preview) { g.ui.Toast("Sleeping here fills your rested XP."); return; }
        var Hm = p.home; long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), left = AHHome.SleepCd(p) - (now - Hm.sleepAt);
        if (left > 0) { g.ui.Toast("Not sleepy yet. Rested again in " + Mathf.CeilToInt(left / 60000f) + " min."); return; }
        Hm.sleepAt = now; p.rested = AHHome.RestCap(p); p.hp = p.maxHp; p.hunger = Mathf.Max(p.hunger, 60f);
        g.ui.Banner("Well rested", "Rested XP is full"); g.ui.Fade(true); g.ui.Invoke("FadeOff", 0.9f); g.MarkDirty();
    }
}

public class AHInteriorTick : MonoBehaviour { void LateUpdate() { if (AHGame.I != null) AHInterior.Tick(AHGame.I); } }

public partial class AHUI
{
    public void OpenFurnish() { wkMode = "furnish"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderFurnish(AHPlayer p)
    {
        wkTitle.text = "Furnish your house";
        var H = p.home;
        wkHint.text = H == null ? "You need a house first." : "Each piece stands in your house and adds comfort (more rested XP, shorter waits to sleep). Comfort from furniture: " + AHInterior.Pts(p) + ".";
        var rows = new List<Action<int>>();
        if (H != null)
        {
            if (!AHInterior.Inside && AHGame.AreaId == AHHome.Area) rows.Add(s => Row(s, "Go inside", new Color(1f, 0.8f, 0.45f), "Walk into your house.", "", new WkBtn { label = "Enter", on = true, col = Go, act = () => { ShowWork(false); AHInterior.Enter(g); } }));
            foreach (var x in AHInterior.Pieces)
            {
                var xx = x; bool have = H.furn != null && H.furn.Contains(x.id); var cost = AHInterior.Cost(x);
                rows.Add(s => Row(s, xx.name + "  <color=#9a9080>+" + xx.pts + " comfort</color>" + (have ? "  <color=#9be37a>placed</color>" : ""), Color.white, xx.blurb, have ? "" : AHHome.CostText(p, cost),
                    have ? null : new WkBtn { label = "Buy", on = AHHome.CanPay(p, cost), col = Go, act = () => { if (!AHHome.Pay(p, cost)) return; if (H.furn == null) H.furn = new List<string>(); H.furn.Add(xx.id); Banner(xx.name, "Comfort " + AHHome.ComfortPts(p)); p.bag.Touch(); g.MarkDirty(); AHInterior.Refresh(g); RenderWork(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
