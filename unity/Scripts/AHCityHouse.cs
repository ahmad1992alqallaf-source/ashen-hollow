// Ashen Hollow: a city house to rent, as in the web game (v66, "Living cities part 3": cbRentPrice / cbMine /
// renderRent / cbRentAct / cbHomeRespawn).
// Every city has a house to let near its square (green sign on the door). Rent it by the week for one and a half
// times the city's market stall rent: a soft bed that fills your rested XP and restores you (once an hour), your bank
// chest by the door, and after a fall in that city you wake up at home instead of at the city gate. Renting in another
// city hands the old key back; rent already paid is never returned.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHCityHouse
{
    const long Week = 7L * 86400000L, Hour = 3600000L;
    static long Now { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
    static string city; static Vector3 door; static bool built;
    public static string City { get { return city; } }
    public static Vector3? Door { get { return built ? door : (Vector3?)null; } }

    public static long Price(string c) { int i = Array.IndexOf(AHMarket.Cities, c); int[] silver = { 2, 5, 3, 3, 3, 4, 4 }; return (long)Math.Round((i >= 0 ? silver[i] : 3) * 1.5 * 1000000); }
    public static bool Mine(AHPlayer p, string c) { return p.prog.chCity == c && Now < p.prog.chUntil; }
    public static bool Rents(AHPlayer p) { return p.prog.chCity != null && Now < p.prog.chUntil; }

    public static void Setup(AHGame g)
    {
        built = false; city = null;
        string c; if (!AHNight.CityOfArea.TryGetValue(AHGame.AreaId, out c)) return;
        // the city's own "house for rent" lot (rebuilt by AHCity as a proper house): put the door and a sign there
        Vector3 lotDoor, lotCentre;
        if (AHCity.RentLot(g, out lotDoor, out lotCentre))
        {
            city = c; built = true; door = lotDoor;
            Sign(g, lotDoor, lotCentre);
            AHGather.Spots.Add(new AHSpot { kind = "use", name = "House for rent", pos = door, r = 0.6f, reach = 2.6f, use = () => g.ui.OpenHouse() });
            return;
        }
        object w = null; if (AHWays.Ways != null) foreach (var q in AHWays.Ways) if (AHJson.S(q, "area") == AHGame.AreaId && (AHJson.S(q, "id") == c || w == null)) w = q;
        if (w == null) return;
        float S = AHDB.S; Vector3 ctr = g.W((float)AHJson.N(w, "x") * S, (float)AHJson.N(w, "y") * S);
        var r = new System.Random(c.GetHashCode() ^ 0x5eed);
        var avoid = AHNight.StallPos();
        Vector3? spot = null;
        // the roomiest free spot near the square: try with a wide clearance first, then narrower
        foreach (float clear in new[] { 3.2f, 2.6f, 2.0f, 1.5f })
            for (int t = 0; t < 400 && !spot.HasValue; t++)
            {
                double a = r.NextDouble() * Math.PI * 2, d = (200 + r.NextDouble() * 420) * S;
                Vector3 p = ctr + new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a)) * (float)d;
                if (!g.InArea(p) || g.Blocked(p, clear)) continue;
                bool bad = false; foreach (var s2 in AHGather.Spots) if ((s2.pos - p).magnitude < 5.5f) bad = true; foreach (var v in avoid) if ((v - p).magnitude < 5.5f) bad = true;
                if (!bad) spot = p;
            }
        if (!spot.HasValue) Debug.Log("Ashen Hollow: no room for the city house in " + c);
        if (!spot.HasValue) return;
        city = c; built = true;
        BuildHouse(g, g.Resolve(spot.Value, 3f), ctr);
    }

    // a green hanging sign and a lantern by the door of the house to let
    static void Sign(AHGame g, Vector3 at, Vector3 centre)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        Vector3 dir = at - centre; dir.y = 0; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
        var root = new GameObject("RentSign").transform; root.position = at + dir.normalized * 0.2f + Vector3.Cross(Vector3.up, dir.normalized) * 1.3f; root.rotation = Quaternion.LookRotation(dir.normalized);
        Func<Color, float, Material> M = (c, glow) => { var m = new Material(sh); m.SetColor("_BaseColor", c); if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); } return m; };
        Action<Vector3, Vector3, Material> box = (p, s, m) => { var o = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube); UnityEngine.Object.Destroy(o.GetComponent<Collider>()); o.transform.SetParent(root, false); o.transform.localPosition = p; o.transform.localScale = s; o.GetComponent<Renderer>().sharedMaterial = m; };
        var wood = M(new Color(0.24f, 0.16f, 0.1f), 0f);
        box(new Vector3(0, 1.1f, 0), new Vector3(0.12f, 2.2f, 0.12f), wood);
        box(new Vector3(0.35f, 2.1f, 0), new Vector3(0.8f, 0.08f, 0.08f), wood);
        box(new Vector3(0.45f, 1.75f, 0), new Vector3(0.7f, 0.45f, 0.05f), M(new Color(0.2f, 0.6f, 0.26f), 0.4f));
        box(new Vector3(0.45f, 1.75f, 0.03f), new Vector3(0.5f, 0.08f, 0.02f), M(new Color(1f, 0.9f, 0.6f), 0.6f));
        var lg = new GameObject("Lamp"); lg.transform.SetParent(root, false); lg.transform.localPosition = new Vector3(0.45f, 2.2f, 0.3f);
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(0.6f, 1f, 0.6f); li.range = 4f; li.intensity = 0.8f; li.shadows = LightShadows.None;
        AHModel.SetShadows(root.gameObject);
    }

    static void BuildHouse(AHGame g, Vector3 at, Vector3 faceTo)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var root = new GameObject("CityHouse").transform; root.position = at; root.rotation = g.Face(faceTo - at);
        Func<int, float, Material> M = (hex, glow) => { var m = new Material(sh); m.SetColor("_BaseColor", AHGame.Hex(hex).linear); if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", AHGame.Hex(hex).linear * glow); } return m; };
        Action<PrimitiveType, Vector3, Vector3, Vector3, Material, bool> P = (pt, pos, size, rot, m, col) =>
        {
            var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); if (!col) UnityEngine.Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(root, false); o.transform.localPosition = pos; o.transform.localRotation = Quaternion.Euler(rot); o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = m;
        };
        Material wall = M(0xd8c8a8, 0), beam = M(0x5a3f28, 0), roof = M(0x8a3a2a, 0), doorM = M(0x6b4a2c, 0), sign = M(0x5ad06a, 1.2f), glass = M(0xffd890, 1.5f), stone = M(0x8a8478, 0);
        P(PrimitiveType.Cube, new Vector3(0, 0.15f, 0), new Vector3(4.6f, 0.3f, 4.2f), Vector3.zero, stone, false);
        P(PrimitiveType.Cube, new Vector3(0, 1.6f, 0), new Vector3(4.2f, 2.8f, 3.8f), Vector3.zero, wall, true);
        foreach (var sx in new[] { -1, 1 }) foreach (var sz in new[] { -1, 1 }) P(PrimitiveType.Cube, new Vector3(sx * 2.08f, 1.6f, sz * 1.88f), new Vector3(0.18f, 2.85f, 0.18f), Vector3.zero, beam, false);
        P(PrimitiveType.Cube, new Vector3(0, 3.0f, 1.9f), new Vector3(4.25f, 0.16f, 0.12f), Vector3.zero, beam, false);
        foreach (var sx in new[] { -1, 1 }) P(PrimitiveType.Cube, new Vector3(sx * 1.25f, 4.0f, 0), new Vector3(2.9f, 0.16f, 4.5f), new Vector3(0, 0, -sx * 40f), roof, false);
        P(PrimitiveType.Cube, new Vector3(1.3f, 4.6f, -0.6f), new Vector3(0.45f, 1.2f, 0.45f), Vector3.zero, stone, false);   // chimney
        P(PrimitiveType.Cube, new Vector3(0, 1.05f, 1.92f), new Vector3(1.0f, 1.8f, 0.08f), Vector3.zero, doorM, false);
        P(PrimitiveType.Sphere, new Vector3(0.32f, 1.0f, 1.98f), new Vector3(0.08f, 0.08f, 0.08f), Vector3.zero, M(0xd9ab3a, 0), false);
        foreach (var sx in new[] { -1, 1 }) { P(PrimitiveType.Cube, new Vector3(sx * 1.35f, 1.7f, 1.92f), new Vector3(0.7f, 0.6f, 0.06f), Vector3.zero, glass, false); P(PrimitiveType.Cube, new Vector3(sx * 1.35f, 1.7f, 1.95f), new Vector3(0.06f, 0.62f, 0.04f), Vector3.zero, beam, false); }
        P(PrimitiveType.Cube, new Vector3(0, 2.25f, 2.0f), new Vector3(1.1f, 0.34f, 0.06f), Vector3.zero, sign, false);
        var lg = new GameObject("Lamp"); lg.transform.SetParent(root, false); lg.transform.localPosition = new Vector3(0, 2.3f, 2.6f);
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.8f, 0.5f); li.range = 6f; li.intensity = 1.2f; li.shadows = LightShadows.None;
        AHModel.SetShadows(root.gameObject);
        door = root.TransformPoint(new Vector3(0, 0, 2.6f));
        AHGather.Spots.Add(new AHSpot { kind = "use", name = "House for rent", pos = door, r = 0.6f, reach = 2.6f, use = () => g.ui.OpenHouse() });
    }

    // web cbHomeRespawn: after a fall in the city where you rent, you wake at home
    public static Vector3? RespawnAt(AHPlayer p) { return built && city != null && Mine(p, city) ? door : (Vector3?)null; }

    public static string Rent(AHGame g)
    {
        var p = g.player; long pr = Price(city); if (p.bag.money < pr) return "Not enough money for the rent.";
        p.bag.money -= pr; p.bag.Touch();
        long from = Mine(p, city) ? p.prog.chUntil : Now; long slept = Mine(p, city) ? p.prog.chSlept : 0;
        p.prog.chId = "house_" + city; p.prog.chCity = city; p.prog.chUntil = from + Week; p.prog.chSlept = slept;
        g.ui.Banner("Your city house", AHMarket.CityName(city) + " · paid for 7 more days"); AHSound.Play("coin"); g.SaveProgress(); return null;
    }
    public static string Sleep(AHGame g)
    {
        var p = g.player; if (!Mine(p, city) || Now < p.prog.chSlept + Hour) return null;
        p.prog.chSlept = Now; p.hp = p.maxHp; p.hunger = 100f; p.rested = Math.Min(AHHome.RestCap(p), p.rested + AHHome.RestCap(p) / 2);
        AHSound.Play("heal"); g.SaveProgress(); return "You sleep in your own bed. Rested and restored.";
    }
    public static void MoveOut(AHGame g) { var p = g.player; p.prog.chId = null; p.prog.chCity = null; p.prog.chUntil = 0; g.SaveProgress(); }
}

public partial class AHUI
{
    public void OpenHouse() { wkMode = "house"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderHouse(AHPlayer p)
    {
        var rows = new List<Action<int>>(); string c = AHCityHouse.City; if (c == null) { ShowWork(false); return; }
        string town = AHMarket.CityName(c); bool mine = AHCityHouse.Mine(p, c); long pr = AHCityHouse.Price(c); long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        wkTitle.text = mine ? "Your city house" : "House for rent";
        wkHint.text = "A furnished house of your own in " + town + ": a soft bed that fills your rested XP, your bank chest by the door, and after a fall nearby you wake up here instead of at the gate.";
        if (mine)
        {
            long left = p.prog.chUntil - now; bool slept = now < p.prog.chSlept + 3600000L;
            rows.Add(s => Row(s, "Rent paid for " + (left / 86400000L) + " days " + (left % 86400000L / 3600000L) + " hours", new Color(0.35f, 0.82f, 0.42f), "Housekeeper Mott keeps the place tidy.", ""));
            rows.Add(s => Row(s, "Sleep", Color.white, slept ? "You slept here less than an hour ago." : "Full health and hunger, and a good night’s rested XP", "",
                new WkBtn { label = "Sleep", on = !slept, col = Go, act = () => { var m = AHCityHouse.Sleep(g); if (m != null) { Toast(m); Fade(true); Invoke("FadeOff", 0.7f); } RenderWork(); } }));
            rows.Add(s => Row(s, "Bank chest", Color.white, "Your bank, from the comfort of home", "", new WkBtn { label = "Open", on = true, col = Plain, act = OpenBank }));
            rows.Add(s => Row(s, "Pay another week", Color.white, AHItems.MoneyText(pr), "", new WkBtn { label = "Pay", on = p.bag.money >= pr, col = Go, act = () => { var m = AHCityHouse.Rent(g); if (m != null) Toast(m); RenderWork(); } }));
            rows.Add(s => Row(s, "Move out", Color.white, "Hand back the key. Rent already paid is not returned.", "", new WkBtn { label = "Move out", on = true, col = Plain, act = () => { AHCityHouse.MoveOut(g); Toast("You hand back the key."); RenderWork(); } }));
        }
        else
        {
            rows.Add(s => Row(s, "Rent for a week", new Color(0.35f, 0.82f, 0.42f), AHItems.MoneyText(pr) + " · then renew from the house", "",
                new WkBtn { label = "Rent", on = p.bag.money >= pr, col = Go, act = () => { var m = AHCityHouse.Rent(g); if (m != null) Toast(m); RenderWork(); } }));
            if (AHCityHouse.Rents(p)) rows.Add(s => Row(s, "You rent a house in " + AHMarket.CityName(p.prog.chCity), new Color(1f, 0.8f, 0.45f), "Renting this one hands that key back.", ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
