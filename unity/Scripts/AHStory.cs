// Ashen Hollow: the story, "The Ashen King". Five chapters told by people you meet in Hollow Meadow: Seren the seer,
// Varek the King's herald, Kael the Ember Monk and the Ashen King himself (VRoid figures in their own costumes). Each
// chapter opens with a short cutscene (letterbox, the camera on whoever speaks, tap to go on), then a task: thin the
// wild beasts, beat the herald's Ash-bound, clear a floor of the Ashen Deep, bring down the Deep's master, and face the
// King's Colossus. The ending leaves its mark: the ash stops, a golden statue of you stands in the meadow, and you
// earn the title Kingsbane. MENU → The Ashen King shows where you are.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHStory
{
    public const string Area = "meadow";
    public class Line { public string who, text; public Line(string w, string t) { who = w; text = t; } }
    public class Ch
    {
        public string name, giver, turnin, task, kind; public int need, lvl; public long gold; public int xp;
        public Line[] intro, after, outro;
    }
    public static readonly Ch[] Chapters =
    {
        new Ch { name = "Ash on the Wind", giver = "Seren", turnin = "Seren", lvl = 1, kind = "kill", need = 10, task = "Defeat creatures that have gone wild", xp = 400, gold = 800,
            intro = new[] { new Line("Seren", "You see it too, don't you? Ash, falling from a clear sky."), new Line("Seren", "I am Seren. I read the old signs, and they all say one thing: the Ashen King is waking."), new Line("Seren", "The beasts feel it first. Thin their numbers, ten of them, and we will know how deep this runs.") },
            outro = new[] { new Line("Seren", "Ash in their fur, cinders in their eyes. It is worse than I feared."), new Line("Seren", "Someone is spreading it on purpose. Stay close. I have a feeling they will come to us.") } },
        new Ch { name = "The Herald", giver = "Seren", turnin = "Seren", lvl = 4, kind = "herald", need = 3, task = "Defeat Varek's Ash-bound", xp = 900, gold = 1500,
            intro = new[] { new Line("Seren", "Do you hear that? Bells, and no church rings them."), new Line("Varek", "So this is the little hero the seer found. How... ordinary."), new Line("Varek", "I am Varek, herald of the Ashen King. Kneel, and you may yet keep your village."), new Line("Seren", "Never! Don't listen to him."), new Line("Varek", "Then burn. Ash-bound, rise!") },
            after = new[] { new Line("Varek", "Hm. Stronger than you look."), new Line("Varek", "Enjoy your little victory. The King has waited a thousand years. He can wait a little longer.") },
            outro = new[] { new Line("Seren", "He fled below, into the Ashen Deep. That is where the King's fire still burns."), new Line("Seren", "Rest a while. Then we go after him.") } },
        new Ch { name = "Into the Deep", giver = "Seren", turnin = "Seren", lvl = 6, kind = "deepfloor", need = 1, task = "Clear a floor of the Ashen Deep (the violet portal in the meadow)", xp = 1500, gold = 2500,
            intro = new[] { new Line("Seren", "The Ashen Deep changes with every step: rooms that were not there yesterday."), new Line("Seren", "Find the way down. Clear one floor of the Deep and tell me what you saw.") },
            outro = new[] { new Line("Seren", "Walls that move... It is his doing. The King shapes the Deep to keep us out."), new Line("Seren", "I know someone who has fought down there. Find Kael, the Ember Monk. He is here, by the square.") } },
        new Ch { name = "Embers of Loyalty", giver = "Kael", turnin = "Kael", lvl = 8, kind = "deepboss", need = 1, task = "Defeat the master of the Ashen Deep (floor 5)", xp = 2500, gold = 4000,
            intro = new[] { new Line("Kael", "Seren sent you? Then you have seen the ash."), new Line("Kael", "My order guarded the Deep for generations. Its master tore through us all."), new Line("Kael", "Prove the Deep can be beaten. Bring down its master, and my fists are yours.") },
            outro = new[] { new Line("Kael", "You did it. The master of the Deep, fallen."), new Line("Kael", "Then it is time. The King will come for you himself now. Tell Seren I am ready.") } },
        new Ch { name = "The Ashen King", giver = "Seren", turnin = "Seren", lvl = 10, kind = "king", need = 1, task = "Defeat the Ashen King's Colossus", xp = 5000, gold = 10000,
            intro = new[] { new Line("Seren", "The sky is turning. Kael, are you with us?"), new Line("Kael", "To the end."), new Line("Ashen King", "Little sparks. You broke my herald's pride and my Deep's master."), new Line("Ashen King", "Now face what made them. Rise, my Colossus!") },
            after = new[] { new Line("Ashen King", "Impossible... the ash... it is leaving me..."), new Line("Seren", "It's over. The fire is out.") },
            outro = new[] { new Line("Seren", "The ash has stopped falling. Hollow Meadow will remember your name."), new Line("Seren", "Kingsbane. Wear it proudly. And look, they have already raised your statue.") } },
    };
    public static bool Done(AHPlayer p) { return p != null && p.prog.storyCh >= Chapters.Length; }
    public static Ch Cur(AHPlayer p) { return p == null || Done(p) ? null : Chapters[p.prog.storyCh]; }
    public static string Status(AHPlayer p)
    {
        var c = Cur(p); if (c == null) return "The story is done. You are Kingsbane.";
        if (p.level < c.lvl) return "Chapter " + (p.prog.storyCh + 1) + " opens at level " + c.lvl + ".";
        if (p.prog.storyState == 0) return "Talk to " + c.giver + " in Hollow Meadow.";
        if (p.prog.storyState == 1) return c.task + (c.need > 1 ? " (" + p.prog.storyN + "/" + c.need + ")" : "") + ".";
        return "Go back to " + c.turnin + " in Hollow Meadow.";
    }

    // ---- the people of the story ----
    class Cast { public string name, cls, sex, hair, costume; public int hairCol, skin; public float size = 1f; public Vector2 off; public Color mark; }
    static readonly Cast[] Casts =
    {
        new Cast { name = "Seren", cls = "priest", sex = "f", hair = "long", hairCol = 7, skin = 1, costume = "sage", off = new Vector2(5f, 3f), mark = new Color(0.6f, 0.85f, 1f) },
        new Cast { name = "Kael", cls = "shaman", sex = "m", hair = "pony", hairCol = 0, skin = 4, costume = "ember", off = new Vector2(-5f, 3f), mark = new Color(1f, 0.5f, 0.2f) },
        new Cast { name = "Varek", cls = "warrior", sex = "m", hair = "short", hairCol = 9, skin = 2, costume = "crimson", off = new Vector2(0f, 15f), mark = new Color(1f, 0.25f, 0.25f) },
        new Cast { name = "Ashen King", cls = "mage", sex = "m", hair = "long", hairCol = 8, skin = 0, costume = "shadow", size = 1.3f, off = new Vector2(0f, 19f), mark = new Color(0.7f, 0.5f, 1f) },
    };
    static readonly Dictionary<string, GameObject> actors = new Dictionary<string, GameObject>();
    static readonly List<AHMob> foes = new List<AHMob>();
    static AHMob colossus;
    static GameObject marker;

    public static void Setup(AHGame g)
    {
        actors.Clear(); foes.Clear(); colossus = null; marker = null;
        if (AHGame.AreaId != Area || g.player == null) return;
        var p = g.player; int ch = p.prog.storyCh;
        // who stands in the meadow now
        Spawn(g, "Seren");
        if (ch >= 3) Spawn(g, "Kael");
        if (ch == 1 && p.prog.storyState == 1) { Spawn(g, "Varek"); Herald(g); }
        if (ch == 4 && p.prog.storyState == 1) { Spawn(g, "Ashen King"); King(g); }
        if (Done(p)) Statue(g);
        Mark(g);
        var c = Cur(p);
        if (c != null && p.prog.storyState == 0 && p.level >= c.lvl) g.StartCoroutine(Nudge(g, c));
    }
    static System.Collections.IEnumerator Nudge(AHGame g, Ch c) { yield return new WaitForSeconds(6f); if (g.ui != null) g.ui.Toast(c.giver + " wants to speak with you. Story: " + c.name, 4f); }

    static Vector3 Spot(AHGame g, Vector2 off)
    {
        var s = g.data.spawn; return g.Resolve(g.W(s.x + off.x, s.z + off.y), 0.8f);
    }
    static GameObject Spawn(AHGame g, string who)
    {
        GameObject go; if (actors.TryGetValue(who, out go) && go != null) return go;
        Cast c = null; foreach (var x in Casts) if (x.name == who) c = x; if (c == null) return null;
        Vector3 at = Spot(g, c.off), face = g.W(g.data.spawn.x, g.data.spawn.z);
        go = Actor(g, c, at, face);
        if (go == null) return null;
        actors[who] = go;
        if (who == "Seren" || who == "Kael")
        {
            string w = who;
            AHGather.Spots.Add(new AHSpot { kind = "use", type = "story", name = "Talk to " + who, pos = at, r = 0.5f, reach = 3.2f, use = () => Talk(g, w) });
            g.AddBlocker(at, 0.45f);
        }
        return go;
    }

    // a figure of the story: the hero builder, in the person's look and costume, breathing
    static GameObject Actor(AHGame g, Cast c, Vector3 at, Vector3 face)
    {
        var holder = new GameObject("Story " + c.name).transform; holder.position = at;
        Vector3 d = face - at; d.y = 0f; if (d.sqrMagnitude > 0.01f) holder.rotation = Quaternion.LookRotation(d.normalized);
        var look = new AHLook { sex = c.sex, hair = c.hair, hairCol = c.hairCol, skin = c.skin, eye = -1 };
        AHAnim a; GameObject rig = null;
        try { rig = AHPeople.BuildHero(holder, AHClasses.Get(c.cls), look, g, out a); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: story figure " + c.name + ": " + e.Message); a = null; }
        if (rig == null) { UnityEngine.Object.Destroy(holder.gameObject); return null; }
        if (c.costume != null) AHCostumes.Apply(rig, c.costume, look, c.name == "Ashen King" ? "dye_violet" : null);
        if (a != null) { a.Play("Idle", true); holder.gameObject.AddComponent<AHStudioPose>().anim = a; }
        holder.localScale *= c.size;
        AHModel.SetShadows(holder.gameObject);
        return holder.gameObject;
    }

    // the golden statue of the hero in the meadow (after the story)
    static void Statue(AHGame g)
    {
        var p = g.player; Vector3 at = Spot(g, new Vector2(0f, 9f));
        var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder); UnityEngine.Object.Destroy(plinth.GetComponent<Collider>());
        plinth.name = "Kingsbane plinth"; plinth.transform.position = at + Vector3.up * 0.35f; plinth.transform.localScale = new Vector3(2.2f, 0.35f, 2.2f);
        var stone = new Material(Shader.Find("Universal Render Pipeline/Lit")); stone.SetColor("_BaseColor", new Color(0.55f, 0.52f, 0.48f)); plinth.GetComponent<Renderer>().sharedMaterial = stone;
        g.AddBlocker(at, 1.1f);
        var holder = new GameObject("Kingsbane statue").transform; holder.position = at + Vector3.up * 0.7f;
        Vector3 d = g.W(g.data.spawn.x, g.data.spawn.z) - at; d.y = 0; if (d.sqrMagnitude > 0.01f) holder.rotation = Quaternion.LookRotation(d.normalized);
        AHAnim a; GameObject rig = null;
        try { rig = AHPeople.BuildHero(holder, p.cls, p.look ?? new AHLook(), g, out a); } catch { a = null; }
        if (rig == null) { UnityEngine.Object.Destroy(holder.gameObject); return; }
        if (a != null) { a.Play("Idle", true); var pose = holder.gameObject.AddComponent<AHStudioPose>(); pose.anim = a; UnityEngine.Object.Destroy(pose, 0.6f); }
        holder.localScale *= 1.15f;
        g.StartCoroutine(Gild(holder.gameObject));
    }
    static System.Collections.IEnumerator Gild(GameObject go)
    {
        yield return new WaitForSeconds(0.3f);   // after the VRoid body and the gear are on
        if (go == null) yield break;
        var gold = new Material(Shader.Find("Universal Render Pipeline/Lit")); gold.SetColor("_BaseColor", new Color(0.95f, 0.72f, 0.25f)); gold.SetFloat("_Metallic", 0.85f); gold.SetFloat("_Smoothness", 0.55f);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = gold; r.sharedMaterials = ms; }
    }

    // a gold diamond over whoever has something for you
    static void Mark(AHGame g)
    {
        if (marker != null) UnityEngine.Object.Destroy(marker);
        var p = g.player; var c = Cur(p); if (c == null || p.level < c.lvl) return;
        string who = p.prog.storyState == 2 ? c.turnin : p.prog.storyState == 0 ? c.giver : null;
        GameObject a; if (who == null || !actors.TryGetValue(who, out a) || a == null) return;
        marker = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.Object.Destroy(marker.GetComponent<Collider>());
        marker.name = "Story mark"; marker.transform.SetParent(a.transform, false); marker.transform.localPosition = Vector3.up * 2.35f;
        marker.transform.localScale = Vector3.one * 0.22f; marker.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", new Color(1f, 0.8f, 0.25f)); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.2f) * 2f);
        marker.GetComponent<Renderer>().sharedMaterial = m;
        marker.AddComponent<AHSpinMark>();
    }

    // the map: a gold mark on whoever the story sends you to now
    public static void MapMarks(AHPlayer p, Action<Vector3, string> mark)
    {
        var c = Cur(p); if (c == null || p.level < c.lvl || p.prog.storyState == 1) return;
        string who = p.prog.storyState == 2 ? c.turnin : c.giver; GameObject a;
        if (actors.TryGetValue(who, out a) && a != null) mark(a.transform.position, "Story: " + who);
    }

    // ---- talking ----
    public static void Talk(AHGame g, string who)
    {
        var p = g.player; var c = Cur(p);
        if (c == null) { g.ui.Toast(who + ": " + (who == "Seren" ? "The meadow is quiet again, thanks to you, Kingsbane." : "My fists are yours whenever the ash returns."), 3.5f); return; }
        if (p.prog.storyState == 0 && who == c.giver)
        {
            if (p.level < c.lvl) { g.ui.Toast(who + ": Grow stronger first. Come back at level " + c.lvl + ".", 3.5f); return; }
            Begin(g, c);
            return;
        }
        if (p.prog.storyState == 2 && who == c.turnin) { Finish(g, c); return; }
        if (p.prog.storyState == 1) { g.ui.Toast(who + ": " + c.task + (c.need > 1 ? " (" + p.prog.storyN + "/" + c.need + ")" : "") + ".", 3f); return; }
        g.ui.Toast(who + ": " + (who == "Kael" ? "Talk to Seren first." : "Kael knows the Deep better than anyone."), 3f);
    }

    static void Begin(AHGame g, Ch c)
    {
        var p = g.player;
        if (c.kind == "herald") Spawn(g, "Varek");
        if (c.kind == "king") Spawn(g, "Ashen King");
        AHCine.Play(g, "Chapter " + (p.prog.storyCh + 1) + " · " + c.name, c.intro, () =>
        {
            p.prog.storyState = 1; p.prog.storyN = 0;
            if (c.kind == "herald") Herald(g);
            if (c.kind == "king") King(g);
            Mark(g); g.SaveProgress();
            g.ui.Banner(c.name, c.task);
        });
    }

    static void Progress(AHGame g, int n = 1)
    {
        var p = g.player; var c = Cur(p); if (c == null || p.prog.storyState != 1) return;
        p.prog.storyN += n; g.MarkDirty();
        if (p.prog.storyN < c.need) { if (c.need > 1) g.ui.Toast(c.name + ": " + p.prog.storyN + "/" + c.need, 1.5f); return; }
        p.prog.storyState = 2; g.SaveProgress();
        Action done = () => { Mark(g); g.ui.Banner(c.name, "Go back to " + c.turnin + " in Hollow Meadow"); };
        if (c.after != null && AHGame.AreaId == Area)
            AHCine.Play(g, null, c.after, () =>
            {
                if (c.kind == "herald") Vanish("Varek");
                if (c.kind == "king") Vanish("Ashen King");
                done();
            });
        else done();
    }

    static void Finish(AHGame g, Ch c)
    {
        var p = g.player;
        AHCine.Play(g, null, c.outro, () =>
        {
            p.GainXp(c.xp); p.AddMoney(c.gold * AHDB.CU, p.transform.position);
            int was = p.prog.storyCh; p.prog.storyCh++; p.prog.storyState = 0; p.prog.storyN = 0;
            g.ui.Banner("Chapter " + (was + 1) + " complete", c.name + " · +" + c.xp + " xp · " + AHItems.MoneyText(c.gold * AHDB.CU));
            AHSound.Play("level");
            if (was == 2) Spawn(g, "Kael");
            if (Done(p)) { g.ui.Banner("Kingsbane", "The story is done · your title is in Deeds"); Statue(g); }
            Mark(g); g.SaveProgress();
        });
    }

    static void Vanish(string who)
    {
        GameObject a; if (!actors.TryGetValue(who, out a) || a == null) return;
        AHFx.Ring(a.transform.position, 0.5f, 3f, new Color(0.6f, 0.4f, 0.9f), 1f);
        AHFx.Pop(a.transform.position + Vector3.up, 2f, new Color(0.4f, 0.35f, 0.4f), 0.8f);
        UnityEngine.Object.Destroy(a); actors.Remove(who);
    }

    // ---- the fights ----
    static AHMobType FoeType(AHGame g, string id, string model, string name, float hpK, float dmgK)
    {
        int L = Mathf.Max(1, g.player.level);
        return new AHMobType
        {
            id = id, model = model, name = name, lvl = L, hp = Mathf.RoundToInt(9f * Mathf.Pow(L, 1.3f) * hpK), dmg = Mathf.RoundToInt((3f + 1.25f * L) * dmgK),
            xp = Mathf.RoundToInt(L * 16 * hpK), gold = L * 4, speed = 120f * AHDB.S, radius = 15f * AHDB.S, aggro = 300f * AHDB.S, atkCd = 1.5f, respawn = 1e9f, noSkin = true,
        };
    }
    static void Herald(AHGame g)
    {
        GameObject v; if (!actors.TryGetValue("Varek", out v) || v == null) return;
        int left = Mathf.Max(0, 3 - g.player.prog.storyN);
        for (int i = 0; i < left; i++)
        {
            float a = i / 3f * 6.28f; Vector3 at = g.Resolve(v.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3.5f, 0.6f);
            var m = AHMob.Create(g, FoeType(g, "w_cultist", "Mobs/w_cultist", "Ash-bound", 1.6f, 1.1f), at); g.mobs.Add(m); foes.Add(m);
            AHFx.Pop(at + Vector3.up * 0.6f, 1.4f, new Color(1f, 0.4f, 0.2f));
        }
    }
    static void King(AHGame g)
    {
        GameObject k; if (!actors.TryGetValue("Ashen King", out k) || k == null) return;
        Vector3 at = g.Resolve(k.transform.position + k.transform.forward * 5f, 1.2f);
        var t = FoeType(g, "h_golem", "Web/mGolem", "The King's Colossus", 22f, 1.8f); t.elite = true; t.speed = 70f * AHDB.S; t.radius = 1.8f; t.aggro = 500f * AHDB.S; t.atkCd = 2f;
        colossus = AHMob.Create(g, t, at); g.mobs.Add(colossus); foes.Add(colossus);
        colossus.transform.localScale *= 1.7f;
        AHFx.Ring(at, 1f, 4f, new Color(0.7f, 0.4f, 1f), 1.2f);
    }

    public static bool IsBoss(AHMob m) { return m != null && m == colossus; }
    static float aT = 4f, bT = 10f;
    public static bool Think(AHGame g, AHMob m, float dt)
    {
        float hit = Mathf.Round(m.type.dmg * 1.3f);
        aT -= dt; bT -= dt;
        if (aT <= 0f) { aT = 6f; AHDungeon.Telegraph(g.player.transform.position, 2.3f, 1.4f, hit, m); return false; }
        if (bT <= 0f) { bT = 13f; AHDungeon.Telegraph(m.transform.position, m.type.radius + 5f, 1.9f, Mathf.Round(hit * 1.3f), m); m.Windup(1.9f); g.ui.Toast("The Colossus raises its fists! Get away!", 2.5f); return true; }
        return false;
    }

    // ---- what moves the story ----
    public static void OnMobDown(AHGame g, AHMob m)
    {
        var p = g.player; var c = Cur(p); if (c == null || p.prog.storyState != 1) return;
        if (c.kind == "kill" && !foes.Contains(m)) Progress(g);
        else if ((c.kind == "herald" || c.kind == "king") && foes.Contains(m)) { foes.Remove(m); Progress(g); }
    }
    public static void Event(AHGame g, string kind)
    {
        var p = g.player; var c = Cur(p); if (c == null || p.prog.storyState != 1 || c.kind != kind) return;
        Progress(g);
    }
}

public class AHSpinMark : MonoBehaviour
{
    float t; Vector3 at;
    void Start() { at = transform.localPosition; }
    void Update() { t += Time.deltaTime; transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World); transform.localPosition = at + Vector3.up * Mathf.Sin(t * 2f) * 0.08f; }
}

// a cutscene: black bars, the camera on whoever speaks, their words below; tap (or wait) to go on
public class AHCine : MonoBehaviour
{
    public static AHCine I; public static bool Active { get { return I != null; } }
    AHGame g; AHStory.Line[] lines; int i = -1; float t; Action done; string title;
    Text txt, who, hint; RectTransform top, bot; Vector3 camPos; Quaternion camRot; bool tapWas = true;

    public static void Play(AHGame g, string title, AHStory.Line[] lines, Action done)
    {
        if (I != null) Destroy(I.gameObject);
        if (lines == null || lines.Length == 0) { if (done != null) done(); return; }
        var go = new GameObject("Cutscene"); I = go.AddComponent<AHCine>(); I.g = g; I.lines = lines; I.done = done; I.title = title;
        if (g.ui != null) { g.ui.ShowWork(false); }
        I.Hide(true);
        I.Build(); I.camPos = g.cam.transform.position; I.camRot = g.cam.transform.rotation; I.Next();
    }

    static Font font;
    void Build()
    {
        var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 500;
        var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1600, 900); sc.matchWidthOrHeight = 0.5f;
        if (font == null) { try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } if (font == null) try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        top = Bar(new Vector2(0, 1)); bot = Bar(new Vector2(0, 0));
        who = Label(bot, new Vector2(0.5f, 1f), new Vector2(0, -20), 30, new Color(1f, 0.8f, 0.4f), FontStyle.Bold);
        txt = Label(bot, new Vector2(0.5f, 1f), new Vector2(0, -62), 28, Color.white, FontStyle.Normal);
        hint = Label(bot, new Vector2(0.5f, 0f), new Vector2(0, 12), 18, new Color(1f, 1f, 1f, 0.5f), FontStyle.Italic); hint.rectTransform.sizeDelta = new Vector2(600, 26);
        hint.text = "tap to continue";
        if (title != null) { var tt = Label(top, new Vector2(0.5f, 0f), new Vector2(0, 50), 32, new Color(1f, 0.8f, 0.4f), FontStyle.Bold); tt.text = title; }
    }
    RectTransform Bar(Vector2 anchor)
    {
        var go = new GameObject("Bar"); go.transform.SetParent(transform, false); var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, anchor.y); rt.anchorMax = new Vector2(1, anchor.y); rt.pivot = new Vector2(0.5f, anchor.y); rt.sizeDelta = new Vector2(0, 170);
        go.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f); return rt;
    }
    Text Label(RectTransform parent, Vector2 anchor, Vector2 pos, int size, Color c, FontStyle st)
    {
        var go = new GameObject("T"); go.transform.SetParent(parent, false); var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, anchor.y > 0.5f ? 1f : 0f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(1400, 80);
        var t = go.AddComponent<Text>(); t.font = font; t.fontSize = size; t.color = c; t.fontStyle = st; t.alignment = TextAnchor.UpperCenter; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.raycastTarget = false;
        return t;
    }

    void Next()
    {
        i++;
        if (i >= lines.Length) { End(); return; }
        t = 0f; who.text = lines[i].who; txt.text = lines[i].text;
        AHSound.Play("whoosh");
    }
    // bystanders (villagers, your pet and helpers) and the HUD step aside while the story speaks
    readonly List<GameObject> hidden = new List<GameObject>(); Canvas hud;
    void Hide(bool on)
    {
        if (on)
        {
            foreach (var n in FindObjectsByType<AHNpc>(FindObjectsSortMode.None)) if (n != null && n.gameObject.activeSelf) { n.gameObject.SetActive(false); hidden.Add(n.gameObject); }
            foreach (var a in AHComp.Allies) if (a != null && a.gameObject.activeSelf) { a.gameObject.SetActive(false); hidden.Add(a.gameObject); }
            var pet = GameObject.Find("Pet"); if (pet != null && pet.activeSelf) { pet.SetActive(false); hidden.Add(pet); }
            hud = g.ui != null ? g.ui.GetComponent<Canvas>() : null; if (hud != null) hud.enabled = false;
        }
        else
        {
            foreach (var go in hidden) if (go != null) go.SetActive(true);
            hidden.Clear(); if (hud != null) hud.enabled = true;
        }
    }
    void OnDestroy() { Hide(false); }

    void End()
    {
        I = null; var d = done; Destroy(gameObject);
        if (d != null) d();
    }

    // the camera: on whoever speaks (the hero for the hero), a little to the side, at face height
    public static bool Drive(Camera cam)
    {
        if (I == null || I.g == null || I.g.player == null) return false;
        var g = I.g; Transform f = g.player.transform; float h = 1.6f;
        if (I.i >= 0 && I.i < I.lines.Length)
        {
            var who = GameObject.Find("Story " + I.lines[I.i].who); if (who != null) { f = who.transform; h = 1.55f * who.transform.lossyScale.y; }
        }
        Vector3 head = f.position + Vector3.up * h;
        Vector3 fwd = f.forward; fwd.y = 0; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
        Vector3 want = head + fwd * 2.5f + Vector3.Cross(Vector3.up, fwd) * 0.9f + Vector3.up * 0.1f;
        Quaternion rot = Quaternion.LookRotation((head - want).normalized);
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f);
        I.camPos = Vector3.Lerp(I.camPos, want, k); I.camRot = Quaternion.Slerp(I.camRot, rot, k);
        cam.transform.position = I.camPos; cam.transform.rotation = I.camRot;
        return true;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        bool tap = AHInput.Pointers().Count > 0 || AHInput.TalkKey();
        if (tap && !tapWas && t > 0.45f) Next();
        else if (t > 7f) Next();
        tapWas = tap;
    }
}

public partial class AHUI
{
    public void OpenStory() { wkMode = "story"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderStory(AHPlayer p)
    {
        wkTitle.text = "The Ashen King";
        wkHint.text = AHStory.Status(p);
        var rows = new List<Action<int>>();
        for (int i = 0; i < AHStory.Chapters.Length; i++)
        {
            var c = AHStory.Chapters[i]; bool done = p.prog.storyCh > i, now = p.prog.storyCh == i;
            string line = done ? "Done" : now ? AHStory.Status(p) : "Opens at level " + c.lvl;
            rows.Add(s => Row(s, "Chapter " + (Array.IndexOf(AHStory.Chapters, c) + 1) + " · " + c.name, done ? new Color(0.6f, 0.9f, 0.5f) : now ? new Color(1f, 0.8f, 0.45f) : new Color(1f, 1f, 1f, 0.5f), line, now || done ? c.task : ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
