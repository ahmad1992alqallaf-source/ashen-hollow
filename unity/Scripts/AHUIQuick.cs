using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// the quick buttons by the fight buttons: food, elixir, health potion, mana potion, each holding the thing you picked for it
// (tap: use it; the small ▾ on its corner: choose what goes in it; nothing picked, or run out: the best one you carry)
public partial class AHUI
{
    class QuickBtn { public string slot; public RectTransform rt; public Image icon, cool; public Text n; public string shown; }
    readonly List<QuickBtn> quickBtns = new List<QuickBtn>();
    string quickSlot = "food";
    float autoEatT;

    public static string QuickPick(string slot) { try { return AHPrefs.GetString("ah_q_" + slot, ""); } catch { return ""; } }
    static void SetQuickPick(string slot, string id) { try { AHPrefs.SetString("ah_q_" + slot, id ?? ""); } catch { } }
    public static bool AutoEat { get { try { return AHPrefs.GetInt("ah_q_autoeat", 0) == 1; } catch { return false; } } set { try { AHPrefs.SetInt("ah_q_autoeat", value ? 1 : 0); } catch { } } }

    static string BuffOf(string id) { return AHJson.S(AHJson.O(AHJson.O(AHDB.Items, id), "potion"), "buff"); }
    static readonly string[] BuffRank = { "titan", "raid", "dragon", "might", "ward", "iron", "swift" };
    static bool FitsSlot(string slot, ItemDef it)
    {
        if (it == null) return false;
        if (slot == "food") return it.IsFood && !it.potion;
        if (slot == "buff") return it.potion && BuffOf(it.id) != null;
        return Array.IndexOf(slot == "mana" ? AHPlayer.ManaOrder : AHPlayer.HealOrder, it.id) >= 0;
    }
    static int Score(string slot, ItemDef it)
    {
        if (slot == "food") return it.foodHunger * 3 + it.foodHp + (it.meal ? -1000 : 0);   // a quick bite, not a sit-down meal
        if (slot == "buff") { int r = Array.IndexOf(BuffRank, BuffOf(it.id)); return r < 0 ? 0 : 100 - r; }
        return 0;
    }
    // what the button would use now
    public static string QuickItem(AHPlayer p, string slot)
    {
        string pick = QuickPick(slot);
        if (!string.IsNullOrEmpty(pick) && p.bag.Count(pick) > 0) return pick;
        string best = null; int bs = int.MinValue;
        foreach (var kv in p.bag.inv)
        {
            if (kv.Value <= 0) continue; var it = AHItems.Get(kv.Key); if (!FitsSlot(slot, it)) continue;
            if (slot == "buff" && p.Elix(BuffOf(it.id))) continue;   // already running: the next best
            int sc = Score(slot, it); if (sc > bs) { bs = sc; best = kv.Key; }
        }
        return best;
    }
    public static string QuickFood(AHPlayer p) { return QuickItem(p, "food"); }
    static int SlotCount(AHPlayer p, string slot) { int n = 0; foreach (var kv in p.bag.inv) if (kv.Value > 0 && FitsSlot(slot, AHItems.Get(kv.Key))) n += kv.Value; return n; }

    void QuickEat() { QuickUse("food"); }
    void QuickUse(string slot)
    {
        var p = g.player; if (p == null) return;
        string id = QuickItem(p, slot);
        if (id == null)
        {
            if (slot == "food") Toast(AHLoc.T("No food in your bag. Inns and the market sell it, or cook at a campfire.", "لا طعام في حقيبتك. النُّزُل والسوق يبيعانه، أو اطبخ عند نار المخيم."), 3f);
            else Toast(AHLoc.T("No elixirs ready. Alchemists sell them, or brew your own.", "لا إكسير جاهز. الخيميائيون يبيعونها، أو اصنعها بنفسك."), 3f);
            return;
        }
        if (slot == "food") p.Eat(id); else p.Drink(id);
    }

    void BuildQuick(Color glassFace)
    {
        if (quickBtns.Count > 0) return;
        foreach (var def in new[] { new object[] { "food", "PotFood", new Vector2(-128f, 290f), new Color(1f, 0.78f, 0.45f) }, new object[] { "buff", "PotBuff", new Vector2(-303f, 210f), new Color(0.85f, 0.6f, 1f) } })
        {
            var q = new QuickBtn { slot = (string)def[0] };
            q.rt = Img((string)def[1], transform, ornDisc != null ? ornDisc : circle, new Vector2(1, 0), (Vector2)def[2], new Vector2(52, 52), glassFace);
            Img("Rim", q.rt, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(55, 55), (Color)def[3]);
            q.icon = Img("Icon", q.rt, white, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(34, 34), Color.white).GetComponent<Image>(); q.icon.preserveAspect = true;
            q.cool = Img("Cool", q.rt, circle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48, 48), new Color(0f, 0f, 0f, 0.55f)).GetComponent<Image>();
            q.cool.type = Image.Type.Filled; q.cool.fillMethod = Image.FillMethod.Radial360; q.cool.fillOrigin = (int)Image.Origin360.Top; q.cool.fillClockwise = false; q.cool.fillAmount = 0f;
            q.n = Label(q.rt, "N", "0", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(30, 18), Color.white); q.n.fontStyle = FontStyle.Bold; Shade(q.n);
            q.n.rectTransform.anchorMin = q.n.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); q.n.rectTransform.anchoredPosition = new Vector2(14, -14);
            string s = q.slot; taps.Add(new TapBtn { rt = q.rt, act = () => QuickUse(s) });
            quickBtns.Add(q);
        }
        foreach (var slot in new[] { "food", "buff", "heal", "mana" })
        {
            var host = slot == "food" ? FindUI("PotFood") : slot == "buff" ? FindUI("PotBuff") : FindUI(slot == "heal" ? "PotHeal" : "PotMana"); if (host == null) continue;
            var b = Img("Pick", host, circle, new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(20, 20), new Color(0.12f, 0.2f, 0.34f, 0.95f));
            Img("R", b, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20), new Color(0.6f, 0.85f, 1f, 0.9f));
            Center(Label(b, "T", "▾", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(20, 18), Color.white));
            string s = slot;
            taps.Insert(0, new TapBtn { rt = b, act = () => OpenQuick(s) });   // first in the list: wins over the button under it
        }
    }

    void QuickTick()
    {
        var p = g.player; if (p == null || quickBtns.Count == 0) return;
        // eating by yourself when hungry (only from the food button, never a sit-down meal you did not pick)
        if (AutoEat && !p.dead && p.hunger < 20f && Modal == 0 && (autoEatT -= Time.deltaTime) <= 0f)
        {
            autoEatT = 4f; string id = QuickItem(p, "food");
            if (id != null && (QuickPick("food") == id || !AHItems.Get(id).meal)) p.Eat(id);
        }
        foreach (var q in quickBtns)
        {
            string id = QuickItem(p, q.slot);
            if (q.slot == "buff")
            {
                // the ring empties as the running elixir wears off
                float left = 0f, tot = 1f; string pb = id != null ? BuffOf(id) : BuffOf(QuickPick("buff"));
                if (pb == null) foreach (var b in BuffRank) if (p.Elix(b)) { pb = b; break; }
                float t; if (pb != null && p.elix.TryGetValue(pb, out t)) { left = Mathf.Max(0f, t - Time.time); tot = pb == "titan" || pb == "raid" ? 600f : pb == "dragon" ? 300f : 180f; }
                q.cool.fillAmount = Mathf.Clamp01(left / tot) * 0.85f;
            }
            string key = (id ?? "") + "|" + SlotCount(p, q.slot) + "|" + (id != null ? p.bag.Count(id) : 0);
            if (key == q.shown) continue; q.shown = key;
            var it = id != null ? AHItems.Get(id) : AHItems.Get(QuickPick(q.slot));
            var sp = it != null ? AHItemIcons.Best(it) : null;
            if (sp == null) sp = q.slot == "food" ? HubIcon("bread") : CGIcon("potion");
            q.icon.sprite = sp; q.icon.color = id != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            q.n.text = id != null ? p.bag.Count(id).ToString() : "0";
        }
    }

    // ---------- choosing what goes in a quick button ----------
    public void OpenQuick(string slot) { quickSlot = slot; wkMode = "quick"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderQuick(AHPlayer p)
    {
        string[] names = { "food", "buff", "heal", "mana" };
        string[] en = { "Food", "Elixir", "Health", "Mana" }, ar = { "الطعام", "الإكسير", "الصحة", "المانا" };
        int si = Array.IndexOf(names, quickSlot); if (si < 0) si = 0;
        wkTitle.text = AHLoc.T("Quick button · ", "زر سريع · ") + AHLoc.T(en[si], ar[si]);
        wkHint.text = AHLoc.T("Pick what this button uses.", "اختر ما يستخدمه هذا الزر.");
        for (int i = 0; i < names.Length; i++)
        {
            string id = names[i]; bool on = i == si;
            var b = Img("Tab", wkPanel, white, new Vector2(0f, 1f), new Vector2(WkW - 36f - (names.Length - 1 - i) * 118f - 56f, -70f), new Vector2(112, 36), on ? new Color(0.32f, 0.24f, 0.14f, 1f) : new Color(0.22f, 0.16f, 0.11f, 1f));
            wkItems.Add(b.gameObject);
            Center(Label(b, "T", AHLoc.T(en[i], ar[i]), 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(112, 30), on ? CGGold : Color.white));
            taps.Add(new TapBtn { rt = b, layer = 5, group = "work", act = () => OpenQuick(id) });
        }
        var rows = new List<ItemDef>();
        foreach (var kv in p.bag.inv) { var it = AHItems.Get(kv.Key); if (kv.Value > 0 && FitsSlot(quickSlot, it)) rows.Add(it); }
        // potions you have none of still show, so you can set the button before you buy them
        if (quickSlot == "heal" || quickSlot == "mana") foreach (var k in quickSlot == "mana" ? AHPlayer.ManaOrder : AHPlayer.HealOrder) { var it = AHItems.Get(k); if (it != null && !rows.Contains(it)) rows.Add(it); }
        rows.Sort((a, b) => Score(quickSlot, b).CompareTo(Score(quickSlot, a)));
        string pick = QuickPick(quickSlot);
        var lines = new List<Action<int>>();
        if (quickSlot == "food")
            lines.Add(r =>
            {
                bool on = AutoEat; rowIcon = CGIcon("potion");
                Row(r, AHLoc.T("Eat by myself when hungry", "كُل تلقائيًا عند الجوع"), on ? CGGold : Color.white,
                    AHLoc.T("Below 20% food your hero eats from this button", "تحت ٢٠٪ شبع يأكل بطلك من هذا الزر"), "",
                    new WkBtn { label = on ? AHLoc.T("On", "مفعّل") : AHLoc.T("Off", "متوقف"), on = true, col = on ? Go : Plain, act = () => { AutoEat = !AutoEat; RenderWork(); } });
            });
        lines.Add(r =>
        {
            bool on = string.IsNullOrEmpty(pick); rowIcon = CGIcon("star");
            Row(r, AHLoc.T("Best I carry", "الأفضل مما أحمل"), on ? CGGold : Color.white, AHLoc.T("Uses your best one each time, and steps in when your pick runs out", "يستخدم أفضل ما لديك، ويحل محل اختيارك عندما ينفد"), "",
                new WkBtn { label = on ? AHLoc.T("In use", "مستخدم") : AHLoc.T("Use this", "استخدم هذا"), on = !on, col = Go, act = () => { SetQuickPick(quickSlot, ""); ResetQuickShown(); RenderWork(); } });
        });
        foreach (var it0 in rows)
        {
            var it = it0;
            lines.Add(r =>
            {
                bool sel = pick == it.id; int have = p.bag.Count(it.id);
                string l2 = quickSlot == "food" ? "+" + it.foodHp + " HP · +" + it.foodHunger + AHLoc.T(" food", " شبع") + (it.meal ? AHLoc.T(" · a sit-down meal", " · وجبة كاملة") : "")
                          : quickSlot == "buff" ? AHPlayer.ElixText(BuffOf(it.id)) : AHLoc.T("You have ", "لديك ") + have;
                string l3 = quickSlot == "food" || quickSlot == "buff" ? AHLoc.T("You have ", "لديك ") + have : "";
                RowI(it, r, it.name, sel ? CGGold : AHItems.Quality(it), l2, l3,
                    new WkBtn { label = sel ? AHLoc.T("In the button", "في الزر") : AHLoc.T("Put in button", "ضعه في الزر"), on = !sel, col = Go, act = () => { SetQuickPick(quickSlot, it.id); ResetQuickShown(); RenderWork(); } });
            });
        }
        int from = Paged(lines.Count);
        for (int r = 0; r < RowsPerPage && from + r < lines.Count; r++) lines[from + r](r);
        if (rows.Count == 0 && quickSlot == "food") wkHint.text = AHLoc.T("You carry no food yet. Inns and the market sell it.", "لا تحمل طعامًا بعد. النُّزُل والسوق يبيعانه.");
        if (rows.Count == 0 && quickSlot == "buff") wkHint.text = AHLoc.T("You carry no elixirs yet. Alchemists sell them.", "لا تحمل إكسيرًا بعد. الخيميائيون يبيعونه.");
    }
    void ResetQuickShown() { foreach (var q in quickBtns) q.shown = null; }
}
