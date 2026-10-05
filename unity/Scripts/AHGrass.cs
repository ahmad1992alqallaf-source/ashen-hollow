// Ashen Hollow: grass that grows around the hero. The ground's colours (exported from the web game)
// decide where grass grows and what colour it is. Built in 8 m patches, kept only near the hero.
using System.Collections.Generic;
using UnityEngine;

public class AHGrass : MonoBehaviour
{
    AHGame g;
    Color32[] px;
    int pw, ph;
    AHGroundMap gm;
    Material mat;
    readonly Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
    readonly List<Vector2Int> drop = new List<Vector2Int>();
    float timer;
    const float Chunk = 8f, Cell = 0.45f;
    public float radius = 14f;
    public float density = 0.62f;

    public static AHGrass Create(AHGame game)
    {
        if (AHDungeon.IsDungeon(AHGame.AreaId)) return null;   // no grass underground
        TextAsset png = Resources.Load<TextAsset>(AHGame.AreaPath + AHGame.AreaId + "_ground");
        if (png == null || game.data.groundMap == null || game.data.groundMap.w <= 0f) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(png.bytes)) return null;
        Material m = AHGame.LoadMat("AH/Materials/Grass", "AshenHollow/Grass");
        if (m == null) return null;
        var go = new GameObject("Grass");
        var gr = go.AddComponent<AHGrass>();
        gr.g = game; gr.gm = game.data.groundMap; gr.mat = m;
        gr.px = tex.GetPixels32(); gr.pw = tex.width; gr.ph = tex.height;
        Destroy(tex);
        return gr;
    }

    // ground colour at a point in web metres, or false outside the map
    bool Sample(float x, float z, out Color32 c)
    {
        c = new Color32();
        float u = (x - gm.x0) / gm.w, v = (z - gm.z0) / gm.h;
        if (u < 0f || v < 0f || u >= 1f || v >= 1f) return false;
        int ix = Mathf.Min(pw - 1, (int)(u * pw)), iy = ph - 1 - Mathf.Min(ph - 1, (int)(v * ph));
        c = px[iy * pw + ix];
        return true;
    }

    static bool IsGrass(Color32 c) { return c.g > c.r + 6 && c.g > c.b + 14 && c.g > 55; }

    static float Hash(int a, int b)
    {
        unchecked
        {
            int h = a * 374761393 + b * 668265263;
            h = (h ^ (int)((uint)h >> 13)) * 1274126177;
            return (uint)(h ^ (int)((uint)h >> 16)) / 4294967296f;
        }
    }
    static float Fr(float v) { return v - Mathf.Floor(v); }

    void Update()
    {
        if (g == null || g.player == null) return;
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = 0.4f;
        Vector2 p = g.ToWeb(g.player.transform.position);
        int r = Mathf.CeilToInt(radius / Chunk);
        int cx = Mathf.FloorToInt(p.x / Chunk), cz = Mathf.FloorToInt(p.y / Chunk);
        int built = 0;
        for (int j = -r; j <= r; j++)
            for (int i = -r; i <= r; i++)
            {
                var k = new Vector2Int(cx + i, cz + j);
                Vector2 centre = new Vector2((k.x + 0.5f) * Chunk, (k.y + 0.5f) * Chunk);
                if ((centre - p).magnitude > radius + Chunk * 0.7f) continue;
                if (chunks.ContainsKey(k)) continue;
                if (built++ > 3) continue;   // a few patches per step, so there is no hitch
                chunks[k] = Build(k);
            }
        drop.Clear();
        foreach (var kv in chunks)
        {
            Vector2 centre = new Vector2((kv.Key.x + 0.5f) * Chunk, (kv.Key.y + 0.5f) * Chunk);
            if ((centre - p).magnitude > radius + Chunk * 1.6f) drop.Add(kv.Key);
        }
        foreach (var k in drop)
        {
            var go = chunks[k];
            if (go != null) { var mf = go.GetComponent<MeshFilter>(); if (mf != null) Destroy(mf.sharedMesh); Destroy(go); }
            chunks.Remove(k);
        }
    }

    GameObject Build(Vector2Int k)
    {
        var verts = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();
        Vector3 origin = g.W(k.x * Chunk, k.y * Chunk);
        int n = Mathf.CeilToInt(Chunk / Cell);
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                int gi = Mathf.RoundToInt(k.x * Chunk / Cell) + i, gj = Mathf.RoundToInt(k.y * Chunk / Cell) + j;
                float h = Hash(gi, gj);
                if (h > density) continue;
                float x = (gi + Fr(h * 7.13f)) * Cell, z = (gj + Fr(h * 13.7f)) * Cell;
                Color32 c;
                if (!Sample(x, z, out c) || !IsGrass(c)) continue;
                if (g.Blocked(g.W(x, z), 0.1f) || AHHome.NoGrass(x, z)) continue;
                float s = 0.65f + Fr(h * 31.1f) * 0.75f, sy = s * (0.8f + Fr(h * 17.3f) * 0.7f), rot = h * 40f;
                Color baseC = new Color(c.r / 255f * 1.05f, c.g / 255f * 1.12f, c.b / 255f * 0.95f).linear;
                Tuft(verts, cols, tris, g.W(x, z) - origin, s, sy, rot, baseC);
            }
        var go = new GameObject("GrassPatch");
        go.transform.SetParent(transform, false);
        go.transform.position = origin;
        if (verts.Count == 0) return go;
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        var b = mesh.bounds; b.Expand(0.5f); mesh.bounds = b;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
        return go;
    }

    // five thin blades: dark at the root, light at the tip
    static void Tuft(List<Vector3> v, List<Color> c, List<int> t, Vector3 at, float s, float sy, float rot, Color col)
    {
        Quaternion q = Quaternion.Euler(0, rot * Mathf.Rad2Deg, 0);
        Color root = col * 0.72f; root.a = 0f;
        Color tip = new Color(col.r * 1.18f, col.g * 1.22f, col.b * 1.08f, 1f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 2.4f + 0.3f, r = 0.04f + (i % 2) * 0.05f, h = 0.26f + (i % 3) * 0.08f, lean = 0.08f + (i % 2) * 0.05f, w = 0.035f;
            Vector3 cpos = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            Vector3 side = new Vector3(Mathf.Cos(a + 1.57f) * w, 0, Mathf.Sin(a + 1.57f) * w);
            Vector3 tipP = cpos + new Vector3(Mathf.Cos(a) * lean, h, Mathf.Sin(a) * lean);
            int b0 = v.Count;
            v.Add(at + q * Vector3.Scale(cpos - side, new Vector3(s, sy, s)));
            v.Add(at + q * Vector3.Scale(cpos + side, new Vector3(s, sy, s)));
            v.Add(at + q * Vector3.Scale(tipP, new Vector3(s, sy, s)));
            c.Add(root); c.Add(root); c.Add(tip);
            t.Add(b0); t.Add(b0 + 1); t.Add(b0 + 2);
        }
    }
}
