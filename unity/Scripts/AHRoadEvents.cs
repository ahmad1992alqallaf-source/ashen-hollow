// Ashen Hollow: things that happen on the road. Out in the wild (never in a town, a dungeon or the arena), every eight
// to fourteen minutes something turns up on a road ahead of you, different each time:
//  - a travelling merchant with a cart and rare goods for a few minutes (elixirs, enhancement stones, mystery sacks)
//  - a bandit ambush on a merchant's wagon: drive the bandits off and the merchant pays you
//  - a lost child crying by the road: talk to them and they follow you; bring them to a town for a reward
//  - a wounded knight who needs a health potion or food: help them and they give you something from their pack
// A pillar of light marks the spot for a few seconds and the chat says what it is. Each one leaves after a while.
using System.Collections.Generic;
using UnityEngine;

public static class AHRoadEvents
{
    enum Kind { None, Merchant, Ambush, Child, Knight }
    static Kind cur = Kind.None;
    static float wait = 240f, life;
    static readonly List<GameObject> props = new List<GameObject>();
    static readonly List<AHMob> bandits = new List<AHMob>();
    static AHNpc npc;
    static bool following, done;
    static string area;
    public static Vector3 At;   // where the current one is (tests, the map)
    public static bool Active { get { return cur != Kind.None; } }

    public static void Tick(AHGame g, float dt)
    {
        var p = g.player; if (p == null) return;
        if (cur != Kind.None && AHGame.AreaId != area) { Clear(g, false); return; }   // left the land: it ends
        if (cur != Kind.None) { Run(g, dt); return; }
        wait -= dt; if (wait > 0f) return;
        wait = Random.Range(480f, 840f);
        if (!CanHappen(g)) { wait = 60f; return; }
        Start(g, (Kind)Random.Range(1, 5));
    }
    static bool CanHappen(AHGame g)
    {
        var p = g.player; if (p.dead || AHKQ.Mode != "" || AHDungeon.IsDungeon(AHGame.AreaId) || AHGame.AreaId == AHDeep.Area || AHGame.AreaId == AHAbyss.Area) return false;
        return !g.InTown(p.transform.position);
    }

    // a spot on a road ahead of you (or any open ground near you)
    static bool Spot(AHGame g, out Vector3 at)
    {
        var p = g.player; Vector3 me = p.transform.position, fw = p.transform.forward; fw.y = 0f; if (fw.sqrMagnitude < 0.01f) fw = Vector3.forward; fw.Normalize();
        float y = g.W(0f, 0f).y;
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < 40; i++)
            {
                float a = (pass == 0 ? Random.Range(-70f, 70f) : Random.Range(0f, 360f)), d = Random.Range(20f, 34f);
                Vector3 q = me + Quaternion.Euler(0, a, 0) * fw * d; q.y = y;
                if (!g.InArea(q) || g.InTown(q) || g.Blocked(q, 2.2f)) continue;
                if (AHHollow.Plot != null && AHHollow.Plot.Contains(q, 4f)) continue;
                bool crowd = false; foreach (var n in AHNpc.All) if (n != null && n.DistTo(q) < 6f) { crowd = true; break; } if (crowd) continue;
                if (pass == 0 && !AHNav.IsDirt(g, q)) continue;
                at = q; return true;
            }
        at = Vector3.zero; return false;
    }

    public static void Start(AHGame g, int kind) { Clear(g, false); Start(g, (Kind)Mathf.Clamp(kind, 1, 4)); }
    static void Start(AHGame g, Kind k)
    {
        Vector3 at; if (!Spot(g, out at)) return;
        var p = g.player; cur = k; area = AHGame.AreaId; following = false; done = false; At = at;
        Vector3 face = p.transform.position - at; face.y = 0f;
        AHFx.Pillar(at, 0.5f, 18f, new Color(1f, 0.9f, 0.55f, 0.55f), 6f);
        switch (k)
        {
            case Kind.Merchant:
            {
                life = 240f;
                Prop(g, "kk_wheelbarrow", at + Side(face) * 1.6f, 1.6f, face); Prop(g, "kk_crate_A_big", at + Side(face) * 2.4f - face.normalized * 0.8f, 0.9f, face); Prop(g, "kk_sack", at - Side(face) * 1.2f, 0.7f, face);
                string[] names = { "Wandering trader Osric", "Travelling merchant Yara", "Peddler Fenwick", "Merchant Selim" };
                npc = AHNpc.SpawnAt(g, names[Random.Range(0, names.Length)], at, face, new[] { "Rare goods from far away! Only while I'm passing through." });
                npc.label = "Shop"; npc.SetSeq("Idle_Talking_Loop", "Yes", "Idle_Loop");
                npc.onTalk = () => { g.ui.OpenShop("road_merchant"); return true; };
                Say(g, "A travelling merchant has stopped on the road with rare goods. They'll move on in a few minutes.");
                break;
            }
            case Kind.Ambush:
            {
                life = 150f;
                Prop(g, "kk_wheelbarrow", at + Side(face) * 1.4f, 1.8f, Side(face)); Prop(g, "kk_crate_A_big", at - Side(face) * 1.4f, 1.0f, face); Prop(g, "kk_dg_barrel_large", at - Side(face) * 2.2f + face.normalized, 1.0f, face);
                npc = AHNpc.SpawnAt(g, "Merchant Tamsin", at - face.normalized * 1.2f, face, new[] { "Help! Bandits!" });
                npc.label = "Talk"; npc.SetSeq("Idle_Loop");
                npc.onTalk = () => { if (!done) { g.ui.Toast("Merchant Tamsin: “Drive them off, please!”", 2.5f); return true; } return false; };
                int L = Mathf.Max(1, p.level);
                for (int i = 0; i < 3; i++)
                {
                    float a = i * 120f + Random.Range(-20f, 20f);
                    Vector3 bp = g.Resolve(at + Quaternion.Euler(0, a, 0) * Vector3.forward * 3.2f, 0.6f);
                    var m = AHMob.Create(g, Bandit(L), bp); m.add = true; g.mobs.Add(m); bandits.Add(m);
                }
                Say(g, "Bandits are attacking a merchant's wagon on the road! Drive them off.");
                break;
            }
            case Kind.Child:
            {
                life = 420f;
                npc = AHNpc.SpawnAt(g, "Little Pip", at, face, new[] { "I want to go home..." });
                npc.height = 1.2f;
                npc.transform.localScale *= 0.68f;
                npc.label = "Help"; npc.SetSeq("Idle_Loop");
                npc.onTalk = () =>
                {
                    if (!following) { following = true; npc.label = "Talk"; g.ui.Toast("Little Pip: “You'll take me home? To the town? Thank you!” (Pip follows you: bring them to a town.)", 4f); AHLife.Speak(npc, "Wait for me!", "انتظرني!", 2.5f); }
                    else g.ui.Toast("Little Pip: “Is it much further?”", 2.5f);
                    return true;
                };
                Say(g, "A lost child is crying by the road.");
                break;
            }
            case Kind.Knight:
            {
                life = 300f;
                npc = AHNpc.SpawnAt(g, "Wounded knight Aldren", at, face, new[] { "I took a bad blow... have you a potion, or something to eat?" });
                npc.label = "Help"; npc.SetSeq("Idle_Loop");
                npc.onTalk = () => { HelpKnight(g); return true; };
                Prop(g, "kk_dg_sword_shield_broken", at + Side(face) * 1.1f, 0.9f, face);
                Say(g, "A wounded knight lies by the road, asking for help.");
                break;
            }
        }
    }
    static Vector3 Side(Vector3 face) { face.y = 0f; return Vector3.Cross(Vector3.up, face.normalized); }
    static void Prop(AHGame g, string id, Vector3 at, float size, Vector3 face)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/" + id); if (pf == null) return;
        var go = Object.Instantiate(pf); go.name = "AH_Road event " + id;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        face.y = 0f; go.transform.rotation = face.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(face.normalized) : Quaternion.identity;
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { Object.Destroy(go); return; }
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        float k = size / Mathf.Max(0.05f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z))); go.transform.localScale = Vector3.one * k;
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        AHModel.SetShadows(go); props.Add(go);
    }
    static void Say(AHGame g, string s) { if (g.ui != null) g.ui.Banner("On the road", s); }

    static AHMobType Bandit(int L)
    {
        return new AHMobType { id = "bandit", name = "Road bandit", model = "Rogue_t", lvl = L, hp = 30 + L * 12, dmg = 4 + L, xp = 20 + L * 14, gold = 2 + L,
            speed = 140 * AHDB.S, radius = 15 * AHDB.S, aggro = 20f, respawn = 1e9f, atkCd = 1.4f, noSkin = true, lunge = true };
    }

    static void Run(AHGame g, float dt)
    {
        var p = g.player;
        life -= dt;
        if (npc == null && cur != Kind.Ambush) { Clear(g, false); return; }
        switch (cur)
        {
            case Kind.Ambush:
                if (!done)
                {
                    bandits.RemoveAll(m => m == null);
                    bool all = bandits.Count > 0; foreach (var m in bandits) if (!m.dead) all = false;
                    if (all)
                    {
                        done = true; life = Mathf.Min(life, 25f);
                        int L = Mathf.Max(1, p.level); long coins = (long)(40 + L * 22) * AHDB.CU;
                        p.AddMoney(coins, p.transform.position);
                        string got = AHItems.MoneyText(coins);
                        if (Random.value < 0.5f && p.bag.Add("enh_stone", 2)) got += " · 2 Enhancement stones";
                        else if (p.bag.Add("big_potion", 2)) got += " · 2 Greater health potions";
                        if (npc != null) { AHLife.Speak(npc, "You saved my wares! Take this, with my thanks.", "أنقذت بضاعتي! خذ هذا مع شكري.", 4f); npc.label = null; }
                        p.prog.Bump("roadevents");
                        g.ui.Banner("Bandits driven off!", "Merchant Tamsin pays you " + got);
                        p.bag.Touch(); g.MarkDirty();
                    }
                }
                break;
            case Kind.Child:
                if (following && npc != null)
                {
                    Vector3 me = p.transform.position, back = -p.transform.forward; back.y = 0f;
                    Vector3 want = me + back.normalized * 1.6f; want.y = npc.transform.position.y;
                    Vector3 d = want - npc.transform.position; d.y = 0f;
                    if (d.magnitude > 22f) npc.transform.position = want;   // (kept up on the road)
                    npc.route = new[] { want }; npc.routeSpeed = Mathf.Clamp(d.magnitude * 1.6f, 1.5f, 9f);
                    if (g.InTown(me) && g.InTown(npc.transform.position))
                    {
                        following = false; done = true; life = 8f; npc.route = null;
                        int L = Mathf.Max(1, p.level); long coins = (long)(30 + L * 16) * AHDB.CU;
                        p.AddMoney(coins, p.transform.position); p.GainClassXpDirect(40 + L * 12);
                        string got = AHItems.MoneyText(coins);
                        if (Random.value < 0.35f && p.bag.Add("mystery_sack")) got += " · a Mystery sack (from Pip's grateful mother)";
                        AHLife.Speak(npc, "Mama! Thank you, hero!", "أمي! شكرًا لك أيها البطل!", 4f);
                        p.prog.Bump("roadevents");
                        g.ui.Banner("Pip is home safe!", got);
                        p.bag.Touch(); g.MarkDirty();
                    }
                }
                break;
        }
        if (cur == Kind.Child && following) return;   // a child you are walking home doesn't wander off
        if (life <= 0f) Clear(g, true);
    }

    static void HelpKnight(AHGame g)
    {
        var p = g.player;
        if (done) { g.ui.Toast("Wounded knight Aldren: “I'll make it back now. Fair roads, friend.”", 3f); return; }
        string[] gifts = { "big_potion", "hp_potion", "stew", "pie", "trout", "bread", "meat" };
        string give = null; foreach (var id in gifts) if (p.bag.Count(id) > 0) { give = id; break; }
        if (give == null) { g.ui.Toast("Wounded knight Aldren: “A health potion... or anything to eat. Please.” (You have nothing to give.)", 4f); return; }
        p.bag.Take(give, 1); done = true; life = 20f;
        int L = Mathf.Max(1, p.level); string got;
        float r = Random.value;
        if (r < 0.35f && p.bag.Add("enh_stone", 3)) got = "3 Enhancement stones";
        else if (r < 0.6f && p.bag.Add(AHRareRecipes.ScrollId)) got = "a Rare recipe scroll";
        else if (r < 0.8f && p.bag.Add("lucky_charm")) got = "a Lucky charm";
        else { long coins = (long)(50 + L * 20) * AHDB.CU; p.AddMoney(coins, p.transform.position); got = AHItems.MoneyText(coins); }
        p.GainClassXpDirect(30 + L * 10);
        var it = AHItems.Get(give);
        AHLife.Speak(npc, "Bless you. Take this, I won't need it where I'm going: home.", "بارك الله فيك. خذ هذا، لن أحتاجه في طريقي إلى البيت.", 4f);
        npc.SetSeq("Idle_Loop"); npc.label = "Talk";
        p.prog.Bump("roadevents");
        g.ui.Banner("You helped the wounded knight", "You gave " + (it != null ? it.name.ToLowerInvariant() : give) + " · he gives you " + got);
        p.bag.Touch(); g.MarkDirty();
    }

    static void Clear(AHGame g, bool leave)
    {
        if (cur == Kind.Merchant && leave && g.ui != null) g.ui.Toast("The travelling merchant has packed up and moved on.", 3f);
        if (cur == Kind.Child && leave && !done && g.ui != null) g.ui.Toast("Little Pip's family found them on the road.", 3f);
        if (cur == Kind.Ambush && !done) foreach (var m in bandits) if (m != null) { g.mobs.Remove(m); Object.Destroy(m.gameObject); }
        bandits.Clear();
        foreach (var go in props) if (go != null) Object.Destroy(go);
        props.Clear();
        if (npc != null) { AHFx.Pop(npc.transform.position + Vector3.up, 1.2f, new Color(1f, 0.95f, 0.8f)); Object.Destroy(npc.gameObject); }
        npc = null; cur = Kind.None; following = false; done = false;
    }
}
