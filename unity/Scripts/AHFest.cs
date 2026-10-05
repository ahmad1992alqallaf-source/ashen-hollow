// Ashen Hollow: seasonal festivals, as in the web game (v66, FESTS / festNow / festEvent / festClaim / festBuy / festAct /
// festEid / festDecor). The Harvest Fair (mid-September to mid-October), Lantern Nights during Ramadan (with an Eid gift
// for three days after) and the Winter Feast (mid-December to early January). Each decorates Ashen Hollow and Varrow,
// brings three daily tasks worth 5 tokens each, a town activity (carve pumpkins, light lanterns after dark, open your
// gift under the tree), and a festival stall selling cosmetics and a festival pet; earn 100 tokens for its title.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHFestState { public string id, day = ""; public int tok, earned, acts; public int[] prog = new int[3]; public bool[] claimed = new bool[3]; public string eid = ""; }

public static class AHFest
{
    public class Fest { public string id, name, blurb, act, title, icon; public Color col; public int actN; public bool night; public string[][] tasks; public object[][] shop; public bool eid; }
    static readonly string[][] Ramadan = { new[] { "2026-02-18", "2026-03-19" }, new[] { "2027-02-08", "2027-03-09" }, new[] { "2028-01-28", "2028-02-25" }, new[] { "2029-01-16", "2029-02-13" }, new[] { "2030-01-06", "2030-02-04" }, new[] { "2030-12-26", "2031-01-24" } };
    static string RamadanPhase(DateTime t)
    {
        foreach (var r in Ramadan)
        {
            var a = DateTime.Parse(r[0]); var b = DateTime.Parse(r[1]);
            if (t >= a && t < b.AddDays(1)) return "ramadan";
            if (t >= b.AddDays(1) && t < b.AddDays(4)) return "eid";
        }
        return null;
    }
    public static Fest Now()
    {
        var d = DateTime.Now; int m = d.Month, day = d.Day;
        if ((m == 9 && day >= 15) || (m == 10 && day <= 15))
            return new Fest { id = "harvest", name = "Harvest Fair", icon = "pumpkin", col = AHGame.Hex(0xff9a3a), blurb = "Pumpkins on every corner and a stall full of autumn finery.", act = "Carve a pumpkin", actN = 3,
                tasks = new[] { new[] { "gather", "20", "Gather 20 materials" }, new[] { "kill", "25", "Defeat 25 monsters" }, new[] { "fest", "3", "Carve 3 pumpkins in town" } },
                shop = new[] { new object[] { "harvest_hat", 30 }, new object[] { "harvest_cape", 40 }, new object[] { "pet:pumpkin_slime", 80 } }, title = "the Harvester" };
        string rp = RamadanPhase(d);
        if (rp != null)
            return new Fest { id = "lantern", name = "Lantern Nights", icon = "lantern", col = AHGame.Hex(0xffd27a), blurb = "Ramadan: lanterns glow in the squares after sunset, and neighbours share their tables.", act = "Light a lantern", actN = 5, night = true, eid = rp == "eid",
                tasks = new[] { new[] { "fest", "5", "Light 5 lanterns after dark" }, new[] { "gift", "2", "Share 2 gifts with neighbours" }, new[] { "talk", "6", "Visit 6 villagers" } },
                shop = new[] { new object[] { "lantern_hood", 30 }, new object[] { "crescent_cape", 40 }, new object[] { "pet:lantern_owl", 80 } }, title = "the Lantern-bearer" };
        if ((m == 12 && day >= 15) || (m == 1 && day <= 5))
            return new Fest { id = "winter", name = "Winter Feast", icon = "gift", col = AHGame.Hex(0x9fd4ff), blurb = "Snow-dusted pines, a gift tree in the square and warm stew for everyone.", act = "Open a gift", actN = 1,
                tasks = new[] { new[] { "kill", "25", "Defeat 25 monsters" }, new[] { "gather", "15", "Gather 15 materials" }, new[] { "fest", "1", "Open your gift under the tree" } },
                shop = new[] { new object[] { "winter_hat", 30 }, new object[] { "snow_cape", 40 }, new object[] { "pet:snow_fox", 80 } }, title = "the Merry" };
        return null;
    }
    static string Day { get { return DateTime.Now.ToString("yyyy-MM-dd"); } }
    public static AHFestState State(AHPlayer p, Fest F)
    {
        if (F == null) return null;
        var S = p.prog.fests.Find(x => x.id == F.id); if (S == null) { S = new AHFestState { id = F.id }; p.prog.fests.Add(S); }
        if (S.prog == null || S.prog.Length != 3) S.prog = new int[3]; if (S.claimed == null || S.claimed.Length != 3) S.claimed = new bool[3];
        if (S.day != Day) { S.day = Day; S.prog = new int[3]; S.claimed = new bool[3]; S.acts = 0; }
        return S;
    }

    // web festEvent (fed by every quest-log event)
    public static void Event(AHGame g, string t, int n)
    {
        var F = Now(); if (F == null || g.player == null) return; var S = State(g.player, F);
        for (int i = 0; i < 3; i++)
        {
            var tk = F.tasks[i]; int need = int.Parse(tk[1]);
            if (tk[0] == t && !S.claimed[i] && S.prog[i] < need) { S.prog[i] = Mathf.Min(need, S.prog[i] + n); if (S.prog[i] >= need) g.ui.Banner(F.name + ": task done", tk[2] + " · claim your tokens at the festival stall"); }
        }
    }
    public static void Claim(AHGame g, int i)
    {
        var F = Now(); if (F == null) return; var S = State(g.player, F);
        if (S.claimed[i] || S.prog[i] < int.Parse(F.tasks[i][1])) return;
        S.claimed[i] = true; S.tok += 5; S.earned += 5; g.ui.Toast("+5 " + F.name + " tokens."); g.SaveProgress();
    }
    public static void Buy(AHGame g, string k)
    {
        var F = Now(); if (F == null) return; var S = State(g.player, F); var p = g.player;
        object[] it = null; foreach (var o in F.shop) if ((string)o[0] == k) it = o; if (it == null || S.tok < (int)it[1]) return;
        if (k.StartsWith("pet:")) { string pk = k.Substring(4); if (p.pets.Contains(pk)) return; p.pets.Add(pk); g.ui.Banner(AHComp.PetName(pk), "A festival companion! Choose it in your pets"); }
        else { if (p.bag.Count(k) > 0 || p.bag.gear.ContainsValue(k)) return; if (!p.bag.Add(k)) { g.ui.Toast(p.bag.lastWarn ?? "Your bag is full."); return; } g.ui.Toast(AHItems.Get(k).name + " is in your bag. Wear it from the bag.", 3f); }
        S.tok -= (int)it[1]; g.SaveProgress();
    }
    // web festAct: pumpkins, lanterns and the gift tree
    public static void Act(AHGame g, Vector3 at)
    {
        var F = Now(); if (F == null) return; var S = State(g.player, F); var p = g.player;
        if (F.night && !g.IsNight) { g.ui.Toast("The lanterns are lit after sunset."); return; }
        if (S.acts >= F.actN) { g.ui.Toast("That’s enough for today. Come back tomorrow!"); return; }
        S.acts++; Event(g, "fest", 1);
        if (F.id == "winter") { long c = (long)Mathf.Round(p.level * 30) * AHDB.CU; p.AddMoney(c, at); string[] gs = { "ruby", "sapphire", "emerald" }; p.bag.Add(gs[UnityEngine.Random.Range(0, 3)]); g.ui.Banner("A gift for you!", AHItems.MoneyText(c) + " and a gem"); }
        else { g.ui.Float(at + Vector3.up * 2f, F.id == "harvest" ? "Carved!" : "Lit!", F.col); AHFx.Ring(at, 0.3f, 1.6f, F.col, 0.8f); }
        g.MarkDirty();
    }
    public static void Eid(AHGame g)
    {
        var F = Now(); if (F == null || !F.eid) return; var S = State(g.player, F); var p = g.player;
        if (S.eid == Day) return; S.eid = Day;
        long c = (long)Mathf.Round(p.level * 120) * AHDB.CU; p.AddMoney(c, p.transform.position); S.tok += 10; S.earned += 10; p.bag.Add("lucky_charm");
        g.ui.Banner("Eid Mubarak!", AHItems.MoneyText(c) + ", 10 tokens and a lucky charm"); g.SaveProgress();
    }

    // web festDecor: a stall and decorations near the waystone of Ashen Hollow and of Varrow
    public static void Setup(AHGame g)
    {
        var F = Now(); if (F == null) return;
        object w = null; var ws = AHWays.Ways;
        if (ws != null) foreach (var q in ws) { string id = AHJson.S(q, "id"); if ((id == "ashen" || id == "varrow") && AHJson.S(q, "area") == AHGame.AreaId) w = q; }
        if (w == null) return;
        float S = AHDB.S; Vector3 c = g.W((float)AHJson.N(w, "x") * S, (float)AHJson.N(w, "y") * S);
        var rnd = new System.Random(F.id.GetHashCode() ^ AHGame.AreaId.GetHashCode());
        var taken = new List<Vector3>();
        Func<float, float, Vector3?> spot = (r0, r1) =>
        {
            for (int k = 0; k < 120; k++)
            {
                double a = rnd.NextDouble() * Math.PI * 2, r = r0 + rnd.NextDouble() * (r1 - r0);
                Vector3 p = c + new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a)) * (float)r;
                if (!g.InArea(p) || g.Blocked(p, 1.2f)) continue;
                bool bad = false; foreach (var s2 in AHGather.Spots) if ((s2.pos - p).magnitude < 4f) bad = true;
                foreach (var tp in taken) if ((tp - p).magnitude < 4f) bad = true;
                if (bad) continue;
                taken.Add(p); return p;
            }
            return null;
        };
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        Func<int, bool, Material> M = (hex, glow) => { var m = new Material(sh); m.SetColor("_BaseColor", AHGame.Hex(hex).linear); if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", AHGame.Hex(hex).linear * 1.6f); } return m; };
        Action<Transform, PrimitiveType, Vector3, Vector3, Material> part = (par, pt, pos, size, m) => { var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); UnityEngine.Object.Destroy(o.GetComponent<Collider>()); o.transform.SetParent(par, false); o.transform.localPosition = pos; o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = m; };
        var stallAt = spot(6.4f, 12.8f) ?? spot(6.4f, 24f);
        if (stallAt.HasValue)
        {
            var root = new GameObject("FestStall").transform; root.position = g.Resolve(stallAt.Value, 1f); root.rotation = g.Face(c - root.position);
            // a KayKit market stall in the festival's colour (yellow for the Harvest Fair, blue for Winter, red for Lanterns)
            string sc = F.id == "harvest" ? "yellow" : F.id == "winter" ? "blue" : "red";
            var spf = Resources.Load<GameObject>("AH/Models/KK/kk_building_market_" + sc);
            if (spf != null) { var st = UnityEngine.Object.Instantiate(spf, root, false); foreach (var cl in st.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(cl); st.transform.localScale = Vector3.one * 2.0f; st.transform.localPosition = Vector3.zero; }
            else
            {
                Material wood = M(0x8a5a32, false), cloth = M(Hex(F.col), false);
                part(root, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(2.4f, 0.9f, 1f), wood);
                part(root, PrimitiveType.Cube, new Vector3(0, 2.45f, -0.1f), new Vector3(2.7f, 0.15f, 1.4f), cloth);
            }
            AHModel.SetShadows(root.gameObject); g.AddBlocker(root.position, 1.2f);
            AHGather.Spots.Add(new AHSpot { kind = "use", name = "Festival stall", pos = root.position, r = 0.8f, reach = 3f, use = () => g.ui.OpenFest() });
        }
        for (int i = 0; i < 6; i++)
        {
            var at = spot(5.6f, 16.8f); if (!at.HasValue) continue;
            var root = new GameObject("FestDeco").transform; root.position = g.Resolve(at.Value, 0.6f);
            if (F.id == "harvest")
            {
                // a great ribbed pumpkin with a curled stem and a leaf, a smaller one beside it, and a crate and a sack
                Pumpkin(root, new Vector3(0, 0, 0), 0.95f, M(0xf07a18, false), M(0x4a6a22, false), M(0x3f8a2a, false), rnd);
                Pumpkin(root, new Vector3(-0.75f, 0, 0.45f), 0.55f, M(0xe8a030, false), M(0x4a6a22, false), M(0x3f8a2a, false), rnd);
                Prop(root, "crate_A_big", new Vector3(0.95f, 0, 0.25f), 2.6f, (float)rnd.NextDouble() * 360f);
                Prop(root, "sack", new Vector3(0.55f, 0, -0.65f), 2.6f, (float)rnd.NextDouble() * 360f);
            }
            else if (F.id == "lantern") { part(root, PrimitiveType.Cube, new Vector3(0, 1.3f, 0), new Vector3(0.1f, 2.6f, 0.1f), M(0x3a2a1a, false)); part(root, PrimitiveType.Cube, new Vector3(0.3f, 2.55f, 0), new Vector3(0.7f, 0.08f, 0.08f), M(0x3a2a1a, false)); part(root, PrimitiveType.Cube, new Vector3(0.6f, 2.2f, 0), new Vector3(0.32f, 0.45f, 0.32f), M(0xffd27a, true)); var lg = new GameObject("L"); lg.transform.SetParent(root, false); lg.transform.localPosition = new Vector3(0.6f, 2.2f, 0); var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.75f, 0.35f); li.range = 6f; li.intensity = 1.4f; li.shadows = LightShadows.None; }
            else
            {
                // a little snowy pine with gifts under it
                var tpf = Resources.Load<GameObject>("AH/Models/KK/kk_ah_pine_" + rnd.Next(3));
                if (tpf != null) { var t = UnityEngine.Object.Instantiate(tpf, root, false); foreach (var cl in t.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(cl); t.transform.localScale = Vector3.one * 2.4f; t.transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0); }
                else part(root, PrimitiveType.Cube, new Vector3(0, 1.4f, 0), new Vector3(1.2f, 1.6f, 1.2f), M(0x2a6a3a, false));
                int[] wrap = { 0xc8202a, 0x2a6ac8, 0xe8c040 };
                for (int k = 0; k < 3; k++)
                {
                    float sz = 0.32f + 0.12f * k; Vector3 gp = new Vector3(Mathf.Cos(k * 2.1f) * 0.75f, sz / 2f, Mathf.Sin(k * 2.1f) * 0.75f);
                    part(root, PrimitiveType.Cube, gp, Vector3.one * sz, M(wrap[k], false));
                    part(root, PrimitiveType.Cube, gp, new Vector3(sz * 1.02f, sz * 1.02f, sz * 0.18f), M(0xf4f0e0, false));
                    part(root, PrimitiveType.Cube, gp, new Vector3(sz * 0.18f, sz * 1.02f, sz * 1.02f), M(0xf4f0e0, false));
                }
            }
            AHModel.SetShadows(root.gameObject); g.AddBlocker(root.position, 0.7f);
            Vector3 ap = root.position;
            AHGather.Spots.Add(new AHSpot { kind = "use", name = F.act, pos = ap, r = 0.6f, reach = 2.6f, use = () => Act(g, ap) });
        }
    }
    // ---------- the harvest pumpkin: a squat ribbed gourd with a dimpled top, a curled stem and a leaf ----------
    static Mesh pumpkinMesh;
    static Mesh PumpkinMesh()
    {
        if (pumpkinMesh != null) return pumpkinMesh;
        int seg = 48, rings = 20; var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= rings; i++)
        {
            float th = Mathf.PI * i / rings, sy = Mathf.Cos(th), sr = Mathf.Sin(th);
            for (int k = 0; k <= seg; k++)
            {
                float ph = 2f * Mathf.PI * k / seg;
                float rib = 1f - 0.11f * Mathf.Pow(Mathf.Abs(Mathf.Sin(ph * 5f)), 0.6f);   // ten deep grooves
                float y = sy * 0.62f * (1f - 0.22f * Mathf.Pow(sr, 8f) * 0f);
                if (Mathf.Abs(sy) > 0.75f) y *= 1f - (Mathf.Abs(sy) - 0.75f) * 1.1f;      // dimpled top and bottom
                v.Add(new Vector3(Mathf.Cos(ph) * sr * rib * 0.5f, 0.31f + y * 0.5f, Mathf.Sin(ph) * sr * rib * 0.5f));
            }
        }
        for (int i = 0; i < rings; i++) for (int k = 0; k < seg; k++) { int a = i * (seg + 1) + k, b = a + seg + 1; t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
        pumpkinMesh = new Mesh { name = "Pumpkin" }; pumpkinMesh.SetVertices(v); pumpkinMesh.SetTriangles(t, 0); pumpkinMesh.RecalculateNormals(); pumpkinMesh.RecalculateBounds();
        return pumpkinMesh;
    }
    static void Pumpkin(Transform root, Vector3 at, float size, Material skin, Material stem, Material leaf, System.Random rnd)
    {
        var p = new GameObject("Pumpkin"); p.transform.SetParent(root, false); p.transform.localPosition = at; p.transform.localScale = Vector3.one * size;
        p.transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
        p.AddComponent<MeshFilter>().sharedMesh = PumpkinMesh(); p.AddComponent<MeshRenderer>().sharedMaterial = skin;
        for (int i = 0; i < 3; i++)
        {
            var s = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cylinder), PrimitiveType.Cylinder); UnityEngine.Object.Destroy(s.GetComponent<Collider>()); s.transform.SetParent(p.transform, false);
            s.transform.localPosition = new Vector3(0.025f * i, 0.6f + 0.06f * i, 0); s.transform.localRotation = Quaternion.Euler(0, 0, -18f * i); s.transform.localScale = new Vector3(0.07f - 0.012f * i, 0.05f, 0.07f - 0.012f * i);
            s.GetComponent<Renderer>().sharedMaterial = stem;
        }
        var l = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Sphere), PrimitiveType.Sphere); UnityEngine.Object.Destroy(l.GetComponent<Collider>()); l.transform.SetParent(p.transform, false);
        l.transform.localPosition = new Vector3(-0.14f, 0.6f, 0.05f); l.transform.localRotation = Quaternion.Euler(10, 30, 25); l.transform.localScale = new Vector3(0.3f, 0.025f, 0.18f);
        l.GetComponent<Renderer>().sharedMaterial = leaf;
    }
    static void Prop(Transform root, string name, Vector3 at, float k, float yaw)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/kk_" + name); if (pf == null) return;
        var go = UnityEngine.Object.Instantiate(pf, root, false); foreach (var c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(c);
        go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * k;
    }

    static int Hex(Color c) { return ((int)(c.r * 255) << 16) | ((int)(c.g * 255) << 8) | (int)(c.b * 255); }
}

public partial class AHUI
{
    public void OpenFest() { wkMode = "fest"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderFest(AHPlayer p)
    {
        var F = AHFest.Now(); var rows = new List<Action<int>>();
        if (F == null)
        {
            wkTitle.text = "Festivals"; wkHint.text = "No festival right now. The Harvest Fair runs mid-September to mid-October, Lantern Nights during Ramadan, and the Winter Feast from mid-December.";
        }
        else
        {
            var S = AHFest.State(p, F);
            wkTitle.text = F.name + " · " + S.tok + " tokens";
            wkHint.text = F.blurb + " Earned this festival: " + S.earned + (S.earned >= 100 ? " · title “" + F.title + "” earned" : " · earn 100 for the title “" + F.title + "”") + ". Today: " + F.act.ToLowerInvariant() + " " + S.acts + "/" + F.actN + ".";
            if (F.eid) rows.Add(s => Row(s, "Eid gift", F.col, "Ramadan is over. Eid Mubarak! A gift waits for you each day of Eid.", "", new WkBtn { label = S.eid == DateTime.Now.ToString("yyyy-MM-dd") ? "Taken today" : "Open gift", on = S.eid != DateTime.Now.ToString("yyyy-MM-dd"), col = Go, act = () => { AHFest.Eid(g); RenderWork(); } }));
            for (int i = 0; i < 3; i++)
            {
                int ii = i; var tk = F.tasks[i]; int need = int.Parse(tk[1]);
                rows.Add(s => Row(s, tk[2], Color.white, S.claimed[ii] ? "Done: +5 tokens" : S.prog[ii] + " / " + need, "", S.claimed[ii] ? null : new WkBtn { label = "Claim 5", on = S.prog[ii] >= need, col = Go, act = () => { AHFest.Claim(g, ii); RenderWork(); } }));
            }
            foreach (var o in F.shop)
            {
                string k = (string)o[0]; int cost = (int)o[1]; bool pet = k.StartsWith("pet:");
                bool own = pet ? p.pets.Contains(k.Substring(4)) : (p.bag.Count(k) > 0 || p.bag.gear.ContainsValue(k));
                string nm = pet ? AHComp.PetName(k.Substring(4)) + " (pet)" : (AHItems.Get(k) != null ? AHItems.Get(k).name : k);
                rows.Add(s => Row(s, "Stall: " + nm, F.col, pet ? AHJson.S(AHJson.O(AHComp.Pets, k.Substring(4)), "blurb") : "Cosmetic · all classes", "",
                    own ? new WkBtn { label = "Owned", on = false, col = Plain } : new WkBtn { label = cost + " tokens", on = S.tok >= cost, col = Go, act = () => { AHFest.Buy(g, k); RenderWork(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
