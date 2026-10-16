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
        bool on = p != null && Modal == 0 && !PhotoOn && (AHGather.KnackOn || AHGather.knackSayT > 0f);
        if (knack.gameObject.activeSelf != on) knack.gameObject.SetActive(on);
        if (!on) return;
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
}
