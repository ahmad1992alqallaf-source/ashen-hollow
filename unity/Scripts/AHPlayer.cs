// Ashen Hollow: the hero. Joystick to run, attack (melee or ranged by class), five class spells,
// dodge-roll, buffs and shields, levels.
using System.Collections.Generic;
using UnityEngine;

public class AHPlayer : MonoBehaviour
{
    // web v67: each class has its own class XP (P.cp[cls].xp, levels from CL_AT up to 90) and the hero has skills
    // (P.xp[skill], levels from XP_AT): Attack grows from kills, Skinning from skinning, and so on.
    [System.NonSerialized] public Dictionary<string, long> classXp = new Dictionary<string, long>();
    [System.NonSerialized] public Dictionary<string, long> banked = new Dictionary<string, long>();
    [System.NonSerialized] public Dictionary<string, long> skillXp = new Dictionary<string, long>();
    public int level { get { return AHDB.ClassLevel(ClassXp); } }
    public long ClassXp { get { long v; return cls != null && classXp.TryGetValue(cls.id, out v) ? v : 0; } set { if (cls != null) classXp[cls.id] = value; } }
    public int Skill(string s) { long v; return AHDB.SkillLevel(skillXp.TryGetValue(s, out v) ? v : 0); }
    public long SkillXp(string s) { long v; return skillXp.TryGetValue(s, out v) ? v : 0; }
    // how far through the current class level (for the XP bar)
    public float XpFrac
    {
        get
        {
            int L = level; var t = AHDB.ClAt;
            if (L >= AHDB.ClMax || L + 1 >= t.Length) return 1f;
            return Mathf.Clamp01((float)(ClassXp - t[L]) / Mathf.Max(1, t[L + 1] - t[L]));
        }
    }
    public float hunger = 100f;

    // ---------- who the hero is (web: P.name, P.look, P.path, P.peace, P.prof.mains) ----------
    public string heroName = "";
    [System.NonSerialized] public AHLook look = new AHLook();
    public string path = "adventurer";            // 'adventurer' or 'artisan'
    public int peace = -1;                         // -1 follow the path, 0 off, 1 on (web: P.peace null / false / true)
    [System.NonSerialized] public List<string> profs = new List<string>();
    public bool PeaceOn { get { return peace == 1 || (peace < 0 && path == "artisan"); } }

    // ---------- professions (web P.prof: mains, main = your focus, xp = mastery XP, spec) ----------
    public string profMain;
    [System.NonSerialized] public Dictionary<string, long> profXp = new Dictionary<string, long>();
    [System.NonSerialized] public Dictionary<string, string> profSpec = new Dictionary<string, string>();
    public static object Profs { get { return AHDB.Table("paths", "PROFS"); } }
    public int MasteryLv(string k) { long v; return AHDB.MasteryLevel(k != null && profXp.TryGetValue(k, out v) ? v : 0); }
    public bool Perk(string k, int lvl) { return profs.Contains(k) && MasteryLv(k) >= lvl; }
    public bool Spec(string k, string sp) { string v; return profs.Contains(k) && profSpec.TryGetValue(k, out v) && v == sp; }

    // web addMastery: banners for a new mastery level, perk and rank
    public void AddMastery(string k, int amt)
    {
        int before = MasteryLv(k);
        long v; profXp.TryGetValue(k, out v); profXp[k] = v + amt;
        int after = MasteryLv(k);
        if (after > before && g.ui != null)
        {
            var pr = AHJson.O(Profs, k); string pn = AHJson.S(pr, "name", k);
            string perkTxt = null;
            var perks = AHJson.A(pr, "perks");
            if (perks != null) foreach (var o in perks) { var row = o as List<object>; if (row != null && row.Count > 1 && (double)row[0] > before && (double)row[0] <= after) { perkTxt = (string)row[1]; break; } }
            string rk = null;
            var ranks = AHJson.A(AHDB.Rules, "PROF_RANKS");
            if (ranks != null) foreach (var o in ranks) { var row = o as List<object>; if (row != null && (double)row[0] > before && (double)row[0] <= after) rk = (string)row[1]; }
            g.ui.Banner(pn + " mastery " + after, rk != null ? "Rank: " + rk + " " + pn : perkTxt != null ? "New perk" : "Mastery up");
            if (perkTxt != null) g.ui.Toast("New " + pn.ToLowerInvariant() + " perk: " + perkTxt + ".", 4f);
            if (before < 15 && after >= 15 && !profSpec.ContainsKey(k)) g.ui.Toast("Mastery 15: choose your " + pn.ToLowerInvariant() + " specialisation with the Guild Clerk.", 4f);
        }
        if (after > before) g.quests.RoadCheck(g);
    }

    // is this skill the skill of one of the professions (web PROFS[k].skill)?
    static bool IsProfSkill(string skill, out List<string> ks)
    {
        ks = new List<string>();
        var all = Profs as Dictionary<string, object>;
        if (all != null) foreach (var kv in all) if (AHJson.S(kv.Value, "skill") == skill) ks.Add(kv.Key);
        return ks.Count > 0;
    }

    // the hero's model: the detailed one from the creator's look, or the KayKit class hero
    string builtSig;
    Transform weaponR, weaponL;
    void BuildModel()
    {
        if (model != null) Destroy(model);
        if (anim != null) anim.Dispose();
        model = null; anim = null;
        if (g.lookHero) model = AHPeople.BuildHero(transform, cls, look ?? new AHLook(), g, out anim);
        if (model == null)
        {
            model = AHModel.Spawn(transform, cls.model, 1.8f, false, g.heroYawFix, out anim);
            if (cls.tint != Color.white) Tint(model, cls.tint);
            if (anim != null) anim.alias = new Dictionary<string, string> { { "Shoot", "Interact" }, { "Chop", "Interact" }, { "Harvest", "Interact" } };
        }
        AHModel.SetShadows(model);
        weaponR = null; weaponL = null;
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("Weapon_")) { if (t.parent != null && t.parent.name == "hand_l") weaponL = t; else weaponR = t; }
        ApplyWeaponGlow();
        baseScale = model.transform.localScale;
        lookScale = Vector3.zero;
        modelBaseY = float.NaN;
        ghosted = false; ghostRs = null; ghostKeep = null;
        AHWardrobe.Dress(this, model);
        builtSig = LookSig();
    }
    public void CheckOutfit() { if (model != null && LookSig() != builtSig) ApplyLook(); }
    string LookSig() { var L = look ?? new AHLook(); return cls.id + L.sex + L.hair + L.beard + L.brow + L.skin + "," + L.hairCol + "," + L.cloth + g.lookHero + AHWardrobe.Sig(this); }

    void LateUpdate()
    {
        ToolTick();
        // weapon grip: each kind of weapon in its own way (AHPeople.Grip); with 'tuneGrip' ticked on the game object
        // the Inspector's weaponRot / weaponOffset are used instead, for trying out a new grip live
        if (g.tuneGrip)
        {
            if (weaponR != null) { weaponR.localRotation = Quaternion.Euler(g.weaponRot); weaponR.localPosition = g.weaponOffset; }
            if (weaponL != null) { weaponL.localRotation = Quaternion.Euler(g.offhandRot); weaponL.localPosition = g.offhandOffset; }
        }
    }

    // the look on this model: rebuilt when sex, hair, beard, brows or colours change; then body type and height
    public void ApplyLook()
    {
        if (model == null) return;
        if (LookSig() != builtSig) BuildModel();
        var L = AHJson.O(AHDB.Rules, "LOOK");
        var h = AHJson.A(L, "HEIGHT"); var b = AHJson.A(AHJson.O(L, "BODY"), look.body ?? "average");
        float hk = h != null && look.height >= 0 && look.height < h.Count ? (float)(double)h[look.height] : 1f;
        float bw = b != null && b.Count > 0 ? Mathf.Sqrt((float)(double)b[0]) : 1f;
        model.transform.localScale = new Vector3(baseScale.x * hk * bw, baseScale.y * hk, baseScale.z * hk * bw);
        lookScale = model.transform.localScale;
    }
    Vector3 lookScale = Vector3.zero;
    Vector3 LookScale { get { return lookScale != Vector3.zero ? lookScale : baseScale; } }
    [System.NonSerialized] public AHStats stat = new AHStats();
    float eatCd;
    public float hp, maxHp, mana, maxMana;
    [System.NonSerialized] public AHMealState meal;
    [System.NonSerialized] public AHProgress prog = new AHProgress();
    // class progression: passives from path, form and talents; the ring of five spells (and the ultimate)
    public bool Moving { get; private set; }
    [System.NonSerialized] public Dictionary<string, float> pass = new Dictionary<string, float>();
    SpellDef[] ring;
    public SpellDef[] Spells { get { return ring != null && ring.Length >= 5 ? ring : cls.spells; } }
    public void RebuildRing() { if (cls == null) return; ring = AHEvo.Ring(this); AHEvo.Aura(this); }
    public float speed = 6.4f;
    public bool dead;
    [System.NonSerialized] public ClassDef cls;
    public readonly float[] cds = new float[6];
    public AHMob target;
    // companions and the bank (web P.pets, P.pet, P.petXp, P.mounts, P.mountSel, P.mountXp, P.party, P.bank)
    [System.NonSerialized] public List<string> pets = new List<string>(), mounts = new List<string>(), party = new List<string>();
    [System.NonSerialized] public string pet, mountSel;
    [System.NonSerialized] public Dictionary<string, long> petXp = new Dictionary<string, long>(), mountXp = new Dictionary<string, long>();
    [System.NonSerialized] public Dictionary<string, int> bank = new Dictionary<string, int>();
    [System.NonSerialized] public bool mounted;
    // work orders and guild standing (web P.prof.orders / rep / done), and what you bought rather than made (web P.bought)
    [System.NonSerialized] public List<AHOrder> orders = new List<AHOrder>();
    [System.NonSerialized] public int guildRep, ordersDone;
    [System.NonSerialized] public AHHomeState home;   // null until you buy the deed
    [System.NonSerialized] public long rested;        // bonus XP: kills give double until it runs out (web P.rested)
    [System.NonSerialized] public Dictionary<string, long> bought = new Dictionary<string, long>();
    public void NoteBought(string id, int n) { if (n <= 0) return; long b; bought.TryGetValue(id, out b); bought[id] = b + n; }
    public int BoughtOf(string id) { long b; bought.TryGetValue(id, out b); return (int)Mathf.Min(bag.Count(id), b); }
    public int OwnOf(string id) { return Mathf.Max(0, bag.Count(id) - BoughtOf(id)); }
    [System.NonSerialized] public GameObject mountGo;
    [System.NonSerialized] public float seatY;
    public float LastHurt { get { return lastHurt; } }
    public bool InFight { get { return Time.time - lastHurt < 6f || Time.time - lastCast < 4f; } }
    [System.NonSerialized] public AHBag bag = new AHBag();

    // buffs
    public float shield, shieldT, invulnT, hotT, hotRate, dmgBuff, dmgBuffT, stealthT, bearT;
    bool critNext;

    AHGame g;
    AHAnim anim;
    GameObject model, targetRing, shieldOrb;
    float atkCd, atkT, hitAt, dodgeCd, dodgeT, iframe, deadT, lastHurt = -99f, whirlT, whirlTick;
    Vector3 dodgeDir;
    AHMob hitTarget;
    SpellDef whirlSpell;
    public const float Radius = 0.35f;

    public static AHPlayer Create(AHGame game, Vector3 pos, string classId)
    {
        var go = new GameObject("Hero");
        go.transform.position = pos;
        var p = go.AddComponent<AHPlayer>();
        p.g = game;
        if (game.data.playerSpeed > 0) p.speed = game.data.playerSpeed;
        p.SetClass(classId);
        p.bag.Changed += p.RefreshStats;
        p.targetRing = AHFx.Keep(PrimitiveType.Quad, new Color(1f, 0.35f, 0.25f, 0.9f), 1f);
        p.targetRing.transform.localScale = Vector3.one * 1.6f;
        p.targetRing.SetActive(false);
        p.shieldOrb = AHFx.Keep(PrimitiveType.Sphere, new Color(0.5f, 0.8f, 1f, 0.35f), 0f);
        p.shieldOrb.transform.SetParent(go.transform, false);
        p.shieldOrb.transform.localPosition = Vector3.up * 0.95f;
        p.shieldOrb.transform.localScale = new Vector3(1.3f, 2.1f, 1.3f);
        p.shieldOrb.SetActive(false);
        return p;
    }

    public void SetClass(string id)
    {
        string prev = cls != null ? cls.id : null;
        cls = AHClasses.Get(id);
        if (model != null) Destroy(model);
        if (anim != null) anim.Dispose();
        BuildModel();
        for (int i = 0; i < cds.Length; i++) cds[i] = 0f;
        // web: a new class puts on its own starter outfit in place of starter pieces; anything it can't use goes to the bag
        var st = Starter(cls.id);
        if (prev != null && prev != cls.id)
            foreach (var sl in AHItems.GearSlots)
            {
                var cur = AHItems.Get(bag.Worn(sl));
                if (cur == null || cur.rarity == "starter") { if (st.ContainsKey(sl)) bag.gear[sl] = st[sl]; else bag.gear.Remove(sl); }
            }
        int moved = 0;
        foreach (var sl in AHItems.GearSlots)
        {
            var cur = bag.Worn(sl);
            if (cur == null || AHItems.CanUse(cur, cls.id)) continue;
            if (AHItems.Get(cur).rarity != "starter" && bag.Add(cur)) moved++;
            if (st.ContainsKey(sl)) bag.gear[sl] = st[sl]; else bag.gear.Remove(sl);
        }
        if (moved > 0 && g.ui != null) g.ui.Toast(moved + " item" + (moved > 1 ? "s" : "") + " your class can’t use went to your bag");
        Recalc();
        hp = maxHp; mana = maxMana;
        bearT = 0f; stealthT = 0f; ghosted = false; ghostRs = null; ghostKeep = null;
        ApplyLook();
    }

    Vector3 baseScale = Vector3.one;

    // gear changed: web recalcStats (current health and mana stay, up to the new tops)
    public void RefreshStats() { Recalc(); }
    public void Recalc()
    {
        stat = bag.Stats();
        AHFinder.Apply(stat);
        AHPower.Apply(this, stat);
        AHWear.Apply(this, stat);   // worn and broken gear gives less
        pass = AHEvo.CalcPass(this);
        stat.dmg += AHEvo.Pass(this, "dmg"); stat.hp += Mathf.RoundToInt(AHEvo.Pass(this, "hp")); stat.heal += AHEvo.Pass(this, "heal");
        stat.crit += AHEvo.Pass(this, "crit"); stat.evade += AHEvo.Pass(this, "evade"); stat.aspd += AHEvo.Pass(this, "atkspd");
        if (cls != null && cls.id == "shaman") stat.evade += 0.08f;   // agility, not shields: the shaman's defence
        ApplyCaps(stat);
        RebuildRing();
        maxHp = MaxHpFor(level);
        hp = Mathf.Min(hp, maxHp);
        maxMana = MaxManaFor(level);
        mana = Mathf.Min(mana, maxMana);
    }

    // how much a heal of 'pct' restores, by class (see the Heal spell)
    public float HealAmount(float pct)
    {
        if (cls.id == "druid") return maxHp * pct * (1f + AHEvo.Pass(this, "pctHeal"));
        if (cls.id == "priest") return maxHp * pct * (0.6f + 0.8f * (maxMana > 0f ? Mathf.Clamp01(mana / maxMana) : 0.5f)) * (1f + AHEvo.Pass(this, "manaHeal"));
        return maxHp * pct;
    }

    // stat caps, so stacked gear, gems, sets and talents can't run away (shown on the Stats page)
    public static readonly AHStats Cap = new AHStats { dmg = 2f, crit = 0.5f, evade = 0.4f, aspd = 0.75f, cdr = 0.4f, heal = 1.5f, hpK = 1f, manaK = 1f };
    public static void ApplyCaps(AHStats s)
    {
        s.dmg = Mathf.Min(s.dmg, Cap.dmg); s.crit = Mathf.Min(s.crit, Cap.crit); s.evade = Mathf.Min(s.evade, Cap.evade); s.aspd = Mathf.Min(s.aspd, Cap.aspd);
        s.cdr = Mathf.Min(s.cdr, Cap.cdr); s.heal = Mathf.Min(s.heal, Cap.heal); s.hpK = Mathf.Min(s.hpK, Cap.hpK); s.manaK = Mathf.Min(s.manaK, Cap.manaK);
    }

    // web STARTERS: the outfit each class starts in
    public static Dictionary<string, string> Starter(string clsId)
    {
        var d = new Dictionary<string, string>();
        var st = AHJson.O(AHJson.O(AHDB.Rules, "STARTERS"), clsId);
        if (st != null) foreach (var kv in st) if (kv.Value is string) d[kv.Key] = (string)kv.Value;
        return d;
    }

    // a brand-new hero: the class outfit worn and two grilled trout in the bag (web: P.inv = { trout: 2 })
    public void StartKit()
    {
        bag.gear.Clear();
        foreach (var kv in Starter(cls.id)) bag.gear[kv.Key] = kv.Value;
        var inv = AHJson.O(AHDB.Rules, "START_INV");
        if (inv != null) foreach (var kv in inv) bag.Add(kv.Key, (int)(double)kv.Value);
        Recalc(); hp = maxHp; mana = maxMana; hunger = 100f;
    }

    static void Tint(GameObject go, Color tint)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", m.GetColor("baseColorFactor") * tint);
                else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * tint);
            }
            r.materials = mats;
        }
    }

    // web maxManaOf: 100 + 3 a class level + 1.5 per point of gear attack
    public float MaxManaFor(int lv) { return cls != null && cls.mana ? Mathf.Round((100f + (lv - 1) * 3f + stat.atk * 1.5f) * (1f + stat.manaK)) : 0f; }
    float lastCast = -99f, autoTake;
    // web: P.maxHp = cls.hp * (1 + (lv - 1) * 0.04) + gear hp + (lv - 1) * 3
    public float MaxHpFor(int lv) { return Mathf.Round(((cls != null ? cls.hp : 100f) * (1f + (lv - 1) * 0.04f) + stat.hp + (lv - 1) * 3f) * AHFinder.HpK * (1f + AHMeal.Fx(this, "hp")) * (1f + stat.hpK)); }
    // web damage(): base 4 + Attack level * 0.7 + gear attack, times buffs; Power is that without the dice
    public float Power
    {
        get
        {
            float b = 4f + Skill("attack") * 0.7f + stat.atk;
            float buff = (dmgBuffT > 0f ? 1f + dmgBuff : 1f) * (1f + stat.dmg) * (1f + (level - 1) * 0.01f) * (bearT > 0f ? 1.2f : 1f)
                * (Elix("might") ? 1.2f : 1f) * (Elix("dragon") ? 1.15f : 1f) * (Elix("titan") ? 1.25f : 1f) * (Elix("raid") ? 1.1f : 1f) * (1f + AHMeal.Fx(this, "dmg")) * (AHEvo.Pass(this, "rage") > 0f && hp < maxHp * 0.5f ? 1.2f : 1f);
            return b * buff;
        }
    }
    public float Damage() { return Mathf.Max(1f, Power * Random.Range(0.7f, 1.3f) * (hunger <= 0f ? 0.8f : 1f)); }
    // web: armorCut = 1 - 100 / (100 + armor * 5)
    public float ArmorCut { get { return 1f - 100f / (100f + stat.def * 5f * (1f + (Elix("iron") ? 0.3f : 0f) + (Elix("titan") ? 0.25f : 0f) + AHMeal.Fx(this, "def") + AHEvo.Pass(this, "armor"))); } }

    // ---------- wading: the shallow rim of a pond slows you, you sink a little and the water ripples ----------
    float wadeDepth, rippleT, modelBaseY = float.NaN;
    void UpdateWading(float dt, bool moving)
    {
        float wd = g.WaterDepth(transform.position);
        wadeDepth = Mathf.MoveTowards(wadeDepth, wd, dt * 3f);
        if (model != null)
        {
            if (float.IsNaN(modelBaseY)) modelBaseY = model.transform.localPosition.y;
            var lp = model.transform.localPosition; lp.y = modelBaseY - 0.4f * wadeDepth * (mounted ? 0.3f : 1f) + seatY + leapY; model.transform.localPosition = lp;
        }
        if (wd <= 0.02f) return;
        rippleT -= dt;
        if (rippleT <= 0f)
        {
            rippleT = moving ? 0.32f : 1.3f;
            g.Ripple(transform.position, moving ? 1f : 0.5f);
            if (moving) AHFx.Pop(transform.position + Vector3.up * 0.15f + transform.forward * 0.3f, 0.5f, new Color(0.85f, 0.93f, 1f, 0.7f), 0.3f);
        }
    }

    // ---------- potions and elixirs (web drink, quickPotion, P.elix, P.potCd, P.manaCd) ----------
    public readonly Dictionary<string, float> elix = new Dictionary<string, float>();
    public float potCd, manaCd;
    public static readonly string[] HealOrder = { "tide_tonic", "big_potion", "witch_brew", "hp_potion" }, ManaOrder = { "great_mana", "mana_potion" };
    public bool Elix(string k) { float t; return elix.TryGetValue(k, out t) && Time.time < t; }
    public float SpeedMult { get { return (Elix("swift") ? 1.25f : 1f) * (Elix("dragon") ? 1.15f : 1f) * (1f + AHMeal.Fx(this, "speed")); } }
    public int PotionCount(bool mana) { int n = 0; foreach (var k in mana ? ManaOrder : HealOrder) n += bag.Count(k); return n; }

    // raw: a heal that is a straight share of max HP (the druid's), not boosted by the healing stat
    public void Heal(float n, bool raw = false)
    {
        n = Mathf.Round(raw ? n : n * (1f + stat.heal));
        float before = hp; hp = Mathf.Min(maxHp, hp + n);
        if (hp - before < 0.5f) return;   // already full: no "+0 HP" and no flash
        if (n >= maxHp * 0.08f) AHSound.Play("heal");
        if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.3f, "+" + Mathf.RoundToInt(hp - before) + " HP", new Color(0.61f, 0.89f, 0.48f));
        AHFx.Pillar(transform.position, 0.6f, 2.2f, new Color(0.5f, 1f, 0.5f, 0.5f), 0.6f);
    }

    public void Drink(string id)
    {
        var d = AHItems.Get(id); if (d == null || !d.potion || dead) return;
        var pot = AHJson.O(AHJson.O(AHDB.Items, id), "potion");
        float now = Time.time;
        var ui = g.ui;
        if (AHJson.Has(pot, "mana"))
        {
            if (maxMana <= 0f) { if (ui != null) ui.Toast("Warriors have no mana to restore."); return; }
            if (now < manaCd) { if (ui != null) ui.Toast("Mana potions are on cooldown (" + Mathf.CeilToInt(manaCd - now) + "s)."); return; }
            if (!bag.Take(id)) return;
            manaCd = now + 15f; float b4 = mana; mana = Mathf.Min(maxMana, mana + (float)AHJson.N(pot, "mana"));
            if (ui != null) ui.Float(transform.position + Vector3.up * 2.3f, "+" + Mathf.RoundToInt(mana - b4) + " mana", new Color(0.5f, 0.69f, 1f));
            AHFx.Pillar(transform.position, 0.6f, 2.2f, new Color(0.45f, 0.6f, 1f, 0.5f), 0.6f);
            return;
        }
        bool heal = AHJson.Has(pot, "heal");
        if (heal && now < potCd) { if (ui != null) ui.Toast("Healing potions are on cooldown (" + Mathf.CeilToInt(potCd - now) + "s)."); return; }
        if (!bag.Take(id)) return;
        if (heal) { potCd = now + 20f * (Spec("alchemist", "healer") ? 0.7f : 1f); Heal((float)AHJson.N(pot, "heal") * (Perk("alchemist", 10) ? 1.2f : 1f)); }
        string buff = AHJson.S(pot, "buff");
        if (buff != null)
        {
            float k = (Perk("alchemist", 40) ? 1.5f : 1f) * (Spec("alchemist", "mixer") ? 1.5f : 1f);
            elix[buff] = now + (float)AHJson.N(pot, "dur", 180) * k;
            if (ui != null)
            {
                if (buff == "dragon") ui.Banner("Dragonfire", "+15% damage and speed for 5 minutes");
                ui.Toast(d.name + ": " + ElixText(buff) + ".", 3f);
            }
            AHFx.Pop(transform.position + Vector3.up * 1.2f, 1.6f, new Color(1f, 0.85f, 0.4f), 0.5f);
            g.MarkDirty();
        }
    }

    public static string ElixText(string buff)
    {
        switch (buff)
        {
            case "might": return "+20% damage for 3 minutes";
            case "swift": return "+25% speed for 3 minutes";
            case "iron": return "+30% armor for 3 minutes";
            case "dragon": return "+15% damage and speed for 5 minutes";
            case "ward": return "25% less damage taken for 3 minutes";
            case "titan": return "+25% damage and armor for 10 minutes";
            case "raid": return "+10% damage and 20% less damage taken for 10 minutes";
        }
        return buff;
    }

    // web quickPotion: the best potion you carry
    public void QuickPotion(bool manaPot)
    {
        if (dead) return;
        foreach (var k in manaPot ? ManaOrder : HealOrder) if (bag.Count(k) > 0) { Drink(k); return; }
        if (g.ui != null) g.ui.Toast(manaPot ? "No mana potions. Alchemists sell them, or brew them from herbs." : "No health potions. General stores and alchemists sell them.");
    }
    float ClassCrit { get { return (float)AHJson.N(AHJson.O(AHDB.Classes, cls.id), "crit"); } }
    // one hit's damage: web damage() * the spell's multiplier, then criticals (x2.5 after Vanish, x2 by chance)
    int Roll(float mult)
    {
        float d = Damage() * mult;
        float cr = ClassCrit + stat.crit;
        if (critNext) { d *= 2.5f; critNext = false; }
        else if (cr > 0f && Random.value < cr) d *= 2f;
        return Mathf.Max(1, Mathf.RoundToInt(d));
    }
    public bool Stealthed { get { return stealthT > 0f; } }

    void OnDestroy() { if (anim != null) anim.Dispose(); }

    void Update()
    {
        float dt = Time.deltaTime;
        // the game is paused while you make or restyle your hero: keep them breathing instead of a stiff T-pose
        if (dt <= 0f) { if (anim != null && g.ui != null && g.ui.CreatorOpen) { anim.Play("Idle", true); anim.Tick(Time.unscaledDeltaTime); } return; }
        if (anim != null) anim.Tick(dt);
        if (dead)
        {
            deadT -= dt;
            if (deadT <= 0f) Respawn();
            targetRing.SetActive(false);
            return;
        }
        atkCd -= dt * (1f + AHMeal.Fx(this, "atkspd") + stat.aspd); dodgeCd -= dt; iframe -= dt;
        for (int i = 0; i < cds.Length; i++) cds[i] -= dt;
        Buffs(dt);

        // steering, relative to where the camera looks
        Vector2 stick = g.ui != null ? g.ui.stick : Vector2.zero;
        Vector2 keys = AHInput.Keys();
        if (keys.sqrMagnitude > stick.sqrMagnitude) stick = keys;
        if (AHCine.Active || (g.ui != null && g.ui.ChatOpen)) stick = Vector2.zero;   // a cutscene is playing, or you are typing
        Vector3 move = g.CamForward() * stick.y + g.CamRight() * stick.x;
        if (move.sqrMagnitude > 1f) move.Normalize();
        // AUTO quest steers when you are not (moving the stick takes over)
        if (AHAuto.On)
        {
            autoTake = stick.sqrMagnitude > 0.09f ? autoTake + dt : 0f;
            if (autoTake > 0.2f) { Debug.Log("Ashen Hollow: auto off, stick " + stick); AHAuto.Stop(g, "Auto quest off: you took the controls."); }
            else move = AHAuto.Steer(this, dt);
        }

        bool keysOff = g.ui != null && g.ui.ChatOpen;   // letters typed in the chat are not fight keys
        bool wantDodge = (g.ui != null && g.ui.dodgePressed) || (!keysOff && AHInput.DodgeKey());
        if (wantDodge && dodgeCd <= 0f)
        {
            if (mounted) AHComp.Dismount(g, true);
            dodgeDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
            Dash(dodgeDir, 0.32f, 2.2f);
            PlayPose("roll");
            dodgeCd = 1.4f; iframe = 0.4f; AHSound.Play("whoosh");
        }

        // lightfoot (qinggong): a long bounding leap, up to three in a row, the breath coming back over a few seconds
        if (leapCharges < LeapMax) { leapRegen += dt; if (leapRegen >= 3.5f) { leapRegen = 0f; leapCharges++; } }
        bool wantLeap = (g.ui != null && g.ui.leapPressed) || AHInput.LeapKey();
        if (wantLeap && leapT <= 0f && leapCharges > 0 && dodgeT <= 0f && !dead && LeapUnlocked)
        {
            if (mounted) AHComp.Dismount(g, true);
            leapCharges--; leapRegen = 0f;
            leapDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward; leapDur = 0.75f; leapT = leapDur;
            transform.rotation = g.Face(leapDir); StopEmote(); AHGather.Cancel();
            if (anim != null && !anim.Play("NinjaJump_Start", false, 1.1f, true)) anim.Play("Running_A", true, 2.2f, true);
            atkT = Mathf.Max(atkT, 0.1f); iframe = Mathf.Max(iframe, 0.2f);
            AHSound.Play("whoosh");
            AHFx.Ring(transform.position, 0.2f, 1.2f, new Color(0.85f, 0.95f, 1f, 0.7f), 0.35f);
        }

        Vector3 pos = transform.position;
        bool moving = false;
        if (leapT > 0f)
        {
            leapT -= dt; float f = 1f - Mathf.Clamp01(leapT / leapDur);
            leapY = Mathf.Sin(f * Mathf.PI) * 1.9f;
            pos += leapDir * speed * 2.6f * dt;
            if (Mathf.Repeat(leapT, 0.08f) < dt) AHSpark.Trail(pos + Vector3.up * (leapY + 0.9f), new Color(0.85f, 0.95f, 1f, 0.6f), 0.12f, 1);
            if (leapT <= 0f) { leapY = 0f; if (anim != null) anim.Play("NinjaJump_Land", false, 1.3f, true); atkT = Mathf.Max(atkT, 0.15f); AHFx.Ring(transform.position, 0.2f, 1.4f, new Color(0.85f, 0.95f, 1f, 0.6f), 0.35f); }
            moving = true;
        }
        else if (dodgeT > 0f)
        {
            dodgeT -= dt;
            pos += dodgeDir * (speed * dashMul) * dt;
            transform.rotation = g.Face(dodgeDir);
            if (dashMul > 2.5f) AHSpark.Trail(pos + Vector3.up * UnityEngine.Random.Range(0.4f, 1.4f), new Color(1f, 0.85f, 0.6f, 0.7f), 0.14f, 1);
            moving = true;
        }
        else if (atkT <= 0f && move.sqrMagnitude > 0.0025f)
        {
            float step = speed * (bearT > 0f ? 1.05f : 1f) * SpeedMult * AHComp.MountSpeed(this) * (1f - 0.4f * wadeDepth * (mounted ? 0.4f : 1f)) * dt;
            pos += move * step;
            AHComp.RideTick(g, step * move.magnitude);
            // a mount at a gallop kicks up dust behind it
            if (mounted && (dustT -= dt) <= 0f) { dustT = 0.11f; AHFx.Pop(transform.position - move.normalized * 1.1f + Vector3.up * 0.12f + transform.right * UnityEngine.Random.Range(-0.3f, 0.3f), 0.8f, new Color(0.78f, 0.7f, 0.56f, 0.4f), 0.55f); }
            transform.rotation = Quaternion.Slerp(transform.rotation, g.Face(move), 1f - Mathf.Exp(-dt * 14f));
            moving = true;
        }
        transform.position = g.Resolve(g.PushFromPeople(pos, Radius), Radius, AHGame.Wade);
        // footsteps, by what is underfoot
        if (moving && leapT <= 0f && dodgeT <= 0f && (stepT -= dt) <= 0f)
        {
            stepT = mounted ? 0.36f : 0.31f;
            AHSound.Play(mounted ? "step_hoof" : wadeDepth > 0.05f ? "step_water" : (g.InTown(transform.position) || AHDungeon.IsDungeon(AHGame.AreaId)) ? "step_stone" : "step_grass");
        }
        Moving = moving && dodgeT <= 0f;
        UpdateWading(dt, moving);

        // keep a target: the one you hit, or the nearest in reach
        if (target != null && (target.dead || Dist(target) > Range + 8f)) target = null;
        if (target == null) target = Nearest(Ranged ? Range : 4f);
        targetRing.SetActive(target != null);
        if (target != null) targetRing.transform.position = target.transform.position + Vector3.up * 0.06f;

        // basic attack
        bool wantAtk = (g.ui != null && g.ui.attackHeld) || (!keysOff && AHInput.AttackKey()) || (AHAuto.On && AHAuto.WantAttack);
        if (wantAtk && atkCd <= 0f && dodgeT <= 0f) Attack();

        // spells
        for (int i = 0; i < Spells.Length && i < 6; i++)
            if (((g.ui != null && g.ui.spellPressed[i]) || (!keysOff && AHInput.SpellKey(i)) || AHAuto.WantCast(i)) && cds[i] <= 0f && dodgeT <= 0f) Cast(i);

        if (atkT > 0f)
        {
            atkT -= dt;
            if (hitAt > 0f)
            {
                hitAt -= dt;
                if (hitAt <= 0f && !Ranged)
                {
                    AHMob t = hitTarget != null && !hitTarget.dead ? hitTarget : Nearest(Range);
                    if (t != null && Dist(t) < Range + 0.3f) { t.Hurt(Roll(cls.atkMult * comboMul), this); OnHitFx(t); }
                }
            }
        }
        if (whirlT > 0f)
        {
            whirlT -= dt; whirlTick -= dt;
            if (whirlTick <= 0f) { whirlTick = 0.3f; HitAround(transform.position, whirlSpell.radius, whirlSpell.mult, whirlSpell); AHFx.Ring(transform.position, 0.5f, whirlSpell.radius, whirlSpell.color, 0.3f); AHSpark.Ring(transform.position + Vector3.up * 0.9f, whirlSpell.color, 16, 5f, 0.35f); }
        }

        // web: hunger falls 0.33 a second out in the wild; health comes back 1.1 a second, 8 s after the last hit, if not hungry
        bool town = g.InTown(transform.position);
        if (!town) hunger = Mathf.Max(0f, hunger - dt * 0.33f);
        if (hunger < 25f && !hungerWarned) { hungerWarned = true; if (g.ui != null) g.ui.Toast("You are hungry. HP stops regenerating. Eat something."); }
        if (hunger >= 30f) hungerWarned = false;
        if (Time.time - lastHurt > 8f && hunger >= 25f && hp < maxHp) hp = Mathf.Min(maxHp, hp + (town ? 3f : 1.1f) * dt);
        UpdateAction(dt, moving);
        // web: mana comes back 7 a second once you have not cast or been hurt for 4 s, 2.2 a second otherwise
        if (maxMana > 0f && mana < maxMana)
        {
            bool calm = Time.time - lastHurt > 4f && Time.time - lastCast > 4f;
            mana = Mathf.Min(maxMana, mana + dt * (1f + AHMeal.Fx(this, "mana")) * (calm ? (town ? 14f : 7f) : 2.2f));
        }
        if (anim != null && mounted) { if (!anim.Play("Ride", true)) anim.Play("Idle", true); }
        if (poseT > 0f) { poseT -= dt; if (moving && !poseHard && dodgeT <= 0f) poseT = 0f; }
        if (emote != null)
        {
            var ed = AHEmote.Find(emote); emoteT -= dt;
            if (ed == null || moving || atkT > 0f || dodgeT > 0f || whirlT > 0f || Busy || mounted || dead || (!ed.loop && emoteT <= 0f)) StopEmote();
        }
        // standing about: now and then the hero folds their arms and waits
        if (moving || atkT > 0f || Busy || emote != null || mounted) idleT = 0f; else idleT += dt;
        string idleClip = idleT > 10f && Mathf.Repeat(idleT - 10f, 22f) < 5f && anim != null && anim.Has("Idle_FoldArms_Loop") ? "Idle_FoldArms_Loop" : "Idle";
        if (anim != null && mounted) { }
        else if (anim != null && dodgeT <= 0f && atkT <= 0f && whirlT <= 0f && poseT <= 0f && !Busy && emote == null) anim.Play(moving ? "Running_A" : idleClip, true);
    }

    float dashMul = 2.2f;
    int comboN; float lastSwing = -9f, comboMul = 1f, perfectCd, idleT, dustT, stepT;
    // lightfoot
    public const int LeapMax = 3; public const int LeapLevel = 5;
    [System.NonSerialized] public int leapCharges = LeapMax; float leapRegen, leapT, leapDur = 0.75f, leapY; Vector3 leapDir;
    public bool LeapUnlocked { get { return level >= LeapLevel; } }
    public bool Leaping { get { return leapT > 0f; } }

    // ---------- emotes ----------
    [System.NonSerialized] public string emote; float emoteT; AHEmotePose emotePose;
    public void Emote(string id)
    {
        var e = AHEmote.Find(id); if (e == null || dead) return;
        if (mounted) { if (g.ui != null) g.ui.Toast("Get off your mount first."); return; }
        if (Busy) return;
        StopEmote();
        emote = id; emoteT = e.time;
        if (anim != null && !anim.Play(e.clip, e.loop, e.speed, true)) anim.Play("Idle", true);
        if (e.proc != null && model != null)
        {
            if (emotePose == null) emotePose = gameObject.AddComponent<AHEmotePose>();
            emotePose.Begin(model, e.proc);
        }
        if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.3f, (string.IsNullOrEmpty(heroName) ? "You" : heroName) + " " + e.verb, e.col);
    }
    public void StopEmote()
    {
        if (emote == null) return;
        emote = null;
        if (emotePose != null) emotePose.End();
    }

    // cast animations: the clip (Quaternius UAL, qAnims / qAnims2), its speed, how long it roots you (atkT) and how long
    // it plays before walking or idling takes over again (moving cuts it short unless it is a roll)
    struct PoseDef { public string clip; public float speed, lockT, holdT; public bool hard; }
    static PoseDef P(string c, float s, float l, float h, bool hard = false) { return new PoseDef { clip = c, speed = s, lockT = l, holdT = h, hard = hard }; }
    static readonly Dictionary<string, PoseDef> Poses = new Dictionary<string, PoseDef>
    {
        { "slash", P("Sword_Regular_A", 1.0f, 0.3f, 0.45f) },
        { "slash2", P("Sword_Regular_B", 1.0f, 0.35f, 0.55f) },
        { "heavy", P("Sword_Attack", 1.5f, 0.5f, 1.0f) },
        { "stab", P("Punch_Jab", 1.3f, 0.3f, 0.65f) },
        { "spin", P("Sword_Regular_C", 1.65f, 0f, 1.2f) },
        { "slam", P("NinjaJump_Land", 1.4f, 0.45f, 0.9f) },
        { "burst", P("Idle_Shield_Break", 1.3f, 0.35f, 0.8f) },
        { "roar", P("Idle_Shield_Break", 1.0f, 0.4f, 1.05f) },
        { "bolt", P("Spell_Simple_Shoot", 1.1f, 0.3f, 0.45f) },
        { "shoot", P("Pistol_Shoot", 1.0f, 0.3f, 0.6f) },
        { "push", P("Shield_OneShot", 1.25f, 0.35f, 0.65f) },
        { "raise", P("Spell_Simple_Enter", 0.8f, 0.3f, 0.65f) },
        { "throw", P("OverhandThrow", 1.6f, 0.4f, 0.8f) },
        { "kneel", P("Farm_PlantSeed", 2.2f, 0.6f, 1.2f) },
        { "dash", P("Sword_Dash", 2.0f, 0f, 0.75f, true) },
        { "hop", P("NinjaJump_Start", 1.6f, 0f, 0.6f, true) },
        { "roll", P("Roll", 3.0f, 0f, 0.48f, true) },
    };
    float poseT; bool poseHard; int swingN;
    static readonly HashSet<string> missingPoses = new HashSet<string>();
    public static IEnumerable<string> PoseKeys { get { return Poses.Keys; } }
    public void PlayPose(string key)
    {
        if (key == null || anim == null) return;
        PoseDef d;
        if (!Poses.TryGetValue(key, out d) || !anim.Has(d.clip))
        {
            if (missingPoses.Add(key)) Debug.LogWarning("Ashen Hollow: cast animation " + key + " (" + d.clip + ") not found, using the plain one");
            // older animation sets: fall back to the original swing or cast
            if (anim.Play(Ranged ? "Shoot" : "1H_Melee_Attack", false, 1.6f, true)) { atkT = Mathf.Max(atkT, 0.45f); hitAt = 0f; }
            return;
        }
        anim.Play(d.clip, false, d.speed, true);
        if (d.lockT > 0f) { atkT = Mathf.Max(atkT, d.lockT); hitAt = 0f; }
        poseT = d.holdT; poseHard = d.hard;
    }
    void Dash(Vector3 dir, float time, float mul)
    {
        dodgeDir = dir; dodgeT = time; dashMul = mul; atkT = 0f;
        if (anim != null) anim.Play("Running_A", true, 2.2f, true);
    }

    void Buffs(float dt)
    {
        if (shieldT > 0f) { shieldT -= dt; if (shieldT <= 0f) shield = 0f; }
        if (invulnT > 0f) invulnT -= dt;
        if (dmgBuffT > 0f) dmgBuffT -= dt;
        if (stealthT > 0f) stealthT -= dt;
        if (hotT > 0f) { hotT -= dt; hp = Mathf.Min(maxHp, hp + hotRate * dt); }
        if (bearT > 0f)
        {
            bearT -= dt;
            model.transform.localScale = LookScale * 1.25f;
            if (bearT <= 0f) model.transform.localScale = LookScale;
        }
        shieldOrb.SetActive(shield > 0f || invulnT > 0f);
        if (shieldOrb.activeSelf) AHFx.Paint(shieldOrb, invulnT > 0f ? new Color(1f, 0.85f, 0.4f, 0.4f) : new Color(0.5f, 0.8f, 1f, 0.3f), 0f);
        // see-through while hidden
        SetGhost(stealthT > 0f);
    }

    // ---------- Vanish: swap the hero's materials for the see-through ghost material and back ----------
    static Material ghostMat;
    Renderer[] ghostRs;
    Material[][] ghostKeep;
    bool ghosted;

    void SetGhost(bool on)
    {
        if (on == ghosted || model == null) return;
        if (ghostMat == null) ghostMat = AHGame.LoadMat("AH/Materials/Ghost", "AshenHollow/Ghost");
        if (ghostMat == null) return;
        ghosted = on;
        if (on)
        {
            ghostRs = model.GetComponentsInChildren<Renderer>(true);
            ghostKeep = new Material[ghostRs.Length][];
            for (int i = 0; i < ghostRs.Length; i++)
            {
                ghostKeep[i] = ghostRs[i].sharedMaterials;
                var ms = new Material[ghostKeep[i].Length];
                for (int k = 0; k < ms.Length; k++) ms[k] = ghostMat;
                ghostRs[i].sharedMaterials = ms;
                ghostRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            AHFx.Pop(transform.position + Vector3.up, 1.8f, new Color(0.6f, 0.45f, 1f, 0.6f), 0.4f);
        }
        else
        {
            if (ghostRs != null)
                for (int i = 0; i < ghostRs.Length; i++)
                {
                    if (ghostRs[i] == null) continue;
                    ghostRs[i].sharedMaterials = ghostKeep[i];
                    ghostRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            ghostRs = null; ghostKeep = null;
        }
    }

    float Dist(AHMob m) { Vector3 d = m.transform.position - transform.position; d.y = 0; return d.magnitude - m.type.radius; }

    AHMob Nearest(float reach)
    {
        AHMob best = null;
        float bd = float.MaxValue;
        Vector3 f = transform.forward;
        foreach (var m in g.mobs)
        {
            if (m.dead || !m.gameObject.activeInHierarchy) continue;
            float dist = Dist(m);
            if (dist > reach) continue;
            Vector3 d = m.transform.position - transform.position; d.y = 0;
            float score = dist - Vector3.Dot(f, d.normalized) * 0.8f;   // prefer what is in front
            if (score < bd) { bd = score; best = m; }
        }
        return best;
    }

    void FaceTarget(AHMob t) { if (t != null) transform.rotation = g.Face(t.transform.position - transform.position); }
    public Vector3 Hand { get { return transform.position + Vector3.up * 1.1f + transform.forward * 0.4f; } }

    // the Shadowshot rogue fights at range (throwing knives); every other class as its class says
    public bool Ranged { get { return cls.ranged || AHEvo.Pass(this, "ranged") > 0f; } }
    public float Range { get { return !cls.ranged && Ranged ? 13f : cls.range; } }
    // on-hit effects from paths and talents: stun (melee rogue), poison and fear (ranged rogue)
    void OnHitFx(AHMob m)
    {
        if (m == null || m.dead) return;
        float st = AHEvo.Pass(this, "stunHit"), fe = AHEvo.Pass(this, "fearHit"), ve = AHEvo.Pass(this, "venom");
        if (st > 0f && Random.value < st) m.AddStatus(AHStatus.Stun, 1.2f, 0f, this);
        if (fe > 0f && Random.value < fe) m.AddStatus(AHStatus.Fear, 2.5f, 0f, this);
        if (ve > 0f) m.AddStatus(AHStatus.Poison, 5f, Power * ve, this);
    }

    void Attack()
    {
        if (mounted) AHComp.Dismount(g, true);
        AHSound.Play(Ranged ? "whoosh" : "swing");
        AHMob t = Ranged ? (target != null && Dist(target) <= Range ? target : Nearest(Range)) : Nearest(Range + 0.4f);
        if (t != null) { target = t; FaceTarget(t); }
        atkCd = cls.atkCd; atkT = Ranged ? 0.35f : 0.5f; hitAt = 0.22f; hitTarget = t;
        // combos: every third swing in a quick chain is a heavy finisher
        comboN = Time.time - lastSwing < cls.atkCd + 1.1f ? comboN + 1 : 1; lastSwing = Time.time;
        comboMul = comboN % 3 == 0 ? 1.8f : 1f;
        if (comboMul > 1f)
        {
            if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.5f, "Combo!", new Color(1f, 0.75f, 0.3f));
            AHFx.Ring(transform.position, 0.3f, 1.8f, cls.color, 0.3f);
        }
        if (anim != null && comboMul > 1f && !Ranged && anim.Has("Sword_Heavy_Combo")) { anim.Play("Sword_Heavy_Combo", false, 1.5f, true); poseT = 0f; atkT = Mathf.Max(atkT, 0.6f); hitAt = 0.3f; }
        else if (anim != null)
        {
            // melee: alternate a forward cut and a sideways slash; the ranger fires the crossbow two-handed
            string clip = Ranged ? (cls.id == "ranger" && anim.Has("Pistol_Shoot") ? "Pistol_Shoot" : "Shoot")
                : anim.Has("Sword_Regular_A") ? ((swingN++ & 1) == 0 ? "Sword_Regular_A" : "Sword_Regular_B") : "1H_Melee_Attack";
            float spd = clip == "Pistol_Shoot" ? 1.3f : clip.StartsWith("Sword_Regular") ? (cls.atkCd < 0.6f ? 1.4f : 1.05f) : Ranged ? 1.8f : (cls.atkCd < 0.6f ? 2f : 1.5f);
            anim.Play(clip, false, spd, true); poseT = 0f;
        }
        if (Ranged && t != null)
        {
            int dmg = Roll(cls.atkMult * comboMul);
            System.Action<AHMob> hitFn = m => { m.Hurt(dmg, this); AHEvo.Arc(this, m, dmg); OnHitFx(m); };
            if (cls.id == "rogue") AHFx.Shoot(Hand, t, new Color(0.55f, 0.95f, 0.45f), 0.14f, 24f, SpellEl.Arrow, hitFn);   // poisoned throwing knives
            else if (cls.id == "ranger") AHFx.Shoot(Hand, t, new Color(1f, 0.95f, 0.8f), 0.18f, 26f, SpellEl.Arrow, hitFn);   // the crossbow fires real bolts
            else AHFx.Shoot(Hand, t, cls.color, 0.18f, 20f, SpellEl.Arcane, hitFn);
        }
        BreakStealth();
    }

    // web v67: hiding lasts its full time even if you attack (stealthUntil is only a timer)
    void BreakStealth() { }

    // everything within radius of a point
    void HitAround(Vector3 at, float radius, float mult, SpellDef sp)
    {
        foreach (var m in g.mobs)
        {
            if (m.dead) continue;
            Vector3 d = m.transform.position - at; d.y = 0;
            if (d.magnitude - m.type.radius > radius) continue;
            m.Hurt(Roll(mult), this);
            if (sp != null && sp.status != AHStatus.None) m.AddStatus(sp.status, sp.statusTime, Power * sp.mult * 0.3f, this);
        }
    }

    void Hit(AHMob m, SpellDef sp)
    {
        if (m == null || m.dead) return;
        if (sp.mult > 0f) m.Hurt(Roll(sp.mult), this);
        if (sp.status != AHStatus.None) m.AddStatus(sp.status, sp.statusTime, Power * Mathf.Max(0.4f, sp.mult) * 0.35f, this);
    }

    public void Cast(int i)
    {
        if (dead || i < 0 || i >= Spells.Length) return;
        if (mounted) AHComp.Dismount(g, true);
        SpellDef sp = Spells[i];
        float spk = AHEvo.SpellK(this, sp); if (spk != 1f) sp = sp.Scale(spk);
        AHMob t = target != null && !target.dead && Dist(target) <= Mathf.Max(sp.range, 3f) ? target : Nearest(Mathf.Max(sp.range, 3f));
        bool needsTarget = sp.kind == SpellKind.Strike || sp.kind == SpellKind.Bolt || sp.kind == SpellKind.Charge || sp.kind == SpellKind.StepBehind || sp.kind == SpellKind.GroundAt || sp.kind == SpellKind.Multi;
        if (cls.mana && mana < sp.cost) { if (g.ui != null) g.ui.Toast("Not enough mana for " + sp.name + " (" + sp.cost + ")."); return; }   // web: mana is checked first
        if (needsTarget && t == null) { if (g.ui != null) g.ui.Toast(sp.name + " needs an enemy in range."); return; }
        if (cls.mana) { mana -= sp.cost; lastCast = Time.time; }
        // cone spells and jumps turn to the enemy too (on a phone you have let go of the stick, so you may face away)
        if (t != null) { target = t; if (needsTarget || sp.kind == SpellKind.Arc || sp.kind == SpellKind.Leap) FaceTarget(t); }
        cds[i] = AHEvo.Cd(this, sp);
        var el = AHSpellLook.Element(sp);
        AHSound.Spell(el, false);
        // melee and instant spells land at once: their impact follows the cast sound
        if (sp.kind == SpellKind.Strike || sp.kind == SpellKind.Arc || sp.kind == SpellKind.Nova || sp.kind == SpellKind.StepBehind || sp.kind == SpellKind.Leap)
            AHSound.Spell(el, true, 0.12f);
        string pose = "Interact";
        Vector3 me = transform.position;
        switch (sp.kind)
        {
            case SpellKind.Strike:
                pose = Ranged ? "Interact" : "1H_Melee_Attack";
                // Execute and Assassinate finish off a wounded enemy: much harder below 30% health
                if ((sp.id == "execute" || sp.id == "assassinate") && t.hp < t.type.hp * 0.3f) sp = sp.Scale(sp.id == "execute" ? 1.5f : 1.7f);
                Hit(t, sp);
                AHFx.Pop(t.transform.position + Vector3.up * 0.8f, 1.4f, sp.color);
                break;
            case SpellKind.Arc:
                pose = "1H_Melee_Attack";
                foreach (var m in g.mobs)
                {
                    if (m.dead) continue;
                    Vector3 d = m.transform.position - me; d.y = 0;
                    if (d.magnitude - m.type.radius > sp.radius) continue;
                    if (Vector3.Dot(transform.forward, d.normalized) < 0.2f) continue;
                    Hit(m, sp);
                }
                AHFx.Ring(me + transform.forward * 1.2f, 0.5f, sp.radius * 0.8f, sp.color, 0.3f);
                break;
            case SpellKind.Nova:
                HitAround(me, sp.radius, sp.mult, sp);
                AHFx.Ring(me, 0.5f, sp.radius, sp.color, 0.45f);
                AHFx.Ring(me, 0.3f, sp.radius * 0.9f, sp.color, 0.6f, true);
                break;
            case SpellKind.Whirl:
                pose = "1H_Melee_Attack";
                whirlSpell = sp; whirlT = 1.2f; whirlTick = 0f;
                break;
            case SpellKind.Bolt:
                {
                    int dmg = Roll(sp.mult);
                    AHFx.Shoot(Hand, t, sp.color, 0.3f, el == SpellEl.Arrow ? 24f : 16f, el, m => { AHSound.Spell(el, true); m.Hurt(dmg, this); if (sp.status != AHStatus.None) m.AddStatus(sp.status, sp.statusTime, Power * sp.mult * 0.3f, this); });
                }
                break;
            case SpellKind.Multi:
                {
                    // your target first, then up to four enemies close to it
                    int shots = 0;
                    var pool = new List<AHMob> { t };
                    foreach (var m in g.mobs) if (m != t && !m.dead && (m.transform.position - t.transform.position).magnitude < 8.8f && pool.Count < 5) pool.Add(m);
                    foreach (var m in pool)
                    {
                        if (m.dead || shots >= 5) continue;
                        int dmg = Roll(sp.mult);
                        bool first = shots == 0; AHFx.Shoot(Hand, m, sp.color, 0.16f, 22f, el, x => { if (first) AHSound.Spell(el, true); x.Hurt(dmg, this); });
                        shots++;
                    }
                }
                break;
            case SpellKind.GroundAt:
                {
                    Vector3 at = t.transform.position;
                    AHFx.Ring(at, sp.radius, sp.radius, new Color(sp.color.r, sp.color.g, sp.color.b, 0.6f), 0.8f);
                    StartCoroutine(Later(0.8f, () =>
                    {
                        AHSound.Spell(el, true);
                        AHFx.Ring(at, 0.3f, sp.radius, sp.color, 0.5f, true);
                        AHFx.Pillar(at, 0.6f, 6f, sp.color, 0.4f);
                        HitAround(at, sp.radius, sp.mult, sp);
                    }));
                }
                break;
            case SpellKind.Consecrate:
                {
                    Vector3 at = me;
                    AHFx.Ring(at, sp.radius, sp.radius, new Color(sp.color.r, sp.color.g, sp.color.b, 0.5f), sp.time, true);
                    for (int k = 0; k < 5; k++)
                        StartCoroutine(Later(k * sp.time / 5f + 0.1f, () => { HitAround(at, sp.radius, sp.mult, null); AHFx.Ring(at, 0.4f, sp.radius, sp.color, 0.4f); }));
                }
                break;
            case SpellKind.Charge:
                {
                    Vector3 d = t.transform.position - me; d.y = 0;
                    float dist = Mathf.Max(0f, d.magnitude - t.type.radius - 0.8f);
                    Dash(d.normalized, Mathf.Clamp(dist / (speed * 3f), 0.05f, 0.5f), 3f);
                    AHMob tt = t;
                    StartCoroutine(Later(dodgeT, () => { AHSound.Spell(el, true); Hit(tt, sp); AHFx.Ring(transform.position, 0.3f, 2f, sp.color, 0.3f); }));
                    pose = null;
                }
                break;
            case SpellKind.Blink:
                {
                    AHFx.Pop(me + Vector3.up, 1.6f, sp.color);
                    Vector3 dir = transform.forward;
                    Vector2 st = g.ui != null ? g.ui.stick : Vector2.zero;
                    if (st.sqrMagnitude > 0.04f) dir = (g.CamForward() * st.y + g.CamRight() * st.x).normalized;
                    transform.position = g.Resolve(me + dir * sp.value, Radius);
                    transform.rotation = g.Face(dir);
                    AHFx.Pop(transform.position + Vector3.up, 1.6f, sp.color);
                    iframe = 0.3f;
                }
                break;
            case SpellKind.StepBehind:
                {
                    AHFx.Pop(me + Vector3.up, 1.4f, sp.color);
                    Vector3 behind = t.transform.position - t.transform.forward * (t.type.radius + 0.8f);
                    transform.position = g.Resolve(behind, Radius);
                    FaceTarget(t);
                    AHFx.Pop(transform.position + Vector3.up, 1.4f, sp.color);
                    critNext = true;
                    Hit(t, sp);
                    pose = "1H_Melee_Attack";
                }
                break;
            case SpellKind.Leap:
                {
                    HitAround(me, sp.radius, 0f, sp);
                    AHFx.Ring(me, 0.5f, sp.radius, sp.color, 0.4f);
                    Dash(-transform.forward, 0.3f, sp.value / (speed * 0.3f));
                    iframe = 0.4f;
                    pose = null;
                }
                break;
            case SpellKind.Heal:
                {
                    // druid: a true percentage of max HP (talents add to the percentage, nothing crits);
                    // priest: the fuller your mana, the stronger the heal, and it leaves a holy shield behind;
                    // shaman and the rest: with your healing bonuses
                    float amt = HealAmount(sp.value);
                    Heal(amt, cls.id == "druid");
                    if (cls.id == "priest") { float sh = amt * (0.3f + AHEvo.Pass(this, "healShield")); if (sh > shield) { shield = sh; shieldT = 6f; } AHFx.Ring(me, 0.5f, 2f, new Color(1f, 0.9f, 0.5f), 0.5f); }
                    if (cls.id == "shaman" || cls.id == "priest" || cls.id == "druid") AHComp.HealParty(g, me, 9f, sp.value * (cls.id == "shaman" ? 1f : 0.6f));
                }
                AHFx.Pillar(me, 0.7f, 3f, sp.color, 0.7f);
                pose = "Cheer";
                break;
            case SpellKind.Hot:
                hotT = sp.time; hotRate = HealAmount(sp.value) / sp.time;
                AHFx.Pillar(me, 0.6f, 2.5f, sp.color, 0.6f);
                pose = "Cheer";
                break;
            case SpellKind.Shield:
                // some shields keep their size in 'mult' (Iron Will, Barkskin, Shield Wall...): they used to give nothing
                shield = maxHp * (sp.value > 0f ? sp.value : sp.mult) * (sp.id == "barrier" ? 1f + AHEvo.Pass(this, "barrier") : 1f); shieldT = sp.time;
                AHFx.Ring(me, 0.5f, 2.2f, sp.color, 0.5f);
                pose = "Cheer";
                break;
            case SpellKind.Invuln:
                invulnT = sp.time;
                AHFx.Pillar(me, 0.8f, 3.5f, sp.color, 0.6f);
                pose = "Cheer";
                break;
            case SpellKind.Buff:
                // a weaker buff never replaces a stronger one that is still running
                if (dmgBuffT <= 0f || sp.value >= dmgBuff) { dmgBuff = sp.value; dmgBuffT = sp.time; } else dmgBuffT = Mathf.Max(dmgBuffT, 1f);
                if (sp.id == "timewarp") for (int k = 0; k < cds.Length; k++) if (k != i) cds[k] = Mathf.Max(0f, cds[k] - 8f);   // Time Warp: other spells ready sooner
                AHFx.Ring(me, 0.5f, 3.5f, sp.color, 0.5f);
                if (sp.id == "cry") hp = Mathf.Min(maxHp, hp + maxHp * 0.15f);
                pose = "Cheer";
                break;
            case SpellKind.Stealth:
                stealthT = sp.time; iframe = 1f; critNext = true;
                foreach (var m in g.mobs) m.LoseTrack(transform.position, 28f);   // 700 web units
                AHFx.Pop(me + Vector3.up, 2f, sp.color, 0.5f);
                AHFx.Ring(me, 0.5f, 3.2f, new Color(0.42f, 0.35f, 0.54f), 0.5f);
                if (g.ui != null) g.ui.Float(me + Vector3.up * 2.3f, "Vanished", new Color(0.78f, 0.65f, 1f));
                pose = null;
                break;
            case SpellKind.Totem:
                AHTotem.Plant(this, sp, me + transform.forward * 1.2f, sp.time * (1f + AHEvo.Pass(this, "totem")));
                pose = "Cheer";
                break;
            case SpellKind.Bear:
                bearT = sp.time;
                hp = Mathf.Min(maxHp, hp + maxHp * sp.value);
                AHFx.Pop(me + Vector3.up, 2.4f, sp.color, 0.5f);
                pose = "Cheer";
                break;
        }
        if (sp.id == "holyfire" && AHEvo.Pass(this, "smite") > 0f) Heal(Power * sp.mult * AHEvo.Pass(this, "smite"));
        AHSpark.ForSpell(this, sp, t);
        if (sp.kind != SpellKind.Stealth) BreakStealth();
        PlayPose(AHSpellLook.Pose(cls, sp));
        if (g.ui != null) g.ui.SpellFlash(i, sp);   // the name floats up above its button
    }

    // weapon mastery: a glow on the weapon in the right hand (AHExtras)
    public void ApplyWeaponGlow()
    {
        if (weaponR == null) return;
        foreach (var gl in weaponR.GetComponentsInChildren<AHWeaponGlow>(true)) Destroy(gl.gameObject);
        int t = AHExtras.MasteryTier(this); if (t <= 0) return;
        var go = new GameObject("MasteryGlow"); go.transform.SetParent(weaponR, false); go.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        go.AddComponent<AHWeaponGlow>().col = AHExtras.MasteryCol[t - 1];
    }

    System.Collections.IEnumerator SlowMo()
    {
        if (Time.timeScale < 0.99f) yield break;
        Time.timeScale = 0.35f;
        yield return new WaitForSecondsRealtime(0.7f);
        if (Time.timeScale > 0.3f && Time.timeScale < 0.4f) Time.timeScale = 1f;
    }
    System.Collections.IEnumerator Later(float t, System.Action a)
    {
        yield return new WaitForSeconds(t);
        if (!dead) a();
    }

    // web hurtPlayer: level gap and night make beasts hit harder, then armour, damage reduction and shields
    float cheatAt;
    public void Hurt(float raw, AHMob src)
    {
        if (dead) return;
        if (src != null)
        {
            float gap = src.type.lvl - level;
            raw *= Mathf.Clamp(1f + gap * 0.05f, 0.6f, 1.9f) * (g.IsNight && !g.InTown(transform.position) ? 1.2f : 1f);
        }
        // a perfect dodge: rolling just as the blow lands slows the world for a moment and sharpens your next strikes
        if (iframe > 0.22f && src != null && Time.time >= perfectCd)
        {
            perfectCd = Time.time + 6f;
            if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.4f, "Perfect dodge!", new Color(0.7f, 0.9f, 1f));
            dmgBuff = Mathf.Max(dmgBuff, 0.3f); dmgBuffT = Mathf.Max(dmgBuffT, 3f);
            StartCoroutine(SlowMo());
            return;
        }
        if (iframe > 0f || invulnT > 0f) { if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2f, invulnT > 0f ? "Immune" : "Dodged", new Color(0.81f, 0.91f, 1f)); return; }
        if (AHEvo.Pass(this, "block") > 0f && Random.value < AHEvo.Pass(this, "block")) { if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2f, "Blocked", new Color(1f, 0.85f, 0.5f)); return; }
        if (stat.evade > 0f && Random.value < stat.evade) { if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2f, "Evaded", new Color(0.81f, 0.91f, 1f)); return; }
        float red = bearT > 0f ? 0.4f : 0f;
        int hit = Mathf.Max(1, Mathf.RoundToInt(raw * (1f - ArmorCut) * (1f - red) * (Elix("ward") ? 0.75f : 1f) * (Elix("raid") ? 0.8f : 1f) * AHFinder.TakenK(g)));
        AHJuice.OnHurt(g, hit);   // a heavy blow shakes the camera
        if (shield > 0f)
        {
            int a = Mathf.Min(Mathf.RoundToInt(shield), hit); shield -= a; hit -= a;
            if (shield <= 0f) shieldT = 0f;
            if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.2f, "Absorbed " + a, new Color(0.62f, 0.89f, 1f));
            if (hit <= 0) return;
        }
        lastHurt = Time.time;
        CancelAction();
        hp -= hit;
        AHWear.OnHurt(g, this);
        AHSound.Play("hurt");
        if (src != null && !src.dead && AHEvo.Pass(this, "thorns") > 0f) src.Hurt(Mathf.Max(1, Mathf.RoundToInt(hit * AHEvo.Pass(this, "thorns"))), this, true);
        if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2f, "-" + hit, new Color(1f, 0.48f, 0.42f));
        // "cheat death" forms (Phoenix and others): once every 2 minutes a killing blow leaves you at 30% instead
        if (hp <= 0f && AHEvo.Pass(this, "cheat") > 0f && Time.time >= cheatAt)
        {
            cheatAt = Time.time + 120f; hp = Mathf.Max(1f, maxHp * 0.3f);
            if (g.ui != null) { g.ui.Float(transform.position + Vector3.up * 2.4f, "Death cheated!", new Color(1f, 0.75f, 0.3f)); g.ui.Toast("You rise again! (again in 2 minutes)"); }
            return;
        }
        if (hp <= 0f)
        {
            hp = 0f; dead = true; deadT = 1.8f; whirlT = 0f; stealthT = 0f; SetGhost(false);
            if (mounted) AHComp.Dismount(g, true);
            if (anim != null) anim.Play("Death_A", false, 1f, true);
            if (g.ui != null) g.ui.Banner("You died", AHDungeon.IsDungeon(AHGame.AreaId) ? "Back to the entrance" : "Respawning at the camp");
        }
        else if (anim != null && atkT <= 0f && dodgeT <= 0f && whirlT <= 0f) { anim.Play("Hit_A", false, 1.4f, true); atkT = 0.25f; hitAt = 0f; }
    }

    // older callers (quest rewards): class XP and Attack
    public void GainXp(int amount) { GainXp("attack", amount, false); }

    // web gainXp: the skill gets 60% (at least 1); Attack XP also feeds the class level in full
    public void GainXp(string skill, int amount, bool fromKill)
    {
        int before = Skill(skill);
        // web: professions you practise learn 25% faster, earn mastery, and an Artisan's class grows from the work
        List<string> pk; bool profSkill = IsProfSkill(skill, out pk);
        bool mine = false; foreach (var k in pk) if (profs.Contains(k)) mine = true;
        int add = Mathf.Max(1, Mathf.RoundToInt(amount * 0.6f * (1f + AHMeal.Fx(this, "xp")) * AHFriends.DayXp(skill) * (mine ? 1.25f : 1f)));
        skillXp[skill] = SkillXp(skill) + add;
        foreach (var k in pk) if (profs.Contains(k)) AddMastery(k, amount);
        if (profSkill && path == "artisan" && amount > 0) GainClassXp(Mathf.RoundToInt(amount * 1.6f + 4f), false);
        if (skill != "attack" && g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.3f, "+" + add + " " + AHDB.SkillName(skill), new Color(0.79f, 0.91f, 0.66f));
        int after = Skill(skill);
        if (after > before && g.ui != null) g.ui.Banner(AHDB.SkillName(skill) + " " + after, "Level up");
        if (skill == "attack") GainClassXp(amount, fromKill);
        g.MarkDirty();
    }

    // web masteryLv: the best mastery among your professions (professions come in step 7)
    public int BestMastery()
    {
        int best = 0;
        var all = Profs as Dictionary<string, object>;
        if (all != null) foreach (var kv in all) best = Mathf.Max(best, MasteryLv(kv.Key));
        return best;
    }

    // class XP that does not come from a kill (the Artisan's Road pays this way)
    public void GainClassXpDirect(int n) { if (n > 0) GainClassXp(n, false); }

    // web gainClassXp, with the trial walls: every 10th level waits for its trial boss; up to one level of XP is banked
    void GainClassXp(int n, bool fromKill)
    {
        int before = level;
        n = Mathf.RoundToInt(n * (1f + AHMeal.Fx(this, "xp")));
        if (fromKill && rested > 0) { long rb = System.Math.Min(rested, n); rested -= rb; n += (int)rb; }
        if (fromKill && g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.3f, "+" + n + " XP", new Color(0.79f, 0.91f, 0.66f));
        var t = AHDB.ClAt;
        int wall = NextTrialLevel();
        long lim = wall > 0 && wall + 1 < t.Length ? t[wall + 1] - 1 : long.MaxValue;
        if (ClassXp + n > lim)
        {
            long over = ClassXp + n - System.Math.Max(ClassXp, lim);
            ClassXp = System.Math.Max(ClassXp, lim);
            long bk; banked.TryGetValue(cls.id, out bk);
            banked[cls.id] = System.Math.Min(bk + over, wall + 2 < t.Length ? t[wall + 2] - t[wall + 1] : over);
            if (Time.time - trialWarn > 90f && g.ui != null) { trialWarn = Time.time; g.ui.Toast("Level " + wall + " is a wall. Defeat " + TrialName(wall) + " to keep levelling. Your extra XP is saved.", 4f); }
        }
        else ClassXp += n;
        int after = level;
        if (after > before)
        {
            Recalc();
            if (g.ui != null) g.ui.Banner("Level " + after, cls.name + " level up");
            // a moment for it: the camera swings around the hero in a column of light
            g.cineT = 2.4f;
            AHFx.Pillar(transform.position, 1.2f, 6f, new Color(1f, 0.85f, 0.45f, 0.6f), 1.6f);
            AHSpark.Ring(transform.position + Vector3.up * 0.4f, new Color(1f, 0.85f, 0.45f), 30, 6f, 0.8f);
            string opens = AHGame.LandsOpeningAt(after);
            if (opens != null && g.ui != null) g.ui.Toast("Now ready for " + opens + ". See the world map (M).", 4f);
            AHSpark.LevelUp(transform.position);
            AHFx.Pillar(transform.position, 0.9f, 4f, new Color(1f, 0.85f, 0.4f, 0.8f), 1f);
            if (anim != null) { anim.Play("Cheer", false, 1f, true); atkT = 0.9f; hitAt = 0f; }
        }
        // a level-up is saved at once; any other XP rides the game's short save delay (saving on every kill stutters on phones)
        if (after > before) g.SaveProgress(); else g.MarkDirty();
    }
    float trialWarn = -999f;
    // the trial boss for a wall level, by name and place ("Old Tusker on Old Mill Road")
    string TrialName(int lv)
    {
        var tr = AHDB.List("classes", "TRIALS");
        if (tr != null) foreach (var o in tr)
            if ((int)AHJson.N(o, "lv") == lv)
            {
                string mob = AHJson.S(o, "mob"), where = AHJson.S(o, "where");
                string nm = AHJson.S(AHJson.O(AHDB.Mobs, mob), "name", mob);
                return nm + (string.IsNullOrEmpty(where) ? "" : " (" + where + ")");
            }
        return "its trial boss";
    }
    // web: killing a trial boss passes that trial (from five levels below its wall), and the XP banked at the wall is paid
    public void OnTrialKill(string mob)
    {
        var tr = AHDB.List("classes", "TRIALS"); if (tr == null) return;
        foreach (var o in tr)
        {
            int lv = (int)AHJson.N(o, "lv");
            if (AHJson.S(o, "mob") != mob || trialsDone.Contains(lv) || level < lv - 5) continue;
            trialsDone.Add(lv);
            if (g.ui != null) g.ui.Banner("Trial passed", "Level " + lv + " is open to you");
            AHSound.Play("level");
            long bk; banked.TryGetValue(cls.id, out bk); banked[cls.id] = 0;
            if (bk > 0) GainClassXp((int)System.Math.Min(bk, int.MaxValue), false);
            g.SaveProgress();
        }
    }
    [System.NonSerialized] public List<int> trialsDone = new List<int>();
    int NextTrialLevel()
    {
        int L = level;
        var tr = AHDB.List("classes", "TRIALS");
        if (tr != null) foreach (var o in tr) { int lv = (int)AHJson.N(o, "lv"); if (lv >= L && !trialsDone.Contains(lv)) return lv; }
        return 0;
    }

    // web addMoney
    public void AddMoney(long b, Vector3 at)
    {
        if (b <= 0) return;
        AHSound.Play("coin");
        bag.money += b;
        bag.Touch();
        if (g.ui != null) g.ui.Float(at + Vector3.up * 2.5f, "+" + AHItems.MoneyText(b, 1), new Color(1f, 0.82f, 0.23f));
    }

    // web eat: 3 s between bites
    public void Eat(string id)
    {
        if (dead) return;
        if (Time.time < eatCd) { if (g.ui != null) g.ui.Toast("You’re still chewing (" + Mathf.CeilToInt(eatCd - Time.time) + "s)."); return; }
        var f = AHItems.Get(id);
        if (f == null || !f.IsFood || !bag.Take(id)) return;
        eatCd = Time.time + 3f;
        hp = Mathf.Min(maxHp, hp + f.foodHp);
        hunger = Mathf.Min(100f, hunger + f.foodHunger);
        if (g.ui != null) g.ui.Float(transform.position + Vector3.up * 2.3f, "+" + f.foodHp + " HP", new Color(1f, 0.71f, 0.65f));
        if (f.meal) { AHMeal.Start(g, this, id); return; }
        if (g.ui != null) g.ui.Toast("You eat the " + f.name.ToLowerInvariant() + ".");
    }

    // ---------- the action button: skinning a fallen beast (web: interact 'skin', 1.2 s) ----------
    AHMob actionMob;
    float actionT;
    bool hungerWarned;
    public bool Busy { get { return actionMob != null || AHGather.Busy; } }
    public float ActionFrac { get { return actionMob != null ? 1f - actionT / 1.2f : AHGather.Frac; } }
    public string ActionName { get { return actionMob != null ? "Skinning" : AHGather.Busy ? (AHGather.actSpot.kind == "light" ? "Light fire" : AHGather.actRecipe != null ? AHItems.Get(AHGather.actRecipe.outId).name : AHGather.Label(AHGather.actSpot)) : null; } }

    // turn to the work and swing the tools
    public void BeginWork(Vector3 at, string clip = "Interact", string tool = null)
    {
        Vector3 d = at - transform.position; d.y = 0;
        if (d.sqrMagnitude > 1e-4f) transform.rotation = g.Face(d);
        StopEmote();
        if (anim != null && !anim.Play(clip, true, 1f, true)) anim.Play("Interact", true, 1f, true);
        ShowTool(tool, at);
    }

    // the work tool in the hand (the weapons are put away meanwhile); the fishing line runs to the water
    GameObject toolGo; string toolKind; LineRenderer fishLine; Vector3 fishAt;
    void ShowTool(string kind, Vector3 at)
    {
        if (kind == toolKind && toolGo != null) { fishAt = at; return; }
        HideTool();
        if (kind == null || model == null || !g.lookHero) return;
        toolGo = AHPeople.Tool(model, kind); toolKind = kind; fishAt = at;
        if (toolGo == null) { toolKind = null; return; }
        if (weaponR != null) weaponR.gameObject.SetActive(false);
        if (weaponL != null) weaponL.gameObject.SetActive(false);
        if (kind == "rod")
        {
            toolGo.AddComponent<AHRodAim>().who = transform;
            var lg = new GameObject("FishLine"); fishLine = lg.AddComponent<LineRenderer>();
            fishLine.positionCount = 2; fishLine.widthMultiplier = 0.01f; fishLine.useWorldSpace = true;
            fishLine.material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); fishLine.material.color = new Color(0.92f, 0.92f, 0.88f, 1f);
            fishLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    void HideTool()
    {
        if (toolGo != null) Destroy(toolGo);
        if (fishLine != null) Destroy(fishLine.gameObject);
        toolGo = null; toolKind = null; fishLine = null;
        if (weaponR != null) weaponR.gameObject.SetActive(true);
        if (weaponL != null) weaponL.gameObject.SetActive(true);
    }
    void ToolTick()
    {
        if (toolKind != null && !AHGather.Busy) HideTool();
        if (fishLine != null && toolGo != null)
        {
            Vector3 tip = toolGo.transform.TransformPoint(new Vector3(0, 1.62f, 0));
            Vector3 bob = fishAt; bob.y = transform.position.y - 0.05f;
            fishLine.SetPosition(0, tip); fishLine.SetPosition(1, bob + Vector3.up * Mathf.Sin(Time.time * 2.2f) * 0.03f);
        }
    }

    public AHMob SkinTarget()
    {
        if (dead) return null;
        AHMob best = null; float bd = 62f * AHDB.S;
        foreach (var m in g.mobs)
        {
            if (!m.CanSkin) continue;
            Vector3 d = m.transform.position - transform.position; d.y = 0;
            if (d.magnitude < bd) { bd = d.magnitude; best = m; }
        }
        return best;
    }

    public void Interact()
    {
        if (dead || Busy) return;
        var m = SkinTarget();
        if (m == null)
        {
            // web interact: a tree, rock, herb, fishing spot or station; or your own fire in the wild
            var s = AHGather.Nearest(transform.position);
            if (s != null) { AHGather.Start(this, s); return; }
            if (AHGather.CanLight(this)) { AHGather.StartLight(this); BeginWork(transform.position + transform.forward); return; }
            if (g.ui != null) g.ui.Toast("Nothing here. Walk up to a tree, rock, the pond, a body, or a town station.");
            return;
        }
        var ids = new List<string> { "raw_meat" };
        if (Skill("skinning") >= m.type.skinReq && m.type.skin != null) ids.AddRange(m.type.skin);
        if (!bag.Fits(ids)) { if (g.ui != null) g.ui.Toast("Your bag is full. Make room to skin it."); return; }
        actionMob = m; actionT = 1.2f;
        transform.rotation = g.Face(m.transform.position - transform.position);
        if (anim != null) anim.Play("Interact", true, 1f, true);
    }

    void CancelAction() { actionMob = null; }

    void UpdateAction(float dt, bool moving)
    {
        AHGather.Update(this, dt, moving);
        if (actionMob == null) return;
        if (moving || !actionMob.CanSkin) { actionMob = null; return; }
        actionT -= dt;
        if (actionT > 0f) return;
        var m = actionMob; actionMob = null;
        int req = m.type.skinReq;
        m.Skinned();
        bag.Add("raw_meat");
        if (Skill("skinning") < req) { GainXp("skinning", 10, false); if (g.ui != null) g.ui.Toast("Requires Skinning " + req + ": you only got the meat (and a little skinning practice)."); }
        else
        {
            if (m.type.skin != null) foreach (var id in m.type.skin) bag.Add(id);
            GainXp("skinning", (20 + req * 8) * 3, false);   // three times the web rate: the higher beasts were out of reach for most heroes
            g.OnSkin(m.type.id);
        }
    }

    // web playerDie: back at half health after 1.8 s, a tenth of your money dropped
    void Respawn()
    {
        dead = false;
        hp = Mathf.Round(maxHp * 0.5f); mana = maxMana; hunger = Mathf.Max(hunger, 50f); shield = 0f; bearT = 0f;
        long lost = AHGame.AreaId == AHKQ.Area ? 0 : bag.money / 10; bag.money -= lost; bag.Touch();
        model.transform.localScale = LookScale;
        transform.position = g.W(g.data.spawn.x, g.data.spawn.z);
        var home = AHCityHouse.RespawnAt(this); if (home.HasValue) transform.position = home.Value;
        foreach (var m in g.mobs) m.LoseTrack(transform.position, 99999f);
        if (anim != null) anim.Play("Idle", true, 1f, true);
        if (g.ui != null) g.ui.Toast((home.HasValue ? "You wake up in your city house." : AHDungeon.IsDungeon(AHGame.AreaId) ? "You wake up at the entrance." : "You wake up in " + g.data.region + ".") + (lost > 0 ? " You dropped " + AHItems.MoneyText(lost) + " as you fell." : ""));
    }
}

// the fishing rod points out over the water and up, whatever the hand is doing (after the VRoid body is posed)
[DefaultExecutionOrder(20020)]
public class AHRodAim : MonoBehaviour
{
    public Transform who;
    void LateUpdate()
    {
        if (who == null) return;
        Vector3 dir = (who.forward * 0.8f + Vector3.up * 0.6f).normalized;
        transform.rotation = Quaternion.LookRotation(who.right, dir) * Quaternion.Euler(0f, 0f, 0f);
        transform.rotation = Quaternion.FromToRotation(transform.up, dir) * transform.rotation;
    }
}
