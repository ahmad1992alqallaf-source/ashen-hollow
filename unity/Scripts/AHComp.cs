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
    // Lucky: rare drops (a chance under 10%) turn up more often
    public static float Lucky(AHPlayer p) { float k = PetK(p, "lucky"); return 1f + (k > 0f ? (0.1f + PetLv(p, p.pet) * 0.01f) * k : 0f); }

    // what a pet does for you, skill by skill, in numbers (the pet window's details page): name, level it comes at,
    // learnt yet, and what it does at the pet's level now
    public static List<string[]> PetDetails(AHPlayer p, string k)
    {
        var res = new List<string[]>(); string was = p.pet; p.pet = k;   // (worked out as if this pet were out)
        int L = PetLv(p, k);
        var sks = AHDB.List("companions", "PET_SKILLS");
        if (sks != null) foreach (var o in sks)
            {
                var r = o as List<object>; int at = (int)(double)r[0]; string nm = (string)r[1]; bool got = L >= at; string what;
                switch (nm)
                {
                    case "Fetch": { float kk = PetK(p, "fetch"); what = got ? "+" + Mathf.RoundToInt((0.05f + L * 0.0075f) * kk * 100f) + "% coins from every kill" : (string)r[2]; break; }
                    case "Forager": { float kk = PetK(p, "forage"); what = got ? Mathf.RoundToInt((0.05f + L * 0.008f) * kk * 100f) + "% chance of an extra material when you gather" : (string)r[2]; break; }
                    case "Guard": { float kk = PetK(p, "guard"); what = got ? "bites what hunts you every 2 s for " + Mathf.RoundToInt(22f * (L / 10f) * kk) + "% of your damage" : (string)r[2]; break; }
                    case "Mend": { float kk = PetK(p, "mend"); what = got ? "heals " + (2.5f * kk).ToString("0.#") + "% of your health every 8 s in a fight" : (string)r[2]; break; }
                    case "Lucky": { float kk = PetK(p, "lucky"); what = got ? "rare drops come " + Mathf.RoundToInt((0.1f + L * 0.01f) * kk * 100f) + "% more often" : (string)r[2]; break; }
                    default: what = (string)r[2]; break;
                }
                res.Add(new[] { nm, at.ToString(), got ? "1" : "0", what });
            }
        p.pet = was; return res;
    }

    // pet evolution: a pet grows as it levels (Grown at 8, Elder at 15, Mythic at 20, glowing)
    public static readonly int[] EvoAt = { 8, 15, 20 };
    public static readonly string[] EvoName = { "Grown", "Elder", "Mythic" };
    public static int EvoStage(AHPlayer p, string k) { int lv = PetLv(p, k), s = 0; foreach (var a in EvoAt) if (lv >= a) s++; return s; }

    public static void PetGain(AHGame g, float xp)
    {
        var p = g.player; string k = p.pet; if (k == null || xp <= 0f) return;
        int b = PetLv(p, k); long x; p.petXp.TryGetValue(k, out x); p.petXp[k] = x + Mathf.RoundToInt(xp); int a = PetLv(p, k);
        if (a > b)
        {
            string sk = null; var sks = AHDB.List("companions", "PET_SKILLS");
            if (sks != null) foreach (var o in sks) { var r = o as List<object>; int l = (int)(double)r[0]; if (l > b && l <= a) { sk = (string)r[1]; break; } }
            g.ui.Banner(PetName(k) + " is level " + a, sk != null ? "New pet skill: " + sk : "Pet level up");
            int eb = 0, ea = 0; foreach (var at in EvoAt) { if (b >= at) eb++; if (a >= at) ea++; }
            if (ea > eb) { g.ui.Banner(PetName(k) + " evolved!", EvoName[ea - 1] + " " + PetName(k).ToLowerInvariant()); AHSound.Play("level"); SpawnPet(g); }
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
        // some mounts are made small (the wolves): grown to a riding size
        float msc = (float)AHJson.N(AHJson.O(Mounts, p.mountSel), "scale", 1); if (msc != 1f) holder.transform.localScale = Vector3.one * msc;
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
    // a healer's spell or totem also mends the sellswords nearby (pct of their own max HP)
    public static void HealParty(AHGame g, Vector3 at, float radius, float pct)
    {
        if (Allies == null || pct <= 0f) return;
        foreach (var b in Allies)
        {
            if (b == null || b.dead || b.hp >= b.maxHp) continue;
            if ((b.transform.position - at).magnitude > radius) continue;
            float n = Mathf.Round(b.maxHp * pct); b.hp = Mathf.Min(b.maxHp, b.hp + n);
            if (g.ui != null) g.ui.Float(b.transform.position + Vector3.up * 2.2f, "+" + n, new Color(0.6f, 0.9f, 0.48f));
        }
    }

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

// a pet: trots beside you, flies (only one with real flying animation), or rides on your shoulder (the birds and the
// baby dragon, made as still figures: a parrot flying about while perched, a dragon "sitting on air", looked wrong);
// bites what you fight (Guard) and heals you (Mend)
public class AHPetFollow : MonoBehaviour
{
    AHGame g; string kind; bool fly, perch; GameObject model; float phase, guardT = 2f, mendT = 8f; AHAnim anim;
    static readonly HashSet<string> Perchers = new HashSet<string> { "parrot", "owl", "lantern_owl", "drake" };
    public static float PerchH = 0.27f, PerchUp = 0.21f, PerchOut = 0.03f, PerchBack = 0.02f;
    Vector3 feetOff; Transform shoulder, upperArm; Vector3 perchVel, perchPos; float lookYaw, lookTo, lookAt, hopT = -1f, nextHop;
    // tricks (AHPetTricks): the one being done, how long the hero has stood still, when the pet may show off again
    AHPetTricks.Trick trick; float trickT, modelH = 0.5f, stillT, showOffAt; Quaternion baseRot = Quaternion.identity; Vector3 baseScale = Vector3.one, lastHeroPos;
    public void StartTrick(AHPetTricks.Trick t) { trick = t; trickT = 0f; showOffAt = Time.time + Random.Range(25f, 40f); }

    public void Setup(AHGame game, string k)
    {
        g = game; kind = k; fly = AHComp.Flies(k);
        AHAnim a;
        if (k == "drake" && Resources.Load<GameObject>("AH/Models/Comp/pet_drake_hd") == null)
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
        // flying only with real wingbeats; the still birds and the dragon ride on your shoulder
        fly = fly && anim != null && anim.Has("Fly");
        perch = !fly && Perchers.Contains(k) && model != null && !(k == "drake" && Resources.Load<GameObject>("AH/Models/Comp/pet_drake_hd") == null);
        AHModel.SetShadows(model);
        // evolved pets are bigger, and a Mythic one glows
        int stage = AHComp.EvoStage(g.player, k);
        if (stage > 0 && model != null) model.transform.localScale *= 1f + 0.12f * stage;
        if (model != null)
        {
            baseRot = model.transform.localRotation; baseScale = model.transform.localScale;
            var rs = model.GetComponentsInChildren<Renderer>(true); if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); modelH = Mathf.Clamp(b.size.y, 0.05f, 2f); }
            if (perch && modelH > 0.01f)
            {
                // shoulder-sized (an evolved one a little bigger)
                float want = PerchH * (k == "drake" ? 1.15f : 1f) * (1f + 0.08f * stage);
                model.transform.localScale *= want / modelH; baseScale = model.transform.localScale; modelH = want;
                // its feet at its origin again (the scaling moved them): measured in this pet's space
                model.transform.localPosition = Vector3.zero;
                Bounds lb = new Bounds(); bool any = false;
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) { var bb = r.bounds; var lo = transform.InverseTransformPoint(bb.min); var hi = transform.InverseTransformPoint(bb.max); var b2 = new Bounds(lo, Vector3.zero); b2.Encapsulate(hi); if (!any) { lb = b2; any = true; } else lb.Encapsulate(b2); }
                feetOff = new Vector3(-lb.center.x, -lb.min.y, -lb.center.z);
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // (a shadow on your own shoulder flickers)
                nextHop = Time.time + Random.Range(6f, 12f); lookAt = Time.time + 2f;
            }
        }
        showOffAt = Time.time + 20f;
        if (stage >= 3 && model != null) { var gl = new GameObject("MythicGlow"); gl.transform.SetParent(model.transform, false); gl.transform.localPosition = Vector3.up * 0.4f; gl.AddComponent<AHWeaponGlow>().col = new Color(1f, 0.8f, 0.35f); }
    }
    void OnDestroy() { if (anim != null) anim.Dispose(); }

    // the hero's left shoulder (the VRoid body's if there is one, else the old rig's)
    void FindShoulder(AHPlayer p)
    {
        var link = p.GetComponentInChildren<AHVRoidLink>();
        if (link != null && link.Body != null)
        {
            var an = link.Body.GetComponent<Animator>();
            if (an != null && an.isHuman) { shoulder = an.GetBoneTransform(HumanBodyBones.LeftShoulder); upperArm = an.GetBoneTransform(HumanBodyBones.LeftUpperArm); }
        }
        if (upperArm == null) foreach (var t in p.GetComponentsInChildren<Transform>(true)) { if (t.name == "clavicle_l") shoulder = t; else if (t.name == "upperarm_l") upperArm = t; }
        if (shoulder == null) shoulder = upperArm;
    }

    // perched: sits on top of the shoulder, turning its head about, now and then shuffling its feet or hopping on the
    // spot; it sways with your steps and leans as you run
    void LateUpdate()
    {
        if (!perch || g == null || g.player == null || model == null) return;
        var p = g.player; float dt = Time.deltaTime; if (dt <= 0f) return;
        if (upperArm == null || shoulder == null) FindShoulder(p);
        if (upperArm == null) { perch = false; return; }
        Vector3 up = p.transform.up, fw = p.transform.forward, right = p.transform.right;
        Vector3 at = Vector3.Lerp(shoulder.position, upperArm.position, 0.85f) + up * PerchUp - right * PerchOut - fw * PerchBack;
        // a soft follow so it rides your movement rather than being nailed on
        if (perchPos == Vector3.zero || (perchPos - at).sqrMagnitude > 4f) perchPos = at;
        perchPos = Vector3.SmoothDamp(perchPos, at, ref perchVel, 0.045f);
        transform.position = perchPos;
        // its head turns to look about, back to the front now and then
        if (Time.time > lookAt) { lookTo = Random.value < 0.4f ? 0f : Random.Range(-70f, 70f); lookAt = Time.time + Random.Range(1.2f, 4f); }
        lookYaw = Mathf.LerpAngle(lookYaw, lookTo, 1f - Mathf.Exp(-dt * 7f));
        bool run = p.Moving;
        float lean = run ? -10f : 0f;
        transform.rotation = Quaternion.LookRotation(fw, up) * Quaternion.Euler(lean, lookYaw, 0f);
        // a hop on the spot, or a shuffle
        float hop = 0f;
        if (hopT < 0f && Time.time > nextHop) { hopT = 0f; nextHop = Time.time + Random.Range(7f, 15f); }
        if (hopT >= 0f) { hopT += dt; hop = Mathf.Sin(Mathf.Clamp01(hopT / 0.35f) * Mathf.PI) * modelH * 0.18f; if (hopT > 0.35f) hopT = -1f; }
        phase += dt;
        float breathe = 1f + Mathf.Sin(phase * 2.2f) * 0.025f;
        model.transform.localPosition = model.transform.localRotation * Quaternion.Inverse(baseRot) * feetOff + new Vector3(0f, hop, 0f);
        model.transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(phase * (run ? 9f : 1.3f)) * (run ? 4f : 1.5f), 0f, Mathf.Sin(phase * (run ? 4.5f : 0.9f)) * (run ? 5f : 2f));
        model.transform.localScale = new Vector3(baseScale.x * breathe, baseScale.y / breathe, baseScale.z * breathe);
    }

    void Update()
    {
        var p = g.player; if (p == null || model == null) return;
        float dt = Time.deltaTime;
        if (perch) { PetJobs(p, dt); return; }   // (placed in LateUpdate, after the body has moved)
        Vector3 goal = p.transform.position - p.transform.forward * 0.3f + p.transform.right * 1.5f;   // at your side, out of the camera's line
        Vector3 d = goal - transform.position; d.y = 0;
        if (d.magnitude > 20f) { transform.position = goal; d = Vector3.zero; }
        if (trick != null && d.magnitude > 3f) trick = null;   // you walked off: it runs after you
        float sp = trick != null ? 0f : d.magnitude > 0.32f ? Mathf.Min(d.magnitude * 3.2f, 13f) : 0f;
        if (sp > 0f)
        {
            Vector3 np = transform.position + d.normalized * sp * dt;
            transform.position = fly ? np : g.Resolve(np, 0.2f);
            transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(d), 1f - Mathf.Exp(-dt * 8f));
        }
        else if (trick == null) transform.rotation = Quaternion.Slerp(transform.rotation, p.transform.rotation, 1f - Mathf.Exp(-dt * 3f));
        else { Vector3 toHero = p.transform.position - transform.position; toHero.y = 0f; if (toHero.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(toHero), 1f - Mathf.Exp(-dt * 5f)); }   // it performs for you
        phase += dt;
        float y = fly ? 1.95f + Mathf.Sin(phase * 2.3f) * 0.15f : sp > 0f && anim == null ? Mathf.Abs(Mathf.Sin(phase * 12f)) * 0.13f : 0f;
        model.transform.localPosition = new Vector3(0f, y, 0f); model.transform.localRotation = baseRot;
        // standing about out of a fight: now and then the pet shows off a trick on its own
        bool still = (p.transform.position - lastHeroPos).sqrMagnitude < 0.0004f; lastHeroPos = p.transform.position;
        stillT = still && !p.InFight && !p.dead ? stillT + dt : 0f;
        if (trick == null && stillT > 10f && Time.time >= showOffAt && sp == 0f)
        {
            var known = AHPetTricks.Known(p);
            if (known.Count > 0) StartTrick(known[Random.Range(0, known.Count)]); else showOffAt = Time.time + 30f;
        }
        if (trick != null)
        {
            trickT += dt; float u = Mathf.Clamp01(trickT / trick.len);
            Vector3 lift, eu; AHPetTricks.Pose(trick, u, trickT, modelH, out lift, out eu);
            // turn about the middle of the body, not its feet
            Quaternion q = Quaternion.Euler(eu); Vector3 c = Vector3.up * modelH * 0.5f;
            model.transform.localRotation = q * baseRot;
            model.transform.localPosition += c - q * c + lift;
            if (trickT >= trick.len) { trick = null; AHPetTricks.Done(g, transform.position + Vector3.up * (y + modelH + 0.2f)); }
        }
        if (anim != null)
        {
            // the animated pets (fox kits, baby dragon): walk, run, sit about, or keep their wings beating
            if (fly) { if (!anim.Play("Fly", true, sp > 0f ? 1.2f : 0.8f)) anim.Play("Idle", true); }
            else if (sp > 4.5f) { float r = Mathf.Clamp(sp / 6f, 0.8f, 1.5f); if (!anim.Play("Gallop", true, r) && !anim.Play("Run", true, r)) anim.Play("Walk", true); }
            else if (sp > 0f) anim.Play("Walk", true, Mathf.Clamp(sp / 2.2f, 0.7f, 1.6f));
            else anim.Play("Idle", true);
            anim.Tick(dt);
        }
        if (kind == "slime" || kind == "toad" || kind == "pumpkin_slime") model.transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + Mathf.Sin(phase * 6f) * 0.07f, 1f - Mathf.Sin(phase * 6f) * 0.07f, 1f));   // (keeping an evolved pet's size)
        PetJobs(p, dt);
    }

    void PetJobs(AHPlayer p, float dt)
    {
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
        if (k == "kael")
        {
            // Kael from the Ashen King story: his VRoid figure in the Emberfist Garb
            var look = new AHLook { sex = "m", hair = "pony", hairCol = 0, skin = 4, eye = -1 };
            AHAnim ka; GameObject kr = null;
            try { kr = AHPeople.BuildHero(transform, AHClasses.Get("shaman"), look, g, out ka); } catch { ka = null; }
            if (kr != null) { AHCostumes.Apply(kr, "ember", look); anim = ka; if (anim != null) anim.Play("Idle", true); return kr; }
        }
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
        // a hero-grade outfit, head to toe (the body under it is put away, the weapons stay)
        string suit = k == "knight" ? "tqo_draconic_warrior" : k == "ranger" ? "tqo_lava_ranger_f" : "tqo_lava_priest_f";
        if (Resources.Load<GameObject>("AH/Models/Outfits/" + suit) != null && AHQOutfit.Wear(rig, suit) != null)
        {
            foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
            {
                bool weapon = false; for (var p = r.transform; p != null && p != rig.transform; p = p.parent) if (p.name.StartsWith("Weapon_")) { weapon = true; break; }
                if (!weapon) r.enabled = false;
            }
            string set, cls; if (AHNpc.SuitSet(suit, out set, out cls)) AHTripo.SetWeapon(rig, set, cls);
            return rig;
        }
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
                if (id == "knight") { cd = 1.1f; t.Hurt(Mathf.RoundToInt(Dmg()), null); if (!t.dead && !t.type.elite && !t.Taunted) t.tgt = this; }
                else if (id == "ranger") { cd = 1.3f; int dm = Mathf.RoundToInt(Dmg()); AHFx.Shoot(transform.position + Vector3.up * 1.3f, t, new Color(0.94f, 0.9f, 0.82f), 0.12f, 28f, m => m.Hurt(dm, null)); }
                else { cd = 1.9f; int dm = Mathf.RoundToInt(Dmg()); AHFx.Shoot(transform.position + Vector3.up * 1.4f, t, new Color(1f, 0.89f, 0.54f), 0.22f, 18f, m => m.Hurt(dm, null)); }
                phase = 0f;
            }
        }
        else
        {
            goal = p.transform.position - p.transform.forward * 0.9f + p.transform.right * (slot == 0 ? 2.1f : -2.1f);
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
    // A mount walks when you go slowly and gallops (runs) when you go fast, each clip played at the speed that keeps its
    // feet planted on the ground (measured from the clip itself). The rider rises and falls with the mount's back.
    public static bool TestRun;   // (editor test: gallop on the spot)
    AHAnim anim; AHPlayer p; float idleT;
    string walk, run; float walkV, runV; bool measured, running;
    Vector3 lastPos; float gs;   // ground speed, smoothed (m/s)
    Transform back; Vector3 back0; int restN; float bobY, bobZ; Transform rig; float rigZ0 = float.NaN;
    // the raptors' own run hardly bends the body: a swaying tail, a rolling spine and a steadied head added on top
    Transform[] tail, spine, neck; float phase, sway; string playing; float playSpeed = 1f;
    public void Setup(AHAnim a, AHPlayer who)
    {
        anim = a; p = who; anim.Play("Idle", true); lastPos = p.transform.position;
        walk = anim.Has("Walk") ? "Walk" : null;
        run = anim.Has("Gallop") ? "Gallop" : anim.Has("Run") ? "Run" : walk;
        if (walk == null) walk = run;
    }

    static Transform[] Feet(Transform root)
    {
        // the tips of the legs: the deepest bones under any bone named like a leg or foot
        var l = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>())
        {
            if (t.childCount > 0) continue;
            for (var q = t; q != null && q != root; q = q.parent)
            {
                string n = q.name.ToLowerInvariant();
                if (n.Contains("tail") || n.Contains("wing") || n.Contains("arm") || n.Contains("hand") || n.Contains("finger") || n.Contains("pole") || n.EndsWith("ik") || n.Contains("ik.") || n.Contains("ik_")) break;
                if (n.Contains("leg") || n.Contains("foot") || n.Contains("toe") || n.Contains("hoof") || n.Contains("ankle") || n.Contains("calf") || n.Contains("thigh") || n.Contains("shin")) { l.Add(t); break; }
            }
        }
        return l.ToArray();
    }
    static Transform Find(Transform root, params string[] names)
    {
        foreach (var n in names) foreach (var t in root.GetComponentsInChildren<Transform>()) if (t.name == n) return t;
        return null;
    }

    void Measure()
    {
        measured = true;
        var feet = Feet(transform);
        var body = Find(transform, "Root", "root", "Hips", "Pelvis", "Torso", "Body");
        walkV = walk != null ? anim.StrideSpeed(walk, transform, feet, body) : 0f;
        runV = run != null ? anim.StrideSpeed(run, transform, feet, body) : 0f;
        // nothing sensible measured: the old fixed rates (about 1.6 m/s walking, 6 galloping, for a horse)
        float size = 1f; foreach (var r in GetComponentsInChildren<Renderer>()) size = Mathf.Max(size, r.bounds.size.y);
        if (walkV < 0.3f || walkV > 6f) walkV = 0.7f * size;
        if (runV < 1f || runV > 20f) runV = 2.6f * size;
        if (runV < walkV * 1.3f) runV = walkV * 2.5f;
        var tl = new List<Transform>(); var sp = new List<Transform>(); var nk = new List<Transform>();
        for (int i = 1; i <= 8; i++) { var t = Find(transform, "Tail_" + i); if (t != null) tl.Add(t); var q = Find(transform, "Spine_" + i); if (q != null) sp.Add(q); var n = Find(transform, "Neck_" + i); if (n != null) nk.Add(n); }
        if (tl.Count >= 3 && sp.Count >= 1) { tail = tl.ToArray(); spine = sp.ToArray(); neck = nk.ToArray(); }
        back = Find(transform, "Torso2", "Back", "Torso", "Spine_2", "Spine_1", "B_Spine_02", "Spine", "spine", "Hips", "Pelvis");
        Debug.Log("Ashen Hollow: mount " + p.mountSel + " walk " + walkV.ToString("F2") + " m/s, " + run + " " + runV.ToString("F2") + " m/s, " + feet.Length + " feet, back " + (back != null ? back.name : "-"));
    }

    void Update()
    {
        if (anim == null || p == null) return;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        if (!measured) Measure();
        Vector3 d = p.transform.position - lastPos; d.y = 0f; lastPos = p.transform.position;
        float v = d.magnitude / dt; if (v > 40f) v = 0f;   // a teleport
        gs = Mathf.Lerp(gs, v, 1f - Mathf.Exp(-dt * 8f));
        if (TestRun) gs = runV;
        if (p.Moving || TestRun || gs > 0.4f)
        {
            idleT = 0f;
            // walk up to a brisk walk, then the run; a little either way of the change so it does not flicker
            float change = Mathf.Min(walkV * 1.5f, runV * 0.6f);
            if (running && gs < change * 0.85f) running = false; else if (!running && gs > change * 1.1f) running = true;
            playing = running ? run : walk; playSpeed = running ? Mathf.Clamp(gs / runV, 0.55f, 1.6f) : Mathf.Clamp(gs / walkV, 0.45f, 1.7f);
            anim.Play(playing, true, playSpeed);
        }
        else
        {
            running = false; idleT += dt; playing = null;
            // now and then it lowers its head or shifts its weight
            string idle = idleT > 9f && idleT < 13f ? (anim.Has("Idle_Headlow") ? "Idle_Headlow" : "Idle_2_HeadLow") : idleT > 20f && idleT < 23f ? "Idle_2" : "Idle";
            if (idleT > 23f) idleT = 0f;
            if (!anim.Play(idle, true)) anim.Play("Idle", true);
        }
        anim.Tick(dt);
    }

    // after the clip has moved the bones: the rider goes up and down (and a little to and fro) with the mount's back
    void LateUpdate()
    {
        if (p == null || !p.mounted) return;
        Sway();
        if (back == null) return;
        Vector3 local = transform.InverseTransformPoint(back.position); local.Scale(transform.lossyScale);
        // where the back sits at rest: learned over the first half second standing still
        if (restN < 30 && gs < 0.3f) { back0 = restN == 0 ? local : Vector3.Lerp(back0, local, 1f / (restN + 1)); restN++; }
        if (restN == 0) return;
        float k = 1f - Mathf.Exp(-Time.deltaTime * 30f);
        bobY = Mathf.Lerp(bobY, Mathf.Clamp(local.y - back0.y, -0.35f, 0.3f), k);
        bobZ = Mathf.Lerp(bobZ, Mathf.Clamp(local.z - back0.z, -0.2f, 0.2f), k);
        p.seatBob = bobY;
        if (rig == null && p.Model != null) rig = p.Model.transform;
        if (rig != null) { if (float.IsNaN(rigZ0)) rigZ0 = rig.localPosition.z; var lp = rig.localPosition; lp.z = rigZ0 + bobZ; rig.localPosition = lp; }
    }
    void Sway()
    {
        if (tail == null) return;
        float dt = Time.deltaTime;
        // one stride cycle per play of the clip; idle: a slow lazy swing
        float len = playing != null ? Mathf.Max(0.2f, anim.Length(playing)) : 3.2f;
        phase += dt * (playing != null ? playSpeed : 1f) / len;
        float want = playing == null ? 0.35f : running ? 1f : 0.7f;
        sway = Mathf.MoveTowards(sway, want, dt * 1.5f);
        float w = phase * Mathf.PI * 2f; Vector3 up = transform.up, fwd = transform.forward, side = transform.right;
        // the tail: a wave running down it, side to side, a little up and down with each step
        for (int i = 0; i < tail.Length; i++)
        {
            float yaw = Mathf.Sin(w - i * 0.7f) * (2.5f + i * 0.4f) * sway, pit = Mathf.Sin(2f * w - i * 0.5f) * 2.5f * sway;
            tail[i].rotation = Quaternion.AngleAxis(yaw, up) * Quaternion.AngleAxis(pit, side) * tail[i].rotation;
        }
        // the body: rolls over each planted foot and twists against the tail
        for (int i = 0; i < spine.Length; i++)
            spine[i].rotation = Quaternion.AngleAxis(Mathf.Sin(w) * 2.5f * sway, fwd) * Quaternion.AngleAxis(-Mathf.Sin(w - 0.5f) * 2f * sway, up) * spine[i].rotation;
        // the neck: bobs with the stride while the head is held steady towards the way it runs
        for (int i = 0; i < neck.Length; i++)
            neck[i].rotation = Quaternion.AngleAxis(Mathf.Sin(w) * 2f * sway, up) * Quaternion.AngleAxis(Mathf.Sin(2f * w + 1f) * 3f * sway, side) * neck[i].rotation;
    }

    void OnDestroy()
    {
        if (p != null) p.seatBob = 0f;
        if (rig != null && !float.IsNaN(rigZ0)) { var lp = rig.localPosition; lp.z = rigZ0; rig.localPosition = lp; }
        if (anim != null) anim.Dispose();
    }
}
