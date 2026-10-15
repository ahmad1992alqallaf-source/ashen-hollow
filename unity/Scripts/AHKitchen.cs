// Ashen Hollow: the Test kitchen, where you invent your own recipes. Put two or three ingredients in the pot and try
// them: most tries make a burnt mess (and a hint), but the right mix discovers one of eight signature recipes, and
// it is yours: it carries your name ("Ahmad's Ember stew") wherever it goes. Once found you can cook it again any
// time from the same ingredients. A signature dish is as good as the town's own dishes of its level, not better.
// Chefs cook two at once. Opened from the menu (Menu -> Test kitchen).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHKitchenState { public List<string> found = new List<string>(); public List<string> pot = new List<string>(); public int tries; public List<string> hints = new List<string>(); }

public static class AHKitchen
{
    public class Sig { public string id, hint; public string[] mats; public int lvl; }
    public static readonly Sig[] Sigs =
    {
        new Sig { id = "sig_honey_cakes",     mats = new[] { "flour", "honey", "egg" },       lvl = 1,  hint = "Something sweet from the hives, baked with flour and egg." },
        new Sig { id = "sig_sunpetal_tea",    mats = new[] { "sunpetal", "honey" },           lvl = 1,  hint = "A golden flower steeped and sweetened." },
        new Sig { id = "sig_garden_bake",     mats = new[] { "potato", "carrot", "cheese" },  lvl = 4,  hint = "Roots from the garden under melted cheese." },
        new Sig { id = "sig_fishers_chowder", mats = new[] { "raw_trout", "milk", "potato" }, lvl = 5,  hint = "A river fish simmered in milk with something that grows underground." },
        new Sig { id = "sig_ember_stew",      mats = new[] { "raw_meat", "tomato", "garlic" },lvl = 8,  hint = "Red meat, a red fruit and a sharp bulb, cooked hot." },
        new Sig { id = "sig_moon_tart",       mats = new[] { "strawberry", "cream", "flour" },lvl = 10, hint = "Red berries and cream on a pastry." },
        new Sig { id = "sig_frost_brew",      mats = new[] { "frostbloom", "mireroot" },      lvl = 12, hint = "A flower from the snow and a root from the bog." },
        new Sig { id = "sig_truffle_risotto", mats = new[] { "truffle", "wheat", "butter" },  lvl = 16, hint = "A rare thing dug from the forest floor, with grain and butter." },
    };
    // what can go in the pot
    public static readonly string[] Ingredients = { "flour", "honey", "egg", "milk", "butter", "cream", "cheese", "wheat", "potato", "carrot", "onion", "garlic", "tomato", "cabbage", "corn", "pumpkin", "strawberry", "raw_meat", "raw_trout", "raw_salmon", "sunpetal", "mireroot", "frostbloom", "truffle", "coconut" };

    public static AHKitchenState S(AHPlayer p) { if (p.prog.kitchen == null) p.prog.kitchen = new AHKitchenState(); return p.prog.kitchen; }
    public static Sig Def(string id) { return Array.Find(Sigs, x => x.id == id); }
    public static string Owner(AHPlayer p) { return string.IsNullOrEmpty(p.heroName) ? "Your" : p.heroName + "’s"; }
    static readonly Dictionary<string, string> baseName = new Dictionary<string, string>();

    // the dishes you have found carry your name
    public static void Names(AHPlayer p)
    {
        if (p == null || p.prog == null) return;
        foreach (var id in S(p).found)
        {
            var it = AHItems.Get(id); if (it == null) continue;
            string b; if (!baseName.TryGetValue(id, out b)) { b = it.name; baseName[id] = b; }
            it.name = Owner(p) + " " + b.ToLowerInvariant();
        }
    }
    public static string BaseName(string id) { string b; if (baseName.TryGetValue(id, out b)) return b; var it = AHItems.Get(id); return it != null ? it.name : id; }

    static bool Same(List<string> pot, string[] mats)
    {
        if (pot.Count != mats.Length) return false;
        foreach (var m in mats) if (!pot.Contains(m)) return false;
        return true;
    }

    public static void Toggle(AHGame g, string id)
    {
        var s = S(g.player);
        if (s.pot.Contains(id)) s.pot.Remove(id);
        else if (s.pot.Count >= 3) g.ui.Toast("Three ingredients at most. Take one out first.");
        else s.pot.Add(id);
    }

    // try what is in the pot: uses one of each
    public static void Try(AHGame g)
    {
        var p = g.player; var s = S(p);
        if (s.pot.Count < 2) { g.ui.Toast("Put at least two ingredients in the pot."); return; }
        foreach (var m in s.pot) if (p.bag.Count(m) < 1) { g.ui.Toast("You have no " + AHRequests.ItemName(m).ToLowerInvariant() + " left."); return; }
        foreach (var m in s.pot) p.bag.Take(m, 1);
        s.tries++;
        p.GainXp("cooking", 10, false);
        Sig hit = null; foreach (var d in Sigs) if (Same(s.pot, d.mats)) hit = d;
        if (hit != null && !s.found.Contains(hit.id))
        {
            if (p.Skill("cooking") < hit.lvl) { g.ui.Toast("Close! You can tell it would work, but you need Cooking " + hit.lvl + " to get it right.", 4f); p.bag.Touch(); g.MarkDirty(); return; }
            s.found.Add(hit.id); s.hints.Remove(hit.id); Names(p);
            p.bag.Add(hit.id, 1);
            p.GainXp("cooking", 120, false);
            g.ui.Banner("A new recipe!", Owner(p) + " " + BaseName(hit.id).ToLowerInvariant() + " · signature recipe " + s.found.Count + " of " + Sigs.Length);
            AHSound.Play("level");
        }
        else if (hit != null) { Cook(g, hit, false); return; }
        else
        {
            if (p.bag.Add("burnt", 1)) { }
            // a hint now and then: one recipe that shares an ingredient with this try
            string said = null;
            if (s.tries % 3 == 0)
                foreach (var d in Sigs)
                {
                    if (s.found.Contains(d.id) || s.hints.Contains(d.id)) continue;
                    bool share = false; foreach (var m in s.pot) if (Array.IndexOf(d.mats, m) >= 0) share = true;
                    if (share) { s.hints.Add(d.id); said = d.hint; break; }
                }
            g.ui.Toast(said != null ? "Burnt! But it gives you an idea: “" + said + "”" : "Burnt! That didn’t work. Try another mix.", 4f);
        }
        p.bag.Touch(); g.MarkDirty();
    }

    public static bool CanCook(AHPlayer p, Sig d) { foreach (var m in d.mats) if (p.bag.Count(m) < 1) return false; return true; }
    public static void Cook(AHGame g, Sig d, bool take = true)
    {
        var p = g.player;
        if (take) { if (!CanCook(p, d)) return; foreach (var m in d.mats) p.bag.Take(m, 1); }
        int n = p.profs.Contains("chef") ? 2 : 1;
        if (!p.bag.Add(d.id, n)) { g.ui.Toast(p.bag.lastWarn ?? "Your bag is full."); return; }
        AHArtisan.OnCraft(p, d.id, n);
        p.GainXp("cooking", 30, false);
        g.ui.Toast("You cook " + n + "× " + AHItems.Get(d.id).name + ".", 2.5f);
        p.bag.Touch(); g.MarkDirty();
    }
}

public partial class AHUI
{
    public void OpenKitchen() { wkMode = "kitchen"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderKitchen(AHPlayer p)
    {
        var s = AHKitchen.S(p); AHKitchen.Names(p);
        wkTitle.text = "Test kitchen · " + s.found.Count + " / " + AHKitchen.Sigs.Length + " signature recipes";
        var potNames = new List<string>(); foreach (var m in s.pot) potNames.Add(AHRequests.ItemName(m));
        wkHint.text = "Put two or three ingredients in the pot and try them. The right mix becomes your own recipe, with your name on it. In the pot: " + (potNames.Count > 0 ? string.Join(" + ", potNames.ToArray()) : "nothing yet");
        var rows = new List<Action<int>>();
        rows.Add(r => Row(r, "Try the pot", new Color(1f, 0.8f, 0.45f), potNames.Count > 0 ? string.Join(" + ", potNames.ToArray()) : "Add ingredients below", "Uses one of each · Cooking XP",
            new WkBtn { label = "Try", on = s.pot.Count >= 2, col = Go, act = () => { AHKitchen.Try(g); RenderWork(); } },
            new WkBtn { label = "Empty", on = s.pot.Count > 0, col = Plain, act = () => { s.pot.Clear(); RenderWork(); } }));
        // your recipes, then the hints you have, then the ingredients you own
        foreach (var d in AHKitchen.Sigs)
        {
            var dd = d; var it = AHItems.Get(d.id); if (it == null) continue;
            if (s.found.Contains(d.id))
            {
                var mn = new List<string>(); foreach (var m in d.mats) mn.Add(AHRequests.ItemName(m).ToLowerInvariant());
                rows.Add(r => Row(r, it.name, new Color(0.6f, 0.9f, 0.5f), string.Join(" + ", mn.ToArray()), "Signature dish · " + AHMeal.Text(MealKeys(dd.id), MealVals(dd.id)),
                    new WkBtn { label = "Cook", on = AHKitchen.CanCook(p, dd), col = Go, act = () => { AHKitchen.Cook(g, dd); RenderWork(); } }));
            }
            else if (s.hints.Contains(d.id))
                rows.Add(r => Row(r, "An idea…", new Color(1f, 1f, 1f, 0.75f), "“" + dd.hint + "”", "Needs Cooking " + dd.lvl));
        }
        foreach (var id in AHKitchen.Ingredients)
        {
            int have = p.bag.Count(id); if (have <= 0 && !s.pot.Contains(id)) continue;
            string ii = id; bool inPot = s.pot.Contains(id);
            rows.Add(r => Row(r, AHRequests.ItemName(ii) + (inPot ? "  <color=#9be37a>in the pot</color>" : ""), Color.white, "You have " + have, "",
                new WkBtn { label = inPot ? "Take out" : "Add", on = inPot || s.pot.Count < 3, col = inPot ? Plain : Go, act = () => { AHKitchen.Toggle(g, ii); RenderWork(); } }));
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }

    static List<string> MealKeys(string id) { var l = new List<string>(); var fx = AHJson.O(AHJson.O(AHItems.Raw(id), "meal"), "fx") as Dictionary<string, object>; if (fx != null) foreach (var kv in fx) l.Add(kv.Key); return l; }
    static List<float> MealVals(string id) { var l = new List<float>(); var fx = AHJson.O(AHJson.O(AHItems.Raw(id), "meal"), "fx") as Dictionary<string, object>; if (fx != null) foreach (var kv in fx) l.Add(Convert.ToSingle(kv.Value)); return l; }
}
