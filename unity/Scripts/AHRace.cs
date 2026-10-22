// Ashen Hollow: the Meadow race. Race steward Fenna waits by the road outside a gate of Hollow Meadow. Talk to her and
// ride the course: golden rings stand along the road out (the next one bright, the rest dim); pass through each in
// turn to the last. The clock starts after a three-count; your best time is kept, and a blue wisp rides your best run
// beside you as a ghost to race against. Beat the par times for bronze, silver and gold (each a title, the first time);
// the first finish of the day pays a little coin. A mount helps (she saddles you up if you own one).
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AHRace : MonoBehaviour
{
    public static AHRace I;
    public bool Busy { get { return running || counting; } }
    AHGame g;
    readonly List<Vector3> cps = new List<Vector3>();
    readonly List<Transform> rings = new List<Transform>();
    Material bright, dim;
    AHNpc steward;
    float len, t, startAt; int next = -1; bool running, counting;
    readonly List<Vector2> rec = new List<Vector2>(); float recT;
    List<Vector2> ghost; Transform wisp;

    public static void Setup(AHGame g, AHTownLife host)
    {
        if (AHHollow.Gates.Count == 0) return;
        I = host.gameObject.AddComponent<AHRace>(); I.g = g;
        try { I.Build(); } catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: race course not built (" + e.Message + ")"); }
    }
    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    void Build()
    {
        // the course: out of the first gate and along the road as far as it goes
        var gt = AHHollow.Gates[0]; float y = AHHollow.C.y;
        Vector3 cur = gt.at + gt.dir * 16f; cur.y = y; Vector3 head = gt.dir;
        cps.Add(cur);
        for (int i = 0; i < 9; i++)
        {
            Vector3 best = Vector3.zero; float bestTurn = 999f;
            for (int pass = 0; pass < 2 && best == Vector3.zero; pass++)
                for (float a = 0f; a <= 80f; a += 8f)
                    foreach (var sd in new[] { 1f, -1f })
                    {
                        Vector3 q = cur + Quaternion.Euler(0, a * sd, 0) * head * 26f; q.y = y;
                        if (!g.InArea(q) || g.Blocked(q, 1.6f) || AHHollow.Inside(q, 6f)) continue;
                        if (pass == 0 && !AHNav.IsDirt(g, q)) continue;
                        bool back = false; foreach (var c in cps) if (Flat(c - q).magnitude < 18f) back = true; if (back) continue;
                        if (a < bestTurn) { bestTurn = a; best = q; }
                    }
            if (best == Vector3.zero) break;
            head = Flat(best - cur).normalized; cur = best; cps.Add(cur);
        }
        if (cps.Count < 4) { Debug.LogWarning("Ashen Hollow: race course too short"); cps.Clear(); return; }
        for (int i = 1; i < cps.Count; i++) len += Flat(cps[i] - cps[i - 1]).magnitude;
        len += 16f;
        bright = Glow(new Color(1f, 0.8f, 0.3f), 2.2f); dim = Glow(new Color(0.75f, 0.62f, 0.4f), 0.15f);
        var mesh = Torus(2.8f, 0.17f);
        for (int i = 0; i < cps.Count; i++)
        {
            Vector3 dir = i + 1 < cps.Count ? Flat(cps[i + 1] - cps[i]).normalized : Flat(cps[i] - cps[i - 1]).normalized;
            if (i > 0) dir = (dir + Flat(cps[i] - cps[i - 1]).normalized).normalized;
            var go = new GameObject("AH_Race ring " + i); go.transform.SetParent(transform, false);
            go.transform.position = cps[i] + Vector3.up * 2.9f; go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = dim; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // two posts holding it up
            foreach (var sd in new[] { -1f, 1f })
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(post.GetComponent<Collider>()); post.transform.SetParent(go.transform, false);
                post.transform.localPosition = new Vector3(sd * 2.95f, -1.45f, 0f); post.transform.localScale = new Vector3(0.18f, 2.9f, 0.18f);
                post.GetComponent<Renderer>().sharedMaterial = dim;
            }
            rings.Add(go.transform);
            if (i < cps.Count - 1) go.SetActive(false);   // only the finish shows while no race is on
        }
        // the steward beside the first ring
        Vector3 side = Vector3.Cross(Vector3.up, gt.dir);
        Vector3 at = Vector3.zero;
        foreach (var sd in new[] { 1f, -1f }) { Vector3 q = gt.at + gt.dir * 11f + side * sd * (gt.hw + 2.2f); q.y = y; if (!g.Blocked(q, 0.8f) && (AHHollow.Plot == null || !AHHollow.Plot.Contains(q, 2f))) { at = q; break; } }
        if (at == Vector3.zero) { at = gt.at + gt.dir * 11f + side * (gt.hw + 2.2f); at.y = y; }
        steward = AHNpc.SpawnAt(g, "Race steward Fenna", at, -side, new[] { "Fancy a race? Through every ring, as fast as you can." });
        steward.label = "Race"; steward.SetSeq("Idle_Loop", "Idle_Talking_Loop");
        steward.onTalk = () => { Offer(); return true; };
        // the finish ring shows from the start so you can see where the road goes
        rings[rings.Count - 1].gameObject.SetActive(true);
        LoadGhost();
    }
    static Material Glow(Color c, float k)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.7f); m.SetFloat("_Metallic", 0.6f);
        if (k > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k); }
        return m;
    }
    static Mesh Torus(float R, float r)
    {
        int n = 40, k = 10; var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= n; i++) for (int j = 0; j <= k; j++)
            {
                float a = i * Mathf.PI * 2f / n, b = j * Mathf.PI * 2f / k;
                v.Add(new Vector3((R + r * Mathf.Cos(b)) * Mathf.Cos(a), (R + r * Mathf.Cos(b)) * Mathf.Sin(a), r * Mathf.Sin(b)));
            }
        for (int i = 0; i < n; i++) for (int j = 0; j < k; j++)
            {
                int a0 = i * (k + 1) + j, a1 = a0 + k + 1;
                t.Add(a0); t.Add(a1); t.Add(a0 + 1); t.Add(a1); t.Add(a1 + 1); t.Add(a0 + 1);
            }
        var m = new Mesh { name = "Race ring" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    // par times for the medals, from the course's length (a horse at a gallop, and slower)
    float Par(int medal) { return len / (medal == 3 ? 9.2f : medal == 2 ? 7.6f : 6.0f); }
    static string Clock(float s) { int m = Mathf.FloorToInt(s / 60f); return m + ":" + (s - m * 60f).ToString("00.0"); }
    float Best { get { return AHPrefs.GetFloat("ah_race_best", 0f); } }

    void Offer()
    {
        if (cps.Count == 0) { g.ui.Toast("Race steward Fenna: “The course is being set up. Come back later.”", 3f); return; }
        if (running || counting) { g.ui.Toast("Race steward Fenna: “Go, go, go!”", 2f); return; }
        var p = g.player;
        if (!p.mounted && p.mounts.Count > 0) AHComp.ToggleRide(g);
        // to the start, behind the first ring
        Vector3 st = cps[0] - Flat(cps[1] - cps[0]).normalized * 6f; st.y = p.transform.position.y;
        p.transform.position = st; p.transform.rotation = Quaternion.LookRotation(Flat(cps[0] - st).normalized);
        g.camYaw = p.transform.rotation.eulerAngles.y;
        foreach (var r in rings) { r.gameObject.SetActive(true); r.GetComponent<Renderer>().sharedMaterial = dim; }
        next = 0; Paint(); counting = true; startAt = Time.time + 3f; rec.Clear(); recT = 0f;
        g.ui.Banner("The Meadow race", "Through all " + cps.Count + " rings! " + (Best > 0f ? "Your best: " + Clock(Best) : "Gold " + Clock(Par(3)) + " · silver " + Clock(Par(2)) + " · bronze " + Clock(Par(1))));
        AHSound.Play("door");
    }
    void Paint()
    {
        for (int i = 0; i < rings.Count; i++)
        {
            var mr = rings[i].GetComponent<Renderer>(); var m = i == next ? bright : dim;
            mr.sharedMaterial = m; foreach (var c in rings[i].GetComponentsInChildren<Renderer>()) c.sharedMaterial = m;
            rings[i].gameObject.SetActive(i >= next);
        }
    }

    void Update()
    {
        if (g == null || g.player == null) return;
        var p = g.player; float now = Time.time;
        if (counting)
        {
            float left = startAt - now;
            g.ui.SetKQText(left > 0f ? "Race · " + Mathf.CeilToInt(left) + "…" : "GO!");
            if (left <= 0f) { counting = false; running = true; t = 0f; AHSound.Play("roar"); if (ghost != null && ghost.Count > 1) MakeWisp(); }
            return;
        }
        if (!running) return;
        t += Time.deltaTime;
        // the ghost of your best run
        recT += Time.deltaTime; if (recT >= 0.1f) { recT = 0f; rec.Add(new Vector2(p.transform.position.x, p.transform.position.z)); }
        if (wisp != null && ghost != null)
        {
            float f = t / 0.1f; int i = Mathf.Min(Mathf.FloorToInt(f), ghost.Count - 1), j = Mathf.Min(i + 1, ghost.Count - 1);
            Vector2 gp = Vector2.Lerp(ghost[i], ghost[j], f - Mathf.Floor(f));
            wisp.position = new Vector3(gp.x, p.transform.position.y + 1.3f, gp.y);
        }
        Vector3 c = cps[next];
        float d = Flat(c - p.transform.position).magnitude;
        if (d < 3.6f)
        {
            AHSound.Play("coin"); AHFx.Ring(c, 0.5f, 3.4f, new Color(1f, 0.85f, 0.4f), 0.4f);
            next++;
            if (next >= cps.Count) { Finish(); return; }
            Paint();
        }
        else if (d > 75f || t > 240f) { Stop("You left the course. Talk to Fenna to try again."); return; }
        g.ui.SetKQText("Race · ring " + (next + 1) + " / " + cps.Count + " · " + Clock(t) + (Best > 0f ? " · best " + Clock(Best) : ""));
    }

    void Finish()
    {
        running = false; var p = g.player; float best = Best;
        string res = "Your time: " + Clock(t);
        bool newBest = best <= 0f || t < best;
        if (newBest) { AHPrefs.SetFloat("ah_race_best", t); SaveGhost(); res += best > 0f ? " · a new best (was " + Clock(best) + ")!" : " · your first finish!"; }
        int medal = t <= Par(3) ? 3 : t <= Par(2) ? 2 : t <= Par(1) ? 1 : 0;
        string[] mn = { "", "Bronze", "Silver", "Gold" };
        if (medal > 0) { res += " · " + mn[medal] + " medal!"; long had = AHProgress.Get(p.prog.stats, "race_medal"); if (medal > had) p.prog.Bump("race_medal", medal - had); }
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (AHPrefs.GetString("ah_race_day", "") != today) { AHPrefs.SetString("ah_race_day", today); long coins = (long)(20 + p.level * 6 + medal * 15) * AHDB.CU; p.AddMoney(coins, p.transform.position); res += " · " + AHItems.MoneyText(coins); }
        p.prog.Bump("races");
        AHSound.Play("level"); AHSpark.Burst(p.transform.position + Vector3.up * 2f, new Color(1f, 0.85f, 0.4f, 1f), 40, 4f, 1.2f, 0.15f, 2f);
        g.ui.Banner("Finish!", res);
        Clean(); g.MarkDirty();
    }
    void Stop(string why) { running = false; counting = false; g.ui.Toast(why, 3f); Clean(); }
    void Clean()
    {
        g.ui.SetKQText(null);
        if (wisp != null) Destroy(wisp.gameObject); wisp = null;
        for (int i = 0; i < rings.Count; i++) rings[i].gameObject.SetActive(i == rings.Count - 1);
        next = -1;
    }

    void MakeWisp()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(go.GetComponent<Collider>()); go.name = "AH_Race ghost";
        go.transform.localScale = Vector3.one * 0.55f; go.GetComponent<Renderer>().sharedMaterial = Glow(new Color(0.45f, 0.75f, 1f), 3f);
        var tr = go.AddComponent<TrailRenderer>(); tr.time = 0.6f; tr.startWidth = 0.4f; tr.endWidth = 0f;
        tr.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); tr.material.SetColor("_BaseColor", new Color(0.5f, 0.8f, 1f, 0.6f));
        wisp = go.transform;
    }
    void SaveGhost()
    {
        var sb = new StringBuilder(); foreach (var v in rec) sb.Append(v.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(v.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(';');
        AHPrefs.SetString("ah_race_ghost", sb.ToString()); ghost = new List<Vector2>(rec);
    }
    void LoadGhost()
    {
        var s = AHPrefs.GetString("ah_race_ghost", ""); if (s == "") return;
        ghost = new List<Vector2>();
        foreach (var part in s.Split(';'))
        {
            var xy = part.Split(','); float x, z;
            if (xy.Length == 2 && float.TryParse(xy[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(xy[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) ghost.Add(new Vector2(x, z));
        }
    }

    public static void Test(string what)
    {
        if (I == null) return;
        if (what == "start" || what == "") I.Offer();
        else if (what == "view" && I.cps.Count > 2)
        {
            Vector3 a = I.cps[0], b = I.cps[Mathf.Min(3, I.cps.Count - 1)];
            AHGame.FreePos = a - Flat(b - a).normalized * 10f + Vector3.up * 9f; AHGame.FreeRot = Quaternion.LookRotation((a + b) / 2f - AHGame.FreePos); AHGame.FreeCam = true;
            foreach (var r in I.rings) r.gameObject.SetActive(true);
        }
    }
}
