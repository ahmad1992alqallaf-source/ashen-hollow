// Ashen Hollow: the HUD and windows for AHFun4: the Gallop button (riding), the daily wheel, the fishing journal and
// the weekly legend's board.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    void Fun4Tick()
    {
        AHFun4.Tick(g, Time.deltaTime);
        GallopTick(); SpinTick();
    }

    // ===================================== Gallop (riding) =====================================
    RectTransform gallopBtn; Image gallopCool;
    void GallopTick()
    {
        var p = g.player; if (p == null) return;
        bool on = AHFun4.CanGallop(p) && Modal == 0 && !PhotoOn;
        if (gallopBtn == null)
        {
            if (!on) return;
            var ride = FindUI("RideBtn");
            gallopBtn = Img("GallopBtn", transform, ornDisc != null ? ornDisc : circle, ride != null ? ride.anchorMin : Vector2.zero, ride != null ? ride.anchoredPosition + new Vector2(0f, 64f) : new Vector2(178f, 98f), new Vector2(52, 52), new Color(0.05f, 0.08f, 0.15f, 0.7f));
            Img("Rim", gallopBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(55, 55), new Color(1f, 0.85f, 0.5f));
            var t = Center(Label(gallopBtn, "T", AHLoc.T("GALLOP", "عدو"), 11, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(52, 20), Color.white)); t.fontStyle = FontStyle.Bold;
            gallopCool = Img("Cool", gallopBtn, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48, 48), new Color(0f, 0f, 0f, 0.55f)).GetComponent<Image>();
            gallopCool.type = Image.Type.Filled; gallopCool.fillMethod = Image.FillMethod.Radial360; gallopCool.fillOrigin = (int)Image.Origin360.Top; gallopCool.fillClockwise = false;
            taps.Add(new TapBtn { rt = gallopBtn, act = () => AHFun4.Gallop(g) });
        }
        if (gallopBtn.gameObject.activeSelf != on) gallopBtn.gameObject.SetActive(on);
        if (on) gallopCool.fillAmount = AHFun4.GallopLeft01(p);
    }

    // ===================================== the daily wheel =====================================
    RectTransform spinRoot, spinDisc, spinBtn; Text spinLabel; float spinT = -1f, spinFrom, spinTo; int spinPick;
    static Sprite wheelSp;
    static Sprite WheelSprite()
    {
        if (wheelSp != null) return wheelSp;
        const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        int n = AHFun4.Wheel.Length;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f, r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f) { t.SetPixel(x, y, Color.clear); continue; }
            float a = Mathf.Repeat(Mathf.Atan2(u, v) * Mathf.Rad2Deg, 360f);   // 0 at the top, clockwise
            int i = Mathf.FloorToInt(a / (360f / n)) % n;
            Color c = AHFun4.Wheel[i].col * (i % 2 == 0 ? 1f : 0.86f); c.a = 1f;
            float edge = Mathf.Abs(Mathf.Repeat(a, 360f / n)); if (edge < 0.9f || r > 0.95f) c = new Color(0.95f, 0.88f, 0.65f, 1f);   // the gold lines between slices and round the rim
            if (r < 0.14f) c = new Color(0.12f, 0.1f, 0.08f, 1f);
            t.SetPixel(x, y, c);
        }
        t.Apply(); wheelSp = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f); return wheelSp;
    }
    public void OpenWheel()
    {
        if (spinRoot == null)
        {
            spinRoot = Box("DailyWheel", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            spinRoot.anchorMin = Vector2.zero; spinRoot.anchorMax = Vector2.one; spinRoot.offsetMin = spinRoot.offsetMax = Vector2.zero;
            spinRoot.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            var card = Img("Card", spinRoot, cgCard != null ? cgCard : white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 560), Color.white);
            if (cgCard != null) card.GetComponent<Image>().type = Image.Type.Sliced;
            var title = Label(card, "T", AHLoc.T("Daily wheel", "عجلة اليوم"), 28, TextAnchor.UpperCenter, Vector2.zero, new Vector2(520, 36), CGGold); title.font = TitleFont;
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f); title.rectTransform.pivot = new Vector2(0.5f, 1f); title.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            spinDisc = Img("Disc", card, WheelSprite(), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(360, 360), Color.white);
            // the slice names round the wheel
            int n = AHFun4.Wheel.Length;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) * 360f / n; var holder = Box("S" + i, spinDisc, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
                holder.localEulerAngles = new Vector3(0, 0, -a);
                var lt = Label(holder, "L", AHLoc.T(AHFun4.Wheel[i].en, AHFun4.Wheel[i].ar), 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(110, 20), new Color(0.1f, 0.07f, 0.04f));
                lt.fontStyle = FontStyle.Bold; lt.rectTransform.anchorMin = lt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); lt.rectTransform.anchoredPosition = new Vector2(0f, 118f); Fit(lt, 9);
            }
            var pointer = Img("Pointer", card, white, new Vector2(0.5f, 0.5f), new Vector2(0f, 206f), new Vector2(26, 26), new Color(1f, 0.92f, 0.6f)); pointer.localEulerAngles = new Vector3(0, 0, 45f);
            spinBtn = Img("Spin", card, cgPillSky != null ? cgPillSky : white, new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(200, 52), Color.white);
            if (cgPillSky != null) spinBtn.GetComponent<Image>().type = Image.Type.Sliced;
            spinLabel = Center(Label(spinBtn, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(196, 44), Color.white)); spinLabel.fontStyle = FontStyle.Bold;
            taps.Add(new TapBtn { rt = spinBtn, layer = 13, act = () => { if (spinT < 0f && AHFun4.WheelReady) StartSpin(); } });
            var close = Img("Close", card, circle, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(40, 40), new Color(0.12f, 0.16f, 0.26f, 0.95f));
            Center(Label(close, "T", "✕", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40, 36), Color.white));
            taps.Add(new TapBtn { rt = close, layer = 13, act = () => { if (spinT < 0f) spinRoot.gameObject.SetActive(false); } });
        }
        spinLabel.text = AHFun4.WheelReady ? AHLoc.T("SPIN", "أدِر") : AHLoc.T("Come back tomorrow", "عد غدًا");
        spinRoot.gameObject.SetActive(true); spinRoot.SetAsLastSibling(); ptrs.Clear();
    }
    public bool SpinOpen { get { return spinRoot != null && spinRoot.gameObject.activeSelf; } }
    void StartSpin()
    {
        spinPick = AHFun4.PickSlice(); int n = AHFun4.Wheel.Length;
        float target = (spinPick + 0.5f) * 360f / n + UnityEngine.Random.Range(-12f, 12f);   // the slice's middle (a little either way)
        spinFrom = spinDisc.localEulerAngles.z; spinTo = 360f * 5f + target; spinT = 0f; spinLabel.text = "…";
        AHSound.Play("whoosh");
    }
    void SpinTick()
    {
        if (spinT < 0f || spinDisc == null) return;
        spinT += Time.unscaledDeltaTime; float k = Mathf.Clamp01(spinT / 4f), e = 1f - Mathf.Pow(1f - k, 3f);
        spinDisc.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(spinFrom, spinTo, e));
        if (k >= 1f) { spinT = -1f; spinRoot.gameObject.SetActive(false); AHFun4.GiveSlice(g, spinPick); }
    }

    // ===================================== the fishing journal =====================================
    public void OpenFishJournal() { wkMode = "fishj"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderFishJournal(AHPlayer p)
    {
        var lands = new List<string>(AHFish.Lands);
        // your own lands first (the ones you know), then the rest
        lands.Sort((a, b) => (p.prog.found.Contains(b) ? 1 : 0).CompareTo(p.prog.found.Contains(a) ? 1 : 0));
        wkTitle.text = AHLoc.T("Fishing journal", "دفتر الصيد");
        wkHint.text = AHLoc.T("Every water has five fish. Rare ones rise in the rain, legendary ones bite better after dark, and schools (rippling water) bring all the rare ones closer. Glowfin: only at night.", "لكل ماء خمس سمكات. النادرة تطفو في المطر، والأسطورية تأكل أكثر ليلًا، والأسراب تقرّب النادرة. السمكة المضيئة: ليلًا فقط.");
        int from = Paged(lands.Count);
        string[] tn = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        string[] when = { AHLoc.T("any time", "أي وقت"), AHLoc.T("any time", "أي وقت"), AHLoc.T("best in rain", "الأفضل في المطر"), AHLoc.T("in schools", "في الأسراب"), AHLoc.T("at night, in schools", "ليلًا، في الأسراب") };
        for (int r = 0; r < RowsPerPage && from + r < lands.Count; r++)
        {
            string land = lands[from + r]; var fish = AHFish.Of(land); bool known = p.prog.found.Contains(land);
            var parts = new List<string>(); int got = 0;
            for (int i = 0; i < fish.Length; i++)
            {
                if (fish[i] == null) continue; long n = AHProgress.Get(p.prog.caught, fish[i].id); if (n > 0) got++;
                string col = i == 4 ? "#ffb347" : i == 3 ? "#c08cff" : i == 2 ? "#6cb6ff" : i == 1 ? "#7be07a" : "#e8e8e8";
                parts.Add("<color=" + col + ">" + (n > 0 || known ? fish[i].name : "???") + "</color>" + (n > 0 ? " ×" + n : ""));
            }
            var lead = fish.Length > 4 && fish[4] != null ? fish[4] : null;
            if (lead != null) rowItem = AHItems.Get(lead.id);
            Row(r, AHAuto.LandName(land) + "  " + got + "/" + fish.Length, known ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 1f, 1f, 0.5f), string.Join(" · ", parts.ToArray()),
                lead != null ? AHLoc.T("Legendary: ", "الأسطورية: ") + (known ? lead.name : "???") + " · " + when[4] : "");
        }
    }

    // ===================================== the weekly legend's board =====================================
    public void OpenLegend() { wkMode = "legend"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderLegend(AHPlayer p)
    {
        SetPager(false); wkPage.text = "";
        wkTitle.text = AHLoc.T("The weekly legend · ", "أسطورة الأسبوع · ") + AHFun4.LegendName;
        var nd = DateTime.Today.AddDays(7 - (((int)DateTime.Today.DayOfWeek + 6) % 7)) - DateTime.Now;
        wkHint.text = AHLoc.T("It walks in ", "تجول في ") + AHAuto.LandName(AHFun4.LegendArea(p)) + AHLoc.T(". The clock starts with your first blow. A new legend in ", ". تبدأ الساعة مع أول ضربة. أسطورة جديدة بعد ") + nd.Days + AHLoc.T("d ", " ي ") + nd.Hours + AHLoc.T("h", " س");
        var board = AHFun4.Board(p); SetPager(true);
        int from = Paged(board.Count);
        for (int i = from; i < board.Count && i < from + RowsPerPage; i++)
        {
            var kv = board[i]; bool me = kv.Key.EndsWith(AHLoc.T(" (you)", " (أنت)"));
            Row(i - from, (i + 1) + ".  " + kv.Key, me ? CGGold : i == 0 ? new Color(1f, 0.85f, 0.45f) : Color.white, AHDeep.Clock(kv.Value), "");
        }
    }
}
