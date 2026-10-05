// Ashen Hollow: a beast of the meadow. Wanders near home, chases when it sees you (if it is the
// chasing kind), runs away if it is a deer, goes home if led too far, respawns after a while.
// Spells can burn, poison, slow, root or stun it.
using UnityEngine;

public class AHMob : MonoBehaviour
{
    public AHMobType type;
    public float hp;
    public bool dead;
    public bool skinned;            // web: m.skinned
    public bool provoked;           // web: the hero hit it first (peaceful Artisans can still be fought back)
    public bool add;                // a boss's helper: it never comes back once killed
    public AHAlly tgt;              // a sellsword that taunted it (web m.tgt)
    public bool Chasing { get { return !dead && state == State.Chase; } }
    public bool enraged;            // a boss below 30%: moves faster
    public Vector3 Home { get { return home; } }
    public float height;            // how tall the model stands (metres), for the camera
    // a special attack: stand still while the warning circle fills
    public void Windup(float t) { windup = t; lungeHit = false; atkCd = Mathf.Max(atkCd, t); }
    public void Engage() { state = State.Chase; calmT = 0f; }
    float corpseT, respawnT;        // web: the body stays 40 s (1.2 s once skinned), then back in 18 s (or d.respawn)
    bool away;                      // night beasts by day, and anything waiting to come back
    bool lungeHit;
    public bool CanSkin { get { return dead && !type.noSkin && !away && !skinned && corpseT > 0f && model.activeSelf; } }

    AHGame g;
    [System.NonSerialized] public AHAnim anim;
    GameObject model;
    Vector3 home, wanderTo, baseLocal;
    enum State { Wander, Chase, Flee, Return }
    State state = State.Wander;
    float wanderWait, atkCd, windup, fleeT, deadT, phase, calmT;
    bool hasClips;

    // conditions
    float burnT, burnDps, poisonT, poisonDps, slowT, rootT, stunT, dotTick;
    AHPlayer dotBy;

    public static AHMob Create(AHGame game, AHMobType t, Vector3 pos)
    {
        var go = new GameObject(t.name);
        go.transform.position = pos;
        var m = go.AddComponent<AHMob>();
        m.g = game; m.type = t; m.home = pos; m.wanderTo = pos; m.hp = t.hp;
        bool wolf = t.model == "Wolf_t";
        // web MOB_SKINS: a few beasts use a real model (size in metres, by body length), the rest are built from blocks
        if (t.id == AHRaid.Boss) AHRaid.EnsureDef();
        // the new animal models (Quaternius CC0, reshaped and recoloured for Ashen Hollow): monsters.json BEASTS
        var beast = AHJson.O(AHDB.Table("monsters", "BEASTS"), t.id);
        string beastPath = null;
        string proc = beast != null ? AHJson.S(beast, "proc") : null;   // built in code (the sea serpent)
        if (beast != null && proc == null)
        {
            var files = AHJson.A(beast, "files");
            if (files != null && files.Count > 0) beastPath = "Beasts/" + (string)files[Random.Range(0, files.Count)];
            else if (AHJson.Has(beast, "path")) beastPath = AHJson.S(beast, "path");   // one of the web game's own models
            if (beastPath == null || Resources.Load<GameObject>("AH/Models/" + beastPath) == null) { beast = null; beastPath = null; }
        }
        string skId = beast != null ? null : SkinBase(t.id);
        // the area file names the model the web game used (some links between beasts are only made as it runs)
        if (beast == null && skId == null && t.model != null && t.model.StartsWith("Web/"))
        {
            var all = AHDB.Table("monsters", "MOB_SKINS") as System.Collections.Generic.Dictionary<string, object>;
            if (all != null) foreach (var kv in all) if (AHJson.S(kv.Value, "file") == t.model.Substring(4)) { skId = kv.Key; break; }
        }
        var sk = beast != null ? beast : skId != null ? AHJson.O(AHDB.Table("monsters", "MOB_SKINS"), skId) : null;
        m.skin = sk;
        float size = sk != null ? (float)AHJson.N(sk, "size", 1.8) * (float)AHJson.N(AHJson.O(AHDB.Mobs, t.id), "scale", 1) : wolf ? 1.8f : 0f;
        bool byLen = sk != null ? AHJson.B(sk, "long") : true;
        // the people among the beasts (bandits, pirates, cultists, the tomb priest) get rigged, dressed bodies
        float personH = 1.8f * (float)AHJson.N(AHJson.O(AHDB.Mobs, t.id), "scale", 1);
        var person = sk == null ? AHMobPeople.Build(go.transform, t, game, out m.anim) : null;
        if (person != null) m.model = person;
        else if (proc == "serpent") { m.anim = null; m.model = AHSerpent.Build(go.transform, size, t.id); }
        else m.model = AHModel.Spawn(go.transform, beastPath ?? t.model, size, byLen, wolf ? game.wolfYawFix : (m.yawOff = sk != null ? (float)AHJson.N(sk, "yaw", 0) : 0f), out m.anim, sk != null ? AHJson.A(sk, "split") : null);
        if (sk != null)
        {
            Tint(m.model, t.id, sk);
            if (AHJson.B(sk, "golem")) GolemSkin(m.model, t.id);
            if (AHJson.S(sk, "rig") == "serpent") m.model.AddComponent<AHSerpentRig>();   // bones moved in code
            // the game asks for Idle / Walk / Gallop / Attack / HitReact / Death; this model's own names (web MOB_SKINS)
            var al = new System.Collections.Generic.Dictionary<string, string>();
            string idle = AHJson.S(sk, "idle"), move = AHJson.S(sk, "move"), hit = AHJson.S(sk, "hit"), death = AHJson.S(sk, "death");
            var atk = AHJson.A(sk, "attack");
            if (idle != null) al["Idle"] = idle;
            if (move != null) { al["Walk"] = move; al["Gallop"] = move; }
            string walk = AHJson.S(sk, "walk"); if (walk != null) al["Walk"] = walk;
            if (hit != null) al["HitReact"] = hit;
            if (death != null) al["Death"] = death;
            if (atk != null && atk.Count > 0) al["Attack"] = (string)atk[0];
            if (m.anim != null) m.anim.alias = al;
            m.moveRate = (float)AHJson.N(sk, "moveRate", 1);
            m.idleRate = (float)AHJson.N(sk, "idleRate", 1);
            m.hop = AHJson.B(sk, "hop"); m.flip = AHJson.B(sk, "flip"); m.floatUp = (float)AHJson.N(sk, "float", 0);
            if (AHJson.B(AHJson.O(AHDB.Mobs, t.id), "buried") && AHJson.Has(sk, "spawn")) m.sleeping = true;
        }
        m.baseLocal = m.model.transform.localPosition; m.baseScale = m.model.transform.localScale;
        m.height = person != null ? personH : AHModel.LastHeight;
        AHModel.SetShadows(m.model);
        m.hasClips = m.anim != null && m.anim.HasClips;
        m.wanderWait = Random.Range(0.5f, 4f);
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        if (t.night && !game.IsNight) m.GoAway(float.PositiveInfinity);   // web: shadow wolves only come out at night
        return m;
    }

    void OnDestroy() { if (anim != null) anim.Dispose(); }

    // ---------- web MOB_SKINS: real models for some beasts ----------
    object skin;
    float moveRate = 1f, wakeT, idleRate = 1f;
    bool hop, flip; Vector3 baseScale; float yawOff, floatUp;

    // real voices (monsters.json BEASTS "sounds": {"aggro": [...], "attack": [...], "hurt": [...], "death": [...]},
    // clips in Resources/AH/Audio): a roar when it spots you, growls as it attacks, yelps when hit, a death cry
    float voiceT;
    static readonly System.Collections.Generic.Dictionary<string, AudioClip> clipCache = new System.Collections.Generic.Dictionary<string, AudioClip>();
    void Voice(string kind, float chance)
    {
        if (skin == null || AHSound.Muted || Time.time < voiceT || Random.value > chance) return;
        var list = AHJson.A(AHJson.O(skin, "sounds"), kind); if (list == null || list.Count == 0) return;
        string n = (string)list[Random.Range(0, list.Count)];
        AudioClip c; if (!clipCache.TryGetValue(n, out c)) { c = Resources.Load<AudioClip>("AH/Audio/" + n); clipCache[n] = c; }
        if (c == null) return;
        var go = new GameObject("Voice"); go.transform.position = transform.position + Vector3.up;
        var a = go.AddComponent<AudioSource>(); a.clip = c; a.spatialBlend = 0.85f; a.minDistance = 4f; a.maxDistance = 45f; a.rolloffMode = AudioRolloffMode.Linear;
        a.volume = AHSound.SfxVol * 0.9f; a.pitch = Random.Range(0.92f, 1.08f) * (height < 1.2f ? 1.15f : 1f); a.Play();
        Destroy(go, c.length / a.pitch + 0.1f);
        voiceT = Time.time + Mathf.Min(c.length, 2.5f) * 0.8f;
    }   // yaw: a model turned on its body (shore crabs scuttle sideways)
    bool sleeping;

    // web msBase: follow MOBS[type].model until a type with a skin
    public static string SkinBase(string type)
    {
        var skins = AHDB.Table("monsters", "MOB_SKINS");
        string t = type; int n = 0;
        while (!AHJson.Has(skins, t) && n++ < 6) { string nx = AHJson.S(AHJson.O(AHDB.Mobs, t), "model") ?? DungeonBase(t); if (nx == null) break; t = nx; }
        return AHJson.Has(skins, t) ? t : null;
    }

    // dungeon beasts are built on another beast (events.json DUNGEONS mobs / mini / boss: [id, base, ...])
    static System.Collections.Generic.Dictionary<string, string> dBase;
    static string DungeonBase(string id)
    {
        if (dBase == null)
        {
            dBase = new System.Collections.Generic.Dictionary<string, string>();
            var l = AHDB.List("events", "DUNGEONS");
            if (l != null)
                foreach (var d in l)
                {
                    var rows = new System.Collections.Generic.List<object>();
                    var ms = AHJson.A(d, "mobs"); if (ms != null) rows.AddRange(ms);
                    rows.Add(AHJson.A(d, "mini")); rows.Add(AHJson.A(d, "boss"));
                    foreach (var o in rows) { var r = o as System.Collections.Generic.List<object>; if (r != null && r.Count > 1 && r[0] is string && r[1] is string) dBase[(string)r[0]] = (string)r[1]; }
                }
        }
        string b; return dBase.TryGetValue(id, out b) ? b : null;
    }

    // web msElement: golems wear one of five elements
    static string Element(string type)
    {
        switch (type) { case "golem": case "emberwarden": case "magmatitan": return "Fire"; case "obsidiangolem": return "Shadow"; }
        var d = AHJson.O(AHDB.Mobs, type);
        object c = AHJson.Has(d, "glow") ? ((System.Collections.Generic.Dictionary<string, object>)d)["glow"] : AHJson.Has(d, "tint") ? ((System.Collections.Generic.Dictionary<string, object>)d)["tint"] : null;
        if (c == null) return "Earth";
        float h, sat, l; Color.RGBToHSV(AHDB.Col(c, Color.white), out h, out sat, out l);
        if (l > 0.9f || (h > 0.5f && h < 0.62f)) return "Ice";
        if (h > 0.68f && h < 0.9f) return "Shadow";
        if (h >= 0.1f && h < 0.18f && sat > 0.5f) return "Light";
        if (h < 0.1f || h > 0.95f) return "Fire";
        return "Earth";
    }

    static void GolemSkin(GameObject model, string type)
    {
        string el = Element(type);
        var ta = Resources.Load<Texture2D>("AH/Models/Web/gol_" + el + "_a");
        var te = Resources.Load<Texture2D>("AH/Models/Web/gol_" + el + "_e");
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials;
            foreach (var mt in mats)
            {
                if (ta != null) { if (mt.HasProperty("baseColorTexture")) mt.SetTexture("baseColorTexture", ta); if (mt.HasProperty("_BaseMap")) mt.SetTexture("_BaseMap", ta); }
                if (te != null)
                {
                    if (mt.HasProperty("emissiveTexture")) mt.SetTexture("emissiveTexture", te);
                    if (mt.HasProperty("emissiveFactor")) mt.SetColor("emissiveFactor", Color.white * (el == "Earth" ? 0.5f : 0.9f));
                    if (mt.HasProperty("_EmissionMap")) { mt.SetTexture("_EmissionMap", te); mt.EnableKeyword("_EMISSION"); }
                }
            }
            r.materials = mats;
        }
    }

    // web: a skinned beast is tinted toward its colour (tintAs, or the beast's own tint), 55% of the way
    static void Tint(GameObject model, string id, object sk)
    {
        if (AHJson.B(sk, "noTint")) return;   // the new animals carry their own colours
        var d = AHJson.O(AHDB.Mobs, id);
        object tc = null;
        if (AHJson.Has(sk, "tintAs") && !AHJson.Has(d, "tint")) tc = ((System.Collections.Generic.Dictionary<string, object>)sk)["tintAs"];
        else if (!AHJson.B(sk, "golem") && AHJson.Has(d, "tint")) tc = ((System.Collections.Generic.Dictionary<string, object>)d)["tint"];
        if (tc == null) return;
        Color c = Color.Lerp(Color.white, AHDB.Col(tc, Color.white).linear, (float)AHJson.N(d, "tintK", 0.55));
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials;
            foreach (var mt in mats)
            {
                if (mt.HasProperty("baseColorFactor")) mt.SetColor("baseColorFactor", mt.GetColor("baseColorFactor") * c);
                else if (mt.HasProperty("_BaseColor")) mt.SetColor("_BaseColor", mt.GetColor("_BaseColor") * c);
            }
            r.materials = mats;
        }
    }

    public string Label { get { return type.name + " · lv " + type.lvl; } }
    public bool Stunned { get { return stunT > 0f; } }

    public void Hurt(int dmg, AHPlayer by, bool quiet = false)
    {
        if (dead) return;
        // web hitMob: beasts above your level shrug off a little, ones below take a little more
        if (by != null) provoked = true;
        if (by != null && !quiet) dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * Mathf.Clamp(1f - (type.lvl - by.level) * 0.03f, 0.55f, 1.15f) * (1f + AHPower.VsBonus(by, type.id))));
        if (by != null && !quiet && (stunT > 0f || rootT > 0f) && AHEvo.Pass(by, "frozen") > 0f) dmg = Mathf.RoundToInt(dmg * (1f + AHEvo.Pass(by, "frozen")));
        hp -= dmg;
        if (by != null) { AHEvo.Leech(by, dmg); if (!quiet) { AHSound.Play("hit"); Voice("hurt", 0.5f); AHSpark.Burst(transform.position + Vector3.up * Mathf.Max(0.6f, type.radius * 0.8f), new Color(1f, 0.75f, 0.4f), 7, 3.2f, 0.35f, 0.1f, 0.8f); } }
        if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 1.6f, dmg.ToString(), quiet ? new Color(1f, 0.6f, 0.3f) : new Color(1f, 0.92f, 0.5f));
        if (hp <= 0f) { Die(by); return; }
        if (type.flee) { state = State.Flee; fleeT = 3f; }
        else if (state != State.Return) { state = State.Chase; calmT = 0f; }
        if (!quiet && hasClips && windup <= 0f) anim.Play("HitReact", false, 1.3f, true);
    }

    public void AddStatus(AHStatus s, float time, float dps, AHPlayer by)
    {
        if (dead) return;
        dotBy = by;
        if (by != null && (s == AHStatus.Burn || s == AHStatus.Poison)) dps *= 1f + AHEvo.Pass(by, "burn");
        switch (s)
        {
            case AHStatus.Burn: burnT = time; burnDps = Mathf.Max(burnDps, dps); AHFx.Pop(transform.position + Vector3.up * 0.8f, 1.2f, new Color(1f, 0.5f, 0.15f)); break;
            case AHStatus.Poison: poisonT = time; poisonDps = Mathf.Max(poisonDps, dps); AHFx.Pop(transform.position + Vector3.up * 0.8f, 1.2f, new Color(0.45f, 0.9f, 0.3f)); break;
            case AHStatus.Slow: slowT = time; break;
            case AHStatus.Root: rootT = time; AHFx.Ring(transform.position, type.radius + 0.4f, type.radius + 0.6f, new Color(0.4f, 0.8f, 0.3f), time); break;
            case AHStatus.Stun: stunT = time; windup = 0f; AHFx.Ring(transform.position + Vector3.up * 1.4f, 0.3f, 0.5f, new Color(1f, 0.9f, 0.4f), time); break;
        }
    }

    // Vanish: beasts chasing you forget you for a while
    public void LoseTrack(Vector3 from, float within)
    {
        if (dead || state != State.Chase) return;
        if ((transform.position - from).magnitude > within) return;
        state = State.Wander; windup = 0f; wanderTo = transform.position;   // they lose you, but don't go home and heal
    }

    void Die(AHPlayer by)
    {
        dead = true; hp = 0f; windup = 0f; skinned = false; corpseT = 40f; respawnT = 0f; voiceT = 0f; Voice("death", 1f);
        burnT = poisonT = slowT = rootT = stunT = 0f;
        if (hasClips && anim.Has("Death")) anim.Play("Death", false, 1f, true);
        else if (flip)
        {
            // crabs, turtles and toads end up on their backs, legs in the air
            if (hasClips) anim.Hold("Idle", 0.3f);
            model.transform.localScale = baseScale;
            model.transform.localRotation = Quaternion.Euler(0, g.ModelYaw + yawOff, 180f);
            model.transform.localPosition = baseLocal + Vector3.up * height * 0.85f;
        }
        else if (floatUp > 0f) { model.transform.localPosition = baseLocal; model.transform.localRotation = Quaternion.Euler(0, g.ModelYaw + yawOff, 90f); }   // the wraith drops
        else if (model.GetComponent<AHSerpentBody>() == null) model.transform.localRotation = Quaternion.Euler(0, g.ModelYaw, 90f);   // the serpent slumps by itself
        g.OnKill(type.id);
        AHSpark.Burst(transform.position + Vector3.up * 0.6f, new Color(0.9f, 0.85f, 0.75f, 0.7f), 18, 2f, 0.7f, 0.2f, 1.2f);
        if (by != null) AHLoot.OnKill(g, this, by);
        AHDungeon.OnMobDown(g, this);
    }

    // skinned: the body goes 1.2 s later
    public void Skinned() { skinned = true; corpseT = Mathf.Min(corpseT, 1.2f); }

    // off the map until 'wait' runs out (infinite = until night for night beasts)
    // timed events (rares, the world boss): held away outside their window
    [System.NonSerialized] public bool evHold;
    public void EvHide() { if (!dead || !away) GoAway(float.PositiveInfinity); }
    public bool Away { get { return dead && away; } }

    void GoAway(float wait)
    {
        dead = true; away = true; skinned = true; corpseT = 0f; respawnT = wait; state = State.Wander;
        model.SetActive(false);
    }

    void Revive()
    {
        dead = false; away = false; skinned = false; provoked = false; hp = type.hp; state = State.Wander;
        transform.position = home;
        model.SetActive(true);
        model.transform.localRotation = Quaternion.Euler(0, g.ModelYaw + (type.model == "Wolf_t" ? g.wolfYawFix : AHMobPeople.Is(type.id) ? g.heroYawFix : yawOff), 0);
        model.transform.localPosition = baseLocal; if (baseScale != Vector3.zero) model.transform.localScale = baseScale;
        if (hasClips) anim.Play("Idle", true, 1f, true);
    }

    void Conditions(float dt)
    {
        burnT -= dt; poisonT -= dt; slowT -= dt; rootT -= dt; stunT -= dt; calmT -= dt;
        if (burnT <= 0f) burnDps = 0f;
        if (poisonT <= 0f) poisonDps = 0f;
        if (burnDps + poisonDps <= 0f) return;
        dotTick -= dt;
        if (dotTick > 0f) return;
        dotTick = 1f;
        int d = Mathf.Max(1, Mathf.RoundToInt(burnDps + poisonDps));
        Hurt(d, dotBy, true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (anim != null) anim.Tick(dt);
        if (dead)
        {
            bool nightOk = (!type.night || g.IsNight) && !evHold;
            if (!away)
            {
                corpseT -= dt;
                if (corpseT <= 0f && add) { g.mobs.Remove(this); Destroy(gameObject); return; }
                if (corpseT <= 0f) GoAway(nightOk ? type.respawn : float.PositiveInfinity);
                return;
            }
            if (float.IsPositiveInfinity(respawnT)) { if (nightOk) respawnT = 0f; return; }
            respawnT -= dt;
            if (respawnT <= 0f && nightOk) Revive();
            return;
        }
        // web msBuried: lying still until you come near (or hit it), then it rises
        if (sleeping || wakeT > 0f)
        {
            var pl = g.player;
            if (sleeping)
            {
                bool near = pl != null && !pl.dead && (pl.transform.position - transform.position).magnitude < (float)AHJson.N(skin, "wake", 300) * AHDB.S || hp < type.hp;
                if (!near) { if (anim != null) anim.Hold(AHJson.S(skin, "rest"), (float)AHJson.N(skin, "restT", 0)); return; }
                sleeping = false;
                string sp = AHJson.S(skin, "spawn");
                if (anim != null && anim.Play(sp, false, 1f, true)) wakeT = Mathf.Max(0.5f, anim.Length(sp)); else wakeT = 0.5f;
            }
            wakeT -= dt;
            if (wakeT > 0f) return;
        }
        // web: at dawn the shadow wolves that are not hunting you melt away
        if (type.night && !g.IsNight && state != State.Chase) { AHFx.Pop(transform.position + Vector3.up * 0.6f, 1.2f, new Color(1f, 0.5f, 0.2f, 0.6f)); GoAway(float.PositiveInfinity); return; }
        atkCd -= dt;
        Conditions(dt);
        if (dead) return;
        AHPlayer p = g.player;
        if (tgt != null && (tgt.dead || (tgt.transform.position - transform.position).magnitude > 24f)) tgt = null;
        Vector3 pos = transform.position, toP = (tgt != null ? tgt.transform.position : p.transform.position) - pos;
        toP.y = 0;
        float dist = toP.magnitude;
        float reach = type.radius + 24f * AHDB.S;   // web: d.r + 14 + 10
        float spd = 0f;
        Vector3 dir = Vector3.zero;
        bool canSee = !p.dead && !p.Stealthed && calmT <= 0f;

        if (state == State.Wander && type.aggro > 0f && canSee && dist < type.aggro && !g.Peaceful(this) && !g.InTown(p.transform.position))
        {
            state = State.Chase; Voice("aggro", 1f);
            // web: the rest of the pack nearby joins in
            foreach (var o in g.mobs) if (o != this && !o.dead && o.type == type && o.state == State.Wander && (o.transform.position - pos).magnitude < 300f * AHDB.S) o.state = State.Chase;
        }
        switch (state)
        {
            case State.Chase:
                if (p.dead || (pos - home).magnitude > 16f || g.InTown(p.transform.position)) { state = State.Return; break; }
                if (stunT > 0f) break;
                if (windup > 0f)
                {
                    // a lunge: dodge out of the red circle in time (web: 13 damage)
                    windup -= dt;
                    if (windup <= 0f && lungeHit && dist < reach + 0.75f) { if (tgt != null) tgt.Hurt(13f); else p.Hurt(13f, this); }
                }
                else if (!p.dead && AHDungeon.Think(this, dt, dist)) { if (hasClips) anim.Play("Attack", false, 0.8f, true); }
                else if (dist > reach) { dir = toP / Mathf.Max(dist, 1e-4f); spd = type.speed * (enraged ? 1.35f : 1f); }
                else if (atkCd <= 0f && type.dmg > 0)
                {
                    if (type.lunge && Random.value < 0.35f)
                    {
                        windup = 0.55f; atkCd = 1.8f; lungeHit = true;
                        AHFx.Ring(pos + transform.forward * 1.2f, 1.76f, 1.76f, new Color(1f, 0.16f, 0.1f, 0.5f), 0.55f, true);
                    }
                    else { if (tgt != null) tgt.Hurt(type.dmg * Random.Range(0.8f, 1.2f)); else p.Hurt(type.dmg * Random.Range(0.8f, 1.2f), this); atkCd = type.atkCd; lungeHit = false; }
                    if (hasClips)
                    {
                        var atk = skin != null ? AHJson.A(skin, "attack") : null;
                        if (atk != null && atk.Count > 0 && anim.alias != null) anim.alias["Attack"] = (string)atk[Random.Range(0, atk.Count)];
                        anim.Play("Attack", false, 1.2f, true);
                    }
                    Voice("attack", 0.45f);
                }
                if (dist > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(toP), 1f - Mathf.Exp(-dt * 10f));
                break;
            case State.Flee:
                fleeT -= dt;
                dir = -toP / Mathf.Max(dist, 1e-4f); spd = type.speed * 1.15f;
                if (fleeT <= 0f || dist > 18f) { state = State.Wander; home = pos; }
                break;
            case State.Return:
                Vector3 back = home - pos; back.y = 0;
                hp = Mathf.Min(type.hp, hp + type.hp * 0.5f * dt);
                if (back.magnitude < 0.5f) state = State.Wander;
                else { dir = back.normalized; spd = type.speed * 1.2f; }
                break;
            default:
                wanderWait -= dt;
                Vector3 w = wanderTo - pos; w.y = 0;
                if (w.magnitude > 0.3f) { dir = w.normalized; spd = type.speed * 0.35f; }
                else if (wanderWait <= 0f)
                {
                    Vector2 r = Random.insideUnitCircle * 4f;
                    wanderTo = home + new Vector3(r.x, 0, r.y);
                    wanderWait = Random.Range(2f, 6f);
                }
                break;
        }
        if (stunT > 0f || rootT > 0f) spd = 0f;
        if (slowT > 0f) spd *= 0.5f;

        bool moving = spd > 0f;
        if (moving)
        {
            transform.position = g.Resolve(pos + dir * spd * dt, type.radius * 0.8f);
            if (state != State.Chase) transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(dir), 1f - Mathf.Exp(-dt * 8f));
        }

        // looks
        if (hasClips)
        {
            if (stunT > 0f) anim.Play("Idle", true, 0.3f);
            else if (windup <= 0f && atkCd < 1.0f) {
                if (!anim.Play(moving ? (spd > type.speed * 0.6f ? "Gallop" : "Walk") : "Idle", true, moving ? Mathf.Clamp(spd / 4f, 0.7f, 1.6f) * moveRate : idleRate) && skin != null)
                {
                    // no walk or idle clip (golems): hold the first attack's first frame and sway
                    var atk = AHJson.A(skin, "attack");
                    if (atk != null && atk.Count > 0) anim.Hold((string)atk[0], 0f);
                    model.transform.localRotation = Quaternion.Euler(0, g.ModelYaw, moving ? Mathf.Sin(Time.time * 4f) * 3.5f : 0f);
                }
            }
        }
        else if (floatUp > 0f)
        {
            // wraiths and the risen dead: drift above the ground, bobbing and swaying, leaning into the chase and
            // rearing back before they strike
            phase += dt;
            float bob = Mathf.Sin(phase * 1.8f + home.x) * 0.12f, lean = moving ? 12f : 0f, rear = windup > 0f ? -14f : 0f;
            model.transform.localPosition = baseLocal + Vector3.up * (floatUp + bob) + (windup > 0f ? model.transform.localRotation * Vector3.forward * 0.35f : Vector3.zero);
            model.transform.localRotation = Quaternion.Euler(lean + rear + Mathf.Sin(phase * 1.1f) * 3f, g.ModelYaw + yawOff, Mathf.Sin(phase * 0.8f + home.z) * 4f);
        }
        else if (hop)
        {
            // toads: hop along in arcs, nose up in the air, squashing as they land; at rest the throat and body breathe
            if (moving) phase += dt * 11f; else phase = 0f;
            float arc = moving ? Mathf.Abs(Mathf.Sin(phase * 0.5f)) : 0f;
            float lunge = windup > 0f ? height * 0.35f : 0f;
            model.transform.localPosition = baseLocal + Vector3.up * arc * height * 0.45f + model.transform.localRotation * Vector3.forward * lunge * 0.5f;
            model.transform.localRotation = Quaternion.Euler(moving ? -arc * 14f : windup > 0f ? -10f : 0f, g.ModelYaw, 0f);
            float sq = moving ? (1f - arc) * (1f - arc) * 0.16f : 0f, br = moving ? 0f : Mathf.Sin(Time.time * 2.6f + home.x * 1.7f + home.z) * 0.03f;
            model.transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + sq * 0.5f - br * 0.4f, 1f - sq + br, 1f + sq * 0.5f - br * 0.4f));
        }
        else
        {
            // the block-built beasts trot with a little bounce
            phase += dt * (moving ? spd * 3.2f : 0f);
            float bob = moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.06f : 0f;
            float lunge = windup > 0f ? 0.15f : 0f;
            model.transform.localPosition = baseLocal + new Vector3(0, bob, 0) + Vector3.forward * lunge;
        }
    }
}
