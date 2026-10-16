// Ashen Hollow: pet eggs, hatching, pet stars and stats, and the store's Mystic egg. Data: Resources/AH/Data/eggs.json.
//  - Eggs: beasts and gathering drop the egg of the land you are in (Ember, Tide, Grove, Stone; a Void egg from beasts
//    slain after dark). They go in the egg satchel (3 places): a full satchel finds no more.
//  - Hatching: an egg grows from the work that matches it (cooking and smithing warm an Ember egg, fishing a Tide egg,
//    woodcutting, herbs and farming a Grove egg, mining and skinning a Stone egg, night kills a Void egg). Essence (from
//    hatching a pet you already have at five stars) or Crowns warm one the rest of the way. A grown egg hatches when you
//    tap Hatch on the Companions page.
//  - What hatches: a rarity is rolled (common, rare, epic, legendary), then one of the egg's pets of that rarity. A
//    legendary is a Gilded pet: a golden one of the egg's pets, at five stars. A pet you have already gets a star (up to
//    five); at five you get Essence instead.
//  - Stars and stats: every pet has 1 to 5 stars. Its stat points (stars and level) split by its lean into Power (its
//    knack hits harder), Vitality (your health), Spirit (its knack is ready sooner) and Luck (Fetch and Lucky).
//  - The Mystic egg (store, Crowns): hatches at once, from any pet, with better odds; an epic or better at least every
//    30 eggs and a legendary at least every 90 (counted across buys). Ten at once costs less.
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AHEggState
{
    public List<AHKV> eggs = new List<AHKV>();          // the satchel: element, points grown
    public List<AHKV> stars = new List<AHKV>();         // stars by pet
    public List<string> gilded = new List<string>();    // pets you have the golden one of
    public long essence;
    public int pityEpic, pityLeg, hatched, mystic;
    public string silverDay = ""; public int silverN, silverAll;   // the Artisans' Silver egg: bought today, and in all
}

public static class AHEggs
{
    public static readonly string[] Rarities = { "common", "rare", "epic", "legendary" };
    public static readonly string[] RarityHex = { "#c9c0aa", "#5ab4ff", "#c07aff", "#ffb030" };
    public static readonly string[] StatName = { "Power", "Vitality", "Spirit", "Luck" };

    static object D { get { return AHDB.File("eggs"); } }
    static Dictionary<string, object> Elements { get { return AHJson.O(D, "ELEMENTS"); } }
    public static int Satchel { get { return (int)AHJson.N(D, "SATCHEL", 3); } }
    public static AHEggState S(AHPlayer p) { if (p.prog.eggs == null) p.prog.eggs = new AHEggState(); return p.prog.eggs; }

    public static string EggName(string el) { return AHJson.S(AHJson.O(Elements, el), "name", "Egg"); }
    public static string EggHex(string el) { return AHJson.S(AHJson.O(Elements, el), "hex", "#ffffff"); }
    public static string Grows(string el) { return AHJson.S(AHJson.O(Elements, el), "grows", ""); }
    public static int Need(string el) { return (int)AHJson.N(AHJson.O(Elements, el), "need", 600); }
    public static string LandEgg(string area) { return AHJson.S(AHJson.O(D, "AREA"), area ?? "", "grove"); }
    public static string Rarity(string pet) { var sp = AHJson.O(AHJson.O(D, "MYSTIC"), "pets"); return sp != null && sp.ContainsKey(pet) ? AHJson.S(sp, pet, "common") : AHJson.S(AHJson.O(D, "RARITY"), pet, "common"); }
    public static int RarityI(string r) { return Mathf.Max(0, System.Array.IndexOf(Rarities, r)); }
    public static string RarityTag(string r) { int i = RarityI(r); return "<color=" + RarityHex[i] + ">" + char.ToUpperInvariant(r[0]) + r.Substring(1) + "</color>"; }

    // a pet can hatch only if it is in the game: in PETS, not retired, and its model is here
    static bool Hatchable(string k)
    {
        if (!AHJson.Has(AHComp.AllPets, k) || AHComp.Retired.Contains(k)) return false;
        return Resources.Load<GameObject>("AH/Models/Comp/pet_" + k + "_hd") != null || Resources.Load<GameObject>("AH/Models/Comp/pet_" + k) != null;
    }
    static List<string> Pool(string el)
    {
        var l = new List<string>(); var src = el == null ? null : AHJson.A(AHJson.O(Elements, el), "pets");
        if (src != null) foreach (var o in src) { var k = o as string; if (k != null && Hatchable(k) && !StoreOnly(k)) l.Add(k); }
        if (l.Count == 0) foreach (var kv in AHJson.O(D, "RARITY")) if (Hatchable(kv.Key) && !StoreOnly(kv.Key)) l.Add(kv.Key);
        return l;
    }
    // the store's pets: only ever from the Mystic egg, with a rarity of their own there
    static Dictionary<string, object> StorePets { get { return AHJson.O(Mystic, "pets"); } }
    public static bool StoreOnly(string k) { return StorePets != null && StorePets.ContainsKey(k); }
    static List<string> StorePool() { var l = new List<string>(); if (StorePets != null) foreach (var kv in StorePets) if (Hatchable(kv.Key)) l.Add(kv.Key); return l; }
    public static bool MysticReady { get { return StorePool().Count > 0; } }
    // pet stats (Power, Vitality, Spirit, Luck) and stars: off while pets are looks only
    public static bool StatsOn { get { return AHJson.B(D, "STATS"); } }

    // ---------- stars and stats ----------
    public static int Stars(AHPlayer p, string k) { return (int)AHProgress.Get(S(p).stars, k ?? ""); }
    public static bool Gilded(AHPlayer p, string k) { return k != null && S(p).gilded.Contains(k); }
    // a pet owned from before the eggs (bought, or from a fair) has the stars its rarity starts with
    public static void EnsureStars(AHPlayer p)
    {
        var s = S(p);
        foreach (var k in p.pets) if (AHProgress.Get(s.stars, k) <= 0) SetStars(s, k, Mathf.Max(1, StarRange(Rarity(k))[0]));
    }
    static void SetStars(AHEggState s, string k, int n) { foreach (var e in s.stars) if (e.k == k) { e.v = n; return; } s.stars.Add(new AHKV { k = k, v = n }); }
    static int[] StarRange(string r) { var a = AHJson.A(AHJson.O(D, "STARS"), r); return a != null && a.Count > 1 ? new[] { (int)(double)a[0], (int)(double)a[1] } : new[] { 1, 2 }; }

    // a pet's stat points: 6 a star, 1.5 a level, 10 more if it is gilded, split by its lean
    public static int[] Stats(AHPlayer p, string k)
    {
        var r = new int[4]; if (k == null) return r;
        float pts = Mathf.Max(1, Stars(p, k)) * 6f + AHComp.PetLv(p, k) * 1.5f + (Gilded(p, k) ? 10f : 0f);
        var lean = AHJson.A(AHJson.O(D, "LEAN"), k); float[] w = { 1, 1, 1, 1 }; float sw = 0f;
        if (lean != null && lean.Count >= 4) for (int i = 0; i < 4; i++) w[i] = (float)(double)lean[i];
        foreach (var x in w) sw += x;
        for (int i = 0; i < 4; i++) r[i] = Mathf.RoundToInt(pts * w[i] / sw);
        return r;
    }
    static int Out(AHPlayer p, int i) { return p == null || p.pet == null || !StatsOn ? 0 : Stats(p, p.pet)[i]; }
    public static float PowK(AHPlayer p) { return 1f + Out(p, 0) * 0.01f; }                              // the knack's damage and healing
    public static float HpK(AHPlayer p) { return Out(p, 1) * 0.0025f; }                                  // your health
    public static float CdK(AHPlayer p) { return 1f - Mathf.Min(0.3f, Out(p, 2) * 0.008f); }            // the knack's rest
    public static float LuckK(AHPlayer p) { return 1f + Out(p, 3) * 0.01f; }                             // Fetch and Lucky
    public static string StarsText(int n) { return "<color=#ffd04a>" + new string('★', Mathf.Clamp(n, 0, 5)) + "</color><color=#5a5048>" + new string('★', 5 - Mathf.Clamp(n, 0, 5)) + "</color>"; }
    public static string StatsLine(AHPlayer p, string k)
    {
        var s = Stats(p, k);
        return StatName[0] + " " + s[0] + " · " + StatName[1] + " " + s[1] + " · " + StatName[2] + " " + s[2] + " · " + StatName[3] + " " + s[3];
    }
    public static string StatsHelp(AHPlayer p, string k)
    {
        var s = Stats(p, k);
        return "+" + s[0] + "% knack power · +" + (s[1] * 0.25f).ToString("0.#") + "% your health · knack ready " + Mathf.RoundToInt(Mathf.Min(30f, s[2] * 0.8f)) + "% sooner · +" + s[3] + "% Fetch and Lucky";
    }

    // ---------- finding eggs ----------
    public static bool Add(AHGame g, string el)
    {
        var s = S(g.player); if (s.eggs.Count >= Satchel || !AHJson.Has(Elements, el)) return false;
        s.eggs.Add(new AHKV { k = el, v = 0 });
        if (g.ui != null)
        {
            g.ui.Banner(EggName(el) + "!", "Into your egg satchel · it grows from " + Grows(el));
            g.ui.Float(g.player.transform.position + Vector3.up * 2.6f, EggName(el), ColorOf(el));
        }
        AHFx.Pop(g.player.transform.position + Vector3.up, 1.6f, ColorOf(el), 0.6f);
        g.MarkDirty();
        return true;
    }
    public static Color ColorOf(string el) { Color c; return ColorUtility.TryParseHtmlString(EggHex(el), out c) ? c : Color.white; }
    static float Drop(string k) { return (float)AHJson.N(AHJson.O(D, "DROP"), k, 0); }

    public static void OnKill(AHGame g, AHMob m)
    {
        var p = g.player; if (p == null || m == null || m.add) return;
        var s = S(p);
        // night kills warm a Void egg
        if (g.IsNight) { int nk = (int)AHJson.N(AHJson.O(Elements, "void"), "night_kill", 45); Warm(g, "void", Mathf.Max(5, nk + m.type.lvl / 2)); }
        if (s.eggs.Count >= Satchel) return;
        float ch = m.type.rare ? Drop("rare") : m.type.elite ? Drop("elite") : Drop("mob");
        ch *= LuckK(p);
        if (Random.value < ch) Add(g, g.IsNight && Random.value < 0.35f ? "void" : LandEgg(AHGame.AreaId));
    }
    public static void OnGather(AHGame g)
    {
        var p = g.player; if (p == null || S(p).eggs.Count >= Satchel) return;
        if (Random.value < Drop("gather") * LuckK(p)) Add(g, LandEgg(AHGame.AreaId));
    }

    // ---------- growing ----------
    // work done (a skill's XP): the eggs that grow from it take the points, the first unready one first
    public static void OnXp(AHGame g, string skill, int amount)
    {
        if (g == null || g.player == null || amount <= 0 || skill == null) return;
        foreach (var e in S(g.player).eggs)
        {
            var sk = AHJson.A(AHJson.O(Elements, e.k), "skills"); if (sk == null || !sk.Contains(skill)) continue;
            Warm(g, e.k, Mathf.Min(amount, 80)); return;
        }
    }
    static void Warm(AHGame g, string el, int pts)
    {
        foreach (var e in S(g.player).eggs)
        {
            if (e.k != el || e.v >= Need(el)) continue;
            e.v = System.Math.Min(Need(el), e.v + pts);
            if (e.v >= Need(el) && g.ui != null) { g.ui.Banner(EggName(el) + " is ready", "Open Companions to hatch it"); AHSound.Play("level"); }
            g.MarkDirty(); return;
        }
    }
    public static bool Ready(AHKV e) { return e.v >= Need(e.k); }
    public static long WarmCost(AHKV e) { long left = Need(e.k) - e.v; return left <= 0 ? 0 : System.Math.Max(10, (left + 3) / (long)AHJson.N(D, "WARM_POINTS_PER_CROWN", 4)); }
    public static void WarmWithCrowns(AHGame g, int i)
    {
        var p = g.player; var s = S(p); if (i < 0 || i >= s.eggs.Count) return; var e = s.eggs[i]; long c = WarmCost(e);
        if (c <= 0) return; if (p.prog.crowns < c) { g.ui.Toast("Not enough " + AHStore.Currency + "."); return; }
        p.prog.crowns -= c; e.v = Need(e.k); g.ui.Toast(EggName(e.k) + " is warm and ready."); g.SaveProgress();
    }
    public static void WarmWithEssence(AHGame g, int i)
    {
        var p = g.player; var s = S(p); if (i < 0 || i >= s.eggs.Count || s.essence <= 0) return; var e = s.eggs[i];
        long use = System.Math.Min(s.essence, Need(e.k) - e.v); if (use <= 0) return;
        s.essence -= use; e.v += use; g.ui.Toast("+" + use + " warmth from Essence"); g.SaveProgress();
    }
    public static void Discard(AHGame g, int i) { var s = S(g.player); if (i >= 0 && i < s.eggs.Count) { g.ui.Toast(EggName(s.eggs[i].k) + " left in a safe nest."); s.eggs.RemoveAt(i); g.SaveProgress(); } }

    // ---------- hatching ----------
    static string Roll(Dictionary<string, object> odds)
    {
        double tot = 0; foreach (var r in Rarities) tot += AHJson.N(odds, r, 0);
        double x = Random.value * tot;
        foreach (var r in Rarities) { x -= AHJson.N(odds, r, 0); if (x < 0) return r; }
        return "common";
    }
    // a pet of the rarity from the pool (the nearest rarity below when there is none of it); a legendary is gilded
    static string Pick(List<string> pool, string rar, out bool gilded)
    {
        // a legendary: a legendary pet when the egg holds one (the Flame hatchling), else a golden (Gilded) epic one
        if (rar == "legendary") { var lg = pool.FindAll(k => Rarity(k) == "legendary"); if (lg.Count > 0) { gilded = false; return lg[Random.Range(0, lg.Count)]; } }
        gilded = rar == "legendary";
        int want = gilded ? 2 : Mathf.Min(2, RarityI(rar));
        for (int r = want; r >= 0; r--)
        {
            var l = pool.FindAll(k => RarityI(Rarity(k)) == r && Rarity(k) != "legendary");
            if (l.Count > 0) return l[Random.Range(0, l.Count)];
        }
        var any = pool.FindAll(k => Rarity(k) != "legendary"); return (any.Count > 0 ? any : pool)[Random.Range(0, (any.Count > 0 ? any : pool).Count)];
    }

    // hand the pet over; the line that says what happened
    static string Give(AHGame g, string k, bool gilded, string rar)
    {
        var p = g.player; var s = S(p); string name = (gilded ? "Gilded " : "") + AHComp.PetName(k);
        string what;
        if (!StatsOn)
        {
            // looks only: a new pet, a golden one, or Essence for one you have
            if (!p.pets.Contains(k)) { p.pets.Add(k); if (gilded && !s.gilded.Contains(k)) s.gilded.Add(k); what = "New pet"; }
            else if (gilded && !s.gilded.Contains(k)) { s.gilded.Add(k); what = "Your " + AHComp.PetName(k).ToLowerInvariant() + " turns to gold"; }
            else { long es = (long)AHJson.N(D, "DUP_ESSENCE", 150); s.essence += es; what = "Already yours · +" + es + " Essence"; }
        }
        else if (!p.pets.Contains(k))
        {
            p.pets.Add(k);
            var rg = StarRange(gilded ? "legendary" : Rarity(k)); int st = Random.Range(rg[0], rg[1] + 1);
            SetStars(s, k, st); if (gilded && !s.gilded.Contains(k)) s.gilded.Add(k);
            what = "New pet · " + st + " star" + (st > 1 ? "s" : "");
        }
        else if (gilded && !s.gilded.Contains(k)) { s.gilded.Add(k); SetStars(s, k, 5); what = "Your " + AHComp.PetName(k).ToLowerInvariant() + " turns to gold · 5 stars"; }
        else if (Stars(p, k) < 5) { SetStars(s, k, Stars(p, k) + 1); what = "Already yours · now " + Stars(p, k) + " stars"; }
        else { long es = (long)AHJson.N(D, "DUP_ESSENCE", 150); s.essence += es; what = "Already five stars · +" + es + " Essence"; }
        s.hatched++;
        if (p.pet == k) AHComp.SpawnPet(g);
        g.ui.Banner(name + " (" + char.ToUpperInvariant(rar[0]) + rar.Substring(1) + ")", what);
        AHFx.Pop(p.transform.position + Vector3.up * 1.2f, 2.4f, gilded ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 0.95f, 0.8f), 0.8f);
        return name + " — " + what;
    }

    public static void Hatch(AHGame g, int i)
    {
        var p = g.player; var s = S(p); if (i < 0 || i >= s.eggs.Count || !Ready(s.eggs[i])) return;
        string el = s.eggs[i].k; s.eggs.RemoveAt(i);
        string rar = Roll(AHJson.O(D, "ODDS_EGG")); bool gl; string k = Pick(Pool(el), rar, out gl);
        Give(g, k, gl, gl ? "legendary" : Rarity(k));
        AHSound.Play("level"); g.SaveProgress();
    }

    // the store's Mystic egg: n at once, paid in Crowns; the lines of what hatched
    public static object Mystic { get { return AHJson.O(D, "MYSTIC"); } }
    public static long MysticPrice(int n) { return n >= 10 ? (long)AHJson.N(Mystic, "price10", 2700) : (long)AHJson.N(Mystic, "price", 300) * n; }
    public static List<string> BuyMystic(AHGame g, int n)
    {
        var p = g.player; var s = S(p); long c = MysticPrice(n); var lines = new List<string>();
        if (p.prog.crowns < c) { g.ui.Toast("Not enough " + AHStore.Currency + "."); return lines; }
        p.prog.crowns -= c;
        var pool = StorePool(); if (pool.Count == 0) { g.ui.Toast("The Mystic egg's pets are not here yet."); p.prog.crowns += c; return lines; }
        int pe = (int)AHJson.N(Mystic, "pity_epic", 30), pl = (int)AHJson.N(Mystic, "pity_legendary", 90);
        for (int j = 0; j < n; j++)
        {
            s.pityEpic++; s.pityLeg++; s.mystic++;
            string rar = Roll(AHJson.O(Mystic, "odds"));
            if (s.pityLeg >= pl) rar = "legendary"; else if (s.pityEpic >= pe && RarityI(rar) < 2) rar = "epic";
            if (rar == "legendary") { s.pityLeg = 0; s.pityEpic = 0; } else if (rar == "epic") s.pityEpic = 0;
            // the store pet of the rolled rarity (the nearest below it when there is none yet)
            string k = null;
            for (int r = RarityI(rar); r >= 0 && k == null; r--) { var l = pool.FindAll(x => AHJson.S(StorePets, x) == Rarities[r]); if (l.Count > 0) k = l[Random.Range(0, l.Count)]; }
            if (k == null) k = pool[Random.Range(0, pool.Count)];
            lines.Add(Give(g, k, false, AHJson.S(StorePets, k, rar)));
        }
        AHSound.Play("level"); g.SaveProgress();
        return lines;
    }
    // the Artisans' Silver egg: in-game coin, a few a day, hatches at once into one of the world's fighting pets (never a
    // store pet, never Gilded). For an Artisan a fighting pet is a companion to collect: looks only.
    public static object Silver { get { return AHJson.O(D, "SILVER"); } }
    public static long SilverPrice { get { return (long)AHJson.N(Silver, "price", 2000) * AHDB.CU; } }
    public static int SilverPerDay { get { return (int)AHJson.N(Silver, "per_day", 3); } }
    static string Today { get { return System.DateTime.UtcNow.ToString("yyyy-MM-dd"); } }
    public static int SilverLeft(AHPlayer p) { var s = S(p); return s.silverDay == Today ? Mathf.Max(0, SilverPerDay - s.silverN) : SilverPerDay; }
    public static string BuySilver(AHGame g)
    {
        var p = g.player; var s = S(p);
        if (p.path != "artisan") { g.ui.Toast("The Silver egg is for Artisans."); return null; }
        if (SilverLeft(p) <= 0) { g.ui.Toast("No more Silver eggs today. New ones tomorrow."); return null; }
        long c = SilverPrice; if (p.bag.money < c) { g.ui.Toast("A Silver egg costs " + AHItems.MoneyText(c) + "."); return null; }
        var pool = Pool(null); if (pool.Count == 0) return null;
        p.bag.money -= c; p.bag.Touch();
        if (s.silverDay != Today) { s.silverDay = Today; s.silverN = 0; }
        s.silverN++; s.silverAll++;
        string rar = Roll(AHJson.O(D, "ODDS_EGG")); if (rar == "legendary") rar = "epic";   // (no Gilded pets from silver)
        bool gl; string k = Pick(pool, rar, out gl);
        string line = Give(g, k, false, Rarity(k));
        AHSound.Play("level"); g.SaveProgress();
        return line;
    }

    public static string OddsLine()
    {
        var o = AHJson.O(Mystic, "odds"); var parts = new List<string>();
        foreach (var r in Rarities) parts.Add(RarityTag(r) + " " + AHJson.N(o, r, 0) + "%");
        return string.Join(" · ", parts.ToArray());
    }
}
