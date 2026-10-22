// Ashen Hollow: playing online with friends (Photon PUN 2).
// Everyone in the same land is in the same Photon room ("ah_<land>"): you see each other walk, ride, fight and emote,
// with your names and gear; what you say in chat reaches everyone in the land. Parties of up to four fight together:
// a blow you strike lands on your partners' copy of the same beast, and whoever is close when it falls gets its
// experience and their own loot. Two players can trade items and coins in a trade window that both must lock and
// confirm. Each player's save stays their own (on their own phone): the room only carries what is happening now.
// The game plays exactly as before when you are offline, when Photon is missing, or without an App ID
// (Resources/AH/Data/online.txt: "appid=...").
using System;
using System.Collections.Generic;
using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
#endif

public static class AHNet
{
    public const byte EvState = 1, EvChat = 2, EvEmote = 3, EvInvite = 10, EvInviteReply = 11, EvParty = 12, EvLeaveParty = 13, EvHit = 20,
        EvTradeReq = 30, EvTradeReply = 31, EvTradeOffer = 32, EvTradeReady = 33, EvTradeConfirm = 34, EvTradeGive = 35, EvTradeCancel = 36;
    public const string Protocol = "ah-net-1";   // players on different protocols never meet

    // ---------------- settings ----------------
    public static string AppId { get { var ta = Resources.Load<TextAsset>("AH/Data/online"); if (ta == null) return ""; foreach (var line in ta.text.Split('\n')) { var l = line.Trim(); if (l.StartsWith("appid=")) return l.Substring(6).Trim(); } return ""; } }
    public static bool HasSdk
    {
        get
        {
#if PHOTON_UNITY_NETWORKING
            return true;
#else
            return false;
#endif
        }
    }
    public static bool Ready { get { return HasSdk && AppId.Length > 10; } }
    public static bool Wanted { get { try { return AHPrefs.GetInt("ah_online", 1) == 1; } catch { return true; } } set { try { AHPrefs.SetInt("ah_online", value ? 1 : 0); } catch { } if (!value) Disconnect(); else Start(); } }
    public static string Status = "Offline";

    // ---------------- who is here ----------------
    public class Peer
    {
        public int actor; public string name = "Hero", cls = "warrior", look = "", gear = "", costume = "", title = "", mount = ""; public int level = 1, hp = 1, maxHp = 1;
        public Vector3 pos; public float yaw; public string clip = "Idle"; public bool loop = true, mounted; public float speed = 1f; public float seen;
        public AHNetAvatar avatar;
    }
    public static readonly Dictionary<int, Peer> Peers = new Dictionary<int, Peer>();
    public static int MyActor { get { return Actor(); } }
    public static bool InRoom
    {
        get
        {
#if PHOTON_UNITY_NETWORKING
            return PhotonNetwork.InRoom;
#else
            return false;
#endif
        }
    }

    // ---------------- party ----------------
    public static readonly List<int> Party = new List<int>();   // actors, the leader first (empty: no party)
    public static bool InParty { get { return Party.Count > 1; } }
    public static bool IsLeader { get { return Party.Count == 0 || Party[0] == MyActor; } }
    public static bool PartyHas(int actor) { return Party.Contains(actor); }

    // ---------------- trade ----------------
    public class Offer { public List<string> ids = new List<string>(); public List<int> ns = new List<int>(); public long money; public string Key { get { var s = money.ToString(); for (int i = 0; i < ids.Count; i++) s += "|" + ids[i] + "x" + ns[i]; return s; } } }
    public static int TradeWith = -1; public static Offer MyOffer = new Offer(), TheirOffer = new Offer();
    public static bool MyReady, TheirReady, MyConfirm, TheirConfirm; static string theirConfirmKey;

    static float sendT, propT; static string lastProps = "", landRoom = "";
    public static int BaseMobs;   // the beasts a land starts with (the same on every phone in that land): blows are shared on these
    public static void LandReady(AHGame g)
    {
        BaseMobs = g.mobs.Count;
        foreach (var p in Peers.Values) p.avatar = null;   // the old land's avatars went with it
        landRoom = "ah_" + AHGame.AreaId;
        Start();
#if PHOTON_UNITY_NETWORKING
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name != landRoom) { LeaveParty(); PhotonNetwork.LeaveRoom(); }
        else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom) JoinLand();
#endif
    }

    // ---------------- connecting ----------------
    public static void Start()
    {
        if (!Ready) { Status = HasSdk ? "Online play needs the Photon App ID" : "Online play is not installed"; return; }
        if (!Wanted) { Status = "Offline (switched off in Settings)"; return; }
#if PHOTON_UNITY_NETWORKING
        AHNetRunner.Ensure();
        if (PhotonNetwork.IsConnected) return;
        var s = PhotonNetwork.PhotonServerSettings.AppSettings;
        s.AppIdRealtime = AppId; s.FixedRegion = "eu"; s.AppVersion = Protocol;   // everyone on the same region, so friends always meet
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.SendRate = 20; PhotonNetwork.SerializationRate = 10;
        var p = AHGame.I != null ? AHGame.I.player : null;
        PhotonNetwork.NickName = p != null && !string.IsNullOrEmpty(p.heroName) ? p.heroName : "Hero";
        Status = "Connecting…";
        PhotonNetwork.ConnectUsingSettings();
#endif
    }
    public static void Disconnect()
    {
#if PHOTON_UNITY_NETWORKING
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
#endif
        ClearRoom(); Status = "Offline";
    }
    public static void JoinLand()
    {
#if PHOTON_UNITY_NETWORKING
        if (string.IsNullOrEmpty(landRoom) || !PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InRoom) return;
        Status = "Joining " + AHAuto.LandName(AHGame.AreaId) + "…";
        PhotonNetwork.JoinOrCreateRoom(landRoom, new RoomOptions { MaxPlayers = 20, CleanupCacheOnLeave = true, PublishUserId = false }, TypedLobby.Default);
#endif
    }
    public static void ClearRoom()
    {
        foreach (var p in Peers.Values) if (p.avatar != null) UnityEngine.Object.Destroy(p.avatar.gameObject);
        Peers.Clear(); Party.Clear(); EndTrade(false);
    }
    static int Actor()
    {
#if PHOTON_UNITY_NETWORKING
        return PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
#else
        return -1;
#endif
    }

    // ---------------- sending ----------------
    public static void Raise(byte code, object data, int[] to = null, bool reliable = true)
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.InRoom) return;
        var o = to != null ? new RaiseEventOptions { TargetActors = to } : new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(code, data, o, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
#endif
    }
    static int[] PartyOthers() { var l = new List<int>(); foreach (var a in Party) if (a != MyActor) l.Add(a); return l.ToArray(); }

    // what you say goes to everyone in the land
    public static void SendChat(string text) { if (InRoom) Raise(EvChat, text); }
    public static void SendEmote(string id) { if (InRoom) Raise(EvEmote, id); }

    // a blow on one of the land's own beasts: your party's copies take it too
    public static void LocalHit(AHMob m, int dmg)
    {
        if (!InParty || m == null || AHGame.I == null) return;
        int i = AHGame.I.mobs.IndexOf(m); if (i < 0 || i >= BaseMobs) return;
        Raise(EvHit, new object[] { i, m.type.id, dmg }, PartyOthers());
    }

    // every frame (AHUI): our state out, a few times a second; who we are, now and then
    public static void Tick(AHGame g, float dt)
    {
#if PHOTON_UNITY_NETWORKING
        if (!PhotonNetwork.InRoom || g == null || g.player == null) return;
        var p = g.player;
        if ((sendT -= dt) <= 0f)
        {
            sendT = 0.12f;
            var a = p.Anim; Vector3 pos = p.transform.position;
            Raise(EvState, new object[] { pos.x, pos.y, pos.z, p.transform.eulerAngles.y, a != null ? a.LastName ?? "Idle" : "Idle", a == null || a.LastLoop, a != null ? a.LastSpeed : 1f, p.mounted, Mathf.CeilToInt(p.hp), Mathf.CeilToInt(p.maxHp) }, null, false);
        }
        if ((propT -= dt) <= 0f)
        {
            propT = 3f;
            var gear = new List<string>(); foreach (var s in AHItems.GearSlots) gear.Add(AHWardrobe.Shown(p, s) ?? "");
            string look = JsonUtility.ToJson(p.look ?? new AHLook()), g2 = string.Join(",", gear.ToArray()), cos = AHCostumes.Worn(p) ?? "", ti = AHAch.Title(p) ?? "", mo = p.mountSel ?? "";
            string key = p.heroName + p.cls.id + p.level + look + g2 + cos + ti + mo;
            if (key != lastProps)
            {
                lastProps = key;
                var h = new ExitGames.Client.Photon.Hashtable { { "c", p.cls.id }, { "lv", p.level }, { "lk", look }, { "g", g2 }, { "co", cos }, { "t", ti }, { "m", mo } };
                PhotonNetwork.NickName = string.IsNullOrEmpty(p.heroName) ? "Hero" : p.heroName;
                PhotonNetwork.LocalPlayer.SetCustomProperties(h);
            }
        }
        // the others: drawn where they are
        foreach (var peer in Peers.Values) { if (peer.avatar == null && peer.seen > 0f) peer.avatar = AHNetAvatar.Make(g, peer); }
#endif
    }

    // ---------------- receiving ----------------
    public static void OnEvent(byte code, int from, object data)
    {
        var g = AHGame.I; if (g == null || g.player == null) return;
        Peer peer; Peers.TryGetValue(from, out peer);
        switch (code)
        {
            case EvState:
                {
                    if (peer == null) return; var d = data as object[]; if (d == null || d.Length < 10) return;
                    peer.pos = new Vector3((float)d[0], (float)d[1], (float)d[2]); peer.yaw = (float)d[3]; peer.clip = (string)d[4]; peer.loop = (bool)d[5]; peer.speed = (float)d[6]; peer.mounted = (bool)d[7]; peer.hp = (int)d[8]; peer.maxHp = Mathf.Max(1, (int)d[9]);
                    peer.seen = Time.time;
                    return;
                }
            case EvChat:
                {
                    if (peer == null) return; string t = data as string; if (string.IsNullOrEmpty(t)) return; if (t.Length > 120) t = t.Substring(0, 120);
                    AHChat.Add(PartyHas(from) ? "party" : "say", peer.name + ": " + t);
                    if (peer.avatar != null) peer.avatar.Say(t);
                    return;
                }
            case EvEmote:
                {
                    if (peer == null) return; var e = AHEmote.Find(data as string); if (e == null) return;
                    if (peer.avatar != null) peer.avatar.Say("*" + e.verb + "*");
                    return;
                }
            case EvInvite:
                {
                    if (peer == null || InParty) return;
                    g.ui.NetAsk(AHLoc.T(peer.name + " invites you to their party.", peer.name + " يدعوك إلى فريقه."), () => Raise(EvInviteReply, true, new[] { from }), () => Raise(EvInviteReply, false, new[] { from }));
                    return;
                }
            case EvInviteReply:
                {
                    if (peer == null) return;
                    if (!(data is bool) || !(bool)data) { g.ui.Toast(AHLoc.T(peer.name + " said no thanks.", peer.name + " اعتذر."), 2.5f); return; }
                    if (Party.Count == 0) Party.Add(MyActor);
                    if (!IsLeader || Party.Count >= 4 || Party.Contains(from)) return;
                    Party.Add(from); SendParty();
                    g.ui.Toast(AHLoc.T(peer.name + " joined your party.", peer.name + " انضم إلى فريقك."), 2.5f);
                    return;
                }
            case EvParty:
                {
                    var arr = data as int[]; if (arr == null) return;
                    Party.Clear(); Party.AddRange(arr);
                    if (!Party.Contains(MyActor)) Party.Clear();
                    if (Party.Count <= 1) Party.Clear();
                    return;
                }
            case EvLeaveParty:
                {
                    if (!Party.Contains(from)) return;
                    Party.Remove(from);
                    if (Party.Count <= 1) { Party.Clear(); g.ui.Toast(AHLoc.T("Your party broke up.", "انحلّ فريقك."), 2.5f); }
                    else if (IsLeader) SendParty();
                    return;
                }
            case EvHit:
                {
                    if (!PartyHas(from)) return; var d = data as object[]; if (d == null || d.Length < 3) return;
                    int i = (int)d[0]; string id = (string)d[1]; int dmg = (int)d[2];
                    if (i < 0 || i >= g.mobs.Count || i >= BaseMobs) return; var m = g.mobs[i];
                    if (m == null || m.dead || m.type == null || m.type.id != id) return;
                    // the last blow from a partner: the kill is ours too when we are close (our own experience and loot)
                    bool near = (m.transform.position - g.player.transform.position).magnitude < 40f && !g.player.dead;
                    m.Hurt(dmg, m.hp - dmg <= 0f && near ? g.player : null, true);
                    return;
                }
            default:
                AHNetTrade.OnEvent(g, code, from, peer, data);
                return;
        }
    }
    public static void SendParty() { if (Party.Count > 1) Raise(EvParty, Party.ToArray(), PartyOthers()); }
    public static void Invite(int actor)
    {
        if (Party.Count >= 4) { AHGame.I.ui.Toast(AHLoc.T("A party has four at most.", "الفريق أربعة على الأكثر."), 2f); return; }
        if (InParty && !IsLeader) { AHGame.I.ui.Toast(AHLoc.T("Only the party leader can invite.", "القائد وحده يدعو."), 2f); return; }
        Raise(EvInvite, true, new[] { actor });
        Peer p; if (Peers.TryGetValue(actor, out p)) AHGame.I.ui.Toast(AHLoc.T("Invited " + p.name + ".", "دعوت " + p.name + "."), 2f);
    }
    public static void LeaveParty()
    {
        if (!InParty) { Party.Clear(); return; }
        Raise(EvLeaveParty, true, PartyOthers()); Party.Clear();
    }

    // ---------------- the room's comings and goings (from the runner) ----------------
    public static void PeerIn(int actor, string name, System.Collections.IDictionary props)
    {
        Peer p; if (!Peers.TryGetValue(actor, out p)) { p = new Peer { actor = actor }; Peers[actor] = p; if (AHGame.I != null && AHGame.I.ui != null) AHChat.Add("system", name + AHLoc.T(" is here.", " هنا.")); }
        p.name = string.IsNullOrEmpty(name) ? "Hero" : name;
        if (props != null)
        {
            string old = p.cls + p.look + p.gear + p.costume + p.mount;
            if (props.Contains("c")) p.cls = props["c"] as string ?? p.cls;
            if (props.Contains("lv") && props["lv"] is int) p.level = (int)props["lv"];
            if (props.Contains("lk")) p.look = props["lk"] as string ?? "";
            if (props.Contains("g")) p.gear = props["g"] as string ?? "";
            if (props.Contains("co")) p.costume = props["co"] as string ?? "";
            if (props.Contains("t")) p.title = props["t"] as string ?? "";
            if (props.Contains("m")) p.mount = props["m"] as string ?? "";
            if (old != p.cls + p.look + p.gear + p.costume + p.mount && p.avatar != null) { UnityEngine.Object.Destroy(p.avatar.gameObject); p.avatar = null; }   // a new look: drawn again
        }
    }
    public static void PeerOut(int actor)
    {
        Peer p; if (!Peers.TryGetValue(actor, out p)) return;
        if (p.avatar != null) UnityEngine.Object.Destroy(p.avatar.gameObject);
        Peers.Remove(actor);
        AHChat.Add("system", p.name + AHLoc.T(" has left.", " غادر."));
        if (Party.Contains(actor)) { Party.Remove(actor); if (Party.Count <= 1) Party.Clear(); else if (IsLeader) SendParty(); }
        if (TradeWith == actor) EndTrade(true);
    }
    public static void EndTrade(bool say)
    {
        bool was = TradeWith >= 0;
        TradeWith = -1; MyOffer = new Offer(); TheirOffer = new Offer(); MyReady = TheirReady = MyConfirm = TheirConfirm = false;
        if (was && say && AHGame.I != null && AHGame.I.ui != null) { AHGame.I.ui.Toast(AHLoc.T("The trade was called off.", "أُلغيت المقايضة."), 2.5f); AHGame.I.ui.CloseTrade(); }
    }
}

#if PHOTON_UNITY_NETWORKING
// the Photon side: callbacks and events (kept across lands)
public class AHNetRunner : MonoBehaviourPunCallbacks, IOnEventCallback
{
    static AHNetRunner I; float retryT;
    public static void Ensure() { if (I != null) return; var go = new GameObject("AH_Net"); DontDestroyOnLoad(go); I = go.AddComponent<AHNetRunner>(); }
    public override void OnConnectedToMaster() { AHNet.Status = "Online"; AHNet.JoinLand(); }
    public override void OnJoinedRoom()
    {
        AHNet.ClearRoom();
        foreach (var p in PhotonNetwork.PlayerListOthers) AHNet.PeerIn(p.ActorNumber, p.NickName, p.CustomProperties);
        int n = PhotonNetwork.CurrentRoom.PlayerCount - 1;
        AHNet.Status = "Online · " + AHAuto.LandName(AHGame.AreaId) + (n > 0 ? " · " + n + (n == 1 ? " other hero" : " other heroes") : "");
        if (n > 0 && AHGame.I != null && AHGame.I.ui != null) AHGame.I.ui.Toast(AHLoc.T(n + (n == 1 ? " other hero is" : " other heroes are") + " in this land.", n + " أبطال آخرون في هذه الأرض."), 3f);
    }
    public override void OnLeftRoom() { AHNet.ClearRoom(); }
    public override void OnJoinRoomFailed(short code, string msg) { AHNet.Status = "Could not join the land (" + msg + ")"; }
    public override void OnPlayerEnteredRoom(Player p) { AHNet.PeerIn(p.ActorNumber, p.NickName, p.CustomProperties); }
    public override void OnPlayerLeftRoom(Player p) { AHNet.PeerOut(p.ActorNumber); }
    public override void OnPlayerPropertiesUpdate(Player p, ExitGames.Client.Photon.Hashtable changed) { if (p.IsLocal) return; AHNet.PeerIn(p.ActorNumber, p.NickName, p.CustomProperties); }
    public override void OnDisconnected(DisconnectCause cause) { AHNet.ClearRoom(); AHNet.Status = "Offline (" + cause + ")"; retryT = 15f; }
    public void OnEvent(EventData e) { if (e.Code >= 200) return; try { AHNet.OnEvent(e.Code, e.Sender, e.CustomData); } catch (Exception ex) { Debug.LogWarning("Ashen Hollow: online event " + e.Code + " (" + ex.Message + ")"); } }
    void Update()
    {
        if (retryT > 0f && (retryT -= Time.unscaledDeltaTime) <= 0f && AHNet.Wanted && !PhotonNetwork.IsConnected) AHNet.Start();
    }
}
#endif
