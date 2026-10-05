// Ashen Hollow: treasure maps of the Fossil Lands.
// The beasts of the Fossil Lands sometimes drop an old map (bosses often do), and the first time you set foot there an
// explorer's map blows to your feet. Read it in the Fossil Lands (tap it twice in your bag) and a red X appears
// somewhere out in the dust, marked on your map too. Walk to it and dig: silver, gems, bones and potions, beast cards,
// sometimes another map, and one dig in a hundred turns up the reins of a Fossilized raptor, a mount of old bones.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHTreasure
{
    public const string MapId = "fossil_map", MountId = "fossilraptor", ReinsId = "reins_fossilraptor";
    const string Key = "ah_fossil_dig", GiftKey = "ah_fossil_gift";
    static GameObject marker; static AHSpot spot;
    static readonly System.Random rnd = new System.Random();

    static bool HasDig(out Vector2 web)
    {
        web = Vector2.zero; string s = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(s)) return false;
        var a = s.Split(','); float x, z;
        if (a.Length != 2 || !float.TryParse(a[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) || !float.TryParse(a[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) return false;
        web = new Vector2(x, z); return true;
    }

    // on arriving in an area: the X if you have one here, and the explorer's map on your first visit
    public static void Setup(AHGame g)
    {
        marker = null; spot = null;
        if (AHGame.AreaId != "fossil" || g.player == null) return;
        Vector2 w; if (HasDig(out w)) Place(g, w);
        if (PlayerPrefs.GetInt(GiftKey, 0) == 0)
        {
            PlayerPrefs.SetInt(GiftKey, 1); PlayerPrefs.Save();
            g.player.bag.Add(MapId); g.MarkDirty();
            g.ui.Banner("An old map", "A treasure map blows to your feet. Read it in your bag");
        }
    }

    // tap the map twice in the bag
    public static void Read(AHGame g)
    {
        Vector2 w; bool has = HasDig(out w);
        if (AHGame.AreaId != "fossil") { g.ui.Toast(has ? "Your X lies out in the Fossil Lands." : "A map of the Fossil Lands. Read it there.", 2.2f); return; }
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
        PlayerPrefs.SetString(Key, pick.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + pick.y.ToString(System.Globalization.CultureInfo.InvariantCulture)); PlayerPrefs.Save();
        Place(g, pick);
        g.ui.Banner("X marks the spot", "A red X is out in the Fossil Lands. It is on your map");
        AHSound.Play("level");
    }

    // marks on the area map
    public static void MapMarks(AHPlayer p, Action<Vector3, string> mark)
    {
        Vector2 w; if (AHGame.AreaId == "fossil" && HasDig(out w) && AHGame.I != null) mark(AHGame.I.W(w.x, w.y), "Treasure X");
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
    static readonly Loot[] pool = {
        new Loot("bone_dust", 3, 6, 10), new Loot("bones", 3, 7, 10), new Loot("bone_meal", 2, 4, 6), new Loot("obsidian_shard", 2, 4, 6),
        new Loot("ruby", 1, 2, 5), new Loot("sapphire", 1, 2, 5), new Loot("emerald", 1, 2, 5), new Loot("diamond", 1, 1, 2),
        new Loot("big_potion", 2, 3, 6), new Loot("elixir_might", 1, 2, 4), new Loot("elixir_iron", 1, 2, 4), new Loot("elixir_swift", 1, 2, 3),
        new Loot("card_fskel", 1, 1, 2), new Loot("card_fhound", 1, 1, 2), new Loot("card_fsentinel", 1, 1, 1.5f), new Loot("card_tricebone", 1, 1, 0.6f),
        new Loot("fossil_horn", 1, 1, 0.8f), new Loot(MapId, 1, 1, 3) };

    static void Dig(AHGame g)
    {
        var p = g.player;
        if (p.bag.Count(MapId) <= 0) { g.ui.Toast("You need the treasure map to dig here.", 2f); return; }
        p.bag.Take(MapId);
        PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save();
        Vector3 at = spot != null ? spot.pos : p.transform.position;
        if (spot != null) AHGather.Spots.Remove(spot); spot = null;
        if (marker != null) UnityEngine.Object.Destroy(marker); marker = null;
        // silver, then three finds from the pool
        long copper = 1500 + rnd.Next(4500);
        p.bag.money += copper * AHDB.CU;
        var got = new List<string> { AHItems.MoneyText(copper * AHDB.CU) };
        float tot = 0f; foreach (var l in pool) if (AHItems.Get(l.id) != null) tot += l.w;
        var taken = new HashSet<string>();
        for (int i = 0; i < 3 && tot > 0f; i++)
        {
            float r = (float)rnd.NextDouble() * tot; Loot pick = pool[0];
            foreach (var l in pool) { if (AHItems.Get(l.id) == null) continue; r -= l.w; if (r <= 0f) { pick = l; break; } }
            if (taken.Contains(pick.id)) { i--; if (taken.Count >= 6) break; continue; }
            taken.Add(pick.id); int n = pick.min + rnd.Next(pick.max - pick.min + 1);
            p.bag.Add(pick.id, n); got.Add(n + " × " + AHItems.Get(pick.id).name);
        }
        bool mount = rnd.NextDouble() < 0.01;
        if (mount && AHItems.Get(ReinsId) != null)
        {
            p.bag.Add(ReinsId); got.Add("Reins: Fossilized raptor!");
            g.ui.Banner("Fossilized raptor!", "A one-in-a-hundred find. Use the reins in your bag");
            AHSpark.Ring(at + Vector3.up * 0.3f, new Color(1f, 0.9f, 0.6f), 50, 7f);
        }
        else g.ui.Banner("Treasure!", string.Join(", ", got.ToArray()));
        AHSpark.Burst(at + Vector3.up * 0.6f, new Color(1f, 0.82f, 0.35f, 0.9f), 50, 4f, 1f, 0.2f, 1.4f);
        AHSound.Play("level");
        PlayerPrefs.SetInt("ah_fossil_dug", PlayerPrefs.GetInt("ah_fossil_dug", 0) + 1); PlayerPrefs.Save();
        p.bag.Touch(); g.MarkDirty();
        Debug.Log("Ashen Hollow: treasure dug: " + string.Join(", ", got.ToArray()));
    }

    // the beasts of the Fossil Lands carry old maps
    public static void OnKill(AHGame g, AHMob m, AHPlayer by)
    {
        if (AHGame.AreaId != "fossil" || by == null || m == null || m.type == null) return;
        bool boss = AHJson.B(AHJson.O(AHDB.Mobs, m.type.id), "boss");
        if (rnd.NextDouble() < (boss ? 0.3 : 0.035))
        {
            by.bag.Add(MapId); g.MarkDirty();
            g.ui.Toast("A treasure map! It is in your bag.", 2.2f);
        }
    }
}
