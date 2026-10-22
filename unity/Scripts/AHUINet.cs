// Ashen Hollow: the windows for playing online: who is in this land (invite, trade), the party frame under the quest
// card, the yes/no question when someone asks you something, and the trade window.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    void NetTick()
    {
        AHNet.Tick(g, Time.deltaTime);
        AskTick(); PartyFrameTick();
    }

    // ===================================== a yes/no question =====================================
    RectTransform askRoot; Text askText; Action askYes, askNo; float askUntil;
    public bool AskOpen { get { return askRoot != null && askRoot.gameObject.activeSelf; } }
    public void NetAsk(string text, Action yes, Action no)
    {
        if (AskOpen && askNo != null) askNo();   // a new question: the old one is turned down
        if (askRoot == null)
        {
            askRoot = Img("NetAsk", transform, cgCard != null ? cgCard : white, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(460, 120), Color.white);
            if (cgCard != null) askRoot.GetComponent<Image>().type = Image.Type.Sliced;
            askText = Label(askRoot, "T", "", 17, TextAnchor.UpperCenter, Vector2.zero, new Vector2(430, 50), Color.white);
            askText.rectTransform.anchorMin = askText.rectTransform.anchorMax = new Vector2(0.5f, 1f); askText.rectTransform.pivot = new Vector2(0.5f, 1f); askText.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            for (int i = 0; i < 2; i++)
            {
                bool isYes = i == 0;
                var b = Img(isYes ? "Yes" : "No", askRoot, isYes ? (cgPillSky != null ? cgPillSky : white) : (cgPillGlass != null ? cgPillGlass : white), new Vector2(0.5f, 0f), new Vector2(isYes ? -80f : 80f, 30f), new Vector2(140, 40), Color.white);
                b.GetComponent<Image>().type = Image.Type.Sliced;
                Center(Label(b, "T", isYes ? AHLoc.T("Accept", "قبول") : AHLoc.T("Decline", "رفض"), 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(136, 34), Color.white));
                taps.Insert(0, new TapBtn { rt = b, layer = 14, act = () => { var a = isYes ? askYes : askNo; askYes = askNo = null; askRoot.gameObject.SetActive(false); if (a != null) a(); } });
            }
        }
        askText.text = text; askYes = yes; askNo = no; askUntil = Time.unscaledTime + 30f;
        askRoot.gameObject.SetActive(true); askRoot.SetAsLastSibling(); AHSound.Play("coin");
    }
    void AskTick()
    {
        if (!AskOpen) return;
        if (Time.unscaledTime > askUntil) { var a = askNo; askYes = askNo = null; askRoot.gameObject.SetActive(false); if (a != null) a(); }
    }

    // ===================================== who is here =====================================
    public void OpenOnline() { wkMode = "online"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderOnline(AHPlayer p)
    {
        wkTitle.text = AHLoc.T("Online · ", "عبر الإنترنت · ") + AHAuto.LandName(AHGame.AreaId);
        wkHint.text = AHNet.Status + (AHNet.InRoom ? AHLoc.T(" · everyone in this land can see you and read your chat", " · كل من في هذه الأرض يراك ويقرأ رسائلك") : "");
        var rows = new List<Action<int>>();
        rows.Add(s => Row(s, AHLoc.T("Play online", "اللعب عبر الإنترنت"), AHNet.Wanted ? new Color(0.55f, 1f, 0.55f) : Color.white,
            !AHNet.Ready ? AHLoc.T("Not set up yet: the game needs its Photon App ID", "لم يُجهّز بعد: تحتاج اللعبة معرّف Photon") : AHLoc.T("See your friends in the same land, chat, party up and trade", "شاهد أصدقاءك في الأرض نفسها، تحدّث، شكّل فريقًا وقايض"), "",
            new WkBtn { label = AHNet.Wanted ? AHLoc.T("On", "مفعّل") : AHLoc.T("Off", "متوقف"), on = AHNet.Ready, col = AHNet.Wanted ? Go : Plain, act = () => { AHNet.Wanted = !AHNet.Wanted; RenderWork(); } }));
        if (AHNet.InParty)
        {
            var names = new List<string>(); foreach (var a in AHNet.Party) { AHNet.Peer pe; names.Add(a == AHNet.MyActor ? (p.heroName ?? "You") : AHNet.Peers.TryGetValue(a, out pe) ? pe.name : "?"); }
            rows.Add(s => Row(s, AHLoc.T("Your party", "فريقك"), new Color(0.55f, 1f, 0.55f), string.Join(", ", names.ToArray()), AHLoc.T("Blows are shared; whoever is close gets the kill", "الضربات مشتركة؛ القريب ينال الغنيمة"),
                new WkBtn { label = AHLoc.T("Leave", "مغادرة"), on = true, col = Plain, act = () => { AHNet.LeaveParty(); RenderWork(); } }));
        }
        foreach (var kv in AHNet.Peers)
        {
            var pe = kv.Value; int actor = kv.Key; bool inP = AHNet.PartyHas(actor);
            var cd = AHClasses.Get(pe.cls); string cn = cd != null ? cd.name : pe.cls;
            float dist = pe.seen > 0f ? Vector3.Distance(pe.pos, p.transform.position) : -1f;
            rows.Add(s => Row(s, pe.name + "  lv " + pe.level, inP ? new Color(0.55f, 1f, 0.55f) : new Color(0.7f, 0.88f, 1f), cn + (string.IsNullOrEmpty(pe.title) ? "" : " · “" + pe.title + "”") + (dist >= 0f ? " · " + Mathf.RoundToInt(dist) + " m" : ""), "",
                inP ? null : new WkBtn { label = AHLoc.T("Invite", "دعوة"), on = AHNet.Party.Count < 4, col = Go, act = () => AHNet.Invite(actor) },
                new WkBtn { label = AHLoc.T("Trade", "مقايضة"), on = AHNet.TradeWith < 0, col = Plain, act = () => AHNetTrade.Ask(g, actor) }));
        }
        if (AHNet.InRoom && AHNet.Peers.Count == 0) rows.Add(s => Row(s, AHLoc.T("Nobody else here yet", "لا أحد هنا بعد"), new Color(1f, 1f, 1f, 0.6f), AHLoc.T("Friends who play in this land will show up here.", "الأصدقاء الذين يلعبون في هذه الأرض سيظهرون هنا."), ""));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    // ===================================== the party frame =====================================
    RectTransform partyRoot; readonly List<Image> partyFill = new List<Image>(); readonly List<Text> partyName = new List<Text>(); float partyT;
    void PartyFrameTick()
    {
        bool on = AHNet.InParty && Modal == 0 && !PhotoOn;
        if (partyRoot == null) { if (!on) return; partyRoot = Box("PartyFrame", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -372f), new Vector2(190, 80)); }
        if (partyRoot.gameObject.activeSelf != on) partyRoot.gameObject.SetActive(on);
        if (!on || (partyT -= Time.unscaledDeltaTime) > 0f) return; partyT = 0.25f;
        var others = new List<AHNet.Peer>(); foreach (var a in AHNet.Party) { AHNet.Peer pe; if (a != AHNet.MyActor && AHNet.Peers.TryGetValue(a, out pe)) others.Add(pe); }
        while (partyFill.Count < others.Count)
        {
            int i = partyFill.Count;
            var row = Img("M" + i, partyRoot, cgPillGlass != null ? cgPillGlass : white, new Vector2(0.5f, 1f), new Vector2(0f, -12f - i * 26f), new Vector2(190, 24), Color.white); row.GetComponent<Image>().type = Image.Type.Sliced;
            var t = Label(row, "N", "", 12, TextAnchor.MiddleLeft, new Vector2(10f, 0f), new Vector2(100, 20), new Color(0.6f, 1f, 0.6f)); t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 0.5f); t.rectTransform.pivot = new Vector2(0f, 0.5f); t.rectTransform.anchoredPosition = new Vector2(10f, 0f);
            var bg = Img("Bg", row, white, new Vector2(1f, 0.5f), new Vector2(-48f, 0f), new Vector2(76, 8), new Color(0f, 0f, 0f, 0.5f));
            var f = Img("F", bg, white, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(76, 8), new Color(0.35f, 0.9f, 0.4f)); f.pivot = new Vector2(0f, 0.5f); f.anchoredPosition = Vector2.zero;
            partyFill.Add(f.GetComponent<Image>()); partyName.Add(t);
        }
        for (int i = 0; i < partyFill.Count; i++)
        {
            bool has = i < others.Count; partyFill[i].transform.parent.parent.gameObject.SetActive(has); if (!has) continue;
            partyName[i].text = others[i].name; float k = Mathf.Clamp01((float)others[i].hp / Mathf.Max(1, others[i].maxHp));
            partyFill[i].rectTransform.sizeDelta = new Vector2(76f * k, 8f); partyFill[i].color = k < 0.3f ? new Color(0.95f, 0.35f, 0.3f) : new Color(0.35f, 0.9f, 0.4f);
        }
    }

    // ===================================== the trade window =====================================
    bool tradePick;
    public void OpenTrade() { tradePick = false; wkMode = "trade"; wkPageI = 0; ShowWork(true); RenderWork(); }
    public void RefreshTrade() { if (WorkOpen && wkMode == "trade") RenderWork(); }
    public void CloseTrade() { if (WorkOpen && wkMode == "trade") ShowWork(false); }
    static string OfferText(AHNet.Offer o)
    {
        var parts = new List<string>(); for (int i = 0; i < o.ids.Count; i++) { var d = AHItems.Get(o.ids[i]); parts.Add(o.ns[i] + "× " + (d != null ? d.name : o.ids[i])); }
        if (o.money > 0) parts.Add(AHItems.MoneyText(o.money));
        return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : AHLoc.T("nothing yet", "لا شيء بعد");
    }
    void RenderTrade(AHPlayer p)
    {
        AHNet.Peer pe; AHNet.Peers.TryGetValue(AHNet.TradeWith, out pe);
        if (AHNet.TradeWith < 0 || pe == null) { ShowWork(false); return; }
        wkTitle.text = AHLoc.T("Trade with ", "مقايضة مع ") + pe.name;
        if (tradePick)
        {
            wkHint.text = AHLoc.T("Tap Add to put something in. Worn gear stays on you.", "اضغط إضافة لتضع شيئًا. العتاد الملبوس يبقى عليك.");
            var ids = new List<string>(); foreach (var id in p.bag.order) if (id != null && p.bag.Count(id) > 0 && AHItems.Get(id) != null && !ids.Contains(id)) ids.Add(id);
            var rows = new List<Action<int>>();
            rows.Add(s => Row(s, AHLoc.T("Back to the trade", "عودة إلى المقايضة"), CGGold, OfferText(AHNet.MyOffer), "", new WkBtn { label = AHLoc.T("Back", "رجوع"), on = true, col = Go, act = () => { tradePick = false; wkPageI = 0; RenderWork(); } }));
            foreach (var id0 in ids)
            {
                string id = id0; var d = AHItems.Get(id); int have = p.bag.Count(id), put = AHNetTrade.Offered(id);
                rows.Add(s => { rowItem = d; Row(s, d.name, AHItems.Quality(d), AHLoc.T("You have ", "لديك ") + have + (put > 0 ? AHLoc.T(" · in the trade: ", " · في المقايضة: ") + put : ""), "",
                    new WkBtn { label = "+1", on = put < have, col = Go, act = () => AHNetTrade.Put(g, id, 1) },
                    new WkBtn { label = AHLoc.T("All", "الكل"), on = put < have, col = Plain, act = () => AHNetTrade.Put(g, id, have) },
                    put > 0 ? new WkBtn { label = "−", on = true, col = Plain, act = () => AHNetTrade.Put(g, id, -1) } : null); });
            }
            int from = Paged(rows.Count);
            for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
            return;
        }
        SetPager(false); wkPage.text = "";
        wkHint.text = AHLoc.T("Both lock in, then both confirm. Any change unlocks both sides.", "كلاكما يثبّت ثم يؤكد. أي تغيير يلغي التثبيت.");
        long step = Math.Max(100L, AHDB.CU * 100);
        Row(0, AHLoc.T("You give", "تعطي"), AHNet.MyReady ? new Color(0.55f, 1f, 0.55f) : Color.white, OfferText(AHNet.MyOffer), AHLoc.T("Your purse: ", "محفظتك: ") + AHItems.MoneyText(p.bag.money, 2),
            new WkBtn { label = AHLoc.T("Add items", "أضف أشياء"), on = !AHNet.MyConfirm, col = Go, act = () => { tradePick = true; wkPageI = 0; RenderWork(); } },
            new WkBtn { label = "+" + AHItems.MoneyText(step, 1), on = !AHNet.MyConfirm && p.bag.money > AHNet.MyOffer.money, col = Plain, act = () => AHNetTrade.Coins(g, step) },
            new WkBtn { label = AHLoc.T("No coins", "بلا عملات"), on = !AHNet.MyConfirm && AHNet.MyOffer.money > 0, col = Plain, act = () => AHNetTrade.Coins(g, 0) });
        Row(1, pe.name + AHLoc.T(" gives", " يعطي"), AHNet.TheirReady ? new Color(0.55f, 1f, 0.55f) : Color.white, OfferText(AHNet.TheirOffer), AHNet.TheirReady ? AHLoc.T("Locked in", "ثبّت") : AHLoc.T("Still choosing", "ما زال يختار"));
        string state = AHNet.MyConfirm ? AHLoc.T("You confirmed · waiting for " + pe.name, "أكّدت · بانتظار " + pe.name) : AHNet.MyReady && AHNet.TheirReady ? AHLoc.T("Both locked in · confirm to swap", "ثبّت الطرفان · أكّد للتبادل") : AHNet.MyReady ? AHLoc.T("You locked in · waiting for " + pe.name, "ثبّتّ · بانتظار " + pe.name) : AHLoc.T("Lock in when you're happy with the deal", "ثبّت حين ترضى بالصفقة");
        Row(2, AHLoc.T("The deal", "الصفقة"), CGGold, state, "",
            new WkBtn { label = AHNet.MyReady ? AHLoc.T("Unlock", "إلغاء التثبيت") : AHLoc.T("Lock in", "تثبيت"), on = !AHNet.MyConfirm, col = Plain, act = () => AHNetTrade.ToggleReady(g) },
            new WkBtn { label = AHLoc.T("Confirm", "تأكيد"), on = AHNet.MyReady && AHNet.TheirReady && !AHNet.MyConfirm, col = Go, act = () => AHNetTrade.Confirm(g) },
            new WkBtn { label = AHLoc.T("Cancel", "إلغاء"), on = true, col = Plain, act = () => AHNetTrade.Cancel(g) });
    }
}
