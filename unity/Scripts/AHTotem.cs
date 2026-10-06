// Ashen Hollow: the shaman's totems. A carved post planted near the shaman that pulses once a second for its life:
//  - healing totems ("heal" in the id) mend the shaman and nearby sellswords by a share of their max HP;
//  - war totems ("war") fill everyone near with fury: more damage while they stand in reach;
//  - fire totems ("sear", "magma") burn the nearest enemies.
// Only one totem of each kind stands at a time: planting a new one pulls the old one up.
using System.Collections.Generic;
using UnityEngine;

public class AHTotem : MonoBehaviour
{
    public enum Kind { Heal, War, Fire }
    static readonly Dictionary<Kind, AHTotem> standing = new Dictionary<Kind, AHTotem>();
    AHPlayer owner; Kind kind; float radius, value, mult, life, tick; Color col; Transform glow;

    public static Kind KindOf(string id) { return id.Contains("heal") || id.Contains("spirit") ? Kind.Heal : id.Contains("war") || id.Contains("wind") ? Kind.War : Kind.Fire; }

    public static void Plant(AHPlayer p, SpellDef sp, Vector3 at, float life)
    {
        var k = KindOf(sp.id);
        AHTotem old; if (standing.TryGetValue(k, out old) && old != null) Destroy(old.gameObject);
        var go = new GameObject("Totem_" + sp.id); go.transform.position = AHGame.I != null ? AHGame.I.Resolve(at, 0.3f) : at;
        var t = go.AddComponent<AHTotem>();
        t.owner = p; t.kind = k; t.radius = Mathf.Max(3f, sp.radius); t.value = sp.value; t.mult = sp.mult; t.life = life; t.col = sp.color;
        t.Build(); standing[k] = t;
        AHFx.Pillar(go.transform.position, 0.6f, 3.5f, sp.color, 0.6f);
        AHFx.Ring(go.transform.position, 0.3f, t.radius, sp.color, 0.6f);
    }

    // the post: carved wood in three tiers, a painted face, little wings and a glowing stone on top
    void Build()
    {
        var wood = Mat(new Color(0.42f, 0.27f, 0.16f)); var paint = Mat(Color.Lerp(col, Color.white, 0.15f)); var dark = Mat(new Color(0.18f, 0.12f, 0.08f));
        Part(PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0), new Vector3(0.5f, 0.08f, 0.5f), dark);
        for (int i = 0; i < 3; i++)
        {
            float y = 0.32f + i * 0.42f, w = 0.36f - i * 0.04f;
            Part(PrimitiveType.Cylinder, new Vector3(0, y, 0), new Vector3(w, 0.2f, w), wood);
            Part(PrimitiveType.Cylinder, new Vector3(0, y + 0.2f, 0), new Vector3(w + 0.04f, 0.025f, w + 0.04f), paint);
            Part(PrimitiveType.Cube, new Vector3(0, y + 0.03f, w * 0.48f), new Vector3(w * 0.6f, 0.07f, 0.03f), dark);                 // the mouth
            foreach (float sx in new[] { -1f, 1f }) Part(PrimitiveType.Cube, new Vector3(sx * w * 0.2f, y + 0.12f, w * 0.48f), new Vector3(0.06f, 0.05f, 0.03f), paint);   // the eyes
        }
        foreach (float sx in new[] { -1f, 1f }) { var wg = Part(PrimitiveType.Cube, new Vector3(sx * 0.3f, 1.25f, 0), new Vector3(0.36f, 0.06f, 0.16f), paint); wg.localRotation = Quaternion.Euler(0, 0, sx * 18f); }
        glow = Part(PrimitiveType.Sphere, new Vector3(0, 1.52f, 0), Vector3.one * 0.22f, GlowMat());
        var l = new GameObject("Light").AddComponent<Light>(); l.transform.SetParent(transform, false); l.transform.localPosition = new Vector3(0, 1.6f, 0);
        l.type = LightType.Point; l.color = col; l.range = 4f; l.intensity = 1.2f; l.shadows = LightShadows.None;
    }
    Transform Part(PrimitiveType pt, Vector3 pos, Vector3 size, Material m)
    {
        var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); Destroy(o.GetComponent<Collider>());
        o.transform.SetParent(transform, false); o.transform.localPosition = pos; o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = m;
        return o.transform;
    }
    static Shader lit;
    static Material Mat(Color c) { if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit"); var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.15f); return m; }
    Material GlowMat() { var m = Mat(col); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", col * 2.2f); return m; }

    void Update()
    {
        var g = AHGame.I;
        life -= Time.deltaTime; tick -= Time.deltaTime;
        if (glow != null) glow.localPosition = new Vector3(0, 1.52f + Mathf.Sin(Time.time * 3f) * 0.05f, 0);
        if (life <= 0f || owner == null || owner.dead || g == null) { AHFx.Pop(transform.position + Vector3.up, 1.2f, col); Destroy(gameObject); return; }
        if (tick > 0f) return;
        tick = 1f;
        Vector3 at = transform.position; bool near = (owner.transform.position - at).magnitude <= radius;
        AHFx.Ring(at, 0.4f, radius, new Color(col.r, col.g, col.b, 0.35f), 0.5f);
        switch (kind)
        {
            case Kind.Heal:
                if (near && owner.hp < owner.maxHp) owner.Heal(owner.maxHp * value);
                AHComp.HealParty(g, at, radius, value);
                break;
            case Kind.War:
                if (near) { owner.dmgBuff = Mathf.Max(owner.dmgBuffT > 0f ? owner.dmgBuff : 0f, value); owner.dmgBuffT = Mathf.Max(owner.dmgBuffT, 1.2f); }
                break;
            case Kind.Fire:
                int hits = 0;
                foreach (var m in g.mobs)
                {
                    if (m == null || m.dead || hits >= 2) continue;
                    if ((m.transform.position - at).magnitude - m.type.radius > radius) continue;
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(owner.Power * Mathf.Max(0.4f, mult)));
                    AHFx.Shoot(at + Vector3.up * 1.5f, m, col, 0.16f, 22f, SpellEl.Fire, x => x.Hurt(dmg, owner));
                    hits++;
                }
                break;
        }
    }

    void OnDestroy() { AHTotem cur; if (standing.TryGetValue(kind, out cur) && cur == this) standing.Remove(kind); }
}
