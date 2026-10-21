// Ashen Hollow: the gathering knack's bar (AHGather.Strike). While you chop, mine, fish or pick herbs, a bar fills
// above the action button with a gold mark on it: tap the button as it crosses the gold for a perfect strike.
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RectTransform knack, knackGold, knackMark; Text knackT;
    const float KnackW = 360f;

    void BuildKnackHud()
    {
        knack = Img("KnackBar", transform, white, new Vector2(0.5f, 0f), new Vector2(0, 236), new Vector2(KnackW, 18), new Color(0.08f, 0.06f, 0.05f, 0.8f));
        knackGold = Img("Gold", knack, white, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(40, 18), new Color(1f, 0.8f, 0.25f, 0.95f));
        knackMark = Img("Mark", knack, white, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5, 28), Color.white);
        knackT = Center(Label(knack, "T", "", 17, TextAnchor.MiddleCenter, new Vector2(0, 26), new Vector2(KnackW + 80, 26), new Color(1f, 0.9f, 0.65f)));
        knack.gameObject.SetActive(false);
    }

    void KnackHudTick(float dt)
    {
        if (knack == null) return;
        if (AHGather.knackSayT > 0f) AHGather.knackSayT -= dt;
        var p = g.player;
        if (AHFishing.Active && p != null && Modal == 0 && !PhotoOn) { FishHud(); return; }
        bool on = p != null && Modal == 0 && !PhotoOn && (AHGather.KnackOn || AHGather.knackSayT > 0f);
        if (knack.gameObject.activeSelf != on) knack.gameObject.SetActive(on);
        if (!on) return;
        knack.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.8f); knackGold.gameObject.SetActive(true); knackMark.gameObject.SetActive(true);
        float w = AHGather.knackW * KnackW;
        knackGold.anchoredPosition = new Vector2(AHGather.knackAt * KnackW, 0f); knackGold.sizeDelta = new Vector2(w, 18f);
        knackGold.pivot = new Vector2(0.5f, 0.5f);
        knackMark.pivot = new Vector2(0.5f, 0.5f);
        knackMark.anchoredPosition = new Vector2(Mathf.Clamp01(AHGather.KnackOn ? AHGather.Frac : 1f) * KnackW, 0f);
        bool hit = AHGather.knackHit;
        knackGold.GetComponent<Image>().color = hit ? new Color(0.55f, 1f, 0.45f, 1f) : new Color(1f, 0.8f, 0.25f, AHGather.knackTried ? 0.45f : 0.95f);
        knackT.text = AHGather.knackSayT > 0f ? AHGather.knackSay : AHGather.knackTried ? "" : "Tap the action button in the gold";
        knackT.color = AHGather.knackSay == "Perfect!" && AHGather.knackSayT > 0f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.9f, 0.65f);
    }

    // fishing: waiting shows nothing but a word; a bite flashes the bar; a big fish fights on the reel bar
    // (a needle sweeps it, tap while it is in the green: three pulls land it, three misses snap the line)
    void FishHud()
    {
        var ph = AHFishing.phase;
        bool bar = ph == AHFishing.Phase.Reel || ph == AHFishing.Phase.Bite;
        bool on = bar || AHFishing.sayT > 0f;
        if (knack.gameObject.activeSelf != on) knack.gameObject.SetActive(on);
        if (!on) return;
        knackGold.pivot = new Vector2(0f, 0.5f); knackMark.pivot = new Vector2(0.5f, 0.5f);
        knack.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, bar ? 0.8f : 0f);
        knackGold.gameObject.SetActive(bar); knackMark.gameObject.SetActive(ph == AHFishing.Phase.Reel);
        if (ph == AHFishing.Phase.Reel)
        {
            knackGold.anchoredPosition = new Vector2(AHFishing.zoneA * KnackW, 0f); knackGold.sizeDelta = new Vector2(AHFishing.zoneW * KnackW, 18f);
            knackGold.GetComponent<Image>().color = new Color(0.4f, 0.95f, 0.45f, 0.95f);
            knackMark.anchoredPosition = new Vector2(AHFishing.needle * KnackW, 0f);
            string pips = ""; for (int i = 0; i < 3; i++) pips += i < AHFishing.hits ? "●" : "○";
            knackT.text = (AHFishing.sayT > 0f ? AHFishing.say + "  " : "Tap in the green  ") + pips;
        }
        else if (ph == AHFishing.Phase.Bite)
        {
            knackGold.anchoredPosition = Vector2.zero; knackGold.sizeDelta = new Vector2(KnackW, 18f);
            knackGold.GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f, 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 8f, 1f));
            knackT.text = "A bite! Tap now!";
        }
        else knackT.text = AHFishing.say;
        knackT.color = new Color(0.75f, 0.92f, 1f);
    }
}
