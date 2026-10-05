// Ashen Hollow: the map (web: "Here" shows the map you are standing in). A camera high above the area takes one picture
// of it (redone each time the map opens), drawn the way the web game's map is drawn: web north up, east right.
// Marked on it: you, waystones (blue diamonds, grey until attuned), dungeon doors (red), the ways out (green),
// bounty boards and hidden treasure you have found (gold).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RenderTexture mapRT; Camera mapCam; bool mapFlip;
    const int MapRes = 1024;

    public void OpenMap() { wkMode = "map"; wkPageI = 0; ShowWork(true); StartCoroutine(SnapMap()); }

    IEnumerator SnapMap()
    {
        if (mapRT == null) { mapRT = new RenderTexture(MapRes, MapRes, 24); mapRT.name = "AH_Map"; }
        if (mapCam == null)
        {
            var go = new GameObject("MapCamera"); mapCam = go.AddComponent<Camera>();
            mapCam.orthographic = true; mapCam.clearFlags = CameraClearFlags.SolidColor; mapCam.backgroundColor = new Color(0.1f, 0.12f, 0.1f);
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
        mapCam.aspect = 1f; mapCam.orthographicSize = Mathf.Max(w, h) * 0.5f;
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        mapCam.enabled = true;
        yield return new WaitForEndOfFrame();
        mapCam.enabled = false; RenderSettings.fog = fog;
        if (WorkOpen && wkMode == "map") RenderWork();
    }

    void RenderMap(AHPlayer p)
    {
        wkTitle.text = "Map · " + g.data.region;
        int ways = 0, waysK = 0, tr = 0, trK = 0;
        if (AHWays.Ways != null) foreach (var w in AHWays.Ways) if (AHJson.S(w, "area") == AHGame.AreaId) { ways++; if (AHWays.Known(p, w)) waysK++; }
        if (AHWays.Treasures != null) foreach (var t in AHWays.Treasures) if (AHJson.S(t, "area") == AHGame.AreaId) { tr++; if (p.prog.found.Contains(AHJson.S(t, "id"))) trK++; }
        wkHint.text = (ways > 0 ? "Waystones here: " + waysK + " / " + ways + " · " : "") + (tr > 0 ? "Treasures here: " + trK + " / " + tr + " · " : "") + "you (white), waystones (blue), dungeons (red), ways out (green), boards and found treasure (gold).";
        wkPage.text = "";
        float size = RowH * RowsPerPage - 4f;
        var holder = Box("Map", wkList, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((WkW - 36) / 2f, 0f), new Vector2(size, size));
        holder.anchorMin = holder.anchorMax = new Vector2(0f, 1f); holder.pivot = new Vector2(0.5f, 1f);
        holder.anchoredPosition = new Vector2((WkW - 36) / 2f, 0f);
        wkItems.Add(holder.gameObject);
        var raw = holder.gameObject.AddComponent<RawImage>(); raw.texture = mapRT; raw.raycastTarget = false;
        raw.uvRect = mapFlip ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
        if (mapCam == null) return;
        Action<Vector3, Color, float, bool> mark = (wp, col, sz, diamond) =>
        {
            Vector3 v = mapCam.WorldToViewportPoint(wp);
            if (v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) return;
            if (mapFlip) v.x = 1f - v.x;
            var m = Img("Mark", holder, diamond ? white : circle, new Vector2(0f, 0f), new Vector2(v.x * size, v.y * size), new Vector2(sz, sz), col);
            if (diamond) m.localRotation = Quaternion.Euler(0, 0, 45);
            var ol = m.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0, 0, 0, 0.85f); ol.effectDistance = new Vector2(1.5f, 1.5f);
        };
        float S = AHDB.S;
        foreach (var s in AHGather.Spots)
        {
            if (s.kind == "gate" && s.gate != null) mark(s.pos, !string.IsNullOrEmpty(s.gate.dung) ? new Color(1f, 0.3f, 0.25f) : new Color(0.4f, 1f, 0.45f), 13f, false);
            else if (s.name == "Bounty board") mark(s.pos, new Color(1f, 0.82f, 0.3f), 11f, true);
        }
        if (g.data.exits != null) foreach (var x in g.data.exits) mark(g.W(x.x, x.z), new Color(0.4f, 1f, 0.45f), 13f, false);
        if (AHWays.Ways != null) foreach (var w in AHWays.Ways) if (AHJson.S(w, "area") == AHGame.AreaId) mark(g.W((float)AHJson.N(w, "x") * S, (float)AHJson.N(w, "y") * S), AHWays.Known(p, w) ? new Color(0.5f, 0.83f, 1f) : new Color(0.45f, 0.45f, 0.5f), 14f, true);
        if (AHWays.Treasures != null) foreach (var t in AHWays.Treasures) if (AHJson.S(t, "area") == AHGame.AreaId && p.prog.found.Contains(AHJson.S(t, "id"))) mark(g.W((float)AHJson.N(t, "x") * S, (float)AHJson.N(t, "y") * S), new Color(1f, 0.82f, 0.3f), 10f, false);
        mark(p.transform.position, Color.white, 16f, false);
    }
}
