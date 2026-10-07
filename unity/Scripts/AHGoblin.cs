// Ashen Hollow: the treasure goblin. Now and then, out in the wild (never in a town, a dungeon or the arena), a
// goblin with a sack of loot pops up near you. It never fights: it runs from you, and every hit makes it bolt. Catch it
// within a minute for coins, experience and a good chance of a mystery sack or a gem; if not, it vanishes with its loot.
using UnityEngine;

public static class AHGoblin
{
    const string TypeId = "treasure_goblin";
    const float Life = 60f;
    static AHMobType type;
    static AHMob cur;
    static float wait = 300f, life, scareT;   // the first one comes no sooner than five minutes in

    static AHMobType Type(int L)
    {
        var all = AHDB.Mobs;
        if (all != null && !all.ContainsKey(TypeId))
        {
            var c = new System.Collections.Generic.Dictionary<string, object>();
            var bd = AHJson.O(AHDB.Mobs, "imp") as System.Collections.Generic.Dictionary<string, object>;
            if (bd != null) foreach (var kv in bd) c[kv.Key] = kv.Value;
            c["name"] = "Treasure goblin"; c["drops"] = new System.Collections.Generic.List<object>(); c["noSkin"] = true;
            all[TypeId] = c;
        }
        if (type == null) type = new AHMobType { id = TypeId, name = "Treasure goblin", model = "Mobs/imp", noSkin = true, respawn = 1e9f, atkCd = 99f, flee = true };
        type.lvl = L; type.hp = 60 + L * 14; type.dmg = 0; type.xp = 20 + L * 8; type.gold = 0;
        type.speed = 105 * AHDB.S; type.radius = 16 * AHDB.S; type.aggro = 0f;
        return type;
    }

    static bool CanAppear(AHGame g)
    {
        var p = g.player; if (p == null || p.dead) return false;
        if (AHKQ.Mode != "" || AHDungeon.IsDungeon(AHGame.AreaId)) return false;
        return !g.InTown(p.transform.position);
    }

    public static void Tick(AHGame g, float dt)
    {
        if (cur != null && (cur.dead || !cur.gameObject.activeInHierarchy)) cur = null;
        if (cur != null)
        {
            life -= dt;
            var p = g.player;
            // it keeps its distance: when you come close it runs
            scareT -= dt;
            if (p != null && scareT <= 0f && (cur.transform.position - p.transform.position).sqrMagnitude < 36f) { cur.Scare(2.5f); scareT = 2f; }
            if (life <= 0f) Escape(g);
            return;
        }
        wait -= dt; if (wait > 0f) return;
        wait = Random.Range(480f, 840f);   // one every eight to fourteen minutes
        if (CanAppear(g) && Random.value < 0.7f) Spawn(g);
    }

    public static bool Spawn(AHGame g)
    {
        var p = g.player; if (p == null) return false;
        if (cur != null && !cur.dead) return false;
        var tp = Type(Mathf.Max(1, p.level));
        float a = Random.value * Mathf.PI * 2f;
        Vector3 at = g.Resolve(p.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 11f, tp.radius);
        cur = AHMob.Create(g, tp, at); cur.add = true; g.mobs.Add(cur);
        cur.transform.localScale *= 0.85f;
        foreach (var r in cur.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.materials;
            foreach (var m in mats) foreach (var pr in new[] { "_BaseColor", "_Color", "baseColorFactor" }) if (m != null && m.HasProperty(pr)) { var c0 = m.GetColor(pr); m.SetColor(pr, Color.Lerp(c0, new Color(1f, 0.82f, 0.3f, c0.a), 0.45f)); }
            r.materials = mats;
        }
        life = Life; scareT = 0f;
        AHFx.Pop(at + Vector3.up * 0.6f, 1.8f, new Color(1f, 0.85f, 0.3f));
        AHSound.Play("coin");
        if (g.ui != null) g.ui.Banner("A treasure goblin!", "Catch it within a minute before it escapes with its loot");
        return true;
    }

    static void Escape(AHGame g)
    {
        if (cur == null) return;
        var m = cur; cur = null;
        AHFx.Pop(m.transform.position + Vector3.up * 0.6f, 1.6f, new Color(0.7f, 0.6f, 1f));
        if (!m.dead) m.Hurt(999999, null, true);
        g.mobs.Remove(m); m.gameObject.SetActive(false); Object.Destroy(m.gameObject, 0.1f);
        if (g.ui != null) g.ui.Toast("The treasure goblin escaped with its loot!", 2.5f);
    }

    // AHMob.Die: the loot when you catch it
    public static void OnKill(AHGame g, AHMob m, AHPlayer by)
    {
        if (m == null || m.type == null || m.type.id != TypeId || by == null) return;
        if (m == cur) cur = null;
        int L = Mathf.Max(1, by.level);
        long coins = (long)(30 + L * 18 + Random.Range(0, 20 + L * 6)) * AHDB.CU;
        by.AddMoney(coins, m.transform.position);
        string got = AHItems.MoneyText(coins);
        if (Random.value < 0.45f && by.bag.Add("mystery_sack")) got += " · a Mystery sack";
        if (Random.value < 0.35f) { string[] gs = { "ruby", "sapphire", "emerald" }; string gm = gs[Random.Range(0, 3)]; if (by.bag.Add(gm)) { var d = AHItems.Get(gm); got += " · " + (d != null ? d.name : gm); } }
        if (Random.value < 0.25f && by.bag.Add("enh_stone", 2)) got += " · 2 Enhancement stones";
        if (Random.value < 0.2f && by.bag.Add(AHRareRecipes.ScrollId)) got += " · a Rare recipe scroll";
        AHFx.Pop(m.transform.position + Vector3.up * 0.6f, 2.2f, new Color(1f, 0.85f, 0.3f));
        if (g.ui != null) g.ui.Banner("Treasure goblin caught!", got);
    }
}
