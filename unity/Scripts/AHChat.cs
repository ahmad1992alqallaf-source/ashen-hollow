// Ashen Hollow: the chat. A slim bar under the joystick shows the newest line (what people said to you, loot, quests,
// the story); its "–" tucks it into a small Chat tab that lights up when something new comes in. Your party talks now and then (in a fight, or on the road). Tap the bar to
// slide the chat open from the left: tabs for All, Party and System, and a Say button to type. What you say shows over your head for a few
// seconds, and someone you are standing next to may answer. Commands: /wave /bow /cheer /dance /sit /nod /no /point
// (emotes), /story, /deep, /map, /where, /time, /help.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHChat
{
    public class Msg { public string ch, text; public float at; }
    public static readonly List<Msg> Log = new List<Msg>();
    public static event Action Changed;
    public const int Keep = 80;

    public static void Add(string ch, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (Log.Count > 0 && Log[Log.Count - 1].text == text && Time.unscaledTime - Log[Log.Count - 1].at < 3f) return;   // the same line twice in a row
        Log.Add(new Msg { ch = ch, text = text, at = Time.unscaledTime });
        if (Log.Count > Keep) Log.RemoveAt(0);
        if (Changed != null) Changed();
    }
    // a toast: someone's line ("Name: “...”") is said aloud; the rest is the game telling you something
    public static void FromToast(string s) { if (s == null) return; Add(s.Contains(": “") ? "say" : "system", s); }

    public static string Color(string ch)
    {
        switch (ch) { case "say": return "#ffffff"; case "you": return "#ffe08a"; case "party": return "#8fd6ff"; case "story": return "#ffb85a"; default: return "#c8c0b0"; }
    }
    public static string Tag(string ch)
    {
        switch (ch) { case "say": return "[Say] "; case "you": return "[Say] "; case "party": return "[Party] "; case "story": return "[Story] "; default: return ""; }
    }

    // ---- your party's banter ----
    static readonly Dictionary<string, string[]> FightLines = new Dictionary<string, string[]>
    {
        { "knight", new[] { "Stay behind my shield!", "Come at me, beast!", "Hold the line!" } },
        { "ranger", new[] { "Got one in my sights.", "Keep it busy, I'll do the rest.", "Arrow away!" } },
        { "cleric", new[] { "I've got you, keep fighting!", "Light, guard us.", "Don't you dare fall on me." } },
        { "kael", new[] { "Ember strike!", "Behind you!", "My fists still burn. Good." } },
    };
    static readonly Dictionary<string, string[]> RoadLines = new Dictionary<string, string[]>
    {
        { "knight", new[] { "A fine day to keep the road safe.", "My armour squeaks. Remind me to oil it.", "I once fought a boar the size of a cart. True story." } },
        { "ranger", new[] { "Smell that? Rain's coming.", "Tracks here. Wolves, three of them.", "You walk loud. I like that, keeps the deer honest." } },
        { "cleric", new[] { "Remember to eat something.", "The Light keeps odd hours, but it keeps them.", "If you get hurt, tell me before you bleed on my robe." } },
        { "kael", new[] { "The ash has gone quiet. I don't trust quiet.", "Seren says hello. She always does.", "My order would have liked you." } },
    };
    static float nextTalk = 40f;
    public static void Tick(AHGame g, float dt)
    {
        var p = g.player; if (p == null || p.dead || p.party.Count == 0) return;
        nextTalk -= dt; if (nextTalk > 0f) return;
        bool fight = false; foreach (var m in g.mobs) if (m != null && !m.dead && m.Chasing && (m.transform.position - p.transform.position).sqrMagnitude < 18f * 18f) { fight = true; break; }
        nextTalk = fight ? UnityEngine.Random.Range(14f, 26f) : UnityEngine.Random.Range(60f, 120f);
        string who = p.party[UnityEngine.Random.Range(0, p.party.Count)]; string[] l;
        if (!(fight ? FightLines : RoadLines).TryGetValue(who, out l)) return;
        Add("party", AHComp.MercName(who) + ": " + l[UnityEngine.Random.Range(0, l.Length)]);
    }

    // ---- what you type ----
    public static void Say(AHGame g, string text)
    {
        var p = g.player; text = (text ?? "").Trim(); if (p == null || text.Length == 0) return;
        if (text.Length > 120) text = text.Substring(0, 120);
        if (text.StartsWith("/")) { Command(g, text.Substring(1).ToLowerInvariant()); return; }
        string me = string.IsNullOrEmpty(p.heroName) ? "You" : p.heroName;
        Add("you", me + ": " + text);
        if (g.ui != null) g.ui.SayBubble(text);
        // someone close by answers with their own words
        var n = AHNpc.Nearest(p.transform.position);
        if (n != null && n.DistTo(p.transform.position) < 5f) { var line = n.Talk(); if (line != null) g.StartCoroutine(Later(1.2f, () => Add("say", line))); }
    }
    static System.Collections.IEnumerator Later(float t, Action a) { yield return new WaitForSecondsRealtime(t); a(); }

    static void Command(AHGame g, string c)
    {
        var p = g.player;
        string[] emotes = { "wave", "bow", "cheer", "dance", "sit", "point", "no", "box", "talk", "fold", "eat" };
        if (c == "nod") c = "yes";
        if (Array.IndexOf(emotes, c) >= 0 || c == "yes") { p.Emote(c); var d = AHEmote.Find(c); Add("system", (string.IsNullOrEmpty(p.heroName) ? "You" : p.heroName) + " " + (d != null ? d.verb : c) + "."); return; }
        switch (c)
        {
            case "help": Add("system", "Commands: /wave /bow /nod /no /cheer /dance /sit /point /story /deep /map /where /time"); return;
            case "story": Add("story", AHStory.Status(p)); g.ui.OpenChat(false); g.ui.OpenStory(); return;
            case "deep": Add("system", "The Ashen Deep · this week " + AHDeep.WeekBoss.name + " · " + AHDeep.WeekTwist.name + (AHDeep.Best > 0 ? " · your best " + AHDeep.Clock(AHDeep.Best) : "")); return;
            case "map": g.ui.OpenChat(false); g.ui.OpenMap(); return;
            case "where": Add("system", "You are in " + (g.data != null ? g.data.region : AHGame.AreaId) + "."); return;
            case "time": Add("system", "It is " + g.TimeText() + " in the Hollow" + (g.IsNight ? ", and the night beasts are out." : ".")); return;
        }
        Add("system", "No such command. Type /help for the list.");
    }
}

public partial class AHUI
{
    RectTransform chatBox, chatFull, sayBubble, chatPill; Text chatText, chatFullText, sayText, chatTabT; CanvasGroup chatGroup; Image chatPillImg;
    string chatTab = "all"; float chatShowT, sayT; bool chatTyping; string chatDraft = ""; TouchScreenKeyboard chatKb;
    bool chatMin, chatClosing; const float ChatW = 500f, ChatH = 600f;

    void BuildChat()
    {
        // the chat bar: one slim line under the joystick, the newest message in it. Tap it to slide the chat open;
        // the little "–" tucks it away into a small Chat tab (tap that to bring the bar back)
        chatBox = Img("ChatBox", transform, white, new Vector2(0f, 0f), new Vector2(190, 28), new Vector2(340, 34), new Color(0.05f, 0.04f, 0.035f, 0.78f));
        chatGroup = chatBox.gameObject.AddComponent<CanvasGroup>(); chatGroup.blocksRaycasts = false;
        chatText = Label(chatBox, "T", "", 14, TextAnchor.MiddleLeft, new Vector2(10, -3), new Vector2(286, 28), Color.white);
        chatText.supportRichText = true; chatText.horizontalOverflow = HorizontalWrapMode.Wrap; chatText.verticalOverflow = VerticalWrapMode.Truncate;
        var ol = chatText.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0f, 0f, 0f, 0.85f); ol.effectDistance = new Vector2(1.2f, -1.2f);   // readable over bright grass
        var hide = Img("Hide", chatBox, white, new Vector2(1f, 0.5f), new Vector2(-18, 0), new Vector2(30, 28), new Color(0.3f, 0.22f, 0.15f, 0.9f));
        Center(Label(hide, "T", "–", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(30, 28), Color.white));
        taps.Add(new TapBtn { rt = hide, act = () => SetChatMin(true) });   // before the bar's own tap: the button sits on it
        taps.Add(new TapBtn { rt = chatBox, act = () => OpenChat(true) });
        chatPill = Img("ChatPill", transform, white, new Vector2(0f, 0f), new Vector2(52, 28), new Vector2(84, 30), new Color(0.05f, 0.04f, 0.035f, 0.62f));
        chatPillImg = chatPill.GetComponent<Image>();
        Center(Label(chatPill, "T", "Chat +", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(84, 28), Color.white));
        taps.Add(new TapBtn { rt = chatPill, act = () => SetChatMin(false) });
        try { chatMin = PlayerPrefs.GetInt("chatMin", 0) == 1; } catch { }

        // the open chat: a tall panel that slides in from the left, to read back and to type
        chatFull = Img("ChatFull", transform, white, new Vector2(0f, 0.5f), new Vector2(-ChatW * 0.5f, 0), new Vector2(ChatW, ChatH), new Color(0.07f, 0.055f, 0.045f, 0.97f));
        Img("Back", chatFull, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ChatW, ChatH), new Color(0.07f, 0.055f, 0.045f, 0.97f));   // solid, whatever the skin does to panels
        Img("Edge", chatFull, white, new Vector2(1f, 0.5f), new Vector2(-2, 0), new Vector2(4, ChatH), new Color(0.85f, 0.6f, 0.25f, 1f));
        var tl = Label(chatFull, "Title", "Chat", 26, TextAnchor.UpperLeft, new Vector2(20, -14), new Vector2(300, 34), new Color(0.95f, 0.65f, 0.3f)); tl.fontStyle = FontStyle.Bold;
        var x = Img("Close", chatFull, white, new Vector2(1f, 1f), new Vector2(-36, -32), new Vector2(52, 44), new Color(0.45f, 0.22f, 0.17f, 1f));
        Center(Label(x, "T", "‹", 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(52, 44), Color.white));
        taps.Add(new TapBtn { rt = x, layer = 9, act = () => OpenChat(false) });
        string[] tabs = { "all", "party", "system" }; string[] names = { "All", "Party", "System" };
        for (int i = 0; i < tabs.Length; i++)
        {
            string k = tabs[i];
            var b = Img("Tab_" + k, chatFull, white, new Vector2(0f, 1f), new Vector2(72 + i * 112, -80), new Vector2(104, 38), new Color(0.22f, 0.16f, 0.11f, 1f));
            Center(Label(b, "T", names[i], 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(104, 30), Color.white));
            taps.Add(new TapBtn { rt = b, layer = 9, act = () => { chatTab = k; RenderChat(); } });
        }
        chatFullText = Label(chatFull, "Log", "", 17, TextAnchor.LowerLeft, new Vector2(20, -110), new Vector2(ChatW - 40, ChatH - 200), Color.white);
        chatFullText.supportRichText = true; chatFullText.horizontalOverflow = HorizontalWrapMode.Wrap; chatFullText.verticalOverflow = VerticalWrapMode.Truncate;
        var say = Img("Say", chatFull, white, new Vector2(0.5f, 0f), new Vector2(0, 44), new Vector2(ChatW - 40, 54), new Color(0.22f, 0.5f, 0.26f, 1f));
        chatTabT = Center(Label(say, "T", "Say…", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(ChatW - 60, 40), Color.white));
        taps.Add(new TapBtn { rt = say, layer = 9, act = StartChatTyping });
        chatFull.gameObject.SetActive(false);

        // what you say, over your head
        sayBubble = Img("SayBubble", transform, white, new Vector2(0f, 0f), Vector2.zero, new Vector2(300, 44), new Color(1f, 1f, 1f, 0.92f));
        sayText = Center(Label(sayBubble, "T", "", 17, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(290, 40), new Color(0.15f, 0.1f, 0.06f)));
        sayBubble.gameObject.SetActive(false);

        AHChat.Changed += () => { chatShowT = 10f; if (ChatOpen) RenderChat(); };
    }

    void SetChatMin(bool on) { chatMin = on; try { PlayerPrefs.SetInt("chatMin", on ? 1 : 0); } catch { } }

    public bool ChatOpen { get { return chatFull != null && chatFull.gameObject.activeSelf; } }
    public void OpenChat(bool on)
    {
        if (chatFull == null) return;
        if (on)
        {
            ShowWork(false); chatTab = "all"; chatClosing = false;
            if (!chatFull.gameObject.activeSelf) chatFull.anchoredPosition = new Vector2(-ChatW * 0.5f, 0f);   // slides in from the left edge
            chatFull.gameObject.SetActive(true); chatFull.SetAsLastSibling();
        }
        else { chatClosing = true; chatTyping = false; if (chatKb != null) { chatKb.active = false; chatKb = null; } }
        ptrs.Clear(); RenderChat();
    }

    public void SayBubble(string s)
    {
        if (sayBubble == null) return;
        sayText.text = s; float w = Mathf.Clamp(s.Length * 9f + 30f, 80f, 420f);
        sayBubble.sizeDelta = new Vector2(w, 44f); sayText.rectTransform.sizeDelta = new Vector2(w - 10f, 40f);
        sayT = 4.5f;
    }

    static string Line(AHChat.Msg m) { return "<color=" + AHChat.Color(m.ch) + ">" + AHChat.Tag(m.ch) + m.text + "</color>"; }

    void RenderChat()
    {
        if (chatFullText == null) return;
        var sb = new System.Text.StringBuilder(); int n = 0;
        for (int i = AHChat.Log.Count - 1; i >= 0 && n < 18; i--)
        {
            var m = AHChat.Log[i];
            if (chatTab == "party" && m.ch != "party" && m.ch != "you" && m.ch != "say") continue;
            if (chatTab == "system" && m.ch != "system" && m.ch != "story") continue;
            sb.Insert(0, Line(m) + "\n"); n++;
        }
        chatFullText.text = n == 0 ? "<color=#a09888>Nothing here yet.</color>" : sb.ToString().TrimEnd('\n');
        foreach (Transform t in chatFull) if (t.name.StartsWith("Tab_")) { var im = t.GetComponent<Image>(); if (im != null) im.color = t.name == "Tab_" + chatTab ? new Color(0.62f, 0.42f, 0.18f, 1f) : new Color(0.22f, 0.16f, 0.11f, 1f); }
        chatTabT.text = chatTyping ? (chatDraft.Length > 0 ? chatDraft : "type…") + "_" : "Say…";
    }

    void StartChatTyping()
    {
        chatTyping = true; chatDraft = ""; AHInput.TypedChars();
        if (TouchScreenKeyboard.isSupported) chatKb = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "Say something (or /help)", 120);
        RenderChat();
    }

    void ChatKeys()
    {
        if (!chatTyping) return;
        bool send = false;
        if (chatKb != null)
        {
            chatDraft = chatKb.text ?? "";
            if (chatKb.status == TouchScreenKeyboard.Status.Done) send = true;
            else if (chatKb.status != TouchScreenKeyboard.Status.Visible) { chatTyping = false; chatKb = null; RenderChat(); return; }
        }
        else
            foreach (char c in AHInput.TypedChars())
            {
                if (c == '\b') { if (chatDraft.Length > 0) chatDraft = chatDraft.Substring(0, chatDraft.Length - 1); }
                else if (c == '\n' || c == '\r') send = true;
                else if (!char.IsControl(c) && chatDraft.Length < 120) chatDraft += c;
            }
        if (send) { chatTyping = false; chatKb = null; string s = chatDraft; chatDraft = ""; AHChat.Say(g, s); }
        RenderChat();
    }

    void ChatTick(float dt)
    {
        if (chatBox == null) return;
        // the open chat slides in and out
        if (chatFull.gameObject.activeSelf)
        {
            float to = chatClosing ? -ChatW * 0.5f : ChatW * 0.5f, x = chatFull.anchoredPosition.x;
            x = Mathf.Lerp(x, to, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f)); if (Mathf.Abs(x - to) < 2f) x = to;
            chatFull.anchoredPosition = new Vector2(x, 0f);
            if (chatClosing && x <= -ChatW * 0.5f + 1f) { chatFull.gameObject.SetActive(false); chatClosing = false; }
        }
        if (ChatOpen && !chatClosing) ChatKeys();
        AHChat.Tick(g, dt);
        // the chat bar (or, tucked away, the little Chat tab, which lights up when something new comes in)
        bool free = !ChatOpen && Modal == 0 && !PhotoOn;
        bool bar = free && !chatMin, pill = free && chatMin;
        if (chatBox.gameObject.activeSelf != bar) chatBox.gameObject.SetActive(bar);
        if (chatPill.gameObject.activeSelf != pill) chatPill.gameObject.SetActive(pill);
        chatShowT -= dt;
        if (pill) chatPillImg.color = Color.Lerp(chatPillImg.color, chatShowT > 0f ? new Color(0.62f, 0.38f, 0.14f, 0.9f) : new Color(0.05f, 0.04f, 0.035f, 0.62f), 1f - Mathf.Exp(-dt * 4f));
        if (bar)
        {
            chatGroup.alpha = Mathf.Lerp(chatGroup.alpha, chatShowT > 0f ? 1f : 0.85f, 1f - Mathf.Exp(-dt * 3f));
            chatText.text = AHChat.Log.Count > 0 ? Line(AHChat.Log[AHChat.Log.Count - 1]) : "<color=#a09888>Tap to chat</color>";
        }
        // the bubble over your head
        var p = g.player;
        if (sayT > 0f && p != null && g.cam != null && (Modal == 0 || ChatOpen))
        {
            sayT -= dt;
            Vector3 sp = g.cam.WorldToScreenPoint(p.transform.position + Vector3.up * 2.75f);
            bool on = sp.z > 0f && sayT > 0f;
            if (sayBubble.gameObject.activeSelf != on) sayBubble.gameObject.SetActive(on);
            if (on) sayBubble.position = sp;
        }
        else if (sayBubble.gameObject.activeSelf) sayBubble.gameObject.SetActive(false);
    }
}
