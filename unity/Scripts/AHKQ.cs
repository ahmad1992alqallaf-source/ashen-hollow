// Ashen Hollow: Kingdom Quests in the Proving Grounds arena, as in the web game (v66, KQ_MODES / kqWindow / kqStart /
// kqTick / kqEnd). Every hour at :00 the Siege opens (five waves of arena fighters, the fifth with the Arena champion),
// at :30 the Gold Rush (catch gold imps for two and a half minutes). Each window lasts 10 minutes, one run per window.
// The arena's monsters are made to measure for your level; your party comes with you. Coins, XP, enhancement stones,
// and sometimes a monster card, a gem or a Lucky charm. Trial-master Garron (Varrow) and Arena crier Bess (Ashen Hollow).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHKQ
{
    public const string Area = "kq";
    const float CX = 233000, CY = 1000;
    public struct Win { public string mode; public bool active; public long id; public double left, next; }
    static double NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0; } }
    public static Win Window()
    {
        double t = NowS, half = 1800, pos = ((t % half) + half) % half, start = t - pos;
        string mode = Math.Floor(start / half) % 2 == 0 ? "siege" : "rush";
        return new Win { mode = mode, active = pos < 600, id = (long)Math.Round(start), left = 600 - pos, next = half - pos };
    }
    public static double Next(string k)
    {
        double t = NowS, s0 = Math.Floor(t / 1800) * 1800;
        for (int i = 0; i < 3; i++) { double st = s0 + i * 1800; if ((Math.Floor(st / 1800) % 2 == 0 ? "siege" : "rush") == k && st + 600 > t) return Math.Max(0, st - t); }
        return 3600;
    }
    public static string Name(string k) { return k == "siege" ? "Siege of the Proving Grounds" : "Gold Rush"; }
    public static string Blurb(string k) { return k == "siege" ? "Survive five waves of arena fighters. The fifth brings the Arena champion." : "Gold imps pour into the arena for two and a half minutes. Catch as many as you can."; }
    static float Length(string k) { return k == "siege" ? 360f : 150f; }

    // the run (kept across the trip into the arena)
    public static string Mode = ""; static long runId;
    static float t, waveT, spawnT; static int wave, score; static bool over;
    static readonly List<AHMob> mine = new List<AHMob>();

    public static void Start(AHGame g, string mode)
    {
        var w = Window(); var p = g.player;
        if (Mode != "" || !w.active || w.mode != mode || p.prog.kqDone == w.id || p.dead) return;
        if (p.level < 5) { g.ui.Toast("Kingdom Quests are for level 5 and up."); return; }
        p.prog.kqDone = w.id; Mode = mode; runId = w.id;
        Vector2 at = g.ToWeb(p.transform.position);
        AHPrefs.SetString("ah_kq", mode + "|" + AHGame.AreaId + "|" + at.x + "|" + at.y);
        if (p.mounted) AHComp.Dismount(g, true);
        g.SaveProgress();
        g.Travel(Area, CX * AHDB.S, (CY + 200) * AHDB.S, -Mathf.PI / 2f);
    }

    // the editor's test menu: a run now, whatever the clock says
    public static void StartTest(AHGame g, string mode)
    {
        var p = g.player; if (Mode != "" || p.dead) return;
        Mode = mode; Vector2 at = g.ToWeb(p.transform.position);
        AHPrefs.SetString("ah_kq", mode + "|" + AHGame.AreaId + "|" + at.x + "|" + at.y);
        g.Travel(Area, CX * AHDB.S, (CY + 200) * AHDB.S, -Mathf.PI / 2f);
    }

    static void Back(AHGame g)
    {
        var s = AHPrefs.GetString("ah_kq", "").Split('|'); float x, z;
        AHPrefs.DeleteKey("ah_kq"); Mode = "";
        if (s.Length == 4 && float.TryParse(s[2], out x) && float.TryParse(s[3], out z) && s[1] != Area) g.Travel(s[1], x, z, Mathf.PI / 2f);
        else { var w = AHWays.Ways != null ? AHWays.Ways.Find(q => AHJson.S(q, "id") == "varrow") : null; g.Travel("city", (float)AHJson.N(w, "x", 41720) * AHDB.S, ((float)AHJson.N(w, "y", 1160) + 70) * AHDB.S, Mathf.PI / 2f); }
    }

    // every area: the arena only holds a run in progress
    public static void Setup(AHGame g)
    {
        mine.Clear();
        if (AHGame.AreaId != Area) { AHPrefs.DeleteKey("ah_kq"); Mode = ""; return; }
        var s = AHPrefs.GetString("ah_kq", "").Split('|');
        if (s.Length < 1 || (s[0] != "siege" && s[0] != "rush") || Mode != s[0]) { Mode = ""; g.ui.Toast("The arena is closed."); Back(g); return; }
        Mode = s[0]; t = 0; waveT = 0; spawnT = 3; wave = 0; score = 0; over = false;
        MakeTypes(g.player.level);
        Decor(g);
        foreach (var sp in AHGather.Spots) if (sp.kind == "gate" && sp.gate != null && sp.gate.label == "Leave the arena") sp.use = () => { if (!over) End(g, "quit"); else Back(g); };
        g.ui.Banner(Name(Mode), "Kingdom Quest · get ready!");
    }

    // web kqMobDefs: arena monsters sized for the hero's level
    static readonly string[][] Foes = { new[] { "wolf", "Arena wolf", "Web/mdlWolf" }, new[] { "bandit", "Arena brawler", "bandit" }, new[] { "scorpion", "Arena scorpion", "scorpion" }, new[] { "raptor", "Arena raptor", "Web/mRaptorHQ" }, new[] { "cinderhound", "Arena hound", "Web/mCerberus" }, new[] { "icewolf", "Arena frostfang", "Web/mdlWolf" } };
    static readonly Dictionary<string, AHMobType> types = new Dictionary<string, AHMobType>();
    static AHMobType Make(string id, string b, string name, string model, int L, int hp, int dmg, int xp, int gold, bool elite)
    {
        var d = AHJson.O(AHDB.Mobs, b);
        var all = AHDB.Mobs; if (all != null && !all.ContainsKey(id)) { var c = new Dictionary<string, object>(); var bd = d as Dictionary<string, object>; if (bd != null) foreach (var kv in bd) c[kv.Key] = kv.Value; c["name"] = name; c["model"] = b; c["drops"] = new List<object>(); c["noSkin"] = true; c["slam"] = id == "kq_champ"; all[id] = c; }
        return new AHMobType { id = id, name = name, model = model, lvl = L, hp = hp, dmg = dmg, xp = xp, gold = gold, elite = elite, speed = (float)AHJson.N(d, "speed", 90) * AHDB.S, radius = (float)AHJson.N(d, "r", 18) * AHDB.S, aggro = 900 * AHDB.S, atkCd = (float)AHJson.N(d, "atkCd", 1.3), respawn = 1e9f, noSkin = true };
    }
    static void MakeTypes(int L)
    {
        int hp = Mathf.RoundToInt(120 + 4 * L), dmg = Mathf.RoundToInt(L * 0.35f + 2), xp = Mathf.RoundToInt(10 * L + 0.06f * L * L);
        for (int i = 0; i < Foes.Length; i++) types["kq_" + i] = Make("kq_" + i, Foes[i][0], Foes[i][1], Foes[i][2], L, hp, dmg, xp, Mathf.RoundToInt(L / 2f), false);
        types["kq_champ"] = Make("kq_champ", "troll", "Arena champion", "troll", L, hp * 4, Mathf.RoundToInt(dmg * 1.5f), xp * 6, L * 4, true);
        types["kq_gold"] = Make("kq_gold", "imp", "Gold imp", "Web/mHellImp", L, Mathf.RoundToInt(hp * 0.35f), Mathf.RoundToInt(dmg * 0.5f), Mathf.RoundToInt(xp * 0.5f), L, false);
        types["kq_gold"].speed = 120 * AHDB.S;
    }
    static void Spawn(AHGame g, string type, int n)
    {
        AHMobType tp; if (!types.TryGetValue(type, out tp)) return;
        for (int k = 0; k < n; k++)
        {
            float a = UnityEngine.Random.value * Mathf.PI * 2f, r = 520 + UnityEngine.Random.value * 120;
            Vector3 at = g.Resolve(g.W((CX + Mathf.Cos(a) * r) * AHDB.S, (CY + Mathf.Sin(a) * r) * AHDB.S), tp.radius);
            var m = AHMob.Create(g, tp, at); m.add = true; m.Engage(); g.mobs.Add(m); mine.Add(m);
            AHFx.Pop(at + Vector3.up * 0.6f, 1.6f, new Color(1f, 0.8f, 0.3f));
        }
    }
    static int Alive() { int n = 0; foreach (var m in mine) if (m != null && !m.dead) n++; return n; }

    // web kqTick
    public static void Tick(AHGame g, float dt)
    {
        if (AHGame.AreaId != Area || Mode == "") { g.ui.SetKQText(null); return; }
        var p = g.player;
        t += dt; float left = Mathf.Max(0f, Length(Mode) - t);
        if (!over)
        {
            if (p.dead) { End(g, "fell"); return; }
            if (Mode == "siege")
            {
                waveT += dt;
                if (t > 4 && (wave == 0 || (Alive() == 0 && waveT > 2) || waveT > 75) && wave < 5)
                {
                    if (wave > 0 && Alive() == 0) score = wave;
                    wave++; waveT = 0;
                    int[] counts = { 0, 2, 2, 3, 3, 4 };
                    Spawn(g, "kq_" + ((wave + UnityEngine.Random.Range(0, 3)) % Foes.Length), counts[wave]);
                    if (wave == 5) Spawn(g, "kq_champ", 1);
                    g.ui.Banner("Wave " + wave + " of 5", wave == 5 ? "The Arena champion enters!" : "Here they come");
                }
                if (wave == 5 && Alive() == 0 && waveT > 1) { score = 5; End(g, "won"); return; }
            }
            else
            {
                spawnT -= dt;
                if (t > 3 && spawnT <= 0 && Alive() < 7) { spawnT = 1.6f; Spawn(g, "kq_gold", 1 + (UnityEngine.Random.value < 0.3f ? 1 : 0)); }
            }
            if (left <= 0) { End(g, "time"); return; }
        }
        g.ui.SetKQText(over ? "Kingdom Quest complete" : Mode == "siege" ? "Siege · wave " + wave + "/5 · " + AHEvents.Fmt(left) : "Gold Rush · " + score + " imps" + (score >= 30 ? " · full reward!" : " (30 = full reward)") + " · " + AHEvents.Fmt(left));
    }

    public static void OnKill(AHMob m) { if (!over && Mode == "rush" && m.type.id == "kq_gold" && mine.Contains(m)) score++; }

    // web kqEnd
    static void End(AHGame g, string why)
    {
        if (over) return; over = true;
        var p = g.player; int L = p.level;
        float frac = Mode == "siege" ? score / 5f : Mathf.Min(1f, score / 30f);
        if (Mode == "siege") p.prog.kqBestSiege = Mathf.Max(p.prog.kqBestSiege, score); else p.prog.kqBestRush = Mathf.Max(p.prog.kqBestRush, score);
        long coins = (long)Mathf.Round((40 + L * 12) * (0.3f + frac * 1.7f)) * AHDB.CU; int xp = Mathf.RoundToInt((60 + L * L * 1.6f) * (0.3f + frac * 1.7f)), stones = Mathf.FloorToInt(frac * 4) + (frac >= 1 ? 1 : 0);
        p.AddMoney(coins, p.transform.position); p.GainClassXpDirect(xp); if (stones > 0) p.bag.Add("enh_stone", stones);
        var extras = new List<string>();
        if (frac >= 0.6f && UnityEngine.Random.value < 0.35f)
        {
            var co = AHDB.Table("monsters", "CARD_OF"); var pool = new List<string>(); if (co != null) foreach (var k in co.Keys) { var d = AHJson.O(AHDB.Mobs, k); if (!AHJson.B(d, "world") && !AHJson.B(d, "dboss") && !AHJson.B(d, "boss")) pool.Add(k); }
            if (pool.Count > 0) { string c = "card_" + pool[UnityEngine.Random.Range(0, pool.Count)]; if (p.bag.Add(c)) extras.Add(AHItems.Get(c).name); }
        }
        if (frac >= 1 && UnityEngine.Random.value < 0.3f && p.bag.Add("lucky_charm")) extras.Add("a Lucky charm");
        if (frac >= 0.8f && UnityEngine.Random.value < 0.5f) { string[] gs = { "ruby", "sapphire", "emerald" }; string gm = gs[UnityEngine.Random.Range(0, 3)]; if (p.bag.Add(gm)) extras.Add(AHItems.Get(gm).name); }
        string head = why == "quit" ? "You left the Kingdom Quest" : why == "fell" ? "You fell in the arena" : why == "won" ? "Victory!" : "Time!";
        g.ui.Banner(head, Mode == "siege" ? "Waves cleared: " + score + " of 5" : "Gold imps caught: " + score);
        g.ui.Toast("Kingdom Quest reward: " + AHItems.MoneyText(coins) + ", " + xp + " XP" + (stones > 0 ? ", " + stones + " enhancement stone" + (stones > 1 ? "s" : "") : "") + (extras.Count > 0 ? ", " + string.Join(", ", extras.ToArray()) : "") + ".", 6f);
        g.quests.Event("kq", "any", 1, g);
        g.SaveProgress();
        foreach (var m in mine) if (m != null && !m.dead) m.Hurt(999999, null, true);
        g.StartCoroutine(BackLater(g, why == "fell" ? 4f : 3.5f));
    }
    static System.Collections.IEnumerator BackLater(AHGame g, float s)
    {
        yield return new WaitForSeconds(s);
        if (AHGame.AreaId == Area) Back(g);
    }

    static void Decor(AHGame g)
    {
        var root = new GameObject("Arena").transform;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var stone = new Material(sh); stone.SetColor("_BaseColor", AHGame.Hex(0x6a5a4a).linear);
        int[] cols = { 0xa3121f, 0x2a5aa0, 0xd9ab3a, 0x2e8a4a };
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2f;
            Vector3 at = g.Resolve(g.W((CX + Mathf.Cos(a) * 735) * AHDB.S, (CY + Mathf.Sin(a) * 735) * AHDB.S), 0.5f);
            var brz = AHStations.Brazier(root, 1.7f, 31 + i); brz.transform.position = at;
            if (i % 3 == 0)
            {
                Vector3 bp = g.Resolve(g.W((CX + Mathf.Cos(a) * 690) * AHDB.S, (CY + Mathf.Sin(a) * 690) * AHDB.S), 0.5f);
                var ban = AHStations.Banner(root, AHGame.Hex(cols[i / 3])); ban.transform.position = bp;
                Vector3 toC = g.W(CX * AHDB.S, CY * AHDB.S) - bp; toC.y = 0f; ban.transform.rotation = Quaternion.LookRotation(toC.normalized);
            }
            g.AddBlocker(at, 0.4f);
        }
    }
}

public partial class AHUI
{
    Text kqText;
    public void SetKQText(string s)
    {
        if (kqText == null) { if (s == null) return; kqText = Center(Label(transform, "KQ", "", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(700, 34), new Color(1f, 0.85f, 0.4f))); kqText.rectTransform.anchorMin = kqText.rectTransform.anchorMax = new Vector2(0.5f, 1f); kqText.rectTransform.anchoredPosition = new Vector2(0, -150); var o = kqText.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.8f); }
        kqText.gameObject.SetActive(s != null); if (s != null && kqText.text != s) kqText.text = s;
    }

    public void OpenKQ() { wkMode = "kq"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderKQ(AHPlayer p)
    {
        var w = AHKQ.Window(); bool done = p.prog.kqDone == w.id;
        wkTitle.text = "Kingdom Quests";
        wkHint.text = "Timed challenges in the Crown’s arena, matched to your level (" + p.level + "); your party comes with you. You can fall without losing coins. Siege every hour at :00, Gold Rush at :30, each open 10 minutes.";
        var rows = new List<Action<int>>();
        foreach (var k in new[] { "siege", "rush" })
        {
            string kk = k; bool open = w.active && w.mode == k;
            rows.Add(s => Row(s, AHKQ.Name(kk) + (open ? "  <color=#9be37a>Open now</color>" : ""), new Color(1f, 0.84f, 0.42f), AHKQ.Blurb(kk),
                open ? (done ? "You already ran this one. Come back for the next." : "Closes in " + AHEvents.Fmt(w.left) + ".") : "Opens in " + AHEvents.Fmt(AHKQ.Next(kk)) + ".",
                new WkBtn { label = "Enter", on = open && !done && p.level >= 5 && AHKQ.Mode == "", col = Go, act = () => { ShowWork(false); AHKQ.Start(g, kk); } }));
        }
        rows.Add(s => Row(s, "Your best", Color.white, "Siege: wave " + p.prog.kqBestSiege + " · Gold Rush: " + p.prog.kqBestRush + " imps", ""));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
