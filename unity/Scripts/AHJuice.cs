// Ashen Hollow: how a fight feels. Big blows freeze the action for a heartbeat (hit-stop) and shake the camera; a boss's
// slam landing near you shakes it harder. Hits in a row build a combo (shown over the ATTACK button): every 5 hits adds
// 1% damage, up to +10%, and it drops if you stop for 3 seconds. Big numbers pop bigger. And in Hollow Meadow a
// Training Golem stands by the square: it never hits back and never falls, and the line at the top shows your damage
// per second while you work on it.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(30000)]   // after the game places the camera
public class AHJuice : MonoBehaviour
{
    public static AHJuice I;
    static float shakeT, shakeAmp, stopUntil;
    public static int Combo; static float comboT;
    public static float ComboK { get { return 1f + Mathf.Min(10, Combo / 5) * 0.01f; } }

    public static void Ensure(AHGame g) { if (I == null) I = g.gameObject.AddComponent<AHJuice>(); }

    public static void Shake(float amp, float time) { if (amp >= shakeAmp || shakeT <= 0f) { shakeAmp = amp; } shakeT = Mathf.Max(shakeT, time); }

    // a hit you landed: combo, and a short freeze and shake for a big one
    public static void OnHit(AHGame g, AHMob m, int dmg)
    {
        Combo++; comboT = 3f;
        bool big = m.type.hp > 0 && dmg >= m.type.hp * 0.12f || dmg >= Mathf.Max(30, g.player.Damage() * 2.2f);
        if (big && Time.timeScale > 0.5f && I != null) { I.StartCoroutine(I.HitStop(0.06f)); Shake(0.12f, 0.18f); }
        if (IsDummy(m)) Dummy(dmg);
    }
    public static void OnHurt(AHGame g, float dmg)
    {
        var p = g.player; if (p == null) return;
        if (dmg >= p.maxHp * 0.15f) Shake(0.22f, 0.3f); else if (dmg >= p.maxHp * 0.07f) Shake(0.08f, 0.15f);
    }

    System.Collections.IEnumerator HitStop(float t)
    {
        if (Time.unscaledTime < stopUntil) yield break;
        stopUntil = Time.unscaledTime + t + 0.25f;
        float was = Time.timeScale; Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(t);
        if (Time.timeScale == 0.05f) Time.timeScale = was;
    }

    void LateUpdate()
    {
        var g = AHGame.I; if (g == null || g.cam == null) return;
        float dt = Time.unscaledDeltaTime;
        if (comboT > 0f) { comboT -= dt; if (comboT <= 0f) Combo = 0; }
        if (shakeT > 0f && Time.timeScale > 0f && !AHCine.Active)
        {
            shakeT -= dt; float k = shakeAmp * Mathf.Clamp01(shakeT * 4f);
            g.cam.transform.position += new Vector3(Random.Range(-k, k), Random.Range(-k, k) * 0.6f, Random.Range(-k, k));
        }
        DummyTick(g, dt);
    }

    // ---- the Training Golem ----
    public const string DummyId = "golem";
    static AHMob dummy; static float dmgSum, firstHit, lastHit;
    public static bool Training { get { return dummy != null && Time.time - lastHit < 4f; } }
    public static float Dps { get { float t = Mathf.Max(1f, lastHit - firstHit); return dmgSum / t; } }
    public static string TrainingText { get { return "TRAINING GOLEM · " + Mathf.RoundToInt(Dps) + " damage a second · " + Mathf.RoundToInt(dmgSum).ToString("#,0") + " in " + Mathf.RoundToInt(lastHit - firstHit) + " s"; } }
    static void Dummy(int dmg)
    {
        if (Time.time - lastHit > 4f) { dmgSum = 0f; firstHit = Time.time; }
        dmgSum += dmg; lastHit = Time.time;
    }
    public static void SetupDummy(AHGame g)
    {
        dummy = null; if (AHGame.AreaId != "meadow" || g.data == null || g.data.spawn == null) return;
        Vector3 at = g.Resolve(g.W(g.data.spawn.x + 10f, g.data.spawn.z - 6f), 1.2f);
        var t = new AHMobType { id = DummyId, model = "Web/mGolem", name = "Training Golem", lvl = Mathf.Max(1, g.player.level), hp = 999999, dmg = 0, xp = 0, gold = 0, speed = 0f, radius = 1f, aggro = 0f, atkCd = 99f, respawn = 1e9f, noSkin = true };
        dummy = AHMob.Create(g, t, at); g.mobs.Add(dummy);
    }
    static void DummyTick(AHGame g, float dt)
    {
        // the golem mends itself as soon as you stop
        if (dummy != null && !dummy.dead && Time.time - lastHit > 4f && dummy.hp < dummy.type.hp) dummy.hp = dummy.type.hp;
    }
    public static bool IsDummy(AHMob m) { return m != null && m == dummy; }
}

public partial class AHUI
{
    Text comboT;
    void BuildCombo()
    {
        comboT = Label(transform, "Combo", "", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(220, 34), new Color(1f, 0.8f, 0.3f));
        comboT.rectTransform.anchorMin = comboT.rectTransform.anchorMax = comboT.rectTransform.pivot = new Vector2(1f, 0f);
        comboT.rectTransform.anchoredPosition = new Vector2(-140, 250); comboT.fontStyle = FontStyle.Bold;
        var ol = comboT.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.2f, 0.05f, 0f, 0.9f); ol.effectDistance = new Vector2(1.5f, -1.5f);
    }
    void ComboTick()
    {
        if (comboT == null) return;
        int c = AHJuice.Combo; bool on = c >= 3 && Modal == 0;
        if (comboT.gameObject.activeSelf != on) comboT.gameObject.SetActive(on);
        if (on) { comboT.text = c + " HIT COMBO" + (AHJuice.ComboK > 1.001f ? "  +" + Mathf.RoundToInt((AHJuice.ComboK - 1f) * 100f) + "%" : ""); comboT.fontSize = 24 + Mathf.Min(10, c / 5); }
    }
}
