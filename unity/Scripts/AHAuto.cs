// Ashen Hollow: AUTO for the main story quests. Tap AUTO on the quest tracker and the hero plays the current story
// step by itself in the land you are in:
//  - a new quest or a finished one: walks to the giver and takes it / hands it in;
//  - defeat X: finds the nearest such monster, runs to it, attacks and casts its spells (heals itself when low);
//  - skin X: defeats one and skins it; gather logs/ore/fish/herbs: walks to the nearest spot and works it;
//  - talk to someone in this land: walks to them and talks.
// Anything else (go to another land, craft, buy, build at home...) stops AUTO and says what to do. Moving the stick
// stops it too, and it pauses when your health drops low. It never picks fights with world bosses or monsters far
// above your level.
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
    static void Clear() { hasGoal = false; foe = null; spot = null; npc = null; WantAttack = false; castI = -1; Doing = ""; }

    // which spell the hero should cast this frame (or -1)
    public static bool WantCast(int i) { return On && castI == i; }

    // the world-space direction to walk this frame (zero: stand)
    public static Vector3 Steer(AHPlayer p, float dt)
    {
        var g = AHGame.I; if (!On || g == null || p == null) return Vector3.zero;
        castI = -1;
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
        if (stuckT > 0.8f) { dir = Quaternion.Euler(0, stuckT % 2f < 1f ? 70f : -70f, 0) * dir; if (stuckT > 6f) { Stop(g, "Auto quest stopped: the way is blocked. Walk a little and tap AUTO again."); return Vector3.zero; } }
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
            if (s.kind == SpellKind.Blink || s.kind == SpellKind.Leap || s.kind == SpellKind.Stealth || s.kind == SpellKind.Invuln || s.kind == SpellKind.Shield) continue;
            castI = i; return;
        }
    }

    static void Decide(AHGame g, AHPlayer p)
    {
        var q = g.quests.Current;
        if (q == null) { Stop(g, "Every story quest is done!"); return; }
        if (foe != null && !foe.dead && foe.Chasing) return;   // finish the fight first
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
        if (best == null) { Stop(g, "Auto quest: no " + (type == "any" ? "monsters you can take" : AHEvents.MobName(type)) + " in this land. Travel to where they live, then tap AUTO."); return; }
        foe = best; goal = best.transform.position; goalReach = 2f; hasGoal = true;
    }

    static void GoTalk(AHGame g, AHPlayer p, string name, bool quest)
    {
        npc = AHNpc.Find(name);
        if (npc == null) { Stop(g, "Auto quest: " + name + " is not in this land. Travel there, then tap AUTO."); return; }
        goal = npc.transform.position; goalReach = 2.2f; hasGoal = true; Doing = "Talk to " + name;
        questTalk = quest;
    }
    static bool questTalk;

    static void Arrive(AHGame g, AHPlayer p)
    {
        hasGoal = false;
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
}
