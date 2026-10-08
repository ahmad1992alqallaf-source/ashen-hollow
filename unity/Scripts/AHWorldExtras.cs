// Ashen Hollow: more to the world.
//  - Weather: clear, cloudy, rain and fog in the green lands, snow in the frozen ones. Rain makes the fish bite and the
//    herbs grow (faster fishing and herb picking); in fog and rain beasts notice you later (AHMob reads AggroK).
//  - Lore: twenty-four pages of the realm's history, found now and then while you hunt, gather and craft, collected in
//    the Library (rewards at 8, 16 and all 24).
//  - The weekend tournament: fishing, woodcutting or mining in turn, every Saturday and Sunday; the more you gather of
//    the weekend's kind the higher you place against the realm's best; prizes are claimed from Monday.
//  - Helpers: hire a woodcutter, a miner, a fisher or a herbalist who works while you are away (up to twelve hours).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHHelperState { public string id; public long since; }
[Serializable]
public class AHWorldState
{
    public List<int> lore = new List<int>(); public int loreClaimed;
    public string tKey = "", tKind = ""; public int tCount; public bool tClaimed;
    public List<AHHelperState> helpers = new List<AHHelperState>();
}

// ---------- weather ----------
public class AHWeather : MonoBehaviour
{
    public enum Kind { Clear, Cloudy, Rain, Fog, Snow }
    public static Kind Now = Kind.Clear;
    static AHWeather inst;
    ParticleSystem fall; float nextT, sunK = 1f, fogK = 1f, wantSun = 1f, wantFog = 1f;

    public static void Setup(AHGame g)
    {
        if (inst == null) { var go = new GameObject("Weather"); inst = go.AddComponent<AHWeather>(); }
        inst.nextT = 0f;
        if (AHDungeon.IsDungeon(AHGame.AreaId) || AHGame.AreaId == AHDeep.Area) { Now = Kind.Clear; inst.Apply(); }   // no weather underground (the Deep too)
    }
    public static string Name { get { return Now == Kind.Rain ? "Rain" : Now == Kind.Fog ? "Fog" : Now == Kind.Snow ? "Snow" : Now == Kind.Cloudy ? "Clouds" : "Clear"; } }
    // gathering in the weather: rain brings fish to the surface and opens the herbs
    public static float GatherK(string kind) { return Now == Kind.Rain ? (kind == "fish" ? 1.25f : kind == "herb" ? 1.15f : 1f) : 1f; }
    public static float AggroK { get { return Now == Kind.Fog ? 0.6f : Now == Kind.Rain ? 0.85f : 1f; } }   // fog and rain: beasts notice you later

    static bool Cold(string a) { return a.Contains("frost"); }
    static bool Dry(string a) { return a.Contains("sand") || a.Contains("ember") || a.Contains("cinder"); }

    void Pick()
    {
        string a = AHGame.AreaId ?? "";
        float r = UnityEngine.Random.value;
        if (AHDungeon.IsDungeon(a) || a == AHDeep.Area) Now = Kind.Clear;
        else if (Cold(a)) Now = r < 0.45f ? Kind.Snow : r < 0.7f ? Kind.Cloudy : Kind.Clear;
        else if (Dry(a)) Now = r < 0.2f ? Kind.Cloudy : Kind.Clear;
        else Now = r < 0.2f ? Kind.Rain : r < 0.32f ? Kind.Fog : r < 0.55f ? Kind.Cloudy : Kind.Clear;
        nextT = UnityEngine.Random.Range(240f, 480f);
        Apply();
    }

    void Apply()
    {
        wantSun = Now == Kind.Rain ? 0.6f : Now == Kind.Cloudy ? 0.82f : Now == Kind.Fog ? 0.75f : Now == Kind.Snow ? 0.85f : 1f;
        wantFog = Now == Kind.Fog ? 0.38f : Now == Kind.Rain ? 0.7f : Now == Kind.Snow ? 0.75f : 1f;
        bool falls = Now == Kind.Rain || Now == Kind.Snow;
        if (falls && fall == null) fall = MakeFall();
        if (fall != null)
        {
            var main = fall.main; var em = fall.emission; var rr = fall.GetComponent<ParticleSystemRenderer>();
            if (Now == Kind.Rain) { main.startSpeed = 18f; main.startLifetime = 1.1f; main.startSize = 0.05f; main.startColor = new Color(0.75f, 0.82f, 0.95f, 0.45f); em.rateOverTime = 700f; rr.renderMode = ParticleSystemRenderMode.Stretch; rr.velocityScale = 0.06f; rr.lengthScale = 1f; }
            else if (Now == Kind.Snow) { main.startSpeed = 1.6f; main.startLifetime = 7f; main.startSize = 0.09f; main.startColor = new Color(1f, 1f, 1f, 0.85f); em.rateOverTime = 260f; rr.renderMode = ParticleSystemRenderMode.Billboard; }
            if (falls) { if (!fall.isPlaying) fall.Play(); } else fall.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    ParticleSystem MakeFall()
    {
        var mat = AHGame.LoadMat("AH/Materials/Spark", "AshenHollow/Spark"); if (mat == null) mat = AHFx.Mat; if (mat == null) return null;
        var go = new GameObject("WeatherFall"); go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = 3000; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = 0f;
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40f, 1f, 40f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // the box emits downward (its z), spread across the sky
        var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        return ps;
    }

    void LateUpdate()
    {
        var g = AHGame.I; if (g == null || g.cam == null) return;
        nextT -= Time.deltaTime; if (nextT <= 0f) Pick();
        sunK = Mathf.MoveTowards(sunK, wantSun, Time.deltaTime * 0.08f);
        fogK = Mathf.MoveTowards(fogK, wantFog, Time.deltaTime * 0.08f);
        // the day cycle sets the light every frame; the weather dims it after
        if (RenderSettings.sun != null) RenderSettings.sun.intensity *= sunK;
        if (!AHDungeon.IsDungeon(AHGame.AreaId)) { float reach = AHSettings.Reach; RenderSettings.fogStartDistance = reach * 0.36f * fogK; RenderSettings.fogEndDistance = (reach - 5f) * Mathf.Max(0.45f, fogK); }
        if (fall != null) fall.transform.position = g.cam.transform.position + Vector3.up * 14f + g.cam.transform.forward * 6f;
    }
}

// ---------- lore, the tournament and helpers ----------
public static class AHWorld
{
    public static AHWorldState St(AHPlayer p) { if (p.prog.world == null) p.prog.world = new AHWorldState(); return p.prog.world; }

    public static readonly string[][] Lore =
    {
        new[] { "The First Fire", "Before the towns, the meadow burned for a hundred days. When the ash cooled, the first settlers found the soil black and rich, and named their village for it: Ashen Hollow." },
        new[] { "The Waystones", "No one living remembers who raised the waystones. The oldest maps already show them, marked only with a rune that means ‘walk on’." },
        new[] { "Mirelle's Apprenticeship", "Mirelle Ashby came to the guild at nine years old, too small to lift a hammer. She learned to forge by holding the tongs for others for five winters." },
        new[] { "Varrow's Charter", "Varrow was a toll bridge before it was a city. The bridge-keepers grew rich on coin from the south road and built walls around their wealth." },
        new[] { "The Silk Moths", "The Silkwood is named for its moths, whose cocoons hang in the canopy like lanterns. Weavers climb for them at dawn, before the spiders wake." },
        new[] { "Mire Lights", "Travellers in the mire follow lights that drift above the water. The lucky ones reach the far bank. The others are found in spring, smiling." },
        new[] { "The Vale Crowns", "Seven kings were crowned in the Vale, and all seven crowns were lost. Treasure hunters still dig the hillsides for them." },
        new[] { "Frostpeak's Wardens", "The wardens of Frostpeak keep a fire burning at the summit that has not gone out in three hundred years. If it ever does, they say, the winter will come down the mountain." },
        new[] { "The Sand Queen", "Ssera ruled the Sunscar from a palace of glass. When the oasis failed she sank her palace into the dunes, and her soldiers became scorpions to guard it." },
        new[] { "Bones of the Isle", "On the Fossil Isle the great beasts of the first age still walk. Scholars argue whether they survived, or were never meant to die." },
        new[] { "Captain Saltbeard", "Saltbeard sailed under every flag and none. He buried his gold on every island of the Tidewake so no single map could lead to all of it." },
        new[] { "Thalassa", "The Tide Queen was a fisherwoman once. She made a bargain with the deep for her drowned children, and the deep kept both sides of it." },
        new[] { "Emberreach", "The land around Cinderhold is still cooling from a war between dragons. Its smiths say the hottest forges in the realm need no coal: only patience and a crack in the ground." },
        new[] { "Ignirax", "The Ashen Wyrm slept under the first fire. Some say the meadow burned because he turned over in his sleep." },
        new[] { "Pyraxis", "The Molten King was crowned in a river of fire, and wears it still. His court of imps fetch him iron to eat." },
        new[] { "The Ember Throne", "Vaelor built a throne from the scales of every dragon he killed. When the throne was finished he found he could not stand up from it." },
        new[] { "Oren's Book", "Master Oren wrote his recipes in a code only apprentices could read: ‘Because masters forget, and apprentices never do.’" },
        new[] { "The Guild Fire", "The guild hall burned on the longest night of the year. Mirelle says it was an accident. Corvin Vale says nothing at all." },
        new[] { "Pet Tamers", "The six tamers of the realm have never lost to one another. Each keeps a single defeat secret: the day their first pet beat them." },
        new[] { "Night Markets", "When the lanterns go up, the night traders come out, selling things that are not quite legal and not quite real." },
        new[] { "The Lightfoot Way", "The wardens of the old roads learned to run across rooftops and treetops, three bounds in a breath. Anyone can learn it, they say, once they stop being afraid of falling." },
        new[] { "The Masterwork Contest", "Every month the guilds of the realm send their best work to be judged. No one has won three months in a row. Corvin Vale has tried." },
        new[] { "Homesteads", "The Land Agents sell deeds to the wild places at the edges of the map. Every farm in the realm started as a deed and a stubborn settler." },
        new[] { "The Last Page", "Whoever gathers every page of this history, wrote its unknown author, ‘has walked further than I ever did. Write the next chapter yourself.’" },
    };

    // every quest event: a lore page now and then, and the weekend tournament's count
    public static void OnEvent(AHGame g, string t, string id, int n)
    {
        var p = g.player; if (p == null) return;
        var s = St(p);
        if ((t == "kill" || t == "gather" || t == "craft") && s.lore.Count < Lore.Length && UnityEngine.Random.value < (t == "kill" ? 0.012f : 0.008f))
        {
            int next = 0; while (s.lore.Contains(next)) next++;
            s.lore.Add(next);
            if (g.ui != null) g.ui.Banner("Lore: " + Lore[next][0], "A page for your Library (" + s.lore.Count + "/" + Lore.Length + ")");
            g.MarkDirty();
        }
        if (t == "gather") { Refresh(p); if (Active() && AHDaily_Kind(id) == s.tKind) { s.tCount += n; } }
    }
    static string AHDaily_Kind(string id) { return AHQuests.GatherKind(id); }

    public static readonly int[] LoreAt = { 8, 16, 24 };
    public static AHDaily.Reward LoreReward(int i) { return i == 2 ? new AHDaily.Reward { marks = 200, items = { new KeyValuePair<string, int>("diamond", 1) } } : new AHDaily.Reward { marks = i == 0 ? 50 : 100, items = { new KeyValuePair<string, int>("mystery_sack", i + 1) } }; }

    // ---------- the weekend tournament ----------
    static readonly string[] TKinds = { "fish", "logs", "ore" };
    static readonly string[] TNames = { "Fishing Tournament", "Woodcutting Race", "Mining Rush" };
    public static string KindOf(DateTime t) { var mon = t.Date.AddDays(-(((int)t.DayOfWeek + 6) % 7)); int wk = (int)((mon - new DateTime(2026, 1, 5)).TotalDays / 7); return TKinds[((wk % 3) + 3) % 3]; }
    public static string NameOf(string kind) { int i = Array.IndexOf(TKinds, kind); return i >= 0 ? TNames[i] : "Tournament"; }
    public static bool Active() { var d = DateTime.Now.DayOfWeek; return d == DayOfWeek.Saturday || d == DayOfWeek.Sunday; }
    public static void Refresh(AHPlayer p)
    {
        var s = St(p); string k = AHDaily.WeekKey(DateTime.Now);
        if (s.tKey != k && (s.tKey == "" || s.tClaimed || s.tCount == 0)) { s.tKey = k; s.tKind = KindOf(DateTime.Now); s.tCount = 0; s.tClaimed = false; }
    }
    public static List<KeyValuePair<string, int>> Field(string key, string kind, int level)
    {
        var rnd = new System.Random((key + kind).GetHashCode());
        string[] names = { "Hal the Angler", "Brisa Quickaxe", "Old Tobin", "Mara Deepline", "Gruff Ingot", "Pip Willow" };
        var l = new List<KeyValuePair<string, int>>(); int top = 60 + Mathf.Clamp(level, 1, 60) * 4;
        foreach (var n in names) l.Add(new KeyValuePair<string, int>(n, Mathf.RoundToInt(top * (0.3f + (float)rnd.NextDouble() * 0.8f))));
        return l;
    }
    public static int Place(AHPlayer p) { var s = St(p); int r = 1; foreach (var kv in Field(s.tKey, s.tKind, p.level)) if (kv.Value > s.tCount) r++; return r; }
    public static bool Claimable(AHPlayer p) { var s = St(p); return !s.tClaimed && s.tCount > 0 && (s.tKey != AHDaily.WeekKey(DateTime.Now) || DateTime.Now.DayOfWeek == DayOfWeek.Monday); }
    public static AHDaily.Reward Prize(AHPlayer p, int place)
    {
        long step = AHHome.LevelStep(p);
        if (place == 1) return new AHDaily.Reward { money = (long)Math.Round(1500 * (1 + p.level / 4.0)), xp = step, marks = 150, items = { new KeyValuePair<string, int>("diamond", 1) } };
        if (place <= 3) return new AHDaily.Reward { money = (long)Math.Round(700 * (1 + p.level / 4.0)), xp = step / 2, marks = 80 };
        return new AHDaily.Reward { money = (long)Math.Round(200 * (1 + p.level / 4.0)), marks = 25 };
    }

    // ---------- helpers ----------
    public class Helper { public string id, name, what; public string[] items; public int skillLv; }
    public static readonly Helper[] Helpers =
    {
        new Helper { id = "woodcutter", name = "Woodcutter", what = "chops logs", items = new[] { "logs", "pine_logs" } },
        new Helper { id = "miner", name = "Miner", what = "mines ore", items = new[] { "copper_ore", "tin_ore", "iron_ore" } },
        new Helper { id = "fisher", name = "Fisher", what = "catches fish", items = new[] { "raw_trout", "raw_salmon" } },
        new Helper { id = "herbalist", name = "Herbalist", what = "picks herbs", items = new[] { "sunpetal", "mireroot" } },
    };
    public const int PerHour = 6, CapHours = 12;
    public static long HireCost(AHPlayer p) { return (long)(400 * (1 + St(p).helpers.Count)) * AHDB.CU; }
    static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
    public static AHHelperState Hired(AHPlayer p, string id) { return St(p).helpers.Find(h => h.id == id); }
    public static bool Hire(AHGame g, string id)
    {
        var p = g.player; if (Hired(p, id) != null) return false; long c = HireCost(p); if (p.bag.money < c) return false;
        p.bag.money -= c; St(p).helpers.Add(new AHHelperState { id = id, since = NowS }); g.SaveProgress(); return true;
    }
    public static int Ready(AHPlayer p, string id)
    {
        var h = Hired(p, id); if (h == null) return 0;
        double hours = Math.Min(CapHours, (NowS - h.since) / 3600.0);
        return (int)Math.Floor(hours * PerHour);
    }
    public static string Collect(AHGame g, string id)
    {
        var p = g.player; var h = Hired(p, id); if (h == null) return null; int n = Ready(p, id); if (n <= 0) return null;
        var def = Array.Find(Helpers, x => x.id == id);
        var got = new Dictionary<string, int>();
        for (int i = 0; i < n; i++) { string it = def.items[UnityEngine.Random.Range(0, def.items.Length)]; if (AHItems.Get(it) == null) continue; got[it] = (got.ContainsKey(it) ? got[it] : 0) + 1; }
        var l = new List<string>();
        foreach (var kv in got) if (p.bag.Add(kv.Key, kv.Value)) l.Add(kv.Value + "× " + AHItems.Get(kv.Key).name);
        h.since = NowS; g.SaveProgress();
        return l.Count > 0 ? "Your " + def.name.ToLowerInvariant() + " brings " + string.Join(", ", l.ToArray()) : (p.bag.lastWarn ?? "Your bag is full.");
    }
}

public partial class AHUI
{
    string worldTab = "lore";
    public void OpenWorld(string tab = null) { if (tab != null) worldTab = tab; wkMode = "world"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderRealm(AHPlayer p)
    {
        var st = AHWorld.St(p); AHWorld.Refresh(p);
        wkTitle.text = "The realm";
        wkHint.text = "Weather here: " + AHWeather.Name + (AHWeather.Now == AHWeather.Kind.Rain ? " (fish bite faster, herbs open)" : AHWeather.Now == AHWeather.Kind.Fog ? " (beasts notice you later)" : "");
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, "", Color.white, "", "",
            new WkBtn { label = "Library", on = true, col = worldTab == "lore" ? Go : Plain, act = () => { worldTab = "lore"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Tournament", on = true, col = worldTab == "tour" ? Go : Plain, act = () => { worldTab = "tour"; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "Helpers", on = true, col = worldTab == "help" ? Go : Plain, act = () => { worldTab = "help"; wkPageI = 0; RenderWork(); } }));
        if (worldTab == "lore")
        {
            int got = st.lore.Count;
            int ci = st.loreClaimed;
            rows.Add(s => Row(s, "Library · " + got + " / " + AHWorld.Lore.Length + " pages", Gold, "Pages turn up now and then while you hunt, gather and craft.", ci < 3 ? "At " + AHWorld.LoreAt[ci] + " pages: " + AHDaily.Text(AHWorld.LoreReward(ci)) : "Every reward claimed",
                ci >= 3 ? null : new WkBtn { label = "Claim", on = got >= AHWorld.LoreAt[ci], col = Go, act = () => { if (st.loreClaimed < 3 && st.lore.Count >= AHWorld.LoreAt[st.loreClaimed]) { AHDaily.Grant(g, AHWorld.LoreReward(st.loreClaimed)); st.loreClaimed++; } RenderWork(); } }));
            var ids = new List<int>(st.lore); ids.Sort();
            foreach (var i in ids)
            {
                var L = AHWorld.Lore[i]; var lines = Wrap(L[1], 92);
                for (int li = 0; li < lines.Count; li += 2) { var a = lines[li]; var b = li + 1 < lines.Count ? lines[li + 1] : ""; bool f = li == 0; rows.Add(s => Row(s, f ? L[0] : "", new Color(1f, 0.85f, 0.55f), "<i>" + a + "</i>", b != "" ? "<i>" + b + "</i>" : "")); }
            }
        }
        else if (worldTab == "tour")
        {
            string nm = AHWorld.NameOf(st.tKind); bool act = AHWorld.Active();
            rows.Add(s => Row(s, nm, new Color(0.56f, 0.85f, 1f), act ? "On now, all weekend: gather as many " + (st.tKind == "fish" ? "fish" : st.tKind == "logs" ? "logs" : "ore") + " as you can." : "The next one runs on Saturday and Sunday.", "Your count: " + st.tCount));
            if (AHWorld.Claimable(p))
            {
                int pl = AHWorld.Place(p); var rw = AHWorld.Prize(p, pl);
                rows.Add(s => Row(s, "You placed " + AHArtisan.Ordinal(pl), pl == 1 ? Gold : Color.white, AHDaily.Text(rw), "",
                    new WkBtn { label = "Claim", on = true, col = Go, act = () => { st.tClaimed = true; AHDaily.Grant(g, rw); Banner(nm, "Prize claimed"); AHWorld.Refresh(p); RenderWork(); } }));
            }
            var field = AHWorld.Field(st.tKey, st.tKind, p.level); field.Sort((a, b) => b.Value.CompareTo(a.Value));
            foreach (var kv in field) { var k2 = kv; rows.Add(s => Row(s, k2.Key, new Color(1f, 1f, 1f, 0.7f), (act || AHWorld.Claimable(p) ? k2.Value + " gathered" : "Entered · competes at the weekend"), "")); }
        }
        else
        {
            foreach (var h in AHWorld.Helpers)
            {
                var hh = h; var hired = AHWorld.Hired(p, hh.id); int ready = AHWorld.Ready(p, hh.id); long cost = AHWorld.HireCost(p);
                rows.Add(s => Row(s, hh.name, new Color(0.6f, 0.9f, 0.5f), hired == null ? "Hire once: " + hh.what + " for you, " + AHWorld.PerHour + " an hour, while you are away (up to " + AHWorld.CapHours + " hours)" : ready + " ready to collect", hired == null ? AHItems.MoneyText(cost) : "",
                    hired == null ? new WkBtn { label = "Hire", on = p.bag.money >= cost, col = Go, act = () => { if (AHWorld.Hire(g, hh.id)) Banner(hh.name + " hired", "Works while you are away"); RenderWork(); } }
                                  : new WkBtn { label = "Collect", on = ready > 0, col = Go, act = () => { string m = AHWorld.Collect(g, hh.id); if (m != null) Toast(m, 3f); RenderWork(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
