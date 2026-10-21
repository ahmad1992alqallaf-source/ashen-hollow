// Ashen Hollow: AUTO for the main story quests. Tap AUTO on the quest tracker and the hero plays the current story
// step by itself in the land you are in:
//  - a new quest or a finished one: walks to the giver and takes it / hands it in;
//  - defeat X: finds the nearest such monster, runs to it, attacks and casts its spells (heals itself when low);
//  - skin X: defeats one and skins it; gather logs/ore/fish/herbs: walks to the nearest spot and works it;
//  - talk to someone in this land: walks to them and talks.
// When the monsters, the person or the quest-giver are in another land, AUTO finds the way there itself: it walks to
// the road out (or the city gate) that leads toward them, land by land, and carries on when it arrives.
// Anything else (craft, buy, build at home...) stops AUTO and says what to do. Moving the stick
// stops it too, and it pauses when your health drops low. It never picks fights with world bosses or monsters far
// above your level.
using System.Collections.Generic;
using UnityEngine;

public static class AHAuto
{
    public static bool On;
    public static string Doing = "";
    static Vector3 goal; static bool hasGoal; static float goalReach = 2f;
    static AHMob foe; static AHSpot spot; static AHNpc npc;
    static float think, talkCd, stuckT; static Vector3 lastPos;
    public static bool WantAttack;
    static int castI = -1;
    static string lastArea;
    static AHExit wayExit; static AHGate wayGate; static string wayTo; static Vector3 wayGoal;

    public static void Toggle(AHGame g)
    {
        On = !On; Clear();
        if (g.ui != null) g.ui.Toast(On ? "Auto quest on. Move the stick to take over." : "Auto quest off.");
        if (On && g.ui != null) g.ui.RefreshAuto();
    }
    public static void Stop(AHGame g, string why)
    {
        if (!On) return;
        On = false; Clear(); if (why != null && g.ui != null) g.ui.Toast(why, 4f);
        if (g.ui != null) g.ui.RefreshAuto();
    }
    static void Clear() { hasGoal = false; foe = null; spot = null; npc = null; WantAttack = false; castI = -1; Doing = ""; wayExit = null; wayGate = null; wayTo = null; }

    // which spell the hero should cast this frame (or -1)
    public static bool WantCast(int i) { return On && castI == i; }

    // the world-space direction to walk this frame (zero: stand)
    public static Vector3 Steer(AHPlayer p, float dt)
    {
        var g = AHGame.I; if (!On || g == null || p == null) return Vector3.zero;
        castI = -1;
        if (lastArea != AHGame.AreaId)
        {
            // a new land: everything we were heading for belonged to the old one
            bool moved = lastArea != null; lastArea = AHGame.AreaId; Clear(); think = moved ? 1.2f : 0f;
            if (moved && g.ui != null) g.ui.RefreshAuto();
        }
        if (p.dead) { Stop(g, "Auto quest stopped: you fell."); return Vector3.zero; }
        if (g.ui != null && g.ui.Modal != 0) return Vector3.zero;   // a window is open: wait
        if (p.hp < p.maxHp * 0.3f && !p.InFight) { Stop(g, "Auto quest paused: your health is low. Eat or rest, then tap AUTO again."); return Vector3.zero; }
        think -= dt; talkCd -= dt;
        if (think <= 0f) { think = 0.3f; Decide(g, p); if (!On) return Vector3.zero; }

        // fighting: stand in range and swing / cast
        WantAttack = false;
        if (foe != null && !foe.dead)
        {
            goal = foe.transform.position; goalReach = p.Ranged ? Mathf.Max(2f, p.Range - 1.5f) : 1.6f + foe.type.radius;
            float d = Flat(goal - p.transform.position);
            if (d <= goalReach + 0.4f)
            {
                p.target = foe; WantAttack = true;
                PickSpell(p);
                return Vector3.zero;
            }
        }
        if (!hasGoal) return Vector3.zero;
        Vector3 to = goal - p.transform.position; to.y = 0f;
        if (to.magnitude <= goalReach) { Arrive(g, p); return Vector3.zero; }
        // unstick: if we have not moved for a while, sidestep
        stuckT = (p.transform.position - lastPos).sqrMagnitude < 0.0004f ? stuckT + dt : 0f; lastPos = p.transform.position;
        Vector3 dir = to.normalized;
        if (stuckT > 0.8f) { dir = Quaternion.Euler(0, stuckT % 2f < 1f ? 70f : -70f, 0) * dir; if (stuckT > 6f) { if (wayExit != null || wayGate != null) { stuckT = 0f; wayGoal = p.transform.position; Arrive(g, p); return Vector3.zero; } Stop(g, "Auto quest stopped: the way is blocked. Walk a little and tap AUTO again."); return Vector3.zero; } }
        return dir;
    }

    static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }

    // heal when hurt, otherwise the first damaging spell that is ready
    static void PickSpell(AHPlayer p)
    {
        var sp = p.Spells; if (sp == null) return;
        for (int i = 0; i < sp.Length && i < 6; i++)
        {
            var s = sp[i]; if (s == null || p.cds[i] > 0f) continue;
            if (p.cls.mana && p.mana < s.cost) continue;
            bool heal = s.kind == SpellKind.Heal || s.kind == SpellKind.Hot || (s.kind == SpellKind.Totem && s.id.Contains("heal"));
            if (heal) { if (p.hp < p.maxHp * 0.6f) { castI = i; return; } continue; }
            if (s.kind == SpellKind.Fortify) { if (p.hp < p.maxHp * 0.5f) { castI = i; return; } continue; }   // stone skin when the fight turns
            if (s.kind == SpellKind.Blink || s.kind == SpellKind.Leap || s.kind == SpellKind.Stealth || s.kind == SpellKind.Invuln || s.kind == SpellKind.Shield || s.kind == SpellKind.Fortify) continue;
            castI = i; return;
        }
    }

    static void Decide(AHGame g, AHPlayer p)
    {
        var q = g.quests.Current;
        if (q == null) { Stop(g, "Every story quest is done!"); return; }
        if (foe != null && !foe.dead && foe.Chasing) return;   // finish the fight first
        if ((wayExit != null || wayGate != null) && hasGoal) { if (foe == null || foe.dead) { goal = wayGoal; goalReach = 1.5f; foe = null; } DefendOnTheWay(g, p); return; }   // on the road out: keep going
        foe = null; spot = null; hasGoal = false;
        if (g.quests.state == "offer" || g.quests.state == "ready")
        {
            GoTalk(g, p, g.quests.NpcName, true); return;
        }
        // the first goal not yet met
        QObj o = null; int k = 0;
        for (; k < q.obj.Count; k++) if (g.quests.Prog(k) < q.obj[k].n) { o = q.obj[k]; break; }
        if (o == null) { GoTalk(g, p, g.quests.NpcName, true); return; }
        Doing = o.label;
        switch (o.t)
        {
            case "kill": HuntFor(g, p, o.id, false); return;
            case "skin":
                foreach (var m in g.mobs) if (m != null && m.CanSkin && (o.id == "any" || m.type.id == o.id) && Flat(m.transform.position - p.transform.position) < 30f) { goal = m.transform.position; goalReach = 1.6f; hasGoal = true; skinMob = m; return; }
                HuntFor(g, p, o.id, true); return;
            case "gather":
                {
                    string id = o.id ?? "", kind, typ = null;
                    if (id == "logs" || id.EndsWith("logs")) kind = "chop";
                    else if (id == "ore") kind = "mine";
                    else if (id.EndsWith("_ore")) { kind = "mine"; typ = id.Substring(0, id.Length - 4); }
                    else if (id == "fish" || id.StartsWith("raw_")) kind = "fish";
                    else if (id == "herb") kind = "herb";
                    else { kind = "herb"; typ = id; }
                    if (!p.bag.Fits(new System.Collections.Generic.List<string> { id.Contains("_") ? id : "logs" })) { Stop(g, "Auto quest paused: your bag is full. Make room, then tap AUTO."); return; }
                    AHSpot best = null; float bd = float.MaxValue;
                    foreach (var s in AHGather.Spots)
                    {
                        if (s.kind != kind || (typ != null && s.type != typ)) continue;
                        if (s.until > Time.time) continue;
                        float d = Flat(s.pos - p.transform.position); if (d < bd) { bd = d; best = s; }
                    }
                    if (best == null) { Stop(g, "Auto quest: nothing to gather for “" + o.label + "” in this land."); return; }
                    spot = best; goal = best.pos; goalReach = Mathf.Max(1.2f, best.reach * 0.8f); hasGoal = true; return;
                }
            case "talk": GoTalk(g, p, o.id, false); return;
            case "deliver":
                if (p.bag.Count(o.id) >= o.n) { GoTalk(g, p, g.quests.NpcName, true); return; }
                Stop(g, "Auto quest: “" + o.label + "” needs " + o.n + " " + (AHItems.Get(o.id) != null ? AHItems.Get(o.id).name : o.id) + " in your bag first."); return;
            default:
                Stop(g, "Auto quest: “" + o.label + "” is one you do yourself. Tap AUTO again after."); return;
        }
    }
    static AHMob skinMob;

    static void HuntFor(AHGame g, AHPlayer p, string type, bool forSkin)
    {
        AHMob best = null; float bd = float.MaxValue;
        foreach (var m in g.mobs)
        {
            if (m == null || m.dead || m.add) continue;
            bool match = type == "any" ? !m.type.elite && !m.type.rare : type == "rare" ? m.type.rare : m.type.id == type;
            if (!match) continue;
            if (AHEvents.IsWorldBoss(m.type.id) && type != m.type.id) continue;
            if (type == "any" && m.type.lvl > p.level + 3) continue;
            if (forSkin && m.type.noSkin) continue;
            float d = Flat(m.transform.position - p.transform.position); if (d < bd) { bd = d; best = m; }
        }
        if (best == null)
        {
            int lv = p.level;
            if (Route(g, (id, d) => HasMob(d, type, lv), type == "any" ? "monsters for you" : AHEvents.MobName(type))) return;
            Stop(g, "Auto quest: no " + (type == "any" ? "monsters you can take" : AHEvents.MobName(type)) + " in this land, and no road leads to them. Travel there, then tap AUTO."); return;
        }
        foe = best; goal = best.transform.position; goalReach = 2f; hasGoal = true;
    }

    static void GoTalk(AHGame g, AHPlayer p, string name, bool quest)
    {
        npc = AHNpc.Find(name);
        if (npc == null)
        {
            Vector2 at; bool known = NpcAt(name, out at);
            if (known && Route(g, (id, d) => Inside(d, at), name)) return;
            Stop(g, "Auto quest: " + name + " is not in this land" + (known ? " and no road leads there" : "") + ". Travel there, then tap AUTO."); return;
        }
        goal = npc.transform.position; goalReach = 2.2f; hasGoal = true; Doing = "Talk to " + name;
        questTalk = quest;
    }
    static bool questTalk;

    static void Arrive(AHGame g, AHPlayer p)
    {
        hasGoal = false;
        if ((wayExit != null || wayGate != null) && Flat(wayGoal - p.transform.position) > 3f) { goal = wayGoal; goalReach = 1.5f; hasGoal = true; return; }   // a fight pulled us off the road
        if (wayExit != null) { var e = wayExit; wayExit = null; g.Travel(e.to, e.tx, e.tz, e.f); return; }
        if (wayGate != null) { var e = wayGate; wayGate = null; g.Travel(e.to, e.tx, e.tz, e.f); return; }
        if (npc != null)
        {
            if (talkCd > 0f) return; talkCd = 2f;
            p.transform.rotation = g.Face(npc.transform.position - p.transform.position);
            if (questTalk && (g.quests.state == "offer" || g.quests.state == "ready"))
            {
                g.quests.Event("talk", npc.npcName, 1, g);
                if (g.quests.state == "offer") { g.quests.Accept(); if (g.ui != null) g.ui.Banner("Quest accepted", g.quests.Current != null ? g.quests.Current.name : ""); }
                else { string msg = g.quests.Claim(p, g); if (g.quests.state == "ready") { Stop(g, "Auto quest paused: " + (msg ?? "the reward can't be taken yet.")); return; } }
                g.SaveProgress();
            }
            else g.TalkTo(npc);
            npc = null; return;
        }
        if (spot != null) { if (!p.Busy) p.Interact(); spot = null; return; }
        if (skinMob != null) { if (skinMob.CanSkin && !p.Busy) p.Interact(); skinMob = null; return; }
    }

    // ---------- finding the way to another land ----------
    static readonly Dictionary<string, AHWorldData> lands = new Dictionary<string, AHWorldData>();
    static AHWorldData Land(string id)
    {
        AHWorldData d;
        if (lands.TryGetValue(id, out d)) return d;
        d = null;
        var ta = Resources.Load<TextAsset>("AH/Areas/" + id);
        if (ta != null) { try { d = JsonUtility.FromJson<AHWorldData>(ta.text); } catch { d = null; } }
        lands[id] = d; return d;
    }
    static bool HasMob(AHWorldData d, string type, int lv)
    {
        if (d.mobs == null) return false;
        foreach (var m in d.mobs)
        {
            if (m == null) continue;
            if (type == "any")
            {
                if (d.mobTypes == null) continue;
                foreach (var t in d.mobTypes) if (t != null && t.id == m.type && t.lvl <= lv + 3 && t.lvl >= lv - 8 && !AHEvents.IsWorldBoss(t.id)) return true;
            }
            else if (m.type == type) return true;
        }
        return false;
    }
    static bool Inside(AHWorldData d, Vector2 at)
    {
        return d.bounds != null && at.x >= d.bounds.x0 && at.x <= d.bounds.x1 && at.y >= d.bounds.z0 && at.y <= d.bounds.z1;
    }
    // where a townsperson stands, in web metres
    static bool NpcAt(string name, out Vector2 at)
    {
        at = Vector2.zero;
        var npc = AHDB.Table("npcs", "NPC");
        if (npc != null && AHJson.S(npc, "name") == name) { at = new Vector2((float)AHJson.N(npc, "x") * AHDB.S, (float)AHJson.N(npc, "y") * AHDB.S); return true; }
        var vs = AHDB.List("npcs", "VILLAGERS");
        if (vs != null) foreach (var o in vs) if (AHJson.S(o, "name") == name) { at = new Vector2((float)AHJson.N(o, "x") * AHDB.S, (float)AHJson.N(o, "y") * AHDB.S); return true; }
        return false;
    }

    // breadth-first over the roads out and the city gates: head for the first step on the shortest way to a land
    // where want() holds. Dungeons are never on the way.
    static bool Route(AHGame g, System.Func<string, AHWorldData, bool> want, string what)
    {
        string found; AHExit ex; AHGate gt; Vector3 at;
        if (!FindWay(g, want, out found, out ex, out gt, out at)) return false;
        if (ex != null) { wayExit = ex; wayGate = null; wayTo = ex.to; }
        else { wayGate = gt; wayExit = null; wayTo = gt.to; }
        goal = at; goalReach = 1.5f; hasGoal = true; wayGoal = goal;
        Doing = "Travelling to " + LandName(found) + (what != null ? " (" + what + ")" : "");
        if (g.ui != null) g.ui.Toast("Auto: heading for " + LandName(found) + ", where " + (what ?? "the quest") + (what != null && what.EndsWith("s") ? " are." : " is."), 3.5f);
        return true;
    }
    // the shortest way (roads out and city gates, never dungeons) to a land where want() holds: the first road to take
    static bool FindWay(AHGame g, System.Func<string, AHWorldData, bool> want, out string found, out AHExit exOut, out AHGate gtOut, out Vector3 at)
    {
        found = null; exOut = null; gtOut = null; at = Vector3.zero;
        string here = AHGame.AreaId;
        var from = new Dictionary<string, string>();       // land -> the land before it
        var firstExit = new Dictionary<string, AHExit>();  // land -> the road out of here that starts the way to it
        var firstGate = new Dictionary<string, AHGate>();
        var q = new Queue<string>(); q.Enqueue(here); from[here] = null;
        while (q.Count > 0 && found == null)
        {
            string cur = q.Dequeue();
            var d = cur == here ? g.data : Land(cur); if (d == null) continue;
            if (cur != here && want(cur, d)) { found = cur; break; }
            if (d.exits != null) foreach (var e in d.exits)
            {
                if (e == null || string.IsNullOrEmpty(e.to) || from.ContainsKey(e.to) || AHDungeon.IsDungeon(e.to) || Land(e.to) == null) continue;
                from[e.to] = cur; q.Enqueue(e.to);
                if (cur == here) firstExit[e.to] = e; else { if (firstExit.ContainsKey(cur)) firstExit[e.to] = firstExit[cur]; if (firstGate.ContainsKey(cur)) firstGate[e.to] = firstGate[cur]; }
            }
            if (d.gates != null) foreach (var e in d.gates)
            {
                if (e == null || string.IsNullOrEmpty(e.to) || !string.IsNullOrEmpty(e.dung) || from.ContainsKey(e.to) || AHDungeon.IsDungeon(e.to) || Land(e.to) == null) continue;
                from[e.to] = cur; q.Enqueue(e.to);
                if (cur == here) firstGate[e.to] = e; else { if (firstExit.ContainsKey(cur)) firstExit[e.to] = firstExit[cur]; if (firstGate.ContainsKey(cur)) firstGate[e.to] = firstGate[cur]; }
            }
        }
        if (found == null) return false;
        AHExit ex; AHGate gt;
        if (firstExit.TryGetValue(found, out ex)) { exOut = ex; at = ex.r > 0f ? g.W(ex.x, ex.z) : g.W((ex.x0 + ex.x1) * 0.5f, (ex.z0 + ex.z1) * 0.5f); return true; }
        if (firstGate.TryGetValue(found, out gt)) { gtOut = gt; at = g.W(gt.x, gt.z); return true; }
        found = null; return false;
    }

    // ---------- the quest guide (the minimap's marker and the arrow at the top of the screen) ----------
    // where the current quest wants you now: a person, a beast, a thing to gather, or the road out toward the land
    // it lies in. label: what is there. Nothing to show: false.
    public static bool QuestTarget(AHGame g, AHPlayer p, out Vector3 at, out string label, out bool road)
    {
        at = Vector3.zero; label = null; road = false;
        if (g == null || p == null || g.quests == null) return false;
        var q = g.quests.Current; if (q == null) return false;
        string who = null; QObj o = null;
        if (g.quests.state == "offer" || g.quests.state == "ready") who = g.quests.NpcName;
        else
        {
            for (int k = 0; k < q.obj.Count; k++) if (g.quests.Prog(k) < q.obj[k].n) { o = q.obj[k]; break; }
            if (o == null) who = g.quests.NpcName;
            else if (o.t == "talk") who = o.id;
            else if (o.t == "deliver") who = p.bag.Count(o.id) >= o.n ? g.quests.NpcName : null;
        }
        if (who != null)
        {
            var n = AHNpc.Find(who);
            if (n != null) { at = n.transform.position; label = who; return true; }
            Vector2 w; string found; AHExit ex; AHGate gt;
            if (NpcAt(who, out w) && FindWay(g, (id, d) => Inside(d, w), out found, out ex, out gt, out at)) { label = "To " + LandName(found); road = true; return true; }
            return false;
        }
        if (o == null) return false;
        label = o.label;
        if (o.t == "kill" || o.t == "skin")
        {
            AHMob best = null; float bd = float.MaxValue;
            foreach (var m in g.mobs)
            {
                if (m == null || m.dead || m.add) continue;
                bool match = o.id == "any" ? !m.type.elite && !m.type.rare && m.type.lvl <= p.level + 3 : o.id == "rare" ? m.type.rare : m.type.id == o.id;
                if (!match) continue;
                float d = Flat(m.transform.position - p.transform.position); if (d < bd) { bd = d; best = m; }
            }
            if (best != null) { at = best.transform.position; return true; }
            int lv = p.level; string found; AHExit ex; AHGate gt;
            if (FindWay(g, (id, d) => HasMob(d, o.id, lv), out found, out ex, out gt, out at)) { label = "To " + LandName(found); road = true; return true; }
            return false;
        }
        if (o.t == "gather")
        {
            string id = o.id ?? "", kind, typ = null;
            if (id == "logs" || id.EndsWith("logs")) kind = "chop";
            else if (id == "ore") kind = "mine";
            else if (id.EndsWith("_ore")) { kind = "mine"; typ = id.Substring(0, id.Length - 4); }
            else if (id == "fish" || id.StartsWith("raw_")) kind = "fish";
            else if (id == "herb") kind = "herb";
            else { kind = "herb"; typ = id; }
            AHSpot best = null; float bd = float.MaxValue;
            foreach (var s in AHGather.Spots) { if (s.kind != kind || (typ != null && s.type != typ)) continue; float d = Flat(s.pos - p.transform.position); if (d < bd) { bd = d; best = s; } }
            if (best != null) { at = best.pos; return true; }
        }
        return false;
    }
    public static string LandName(string id)
    {
        foreach (var list in new[] { AHDB.List("world", "REGIONS"), AHDB.List("world", "CONNS") })
            if (list != null) foreach (var o in list) if (AHJson.S(o, "id") == id) return AHJson.S(o, "name", id);
        return id;
    }
    // on the road: fight back if something is chasing us, otherwise keep walking
    static void DefendOnTheWay(AHGame g, AHPlayer p)
    {
        foreach (var m in g.mobs)
            if (m != null && !m.dead && m.Chasing && Flat(m.transform.position - p.transform.position) < 8f) { foe = m; return; }
    }
}
