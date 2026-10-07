// Ashen Hollow (Unity slice): builds Hollow Meadow from the data exported by the web game,
// then runs the day, the camera, the water and the sky. Put this on one empty GameObject in a scene.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AHGame : MonoBehaviour
{
    public static AHGame I;

    [Header("Camera")]
    public float camDistance = 7f;
    [System.NonSerialized] public float cineT;
    public float camPitch = 22f;          // degrees above the hero
    public float camYaw = 90f;
    float crSpin, crDist = 4.2f; bool wasCreating;   // the hero creator's own turn and zoom
    [Header("Day")]
    [Tooltip("Seconds for a whole day and night (24 minutes in the web game)")]
    public float dayLength = 1440f;
    [Range(0f, 1f)] public float startTimeOfDay = 0.2f;   // 0.25 is noon
    [Header("Models")]
    [Tooltip("Extra turn for models that face sideways after import")]
    public float heroYawFix = 0f;
    public float wolfYawFix = 0f;
    [Tooltip("The detailed hero built from the character creator's look (off: the KayKit class heroes)")]
    public bool lookHero = true;
    [Tooltip("How the hero holds the weapon in the right hand (degrees) and where")]
    public bool tuneGrip = false;
    public Vector3 weaponRot = new Vector3(180f, 0f, 0f), weaponOffset = new Vector3(0f, 0.06f, 0f);
    public Vector3 offhandRot = new Vector3(180f, 0f, 0f), offhandOffset = new Vector3(0f, 0.06f, 0f);

    [HideInInspector] public AHWorldData data;
    [HideInInspector] public Camera cam;
    [HideInInspector] public AHPlayer player;
    [HideInInspector] public readonly List<AHMob> mobs = new List<AHMob>();
    [HideInInspector] public AHUI ui;

    Transform world;
    public Transform World { get { return world; } }
    Vector3 ax = new Vector3(-1, 0, 0), az = new Vector3(0, 0, 1);   // Unity metres per web metre along web x and z
    float modelYaw;                                                  // turn that makes a model built facing web +z face Unity +z
    readonly List<Vector3> cPos = new List<Vector3>();
    readonly List<float> cRad = new List<float>();
    readonly List<Rect> rects = new List<Rect>();
    [System.NonSerialized] public readonly List<Rect> doors = new List<Rect>();   // dungeon gates (removed when they open)
    readonly List<Rect> floors = new List<Rect>();                                // data.walk: the only floor there is
    // the web zones' tiles (zones.json ALLZ): rock, deep water and lava can't be walked on
    byte[] tiles; int tCols, tRows; float tX0, tZ0; const float Tile = 4f, RockPad = 1.1f;   // metres
    static readonly bool[] WalkT = { true, true, false, false, true, false, true, true, true, true };
    readonly List<Vector4> lakes = new List<Vector4>();               // centre x, centre z, radius x, radius z
    Rect area;
    public Rect AreaRect { get { return area; } }
    Light sun;
    Material skyMat, waterMat;
    float dayT;
    Vector3 camTarget;

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        if (Application.isMobilePlatform) PhoneQuality();
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;   // the phone screen stays on while you play
        Build();
    }

    // phones: 30 frames a second, a slightly lower render size on very sharp screens and shorter shadows, so the
    // phone stays cool and the battery lasts (the picture still looks the same at arm's length)
    void PhoneQuality()
    {
        Application.targetFrameRate = 30;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            int longSide = Mathf.Max(Screen.width, Screen.height);
            urp.renderScale = longSide > 2000 ? 0.7f : longSide > 1600 ? 0.8f : 0.9f;
            urp.shadowDistance = Mathf.Min(urp.shadowDistance, 30f);
            urp.msaaSampleCount = 1;
        }
    }

    // ---------- positions: web metres to Unity ----------
    public Vector3 W(float x, float z) { return ax * x + az * z + (world != null ? world.position : Vector3.zero); }
    public Quaternion Face(Vector3 dir)
    {
        dir.y = 0;
        if (dir.sqrMagnitude < 1e-6f) return Quaternion.identity;
        return Quaternion.Euler(0, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 0);
    }
    public float ModelYaw { get { return modelYaw; } }

    void Build()
    {
        // which part of the world the hero is in (web: regions and the roads between them, exported to Resources/AH/Areas)
        AHPrefs.Locked = false;
        AreaId = AHPrefs.GetString("ah_area", "meadow");
        TextAsset ta = Resources.Load<TextAsset>(AreaPath + AreaId);
        if (ta == null && AreaId != "meadow") { Debug.LogWarning("Ashen Hollow: area " + AreaId + " is missing, back to the meadow"); AreaId = "meadow"; ta = Resources.Load<TextAsset>(AreaPath + AreaId); }
        if (ta == null) { AreaPath = "AH/"; ta = Resources.Load<TextAsset>("AH/meadow"); }   // the v0.3 meadow files
        if (ta == null) { Debug.LogError("Ashen Hollow: Resources/AH/Areas/meadow.json is missing"); return; }
        data = JsonUtility.FromJson<AHWorldData>(ta.text);
        if (string.IsNullOrEmpty(data.id)) data.id = AreaId;
        if (data.dayLength > 0) dayLength = data.dayLength;

        // the area itself: ground, trees, mountains, houses and water
        Spread = 1f;
        GameObject wp = Resources.Load<GameObject>(AreaPath + AreaId + "_world_kk");   // a city with its old houses cut out for the detailed buildings
        if (wp == null) wp = Resources.Load<GameObject>(AreaPath + AreaId + "_world");
        if (wp == null) Debug.LogError("Ashen Hollow: the " + AreaId + " model did not import. Is the glTFast package installed?");
        else
        {
            world = Instantiate(wp).transform;
            world.name = data.region;
            Transform mx = FindDeep(world, "AH_MARK_X"), mz = FindDeep(world, "AH_MARK_Z");
            if (mx != null && mz != null) { ax = (mx.position - world.position) / 100f; az = (mz.position - world.position) / 100f; }
            // the wild lands are laid out wider than the web game drew them (everything keeps its size, the gaps grow)
            Spread = SpreadOf(AreaId);
            if (Spread != 1f) { ax *= Spread; az *= Spread; SpreadWorld(Spread); }
            modelYaw = -Mathf.Atan2(az.x, az.z) * Mathf.Rad2Deg;
            SetupWorldRenderers();
            AHCity.Outskirts(this, world);   // land beyond a city's walls, so gates don't open onto nothing
            AHCity.Setup(this, world);   // detailed buildings in the cities
        }

        // things you bump into
        foreach (var c in data.circles) { cPos.Add(W(c.x, c.z)); cRad.Add(c.r * 0.8f); }
        foreach (var b in data.boxes)
        {
            var rr = MapRect(b.x, b.z, b.w, b.h);   // a wall or house keeps its own size when the land is spread
            if (Spread != 1f) { var c = rr.center; rr.size /= Spread; rr.center = c; }
            rects.Add(rr);
        }
        foreach (var w in data.water)
        {
            Vector3 c = W(w.x, w.z);
            float rx = Mathf.Abs(ax.x) * w.rx + Mathf.Abs(az.x) * w.rz, rz = Mathf.Abs(ax.z) * w.rx + Mathf.Abs(az.z) * w.rz;
            lakes.Add(new Vector4(c.x, c.z, rx, rz));
        }
        area = MapRect(data.bounds.x0, data.bounds.z0, data.bounds.x1 - data.bounds.x0, data.bounds.z1 - data.bounds.z0);
        if (data.walk != null) foreach (var b in data.walk) floors.Add(MapRect(b.x, b.z, b.w, b.h));
        LoadTiles();
        // trees, rocks, herbs, fishing spots and the town's work stations
        AHGather.Build(this, world);
        AHVillage.Setup(this, world);  // a ring of buildings around the starter village's square (before the trees)
        AHForest.Setup(this, world);   // the new trees on the same spots
        AHOre.Setup(this);             // boulders with ore crystals for the mining rocks
        AHHerbs.Setup(this);           // each herb a little plant of its own kind
        AHStations.Setup(this, world);  // campfires, furnaces and anvils rebuilt
        AHMountains.Setup(this, world); // craggy rock mountains instead of the plain cones
        AHRange.Build(this, world);     // great mountain ranges on the horizon, past the border crags
        AHTents.Setup(this, world);     // striped pavilions and ridge tents instead of the pyramids
        AHScenery.Setup(this, world);   // wooden footbridges and broken stone columns
        AHDreamScatter.Setup(this, world);  // Dreamscape bushes, flowers, grass and rocks (lands that use the pack)

        SetupLightAndSky();
        SetupCamera();
        if (GetComponent<AHCull>() == null) gameObject.AddComponent<AHCull>();   // far-off figures stop drawing

        // the time of day first: night beasts only come out after dark (it carries on when you change area)
        dayT = carryDayT >= 0f ? carryDayT : startTimeOfDay * dayLength;
        UpdateDay();

        // the hero and the beasts of the meadow
        string saved = AHPrefs.GetString("ah_class", "");
        player = AHPlayer.Create(this, W(data.spawn.x, data.spawn.z), saved == "" ? "warrior" : saved);
        // a saved hero comes back as they were; a new one starts in the class outfit with two grilled trout
        if (!AHSave.Load(this)) player.StartKit();
        player.bag.Changed += MarkDirty;
        quests.Changed += MarkDirty;
        var types = new Dictionary<string, AHMobType>();
        foreach (var t in data.mobTypes) { ApplyWebStats(t); types[t.id] = t; }
        foreach (var s in data.mobs)
        {
            AHMobType t;
            if (!types.TryGetValue(s.type, out t)) continue;
            mobs.Add(AHMob.Create(this, t, W(s.x, s.z)));
        }
        AHNpc.SpawnTown(this);
        AHNpc.HideOldFigures(this);   // the web version's box-built townsfolk still stood in the area models
        ui = AHUI.Create(this);
        AHTreasure.Setup(this);         // the treasure X you marked here (and the explorer's map on your first Fossil Lands visit): needs the hero and the UI
        AHSound.Ensure(this);
        AHFinder.Setup(this);
        AHDungeon.Setup(this, world);
        AHDungeonDress.Setup(this);   // torches, crates and rubble along the dungeon walls
        AHShops.Setup(this);
        AHRep.Setup(this);
        AHWays.Setup(this);
        AHGraves.Setup(this);
        AHFest.Setup(this);
        AHNight.Setup(this);
        AHWardrobe.Setup(this);
        AHCityHouse.Setup(this);
        AHAuction.Load();
        AHHomeView.Setup(this);
        AHRaidView.Setup(this);
        AHKQ.Setup(this);
        AHComp.SpawnPet(this);
        AHComp.SpawnAllies(this);
        grass = AHGrass.Create(this);
        AHSettings.Apply(this);
        AHWeather.Setup(this);
        if (saved == "") ui.OpenCreator();
        else if (SelectAtStart) ui.OpenSelect();
        else Welcome();
        SelectAtStart = false;
        camTarget = player.transform.position;
        travelLock = Time.time + 1.5f;
        if (arriving)
        {
            arriving = false;
            // face the way you were going, with the camera behind you
            player.transform.rotation = Quaternion.Euler(0f, carryYaw, 0f); camYaw = carryYaw;
            // come in at a land's edge: turn to face into the land (toward its heart), never out at the border
            Vector3 pp = player.transform.position, heart = W(data.spawn.x, data.spawn.z), into = heart - pp; into.y = 0f;
            float edge = Mathf.Min(Mathf.Min(pp.x - area.xMin, area.xMax - pp.x), Mathf.Min(pp.z - area.yMin, area.yMax - pp.z));
            if (edge < 18f && into.sqrMagnitude > 4f && Vector3.Dot(player.transform.forward, into.normalized) < 0.5f)
            {
                player.transform.rotation = Quaternion.LookRotation(into.normalized);
                camYaw = player.transform.eulerAngles.y;
            }
            foreach (var place in AHQuestLog.PlacesOf(AreaId)) quests.Event("visit", place, 1, this);
        }
    }

    // the beasts use the web game's numbers (monsters.json MOBS, after its own tuning); the meadow file gives where they stand
    static void ApplyWebStats(AHMobType t)
    {
        var d = AHJson.O(AHDB.Mobs, t.id);
        if (d == null) return;
        t.name = AHJson.S(d, "name", t.name);
        t.lvl = (int)AHJson.N(d, "lvl", t.lvl);
        t.hp = (int)AHJson.N(d, "hp", t.hp);
        t.dmg = (int)AHJson.N(d, "dmg", t.dmg);
        t.xp = (int)AHJson.N(d, "xp", t.xp);
        t.speed = (float)AHJson.N(d, "speed", t.speed / AHDB.S) * AHDB.S;
        t.radius = (float)AHJson.N(d, "r", t.radius / AHDB.S) * AHDB.S;
        t.aggro = (float)AHJson.N(d, "aggro", 0) * AHDB.S;
        t.flee = AHJson.B(d, "flee");
        t.gold = (int)AHJson.N(d, "gold");
        t.skinReq = (int)AHJson.N(d, "skinReq", 1);
        t.noSkin = AHJson.B(d, "noSkin");
        t.atkCd = (float)AHJson.N(d, "atkCd", 1.3);
        t.respawn = (float)AHJson.N(d, "respawn", 18);
        t.night = AHJson.B(d, "night"); t.lunge = AHJson.B(d, "lunge"); t.elite = AHJson.B(d, "elite"); t.rare = AHJson.B(d, "rare");
        t.drops = AHJson.A(d, "drops");
        var sk = AHJson.A(AHJson.O(AHDB.Rules, "MOB_SKIN"), t.id);
        if (sk != null) { t.skin = new string[sk.Count]; for (int i = 0; i < sk.Count; i++) t.skin[i] = (string)sk[i]; }
    }

    public void Welcome() { ui.Banner(data.region, AreaSub()); DangerCheck(); Discover(); }

    // ---------- several heroes: log out, change hero ----------
    public static bool SelectAtStart = true;   // the hero select screen when the game starts and after logging out
    public void Logout()
    {
        if (leaving) return;
        SaveProgress();
        Reload(true, true);
    }
    // build this land again (another hero, a changed setting); select: open the hero select screen after
    public void Reload(bool select, bool noSave = false)
    {
        if (leaving) return;
        leaving = true;
        if (!noSave) SaveProgress();
        AHPrefs.Locked = true;
        SelectAtStart = select;
        arriving = false; carryDayT = dayT;
        Time.timeScale = 1f;
        if (ui != null) ui.Fade(true);
        StartCoroutine(LoadNext());
    }

    // the creator's Begin button: the hero is made
    public void FinishCreation()
    {
        if (player.profMain == null && player.profs.Count > 0) player.profMain = player.profs[0];
        AHPrefs.SetString("ah_class", player.cls.id);
        player.SetClass(player.cls.id);
        ui.RefreshClass();
        SaveProgress();
        ui.Banner(data.region, AreaSub());
        if (player.PeaceOn) ui.Toast("Artisan: monsters leave you alone unless you strike first.");
    }

    // a beast was skinned (web questEvent('skin', type))
    public void OnSkin(string mob) { quests.Event("skin", mob, 1, this); }

    // web peacefulFor: Artisans are left alone (step 4)
    public bool Peaceful(AHMob m) { return player != null && player.PeaceOn && !m.provoked; }

    [HideInInspector] public AHGrass grass;
    [System.NonSerialized] public AHQuestLog quests = new AHQuestLog();

    // something solid the hero and the beasts walk around
    // townsfolk are solid: the hero walks around them, not through them (they move, so this is checked live)
    public Vector3 PushFromPeople(Vector3 p, float r)
    {
        foreach (var n in AHNpc.All)
        {
            if (n == null) continue;
            Vector3 np = n.transform.position; float dx = p.x - np.x, dz = p.z - np.z, rr = 0.35f + r, d2 = dx * dx + dz * dz;
            if (d2 < rr * rr && d2 > 1e-6f) { float d = Mathf.Sqrt(d2); p.x = np.x + dx / d * rr; p.z = np.z + dz / d * rr; }
        }
        return p;
    }

    public void AddBlocker(Vector3 p, float r) { cPos.Add(new Vector3(p.x, 0f, p.z)); cRad.Add(r); }
    // a solid box on the ground (a market stall, a booth), from its world bounds, shrunk a little so you can still reach the counter
    public void AddBlockBox(Bounds b, float shrink = 0.25f)
    {
        float w = Mathf.Max(0.2f, b.size.x - shrink * 2f), d = Mathf.Max(0.2f, b.size.z - shrink * 2f);
        rects.Add(new Rect(b.center.x - w / 2f, b.center.z - d / 2f, w, d));
    }

    // a beast fell to the hero (web killMob -> questEvent('kill', type))
    public void OnKill(string mob) { quests.Event("kill", mob, 1, this); if (player != null) player.OnTrialKill(mob); }

    // web inTown: inside a town's walls (world.json TOWNS, in web units). Safe: no hunger, faster healing, no hunting beasts.
    Rect[] towns;
    public bool InTown(Vector3 p)
    {
        if (towns == null)
        {
            var l = new List<Rect>();
            var ts = AHDB.List("world", "TOWNS");
            if (ts != null) foreach (var t in ts) l.Add(new Rect((float)AHJson.N(t, "x"), (float)AHJson.N(t, "y"), (float)AHJson.N(t, "w"), (float)AHJson.N(t, "h")));
            towns = l.ToArray();
        }
        Vector2 w = ToWeb(p) / AHDB.S;
        foreach (var r in towns) if (w.x > r.xMin && w.x < r.xMax && w.y > r.yMin && w.y < r.yMax) return true;
        return false;
    }

    // web interact on a villager or Captain Mara: their line in the chat, the quest 'talk' event, then their window
    public void TalkTo(AHNpc n)
    {
        if (n == null || player == null || player.dead) return;
        string line = n.Talk();
        if (n.sail != null)
        {
            // web sail(dest): over the water to another land
            var sd = AHJson.O(AHJson.O(AHDB.Rules, "SAIL"), n.sail);
            if (line != null) ui.Toast(line, 3f);
            Travel(AHJson.S(sd, "area"), (float)AHJson.N(sd, "x") * AHDB.S, (float)AHJson.N(sd, "y") * AHDB.S, 0f);
            return;
        }
        if (AHFriends.Is(n.npcName)) AHFriends.Talk(this, n.npcName);
        if (n.isCaptain)
        {
            quests.Event("talk", n.npcName, 1, this);
            ui.OpenQuest();
            return;
        }
        string note = n.guild ? null
            : n.barber ? null
            : n.auction ? null : n.market ? null
            : null;
        bool giver = quests.Current != null && quests.NpcName == n.npcName;
        if (n.market && !giver) { if (line != null) ui.Toast(line, 3f); quests.Event("talk", n.npcName, 1, this); ui.OpenWarden(AHRep.At(this, n.transform.position) ?? "ashen"); return; }
        if (n.kq && !giver) { if (line != null) ui.Toast(line, 3f); quests.Event("talk", n.npcName, 1, this); ui.OpenKQ(); return; }
        if (n.barber && !giver) { if (line != null) ui.Toast(line, 3f); quests.Event("talk", n.npcName, 1, this); ui.OpenCreator("barber"); return; }
        if (n.agent && !giver) { if (line != null) ui.Toast(line, 3f); quests.Event("talk", n.npcName, 1, this); ui.OpenFarmAgent(); return; }
        if (n.auction && !giver) { if (line != null) ui.Toast(line, 3f); quests.Event("talk", n.npcName, 1, this); ui.OpenAH(); return; }
        if (!giver && !n.guild && AHFriends.Is(n.npcName)) { quests.Event("talk", n.npcName, 1, this); ui.OpenFriend(n.npcName, line); return; }
        if (line != null && !giver) ui.Toast(line + (note != null ? "\n" + note : ""), 5f);
        quests.Event("talk", n.npcName, 1, this);
        if (n.guild) quests.RoadEvent("talkguild", null, 1, this);
        if (giver) ui.OpenQuest();
        else if (n.guild) ui.OpenProf();
    }

    // choose or change class (from the class picker)
    public void ChooseClass(string id)
    {
        bool first = AHPrefs.GetString("ah_class", "") == "";
        AHPrefs.SetString("ah_class", id);
        player.SetClass(id);
        ui.RefreshClass();
        SaveProgress();
        if (first) ui.Banner(data.region, AreaSub());
        else ui.Banner(player.cls.name, player.cls.role);
    }

    public void SaveProgress()
    {
        if (player == null) return;
        AHSave.Save(this);
        dirty = false;
    }

    // ---------- areas and travel (web passCheck, enterConn, leaveConn) ----------
    public static string AreaId = "meadow";
    public static string AreaPath = "AH/Areas/";
    static float carryDayT = -1f;
    static bool arriving;
    static float carryYaw;
    float travelLock;
    bool leaving;

    // the banner's second line: levels and what lives here (web REGIONS blurb, or a road's 'a long road on foot')
    // the lands you have walked in (the world map shows the rest in fog); a first visit pays a little
    public static bool Seen(string id) { return ("," + AHPrefs.GetString("ah_seen", "") + ",").Contains("," + id + ","); }
    public static int SeenCount { get { string s = AHPrefs.GetString("ah_seen", ""); return s == "" ? 0 : s.Split(',').Length; } }
    void Discover()
    {
        if (Seen(AreaId) || AHDungeon.IsDungeon(AreaId)) return;
        string s = AHPrefs.GetString("ah_seen", ""); AHPrefs.SetString("ah_seen", s == "" ? AreaId : s + "," + AreaId); AHPrefs.Save();
        if (SeenCount <= 1) return;   // where you start does not count
        int xp = 40 + player.level * 12; long silver = (300 + player.level * 40) * AHDB.CU;
        player.GainXp(xp); player.bag.money += silver; player.bag.Touch(); MarkDirty();
        ui.Toast("New land discovered: " + data.region + " · +" + xp + " XP, " + AHItems.MoneyText(silver), 3.5f);
    }

    // the lands whose levels start at this level (for a hint when you level up)
    public static string LandsOpeningAt(int lv)
    {
        var names = new List<string>();
        foreach (var list in new[] { AHDB.List("world", "REGIONS"), AHDB.List("world", "CONNS") })
        {
            if (list == null) continue;
            foreach (var o in list)
            {
                if (AHJson.B(o, "dung")) continue;
                string l = AHJson.S(o, "lv", ""); int a; if (l == "" || !int.TryParse(l.Replace("–", "-").Split('-')[0], out a)) continue;
                string n = AHJson.S(o, "name", ""); if (a == lv && n != "" && !names.Contains(n)) names.Add(n);
            }
        }
        return names.Count == 0 ? null : string.Join(", ", names.ToArray());
    }

    // arriving somewhere far above your level: a plain warning (the web game let you walk in and die)
    void DangerCheck()
    {
        string lv = "";
        var r = AHDB.List("world", "REGIONS"); if (r != null) foreach (var o in r) if (AHJson.S(o, "id") == AreaId) lv = AHJson.S(o, "lv", "");
        var c = AHDB.List("world", "CONNS"); if (lv == "" && c != null) foreach (var o in c) if (AHJson.S(o, "id") == AreaId) lv = AHJson.S(o, "lv", "");
        if (lv == "" || lv == "safe" || lv == "any") return;
        int min; if (!int.TryParse(lv.Replace("–", "-").Split('-')[0], out min)) return;
        if (min > player.level + 3) ui.Toast("Danger: these lands are for level " + min + " and up. You are level " + player.level + ". Stay near the way back.", 4.5f);
    }

    public string AreaSub()
    {
        var r = AHDB.List("world", "REGIONS");
        if (r != null) foreach (var o in r) if (AHJson.S(o, "id") == AreaId) { string lv = AHJson.S(o, "lv", data.levels); return (lv == "safe" ? "Safe city" : "Level " + lv) + " · " + AHJson.S(o, "blurb", ""); }
        var c = AHDB.List("world", "CONNS");
        if (c != null) foreach (var o in c) if (AHJson.S(o, "id") == AreaId) return "Level " + AHJson.S(o, "lv", data.levels) + " · a long road on foot";
        return string.IsNullOrEmpty(data.levels) ? "" : "Level " + data.levels;
    }

    void CheckExits()
    {
        if (leaving || data.exits == null || player == null || player.dead || Time.time < travelLock) return;
        if (ui != null && ui.Modal != 0) return;
        Vector2 w = ToWeb(player.transform.position);
        foreach (var e in data.exits)
        {
            bool hit = e.r > 0f ? (new Vector2(w.x - e.x, w.y - e.z)).magnitude < e.r
                                : w.x >= e.x0 && w.x <= e.x1 && w.y >= e.z0 && w.y <= e.z1;
            if (hit) { Travel(e.to, e.tx, e.tz, e.f); return; }
        }
    }

    // go to another area: the hero is placed where they arrive, everything is saved, and the scene loads the new area
    public void Travel(string to, float tx, float tz, float face)
    {
        if (leaving) return;
        if (Resources.Load<TextAsset>("AH/Areas/" + to) == null) { ui.Toast("The road ahead is not open yet."); travelLock = Time.time + 3f; return; }
        leaving = true;
        AHSound.Play("door");
        player.transform.position = W(tx, tz);
        player.transform.rotation = Face(W(Mathf.Cos(face), Mathf.Sin(face)) - W(0f, 0f));
        carryYaw = player.transform.eulerAngles.y;
        AHGather.Cancel();
        AHPrefs.SetString("ah_area", to);
        AreaId = to;
        SaveProgress();
        carryDayT = dayT;
        arriving = true;
        if (ui != null) ui.Fade(true);
        StartCoroutine(LoadNext());
    }

    System.Collections.IEnumerator LoadNext()
    {
        yield return null;   // one frame for the fade to show
        var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
#if UNITY_EDITOR
        if (sc.buildIndex < 0) { UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(sc.path, new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single)); yield break; }
#endif
        if (sc.buildIndex >= 0) UnityEngine.SceneManagement.SceneManager.LoadScene(sc.buildIndex);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(sc.name);
    }

    // something worth keeping changed: save a moment later (several changes at once save once)
    bool dirty;
    float dirtyT;
    float autoT = 15f, ahT = 2f;
    // inside the map's edges
    public bool InArea(Vector3 p) { return p.x > area.xMin && p.x < area.xMax && p.z > area.yMin && p.z < area.yMax; }
    public void MarkDirty() { if (!dirty) { dirty = true; dirtyT = 1f; } }
    void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
    void OnApplicationQuit() { SaveProgress(); }

    // Unity position back to web metres (x, z)
    public Vector2 ToWeb(Vector3 p)
    {
        Vector3 o = p - (world != null ? world.position : Vector3.zero);
        return new Vector2(Vector3.Dot(o, ax) / ax.sqrMagnitude, Vector3.Dot(o, az) / az.sqrMagnitude);
    }

    // is this spot inside a tree, wall or lake?
    public bool Blocked(Vector3 p, float r)
    {
        for (int i = 0; i < cPos.Count; i++)
        {
            float dx = p.x - cPos[i].x, dz = p.z - cPos[i].z, rr = cRad[i] * 0.7f + r;
            if (dx * dx + dz * dz < rr * rr) return true;
        }
        foreach (var b in rects) if (p.x > b.xMin - r && p.x < b.xMax + r && p.z > b.yMin - r && p.z < b.yMax + r) return true;
        foreach (var b in doors) if (p.x > b.xMin - r && p.x < b.xMax + r && p.z > b.yMin - r && p.z < b.yMax + r) return true;
        if (!TilesOk(p, r)) return true;
        if (floors.Count > 0) { bool on = false; foreach (var b in floors) if (p.x > b.xMin + r && p.x < b.xMax - r && p.z > b.yMin + r && p.z < b.yMax - r) { on = true; break; } if (!on) return true; }
        foreach (var l in lakes)
        {
            float u = (p.x - l.x) / (l.z + r), v = (p.z - l.y) / (l.w + r);
            if (u * u + v * v < 1f) return true;
        }
        return false;
    }

    // ---------- zone tiles (web tileAt / zoneWalk) ----------
    void LoadTiles()
    {
        tiles = null;
        var zs = AHDB.List("zones", "ALLZ");
        if (zs == null) return;
        foreach (var z in zs)
        {
            if (AHJson.S(z, "id") != AreaId) continue;
            string b64 = AHJson.S(AHJson.O(z, "t"), "_bytes");
            if (string.IsNullOrEmpty(b64)) return;
            tiles = System.Convert.FromBase64String(b64);
            tCols = (int)AHJson.N(z, "cols"); tRows = (int)AHJson.N(z, "rows");
            tX0 = (float)AHJson.N(z, "x") * AHDB.S; tZ0 = (float)AHJson.N(z, "y") * AHDB.S;
            if (tiles.Length < tCols * tRows) tiles = null;
            return;
        }
    }
    bool TileWalk(int c, int r) { if (c < 0 || r < 0 || c >= tCols || r >= tRows) return false; int k = tiles[r * tCols + c]; return k < WalkT.Length && WalkT[k]; }
    bool TileWalkAt(float wx, float wz) { return TileWalk(Mathf.FloorToInt((wx - tX0) / Tile), Mathf.FloorToInt((wz - tZ0) / Tile)); }
    // web zoneWalk: the spot and four points q around it all on walkable tiles
    bool TilesOk(Vector3 p, float r)
    {
        if (tiles == null) return true;
        Vector2 w = ToWeb(p); float q = Mathf.Min(r * 0.7f, 1.8f), k = Mathf.Max(q, RockPad);
        if (!(TileWalkAt(w.x, w.y) && TileWalkAt(w.x - q, w.y - q) && TileWalkAt(w.x + q, w.y - q) && TileWalkAt(w.x - q, w.y + q) && TileWalkAt(w.x + q, w.y + q))) return false;
        return !(Rock(w.x - k, w.y - k) || Rock(w.x + k, w.y - k) || Rock(w.x - k, w.y + k) || Rock(w.x + k, w.y + k));
    }
    // push out of the blocked tiles around (each one a square, grown by q)
    Vector3 TilesPush(Vector3 p, float r)
    {
        if (tiles == null) return p;
        Vector2 w = ToWeb(p); float q = Mathf.Min(r * 0.7f, 1.8f);
        for (int pass = 0; pass < 2; pass++)
        {
            int c0 = Mathf.FloorToInt((w.x - tX0) / Tile), r0 = Mathf.FloorToInt((w.y - tZ0) / Tile);
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int c = c0 + dc, rr = r0 + dr;
                    if (TileWalk(c, rr)) continue;
                    // the rock's shapes reach past their tiles: keep a little further from them
                    float e = c < 0 || rr < 0 || c >= tCols || rr >= tRows || tiles[rr * tCols + c] == 5 ? Mathf.Max(q, RockPad) : q;
                    float x0 = tX0 + c * Tile - e, x1 = tX0 + (c + 1) * Tile + e, z0 = tZ0 + rr * Tile - e, z1 = tZ0 + (rr + 1) * Tile + e;
                    if (w.x <= x0 || w.x >= x1 || w.y <= z0 || w.y >= z1) continue;
                    // out the side that opens onto a walkable tile, the nearest one
                    float best = float.MaxValue; Vector2 to = w;
                    if (TileWalk(c - 1, rr) && w.x - x0 < best) { best = w.x - x0; to = new Vector2(x0, w.y); }
                    if (TileWalk(c + 1, rr) && x1 - w.x < best) { best = x1 - w.x; to = new Vector2(x1, w.y); }
                    if (TileWalk(c, rr - 1) && w.y - z0 < best) { best = w.y - z0; to = new Vector2(w.x, z0); }
                    if (TileWalk(c, rr + 1) && z1 - w.y < best) { best = z1 - w.y; to = new Vector2(w.x, z1); }
                    if (best == float.MaxValue)
                    {
                        float l = w.x - x0, rg = x1 - w.x, d = w.y - z0, u = z1 - w.y, m = Mathf.Min(Mathf.Min(l, rg), Mathf.Min(d, u));
                        to = m == l ? new Vector2(x0, w.y) : m == rg ? new Vector2(x1, w.y) : m == d ? new Vector2(w.x, z0) : new Vector2(w.x, z1);
                    }
                    w = to;
                }
        }
        Vector3 o = W(w.x, w.y); o.y = p.y;
        return o;
    }

    // a dungeon's darkness: no sky, a dim cave light, fog the colour of the rock (web setZone(true))
    bool dark; Color darkCol;
    public bool Dark { get { return dark; } }
    public void SetDark(Color around)
    {
        dark = true; darkCol = around;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = around * 0.6f; }
        // the hero carries a lantern underground, so what comes at you out of the dark can be seen
        if (player != null && player.transform.Find("Lantern") == null)
        {
            var lg = new GameObject("Lantern"); lg.transform.SetParent(player.transform, false); lg.transform.localPosition = new Vector3(0f, 2.6f, 0.4f);
            var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.82f, 0.6f); li.range = 13f; li.intensity = 2.2f; li.shadows = LightShadows.None;
        }
        UpdateDay();
    }

    // how much wider each area is laid out than the web game's map (1 = as drawn): sizes from about 100 to 180 metres
    public static float Spread = 1f;
    static readonly Dictionary<string, float> spreadK = new Dictionary<string, float> {
        { "meadow", 1.5f }, { "silkwood", 1.3f }, { "frost", 1.2f }, { "mire", 1.1f }, { "vale", 1.15f }, { "sands", 1.3f }, { "isle", 1.4f } };
    public static bool NoSpread;   // editor test: the areas as the web game drew them
    public static float SpreadOf(string id) { float k; return !NoSpread && spreadK.TryGetValue(id, out k) ? k : 1f; }
    // move every piece of the area's model out from the middle by k (pieces keep their size); ground, water and the
    // batched border peaks stretch with it
    void SpreadWorld(float k)
    {
        Vector3 wp = world.position; int moved = 0;
        System.Action<Transform> visit = null;
        visit = c =>
        {
            string n = c.name;
            if (n.StartsWith("AH_MARK")) return;
            if (n.StartsWith("AH_GROUND") || n.StartsWith("AH_WATER") || n.StartsWith("AH_INST"))
            {
                // stretch along the world's own axes (the ground is drawn lying in its node's x-y plane, turned flat)
                var hold = new GameObject(n + " (spread)").transform; hold.SetParent(world, false);
                hold.position = wp; hold.rotation = Quaternion.identity; hold.localScale = Vector3.one;
                c.SetParent(hold, true); hold.localScale = new Vector3(k, 1f, k);
                var gr = c.GetComponent<Renderer>(); Debug.Log("Ashen Hollow: stretched " + n + (gr != null ? " to " + gr.bounds.size.ToString("0") : "") + " parent " + (c.parent != null ? c.parent.name : "-") + " static " + c.gameObject.isStatic);
                return;
            }
            var rs = c.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (c.childCount > 0 && (Mathf.Max(b.size.x, b.size.z) > 40f || c == world))
            {
                // a group of many things (or the whole area): spread its parts one by one
                var kids = new List<Transform>(); foreach (Transform cc in c) kids.Add(cc);
                foreach (var cc in kids) visit(cc);
                return;
            }
            Vector3 d = b.center - wp; d.y = 0f; c.position += d * (k - 1f); moved++;
        };
        visit(world);
        Debug.Log("Ashen Hollow: " + AreaId + " laid out " + k.ToString("0.00") + "x wider (" + moved + " pieces moved)");
    }

    public Rect MapRect(float x, float z, float w, float h)
    {
        Vector3 a = W(x, z), b = W(x + w, z + h);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), name); if (r != null) return r; }
        return null;
    }

    public static Material LoadMat(string res, string shader)
    {
        Material m = Resources.Load<Material>(res);
        if (m != null) return new Material(m);
        Shader s = Shader.Find(shader);
        if (s == null) { Debug.LogWarning("Ashen Hollow: shader " + shader + " not found. Run the menu Ashen Hollow > Set Up Meadow Scene."); return null; }
        return new Material(s);
    }

    void SetupWorldRenderers()
    {
        waterMat = LoadMat("AH/Materials/Water", "AshenHollow/Water");
        var groundMat = LoadMat("AH/Materials/Ground", "AshenHollow/Ground");
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name;
            if (n.StartsWith("AH_WATER"))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (waterMat != null)
                {
                    var m = new Material(waterMat);
                    bool bog = n.Contains("_K2");
                    m.SetColor("_Color", bog ? new Color32(0x3a, 0x4a, 0x2a, 255) : new Color32(0x3c, 0x7e, 0xa0, 255));
                    m.SetFloat("_Foam", bog ? 0.25f : 1f);
                    m.SetFloat("_Mode", n.Contains("_M1") ? 1f : 0f);
                    m.SetFloat("_Refract", 1f);
                    r.sharedMaterial = m;
                }
            }
            else if (n.StartsWith("AH_GROUND"))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = true;
                // the painted ground map, with detail and real lighting
                if (groundMat != null && r.sharedMaterial != null)
                {
                    var old = r.sharedMaterial;
                    Texture tex = old.HasProperty("baseColorTexture") ? old.GetTexture("baseColorTexture") : old.mainTexture;
                    if (tex != null)
                    {
                        var m = new Material(groundMat);
                        m.SetTexture("_BaseMap", tex);
                        AHCity.Pave(this, m);   // cobbled streets inside city walls
                        r.sharedMaterial = m;
                    }
                }
            }
            else r.shadowCastingMode = ShadowCastingMode.On;
        }
        // Coralport stands on a flat harbour floor, but its sea was one big plane a hair above the streets, so the whole
        // town stood in water: drop that sea just under the town's floor (the islands elsewhere are their own raised
        // pieces and keep the sea as it is)
        // Dragonscale Isle is the same: the island is the painted floor and the sea lay over all of it
        if (AreaId == "co_city" || AreaId == "isle")
        {
            var rs = world.GetComponentsInChildren<Renderer>(true); float floor = float.NaN;
            foreach (var gr in rs) if (gr.gameObject.name.StartsWith("AH_GROUND")) floor = gr.bounds.min.y;
            if (!float.IsNaN(floor))
                foreach (var wr in rs)
                    if (wr.gameObject.name.StartsWith("AH_WATER") && wr.bounds.size.x > 150f) wr.transform.position += Vector3.up * (floor - 0.12f - wr.bounds.center.y);
        }
    }

    void SetupLightAndSky()
    {
        Light existing = RenderSettings.sun;
        if (existing == null) { var any = FindAnyObjectByType<Light>(); if (any != null && any.type == LightType.Directional) existing = any; }
        if (existing == null)
        {
            var go = new GameObject("Sun");
            existing = go.AddComponent<Light>();
            existing.type = LightType.Directional;
        }
        sun = existing;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 70f;   // the big meadows read clear across; only the far hills soften
        RenderSettings.fogEndDistance = 190f;   // just inside the camera's reach, so nothing pops
        skyMat = LoadMat("AH/Materials/Sky", "AshenHollow/Sky");
        if (skyMat != null) RenderSettings.skybox = skyMat;
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.fieldOfView = 58f;
        cam.nearClipPlane = 0.1f;
        // the world draws to 195 m (inside the fog); only the great mountain ranges (AHRange, layer 30) reach 420 m
        cam.farClipPlane = 420f;
        { var lc = new float[32]; for (int i = 0; i < 32; i++) lc[i] = 195f; lc[AHRange.Layer] = 420f; cam.layerCullDistances = lc; }
        cam.clearFlags = CameraClearFlags.Skybox;
        // the water shows the ground through it: keep the scene's colour and depth for it
        var cd = cam.GetComponent<UniversalAdditionalCameraData>();
        if (cd == null) cd = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        cd.requiresColorOption = CameraOverrideOption.On;
        cd.requiresDepthOption = CameraOverrideOption.On;
        cd.renderPostProcessing = true; cd.stopNaN = true;   // a stray NaN pixel (a broken mesh or shader) becomes black instead of blooming into a white flash
        Shader.SetGlobalVectorArray("_AH_Ripples", ripples);
    }

    // ---------- water: ripples, and wading in the shallow rim of a pond ----------
    readonly Vector4[] ripples = new Vector4[8];
    int nextRipple;
    public void Ripple(Vector3 at, float strength = 1f)
    {
        ripples[nextRipple] = new Vector4(at.x, at.z, Time.time, strength);
        nextRipple = (nextRipple + 1) % ripples.Length;
        Shader.SetGlobalVectorArray("_AH_Ripples", ripples);
    }
    public const float Wade = 0.3f;   // the outer 30% of a pond is shallow enough to walk in

    // how deep the hero stands: 0 on land, 1 at the deepest they can wade
    public float WaterDepth(Vector3 p)
    {
        float best = 0f;
        foreach (var l in lakes)
        {
            float u = (p.x - l.x) / l.z, v = (p.z - l.y) / l.w, d = Mathf.Sqrt(u * u + v * v);
            if (d < 1f) best = Mathf.Max(best, Mathf.Clamp01((1f - d) / Wade));
        }
        return best;
    }

    // ---------- collisions: keep something of radius r out of trees, walls and lakes, and inside the meadow ----------
    public Vector3 Resolve(Vector3 p, float r, float wade = 0f)
    {
        for (int i = 0; i < cPos.Count; i++)
        {
            float dx = p.x - cPos[i].x, dz = p.z - cPos[i].z, rr = cRad[i] + r, d2 = dx * dx + dz * dz;
            if (d2 < rr * rr && d2 > 1e-6f) { float d = Mathf.Sqrt(d2); p.x = cPos[i].x + dx / d * rr; p.z = cPos[i].z + dz / d * rr; }
        }
        foreach (var b in rects) p = PushOutOf(b, p, r);

        foreach (var b in doors) p = PushOutOf(b, p, r);
        foreach (var l in lakes)
        {
            float lx = l.z * (1f - wade) + (wade > 0f ? 0f : r), lz = l.w * (1f - wade) + (wade > 0f ? 0f : r);
            float u = (p.x - l.x) / lx, v = (p.z - l.y) / lz, d = Mathf.Sqrt(u * u + v * v);
            if (d < 1f && d > 1e-4f) { p.x = l.x + u / d * lx; p.z = l.y + v / d * lz; }
        }
        p = TilesPush(p, r);
        if (floors.Count > 0)
        {
            // stay on the floor: inside one of the rooms (or back into the nearest)
            bool on = false; Vector3 best = p; float bd = float.MaxValue;
            foreach (var b in floors)
            {
                Vector3 c = new Vector3(Mathf.Clamp(p.x, b.xMin + r, b.xMax - r), p.y, Mathf.Clamp(p.z, b.yMin + r, b.yMax - r));
                float d2 = (c - p).sqrMagnitude;
                if (d2 < 1e-8f) { on = true; break; }
                if (d2 < bd) { bd = d2; best = c; }
            }
            if (!on) p = best;
        }
        p.x = Mathf.Clamp(p.x, area.xMin + r, area.xMax - r);
        p.z = Mathf.Clamp(p.z, area.yMin + r, area.yMax - r);
        p.y = 0f;
        return p;
    }

    static Vector3 PushOutOf(Rect b, Vector3 p, float r)
    {
        {
            if (p.x < b.xMin - r || p.x > b.xMax + r || p.z < b.yMin - r || p.z > b.yMax + r) return p;
            float left = p.x - (b.xMin - r), right = (b.xMax + r) - p.x, down = p.z - (b.yMin - r), up = (b.yMax + r) - p.z;
            float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
            if (m == left) p.x = b.xMin - r; else if (m == right) p.x = b.xMax + r; else if (m == down) p.z = b.yMin - r; else p.z = b.yMax + r;
        }
        return p;
    }

    // ---------- every frame ----------
    float qT;
    void Update()
    {
        if (data == null || player == null) return;
        dayT = (dayT + Time.deltaTime) % dayLength;
        UpdateDay();
        AHGather.Tick(this);
        ahT -= Time.unscaledDeltaTime; if (ahT <= 0f) { ahT = 5f; AHAuction.Tick(this); AHOrders.Tick(this); AHHome.Tick(this); AHMarket.Tick(this); }
        AHHome.RestTick(this, Time.deltaTime);
        AHFinder.Tick(this, Time.deltaTime);
        if (player != null) AHMeal.Tick(this, player, Time.deltaTime);
        AHAch.Tick(this, Time.deltaTime);
        AHEvents.Tick(this, Time.deltaTime);
        AHWays.Tick(this, Time.deltaTime);
        AHKQ.Tick(this, Time.deltaTime);
        AHGoblin.Tick(this, Time.deltaTime);
        AHSecrets.Tick(this, Time.deltaTime);
        AHNight.Tick(this);
        AHWardrobe.Tick(this);
        if (player != null && player.cls != null) AHDaily.Tick(this, Time.deltaTime);
        CheckExits();
        // quest goals that are a state (level, skills, home, friends, deliveries), and the Riven Crater in Kingsvale
        qT -= Time.unscaledDeltaTime;
        if (qT <= 0f)
        {
            qT = 1f; quests.Check(this);
            if (AreaId == "vale") { Vector2 w = ToWeb(player.transform.position); var B = AHDB.Table("monsters", "WORLD_BOSS"); Vector2 c = new Vector2((float)AHJson.N(B, "x", 6336) * AHDB.S, (float)AHJson.N(B, "y", 3472) * AHDB.S); if ((w - c).magnitude < 22f) quests.Event("reach", "rift", 1, this); }
        }
        if (dirty) { dirtyT -= Time.unscaledDeltaTime; if (dirtyT <= 0f) SaveProgress(); }
        // where you stand and your health change all the time: kept every 15 s, like the web game's autosave
        autoT -= Time.unscaledDeltaTime;
        if (autoT <= 0f) { autoT = 15f; SaveProgress(); }
    }

    void LateUpdate()
    {
        if (player == null || cam == null) return;
        // a near-square screen (an unfolded phone, a tablet) gets a taller view so it shows as much side to side
        // as a normal phone instead of looking zoomed in
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.78f;
        float wantFov = Mathf.Max(58f, 2f * Mathf.Atan(Mathf.Tan(37.5f * Mathf.Deg2Rad) / Mathf.Max(0.5f, aspect)) * Mathf.Rad2Deg);
        if (Mathf.Abs(cam.fieldOfView - wantFov) > 0.05f && !(ui != null && ui.CreatorOpen)) cam.fieldOfView = wantFov;
        bool creating = ui != null && ui.CreatorOpen;
        if (ui != null && !creating)
        {
            camYaw += ui.camDelta.x * 0.22f;
            camPitch = Mathf.Clamp(camPitch - ui.camDelta.y * 0.15f, 5f, 65f);
            camDistance = Mathf.Clamp(camDistance * ui.zoom, 3.5f, 14f);
        }
        if (cineT > 0f) { cineT -= Time.unscaledDeltaTime; camYaw += Time.unscaledDeltaTime * 150f; }   // the level-up swing
        camTarget = Vector3.Lerp(camTarget, player.transform.position, 1f - Mathf.Exp(-Time.deltaTime * 10f));
        // making your hero: a close shot with the hero on the left, facing you, clear of the creator panel
        // drag the hero to turn them round; pinch (or the mouse wheel) to zoom in on the face
        if (creating)
        {
            if (!wasCreating) { crSpin = 0f; crDist = 4.2f; }
            crSpin -= ui.camDelta.x * 0.45f;
            crDist = Mathf.Clamp(crDist * ui.zoom, 1.1f, 4.6f);
            float near = Mathf.InverseLerp(4.2f, 1.1f, crDist);
            float tall = player.transform.lossyScale.y;
            Quaternion cq = Quaternion.Euler(Mathf.Lerp(8f, 3f, near), camYaw, 0);
            Vector3 right = cq * Vector3.right;
            Vector3 head = player.transform.position + Vector3.up * Mathf.Lerp(1.05f, 1.5f, near) * Mathf.Max(0.6f, tall);
            cam.transform.position = head - cq * Vector3.forward * crDist + right * 1.25f * (crDist / 4.2f);
            cam.transform.rotation = cq;
            Vector3 toCam = cam.transform.position - player.transform.position; toCam.y = 0f;
            player.transform.rotation = Face(Quaternion.Euler(0f, crSpin, 0f) * toCam);
            wasCreating = true;
            return;
        }
        wasCreating = false;
        Vector3 look = camTarget + Vector3.up * (1.55f + (player.mounted ? 0.65f : 0f));
        // in caves and among cliffs the camera rises over the rock rather than diving into it
        // a big beast close by (a boss, a golem, a dragon): step back and up so it all fits in the view
        float dist = camDistance, minPitch = camPitch;
        foreach (var m in mobs)
        {
            if (m.dead || m.height < 3f) continue;
            float near = (m.transform.position - camTarget).magnitude;
            if (near > m.height * 2.5f + 4f) continue;
            dist = Mathf.Max(dist, Mathf.Min(20f, m.height * 1.7f));
            minPitch = Mathf.Max(minPitch, 34f);
        }
        distShown = distShown <= 0f ? dist : Mathf.Lerp(distShown, dist, 1f - Mathf.Exp(-Time.deltaTime * 2.5f));
        if (occArea != AreaId) { occArea = AreaId; occAge = 0f; occ.Clear(); }
        float oa = occAge; occAge += Time.deltaTime;
        if ((oa < 1.5f && occAge >= 1.5f) || (oa < 6f && occAge >= 6f)) BuildOccluders();
        float wantPitch = minPitch;
        for (float pt = minPitch; pt <= 74f; pt += 6f)
        {
            wantPitch = pt;
            Vector3 bk = -(Quaternion.Euler(pt, camYaw, 0) * Vector3.forward);
            float cl = Mathf.Min(AHMountains.Clear(look, bk, distShown), OccClear(look, bk, distShown));
            if (tiles != null) cl = Mathf.Min(cl, RockClear(look, bk, distShown));
            if (cl >= distShown * 0.9f) break;
        }
        pitchShown = pitchShown <= 0f ? wantPitch : Mathf.Lerp(pitchShown, wantPitch, 1f - Mathf.Exp(-Time.deltaTime * 4f));
        Quaternion q = Quaternion.Euler(pitchShown, camYaw, 0);
        Vector3 back = -(q * Vector3.forward);
        // come in quickly when a tree crown is in the way, ease back out once it is clear
        float clear = ClearDistance(look, back, distShown);
        if (camShown <= 0f || clear < camShown) camShown = clear;
        else camShown = Mathf.MoveTowards(camShown, clear, Time.deltaTime * 4f);
        cam.transform.position = look + back * camShown;
        if (cam.transform.position.y < 0.6f) { var p = cam.transform.position; p.y = 0.6f; cam.transform.position = p; }
        cam.transform.LookAt(look);
    }

    float camShown, pitchShown, distShown;

    bool Rock(float wx, float wz)
    {
        int c = Mathf.FloorToInt((wx - tX0) / Tile), r = Mathf.FloorToInt((wz - tZ0) / Tile);
        return c < 0 || r < 0 || c >= tCols || r >= tRows || tiles[r * tCols + c] == 5;
    }

    // how far back along 'back' before a rock tile (web zone tiles), up to 'want'
    // big solid things (cliffs, boulders, buildings, waystones) as boxes, gathered a moment after a land loads
    readonly List<Bounds> occ = new List<Bounds>();
    string occArea; float occAge;
    void BuildOccluders()
    {
        occ.Clear();
        foreach (var top in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (var r in top.GetComponentsInChildren<MeshRenderer>(false))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            var b = r.bounds; float lo = Mathf.Min(b.size.x, b.size.z), hi = Mathf.Max(b.size.x, b.size.z);
            if (b.size.y < 2.2f || lo < 1.0f || hi > 45f) continue;
            var root = r.transform.root; string rn = root.name;
            if (rn == "Forest" || rn.StartsWith("Grass") || root == transform) continue;
            if (r.GetComponentInParent<AHMob>() != null || r.GetComponentInParent<AHNpc>() != null || r.GetComponentInParent<AHPlayer>() != null) continue;
            b.extents = new Vector3(b.extents.x * 0.85f, b.extents.y, b.extents.z * 0.85f);   // rocks are rounder than their box
            occ.Add(b);
        }
    }
    float OccClear(Vector3 look, Vector3 back, float want)
    {
        float best = want; var ray = new Ray(look, back);
        foreach (var b in occ)
        {
            Vector3 d = b.center - look; float r = b.extents.magnitude + want;
            if (d.sqrMagnitude > r * r || b.Contains(look)) continue;
            float hit; if (b.IntersectRay(ray, out hit) && hit < best) best = Mathf.Max(0.8f, hit - 0.35f);
        }
        return best;
    }

    float RockClear(Vector3 look, Vector3 back, float want)
    {
        for (float t = 0.6f; t < want; t += 0.35f)
        {
            Vector3 q = look + back * t;
            if (q.y > 7f) break;
            Vector2 w = ToWeb(q);
            // the rock's shapes reach a little past their tiles
            if (Rock(w.x, w.y) || Rock(w.x + 1f, w.y) || Rock(w.x - 1f, w.y) || Rock(w.x, w.y + 1f) || Rock(w.x, w.y - 1f)) return t - 0.45f;
        }
        return want;
    }
    const float CrownScale = 3f, CrownLow = 1.3f, CrownHigh = 5.5f, CamMin = 2.2f;

    // how far the camera can sit behind the hero before a tree crown hides the view
    float ClearDistance(Vector3 look, Vector3 back, float want)
    {
        float best = want;
        // a zone's rock (cave walls, cliffs): come in front of it (web camOccl)
        if (tiles != null) best = Mathf.Min(best, RockClear(look, back, best));
        best = Mathf.Min(best, AHMountains.Clear(look, back, best));   // and out of the mountains
        best = Mathf.Min(best, OccClear(look, back, best));            // and in front of cliffs, boulders and walls
        float a = back.x * back.x + back.z * back.z;
        if (a < 1e-4f) return best;
        for (int i = 0; i < cPos.Count; i++)
        {
            float R = cRad[i] / 0.8f * CrownScale;
            float ox = look.x - cPos[i].x, oz = look.z - cPos[i].z;
            if (ox * ox + oz * oz > (want + R) * (want + R)) continue;
            float b = 2f * (ox * back.x + oz * back.z), c = ox * ox + oz * oz - R * R;
            if (c < 0f) continue;   // the hero stands under this tree: leave the view alone
            float disc = b * b - 4f * a * c;
            if (disc < 0f) continue;
            float t = (-b - Mathf.Sqrt(disc)) / (2f * a);
            if (t <= 0f || t >= best) continue;
            float y = look.y + back.y * t;
            if (y < CrownLow || y > CrownHigh) continue;
            best = t - 0.4f;
        }
        return Mathf.Max(CamMin, best);
    }

    // the camera's flat forward and right, for joystick steering
    public Vector3 CamForward() { Vector3 f = cam.transform.forward; f.y = 0; return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward; }
    public Vector3 CamRight() { Vector3 r = cam.transform.right; r.y = 0; return r.sqrMagnitude > 1e-6f ? r.normalized : Vector3.right; }

    static readonly Color NightH = Hex(0x141a2c), DuskH = Hex(0xa06a52), DayH = Hex(0x93a2b0);
    static readonly Color NightZ = Hex(0x060914), DuskZ = Hex(0x4a3f6e), DayZ = Hex(0x3f74b8);
    static readonly Color SunC = Hex(0xffd9a8), MoonC = Hex(0x8fa8ff);
    public static Color Hex(int h) { return new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f, 1f); }

    public float DayLight { get; private set; }
    public bool IsNight { get { return DayLight < 0.2f; } }

    public void SetTimeOfDay(float frac) { dayT = Mathf.Repeat(frac, 1f) * dayLength; UpdateDay(); }
    void UpdateDay()
    {
        float p = dayT / dayLength, a = p * Mathf.PI * 2f, sn = Mathf.Sin(a);
        DayLight = Mathf.Clamp01(sn * 2.2f + 0.35f);
        float dl = DayLight;
        Color hor = dl < 0.5f ? Color.Lerp(NightH, DuskH, dl * 2f) : Color.Lerp(DuskH, DayH, (dl - 0.5f) * 2f);
        Color zen = dl < 0.5f ? Color.Lerp(NightZ, DuskZ, dl * 2f) : Color.Lerp(DuskZ, DayZ, (dl - 0.5f) * 2f);
        RenderSettings.fogColor = hor;
        RenderSettings.ambientSkyColor = Color.Lerp(hor, zen, 0.5f) * (0.55f + 0.5f * dl);
        RenderSettings.ambientEquatorColor = hor * (0.5f + 0.4f * dl);
        RenderSettings.ambientGroundColor = Hex(0x3e3a2c) * (0.4f + 0.5f * dl);

        // the light: the sun by day, the moon by night, never so low that shadows stretch forever
        bool up = sn >= 0f;
        float lx = up ? Mathf.Cos(a) : -Mathf.Cos(a), ly = Mathf.Max(0.35f, Mathf.Abs(sn));
        Vector3 toLight = new Vector3(-lx, ly, 0.35f).normalized;
        sun.transform.rotation = Quaternion.LookRotation(-toLight);
        sun.intensity = 0.25f + 0.95f * dl;
        sun.color = Color.Lerp(MoonC, SunC, dl);

        Vector3 sunDir = new Vector3(-Mathf.Cos(a), Mathf.Sin(a), 0.45f).normalized;
        Shader.SetGlobalColor("_AH_Sky", Color.Lerp(hor, zen, 0.4f));
        Shader.SetGlobalColor("_AH_SkyTop", zen);
        Shader.SetGlobalFloat("_AH_Now", Time.time);
        Shader.SetGlobalFloat("_AH_Light", Mathf.Clamp01(0.3f + 0.75f * dl));
        if (skyMat != null)
        {
            skyMat.SetColor("_HorizonColor", hor);
            skyMat.SetColor("_ZenithColor", zen);
            skyMat.SetVector("_SunDir", sunDir);
            skyMat.SetVector("_MoonDir", -sunDir);
            skyMat.SetColor("_SunColor", SunC * Mathf.Clamp01(sn * 3f + 0.3f));
            Color cloud = dl < 0.5f ? Color.Lerp(Hex(0x1c2233), Hex(0xffb08a), dl * 2f) : Color.Lerp(Hex(0xffb08a), Color.white, (dl - 0.5f) * 2f);
            skyMat.SetColor("_CloudColor", cloud);
            skyMat.SetFloat("_Night", 1f - dl);
        }
        if (dark)
        {
            // underground: the same dim light all day, torches do the rest
            RenderSettings.fogColor = darkCol * 0.6f;
            RenderSettings.fogStartDistance = 10f; RenderSettings.fogEndDistance = 48f;
            RenderSettings.ambientSkyColor = darkCol * 1.6f + new Color(0.13f, 0.12f, 0.12f);
            RenderSettings.ambientEquatorColor = darkCol * 1.3f + new Color(0.1f, 0.09f, 0.09f);
            RenderSettings.ambientGroundColor = darkCol * 0.8f + new Color(0.05f, 0.05f, 0.05f);
            sun.intensity = 0.32f; sun.color = new Color(0.75f, 0.7f, 0.68f);
            DayLight = 0.5f;
        }
    }
}
