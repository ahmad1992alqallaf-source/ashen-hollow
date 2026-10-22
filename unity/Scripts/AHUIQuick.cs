using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// the quick buttons by the fight buttons: food, health potion, mana potion, each holding the thing you picked for it
// (tap: use it; the small ▾ on its corner: choose what goes in it; nothing picked, or run out: the best one you carry)
public partial class AHUI
{
    RectTransform qFood; Image qFoodIcon; Text qFoodN; string qFoodShown;
    string quickSlot = "food";

    public static string QuickPick(string slot) { try { return AHPrefs.GetString("ah_q_" + slot, ""); } catch { return ""; } }
    static void SetQuickPick(string slot, string id) { try { AHPrefs.SetString("ah_q_" + slot, id ?? ""); } catch { } }

    static bool FitsSlot(string slot, ItemDef it)
    {
        if (it == null) return false;
        if (slot == "food") return it.IsFood && !it.potion;
        return Array.IndexOf(slot == "mana" ? AHPlayer.ManaOrder : AHPlayer.HealOrder, it.id) >= 0;
    }
    // the food this button would eat now
    public static string QuickFood(AHPlayer p)
    {
        string pick = QuickPick("food");
        if (!string.IsNullOrEmpty(pick) && p.bag.Count(pick) > 0) return pick;
        string best = null; int bs = -1;
        foreach (var kv in p.bag.inv)
        {
            if (kv.Value <= 0) continue; var it = AHItems.Get(kv.Key); if (!FitsSlot("food", it)) continue;
            int sc = it.foodHunger * 3 + it.foodHp + (it.meal ? -1000 : 0);   // a quick bite, not a sit-down meal
            if (sc > bs) { bs = sc; best = kv.Key; }
        }
        return best;
    }
    public static int FoodCount(AHPlayer p) { int n = 0; foreach (var kv in p.bag.inv) if (kv.Value > 0 && FitsSlot("food", AHItems.Get(kv.Key))) n += kv.Value; return n; }

    void QuickEat()
    {
        var p = g.player; if (p == null) return;
        string id = QuickFood(p);
        if (id == null) { Toast(AHLoc.T("No food in your bag. Inns and the market sell it, or cook at a campfire.", "لا طعام في حقيبتك. النُّزُل والسوق يبيعانه، أو اطبخ عند نار المخيم."), 3f); return; }
        p.Eat(id);
    }

    void BuildQuick(Color glassFace)
    {
        if (qFood != null) return;
        qFood = Img("PotFood", transform, ornDisc != null ? ornDisc : circle, new Vector2(1, 0), new Vector2(-128f, 290f), new Vector2(52, 52), glassFace);
        Img("Rim", qFood, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(55, 55), new Color(1f, 0.78f, 0.45f, 1f));
        qFoodIcon = Img("Icon", qFood, white, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(34, 34), Color.white).GetComponent<Image>(); qFoodIcon.preserveAspect = true;
        qFoodN = Label(qFood, "N", "0", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(30, 18), Color.white); qFoodN.fontStyle = FontStyle.Bold; Shade(qFoodN);
        qFoodN.rectTransform.anchorMin = qFoodN.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); qFoodN.rectTransform.anchoredPosition = new Vector2(14, -14);
        taps.Add(new TapBtn { rt = qFood, act = QuickEat });
        foreach (var slot in new[] { "food", "heal", "mana" })
        {
            var host = slot == "food" ? qFood : FindUI(slot == "heal" ? "PotHeal" : "PotMana"); if (host == null) continue;
            var b = Img("Pick", host, circle, new Vector2(0f, 1f), new Vector2(2f, -2f), new Vector2(20, 20), new Color(0.12f, 0.2f, 0.34f, 0.95f));
            Img("R", b, ring, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20), new Color(0.6f, 0.85f, 1f, 0.9f));
            Center(Label(b, "T", "▾", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(20, 18), Color.white));
            string s = slot;
            taps.Insert(0, new TapBtn { rt = b, act = () => OpenQuick(s) });   // first in the list: wins over the button under it
        }
    }

    void QuickTick()
    {
        var p = g.player; if (p == null || qFood == null) return;
        string id = QuickFood(p);
        string key = (id ?? "") + "|" + FoodCount(p);
        if (key == qFoodShown) return; qFoodShown = key;
        var it = id != null ? AHItems.Get(id) : AHItems.Get(QuickPick("food"));
        var sp = it != null ? AHItemIcons.Best(it) : null;
        if (sp == null) sp = HubIcon("bread") ?? CGIcon("bag");
        qFoodIcon.sprite = sp; qFoodIcon.color = id != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        qFoodN.text = id != null ? p.bag.Count(id).ToString() : "0";
    }

    // ---------- choosing what goes in a quick button ----------
    public void OpenQuick(string slot) { quickSlot = slot; wkMode = "quick"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderQuick(AHPlayer p)
    {
        string[] names = { "food", "heal", "mana" };
        string[] en = { "Food", "Health potion", "Mana potion" }, ar = { "الطعام", "جرعة الصحة", "جرعة المانا" };
        int si = Array.IndexOf(names, quickSlot); if (si < 0) si = 0;
        wkTitle.text = AHLoc.T("Quick button · ", "زر سريع · ") + AHLoc.T(en[si], ar[si]);
        wkHint.text = AHLoc.T("Pick what this button uses.", "اختر ما يستخدمه هذا الزر.");
        // the three buttons as tabs
        for (int i = 0; i < 3; i++)
        {
            string id = names[i]; bool on = i == si;
            var b = Img("Tab", wkPanel, white, new Vector2(0f, 1f), new Vector2(WkW - 36f - (2 - i) * 150f - 70f, -70f), new Vector2(142, 36), on ? new Color(0.32f, 0.24f, 0.14f, 1f) : new Color(0.22f, 0.16f, 0.11f, 1f));
            wkItems.Add(b.gameObject);
            Center(Label(b, "T", AHLoc.T(en[i], ar[i]), 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(142, 30), on ? CGGold : Color.white));
            taps.Add(new TapBtn { rt = b, layer = 5, group = "work", act = () => OpenQuick(id) });
        }
        var rows = new List<ItemDef>();
        foreach (var kv in p.bag.inv) { var it = AHItems.Get(kv.Key); if (kv.Value > 0 && FitsSlot(quickSlot, it)) rows.Add(it); }
        // potions you have none of still show, so you can set the button before you buy them
        if (quickSlot != "food") foreach (var k in quickSlot == "mana" ? AHPlayer.ManaOrder : AHPlayer.HealOrder) { var it = AHItems.Get(k); if (it != null && !rows.Contains(it)) rows.Add(it); }
        rows.Sort((a, b) => quickSlot == "food" ? (b.foodHunger * 3 + b.foodHp).CompareTo(a.foodHunger * 3 + a.foodHp) : 0);
        string pick = QuickPick(quickSlot);
        int count = rows.Count + 1, from = Paged(count);
        for (int r = 0; r < RowsPerPage && from + r < count; r++)
        {
            int k = from + r;
            if (k == 0)
            {
                bool on = string.IsNullOrEmpty(pick);
                rowIcon = CGIcon("star");
                Row(r, AHLoc.T("Best I carry", "الأفضل مما أحمل"), on ? CGGold : Color.white, AHLoc.T("Uses your best one each time, and steps in when your pick runs out", "يستخدم أفضل ما لديك، ويحل محل اختيارك عندما ينفد"), "",
                    new WkBtn { label = on ? AHLoc.T("In use", "مستخدم") : AHLoc.T("Use this", "استخدم هذا"), on = !on, col = Go, act = () => { SetQuickPick(quickSlot, ""); qFoodShown = null; RenderWork(); } });
                continue;
            }
            var it = rows[k - 1]; bool sel = pick == it.id; int have = p.bag.Count(it.id);
            string l2 = quickSlot == "food" ? "+" + it.foodHp + " HP · +" + it.foodHunger + AHLoc.T(" food", " شبع") + (it.meal ? AHLoc.T(" · a sit-down meal", " · وجبة كاملة") : "")
                                            : AHLoc.T("You have ", "لديك ") + have;
            string l3 = quickSlot == "food" ? AHLoc.T("You have ", "لديك ") + have : "";
            RowI(it, r, it.name, sel ? CGGold : AHItems.Quality(it), l2, l3,
                new WkBtn { label = sel ? AHLoc.T("In the button", "في الزر") : AHLoc.T("Put in button", "ضعه في الزر"), on = !sel, col = Go, act = () => { SetQuickPick(quickSlot, it.id); qFoodShown = null; RenderWork(); } });
        }
        if (rows.Count == 0 && quickSlot == "food") wkHint.text = AHLoc.T("You carry no food yet. Inns and the market sell it, or cook at a campfire.", "لا تحمل طعامًا بعد. النُّزُل والسوق يبيعانه، أو اطبخ عند نار المخيم.");
    }
}
