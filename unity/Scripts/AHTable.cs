// Ashen Hollow: games at the inn's tables, each on its own full-screen table:
//  - Beastcards, with Card sharp Wilma: a 3 x 3 board, five cards a side. Every monster card has four numbers, one on
//    each edge; lay a card next to the other player's and if your edge beats theirs it turns to your colour. Whoever owns
//    more of the board when it is full wins. Your hand comes from your own collection (the cards you added) and the
//    spare cards in your bag, plus five common beasts everyone starts with. Stronger beasts have bigger numbers.
//    Three tables: a friendly game, a copper table and the high table; winning at a stake table can win you one of
//    the other player's cards.
//  - Bones, with Old Ottar: roll the die as often as you dare, a one loses everything you rolled this turn; hold to
//    bank it. First to 30.
//  - Arm-wrestling, with Big Brann: tap PUSH as fast as you can and force his arm down before he forces yours.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AHTable : MonoBehaviour
{
    public static bool Open { get { return I != null && I.gameObject.activeSelf; } }
    static AHTable I;
    AHGame g;
    RectTransform root, panel;
    Text title, hint;
    readonly List<GameObject> live = new List<GameObject>();
    Action onUpdate;

    // ---------- the table itself: a dimmed screen, a glass panel, a title and a hint ----------
    static AHTable Make(AHGame g)
    {
        if (I == null)
        {
            var go = new GameObject("AH_Table");
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 600;
            var cs = go.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.6f;
            go.AddComponent<GraphicRaycaster>();
            I = go.AddComponent<AHTable>();
            I.root = go.GetComponent<RectTransform>();
            var dim = I.Box(I.root, Vector2.zero, Vector2.zero, new Color(0.02f, 0.03f, 0.06f, 0.78f), false); dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.offsetMin = dim.offsetMax = Vector2.zero;
            I.panel = I.Box(I.root, Vector2.zero, new Vector2(1500, 960), new Color(0.07f, 0.09f, 0.14f, 0.94f), true);
            I.Edge(I.panel, new Color(0.62f, 0.8f, 1f, 0.55f));
            I.title = I.Label(I.panel, "", new Vector2(0, 430), new Vector2(1300, 70), 46, new Color(1f, 0.88f, 0.6f), true);
            I.hint = I.Label(I.panel, "", new Vector2(0, 378), new Vector2(1350, 40), 24, new Color(0.85f, 0.9f, 1f, 0.85f), false);
            I.Btn(I.panel, "✕", new Vector2(700, 430), new Vector2(70, 70), new Color(0.35f, 0.18f, 0.18f, 0.95f), () => I.Close());
            I.live.Clear();   // (the frame stays; only what each game puts on it is cleared)
        }
        I.g = g; I.gameObject.SetActive(true);
        if (g.player != null && g.player.mounted) AHComp.Dismount(g, true);
        I.Clear();
        return I;
    }
    void Clear() { foreach (var x in live) if (x != null) Destroy(x); live.Clear(); onUpdate = null; StopAllCoroutines(); }
    void Close() { Clear(); gameObject.SetActive(false); }
    public static void CloseAll() { if (I != null) I.Close(); }
    public static void TestDeal(AHGame g) { var t = Make(g); var p = g.player; t.hand.Clear(); var own = Owned(p); t.hand.AddRange(own.GetRange(0, Mathf.Min(5, own.Count))); t.StartCards(Stakes(p)[0]); }
    void Update() { if (onUpdate != null) onUpdate(); if (Input.GetKeyDown(KeyCode.Escape)) Close(); }

    // ---------- little UI helpers ----------
    static Sprite round;
    static Sprite Round()
    {
        if (round != null) return round;
        const int N = 64, R = 18; var t = new Texture2D(N, N, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(R - x - 0.5f, x + 0.5f - (N - R))), dy = Mathf.Max(0, Mathf.Max(R - y - 0.5f, y + 0.5f - (N - R)));
                float d = Mathf.Sqrt(dx * dx + dy * dy); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(R - d + 0.5f)));
            }
        t.Apply();
        round = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        return round;
    }
    static Sprite disc;
    static Sprite Disc()
    {
        if (disc != null) return disc;
        const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(N / 2f - d))); }
        t.Apply(); disc = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f)); return disc;
    }
    RectTransform Box(RectTransform parent, Vector2 pos, Vector2 size, Color c, bool rounded)
    {
        var go = new GameObject("box", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var im = go.GetComponent<Image>(); im.color = c; if (rounded) { im.sprite = Round(); im.type = Image.Type.Sliced; }
        if (parent != root && parent != panel) { } else if (parent == panel) live.Add(go);
        return rt;
    }
    void Edge(RectTransform r, Color c) { var o = r.gameObject.AddComponent<Outline>(); o.effectColor = c; o.effectDistance = new Vector2(2, -2); }
    Text Label(RectTransform parent, string s, Vector2 pos, Vector2 size, int fs, Color c, bool titleFont, TextAnchor a = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("txt", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = go.GetComponent<Text>(); t.font = titleFont ? AHUI.TitleFont : AHUI.MainFont; t.fontSize = fs; t.color = c; t.alignment = a; t.text = s; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        if (parent == panel && go != null && I != null && title != null) live.Add(go);
        return t;
    }
    UnityEngine.UI.Button Btn(RectTransform parent, string s, Vector2 pos, Vector2 size, Color c, Action act)
    {
        var rt = Box(parent, pos, size, c, true); Edge(rt, new Color(1f, 1f, 1f, 0.25f));
        var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.onClick.AddListener(() => { AHSound.Play("step"); act(); });
        var t = Label(rt, s, Vector2.zero, size, Mathf.RoundToInt(Mathf.Min(34, size.y * 0.45f)), Color.white, false);
        return b;
    }
    void Head(string t, string h) { title.text = t; hint.text = h; }
    static string Money(long cu) { return AHItems.MoneyText(cu * AHDB.CU); }
    bool Pay(long cu)
    {
        var p = g.player; long c = cu * AHDB.CU; if (c <= 0) return true;
        if (p.bag.money < c) { g.ui.Toast("You need " + AHItems.MoneyText(c) + " to sit at this table.", 2.5f); return false; }
        p.bag.money -= c; p.bag.Touch(); g.MarkDirty(); return true;
    }
    void Win(long cu) { var p = g.player; if (cu > 0) { p.AddMoney(cu * AHDB.CU, p.transform.position); AHSound.Play("coin"); } p.bag.Touch(); g.MarkDirty(); }

    // =====================================================================================================
    // ---------- Beastcards ----------
    // =====================================================================================================
    public static readonly string[] Starter = { "deer", "boar", "crab", "bogtoad", "wolf" };
    class Card { public string id, name; public int[] s; public int owner; public RectTransform rt; public Image bg; }
    // a beast's four edges (top, right, bottom, left): stronger beasts have bigger numbers
    public static int[] Sides(string id)
    {
        var d = AHJson.O(AHDB.Mobs, id);
        float lvl = (float)AHJson.N(d, "lvl", 1);
        bool big = AHJson.B(d, "boss") || AHJson.B(d, "world") || AHJson.B(d, "dboss") || AHJson.B(d, "elite") || AHJson.B(d, "rare");
        int total = Mathf.Clamp(Mathf.RoundToInt(10f + lvl * 0.3f + (big ? 5f : 0f)), 9, 32);
        var r = new System.Random(id.GetHashCode());
        var w = new float[4]; float sum = 0f; for (int i = 0; i < 4; i++) { w[i] = 0.4f + (float)r.NextDouble(); sum += w[i]; }
        var s = new int[4]; int got = 0; for (int i = 0; i < 4; i++) { s[i] = Mathf.Clamp(Mathf.RoundToInt(total * w[i] / sum), 1, 10); got += s[i]; }
        // keep the total (within the 1..10 bounds)
        for (int k = 0; k < 12 && got != total; k++) { int i = r.Next(4); if (got < total && s[i] < 10) { s[i]++; got++; } else if (got > total && s[i] > 1) { s[i]--; got--; } }
        return s;
    }
    static int Strength(string id) { int t = 0; foreach (var v in Sides(id)) t += v; return t; }
    static string Name(string id) { var n = AHJson.S(AHJson.O(AHDB.Mobs, id), "name", id); return n.Split(',')[0]; }
    // every card you can play: the starters, your collection and the spare cards in your bag
    public static List<string> Owned(AHPlayer p)
    {
        var l = new List<string>(Starter);
        foreach (var c in p.prog.cards) if (!l.Contains(c) && AHJson.O(AHDB.Mobs, c) != null) l.Add(c);
        var co = AHDB.Table("monsters", "CARD_OF") as Dictionary<string, object>;
        if (co != null) foreach (var k in co.Keys) if (!l.Contains(k) && p.bag.Count("card_" + k) > 0) l.Add(k);
        l.Sort((a, b) => Strength(b).CompareTo(Strength(a)));
        return l;
    }

    class Stake { public string name, blurb; public long fee; public int maxLvl, minLvl; public float prize; }
    static Stake[] Stakes(AHPlayer p)
    {
        int L = Mathf.Max(1, p.level);
        return new[] {
            new Stake { name = "Friendly game", blurb = "No coin. Wilma plays common beasts.", fee = 0, minLvl = 0, maxLvl = 12, prize = 0f },
            new Stake { name = "Copper table", blurb = "25 copper in, 50 back if you win · a chance to win one of her cards", fee = 25, minLvl = Mathf.Max(0, L - 12), maxLvl = L + 6, prize = 0.25f },
            new Stake { name = "High table", blurb = "150 copper in, 300 back · she plays her best: bosses and rare beasts", fee = 150, minLvl = Mathf.Max(10, L), maxLvl = 999, prize = 0.45f },
        };
    }

    public static void OpenCards(AHGame g) { var t = Make(g); t.CardsMenu(); }
    void CardsMenu()
    {
        Clear();
        var p = g.player; var own = Owned(p);
        long wins = AHProgress.Get(p.prog.stats, "cardwins");
        Head("Beastcards · Card sharp Wilma", "Beat her edges with yours to turn her cards to your colour. You own " + own.Count + " cards · " + wins + " games won.");
        float y = 230f; int i = 0;
        foreach (var st in Stakes(p))
        {
            var s = st; var row = Box(panel, new Vector2(0, y - i * 150f), new Vector2(1250, 130), new Color(1f, 1f, 1f, 0.06f), true);
            Label(row, "<b>" + s.name + "</b>", new Vector2(-300, 22), new Vector2(560, 50), 34, new Color(1f, 0.9f, 0.65f), true, TextAnchor.MiddleLeft);
            Label(row, s.blurb, new Vector2(-300, -26), new Vector2(560, 50), 22, new Color(0.85f, 0.9f, 1f), false, TextAnchor.MiddleLeft);
            Btn(row, s.fee > 0 ? "Play · " + Money(s.fee) : "Play", new Vector2(450, 0), new Vector2(300, 84), new Color(0.2f, 0.45f, 0.28f, 0.95f), () => PickHand(s));
            i++;
        }
        Label(panel, "Your strongest: " + string.Join(", ", own.GetRange(0, Mathf.Min(5, own.Count)).ConvertAll(Name).ToArray()), new Vector2(0, -260), new Vector2(1300, 60), 24, new Color(0.8f, 0.85f, 0.95f, 0.8f), false);
        Label(panel, "Monster cards drop from beasts now and then. Add them to your collection from the bag and they join your hand here.", new Vector2(0, -320), new Vector2(1300, 60), 22, new Color(0.8f, 0.85f, 0.95f, 0.6f), false);
    }

    // choose five
    readonly List<string> hand = new List<string>(); int pickPage;
    void PickHand(Stake s)
    {
        Clear(); var p = g.player; var own = Owned(p);
        if (hand.Count == 0 || hand.Exists(x => !own.Contains(x))) { hand.Clear(); hand.AddRange(own.GetRange(0, Mathf.Min(5, own.Count))); }
        Head("Choose your five cards", s.name + " · tap cards to pick them · " + hand.Count + " / 5 chosen");
        const int per = 10; int pages = Mathf.Max(1, (own.Count + per - 1) / per); pickPage = Mathf.Clamp(pickPage, 0, pages - 1);
        for (int k = 0; k < per && pickPage * per + k < own.Count; k++)
        {
            string id = own[pickPage * per + k]; bool on = hand.Contains(id);
            var c = DrawCard(panel, id, new Vector2(-560 + (k % 5) * 280, 170 - (k / 5) * 300), on ? 0 : -1, 1f);
            if (on) Edge(c.rt, new Color(0.5f, 1f, 0.6f, 1f));
            var b = c.rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.onClick.AddListener(() => { if (hand.Contains(id)) hand.Remove(id); else if (hand.Count < 5) hand.Add(id); PickHand(s); });
        }
        if (pages > 1)
        {
            Btn(panel, "◀", new Vector2(-660, -390), new Vector2(110, 70), new Color(0.3f, 0.3f, 0.4f, 0.95f), () => { pickPage = (pickPage + pages - 1) % pages; PickHand(s); });
            Btn(panel, "▶", new Vector2(-520, -390), new Vector2(110, 70), new Color(0.3f, 0.3f, 0.4f, 0.95f), () => { pickPage = (pickPage + 1) % pages; PickHand(s); });
        }
        Btn(panel, "Best five", new Vector2(-150, -390), new Vector2(260, 76), new Color(0.3f, 0.3f, 0.42f, 0.95f), () => { hand.Clear(); hand.AddRange(own.GetRange(0, Mathf.Min(5, own.Count))); PickHand(s); });
        Btn(panel, "Back", new Vector2(150, -390), new Vector2(220, 76), new Color(0.3f, 0.3f, 0.42f, 0.95f), CardsMenu);
        var play = Btn(panel, s.fee > 0 ? "Deal · " + Money(s.fee) : "Deal", new Vector2(480, -390), new Vector2(320, 84), new Color(0.2f, 0.45f, 0.28f, 0.95f), () => { if (hand.Count == 5 && Pay(s.fee)) StartCards(s); });
        play.interactable = hand.Count == 5;
    }

    Card DrawCard(RectTransform parent, string id, Vector2 pos, int owner, float scale)
    {
        var c = new Card { id = id, name = Name(id), s = Sides(id), owner = owner };
        c.rt = Box(parent, pos, new Vector2(230, 280) * scale, CardCol(owner), true); live.Add(c.rt.gameObject);
        c.bg = c.rt.GetComponent<Image>();
        Edge(c.rt, new Color(1f, 0.9f, 0.6f, 0.5f));
        var it = AHItems.Get("card_" + id); var sp = it != null ? AHItemIcons.Best(it) : null;
        if (sp != null) { var ic = Box(c.rt, new Vector2(0, 8) * scale, new Vector2(120, 120) * scale, Color.white, false); ic.GetComponent<Image>().sprite = sp; ic.GetComponent<Image>().preserveAspect = true; }
        Label(c.rt, c.name, new Vector2(0, -100) * scale, new Vector2(210, 40) * scale, Mathf.RoundToInt(22 * scale), Color.white, false);
        Vector2[] at = { new Vector2(0, 112), new Vector2(92, 8), new Vector2(0, -60), new Vector2(-92, 8) };
        for (int i = 0; i < 4; i++)
        {
            var d = Box(c.rt, at[i] * scale, new Vector2(46, 46) * scale, new Color(0.05f, 0.05f, 0.08f, 0.85f), false); d.GetComponent<Image>().sprite = Disc();
            Label(d, c.s[i] == 10 ? "A" : c.s[i].ToString(), Vector2.zero, new Vector2(46, 46) * scale, Mathf.RoundToInt(28 * scale), new Color(1f, 0.92f, 0.6f), true);
        }
        return c;
    }
    static Color CardCol(int owner) { return owner == 0 ? new Color(0.16f, 0.32f, 0.55f, 0.97f) : owner == 1 ? new Color(0.55f, 0.18f, 0.17f, 0.97f) : new Color(0.22f, 0.22f, 0.27f, 0.97f); }

    // the match
    Card[] board; List<Card> mine, theirs; Card held; bool myTurn, busy; Stake stake; Text score;
    RectTransform[] cells;
    void StartCards(Stake s)
    {
        Clear(); stake = s; board = new Card[9]; cells = new RectTransform[9]; held = null; busy = false;
        Head("Beastcards · " + s.name, "Tap one of your cards, then an empty square. Higher edge beats lower: the card turns your colour.");
        for (int i = 0; i < 9; i++)
        {
            int k = i; var cell = Box(panel, new Vector2(-250 + (i % 3) * 250, 210 - (i / 3) * 290), new Vector2(236, 284), new Color(1f, 1f, 1f, 0.07f), true);
            var b = cell.gameObject.AddComponent<UnityEngine.UI.Button>(); b.onClick.AddListener(() => PlaceMine(k)); cells[i] = cell;
        }
        // their five, from the stake's range of beasts
        var p = g.player; var pool = new List<string>(); var co = AHDB.Table("monsters", "CARD_OF") as Dictionary<string, object>;
        if (co != null) foreach (var k in co.Keys) { float lv = (float)AHJson.N(AHJson.O(AHDB.Mobs, k), "lvl", 1); if (lv >= s.minLvl && lv <= s.maxLvl) pool.Add(k); }
        if (pool.Count < 5) pool.AddRange(Starter);
        if (s.maxLvl >= 999) pool.Sort((a, b) => Strength(b).CompareTo(Strength(a)));
        var them = new List<string>();
        for (int i = 0; i < 40 && them.Count < 5; i++) { string id = s.maxLvl >= 999 ? pool[Mathf.Min(them.Count + UnityEngine.Random.Range(0, 3), pool.Count - 1)] : pool[UnityEngine.Random.Range(0, pool.Count)]; if (!them.Contains(id)) them.Add(id); }
        while (them.Count < 5) them.Add(Starter[them.Count]);
        mine = new List<Card>(); theirs = new List<Card>();
        for (int i = 0; i < 5; i++) mine.Add(DrawCard(panel, hand[i], new Vector2(-640, 300 - i * 150), 0, 0.6f));
        for (int i = 0; i < 5; i++) { var c = DrawCard(panel, them[i], new Vector2(640, 300 - i * 150), 1, 0.6f); theirs.Add(c); }
        foreach (var c in mine) { var cc = c; var b = c.rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.onClick.AddListener(() => Hold(cc)); }
        score = Label(panel, "", new Vector2(0, -455), new Vector2(900, 50), 30, Color.white, true);
        myTurn = UnityEngine.Random.value < 0.5f; Score();
        Label(panel, "You", new Vector2(-640, 400), new Vector2(200, 40), 28, new Color(0.6f, 0.8f, 1f), true);
        Label(panel, "Wilma", new Vector2(640, 400), new Vector2(200, 40), 28, new Color(1f, 0.6f, 0.55f), true);
        if (!myTurn) StartCoroutine(TheirTurn());
    }
    void Hold(Card c)
    {
        if (!myTurn || busy || !mine.Contains(c)) return;
        if (held != null) held.rt.localScale = Vector3.one;
        held = c; c.rt.localScale = Vector3.one * 1.12f;
    }
    void PlaceMine(int cell)
    {
        if (!myTurn || busy || held == null || board[cell] != null) return;
        var c = held; held = null; c.rt.localScale = Vector3.one;
        mine.Remove(c); Place(c, cell, 0);
        myTurn = false; Score();
        if (Full()) { StartCoroutine(EndCards()); return; }
        StartCoroutine(TheirTurn());
    }
    void Place(Card c, int cell, int owner)
    {
        board[cell] = c; c.owner = owner;
        Destroy(c.rt.gameObject);
        var n = DrawCard(panel, c.id, cells[cell].anchoredPosition, owner, 1f); c.rt = n.rt; c.bg = n.bg;
        AHSound.Play("step");
        int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };   // top, right, bottom, left
        int x = cell % 3, y = cell / 3;
        for (int e = 0; e < 4; e++)
        {
            int nx = x + dx[e], ny = y + dy[e]; if (nx < 0 || nx > 2 || ny < 0 || ny > 2) continue;
            var o = board[ny * 3 + nx]; if (o == null || o.owner == owner) continue;
            if (c.s[e] > o.s[(e + 2) % 4]) StartCoroutine(Flip(o, owner));
        }
    }
    IEnumerator Flip(Card c, int owner)
    {
        c.owner = owner; float t = 0f;
        while (t < 0.24f) { t += Time.unscaledDeltaTime; float k = t / 0.24f; c.rt.localScale = new Vector3(Mathf.Abs(Mathf.Cos(k * Mathf.PI)), 1f, 1f); if (k > 0.5f) c.bg.color = CardCol(owner); yield return null; }
        c.rt.localScale = Vector3.one; AHSound.Play("coin"); Score();
    }
    bool Full() { foreach (var c in board) if (c == null) return false; return true; }
    void Score()
    {
        int a = 0, b = 0; foreach (var c in board) if (c != null) { if (c.owner == 0) a++; else b++; }
        a += mine.Count; b += theirs.Count;
        if (score != null) score.text = "<color=#9cc8ff>You " + a + "</color>   ·   <color=#ff9a90>Wilma " + b + "</color>" + (busy || Full() ? "" : myTurn ? "   ·   your turn" : "   ·   Wilma is thinking…");
    }
    IEnumerator TheirTurn()
    {
        busy = true; Score(); yield return new WaitForSecondsRealtime(0.9f);
        // she lays the card and square that turn the most of yours (and leaves the weakest edge to the wall)
        Card best = null; int bestCell = -1; float bestV = -99f;
        foreach (var c in theirs)
            for (int cell = 0; cell < 9; cell++)
            {
                if (board[cell] != null) continue;
                float v = 0f; int x = cell % 3, y = cell / 3; int[] dx = { 0, 1, 0, -1 }, dy = { -1, 0, 1, 0 };
                for (int e = 0; e < 4; e++)
                {
                    int nx = x + dx[e], ny = y + dy[e];
                    if (nx < 0 || nx > 2 || ny < 0 || ny > 2) { v += (10 - c.s[e]) * 0.05f; continue; }   // a weak edge against the wall is safe
                    var o = board[ny * 3 + nx];
                    if (o == null) v -= (10 - c.s[e]) * 0.04f;
                    else if (o.owner != 1 && c.s[e] > o.s[(e + 2) % 4]) v += 1f;
                }
                v += UnityEngine.Random.value * (stake.prize > 0.4f ? 0.05f : 0.35f);
                if (v > bestV) { bestV = v; best = c; bestCell = cell; }
            }
        busy = false;
        if (best != null) { theirs.Remove(best); Place(best, bestCell, 1); }
        myTurn = true; Score();
        if (Full()) StartCoroutine(EndCards());
    }
    IEnumerator EndCards()
    {
        busy = true; yield return new WaitForSecondsRealtime(0.8f);
        int a = mine.Count, b = theirs.Count; foreach (var c in board) if (c.owner == 0) a++; else b++;
        var p = g.player; string res;
        if (a > b)
        {
            Win(stake.fee * 2); p.prog.Bump("cardwins"); if (stake.maxLvl >= 999) p.prog.Bump("cardhigh");
            res = "You win " + a + " to " + b + "!" + (stake.fee > 0 ? " +" + Money(stake.fee * 2) : "");
            if (stake.prize > 0f && UnityEngine.Random.value < stake.prize)
            {
                var won = new List<string>(); foreach (var c in board) if (c.owner == 0 && !hand.Contains(c.id) && AHItems.Get("card_" + c.id) != null) won.Add(c.id);
                if (won.Count > 0) { string id = won[UnityEngine.Random.Range(0, won.Count)]; if (p.bag.Add("card_" + id)) res += " Wilma hands over her " + Name(id) + " card!"; }
            }
            AHSound.Play("level");
        }
        else if (a == b) { Win(stake.fee); res = "A draw, " + a + " to " + b + ". Your coin back."; }
        else res = "Wilma wins " + b + " to " + a + ". “Again?”";
        Head("Beastcards · " + stake.name, res);
        Btn(panel, "Play again", new Vector2(-180, -380), new Vector2(300, 80), new Color(0.2f, 0.45f, 0.28f, 0.95f), () => PickHand(stake));
        Btn(panel, "Leave the table", new Vector2(180, -380), new Vector2(320, 80), new Color(0.3f, 0.3f, 0.42f, 0.95f), Close);
        g.MarkDirty();
    }

    // =====================================================================================================
    // ---------- Bones: the dice game ----------
    // =====================================================================================================
    public static void OpenDice(AHGame g) { var t = Make(g); t.DiceMenu(); }
    int myScore, hisScore, turnPts, die; long diceFee; Text diceTxt, diceRes; RectTransform dieRt; bool rolling;
    readonly List<GameObject> pips = new List<GameObject>();
    void DiceMenu()
    {
        Clear();
        Head("Bones · Old Ottar", "Roll as often as you dare; a one loses everything you rolled this turn. Hold to bank it. First to 30 wins.");
        long[] fees = { 10, 50, 200 }; string[] names = { "A friendly roll", "Copper bones", "Silver bones" };
        for (int i = 0; i < 3; i++)
        {
            long f = fees[i]; var row = Box(panel, new Vector2(0, 200 - i * 150), new Vector2(1100, 130), new Color(1f, 1f, 1f, 0.06f), true);
            Label(row, "<b>" + names[i] + "</b>", new Vector2(-220, 20), new Vector2(560, 50), 34, new Color(1f, 0.9f, 0.65f), true, TextAnchor.MiddleLeft);
            Label(row, Money(f) + " in, " + Money(f * 2) + " back if you win", new Vector2(-220, -26), new Vector2(560, 50), 22, new Color(0.85f, 0.9f, 1f), false, TextAnchor.MiddleLeft);
            Btn(row, "Play · " + Money(f), new Vector2(380, 0), new Vector2(300, 84), new Color(0.2f, 0.45f, 0.28f, 0.95f), () => { if (Pay(f)) StartDice(f); });
        }
    }
    void StartDice(long fee)
    {
        Clear(); diceFee = fee; myScore = hisScore = turnPts = 0; rolling = false;
        Head("Bones · Old Ottar", "First to 30. A one ends your turn and loses what you rolled in it.");
        dieRt = Box(panel, new Vector2(0, 90), new Vector2(240, 240), new Color(0.96f, 0.93f, 0.85f, 1f), true); live.Add(dieRt.gameObject); Edge(dieRt, new Color(0.4f, 0.3f, 0.2f, 1f));
        diceTxt = Label(panel, "", new Vector2(0, -110), new Vector2(1200, 60), 34, Color.white, true);
        diceRes = Label(panel, "", new Vector2(0, -170), new Vector2(1200, 50), 26, new Color(1f, 0.9f, 0.7f), false);
        Btn(panel, "Roll", new Vector2(-180, -300), new Vector2(300, 100), new Color(0.2f, 0.45f, 0.28f, 0.95f), () => { if (!rolling) StartCoroutine(Roll(true)); });
        Btn(panel, "Hold", new Vector2(180, -300), new Vector2(300, 100), new Color(0.45f, 0.35f, 0.18f, 0.95f), () => { if (!rolling && turnPts > 0) { myScore += turnPts; turnPts = 0; DiceText("You bank it."); if (myScore >= 30) EndDice(true); else StartCoroutine(Ottar()); } });
        ShowDie(6); DiceText("Your roll.");
    }
    void DiceText(string s) { diceTxt.text = "<color=#9cc8ff>You " + myScore + "</color>   ·   this turn " + turnPts + "   ·   <color=#ff9a90>Ottar " + hisScore + "</color>"; diceRes.text = s; }
    void ShowDie(int v)
    {
        die = v; foreach (var x in pips) Destroy(x); pips.Clear();
        var at = new Dictionary<int, Vector2[]> {
            { 1, new[] { Vector2.zero } }, { 2, new[] { new Vector2(-60, 60), new Vector2(60, -60) } }, { 3, new[] { new Vector2(-60, 60), Vector2.zero, new Vector2(60, -60) } },
            { 4, new[] { new Vector2(-60, 60), new Vector2(60, 60), new Vector2(-60, -60), new Vector2(60, -60) } },
            { 5, new[] { new Vector2(-60, 60), new Vector2(60, 60), Vector2.zero, new Vector2(-60, -60), new Vector2(60, -60) } },
            { 6, new[] { new Vector2(-60, 70), new Vector2(60, 70), new Vector2(-60, 0), new Vector2(60, 0), new Vector2(-60, -70), new Vector2(60, -70) } } };
        foreach (var q in at[v]) { var d = Box(dieRt, q, new Vector2(44, 44), v == 1 ? new Color(0.75f, 0.1f, 0.1f) : new Color(0.12f, 0.1f, 0.1f), false); d.GetComponent<Image>().sprite = Disc(); pips.Add(d.gameObject); }
    }
    IEnumerator Roll(bool me)
    {
        rolling = true;
        for (int i = 0; i < 8; i++) { ShowDie(UnityEngine.Random.Range(1, 7)); dieRt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-25f, 25f)); AHSound.Play("step"); yield return new WaitForSecondsRealtime(0.07f); }
        dieRt.localRotation = Quaternion.identity;
        int v = UnityEngine.Random.Range(1, 7); ShowDie(v); rolling = false;
        if (v == 1) { turnPts = 0; DiceText(me ? "A one! You lose this turn's points." : "Ottar rolls a one!"); AHSound.Play("hurt"); if (me) StartCoroutine(Ottar()); }
        else { turnPts += v; DiceText((me ? "You roll " : "Ottar rolls ") + v + "."); }
    }
    IEnumerator Ottar()
    {
        rolling = true; turnPts = 0; yield return new WaitForSecondsRealtime(0.8f);
        while (true)
        {
            yield return StartCoroutine(Roll(false)); rolling = true;
            if (turnPts == 0) break;
            int want = hisScore >= myScore + 8 ? 10 : myScore >= 22 ? 30 - hisScore : 15;
            if (hisScore + turnPts >= 30 || turnPts >= want) { hisScore += turnPts; DiceText("Ottar banks " + turnPts + "."); turnPts = 0; break; }
            yield return new WaitForSecondsRealtime(0.6f);
        }
        rolling = false;
        if (hisScore >= 30) { EndDice(false); yield break; }
        DiceText(diceRes.text + " Your roll.");
    }
    void EndDice(bool won)
    {
        StopAllCoroutines(); rolling = true;
        if (won) { Win(diceFee * 2); g.player.prog.Bump("dicewins"); AHSound.Play("level"); }
        diceRes.text = won ? "You win! +" + Money(diceFee * 2) : "Ottar wins. “Heh. Another round?”";
        Btn(panel, "Again", new Vector2(-180, -420), new Vector2(260, 70), new Color(0.2f, 0.45f, 0.28f, 0.95f), DiceMenu);
        Btn(panel, "Leave", new Vector2(180, -420), new Vector2(260, 70), new Color(0.3f, 0.3f, 0.42f, 0.95f), Close);
    }

    // =====================================================================================================
    // ---------- arm-wrestling ----------
    // =====================================================================================================
    public static void OpenArm(AHGame g) { var t = Make(g); t.ArmMenu(); }
    float armPos, armT, brannK; long armFee; bool armOn; RectTransform knob; Text armTxt;
    void ArmMenu()
    {
        Clear();
        Head("Arm-wrestling · Big Brann", "Tap PUSH as fast as you can (or the space bar). Force his arm down before he forces yours.");
        long[] fees = { 10, 40, 150 }; string[] names = { "Warm-up", "Proper match", "Brann's full strength" }; float[] ks = { 0.75f, 1.0f, 1.3f };
        for (int i = 0; i < 3; i++)
        {
            long f = fees[i]; float k = ks[i]; var row = Box(panel, new Vector2(0, 200 - i * 150), new Vector2(1100, 130), new Color(1f, 1f, 1f, 0.06f), true);
            Label(row, "<b>" + names[i] + "</b>", new Vector2(-220, 20), new Vector2(560, 50), 34, new Color(1f, 0.9f, 0.65f), true, TextAnchor.MiddleLeft);
            Label(row, Money(f) + " in, " + Money(f * 2) + " back if you win", new Vector2(-220, -26), new Vector2(560, 50), 22, new Color(0.85f, 0.9f, 1f), false, TextAnchor.MiddleLeft);
            Btn(row, "Play · " + Money(f), new Vector2(380, 0), new Vector2(300, 84), new Color(0.2f, 0.45f, 0.28f, 0.95f), () => { if (Pay(f)) StartArm(f, k); });
        }
    }
    void StartArm(long fee, float k)
    {
        Clear(); armFee = fee; brannK = k; armPos = 0f; armT = -3f; armOn = true;
        Head("Arm-wrestling · Big Brann", "Get ready…");
        var bar = Box(panel, new Vector2(0, 80), new Vector2(1100, 70), new Color(0.15f, 0.15f, 0.2f, 1f), true); live.Add(bar.gameObject);
        var mine2 = Box(bar, new Vector2(-412, 0), new Vector2(275, 70), new Color(0.2f, 0.4f, 0.7f, 0.9f), true);
        var his = Box(bar, new Vector2(412, 0), new Vector2(275, 70), new Color(0.65f, 0.2f, 0.18f, 0.9f), true);
        Label(bar, "YOU", new Vector2(-470, 0), new Vector2(150, 60), 30, Color.white, true); Label(bar, "BRANN", new Vector2(470, 0), new Vector2(150, 60), 30, Color.white, true);
        knob = Box(bar, Vector2.zero, new Vector2(90, 110), new Color(1f, 0.85f, 0.55f, 1f), true); Edge(knob, new Color(0.3f, 0.2f, 0.1f, 1f));
        Label(knob, "✊", Vector2.zero, new Vector2(90, 110), 54, new Color(0.35f, 0.22f, 0.12f), false);
        armTxt = Label(panel, "", new Vector2(0, -60), new Vector2(1000, 60), 36, Color.white, true);
        Btn(panel, "PUSH!", new Vector2(0, -250), new Vector2(520, 170), new Color(0.6f, 0.25f, 0.15f, 0.95f), Push);
        onUpdate = ArmTick;
    }
    void Push() { if (!armOn || armT < 0f) return; armPos -= 0.042f; knob.localScale = Vector3.one * 1.08f; }
    void ArmTick()
    {
        if (!armOn) return;
        if (Input.GetKeyDown(KeyCode.Space)) Push();
        float dt = Time.unscaledDeltaTime; armT += dt;
        if (armT < 0f) { armTxt.text = Mathf.CeilToInt(-armT).ToString(); return; }
        // Brann pushes steadily, with a heave every few seconds
        float heave = (Mathf.Sin(armT * 1.7f) > 0.85f) ? 2.2f : 1f;
        armPos += 0.33f * brannK * heave * dt;
        knob.localScale = Vector3.Lerp(knob.localScale, Vector3.one, dt * 10f);
        knob.anchoredPosition = new Vector2(armPos * 500f, 0);
        armTxt.text = armT < 1f ? "PUSH!" : armPos < -0.6f ? "He's going down!" : armPos > 0.6f ? "Hold on!" : "Push!";
        if (armPos <= -1f || armPos >= 1f || armT > 14f)
        {
            armOn = false; bool won = armPos < 0f;
            if (won) { Win(armFee * 2); g.player.prog.Bump("armwins"); AHSound.Play("level"); } else AHSound.Play("hurt");
            Head("Arm-wrestling · Big Brann", won ? "You slam his arm down! +" + Money(armFee * 2) : "Brann wins. “Ha! Come back when you've eaten more stew.”");
            Btn(panel, "Again", new Vector2(-180, -420), new Vector2(260, 70), new Color(0.2f, 0.45f, 0.28f, 0.95f), ArmMenu);
            Btn(panel, "Leave", new Vector2(180, -420), new Vector2(260, 70), new Color(0.3f, 0.3f, 0.42f, 0.95f), Close);
        }
    }
}
