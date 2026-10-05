// Ashen Hollow: companions, as in the web game (v67): pets that follow you and level up (Fetch, Forager, Guard,
// Mend, Lucky, Loyal), mounts you ride that get faster the more ground they cover, and sellswords you hire at
// a tavern (Sir Aldric the knight taunts and soaks hits, Wren the ranger shoots, Sister Mae heals).
// Tables: companions.json PETS, MOUNTS, MERCS, SEAT, PET_FLY, PET_SKILLS, PET_KIND. Models: Resources/AH/Models/Comp.
using System.Collections.Generic;
using UnityEngine;

public static class AHComp
{
    public static Dictionary<string, object> Pets { get { return AHDB.Table("companions", "PETS"); } }
    public static Dictionary<string, object> Mounts { get { return AHDB.Table("companions", "MOUNTS"); } }
    public static Dictionary<string, object> Mercs { get { return AHDB.Table("companions", "MERCS"); } }
    public const int PetMax = 20, MountMax = 10, PartyMax = 2;
    static long[] petAt, mountAt;
    static long[] PetAt { get { if (petAt == null) { petAt = new long[PetMax + 2]; for (int L = 1; L <= PetMax; L++) petAt[L + 1] = petAt[L] + Mathf.RoundToInt(160f * Mathf.Pow(L, 1.45f) + 100f); } return petAt; } }
    static long[] MountAt { get { if (mountAt == null) { mountAt = new long[MountMax + 2]; for (int L = 1; L <= MountMax; L++) mountAt[L + 1] = mountAt[L] + Mathf.RoundToInt(9000f * Mathf.Pow(L, 1.35f)); } return mountAt; } }
    static int LvFrom(long xp, long[] at, int max) { int L = 1; while (L < max && xp >= at[L + 1]) L++; return L; }

    public static int PetLv(AHPlayer p, string k) { long x; p.petXp.TryGetValue(k ?? "", out x); return LvFrom(x, PetAt, PetMax); }
    public static int MountLv(AHPlayer p, string k) { long x; p.mountXp.TryGetValue(k ?? "", out x); return LvFrom(x, MountAt, MountMax); }
    public static float MountBonus(AHPlayer p, string k) { int L = MountLv(p, k); return 1f + 0.015f * (L - 1) + (L >= MountMax ? 0.05f : 0f); }
    public static float MountSpeed(AHPlayer p) { return p.mounted && p.mountSel != null ? (float)AHJson.N(AHJson.O(Mounts, p.mountSel), "speed", 1.5) * MountBonus(p, p.mountSel) : 1f; }
    public static float Seat(string k) { return (float)AHJson.N(AHDB.Table("companions", "SEAT"), k, 0.64); }
    public static string PetName(string k) { return AHJson.S(AHJson.O(Pets, k), "name", k); }
    public static string MountName(string k) { return AHJson.S(AHJson.O(Mounts, k), "name", k); }
    public static string MercName(string k) { return AHJson.S(AHJson.O(Mercs, k), "name", k); }
    public static bool Flies(string k) { var l = AHDB.List("companions", "PET_FLY"); return l != null && l.Contains(k); }

    // the XP bar of a pet or mount: done / needed this level
    public static void Bar(AHPlayer p, string k, bool mount, out long done, out long need, out int L)
    {
        long x; (mount ? p.mountXp : p.petXp).TryGetValue(k, out x);
        var at = mount ? MountAt : PetAt; int max = mount ? MountMax : PetMax;
        L = LvFrom(x, at, max); done = x - at[L]; need = L >= max ? 0 : at[L + 1] - at[L];
    }

    // web petK: how strong a pet skill is (0 = not learnt yet)
    public static float PetK(AHPlayer p, string skill)
    {
        string k = p.pet; if (k == null) return 0f;
        int L = PetLv(p, k), need = skill == "fetch" ? 3 : skill == "forage" ? 5 : skill == "guard" ? 8 : skill == "mend" ? 12 : 16;
        if (L < need) return 0f;
        var kind = AHJson.A(AHDB.Table("companions", "PET_KIND"), k);
        string ks = kind != null && kind.Count > 0 ? (string)kind[0] : null;
        return (L >= 20 ? 1.5f : 1f) * (ks == skill ? 1.5f : ks == "all" ? 1.25f : 1f);
    }
    public static float Fetch(AHPlayer p) { float k = PetK(p, "fetch"); return 1f + (k > 0f ? (0.05f + PetLv(p, p.pet) * 0.0075f) * k : 0f); }

    public static void PetGain(AHGame g, float xp)
    {
        var p = g.player; string k = p.pet; if (k == null || xp <= 0f) return;
        int b = PetLv(p, k); long x; p.petXp.TryGetValue(k, out x); p.petXp[k] = x + Mathf.RoundToInt(xp); int a = PetLv(p, k);
        if (a > b)
        {
            string sk = null; var sks = AHDB.List("companions", "PET_SKILLS");
            if (sks != null) foreach (var o in sks) { var r = o as List<object>; int l = (int)(double)r[0]; if (l > b && l <= a) { sk = (string)r[1]; break; } }
            g.ui.Banner(PetName(k) + " is level " + a, sk != null ? "New pet skill: " + sk : "Pet level up");
        }
        g.MarkDirty();
    }

    // a beast fell with your pet out (web petOnKill)
    public static void OnKill(AHGame g, AHMob m)
    {
        var p = g.player; if (p.pet == null || m.add) return;
        PetGain(g, m.type.xp * AHLoot.KillXpMult(m.type.lvl, p.level) * 0.35f);
    }

    // web petForage: now and then an extra material when you gather
    public static void Forage(AHGame g, string id)
    {
        var p = g.player; float k = PetK(p, "forage"); if (k <= 0f || AHItems.Get(id) == null) return;
        if (Random.value < (0.05f + PetLv(p, p.pet) * 0.008f) * k && p.bag.Add(id) && g.ui != null)
            g.ui.Float(p.transform.position + Vector3.up * 2.4f, "+1 " + AHItems.Get(id).name + " (pet)", new Color(0.79f, 0.9f, 0.66f));
    }

    // web petTreat: share food with your pet for XP
    public static void Treat(AHGame g)
    {
        var p = g.player; if (p.pet == null) return;
        string best = null; long bv = -1;
        foreach (var id in p.bag.order)
        {
            var it = AHItems.Get(id); if (it == null || p.bag.Count(id) <= 0) continue;
            if (!(it.IsFood || id.StartsWith("raw_")) || it.meal) continue;
            long v = Value(id); if (v > bv) { bv = v; best = id; }
        }
        if (best == null) { g.ui.Toast("You have no food to share. Raw meat and fish work best."); return; }
        p.bag.Take(best);
        PetGain(g, Mathf.Max(25f, Mathf.Max(1, bv) * 6f));
        g.ui.Toast(PetName(p.pet) + " gobbles the " + AHItems.Get(best).name.ToLowerInvariant() + ".");
    }

    // web valueOf (items.json VALUE, worked out by the web game for every item)
    public static long Value(string id)
    {
        var it = AHItems.Get(id);
        if (it != null && it.enh > 0) return (long)System.Math.Round(Value(it.baseId) * (1 + 0.25 * it.enh));
        return (long)AHJson.N(AHDB.Table("items", "VALUE"), id, 10);
    }

    // ---------- the pet that follows you ----------
    public static void SpawnPet(AHGame g)
    {
        var old = GameObject.Find("Pet"); if (old != null) Object.Destroy(old);
        var p = g.player; if (p.pet == null) return;
        var go = new GameObject("Pet");
        go.transform.position = p.transform.position - p.transform.forward * 1.2f;
        var f = go.AddComponent<AHPetFollow>(); f.Setup(g, p.pet);
    }

    // ---------- riding (web mountUp / dismount) ----------
    public static bool CanRide(AHGame g, bool loud)
    {
        var p = g.player; string why = null;
        if (p.mountSel == null) why = "You have no mount yet. Visit the Varrow Stables.";
        else if (p.dead || g.Dark || AHDungeon.IsDungeon(AHGame.AreaId)) why = "You can’t ride here.";
        else foreach (var m in g.mobs) if (!m.dead && m.Chasing && (m.transform.position - p.transform.position).magnitude < 16f) { why = "You can’t mount up in the middle of a fight."; break; }
        if (why != null && loud) g.ui.Toast(why);
        return why == null;
    }

    public static void ToggleRide(AHGame g) { if (g.player.mounted) Dismount(g, false); else Mount(g); }

    public static void Mount(AHGame g)
    {
        var p = g.player; if (p.mounted || !CanRide(g, true)) return;
        AHGather.Cancel();
        var holder = new GameObject("Mount");
        holder.transform.SetParent(p.transform, false);
        if (p.mountSel == AHPhoenix.Id) AHPhoenix.Build(holder.transform, p);   // built from flame, not loaded
        else { AHAnim a; AHModel.Spawn(holder.transform, "Comp/mount_" + p.mountSel, 0f, false, 0f, out a); if (a != null && a.HasClips) holder.AddComponent<AHMountGait>().Setup(a, p); else if (a != null) a.Dispose(); }
        foreach (var c in holder.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        AHModel.SetShadows(holder);
        p.mountGo = holder; p.mounted = true; p.seatY = p.mountSel == AHPhoenix.Id && holder.GetComponentInChildren<AHPhoenixRide>() != null ? AHPhoenixRide.Seat + 0.4f : Seat(p.mountSel);
        AHFx.Ring(p.transform.position, 0.4f, 1.8f, new Color(0.85f, 0.75f, 0.55f, 0.8f), 0.5f);
        if (p.mountSel == AHPhoenix.Id) { AHSpark.Burst(p.transform.position + Vector3.up, new Color(1f, 0.55f, 0.15f), 60, 5f, 0.8f, 0.2f, 1.5f); AHSpark.Ring(p.transform.position + Vector3.up * 0.2f, new Color(1f, 0.75f, 0.3f), 40, 6f); }
        g.ui.RefreshRide();
    }

    public static void Dismount(AHGame g, bool quiet)
    {
        var p = g.player; if (!p.mounted) return;
        p.mounted = false; p.seatY = 0f;
        if (p.mountGo != null) Object.Destroy(p.mountGo); p.mountGo = null;
        if (!quiet) AHFx.Ring(p.transform.position, 0.4f, 1.6f, new Color(0.85f, 0.75f, 0.55f, 0.8f), 0.5f);
        g.ui.RefreshRide();
    }

    // mounts learn from the road (web petTick): XP for the ground covered
    static float mountAcc;
    public static void RideTick(AHGame g, float dist)
    {
        var p = g.player; if (!p.mounted || p.mountSel == null) return;
        mountAcc += dist / AHDB.S;
        if (mountAcc < 200f) return;
        string k = p.mountSel; int b = MountLv(p, k); long x; p.mountXp.TryGetValue(k, out x); p.mountXp[k] = x + (long)mountAcc; mountAcc = 0f;
        int a = MountLv(p, k);
        if (a > b) g.ui.Banner(MountName(k) + " is level " + a, "Now " + Mathf.RoundToInt((MountBonus(p, k) - 1f) * 100f) + "% faster");
        g.MarkDirty();
    }

    // ---------- sellswords (web spawnAllies) ----------
    public static readonly List<AHAlly> Allies = new List<AHAlly>();
    public static void SpawnAllies(AHGame g)
    {
        foreach (var a in Allies) if (a != null) Object.Destroy(a.gameObject);
        Allies.Clear();
        var p = g.player;
        for (int i = 0; i < p.party.Count; i++)
        {
            var go = new GameObject(MercName(p.party[i]));
            go.transform.position = g.Resolve(p.transform.position - p.transform.forward * (2f + (i / 2) * 1.6f) + p.transform.right * (i % 2 == 0 ? 1.2f : -1.2f), 0.35f);
            var a = go.AddComponent<AHAlly>(); a.Setup(g, p.party[i], i);
            Allies.Add(a);
        }
    }
}

// a pet: trots (or flies) behind your shoulder, bites what you fight (Guard) and heals you (Mend)
public class AHPetFollow : MonoBehaviour
{
    AHGame g; string kind; bool fly; GameObject model; float phase, guardT = 2f, mendT = 8f; AHAnim anim;

    public void Setup(AHGame game, string k)
    {
        g = game; kind = k; fly = AHComp.Flies(k);
        AHAnim a;
        if (k == "drake")
        {
            // the baby dragon: the great wyrm's model, small and ember red
            model = AHModel.Spawn(transform, "Web/mDragonHQ", 1.25f, true, 0f, out a);
            fly = false;   // it scampers along on the ground beside you
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;
                foreach (var m in mats) foreach (var pr in new[] { "baseColorFactor", "_BaseColor" }) if (m.HasProperty(pr)) m.SetColor(pr, m.GetColor(pr) * new Color(1.7f, 0.8f, 0.62f));
                r.materials = mats;
            }
        }
        else model = AHModel.Spawn(transform, "Comp/pet_" + k, 0f, false, 0f, out a);
        if (a != null && a.HasClips) anim = a; else if (a != null) a.Dispose();
        AHModel.SetShadows(model);
    }
    void OnDestroy() { if (anim != null) anim.Dispose(); }

    void Update()
    {
        var p = g.player; if (p == null || model == null) return;
        float dt = Time.deltaTime;
        Vector3 goal = p.transform.position - p.transform.forward * 1.5f + p.transform.right * 0.95f;
        Vector3 d = goal - transform.position; d.y = 0;
        if (d.magnitude > 20f) { transform.position = goal; d = Vector3.zero; }
        float sp = d.magnitude > 0.32f ? Mathf.Min(d.magnitude * 3.2f, 13f) : 0f;
        if (sp > 0f)
        {
            Vector3 np = transform.position + d.normalized * sp * dt;
            transform.position = fly ? np : g.Resolve(np, 0.2f);
            transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(d), 1f - Mathf.Exp(-dt * 8f));
        }
        else transform.rotation = Quaternion.Slerp(transform.rotation, p.transform.rotation, 1f - Mathf.Exp(-dt * 3f));
        phase += dt;
        float y = fly ? 1.95f + Mathf.Sin(phase * 2.3f) * 0.15f : sp > 0f && anim == null ? Mathf.Abs(Mathf.Sin(phase * 12f)) * 0.13f : 0f;
        model.transform.localPosition = new Vector3(0f, y, 0f);
        if (anim != null)
        {
            // the animated pets (fox kits, baby dragon): walk, run, sit about, or keep their wings beating
            if (fly) { if (!anim.Play("Fly", true, sp > 0f ? 1.2f : 0.8f)) anim.Play("Idle", true); }
            else if (sp > 4.5f) { float r = Mathf.Clamp(sp / 6f, 0.8f, 1.5f); if (!anim.Play("Gallop", true, r) && !anim.Play("Run", true, r)) anim.Play("Walk", true); }
            else if (sp > 0f) anim.Play("Walk", true, Mathf.Clamp(sp / 2.2f, 0.7f, 1.6f));
            else anim.Play("Idle", true);
            anim.Tick(dt);
        }
        if (kind == "slime" || kind == "toad" || kind == "pumpkin_slime") model.transform.localScale = new Vector3(1f + Mathf.Sin(phase * 6f) * 0.07f, 1f - Mathf.Sin(phase * 6f) * 0.07f, 1f);
        if (p.dead) return;
        // Guard: a bite every 2 s at whatever is hunting you
        guardT -= dt;
        if (guardT <= 0f)
        {
            guardT = 2f; float k = AHComp.PetK(p, "guard");
            if (k > 0f)
            {
                AHMob best = null; float bd = 260f * AHDB.S;
                foreach (var m in g.mobs) { if (m.dead || !m.Chasing) continue; float dm = (m.transform.position - transform.position).magnitude; if (dm < bd) { bd = dm; best = m; } }
                if (best != null) { best.Hurt(Mathf.Max(1, Mathf.RoundToInt(p.Damage() * 0.22f * (AHComp.PetLv(p, kind) / 10f) * k)), null, true); AHFx.Pop(best.transform.position + Vector3.up, 1f, new Color(1f, 0.82f, 0.5f)); }
            }
        }
        // Mend: a little healing every 8 s while you fight
        mendT -= dt;
        if (mendT <= 0f)
        {
            mendT = 8f; float k = AHComp.PetK(p, "mend");
            if (k > 0f && p.InFight && p.hp < p.maxHp) p.Heal(p.maxHp * 0.025f * k);
        }
    }
}

// a sellsword (web updateAllies / allyHit / hurtAlly): follows you, fights what hunts you, goes down and gets back up
public class AHAlly : MonoBehaviour
{
    AHGame g; public string id; int slot; object d;
    public float hp, maxHp; public bool dead;
    float cd, healCd, reviveAt, phase;
    GameObject model;

    public void Setup(AHGame game, string k, int i)
    {
        g = game; id = k; slot = i; d = AHJson.O(AHComp.Mercs, k);
        maxHp = hp = Mathf.Round((float)AHJson.N(d, "hp", 150) + g.player.level * 4f);
        model = BuildRig(k);   // a rigged, animated sellsword in proper kit
        if (model == null) { AHAnim a; model = AHModel.Spawn(transform, "Comp/merc_" + k, 0f, false, 0f, out a); if (a != null) a.Dispose(); }
        AHModel.SetShadows(model);
        baseRot = model.transform.localRotation;
    }

    AHAnim anim; Quaternion baseRot; bool walking; float atkPose;
    void OnDestroy() { if (anim != null) anim.Dispose(); }
    // the three sellswords, dressed like the hero: body, hair, kit drawn from real items, and their KayKit weapons
    GameObject BuildRig(string k)
    {
        string body, wm, main, off; uint hairC; var parts = new System.Collections.Generic.List<string>(); string[] kit;
        if (k == "knight") { body = "qMaleRanger"; parts.AddRange(new[] { "qHead_Male", "qHair_Beard", "qEyebrows_Regular" }); hairC = 0x6a4a2a; wm = "Knight_t"; main = "1H_Sword"; off = "Round_Shield"; kit = new[] { "steel_helm", "iron_pauldrons", "steel_plate", "iron_gauntlets", "iron_greaves", "iron_boots", "royal_cape" }; }
        else if (k == "ranger") { body = "qFemalePeasant"; parts.AddRange(new[] { "qHead_Female", "qHair_Long", "qEyebrows_Female" }); hairC = 0x8a4a22; wm = "Rogue_t"; main = "2H_Crossbow"; off = null; kit = new[] { "verdant_hood", "verdant_mantle", "verdant_vest", "boar_gloves", null, "deer_boots", "verdant_quiver" }; }
        else { body = "qFemalePeasant"; parts.AddRange(new[] { "qHead_Female", "qHair_Buns", "qEyebrows_Female" }); hairC = 0xd8c8a0; wm = "Mage_t"; main = "1H_Wand"; off = "Spellbook"; kit = new[] { "holy_circlet", "holy_mantle", "holy_robe", "holy_gloves", null, null, "holy_cape" }; }
        AHAnim a; var rig = AHPeople.Assemble(transform, body, parts, g, out a);
        if (rig == null) return null;
        anim = a; if (anim != null) { anim.alias = AHPeople.HeroAlias; anim.Play("Idle", true); }
        Color hc = AHGame.Hex((int)hairC);
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name.Contains("Hood") && k != "ranger") r.enabled = false;
            var mats = r.materials; bool ch = false;
            foreach (var m in mats) if (m != null && m.name.Contains("Hair")) { foreach (var pr in new[] { "baseColorFactor", "_BaseColor" }) if (m.HasProperty(pr)) m.SetColor(pr, hc.linear * 1.6f); ch = true; }
            if (ch) r.materials = mats;
        }
        AHPeople.Arm(rig, wm, main, off, g);
        string[] slots = { "head", "shoulders", "chest", "hands", "legs", "feet", "cape" };
        AHWardrobe.DressWith(rig, transform, s => { int i = System.Array.IndexOf(slots, s); return i >= 0 && AHItems.Get(kit[i] ?? "") != null ? kit[i] : null; }, () => walking);
        return rig;
    }

    public string Name { get { return AHJson.S(d, "name", id); } }
    float Range { get { return (float)AHJson.N(d, "range", 50) * AHDB.S; } }
    float Speed { get { return (float)AHJson.N(d, "speed", 175) * AHDB.S; } }

    float Dmg()
    {
        var p = g.player;
        return ((float)AHJson.N(d, "dmg", 6) + p.level * 0.55f + p.Skill("attack") * 0.35f) * Random.Range(0.85f, 1.15f);
    }

    public void Hurt(float raw)
    {
        if (dead) return;
        int hit = Mathf.Max(1, Mathf.RoundToInt(raw * (float)AHJson.N(d, "armor", 0.8)));
        hp -= hit;
        if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2f, "-" + hit, new Color(1f, 0.6f, 0.54f));
        if (hp <= 0f)
        {
            hp = 0f; dead = true; reviveAt = Time.time + 20f;
            if (anim != null) anim.Play("Death_A", false, 1f, true); else model.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, 90f);
            g.ui.Toast(Name + " is down! Back on their feet in 20 seconds.", 3f);
        }
    }

    AHMob Target()
    {
        var p = g.player; AHMob best = null; float bd = 420f * AHDB.S;
        foreach (var m in g.mobs) { if (m.dead || !m.Chasing) continue; float dm = (m.transform.position - p.transform.position).magnitude; if (dm < bd) { bd = dm; best = m; } }
        if (best == null && p.target != null && !p.target.dead && p.target.provoked && (p.target.transform.position - p.transform.position).magnitude < 450f * AHDB.S) best = p.target;
        return best;
    }

    void Update()
    {
        var p = g.player; if (p == null || model == null) return;
        float dt = Time.deltaTime;
        if (anim != null) anim.Tick(dt);
        cd -= dt; healCd -= dt;
        if (dead)
        {
            if (Time.time < reviveAt) return;
            dead = false; hp = Mathf.Round(maxHp * 0.6f);
            transform.position = g.Resolve(p.transform.position - p.transform.forward * 1.6f, 0.35f);
            model.transform.localRotation = baseRot; if (anim != null) anim.Play("Idle", true, 1f, true);
            AHFx.Pillar(transform.position, 0.6f, 2.4f, new Color(0.6f, 1f, 0.6f, 0.5f), 0.8f);
            g.ui.Toast(Name + " is back in the fight.");
        }
        if ((transform.position - p.transform.position).magnitude > 26f) transform.position = g.Resolve(p.transform.position - p.transform.forward * 2f + p.transform.right * (slot == 0 ? 1.2f : -1.2f), 0.35f);
        if (Time.time - p.LastHurt > 5f) hp = Mathf.Min(maxHp, hp + dt * 3f);
        // the cleric heals whoever is lowest (you or the other sellsword)
        if (id == "cleric" && healCd <= 0f && !p.dead)
        {
            float fp = p.hp / Mathf.Max(1f, p.maxHp); AHAlly low = null; float fl = fp;
            foreach (var b in AHComp.Allies) if (b != null && !b.dead && b.hp / b.maxHp < fl) { fl = b.hp / b.maxHp; low = b; }
            Vector3 at = low != null ? low.transform.position : p.transform.position;
            if (fl < 0.7f && (at - transform.position).magnitude < 330f * AHDB.S)
            {
                float n = Mathf.Round(18f + p.level * 0.6f); healCd = 3.2f;
                if (low == null) p.Heal(n); else { low.hp = Mathf.Min(low.maxHp, low.hp + n); g.ui.Float(at + Vector3.up * 2.2f, "+" + n, new Color(0.6f, 0.9f, 0.48f)); }
                AHFx.Pillar(at, 0.5f, 2.2f, new Color(1f, 0.9f, 0.55f, 0.5f), 0.6f);
            }
        }
        Vector3 goal = Vector3.zero; bool go = false;
        var t = p.dead ? null : Target();
        if (t != null)
        {
            Vector3 to = t.transform.position - transform.position; to.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(to), 1f - Mathf.Exp(-dt * 10f));
            float dist = to.magnitude - t.type.radius;
            if (dist > Range) { goal = t.transform.position; go = true; }
            else if (cd <= 0f)
            {
                if (anim != null) anim.Play(id == "knight" ? "1H_Melee_Attack" : "Shoot", false, 1.4f, true);
                atkPose = 0.6f;
                if (id == "knight") { cd = 1.1f; t.Hurt(Mathf.RoundToInt(Dmg()), null); if (!t.dead && !t.type.elite) t.tgt = this; }
                else if (id == "ranger") { cd = 1.3f; int dm = Mathf.RoundToInt(Dmg()); AHFx.Shoot(transform.position + Vector3.up * 1.3f, t, new Color(0.94f, 0.9f, 0.82f), 0.12f, 28f, m => m.Hurt(dm, null)); }
                else { cd = 1.9f; int dm = Mathf.RoundToInt(Dmg()); AHFx.Shoot(transform.position + Vector3.up * 1.4f, t, new Color(1f, 0.89f, 0.54f), 0.22f, 18f, m => m.Hurt(dm, null)); }
                phase = 0f;
            }
        }
        else
        {
            goal = p.transform.position - p.transform.forward * 2.2f + p.transform.right * (slot == 0 ? 1.36f : -1.36f);
            Vector3 off = goal - transform.position; off.y = 0;
            go = off.magnitude > 1f;
            if (!go) transform.rotation = Quaternion.Slerp(transform.rotation, p.transform.rotation, 1f - Mathf.Exp(-dt * 4f));
        }
        bool moving = false;
        if (go)
        {
            Vector3 dir = goal - transform.position; dir.y = 0;
            float far = dir.magnitude > 10f ? 1.4f : 1f, sp = Speed * AHComp.MountSpeed(p) * far;
            transform.position = g.Resolve(transform.position + dir.normalized * Mathf.Min(sp * dt, dir.magnitude), 0.35f);
            transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(dir), 1f - Mathf.Exp(-dt * 8f));
            moving = true;
        }
        walking = moving; atkPose -= dt;
        if (anim != null) { if (atkPose <= 0f) anim.Play(moving ? "Running_A" : "Idle", true); }
        else { phase += dt * (moving ? 10f : 0f); model.transform.localPosition = new Vector3(0f, moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.08f : 0f, 0f); }
    }
}

// a living mount (the Quaternius horses and wolves): stands and breathes, gallops while you ride on
public class AHMountGait : MonoBehaviour
{
    AHAnim anim; AHPlayer p; float idleT;
    public void Setup(AHAnim a, AHPlayer who) { anim = a; p = who; anim.Play("Idle", true); }
    void Update()
    {
        if (anim == null || p == null) return;
        if (p.Moving) { idleT = 0f; float sp = 0.85f + 0.25f * Mathf.Clamp01(AHComp.MountSpeed(p) - 1.5f); if (!anim.Play("Gallop", true, sp)) anim.Play("Run", true, sp); }
        else
        {
            idleT += Time.deltaTime;
            // now and then it lowers its head or shifts its weight
            string idle = idleT > 9f && idleT < 13f ? (anim.Has("Idle_Headlow") ? "Idle_Headlow" : "Idle_2_HeadLow") : idleT > 20f && idleT < 23f ? "Idle_2" : "Idle";
            if (idleT > 23f) idleT = 0f;
            if (!anim.Play(idle, true)) anim.Play("Idle", true);
        }
        anim.Tick(Time.deltaTime);
    }
    void OnDestroy() { if (anim != null) anim.Dispose(); }
}
