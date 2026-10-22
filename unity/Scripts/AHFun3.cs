// Ashen Hollow: more reasons to come back.
//  - The bounty board: three named beasts a day, each with its story ("the wolf that steals from the baker"), each
//    prowling one of the lands you know. Go there and it shows itself; beat it for coin, experience and marks.
//  - Photo spots: fine views (each waystone, and a few in Hollow Meadow). Stand at one in photo mode and SNAP: the
//    picture goes in your album.
//  - Recently picked up: the last things that came into your bag (the bag's "New" tab).
//  - While you were away: back after a while, one card says what finished, grew and arrived.
//  - The night: glowfin bite after dark (any water), a pale peddler haunts the Hollow square, and every third night
//    the town lets lanterns rise over the square (AHNightFun).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHFun3
{
    public const string Glowfin = "glowfin";
    public static void Inject()
    {
        AHBag.Added = OnAdded;
        var items = AHDB.Items; if (items == null || items.ContainsKey(Glowfin)) return;
        items[Glowfin] = new Dictionary<string, object>
        {
            { "name", "Glowfin" }, { "icon", "trout" }, { "c", "#7fe0ff" }, { "price", 60.0 },
            { "food", new Dictionary<string, object> { { "hp", 40.0 }, { "hunger", 45.0 } } },
            { "note", "A fish that shines like the moon. It only bites after dark, and fetches a good price." },
        };
    }

    // ===================================== recently picked up =====================================
    static readonly List<string> recent = new List<string>();
    static float readyAt;
    public static void OnAdded(string id, int n)
    {
        if (AHGame.I == null || AHGame.I.player == null || AHLoading.Showing || Time.time < readyAt) return;
        recent.Remove(id); recent.Insert(0, id); while (recent.Count > 10) recent.RemoveAt(recent.Count - 1);
    }
    public static bool IsRecent(string id) { return recent.Contains(id); }
    public static int RecentIndex(string id) { return recent.IndexOf(id); }
    public static int RecentCount { get { return recent.Count; } }
    public static void LandReady() { readyAt = Time.time + 3f; }   // a land's own setup fills the bag too: not "new"

    // ===================================== the bounty board =====================================
    public class Bounty { public string area, name, story; public bool done, paid; }
    static readonly string[] Names = { "Greytooth", "Old Ironhide", "Mother Mudfang", "Blackclaw", "Saltjaw", "Ember-eye", "Hollowhowl", "Thornback", "Grimtusk", "Sootfur", "Widow Silkleg", "Crookhorn" };
    static readonly string[][] Stories =
    {
        new[] { "that steals from the baker", "الذي يسرق من الخبّازة" },
        new[] { "that frightened the miller's children", "الذي أخاف أطفال الطحّان" },
        new[] { "that tore down Farmer Tobin's fence", "الذي هدم سياج المزارع توبن" },
        new[] { "that the caravans fear", "الذي تخشاه القوافل" },
        new[] { "with a scar across its eye", "ذو الندبة على عينه" },
        new[] { "that ate the guard's lunch", "الذي أكل غداء الحارس" },
        new[] { "that howls outside the inn every night", "الذي يعوي خارج النُّزُل كل ليلة" },
        new[] { "that nobody has seen and lived to boast of", "الذي لم يره أحد وعاش ليتفاخر" },
    };
    static List<Bounty> today; static string todayKey;
    static string Day() { return DateTime.Now.ToString("yyyy-MM-dd"); }
    public static List<Bounty> Today(AHPlayer p)
    {
        string d = Day();
        if (today != null && todayKey == d) return today;
        today = new List<Bounty>(); todayKey = d;
        string saved = AHPrefs.GetString("ah_bounty", "");
        if (saved.StartsWith(d + "#"))
        {
            foreach (var part in saved.Substring(d.Length + 1).Split(';'))
            {
                var f = part.Split('~'); if (f.Length < 5) continue;
                today.Add(new Bounty { area = f[0], name = f[1], story = f[2], done = f[3] == "1", paid = f[4] == "1" });
            }
            if (today.Count > 0) return today;
        }
        // three of the lands you know, near your level (Hollow Meadow when you know few)
        var rnd = new System.Random(d.GetHashCode() ^ (p.heroName ?? "").GetHashCode());
        var lands = new List<string>();
        foreach (var o in AHDB.List("world", "REGIONS") ?? new List<object>())
        {
            string id = AHJson.S(o, "id"), lv = AHJson.S(o, "lv", "");
            if (id == null || lv == "safe" || !p.prog.found.Contains(id)) continue;
            int lo, hi; if (!Range(lv, out lo, out hi)) continue;
            if (p.level + 3 >= lo && p.level - 14 <= hi) lands.Add(id);
        }
        if (lands.Count == 0) lands.Add("meadow");
        var names = new List<string>(Names); var stories = new List<string[]>(Stories);
        for (int i = 0; i < 3; i++)
        {
            string a = lands[rnd.Next(lands.Count)]; if (lands.Count > 2) lands.Remove(a);
            var st = stories[rnd.Next(stories.Count)]; stories.Remove(st);
            string nm = names[rnd.Next(names.Count)]; names.Remove(nm);
            today.Add(new Bounty { area = a, name = nm, story = AHLoc.Arabic ? st[1] : st[0] });
        }
        Save(); return today;
    }
    static bool Range(string lv, out int lo, out int hi)
    {
        lo = hi = 0; var f = lv.Replace("–", "-").Split('-');
        return f.Length == 2 && int.TryParse(f[0].Trim(), out lo) && int.TryParse(f[1].Trim(), out hi);
    }
    static void Save()
    {
        var parts = new List<string>(); foreach (var b in today) parts.Add(b.area + "~" + b.name + "~" + b.story + "~" + (b.done ? 1 : 0) + "~" + (b.paid ? 1 : 0));
        AHPrefs.SetString("ah_bounty", todayKey + "#" + string.Join(";", parts.ToArray()));
    }
    public static void TestHere(AHPlayer p) { var l = Today(p); if (l.Count > 0) { l[0].area = AHGame.AreaId; l[0].done = false; l[0].paid = false; Save(); } spawnT = 1f; hunted = null; }   // test tools
    public static int Ready(AHPlayer p) { int n = 0; foreach (var b in Today(p)) if (b.done && !b.paid) n++; return n; }
    public static AHDaily.Reward Reward(AHPlayer p) { int L = Mathf.Max(1, p.level); return new AHDaily.Reward { money = 80 + L * 25, xp = (long)(L * L * 6 + 150), marks = 15 }; }
    public static void Claim(AHGame g, Bounty b)
    {
        if (b == null || !b.done || b.paid) return;
        b.paid = true; Save(); var r = Reward(g.player); r.name = AHLoc.T("Bounty · " + b.name, "مكافأة · " + b.name);
        AHDaily.Grant(g, r);   // (with its reward chest)
    }
    // in the bounty's land: it shows itself a little way off
    static AHMob hunted; static Bounty huntedB; static string huntedArea; static float spawnT;
    public static AHMob Hunted { get { return hunted != null && !hunted.dead ? hunted : null; } }
    static void BountyTick(AHGame g, float dt)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        if (huntedArea != AHGame.AreaId) { hunted = null; huntedB = null; huntedArea = AHGame.AreaId; spawnT = 6f; }
        if (hunted != null)
        {
            if (hunted.dead && huntedB != null && !huntedB.done)
            {
                huntedB.done = true; Save();
                g.ui.Banner(AHLoc.T("Bounty done: " + huntedB.name, "أُنجزت المكافأة: " + huntedB.name), AHLoc.T("Claim your reward on the bounty board", "استلم مكافأتك من لوحة المكافآت"));
                hunted = null;
            }
            return;
        }
        if ((spawnT -= dt) > 0f || g.mobs.Count == 0 || AHHollow.Active && g.InTown(p.transform.position)) return;
        spawnT = 20f;
        foreach (var b in Today(p))
        {
            if (b.done || b.area != AHGame.AreaId) continue;
            // the strongest ordinary beast of this land, grown into a named one
            AHMob like = null; foreach (var m in g.mobs) if (m != null && !m.dead && m.type != null && !m.type.elite && !m.golden && (like == null || m.type.lvl > like.type.lvl)) like = m;
            if (like == null) return;
            Vector3 at = like.transform.position; float best = 0f;
            foreach (var m in g.mobs) { if (m == null || m.dead) continue; float d = (m.transform.position - p.transform.position).magnitude; if (d > 25f && d < 90f && (best == 0f || Mathf.Abs(d - 45f) < Mathf.Abs(best - 45f))) { best = d; at = m.transform.position; } }
            var t = like.type;
            var bt = new AHMobType { id = t.id, name = b.name, model = t.model, lvl = t.lvl + 3, hp = t.hp * 7, dmg = Mathf.RoundToInt(t.dmg * 1.5f), xp = t.xp * 6, speed = t.speed, radius = t.radius, aggro = t.aggro, atkCd = t.atkCd, respawn = 1e9f, elite = true, drops = t.drops, gold = t.gold * 4 };
            hunted = AHMob.Create(g, bt, at); g.mobs.Add(hunted); huntedB = b;
            if (hunted.Model != null) hunted.Model.transform.localScale *= 1.3f;
            g.ui.Banner(b.name, AHLoc.T("Your bounty prowls nearby · the red mark on the minimap", "طريدتك قريبة · العلامة الحمراء على الخريطة"));
            return;
        }
    }

    // ===================================== photo spots =====================================
    public class Spot { public string id, name; public Vector3 pos; }
    public static List<Spot> SpotsHere(AHGame g)
    {
        var l = new List<Spot>();
        var ws = AHWays.Ways;
        if (ws != null) foreach (var w in ws) if (AHJson.S(w, "area") == AHGameAreaId()) l.Add(new Spot { id = "way_" + AHJson.S(w, "id"), name = AHJson.S(w, "name"), pos = g.W((float)AHJson.N(w, "x") * AHDB.S, (float)AHJson.N(w, "y") * AHDB.S) });
        if (AHHollow.Active)
        {
            l.Add(new Spot { id = "hm_square", name = "The Hollow square", pos = AHHollow.C });
            if (AHHollow.Bell != null) l.Add(new Spot { id = "hm_bell", name = "The bell tower", pos = AHHollow.Bell.position });
            if (AHHollow.Gates.Count > 0) l.Add(new Spot { id = "hm_gate", name = "The town gate", pos = AHHollow.Gates[0].at });
        }
        return l;
    }
    static string AHGameAreaId() { return AHGame.AreaId; }
    public static List<string> Album(AHPlayer p) { var s = AHPrefs.GetString("ah_album", ""); return new List<string>(s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)); }
    public static string AlbumPath(string id) { return System.IO.Path.Combine(Application.persistentDataPath, "album", id + ".jpg"); }
    public static Spot NearSpot(AHGame g)
    {
        var p = g.player; if (p == null) return null;
        Spot best = null; float bd = 14f;
        foreach (var s in SpotsHere(g)) { float d = Vector3.Distance(s.pos, p.transform.position); if (d < bd) { bd = d; best = s; } }
        return best;
    }
    // from photo mode's SNAP, with the picture as it was taken
    public static void OnSnap(AHGame g, Texture2D shot)
    {
        var s = NearSpot(g); if (s == null || shot == null) { if (shot != null) UnityEngine.Object.Destroy(shot); return; }
        var have = Album(g.player); bool first = !have.Contains(s.id);
        // saved as taken (a phone's picture is a few hundred kilobytes as a JPG)
        try { var f = AlbumPath(s.id); System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(f)); System.IO.File.WriteAllBytes(f, shot.EncodeToJPG(80)); } catch { }
        UnityEngine.Object.Destroy(shot);
        if (first) { have.Add(s.id); AHPrefs.SetString("ah_album", string.Join(",", have.ToArray())); AHPrefs.SetString("ah_album_n_" + s.id, s.name); g.ui.Banner(AHLoc.T("New photo: " + s.name, "صورة جديدة: " + s.name), AHLoc.T("Added to your album", "أُضيفت إلى ألبومك")); }
        else g.ui.Toast(AHLoc.T("Your album's picture of " + s.name + " is updated.", "حُدّثت صورة " + s.name + " في ألبومك."), 2.5f);
    }
    static readonly HashSet<string> hinted = new HashSet<string>(); static float spotT;
    static void SpotTick(AHGame g, float dt)
    {
        if ((spotT -= dt) > 0f) return; spotT = 1.5f;
        var s = NearSpot(g); if (s == null || hinted.Contains(s.id) || g.ui.PhotoOn) return;
        hinted.Add(s.id);
        if (!Album(g.player).Contains(s.id)) g.ui.Toast(AHLoc.T("A fine view: " + s.name + ". Open photo mode (the emote button → Photo) and SNAP it for your album.", "منظر جميل: " + s.name + ". افتح وضع التصوير والتقط صورة لألبومك."), 4f);
    }

    // ===================================== while you were away =====================================
    static float seenT; static bool awayDone;
    public static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
    static void AwayTick(AHGame g, float dt)
    {
        var p = g.player; if (p == null || p.prog == null || AHLoading.Showing) return;
        if (!awayDone)
        {
            awayDone = true;
            long last = 0; long.TryParse(AHPrefs.GetString("ah_seen", "0"), out last);
            long gap = last > 0 ? NowS - last : 0;
            if (gap >= 15 * 60) g.ui.AwayCard(gap, AwayLines(g, p));
        }
        if ((seenT -= dt) <= 0f) { seenT = 20f; AHPrefs.SetString("ah_seen", NowS.ToString()); }
    }
    static List<string> AwayLines(AHGame g, AHPlayer p)
    {
        var l = new List<string>();
        int mail = AHMail.Count(p); if (mail > 0) l.Add(AHLoc.T(mail + " letters wait in your mail", mail + " رسائل تنتظرك في البريد"));
        int dr = AHDaily.Ready(p); if (dr > 0) l.Add(AHLoc.T(dr + " daily goals to claim", dr + " أهداف يومية للاستلام"));
        if (p.home != null)
        {
            int ripe = 0; foreach (var f in p.home.fields) if (!string.IsNullOrEmpty(f.crop) && f.prog >= 1f) ripe++;
            int pens = 0; foreach (var pn in p.home.pens) if (pn.ready > 0) pens++;
            if (ripe > 0) l.Add(AHLoc.T(ripe + " fields ready to harvest", ripe + " حقول جاهزة للحصاد"));
            if (pens > 0) l.Add(AHLoc.T("Your animals have things to collect", "حيواناتك لديها ما تجمعه"));
        }
        int back = 0; foreach (var e in AHExpeditions.L(p)) if (AHExpeditions.Back(e)) back++;
        if (back > 0) l.Add(AHLoc.T(back + " pets are back from their expeditions", back + " حيوانات عادت من رحلاتها"));
        int bn = 0; foreach (var b in Today(p)) if (!b.done) bn++;
        if (bn > 0) l.Add(AHLoc.T(bn + " bounties on the board today", bn + " مكافآت على اللوحة اليوم"));
        if (l.Count == 0) l.Add(AHLoc.T("All quiet. The realm waited for you.", "كل شيء هادئ. المملكة انتظرتك."));
        return l;
    }

    // ===================================== every frame (from the UI) =====================================
    public static void Tick(AHGame g, float dt)
    {
        if (g == null || g.player == null || g.ui == null) return;
        try { BountyTick(g, dt); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: bounty (" + e.Message + ")"); spawnT = 60f; }
        try { SpotTick(g, dt); } catch { }
        try { AwayTick(g, dt); } catch { }
    }
}

// ===================================== the night in Hollow Meadow =====================================
public class AHNightFun : MonoBehaviour
{
    AHGame g; AHNpc ghost; ParticleSystem lanterns; bool night, festival; float checkT, emitT;
    public static bool Festival(AHGame g) { return g != null && g.IsNight && AHNight.NightId(g) % 3 == 0; }
    public void Init(AHGame game)
    {
        g = game;
        var at = AHHollow.C + (AHGame.I.W(0f, 1f) - AHGame.I.W(0f, 0f)).normalized * 7.5f + (AHGame.I.W(1f, 0f) - AHGame.I.W(0f, 0f)).normalized * 4f;
        ghost = AHNpc.SpawnAt(g, "The Pale Peddler", at, AHHollow.C - at, new[] { "I only trade by moonlight, friend.", "Old maps, odd luck. Gone by morning." });
        ghost.label = AHLoc.T("Night trader", "تاجر الليل");
        ghost.onTalk = () => { g.ui.TalkLine(ghost, AHLoc.T("Look quick. I'm gone at dawn.", "انظر بسرعة. سأرحل عند الفجر.")); g.ui.OpenShop("ghost_peddler"); return true; };
        StartCoroutine(Pale());
        ghost.gameObject.SetActive(g.IsNight);
        night = g.IsNight;
    }
    System.Collections.IEnumerator Pale()
    {
        yield return new WaitForSeconds(1.5f);
        if (ghost == null) yield break;
        foreach (var r in ghost.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.materials;
            foreach (var m in ms) foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) { var c = m.GetColor(pr); float l = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f; m.SetColor(pr, new Color(0.55f + l * 0.4f, 0.75f + l * 0.25f, 1f, c.a)); }
            r.materials = ms;
        }
        var glow = new GameObject("PaleGlow"); glow.transform.SetParent(ghost.transform, false); glow.transform.localPosition = Vector3.up * 1.1f; glow.AddComponent<AHWeaponGlow>().col = new Color(0.6f, 0.85f, 1f);
    }
    void Update()
    {
        if (g == null) return;
        if ((checkT -= Time.deltaTime) <= 0f)
        {
            checkT = 2f;
            bool n = g.IsNight || forced;
            if (n != night) { night = n; if (ghost != null) ghost.gameObject.SetActive(n); if (n && AHHollow.Inside(g.player.transform.position, 30f)) g.ui.Toast(AHLoc.T("A pale peddler has drifted into the square.", "تاجر شاحب ظهر في الساحة."), 3f); }
            bool f = Festival(g) || forced;
            if (f != festival) { festival = f; if (f) { if (lanterns == null) lanterns = MakeLanterns(); g.ui.Banner(AHLoc.T("Lantern night", "ليلة الفوانيس"), AHLoc.T("Lanterns rise over the Hollow square", "الفوانيس ترتفع فوق ساحة هولو")); } }
        }
        if (festival && lanterns != null && (emitT -= Time.deltaTime) <= 0f)
        {
            emitT = 0.45f;
            var ep = new ParticleSystem.EmitParams { position = AHHollow.C + new Vector3(UnityEngine.Random.Range(-9f, 9f), 1.5f, UnityEngine.Random.Range(-9f, 9f)), velocity = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), UnityEngine.Random.Range(0.55f, 0.9f), UnityEngine.Random.Range(-0.15f, 0.15f)), startSize = UnityEngine.Random.Range(0.6f, 0.9f), startLifetime = 28f, startColor = Color.Lerp(new Color(1f, 0.62f, 0.25f, 0.95f), new Color(1f, 0.85f, 0.45f, 0.95f), UnityEngine.Random.value) };
            lanterns.Emit(ep, 1);
        }
    }
    void ForceFestival() { festival = true; forced = true; if (lanterns == null) lanterns = MakeLanterns(); if (ghost != null) ghost.gameObject.SetActive(true); }   // test tools
    bool forced;
    ParticleSystem MakeLanterns()
    {
        var mat = AHGame.LoadMat("AH/Materials/Spark", "AshenHollow/Spark"); if (mat == null) mat = AHFx.Mat; if (mat == null) return null;
        var go = new GameObject("Lanterns"); go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = 120; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = 0f;
        var em = ps.emission; em.rateOverTime = 0f; var sh = ps.shape; sh.enabled = false;
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.15f;
        var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) }); col.color = gr;
        var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        return ps;
    }
}
