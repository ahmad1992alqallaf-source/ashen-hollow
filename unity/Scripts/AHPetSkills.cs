// Ashen Hollow: your pet's own move. With a pet out, a PET button shows above LEAP: tap it and the pet
// does its signature move, then rests for 40 seconds. It grows stronger as the pet levels and evolves.
//   toad / pumpkin slime: Croak (stuns what hunts you nearby)      slime: Jelly mend (heals you)
//   fox: Pounce (a big bite)       snow fox: Frost pounce (a bite that chills)       owl / lantern owl: Dive (hits and dazes)
//   parrot: Squawk (slows everything hunting you)                    drake: Fire breath (burns everything in front)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHPetSkills
{
    public const float Cooldown = 40f;
    public const int MinLevel = 1;
    static float readyAt;

    public static bool Has(AHPlayer p) { return p != null && p.pet != null && AHComp.PetLv(p, p.pet) >= MinLevel; }
    public static float Left { get { return Mathf.Max(0f, readyAt - Time.time); } }

    public static string SkillName(string k)
    {
        switch (k)
        {
            case "toad": case "pumpkin_slime": return "Croak";
            case "slime": return "Jelly mend";
            case "fox": return "Pounce";
            case "snow_fox": return "Frost pounce";
            case "owl": case "lantern_owl": return "Dive";
            case "parrot": return "Squawk";
            case "drake": return "Fire breath";
            default: return "Rally";
        }
    }

    static float Power(AHPlayer p) { return 1f + AHComp.PetLv(p, p.pet) * 0.03f + AHComp.EvoStage(p, p.pet) * 0.15f; }

    static List<AHMob> Hunting(AHGame g, Vector3 at, float r)
    {
        var l = new List<AHMob>();
        foreach (var m in g.mobs) if (m != null && !m.dead && m.Chasing && (m.transform.position - at).sqrMagnitude < r * r) l.Add(m);
        return l;
    }
    static AHMob Nearest(AHGame g, Vector3 at, float r)
    {
        AHMob best = null; float bd = r * r;
        foreach (var m in g.mobs) { if (m == null || m.dead || !m.Chasing) continue; float d = (m.transform.position - at).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
        return best;
    }

    public static void Use(AHGame g)
    {
        var p = g.player; if (!Has(p) || p.dead) return;
        if (Left > 0f) { if (g.ui != null) g.ui.Toast(AHComp.PetName(p.pet) + " is resting (" + Mathf.CeilToInt(Left) + " s)"); return; }
        var petGo = GameObject.Find("Pet"); Vector3 from = petGo != null ? petGo.transform.position : p.transform.position;
        float k = Power(p); string kind = p.pet; bool did = false;
        Color fx = new Color(1f, 0.85f, 0.5f);
        switch (kind)
        {
            case "slime":
                if (p.hp < p.maxHp) { p.Heal(p.maxHp * 0.2f * k); did = true; fx = new Color(0.5f, 1f, 0.6f); AHFx.Pop(p.transform.position + Vector3.up, 2f, fx, 0.6f); }
                else if (g.ui != null) g.ui.Toast("You are already at full health");
                break;
            case "toad": case "pumpkin_slime":
                foreach (var m in Hunting(g, from, 7f)) { m.AddStatus(AHStatus.Stun, 2.5f, 0f, p); did = true; }
                fx = new Color(0.6f, 1f, 0.4f); break;
            case "parrot":
                foreach (var m in Hunting(g, from, 9f)) { m.AddStatus(AHStatus.Slow, 5f, 0f, p); did = true; }
                fx = new Color(0.4f, 0.9f, 1f); break;
            case "drake":
                foreach (var m in Hunting(g, from, 7f))
                {
                    m.Hurt(Mathf.RoundToInt(p.Damage() * 0.8f * k), p);
                    if (!m.dead) m.AddStatus(AHStatus.Burn, 4f, p.Damage() * 0.08f * k, p);
                    AHFx.Pop(m.transform.position + Vector3.up, 1.4f, new Color(1f, 0.5f, 0.15f)); did = true;
                }
                fx = new Color(1f, 0.5f, 0.15f); break;
            default:
                {
                    // fox, snow fox, owls and the rest: a single big hit on the nearest thing hunting you
                    var m = Nearest(g, from, 12f);
                    if (m != null)
                    {
                        float mult = kind == "fox" || kind == "snow_fox" ? 1.5f : 1.0f;
                        m.Hurt(Mathf.RoundToInt(p.Damage() * mult * k), p);
                        if (!m.dead && kind == "snow_fox") m.AddStatus(AHStatus.Slow, 4f, 0f, p);
                        if (!m.dead && (kind == "owl" || kind == "lantern_owl")) m.AddStatus(AHStatus.Stun, 1.5f, 0f, p);
                        if (petGo != null) petGo.transform.position = m.transform.position - (m.transform.position - p.transform.position).normalized * 1f;
                        AHFx.Pop(m.transform.position + Vector3.up, 1.6f, fx); did = true;
                    }
                    break;
                }
        }
        if (!did) { if (g.ui != null && kind != "slime") g.ui.Toast("Nothing is hunting you"); return; }
        readyAt = Time.time + Cooldown;
        AHFx.Pop(from + Vector3.up * 0.6f, 1.8f, fx, 0.5f);
        AHSound.Play("swing");
        if (g.ui != null) g.ui.Toast(AHComp.PetName(kind) + ": " + SkillName(kind) + "!", 1.6f);
    }
}

public partial class AHUI
{
    RectTransform petBtn; Text petBtnT;
    void BuildPetHud()
    {
        petBtn = Img("PetSkillBtn", transform, circle, new Vector2(1, 0), new Vector2(-58, 560), new Vector2(64, 64), new Color(0.38f, 0.24f, 0.12f, 0.92f));
        Img("Rim", petBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), new Color(1f, 0.82f, 0.5f, 1f));
        petBtnT = Center(Label(petBtn, "T", "PET", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 24), Color.white));
        taps.Add(new TapBtn { rt = petBtn, act = () => AHPetSkills.Use(g) });
    }
    void PetHudTick()
    {
        var p = g.player; if (petBtn == null || p == null) return;
        bool on = AHPetSkills.Has(p) && Modal == 0 && !PhotoOn && !p.mounted;
        if (petBtn.gameObject.activeSelf != on) petBtn.gameObject.SetActive(on);
        if (!on) return;
        float left = AHPetSkills.Left;
        petBtnT.text = left > 0f ? Mathf.CeilToInt(left).ToString() : "PET";
        petBtn.GetComponent<Image>().color = left > 0f ? new Color(0.2f, 0.16f, 0.12f, 0.8f) : new Color(0.38f, 0.24f, 0.12f, 0.92f);
    }
}
