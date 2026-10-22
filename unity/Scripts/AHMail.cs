// Ashen Hollow: the mailbox. Rewards that come while you are busy elsewhere (a caravan selling far away, a festival
// gift, a friend's present) wait here as letters instead of flashing past; an envelope by the quest card shows how
// many wait, and taking one opens the reward chest. Friends' letters live in the same box (p.prog.letters).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHMail
{
    public const int System = -1;   // AHLetter.h for letters that are not from a friend
    static bool Has(AHLetter l) { return l.money > 0 || (l.items != null && l.items.Count > 0); }
    public static bool Waiting(AHLetter l) { return !l.taken && Has(l); }
    public static int Count(AHPlayer p) { if (p == null || p.prog == null) return 0; int n = 0; foreach (var l in p.prog.letters) if (Waiting(l) || !l.read) n++; return n; }

    // silver: whole silver coins (the letter's money is counted in silver, like friends' letters)
    public static void Send(AHGame g, string from, string text, long silver, params AHStack[] items)
    {
        var p = g != null ? g.player : null; if (p == null) return;
        var l = new AHLetter { from = from, h = System, text = text, money = silver };
        if (items != null) foreach (var s in items) if (s != null && AHItems.Get(s.id) != null && s.n > 0) l.items.Add(s);
        var L = p.prog.letters; L.Insert(0, l);
        while (L.Count > 60) { int i = L.FindLastIndex(x => x.taken || !Has(x)); if (i < 0) break; L.RemoveAt(i); }
        if (g.ui != null) g.ui.Toast(AHLoc.T("New mail from " + from + ".", "بريد جديد من " + from + "."), 3f);
        AHSound.Play("coin");
        g.MarkDirty();
    }

    // take what a letter holds, shown in the reward chest
    public static bool Take(AHGame g, AHLetter l)
    {
        var p = g.player; if (l == null || l.taken) return false;
        l.read = true;
        if (!Has(l)) { l.taken = true; return true; }
        var ids = new List<string>(); foreach (var s in l.items) ids.Add(s.id);
        if (!p.bag.Fits(ids)) { g.ui.Toast(AHLoc.T("Your bag is full. Make room first.", "حقيبتك ممتلئة. أفرغ مكانًا أولًا."), 3f); return false; }
        var got = new List<KeyValuePair<string, int>>(); foreach (var s in l.items) got.Add(new KeyValuePair<string, int>(s.id, s.n));
        long money = l.money;
        AHFriends.TakeLetter(g, l);
        if (!l.taken) return false;
        g.ui.RewardChest(AHLoc.T("Mail from ", "بريد من ") + l.from, money, 0, 0, got);
        return true;
    }
}

public partial class AHUI
{
    RectTransform mailBtn; Text mailN; int mailShown = -1;

    void BuildMail()
    {
        if (mailBtn != null) return;
        mailBtn = Img("MailBtn", transform, ornDisc != null ? ornDisc : circle, new Vector2(0f, 1f), new Vector2(40f, -308f), new Vector2(48, 48), new Color(0.05f, 0.08f, 0.15f, 0.6f));
        Img("Rim", mailBtn, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(51, 51), new Color(1f, 0.85f, 0.5f, 1f));
        Center(Label(mailBtn, "T", "✉", 26, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(48, 40), new Color(1f, 0.92f, 0.75f)));
        var b = Img("Badge", mailBtn, circle, new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(20, 20), new Color(0.86f, 0.25f, 0.22f, 1f));
        mailN = Center(Label(b, "N", "", 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(20, 18), Color.white)); mailN.fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = mailBtn, act = OpenMail });
        mailBtn.gameObject.SetActive(false);
    }
    void MailTick()
    {
        if (mailBtn == null || g.player == null) return;
        int n = AHMail.Count(g.player);
        if (n == mailShown) return; mailShown = n;
        mailN.text = n > 9 ? "9+" : n.ToString();
        mailBtn.gameObject.SetActive(n > 0);
    }

    public void OpenMail() { wkMode = "mail"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderMail(AHPlayer p)
    {
        SetPager(true);
        int waiting = 0; foreach (var l in p.prog.letters) if (AHMail.Waiting(l)) waiting++;
        wkTitle.text = AHLoc.T("Mail", "البريد") + (waiting > 0 ? " · " + waiting + AHLoc.T(" waiting", " بانتظارك") : "");
        wkHint.text = AHLoc.T("Rewards and letters wait here until you take them.", "المكافآت والرسائل تنتظرك هنا حتى تأخذها.");
        var rows = new List<Action<int>>();
        if (waiting > 1)
            rows.Add(s => Row(s, AHLoc.T("Take everything", "خذ كل شيء"), new Color(1f, 0.85f, 0.45f), waiting + AHLoc.T(" letters with something inside", " رسائل فيها شيء"), "",
                new WkBtn { label = AHLoc.T("Take all", "خذ الكل"), on = true, col = Go, act = () => { MailTakeAll(p); RenderWork(); } }));
        // waiting first, then the rest newest first
        var order = new List<AHLetter>(p.prog.letters);
        order.Sort((a, b) => AHMail.Waiting(b).CompareTo(AHMail.Waiting(a)));
        foreach (var l in order)
        {
            var ll = l; var parts = new List<string>(); foreach (var st in l.items) { var it = AHItems.Get(st.id); if (it != null) parts.Add(st.n + "× " + it.name); }
            if (l.money > 0) parts.Add(AHItems.MoneyText(l.money * AHDB.CU));
            bool fr = l.h != AHMail.System;
            string head = (fr ? AHLoc.T("From ", "من ") : "") + ll.from + (fr ? "  " + AHFriends.HeartStr(ll.h) : "");
            if (parts.Count > 0) { var first = l.items.Count > 0 ? AHItems.Get(l.items[0].id) : null; if (first != null) rowItem = first; else rowIcon = HubIcon("gold_crown"); }
            else rowIcon = CGIcon("chat");
            rows.Add(s => Row(s, head, AHMail.Waiting(ll) ? new Color(1f, 0.85f, 0.55f) : new Color(1f, 1f, 1f, 0.6f), ll.text, parts.Count > 0 ? AHLoc.T("Inside: ", "بداخلها: ") + string.Join(", ", parts.ToArray()) : "",
                parts.Count > 0 ? new WkBtn { label = ll.taken ? AHLoc.T("Taken", "أُخذت") : AHLoc.T("Take", "خذ"), on = !ll.taken, col = Go, act = () => { AHMail.Take(g, ll); RenderWork(); } } : null));
        }
        if (p.prog.letters.Count == 0) rows.Add(s => Row(s, AHLoc.T("No mail yet", "لا بريد بعد"), new Color(1f, 1f, 1f, 0.7f), AHLoc.T("Caravans, festivals and friends send things here.", "القوافل والمهرجانات والأصدقاء يرسلون الأشياء هنا."), ""));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
        // opening the box counts as reading what has nothing to take
        foreach (var l in p.prog.letters) if (!AHMail.Waiting(l)) l.read = true;
        mailShown = -1;
    }
    void MailTakeAll(AHPlayer p)
    {
        long money = 0; var got = new Dictionary<string, int>(); int n = 0;
        foreach (var l in new List<AHLetter>(p.prog.letters))
        {
            if (!AHMail.Waiting(l)) continue;
            var ids = new List<string>(); foreach (var s in l.items) ids.Add(s.id);
            if (!p.bag.Fits(ids)) { Toast(AHLoc.T("Your bag filled up. The rest waits in the mail.", "امتلأت حقيبتك. الباقي ينتظرك في البريد."), 3f); break; }
            long m = l.money; AHFriends.TakeLetter(g, l); if (!l.taken) break;
            l.read = true; n++; money += m; foreach (var s in l.items) { int c; got.TryGetValue(s.id, out c); got[s.id] = c + s.n; }
        }
        if (n > 0) RewardChest(AHLoc.T("Your mail · ", "بريدك · ") + n + AHLoc.T(" letters", " رسائل"), money, 0, 0, new List<KeyValuePair<string, int>>(got));
    }
}
