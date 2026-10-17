// Ashen Hollow: Dawnrest Isle, the training island (web TUT_STEPS). Every new hero starts here and learns the game one
// step at a time, following a glowing light: walk, look around, talk, chop, mine, cook, the bag, gear, fight, spells,
// dodging red circles, potions and the map. Ferryman Oskar rows you to Ashen Hollow when you are done.
// Nothing on the island counts: no XP of any kind (class, skills, mastery, season), no quests, no Monster book pages,
// no loot or money from the training toads. What you were carrying when you arrived is exactly what you leave with
// (anything gathered or made on the island stays there), plus one fixed starter gift the first time. Leaving any other
// way (logging out and changing hero, a waystone...) puts the bag back the same way. Any hero can visit again from
// Settings to practise.
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public static class AHTraining
{
    public const string Area = "tutorial";
    const float X0 = 175000f;   // the island's corner in web units (web TUTZ)
    const string KStep = "ah_train_step", KSnap = "ah_train_snap", KBack = "ah_train_back", KGift = "ah_train_gift";
    public const int Done = 99;

    public static bool Exists { get { return Resources.Load<TextAsset>("AH/Areas/" + Area) != null; } }
    public static bool OnIsland { get { return AHGame.AreaId == Area; } }
    public static bool NoXp { get { return OnIsland; } }
    public static int Step { get { return AHPrefs.GetInt(KStep, Done); } set { AHPrefs.SetInt(KStep, value); } }
    public static bool Active { get { return OnIsland && Step < Steps.Length; } }

    // web units on the island (web TL2) -> world
    public static Vector3 At(AHGame g, float u, float v) { return g.W((X0 + u) * AHDB.S, v * AHDB.S); }

    // ---------- the steps ----------
    public class StepDef { public string id, text, textTouch; public float u = -1f, v; public float reach = 4.5f; }
    public static readonly StepDef[] Steps =
    {
        new StepDef { id = "move",   text = "Walk to the glowing light (WASD).", textTouch = "Walk to the glowing light with the stick on the left.", u = 760, v = 760, reach = 4f },
        new StepDef { id = "look",   text = "Look around: drag with the mouse, scroll to zoom.", textTouch = "Look around: drag a finger on the right side of the screen. Pinch to zoom." },
        new StepDef { id = "talk",   text = "Talk to Guide Elara: walk up to her and press E.", textTouch = "Talk to Guide Elara: walk up to her and tap the big button.", u = 820, v = 900 },
        new StepDef { id = "chop",   text = "Chop a tree in the grove: stand next to one and press E.", textTouch = "Chop a tree in the grove: stand next to one and tap the big button.", u = 820, v = 470, reach = 12f },
        new StepDef { id = "mine",   text = "Mine one of the copper rocks.", textTouch = "Mine one of the copper rocks with the big button.", u = 1200, v = 520, reach = 7f },
        new StepDef { id = "cook",   text = "Cook the raw trout Elara gave you at her campfire.", u = 960, v = 1010, reach = 4f },
        new StepDef { id = "bag",    text = "Open your bag (B) to see everything you carry.", textTouch = "Tap BAG to see everything you carry." },
        new StepDef { id = "gear",   text = "Equip the training weapon Elara gave you: open the bag, tap it and press Equip." },
        new StepDef { id = "fight",  text = "Defeat 3 training toads in the pen.", textTouch = "Defeat 3 training toads in the pen: tap ATTACK.", u = 1520, v = 1180, reach = 9f },
        new StepDef { id = "spell",  text = "Cast a spell (keys 1-4). Attack spells need an enemy nearby.", textTouch = "Cast a spell: tap one of the round buttons around ATTACK. Attack spells need an enemy nearby." },
        new StepDef { id = "dodge",  text = "Red circles mean danger! Dodge (Space) or run out before one fills.", textTouch = "Red circles mean danger! Tap DODGE or run out before one fills.", u = 1660, v = 700, reach = 6f },
        new StepDef { id = "potion", text = "You’re hurt! Drink a health potion (H).", textTouch = "You’re hurt! Tap the red HP button to drink a health potion." },
        new StepDef { id = "map",    text = "Open the map (M).", textTouch = "Open the map: tap the minimap in the corner." },
        new StepDef { id = "ferry",  text = "Training complete! Talk to Ferryman Oskar at the dock to sail to Ashen Hollow.", u = 2240, v = 850, reach = 4f },
    };
    static readonly string[] ArtisanSkip = { "fight", "spell", "dodge", "potion" };   // web ART_SKIP: Artisans keep the peace
    public static StepDef Cur { get { int s = Step; return s >= 0 && s < Steps.Length ? Steps[s] : null; } }
    public static string Text(StepDef s) { return Application.isMobilePlatform && s.textTouch != null ? s.textTouch : s.text; }
    public static Vector3? Target(AHGame g) { var s = Cur; if (!Active || s == null || s.u < 0f) return null; return At(g, s.u, s.v); }

    // ---------- going there ----------
    // the creator's Begin: a brand new hero sails from Dawnrest Isle
    public static void NewHero(AHGame g)
    {
        if (!Exists) return;
        AHPrefs.DeleteKey(KBack);
        Begin(g);
    }
    // from Settings: any hero can train again (they come back where they were)
    public static void Visit(AHGame g)
    {
        if (!Exists) { g.ui.Toast("Dawnrest Isle is not in this build."); return; }
        if (OnIsland) { g.ui.Toast("You are on Dawnrest Isle."); return; }
        var p = g.player.transform.position; Vector2 m = g.ToWeb(p);
        AHPrefs.SetString(KBack, AHGame.AreaId + "|" + m.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + m.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Begin(g);
    }
    static void Begin(AHGame g)
    {
        AHPrefs.SetString(KSnap, Snap(g.player.bag));
        Step = 0; StepReset();
        g.Travel(Area, (X0 + 420f) * AHDB.S, 900f * AHDB.S, 0f);
    }

    // ---------- leaving ----------
    // talking to Oskar once the training is done; or skipping it
    public static void Finish(AHGame g, bool skipped)
    {
        var p = g.player;
        Restore(p);
        Step = Done;
        bool back = AHPrefs.HasKey(KBack);
        if (!back && AHPrefs.GetInt(KGift, 0) == 0) Gift(p);
        p.Recalc(); p.bag.Touch();
        string to = "meadow"; float x = 0f, z = 0f; bool have = false;
        if (back)
        {
            var parts = AHPrefs.GetString(KBack).Split('|');
            if (parts.Length == 3 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z)) { to = parts[0]; have = true; }
            AHPrefs.DeleteKey(KBack);
        }
        if (!have)
        {
            var ta = Resources.Load<TextAsset>("AH/Areas/meadow"); if (ta == null) return;
            var d = JsonUtility.FromJson<AHWorldData>(ta.text); x = d.spawn.x; z = d.spawn.z;
        }
        AHPrefs.SetInt("ah_train_arrive", skipped ? 2 : 1);   // the welcome after the crossing
        g.Travel(to, x, z, 0f);
    }

    // the first time: the training weapon, a little money, potions and bread (the same for everyone)
    static void Gift(AHPlayer p)
    {
        AHPrefs.SetInt(KGift, 1);
        string w = Weapon(p); if (w != null) p.bag.Add(w);
        p.bag.money += (p.path == "artisan" ? 90 : 30) * AHDB.CU;
        p.bag.Add("hp_potion", 2); p.bag.Add("bread", p.path == "artisan" ? 5 : 2);
    }

    // the bag as it was when the hero arrived: "money \t id*n;id*n \t slot=id;slot=id"
    static string Snap(AHBag b)
    {
        var sb = new StringBuilder(); sb.Append(b.money).Append('\t');
        foreach (var id in b.order) { int n = b.Count(id); if (n > 0) sb.Append(id).Append('*').Append(n).Append(';'); }
        sb.Append('\t');
        foreach (var kv in b.gear) if (!string.IsNullOrEmpty(kv.Value)) sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
        return sb.ToString();
    }
    static void Restore(AHPlayer p)
    {
        string s = AHPrefs.GetString(KSnap, null); AHPrefs.DeleteKey(KSnap);
        if (string.IsNullOrEmpty(s)) return;
        var parts = s.Split('\t'); if (parts.Length < 3) return;
        var b = p.bag; long money;
        if (!long.TryParse(parts[0], out money)) return;
        b.money = money; b.inv.Clear(); b.order.Clear(); b.gear.Clear();
        foreach (var e in parts[1].Split(';')) { int i = e.LastIndexOf('*'); int n; if (i > 0 && int.TryParse(e.Substring(i + 1), out n) && n > 0) { string id = e.Substring(0, i); b.inv[id] = n; b.order.Add(id); } }
        foreach (var e in parts[2].Split(';')) { int i = e.IndexOf('='); if (i > 0) b.gear[e.Substring(0, i)] = e.Substring(i + 1); }
    }

    // ---------- each area load ----------
    public static void Setup(AHGame g)
    {
        var p = g.player; if (p == null) return;
        // left the island some other way: the bag goes back to how it was, and the training counts as done
        if (!OnIsland && AHPrefs.HasKey(KSnap))
        {
            Restore(p); Step = Done;
            if (!AHPrefs.HasKey(KBack) && AHPrefs.GetInt(KGift, 0) == 0) Gift(p);
            AHPrefs.DeleteKey(KBack); p.Recalc(); p.bag.Touch(); g.MarkDirty();
        }
        if (OnIsland && Step >= Steps.Length && !AHPrefs.HasKey(KSnap)) { AHPrefs.SetString(KSnap, Snap(p.bag)); Step = 0; }   // came by another road
        StepReset();
        if (OnIsland) AHTrainingIsland.Dress(g);
        int arr = AHPrefs.GetInt("ah_train_arrive", 0);
        if (arr != 0 && !OnIsland && g.ui != null)
        {
            AHPrefs.DeleteKey("ah_train_arrive");
            g.ui.Toast(p.path == "artisan" ? "Welcome to Ashen Hollow! Guild Clerk Mina by the waystone will set you on the Artisan’s Road." : "Welcome to Ashen Hollow! Captain Mara in the plaza has your first quest.", 5f);
        }
    }

    // ---------- watching the steps ----------
    static float yaw0, look, beamT, dodgeNext, stepAt; static int bag0, kills, dodged; static bool talked, cooked;
    static Vector3 dodgeAt; static float dodgeEnd = -1f; static string weapon;

    // training items go in even when the bag is full (they stay behind on the island anyway)
    static void Give(AHBag b, string id, int n = 1)
    {
        if (AHItems.Get(id) == null || n <= 0) return;
        if (b.Add(id, n)) return;
        b.inv[id] = b.Count(id) + n; if (!b.order.Contains(id)) b.order.Add(id); b.Touch();
    }
    public static string TrainingWeapon(AHPlayer p) { return Weapon(p); }
    static int Has(AHBag b, params string[] ids) { int n = 0; foreach (var id in ids) n += b.Count(id); return n; }
    static string Weapon(AHPlayer p)
    {
        foreach (var id in new[] { "bronze_sword", "bronze_mace", "bronze_dagger", "bronze_staff", "oak_staff", "oak_bow", "bronze_warhammer" })
            if (AHItems.Get(id) != null && AHItems.CanUse(id, p.cls.id)) return id;
        return null;
    }

    static void StepReset() { look = 0f; kills = 0; dodged = 0; talked = false; cooked = false; dodgeEnd = -1f; dodgeNext = 0f; bag0 = -1; stepAt = Time.time; yaw0 = float.NaN; }

    // what happens as a step begins (web s.start)
    static void StartStep(AHGame g)
    {
        var p = g.player; var s = Cur; StepReset(); if (s == null) return;
        switch (s.id)
        {
            case "cook": if (p.bag.Count("raw_trout") <= 0) Give(p.bag, "raw_trout"); bag0 = Has(p.bag, "trout"); break;
            case "chop": bag0 = Has(p.bag, "logs", "pine_logs"); break;
            case "mine": bag0 = Has(p.bag, "copper_ore", "tin_ore"); break;
            case "gear":
                weapon = Weapon(p); bag0 = 0;
                if (weapon == null || p.bag.Worn("weapon") == weapon) break;
                if (p.bag.Count(weapon) <= 0) Give(p.bag, weapon);
                break;
            case "potion":
                if (p.bag.Count("hp_potion") < 2) Give(p.bag, "hp_potion", 2 - p.bag.Count("hp_potion"));
                if (p.cls.mana && p.bag.Count("mana_potion") <= 0) Give(p.bag, "mana_potion");
                p.hp = Mathf.Min(p.hp, Mathf.Round(p.maxHp * 0.45f)); bag0 = p.bag.Count("hp_potion");
                break;
        }
        if (g.ui != null) g.ui.TrainRefresh();
    }

    public static bool Talk(AHGame g, AHNpc n)
    {
        if (!OnIsland || n == null) return false;
        if (n.npcName == "Guide Elara")
        {
            var s = Cur; talked = true;
            g.ui.Toast(s == null ? "Elara: Come back and practise any time, hero." : s.id == "talk" ? "Elara: Welcome, hero! Follow the glowing light. I’ll teach you everything you need." : "Elara: " + Text(s), 4f);
            return true;
        }
        if (n.npcName == "Ferryman Oskar")
        {
            var s = Cur;
            if (s != null && s.id != "ferry") { g.ui.Toast("Oskar: Finish your training first! Follow the glowing light.", 3f); return true; }
            g.ui.Toast("Oskar: Hop in, hero. Ashen Hollow is waiting.", 2.5f);
            Finish(g, false);
            return true;
        }
        return false;
    }

    public static void OnKill(AHGame g, AHMob m) { if (Active && m != null && m.type.id == "tutrat") { kills++; if (g.ui != null) g.ui.TrainRefresh(); } }

    public static string Prog()
    {
        var s = Cur; if (s == null) return "";
        if (s.id == "fight") return Mathf.Min(kills, 3) + "/3";
        if (s.id == "dodge") return dodged + "/2";
        return "";
    }

    // every frame while on the island (from the HUD)
    public static void Tick(AHGame g, float dt)
    {
        if (!Active) return;
        var p = g.player; if (p == null || p.dead) return;
        // Artisans skip the fighting lessons
        while (p.path == "artisan" && Cur != null && System.Array.IndexOf(ArtisanSkip, Cur.id) >= 0) { Step = Step + 1; StartStep(g); }
        var s = Cur; if (s == null) return;
        if (bag0 < 0 && (s.id == "cook" || s.id == "chop" || s.id == "mine" || s.id == "potion" || s.id == "gear")) StartStep(g);
        // the glowing light over where to go next
        var t = Target(g);
        beamT -= dt;
        if (t.HasValue && beamT <= 0f) { beamT = 1.4f; AHFx.Pillar(g.Resolve(t.Value, 0.5f), 0.9f, 7f, new Color(1f, 0.86f, 0.45f, 0.55f), 1.5f); }
        if (float.IsNaN(yaw0)) yaw0 = g.camYaw;
        look += Mathf.Abs(Mathf.DeltaAngle(yaw0, g.camYaw)); yaw0 = g.camYaw;
        bool ok = false; var b = p.bag; var pp = p.transform.position;
        switch (s.id)
        {
            case "move": ok = t.HasValue && Flat(pp, t.Value) < s.reach; break;
            case "look": ok = look > 120f; break;
            case "talk": ok = talked; break;
            case "chop": ok = Has(b, "logs", "pine_logs") > bag0; break;
            case "mine": ok = Has(b, "copper_ore", "tin_ore") > bag0; break;
            case "cook": ok = Has(b, "trout") > bag0; break;
            case "bag": ok = g.ui != null && g.ui.BagOpen; break;
            case "gear": ok = weapon == null || b.Worn("weapon") == weapon; break;
            case "fight": ok = kills >= 3; break;
            case "spell": ok = p.lastSpellAt > stepAt; break;
            case "potion": ok = b.Count("hp_potion") < bag0; break;
            case "map": ok = g.ui != null && g.ui.MapOpen; break;
            case "ferry": ok = false; break;   // Oskar finishes it
            case "dodge":
                if (dodgeEnd < 0f && Time.time > dodgeNext && t.HasValue && Flat(pp, t.Value) < 18f)
                {
                    dodgeAt = pp; dodgeEnd = Time.time + 1.6f;
                    AHDungeon.Telegraph(pp, 2.8f, 1.6f, Mathf.Max(1f, p.maxHp * 0.08f), null);
                }
                if (dodgeEnd > 0f && Time.time > dodgeEnd + 0.05f)
                {
                    if (Flat(pp, dodgeAt) > 2.8f + AHPlayer.Radius) { dodged++; if (g.ui != null) { g.ui.Float(pp + Vector3.up * 2.4f, "Dodged!", new Color(0.79f, 0.91f, 0.66f)); g.ui.TrainRefresh(); } }
                    else if (g.ui != null) g.ui.Toast("Too slow! Roll out of the circle before it fills.");
                    dodgeEnd = -1f; dodgeNext = Time.time + 1.2f;
                }
                ok = dodged >= 2;
                break;
        }
        if (!ok) return;
        Step = Step + 1;
        AHSound.Play("level");
        if (g.ui != null) g.ui.Banner("Well done!", Cur != null ? "Training " + (Step + 1) + " of " + Steps.Length : "Training complete");
        StartStep(g); g.MarkDirty();
    }
    static float Flat(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return (a - b).magnitude; }
}

// the island's buildings and props, made in the game instead of as boxes in the area model: Elara's cottage with
// barrels, crates and sacks by the door, a log fence round the toad pen, a weapon rack, a wheelbarrow by the rocks,
// and a plank pier on round posts out to Oskar's boat
public static class AHTrainingIsland
{
    static Material wood, darkWood;
    static Material Mat(Color c) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", 0.12f); m.enableInstancing = true; return m; }

    // a KayKit model, its widest side fitted to span metres, standing on the ground at p, facing toward 'face'
    static GameObject Model(Transform par, string file, float span, Vector3 p, Vector3? face = null, float yaw = 0f)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/" + file); if (pf == null) return null;
        var holder = new GameObject(file).transform; holder.SetParent(par, false);
        var go = Object.Instantiate(pf, holder, false);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { Object.Destroy(holder.gameObject); return null; }
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        float k = span / Mathf.Max(0.1f, Mathf.Max(b.size.x, b.size.z));
        go.transform.localScale = Vector3.one * k;
        go.transform.localPosition = new Vector3(-b.center.x * k, -b.min.y * k, -b.center.z * k);
        holder.position = p;
        if (face.HasValue) { Vector3 d = face.Value - p; d.y = 0f; if (d.sqrMagnitude > 0.01f) holder.rotation = Quaternion.LookRotation(d.normalized); }
        holder.rotation *= Quaternion.Euler(0f, yaw, 0f);
        AHModel.SetShadows(holder.gameObject);
        return holder.gameObject;
    }
    // a round log between two points (posts standing, rails lying)
    static void Log(Transform par, string name, Vector3 a, Vector3 b, float r, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(par, true);
        Vector3 d = b - a; go.transform.position = (a + b) * 0.5f;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        go.transform.localScale = new Vector3(r * 2f, d.magnitude * 0.5f, r * 2f);
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    public static void Dress(AHGame g)
    {
        if (wood == null) { wood = Mat(new Color(0.52f, 0.37f, 0.22f)); darkWood = Mat(new Color(0.33f, 0.23f, 0.14f)); }
        var root = new GameObject("Dawnrest Isle props").transform;
        var rnd = new System.Random(1717);
        System.Func<float, float, Vector3> A = (u, v) => g.Resolve(AHTraining.At(g, u, v), 0.3f);
        // Elara's cottage, its door to the road
        Vector3 hut = AHTraining.At(g, 600, 1000);
        Model(root, "kk_building_home_A_green", 6.2f, hut, AHTraining.At(g, 600, 880));
        Model(root, "kk_barrel", 0.8f, A(700, 930), null, 20f);
        Model(root, "kk_barrel", 0.8f, A(718, 952), null, 70f);
        Model(root, "kk_crate_A_big", 0.9f, A(500, 935), null, 12f);
        Model(root, "kk_sack", 0.7f, A(470, 950), null, 40f);
        Model(root, "kk_resource_lumber", 1.6f, A(760, 470), null, 30f);   // logs stacked by the grove
        Model(root, "kk_wheelbarrow", 1.4f, A(1150, 600), null, 120f);       // by the rocks
        Model(root, "kk_weaponrack", 1.4f, A(1330, 1110), A(1520, 1180));   // by the toad pen
        Model(root, "kk_flag_green", 1.0f, A(2120, 830), null, 0f);          // where the pier starts
        // the toad pen: a ring of log posts with two rails, open toward the road
        var pen = new GameObject("Fence").transform; pen.SetParent(root, false);
        Vector2 c = new Vector2(1520, 1180); float rx = 230, rv = 150; int n = 22;
        Vector3 prev = Vector3.zero; bool hasPrev = false;
        for (int i = 0; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            bool gap = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, -100f)) < 22f;   // the way in, from the road above
            Vector3 p = AHTraining.At(g, c.x + Mathf.Cos(a) * rx, c.y + Mathf.Sin(a) * rv); p.y = 0f;
            if (!gap && i < n) Log(pen, "Fence post", p, p + Vector3.up * (0.95f + (float)rnd.NextDouble() * 0.15f), 0.07f, darkWood);
            if (hasPrev && !gap)
                foreach (float h in new[] { 0.4f, 0.8f }) Log(pen, "Fence rail", prev + Vector3.up * h, p + Vector3.up * h, 0.045f, wood);
            prev = p; hasPrev = !gap;
        }
        // the pier: planks across two stringers on round posts, each plank its own shade and a little crooked
        var pier = new GameObject("Pier").transform; pier.SetParent(root, false);
        Vector3 p0 = AHTraining.At(g, 2120, 900), p1 = AHTraining.At(g, 2398, 900); p0.y = p1.y = 0f;
        Vector3 along = (p1 - p0).normalized, side = Vector3.Cross(Vector3.up, along); float len = (p1 - p0).magnitude, half = 1.5f;
        var shades = new[] { Mat(new Color(0.55f, 0.4f, 0.25f)), Mat(new Color(0.48f, 0.34f, 0.2f)), Mat(new Color(0.6f, 0.45f, 0.29f)) };
        for (float t = 0.15f; t < len; t += 0.34f)
        {
            var pl = GameObject.CreatePrimitive(PrimitiveType.Cube); pl.name = "Plank"; Object.Destroy(pl.GetComponent<Collider>());
            pl.transform.SetParent(pier, false);
            pl.transform.position = p0 + along * t + Vector3.up * 0.08f + side * (((float)rnd.NextDouble() - 0.5f) * 0.08f);
            pl.transform.rotation = Quaternion.LookRotation(side) * Quaternion.Euler(((float)rnd.NextDouble() - 0.5f) * 2f, ((float)rnd.NextDouble() - 0.5f) * 3f, 0f);
            pl.transform.localScale = new Vector3(0.3f, 0.06f, half * 2f + ((float)rnd.NextDouble() - 0.5f) * 0.12f);
            pl.GetComponent<Renderer>().sharedMaterial = shades[rnd.Next(3)];
        }
        foreach (float s in new[] { -1f, 1f })
        {
            Log(pier, "Pier beam", p0 + side * s * (half - 0.25f) + Vector3.up * 0.0f, p1 + side * s * (half - 0.25f), 0.09f, darkWood);
            for (float t = 0.4f; t < len; t += 2.6f)
            {
                Vector3 b = p0 + along * t + side * s * (half + 0.05f);
                Log(pier, "Pier post", b - Vector3.up * 1.6f, b + Vector3.up * 0.55f, 0.13f, darkWood);
            }
        }
        AHModel.SetShadows(root.gameObject);
        // the boxes the area model still draws for the hut and the pier are hidden (the old area model had them)
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (r.transform.root != root && (r.name.StartsWith("Hut ") || r.name == "Pier deck" || r.name == "Pier post")) r.enabled = false;
    }
}

public partial class AHUI
{
    // the training line at the top of the screen: which step, what to do, and a Skip button
    RectTransform trainBox; Text trainText; float trainSkipAsk = -99f; int trainShown = -2; string trainProg = null;
    public bool MapOpen { get { return WorkOpen && wkMode == "map"; } }

    void BuildTrainHud()
    {
        trainBox = Img("TrainBox", transform, white, new Vector2(0.5f, 0f), new Vector2(-40f, 128f), new Vector2(500f, 64f), new Color(0.08f, 0.06f, 0.05f, 0.85f));
        trainText = Label(trainBox, "T", "", 18, TextAnchor.MiddleLeft, new Vector2(10f, -4f), new Vector2(392f, 56f), new Color(1f, 0.93f, 0.78f));
        trainText.horizontalOverflow = HorizontalWrapMode.Wrap; trainText.resizeTextForBestFit = true; trainText.resizeTextMinSize = 12; trainText.resizeTextMaxSize = 18;
        var skip = Img("Skip", trainBox, white, new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(84f, 40f), new Color(0.35f, 0.27f, 0.2f, 1f));
        Center(Label(skip, "T", "Skip", 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(84f, 30f), Color.white));
        taps.Add(new TapBtn { rt = skip, act = () =>
        {
            if (!AHTraining.Active) return;
            if (Time.unscaledTime - trainSkipAsk > 4f) { trainSkipAsk = Time.unscaledTime; Toast("Skip the training and sail to Ashen Hollow? Tap Skip again.", 3.5f); return; }
            AHTraining.Finish(g, true);
        } });
        trainBox.gameObject.SetActive(false);
    }

    public void TrainRefresh() { trainShown = -2; }

    void TrainTick(float dt)
    {
        AHTraining.Tick(g, dt);
        if (trainBox == null) return;
        bool on = AHTraining.Active && Modal == 0 && !PhotoOn;
        if (trainBox.gameObject.activeSelf != on) trainBox.gameObject.SetActive(on);
        if (!on) return;
        string prog = AHTraining.Prog();
        if (trainShown == AHTraining.Step && prog == trainProg) return;
        trainShown = AHTraining.Step; trainProg = prog;
        var s = AHTraining.Cur;
        trainText.text = s == null ? "" : "<b>Training " + (AHTraining.Step + 1) + "/" + AHTraining.Steps.Length + "</b> · " + AHTraining.Text(s) + (prog.Length > 0 ? "  <b>" + prog + "</b>" : "") + "\n<size=13><color=#9fd49a>No XP on the island: it is all practice.</color></size>";
    }
}
