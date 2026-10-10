// Ashen Hollow: waystones and hidden treasure, as in the web game (v66, WAYS / discovery / renderWays / TREASURES / openTreasure).
// Walk near a waystone to attune it; touch any waystone to travel to any attuned one (or home to your homestead).
// Hidden treasure chests lie around the world: each opens once, with coins and an item.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHWays
{
    public static List<object> Ways { get { return AHDB.List("world", "WAYS"); } }
    public static List<object> Treasures { get { return AHDB.List("world", "TREASURES"); } }
    static readonly List<KeyValuePair<object, Vector3>> here = new List<KeyValuePair<object, Vector3>>();

    public static void Setup(AHGame g)
    {
        here.Clear();
        float S = AHDB.S; var p = g.player;
        var ws = Ways;
        if (ws != null)
            foreach (var w in ws)
            {
                if (AHJson.S(w, "area") != AHGame.AreaId)
                {
                    // a neighbour's waystone that shows past the edge of this area: the same stone, just to look at
                    float wx = (float)AHJson.N(w, "x") * S, wz = (float)AHJson.N(w, "y") * S; var bd = g.data != null ? g.data.bounds : null;
                    if (bd != null && wx > bd.x0 - 90 && wx < bd.x1 + 90 && wz > bd.z0 - 90 && wz < bd.z1 + 90 && !(wx > bd.x0 && wx < bd.x1 && wz > bd.z0 && wz < bd.z1))
                        Stone(g, g.W(wx, wz), false, true);
                    continue;
                }
                Vector3 at = g.W((float)AHJson.N(w, "x") * S, (float)AHJson.N(w, "y") * S);
                here.Add(new KeyValuePair<object, Vector3>(w, at));
                var spot = new AHSpot { kind = "use", name = "Waystone", pos = at, r = 0.6f, reach = 3f };
                spot.use = () => { Attune(g, w); g.ui.OpenWays(); };
                AHGather.Spots.Add(spot);
                Stone(g, at, p.prog.found.Contains("way_" + AHJson.S(w, "id")));
            }
        var ts = Treasures;
        if (ts != null)
            foreach (var t in ts)
            {
                if (AHJson.S(t, "area") != AHGame.AreaId) continue;
                string id = AHJson.S(t, "id");
                Vector3 at = g.W((float)AHJson.N(t, "x") * S, (float)AHJson.N(t, "y") * S);
                bool open = p.prog.found.Contains(id);
                var lid = Chest(g, at, open);
                if (open) continue;
                AHSpot spot = null;
                spot = new AHSpot { kind = "use", name = "Hidden treasure", pos = at, r = 0.5f, reach = 2.4f };
                spot.use = () =>
                {
                    string item = AHJson.S(t, "item"); var it = AHItems.Get(item);
                    if (it != null && !it.cosmetic && p.bag.Count(item) <= 0 && p.bag.UsedSlots >= p.bag.SlotsMax) { g.ui.Toast("Make room in your bag first."); return; }
                    p.prog.found.Add(id); AHGather.Spots.Remove(spot);
                    if (lid != null) lid.localRotation = Quaternion.Euler(-70f, 0, 0);
                    if (it != null) p.bag.Add(item);
                    long c = (long)AHJson.N(t, "gold", 50) * AHDB.CU; p.AddMoney(c, at);
                    g.ui.Banner("Hidden treasure!", AHItems.MoneyText(c) + (it != null ? " and " + it.name : ""));
                    AHFx.Ring(at, 0.3f, 2f, new Color(1f, 0.85f, 0.4f), 1f);
                    g.SaveProgress();
                };
                AHGather.Spots.Add(spot);
            }
    }

    static void Attune(AHGame g, object w)
    {
        var p = g.player; string k = "way_" + AHJson.S(w, "id");
        if (p.prog.found.Contains(k)) return;
        p.prog.found.Add(k);
        g.ui.Banner(AHJson.S(w, "name"), "Waystone attuned");
        g.ui.Toast("Waystone attuned: " + AHJson.S(w, "name") + ". Touch any waystone to travel between them.", 4f);
        g.SaveProgress();
    }
    public static bool Known(AHPlayer p, object w) { return p.prog.found.Contains("way_" + AHJson.S(w, "id")); }
    public static int KnownCount(AHPlayer p) { int n = 0; var ws = Ways; if (ws != null) foreach (var w in ws) if (Known(p, w)) n++; return n; }

    // web discovery: walking near a waystone attunes it
    static float t;
    public static void Tick(AHGame g, float dt)
    {
        t -= dt; if (t > 0f) return; t = 0.5f;
        var p = g.player; if (p == null || p.dead) return;
        foreach (var kv in here)
            if (!Known(p, kv.Key) && (kv.Value - p.transform.position).magnitude < 190f * AHDB.S) Attune(g, kv.Key);
    }

    public static void Go(AHGame g, object w)
    {
        g.ui.Banner(AHJson.S(w, "name"), "Waystone");
        g.Travel(AHJson.S(w, "area"), (float)AHJson.N(w, "x") * AHDB.S, ((float)AHJson.N(w, "y") + 70) * AHDB.S, Mathf.PI / 2f);
    }

    static Material rock, rockDark, rune, wood, gold;
    static void Mats()
    {
        if (rock != null) return;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        rock = new Material(sh); rock.SetColor("_BaseColor", AHGame.Hex(0x7a7a84).linear);
        rockDark = new Material(sh); rockDark.SetColor("_BaseColor", AHGame.Hex(0x55555e).linear);
        // carved stone: the Dreamscape rocks' own textured stone when the pack is there, instead of flat grey
        var ds = AHDreamSet.Get(); GameObject rp = null; if (ds != null && ds.smallRocks != null) foreach (var sr in ds.smallRocks) if (sr != null && sr.name.Contains("StoneSmall")) { rp = sr; break; }
        var rr = rp != null ? rp.GetComponentInChildren<Renderer>() : null;
        if (rr != null && rr.sharedMaterial != null)
        {
            rock = new Material(rr.sharedMaterial); rock.SetColor("_BaseColor", new Color(0.92f, 0.92f, 0.95f));
            rockDark = new Material(rr.sharedMaterial); rockDark.SetColor("_BaseColor", new Color(0.62f, 0.62f, 0.68f));
        }
        rune = new Material(sh); rune.SetColor("_BaseColor", AHGame.Hex(0x7fd4ff).linear); rune.EnableKeyword("_EMISSION"); rune.SetColor("_EmissionColor", AHGame.Hex(0x7fd4ff).linear * 2.5f);
        wood = new Material(sh); wood.SetColor("_BaseColor", AHGame.Hex(0x5a3a22).linear);
        gold = new Material(sh); gold.SetColor("_BaseColor", AHGame.Hex(0xe0b040).linear); gold.SetFloat("_Metallic", 0.8f);
    }
    static Transform Part(Transform par, PrimitiveType pt, Vector3 pos, Vector3 size, Material m, Vector3 rot = default(Vector3))
    {
        var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); UnityEngine.Object.Destroy(o.GetComponent<Collider>());
        o.transform.SetParent(par, false); o.transform.localPosition = pos; o.transform.localScale = size; o.transform.localRotation = Quaternion.Euler(rot); o.GetComponent<Renderer>().sharedMaterial = m;
        return o.transform;
    }
    // a tapered four-sided pillar with a pyramid cap (bottom w x d, top shrunk by k, height h), flat shaded
    static Mesh pillar, gem;
    static Mesh Pillar()
    {
        if (pillar != null) return pillar;
        float w = 0.5f, d = 0.38f, k = 0.62f, h = 1f, cap = 0.16f;
        Vector3[] b = { new Vector3(-w, 0, -d), new Vector3(w, 0, -d), new Vector3(w, 0, d), new Vector3(-w, 0, d) };
        Vector3[] t = new Vector3[4]; for (int i = 0; i < 4; i++) t[i] = new Vector3(b[i].x * k, h, b[i].z * k);
        Vector3 apex = new Vector3(0, h + cap, 0);
        var v = new List<Vector3>(); var tri = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4, n = v.Count;
            v.AddRange(new[] { b[i], b[j], t[j], t[i] }); tri.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
            n = v.Count; v.AddRange(new[] { t[i], t[j], apex }); tri.AddRange(new[] { n, n + 2, n + 1 });
        }
        pillar = new Mesh { name = "Waystone pillar" }; pillar.SetVertices(v); pillar.SetTriangles(tri, 0); pillar.RecalculateNormals(); pillar.RecalculateBounds();
        return pillar;
    }
    static Mesh Gem()
    {
        if (gem != null) return gem;
        Vector3 up = Vector3.up * 0.6f, dn = Vector3.down * 0.6f; Vector3[] r = { new Vector3(0.35f, 0, 0), new Vector3(0, 0, 0.35f), new Vector3(-0.35f, 0, 0), new Vector3(0, 0, -0.35f) };
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4, n = v.Count; v.AddRange(new[] { r[i], r[j], up }); t.AddRange(new[] { n, n + 2, n + 1 });
            n = v.Count; v.AddRange(new[] { r[i], r[j], dn }); t.AddRange(new[] { n, n + 1, n + 2 });
        }
        gem = new Mesh { name = "Waystone gem" }; gem.SetVertices(v); gem.SetTriangles(t, 0); gem.RecalculateNormals(); gem.RecalculateBounds();
        return gem;
    }
    static Transform MeshPart(Transform par, Mesh mesh, Vector3 pos, Vector3 size, Material m, Vector3 rot = default(Vector3))
    {
        var o = new GameObject(mesh.name); o.transform.SetParent(par, false); o.transform.localPosition = pos; o.transform.localScale = size; o.transform.localRotation = Quaternion.Euler(rot);
        o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = m; return o.transform;
    }
    // a waystone: a stepped stone platform ringed with leaning standing stones, a tapered carved pillar with
    // glowing runes down its faces, and a crystal floating and turning above it (brighter once you are attuned)
    static void Stone(AHGame g, Vector3 at, bool known, bool far = false)
    {
        Mats();
        AHScenery.HideNear(g.World, at, 1.7f, 2.8f, 4.2f);   // the web game's plain slab stood on the same spot
        var root = new GameObject("Waystone").transform; root.position = g.Resolve(at, 0.6f);
        var rnd = new System.Random((int)(at.x * 13 + at.z * 7));
        Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0), new Vector3(2.6f, 0.08f, 2.6f), rockDark);
        Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.22f, 0), new Vector3(1.8f, 0.07f, 1.8f), rock);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2 / 6 + 0.3f, h = 0.7f + (float)rnd.NextDouble() * 0.5f;
            Vector3 p = new Vector3(Mathf.Cos(a) * 1.2f, h / 2f, Mathf.Sin(a) * 1.2f);
            Vector3 rot = new Vector3((float)rnd.NextDouble() * 10 - 5, -a * Mathf.Rad2Deg + 90f, (float)rnd.NextDouble() * 12 - 6);
            // a rough standing stone (a Dreamscape rock stood on end); a plain block only without the pack
            var sp = AHScenery.RockPrefab(i + (int)(at.x + at.z));
            if (sp != null && AHDreamSet.Get() != null)
            {
                var st = UnityEngine.Object.Instantiate(sp, root, false); foreach (var cl in st.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(cl);
                var rs = st.GetComponentsInChildren<Renderer>(); if (rs.Length > 0)
                {
                    st.transform.localRotation = Quaternion.identity; st.transform.localScale = Vector3.one;
                    Bounds sb = rs[0].bounds; foreach (var r in rs) sb.Encapsulate(r.bounds);
                    st.transform.localScale = new Vector3(0.38f / Mathf.Max(0.05f, sb.size.x), h / Mathf.Max(0.05f, sb.size.y), 0.3f / Mathf.Max(0.05f, sb.size.z));
                    st.transform.localPosition = new Vector3(p.x, 0f, p.z) - (sb.min.y - root.position.y) * st.transform.localScale.y * Vector3.up;
                    st.transform.localRotation = Quaternion.Euler(rot);
                    continue;
                }
                UnityEngine.Object.Destroy(st);
            }
            Part(root, PrimitiveType.Cube, p, new Vector3(0.28f, h, 0.2f), i % 2 == 0 ? rock : rockDark, rot);
        }
        MeshPart(root, Pillar(), new Vector3(0, 0.29f, 0), new Vector3(1f, 2.5f, 1f), rock, new Vector3(0, 0, 2));
        // runes down the front and back faces, a little proud of the stone
        for (int f = 0; f < 2; f++)
            for (int i = 0; i < 5; i++)
            {
                float y = 0.75f + i * 0.38f, depth = 0.38f * (1f - (y - 0.29f) / 2.5f * 0.38f) + 0.012f;
                var rr = Part(root, PrimitiveType.Cube, new Vector3(0, y, f == 0 ? -depth : depth), new Vector3(0.16f + 0.05f * (i % 2), 0.05f, 0.02f), rune, new Vector3(0, 0, i % 2 == 0 ? 0 : 90));
                if (i % 2 == 0) Part(root, PrimitiveType.Cube, rr.localPosition, new Vector3(0.05f, 0.18f, 0.02f), rune);
            }
        var c = MeshPart(root, Gem(), new Vector3(0, 3.35f, 0), Vector3.one * 0.5f, rune);
        c.gameObject.AddComponent<AHPortalSpin>();
        var lg = new GameObject("Glow"); lg.transform.SetParent(root, false); lg.transform.localPosition = Vector3.up * 2.6f;
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(0.5f, 0.83f, 1f); li.range = 7f; li.intensity = known ? 2.2f : 1f; li.shadows = LightShadows.None;
        if (far) li.enabled = false;
        AHModel.SetShadows(root.gameObject);
        if (!far) g.AddBlocker(root.position, 0.6f);
    }
    static Transform Chest(AHGame g, Vector3 at, bool open)
    {
        Mats();
        AHScenery.HideNear(g.World, at, 0.9f, 1.3f, 1.3f);   // and its box chest
        var root = new GameObject("Treasure").transform; root.position = g.Resolve(at, 0.5f);
        root.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
        // a KayKit treasure chest, iron-banded, its lid hinged at the back (KayKit Dungeon Remastered, CC0)
        var pf = Resources.Load<GameObject>("AH/Models/KK/kk_dg_chest");
        if (pf != null)
        {
            var ch = UnityEngine.Object.Instantiate(pf, root, false); ch.transform.localScale = Vector3.one * 0.62f;
            foreach (var cl in ch.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(cl);
            Transform kl = null; foreach (var t in ch.GetComponentsInChildren<Transform>(true)) if (t.name.Contains("lid")) kl = t;
            if (kl != null)
            {
                if (open) kl.localRotation = Quaternion.Euler(-70f, 0, 0);
                AHModel.SetShadows(root.gameObject);
                return kl;
            }
            UnityEngine.Object.Destroy(ch);
        }
        Part(root, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.6f), wood);
        Part(root, PrimitiveType.Cube, new Vector3(0, 0.26f, 0), new Vector3(0.1f, 0.52f, 0.64f), gold);
        var lid = new GameObject("Lid").transform; lid.SetParent(root, false); lid.localPosition = new Vector3(0, 0.5f, -0.3f);
        Part(lid, PrimitiveType.Cube, new Vector3(0, 0.1f, 0.3f), new Vector3(0.9f, 0.2f, 0.6f), wood);
        Part(lid, PrimitiveType.Cube, new Vector3(0, 0.2f, 0.3f), new Vector3(0.94f, 0.04f, 0.64f), gold);
        if (open) lid.localRotation = Quaternion.Euler(-70f, 0, 0);
        AHModel.SetShadows(root.gameObject);
        return lid;
    }
}

public partial class AHUI
{
    static readonly Dictionary<string, string> regionOf = new Dictionary<string, string>();
    static string RegionOf(string area)
    {
        string r; if (area == null) return "";
        if (regionOf.TryGetValue(area, out r)) return r;
        var dat = Resources.Load<TextAsset>("AH/Areas/" + area); r = "";
        if (dat != null) { var m = System.Text.RegularExpressions.Regex.Match(dat.text, "\"region\"\\s*:\\s*\"([^\"]*)\""); if (m.Success) r = m.Groups[1].Value; Resources.UnloadAsset(dat); }
        regionOf[area] = r; return r;
    }
    public void OpenWays() { wkMode = "ways"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderWays(AHPlayer p)
    {
        wkTitle.text = "Waystones · " + AHWays.KnownCount(p) + " / " + (AHWays.Ways != null ? AHWays.Ways.Count : 0);
        wkHint.text = "Touch any waystone to travel to one you have attuned. Walk near a waystone to attune it.";
        var rows = new List<Action<int>>();
        if (p.home != null)
            rows.Add(s => Row(s, "Your Homestead", new Color(0.6f, 0.9f, 0.5f), "Your own land", "", new WkBtn { label = "Travel", on = AHGame.AreaId != AHHome.Area, col = Go, act = () => { ShowWork(false); AHHome.TravelHome(g); } }));
        var ws = AHWays.Ways;
        if (ws != null)
            foreach (var w in ws)
            {
                var ww = w; bool k = AHWays.Known(p, w);
                string region = RegionOf(AHJson.S(w, "area"));
                rows.Add(s => Row(s, k ? AHJson.S(ww, "name") : "???", k ? new Color(0.5f, 0.83f, 1f) : new Color(0.5f, 0.5f, 0.55f), k ? region : "Not attuned yet", "",
                    k ? new WkBtn { label = "Travel", on = !p.dead, col = Go, act = () => { ShowWork(false); AHWays.Go(g, ww); } } : null));
            }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
