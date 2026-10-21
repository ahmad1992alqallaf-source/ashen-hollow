// Ashen Hollow: the roads out of a land, the way World of Warcraft does its zone borders. A land is ringed by mountains
// you cannot cross; where a road leaves it, the mountains open into a pass: a gorge of tall rock walls the road runs
// on into, two carved stone pillars with the land's banners marking the way, a signpost naming the land ahead and its
// levels, and a dip in the ranges behind (AHRange cuts a saddle there) so you see the next land's far peaks through
// the gap. Coming up to it, a line on the screen tells you where the road goes.
using System.Collections.Generic;
using UnityEngine;

public static class AHPass
{
    public class Way { public Vector3 at, outw, along; public float width; public string to, name, levels; }
    public static readonly List<Way> Ways = new List<Way>();
    public static int Rocks;

    // where the roads leave the land (before the ranges are built: they open a saddle at each)
    public static void Plan(AHGame g)
    {
        Ways.Clear(); Rocks = 0;
        if (g == null || g.data == null || g.data.exits == null || AHDungeon.IsDungeon(AHGame.AreaId)) return;
        var R = g.WalkArea;
        foreach (var e in g.data.exits)
        {
            if (string.IsNullOrEmpty(e.to)) continue;
            var ta = Resources.Load<TextAsset>("AH/Areas/" + e.to); if (ta == null) continue;
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            Vector3 c; float w;
            if (e.r > 0f) { c = g.W(e.x, e.z); w = (g.W(e.x + e.r, e.z) - c).magnitude * 2f; }
            else { Vector3 a = g.W(e.x0, e.z0), b = g.W(e.x1, e.z1); c = (a + b) * 0.5f; w = Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z)); }
            // which edge of the land it opens on (an exit far inside, a cave mouth, has no pass)
            float dl = c.x - R.xMin, dr = R.xMax - c.x, db = c.z - R.yMin, dt = R.yMax - c.z;
            float m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt)); if (m > 30f) continue;
            Vector3 outw = m == dl ? Vector3.left : m == dr ? Vector3.right : m == db ? Vector3.back : Vector3.forward;
            Vector3 edge = new Vector3(m == dl ? R.xMin : m == dr ? R.xMax : c.x, c.y, m == db ? R.yMin : m == dt ? R.yMax : c.z);
            Ways.Add(new Way { at = edge, outw = outw, along = Vector3.Cross(Vector3.up, outw), width = Mathf.Clamp(w, 9f, 22f), to = e.to, name = d.region, levels = d.levels });
        }
    }

    // how much of the mountain ranges stand at a point on their foot line (a saddle where a road goes out)
    public static float RangeK(Vector3 p)
    {
        float k = 1f;
        foreach (var w in Ways) { Vector3 d = p - w.at; d.y = 0f; if (Vector3.Dot(d, w.outw) < -30f) continue; float along = Mathf.Abs(Vector3.Dot(d, w.along)); k = Mathf.Min(k, Mathf.Lerp(0.2f, 1f, Mathf.SmoothStep(0f, 1f, (along - 12f) / 40f))); }
        return k;
    }

    public static void Build(AHGame g)
    {
        if (Ways.Count == 0) return;
        var root = new GameObject("Passes").transform;
        var rnd = new System.Random(AHGame.AreaId.GetHashCode() ^ 4517);
        float gy = g.W(0f, 0f).y; foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AH_GROUND")) { gy = r.bounds.max.y; break; }
        string col; if (!AHCity.ColourOf(AHGame.AreaId, out col)) col = "red";
        foreach (var w in Ways)
        {
            Vector3 at = new Vector3(w.at.x, gy, w.at.z);
            // the gorge: two walls of rock either side of the road, rising as they run out of the land
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < 9; k++)
                {
                    float outD = 1.5f + k * 4.6f + (float)rnd.NextDouble() * 1.5f;
                    float side = w.width * 0.5f + 2.2f + k * 0.35f + (float)rnd.NextDouble() * 1.4f;
                    float h = 7f + k * 1.6f + (float)rnd.NextDouble() * 3f;
                    Rock(root, at + w.outw * outD + w.along * s * side, h, 5.5f + (float)rnd.NextDouble() * 2.5f, rnd);
                    // a second rank behind, taller: a cliff, not a row of stones
                    Rock(root, at + w.outw * (outD + 2f) + w.along * s * (side + 4.5f), h * 1.35f, 7f + (float)rnd.NextDouble() * 3f, rnd);
                }
            // carved stone pillars with the land's banners at the mouth of the pass
            for (int s = -1; s <= 1; s += 2)
            {
                var p = at - w.outw * 1.0f + w.along * s * (w.width * 0.5f + 0.6f);
                var pil = Place(root, "AH/Models/KK/kk_dg_pillar_decorated", p, 5.2f, Quaternion.LookRotation(-w.outw));
                if (pil != null) { g.AddBlocker(p, 0.9f); AHWilds.TintStone(pil); }
                var ban = Place(root, "AH/Models/KK/kk_dg_banner_" + (col == "blue" ? "blue" : "red"), p - w.outw * 0.55f + Vector3.up * 1.4f, 2.6f, Quaternion.LookRotation(-w.outw));
                var torch = Place(root, "AH/Models/KK/kk_dg_torch_lit", p + Vector3.up * 5.2f, 0.9f, Quaternion.identity);
            }
            // the signpost: the name of the land ahead and its levels, read as you walk up to it
            Signpost(root, at - w.outw * 4.5f + w.along * (w.width * 0.5f + 2.2f), w);
        }
        root.gameObject.AddComponent<AHPassNotice>();
        AHModel.SetShadows(root.gameObject);
        Debug.Log("Ashen Hollow: " + Ways.Count + " passes out of " + AHGame.AreaId + " (" + Rocks + " rocks)");
    }

    static GameObject Place(Transform root, string res, Vector3 at, float height, Quaternion rot)
    {
        var pf = Resources.Load<GameObject>(res); if (pf == null) return null;
        var go = Object.Instantiate(pf, root, false); go.name = "Pass " + pf.name;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        go.transform.rotation = rot; go.transform.localScale = Vector3.one; go.transform.position = Vector3.zero;
        var b = Measure(go); float k = height / Mathf.Max(0.05f, b.size.y); go.transform.localScale = Vector3.one * k;
        b = Measure(go); go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        return go;
    }

    static void Rock(Transform root, Vector3 at, float h, float wide, System.Random rnd)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/kk_rock_single_" + "BCDE"[rnd.Next(4)]); if (pf == null) return;
        var go = Object.Instantiate(pf, root, false); go.name = "Pass rock";
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f); go.transform.localScale = Vector3.one; go.transform.position = Vector3.zero;
        var b = Measure(go);
        go.transform.localScale = new Vector3(wide / Mathf.Max(0.1f, b.size.x), h / Mathf.Max(0.1f, b.size.y), wide / Mathf.Max(0.1f, b.size.z));
        b = Measure(go); go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y - 0.4f, at.z - b.center.z);
        AHWilds.TintStone(go); Rocks++;
    }

    static void Signpost(Transform root, Vector3 at, Way w)
    {
        var sp = new GameObject("Pass signpost").transform; sp.SetParent(root, false); sp.position = at;
        sp.rotation = Quaternion.LookRotation(w.outw);
        var wood = new Material(Shader.Find("Universal Render Pipeline/Lit")); wood.color = new Color(0.36f, 0.24f, 0.14f);
        var plank = new Material(wood); plank.color = new Color(0.17f, 0.11f, 0.07f);   // a dark board, so the letters stand out
        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(post.GetComponent<Collider>()); post.transform.SetParent(sp, false);
        post.transform.localPosition = new Vector3(0f, 1.4f, 0f); post.transform.localScale = new Vector3(0.16f, 1.4f, 0.16f); post.GetComponent<Renderer>().sharedMaterial = wood;
        var board = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(board.GetComponent<Collider>()); board.transform.SetParent(sp, false);
        board.transform.localPosition = new Vector3(-1.05f, 2.35f, 0f); board.transform.localScale = new Vector3(2.6f, 0.85f, 0.07f); board.GetComponent<Renderer>().sharedMaterial = plank;
        // the point of the arrow, towards the road out
        var tip = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(tip.GetComponent<Collider>()); tip.transform.SetParent(sp, false);
        tip.transform.localPosition = new Vector3(-2.35f, 2.35f, 0f); tip.transform.localRotation = Quaternion.Euler(0f, 0f, 45f); tip.transform.localScale = new Vector3(0.6f, 0.6f, 0.069f); tip.GetComponent<Renderer>().sharedMaterial = plank;
        var font = Resources.Load<Font>("AH/Fonts/Nunito-ExtraBold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var tmat = TextMat(font);
        foreach (int face in new[] { -1, 1 })
        {
            var t = new GameObject("Text").AddComponent<TextMesh>(); t.transform.SetParent(sp, false);
            t.font = font; t.GetComponent<MeshRenderer>().sharedMaterial = tmat;
            // big font letters drawn small: sharp on a phone (the old 36-point letters came out blurred)
            t.text = w.name + "\n<size=78><color=#ffd27a>" + (string.IsNullOrEmpty(w.levels) ? "" : (w.levels == "safe" ? "a safe city" : "Level " + w.levels)) + "</color></size>";
            t.fontSize = 120; t.characterSize = 0.022f * Mathf.Min(1f, 11f / Mathf.Max(1, w.name.Length)); t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = new Color(1f, 0.97f, 0.88f);
            t.lineSpacing = 0.85f; t.richText = true;
            t.transform.localPosition = new Vector3(-1.05f, 2.35f, face * 0.045f); t.transform.localRotation = Quaternion.Euler(0f, face > 0 ? 180f : 0f, 0f);
        }
    }

    // the letters' material: hidden behind the board like any surface (the font's own material draws on top of
    // everything, so the back's mirrored words showed through and smeared the front's)
    static Material textMat; static Font textFont;
    static Material TextMat(Font font)
    {
        if (textMat != null && textFont == font) return textMat;
        var sh = Shader.Find("AshenHollow/Text3D"); if (sh == null) return font.material;
        textMat = new Material(sh) { name = "Signpost letters" }; textMat.mainTexture = font.material.mainTexture; textFont = font;
        Font.textureRebuilt -= Rebuilt; Font.textureRebuilt += Rebuilt;
        return textMat;
    }
    static void Rebuilt(Font f) { if (f == textFont && textMat != null) textMat.mainTexture = f.material.mainTexture; }

    static Bounds Measure(GameObject go) { var rs = go.GetComponentsInChildren<Renderer>(true); Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.zero); for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); return b; }
}

// coming up to a road out of the land: a line on the screen says where it goes (once, until you walk away again)
public class AHPassNotice : MonoBehaviour
{
    readonly HashSet<AHPass.Way> told = new HashSet<AHPass.Way>(); float next;
    void Update()
    {
        if (Time.time < next) return; next = Time.time + 0.4f;
        var g = AHGame.I; if (g == null || g.player == null || g.ui == null) return;
        Vector3 p = g.player.transform.position;
        foreach (var w in AHPass.Ways)
        {
            Vector3 d = p - w.at; d.y = 0f; float r = d.magnitude;
            if (r < 26f && !told.Contains(w)) { told.Add(w); g.ui.Toast("The road to " + w.name + (string.IsNullOrEmpty(w.levels) ? "" : w.levels == "safe" ? " (a safe city)" : " · Level " + w.levels), 3f); }
            else if (r > 40f) told.Remove(w);
        }
    }
}
