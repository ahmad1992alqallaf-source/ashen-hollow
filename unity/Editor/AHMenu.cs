// Ashen Hollow: one menu item that makes the materials and a ready-to-play scene
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AHMenu
{
    // new shaders get their material in Resources/AH/Materials by themselves (no need to run the setup again)
    static AHMenu()
    {
        EditorApplication.delayCall += () =>
        {
            string matDir = Root() + "/Resources/AH/Materials";
            if (!AssetDatabase.IsValidFolder(matDir)) return;
            foreach (var n in new[] { "Water", "Sky", "Fx", "Grass", "Ghost", "Ground", "Spark" })
                if (AssetDatabase.LoadAssetAtPath<Material>(matDir + "/" + n + ".mat") == null && Shader.Find("AshenHollow/" + n) != null) MakeMat("AshenHollow/" + n, matDir + "/" + n + ".mat");
            AssetDatabase.SaveAssets();
        };
    }

    // the AshenHollow folder, wherever it was put inside Assets
    static string Root()
    {
        foreach (var guid in AssetDatabase.FindAssets("AHGame t:MonoScript"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith("/Scripts/AHGame.cs")) return p.Substring(0, p.Length - "/Scripts/AHGame.cs".Length);
        }
        return "Assets/AshenHollow";
    }

    [MenuItem("Ashen Hollow/Set Up Meadow Scene")]
    public static void Setup()
    {
        string root = Root(), ah = root + "/Resources/AH", matDir = ah + "/Materials";
        if (!AssetDatabase.IsValidFolder(matDir)) AssetDatabase.CreateFolder(ah, "Materials");
        bool ok = MakeMat("AshenHollow/Water", matDir + "/Water.mat");
        ok &= MakeMat("AshenHollow/Sky", matDir + "/Sky.mat");
        ok &= MakeMat("AshenHollow/Fx", matDir + "/Fx.mat");
        ok &= MakeMat("AshenHollow/Grass", matDir + "/Grass.mat");
        ok &= MakeMat("AshenHollow/Ghost", matDir + "/Ghost.mat");
        ok &= MakeMat("AshenHollow/Ground", matDir + "/Ground.mat");
        ok &= MakeMat("AshenHollow/Spark", matDir + "/Spark.mat");
        AssetDatabase.SaveAssets();
        if (Resources.Load<GameObject>("AH/Areas/meadow_world_kk") == null && Resources.Load<GameObject>("AH/Areas/meadow_world") == null && Resources.Load<GameObject>("AH/meadow_world") == null)
        {
            EditorUtility.DisplayDialog("Ashen Hollow", "The meadow model has not imported. Install the glTFast package first (Window > Package Manager > + > Install package by name > com.unity.cloud.gltfast), wait for it to finish, then run this again.", "OK");
            return;
        }
        string scenePath = root + "/AshenMeadow.unity";
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Ashen Hollow").AddComponent<AHGame>();
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        EditorUtility.DisplayDialog("Ashen Hollow", ok ? "Hollow Meadow is ready. Press Play." : "The scene is ready, but a shader did not compile. Check the Console.", "OK");
    }

    [MenuItem("Ashen Hollow/Reset Saved Progress")]
    public static void ResetProgress()
    {
        if (!EditorUtility.DisplayDialog("Ashen Hollow", "Clear the saved hero (class, level, gold, bag, gear, quests, dungeons and auction house)? This cannot be undone.", "Clear it", "Cancel")) return;
        AHSave.Clear();
        EditorUtility.DisplayDialog("Ashen Hollow", "Saved hero cleared (class, level, gold, bag, gear and quests). You will start fresh on the next Play.", "OK");
    }

    // testing helpers: only in the editor, only while playing
    [MenuItem("Ashen Hollow/Test: Give 3 Wolf Pelts")]
    public static void GivePelts()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        AHGame.I.player.bag.Add("wolf_pelt", 3);
        if (AHGame.I.ui != null) AHGame.I.ui.Toast("+3 Wolf Pelt (test)");
    }

    // fills in the current quest's objectives, as if you had done them (gathering and crafting arrive in step 7)
    [MenuItem("Ashen Hollow/Test: Finish Quest Objectives")]
    public static void FinishObjectives()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var g = AHGame.I; var log = g.quests; var q = log.Current;
        if (q == null || log.state != "active") { g.ui.Toast("No active quest (accept one from Captain Mara first)."); return; }
        foreach (var o in q.obj) log.Event(o.t, o.id, o.n, g);
    }

    // puts the hero next to each townsperson in turn (Captain Mara first)
    static int npcVisit;
    [MenuItem("Ashen Hollow/Test: Go To Next Townsperson")]
    public static void GoToNpc()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null || AHNpc.All.Count == 0) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var n = AHNpc.All[npcVisit++ % AHNpc.All.Count];
        var p = AHGame.I.player.transform;
        p.position = n.transform.position + n.transform.forward * 1.6f;
        p.rotation = AHGame.I.Face(n.transform.position - p.position);
        AHGame.I.ui.Toast("Test: next to " + n.npcName);
    }

    // puts the hero by one spot of each kind in turn: tree, copper, tin, herb, fishing, campfire, furnace, anvil, loom...
    static int spotVisit;
    [MenuItem("Ashen Hollow/Test: Cycle House Tier")]
    public static void CycleHouseTier()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null || AHGame.I.player.home == null) return;
        var H = AHGame.I.player.home; H.tier = H.tier % 3 + 1;
        var v = Object.FindAnyObjectByType<AHHomeView>(); if (v != null) v.Refresh();
        AHGame.I.ui.Toast("Test: house tier " + H.tier);
    }

    [MenuItem("Ashen Hollow/Test: Dump Old Shapes")]
    public static void DumpShapes()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.World == null) return;
        var sb = new System.Text.StringBuilder(); int n = 0;
        foreach (var r in AHGame.I.World.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r.name.StartsWith("AH_GROUND") || r.name.StartsWith("AH_WATER")) continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var m = mf.sharedMesh; long tris = 0; for (int k = 0; k < m.subMeshCount; k++) tris += m.GetIndexCount(k) / 3;
            var b = r.bounds; var mat = r.sharedMaterial; Color c = Color.white;
            if (mat != null) { if (mat.HasProperty("baseColorFactor")) c = mat.GetColor("baseColorFactor"); else if (mat.HasProperty("_BaseColor")) c = mat.GetColor("_BaseColor"); }
            var path = r.transform.parent != null ? r.transform.parent.name + "/" + r.name : r.name;
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\t{1:0.0}\t{2:0.0}\t{3:0.0}\t{4:0.00}\t{5:0.00}\t{6:0.00}\t{7}\t{8}\t{9:0.00},{10:0.00},{11:0.00}", path, b.center.x, b.center.y, b.center.z, b.size.x, b.size.y, b.size.z, tris, m.subMeshCount, c.r, c.g, c.b));
            n++;
        }
        System.IO.Directory.CreateDirectory("HeroShots");
        System.IO.File.WriteAllText("HeroShots/shapes_" + AHGame.AreaId + ".txt", sb.ToString());
        AHGame.I.ui.Toast("Test: " + n + " old shapes listed");
        Debug.Log("Shapes dumped " + AHGame.AreaId + " " + n);
    }

    [MenuItem("Ashen Hollow/Test: Go To Next Landmark")]
    public static void GoToLandmark()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null) return;
        var list = new System.Collections.Generic.List<Transform>();
        foreach (var n in new[] { "Bridges", "Ruins", "Crystals", "Palms", "Cacti", "Bones", "Coral", "Graves", "Tents", "Ships" }) { var r = GameObject.Find(n); if (r != null) foreach (Transform c in r.transform) list.Add(c); }
        foreach (var gr in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) { var go = gr.transform; if (go.name == "Castle" || go.name == "Palace" || go.name == "Windmill" || go.name == "Statue") list.Add(go); }
        if (list.Count == 0) { AHGame.I.ui.Toast("Test: no landmarks here"); return; }
        var t = list[landVisit++ % list.Count]; var p = AHGame.I.player.transform;
        // stand a few metres in front of it, between it and the camera, so the camera looks straight at it
        Vector3 f = AHGame.I.cam != null ? AHGame.I.cam.transform.forward : Vector3.forward; f.y = 0; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
        bool big = t.name == "Castle" || t.name == "Palace";
        p.position = AHGame.I.Resolve(big ? t.position + t.forward * 22f : t.position - f * (t.name == "ship" ? 14f : 5f), 0.3f); p.rotation = AHGame.I.Face(t.position - p.position);
        AHGame.I.ui.Toast("Test: at " + t.name);
    }
    static int landVisit;

    // six views of the whole area from above its edges and centre, into HeroShots/area_<id>.png (a 3x2 sheet)
    [MenuItem("Ashen Hollow/Test: Area Overview Shots")]
    static void AreaShots()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.data == null || g.data.bounds == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var b = g.data.bounds; float cx = (b.x0 + b.x1) / 2f, cz = (b.z0 + b.z1) / 2f, w = b.x1 - b.x0, d = b.z1 - b.z0;
        Vector3 C = g.W(cx, cz); float span = Mathf.Max(w, d);
        var eyes = new[] { g.W(b.x0 + w * 0.15f, b.z0 + d * 0.15f), g.W(b.x1 - w * 0.15f, b.z0 + d * 0.15f), g.W(b.x1 - w * 0.15f, b.z1 - d * 0.15f), g.W(b.x0 + w * 0.15f, b.z1 - d * 0.15f), g.W(cx, b.z0 + d * 0.05f), g.W(cx, b.z1 - d * 0.05f) };
        const int W = 640, H = 360;
        var sheet = new Texture2D(W * 3, H * 2, TextureFormat.RGB24, false);
        var go = new GameObject("AreaCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 55f; cam.farClipPlane = 600f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        for (int i = 0; i < 6; i++)
        {
            Vector3 e = eyes[i] + Vector3.up * Mathf.Clamp(span * 0.12f, 10f, 40f);
            go.transform.position = e; go.transform.LookAt(Vector3.Lerp(e, C, 0.6f) - Vector3.up * 0f + (C - e).normalized * 5f);
            go.transform.LookAt(new Vector3(C.x, 0f, C.z));
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels((i % 3) * W, (1 - i / 3) * H, W, H, tex.GetPixels());
        }
        RenderSettings.fog = fog;
        sheet.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "area_" + AHGame.AreaId + ".png"), sheet.EncodeToPNG());
        cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
        Debug.Log("Area overview " + AHGame.AreaId);
    }

    [MenuItem("Ashen Hollow/Test: Go To Next Work Spot")]
    public static void GoToSpot()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null || AHGather.Spots.Count == 0) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var pick = new System.Collections.Generic.List<AHSpot>(); var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var sp in AHGather.Spots) if (seen.Add(sp.kind + sp.type + (sp.kind == "use" ? sp.name : ""))) pick.Add(sp);
        var s = pick[spotVisit++ % pick.Count];
        var p = AHGame.I.player.transform;
        Vector3 d = p.position - s.pos; d.y = 0; if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
        p.position = s.pos + d.normalized * Mathf.Max(0.6f, Mathf.Min(s.reach * 0.6f, s.r + 0.7f));
        p.rotation = AHGame.I.Face(s.pos - p.position);
        AHGame.I.ui.Toast("Test: at " + s.name + " (" + s.kind + ")");
    }

    // walks the hero up to the next way out of this area (the road to the next region); keep walking forward to cross
    static readonly System.Collections.Generic.HashSet<string> exitSeen = new System.Collections.Generic.HashSet<string>();
    [MenuItem("Ashen Hollow/Test: Take Next Way Out")]
    public static void GoToExit()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null || g.data.exits == null || g.data.exits.Length == 0) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        // onward: the first open road to somewhere this test has not been yet, else the first open road
        exitSeen.Add(AHGame.AreaId);
        AHExit e = null;
        foreach (var c in g.data.exits) if (e == null && !exitSeen.Contains(c.to) && Resources.Load<TextAsset>("AH/Areas/" + c.to) != null) e = c;
        foreach (var c in g.data.exits) if (e == null && Resources.Load<TextAsset>("AH/Areas/" + c.to) != null) e = c;
        if (e == null) e = g.data.exits[0];
        Vector3 at = e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) / 2f, (e.z0 + e.z1) / 2f);
        Vector3 from = g.W((g.data.bounds.x0 + g.data.bounds.x1) / 2f, (g.data.bounds.z0 + g.data.bounds.z1) / 2f);
        Vector3 dir = at - from; dir.y = 0; dir.Normalize();
        var p = g.player.transform;
        p.position = at - dir * (e.r > 0f ? 1.5f : 0f);   // inside the way out: the game should take you through at once
        p.rotation = g.Face(dir);
        g.ui.Toast("Test: stepping onto the way to " + e.to + ".");
    }

    [MenuItem("Ashen Hollow/Test: Give Potions")]
    public static void GivePotions()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var b = AHGame.I.player.bag;
        b.Add("hp_potion", 3); b.Add("mana_potion", 2); b.Add("elixir_might", 1); b.Add("elixir_swift", 1); b.Add("enh_stone", 12); b.Add("lucky_charm", 1); b.Add("ruby", 2); b.Add("sapphire", 1); b.Add("card_wolf", 1);
        AHGame.I.ui.Toast("+3 health, +2 mana potions, Elixir of Might, Swiftness draught (test)");
    }

    [MenuItem("Ashen Hollow/Test: Give Meals")]
    public static void GiveMeals()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var b = AHGame.I.player.bag;
        b.Add("hunter_stew", 1); b.Add("steak_frites_fine", 1); b.Add("kings_feast_master", 1); b.Add("honey_porridge", 1);
        AHGame.I.ui.Toast("+ Hunter's stew, fine steak and chips, masterwork King's feast, honey porridge (test)");
    }

    // ---------- test: go straight to any exported area (Play mode) ----------
    static void TravelTo(string id)
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var ta = Resources.Load<TextAsset>("AH/Areas/" + id);
        if (ta == null) { EditorUtility.DisplayDialog("Ashen Hollow", id + " is not exported yet.", "OK"); return; }
        var d = JsonUtility.FromJson<AHWorldData>(ta.text);
        g.Travel(id, d.spawn.x, d.spawn.z, 0f);
    }
    [MenuItem("Ashen Hollow/Test: Give Money")]
    static void GiveMoney()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        g.player.bag.money += 50000L * AHDB.CU; g.player.bag.Touch(); g.MarkDirty();
        Debug.Log("Ashen Hollow: +" + AHItems.MoneyText(50000L * AHDB.CU));
    }
    [MenuItem("Ashen Hollow/Test: Open Auction House")]
    static void OpenAH()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        g.ui.OpenAH();
    }
    [MenuItem("Ashen Hollow/Test: Open Work Orders")]
    static void OpenOrders()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        g.ui.OpenOrders();
    }
    [MenuItem("Ashen Hollow/Test: Open Land Agent")]
    static void OpenAgent()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        g.ui.OpenFarmAgent();
    }
    // stand at the nearest shop door or bank vault in this area
    [MenuItem("Ashen Hollow/Test: Go To Next Shop or Plot")]
    static void NextShop()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) return;
        var list = AHGather.Spots.FindAll(s => s.kind == "use" || s.kind == "oven" || s.kind == "mill" || s.kind == "dairy" || s.kind == "spin" || s.kind == "compost");
        if (list.Count == 0) { Debug.Log("Ashen Hollow: no shop in " + g.data.region); return; }
        shopI = (shopI + 1) % list.Count; var sp = list[shopI];
        g.player.transform.position = g.Resolve(sp.pos + g.CamForward() * -1.2f, 0.35f);
        Debug.Log("Ashen Hollow: at " + sp.name);
    }
    static int shopI = -1;
    [MenuItem("Ashen Hollow/Test: Go To Waystone or Treasure")]
    static void ToWay()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) return;
        var list = AHGather.Spots.FindAll(s => s.name == "Waystone" || s.name == "Hidden treasure");
        if (list.Count == 0) { Debug.Log("Ashen Hollow: no waystone or treasure in " + g.data.region); return; }
        wayI = (wayI + 1) % list.Count; var sp = list[wayI];
        g.player.transform.position = g.Resolve(sp.pos + g.CamForward() * -1.8f, 0.35f);
        Debug.Log("Ashen Hollow: at " + sp.name);
    }
    static int wayI = -1;
    [MenuItem("Ashen Hollow/Test: Level Up +10")]
    static void LevelUp10()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) return;
        var p = g.player; int L = Mathf.Min(90, p.level + 10); var t = AHDB.ClAt;
        for (int lv = 10; lv <= L; lv += 10) if (!p.trialsDone.Contains(lv)) p.trialsDone.Add(lv);
        if (L < t.Length) p.ClassXp = t[L];
        p.Recalc(); p.hp = p.maxHp; g.ui.RefreshClass(); g.SaveProgress();
        g.ui.Banner("Level " + p.level, "Test level-up");
    }
    [MenuItem("Ashen Hollow/Test: Open Market Warden")]
    static void OpenWarden() { var g = AHGame.I; if (Application.isPlaying && g != null && g.ui != null) g.ui.OpenWarden(AHRep.At(g, g.player.transform.position) ?? "ashen"); }
    [MenuItem("Ashen Hollow/Test: Make It Night")] static void Night() { var g = AHGame.I; if (Application.isPlaying && g != null) g.SetTimeOfDay(0.75f); }
    [MenuItem("Ashen Hollow/Test: Make It Day")] static void Day() { var g = AHGame.I; if (Application.isPlaying && g != null) g.SetTimeOfDay(0.25f); }
    // renders the hero from the front, side and back into HeroShots/ next to Assets (for checking outfits up close)
    // the hero at work: stands at the next tree, ore rock, herb or fishing spot, starts the work and takes pictures
    static int workI;
    [MenuItem("Ashen Hollow/Test: Work Shots %&w")]
    static void WorkShots()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string[] kinds = { "chop", "mine", "herb", "fish" };
        for (int n = 0; n < kinds.Length; n++)
        {
            string k = kinds[workI++ % kinds.Length];
            AHSpot best = null; float bd = 1e9f;
            foreach (var sp in AHGather.Spots) { if (sp.kind != k || !AHGather.CanDo(g.player, sp, false)) continue; float d = (sp.pos - g.player.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = sp; } }
            if (best == null) continue;
            var p = g.player.transform; Vector3 dd = p.position - best.pos; dd.y = 0; if (dd.sqrMagnitude < 0.01f) dd = Vector3.forward;
            p.position = best.pos + dd.normalized * Mathf.Max(0.6f, Mathf.Min(best.reach * 0.6f, best.r + 0.7f));
            AHGather.Start(g.player, best);
            g.StartCoroutine(ShotLater(k));
            return;
        }
        Debug.Log("Ashen Hollow: no work spots here");
    }
    static System.Collections.IEnumerator ShotLater(string k)
    {
        yield return new WaitForSeconds(0.7f); HeroShots("work_" + k + "_a");
        yield return new WaitForSeconds(0.4f); HeroShots("work_" + k + "_b");
        AHGather.Cancel();   // pictures only: the work is not finished (no XP, nothing gathered)
    }

    // a weapon a little better than the one worn (to see the better-gear card); run again to take it back
    static string testGift;
    [MenuItem("Ashen Hollow/Test: Give Better Weapon")]
    static void GiveBetter()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player;
        if (testGift != null) { p.bag.Take(testGift); Debug.Log("Ashen Hollow: took back " + testGift); testGift = null; return; }
        var worn = AHItems.Get(p.bag.Worn("weapon")); float ws = AHUI.GearScore(worn); string best = null; float bs = 1e9f;
        foreach (var id in AHDB.Items.Keys)
        {
            var d = AHItems.Get(id); if (d == null || d.slot != "weapon" || d.cosmetic || !AHItems.CanUse(id, p.cls.id)) continue;
            float s = AHUI.GearScore(d); if (s > ws + 0.5f && s < bs) { bs = s; best = id; }
        }
        if (best == null) { Debug.Log("Ashen Hollow: nothing better found"); return; }
        p.bag.Add(best); testGift = best; Debug.Log("Ashen Hollow: gave " + best);
    }

    // takes back the last workshop level (and pays it back): undoes a mistaken test purchase
    [MenuItem("Ashen Hollow/Test: Open House Window")]
    static void TestHouse()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        g.ui.OpenFarmPlot("house");
        Debug.Log("Ashen Hollow: house window · homestead level " + AHTrophies.Level(g.player) + " · renown " + AHTrophies.Renown(g.player) + " · trophies " + AHTrophies.Count(g.player));
    }

    [MenuItem("Ashen Hollow/Test: Trophy Statues")]
    static void TestTrophies()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var stand = new GameObject("TrophyTestStand").transform; stand.position = new Vector3(5000f, -900f, 0f);
        var list = AHTrophies.Sample("troll", "alphawolf", "boarlord", "banditchief", "wyrm", "grull", "mirehulk", "saltbeard");
        var top = GameObject.CreatePrimitive(PrimitiveType.Cube); top.transform.SetParent(stand, false); top.transform.localPosition = new Vector3(0, 0.52f, 0); top.transform.localScale = new Vector3(list.Count * 0.7f + 0.4f, 1.04f, 0.7f);
        AHTrophies.BuildStand(g, stand, list);
        foreach (var t in stand.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 29;
        foreach (var r in stand.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = false;
        var cg = new GameObject("TrophyCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.68f); cam.cullingMask = 1 << 29;
        var lg = new GameObject("TrophyLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; li.cullingMask = 1 << 29; lg.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
        var c = stand.position + Vector3.up * 1.2f;
        cam.transform.position = c + new Vector3(0f, 1.0f, -6.5f); cam.transform.LookAt(c);
        var rt = new RenderTexture(1200, 600, 24); cam.targetTexture = rt; cam.Render();
        var tex = new Texture2D(1200, 600, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1200, 600), 0, 0); tex.Apply(); RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../HeroShots/trophies.png"), tex.EncodeToPNG());
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(stand.gameObject);
        Debug.Log("Ashen Hollow: trophy statues saved to HeroShots/trophies.png (" + list.Count + ")");
    }

    [MenuItem("Ashen Hollow/Test: Rare Recipes Check")]
    static void TestRare()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        int a = AHGather.Recipes("anvil").FindAll(r => r.rare).Count, l = AHGather.Recipes("loom").FindAll(r => r.rare).Count;
        var sc = AHItems.Get(AHRareRecipes.ScrollId);
        Debug.Log("Ashen Hollow: rare recipes anvil " + a + " loom " + l + " · known " + AHRareRecipes.KnownCount(g.player) + " / " + AHRareRecipes.Total + " · scroll item " + (sc != null ? sc.name : "MISSING"));
    }

    [MenuItem("Ashen Hollow/Test: World Event")]
    static void TestEvent()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        bool ok = AHSecrets.StartEvent(g, UnityEngine.Random.Range(0, 3));
        Vector3 at; string s = AHSecrets.NearestSecret(g, out at) ? " · nearest secret " + Mathf.RoundToInt((at - g.player.transform.position).magnitude) + " m away at " + at.ToString("F0") : " · no secrets left here";
        Debug.Log("Ashen Hollow: world event " + (ok ? "started" : "already running") + s + " · found " + AHSecrets.FoundHere() + " here");
    }

    [MenuItem("Ashen Hollow/Test: Treasure Goblin")]
    static void TestGoblin()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        Debug.Log("Ashen Hollow: treasure goblin " + (AHGoblin.Spawn(g) ? "spawned" : "not spawned (one is already out)"));
    }

    [MenuItem("Ashen Hollow/Test: Refund Kitchen Upgrade")]
    static void RefundKitchen()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player; var s = AHArtisan.St(p);
        if (s.kitchen <= 0) { Debug.Log("Ashen Hollow: no kitchen upgrade to refund"); return; }
        s.kitchen--; long c = AHArtisan.Cost(p, "kitchen"); p.bag.money += c; g.SaveProgress();
        Debug.Log("Ashen Hollow: kitchen upgrade refunded (" + AHItems.MoneyText(c) + ")");
    }

    [MenuItem("Ashen Hollow/Test: Hero Snapshots %&k")]
    static void HeroShots() { HeroShots("hero"); }
    static void HeroShots(string prefix)
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var hero = g.player.transform; string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var go = new GameObject("ShotCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f;
        var rt = new RenderTexture(640, 640, 24); cam.targetTexture = rt; var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
        string[] names = { "front", "side", "back", "threeq" }; float[] yaw = { 0f, 90f, 180f, 35f };
        for (int i = 0; i < names.Length; i++)
        {
            Vector3 dir2 = Quaternion.Euler(0, yaw[i], 0) * hero.forward;
            bool big = g.player.mounted; Vector3 at = hero.position + Vector3.up * (big ? 1.4f : 1.0f);
            cam.transform.position = at + dir2 * (big ? 7.5f : 4.2f) + Vector3.up * 0.35f; cam.transform.LookAt(at);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, prefix + "_" + names[i] + ".png"), tex.EncodeToPNG());
        }
        cam.transform.position = hero.position + Vector3.up * 26f - hero.forward * 6f; cam.transform.LookAt(hero.position); cam.fieldOfView = 50f;
        cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, prefix + "_top.png"), tex.EncodeToPNG());
        cam.transform.position = hero.position + Vector3.up * 55f - hero.forward * 40f; cam.transform.LookAt(hero.position + hero.forward * 8f); cam.fieldOfView = 55f;
        cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, prefix + "_wide.png"), tex.EncodeToPNG());
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(tex);
        Debug.Log("Hero snapshots saved to " + System.IO.Path.GetFullPath(dir));
    }
    static int lookI;
    [MenuItem("Ashen Hollow/Test: Next Cosmetic Look")]
    static void NextLook()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player;
        string[][] L =
        {
            new[] { "royal_helm", "sun_mantle", "royal_plate", "royal_cape", "royal_boots" },
            new[] { "gold_crown", "bloom_mantle", "bloom_robe", "wyrm_wings", "" },
            new[] { "corsair_hat", "", "corsair_coat", "captain_cape", "corsair_boots" },
            new[] { "bloom_hood", "", "bloom_robe", "bloom_cape", "" },
            new[] { "lava_horns", "sun_mantle", "", "magma_wings", "" },
        };
        var o = L[lookI++ % L.Length]; string[] sl = { "head", "shoulders", "chest", "cape", "feet" };
        for (int i = 0; i < 5; i++) { if (o[i] != "") p.bag.Add(o[i]); AHWardrobe.SetCos(p, sl[i], o[i] == "" ? null : o[i]); }
        p.CheckOutfit(); g.ui.Toast("Look " + lookI);
    }
    [MenuItem("Ashen Hollow/Test: Spark Check")]
    static void SparkCheck()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player.transform;
        AHSpark.Burst(p.position + Vector3.up * 1.2f + p.forward * 1.5f, new Color(1f, 0.6f, 0.2f), 120, 2.5f, 4f, 0.3f);
        AHSpark.LevelUp(p.position);
        var s = GameObject.Find("Sparks"); var r = s != null ? s.GetComponent<ParticleSystemRenderer>() : null;
        Debug.Log("Sparks: " + (s != null) + " particles " + (s != null ? s.GetComponent<ParticleSystem>().particleCount : -1) + " mat " + (r != null && r.sharedMaterial != null ? r.sharedMaterial.shader.name : "none"));
    }
    [MenuItem("Ashen Hollow/Test: Go To City House")]
    static void ToHouse()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var d = AHCityHouse.Door; if (!d.HasValue) { g.ui.Toast("Test: no city house here (travel to a city)."); return; }
        var p = g.player.transform; p.position = d.Value + (d.Value - p.position).normalized * 0f; g.ui.Toast("Test: at the city house");
    }
    [MenuItem("Ashen Hollow/Test: Give Phoenix Mount")]
    static void GivePhoenix()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player;
        if (!p.mounts.Contains(AHPhoenix.Id)) p.mounts.Add(AHPhoenix.Id);
        if (p.mounted) AHComp.Dismount(g, true);
        p.mountSel = AHPhoenix.Id; g.ui.RefreshRide(); AHComp.Mount(g);
    }
    // gives every mount and rides the next one in the list (for checking the seat and the gait)
    [MenuItem("Ashen Hollow/Test: Ride Next Mount")]
    static void NextMount()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player;
        var keys = new System.Collections.Generic.List<string>(AHComp.Mounts.Keys);
        foreach (var k in keys) if (!p.mounts.Contains(k)) p.mounts.Add(k);
        int i = p.mountSel == null ? 0 : (keys.IndexOf(p.mountSel) + 1) % keys.Count;
        if (p.mounted) AHComp.Dismount(g, true);
        p.mountSel = keys[i]; g.ui.RefreshRide(); AHComp.Mount(g);
        g.ui.Toast("Test: riding " + AHComp.MountName(keys[i]));
    }
    // gives every pet and brings out the next one
    [MenuItem("Ashen Hollow/Test: Next Pet")]
    static void NextPet()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player;
        var keys = new System.Collections.Generic.List<string>(AHDB.Table("companions", "PETS").Keys);
        foreach (var k in keys) if (!p.pets.Contains(k)) p.pets.Add(k);
        int i = p.pet == null ? 0 : (keys.IndexOf(p.pet) + 1) % keys.Count;
        p.pet = keys[i]; AHComp.SpawnPet(g);
        g.ui.Toast("Test: pet " + keys[i]);
    }
    [MenuItem("Ashen Hollow/Test: Go To Bandit or Pirate")]
    static void ToPerson()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        AHMob best = null; float bd = float.MaxValue; var p = g.player.transform;
        foreach (var m in g.mobs) { if (m == null || m.dead || !AHMobPeople.Is(m.type.id)) continue; float d = (m.transform.position - p.position).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
        if (best == null) { g.ui.Toast("Test: no bandits, pirates or cultists in this area."); return; }
        Vector3 at = best.transform.position + best.transform.forward * 3.5f; p.position = g.Resolve(at, 0.4f); p.rotation = g.Face(best.transform.position - p.position);
        g.ui.Toast("Test: near " + best.type.name);
    }
    // the nearest beast from four sides into HeroShots/mob_*.png (it is held still while the pictures are taken)
    [MenuItem("Ashen Hollow/Test: Monster Snapshots")]
    static void MobShots()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        AHMob best = null; float bd = float.MaxValue; var hp = g.player.transform.position;
        foreach (var m in g.mobs) { if (m == null || m.dead) continue; float d = (m.transform.position - hp).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
        if (best == null) return;
        var t = best.transform; float h = Mathf.Max(1.2f, best.height);
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var go = new GameObject("ShotCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f;
        var rt = new RenderTexture(640, 640, 24); cam.targetTexture = rt; var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
        string[] names = { "front", "side", "back", "threeq" }; float[] yaw = { 0f, 90f, 180f, 35f };
        for (int i = 0; i < names.Length; i++)
        {
            Vector3 at = t.position + Vector3.up * h * 0.55f, d2 = Quaternion.Euler(0, yaw[i], 0) * t.forward;
            cam.transform.position = at + d2 * (h * 2.4f + 1.5f) + Vector3.up * h * 0.2f; cam.transform.LookAt(at);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "mob_" + names[i] + ".png"), tex.EncodeToPNG());
        }
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(tex);
        Debug.Log("Monster snapshots of " + best.type.name);
    }
    // every new animal (monsters.json BEASTS) and every mount, each photographed close up into one contact sheet:
    // HeroShots/parade_beasts.png and parade_mounts.png (they are made far above the hero, then taken away again)
    [MenuItem("Ashen Hollow/Test: Sea and Swamp Beasts")]   // also the scorpions, Pterra, the wraiths and the bone sentinel
    static void SeaBeasts()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        string[] ids = { "crab", "reefcrab", "coralking", "turtle", "seaserpent", "bogtoad", "w_toad", "scorpion", "t_scorp", "pterra", "sandwraith", "fsentinel", "fnight", "raptor", "thickskull", "spikeback" };
        Vector3 sky = g.player.transform.position + Vector3.up * 400f;
        var shots = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < ids.Length; i++)
        {
            var t = new AHMobType { id = ids[i], name = ids[i], hp = 10, lvl = 1, model = "Mobs/" + ids[i], speed = 0, radius = 0.5f };
            var m = AHMob.Create(g, t, sky + Vector3.right * i * 40f); m.enabled = false; m.transform.rotation = Quaternion.Euler(0, ids[i] == "seaserpent" ? 125f : 200f, 0);
            if (m.anim != null) { m.anim.Play("Idle", true); m.anim.Tick(0.6f); }
            var sb = m.GetComponentInChildren<AHSerpentBody>(); if (sb != null) sb.Pose(1.3f, 0f, 0f); var sr = m.GetComponentInChildren<AHSerpentRig>(); if (sr != null) sr.PoseAt(1.1f, 1f);
            shots.Add(m.gameObject);
        }
        Sheet(dir, "sea_beasts", shots);
        Debug.Log("Sea and swamp beast snapshots");
    }
    // what the scene asks of the GPU: how many things are drawn, how many triangles, what is heaviest
    [MenuItem("Ashen Hollow/Test: Count Draw Load")]
    static void SceneStats()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        var cam = AHGame.I.cam; var planes = cam != null ? GeometryUtility.CalculateFrustumPlanes(cam) : null;
        long tris = 0, visTris = 0; int rend = 0, vis = 0, skinned = 0, lights = 0; var mats = new System.Collections.Generic.HashSet<Material>();
        var heavy = new System.Collections.Generic.Dictionary<string, long>();
        var allR = new System.Collections.Generic.List<Renderer>(); var allL = new System.Collections.Generic.List<Light>();
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) { allR.AddRange(go.GetComponentsInChildren<Renderer>(false)); allL.AddRange(go.GetComponentsInChildren<Light>(false)); }
        foreach (var r in allR)
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
            Mesh m = null; var mf = r.GetComponent<MeshFilter>(); if (mf != null) m = mf.sharedMesh; var sm = r as SkinnedMeshRenderer; if (sm != null) { m = sm.sharedMesh; skinned++; }
            if (m == null) continue; long t = 0; for (int k = 0; k < m.subMeshCount; k++) t += m.GetIndexCount(k) / 3;
            rend++; tris += t; foreach (var mm in r.sharedMaterials) if (mm != null) mats.Add(mm);
            bool seen = planes != null && GeometryUtility.TestPlanesAABB(planes, r.bounds) && (r.bounds.center - cam.transform.position).magnitude < 110f;
            if (seen) { vis++; visTris += t; }
            var root = r.transform; while (root.parent != null && root.parent.parent != null) root = root.parent;
            long cur; heavy.TryGetValue(root.name, out cur); heavy[root.name] = cur + t;
        }
        foreach (var l in allL) if (l.enabled) lights++;
        var top = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, long>>(heavy); top.Sort((a, b) => b.Value.CompareTo(a.Value));
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(AHGame.AreaId + ": " + rend + " renderers (" + skinned + " skinned), " + tris + " tris, " + mats.Count + " materials, " + lights + " lights; in view: " + vis + " renderers, " + visTris + " tris");
        for (int i = 0; i < Mathf.Min(15, top.Count); i++) sb.AppendLine("  " + top[i].Key + ": " + top[i].Value);
        System.IO.Directory.CreateDirectory("HeroShots"); System.IO.File.WriteAllText("HeroShots/stats_" + AHGame.AreaId + ".txt", sb.ToString());
        Debug.Log(sb.ToString());
    }

    // every monster of every area and dungeon, built as the game builds it, checked and photographed:
    // HeroShots/all_mobs_<n>.png sheets and all_mobs.txt (what model each uses, rigged or not, animated or not)
    [MenuItem("Ashen Hollow/Test: All Monsters Check")]
    static void AllMobs()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var types = new System.Collections.Generic.List<AHMobType>(); var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var ta in Resources.LoadAll<TextAsset>("AH/Areas"))
        {
            object d; try { d = AHJson.Parse(ta.text); } catch { continue; }
            var mts = AHJson.A(d, "mobTypes"); if (mts == null) continue;
            foreach (var o in mts)
            {
                string id = AHJson.S(o, "id"); if (id == null || !seen.Add(id)) continue;
                types.Add(new AHMobType { id = id, name = AHJson.S(o, "name", id), model = AHJson.S(o, "model"), hp = 10, lvl = 1, speed = 0, radius = 0.5f });
            }
        }
        types.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        Vector3 sky = g.player.transform.position + Vector3.up * 500f;
        var sb = new System.Text.StringBuilder(); var shots = new System.Collections.Generic.List<GameObject>(); int sheet = 0;
        for (int i = 0; i < types.Count; i++)
        {
            var t = types[i]; AHMob m = null;
            try { m = AHMob.Create(g, t, sky + Vector3.right * (i % 25) * 30f); } catch (System.Exception e) { sb.AppendLine(t.id + "\tERROR " + e.Message); continue; }
            m.enabled = false; m.transform.rotation = Quaternion.identity;
            if (m.anim != null && m.anim.HasClips) { m.anim.Play("Idle", true); m.anim.Tick(1.3f); }
            var sk = m.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length; var mr = m.GetComponentsInChildren<MeshRenderer>(true).Length;
            string file = m.transform.childCount > 0 ? m.transform.GetChild(0).name : "?";
            string kind = sk > 0 ? "rigged" : file.StartsWith("Mobs/") ? "OLD BLOCKS" : file.Contains("Capsule") ? "MISSING (capsule)" : "built";
            sb.AppendLine(t.id + "\t" + t.name + "\t" + (t.model ?? "") + " -> " + file + "\t" + kind + "\t" + (m.anim != null && m.anim.HasClips ? "animated" : "still") + "\tskinned " + sk + ", parts " + mr);
            shots.Add(m.gameObject);
            if (shots.Count == 25 || i == types.Count - 1) { Sheet(dir, "all_mobs_" + sheet, shots); sheet++; shots = new System.Collections.Generic.List<GameObject>(); }
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "all_mobs.txt"), sb.ToString());
        Debug.Log("All monsters check: " + types.Count + " types, " + sheet + " sheets");
    }

    // a monster stood a few metres in front of you, alive (breathing, idling) but harmless; again for the next one
    static readonly string[] showIds = { "bogtoad", "lurker", "bear", "croc", "ostrich", "ram", "m_flameguard", "emberwarden", "voidling", "grull", "pyraxis", "imp", "magmaimp", "troll", "f_hrimgar", "f_glacius", "lurker", "mirehulk", "bogmother", "w_bogking", "w_rotfang", "sandqueen", "thalassa", "voidmaw", "m_ignis" };
    static int showAt; static GameObject showGo;   // the list starts again after each script reload (new animals come first)
    [MenuItem("Ashen Hollow/Test: Display Monster Here")]
    static void ShowMonster()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        if (showGo != null) Object.Destroy(showGo);
        string id = showIds[showAt++ % showIds.Length]; AHMobType t = null;
        foreach (var ta in Resources.LoadAll<TextAsset>("AH/Areas"))
        {
            object d; try { d = AHJson.Parse(ta.text); } catch { continue; }
            var mts = AHJson.A(d, "mobTypes"); if (mts == null) continue;
            foreach (var o in mts) if (AHJson.S(o, "id") == id) { t = new AHMobType { id = id, name = AHJson.S(o, "name", id), model = AHJson.S(o, "model"), hp = 10, lvl = 1, radius = 0.5f }; break; }
            if (t != null) break;
        }
        if (t == null && AHJson.O(AHDB.Mobs, id) != null) t = new AHMobType { id = id, name = AHJson.S(AHJson.O(AHDB.Mobs, id), "name", id), hp = 10, lvl = 1, radius = 0.5f };   // one that only comes as a boss's add
        if (t == null) return;
        var p = g.player.transform; Vector3 f = g.cam != null ? -g.cam.transform.forward : -p.forward; f.y = 0; f.Normalize();
        var m = AHMob.Create(g, t, g.Resolve(p.position - f * 6f, 0.2f)); m.enabled = false;
        m.transform.rotation = Quaternion.LookRotation(f);
        var sa = m.gameObject.AddComponent<AHShowAnim>(); sa.anim = m.anim; sa.shotName = "show_" + id;
        showGo = m.gameObject; g.ui.Toast("Test: " + t.name);
    }

    [MenuItem("Ashen Hollow/Test: Farm Showcase")]
    static void FarmShowcase()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var shots = AHHomeView.Showcase(g, g.player.transform.position + Vector3.up * 400f);
        Sheet(dir, "farm", shots);
        var sc = GameObject.Find("FarmShowcase"); if (sc != null) Object.Destroy(sc);
        Debug.Log("Farm showcase: " + shots.Count);
    }

    [MenuItem("Ashen Hollow/Test: Beast and Mount Parade")]
    static void Parade()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var beasts = AHDB.Table("monsters", "BEASTS") as System.Collections.Generic.Dictionary<string, object>;
        var ids = new System.Collections.Generic.List<string>(beasts.Keys);
        Vector3 sky = g.player.transform.position + Vector3.up * 400f;
        var shots = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < ids.Count; i++)
        {
            var t = new AHMobType { id = ids[i], name = ids[i], hp = 10, lvl = 1, model = "Mobs/" + ids[i], speed = 0, radius = 0.5f };
            var m = AHMob.Create(g, t, sky + Vector3.right * i * 30f); m.enabled = false; m.transform.rotation = Quaternion.identity;
            if (m.anim != null) { m.anim.Play("Idle", true); m.anim.Tick(1f); }
            shots.Add(m.gameObject);
        }
        Sheet(dir, "parade_beasts", shots);
        var mounts = AHComp.Mounts as System.Collections.Generic.Dictionary<string, object>;
        foreach (var k in mounts.Keys)
        {
            if (k == AHPhoenix.Id) continue;
            var h = new GameObject("Parade " + k); h.transform.position = sky + Vector3.right * shots.Count * 30f;
            AHAnim a; AHModel.Spawn(h.transform, "Comp/mount_" + k, 0f, false, 0f, out a);
            if (a != null && a.HasClips) { a.Play("Idle", true); a.Tick(1f); }
            shots.Add(h);
        }
        shots.RemoveRange(0, ids.Count);
        Sheet(dir, "parade_mounts", shots);
        Debug.Log("Parade snapshots: " + ids.Count + " beasts, " + shots.Count + " mounts");
    }
    // the townsfolk of this area, close up, into HeroShots/townsfolk.png (up to 20)
    [MenuItem("Ashen Hollow/Test: Townsfolk Snapshots")]
    static void Townsfolk()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var objs = new System.Collections.Generic.List<GameObject>();
        foreach (var n in AHNpc.All) { if (objs.Count >= 20) break; objs.Add(n.gameObject); }
        Sheet(dir, "townsfolk", objs, false);
        Debug.Log("Townsfolk snapshots: " + objs.Count);
    }
    // one of each herb, close up, into HeroShots/herbs.png
    [MenuItem("Ashen Hollow/Test: Herb Snapshots")]
    static void Herbs()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var objs = new System.Collections.Generic.List<GameObject>(); Vector3 sky = g.player.transform.position + Vector3.up * 400f; int i = 0;
        foreach (var t in new[] { "sunpetal", "ashbloom", "frostbloom", "emberthorn", "mireroot", "dragonfern", "reefmoss" })
        {
            Renderer b; GameObject f; Color c; var p = AHHerbs.Build(t, 7 + i, out b, out f, out c);
            p.transform.position = sky + Vector3.right * 20f * i++; objs.Add(p);
        }
        Sheet(dir, "herbs", objs);
        Debug.Log("Herb snapshots: " + objs.Count);
    }
    // a campfire, a furnace and an anvil, close up, into HeroShots/stations.png
    [MenuItem("Ashen Hollow/Test: Open Spellbook")]
    static void OpenBook()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.ui == null) return;
        g.ui.OpenClassWin("book");
    }
    [MenuItem("Ashen Hollow/Test: Cast Pose Snapshots")]
    static void PoseShots()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        if (g.player.mounted) AHComp.Dismount(g, true);
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        g.gameObject.AddComponent<AHPoseShots>().dir = dir;
    }
    [MenuItem("Ashen Hollow/Test: Station Snapshots")]
    static void Stations()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var holder = new GameObject("Station shots").transform; Vector3 sky = g.player.transform.position + Vector3.up * 400f;
        var objs = new System.Collections.Generic.List<GameObject> { AHStations.Campfire(holder, 1.25f, 3), AHStations.Furnace(holder, 1.6f, 1.25f, 5), AHStations.Anvil(holder, 1.25f), AHStations.Fountain(holder, 2.6f), AHStations.Brazier(holder, 1.7f, 9), AHStations.Banner(holder, new Color(0.64f, 0.07f, 0.12f)), AHStations.Column(holder, 6.2f, 3), AHStations.Throne(holder), AHTents.Pavilion(holder, 4), AHTents.Ridge(holder, 7), AHScenery.Bridge(holder, 6.2f, 5.6f, 3), AHScenery.Ruin(holder, 5.4f, new Color(0.58f, 0.39f, 0.18f), 11), AHScenery.Grave(holder, 1), AHScenery.Grave(holder, 2), AHScenery.Grave(holder, 5), AHScenery.Wreck(holder, 7) };
        for (int i = 0; i < objs.Count; i++) objs[i].transform.position = sky + Vector3.right * 20f * i;
        Sheet(dir, "stations", objs); Object.DestroyImmediate(holder.gameObject);
        Debug.Log("Station snapshots");
    }
    static void Sheet(string dir, string name, System.Collections.Generic.List<GameObject> objs, bool destroy = true)
    {
        const int S = 360; int cols = Mathf.Min(5, objs.Count), rows = (objs.Count + cols - 1) / cols;
        var sheet = new Texture2D(S * cols, S * rows, TextureFormat.RGB24, false);
        var go = new GameObject("ShotCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.7f, 0.76f);
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt; var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        var lg = new GameObject("ShotLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.1f; lg.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        for (int i = 0; i < objs.Count; i++)
        {
            var rs = objs[i].GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            var tr = objs[i].transform; Vector3 d = (tr.forward * 0.75f + tr.right * 0.55f + Vector3.up * 0.32f).normalized;
            cam.transform.position = b.center + d * (size * 2.1f + 0.5f); cam.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, S, S), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels((i % cols) * S, (rows - 1 - i / cols) * S, S, S, tex.GetPixels());
        }
        sheet.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), sheet.EncodeToPNG());
        cam.targetTexture = null; Object.DestroyImmediate(go); Object.DestroyImmediate(lg); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet);
        if (destroy) foreach (var o in objs) Object.DestroyImmediate(o);
    }
    // two floors of the Ashen Deep built far below the world (nothing awake, nothing to walk on), photographed from above
    // and close up: HeroShots/deep.png. The hero stays where they are.
    [MenuItem("Ashen Hollow/Test: Deep Floor Shots")]
    static void DeepShots()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var views = new List<AHDeepView>();
        int[] floors = { 2, 5 };
        for (int i = 0; i < floors.Length; i++)
        {
            var v = AHDeepView.Preview(g, 4242 + i * 17, floors[i]);
            Vector3 off = new Vector3(6000f + i * 400f, -1500f, 0f);
            v.transform.position += off; foreach (var e in v.Extra) if (e != null) e.transform.position += off;
            views.Add(v);
        }
        deepViews = views; deepFrame = Time.frameCount;
        EditorApplication.update -= DeepStep; EditorApplication.update += DeepStep;
    }
    static List<AHDeepView> deepViews; static int deepFrame;
    static void DeepStep()
    {
        if (!Application.isPlaying) { EditorApplication.update -= DeepStep; return; }
        if (Time.frameCount < deepFrame + 10) return;
        EditorApplication.update -= DeepStep;
        const int L = 29, W = 700, H = 700;
        foreach (var v in deepViews) { foreach (var t in v.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = L; foreach (var e in v.Extra) foreach (var t in e.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = L; }
        var cg = new GameObject("DeepCam"); var cam = cg.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.04f, 0.05f); cam.cullingMask = 1 << L; cam.farClipPlane = 600f;
        var lg = new GameObject("DeepLight"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 0.7f; li.cullingMask = 1 << L; lg.transform.rotation = Quaternion.Euler(60f, 30f, 0f);
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        var sheet = new Texture2D(W * 2, H * 2, TextureFormat.RGB24, false);
        for (int i = 0; i < deepViews.Count; i++)
        {
            var v = deepViews[i]; var rs = v.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            // from above
            cam.orthographic = true; cam.orthographicSize = Mathf.Max(b.size.x, b.size.z) * 0.55f;
            cam.transform.position = b.center + Vector3.up * 200f; cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels(i * W, H, W, H, tex.GetPixels());
            // close up: the busiest room, from a hero's camera height
            cam.orthographic = false; cam.fieldOfView = 55f;
            Vector3 focus = b.center; AHMob big = null;
            foreach (var e in v.Extra) { var m = e != null ? e.GetComponent<AHMob>() : null; if (m != null && (big == null || m.type.hp > big.type.hp)) big = m; }
            if (big != null) focus = big.transform.position;
            cam.transform.position = focus + new Vector3(0f, 7f, -11f); cam.transform.LookAt(focus + Vector3.up * 1f);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels(i * W, 0, W, H, tex.GetPixels());
        }
        sheet.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../HeroShots/deep.png"), sheet.EncodeToPNG());
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(lg); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet);
        foreach (var v in deepViews) { foreach (var e in v.Extra) if (e != null) Object.Destroy(e); Object.Destroy(v.gameObject); }
        deepViews = null;
        Debug.Log("Ashen Hollow: Deep floor shots saved to HeroShots/deep.png");
    }

    // the Ashen King story: play a chapter's opening cutscene without starting it (nothing is saved)
    [MenuItem("Ashen Hollow/Test: Story Cutscene Preview")]
    static void StoryCine()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var c = AHStory.Chapters[1];
        AHCine.Play(g, "Chapter 2 · " + c.name + " (preview)", c.intro, () => g.ui.Toast("Cutscene preview over. Nothing was changed.", 3f));
    }

    [MenuItem("Ashen Hollow/Test: Tale Cutscene Preview")]
    static void TaleCine()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        var t = AHTales.All[0]; var pt = t.parts[0];
        AHCine.Play(g, t.name + " · part 1 (preview)", pt.intro, () => AHCine.Play(g, null, pt.outro, () => g.ui.Toast("Tale preview over. Nothing was changed.", 3f)));
    }

    [MenuItem("Ashen Hollow/Test: House Interior Preview")]
    static void HousePrev()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        if (AHInterior.Inside) AHInterior.Leave(g); else AHInterior.Enter(g, true);
    }

    // the real guild hall (your own banners, orders and trophies); again to leave
    [MenuItem("Ashen Hollow/Test: Guild Hall Visit")]
    static void GuildVisit()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        if (AHInterior.Inside) AHInterior.Leave(g); else AHInterior.EnterGuild(g, false);
    }

    [MenuItem("Ashen Hollow/Test: Open Furnish Window")]
    static void FurnishWin() { var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; g.ui.OpenFurnish(); }

    [MenuItem("Ashen Hollow/Test: Win Derby")]
    static void WinDerby()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        AHDerby.TestWin(g); g.ui.OpenDerby();
    }

    [MenuItem("Ashen Hollow/Test: Win Cook-off")]
    static void WinCook()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        AHCookOff.TestWin(g); g.ui.OpenCookOff();
    }

    [MenuItem("Ashen Hollow/Test: Costume Gallery Preview")]
    static void GalleryPrev()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        AHGallery.Preview(g); Debug.Log("Ashen Hollow: gallery preview built in front of you (not saved)");
    }

    // opens the Wardrobe as it is (costumes at the top), changing nothing
    [MenuItem("Ashen Hollow/Test: Open Wardrobe")]
    static void OpenWard() { var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; g.ui.OpenWardrobe(); }
    [MenuItem("Ashen Hollow/Test: Give Cosmetics")]
    static void GiveCos()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; var p = g.player;
        foreach (var id in new[] { "royal_helm", "royal_plate", "royal_cape", "royal_boots", "bloom_mantle", "gold_crown", "wyrm_wings", "corsair_hat", "lava_horns", "bloom_robe", "bloom_hood", "sun_mantle", "captain_cape" }) p.bag.Add(id);
        AHWardrobe.SetCos(p, "head", "royal_helm"); AHWardrobe.SetCos(p, "shoulders", "sun_mantle"); AHWardrobe.SetCos(p, "chest", "royal_plate");
        AHWardrobe.SetCos(p, "cape", "royal_cape"); AHWardrobe.SetCos(p, "feet", "royal_boots");
        p.CheckOutfit(); g.ui.OpenWardrobe();
    }
    [MenuItem("Ashen Hollow/Test: Go To Bounty Board")]
    static void ToBoard()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) return;
        var sp = AHGather.Spots.Find(s => s.name == "Bounty board");
        if (sp == null) { Debug.Log("Ashen Hollow: no bounty board in " + g.data.region); return; }
        g.player.transform.position = g.Resolve(sp.pos + g.CamForward() * -1.6f, 0.35f);
    }
    [MenuItem("Ashen Hollow/Dungeons (test)/The Sunken Forge")] static void D0() { TravelTo("forge"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/The Mire Warrens")] static void D1() { TravelTo("d_warrens"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/Frostpeak Caverns")] static void D2() { TravelTo("d_frost"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/Tomb of the Sun King")] static void D3() { TravelTo("d_tomb"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/Molten Depths")] static void D4() { TravelTo("d_molten"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/The Hollow Below")] static void D5() { TravelTo("d_hollow"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/Kingdom Quest: Siege")] static void KQS() { var g = AHGame.I; if (Application.isPlaying && g != null) AHKQ.StartTest(g, "siege"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/Kingdom Quest: Gold Rush")] static void KQR() { var g = AHGame.I; if (Application.isPlaying && g != null) AHKQ.StartTest(g, "rush"); }
    [MenuItem("Ashen Hollow/Dungeons (test)/The Ember Throne (raid)")] static void DRaid() { var g = AHGame.I; if (!Application.isPlaying || g == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; } AHRaid.Enter(g); }
    [MenuItem("Ashen Hollow/Dungeons (test)/Clear Dungeon Seals")] static void D6() { AHDungeon.ResetAll(); Debug.Log("Ashen Hollow: dungeon seals cleared"); }
    // defeat the nearest beast (to try boss fights, gates and chests quickly)
    [MenuItem("Ashen Hollow/Dungeons (test)/Defeat Nearest Beast")]
    static void D7()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        AHMob best = null; float bd = float.MaxValue;
        foreach (var m in g.mobs) { if (m.dead) continue; float d = (m.transform.position - g.player.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
        if (best != null) best.Hurt(999999, g.player, true);
    }
    // stand at a dungeon door in this area (or the Sunken Forge's portal)
    [MenuItem("Ashen Hollow/Dungeons (test)/Go To Dungeon Door")]
    static void D9()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) return;
        foreach (var s in AHGather.Spots)
            if (s.kind == "gate" && s.gate != null && s.gate.dung != null) { g.player.transform.position = g.Resolve(s.pos + g.CamForward() * -2.5f, 0.35f); Debug.Log("Ashen Hollow: at " + s.name); return; }
        Debug.Log("Ashen Hollow: no dungeon door in " + g.data.region);
    }
    // bring the hero next to the beast (or boss) furthest into the dungeon
    [MenuItem("Ashen Hollow/Dungeons (test)/Go To Boss")]
    static void D8()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) return;
        AHMob best = null;
        foreach (var m in g.mobs) if (!m.dead && (best == null || m.type.hp > best.type.hp)) best = m;
        if (best != null) g.player.transform.position = g.Resolve(best.transform.position + (g.player.transform.position - best.transform.position).normalized * 6f, 0.35f);
    }
    [MenuItem("Ashen Hollow/Travel (test)/Your Homestead")] static void THome() { var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return; if (g.player.home == null) { Debug.Log("Ashen Hollow: buy the deed first (Test: Open Land Agent)"); return; } AHHome.TravelHome(g); }
    [MenuItem("Ashen Hollow/Travel (test)/Hollow Meadow")] static void T0() { TravelTo("meadow"); }
    [MenuItem("Ashen Hollow/Travel (test)/Old Mill Road")] static void T1() { TravelTo("mill"); }
    [MenuItem("Ashen Hollow/Travel (test)/Silkwood")] static void T2() { TravelTo("silkwood"); }
    [MenuItem("Ashen Hollow/Travel (test)/Duskmire")] static void T3() { TravelTo("mire"); }
    [MenuItem("Ashen Hollow/Travel (test)/Kingsvale")] static void T4() { TravelTo("vale"); }
    [MenuItem("Ashen Hollow/Travel (test)/Varrow")] static void T5() { TravelTo("city"); }
    [MenuItem("Ashen Hollow/Travel (test)/Frostfang Reach")] static void T6() { TravelTo("frost"); }
    [MenuItem("Ashen Hollow/Travel (test)/Highcairn")] static void T7() { TravelTo("hc_city"); }
    [MenuItem("Ashen Hollow/Travel (test)/Sunscar Wastes")] static void T8() { TravelTo("sands"); }
    [MenuItem("Ashen Hollow/Travel (test)/Sunspire Oasis")] static void T9() { TravelTo("ss_city"); }
    [MenuItem("Ashen Hollow/Travel (test)/Mirewatch")] static void T10() { TravelTo("mw_city"); }
    [MenuItem("Ashen Hollow/Travel (test)/Dragonscale Isle")] static void T11() { TravelTo("isle"); }
    [MenuItem("Ashen Hollow/Travel (test)/Tidewake Isles")] static void T12() { TravelTo("tide"); }
    [MenuItem("Ashen Hollow/Travel (test)/Coralport")] static void T13() { TravelTo("co_city"); }
    [MenuItem("Ashen Hollow/Travel (test)/Emberreach")] static void T14() { TravelTo("ember"); }
    [MenuItem("Ashen Hollow/Travel (test)/Cinderhold")] static void T15() { TravelTo("ch_city"); }
    [MenuItem("Ashen Hollow/Travel (test)/Fossil Lands")] static void T16() { TravelTo("fossil"); }

    static bool MakeMat(string shaderName, string path)
    {
        Shader sh = Shader.Find(shaderName);
        if (sh == null) { Debug.LogError("Ashen Hollow: shader " + shaderName + " not found"); return false; }
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.shader = sh; EditorUtility.SetDirty(existing); return true; }
        AssetDatabase.CreateAsset(new Material(sh), path);
        return true;
    }

    // ---------- every building and tent in every area: in-place pictures, and a list of any old block shapes left ----------
    static readonly string[] sweepAreas = { "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "meadow" };
    static readonly string[] sweepTowns = { "city", "hc_city", "ss_city", "mw_city", "co_city", "ch_city", "vale", "kingsroad", "ember", "frost", "sands", "tide", "meadow" };
    static string[] sweepList = sweepAreas;
    static int sweepAt = -1; static double sweepT; static bool sweepShot;
    [MenuItem("Ashen Hollow/Test: Building Sweep (towns)")]
    static void BuildingSweepTowns() { sweepList = sweepTowns; StartSweep(); }
    [MenuItem("Ashen Hollow/Test: Building Sweep (all areas)")]
    static void BuildingSweep()
    {
        if (!Application.isPlaying || AHGame.I == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        sweepList = sweepAreas; StartSweep();
    }
    static void StartSweep()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        System.IO.File.WriteAllText("HeroShots/builds_all.txt", "");
        sweepAt = 0; sweepShot = false; EditorApplication.update -= SweepTick; EditorApplication.update += SweepTick; TravelTo(sweepList[0]); sweepT = EditorApplication.timeSinceStartup;
    }
    [MenuItem("Ashen Hollow/Test: Building Shots Here")]
    static void BuildingShotsHere() { if (Application.isPlaying && AHGame.I != null) BuildingShots(AHGame.I); }
    static void SweepTick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= SweepTick; sweepAt = -1; return; }
        var g = AHGame.I; double now = EditorApplication.timeSinceStartup;
        if (g == null || g.World == null || AHGame.AreaId != sweepList[sweepAt]) { sweepT = now; return; }
        if (!sweepShot && now - sweepT > 7.0) { sweepShot = true; BuildingShots(g); sweepT = now; return; }
        if (sweepShot && now - sweepT > 1.0)
        {
            sweepAt++; sweepShot = false;
            if (sweepAt >= sweepList.Length) { EditorApplication.update -= SweepTick; sweepAt = -1; Debug.Log("Ashen Hollow: building sweep done"); return; }
            TravelTo(sweepList[sweepAt]); sweepT = now;
        }
    }
    static void BuildingShots(AHGame g)
    {
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        var news = new System.Collections.Generic.List<Transform>();
        foreach (var gr in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var t = gr.transform; string n = t.name;
            if (n == "City buildings") { foreach (Transform c in t) if (c.name.StartsWith("Townhouse_") || c.name.StartsWith("Building_")) news.Add(c); }
            else if (n == "Stalls and wells" || n == "Tents") { foreach (Transform c in t) if (c.GetComponentInChildren<Renderer>() != null && c.name != "Street lamp") news.Add(c); }
            else if (n == "Castle" || n == "Palace" || n == "Windmill" || n == "Statue" || n == "Bank vault" || n == "Homestead") news.Add(t);
        }
        // old shapes still drawn: tall, house-sized things in the area's own model, grouped where they touch
        var boxes = new System.Collections.Generic.List<Bounds>(); var tris = new System.Collections.Generic.List<int>();
        if (g.World != null)
            foreach (var r in g.World.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled || r.forceRenderingOff || r.name.StartsWith("AH_")) continue;
                var b = r.bounds; float foot = Mathf.Max(b.size.x, b.size.z);
                if (b.size.y < 1.6f || foot < 1.6f || foot > 26f || b.size.y > 20f) continue;
                var mf = r.GetComponent<MeshFilter>(); int tc = 0; if (mf != null && mf.sharedMesh != null) for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) tc += (int)(mf.sharedMesh.GetIndexCount(k) / 3);
                bool merged = false;
                for (int i = 0; i < boxes.Count; i++) { var e = boxes[i]; e.Expand(new Vector3(1.0f, 0f, 1.0f)); if (e.Intersects(b)) { var m = boxes[i]; m.Encapsulate(b); boxes[i] = m; tris[i] += tc; merged = true; break; } }
                if (!merged) { boxes.Add(b); tris.Add(tc); }
            }
        var sb = new System.Text.StringBuilder(); sb.AppendLine("== " + AHGame.AreaId + "  new: " + news.Count + "  old shapes: " + boxes.Count);
        for (int i = 0; i < news.Count; i++) { var p = g.ToWeb(news[i].position); sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "  N{0}\t{1}\t{2:0},{3:0}", i, news[i].name, p.x, p.y)); }
        for (int i = 0; i < boxes.Count; i++) { var p = g.ToWeb(boxes[i].center); sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "  O{0}\t{1:0.0}x{2:0.0}x{3:0.0}\t{4} tris\t{5:0},{6:0}", i, boxes[i].size.x, boxes[i].size.y, boxes[i].size.z, tris[i], p.x, p.y)); }
        System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "builds_all.txt"), sb.ToString());
        var nb = new System.Collections.Generic.List<Bounds>(); var nf = new System.Collections.Generic.List<Vector3>();
        foreach (var t in news) { var rs = t.GetComponentsInChildren<Renderer>(); Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); nb.Add(b); nf.Add(t.forward); }
        Vector3 mid = g.W(g.data.spawn.x, g.data.spawn.z);
        var of = new System.Collections.Generic.List<Vector3>(); foreach (var b in boxes) { var d = mid - b.center; d.y = 0; of.Add(d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward); }
        for (int k = 0; k * 30 < nb.Count; k++) PlaceSheet(dir, "build_" + AHGame.AreaId + "_new" + (k > 0 ? "_" + k : ""), nb.GetRange(k * 30, Mathf.Min(30, nb.Count - k * 30)), nf.GetRange(k * 30, Mathf.Min(30, nb.Count - k * 30)), g);
        if (boxes.Count > 0) PlaceSheet(dir, "build_" + AHGame.AreaId + "_old", boxes, of, g);
        Debug.Log("Ashen Hollow: buildings " + AHGame.AreaId + " new " + news.Count + " old " + boxes.Count);
    }
    // pictures of things where they stand (with their surroundings), up to 30 to a sheet
    static void PlaceSheet(string dir, string name, System.Collections.Generic.List<Bounds> bs, System.Collections.Generic.List<Vector3> fwd, AHGame g)
    {
        const int S = 300; int n = Mathf.Min(30, bs.Count), cols = Mathf.Min(6, n), rows = (n + cols - 1) / cols;
        var sheet = new Texture2D(S * cols, S * rows, TextureFormat.RGB24, false);
        var go = new GameObject("PlaceCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 45f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 400f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; }
        var rt = new RenderTexture(S, S, 24); cam.targetTexture = rt; var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        for (int i = 0; i < n; i++)
        {
            var b = bs[i]; float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            var f = fwd[i]; f.y = 0; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
            Vector3 d = (f * 0.75f + Vector3.Cross(Vector3.up, f) * 0.35f + Vector3.up * 0.8f).normalized;
            go.transform.position = b.center + d * (size * 1.25f + 3f); go.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, S, S), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels((i % cols) * S, (rows - 1 - i / cols) * S, S, S, tex.GetPixels());
        }
        RenderSettings.fog = fog; sheet.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), sheet.EncodeToPNG());
        cam.targetTexture = null; Object.DestroyImmediate(go); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet);
    }

    // what stands just around each shop building (to find stray flat pieces)
    [MenuItem("Ashen Hollow/Test: Around Buildings List")]
    static void ThingsAround()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        var sb = new System.Text.StringBuilder(); var seen = new System.Collections.Generic.HashSet<Renderer>();
        var all = new System.Collections.Generic.List<Renderer>();
        foreach (var gr in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) all.AddRange(gr.GetComponentsInChildren<Renderer>(false));
        // every flat sheet in the scene (a few triangles, thin, at least a metre across)
        foreach (var r in all)
        {
            if (!r.enabled || r is SkinnedMeshRenderer || r is ParticleSystemRenderer || r.name.StartsWith("AH_") || r.name == "Outskirts") continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue; var mesh = mf.sharedMesh;
            int tc = 0; for (int k = 0; k < mesh.subMeshCount; k++) tc += (int)(mesh.GetIndexCount(k) / 3);
            var rb = r.bounds; float big = Mathf.Max(rb.size.x, rb.size.z, rb.size.y);
            var ls = Vector3.Scale(mesh.bounds.size, r.transform.lossyScale); float thin = Mathf.Min(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            if (tc > 24 || big < 1.0f || thin > 0.12f) continue;
            string path = r.name; for (var t = r.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            var m = r.sharedMaterial; Color c = Color.white; if (m != null) { if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); }
            sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "FLAT {0}	{1} tris	{2:0.00}x{3:0.00}x{4:0.00}	mesh {5}	mat {6}	col {7:0.00},{8:0.00},{9:0.00}", path, tc, rb.size.x, rb.size.y, rb.size.z, mesh.name, m != null ? m.name : "-", c.r, c.g, c.b));
        }
        var cb = GameObject.Find("City buildings"); if (cb == null) { System.IO.File.WriteAllText("HeroShots/around_" + AHGame.AreaId + ".txt", sb.ToString()); return; }
        foreach (Transform b in cb.transform)
        {
            if (!b.name.StartsWith("Building_")) continue;
            var rs = b.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); bb.Expand(new Vector3(5f, 2f, 5f));
            foreach (var r in all)
            {
                if (!r.enabled || seen.Contains(r) || r is SkinnedMeshRenderer || r is ParticleSystemRenderer) continue;
                var rb = r.bounds; if (!bb.Intersects(rb)) continue;
                float big = Mathf.Max(rb.size.x, rb.size.z); if (big < 0.6f || big > 30f || r.name.StartsWith("AH_") || r.name == "Outskirts" || r.transform.IsChildOf(b)) continue;
                if (r.transform.root.name == "City buildings" && r.transform.parent != null && r.transform.parent.name.StartsWith("Townhouse_")) continue;
                var m = r.sharedMaterial; Color c = Color.white; if (m != null) { if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); }
                seen.Add(r);
                string path = r.name; for (var t = r.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\tnear {1}\t{2:0.00}x{3:0.00}x{4:0.00}\tcol {5:0.00},{6:0.00},{7:0.00}\tmat {8}", path, b.name, rb.size.x, rb.size.y, rb.size.z, c.r, c.g, c.b, m != null ? m.name : "-"));
            }
        }
        System.IO.File.WriteAllText("HeroShots/around_" + AHGame.AreaId + ".txt", sb.ToString());
        Debug.Log("Ashen Hollow: things around buildings listed " + seen.Count);
    }

    [MenuItem("Ashen Hollow/Test: Give Fossil Treasure Maps")]
    static void GiveTreasureMaps()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        g.player.bag.Add(AHTreasure.MapId, 3); g.player.bag.Touch(); g.ui.Toast("Test: 3 treasure maps");
    }
    [MenuItem("Ashen Hollow/Test: Go To Treasure X")]
    static void GoToX()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        foreach (var s in AHGather.Spots) if (s.name == "Dig for treasure") { g.player.transform.position = g.Resolve(s.pos + Vector3.right * 1.5f, 0.3f); g.ui.Toast("Test: at the X"); return; }
        g.ui.Toast("Test: no X here (read a map in the Fossil Lands)");
    }

    [MenuItem("Ashen Hollow/Test: Toggle Wider Areas")]
    static void ToggleSpread()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) return;
        AHGame.NoSpread = !AHGame.NoSpread; Debug.Log("Ashen Hollow: wider areas " + (AHGame.NoSpread ? "off" : "on"));
        TravelTo(AHGame.AreaId);
    }
    [MenuItem("Ashen Hollow/Test: Map Picture")]
    static void MapPicture()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) return;
        g.ui.OpenMap();
    }

    // the wider areas: an overview sheet and the inked map of each, into HeroShots/area_<id>.png and map_<id>.png
    static readonly string[] viewAreas = { "silkwood", "frost", "mire", "vale", "sands", "isle", "fossil", "ember", "tide", "city", "meadow" };
    static int viewAt = -1; static double viewT; static int viewStep;
    [MenuItem("Ashen Hollow/Test: Map Sweep (lands)")]
    static void MapSweep()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        viewAt = 0; viewStep = 0; EditorApplication.update -= ViewTick; EditorApplication.update += ViewTick; TravelTo(viewAreas[0]); viewT = EditorApplication.timeSinceStartup;
    }
    static void ViewTick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= ViewTick; viewAt = -1; return; }
        var g = AHGame.I; double now = EditorApplication.timeSinceStartup;
        if (g == null || g.World == null || AHGame.AreaId != viewAreas[viewAt]) { viewT = now; return; }
        if (viewStep == 0 && now - viewT > 7.0) { viewStep = 1; AreaShots(); g.ui.OpenMap(); viewT = now; return; }
        if (viewStep == 1 && now - viewT > 2.0)
        {
            viewStep = 2; var t = g.ui.MapTexture;
            if (t != null) { var c = new Texture2D(t.width, t.height, TextureFormat.RGB24, false); var px = t.GetPixels(); if (g.ui.MapFlipped) { int n = t.width; var f = new Color[px.Length]; for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) f[y * n + x] = px[y * n + (n - 1 - x)]; px = f; } c.SetPixels(px); c.Apply(); System.IO.File.WriteAllBytes("HeroShots/map_" + AHGame.AreaId + ".png", c.EncodeToPNG()); Object.Destroy(c); }
            g.ui.ShowWork(false); viewT = now; return;
        }
        if (viewStep == 2 && now - viewT > 0.5)
        {
            viewAt++; viewStep = 0;
            if (viewAt >= viewAreas.Length) { EditorApplication.update -= ViewTick; viewAt = -1; Debug.Log("Ashen Hollow: map sweep done"); return; }
            TravelTo(viewAreas[viewAt]); viewT = now;
        }
    }

    // in the Fossil Lands: read a map, walk to the X and dig (the result shows as a banner and in the Console)
    [MenuItem("Ashen Hollow/Test: Fossil Treasure Run")]   // or the land you are in, if it has maps
    static void TreasureRun()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string mid = AHTreasure.MapHere;
        if (mid == null) { TravelTo("fossil"); Debug.Log("Ashen Hollow: no treasure maps here; run this again once in the Fossil Lands"); return; }
        g.player.bag.Add(mid, 1); AHTreasure.Read(g, mid);
        foreach (var s in AHGather.Spots) if (s.name == "Dig for treasure") { g.player.transform.position = g.Resolve(s.pos + Vector3.right * 1.5f, 0.3f); if (s.use != null) s.use(); return; }
    }

    [MenuItem("Ashen Hollow/Test: Treasure Full Bag")]   // dig with a full bag, then with room
    static void TreasureFullBag()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var bag = g.player.bag; string mid = AHTreasure.MapHere;
        if (mid == null) { Debug.Log("Ashen Hollow: bag test: no treasure maps in this land"); return; }
        var stash = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>();
        System.Action<int> free = (k) => {
            for (int i = bag.order.Count - 1; i >= 0 && k > 0; i--) { string id = bag.order[i]; int n = bag.Count(id); if (n <= 0 || id == mid) continue; bag.Take(id, n); stash.Add(new System.Collections.Generic.KeyValuePair<string, int>(id, n)); k--; }
        };
        while (bag.Count(mid) <= 0 && !bag.Add(mid, 1)) free(1);
        AHTreasure.Read(g, mid);
        AHSpot s = null; foreach (var x in AHGather.Spots) if (x.name == "Dig for treasure") s = x;
        if (s == null) { Debug.Log("Ashen Hollow: bag test: no X placed"); return; }
        g.player.transform.position = g.Resolve(s.pos + Vector3.right * 1.5f, 0.3f);
        int used0 = bag.UsedSlots; long m0 = bag.money; g.ui.lastToast = null; s.use();
        Debug.Log("Ashen Hollow: bag test FULL: slots " + used0 + "/" + bag.SlotsMax + " -> toast '" + g.ui.lastToast + "', map kept " + (bag.Count(mid) > 0) + ", silver change " + (bag.money - m0));
        free(4);
        used0 = bag.UsedSlots; m0 = bag.money; g.ui.lastToast = null; s.use();
        Debug.Log("Ashen Hollow: bag test ROOM: slots " + used0 + " -> " + bag.UsedSlots + ", toast '" + g.ui.lastToast + "', map used " + (bag.Count(mid) <= 0) + ", silver change " + (bag.money - m0));
        string lost = "";
        foreach (var kv in stash) if (!bag.Add(kv.Key, kv.Value)) lost += kv.Key + " x" + kv.Value + " ";
        Debug.Log("Ashen Hollow: bag test: items put back" + (lost.Length > 0 ? ", no room for " + lost : " (all)"));
    }

    [MenuItem("Ashen Hollow/Test: Describe Captain Mara")]   // her model, renderers and where they are, to HeroShots/mara.txt
    public static void DescribeMara()
    {
        if (!Application.isPlaying) return;
        var go = GameObject.Find("Captain Mara"); var sb = new System.Text.StringBuilder();
        if (go == null) sb.AppendLine("no Captain Mara");
        else
        {
            sb.AppendLine("pos " + go.transform.position + " active " + go.activeInHierarchy + " children " + go.transform.childCount);
            foreach (Transform c in go.transform) sb.AppendLine(" child " + c.name + " active " + c.gameObject.activeSelf + " local " + c.localPosition + " scale " + c.localScale + " rot " + c.localEulerAngles);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                sb.AppendLine("  r " + r.name + " en " + r.enabled + " act " + r.gameObject.activeInHierarchy + " b " + r.bounds.center + " " + r.bounds.size + " mat " + (r.sharedMaterial != null ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "none"));
        }
        if (go != null)
        {
            // everything else whose box covers her spot
            Vector3 at = go.transform.position + Vector3.up;
            foreach (var top in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var r in top.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.transform.IsChildOf(go.transform)) continue;
                    var b = r.bounds; b.Expand(0.6f);
                    if (b.Contains(at)) sb.AppendLine("covers: " + top.name + " / " + (r.transform.parent != null ? r.transform.parent.name + "/" : "") + r.name + " b " + r.bounds.center + " " + r.bounds.size);
                }
        }
        System.IO.Directory.CreateDirectory("HeroShots");
        System.IO.File.WriteAllText("HeroShots/mara.txt", sb.ToString());
        Debug.Log("Mara described");
    }

    [MenuItem("Ashen Hollow/Test: Arrive In Meadow From Mill Road")]   // the same arrival as walking in from Old Mill Road
    public static void ArriveMeadow()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        AHGame.I.Travel("meadow", 89.6f, 53.12f, Mathf.PI);
    }

    [MenuItem("Ashen Hollow/Test: Describe Exits")]   // what stands at each way out of this land, to HeroShots/exits.txt
    public static void DescribeExits()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.data == null) return;
        var sb = new System.Text.StringBuilder();
        foreach (var e in g.data.exits)
        {
            Vector3 c = g.W(e.x, e.z); sb.AppendLine("exit to " + e.to + " at " + c);
            foreach (var top in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var r in top.GetComponentsInChildren<Renderer>(false))
                {
                    var b = r.bounds; Vector3 d = b.center - c; d.y = 0; if (d.magnitude > 9f || r.name.StartsWith("AH_GROUND") || Mathf.Max(b.size.x, b.size.z) > 20f) continue;
                    var mf = r.GetComponent<MeshFilter>(); int tri = mf != null && mf.sharedMesh != null ? mf.sharedMesh.triangles.Length / 3 : -1;
                    Color col = Color.white; var m = r.sharedMaterial; if (m != null) { if (m.HasProperty("baseColorFactor")) col = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) col = m.GetColor("_BaseColor"); }
                    sb.AppendLine("  " + top.name + " / " + (r.transform.parent != null ? r.transform.parent.name + "/" : "") + r.name + " c " + b.center + " s " + b.size + " tri " + tri + " col " + ColorUtility.ToHtmlStringRGB(col));
                }
        }
        System.IO.Directory.CreateDirectory("HeroShots"); System.IO.File.WriteAllText("HeroShots/exits.txt", sb.ToString());
        Debug.Log("Exits described");
    }

    [MenuItem("Ashen Hollow/Test: Toggle Fog %&g")]   // to see whether the haze is fog
    public static void ToggleFog()
    {
        RenderSettings.fog = !RenderSettings.fog;
        Debug.Log("Fog " + RenderSettings.fog + " mode " + RenderSettings.fogMode + " start " + RenderSettings.fogStartDistance + " end " + RenderSettings.fogEndDistance + " density " + RenderSettings.fogDensity + " colour " + RenderSettings.fogColor);
    }

    [MenuItem("Ashen Hollow/Test: Open Look Editor")]   // the mirror: change hair, skin, outfit on the running hero
    public static void OpenLook()
    {
        if (!Application.isPlaying || AHGame.I == null || AHGame.I.ui == null) return;
        AHGame.I.ui.OpenCreator("mirror");
    }

    [MenuItem("Ashen Hollow/Test: Go To Throne")]   // stand in front of the nearest throne and take a picture of it
    static void GoThrone()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var th = GameObject.Find("Ember Throne"); bool temp = false;
        if (th == null)
        {   // none in this land: put one down beside you for the picture
            th = AHStations.Throne(null); temp = true;
            Vector3 fw = g.player.transform.forward; fw.y = 0; fw.Normalize();
            th.transform.position = g.Resolve(g.player.transform.position + fw * 14f, 0.3f); th.transform.rotation = Quaternion.LookRotation(-fw);
        }
        Vector3 f = th.transform.forward; f.y = 0; f.Normalize();
        if (!temp) { g.player.transform.position = g.Resolve(th.transform.position + f * 9f, 0.3f); g.player.transform.rotation = Quaternion.LookRotation(-f); }
        var go = new GameObject("ThroneCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 40f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; }
        float s = th.transform.lossyScale.y;
        cam.transform.position = th.transform.position + f * 11f * s + Vector3.up * 4f * s + th.transform.right * 4f * s; cam.transform.LookAt(th.transform.position + Vector3.up * 3f * s);
        var rt = new RenderTexture(900, 700, 24); cam.targetTexture = rt; cam.Render();
        var tex = new Texture2D(900, 700, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 900, 700), 0, 0); tex.Apply(); RenderTexture.active = null;
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "throne_" + AHGame.AreaId + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(tex);
        if (temp) Object.Destroy(th);
        Debug.Log("Ashen Hollow: throne picture taken in " + AHGame.AreaId);
    }
    [MenuItem("Ashen Hollow/Test: Class Lineup %&l")]   // every class, man and woman, in starter outfit, to HeroShots/lineup_*.png
    static void ClassLineup() { var g = AHGame.I; if (Application.isPlaying && g != null && g.player != null) AHLineup.Run(g); }
    [MenuItem("Ashen Hollow/Test: HUD Shot %&h")]   // the game view with the HUD, to HeroShots/hud.png
    static void HudShot() { if (!Application.isPlaying) return; string d = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(d); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(d, "hud.png"), 2); Debug.Log("Ashen Hollow: HUD shot saved"); }
    [MenuItem("Ashen Hollow/Test: Open Mirror (look editor) %&m")]
    static void Mirror() { var g = AHGame.I; if (Application.isPlaying && g != null && g.ui != null) g.ui.OpenCreator("mirror"); }
    [MenuItem("Ashen Hollow/Test: Talent Tree %&t")]
    static void TalentTree() { var g = AHGame.I; if (Application.isPlaying && g != null && g.ui != null) g.ui.OpenClassWin("tal"); }
    [MenuItem("Ashen Hollow/Test: Toggle Auto Quest %&u")]
    static void AutoQ() { var g = AHGame.I; if (Application.isPlaying && g != null) AHAuto.Toggle(g); }
    [MenuItem("Ashen Hollow/Test: Horizon Shots %&j")]   // four views out from the hero at head height, to HeroShots/horizon.png
    static void HorizonShots()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        var go = new GameObject("HorizonCam"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(g.cam); cam.fieldOfView = 62f;
        int W = 800, H = 450; var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
        var sheet = new Texture2D(W * 2, H * 2, TextureFormat.RGB24, false); var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        for (int k = 0; k < 4; k++)
        {
            cam.transform.position = g.player.transform.position + Vector3.up * 5f;
            cam.transform.rotation = Quaternion.Euler(-4f, k * 90f, 0f);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels((k % 2) * W, (1 - k / 2) * H, W, H, tex.GetPixels());
        }
        sheet.Apply();
        string d = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(d);
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(d, "horizon.png"), sheet.EncodeToPNG());
        cam.targetTexture = null; Object.Destroy(go); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet);
        Debug.Log("Ashen Hollow: horizon shots saved");
    }
    [MenuItem("Ashen Hollow/Test: Class Feet Running %&y")]
    static void ClassFeet() { var g = AHGame.I; if (Application.isPlaying && g != null && g.player != null) AHLineup.Run(g, true); }
}
