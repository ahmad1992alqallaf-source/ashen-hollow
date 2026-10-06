// Ashen Hollow: treasure maps.
// Every wild land has its own old maps: its beasts sometimes drop one (bosses often do), and the first time you set foot
// in the Fossil Lands an explorer's map blows to your feet. Read a map in its own land (tap it twice in your bag) and a
// red X with a pale light appears somewhere out there, marked on your map too. Walk to it and dig: silver, ore and
// gems for the land's level, its herbs, potions, a card of one of its beasts, sometimes another map, and one dig in
// a hundred turns up the reins of that land's rare mount (in the Fossil Lands: the Fossilized raptor, a mount of bones).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHTreasure
{
    public const string MapId = "fossil_map", MountId = "fossilraptor", ReinsId = "reins_fossilraptor";
    const string GiftKey = "ah_fossil_gift";
    // the lands with maps: their map item, their land level, their herb and the rare mount one dig in a hundred gives
    class Land { public string area, map, herb, mount; public int lv; }
    static readonly Land[] lands = {
        new Land { area = "meadow", map = "tmap_meadow", lv = 1, herb = "sunpetal", mount = "goldsteed" },
        new Land { area = "silkwood", map = "tmap_silkwood", lv = 8, herb = "sunpetal", mount = "nightmare" },
        new Land { area = "mire", map = "tmap_mire", lv = 6, herb = "mireroot", mount = "bogstrider" },
        new Land { area = "vale", map = "tmap_vale", lv = 4, herb = "sunpetal", mount = "goldsteed" },
        new Land { area = "frost", map = "tmap_frost", lv = 20, herb = "frostbloom", mount = "glacierwolf" },
        new Land { area = "sands", map = "tmap_sands", lv = 15, herb = "emberthorn", mount = "dunestrider" },
        new Land { area = "isle", map = "tmap_isle", lv = 28, herb = "sunpetal", mount = "raptor" },
        new Land { area = "tide", map = "tmap_tide", lv = 30, herb = "mireroot", mount = "tidelizard" },
        new Land { area = "ember", map = "tmap_ember", lv = 36, herb = "emberthorn", mount = "emberlizard" },
        new Land { area = "fossil", map = MapId, lv = 40, herb = "frostbloom", mount = MountId } };
    static Land Here { get { foreach (var l in lands) if (l.area == AHGame.AreaId) return l; return null; } }
    public static string MapHere { get { var l = Here; return l != null ? l.map : null; } }
    public static bool IsMap(string id) { foreach (var l in lands) if (l.map == id) return true; return false; }
    static Land OfMap(string id) { foreach (var l in lands) if (l.map == id) return l; return null; }
    static string Key(string area) { return area == "fossil" ? "ah_fossil_dig" : "ah_dig_" + area; }

    static GameObject marker; static AHSpot spot;
    static readonly System.Random rnd = new System.Random();

    static bool HasDig(string area, out Vector2 web)
    {
        web = Vector2.zero; string s = PlayerPrefs.GetString(Key(area), "");
        if (string.IsNullOrEmpty(s)) return false;
        var a = s.Split(','); float x, z;
        if (a.Length != 2 || !float.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) || !float.TryParse(a[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) return false;
        web = new Vector2(x, z); return true;
    }

    // on arriving in an area: the X if you have one here, and the explorer's map on your first visit to the Fossil Lands
    public static void Setup(AHGame g)
    {
        marker = null; spot = null;
        var L = Here; if (L == null || g.player == null) return;
        Vector2 w; if (HasDig(L.area, out w)) Place(g, w);
        if (L.area == "fossil" && PlayerPrefs.GetInt(GiftKey, 0) == 0)
        {
            PlayerPrefs.SetInt(GiftKey, 1); PlayerPrefs.Save();
            g.player.bag.Add(MapId); g.MarkDirty();
            g.ui.Banner("An old map", "A treasure map blows to your feet. Read it in your bag");
        }
    }

    public static void Read(AHGame g) { Read(g, MapId); }
    // tap a map twice in the bag
    public static void Read(AHGame g, string mapId)
    {
        var L = OfMap(mapId); if (L == null) return;
        string landName = AHItems.Get(mapId) != null ? AHItems.Get(mapId).name.Replace("Treasure map: ", "") : L.area;
        Vector2 w; bool has = HasDig(L.area, out w);
        if (AHGame.AreaId != L.area) { g.ui.Toast(has ? "Your X lies out in " + landName + "." : "A map of " + landName + ". Read it there.", 2.2f); return; }
        if (has) { g.ui.Toast("Your X is already marked. Open the map to find it.", 2f); return; }
        var b = g.data.bounds; Vector2 pick = Vector2.zero; bool ok = false;
        for (int t = 0; t < 200 && !ok; t++)
        {
            float x = Mathf.Lerp(b.x0, b.x1, 0.12f + (float)rnd.NextDouble() * 0.76f), z = Mathf.Lerp(b.z0, b.z1, 0.12f + (float)rnd.NextDouble() * 0.76f);
            Vector3 p = g.W(x, z);
            if (g.Blocked(p, 1.6f) || (p - g.player.transform.position).magnitude < 25f) continue;
            pick = new Vector2(x, z); ok = true;
        }
        if (!ok) { g.ui.Toast("The map's lines blur. Try again somewhere else.", 2f); return; }
        PlayerPrefs.SetString(Key(L.area), pick.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + pick.y.ToString(System.Globalization.CultureInfo.InvariantCulture)); PlayerPrefs.Save();
        Place(g, pick);
        g.ui.Banner("X marks the spot", "A red X is out in " + landName + ". It is on your map");
        AHSound.Play("level");
    }

    // marks on the area map
    public static void MapMarks(AHPlayer p, Action<Vector3, string> mark)
    {
        var L = Here; Vector2 w; if (L != null && HasDig(L.area, out w) && AHGame.I != null) mark(AHGame.I.W(w.x, w.y), "Treasure X");
    }

    // a heap of turned earth, two red planks crossed, a spade and a pale light rising so you can find it from afar
    static void Place(AHGame g, Vector2 w)
    {
        if (marker != null) UnityEngine.Object.Destroy(marker);
        Vector3 at = g.W(w.x, w.y);
        marker = new GameObject("Treasure X"); marker.transform.position = at;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        Func<Color, float, Material> M = (c, glow) => { var m = new Material(sh); m.SetColor("_BaseColor", c); if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); } return m; };
        Action<PrimitiveType, Vector3, Vector3, Vector3, Material> P = (pt, pos, size, rot, m) =>
        {
            var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); UnityEngine.Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(marker.transform, false); o.transform.localPosition = pos; o.transform.localRotation = Quaternion.Euler(rot); o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = m;
        };
        Material earth = M(new Color(0.36f, 0.27f, 0.18f), 0f), red = M(new Color(0.75f, 0.12f, 0.08f), 0.4f), wood = M(new Color(0.45f, 0.3f, 0.18f), 0f), iron = M(new Color(0.5f, 0.5f, 0.52f), 0f), beam = M(new Color(1f, 0.85f, 0.45f), 2.5f);
        P(PrimitiveType.Sphere, new Vector3(0, 0.05f, 0), new Vector3(2.2f, 0.35f, 2.2f), Vector3.zero, earth);
        P(PrimitiveType.Cube, new Vector3(0, 0.24f, 0), new Vector3(2.0f, 0.08f, 0.3f), new Vector3(0, 45, 0), red);
        P(PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(2.0f, 0.08f, 0.3f), new Vector3(0, -45, 0), red);
        P(PrimitiveType.Cube, new Vector3(1.1f, 0.7f, 0.4f), new Vector3(0.07f, 1.2f, 0.07f), new Vector3(0, 0, -12), wood);
        P(PrimitiveType.Cube, new Vector3(1.2f, 0.12f, 0.4f), new Vector3(0.3f, 0.36f, 0.04f), new Vector3(0, 0, -12), iron);
        P(PrimitiveType.Cylinder, new Vector3(0, 6f, 0), new Vector3(0.12f, 6f, 0.12f), Vector3.zero, beam);
        var lg = new GameObject("Glow"); lg.transform.SetParent(marker.transform, false); lg.transform.localPosition = Vector3.up * 1.5f;
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.8f, 0.4f); li.range = 8f; li.intensity = 2f; li.shadows = LightShadows.None;
        if (spot != null) AHGather.Spots.Remove(spot);
        spot = new AHSpot { kind = "use", name = "Dig for treasure", pos = at, r = 0.9f, reach = 2.8f, use = () => Dig(g) };
        AHGather.Spots.Add(spot);
    }

    struct Loot { public string id; public int min, max; public float w; public Loot(string i, int a, int b, float wt) { id = i; min = a; max = b; w = wt; } }
    // what a land's dig can hold: ore and gems climb with the land's level; the Fossil Lands add bones
    static List<Loot> Pool(Land L)
    {
        var l = new List<Loot>();
        if (L.lv < 10) { l.Add(new Loot("copper_ore", 3, 6, 8)); l.Add(new Loot("tin_ore", 3, 6, 8)); l.Add(new Loot("iron_ore", 2, 4, 5)); l.Add(new Loot("hp_potion", 2, 4, 7)); l.Add(new Loot("sapphire", 1, 1, 2)); }
        else if (L.lv < 30) { l.Add(new Loot("iron_ore", 3, 6, 8)); l.Add(new Loot("gold_ore", 2, 4, 6)); l.Add(new Loot("sapphire", 1, 2, 4)); l.Add(new Loot("emerald", 1, 2, 4)); l.Add(new Loot("big_potion", 2, 3, 6)); l.Add(new Loot("pearl", 1, 2, 3)); }
        else { l.Add(new Loot("gold_ore", 3, 5, 6)); l.Add(new Loot("obsidian_shard", 2, 4, 6)); l.Add(new Loot("ruby", 1, 2, 5)); l.Add(new Loot("emerald", 1, 2, 4)); l.Add(new Loot("diamond", 1, 1, 2)); l.Add(new Loot("big_potion", 2, 3, 6)); l.Add(new Loot("elixir_might", 1, 2, 4)); l.Add(new Loot("elixir_iron", 1, 2, 4)); }
        l.Add(new Loot(L.herb, 3, 6, 7)); l.Add(new Loot("elixir_swift", 1, 2, 2)); l.Add(new Loot(L.map, 1, 1, 3));
        if (L.area == "fossil") { l.Add(new Loot("bone_dust", 3, 6, 10)); l.Add(new Loot("bones", 3, 7, 10)); l.Add(new Loot("bone_meal", 2, 4, 6)); l.Add(new Loot("fossil_horn", 1, 1, 0.8f)); l.Add(new Loot("card_tricebone", 1, 1, 0.6f)); }
        // a card of one of the land's own beasts
        var g = AHGame.I;
        if (g != null && g.data != null && g.data.mobTypes != null)
        {
            var cards = new List<string>(); foreach (var t in g.data.mobTypes) if (t != null && AHItems.Get("card_" + t.id) != null && !cards.Contains("card_" + t.id)) cards.Add("card_" + t.id);
            if (cards.Count > 0) l.Add(new Loot(cards[rnd.Next(cards.Count)], 1, 1, 3));
        }
        return l;
    }

    static void Dig(AHGame g)
    {
        var p = g.player; var L = Here; if (L == null) return;
        if (p.bag.Count(L.map) <= 0) { g.ui.Toast("You need this land's treasure map to dig here.", 2f); return; }
        if (p.bag.UsedSlots > p.bag.SlotsMax - 4) { g.ui.Toast("Make room in your bag first: a treasure needs 4 free slots.", 2.5f); return; }
        p.bag.Take(L.map);
        PlayerPrefs.DeleteKey(Key(L.area)); PlayerPrefs.Save();
        Vector3 at = spot != null ? spot.pos : p.transform.position;
        if (spot != null) AHGather.Spots.Remove(spot); spot = null;
        if (marker != null) UnityEngine.Object.Destroy(marker); marker = null;
        // silver for the land's level, then three finds
        long copper = (600 + L.lv * 120) + rnd.Next(1500 + L.lv * 150);
        p.bag.money += copper * AHDB.CU;
        var got = new List<string> { AHItems.MoneyText(copper * AHDB.CU) };
        var pool = Pool(L); pool.RemoveAll(x => AHItems.Get(x.id) == null);
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            float tot = 0f; foreach (var x in pool) tot += x.w;
            float r = (float)rnd.NextDouble() * tot; Loot pick = pool[0];
            foreach (var x in pool) { r -= x.w; if (r <= 0f) { pick = x; break; } }
            pool.RemoveAll(x => x.id == pick.id);
            int n = pick.min + rnd.Next(pick.max - pick.min + 1);
            if (p.bag.Add(pick.id, n)) got.Add(n + " × " + AHItems.Get(pick.id).name);
        }
        string reins = "reins_" + L.mount;
        if (rnd.NextDouble() < 0.01 && AHItems.Get(reins) != null)
        {
            p.bag.Add(reins);   // four free slots were kept for this
            got.Add(AHItems.Get(reins).name + "!");
            g.ui.Banner(AHComp.MountName(L.mount) + "!", "A one-in-a-hundred find. Use the reins in your bag");
            AHSpark.Ring(at + Vector3.up * 0.3f, new Color(1f, 0.9f, 0.6f), 50, 7f);
        }
        else g.ui.Banner("Treasure!", string.Join(", ", got.ToArray()));
        AHSpark.Burst(at + Vector3.up * 0.6f, new Color(1f, 0.82f, 0.35f, 0.9f), 50, 4f, 1f, 0.2f, 1.4f);
        AHSound.Play("level");
        PlayerPrefs.SetInt("ah_digs", PlayerPrefs.GetInt("ah_digs", 0) + 1);
        if (L.area == "fossil") PlayerPrefs.SetInt("ah_fossil_dug", PlayerPrefs.GetInt("ah_fossil_dug", 0) + 1);
        PlayerPrefs.Save();
        p.bag.Touch(); g.MarkDirty();
        Debug.Log("Ashen Hollow: treasure dug in " + L.area + ": " + string.Join(", ", got.ToArray()));
    }

    // a land's beasts carry its old maps
    public static void OnKill(AHGame g, AHMob m, AHPlayer by)
    {
        var L = Here; if (L == null || by == null || m == null || m.type == null) return;
        var d = AHJson.O(AHDB.Mobs, m.type.id);
        bool boss = AHJson.B(d, "boss") || AHJson.B(d, "elite") || AHJson.B(d, "world") || AHJson.Has(d, "boss2");
        bool rare = AHJson.B(d, "rare"); if (!rare && AHEvents.Rares != null) foreach (var r in AHEvents.Rares) if (AHJson.S(r, "type") == m.type.id) rare = true;
        double chance;
        if (L.area == "fossil")
        {
            // the Fossil Lands keep their maps on their strongest: bosses, rare beasts and the high-level dead (level 50+)
            chance = boss ? 0.35 : rare ? 0.2 : m.type.lvl >= FossilMapLevel ? 0.05 : 0.0;
        }
        else chance = boss ? 0.3 : rare ? 0.15 : 0.02;
        if (chance > 0 && rnd.NextDouble() < chance)
        {
            if (by.bag.Add(L.map)) { g.MarkDirty(); g.ui.Toast("A treasure map! It is in your bag.", 2.2f); }
            else g.ui.Toast("A treasure map blew away: your bag is full.", 2.2f);
        }
    }
    public const int FossilMapLevel = 50;
}
