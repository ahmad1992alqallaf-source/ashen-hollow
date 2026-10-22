// Ashen Hollow: golden monsters. About one beast in three hundred comes back from the wild as a golden one: the same
// beast, covered in shining gold, glittering as it moves. Word spreads (the toast, the chat, and the town crier in
// Hollow Meadow), and whoever brings it down gets a golden haul: a heap of coins, that beast's monster card, and good
// odds of a mystery sack, gems and enhancement stones. Never in towns, dungeons, the Deep or among bosses and elites.
using System.Collections.Generic;
using UnityEngine;

public static class AHGolden
{
    public static float Chance = 1f / 300f;
    static readonly Dictionary<AHMob, List<KeyValuePair<Material, Color>>> was = new Dictionary<AHMob, List<KeyValuePair<Material, Color>>>();
    public static readonly List<AHMob> Live = new List<AHMob>();
    static float nextSpark;
    public static string LastSeen;   // for the crier

    static bool Can(AHGame g, AHMob m)
    {
        if (m == null || m.type == null || m.type.elite || m.type.rare) return false;
        string a = AHGame.AreaId ?? "";
        if (AHDungeon.IsDungeon(a) || a == AHDeep.Area || a == AHAbyss.Area) return false;
        if (AHMobPeople.Is(m.type.id) || m.type.id == "treasure_goblin" || m.type.id.StartsWith("kq") || AHEvents.IsWorldBoss(m.type.id)) return false;
        var d = AHJson.O(AHDB.Mobs, m.type.id);
        if (AHJson.B(d, "boss") || AHJson.B(d, "dboss") || AHJson.B(d, "world") || AHJson.B(d, "dungeon")) return false;
        if (g.InTown(m.Home)) return false;
        return true;
    }

    // each time a beast (re)appears: golden or not
    public static void Roll(AHGame g, AHMob m, bool force = false)
    {
        bool want = Can(g, m) && (force || Random.value < Chance * AHWeather.GoldenK);
        if (want == m.golden) return;
        if (want) Make(g, m); else Unmake(m);
    }

    static void Make(AHGame g, AHMob m)
    {
        m.golden = true; Live.Add(m);
        var keep = new List<KeyValuePair<Material, Color>>();
        foreach (var r in m.Model.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var mt in r.materials)
            {
                if (mt == null) continue;
                string prop = mt.HasProperty("baseColorFactor") ? "baseColorFactor" : mt.HasProperty("_BaseColor") ? "_BaseColor" : null;
                if (prop == null) continue;
                Color c = mt.GetColor(prop); keep.Add(new KeyValuePair<Material, Color>(mt, c));
                float lum = 0.35f + 0.65f * Mathf.Clamp01(c.r * 0.3f + c.g * 0.59f + c.b * 0.11f) * 1.6f;
                mt.SetColor(prop, new Color(1.0f, 0.78f, 0.28f, c.a) * lum * 1.35f);
                if (mt.HasProperty("_Metallic")) mt.SetFloat("_Metallic", 0.75f);
                if (mt.HasProperty("metallicFactor")) mt.SetFloat("metallicFactor", 0.75f);
                if (mt.HasProperty("_Smoothness")) mt.SetFloat("_Smoothness", 0.72f);
                if (mt.HasProperty("_EmissionColor")) { mt.EnableKeyword("_EMISSION"); mt.SetColor("_EmissionColor", new Color(0.35f, 0.24f, 0.04f)); }
            }
        }
        was[m] = keep;
        LastSeen = m.type.name.ToLowerInvariant();
        AHFx.Pillar(m.transform.position, 0.6f, 14f, new Color(1f, 0.82f, 0.3f, 0.6f), 6f);
        var p = g.player;
        if (p != null && g.ui != null)
        {
            Vector3 d = m.transform.position - p.transform.position; d.y = 0f;
            g.ui.Toast("A golden " + LastSeen + " has been seen" + (d.magnitude < 80f ? " nearby!" : " in " + (g.data != null ? g.data.region : "these lands") + "!"), 5f);
        }
        AHTownLife.Golden(m.type.name);
    }

    static void Unmake(AHMob m)
    {
        m.golden = false; Live.Remove(m);
        List<KeyValuePair<Material, Color>> keep;
        if (!was.TryGetValue(m, out keep)) return;
        foreach (var kv in keep)
        {
            if (kv.Key == null) continue;
            if (kv.Key.HasProperty("baseColorFactor")) kv.Key.SetColor("baseColorFactor", kv.Value); else if (kv.Key.HasProperty("_BaseColor")) kv.Key.SetColor("_BaseColor", kv.Value);
            if (kv.Key.HasProperty("_Metallic")) kv.Key.SetFloat("_Metallic", 0f);
            if (kv.Key.HasProperty("metallicFactor")) kv.Key.SetFloat("metallicFactor", 0f);
            if (kv.Key.HasProperty("_EmissionColor")) kv.Key.SetColor("_EmissionColor", Color.black);
        }
        was.Remove(m);
    }

    // a glitter of gold off every golden beast that is about
    public static void Tick()
    {
        if (Time.time < nextSpark || Live.Count == 0) return;
        nextSpark = Time.time + 0.18f;
        Live.RemoveAll(x => x == null);
        foreach (var m in Live)
        {
            if (m.dead || m.Model == null || !m.Model.activeInHierarchy) continue;
            Vector3 at = m.transform.position + Vector3.up * Random.Range(0.2f, Mathf.Max(0.5f, m.height)) + Random.insideUnitSphere * m.type.radius * 0.8f;
            AHSpark.Burst(at, new Color(1f, 0.85f, 0.35f, 0.95f), 2, 0.6f, 0.7f, 0.07f, 0.8f);
        }
    }

    public static void OnKill(AHGame g, AHMob m, AHPlayer by)
    {
        if (m == null || !m.golden || by == null) return;
        AHTrophies.AddSpecial(g, by, "golden:" + m.type.id, "Golden " + m.type.name.ToLowerInvariant(), m.type.model, "golden");
        int L = Mathf.Max(1, by.level);
        long coins = (long)(60 + L * 30 + Random.Range(0, 40 + L * 10)) * AHDB.CU;
        by.AddMoney(coins, m.transform.position);
        string got = AHItems.MoneyText(coins);
        string card = "card_" + m.type.id; if (AHItems.Get(card) != null && by.bag.Add(card)) got += " · its monster card";
        if (Random.value < 0.6f && by.bag.Add("mystery_sack")) got += " · a Mystery sack";
        if (Random.value < 0.45f) { string[] gs = { "ruby", "sapphire", "emerald" }; string gm = gs[Random.Range(0, 3)]; if (by.bag.Add(gm)) { var d = AHItems.Get(gm); got += " · " + (d != null ? d.name : gm); } }
        if (Random.value < 0.5f && by.bag.Add("enh_stone", 3)) got += " · 3 Enhancement stones";
        by.prog.Bump("golden");
        AHFx.Pop(m.transform.position + Vector3.up * 0.8f, 3f, new Color(1f, 0.85f, 0.3f));
        AHSpark.Burst(m.transform.position + Vector3.up, new Color(1f, 0.85f, 0.3f, 1f), 40, 4f, 1.2f, 0.12f, 2f);
        AHSound.Play("loot");
        if (g.ui != null) g.ui.Banner("Golden " + m.type.name.ToLowerInvariant() + " defeated!", got);
        by.bag.Touch(); g.MarkDirty();
        Unmake(m);
    }
}
