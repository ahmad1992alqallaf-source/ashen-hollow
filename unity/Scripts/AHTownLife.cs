// Ashen Hollow: the walled town of Hollow Meadow, alive. Everything here runs on top of the buildings AHHollow puts up:
//  - the town crier walks round the square ringing his hand bell and calling the news (the hour, the weather, you)
//  - keepers stand at the market stalls and sell their wares; trainers wait in the workshops
//  - townsfolk stroll the square by day; at night they go home and the inn fills up, with the bard playing
//  - the watch walks the ring street, two guards stand at every gate, and the portcullis comes down at night
//    (it lifts for you as you come near)
//  - the bell tower rings the hours, its clock keeps the town's time
//  - the stable keeps horses in their stalls and sells mounts
//  - the wishing fountain takes a coin a day; the house plot can be bought and built on; the jobs board has small
//    paid jobs for the townsfolk every day
//  - smoke from the chimneys, birds over the roofs by day, fireflies at night, puddles in the rain
using System.Collections.Generic;
using UnityEngine;

public class AHTownLife : MonoBehaviour
{
    public static AHTownLife I;
    AHGame g;
    AHNpc crier, bard;
    readonly List<AHNpc> dayFolk = new List<AHNpc>(), nightFolk = new List<AHNpc>();
    readonly List<AHAnim> horses = new List<AHAnim>();
    readonly List<Transform> horseT = new List<Transform>();
    readonly List<float> gateY = new List<float>();
    readonly List<int> gateDir = new List<int>();
    float raisedY, nextCry, nextLute, nextCheck, ringAt, swing, nextSnort, plotConfirmT;
    int lute, hourLast = -1, ringsLeft, cryI;
    bool night;
    Quaternion bellBase = Quaternion.identity, clockBase = Quaternion.identity;
    AHSpot plotSpot;
    GameObject house;

    public static void Setup(AHGame g, Transform root)
    {
        if (root == null || g == null) return;
        var tl = root.gameObject.AddComponent<AHTownLife>(); tl.g = g; I = tl;
        try { tl.Build(); } catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: town life could not start fully (" + e.Message + ")\n" + e.StackTrace); }
        try { root.gameObject.AddComponent<AHTownMore>().Init(g); } catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: more town life could not start (" + e.Message + ")"); }
        try { root.gameObject.AddComponent<AHNightFun>().Init(g); } catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: the night's extras could not start (" + e.Message + ")"); }
    }

    static Vector3 C { get { return AHHollow.C; } }
    static Vector3 Ring(float deg, float r) { float a = deg * Mathf.Deg2Rad; Vector3 E = AHGame.I.W(1f, 0f) - AHGame.I.W(0f, 0f), S = AHGame.I.W(0f, 1f) - AHGame.I.W(0f, 0f); E.y = 0; S.y = 0; return C + (E.normalized * Mathf.Cos(a) + S.normalized * Mathf.Sin(a)) * r; }
    AHHollow.Hall Hall(string id) { foreach (var h in AHHollow.Halls) if (h.id == id) return h; return null; }

    void Build()
    {
        night = g.IsNight;
        if (AHHollow.Bell != null) bellBase = AHHollow.Bell.rotation;
        if (AHHollow.ClockHour != null) clockBase = AHHollow.ClockHour.rotation;
        // ---------- 1. the crier, round the square ----------
        {
            var route = new List<Vector3>(); for (int i = 0; i < 12; i++) route.Add(Ring(i * 30f + 15f, 5.6f));
            crier = AHNpc.SpawnAt(g, "Town crier Odo", route[0], route[1] - route[0], new[] { "Hear ye! News at every bell.", "If it happened in Hollow Meadow, I have shouted it." });
            crier.route = route.ToArray(); crier.routeSpeed = 1.0f; crier.SetSeq("Idle_Talking_Loop", "Idle_Loop"); crier.label = "Hear the news";
            crier.onTalk = () => { var n = News(); AHLife.Speak(crier, n[0], n[1], 5f); g.ui.TalkLine(crier, AHLoc.Arabic ? AHLoc.Shape(n[1]) : n[0]); return true; };
            nextCry = Time.time + 8f;
        }
        // ---------- 2. keepers at the market stalls ----------
        {
            var stalls = new List<Transform>();
            foreach (Transform t in AHHollow.Root) if (t.name == "AH_Stall") stalls.Add(t);
            stalls.Sort((a, b) => Flat(a.position - C).CompareTo(Flat(b.position - C)));
            string[,] keepers = { { "Grocer Hilde", "hm_grocer", "Fresh bread! Hot pies!", "خبز طازج! فطائر ساخنة!" }, { "Fishmonger Bram", "hm_fish", "Trout from the river, grilled this morning!", "سمك السلمون من النهر، مشوي هذا الصباح!" },
                                  { "Herbwife Nia", "hm_herbs", "Potions for every ache!", "جرعات لكل وجع!" }, { "Peddler Jory", "hm_peddler", "Bags, picks and axes! Everything you forgot!", "حقائب ومعاول وفؤوس! كل ما نسيته!" } };
            int k = 0;
            foreach (var st in stalls)
            {
                if (k >= 4) break;
                Vector3 back = Flat3(st.position - C).normalized; if (back.sqrMagnitude < 0.5f) continue;
                if (Flat(st.position - C) > 14f) break;   // only the square's own stalls
                Vector3 at = st.position + back * 1.05f; at.y = C.y;
                string shop = keepers[k, 1], en = keepers[k, 2], ar = keepers[k, 3];
                var v = AHNpc.SpawnAt(g, keepers[k, 0], at, -back, new[] { en });
                v.label = "Shop"; v.SetSeq("Idle_Talking_Loop", "Idle_Loop", "Yes");
                v.onTalk = () => { AHLife.Speak(v, en, ar, 3f); g.ui.OpenShop(shop); return true; };
                k++;
            }
        }
        // ---------- 3. the town jobs board, beside the bounty board ----------
        {
            Vector3 at = AHHollow.FreeSpot(290f, 10.5f, 2.6f);
            var bd = AHHollow.JobsBoard(at, C - at);
            var spot = new AHSpot { kind = "use", type = "town_jobs", name = "Town jobs", pos = at + Flat3(C - at).normalized * 1.0f, r = 0.8f, reach = 2.8f };
            spot.use = () => g.ui.OpenJobs();
            AHGather.Spots.Add(spot);
        }
        // ---------- 4. trainers in the workshops ----------
        {
            var tr = new[] {
                new { id = "forge", who = "Smith Brenna", x = -0.08f, z = 0.42f, tip = "Smelt your ore at the furnace, then work the bars on the anvil. Better bars, better gear." },
                new { id = "kitchen", who = "Cook Hobb", x = 0f, z = 0.38f, tip = "Raw fish and meat cook in the pots. Cooked food fills you up and heals you on the road." },
                new { id = "apothecary", who = "Alchemist Wyn", x = 0.24f, z = 0.62f, tip = "Herbs go in the cauldron. Sunpetal for health, mireroot for mana; rarer herbs, stronger brews." },
                new { id = "tannery", who = "Tanner Rolf", x = 0.02f, z = 0.42f, tip = "Skin what you hunt, then work the hides on the loom into leather for armour and bags." },
                new { id = "jeweler", who = "Gemcutter Isolde", x = 0.22f, z = 0.72f, tip = "Gems and gold become rings and amulets at the bench. Cut them carefully, the shine is in the cut." },
            };
            foreach (var t in tr)
            {
                var h = Hall(t.id); if (h == null) continue;
                string tip = t.tip;
                var n = AHNpc.SpawnAt(g, t.who, h.At(h.w * t.x, h.d * t.z), -h.b, new[] { tip });
                n.label = "Train"; n.SetSeq("Idle_Loop", "Idle_FoldArms_Loop");
                n.onTalk = () => { g.ui.Toast(n.npcName + ": “" + tip + "”", 6f); g.ui.OpenProf(); return true; };
            }
        }
        // ---------- 4b. the ranked arena's master, in the Barracks & Arena ----------
        {
            var h = Hall("barracks");
            if (h != null)
            {
                var v = AHNpc.SpawnAt(g, "Arena master Varek", h.At(h.w * 0.3f, 2.4f), -h.b, new[] { "Think you can climb the ranks? Prove it." });
                v.label = "Ranked arena"; v.SetSeq("Idle_FoldArms_Loop", "Idle_Loop");
                v.onTalk = () => { g.ui.OpenRanked(); return true; };
            }
        }
        // ---------- 5. townsfolk by day, the watch round the ring street ----------
        {
            var folk = new[] {
                new { who = "Townsman Pell", deg = 60f, lines = new[] { "Fine day for the market.", "My wife sent me for bread. I came back with a hat." } },
                new { who = "Washerwoman Ulla", deg = 140f, lines = new[] { "The river was cold this morning!", "Mind the puddles, they're mine." } },
                new { who = "Old Gerd", deg = 250f, lines = new[] { "In my day the wall was half this size.", "Hear that bell? Means I'm late for something." } },
                new { who = "Farmhand Kessa", deg = 320f, lines = new[] { "Wheat's coming in well this year.", "The boars got into the turnips again." } },
            };
            foreach (var f in folk)
            {
                Vector3 at = AHHollow.FreeSpot(f.deg, 7.5f, 1.4f);
                var n = AHNpc.SpawnAt(g, f.who, at, C - at, f.lines); n.wander = true;
                dayFolk.Add(n);
            }
            float rr = AHHollow.R - 3.4f;
            var ring = new List<Vector3>(); for (int i = 0; i < 36; i++) ring.Add(Ring(i * 10f, rr));
            int gi = 0;
            foreach (var who in new[] { "Town guard Aldric", "Town guard Hilde" })
            {
                var rt = new List<Vector3>(); for (int i = 0; i < ring.Count; i++) rt.Add(ring[(i + gi * 18) % ring.Count]);
                if (gi == 1) rt.Reverse();
                var n = AHNpc.SpawnAt(g, who, rt[0], rt[1] - rt[0], new[] { "All quiet on the ring street.", "Keep your blade sheathed inside the walls, friend." }, true);
                n.route = rt.ToArray(); n.routeSpeed = 1.1f; gi++;
            }
        }
        // ---------- 6. the night at the inn: the bard and the regulars ----------
        {
            var inn = Hall("inn");
            if (inn != null)
            {
                bard = AHNpc.SpawnAt(g, "Bard Lyra", inn.At(inn.w * 0.13f, inn.d * 0.6f), -inn.b, new[] { "A song for a coin? Or for free, I like your face.", "Come back at night, that's when the inn sings." });
                bard.SetSeq("Idle_Loop", "Idle_Talking_Loop");
                bard.onTalk = () => { g.ui.TalkLine(bard, night ? "This one's for the hero in the corner!" : "Come back after dark, that's when the inn sings."); if (night) { lute++; AHSound.Town("lute", 1f, lute); } return true; };
                var spots = new[] { new Vector2(-0.27f, 0.4f), new Vector2(0.27f, 0.4f), new Vector2(0f, 0.4f) };
                var offs = new[] { new Vector2(0f, -1.25f), new Vector2(0f, -1.25f), new Vector2(0f, 0.3f) };
                // the table games, day and night
                var games = new[] {
                    new { who = "Card sharp Wilma", x = -0.27f, dx = -1.6f, z = 0.4f, dz = 0f, label = "Beastcards", line = "Fancy a hand of Beastcards?", go = (System.Action)(() => AHTable.OpenCards(g)) },
                    new { who = "Old Ottar", x = 0.27f, dx = 1.6f, z = 0.4f, dz = 0f, label = "Play dice", line = "Bones, friend? First to thirty.", go = (System.Action)(() => AHTable.OpenDice(g)) },
                    new { who = "Big Brann", x = -0.12f, dx = 0f, z = 0.62f, dz = 0f, label = "Arm-wrestle", line = "Think you're strong? Sit down.", go = (System.Action)(() => AHTable.OpenArm(g)) },
                };
                foreach (var gm in games)
                {
                    var gg = gm; Vector3 at = inn.At(inn.w * gm.x + gm.dx, inn.d * gm.z + gm.dz);
                    var n = AHNpc.SpawnAt(g, gm.who, at, Flat3(inn.At(0f, inn.d * 0.4f) - at), new[] { gm.line });
                    n.label = gm.label; n.SetSeq("Idle_Loop", "Idle_Talking_Loop");
                    n.onTalk = () => { AHLife.Speak(n, gg.line, gg.line, 3f); gg.go(); return true; };
                }
                var names = new[] { "Patron Ott", "Patron Inga", "Patron Nami" };
                var said = new[] { new[] { "Another round! The bard's on fire tonight!" }, new[] { "Best stew in the Meadow, don't tell the cook I said so." }, new[] { "Sit, sit! The night is young." } };
                for (int i = 0; i < 3; i++)
                {
                    Vector3 at = inn.At(inn.w * spots[i].x + offs[i].x, inn.d * spots[i].y + offs[i].y);
                    var n = AHNpc.SpawnAt(g, names[i], at, inn.At(inn.w * spots[i].x, inn.d * spots[i].y) - at, said[i]);
                    n.SetSeq("Idle_Talking_Loop", "Consume", "Yes", "Idle_Loop");
                    nightFolk.Add(n);
                }
            }
        }
        // ---------- 7. guards at every gate, and the portcullis ----------
        {
            foreach (var gt in AHHollow.Gates)
            {
                foreach (var sd in new[] { -1f, 1f })
                {
                    Vector3 at = gt.at - gt.dir * 4.4f + gt.tan * sd * (gt.hw + 0.9f); at.y = C.y;
                    AHNpc.SpawnAt(g, "Gate guard", at, gt.dir, new[] { "Safe roads, traveller.", "The gate comes down at nightfall. It lifts for friends." }, true);
                }
                gateY.Add(gt.grid.position.y); gateDir.Add(0);
                raisedY = gt.grid.position.y;
            }
        }
        // ---------- 8. the wishing fountain ----------
        if (AHHollow.FountainAt != Vector3.zero)
        {
            var spot = new AHSpot { kind = "use", type = "wish", name = "Toss a coin in the fountain", pos = AHHollow.FountainAt + Flat3(C - AHHollow.FountainAt).normalized * 1.9f, r = 0.6f, reach = 2.8f };
            spot.use = Wish;
            AHGather.Spots.Add(spot);
        }
        // ---------- 9. the house plot ----------
        if (AHHollow.Plot != null)
        {
            var h = AHHollow.Plot;
            plotSpot = new AHSpot { kind = "use", type = "house_plot", name = "House plot", pos = h.At(0f, 0.9f), r = 0.6f, reach = 2.8f };
            plotSpot.use = UsePlot;
            AHGather.Spots.Add(plotSpot);
            if (PlotState == 2) BuildHouse(false);
            PlotLabel();
        }
        // ---------- 10. the stable: horses in their stalls, the stablemaster ----------
        {
            var h = AHHollow.StableHall;
            if (h != null)
            {
                var sm = AHNpc.SpawnAt(g, "Stablemaster Rook", h.At(-h.w * 0.12f, 2.0f), -h.b, new[] { "Four legs beat two. Pick your steed, then press Ride." });
                sm.label = "Stable"; sm.SetSeq("Idle_FoldArms_Loop", "Idle_Loop");
                sm.onTalk = () => { g.ui.OpenShop("hm_stable"); return true; };
                string[] models = { "mount_horse", "mount_destrier" };
                float[] xs = { -h.w / 3f, h.w / 3f };
                for (int i = 0; i < 2; i++)
                {
                    if (Resources.Load<GameObject>("AH/Models/Comp/" + models[i]) == null && Resources.Load<GameObject>("AH/Models/Comp/" + models[i] + "_hd") == null) continue;
                    var holder = new GameObject("AH_Stable horse").transform; holder.SetParent(AHHollow.Root, false);
                    holder.position = h.At(xs[i], h.d * 0.7f) + Vector3.up * 0.1f; holder.rotation = Quaternion.LookRotation(-h.b);
                    AHAnim a; AHModel.Spawn(holder, "Comp/" + models[i], 0f, false, 0f, out a);
                    AHModel.SetShadows(holder.gameObject);
                    // the stall is only so deep: a big horse is brought down to fit
                    var rs = holder.GetComponentsInChildren<Renderer>(); if (rs.Length > 0) { Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); float len = Mathf.Max(b.size.x, b.size.z); if (len > 3.0f) holder.localScale *= 3.0f / len; }
                    if (a != null && a.HasClips) { a.Play("Idle", true); horses.Add(a); } else if (a != null) a.Dispose();
                    horseT.Add(holder);
                }
            }
        }
        // ---------- 11. the weather's touches ----------
        Smoke(); Flies(); Puddles(); Birds(); Updrafts();
        AHRace.Setup(g, this);
        SetNight(night, true);
        Debug.Log("Ashen Hollow: town life started (crier, " + AHHollow.Gates.Count + " gates, " + horses.Count + " horses)");
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    static Vector3 Flat3(Vector3 v) { v.y = 0f; return v; }

    // ---------- the crier's news ----------
    string[] News()
    {
        var p = g.player; string hero = p != null && !string.IsNullOrEmpty(p.heroName) ? p.heroName : "a hero";
        var list = new List<string[]>();
        list.Add(new[] { "Hear ye! It is " + g.TimeText() + " in Hollow Meadow, and all is well!", "اسمعوا! الساعة " + g.TimeText() + " في هولو ميدو، وكل شيء بخير!" });
        if (AHWeather.Raining) list.Add(new[] { "Rain over the Meadow! The fish are biting and the herbs are open!", "مطر فوق المرج! السمك يأكل والأعشاب متفتحة!" });
        else if (AHWeather.Now == AHWeather.Kind.Fog) list.Add(new[] { "Fog on the roads! Beasts see less, and so do you. Walk carefully!", "ضباب على الطرق! الوحوش ترى أقل، وأنت كذلك. امشِ بحذر!" });
        else list.Add(new[] { "Fair skies over the Meadow! A good day for the road!", "سماء صافية فوق المرج! يوم جميل للسفر!" });
        if (p != null) list.Add(new[] { "Hear ye! " + hero + " the " + (p.cls != null ? p.cls.name : "hero") + " has reached level " + p.level + "!", "اسمعوا! " + hero + " وصل إلى المستوى " + p.level + "!" });
        list.Add(new[] { "New jobs on the town board! Coin for honest work!", "أعمال جديدة على لوحة البلدة! مال مقابل عمل شريف!" });
        list.Add(new[] { "Fresh bread and fish at the market stalls!", "خبز وسمك طازج في أكشاك السوق!" });
        list.Add(new[] { "A coin in the fountain, a wish for the day!", "قطعة نقود في النافورة، وأمنية لليوم!" });
        list.Add(new[] { "Plot for sale on the ring street! Build your own house in town!", "قطعة أرض للبيع على الشارع الدائري! ابنِ بيتك في البلدة!" });
        list.Add(new[] { g.IsNight ? "The gates are down for the night! The inn is open and the bard is playing!" : "Horses for sale at the Hollow Stables!", g.IsNight ? "البوابات مغلقة لليل! النُزل مفتوح والشاعرة تعزف!" : "خيول للبيع في إسطبلات هولو!" });
        cryI++;
        return list[(cryI + Random.Range(0, 2)) % list.Count];
    }

    void Update()
    {
        if (g == null || g.player == null || !AHHollow.Active) return;
        float dt = Time.deltaTime, now = Time.time;
        Vector3 pp = g.player.transform.position;
        foreach (var a in horses) a.Tick(dt);

        // day and night (checked now and then)
        if (now >= nextCheck)
        {
            nextCheck = now + 1.5f;
            bool n = g.IsNight; if (n != night) SetNight(n, false);
            WeatherTick();
        }

        // the crier stops now and then to ring and call
        if (crier != null && now >= nextCry)
        {
            nextCry = now + Random.Range(22f, 34f);
            float d = crier.DistTo(pp);
            if (d < 28f)
            {
                crier.hold = 5f;
                AHSound.Town("handbell", 1f - d / 30f);
                var nw = News(); AHLife.Speak(crier, nw[0], nw[1], 4.6f);
            }
        }

        // the bard's lute at night, heard near the inn
        if (night && bard != null && now >= nextLute)
        {
            nextLute = now + Random.Range(2.6f, 3.4f);
            float d = bard.DistTo(pp); if (d < 16f) { lute++; AHSound.Town("lute", 1f - d / 16f, lute); }
        }

        // a horse snorts in the stable
        if (horseT.Count > 0 && now >= nextSnort)
        {
            nextSnort = now + Random.Range(7f, 14f);
            var h = horseT[Random.Range(0, horseT.Count)]; float d = Flat(h.position - pp);
            if (d < 12f) AHSound.Town("snort", 1f - d / 12f);
            foreach (var a in horses) if (Random.value < 0.4f && a.Has("Eating")) a.Play("Eating", true); else a.Play("Idle", true);
        }

        Gates(dt, pp);
        UpdraftTick(pp, now);
        BellAndClock(dt, pp, now);
        BirdsTick(dt);
        if (smoke != null && now >= nextPuff) { nextPuff = now + 0.35f; foreach (var c in AHHollow.Chimneys) { var e = new ParticleSystem.EmitParams { position = c + Random.insideUnitSphere * 0.15f, velocity = new Vector3(0.25f, Random.Range(0.6f, 0.9f), 0.1f), startSize = Random.Range(0.5f, 0.8f), startColor = new Color(0.62f, 0.6f, 0.58f, 0.45f), startLifetime = Random.Range(4f, 6f) }; smoke.Emit(e, 1); } }
    }

    void SetNight(bool n, bool first)
    {
        night = n;
        foreach (var f in dayFolk) if (f != null) f.gameObject.SetActive(!n);
        foreach (var f in nightFolk) if (f != null) f.gameObject.SetActive(n);
        if (bard != null) bard.SetSeq(n ? new[] { "Dance_Loop", "Idle_Talking_Loop", "Yes" } : new[] { "Idle_Loop", "Idle_Talking_Loop" });
        if (flies != null) { if (n) flies.Play(); else flies.Stop(); }
        if (!first && AHHollow.Inside(g.player.transform.position, 10f)) g.ui.Toast(n ? "Night falls on Hollow Meadow: the gates come down and the inn fills up." : "Morning in Hollow Meadow: the gates are up and the market is open.", 4f);
    }

    // ---------- the portcullis: down at night, up by day, and up for you as you come near ----------
    void Gates(float dt, Vector3 pp)
    {
        for (int i = 0; i < AHHollow.Gates.Count && i < gateY.Count; i++)
        {
            var gt = AHHollow.Gates[i]; if (gt.grid == null) continue;
            float d = Flat(gt.at - pp);
            float want = night && d > 15f ? 0f : raisedY;
            float y = gt.grid.position.y;
            if (Mathf.Abs(y - want) < 0.01f) { gateDir[i] = 0; continue; }
            int dir = want > y ? 1 : -1;
            if (gateDir[i] != dir) { gateDir[i] = dir; if (d < 40f) AHSound.Town("gate", 1f - d / 45f); }
            y = Mathf.MoveTowards(y, want, (dir > 0 ? 1.6f : 1.1f) * dt);
            gt.grid.position = new Vector3(0f, y, 0f);
        }
    }

    // ---------- the bell rings the hours; the clock keeps time ----------
    void BellAndClock(float dt, Vector3 pp, float now)
    {
        float hf = g.DayFrac * 24f; int hour = Mathf.FloorToInt(hf);
        if (AHHollow.ClockHour != null)
        {
            AHHollow.ClockHour.rotation = clockBase * Quaternion.Euler(0, 0, (hf % 12f) / 12f * 360f);
            if (AHHollow.ClockMin != null) AHHollow.ClockMin.rotation = clockBase * Quaternion.Euler(0, 0, (hf % 1f) * 360f);
        }
        if (hourLast < 0) hourLast = hour;
        if (hour != hourLast)
        {
            hourLast = hour;
            ringsLeft = hour == 0 ? 0 : (hour == 6 || hour == 12 || hour == 18) ? 3 : 1;
            ringAt = now;
        }
        if (ringsLeft > 0 && now >= ringAt)
        {
            ringsLeft--; ringAt = now + 2.4f; swing = 1f;
            float d = Flat(AHHollow.BellAt - pp); if (d < 110f) AHSound.Town("bell", Mathf.Clamp01(1.1f - d / 110f));
        }
        if (AHHollow.Bell != null)
        {
            swing = Mathf.Max(0f, swing - dt * 0.35f);
            AHHollow.Bell.rotation = bellBase * Quaternion.Euler(Mathf.Sin(now * 3.1f) * 24f * swing, 0, 0);
        }
    }

    // ---------- the wishing fountain: a copper coin a day ----------
    void Wish()
    {
        var p = g.player; long cost = 10 * AHDB.CU;
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (AHPrefs.GetString("ah_wish_day", "") == today) { g.ui.Toast("You've made today's wish. The fountain grants one a day.", 3f); return; }
        if (p.bag.money < cost) { g.ui.Toast("A wish costs " + AHItems.MoneyText(cost) + ".", 3f); return; }
        p.bag.money -= cost; AHPrefs.SetString("ah_wish_day", today);
        Vector3 at = AHHollow.FountainAt + Vector3.up * 1.2f;
        AHSound.Play("coin"); AHSound.Town("wish", 1f);
        AHSpark.Burst(at, new Color(0.6f, 0.9f, 1f, 0.9f), 18, 2.2f, 0.9f, 0.12f, 1.2f);
        float r = Random.value; string what;
        if (r < 0.35f) { long back = Random.Range(20, 60) * AHDB.CU; p.AddMoney(back, at); what = "Coins glint back up from the water: " + AHItems.MoneyText(back) + "!"; }
        else if (r < 0.6f) { p.bag.Add("hp_potion", 1); what = "A little bottle bobs up from the bottom: a health potion!"; }
        else if (r < 0.8f) { p.bag.Add("bread", 2); what = "Someone's lost picnic floats up. Two loaves of bread, still dry somehow."; }
        else if (r < 0.92f && AHItems.Get("mystery_sack") != null) { p.bag.Add("mystery_sack", 1); what = "The water glows and a small sack rises to your hand. A mystery sack!"; }
        else what = "The water ripples. Somewhere, something listened.";
        string[] fortunes = { "Today the road is kind to you.", "A friend is closer than you think.", "Rare things are found by those who look twice.", "Rest is part of the journey.", "The boar fears you more than you fear it. Probably." };
        g.ui.Banner("A wish in the fountain", what);
        g.ui.Toast("Your fortune: " + fortunes[Random.Range(0, fortunes.Length)], 5f);
        p.bag.Touch(); g.MarkDirty();
    }

    // ---------- the house plot: buy it, then build on it; your house's door takes you to your homestead ----------
    const long PlotPriceSilver = 15, BuildSilver = 8; const int BuildLogs = 30;
    static int PlotState { get { return AHPrefs.GetInt("ah_hm_plot", 0); } set { AHPrefs.SetInt("ah_hm_plot", value); } }
    void PlotLabel()
    {
        if (plotSpot == null) return;
        plotSpot.name = PlotState == 0 ? "Buy this plot (" + PlotPriceSilver + " silver)" : PlotState == 1 ? "Build your house" : "Your house: go home";
    }
    void UsePlot()
    {
        var p = g.player; long silver = 1000000;
        if (PlotState == 0)
        {
            long cost = PlotPriceSilver * silver;
            if (p.bag.money < cost) { g.ui.Toast("This plot costs " + AHItems.MoneyText(cost) + ". Come back when your purse is heavier.", 4f); return; }
            if (Time.time > plotConfirmT) { plotConfirmT = Time.time + 6f; g.ui.Toast("Buy this plot for " + AHItems.MoneyText(cost) + "? Tap again to buy.", 5f); return; }
            p.bag.money -= cost; PlotState = 1; plotConfirmT = 0f; PlotLabel();
            AHSound.Play("coin");
            g.ui.Banner("The plot is yours", "Bring " + BuildLogs + " logs and " + BuildSilver + " silver to build your house.");
            p.bag.Touch(); g.MarkDirty(); return;
        }
        if (PlotState == 1)
        {
            long cost = BuildSilver * silver; int logs = p.bag.Count("logs");
            if (logs < BuildLogs || p.bag.money < cost) { g.ui.Toast("To build you need " + BuildLogs + " logs (you have " + logs + ") and " + AHItems.MoneyText(cost) + ".", 5f); return; }
            if (Time.time > plotConfirmT) { plotConfirmT = Time.time + 6f; g.ui.Toast("Build your house for " + BuildLogs + " logs and " + AHItems.MoneyText(cost) + "? Tap again to build.", 5f); return; }
            p.bag.Take("logs", BuildLogs); p.bag.money -= cost; PlotState = 2; plotConfirmT = 0f;
            BuildHouse(true); PlotLabel();
            p.bag.Touch(); g.MarkDirty(); return;
        }
        AHHome.TravelHome(g);
    }
    void BuildHouse(bool fresh)
    {
        var h = AHHollow.Plot; if (h == null || house != null) return;
        if (AHHollow.PlotForSale != null) AHHollow.PlotForSale.SetActive(false);
        house = AHHollow.TownProp("kk_building_home_B_" + AHHollow.TownColour, h.go, h.At(0f, h.d * 0.56f), Mathf.Min(h.w, h.d) * 0.82f, -h.b);
        if (house == null) house = AHHollow.TownProp("kk_building_home_A_" + AHHollow.TownColour, h.go, h.At(0f, h.d * 0.56f), Mathf.Min(h.w, h.d) * 0.82f, -h.b);
        string who = g.player != null && !string.IsNullOrEmpty(g.player.heroName) ? g.player.heroName : "Your";
        AHHollow.TownSign(h.go, h.At(0f, 0.15f) + Vector3.up * 1.55f - h.b * 0.1f, -h.b, who == "Your" ? "Your House" : who + "’s House", "9fe07a");
        var c = h.At(0f, h.d * 0.56f); g.AddBlocker(new Vector3(c.x, 0f, c.z), Mathf.Min(h.w, h.d) * 0.36f);
        // the garden shows off what you have done: your trophies on a bench, your best catch on a sign
        try
        {
            if (AHTrophies.Count(g.player) > 0) { var st = AHHollow.DisplayStand(h.go, h.At(h.w / 2f - 0.75f, h.d * 0.52f), -h.r, 5.8f); AHTrophies.BuildStand(g, st); }
            if (AHDerby.Best > 0f) AHHollow.TownSign(h.go, h.At(-h.w / 2f + 1.9f, 0.15f) + Vector3.up * 1.45f - h.b * 0.1f, -h.b, "Best catch " + AHDerby.Best.ToString("0.0") + " kg", "6fe7ff");
        }
        catch (System.Exception e) { Debug.LogWarning("Ashen Hollow: house garden (" + e.Message + ")"); }
        if (fresh)
        {
            AHSound.Play("level"); AHSpark.Burst(c + Vector3.up * 2f, new Color(1f, 0.9f, 0.5f, 0.9f), 30, 3f, 1f, 0.15f, 1.5f);
            g.ui.Banner("Your house is built!", "Its door takes you to your homestead.");
        }
    }

    // ---------- the weather's touches ----------
    ParticleSystem smoke, flies; float nextPuff;
    readonly List<Transform> puddles = new List<Transform>();
    float puddleK;
    static Texture2D softTex;
    static Material Soft(bool add)
    {
        if (softTex == null)
        {
            const int N = 64; softTex = new Texture2D(N, N, TextureFormat.RGBA32, false); softTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / (N / 2f) - 1f, v = (y + 0.5f) / (N / 2f) - 1f, r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Clamp01(1f - r) * (0.85f + 0.15f * Mathf.PerlinNoise(x * 0.18f, y * 0.18f)); a = a * a * (3f - 2f * a);
                    softTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            softTex.Apply();
        }
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); m.SetTexture("_BaseMap", softTex); m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", add ? 2f : 0f); m.SetFloat("_ZWrite", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", add ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000;
        return m;
    }
    ParticleSystem MakePS(string name, bool add, int max)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = max; main.startSpeed = 0f;
        var em = ps.emission; em.rateOverTime = 0f;
        var shp = ps.shape; shp.enabled = false;
        var pr = go.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = Soft(add); pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false;
        return ps;
    }
    void Smoke()
    {
        if (AHHollow.Chimneys.Count == 0) return;
        smoke = MakePS("AH_Chimney smoke", false, 400);
        var size = smoke.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 3.2f));
        var col = smoke.colorOverLifetime; col.enabled = true;
        var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.45f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;
        smoke.Play();
    }
    void Flies()
    {
        flies = MakePS("AH_Fireflies", true, 120);
        var main = flies.main; main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 1f, 0.45f, 1f), new Color(1f, 0.9f, 0.4f, 1f));
        var em = flies.emission; em.rateOverTime = 14f;
        var shp = flies.shape; shp.enabled = true; shp.shapeType = ParticleSystemShapeType.Circle; shp.radius = AHHollow.R - 4f; shp.rotation = new Vector3(90f, 0f, 0f);
        flies.transform.position = C + Vector3.up * 1.4f;
        var nz = flies.noise; nz.enabled = true; nz.strength = 0.45f; nz.frequency = 0.35f; nz.scrollSpeed = 0.2f;
        var col = flies.colorOverLifetime; col.enabled = true;
        var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.2f, 0.5f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;
    }
    void Puddles()
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Puddle" };
        m.SetColor("_BaseColor", new Color(0.16f, 0.17f, 0.19f)); m.SetFloat("_Smoothness", 0.96f); m.SetFloat("_Metallic", 0.1f);
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere); var mesh = tmp.GetComponent<MeshFilter>().sharedMesh; Destroy(tmp);
        var rnd = new System.Random(31);
        for (int i = 0; i < 40 && puddles.Count < 16; i++)
        {
            float a = (float)rnd.NextDouble() * 360f, r = 4f + (float)rnd.NextDouble() * (AHHollow.R - 8f);
            Vector3 p = Ring(a, r); bool bad = false;
            foreach (var h in AHHollow.Halls) if (h.Contains(p, 1.2f)) { bad = true; break; }
            if (AHHollow.Plot != null && AHHollow.Plot.Contains(p, 1f)) bad = true;
            if (Flat(p - AHHollow.FountainAt) < 3f || Flat(p - AHHollow.BellAt) < 3.5f) bad = true;
            if (bad) continue;
            var go = new GameObject("AH_Puddle"); go.transform.SetParent(transform, false);
            go.transform.position = p + Vector3.up * 0.02f; go.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 0);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.transform.localScale = new Vector3(1.2f + (float)rnd.NextDouble() * 1.4f, 0.006f, 0.8f + (float)rnd.NextDouble() * 0.9f);
            go.SetActive(false); puddles.Add(go.transform);
        }
    }
    readonly List<Vector3> puddleSize = new List<Vector3>();
    void WeatherTick()
    {
        bool rain = AHWeather.Raining;
        puddleK = Mathf.MoveTowards(puddleK, rain ? 1f : 0f, 1.5f / 40f);
        if (puddleSize.Count != puddles.Count) { puddleSize.Clear(); foreach (var p in puddles) puddleSize.Add(p.localScale); }
        for (int i = 0; i < puddles.Count; i++)
        {
            var p = puddles[i]; bool on = puddleK > 0.02f; if (p.gameObject.activeSelf != on) p.gameObject.SetActive(on);
            if (on) p.localScale = new Vector3(puddleSize[i].x * puddleK, puddleSize[i].y, puddleSize[i].z * puddleK);
        }
        bool birdsOut = !night && !rain && AHWeather.Now != AHWeather.Kind.Fog;
        foreach (var b in birds) if (b.t.gameObject.activeSelf != birdsOut) b.t.gameObject.SetActive(birdsOut);
    }

    // ---------- updrafts: columns of rising wind round the town; with wings on your back, ride one up and glide ----------
    readonly List<Vector3> drafts = new List<Vector3>(); float draftHintT;
    void Updrafts()
    {
        var spots = new List<Vector3>();
        if (AHHollow.BellAt != Vector3.zero) { Vector3 d = Flat3(C - AHHollow.BellAt).normalized; spots.Add(AHHollow.BellAt + d * 3.6f + Vector3.Cross(Vector3.up, d) * 2.6f); }
        foreach (var a in new[] { 40f, 130f, 220f, 310f })
            for (int k = 0; k < 12; k++)
            {
                Vector3 p = Ring(a + k * 7f, AHHollow.R + 16f + (k % 3) * 4f); p.y = C.y;
                bool nearGate = false; foreach (var gt in AHHollow.Gates) if (Flat(gt.at - p) < 18f) nearGate = true;
                if (nearGate || !g.InArea(p) || g.Blocked(p, 2.2f) || AHNav.IsDirt(g, p) || (AHHollow.Plot != null && AHHollow.Plot.Contains(p, 3f))) continue;
                spots.Add(p); break;
            }
        foreach (var p in spots)
        {
            drafts.Add(p);
            var ps = MakePS("AH_Updraft", true, 160); ps.transform.position = p;
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.2f); main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.9f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.35f));
            var em = ps.emission; em.rateOverTime = 40f;
            var shp = ps.shape; shp.enabled = true; shp.shapeType = ParticleSystemShapeType.Circle; shp.radius = 1.1f; shp.rotation = new Vector3(90f, 0f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalY = new ParticleSystem.MinMaxCurve(2.2f); vel.x = new ParticleSystem.MinMaxCurve(0f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.speedModifier = new ParticleSystem.MinMaxCurve(1f);
            var lim = ps.velocityOverLifetime; lim.radial = new ParticleSystem.MinMaxCurve(0f);
            var force = ps.forceOverLifetime; force.enabled = true; force.space = ParticleSystemSimulationSpace.World; force.x = new ParticleSystem.MinMaxCurve(0f); force.y = new ParticleSystem.MinMaxCurve(5f); force.z = new ParticleSystem.MinMaxCurve(0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = gr;
            ps.Play();
            AHFx.Ring(p, 1.2f, 1.4f, new Color(0.75f, 0.9f, 1f, 0.5f), 99999f);
        }
    }
    void UpdraftTick(Vector3 pp, float now)
    {
        var p = g.player; if (p.Gliding) return;
        foreach (var d in drafts)
        {
            if (Flat(d - pp) > 1.5f) continue;
            if (!p.Updraft(12f))
            {
                if (now >= draftHintT) { draftHintT = now + 12f; g.ui.Toast("A rising wind. With wings on your back you could ride it up and glide.", 4f); }
            }
            else { g.ui.Toast("The wind lifts you! Steer as you glide down.", 3f); p.prog.Bump("glides"); }
            return;
        }
    }

    // a few birds wheeling over the roofs by day: a small dark body with two beating wings
    class Bird { public Transform t, wl, wr; public float a, r, h, sp, ph; }
    readonly List<Bird> birds = new List<Bird>();
    void Birds()
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Bird" }; m.SetColor("_BaseColor", new Color(0.12f, 0.11f, 0.12f)); m.SetFloat("_Smoothness", 0.2f);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); var box = cube.GetComponent<MeshFilter>().sharedMesh; Destroy(cube);
        var sph = GameObject.CreatePrimitive(PrimitiveType.Sphere); var ball = sph.GetComponent<MeshFilter>().sharedMesh; Destroy(sph);
        System.Func<Transform, Mesh, Vector3, Vector3, Transform> Part = (par, mesh, pos, sc) =>
        {
            var go = new GameObject("part"); go.transform.SetParent(par, false); go.transform.localPosition = pos; go.transform.localScale = sc;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return go.transform;
        };
        for (int f = 0; f < 2; f++)
            for (int i = 0; i < 5; i++)
            {
                var b = new Bird { a = f * 180f + i * 9f + Random.Range(-3f, 3f), r = 20f + f * 8f + Random.Range(-2f, 2f), h = 17f + f * 4f + Random.Range(-1.5f, 1.5f), sp = (f == 0 ? 11f : -9f) + Random.Range(-1f, 1f), ph = Random.value * 6f };
                b.t = new GameObject("AH_Bird").transform; b.t.SetParent(transform, false);
                Part(b.t, ball, Vector3.zero, new Vector3(0.12f, 0.1f, 0.34f));
                Part(b.t, ball, new Vector3(0f, 0.03f, 0.17f), new Vector3(0.09f, 0.08f, 0.1f));
                Part(b.t, box, new Vector3(0f, 0f, -0.2f), new Vector3(0.14f, 0.015f, 0.12f));
                foreach (var sd in new[] { -1f, 1f })
                {
                    var pv = new GameObject(sd < 0 ? "wl" : "wr").transform; pv.SetParent(b.t, false); pv.localPosition = new Vector3(sd * 0.04f, 0.02f, 0.02f);
                    Part(pv, box, new Vector3(sd * 0.2f, 0f, 0f), new Vector3(0.4f, 0.012f, 0.13f));
                    if (sd < 0) b.wl = pv; else b.wr = pv;
                }
                birds.Add(b);
            }
    }
    void BirdsTick(float dt)
    {
        float t = Time.time;
        foreach (var b in birds)
        {
            if (!b.t.gameObject.activeSelf) continue;
            b.a += b.sp * dt / b.r * Mathf.Rad2Deg * 0.18f;
            Vector3 p = Ring(b.a, b.r) + Vector3.up * (b.h + Mathf.Sin(t * 0.7f + b.ph) * 0.8f);
            Vector3 fwd = p - b.t.position; if (fwd.sqrMagnitude > 1e-5f) b.t.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(0, 0, b.sp > 0 ? -12f : 12f);
            b.t.position = p;
            float flap = (Mathf.Sin(t * 2.2f + b.ph) > 0.2f) ? Mathf.Sin(t * 16f + b.ph) * 38f : 6f;   // beat, then glide
            b.wl.localRotation = Quaternion.Euler(0, 0, -flap); b.wr.localRotation = Quaternion.Euler(0, 0, flap);
        }
    }

    // a golden beast was seen: the crier calls it out at once (when you are in town to hear)
    public static void Golden(string name)
    {
        if (I == null || I.crier == null || I.g == null || I.g.player == null) return;
        if (!AHHollow.Inside(I.g.player.transform.position, 10f)) return;
        string n = name.ToLowerInvariant();
        I.crier.hold = 5f; AHSound.Town("handbell", 0.8f);
        AHLife.Speak(I.crier, "Hear ye! A golden " + n + " roams the Meadow! Gold to whoever brings it down!", "اسمعوا! " + name + " ذهبي يجوب المرج! الذهب لمن يصطاده!", 5f);
        I.nextCry = Time.time + 25f;
    }

    // test tools (AHRemote "town ..."): ring the bell now, cry now, toggle the gates' night, build the house
    public static void Test(string what)
    {
        if (I == null) return;
        if (what == "bell") { I.ringsLeft = 3; I.ringAt = Time.time; }
        else if (what == "cry") I.nextCry = 0f;
        else if (what == "night") I.SetNight(!I.night, false);
        else if (what == "house") { PlotState = 2; I.BuildHouse(true); I.PlotLabel(); }
        else if (what == "plotreset") { PlotState = 0; }
        else if (what == "jobs") AHGame.I.ui.OpenJobs();
        else if (what == "close") { AHGame.I.ui.ShowWork(false); AHGame.I.ui.ShowBag(false); AHGame.I.ui.CloseOverlays(); AHTable.CloseAll(); }
        else if (what == "deal") AHTable.TestDeal(AHGame.I);
        else if (what.StartsWith("road")) { int k; int.TryParse(what.Substring(4), out k); AHRoadEvents.Start(AHGame.I, k); }
        else if (what == "ranked") AHGame.I.ui.OpenRanked();
        else if (what == "glide") { var p = AHGame.I.player; AHWingRig.TestAir = 0f; if (!p.Updraft(12f)) Debug.Log("Ashen Hollow: glide test: no wings on"); }
        else if (what == "draft" && I.drafts.Count > 0) { var p = AHGame.I.player; Vector3 d = I.drafts[0]; p.transform.position = d + (C - d).normalized * 5f; }
        else if (what.StartsWith("race")) AHRace.Test(what.Substring(4).Trim());
        else if (what == "cards") AHTable.OpenCards(AHGame.I);
        else if (what == "hub") AHGame.I.ui.OpenHub();
        else if (what == "char") AHGame.I.ui.OpenChar();
        else if (what == "bounty") AHGame.I.ui.OpenBounty();
        else if (what == "bagnew") AHGame.I.ui.TestBagNew();
        else if (what == "bountyreset") AHPrefs.SetString("ah_bounty", "");
        else if (what == "hunthere") AHFun3.TestHere(AHGame.I.player);
        else if (what == "logout") AHGame.I.Logout();
        else if (what == "netstatus") { Debug.Log("Ashen Hollow: online status: " + AHNet.Status + " · in room " + AHNet.InRoom + " · peers " + AHNet.Peers.Count); AHGame.I.ui.OpenOnline(); }
        else if (what == "netfake")
        {
            // a pretend second player beside you (to see how others are drawn), in your party
            var pl = AHGame.I.player; var gear = new System.Collections.Generic.List<string>(); foreach (var sl in AHItems.GearSlots) gear.Add(AHWardrobe.Shown(pl, sl) ?? "");
            var props = new System.Collections.Hashtable { { "c", "mage" }, { "lv", 18 }, { "lk", JsonUtility.ToJson(new AHLook { sex = "f", hair = "long" }) }, { "g", string.Join(",", gear.ToArray()) }, { "co", "" }, { "t", "the Brave" }, { "m", "" } };
            AHNet.PeerIn(999, "Layla", props);
            var pe = AHNet.Peers[999]; pe.pos = pl.transform.position + pl.transform.right * 2.5f; pe.yaw = pl.transform.eulerAngles.y; pe.clip = "Idle"; pe.seen = Time.time; pe.hp = 300; pe.maxHp = 420;
            AHNet.Party.Clear(); AHNet.Party.Add(AHNet.MyActor); AHNet.Party.Add(999);
        }
        else if (what == "netsay") { AHNet.OnEvent(AHNet.EvChat, 999, "Hi Ahmad! Want to trade?"); var pe = AHNet.Peers.ContainsKey(999) ? AHNet.Peers[999] : null; if (pe != null) pe.seen = Time.time; }
        else if (what == "netask") AHGame.I.ui.NetAsk("Layla invites you to their party.", () => AHGame.I.ui.Toast("yes", 2f), () => AHGame.I.ui.Toast("no", 2f));
        else if (what == "nettrade") { AHNet.TradeWith = 999; AHNet.TheirOffer = new AHNet.Offer(); AHNet.TheirOffer.ids.Add("ruby"); AHNet.TheirOffer.ns.Add(2); AHNet.TheirOffer.money = 500; AHNet.TheirReady = true; AHGame.I.ui.OpenTrade(); }
        else if (what == "wheel2") AHGame.I.ui.OpenWheel();
        else if (what == "wheelreset") AHPrefs.SetString("ah_wheel_day", "");
        else if (what == "spin") AHGame.I.ui.SendMessage("StartSpin");
        else if (what == "fishj") AHGame.I.ui.OpenFishJournal();
        else if (what == "legend") AHGame.I.ui.OpenLegend();
        else if (what == "legendhere") AHFun4.TestLegendHere(AHGame.I.player);
        else if (what == "fightlegend") { var h = AHFun4.Legend; if (h != null) { var pl = AHGame.I.player; pl.transform.position = h.transform.position + (pl.transform.position - h.transform.position).normalized * 6f; pl.target = h; } }
        else if (what == "dragon") AHFun4.SummonDragon(AHGame.I);
        else if (what == "lookdragon") { var d = AHFun4.Dragon; if (d != null) { Vector3 c = d.transform.position; AHGame.FreePos = c + new Vector3(14f, 6f, 14f); AHGame.FreeRot = Quaternion.LookRotation(c - AHGame.FreePos); AHGame.FreeCam = true; } }
        else if (what == "godragon") { var d = AHFun4.Dragon; if (d != null) { var pl = AHGame.I.player; pl.transform.position = d.transform.position + new Vector3(8f, 0f, 8f); } }
        else if (what == "gallop") { var pl = AHGame.I.player; if (pl.mountSel != null) pl.mountXp[pl.mountSel] = 100000000; if (!pl.mounted) AHComp.ToggleRide(AHGame.I); }
        else if (what == "craftstar") { for (int i = 0; i < 25; i++) AHFun4.OnCrafted(AHGame.I, AHGame.I.player, "bronze_helm"); }
        else if (what == "giverosa") { AHGame.I.player.bag.Add("stew", 1); AHGame.I.player.bag.Add("trout", 1); AHGame.I.player.bag.Add("salmon", 1); AHGame.I.player.bag.Add("meat", 1); AHGame.I.player.bag.Add("pie", 1); }
        else if (what == "pip" || what == "pipfind" || what == "rosa" || what.StartsWith("season:")) AHTownMore.Test(what);
        else if (what == "hunt") { var h = AHFun3.Hunted; if (h != null) { var pl = AHGame.I.player; pl.transform.position = h.transform.position + (pl.transform.position - h.transform.position).normalized * 7f; pl.target = h; } }
        else if (what == "album") AHGame.I.ui.OpenAlbum();
        else if (what == "wheel") AHGame.I.ui.SendMessage("ToggleEmoteWheel");
        else if (what == "away") AHGame.I.ui.AwayCard(3 * 3600 + 25 * 60, new System.Collections.Generic.List<string> { "2 letters wait in your mail", "3 daily goals to claim", "3 bounties on the board today" });
        else if (what == "deed") AHGame.I.ui.DeedPop(AHAch.All[0]);
        else if (what == "storm") { for (int i = 0; i < 6 && AHWeather.Now != AHWeather.Kind.Storm; i++) AHWeather.TestNext(); }
        else if (what == "photoshot") { AHGame.I.ui.PhotoMode(true); AHGame.I.ui.SendMessage("Snap"); }
        else if (what == "seen") AHPrefs.SetString("ah_seen", (AHFun3.NowS - 5 * 3600).ToString());
        else if (what == "lanterns") { var nf = Object.FindFirstObjectByType<AHNightFun>(); if (nf != null) nf.SendMessage("ForceFestival"); }
        else if (what == "mailtest") { AHMail.Send(AHGame.I, "Caravan master of Ashen Hollow", "Your goods sold well in Ashen Hollow. Here is your coin.", 340); AHMail.Send(AHGame.I, "The Winter Fair", "A gift for you, with the season's warmest wishes.", 630, new AHStack { id = "ruby", n = 1 }); }
        else if (what == "mail") AHGame.I.ui.OpenMail();
        else if (what == "loading") { AHLoading.Show("mill"); AHLoading.Ready(); }
        else if (what == "loottest")
        {
            var g = AHGame.I; AHMob near = null; foreach (var m in g.mobs) if (m != null && !m.dead) { near = m; break; }
            if (near != null)
            {
                var pt = g.player.transform; var b = AHMob.Create(g, near.type, pt.position + pt.forward * 3f); g.mobs.Add(b);
                b.Hurt(999999, g.player); b.loot.Clear(); b.loot.AddRange(new[] { "iron_helm", "shadow_plate", "bronze_plate", "big_potion" }); b.KeepForLoot(); g.ui.OpenLoot(b);
            }
        }
        else if (what == "fast") AHGame.I.ui.OpenFastTravel();
        else if (what == "mmbig") AHGame.I.ui.SendMessage("MmToggle");
        else if (what == "tipsreset") { foreach (var k in new[] { "hub", "char", "shop", "mail", "quick", "map", "ways" }) AHPrefs.SetInt("ah_tip_" + k, 0); }
        else if (what == "giveelix") { var b = AHGame.I.player.bag; foreach (var id in new[] { "elixir_might", "elixir_swift", "dragon_draught" }) if (AHItems.Get(id) != null) b.Add(id, 2); b.Touch(); }
        else if (what == "crits") { var g = AHGame.I; foreach (var m in g.mobs) if (m != null && !m.dead && (m.transform.position - g.player.transform.position).magnitude < 15f) { AHPlayer.critFrame = Time.frameCount; m.Hurt(321, g.player); AHPlayer.hitEl = SpellEl.Fire; AHPlayer.elFrame = Time.frameCount; m.Hurt(87, g.player); AHPlayer.hitEl = SpellEl.Frost; AHPlayer.elFrame = Time.frameCount; m.Hurt(64, g.player); g.player.Heal(40f, true); break; } }
        else if (what == "givefood") { var b = AHGame.I.player.bag; b.Add("bread", 4); b.Add("stew", 2); b.Add("salmon", 3); b.Touch(); }
        else if (what.StartsWith("quick:")) AHGame.I.ui.OpenQuick(what.Substring(6));
        else if (what == "eat") AHGame.I.ui.SendMessage("QuickEat");
        else if (what == "map") AHGame.I.ui.OpenMap();
        else if (what.StartsWith("talk:")) { var n = AHNpc.Find(what.Substring(5)); if (n != null) { AHGame.I.player.transform.position = n.transform.position + n.transform.forward * 2.2f; AHGame.I.TalkTo(n); } }
        else if (what == "reward") AHGame.I.ui.RewardChest("Login calendar · day 3", 120, 800, 15, new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>> { new System.Collections.Generic.KeyValuePair<string, int>("big_potion", 2), new System.Collections.Generic.KeyValuePair<string, int>("ruby", 1) });
        else if (what == "hudedit") AHGame.I.ui.OpenHudEdit();
        else if (what == "hudreset") { foreach (var id in new[] { "joy", "fight", "top", "quest", "chat", "ride" }) { AHPrefs.SetFloat("ah_hud_" + id + "_x", 0f); AHPrefs.SetFloat("ah_hud_" + id + "_y", 0f); } AHPrefs.SetFloat("ah_hud_scale", 1f); }
        else if (what == "ar") AHLoc.Lang = "ar";
        else if (what == "en") AHLoc.Lang = "en";
        else if (what == "bosstest")
        {
            var g = AHGame.I; AHMobType t = null;
            foreach (var m in g.mobs) if (m != null && !m.dead && m.type != null) { t = m.type; break; }
            if (t != null)
            {
                var pt = g.player.transform; Vector3 fw = pt.forward; fw.y = 0f; fw.Normalize();
                var et = new AHMobType { id = t.id, name = "Old Tusk the Hollow King", model = t.model, lvl = t.lvl + 4, hp = t.hp * 8, dmg = t.dmg, xp = t.xp * 5, speed = t.speed, radius = t.radius, aggro = t.aggro, atkCd = t.atkCd, respawn = 9999f, elite = true, drops = t.drops };
                var b = AHMob.Create(g, et, pt.position + fw * 7f); g.mobs.Add(b);
                g.player.target = b; b.hp = et.hp * 0.62f;
            }
        }
        else if (what.StartsWith("hub:")) AHGame.I.ui.OpenHubCat(what.Substring(4));
        else if (what.StartsWith("tile:")) AHGame.I.ui.TestTile(what.Substring(5));
        else if (what == "dice") AHTable.OpenDice(AHGame.I);
        else if (what == "arm") AHTable.OpenArm(AHGame.I);
        else if (what == "duel") AHRanked.Begin(AHGame.I, AHRanked.Challengers()[0], false);
        else if (what == "golden")
        {
            // the nearest live beast turns golden now
            var g = AHGame.I; AHMob best = null; float bd = 1e9f;
            foreach (var m in g.mobs) { if (m == null || m.dead || m.golden) continue; float d = (m.transform.position - g.player.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
            if (best != null) AHGolden.Roll(g, best, true);
        }
        else if (what == "wish") { AHPrefs.SetString("ah_wish_day", ""); I.Wish(); }
    }
}
