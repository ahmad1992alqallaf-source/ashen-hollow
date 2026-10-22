// Ashen Hollow: the loot window (World of Warcraft style). A body that still holds something glows; stand by it and
// the big seal says LOOT. Tap it and a window opens with the money and each thing it dropped, coloured by quality:
// take them one by one, or all at once. What does not fit in the bag stays on the body until it fades.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    AHMob lootMob;
    RectTransform lootAllBtn;

    // a big Take all under the right thumb (where the attack seal sits), while the loot window is open
    void LootAllTick()
    {
        bool on = false;   // the loot window's own Take all is enough (this one sat loose over the window's edge)
        if (lootAllBtn == null)
        {
            if (!on) return;
            lootAllBtn = Img("LootTakeAll", transform, cgBtnGo != null ? cgBtnGo : white, new Vector2(1, 0), new Vector2(-150f, 92f), new Vector2(230, 84), Color.white);
            lootAllBtn.GetComponent<Image>().type = Image.Type.Sliced;
            var ic = Img("Icon", lootAllBtn, CGIcon("loot"), new Vector2(0f, 0.5f), new Vector2(40, 0), new Vector2(34, 34), Color.white); ic.GetComponent<Image>().preserveAspect = true;
            var t = Center(Label(lootAllBtn, "T", "Take all", 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(160, 50), Color.white)); t.fontStyle = FontStyle.Bold; t.rectTransform.anchoredPosition = new Vector2(22, 0);
            taps.Insert(0, new TapBtn { rt = lootAllBtn, layer = 5, act = () => { var m = lootMob; if (m == null || !m.HasLoot) return; AHLoot.TakeAll(g, g.player, m); AfterLoot(); } });
        }
        if (lootAllBtn.gameObject.activeSelf != on) lootAllBtn.gameObject.SetActive(on);
        if (on) lootAllBtn.SetAsLastSibling();
    }

    // the nearest body with loot on it, close enough to open
    public AHMob LootTarget(AHPlayer p)
    {
        if (p == null || p.dead) return null;
        AHMob best = null; float bd = 3.2f;
        foreach (var m in g.mobs)
        {
            if (m == null || !m.HasLoot) continue;
            Vector3 d = m.transform.position - p.transform.position; d.y = 0f;
            if (d.magnitude < bd) { bd = d.magnitude; best = m; }
        }
        return best;
    }

    public void OpenLoot(AHMob m)
    {
        if (m == null) return;
        lootMob = m; wkMode = "loot"; wkPageI = 0;
        AHSound.Play("loot");
        ShowWork(true); RenderWork();
    }

    void RenderLoot(AHPlayer p)
    {
        var m = lootMob;
        if (m == null || !m.HasLoot) { ShowWork(false); return; }
        wkTitle.text = "Loot · " + m.type.name;
        wkHint.text = "Tap Take for one thing, or Take all. What does not fit in your bag stays on the body.";
        var rows = new List<System.Action<int>>();
        rows.Add(s => Row(s, "Take everything", new Color(1f, 0.85f, 0.45f), m.loot.Count + (m.loot.Count == 1 ? " thing" : " things") + (m.lootMoney > 0 ? " and " + AHItems.MoneyText(m.lootMoney) : ""), "",
            new WkBtn { label = "Take all", on = true, col = Go, act = () => { AHLoot.TakeAll(g, p, m); AfterLoot(); } }));
        if (m.lootMoney > 0)
            rows.Add(s => Row(s, AHItems.MoneyText(m.lootMoney), new Color(1f, 0.84f, 0.35f), "Coins", "",
                new WkBtn { label = "Take", on = true, col = Plain, act = () => { AHLoot.TakeMoney(g, p, m); AfterLoot(); } }));
        for (int i = 0; i < m.loot.Count; i++)
        {
            int ii = i; var d = AHItems.Get(m.loot[i]); if (d == null) continue;
            string q = AHItems.QualityName(d); q = char.ToUpperInvariant(q[0]) + q.Substring(1);
            string l2 = d.IsGear ? AHItems.StatLine(d) + " · " + AHItems.WhoUses(d.id) : (d.note ?? q);
            string l3 = d.IsGear && !d.cosmetic ? (AHItems.CanUse(d.id, p.cls.id) ? Compare(p, d) : "<color=#ff8a7a>✕ your class can't wear it</color> · " + q) : (l2 == q ? "" : q);
            rows.Add(s => { rowItem = d; Row(s, d.name, AHItems.Quality(d), l2, l3, new WkBtn { label = "Take", on = true, col = Plain, act = () => { AHLoot.TakeOne(g, p, m, ii); AfterLoot(); } }); });
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
    void AfterLoot()
    {
        if (lootMob == null || !lootMob.HasLoot) { ShowWork(false); return; }
        RenderWork();
    }
}
