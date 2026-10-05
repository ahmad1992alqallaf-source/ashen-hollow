// Ashen Hollow: Well Fed, the meal buffs, as in the web game (v66, DISHES / QUAL / rollQuality / startMeal / mealFx).
// Every dish cooked at a kitchen (the homestead oven) comes out plain, fine or masterwork (higher Cooking: better odds).
// Eating one gives its Well Fed buff, one at a time: move speed, attack speed, mana regen, max HP, damage, armor,
// gathering speed, XP, or health back every second. A new meal replaces the old buff. The buff is saved with the hero.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AHMealState
{
    public string id, name;
    public List<string> k = new List<string>();
    public List<float> v = new List<float>();
    public long until;   // unix ms
}

public static class AHMeal
{
    static readonly Dictionary<string, string> Txt = new Dictionary<string, string>
    {
        { "speed", "move speed" }, { "atkspd", "attack speed" }, { "mana", "mana regen" }, { "hp", "max HP" },
        { "dmg", "damage" }, { "def", "armor" }, { "gather", "gathering speed" }, { "xp", "XP" },
    };
    static readonly string[] Baked = { "bread", "pancakes", "pumpkin_pie", "strawberry_tart", "shepherds_pie", "fisherman_pie" };
    static readonly string[] Sfx = { "", "_fine", "_master" };
    public static long Now { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }

    public static bool Active(AHPlayer p) { return p != null && p.meal != null && Now < p.meal.until; }

    // web mealFx
    public static float Fx(AHPlayer p, string key)
    {
        if (!Active(p)) return 0f;
        int i = p.meal.k.IndexOf(key);
        return i >= 0 ? p.meal.v[i] : 0f;
    }

    public static string Text(List<string> k, List<float> v)
    {
        var l = new List<string>();
        for (int i = 0; i < k.Count; i++)
            l.Add(k[i] == "hot" ? "heals " + (v[i] * 100f).ToString("0.0") + "% HP a second" : "+" + Mathf.RoundToInt(v[i] * 100f) + "% " + (Txt.ContainsKey(k[i]) ? Txt[k[i]] : k[i]));
        return string.Join(", ", l.ToArray());
    }
    public static string Describe(string id)
    {
        var m = AHJson.O(AHItems.Raw(id), "meal"); var fx = AHJson.O(m, "fx") as Dictionary<string, object>;
        if (fx == null) return "";
        var k = new List<string>(); var v = new List<float>();
        foreach (var kv in fx) { k.Add(kv.Key); v.Add((float)(double)kv.Value); }
        return Text(k, v) + " · " + (int)AHJson.N(m, "min", 10) + " min";
    }

    // web rollQuality: plain, fine or masterwork
    public static int Roll(AHPlayer p, AHRecipe r)
    {
        int d = p.Skill("cooking") - r.lvl;
        float bf = p.Perk("chef", 10) ? 0.15f : 0f, bm = p.Perk("chef", 40) ? 0.1f : 0f;
        float pm = Mathf.Clamp((d - 3) * 0.03f + bm, 0f, 0.35f), pf = Mathf.Clamp(0.1f + d * 0.05f + bf, 0f, 0.65f), x = UnityEngine.Random.value;
        return x < pm ? 2 : x < pm + pf ? 1 : 0;
    }
    public static string Out(AHPlayer p, AHRecipe r, out int q)
    {
        q = 0; var it = AHItems.Get(r.outId);
        if (it == null || !it.meal) return r.outId;
        q = Roll(p, r); string id = r.outId + Sfx[q];
        return AHItems.Get(id) != null ? id : r.outId;
    }
    // web: a chef's oven sometimes makes two (bakers twice as often for baked goods)
    public static float Double(AHPlayer p, AHRecipe r)
    {
        if (!p.profs.Contains("chef")) return 0f;
        return (p.Spec("chef", "baker") && Array.IndexOf(Baked, r.outId) >= 0 ? 0.35f : 0f) + (p.Perk("chef", 30) ? 0.25f : 0f);
    }

    // web startMeal
    public static void Start(AHGame g, AHPlayer p, string id)
    {
        var m = AHJson.O(AHItems.Raw(id), "meal"); if (m == null) return;
        bool had = Active(p);
        string bas = AHJson.S(m, "base", id);
        bool meat = false;
        foreach (var r in AHGather.Recipes("oven")) if (r.outId == bas) foreach (var kv in r.mats) if (kv.Key.StartsWith("raw_")) meat = true;
        float fk = p.Spec("chef", "grill") && meat ? 1.2f : 1f;
        var st = new AHMealState { id = id, name = AHItems.Get(id).name };
        var fx = AHJson.O(m, "fx") as Dictionary<string, object>;
        if (fx != null) foreach (var kv in fx) { st.k.Add(kv.Key); st.v.Add((float)Math.Round((double)kv.Value * fk, 4)); }
        int min = Mathf.RoundToInt((float)AHJson.N(m, "min", 10) * (p.Perk("chef", 5) ? 1.25f : 1f));
        st.until = Now + min * 60000L;
        p.meal = st;
        if ((int)AHJson.N(m, "q", 0) == 2) p.prog.Bump("master_meals");
        p.Recalc();
        if (p.Perk("chef", 20)) p.Heal(p.maxHp * 0.2f);
        g.ui.Banner("Well fed", Text(st.k, st.v) + " · " + min + " min");
        g.ui.Toast((had ? "Your new meal replaces the old buff. " : "") + "Well fed: " + Text(st.k, st.v) + " for " + min + " minutes.", 4f);
        g.MarkDirty();
    }

    // every frame: the slow heal, and the buff wearing off
    public static void Tick(AHGame g, AHPlayer p, float dt)
    {
        if (p.meal == null) return;
        if (!Active(p)) { g.ui.Toast("Your " + p.meal.name.ToLowerInvariant() + " buff has worn off."); p.meal = null; p.Recalc(); g.MarkDirty(); return; }
        float hot = Fx(p, "hot");
        if (hot > 0f && !p.dead && p.hp < p.maxHp) p.hp = Mathf.Min(p.maxHp, p.hp + p.maxHp * hot * dt);
    }
}
