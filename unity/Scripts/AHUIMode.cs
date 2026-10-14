// Ashen Hollow: FIGHT and SKILL. The green action button in the middle of the screen (LIGHT FIRE, CHOP, TALK) is
// gone: the big seal does everything. A small swap button beside the spells changes what it is for:
//   FIGHT (the usual): the seal attacks. Out of danger, by a person, a door or a town station, it turns blue and
//          says what it does (TALK, ENTER, SMELT...) and a tap does that instead.
//   SKILL: the spells are put away and the seal turns green: CHOP, MINE, FISH, PICK, SKIN, LIGHT FIRE. A tap does
//          the job, so you never swing at a wolf when you meant to cut the tree beside it.
// Something hits you while you are skilling: back to FIGHT at once, with a word on the screen. In FIGHT mode the swap
// button glows when there is something to gather where you stand.
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RectTransform modeBtn;
    Image modeArrows, modeGlow;
    Text modeText, atkLabelText;
    bool skillMode, sealStyled;
    string sealAct;        // what the seal does when tapped; null: it attacks
    TapBtn sealTap;
    float hurtSeen = -99f;
    static readonly Vector2 ModePos = new Vector2(-108f, 240f);   // up and to the right of the seal, clear of the spells and the potions

    public bool SkillMode { get { return skillMode; } }

    void BuildModeSwitch()
    {
        modeBtn = Img("ModeBtn", transform, circle, new Vector2(1, 0), ModePos, new Vector2(56, 56), new Color(0.55f, 0.16f, 0.12f, 0.95f));
        // the glow sits behind the button (a sibling drawn first), a soft green halo round it
        var gl = Img("ModeGlow", transform, circle, new Vector2(1, 0), ModePos, new Vector2(84, 84), new Color(0.5f, 1f, 0.55f, 0f));
        gl.SetSiblingIndex(modeBtn.GetSiblingIndex()); modeGlow = gl.GetComponent<Image>(); modeGlow.raycastTarget = false;
        modeArrows = Img("Arrows", modeBtn, MakeSwap(), new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(30, 30), Color.white).GetComponent<Image>();
        modeText = Center(Label(modeBtn, "Mode", "FIGHT", 12, TextAnchor.MiddleCenter, new Vector2(0, -13), new Vector2(60, 18), Color.white));
        modeText.rectTransform.anchoredPosition = new Vector2(0, -13); modeText.fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = modeBtn, act = () => SetSkillMode(!skillMode, true) });
        sealTap = new TapBtn { rt = atkBtn, act = PressSeal };
        try { skillMode = AHPrefs.GetInt("ah_set_skillmode", 0) == 1; } catch { }
        ShowMode();
    }

    public void SetSkillMode(bool on, bool say)
    {
        if (skillMode == on) return;
        skillMode = on;
        try { AHPrefs.SetInt("ah_set_skillmode", on ? 1 : 0); } catch { }
        ShowMode();
        if (say) Toast(on ? "Skilling: the seal chops, mines, fishes and gathers" : "Fighting: the seal attacks", 1.6f);
    }

    void ShowMode()
    {
        if (modeBtn == null) return;
        modeBtn.GetComponent<Image>().color = skillMode ? new Color(0.18f, 0.5f, 0.24f) : new Color(0.62f, 0.14f, 0.1f);
        modeText.text = skillMode ? "SKILL" : "FIGHT";
        var sps = g != null && g.player != null && g.player.cls != null ? g.player.Spells : null;
        for (int i = 0; i < 6; i++)
            if (spellBtns[i] != null) spellBtns[i].rt.gameObject.SetActive(!skillMode && (i < 5 || (sps != null && sps.Length > 5)));
    }

    static bool FieldJob(AHSpot s) { return s != null && (s.kind == "chop" || s.kind == "mine" || s.kind == "herb" || s.kind == "fish"); }

    // a monster on you or close by: no talking or gathering from the seal, it is for fighting
    bool Danger(AHPlayer p)
    {
        if (Time.time - p.LastHurt < 3f) return true;
        Vector3 pp = p.transform.position;
        foreach (var m in g.mobs)
        {
            if (m == null || m.dead) continue;
            Vector3 d = m.transform.position - pp; d.y = 0f; float r = d.magnitude;
            if (r < 6f || (m.Chasing && r < 18f)) return true;
        }
        return false;
    }

    void UpdateSeal(AHPlayer p, string ignored)
    {
        if (atkLabelText == null) return;
        if (!sealStyled)
        {
            sealStyled = true;
            atkLabelText.resizeTextForBestFit = true; atkLabelText.resizeTextMinSize = 11; atkLabelText.resizeTextMaxSize = 22;
            atkLabelText.horizontalOverflow = HorizontalWrapMode.Wrap; atkLabelText.verticalOverflow = VerticalWrapMode.Truncate;
            atkLabelText.rectTransform.sizeDelta = new Vector2(100, 60);
        }
        string act = null; bool fieldHere = false;
        if (p != null && !p.dead && Modal == 0)
        {
            // hit while skilling: back to the fight
            if (skillMode && p.LastHurt > hurtSeen + 0.01f && Time.time - p.LastHurt < 0.5f) { SetSkillMode(false, false); Toast("Under attack! Back to FIGHT", 1.6f); }
            hurtSeen = p.LastHurt;
            var n = NearNpc();
            var us = AHGather.Nearest(p.transform.position);
            n = PersonOrThing(n, us);
            var sk = p.SkinTarget();
            fieldHere = FieldJob(us) || sk != null;
            if (p.Busy) act = p.ActionName.ToUpperInvariant() + "\n" + Mathf.RoundToInt(p.ActionFrac * 100) + "%";
            else if (skillMode)
            {
                if (n != null) act = n.Label.ToUpperInvariant();
                else if (sk != null) act = "SKIN";
                else if (us != null) act = AHGather.Label(us).ToUpperInvariant();
                else if (AHGather.CanLight(p) && !AHInterior.Inside) act = "LIGHT FIRE";
                else act = "SKILL";
            }
            else if (!Danger(p))
            {
                if (n != null) act = n.Label.ToUpperInvariant();
                else if (us != null && !FieldJob(us) && sk == null) act = AHGather.Label(us).ToUpperInvariant();
            }
            if (p.Busy && !skillMode && Danger(p)) act = null;   // gathering in FIGHT mode is impossible anyway; never hide the attack in a fight
        }
        sealAct = act;
        atkLabelText.text = act ?? "ATTACK";
        // the swap button glows when there is something to gather here and you are in FIGHT mode
        if (modeGlow != null)
        {
            float a = !skillMode && fieldHere ? 0.25f + 0.3f * Mathf.PingPong(Time.unscaledTime * 1.6f, 1f) : 0f;
            modeGlow.color = new Color(0.5f, 1f, 0.55f, a);
            modeBtn.gameObject.SetActive(Modal == 0); modeGlow.gameObject.SetActive(Modal == 0);
        }
    }

    void PressSeal()
    {
        var p = g.player; if (p == null || p.Busy) return;
        if (skillMode) { OnAction(); return; }
        // FIGHT mode: only people, doors and stations come here
        var n = PersonOrThing(NearNpc(), AHGather.Nearest(p.transform.position));
        if (n != null) { g.TalkTo(n); return; }
        var s = AHGather.Nearest(p.transform.position);
        if (s != null && !FieldJob(s)) AHGather.Start(p, s);
    }

    // two arrows, one over the other, pointing opposite ways
    static Sprite MakeSwap()
    {
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float a = 0f;
                for (int k = 0; k < 2; k++)
                {
                    float cy = k == 0 ? 44f : 20f; int dir = k == 0 ? 1 : -1;
                    float fx = dir > 0 ? x : N - 1 - x;   // the arrow points to +x for the top one
                    float dy = Mathf.Abs(y - cy);
                    // shaft
                    if (fx >= 8f && fx <= 44f) a = Mathf.Max(a, Mathf.Clamp01(4.5f - dy));
                    // head: a triangle from x 40 to 58
                    if (fx >= 38f && fx <= 58f) a = Mathf.Max(a, Mathf.Clamp01((58f - fx) * 0.75f - dy + 1f));
                }
                px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }
}
