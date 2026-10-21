// Ashen Hollow: talent boards, after Aion 2's Daevanion boards, in our own Celestial Glass look. Four boards for every
// class, each opening at a level (10, 25, 40, 55): Might, Guard, Arcana and Mastery. Each is a diamond of 24 stones
// around a centre; you start at the centre and take a stone next to one you already hold, so you choose your own
// way out. Small stones give a little (damage, health, armour, cooldowns...), the four great stones at the tips give
// a lot. One point a level from level 10 on: never enough for everything, so the route matters. Clearing a board
// gives the points back for a little coin. This sits beside the class tree (path, form and talents), not instead.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class AHBoards
{
    public class Stone { public int x, y; public string key; public float v; public bool great; public string name; }
    public class Board { public string id, name; public int lvl; public Color col; public List<Stone> stones = new List<Stone>(); }
    public const int R = 3;
    static List<Board> boards;

    static Stone S(int x, int y, string key, float v, bool great, string name) { return new Stone { x = x, y = y, key = key, v = v, great = great, name = name }; }
    public static List<Board> All
    {
        get
        {
            if (boards != null) return boards;
            boards = new List<Board>();
            Make("might", "Might", 10, new Color(1f, 0.55f, 0.35f), new[] { "dmg", "crit", "atkspd" }, new[] { 0.012f, 0.005f, 0.012f },
                new[] { S(3, 0, "dmg", 0.06f, true, "Warlord's edge"), S(-3, 0, "crit", 0.03f, true, "Keen eye"), S(0, 3, "atkspd", 0.06f, true, "Flurry"), S(0, -3, "rage", 1f, true, "Last stand") });
            Make("guard", "Guard", 25, new Color(0.55f, 0.8f, 1f), new[] { "hp", "armor", "block" }, new[] { 14f, 0.018f, 0.008f },
                new[] { S(3, 0, "hp", 90f, true, "Mountain heart"), S(-3, 0, "armor", 0.07f, true, "Ironhide"), S(0, 3, "block", 0.04f, true, "Bulwark"), S(0, -3, "thorns", 0.06f, true, "Bramble mail") });
            Make("arcana", "Arcana", 40, new Color(0.75f, 0.6f, 1f), new[] { "cdr", "heal", "leech" }, new[] { 0.01f, 0.015f, 0.004f },
                new[] { S(3, 0, "cdr", 0.05f, true, "Quickened mind"), S(-3, 0, "heal", 0.08f, true, "Wellspring"), S(0, 3, "leech", 0.025f, true, "Bloodbond"), S(0, -3, "evo", 0.1f, true, "Ascendant") });
            Make("mastery", "Mastery", 55, new Color(0.94f, 0.84f, 0.54f), new[] { "evade", "dmg", "hp" }, new[] { 0.005f, 0.008f, 10f },
                new[] { S(3, 0, "evade", 0.035f, true, "Windstep"), S(-3, 0, "killHeal", 0.025f, true, "Victor's breath"), S(0, 3, "dmg", 0.05f, true, "Mastery"), S(0, -3, "hp", 70f, true, "Undying") });
            return boards;
        }
    }
    static readonly Dictionary<string, string> KeyName = new Dictionary<string, string>
    {
        { "dmg", "Power" }, { "crit", "Precision" }, { "atkspd", "Haste" }, { "hp", "Vigour" }, { "armor", "Stone skin" }, { "block", "Parry" },
        { "cdr", "Focus" }, { "heal", "Grace" }, { "leech", "Hunger" }, { "evade", "Grace of foot" },
    };
    static void Make(string id, string name, int lvl, Color col, string[] keys, float[] vals, Stone[] greats)
    {
        var b = new Board { id = id, name = name, lvl = lvl, col = col };
        for (int y = -R; y <= R; y++)
            for (int x = -R; x <= R; x++)
            {
                int d = Mathf.Abs(x) + Mathf.Abs(y); if (d == 0 || d > R) continue;
                var gs = Array.Find(greats, s => s.x == x && s.y == y);
                if (gs != null) { b.stones.Add(gs); continue; }
                int k = Mathf.Abs((x * 7 + y * 3 + 30)) % keys.Length;
                float v = vals[k] * (d == 2 ? 1.25f : 1f);
                b.stones.Add(S(x, y, keys[k], v, false, KeyName.ContainsKey(keys[k]) ? KeyName[keys[k]] : keys[k]));
            }
        boards.Add(b);
    }

    public static int Points(AHPlayer p) { return Mathf.Max(0, p.level - 9); }
    public static int Spent(AHPlayer p) { return AHEvo.CP(p).board.Count; }
    public static int Free(AHPlayer p) { return Points(p) - Spent(p); }
    static string Key(Board b, Stone s) { return b.id + ":" + s.x + "," + s.y; }
    public static bool Has(AHPlayer p, Board b, int x, int y) { return (x == 0 && y == 0) || AHEvo.CP(p).board.Contains(b.id + ":" + x + "," + y); }
    public static bool Has(AHPlayer p, Board b, Stone s) { return AHEvo.CP(p).board.Contains(Key(b, s)); }
    public static bool Open(AHPlayer p, Board b) { return p.level >= b.lvl; }
    public static bool CanTake(AHPlayer p, Board b, Stone s)
    {
        if (!Open(p, b) || Has(p, b, s) || Free(p) <= 0) return false;
        return Has(p, b, s.x + 1, s.y) || Has(p, b, s.x - 1, s.y) || Has(p, b, s.x, s.y + 1) || Has(p, b, s.x, s.y - 1);
    }
    public static void Take(AHGame g, Board b, Stone s)
    {
        var p = g.player; if (!CanTake(p, b, s)) return;
        AHEvo.CP(p).board.Add(Key(b, s)); p.Recalc(); g.MarkDirty();
        AHSound.Play(s.great ? "level" : "coin");
    }
    public static int On(AHPlayer p, Board b) { int n = 0; foreach (var k in AHEvo.CP(p).board) if (k.StartsWith(b.id + ":")) n++; return n; }
    public static long ClearCost(AHPlayer p, Board b) { return (long)Math.Round(On(p, b) * (4 + p.level * 0.5) * AHDB.CU); }
    public static bool Clear(AHGame g, Board b)
    {
        var p = g.player; long c = ClearCost(p, b); if (On(p, b) == 0) return false;
        if (p.bag.money < c) { g.ui.Toast("Clearing this board costs " + AHItems.MoneyText(c) + "."); return false; }
        p.bag.money -= c; p.bag.Touch();
        AHEvo.CP(p).board.RemoveAll(k => k.StartsWith(b.id + ":"));
        p.Recalc(); g.MarkDirty(); return true;
    }

    // into the class passives (AHEvo.CalcPass)
    public static void AddPass(AHPlayer p, Dictionary<string, float> o)
    {
        var cp = AHEvo.CP(p); if (cp == null || cp.board.Count == 0) return;
        foreach (var b in All)
            foreach (var s in b.stones)
            {
                if (!cp.board.Contains(Key(b, s))) continue;
                float cur; o.TryGetValue(s.key, out cur);
                o[s.key] = s.key == "rage" ? Mathf.Max(cur, 1f) : cur + s.v;
            }
    }
    public static string Text(Stone s)
    {
        var d = new Dictionary<string, object> { { s.key, s.key == "rage" ? (object)true : (object)(double)s.v } };
        return AHEvo.PassText(d);
    }
}

public partial class AHUI
{
    int boardSel; AHBoards.Stone stoneSel;

    void RenderBoards(AHPlayer p)
    {
        SetPager(false); wkPage.text = "";
        var all = AHBoards.All; boardSel = Mathf.Clamp(boardSel, 0, all.Count - 1);
        var b = all[boardSel]; bool open = AHBoards.Open(p, b);
        wkHint.text = "Talent boards: " + AHBoards.Free(p) + " points free (" + AHBoards.Points(p) + " earned, one a level from level 10). Start at the centre and take a stone next to one you hold. The great stones are at the tips.";
        // the board picker along the top
        for (int i = 0; i < all.Count; i++)
        {
            int ii = i; var bb = all[i]; bool on = AHBoards.Open(p, bb);
            var br = CBtn(wkList, bb.name + (on ? " · " + AHBoards.On(p, bb) : " · Lv " + bb.lvl), new Vector2(98 + i * 196, -92), 188, 40, i == boardSel ? new Color(bb.col.r * 0.5f, bb.col.g * 0.5f, bb.col.b * 0.5f, 1f) : Plain, true, () => { boardSel = ii; stoneSel = null; RenderWork(); });
            wkItems.Add(br.gameObject);
        }
        // the board: a diamond of stones on frosted glass
        const float Cell = 44f, BX = 180f, BY = -280f;
        var pane = Img("Board", wkList, white, new Vector2(0f, 1f), new Vector2(BX, BY), new Vector2(350, 320), new Color(0.06f, 0.1f, 0.16f, 0.85f));
        wkItems.Add(pane.gameObject);
        Func<int, int, Vector2> at = (x, y) => new Vector2(BX + x * Cell, BY + y * Cell);
        // the links: lit between stones you hold
        foreach (var s in b.stones)
            foreach (var n in new[] { new Vector2Int(s.x + 1, s.y), new Vector2Int(s.x, s.y + 1) })
            {
                if (Mathf.Abs(n.x) + Mathf.Abs(n.y) > AHBoards.R) continue;
                bool lit = AHBoards.Has(p, b, s.x, s.y) && AHBoards.Has(p, b, n.x, n.y);
                Vector2 a0 = at(s.x, s.y), a1 = at(n.x, n.y);
                var ln = Img("Link", wkList, white, new Vector2(0f, 1f), (a0 + a1) * 0.5f, new Vector2(n.x != s.x ? Cell : 3f, n.x != s.x ? 3f : Cell), lit ? new Color(b.col.r, b.col.g, b.col.b, 0.9f) : new Color(0.75f, 0.85f, 0.95f, 0.18f));
                wkItems.Add(ln.gameObject);
            }
        // centre ties to its four neighbours
        foreach (var n in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) })
        {
            bool lit = AHBoards.Has(p, b, n.x, n.y); Vector2 a1 = at(n.x, n.y), a0 = at(0, 0);
            var ln = Img("Link", wkList, white, new Vector2(0f, 1f), (a0 + a1) * 0.5f, new Vector2(n.x != 0 ? Cell : 3f, n.x != 0 ? 3f : Cell), lit ? new Color(b.col.r, b.col.g, b.col.b, 0.9f) : new Color(0.75f, 0.85f, 0.95f, 0.18f));
            wkItems.Add(ln.gameObject);
        }
        var c0 = Img("Centre", wkList, circle, new Vector2(0f, 1f), at(0, 0), new Vector2(30, 30), new Color(b.col.r, b.col.g, b.col.b, open ? 1f : 0.3f)); wkItems.Add(c0.gameObject);
        foreach (var s in b.stones)
        {
            bool has = AHBoards.Has(p, b, s), can = AHBoards.CanTake(p, b, s), sel = stoneSel == s;
            float sz = s.great ? 38f : 26f;
            Color c = has ? b.col : can ? new Color(0.5f, 0.88f, 1f, 0.95f) : new Color(0.45f, 0.5f, 0.58f, open ? 0.85f : 0.35f);
            if (sel) { var hl = Img("Sel", wkList, ring, new Vector2(0f, 1f), at(s.x, s.y), new Vector2(sz + 14, sz + 14), new Color(0.94f, 0.84f, 0.54f, 1f)); wkItems.Add(hl.gameObject); }
            var st = Img("Stone", wkList, s.great ? star != null ? star : circle : circle, new Vector2(0f, 1f), at(s.x, s.y), new Vector2(sz, sz), c);
            wkItems.Add(st.gameObject);
            var ss = s; taps.Add(new TapBtn { rt = st, layer = 5, group = "work", act = () => { stoneSel = ss; RenderWork(); } });
        }
        // the card at the right
        float CX = 372f, CW = (WkW - 36f) - CX;
        var card = Img("Card", wkList, white, new Vector2(0f, 1f), new Vector2(CX + CW / 2f, -280f), new Vector2(CW, 320), new Color(0.13f, 0.1f, 0.075f, 1f)); wkItems.Add(card.gameObject);
        var tt = Label(card, "T", b.name + " board", 22, TextAnchor.UpperLeft, new Vector2(16, -12), new Vector2(CW - 30, 30), b.col); tt.fontStyle = FontStyle.Bold;
        Label(card, "S", open ? AHBoards.On(p, b) + " of " + b.stones.Count + " stones held" : "Opens at level " + b.lvl, 15, TextAnchor.UpperLeft, new Vector2(16, -44), new Vector2(CW - 30, 22), new Color(1f, 1f, 1f, 0.7f));
        if (stoneSel != null && b.stones.Contains(stoneSel))
        {
            var s = stoneSel; bool has = AHBoards.Has(p, b, s), can = AHBoards.CanTake(p, b, s);
            var nm = Label(card, "N", (s.great ? "★ " : "") + s.name, 20, TextAnchor.UpperLeft, new Vector2(16, -86), new Vector2(CW - 30, 28), s.great ? new Color(0.94f, 0.84f, 0.54f) : Color.white); nm.fontStyle = FontStyle.Bold;
            var tx = Label(card, "X", AHBoards.Text(s), 17, TextAnchor.UpperLeft, new Vector2(16, -118), new Vector2(CW - 30, 50), new Color(0.5f, 0.88f, 1f)); tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            string why = has ? "You hold this stone." : !open ? "This board opens at level " + b.lvl + "." : AHBoards.Free(p) <= 0 ? "No points free. You get one each level." : can ? "Next to a stone you hold." : "Take a stone next to it first.";
            Label(card, "W", why, 15, TextAnchor.UpperLeft, new Vector2(16, -172), new Vector2(CW - 30, 22), new Color(1f, 1f, 1f, 0.65f));
            CBtn(card, "Take stone", new Vector2(CW / 2f, -230), CW - 40, 46, Go, can, () => { AHBoards.Take(g, b, s); RenderWork(); });
        }
        else Label(card, "H", "Tap a stone to read it.", 16, TextAnchor.UpperLeft, new Vector2(16, -90), new Vector2(CW - 30, 22), new Color(1f, 1f, 1f, 0.7f));
        long cost = AHBoards.ClearCost(p, b);
        CBtn(card, "Clear board · " + AHItems.MoneyText(cost), new Vector2(CW / 2f, -296), CW - 40, 40, Plain, AHBoards.On(p, b) > 0 && p.bag.money >= cost, () => { if (AHBoards.Clear(g, b)) { stoneSel = null; Toast("The " + b.name + " board is clear. Your points are back."); } RenderWork(); });
    }
}
