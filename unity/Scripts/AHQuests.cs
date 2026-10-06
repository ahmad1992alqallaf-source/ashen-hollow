// Ashen Hollow: the story quests (quests.json + the objectives in rules.json QUEST_META) and the Artisan's Road,
// with the web game's rules (v67): one quest at a time from its giver, offer -> active -> ready -> claim,
// objectives counted by events (kill, skin, gather, talk, craft...), rewards of skill XP, coins and items.
using System.Collections.Generic;
using UnityEngine;

public class QObj { public string t, id, label; public int n; }

public class QuestDef
{
    public int index;
    public string name, text, giver, turnin;
    public List<QObj> obj = new List<QObj>();
    public string xpSkill; public int xp; public int gold;   // gold is written in copper (x CU for bronze)
    public List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();
}

public static class AHQuests
{
    static List<QuestDef> all;
    public static string NpcName { get { return AHJson.S(AHDB.Table("npcs", "NPC"), "name", "Captain Mara"); } }

    public static List<QuestDef> All
    {
        get
        {
            if (all != null) return all;
            all = new List<QuestDef>();
            var qs = AHDB.List("quests", "QUESTS");
            var meta = AHJson.A(AHDB.Rules, "QUEST_META");
            if (qs == null) return all;
            for (int i = 0; i < qs.Count; i++)
            {
                var q = qs[i]; var m = meta != null && i < meta.Count ? meta[i] : null;
                var d = new QuestDef { index = i, name = AHJson.S(q, "name", "?"), text = AHJson.S(q, "text", ""), giver = AHJson.S(m, "giver"), turnin = AHJson.S(m, "turnin") };
                var obj = AHJson.A(m, "obj");
                if (obj != null) foreach (var o in obj) d.obj.Add(new QObj { t = AHJson.S(o, "t"), id = AHJson.S(o, "id"), label = AHJson.S(o, "label", ""), n = (int)AHJson.N(o, "n", 1) });
                var rw = AHJson.O(q, "reward");
                var xp = AHJson.A(rw, "xp");
                if (xp != null && xp.Count >= 2) { d.xpSkill = (string)xp[0]; d.xp = (int)(double)xp[1]; }
                d.gold = (int)AHJson.N(rw, "gold");
                var it = AHJson.O(rw, "items");
                if (it != null) foreach (var kv in it) d.items.Add(new KeyValuePair<string, int>(kv.Key, (int)(double)kv.Value));
                all.Add(d);
            }
            return all;
        }
    }

    // web rwItem: '$head' means the head piece of your own class's dungeon set
    public static string RewardItem(string k, string cls)
    {
        if (k == null || k.Length == 0 || k[0] != '$') return k;
        var drops = AHJson.A(AHDB.Table("classes", "CLASS_DROPS"), cls);
        if (drops != null) foreach (var o in drops) { var d = AHItems.Get(o as string); if (d != null && d.slot == k.Substring(1)) return d.id; }
        return null;
    }

    // web GATHER_KIND
    public static string GatherKind(string id)
    {
        if (id == null) return "other";
        if (id.EndsWith("_ore")) return "ore";
        if (id.EndsWith("logs")) return "logs";
        if (id.StartsWith("raw_") && id != "raw_beef" && id != "raw_pork" && id != "raw_mutton" && id != "raw_chicken" && id != "raw_duck" && id != "raw_meat") return "fish";
        return "other";
    }
}

// where the hero is in the story (web P.quest = { i, state, prog }) and on the Artisan's Road (web P.aroad = { i, p })
public class AHQuestLog
{
    public int i;
    public string state = "offer";
    public int[] prog = new int[0];
    public int roadI, roadP;
    public System.Action Changed;

    public QuestDef Current { get { var a = AHQuests.All; return i >= 0 && i < a.Count ? a[i] : null; } }
    public bool AllDone { get { return Current == null; } }
    public int Prog(int k) { return prog != null && k < prog.Length ? prog[k] : 0; }
    void Touch() { if (Changed != null) Changed(); }

    // web questNpcName: who to talk to right now
    public string NpcName { get { var q = Current; if (q == null) return null; return state == "ready" ? (q.turnin ?? q.giver ?? AHQuests.NpcName) : (q.giver ?? AHQuests.NpcName); } }

    // web questEvent. Returns a banner line when the quest became ready.
    public bool Event(string t, string id, int n, AHGame g)
    {
        AHDaily.Event(g, t, id, n);
        RoadEvent(t, id, n, g);
        var q = Current;
        if (q == null || state != "active") return false;
        Check(g); if (state != "active") return true;
        bool changed = false;
        for (int k = 0; k < q.obj.Count; k++)
        {
            var o = q.obj[k];
            bool match = o.t == t && (o.id == id || (t == "gather" && AHQuests.GatherKind(id) == o.id) || (o.id == "any" && t != "talk")
                || (t == "kill" && o.id == "rare" && AHJson.B(AHJson.O(AHDB.Mobs, id), "rare")));
            if (match && Prog(k) < o.n) { prog[k] = Mathf.Min(o.n, Prog(k) + n); changed = true; }
        }
        if (!changed) return false;
        bool all = true;
        for (int k = 0; k < q.obj.Count; k++) if (Prog(k) < q.obj[k].n) all = false;
        if (all) { state = "ready"; if (g.ui != null) g.ui.Banner("Quest complete", "Return to " + NpcName); }
        Touch();
        return true;
    }

    public void Accept()
    {
        var q = Current; if (q == null || state != "offer") return;
        state = "active"; prog = new int[q.obj.Count];
        Touch();
        if (AHGame.I != null) Check(AHGame.I);   // goals already met (a level, a home, a place you stand in) count at once
    }

    // the places a visit goal can name, for the land you are in
    public static string[] PlacesOf(string area)
    {
        switch (area)
        {
            case "city": return new[] { "city", "varrow" };
            case "hc_city": return new[] { "hc_city", "highcairn" };
            case "mw_city": return new[] { "mw_city", "mirewatch" };
            case "ss_city": return new[] { "ss_city", "sunspire" };
            case "ch_city": return new[] { "ch_city", "cinderhold" };
            case "co_city": return new[] { "co_city", "coralport" };
            case "meadow": return new[] { "meadow", "ashen" };
            case "fossil": return new[] { "fossil", "bonewatch" };
            default: return new[] { area };
        }
    }

    // goals that are a state rather than an event (web questCheck): your level, a skill, home comforts, owning a home,
    // a profession, friends, items to deliver, and the land you stand in. Called when a quest starts, on every quest
    // event and once a second, so a goal met before the quest began is not lost.
    public void Check(AHGame g)
    {
        var q = Current; var p = g != null ? g.player : null;
        if (q == null || p == null || state != "active") return;
        if (prog == null || prog.Length != q.obj.Count) { var np = new int[q.obj.Count]; for (int k = 0; k < np.Length && prog != null && k < prog.Length; k++) np[k] = prog[k]; prog = np; }
        bool changed = false;
        var here = PlacesOf(AHGame.AreaId);
        for (int k = 0; k < q.obj.Count; k++)
        {
            var o = q.obj[k]; int v = -1;
            switch (o.t)
            {
                case "level": v = p.level; break;
                case "skill":
                    if (o.id == "any") { v = 0; var sk = AHJson.A(AHDB.Rules, "SKILLS"); if (sk != null) foreach (var x in sk) v = Mathf.Max(v, p.Skill(x as string)); }
                    else v = p.Skill(o.id);
                    break;
                case "comfort": v = p.home != null ? AHHome.ComfortPts(p) : 0; break;
                case "home": v = p.home != null ? 1 : 0; break;
                case "prof": v = p.profs.Count; break;
                case "friend": { int h; v = int.TryParse(o.id, out h) ? AHFriends.Count(p, h) : 0; } break;
                case "deliver": v = p.bag.Count(o.id); break;
                case "visit": if (System.Array.IndexOf(here, o.id) >= 0) v = o.n; break;
            }
            if (v < 0) continue;
            int nv = Mathf.Min(o.n, v);
            // deliveries follow the bag both ways; the rest only ever go up
            if (o.t == "deliver" ? nv != Prog(k) : nv > Prog(k)) { prog[k] = nv; changed = true; }
        }
        if (!changed) return;
        bool all = true;
        for (int k = 0; k < q.obj.Count; k++) if (Prog(k) < q.obj[k].n) all = false;
        if (all) { state = "ready"; if (g.ui != null) g.ui.Banner("Quest complete", "Return to " + NpcName); }
        Touch();
    }

    // web questButton 'ready': deliveries taken, room for rewards checked, then XP, coins and items. Returns a message.
    public string Claim(AHPlayer p, AHGame g)
    {
        var q = Current; if (q == null || state != "ready") return null;
        foreach (var o in q.obj) if (o.t == "deliver" && p.bag.Count(o.id) < o.n) { state = "active"; Touch(); return "You need " + o.n + " " + AHItems.Get(o.id).name.ToLowerInvariant() + " in your bag."; }
        int need = 0;
        foreach (var kv in q.items) { string id = AHQuests.RewardItem(kv.Key, p.cls.id); var d = AHItems.Get(id); if (d != null && p.bag.Count(id) <= 0 && !d.cosmetic) need++; }
        if (p.bag.UsedSlots + need > p.bag.SlotsMax) return "Make room in your bag for the reward first.";
        foreach (var o in q.obj) if (o.t == "deliver") p.bag.Take(o.id, o.n);
        if (q.xpSkill != null) p.GainXp(q.xpSkill, q.xp, false);
        if (q.gold > 0) p.AddMoney(q.gold * AHDB.CU, p.transform.position);
        foreach (var kv in q.items) { string id = AHQuests.RewardItem(kv.Key, p.cls.id); if (AHItems.Get(id) != null) p.bag.Add(id, kv.Value); }
        string done = "Quest complete: " + q.name;
        i++; state = "offer"; prog = new int[0];
        Touch();
        return done;
    }

    // ---------- the Artisan's Road (web aroadEvent / aroadCheck) ----------
    public static List<object> Road { get { return AHJson.A(AHDB.Rules, "AROAD"); } }
    public object RoadStep { get { var r = Road; return r != null && roadI < r.Count ? r[roadI] : null; } }

    public void RoadEvent(string t, string id, int n, AHGame g)
    {
        var s = RoadStep; if (s == null || g.player == null || g.player.path != "artisan") return;
        string st = AHJson.S(s, "t");
        if (st == "talk" && t == "talkguild") roadP = 1;
        else if (st == t && st != "talk" && st != "mastery" && st != "prof") roadP = Mathf.Min((int)AHJson.N(s, "n"), roadP + n);
        RoadCheck(g);
    }

    public void RoadCheck(AHGame g)
    {
        var p = g.player;
        var s = RoadStep; if (s == null || p == null || p.path != "artisan") return;
        string st = AHJson.S(s, "t");
        if (st == "prof") roadP = p.profs.Count;
        if (st == "mastery") roadP = p.BestMastery();
        if (roadP < (int)AHJson.N(s, "n")) { Touch(); return; }
        long c = (long)(AHJson.N(s, "pay") * AHDB.CU * (1 + Mathf.FloorToInt(p.level / 10f)));
        p.AddMoney(c, p.transform.position);
        p.GainClassXpDirect(Mathf.RoundToInt(80 + p.level * p.level * 2));
        string label = AHJson.S(s, "label", "");
        if (g.ui != null) g.ui.Banner("Artisan’s Road", label.Split(':')[0] + " · " + AHItems.MoneyText(c));
        roadI++; roadP = 0;
        var nx = RoadStep;
        if (g.ui != null) g.ui.Toast(nx != null ? "Next on the Artisan’s Road: " + AHJson.S(nx, "label") + "." : "You walked the whole Artisan’s Road. The realm knows your name, Master Artisan.");
        Touch();
        string nt = AHJson.S(nx, "t");
        if (nx != null && (nt == "mastery" || nt == "prof")) RoadCheck(g);   // goals already met complete at once
    }

    // ---------- web renderTracker ----------
    public string Tracker(AHPlayer p, out string title)
    {
        title = "";
        if (p != null && p.path == "artisan" && RoadStep != null && (i == 0 || state == "offer"))
        {
            var s = RoadStep; var r = Road;
            title = "Artisan’s Road " + (roadI + 1) + "/" + r.Count;
            return AHJson.S(s, "label") + "  " + Mathf.Min(roadP, (int)AHJson.N(s, "n")) + "/" + (int)AHJson.N(s, "n");
        }
        var q = Current;
        if (q == null) return "";
        if (state == "offer") { title = "New quest"; return "Talk to " + NpcName + (NpcName == AHQuests.NpcName ? " in the plaza" : ""); }
        title = q.name;
        if (state == "ready") return "Return to " + NpcName + " in Ashen Hollow";
        var lines = new List<string>();
        for (int k = 0; k < q.obj.Count; k++) lines.Add(q.obj[k].label + " " + Prog(k) + "/" + q.obj[k].n);
        return string.Join("\n", lines.ToArray());
    }
}
