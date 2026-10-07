// Ashen Hollow: the maps.
//  - This land: a camera high above the area takes one picture of it (web north up, east right), then it is drawn over
//    as an inked, watercoloured parchment map in the area's own colours, inside a frame with a compass rose. Marked on
//    it: you, waystones (blue diamonds, grey until attuned), dungeon doors (red), the roads out (green, with where
//    they lead), bounty boards and hidden treasure you have found (gold).
//  - The world: the whole realm as a hand-drawn map (Resources/AH/UI/worldmap, made for Ashen Hollow) with a pin
//    where you are. Prev / Next (or the button on the page) switch between the two.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RenderTexture mapRT; Camera mapCam; bool mapFlip; Texture2D mapTex;
    public Texture2D MapTexture { get { return mapTex; } }   // the last inked area map (editor tests save it)
    public bool MapFlipped { get { return mapFlip; } }
    const int MapRes = 512;
    static Texture2D mapFrame, mapCompass, mapPin, worldTex, mapFog; static Dictionary<string, Vector2> worldPos;

    public void OpenMap() { wkMode = "map"; wkPageI = 0; ShowWork(true); StartCoroutine(SnapMap()); }
    public void OpenWorldMap() { wkMode = "map"; wkPageI = 1; ShowWork(true); StartCoroutine(SnapMap()); }

    IEnumerator SnapMap()
    {
        if (mapRT == null) { mapRT = new RenderTexture(MapRes, MapRes, 24); mapRT.name = "AH_Map"; }
        if (mapCam == null)
        {
            var go = new GameObject("MapCamera"); mapCam = go.AddComponent<Camera>();
            mapCam.orthographic = true; mapCam.clearFlags = CameraClearFlags.SolidColor; mapCam.backgroundColor = new Color(0.36f, 0.46f, 0.5f);
            mapCam.targetTexture = mapRT; mapCam.enabled = false; mapCam.nearClipPlane = 1f; mapCam.farClipPlane = 1200f;
            mapCam.cullingMask = ~(1 << 5);   // not the UI
        }
        // the area in Unity metres, looking straight down with web north (-z web) at the top
        Vector3 o = g.W(g.data.bounds.x0, g.data.bounds.z0), e = g.W(g.data.bounds.x1, g.data.bounds.z1);
        Vector3 c = (o + e) * 0.5f;
        Vector3 up = -g.W(0f, 1f) + g.W(0f, 0f); up.y = 0; up.Normalize();
        Vector3 webRight = g.W(1f, 0f) - g.W(0f, 0f); webRight.y = 0; webRight.Normalize();
        mapCam.transform.position = new Vector3(c.x, 400f, c.z);
        mapCam.transform.rotation = Quaternion.LookRotation(Vector3.down, up);
        mapFlip = Vector3.Dot(mapCam.transform.right, webRight) < 0f;
        float w = Mathf.Abs(Vector3.Dot(e - o, webRight)), h = Mathf.Abs(Vector3.Dot(e - o, up));
        mapCam.aspect = 1f; mapCam.orthographicSize = Mathf.Max(w, h) * 0.5f * 1.04f;
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        mapCam.enabled = true;
        yield return new WaitForEndOfFrame();
        mapCam.enabled = false; RenderSettings.fog = fog;
        // read it back and ink it
        if (mapTex == null) { mapTex = new Texture2D(MapRes, MapRes, TextureFormat.RGBA32, false); mapTex.wrapMode = TextureWrapMode.Clamp; }
        var prev = RenderTexture.active; RenderTexture.active = mapRT;
        mapTex.ReadPixels(new Rect(0, 0, MapRes, MapRes), 0, 0); RenderTexture.active = prev;
        InkMap(mapTex, AHGame.AreaId);
        if (WorkOpen && wkMode == "map") RenderWork();
    }

    // the area's paper and wash colours
    static Color MapTint(string k)
    {
        if (k.StartsWith("d_") || k == "forge") return new Color(0.82f, 0.78f, 0.74f);
        switch (k)
        {
            case "frost": case "hc_city": case "whitepine": return new Color(0.9f, 0.95f, 1.02f);
            case "ember": case "ch_city": case "ashfall": case "raid": return new Color(1.06f, 0.86f, 0.76f);
            case "sands": case "ss_city": case "scorchwind": return new Color(1.06f, 0.98f, 0.82f);
            case "mire": case "mw_city": case "fenwick": case "causeway": return new Color(0.9f, 0.97f, 0.84f);
            case "tide": case "co_city": case "isle": return new Color(0.92f, 1.0f, 1.02f);
            case "fossil": return new Color(1.0f, 0.93f, 0.84f);
            default: return new Color(1.02f, 0.99f, 0.9f);
        }
    }
    static float Hash(int x, int y) { unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
    static float VNoise(float x, float y)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }
    // watercolour over parchment, inked outlines where the picture changes, a darker rim
    static void InkMap(Texture2D t, string id)
    {
        int N = t.width; var px = t.GetPixels32(); var lum = new float[N * N];
        for (int i = 0; i < px.Length; i++) lum[i] = (px[i].r * 0.3f + px[i].g * 0.59f + px[i].b * 0.11f) / 255f;
        Color tint = MapTint(id); Color paper = new Color(0.95f, 0.89f, 0.75f); Color ink = new Color(0.24f, 0.17f, 0.11f);
        var outp = new Color32[px.Length];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int i = y * N + x;
                float l = lum[i];
                int xm = Mathf.Max(0, x - 1), xp = Mathf.Min(N - 1, x + 1), ym = Mathf.Max(0, y - 1), yp = Mathf.Min(N - 1, y + 1);
                float gx = lum[y * N + xp] - lum[y * N + xm], gy = lum[yp * N + x] - lum[ym * N + x];
                float edge = Mathf.Clamp01((Mathf.Abs(gx) + Mathf.Abs(gy) - 0.07f) * 3.2f) * 0.6f;
                float n = VNoise(x / 24f, y / 24f) * 0.6f + VNoise(x / 6f, y / 6f) * 0.25f + VNoise(x / 1.5f, y / 1.5f) * 0.15f;
                Color p = paper * (0.88f + 0.16f * n);
                Color c = (Color)px[i];
                // the picture's colour, softened and pulled toward the paper (a wash, not a photo)
                Color wash = Color.Lerp(c, p * (0.5f + 0.7f * l), 0.42f);
                wash = new Color(wash.r * tint.r, wash.g * tint.g, wash.b * tint.b);
                float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f; float vig = 1f - 0.32f * Mathf.Pow(Mathf.Clamp01((u * u + v * v) * 2.6f), 1.6f);
                Color o = Color.Lerp(wash, ink, edge) * vig; o.a = 1f;
                outp[i] = o;
            }
        t.SetPixels32(outp); t.Apply();
    }

    static void LoadMapArt()
    {
        if (mapFrame == null) mapFrame = Resources.Load<Texture2D>("AH/UI/map_frame");
        if (mapCompass == null) mapCompass = Resources.Load<Texture2D>("AH/UI/map_compass");
        if (mapPin == null) mapPin = Resources.Load<Texture2D>("AH/UI/map_pin");
        if (worldTex == null) worldTex = Resources.Load<Texture2D>("AH/UI/worldmap");
        if (mapFog == null) mapFog = Resources.Load<Texture2D>("AH/UI/map_fog");
        if (worldPos == null)
        {
            worldPos = new Dictionary<string, Vector2>();
            var ta = Resources.Load<TextAsset>("AH/UI/worldmap_pos");
            var d = ta != null ? AHJson.Parse(ta.text) as Dictionary<string, object> : null;
            if (d != null) foreach (var kv in d) { var a = kv.Value as List<object>; if (a != null && a.Count >= 2) worldPos[kv.Key] = new Vector2((float)(double)a[0], (float)(double)a[1]); }
        }
    }
    // where an area sits on the world map: dungeons and halls under the land they are in
    static string WorldSpot(string id)
    {
        switch (id)
        {
            case "d_warrens": return "mire"; case "d_frost": return "frost"; case "d_tomb": return "sands"; case "d_molten": case "raid": return "ember";
            case "d_hollow": return "meadow"; case "forge": return "silkwood"; case "kq": case "mquarter": case "ghall": return "city";
            default: return id;
        }
    }
    // lands that suit the hero's level (the REGIONS 'lv' ranges)
    static string WorldTip(AHPlayer p)
    {
        var r = AHDB.List("world", "REGIONS"); if (r == null) return "";
        var good = new List<string>(); var hard = new List<string>();
        foreach (var o in r)
        {
            string lv = AHJson.S(o, "lv", ""); if (lv == "" || lv == "safe" || lv == "any" || AHJson.B(o, "dung")) continue;
            var parts = lv.Replace("–", "-").Split('-'); int a, b; if (parts.Length < 1 || !int.TryParse(parts[0], out a)) continue; if (parts.Length < 2 || !int.TryParse(parts[1], out b)) b = a;
            string nm = AHJson.S(o, "name", "");
            if (p.level >= a - 2 && p.level <= b + 2) good.Add(nm); else if (a > p.level + 2 && a <= p.level + 10) hard.Add(nm);
        }
        string s = "Good for your level (" + p.level + "):\n" + (good.Count > 0 ? string.Join("\n", good.ToArray()) : "anywhere safe") ;
        if (hard.Count > 0) s += "\n\nNext, when you are stronger:\n" + string.Join("\n", hard.GetRange(0, Mathf.Min(3, hard.Count)).ToArray());
        return s;
    }
    static string RegionName(string id)
    {
        var r = AHDB.List("world", "REGIONS");
        if (r != null) foreach (var o in r) if (AHJson.S(o, "id") == id) return AHJson.S(o, "name", id);
        return id;
    }

    RawImage Raw(string name, Transform parent, Texture tex, Vector2 pos, Vector2 size)
    {
        var rt = Box(name, parent, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), pos, size);
        var ri = rt.gameObject.AddComponent<RawImage>(); ri.texture = tex; ri.raycastTarget = false; return ri;
    }
    Text MapText(Transform parent, string s, int size, Vector2 pos, Vector2 box, TextAnchor al, Color c, bool bold = false)
    {
        var t = Label(parent, "Ink", s, size, al, pos, box, c); if (bold) t.fontStyle = FontStyle.Bold;
        var sh = t.GetComponent<Shadow>(); if (sh != null) { sh.effectColor = new Color(1f, 0.95f, 0.82f, 0.85f); sh.effectDistance = new Vector2(1.5f, -1.5f); }
        return t;
    }
    void MapButton(Transform parent, string label, Vector2 pos, float w, Action act)
    {
        var b = Img("MapBtn", parent, white, new Vector2(0f, 1f), pos, new Vector2(w, 46), new Color(0.42f, 0.3f, 0.18f, 1f));
        Center(Label(b, "T", label, 19, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(w, 34), Color.white));
        taps.Add(new TapBtn { rt = b, layer = 5, group = "work", act = act });
    }

    void RenderMap(AHPlayer p)
    {
        LoadMapArt();
        wkPageI = ((wkPageI % 2) + 2) % 2;
        wkPage.text = wkPageI == 0 ? "This land" : "The world";
        if (wkPageI == 1) { RenderWorld(p); return; }
        wkTitle.text = "Map · " + g.data.region;
        int ways = 0, waysK = 0, tr = 0, trK = 0;
        if (AHWays.Ways != null) foreach (var w in AHWays.Ways) if (AHJson.S(w, "area") == AHGame.AreaId) { ways++; if (AHWays.Known(p, w)) waysK++; }
        if (AHWays.Treasures != null) foreach (var t in AHWays.Treasures) if (AHJson.S(t, "area") == AHGame.AreaId) { tr++; if (p.prog.found.Contains(AHJson.S(t, "id"))) trK++; }
        wkHint.text = g.AreaSub();
        float size = RowH * RowsPerPage - 4f, listW = WkW - 36;
        // the parchment map with its frame and compass
        var holder = Box("Map", wkList, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(size, size));
        wkItems.Add(holder.gameObject);
        var raw = holder.gameObject.AddComponent<RawImage>(); raw.texture = mapTex != null ? (Texture)mapTex : mapRT; raw.raycastTarget = false;
        raw.uvRect = mapFlip ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
        if (mapFrame != null) Raw("Frame", holder, mapFrame, new Vector2(size / 2f, -size / 2f), new Vector2(size * 1.04f, size * 1.04f));
        if (mapCompass != null) Raw("Compass", holder, mapCompass, new Vector2(size - 52f, -52f), new Vector2(70, 70));
        if (mapCam != null)
        {
            Action<Vector3, Color, float, bool, string> mark = (wp, col, sz, diamond, name) =>
            {
                Vector3 v = mapCam.WorldToViewportPoint(wp);
                if (v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) return;
                if (mapFlip) v.x = 1f - v.x;
                var m = Img("Mark", holder, diamond ? white : circle, new Vector2(0f, 0f), new Vector2(v.x * size, v.y * size), new Vector2(sz, sz), col);
                if (diamond) m.localRotation = Quaternion.Euler(0, 0, 45);
                var ol = m.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.15f, 0.1f, 0.06f, 0.95f); ol.effectDistance = new Vector2(1.5f, 1.5f);
                if (!string.IsNullOrEmpty(name))
                {
                    float lx = Mathf.Clamp(v.x * size - 70f, 6f, size - 146f), ly = -(1f - v.y) * size + (v.y > 0.85f ? -10f : 22f);
                    MapText(holder, name, 13, new Vector2(lx, ly), new Vector2(140, 18), TextAnchor.MiddleCenter, new Color(0.22f, 0.13f, 0.07f), true);
                }
            };
            float S = AHDB.S;
            foreach (var s in AHGather.Spots)
            {
                if (s.kind == "gate" && s.gate != null) mark(s.pos, !string.IsNullOrEmpty(s.gate.dung) ? new Color(0.85f, 0.2f, 0.15f) : new Color(0.3f, 0.75f, 0.35f), 13f, false, !string.IsNullOrEmpty(s.gate.dung) ? RegionName(s.gate.dung) : null);
                else if (s.name == "Bounty board") mark(s.pos, new Color(0.95f, 0.75f, 0.25f), 11f, true, null);
                else if (s.type == "deep_in") mark(s.pos, new Color(0.6f, 0.4f, 1f), 15f, false, "Ashen Deep");
            }
            if (g.data.exits != null) foreach (var x in g.data.exits) mark(g.W(x.x, x.z), new Color(0.3f, 0.75f, 0.35f), 13f, false, "to " + RegionName(x.to));
            if (AHWays.Ways != null) foreach (var w in AHWays.Ways) if (AHJson.S(w, "area") == AHGame.AreaId) mark(g.W((float)AHJson.N(w, "x") * S, (float)AHJson.N(w, "y") * S), AHWays.Known(p, w) ? new Color(0.35f, 0.65f, 0.95f) : new Color(0.5f, 0.5f, 0.55f), 14f, true, null);
            if (AHWays.Treasures != null) foreach (var t in AHWays.Treasures) if (AHJson.S(t, "area") == AHGame.AreaId && p.prog.found.Contains(AHJson.S(t, "id"))) mark(g.W((float)AHJson.N(t, "x") * S, (float)AHJson.N(t, "y") * S), new Color(0.95f, 0.75f, 0.25f), 10f, false, null);
            AHTreasure.MapMarks(p, (wp, nm) => mark(wp, new Color(0.95f, 0.55f, 0.15f), 15f, true, nm));
            foreach (var sp in AHSecrets.FoundSpots()) mark(sp, new Color(1f, 0.9f, 0.35f), 12f, true, null);
            Vector3 ev; if (AHSecrets.EventAt(out ev)) mark(ev, new Color(0.45f, 0.95f, 1f), 17f, true, "Event!");
            var pins = AHSecrets.Pins(); for (int pi = 0; pi < pins.Count; pi++) mark(pins[pi], new Color(0.9f, 0.2f, 0.55f), 15f, false, "Pin " + (pi + 1));
            AHStory.MapMarks(p, (wp, nm) => mark(wp, new Color(1f, 0.72f, 0.2f), 18f, true, nm));   // whoever the story sends you to
            mark(p.transform.position, Color.white, 18f, false, "You");
        }
        // the side panel: the land's name, its levels and what the marks mean
        float sx = size + 18f, sw = listW - size - 18f;
        var side = Img("Side", wkList, white, new Vector2(0f, 1f), new Vector2(sx + sw / 2f, -size / 2f), new Vector2(sw, size), new Color(0.9f, 0.84f, 0.7f, 1f));
        wkItems.Add(side.gameObject);
        MapText(side, g.data.region, 24, new Vector2(14, -10), new Vector2(sw - 28, 30), TextAnchor.UpperLeft, new Color(0.3f, 0.12f, 0.06f), true);
        var sub = MapText(side, g.AreaSub(), 15, new Vector2(14, -44), new Vector2(sw - 28, 60), TextAnchor.UpperLeft, new Color(0.25f, 0.17f, 0.1f)); sub.horizontalOverflow = HorizontalWrapMode.Wrap;
        string[] keyText = { "You", "Ways out", "Dungeon doors", "Waystones" + (ways > 0 ? "  " + waysK + " / " + ways : ""), "Boards and found treasure" + (tr > 0 ? "  " + trK + " / " + tr : ""), "Secrets found  " + AHSecrets.FoundHere() + " / " + AHSecrets.PerLand, "Your pins  " + AHSecrets.Pins().Count + " / " + AHSecrets.MaxPins };
        Color[] keyCol = { Color.white, new Color(0.3f, 0.75f, 0.35f), new Color(0.85f, 0.2f, 0.15f), new Color(0.35f, 0.65f, 0.95f), new Color(0.95f, 0.75f, 0.25f), new Color(1f, 0.9f, 0.35f), new Color(0.9f, 0.2f, 0.55f) };
        for (int i = 0; i < keyText.Length; i++)
        {
            float y = -110f - i * 23f;
            bool dia = i >= 3 && i <= 5;
            var dot = Img("Key", side, dia ? white : circle, new Vector2(0f, 1f), new Vector2(26f, y - 10f), new Vector2(14, 14), keyCol[i]);
            if (dia) dot.localRotation = Quaternion.Euler(0, 0, 45);
            var ol = dot.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.15f, 0.1f, 0.06f, 0.95f);
            MapText(side, keyText[i], 15, new Vector2(44, y), new Vector2(sw - 56, 20), TextAnchor.UpperLeft, new Color(0.22f, 0.15f, 0.09f));
        }
        float half = (sw - 36f) / 2f;
        MapButton(side, "Pin here", new Vector2(14f + half / 2f, -size + 88f), half, () => { if (AHSecrets.AddPin(p.transform.position)) Toast("Pinned where you stand"); else Toast("Six pins at most: clear them first"); StartCoroutine(SnapMap()); });
        MapButton(side, "Clear pins", new Vector2(22f + half * 1.5f, -size + 88f), half, () => { AHSecrets.ClearPins(); Toast("Pins cleared"); StartCoroutine(SnapMap()); });
        MapButton(side, "World map ▶", new Vector2(sw / 2f, -size + 34f), sw - 28f, () => { wkPageI = 1; RenderWork(); });
    }

    void RenderWorld(AHPlayer p)
    {
        wkTitle.text = "Map · The Realm of Ashen Hollow";
        string here = WorldSpot(AHGame.AreaId);
        wkHint.text = "You are in " + g.data.region + ". The long roads join the lands; ships sail to the isles.";
        float h = RowH * RowsPerPage - 4f, listW = WkW - 36;
        float aspect = 2048f / 1440f, w = Mathf.Min(listW, h * aspect); h = w / aspect;
        var holder = Box("World", wkList, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(listW - w, 0f), new Vector2(w, h));
        wkItems.Add(holder.gameObject);
        var raw = holder.gameObject.AddComponent<RawImage>(); raw.texture = worldTex; raw.raycastTarget = false;
        if (worldTex == null) { MapText(holder, "The world map is missing (Resources/AH/UI/worldmap).", 18, new Vector2(10, -10), new Vector2(w - 20, 30), TextAnchor.UpperLeft, Color.white); return; }
        // lands not yet walked in lie under a cloud of fog
        int lands = 0, seen = 0;
        foreach (var kv in worldPos)
        {
            if (kv.Key == "homestead" || kv.Key == "tutorial") continue;
            bool road = kv.Key == "mill" || kv.Key == "whitepine" || kv.Key == "fenwick" || kv.Key == "kingsroad" || kv.Key == "causeway" || kv.Key == "scorchwind" || kv.Key == "ashfall";
            if (road) continue;   // the roads are drawn; you learn their names as you walk them
            lands++; if (AHGame.Seen(kv.Key) || kv.Key == here) { seen++; continue; }
            bool town = kv.Key.EndsWith("_city") || kv.Key == "city";
            float fs = town ? 46f : 118f;
            if (mapFog != null) Raw("Fog", holder, mapFog, new Vector2(kv.Value.x * w, -kv.Value.y * h), new Vector2(fs * 1.3f, fs));
            else Img("Fog", holder, circle, new Vector2(0f, 1f), new Vector2(kv.Value.x * w, -kv.Value.y * h), new Vector2(fs * 1.25f, fs), new Color(0.86f, 0.82f, 0.74f, 0.82f));
        }
        wkHint.text = "You are in " + g.data.region + ". Lands discovered: " + seen + " / " + lands + ". Fog hides the rest until you walk there.";
        Vector2 uv;
        if (worldPos.TryGetValue(here, out uv))
        {
            float px = uv.x * w, py = -uv.y * h;
            Img("Halo", holder, circle, new Vector2(0f, 1f), new Vector2(px, py), new Vector2(46, 46), new Color(1f, 0.85f, 0.3f, 0.45f));
            if (mapPin != null) Raw("Pin", holder, mapPin, new Vector2(px, py + 20f), new Vector2(40, 40));
            MapText(holder, "You are here", 15, new Vector2(Mathf.Clamp(px - 70f, 4f, w - 144f), py + 54f), new Vector2(140, 20), TextAnchor.MiddleCenter, new Color(0.45f, 0.08f, 0.05f), true);
        }
        float side = listW - w - 12f;
        if (side > 120f)
        {
            var key = Img("WorldKey", wkList, white, new Vector2(0f, 1f), new Vector2(side / 2f, -h / 2f), new Vector2(side, h), new Color(0.9f, 0.84f, 0.7f, 1f)); wkItems.Add(key.gameObject);
            var t1 = MapText(key, "Where to next?", 17, new Vector2(10, -10), new Vector2(side - 20, 24), TextAnchor.UpperLeft, new Color(0.3f, 0.12f, 0.06f), true);
            var t2 = MapText(key, WorldTip(p), 14, new Vector2(10, -38), new Vector2(side - 20, h - 110), TextAnchor.UpperLeft, new Color(0.22f, 0.15f, 0.09f)); t2.horizontalOverflow = HorizontalWrapMode.Wrap;
            MapButton(key, "◀ This land", new Vector2(side / 2f, -h + 32f), side - 16f, () => { wkPageI = 0; RenderWork(); });
        }
        else MapButton(holder, "◀ This land", new Vector2(110f, -h + 32f), 180f, () => { wkPageI = 0; RenderWork(); });
    }
}
