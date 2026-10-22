// Ashen Hollow: more life in Hollow Meadow.
//  - Hide and seek: Little Pip plays by the fountain by day. Ask to play and Pip runs off to hide somewhere in town;
//    find Pip within three minutes (walk right up close) for a small prize. The hint says roughly where.
//  - The inn's kitchen orders: Innkeeper Rosa wants a dish (up to five a day). Bring it and she pays well; bring it
//    fresh (cooked in the last ten minutes) and she pays more.
//  - The seasons: pumpkins in autumn, lights in winter, flowers in spring, bunting in summer, round the square.
using System;
using System.Collections.Generic;
using UnityEngine;

public class AHTownMore : MonoBehaviour
{
    AHGame g;
    public static AHTownMore I;
    public void Init(AHGame game)
    {
        g = game; I = this;
        try { BuildPip(); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: hide and seek (" + e.Message + ")"); }
        try { BuildRosa(); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: the inn's orders (" + e.Message + ")"); }
        try { BuildSeason(); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: the season's decorations (" + e.Message + ")"); }
    }
    static Vector3 E { get { var v = AHGame.I.W(1f, 0f) - AHGame.I.W(0f, 0f); v.y = 0; return v.normalized; } }
    static Vector3 S { get { var v = AHGame.I.W(0f, 1f) - AHGame.I.W(0f, 0f); v.y = 0; return v.normalized; } }
    static Vector3 Ring(float deg, float r) { float a = deg * Mathf.Deg2Rad; return AHHollow.C + (E * Mathf.Cos(a) + S * Mathf.Sin(a)) * r; }

    // ===================================== hide and seek =====================================
    AHNpc pip; Vector3 pipHome; bool hiding; float hideUntil; string hideHint; int finds;
    void BuildPip()
    {
        pipHome = Ring(150f, 4.2f);
        pip = AHNpc.SpawnAt(g, "Little Pip", pipHome, AHHollow.C - pipHome, new[] { "Wanna play hide and seek?", "I'm the best hider in the whole Meadow!" });
        pip.label = AHLoc.T("Play", "العب");
        pip.onTalk = () =>
        {
            if (hiding) { Found(); return true; }
            if (g.IsNight) { g.ui.TalkLine(pip, AHLoc.T("Mum says no games after dark...", "أمي تقول لا ألعاب بعد الظلام...")); return true; }
            if (finds >= 3) { g.ui.TalkLine(pip, AHLoc.T("You always find me! Let's play again tomorrow.", "تجدني دائمًا! لنلعب مرة أخرى غدًا.")); return true; }
            Hide(); return true;
        };
        finds = AHPrefs.GetString("ah_pip_day", "") == DateTime.Now.ToString("yyyy-MM-dd") ? AHPrefs.GetInt("ah_pip_n", 0) : 0;
        pip.gameObject.SetActive(!g.IsNight);
    }
    void Hide()
    {
        // somewhere by a building of the town (outside, by a corner), never right next to you
        var spots = new List<KeyValuePair<Vector3, string>>();
        foreach (var h in AHHollow.Halls)
        {
            if (h == null) continue;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 at = h.At(side * (h.w * 0.5f + 0.9f), 0.6f);
                if ((at - g.player.transform.position).magnitude < 14f) continue;
                spots.Add(new KeyValuePair<Vector3, string>(at, h.name));
            }
        }
        if (spots.Count == 0) { g.ui.TalkLine(pip, AHLoc.T("Hmm, nowhere good to hide...", "همم، لا مكان جيد للاختباء...")); return; }
        var pick = spots[UnityEngine.Random.Range(0, spots.Count)];
        g.ui.TalkLine(pip, AHLoc.T("Count to ten! No peeking!", "عُدّ إلى عشرة! لا تختلس النظر!"));
        pip.transform.position = pick.Key; pip.MoveHome(pick.Key, AHHollow.C - pick.Key);
        hiding = true; hideUntil = Time.time + 180f; hideHint = pick.Value;
        g.ui.Banner(AHLoc.T("Hide and seek!", "الغميضة!"), AHLoc.T("Find Pip within 3 minutes · somewhere near the " + hideHint, "اعثر على بيب خلال ٣ دقائق · قرب " + hideHint));
    }
    void Found()
    {
        hiding = false; finds++;
        AHPrefs.SetString("ah_pip_day", DateTime.Now.ToString("yyyy-MM-dd")); AHPrefs.SetInt("ah_pip_n", finds);
        g.ui.TalkLine(pip, AHLoc.T("Aww, you found me! Here, my best marble!", "أوه، وجدتني! خذ أفضل كرة زجاجية عندي!"));
        var r = new AHDaily.Reward { name = AHLoc.T("Found Little Pip!", "وجدت بيب الصغير!"), money = 25 + g.player.level * 3, marks = 5 };
        if (finds == 3) r.items.Add(new KeyValuePair<string, int>("lucky_charm", 1));
        AHDaily.Grant(g, r);
        pip.MoveHome(pipHome, AHHollow.C - pipHome);
        AHSound.Play("coin");
    }
    void PipTick()
    {
        if (pip == null) return;
        bool day = !g.IsNight; if (pip.gameObject.activeSelf != (day || hiding)) pip.gameObject.SetActive(day || hiding);
        if (!hiding) return;
        if ((pip.transform.position - g.player.transform.position).magnitude < 2.2f) { Found(); return; }
        if (Time.time > hideUntil)
        {
            hiding = false; pip.transform.position = pipHome; pip.MoveHome(pipHome, AHHollow.C - pipHome);
            g.ui.Toast(AHLoc.T("Pip wins this time! (They were by the " + hideHint + ")", "بيب فاز هذه المرة! (كان قرب " + hideHint + ")"), 4f);
        }
    }

    // ===================================== the inn's orders =====================================
    AHNpc rosa; static readonly string[] Dishes = { "trout", "salmon", "meat", "stew", "pie" };
    string Want { get { var d = DateTime.Now.ToString("yyyy-MM-dd"); string w = AHPrefs.GetString("ah_rosa_want", ""); if (!w.StartsWith(d + "|")) { int n = AHPrefs.GetString("ah_rosa_day", "") == d ? AHPrefs.GetInt("ah_rosa_n", 0) : 0; w = d + "|" + Dishes[UnityEngine.Random.Range(0, Dishes.Length)]; AHPrefs.SetString("ah_rosa_want", w); } return w.Substring(d.Length + 1); } }
    int ServedToday { get { return AHPrefs.GetString("ah_rosa_day", "") == DateTime.Now.ToString("yyyy-MM-dd") ? AHPrefs.GetInt("ah_rosa_n", 0) : 0; } }
    void BuildRosa()
    {
        AHHollow.Hall inn = null; foreach (var h in AHHollow.Halls) if (h.id == "inn") inn = h;
        if (inn == null) return;
        Vector3 at = inn.At(-inn.w * 0.18f, inn.d * 0.62f);
        rosa = AHNpc.SpawnAt(g, "Innkeeper Rosa", at, -inn.b, new[] { "The kitchen's always busy. Fancy helping?" });
        rosa.label = AHLoc.T("Orders", "الطلبات");
        rosa.onTalk = () =>
        {
            var p = g.player;
            if (ServedToday >= 5) { g.ui.TalkLine(rosa, AHLoc.T("That's the dinner rush done. Thank you, love! Come back tomorrow.", "انتهى زحام العشاء. شكرًا يا عزيزي! عد غدًا.")); return true; }
            string want = Want; var d = AHItems.Get(want); if (d == null) return false;
            if (p.bag.Count(want) <= 0) { g.ui.TalkLine(rosa, AHLoc.T("I need a " + d.name.ToLowerInvariant() + " for a guest. Cook one fresh and I'll pay extra!", "أحتاج " + d.name + " لضيف. اطبخه طازجًا وسأدفع أكثر!")); return true; }
            bool fresh = AHFun3.AddedAgo(want) < 600f;
            long pay = Math.Max(15 + p.level * 6, (long)Math.Round(AHShops.PriceBuy(want) / (double)AHDB.CU * 1.3)) * (fresh ? 16 : 10) / 10;
            p.bag.Take(want); p.AddMoney(pay * AHDB.CU, p.transform.position);
            AHPrefs.SetString("ah_rosa_day", DateTime.Now.ToString("yyyy-MM-dd")); AHPrefs.SetInt("ah_rosa_n", ServedToday + 1);
            AHPrefs.SetString("ah_rosa_want", "");
            g.ui.TalkLine(rosa, fresh ? AHLoc.T("Still warm! The guest will love it. A little extra for you.", "ما زال دافئًا! سيحبه الضيف. زيادة صغيرة لك.") : AHLoc.T("Thank you! A bit cold, but it'll do.", "شكرًا! بارد قليلًا لكنه يفي بالغرض."));
            g.ui.Toast(AHLoc.T("Rosa paid ", "دفعت روزا ") + AHItems.MoneyText(pay * AHDB.CU) + (fresh ? AHLoc.T(" (fresh bonus!)", " (مكافأة الطزاجة!)") : ""), 3f);
            g.MarkDirty();
            return true;
        };
    }

    // ===================================== the seasons =====================================
    public static string SeasonNow { get { string f = AHPrefs.GetString("ah_season_test", ""); if (f != "") return f; int m = DateTime.Now.Month; return m == 12 || m <= 2 ? "winter" : m <= 5 ? "spring" : m <= 8 ? "summer" : "autumn"; } }
    readonly List<Renderer> glows = new List<Renderer>(); Transform seasonRoot;
    static Material Mat(Color c, float glow = 0f)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c);
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
        return m;
    }
    static GameObject P(PrimitiveType t, Transform par, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(t); Destroy(go.GetComponent<Collider>()); go.transform.SetParent(par, false); go.transform.position = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = m;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return go;
    }
    public void BuildSeason()
    {
        if (seasonRoot != null) Destroy(seasonRoot.gameObject); glows.Clear();
        seasonRoot = new GameObject("Season").transform; seasonRoot.SetParent(transform, false);
        string s = SeasonNow; float r = 7.2f;
        for (int i = 0; i < 12; i++)
        {
            float deg = i * 30f + 15f; Vector3 at = Ring(deg, r); at.y = AHHollow.C.y;
            if (s == "autumn")
            {
                // a pumpkin: a squashed orange ball with ribs, a stem; it glows from inside at night
                var pm = Mat(new Color(0.95f, 0.45f, 0.08f));
                var body = P(PrimitiveType.Sphere, seasonRoot, at + Vector3.up * 0.34f, new Vector3(0.9f, 0.66f, 0.9f), pm);
                for (int k = 0; k < 3; k++) { var rib = P(PrimitiveType.Sphere, seasonRoot, at + Vector3.up * 0.34f, new Vector3(0.96f, 0.64f, 0.5f), pm); rib.transform.rotation = Quaternion.Euler(0, k * 60f, 0); }
                P(PrimitiveType.Cylinder, seasonRoot, at + Vector3.up * 0.74f, new Vector3(0.09f, 0.1f, 0.09f), Mat(new Color(0.3f, 0.45f, 0.15f)));
                if (i % 2 == 0) { var lit = P(PrimitiveType.Sphere, seasonRoot, at + Vector3.up * 0.34f, new Vector3(0.82f, 0.58f, 0.82f), Mat(new Color(1f, 0.6f, 0.15f), 2.5f)); glows.Add(lit.GetComponent<Renderer>()); }
            }
            else if (s == "winter")
            {
                // a post with a string of coloured lights round it
                P(PrimitiveType.Cylinder, seasonRoot, at + Vector3.up * 1.1f, new Vector3(0.08f, 1.1f, 0.08f), Mat(new Color(0.35f, 0.24f, 0.15f)));
                Color[] cs = { new Color(1f, 0.3f, 0.3f), new Color(0.3f, 1f, 0.45f), new Color(0.4f, 0.6f, 1f), new Color(1f, 0.85f, 0.3f) };
                for (int k = 0; k < 8; k++) { float a = k * 45f * Mathf.Deg2Rad; var b = P(PrimitiveType.Sphere, seasonRoot, at + new Vector3(Mathf.Cos(a) * 0.14f, 0.5f + k * 0.2f, Mathf.Sin(a) * 0.14f), Vector3.one * 0.09f, Mat(cs[k % 4], 3f)); glows.Add(b.GetComponent<Renderer>()); }
                P(PrimitiveType.Sphere, seasonRoot, at + Vector3.up * 0.08f, new Vector3(0.9f, 0.16f, 0.9f), Mat(new Color(0.95f, 0.97f, 1f)));   // a little drift of snow
            }
            else if (s == "spring")
            {
                // a pot of flowers
                P(PrimitiveType.Cylinder, seasonRoot, at + Vector3.up * 0.2f, new Vector3(0.42f, 0.2f, 0.42f), Mat(new Color(0.65f, 0.35f, 0.22f)));
                Color[] fl = { new Color(1f, 0.55f, 0.75f), new Color(1f, 0.9f, 0.35f), new Color(0.7f, 0.55f, 1f), new Color(1f, 1f, 1f) };
                for (int k = 0; k < 5; k++) { float a = k * 72f * Mathf.Deg2Rad; P(PrimitiveType.Sphere, seasonRoot, at + new Vector3(Mathf.Cos(a) * 0.12f, 0.5f + (k % 2) * 0.06f, Mathf.Sin(a) * 0.12f), Vector3.one * 0.15f, Mat(fl[(i + k) % 4])); }
                P(PrimitiveType.Sphere, seasonRoot, at + Vector3.up * 0.42f, new Vector3(0.38f, 0.14f, 0.38f), Mat(new Color(0.3f, 0.6f, 0.25f)));
            }
            else
            {
                // summer: a pole with a bright pennant
                P(PrimitiveType.Cylinder, seasonRoot, at + Vector3.up * 1.3f, new Vector3(0.06f, 1.3f, 0.06f), Mat(new Color(0.85f, 0.85f, 0.8f)));
                Color[] cs = { new Color(0.95f, 0.3f, 0.25f), new Color(0.25f, 0.6f, 0.95f), new Color(0.98f, 0.82f, 0.25f), new Color(0.35f, 0.8f, 0.4f) };
                var fl = P(PrimitiveType.Cube, seasonRoot, at + Vector3.up * 2.35f + E * 0.3f, new Vector3(0.6f, 0.36f, 0.02f), Mat(cs[i % 4]));
                fl.transform.rotation = Quaternion.LookRotation(S);
            }
        }
    }
    void SeasonTick()
    {
        bool n = g.IsNight;
        foreach (var r in glows) if (r != null && r.enabled != (n || SeasonNow == "winter")) r.enabled = n || SeasonNow == "winter";
    }

    float slowT;
    void Update()
    {
        if (g == null || g.player == null) return;
        try { PipTick(); } catch { }
        if ((slowT -= Time.deltaTime) > 0f) return; slowT = 2f;
        try { SeasonTick(); } catch { }
    }
    public static void Test(string what)
    {
        if (I == null) return;
        if (what == "pip") I.Hide();
        else if (what == "pipfind" && I.pip != null) { var pl = AHGame.I.player; pl.transform.position = I.pip.transform.position + (pl.transform.position - I.pip.transform.position).normalized * 3f; }
        else if (what.StartsWith("season:")) { AHPrefs.SetString("ah_season_test", what.Substring(7) == "now" ? "" : what.Substring(7)); I.BuildSeason(); }
        else if (what == "rosa" && I.rosa != null) { var pl = AHGame.I.player; pl.transform.position = I.rosa.transform.position + I.rosa.transform.forward * 2f; AHGame.I.TalkTo(I.rosa); }
    }
}
