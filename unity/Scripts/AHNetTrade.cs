// Ashen Hollow: trading between two players. Ask, the other accepts, and a trade window opens for both. Each puts in
// things from their bag (never what they wear) and coins; any change unlocks both sides. When both have locked in,
// both confirm; then each phone takes its own side out of its own bag and hands it over, so nothing can be had twice.
using System.Collections.Generic;
using UnityEngine;

public static class AHNetTrade
{
    static bool gave, got; static AHNet.Offer gotOffer;

    public static void Ask(AHGame g, int actor)
    {
        if (AHNet.TradeWith >= 0) { g.ui.Toast(AHLoc.T("You are already trading.", "أنت في مقايضة بالفعل."), 2f); return; }
        AHNet.Raise(AHNet.EvTradeReq, true, new[] { actor });
        AHNet.Peer p; if (AHNet.Peers.TryGetValue(actor, out p)) g.ui.Toast(AHLoc.T("Asked " + p.name + " to trade.", "طلبت مقايضة من " + p.name + "."), 2f);
    }
    static void Begin(AHGame g, int actor) { AHNet.EndTrade(false); AHNet.TradeWith = actor; gave = got = false; gotOffer = null; g.ui.OpenTrade(); }

    public static void OnEvent(AHGame g, byte code, int from, AHNet.Peer peer, object data)
    {
        if (peer == null) return;
        switch (code)
        {
            case AHNet.EvTradeReq:
                if (AHNet.TradeWith >= 0) { AHNet.Raise(AHNet.EvTradeReply, false, new[] { from }); return; }
                g.ui.NetAsk(AHLoc.T(peer.name + " wants to trade with you.", peer.name + " يريد مقايضتك."), () => { AHNet.Raise(AHNet.EvTradeReply, true, new[] { from }); Begin(g, from); }, () => AHNet.Raise(AHNet.EvTradeReply, false, new[] { from }));
                return;
            case AHNet.EvTradeReply:
                if (data is bool && (bool)data) Begin(g, from); else g.ui.Toast(AHLoc.T(peer.name + " doesn't want to trade now.", peer.name + " لا يريد المقايضة الآن."), 2.5f);
                return;
        }
        if (from != AHNet.TradeWith) return;
        switch (code)
        {
            case AHNet.EvTradeOffer:
                {
                    var d = data as object[]; if (d == null || d.Length < 3) return;
                    var o = new AHNet.Offer(); var ids = d[0] as string[]; var ns = d[1] as int[]; o.money = d[2] is long ? (long)d[2] : 0;
                    if (ids != null && ns != null) for (int i = 0; i < ids.Length && i < ns.Length; i++) if (AHItems.Get(ids[i]) != null && ns[i] > 0) { o.ids.Add(ids[i]); o.ns.Add(ns[i]); }
                    AHNet.TheirOffer = o; Unlock();
                    break;
                }
            case AHNet.EvTradeReady: AHNet.TheirReady = data is bool && (bool)data; if (!AHNet.TheirReady) AHNet.TheirConfirm = AHNet.MyConfirm = false; break;
            case AHNet.EvTradeConfirm:
                {
                    // their confirm names both sides as they saw them: it must be the same deal we see
                    string k = data as string; string want = AHNet.TheirOffer.Key + "#" + AHNet.MyOffer.Key;
                    if (k != want) { Unlock(); SendReady(); return; }
                    AHNet.TheirConfirm = true; if (AHNet.MyConfirm) Hand(g);
                    break;
                }
            case AHNet.EvTradeGive:
                {
                    var d = data as object[]; if (d == null || d.Length < 3 || got) return;
                    var o = new AHNet.Offer(); var ids = d[0] as string[]; var ns = d[1] as int[]; o.money = d[2] is long ? (long)d[2] : 0;
                    if (ids != null && ns != null) for (int i = 0; i < ids.Length && i < ns.Length; i++) { o.ids.Add(ids[i]); o.ns.Add(ns[i]); }
                    got = true; gotOffer = o;
                    var p = g.player;
                    for (int i = 0; i < o.ids.Count; i++) if (!p.bag.Add(o.ids[i], o.ns[i])) AHMail.Send(g, peer.name, AHLoc.T("Your bag was full, so the rest of our trade came by mail.", "كانت حقيبتك ممتلئة، فوصل باقي المقايضة بالبريد."), 0, new AHStack { id = o.ids[i], n = o.ns[i] });
                    if (o.money > 0) { p.bag.money += o.money; p.bag.Touch(); }
                    g.MarkDirty(); g.SaveProgress();
                    if (gave) Done(g, peer);
                    break;
                }
            case AHNet.EvTradeCancel: AHNet.EndTrade(true); return;
        }
        g.ui.RefreshTrade();
    }
    static void Done(AHGame g, AHNet.Peer peer)
    {
        var items = new List<KeyValuePair<string, int>>(); for (int i = 0; i < gotOffer.ids.Count; i++) items.Add(new KeyValuePair<string, int>(gotOffer.ids[i], gotOffer.ns[i]));
        long money = gotOffer.money;
        AHNet.TradeWith = -1; AHNet.MyOffer = new AHNet.Offer(); AHNet.TheirOffer = new AHNet.Offer(); AHNet.MyReady = AHNet.TheirReady = AHNet.MyConfirm = AHNet.TheirConfirm = false;
        g.ui.CloseTrade();
        if (items.Count > 0 || money > 0) g.ui.RewardChest(AHLoc.T("Trade with ", "مقايضة مع ") + peer.name, 0, 0, 0, items);
        else g.ui.Toast(AHLoc.T("Trade done.", "تمت المقايضة."), 2.5f);
        if (money > 0) g.ui.Toast(AHLoc.T("+ ", "+ ") + AHItems.MoneyText(money), 3f);
    }

    // ---------------- your side ----------------
    static void Unlock() { AHNet.MyReady = AHNet.TheirReady = AHNet.MyConfirm = AHNet.TheirConfirm = false; }
    static void SendOffer() { var o = AHNet.MyOffer; AHNet.Raise(AHNet.EvTradeOffer, new object[] { o.ids.ToArray(), o.ns.ToArray(), o.money }, new[] { AHNet.TradeWith }); }
    static void SendReady() { AHNet.Raise(AHNet.EvTradeReady, AHNet.MyReady, new[] { AHNet.TradeWith }); }
    public static int Offered(string id) { int i = AHNet.MyOffer.ids.IndexOf(id); return i >= 0 ? AHNet.MyOffer.ns[i] : 0; }
    public static void Put(AHGame g, string id, int n)
    {
        var p = g.player; int have = p.bag.Count(id), now = Offered(id); n = Mathf.Clamp(now + n, 0, have);
        int i = AHNet.MyOffer.ids.IndexOf(id);
        if (n <= 0) { if (i >= 0) { AHNet.MyOffer.ids.RemoveAt(i); AHNet.MyOffer.ns.RemoveAt(i); } }
        else if (i >= 0) AHNet.MyOffer.ns[i] = n;
        else { if (AHNet.MyOffer.ids.Count >= 8) { g.ui.Toast(AHLoc.T("Eight kinds of things at most in one trade.", "ثمانية أصناف على الأكثر في المقايضة."), 2f); return; } AHNet.MyOffer.ids.Add(id); AHNet.MyOffer.ns.Add(n); }
        Unlock(); SendOffer(); SendReady(); g.ui.RefreshTrade();
    }
    public static void Coins(AHGame g, long add)
    {
        var p = g.player; AHNet.MyOffer.money = add <= 0 ? 0 : (long)Mathf.Min(p.bag.money, AHNet.MyOffer.money + add);
        Unlock(); SendOffer(); SendReady(); g.ui.RefreshTrade();
    }
    public static void ToggleReady(AHGame g)
    {
        if (!AHNet.MyReady)
        {
            // what you put in is still in your bag, and what comes your way fits
            var p = g.player;
            for (int i = 0; i < AHNet.MyOffer.ids.Count; i++) if (p.bag.Count(AHNet.MyOffer.ids[i]) < AHNet.MyOffer.ns[i]) { g.ui.Toast(AHLoc.T("You no longer have all of that.", "لم يعد لديك كل ذلك."), 2.5f); return; }
            if (p.bag.money < AHNet.MyOffer.money) { g.ui.Toast(AHLoc.T("You don't have that many coins.", "ليس لديك هذا القدر من العملات."), 2.5f); return; }
        }
        AHNet.MyReady = !AHNet.MyReady; if (!AHNet.MyReady) AHNet.MyConfirm = AHNet.TheirConfirm = false;
        SendReady(); g.ui.RefreshTrade();
    }
    public static void Confirm(AHGame g)
    {
        if (!AHNet.MyReady || !AHNet.TheirReady || AHNet.MyConfirm) return;
        AHNet.MyConfirm = true;
        AHNet.Raise(AHNet.EvTradeConfirm, AHNet.MyOffer.Key + "#" + AHNet.TheirOffer.Key, new[] { AHNet.TradeWith });
        if (AHNet.TheirConfirm) Hand(g);
        g.ui.RefreshTrade();
    }
    // both have confirmed: our side leaves our bag and goes over
    static void Hand(AHGame g)
    {
        if (gave) return;
        var p = g.player; var o = AHNet.MyOffer;
        for (int i = 0; i < o.ids.Count; i++) if (p.bag.Count(o.ids[i]) < o.ns[i]) { Cancel(g); return; }
        if (p.bag.money < o.money) { Cancel(g); return; }
        for (int i = 0; i < o.ids.Count; i++) p.bag.Take(o.ids[i], o.ns[i]);
        p.bag.money -= o.money; p.bag.Touch(); g.MarkDirty(); g.SaveProgress();
        gave = true;
        AHNet.Raise(AHNet.EvTradeGive, new object[] { o.ids.ToArray(), o.ns.ToArray(), o.money }, new[] { AHNet.TradeWith });
        AHNet.Peer peer; AHNet.Peers.TryGetValue(AHNet.TradeWith, out peer);
        if (got && peer != null) Done(g, peer);
    }
    public static void Cancel(AHGame g)
    {
        if (AHNet.TradeWith >= 0 && !gave) AHNet.Raise(AHNet.EvTradeCancel, true, new[] { AHNet.TradeWith });
        AHNet.EndTrade(false); g.ui.CloseTrade();
    }
}
