// Ashen Hollow: making your hero, as in the web game's creator: class, body, face, hair, "How do you want to play?"
// (Adventurer or Artisan, with professions) and a name. Built in code like the rest of the HUD; works by touch and keyboard.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RectTransform crRoot, crPanel, crBody, crContent; float crScroll; Text crMore;
    readonly List<GameObject> crItems = new List<GameObject>();
    readonly Dictionary<string, Image> crTabImgs = new Dictionary<string, Image>();
    Text crTitle, crSub, crHint, crBeginText;
    // the barber's chair and the home mirror (web crMode 'barber' / 'mirror'): change your look for a price
    string crMode = "new"; AHLook crOrig; string crOrigName; RectTransform crCancel;
    static readonly Dictionary<string, string[]> LookGroups = new Dictionary<string, string[]>
    {
        { "hair", new[] { "hair", "beard", "hairCol", "hiCol", "cloth" } }, { "face", new[] { "eyeShape", "eye", "brow", "mouth", "nose", "ears", "mark", "markCol" } },
        { "body", new[] { "body", "height", "skin" } }, { "gender", new[] { "sex" } },
    };
    static readonly Dictionary<string, long> LookPrice = new Dictionary<string, long> { { "hair", 300 }, { "face", 400 }, { "body", 900 }, { "name", 1500 }, { "gender", 5000000 } };
    static readonly Dictionary<string, string> LookGName = new Dictionary<string, string> { { "hair", "Hair and outfit" }, { "face", "Face" }, { "body", "Body and skin" }, { "name", "Name" }, { "gender", "Gender" } };
    long CrCost(out string lines)
    {
        var p = g.player; long total = 0; var l = new List<string>();
        if (crMode != "barber" && crMode != "mirror") { lines = ""; return 0; }
        foreach (var kv in LookGroups)
        {
            bool ch = false; foreach (var k in kv.Value) if (LookGet(p.look, k) != LookGet(crOrig, k)) ch = true;
            if (!ch) continue;
            long pr = crMode == "mirror" && (kv.Key == "hair" || kv.Key == "face") ? 0 : LookPrice[kv.Key];
            total += pr; l.Add(LookGName[kv.Key] + " " + (pr > 0 ? AHItems.MoneyText(pr * AHDB.CU) : "free"));
        }
        if ((p.heroName ?? "") != (crOrigName ?? "")) { total += LookPrice["name"]; l.Add("Name " + AHItems.MoneyText(LookPrice["name"] * AHDB.CU)); }
        lines = string.Join(" · ", l.ToArray()); return total;
    }
    string crTab = "class";
    bool crTyping;
    TouchScreenKeyboard crKb;
    public bool CreatorOpen { get { return crRoot != null && crRoot.gameObject.activeSelf; } }

    static readonly string[] CrTabs = { "class", "body", "face", "hair", "path", "name" };
    static readonly string[] CrTabNames = { "Class", "Body", "Face", "Hair", "Path", "Name" };
    const float CrW = 600f, CrH = 600f;

    // web CR_TABS: [key, label, kind, list]
    static readonly string[][] CrRows =
    {
        new[] { "body", "sex", "Gender", "opt" }, new[] { "body", "body", "Body type", "opt" }, new[] { "body", "height", "Height", "opt" },
        new[] { "body", "skin", "Skin tone", "sw:SKIN" }, new[] { "body", "cloth", "Outfit colour", "cloth" },
        new[] { "face", "eyeShape", "Eye shape", "opt" }, new[] { "face", "eye", "Eye colour", "eye:EYE" }, new[] { "face", "brow", "Brows", "opt" },
        new[] { "face", "mouth", "Expression", "opt" }, new[] { "face", "nose", "Nose", "opt" }, new[] { "face", "ears", "Ears", "opt" },
        new[] { "face", "mark", "Face markings", "opt" }, new[] { "face", "markCol", "Marking colour", "sw:MARKC" },
        new[] { "hair", "hair", "Hairstyle", "opt" }, new[] { "hair", "hairCol", "Hair colour", "sw:HAIR" }, new[] { "hair", "hiCol", "Highlights", "hi:HAIR" },
        new[] { "hair", "beard", "Facial hair", "opt" },
    };

    void BuildCreator()
    {
        crRoot = Box("Creator", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        crRoot.anchorMin = Vector2.zero; crRoot.anchorMax = Vector2.one; crRoot.offsetMin = crRoot.offsetMax = Vector2.zero;
        // the panel sits on the right so the hero stays in view on the left
        Img("CreatorBack", crRoot, white, new Vector2(1f, 0.5f), new Vector2(-CrW / 2f - 16f, 0), new Vector2(CrW - 16, CrH - 16), new Color(0.085f, 0.065f, 0.05f, 0.97f));
        crPanel = Img("CreatorPanel", crRoot, white, new Vector2(1f, 0.5f), new Vector2(-CrW / 2f - 16f, 0), new Vector2(CrW, CrH), new Color(0.09f, 0.07f, 0.05f, 1f));
        Img("Edge", crPanel, white, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(CrW, 6), new Color(0.85f, 0.6f, 0.25f, 1f));
        crTitle = Label(crPanel, "Title", "Create your hero", 28, TextAnchor.UpperLeft, new Vector2(20, -14), new Vector2(560, 38), new Color(0.95f, 0.65f, 0.3f));
        crTitle.fontStyle = FontStyle.Bold;
        crSub = Label(crPanel, "Sub", "", 16, TextAnchor.UpperLeft, new Vector2(20, -50), new Vector2(560, 24), new Color(1f, 0.9f, 0.75f, 0.85f));
        for (int i = 0; i < CrTabs.Length; i++)
        {
            string k = CrTabs[i];
            var tab = Img("Tab_" + k, crPanel, white, new Vector2(0f, 1f), new Vector2(20 + 46 + i * 94, -96), new Vector2(90, 38), new Color(0.22f, 0.16f, 0.11f, 1f));
            Center(Label(tab, "T", CrTabNames[i], 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(90, 30), Color.white));
            crTabImgs[k] = tab.GetComponent<Image>();
            taps.Add(new TapBtn { rt = tab, layer = 4, act = () => { crTab = k; crTyping = false; crScroll = 0f; RenderCreator(); } });
        }
        crBody = Box("Body", crPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20, -124), new Vector2(560, 400));
        // the choices scroll inside the body: drag them up and down (or use the mouse wheel)
        crBody.gameObject.AddComponent<RectMask2D>();
        var hit = crBody.gameObject.AddComponent<Image>(); hit.color = new Color(0f, 0f, 0f, 0f);
        crContent = Box("Content", crBody, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(560, 400));
        crMore = Label(crPanel, "More", "▼ drag up for more", 14, TextAnchor.MiddleRight, new Vector2(400, -506), new Vector2(180, 20), new Color(1f, 0.85f, 0.4f, 0.9f));
        crHint = Label(crPanel, "Hint", "", 14, TextAnchor.UpperLeft, new Vector2(20, -528), new Vector2(380, 60), new Color(1f, 1f, 1f, 0.6f));
        crHint.horizontalOverflow = HorizontalWrapMode.Wrap;
        var begin = Img("Begin", crPanel, white, new Vector2(1f, 0f), new Vector2(-90, 40), new Vector2(160, 54), new Color(0.22f, 0.5f, 0.26f, 1f));
        var bt = Center(Label(begin, "T", "Begin", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(160, 40), Color.white)); bt.fontStyle = FontStyle.Bold; crBeginText = bt;
        crCancel = Img("Cancel", crPanel, white, new Vector2(1f, 0f), new Vector2(-260, 40), new Vector2(150, 54), new Color(0.45f, 0.22f, 0.17f, 1f));
        Center(Label(crCancel, "T", "Cancel", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 40), Color.white));
        taps.Add(new TapBtn { rt = crCancel, layer = 4, act = CancelCreator });
        crCancel.gameObject.SetActive(false);
        taps.Add(new TapBtn { rt = begin, layer = 4, act = FinishCreator });
        crRoot.gameObject.SetActive(false);
    }

    public void OpenCreator() { OpenCreator("new"); }
    public void OpenCreator(string mode)
    {
        ShowBag(false); ShowDialog(false); ShowWork(false);
        crMode = mode;
        bool paid = mode == "barber" || mode == "mirror";
        var p = g.player;
        if (paid) { crOrig = JsonUtility.FromJson<AHLook>(JsonUtility.ToJson(p.look)); crOrigName = p.heroName; }
        crRoot.gameObject.SetActive(true);
        crRoot.SetAsLastSibling();
        Time.timeScale = 0f;
        ptrs.Clear();
        crTab = paid ? "hair" : "class";
        foreach (var kv in crTabImgs) kv.Value.gameObject.SetActive(!paid || (kv.Key != "class" && kv.Key != "path"));
        crCancel.gameObject.SetActive(paid || (mode == "new" && AHPrefs.Slots().Count > 0 && !AHPrefs.Slots().Contains(AHPrefs.Slot)));
        crTitle.text = paid ? (mode == "mirror" ? "Your mirror" : "The barber’s chair") : "Create your hero";
        crBeginText.text = paid ? "Done" : "Begin";
        // the hint keeps clear of the Cancel button when it shows
        var hr = crHint.rectTransform; hr.sizeDelta = new Vector2(crCancel.gameObject.activeSelf ? 236f : 380f, 60f);
        RenderCreator();
    }

    void CancelCreator()
    {
        if (crMode == "new")
        {
            // a new hero not made after all: back to the hero select screen
            var l = AHPrefs.Slots(); if (l.Count == 0) return;
            crRoot.gameObject.SetActive(false); Time.timeScale = 1f; ptrs.Clear();
            AHPrefs.Use(l.Contains(AHPrefs.Prev) ? AHPrefs.Prev : l[0]);
            g.Reload(true, true);
            return;
        }
        var p = g.player;
        if (crOrig != null) { p.look = crOrig; p.heroName = crOrigName; p.ApplyLook(); }
        crTyping = false; if (crKb != null) { crKb.active = false; crKb = null; }
        crRoot.gameObject.SetActive(false); Time.timeScale = 1f; ptrs.Clear(); crMode = "new";
        foreach (var kv in crTabImgs) kv.Value.gameObject.SetActive(true);
    }

    void FinishCreator()
    {
        var p = g.player;
        if (crMode == "barber" || crMode == "mirror")
        {
            string lines; long c = CrCost(out lines);
            if (c * AHDB.CU > p.bag.money) { Toast("You can’t afford all of that. Change less, or come back richer."); return; }
            if (string.IsNullOrEmpty(p.heroName)) p.heroName = crOrigName;
            if (c > 0) { p.bag.money -= c * AHDB.CU; p.bag.Touch(); }
            crOrig = null; CancelCreator();
            RefreshClass(); g.SaveProgress();
            Toast(c > 0 ? "A new look, for " + AHItems.MoneyText(c * AHDB.CU) + "." : "Looking good.", 3f);
            return;
        }
        if (p.path == "artisan" && p.profs.Count == 0) { p.profs.Add("smith"); p.profs.Add("fisher"); }
        if (string.IsNullOrEmpty(p.heroName)) p.heroName = RandomName(p.look.sex);
        crTyping = false;
        if (crKb != null) { crKb.active = false; crKb = null; }
        crRoot.gameObject.SetActive(false);
        Time.timeScale = 1f;
        ptrs.Clear();
        g.FinishCreation();
    }

    // ---------- the tab contents ----------
    float crY;
    void CrClear()
    {
        foreach (var go in crItems) if (go != null) Destroy(go);
        crItems.Clear();
        RemoveTaps("cr");
        crY = 0f;
    }

    Text CrLabel(string s, int size, Color c, float h = 26f)
    {
        var t = Label(crContent, "L", s, size, TextAnchor.UpperLeft, new Vector2(0, -crY), new Vector2(560, h), c);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        crItems.Add(t.gameObject);
        crY += h;
        return t;
    }

    // a row of chips that wrap to the next line
    void CrChips(List<KeyValuePair<string, string>> items, Func<string, bool> on, Action<string> pick, Func<string, Color?> swatch = null)
    {
        float x = 0f, h = swatch != null ? 34f : 36f;
        foreach (var it in items)
        {
            string val = it.Key;
            Color? sw = swatch != null ? swatch(val) : null;
            float w = sw.HasValue ? 34f : Mathf.Max(56f, it.Value.Length * 9.5f + 26f);
            if (x + w > 560f) { x = 0f; crY += h + 6f; }
            bool sel = on(val);
            var chip = Img("Chip", crContent, sw.HasValue ? white : white, new Vector2(0f, 1f), new Vector2(x + w / 2f, -crY - h / 2f), new Vector2(w, h),
                sw.HasValue ? sw.Value : (sel ? new Color(0.62f, 0.42f, 0.18f, 1f) : new Color(0.24f, 0.18f, 0.13f, 1f)));
            if (sw.HasValue && sel) { var ring2 = Img("Sel", chip, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w + 10, h + 10), new Color(1f, 0.85f, 0.4f)); }
            else if (sw.HasValue) { Img("Edge", chip, white, new Vector2(0.5f, 0f), new Vector2(0, 1), new Vector2(w, 2), new Color(0f, 0f, 0f, 0.4f)); }
            if (!sw.HasValue) Center(Label(chip, "T", it.Value, 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(w, 30), Color.white));
            crItems.Add(chip.gameObject);
            taps.Add(new TapBtn { rt = chip, layer = 4, group = "cr", act = () => { pick(val); RenderCreator(); } });
            x += w + 6f;
        }
        crY += h + 10f;
    }

    static List<KeyValuePair<string, string>> Opts(string key)
    {
        var l = new List<KeyValuePair<string, string>>();
        var o = AHJson.A(AHJson.O(AHJson.O(AHDB.Rules, "LOOK"), "OPTS"), key);
        if (o != null) foreach (var r in o) { var row = r as List<object>; if (row == null || row.Count < 2) continue; l.Add(new KeyValuePair<string, string>(Convert.ToString(row[0], System.Globalization.CultureInfo.InvariantCulture), (string)row[1])); }
        return l;
    }

    static List<object> LookList(string name) { return AHJson.A(AHJson.O(AHDB.Rules, "LOOK"), name); }

    static string LookGet(AHLook L, string k)
    {
        var f = typeof(AHLook).GetField(k);
        return f == null ? "" : Convert.ToString(f.GetValue(L), System.Globalization.CultureInfo.InvariantCulture);
    }
    static void LookSet(AHLook L, string k, string v)
    {
        var f = typeof(AHLook).GetField(k);
        if (f == null) return;
        if (f.FieldType == typeof(int)) f.SetValue(L, int.Parse(v, System.Globalization.CultureInfo.InvariantCulture)); else f.SetValue(L, v);
    }

    void RenderCreator()
    {
        CrClear();
        var p = g.player;
        foreach (var kv in crTabImgs) kv.Value.color = kv.Key == crTab ? new Color(0.62f, 0.42f, 0.18f, 1f) : new Color(0.22f, 0.16f, 0.11f, 1f);
        crSub.text = p.cls.name + ". Make them yours.";
        if (crMode == "barber" || crMode == "mirror") { string lines; long c = CrCost(out lines); crSub.text = lines.Length > 0 ? lines + (c > 0 ? " · you have " + AHItems.MoneyText(p.bag.money) : "") : (crMode == "mirror" ? "Hair and face are free here; body, name and gender cost as at a barber." : "Hair 300 copper · face 400 · body 900 · name 1,500 · gender 5 gold."); }
        crHint.text = "";

        if (crTab == "class")
        {
            var items = new List<KeyValuePair<string, string>>();
            foreach (var c in AHClasses.All) items.Add(new KeyValuePair<string, string>(c.id, c.name));
            CrLabel("Class", 19, new Color(1f, 0.85f, 0.55f));
            CrChips(items, v => v == p.cls.id, v => { p.SetClass(v); RefreshClass(); });
            CrLabel(p.cls.role, 17, Color.white);
            var cd = AHJson.O(AHDB.Classes, p.cls.id);
            CrLabel((AHJson.B(cd, "noMana") ? "No mana. " : "Uses mana. ") + (AHJson.B(cd, "melee") ? "Fights up close." : "Fights from range."), 15, new Color(1f, 1f, 1f, 0.7f));
            CrY(8);
            CrLabel("Starting spells", 17, new Color(1f, 0.85f, 0.55f));
            var names = new List<string>(); foreach (var sp in p.cls.spells) names.Add(sp.name);
            CrLabel(string.Join(" · ", names.ToArray()), 15, Color.white, 44f);
            crHint.text = "You start in your class's own outfit. Spells and gear depend on the class.";
        }
        else if (crTab == "path")
        {
            var pl = AHDB.Table("paths", "PATHS_LIFE");
            CrLabel("How do you want to play?", 19, new Color(1f, 0.85f, 0.55f));
            var items = new List<KeyValuePair<string, string>>();
            foreach (var kv in pl) { var row = kv.Value as List<object>; items.Add(new KeyValuePair<string, string>(kv.Key, (string)row[0])); }
            CrChips(items, v => v == p.path, v =>
            {
                p.path = v; p.peace = -1;
                if (v == "artisan" && p.profs.Count == 0) { p.profs.Add("smith"); p.profs.Add("fisher"); }
            });
            var desc = AHJson.A(pl, p.path);
            CrLabel(desc != null && desc.Count > 1 ? (string)desc[1] : "", 15, Color.white, 46f);
            CrY(6);
            var profs = AHDB.Table("paths", "PROFS");
            var chosen = new List<string>(); foreach (var k in p.profs) chosen.Add(AHJson.S(AHJson.O(profs, k), "name", k));
            CrLabel("Your professions:  " + (chosen.Count > 0 ? string.Join(", ", chosen.ToArray()) : "none yet"), 18, new Color(1f, 0.85f, 0.55f));
            var pi = new List<KeyValuePair<string, string>>();
            foreach (var kv in profs) pi.Add(new KeyValuePair<string, string>(kv.Key, AHJson.S(kv.Value, "name", kv.Key)));
            CrChips(pi, v => p.profs.Contains(v), v =>
            {
                if (p.profs.Contains(v)) p.profs.Remove(v); else p.profs.Add(v);
                if (p.path == "artisan" && p.profs.Count == 0) p.profs.Add(v);
            });
            string last = p.profs.Count > 0 ? p.profs[p.profs.Count - 1] : null;
            if (last != null) CrLabel(AHJson.S(AHJson.O(profs, last), "blurb", ""), 14, new Color(1f, 1f, 1f, 0.75f), 40f);
            crHint.text = "Pick as many as you like" + (p.path == "artisan" ? " (at least one)" : ", or none") + ". You can take up more, or set some aside, at any Guild Registrar later.";
        }
        else if (crTab == "name")
        {
            CrLabel("Name", 19, new Color(1f, 0.85f, 0.55f));
            var box = Img("NameBox", crContent, white, new Vector2(0f, 1f), new Vector2(190, -crY - 24), new Vector2(380, 46), crTyping ? new Color(0.3f, 0.24f, 0.16f, 1f) : new Color(0.18f, 0.14f, 0.1f, 1f));
            var nt = Label(box, "T", (string.IsNullOrEmpty(p.heroName) ? (crTyping ? "" : "Your hero’s name") : p.heroName) + (crTyping ? "_" : ""), 22, TextAnchor.MiddleLeft, new Vector2(12, -8), new Vector2(360, 32),
                string.IsNullOrEmpty(p.heroName) && !crTyping ? new Color(1f, 1f, 1f, 0.4f) : Color.white);
            crItems.Add(box.gameObject);
            taps.Add(new TapBtn { rt = box, layer = 4, group = "cr", act = StartTyping });
            var rnd = Img("Random", crContent, white, new Vector2(0f, 1f), new Vector2(460, -crY - 24), new Vector2(150, 46), new Color(0.24f, 0.18f, 0.13f, 1f));
            Center(Label(rnd, "T", "Random", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150, 30), Color.white));
            crItems.Add(rnd.gameObject);
            taps.Add(new TapBtn { rt = rnd, layer = 4, group = "cr", act = () => { p.heroName = RandomName(p.look.sex); crTyping = false; RenderCreator(); } });
            crY += 60f;
            CrLabel("Up to 16 letters. It shows above your head.", 15, new Color(1f, 1f, 1f, 0.7f));
            crHint.text = crTyping ? "Type the name, then press Enter (or tap Begin)." : "Tap the box to type a name.";
        }
        else
        {
            var L = p.look;
            foreach (var row in CrRows)
            {
                if (row[0] != crTab) continue;
                string k = row[1], label = row[2], kind = row[3];
                if (k == "markCol" && (L.mark == "none" || L.mark == "scar" || L.mark == "freckles" || L.mark == "blush")) continue;
                CrLabel(label, 16, new Color(1f, 0.85f, 0.55f), 22f);
                if (kind == "opt") { CrChips(Opts(k), v => LookGet(L, k) == v, v => { LookSet(L, k, v); p.ApplyLook(); }); continue; }
                if (kind == "cloth")
                {
                    var cl = LookList("CLOTH"); var it = new List<KeyValuePair<string, string>>();
                    for (int i = 0; i < cl.Count; i++) it.Add(new KeyValuePair<string, string>(i.ToString(), (string)((List<object>)cl[i])[3]));
                    CrChips(it, v => L.cloth.ToString() == v, v => { L.cloth = int.Parse(v); p.ApplyLook(); }, v => AHGame.Hex((int)(double)((List<object>)cl[int.Parse(v)])[1]));
                    continue;
                }
                string[] kk = kind.Split(':');
                var list = LookList(kk[1]);
                var sw = new List<KeyValuePair<string, string>>();
                if (kk[0] == "eye") sw.Add(new KeyValuePair<string, string>("-1", "Class colour"));
                if (kk[0] == "hi") sw.Add(new KeyValuePair<string, string>("-1", "Natural"));
                if (kk[0] == "eye" || kk[0] == "hi")
                {
                    // the 'class colour' / 'natural' choice is a word chip, the rest are swatches
                    CrChips(sw, v => LookGet(L, k) == v, v => { LookSet(L, k, v); p.ApplyLook(); });
                    crY -= 4f;
                    sw.Clear();
                }
                for (int i = 0; i < list.Count; i++) sw.Add(new KeyValuePair<string, string>(i.ToString(), (string)((List<object>)list[i])[1]));
                CrChips(sw, v => LookGet(L, k) == v, v => { LookSet(L, k, v); p.ApplyLook(); }, v => AHGame.Hex((int)(double)((List<object>)list[int.Parse(v)])[0]));
            }
            crHint.text = "Drag your hero to turn them. Pinch or scroll to zoom.";
        }
        CrScrollBy(0f);
    }

    void CrY(float dy) { crY += dy; }

    void CrScrollBy(float dy)
    {
        if (crContent == null) return;
        float max = Mathf.Max(0f, crY - crBody.rect.height + 10f);
        crScroll = Mathf.Clamp(crScroll + dy, 0f, max);
        crContent.anchoredPosition = new Vector2(crContent.anchoredPosition.x, crScroll);
        crMore.gameObject.SetActive(crScroll < max - 2f);
    }

    // ---------- typing the name: the phone keyboard, or the computer's keys in the editor ----------
    void StartTyping()
    {
        crTyping = true;
        AHInput.TypedChars();   // forget keys pressed before the box was tapped
        if (TouchScreenKeyboard.isSupported) crKb = TouchScreenKeyboard.Open(g.player.heroName ?? "", TouchScreenKeyboardType.Default, false, false, false, false, "Your hero’s name", 16);
        RenderCreator();
    }

    void CreatorKeys()
    {
        if (!crTyping) return;
        var p = g.player;
        string before = p.heroName ?? "";
        string nm = before;
        if (crKb != null)
        {
            nm = crKb.text ?? "";
            if (crKb.status != TouchScreenKeyboard.Status.Visible) { crTyping = false; crKb = null; }
        }
        else
        {
            foreach (char c in AHInput.TypedChars())
            {
                if (c == '\b') { if (nm.Length > 0) nm = nm.Substring(0, nm.Length - 1); }
                else if (c == '\n' || c == '\r') crTyping = false;
                else if (char.IsLetter(c) || c == ' ' || c == '\'' || c == '-') nm += c;
            }
        }
        nm = CleanName(nm);
        if (nm != before || !crTyping) { p.heroName = nm; RenderCreator(); }
    }

    // web cleanName: letters, spaces, apostrophes and hyphens, up to 16
    static string CleanName(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s ?? "") if (char.IsLetter(c) || c == ' ' || c == '\'' || c == '-') sb.Append(c);
        string r = sb.ToString().TrimStart();
        return r.Length > 16 ? r.Substring(0, 16) : r;
    }

    // web randomName: a start and an ending that fits the gender
    public static string RandomName(string sex)
    {
        var n = AHJson.O(AHDB.Rules, "NAMES");
        var a = AHJson.A(n, "NAME_A"); var b = AHJson.A(n, sex == "f" ? "NAME_F" : sex == "m" ? "NAME_M" : "NAME_B");
        if (a == null || b == null || a.Count == 0 || b.Count == 0) return "Hero";
        return (string)a[UnityEngine.Random.Range(0, a.Count)] + (string)b[UnityEngine.Random.Range(0, b.Count)];
    }
}
