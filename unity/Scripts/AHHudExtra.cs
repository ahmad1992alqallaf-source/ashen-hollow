// Ashen Hollow: a small pill at the top of the screen. In the Ashen Deep it shows the floor, the run's clock and the foes
// left on the floor; elsewhere, what the Ashen King story asks of you next (tap it to open the story).
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RectTransform pill; Text pillT; float pillTick;
    void BuildExtraHud()
    {
        pill = Img("StoryPill", transform, white, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(520, 34), new Color(0.08f, 0.06f, 0.05f, 0.78f));
        Img("Edge", pill, white, new Vector2(0.5f, 0f), new Vector2(0, 1), new Vector2(520, 2), new Color(1f, 0.75f, 0.35f, 0.9f));
        pillT = Center(Label(pill, "T", "", 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(510, 30), new Color(1f, 0.88f, 0.6f)));
        taps.Add(new TapBtn { rt = pill, act = () => { if (AHGame.AreaId != AHDeep.Area) OpenStory(); } });
        pill.gameObject.SetActive(false);
    }

    void ExtraHudTick(float dt)
    {
        if (pill == null) return;
        pillTick -= dt; if (pillTick > 0f) return; pillTick = 0.25f;
        var p = g.player; string s = null;
        if (p != null && Modal == 0 && !PhotoOn)
        {
            if (AHGame.AreaId == AHDeep.Area && AHDeep.InRun)
            {
                int left = AHDeep.Left;
                s = "ASHEN DEEP · floor " + AHDeep.Floor + "/" + AHDeep.Floors + " · " + AHDeep.Clock(AHDeep.Elapsed) + " · " + (left > 0 ? left + (left == 1 ? " foe left" : " foes left") : "stairs open");
            }
            else if (!AHStory.Done(p))
            {
                var c = AHStory.Cur(p);
                if (c != null && p.level >= c.lvl) s = "STORY · " + AHStory.Status(p);
            }
        }
        bool on = s != null;
        if (pill.gameObject.activeSelf != on) pill.gameObject.SetActive(on);
        if (on)
        {
            pillT.text = s;
            float w = Mathf.Clamp(s.Length * 9.2f + 40f, 220f, 760f);
            pill.sizeDelta = new Vector2(w, 34f); pillT.rectTransform.sizeDelta = new Vector2(w - 10f, 30f);
            var e = pill.Find("Edge") as RectTransform; if (e != null) e.sizeDelta = new Vector2(w, 2f);
        }
    }
}
