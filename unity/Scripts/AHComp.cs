// Ashen Hollow: companions, as in the web game (v67): pets that follow you and level up (Fetch, Forager, Guard,
// Mend, Lucky, Loyal), mounts you ride that get faster the more ground they cover, and sellswords you hire at
// a tavern (Sir Aldric the knight taunts and soaks hits, Wren the ranger shoots, Sister Mae heals).
// Tables: companions.json PETS, MOUNTS, MERCS, SEAT, PET_FLY, PET_SKILLS, PET_KIND. Models: Resources/AH/Models/Comp.
using System.Collections.Generic;
using UnityEngine;

public static class AHComp
{
    // the pets you can see and get: the shoulder riders (owls, parrot, drake) are put away until they have new models
    // (anyone who has one keeps it in their save; it just does not come out)
    public static readonly HashSet<string> Retired = new HashSet<string> { "parrot", "owl", "lantern_owl", "drake" };
    static Dictionary<string, object> petsAll, petsShown;
    public static Dictionary<string, object> AllPets { get { return AHDB.Table("companions", "PETS"); } }
    public static Dictionary<string, object> Pets
    {
        get
        {
            var all = AllPets; if (all == null) return null;
            if (!ReferenceEquals(all, petsAll)) { petsAll = all; petsShown = new Dictionary<string, object>(); foreach (var kv in all) if (!Retired.Contains(kv.Key)) petsShown[kv.Key] = kv.Value; }
            return petsShown;
        }
    }
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
    public static string PetName(string k) { return AHJson.S(AHJson.O(AllPets, k), "name", k); }
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
    public static float Fetch(AHPlayer p) { float k = PetK(p, "fetch"); return 1f + (k > 0f ? (0.05f + PetLv(p, p.pet) * 0.0075f) * k * AHEggs.LuckK(p) : 0f); }
    // Lucky: rare drops (a chance under 10%) turn up more often
    public static float Lucky(AHPlayer p) { float k = PetK(p, "lucky"); return 1f + (k > 0f ? (0.1f + PetLv(p, p.pet) * 0.01f) * k * AHEggs.LuckK(p) : 0f); }

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
        AHEggs.OnGather(g);   // now and then an egg
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
        var p = g.player; if (p.pet == null || Retired.Contains(p.pet)) return;
        var go = new GameObject("Pet");
        go.transform.position = p.transform.position - p.transform.forward * 1.2f;
        var f = go.AddComponent<AHPetFollow>(); f.Setup(g, p.pet);
        p.Recalc();   // the pet's Vitality counts to your health
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

    // TESTING: every mount can be ridden from PETS -> "All mounts", owned or not. Turn this off before release.
    public const bool TestMounts = true;
    static string trialPrev; static bool inTrial;
    // ride any mount for a try: nothing is learned or saved, and getting off gives you back your own mount
    public static void Try(AHGame g, string k)
    {
        var p = g.player;
        if (p.mounted) Dismount(g, true);
        if (!inTrial) { trialPrev = p.mountSel; inTrial = true; }
        p.mountSel = k; Mount(g);
        if (!p.mounted) EndTrial(p);
    }
    static void EndTrial(AHPlayer p) { if (!inTrial) return; inTrial = false; p.mountSel = trialPrev; trialPrev = null; }

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
        // a flier carries you a little above the ground (the Sunfire phoenix)
        float hov = (float)AHJson.N(AHJson.O(Mounts, p.mountSel), "hover", 0); if (hov > 0f && holder.transform.childCount > 0) holder.transform.GetChild(0).position += Vector3.up * hov;
        // the Voidwing: a black horse with great dark feathered wings on its shoulders
        if (p.mountSel == "voidwing") holder.AddComponent<AHMountWings>();
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
        EndTrial(p);
        if (!quiet) AHFx.Ring(p.transform.position, 0.4f, 1.6f, new Color(0.85f, 0.75f, 0.55f, 0.8f), 0.5f);
        g.ui.RefreshRide();
    }

    // mounts learn from the road (web petTick): XP for the ground covered
    static float mountAcc;
    public static void RideTick(AHGame g, float dist)
    {
        var p = g.player; if (!p.mounted || p.mountSel == null || !p.mounts.Contains(p.mountSel)) return;   // (a mount only being tried learns nothing)
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
        // the birds and the baby dragon ride on your shoulder (the parrot's model sits on its own branch, so it can't
        // fly off); with wings that can beat they flap while you run
        fly = fly && anim != null && anim.Has("Fly") && !Perchers.Contains(k);
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
        // a Gilded pet (a legendary egg): golden, and it shines
        if (model != null && AHEggs.Gilded(g.player, k))
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.materials;
                foreach (var m in mats) foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) { var c0 = m.GetColor(pr); float l = c0.r * 0.3f + c0.g * 0.59f + c0.b * 0.11f; m.SetColor(pr, Color.Lerp(c0, new Color(1f, 0.78f, 0.3f) * (0.55f + l * 0.9f), 0.7f)); }
                r.materials = mats;
            }
            if (stage < 3) { var gg = new GameObject("GildedGlow"); gg.transform.SetParent(model.transform, false); gg.transform.localPosition = Vector3.up * 0.3f; gg.AddComponent<AHWeaponGlow>().col = new Color(1f, 0.82f, 0.35f); }
        }
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
        if (perch)
        {
            // looking about, ruffling its feathers; flapping to keep its balance while you run
            if (anim != null) { if (!(p.Moving && anim.Play("Fly", true, 1.1f))) anim.Play("Idle", true); anim.Tick(dt); }
            PetJobs(p, dt); return;   // (placed in LateUpdate, after the body has moved)
        }
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
    string walk, run, trot; float walkV, runV, trotV; bool measured, running, trotting;
    Vector3 lastPos; float gs;   // ground speed, smoothed (m/s)
    Transform back; Vector3 back0; int restN; float bobY, bobZ; Transform rig; float rigZ0 = float.NaN;
    // the raptors' own run hardly bends the body: a swaying tail, a rolling spine and a steadied head added on top
    Transform[] tail, spine, neck; float phase, sway; string playing; float playSpeed = 1f;
    // the saddle: where on the mount's back the rider's hips belong (found from the mount's own mesh), and how far the
    // rider has been moved to sit there
    string seatNote = ""; bool seatFound; Vector3 seat; AHVRoidLink link; float seatDz; int seatPass, seatFrame; Transform hips;
    public string State { get { return (playing ?? "idle") + " x" + playSpeed.ToString("F2") + " at " + gs.ToString("F2") + " m/s (walk " + walkV.ToString("F2") + ", run " + runV.ToString("F2") + ")"; } }
    public void Setup(AHAnim a, AHPlayer who)
    {
        anim = a; p = who; anim.Play("Idle", true); lastPos = p.transform.position;
        walk = anim.Has("Walk") ? "Walk" : null;
        run = anim.Has("Gallop") ? "Gallop" : anim.Has("Run") ? "Run" : walk;
        if (walk == null) walk = run;
        trot = anim.Has("Trot") ? "Trot" : null;   // (the wolves: walk, trot, then run)
        // a winged mount that walks on the ground and takes to the air when you go fast (the Ashwing drake): MOUNTS
        // "flyRun": metres it rises; its Fly clip is its run
        flyUp = (float)AHJson.N(AHJson.O(AHComp.Mounts, p.mountSel), "flyRun", 0);
        if (flyUp > 0f && anim.Has("Fly")) run = "Fly"; else flyUp = 0f;
        if (transform.childCount > 0) { body = transform.GetChild(0); body0 = body.localPosition; }
    }
    float flyUp, lift; Transform body; Vector3 body0;
    public float Lift { get { return lift; } }

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
                if (n.Contains("leg") || n.Contains("foot") || n.Contains("toe") || n.Contains("hoof") || n.Contains("ankle") || n.Contains("calf") || n.Contains("thigh") || n.Contains("shin") || n.Contains("pfote") || n.Contains("schenkel") || n.Contains("tarsus") || n.Contains("tibia") || n.Contains("femur")) { l.Add(t); break; }   // (the wolf's rig names its paws in German; the spider's legs are named like an insect's)
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
        var body = Find(transform, "Root", "root", "Hips", "body", "Pelvis", "Torso", "Body", "BN_Root_01", "BN_Pelvis_060");
        walkV = walk != null ? anim.StrideSpeed(walk, transform, feet, body) : 0f;
        runV = run != null ? anim.StrideSpeed(run, transform, feet, body) : 0f;
        trotV = trot != null ? anim.StrideSpeed(trot, transform, feet, body) : 0f;
        // nothing sensible measured: the old fixed rates (about 1.6 m/s walking, 6 galloping, for a horse)
        float size = 1f; foreach (var r in GetComponentsInChildren<Renderer>()) size = Mathf.Max(size, r.bounds.size.y);
        if (walkV < 0.3f || walkV > 6f) walkV = 0.7f * size;
        if (flyUp > 0f) runV = Mathf.Max(walkV * 2.2f, 2.4f * size);   // (wings have no stride to measure)
        // the pace a hand-keyed gait was made for (MOUNTS "walkV", "runV": m/s), where measuring its feet reads wrong
        var md = AHJson.O(AHComp.Mounts, p.mountSel);
        if (AHJson.N(md, "walkV", 0) > 0) walkV = (float)AHJson.N(md, "walkV", 0);
        if (AHJson.N(md, "runV", 0) > 0) runV = (float)AHJson.N(md, "runV", 0);
        if (runV < 1f || runV > 20f) runV = 2.6f * size;
        if (runV < walkV * 1.3f) runV = walkV * 2.5f;
        if (trot != null && (trotV <= walkV * 1.1f || trotV >= runV)) trotV = (walkV + runV) * 0.5f;
        p.mountWalk = walkV * 1.3f; p.mountTrot = trot != null ? trotV * 1.15f : 0f;
        var tl = new List<Transform>(); var sp = new List<Transform>(); var nk = new List<Transform>();
        for (int i = 1; i <= 8; i++) { var t = Find(transform, "Tail_" + i); if (t != null) tl.Add(t); var q = Find(transform, "Spine_" + i); if (q != null) sp.Add(q); var n = Find(transform, "Neck_" + i); if (n != null) nk.Add(n); }
        if (tl.Count >= 3 && sp.Count >= 1) { tail = tl.ToArray(); spine = sp.ToArray(); neck = nk.ToArray(); }
        back = Find(transform, "Torso2", "Back", "Torso", "Spine_2", "Spine_1", "B_Spine_02", "Spine", "spine", "Bauch.001", "Brust", "Hips", "Pelvis", "body");   // (the spider: its carapace bone)
        if (back == null)
        {
            // other skeletons (BN_Spine_00_02 ...): the middle one of the bones named like a spine
            var sp2 = new List<Transform>(); foreach (var t in GetComponentsInChildren<Transform>()) if (t.name.ToLowerInvariant().Contains("spine") && !t.name.EndsWith("_end")) sp2.Add(t);
            if (sp2.Count > 0) back = sp2[sp2.Count / 2];
        }
        // bones with no names to go by (the Tyrant's Bone_001...): the saddle's own bone, so the rider is still seated
        if (back == null) back = Find(transform, "Socket_Saddle");
        Debug.Log("Ashen Hollow: mount " + p.mountSel + " walk " + walkV.ToString("F2") + " m/s, " + (trot != null ? "trot " + trotV.ToString("F2") + " m/s, " : "") + run + " " + runV.ToString("F2") + " m/s, " + feet.Length + " feet, back " + (back != null ? back.name : "-"));
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
            // (a slowed-down gallop looks like hopping on the spot: below the change it walks, quickening into a trot)
            float change = trot != null ? Mathf.Max(trotV * 1.45f, runV * 0.8f) : Mathf.Max(walkV * 1.8f, runV * 0.8f);
            if (flyUp > 0f) change = walkV * 1.4f;   // (a flier takes off as soon as you go faster than a walk)
            if (running && gs < change * 0.85f) running = false; else if (!running && gs > change * 1.05f) running = true;
            if (!running && trot != null)
            {
                // between the walk and the run: a trot
                float tIn = walkV * 1.5f;
                if (trotting && gs < tIn * 0.85f) trotting = false; else if (!trotting && gs > tIn) trotting = true;
            }
            else trotting = false;
            playing = running ? run : trotting ? trot : walk;
            playSpeed = running ? Mathf.Clamp(gs / runV, 1.0f, 1.9f) : trotting ? Mathf.Clamp(gs / trotV, 0.7f, 1.8f) : Mathf.Clamp(gs / walkV, 0.5f, 2.0f);
            if (flyUp > 0f && running) playSpeed = 1f;   // (a wingbeat at its own pace)
            anim.Play(playing, true, playSpeed);
        }
        else
        {
            running = false; trotting = false; idleT += dt; playing = null;
            // now and then it lowers its head or shifts its weight
            string idle = idleT > 9f && idleT < 13f ? (anim.Has("Idle_Headlow") ? "Idle_Headlow" : "Idle_2_HeadLow") : idleT > 20f && idleT < 23f ? "Idle_2" : "Idle";
            if (idleT > 23f) idleT = 0f;
            if (!anim.Play(idle, true)) anim.Play("Idle", true);
        }
        anim.Tick(dt);
        // up into the air while running, down to walk again (eased; the rider goes with it)
        if (flyUp > 0f && body != null)
        {
            lift = Mathf.MoveTowards(lift, running ? flyUp : 0f, dt * (running ? 2.2f : 3f));
            float e = lift <= 0f ? 0f : Mathf.SmoothStep(0f, flyUp, lift / flyUp) + (running ? Mathf.Sin(Time.time * 2.4f) * 0.08f * (lift / flyUp) : 0f);
            body.localPosition = body0 + Vector3.up * e / Mathf.Max(1e-3f, transform.lossyScale.y);
            lift = Mathf.Max(lift, 0f); flyE = e;
        }
    }
    float flyE;

    // after the clip has moved the bones: the rider goes up and down (and a little to and fro) with the mount's back
    void LateUpdate()
    {
        if (p == null || !p.mounted) return;
        if (link == null) link = p.GetComponentInChildren<AHVRoidLink>();
        if (link != null) link.ride = Mathf.MoveTowards(link.ride, 1f, Time.deltaTime * 3f);
        Sway();
        if (back == null) return;
        Vector3 local = transform.InverseTransformPoint(back.position); local.Scale(transform.lossyScale);
        local.y -= flyE;   // (the flight's rise is added on its own below)
        // where the back sits at rest: learned over the first half second standing still
        if (restN < 30 && gs < 0.3f) { back0 = restN == 0 ? local : Vector3.Lerp(back0, local, 1f / (restN + 1)); restN++; }
        if (restN == 0) return;
        if (restN >= 30 && seatPass < 3 && gs < 0.3f && Time.frameCount - seatFrame > 15) { seatFrame = Time.frameCount; SitOnSaddle(); }   // a few frames apart: the body takes a frame to follow
        float k = 1f - Mathf.Exp(-Time.deltaTime * 30f);
        bobY = Mathf.Lerp(bobY, Mathf.Clamp(local.y - back0.y, -0.35f, 0.3f), k);
        bobZ = Mathf.Lerp(bobZ, Mathf.Clamp(local.z - back0.z, -0.2f, 0.2f), k);
        p.seatBob = bobY + flyE;   // (and up in the air with a flying mount)
        if (rig == null && p.Model != null) rig = p.Model.transform;
        if (rig != null) { if (float.IsNaN(rigZ0)) rigZ0 = rig.localPosition.z; var lp = rig.localPosition; lp.z = rigZ0 + seatDz + bobZ; rig.localPosition = lp; }
    }
    // a point in the mount's own frame (metres: x right, y up, z forward)
    Vector3 Local(Vector3 w) { Vector3 d = w - transform.position; return new Vector3(Vector3.Dot(d, transform.right), Vector3.Dot(d, transform.up), Vector3.Dot(d, transform.forward)); }

    // the seat of the saddle: the low point of the back between the pommel and the cantle (or the middle of the saddle
    // blanket when the saddle is its own part), measured on the mesh as it stands now
    bool FindSeat()
    {
        // a rig made for riding marks its seat with a bone (the Creatures beasts: Socket_Saddle)
        var sock = Find(transform, "Socket_Saddle", "Saddle_Socket", "Seat");
        if (sock != null) { seat = Local(sock.position); seatNote += "socket; "; return true; }
        var pts = new List<Vector3>(); var sad = new List<Vector3>(); var mesh = new Mesh();
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            // the baked vertices' space differs with how the model was imported: of the likely ones, keep the one whose
            // box matches where the renderer really is
            var wb = smr.bounds; Matrix4x4 m = Matrix4x4.identity; bool bestScale = true; float bestErr = float.MaxValue;
            var tries = new[] { Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one), smr.transform.localToWorldMatrix,
                                smr.rootBone != null ? smr.rootBone.localToWorldMatrix : smr.transform.localToWorldMatrix,
                                Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, transform.lossyScale),
                                Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, new Vector3(1f / Mathf.Max(1e-4f, transform.lossyScale.x), 1f / Mathf.Max(1e-4f, transform.lossyScale.y), 1f / Mathf.Max(1e-4f, transform.lossyScale.z))) };
            foreach (bool useScale in new[] { true, false })
            {
                smr.BakeMesh(mesh, useScale); var v0 = mesh.vertices; if (v0.Length == 0) continue;
                foreach (var tm in tries)
                {
                    var bb = new Bounds(tm.MultiplyPoint3x4(v0[0]), Vector3.zero);
                    for (int i = 0; i < v0.Length; i += 7) bb.Encapsulate(tm.MultiplyPoint3x4(v0[i]));
                    float err = (bb.min - wb.min).magnitude + (bb.max - wb.max).magnitude;
                    if (err < bestErr) { bestErr = err; m = tm; bestScale = useScale; }
                }
            }
            seatNote += smr.name + " fit " + bestErr.ToString("F2") + (bestScale ? " scaled" : "") + "; ";
            if (bestErr > 0.6f) continue;   // nothing lined up: not trusted
            smr.BakeMesh(mesh, bestScale); var vs = mesh.vertices;
            var mats = smr.sharedMaterials;
            for (int sm = 0; sm < mesh.subMeshCount; sm++)
            {
                string mn = sm < mats.Length && mats[sm] != null ? mats[sm].name.ToLowerInvariant() : "";
                string rn = smr.name.ToLowerInvariant();   // (the bridle can share the saddle's leather: not the seat)
                bool isSaddle = (mn.Contains("saddle") || mn.Contains("blanket") || mn.Contains("seat")) && !rn.Contains("bridle") && !rn.Contains("rein") && !rn.Contains("head");
                var tri = mesh.GetTriangles(sm);
                for (int i = 0; i < tri.Length; i += 3) { var q = Local(m.MultiplyPoint3x4(vs[tri[i]])); pts.Add(q); if (isSaddle) sad.Add(q); }
            }
        }
        Destroy(mesh);
        if (pts.Count < 50) return false;
        float zMin, zMax;
        if (sad.Count > 20) { zMin = float.MaxValue; zMax = float.MinValue; foreach (var q in sad) { zMin = Mathf.Min(zMin, q.z); zMax = Mathf.Max(zMax, q.z); } float c = (zMin + zMax) * 0.5f, h = (zMax - zMin) * 0.3f; zMin = c - h; zMax = c + h; }
        else
        {
            // between the hind legs and the forelegs
            var hind = Find(transform, "BackShoulder.L", "BackUpperLeg.L", "Thigh.L", "Hip.L", "Pelvis.L", "BN_L_Calf_052", "Oberschenkel_L", "ThighL"); var fore = Find(transform, "FrontShoulder.L", "FrontUpperLeg.L", "Shoulder.L", "UpperArm.L", "BN_L_UpperArm_039", "Oberarm_L", "UpperArmL");
            if (hind == null || fore == null) return false;
            float a = Local(hind.position).z, b = Local(fore.position).z; if (a > b) { float t = a; a = b; b = t; }
            zMin = a + (b - a) * 0.25f; zMax = a + (b - a) * 0.8f;
        }
        // the top line of the back along the middle, in short steps
        const int N = 24; var top = new float[N]; for (int i = 0; i < N; i++) top[i] = float.MinValue;
        float half = 0.12f;
        foreach (var q in pts)
        {
            if (Mathf.Abs(q.x) > half || q.z < zMin || q.z > zMax) continue;
            int i = Mathf.Clamp((int)((q.z - zMin) / (zMax - zMin) * N), 0, N - 1); if (q.y > top[i]) top[i] = q.y;
        }
        int best = -1; float bestY = float.MaxValue;
        for (int i = 1; i < N - 1; i++)
        {
            if (top[i - 1] == float.MinValue || top[i] == float.MinValue || top[i + 1] == float.MinValue) continue;
            float y = (top[i - 1] + top[i] + top[i + 1]) / 3f; if (y < bestY) { bestY = y; best = i; }
        }
        if (best < 0) return false;
        seat = new Vector3(0f, top[best], zMin + (best + 0.5f) * (zMax - zMin) / N);
        return true;
    }

    void SitOnSaddle()
    {
        seatPass++;
        if (!seatFound)
        {
            // a reading that makes no sense (the seat below the back, or far above it) is replaced by a spot just above the
            // middle of the back (scaled-up mounts like the wolves read badly from their mesh)
            seatFound = FindSeat();
            if (!seatFound || (!seatNote.Contains("socket; ") && (seat.y < back0.y + 0.1f || seat.y > back0.y + 0.7f || Mathf.Abs(seat.z - back0.z) > 0.6f))) {   // (a rig's own seat bone is trusted)
                // over the middle of the body, between the hind legs and the forelegs (where a saddle is girthed)
                float z = back0.z; var hind = Find(transform, "BackShoulder.L", "BackUpperLeg.L", "Thigh.L", "Hip.L"); var fore = Find(transform, "FrontShoulder.L", "FrontUpperLeg.L", "Shoulder.L");
                if (hind != null && fore != null) z = (Local(hind.position).z + Local(fore.position).z) * 0.5f - 0.12f;   // a little behind the middle
                seat = new Vector3(0f, back0.y + 0.28f * Mathf.Max(1f, transform.lossyScale.y * 0.8f), z); seatFound = true; seatNote += "from the back; ";
            }
        }
        if (hips == null)
        {
            var link = p.GetComponentInChildren<AHVRoidLink>(); Animator an = null;
            if (link != null && link.Body != null) an = link.Body.GetComponent<Animator>();
            if (an == null || !an.isHuman) foreach (var a in p.GetComponentsInChildren<Animator>()) if (a.isHuman && !a.transform.IsChildOf(transform)) { an = a; break; }
            if (an != null) hips = an.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) { seatPass = 99; return; }
        }
        // the hip joint sits a hand's width above the leather
        Vector3 h = Local(hips.position); Vector3 want = seat + Vector3.up * 0.1f;
        float dy = Mathf.Clamp(want.y - h.y, -0.6f, 0.6f), dz = Mathf.Clamp(want.z - h.z, -0.6f, 0.6f);
        string line = p.mountSel + " pass " + seatPass + " [" + seatNote + "]: saddle " + seat.ToString("F2") + " hips " + h.ToString("F2") + " (" + hips.root.name + "/" + hips.name + ") seatY " + p.seatY.ToString("F2") + " seatDz " + seatDz.ToString("F2") + " back0 " + back0.ToString("F2") + " rig " + (rig != null ? rig.localPosition.ToString("F2") : "-");
        if (Mathf.Abs(want.y - h.y) > 0.03f || Mathf.Abs(want.z - h.z) > 0.03f) { p.seatY += dy; seatDz += dz; line += " -> moved " + dy.ToString("F2") + " up, " + dz.ToString("F2") + " fwd"; }
        Debug.Log("Ashen Hollow: saddle " + line);
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(Application.dataPath, "../HeroShots/seat_log.txt"), line + "\n"); } catch { }
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
        if (p != null) { p.seatBob = 0f; p.mountWalk = 0f; p.mountTrot = 0f; }
        if (link != null) link.ride = 0f;
        if (rig != null && !float.IsNaN(rigZ0)) { var lp = rig.localPosition; lp.z = rigZ0; rig.localPosition = lp; }
        if (anim != null) anim.Dispose();
    }
}

// feathered wings on a mount's shoulders (the Voidwing): they follow the top of its back, held half open and swept back,
// beating slowly while it stands and harder at the gallop
// the Voidwing's wings: two feathered cards (one picture, tools/mounts/wing_tex.py, mirrored for the left) at the
// withers, each in two parts hinged at the wrist so the tip lags the beat. Spread and beating while it moves; half
// folded and swept back, breathing slowly, while it stands.
public class AHMountWings : MonoBehaviour
{
    Transform spine; Transform[] roots = new Transform[2], tips = new Transform[2]; AHPlayer p; float t, amp;
    const float Span = 2.0f;               // one wing, shoulder to tip (metres)
    const float U0 = 30f / 2048f, V0 = 330f / 1024f, UW = 1250f / 2048f, VW = 300f / 1024f;   // the picture's shoulder and wrist
    static Material mat;
    static Material Mat()
    {
        if (mat != null) return mat;
        var tex = Resources.Load<Texture2D>("AH/Textures/Wings/voidwing");
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name = "VoidwingWing";
        mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.3f); mat.SetFloat("_Cull", 0f); mat.doubleSidedGI = true;
        if (tex != null)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            mat.SetTexture("_BaseMap", tex); mat.SetFloat("_AlphaClip", 1f); mat.SetFloat("_Cutoff", 0.45f); mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            // a faint violet glow from the feathers themselves, so they read in the dark
            mat.EnableKeyword("_EMISSION"); mat.SetTexture("_EmissionMap", tex); mat.SetColor("_EmissionColor", new Color(0.45f, 0.35f, 0.7f));
        }
        else mat.SetColor("_BaseColor", new Color(0.12f, 0.07f, 0.22f));
        return mat;
    }
    // where a point of the picture (u across, vt down from the top) lies in the wing: x out from the body, z back
    static Vector3 At(float u, float vt, int sx) { return new Vector3(sx * (u - U0) * Span, 0f, (V0 - vt) * Span * 0.5f); }
    static Mesh Panel(float u0, float u1, int sx, Vector3 origin)
    {
        // a small grid (so the light and the fog fall smoothly over it)
        const int NX = 4, NY = 3; var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int y = 0; y <= NY; y++) for (int x = 0; x <= NX; x++)
        {
            float u = Mathf.Lerp(u0, u1, x / (float)NX), vu = y / (float)NY;
            v.Add(At(u, 1f - vu, sx) - origin); uv.Add(new Vector2(u, vu));
        }
        for (int y = 0; y < NY; y++) for (int x = 0; x < NX; x++)
        {
            int a = y * (NX + 1) + x, b = a + 1, c = a + NX + 1, d = c + 1;
            if (sx > 0) { tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d); }
            else { tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(b); tri.Add(d); tri.Add(c); }
        }
        var m = new Mesh { name = "MountWingPanel" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
    static void Show(Transform parent, Mesh mesh)
    {
        parent.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; parent.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Mat();
    }
    void Start()
    {
        p = GetComponentInParent<AHPlayer>();
        var sp = new List<Transform>(); foreach (var t2 in GetComponentsInChildren<Transform>()) if (t2.name.ToLowerInvariant().Contains("spine") && !t2.name.EndsWith("_end")) sp.Add(t2);
        // the withers: the spine bone nearest the front legs
        if (sp.Count > 0) spine = sp[Mathf.Min(sp.Count - 1, (sp.Count * 3) / 4)];
        for (int s = 0; s < 2; s++)
        {
            int sx = s == 0 ? -1 : 1;
            var root = new GameObject("MountWing").transform; root.SetParent(transform, false);
            var inner = new GameObject("Inner").transform; inner.SetParent(root, false); Show(inner, Panel(0f, UW, sx, Vector3.zero));
            Vector3 wrist = At(UW, VW, sx);
            var tip = new GameObject("Tip").transform; tip.SetParent(root, false); tip.localPosition = wrist; Show(tip, Panel(UW, 1f, sx, wrist));
            roots[s] = root; tips[s] = tip;
        }
    }
    void LateUpdate()
    {
        if (roots[0] == null) return;
        float dt = Time.deltaTime; bool moving = p != null && p.Moving;
        amp = Mathf.Lerp(amp, moving ? 1f : 0f, 1f - Mathf.Exp(-dt * 2.5f));
        t += dt * (1.2f + amp * 3.6f);
        float beat = Mathf.Sin(t), lag = Mathf.Sin(t - 0.9f);
        // standing: raised a little, swept well back, the tips drooping; moving: spread, beating, the tips following
        // (the beat stays mostly above the back: a full downstroke on the ground swept the wing into the dirt)
        float sweep = Mathf.Lerp(52f, 16f, amp), lift = Mathf.Lerp(24f, 22f, amp) + beat * Mathf.Lerp(3f, 24f, amp);
        float bend = Mathf.Lerp(-38f, -6f, amp) + lag * Mathf.Lerp(2f, 16f, amp);
        Vector3 at = spine != null ? spine.position : transform.position + transform.up * 1.4f;
        for (int s = 0; s < 2; s++)
        {
            int sx = s == 0 ? -1 : 1; var w = roots[s];
            w.position = at + transform.up * 0.1f + transform.right * sx * 0.2f;
            w.rotation = transform.rotation * Quaternion.AngleAxis(sx * sweep, Vector3.up) * Quaternion.AngleAxis(sx * lift, Vector3.forward) * Quaternion.AngleAxis(-6f, Vector3.right);
            tips[s].localRotation = Quaternion.AngleAxis(sx * bend, Vector3.forward);
        }
    }
}
