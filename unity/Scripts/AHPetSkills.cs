// Ashen Hollow: your pet's own move. With a pet out, a PET button shows above LEAP: tap it and the pet
// does its signature move, then rests for 40 seconds. It grows stronger as the pet levels and evolves.
//   toad / pumpkin slime: Croak (stuns what hunts you nearby)      slime: Jelly mend (heals you)
//   fox: Pounce (a big bite)       snow fox: Frost pounce (a bite that chills)       owl / lantern owl: Dive (hits and dazes)
//   parrot: Squawk (slows everything hunting you)                    drake: Fire breath (burns everything in front)
//   bee: Pollinate (heals you and slows what hunts you)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHPetSkills
{
    public const float Cooldown = 40f;
    public const int MinLevel = 1;
    static float readyAt;

    public static bool Has(AHPlayer p) { return p != null && p.pet != null && !AHProfPets.Is(p.pet) && AHComp.PetLv(p, p.pet) >= MinLevel; }
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
            case "bee": return "Pollinate";
            case "rock_golem": return "Stoneskin";
            case "shadow_cat": return "Shadowstep";
            case "spirit_wisp": return "Mana spring";
            case "phoenix_chick": return "Ember burst";
            case "spell_book": return "Arcane volley";
            default: return "Rally";
        }
    }

    static float Power(AHPlayer p) { return (1f + AHComp.PetLv(p, p.pet) * 0.03f + AHComp.EvoStage(p, p.pet) * 0.15f) * AHEggs.PowK(p); }   // (and its Power stat)

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

    // the pet uses its knack by itself once it is ready and there is something to use it on (it has no button of its
    // own any more: the PETS button at the top has the pet's page)
    public static void Auto(AHGame g)
    {
        var p = g.player; if (p == null || p.dead || p.mounted || !Has(p) || Left > 0f) return;
        bool worth = p.pet == "slime" || p.pet == "bee" || p.pet == "spirit_wisp" ? p.hp < p.maxHp * 0.6f || (p.pet == "spirit_wisp" && p.maxMana > 0f && p.mana < p.maxMana * 0.3f) : false;
        if (!worth) { var petGo = GameObject.Find("Pet"); Vector3 from = petGo != null ? petGo.transform.position : p.transform.position; foreach (var m in Hunting(g, from, 7f)) { worth = true; break; } }
        if (worth) Use(g);
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
            case "bee":
                // Pollinate: a cloud of pollen that heals you now and slows everything hunting you
                if (p.hp < p.maxHp) { p.Heal(p.maxHp * 0.12f * k); did = true; }
                foreach (var m in Hunting(g, from, 7f)) { m.AddStatus(AHStatus.Slow, 3f, 0f, p); did = true; }
                fx = new Color(1f, 0.85f, 0.25f); if (did) AHFx.Pop(p.transform.position + Vector3.up, 2.2f, fx, 0.7f);
                if (!did && g.ui != null) g.ui.Toast("You are already at full health");
                break;
            case "rock_golem":
                // Stoneskin: a shell of stone over you that soaks up blows for 8 seconds
                foreach (var m in Hunting(g, from, 8f)) { did = true; break; }
                if (did) { p.shield = Mathf.Max(p.shield, p.maxHp * 0.18f * k); p.shieldT = 8f; fx = new Color(0.55f, 0.75f, 1f); AHFx.Ring(p.transform.position, 0.3f, 1.4f, fx, 0.6f); }
                break;
            case "shadow_cat":
                {
                    // Shadowstep: it vanishes and strikes the nearest foe from behind, a heavy blow that slows
                    var m = Nearest(g, from, 12f);
                    if (m != null)
                    {
                        m.Hurt(Mathf.RoundToInt(p.Damage() * 1.8f * k), p); if (!m.dead) m.AddStatus(AHStatus.Slow, 3f, 0f, p);
                        if (petGo != null) { AHSpark.Burst(petGo.transform.position + Vector3.up * 0.3f, new Color(0.5f, 0.2f, 0.9f), 14, 2.5f, 0.5f, 0.14f); petGo.transform.position = m.transform.position + m.transform.forward * -1.1f; }
                        fx = new Color(0.6f, 0.25f, 1f); AHFx.Pop(m.transform.position + Vector3.up, 1.6f, fx); did = true;
                    }
                    break;
                }
            case "spirit_wisp":
                // Mana spring: spirit-fire that heals you and fills your mana
                if (p.hp < p.maxHp || (p.maxMana > 0f && p.mana < p.maxMana))
                {
                    p.Heal(p.maxHp * 0.1f * k); if (p.maxMana > 0f) p.mana = Mathf.Min(p.maxMana, p.mana + p.maxMana * 0.25f * k);
                    did = true; fx = new Color(0.4f, 0.6f, 1f); AHFx.Pop(p.transform.position + Vector3.up, 2.2f, fx, 0.7f);
                }
                else if (g.ui != null) g.ui.Toast("You are already at full health and mana");
                break;
            case "phoenix_chick":
                // Ember burst: a ring of fire round it that burns everything hunting you
                foreach (var m in Hunting(g, from, 6f))
                {
                    m.Hurt(Mathf.RoundToInt(p.Damage() * 0.6f * k), p);
                    if (!m.dead) m.AddStatus(AHStatus.Burn, 4f, p.Damage() * 0.06f * k, p);
                    did = true;
                }
                fx = new Color(1f, 0.55f, 0.15f); if (did) AHSpark.Ring(from + Vector3.up * 0.3f, fx, 30, 5f);
                break;
            case "spell_book":
                // Arcane volley: its pages fly out as glowing bolts at up to three foes hunting you
                {
                    var hl = Hunting(g, from, 10f); int shots = 0;
                    foreach (var m in hl)
                    {
                        if (shots++ >= 3) break;
                        m.Hurt(Mathf.RoundToInt(p.Damage() * 0.7f * k), p);
                        AHFx.Pop(m.transform.position + Vector3.up, 1.2f, new Color(0.7f, 0.5f, 1f)); did = true;
                    }
                    fx = new Color(0.7f, 0.5f, 1f); if (did) AHSpark.Ring(from + Vector3.up * 0.5f, fx, 18, 3f);
                }
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
        if (!did) { if (g.ui != null && kind != "slime" && kind != "bee" && kind != "spirit_wisp") g.ui.Toast("Nothing is hunting you"); return; }
        readyAt = Time.time + Cooldown * AHEggs.CdK(p);   // (sooner with its Spirit)
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
        petBtn = Img("PetSkillBtn", transform, circle, new Vector2(1, 0), new Vector2(-58, 576), new Vector2(64, 64), new Color(0.38f, 0.24f, 0.12f, 0.92f));
        Img("Rim", petBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), new Color(1f, 0.82f, 0.5f, 1f));
        petBtnT = Center(Label(petBtn, "T", "PET", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 24), Color.white));
        taps.Add(new TapBtn { rt = petBtn, act = () => AHPetSkills.Use(g) });
    }
    void PetHudTick()
    {
        var p = g.player; if (petBtn == null || p == null) return;
        bool on = false;   // (no button: the pet acts on its own)
        if (petBtn.gameObject.activeSelf != on) petBtn.gameObject.SetActive(on);
        if (Modal == 0) AHPetSkills.Auto(g);
        if (!on) return;
        float left = AHPetSkills.Left;
        petBtnT.text = left > 0f ? Mathf.CeilToInt(left).ToString() : "PET";
        petBtn.GetComponent<Image>().color = left > 0f ? new Color(0.2f, 0.16f, 0.12f, 0.8f) : new Color(0.38f, 0.24f, 0.12f, 0.92f);
    }
}
