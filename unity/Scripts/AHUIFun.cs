// Ashen Hollow: the windows and HUD pieces for AHFun3 and friends: the bounty board, the photo album, the
// "while you were away" card, the login streak flame on the hero card, the gold deed pop-up and the emote wheel.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    void FunTick()
    {
        AHFun3.Tick(g, Time.deltaTime);
        StreakTick(); DeedTick(); WheelTick(); Fun4Tick(); NetTick();
    }

    public void TestBagNew() { bagTab = "new"; ShowBag(true); }

    // ===================================== the bounty board =====================================
    public void OpenBounty() { wkMode = "bounty"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderBounty(AHPlayer p)
    {
        SetPager(false); wkPage.text = "";
        var list = AHFun3.Today(p); var rw = AHFun3.Reward(p);
        wkTitle.text = AHLoc.T("Bounty board", "لوحة المكافآت");
        var nd = DateTime.Today.AddDays(1) - DateTime.Now;
        wkHint.text = AHLoc.T("Three named beasts a day. Go to their land and they show themselves. New bounties in " + (int)nd.TotalHours + "h " + nd.Minutes + "m.", "ثلاثة وحوش مسماة كل يوم. اذهب إلى أرضها وستظهر لك. مكافآت جديدة بعد " + (int)nd.TotalHours + " س " + nd.Minutes + " د.");
        for (int i = 0; i < list.Count; i++)
        {
            var b = list[i];
            string where = AHAuto.LandName(b.area);
            string state = b.paid ? AHLoc.T("Done", "تم") : b.done ? AHLoc.T("<color=#9be37a>Beaten! Claim your reward</color>", "<color=#9be37a>هُزم! استلم مكافأتك</color>") : AHGame.AreaId == b.area ? AHLoc.T("It is somewhere in this land", "إنه في مكان ما في هذه الأرض") : AHLoc.T("Prowls in ", "يجول في ") + where;
            rowIcon = HubIcon("card_wolf") ?? CGIcon("sword");
            Row(i, b.name + " " + b.story, b.paid ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 0.82f, 0.45f), state,
                AHLoc.T("Reward: ", "المكافأة: ") + AHItems.MoneyText(rw.money * AHDB.CU) + " · " + rw.xp.ToString("#,0") + " XP · " + rw.marks + AHLoc.T(" marks", " علامة"),
                b.paid ? null : new WkBtn { label = b.done ? AHLoc.T("Claim", "استلم") : AHLoc.T("Hunting", "مطاردة"), on = b.done, col = Go, act = () => { AHFun3.Claim(g, b); RenderWork(); } });
        }
    }

    // ===================================== the photo album =====================================
    readonly List<Texture2D> albumTex = new List<Texture2D>();
    public void OpenAlbum() { wkMode = "album"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderAlbum(AHPlayer p)
    {
        foreach (var t in albumTex) if (t != null) Destroy(t); albumTex.Clear();
        var have = AHFun3.Album(p);
        int total = AHFun3.SpotsHere(g).Count;
        wkTitle.text = AHLoc.T("Photo album · ", "ألبوم الصور · ") + have.Count;
        wkHint.text = AHLoc.T("Fine views: every waystone, and the sights of Hollow Meadow. Stand at one in photo mode and SNAP.", "مناظر جميلة: كل حجر طريق، ومعالم هولو. قف عند أحدها في وضع التصوير والتقط.") + (total > 0 ? AHLoc.T(" Here: ", " هنا: ") + total + AHLoc.T(" spots", " أماكن") : "");
        const int per = 6; int pages = Mathf.Max(1, (have.Count + per - 1) / per); wkPageI = Mathf.Clamp(wkPageI, 0, pages - 1);
        SetPager(pages > 1); wkPage.text = (wkPageI + 1) + " / " + pages;
        float W = WkW - 36f, cw = (W - 24f) / 3f, ch = (RowH * RowsPerPage - 12f) / 2f;
        for (int i = 0; i < per; i++)
        {
            int k = wkPageI * per + i; if (k >= have.Count) break;
            string id = have[k];
            var card = Img("Photo", wkList, white, new Vector2(0f, 1f), Vector2.zero, new Vector2(cw, ch), new Color(0.16f, 0.12f, 0.09f, 1f));
            card.pivot = new Vector2(0f, 1f); card.anchoredPosition = new Vector2((i % 3) * (cw + 12f), -(i / 3) * (ch + 12f)); wkItems.Add(card.gameObject);
            var pic = Box("Pic", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(cw - 12f, ch - 40f));
            var raw = pic.gameObject.AddComponent<RawImage>(); raw.raycastTarget = false;
            Texture2D tex = null;
            try { var f = AHFun3.AlbumPath(id); if (System.IO.File.Exists(f)) { tex = new Texture2D(2, 2, TextureFormat.RGB24, false); tex.LoadImage(System.IO.File.ReadAllBytes(f)); albumTex.Add(tex); } } catch { }
            raw.texture = tex; raw.color = tex != null ? Color.white : new Color(0.3f, 0.3f, 0.35f, 1f);
            if (tex != null) { float a = (float)tex.width / Mathf.Max(1, tex.height), bw = cw - 12f, bh = ch - 40f; if (bw / bh > a) raw.uvRect = new Rect(0, 0.5f - (a * bh / bw) * 0.5f, 1, a * bh / bw); else raw.uvRect = new Rect(0.5f - (bw / bh / a) * 0.5f, 0, bw / bh / a, 1); }
            string nm = AHPrefs.GetString("ah_album_n_" + id, id);
            var t2 = Label(card, "Name", nm, 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(cw - 10f, 26f), new Color(1f, 0.85f, 0.55f)); Fit(t2, 11);
            t2.rectTransform.anchorMin = t2.rectTransform.anchorMax = new Vector2(0.5f, 0f); t2.rectTransform.pivot = new Vector2(0.5f, 0f); t2.rectTransform.anchoredPosition = new Vector2(0f, 6f);
        }
        if (have.Count == 0) Row(0, AHLoc.T("No photos yet", "لا صور بعد"), new Color(1f, 1f, 1f, 0.7f), AHLoc.T("Find a waystone, open photo mode (the emote button → Photo) and tap SNAP.", "اعثر على حجر طريق، وافتح وضع التصوير (زر الحركات ← تصوير)، واضغط التقط."), "");
    }

    // ===================================== while you were away =====================================
    RectTransform awayRoot;
    public void AwayCard(long gapS, List<string> lines)
    {
        if (awayRoot != null) Destroy(awayRoot.gameObject);
        awayRoot = Img("AwayCard", transform, cgCard != null ? cgCard : white, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(520, 120 + lines.Count * 30), Color.white);
        if (cgCard != null) awayRoot.GetComponent<Image>().type = Image.Type.Sliced;
        long h = gapS / 3600, m = (gapS % 3600) / 60;
        string gap = h >= 24 ? (h / 24) + AHLoc.T(" days", " أيام") : h > 0 ? h + AHLoc.T(" h ", " س ") + m + AHLoc.T(" min", " د") : m + AHLoc.T(" minutes", " دقيقة");
        var t = Label(awayRoot, "T", AHLoc.T("Welcome back!", "أهلًا بعودتك!"), 26, TextAnchor.UpperCenter, Vector2.zero, new Vector2(500, 34), CGGold); t.font = TitleFont;
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f); t.rectTransform.pivot = new Vector2(0.5f, 1f); t.rectTransform.anchoredPosition = new Vector2(0f, -14f);
        var s = Label(awayRoot, "S", AHLoc.T("You were away " + gap + ". While you were gone:", "غبت " + gap + ". أثناء غيابك:"), 15, TextAnchor.UpperCenter, Vector2.zero, new Vector2(500, 22), new Color(0.74f, 0.83f, 0.93f));
        s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 1f); s.rectTransform.pivot = new Vector2(0.5f, 1f); s.rectTransform.anchoredPosition = new Vector2(0f, -50f);
        for (int i = 0; i < lines.Count; i++)
        {
            var r = Img("L", awayRoot, cgPillGlass != null ? cgPillGlass : white, new Vector2(0.5f, 1f), new Vector2(0f, -92f - i * 30f), new Vector2(460, 26), Color.white);
            if (cgPillGlass != null) r.GetComponent<Image>().type = Image.Type.Sliced;
            Center(Label(r, "T", "• " + lines[i], 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(450, 24), Color.white));
        }
        var hint = Label(awayRoot, "H", AHLoc.T("Tap to carry on", "اضغط للمتابعة"), 13, TextAnchor.LowerCenter, Vector2.zero, new Vector2(500, 20), new Color(1f, 1f, 1f, 0.55f));
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f); hint.rectTransform.pivot = new Vector2(0.5f, 0f); hint.rectTransform.anchoredPosition = new Vector2(0f, 10f);
        var rt = awayRoot; taps.Insert(0, new TapBtn { rt = rt, act = () => { if (rt != null) Destroy(rt.gameObject); } });
        awayRoot.SetAsLastSibling(); AHSound.Play("loot");
    }

    // ===================================== the login streak flame =====================================
    RectTransform flameRt; Text flameN; int flameShown = -1; static Sprite flameSp;
    static Sprite FlameSprite()
    {
        if (flameSp != null) return flameSp;
        const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N;   // v: 0 bottom .. 1 top
            // a teardrop: round at the foot, a point at the top, a little lean
            float w = v < 0.38f ? Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((0.38f - v) / 0.36f, 2f))) * 0.62f : 0.62f * Mathf.Pow(Mathf.Max(0f, 1f - (v - 0.38f) / 0.6f), 1.4f);
            float cu = u - (v - 0.3f) * 0.18f, d = w > 0f ? Mathf.Abs(cu) / w : 2f;
            float a = Mathf.Clamp01((1f - d) * 6f) * (v > 0.02f ? 1f : 0f);
            float core = Mathf.Clamp01(1f - d * 1.3f) * Mathf.Clamp01(1f - v * 1.4f);
            var c = Color.Lerp(new Color(1f, 0.38f, 0.08f), new Color(1f, 0.92f, 0.45f), core); c.a = a;
            t.SetPixel(x, y, c);
        }
        t.Apply(); flameSp = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f); return flameSp;
    }
    void StreakTick()
    {
        var p = g.player; if (p == null || p.prog == null || p.prog.daily == null) return;
        int s = p.prog.daily.streak;
        if (flameRt == null)
        {
            var card = FindUI("Portrait"); RectTransform host = card != null ? card.parent as RectTransform : null;
            flameRt = Img("Streak", transform, cgPillGlass != null ? cgPillGlass : white, new Vector2(0f, 1f), new Vector2(378f, -105f), new Vector2(62, 24), Color.white);
            if (cgPillGlass != null) flameRt.GetComponent<Image>().type = Image.Type.Sliced;
            Img("F", flameRt, FlameSprite(), new Vector2(0f, 0.5f), new Vector2(14f, 1f), new Vector2(18, 22), Color.white);
            flameN = Label(flameRt, "N", "", 14, TextAnchor.MiddleLeft, new Vector2(26f, 0f), new Vector2(34, 22), new Color(1f, 0.85f, 0.5f)); flameN.fontStyle = FontStyle.Bold;
            flameN.rectTransform.anchorMin = flameN.rectTransform.anchorMax = new Vector2(0f, 0.5f); flameN.rectTransform.pivot = new Vector2(0f, 0.5f); flameN.rectTransform.anchoredPosition = new Vector2(26f, 0f);
            taps.Add(new TapBtn { rt = flameRt, act = () => Toast(AHLoc.T("Login streak: " + p.prog.daily.streak + " days in a row. Day 7 and every 30 days bring a bonus. Miss a day and it starts again.", "سلسلة الدخول: " + p.prog.daily.streak + " أيام متتالية. اليوم السابع وكل ٣٠ يومًا مكافأة. إن فاتك يوم تبدأ من جديد."), 4f) });
        }
        bool on = Modal == 0 && !PhotoOn && s > 0;
        if (flameRt.gameObject.activeSelf != on) flameRt.gameObject.SetActive(on);
        if (s != flameShown) { flameShown = s; flameN.text = s.ToString(); float k = 1f + Mathf.Min(0.35f, s / 60f); flameRt.Find("F").localScale = new Vector3(k, k, 1f); }
        if (on) { float f = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.04f + Mathf.Sin(Time.unscaledTime * 11.3f) * 0.03f; var fl = flameRt.Find("F"); fl.localScale = new Vector3(fl.localScale.x, Mathf.Abs(fl.localScale.x) * f, 1f); }
    }

    // ===================================== the gold deed pop-up =====================================
    RectTransform deedRoot; Text deedName, deedDesc, deedTitle; float deedT; readonly Queue<AHAch.Deed> deedQ = new Queue<AHAch.Deed>();
    public void DeedPop(AHAch.Deed d) { if (d != null) deedQ.Enqueue(d); }
    void DeedTick()
    {
        if (deedT > 0f)
        {
            deedT -= Time.unscaledDeltaTime;
            float k = deedT > 4.4f ? Mathf.SmoothStep(0f, 1f, (4.9f - deedT) / 0.5f) : deedT < 0.5f ? Mathf.SmoothStep(0f, 1f, deedT / 0.5f) : 1f;
            deedRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(80f, -96f, k));
            if (deedT <= 0f) deedRoot.gameObject.SetActive(false);
            return;
        }
        if (deedQ.Count == 0) return;
        var dd = deedQ.Dequeue();
        if (deedRoot == null)
        {
            deedRoot = Img("DeedPop", transform, cgCard != null ? cgCard : white, new Vector2(0.5f, 1f), new Vector2(0f, 80f), new Vector2(460, 92), new Color(1f, 0.92f, 0.7f, 1f));
            if (cgCard != null) deedRoot.GetComponent<Image>().type = Image.Type.Sliced;
            Img("Rim", deedRoot, cgRim != null ? cgRim : white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(464, 96), new Color(1f, 0.8f, 0.3f, 0.9f)).GetComponent<Image>().type = Image.Type.Sliced;
            var cup = Img("Cup", deedRoot, HubIcon("gold_crown") ?? CGIcon("star"), new Vector2(0f, 0.5f), new Vector2(48f, 0f), new Vector2(56, 56), Color.white); cup.GetComponent<Image>().preserveAspect = true;
            var head = Label(deedRoot, "H", AHLoc.T("DEED EARNED", "إنجاز جديد"), 12, TextAnchor.UpperLeft, new Vector2(88f, -10f), new Vector2(350, 16), new Color(1f, 0.8f, 0.35f)); head.fontStyle = FontStyle.Bold;
            deedName = Label(deedRoot, "N", "", 22, TextAnchor.UpperLeft, new Vector2(88f, -26f), new Vector2(360, 28), Color.white); deedName.font = TitleFont;
            deedDesc = Label(deedRoot, "D", "", 13, TextAnchor.UpperLeft, new Vector2(88f, -54f), new Vector2(360, 18), new Color(0.82f, 0.88f, 0.95f)); Fit(deedDesc, 10);
            deedTitle = Label(deedRoot, "T", "", 12, TextAnchor.UpperLeft, new Vector2(88f, -71f), new Vector2(360, 16), new Color(1f, 0.85f, 0.5f));
        }
        deedName.text = dd.name; deedDesc.text = dd.desc; deedTitle.text = string.IsNullOrEmpty(dd.title) ? "" : AHLoc.T("New title: “" + dd.title + "” · Menu → Deeds", "لقب جديد: “" + dd.title + "”");
        deedRoot.gameObject.SetActive(true); deedRoot.SetAsLastSibling(); deedT = 4.9f;
        AHSound.Play("level"); AHChat.Add("system", "Deed earned · " + dd.name);
    }

    // ===================================== the emote wheel =====================================
    RectTransform wheelRoot; float wheelT;
    void ToggleEmoteWheel()
    {
        if (wheelRoot != null && wheelRoot.gameObject.activeSelf) { wheelRoot.gameObject.SetActive(false); return; }
        if (wheelRoot == null)
        {
            wheelRoot = Box("EmoteWheel", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-352f, 200f), new Vector2(10, 10));
            var ring0 = Img("Ring", wheelRoot, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300), new Color(0.03f, 0.05f, 0.1f, 0.55f));
            ring0.GetComponent<Image>().raycastTarget = false;
            var list = AHEmote.All; int n = Mathf.Min(8, list.Count);
            for (int i = 0; i < n; i++)
            {
                var e = list[i]; float a = (90f - i * 360f / n) * Mathf.Deg2Rad;
                var b = Img("E_" + e.id, wheelRoot, ornDisc != null ? ornDisc : circle, new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 108f, new Vector2(66, 66), new Color(0.05f, 0.08f, 0.15f, 0.85f));
                Img("Rim", b, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68, 68), e.col);
                var t = Center(Label(b, "T", e.name, 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64, 40), Color.white)); t.fontStyle = FontStyle.Bold; Fit(t, 9);
                string id = e.id; taps.Insert(0, new TapBtn { rt = b, act = () => { wheelRoot.gameObject.SetActive(false); g.player.Emote(id); } });
            }
            var more = Img("More", wheelRoot, ornDisc != null ? ornDisc : circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70), new Color(0.2f, 0.14f, 0.26f, 0.92f));
            Img("Rim", more, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72), new Color(1f, 0.8f, 0.45f));
            Center(Label(more, "T", AHLoc.T("All…", "الكل…"), 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(66, 30), new Color(1f, 0.85f, 0.55f)));
            taps.Insert(0, new TapBtn { rt = more, act = () => { wheelRoot.gameObject.SetActive(false); OpenEmotes(); } });
            var ph = Img("Photo", wheelRoot, cgPillSky != null ? cgPillSky : white, new Vector2(0.5f, 0.5f), new Vector2(0f, -172f), new Vector2(120, 38), Color.white);
            if (cgPillSky != null) ph.GetComponent<Image>().type = Image.Type.Sliced;
            Center(Label(ph, "T", AHLoc.T("Photo", "تصوير"), 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(116, 30), Color.white));
            taps.Insert(0, new TapBtn { rt = ph, act = () => { wheelRoot.gameObject.SetActive(false); PhotoMode(true); } });
            ph.anchoredPosition = new Vector2(-66f, -172f);
            var ou = Img("Outfits", wheelRoot, cgPillSky != null ? cgPillSky : white, new Vector2(0.5f, 0.5f), new Vector2(66f, -172f), new Vector2(120, 38), Color.white);
            if (cgPillSky != null) ou.GetComponent<Image>().type = Image.Type.Sliced;
            Center(Label(ou, "T", AHLoc.T("Outfits", "الأزياء"), 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(116, 30), Color.white));
            taps.Insert(0, new TapBtn { rt = ou, act = () => { wheelRoot.gameObject.SetActive(false); OpenWardrobe(); } });
        }
        wheelRoot.gameObject.SetActive(true); wheelRoot.SetAsLastSibling(); wheelT = 6f;
    }
    void WheelTick()
    {
        if (wheelRoot == null || !wheelRoot.gameObject.activeSelf) return;
        wheelT -= Time.unscaledDeltaTime;
        if (wheelT <= 0f || Modal != 0) { wheelRoot.gameObject.SetActive(false); return; }
        float k = Mathf.Clamp01((6f - wheelT) / 0.15f); wheelRoot.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
    }
}
