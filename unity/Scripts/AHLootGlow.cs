// Ashen Hollow: a body that still holds loot glows on the ground beneath it, in the colour of the best thing it holds
// (grey, green, blue, purple, gold for legendary), with motes drifting up, so you can tell from afar there is
// something to take and how good it is. The glow goes when the body is emptied.
using UnityEngine;

public class AHLootGlow : MonoBehaviour
{
    AHMob m; Transform disc, beam; Material mat, beamMat; Color col; float t, sparkT;
    static Mesh quad; static Shader spark;

    public static void Refresh(AHMob m)
    {
        if (m == null) return;
        var gl = m.GetComponentInChildren<AHLootGlow>(true);
        if (!m.HasLoot) { if (gl != null) Destroy(gl.gameObject); return; }
        if (gl == null)
        {
            var go = new GameObject("LootGlow"); go.transform.SetParent(m.transform, false);
            gl = go.AddComponent<AHLootGlow>(); gl.m = m; gl.Build();
        }
        gl.SetColour(Best(m));
    }

    // the best quality colour among what the body holds (money alone: a soft gold)
    public static Color Best(AHMob m)
    {
        int best = -1; Color c = new Color(1f, 0.85f, 0.45f);
        foreach (var id in m.loot)
        {
            var d = AHItems.Get(id); if (d == null) continue;
            int r = AHGrades.Rank(d);
            if (r > best) { best = r; c = AHItems.Quality(d); }
        }
        return c;
    }

    void Build()
    {
        if (spark == null) spark = Shader.Find("AshenHollow/Spark") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (quad == null)
        {
            quad = new Mesh { name = "LootGlowQuad" };
            quad.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quad.triangles = new[] { 0, 2, 1, 1, 2, 3 }; quad.RecalculateBounds();
        }
        float r = Mathf.Max(1.2f, m.height * 1.3f);
        disc = Part("Disc", r, out mat); disc.localPosition = Vector3.up * 0.05f;
        beam = Part("Beam", 1f, out beamMat); beam.localRotation = Quaternion.Euler(90f, 0f, 0f); beam.localScale = new Vector3(0.35f, 1f, 3.2f); beam.localPosition = Vector3.up * 1.6f;
    }
    Transform Part(string name, float size, out Material mt)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.localScale = Vector3.one * size;
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        var mr = go.AddComponent<MeshRenderer>(); mt = new Material(spark); mr.sharedMaterial = mt;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        return go.transform;
    }
    void SetColour(Color c) { col = c; Apply(1f); }
    void Apply(float k)
    {
        if (mat == null) return;
        Color a = new Color(col.r, col.g, col.b, 0.75f * k); mat.color = a; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", a);
        Color b = new Color(col.r, col.g, col.b, 0.35f * k); beamMat.color = b; if (beamMat.HasProperty("_BaseColor")) beamMat.SetColor("_BaseColor", b);
        // the spark shader takes its colour from the mesh's vertex colours
        var mf = disc.GetComponent<MeshFilter>(); var bf = beam.GetComponent<MeshFilter>();
        if (mf.sharedMesh == quad) { mf.mesh = Instantiate(quad); bf.mesh = Instantiate(quad); }
        mf.mesh.colors = new[] { a, a, a, a }; bf.mesh.colors = new[] { b, b, b, b };
    }
    void Update()
    {
        if (m == null || !m.HasLoot) { Destroy(gameObject); return; }
        t += Time.deltaTime;
        float k = 0.75f + 0.25f * Mathf.Sin(t * 3f);
        if (Mathf.Repeat(t, 0.25f) < Time.deltaTime) Apply(k);
        // only the best stuff sends up a beam you can see across the field
        bool showBeam = AHGrades.RankOf(col) >= 2;
        if (beam.gameObject.activeSelf != showBeam) beam.gameObject.SetActive(showBeam);
        if (Camera.main != null && showBeam) { Vector3 f = Camera.main.transform.position - beam.position; f.y = 0f; if (f.sqrMagnitude > 0.01f) beam.rotation = Quaternion.LookRotation(Vector3.up, -f.normalized) ; }
        sparkT -= Time.deltaTime;
        if (sparkT <= 0f) { sparkT = 0.5f; AHSpark.Burst(transform.position + Vector3.up * 0.4f, new Color(col.r, col.g, col.b, 0.8f), 3, 0.6f, 0.9f, 0.12f, 1.4f); }
    }
}
