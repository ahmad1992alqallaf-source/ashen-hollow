// Ashen Hollow: a window of its own for a piece of gear (from the bag, a shop, the auction house, the market or the
// bank: tap the item's picture). It shows the piece turning in the light, or your hero wearing it ("Try it on"),
// beside its stats and how it compares with what you wear. And when you pick up or buy something better than what
// you wear, a little card pops up to put it on at once.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    RectTransform insRoot, insPanel, insView; RawImage insRaw; Image insGlow; Text insName, insSlot, insText, insHint;
    ItemDef insItem; bool insOn; Action insBuy; string insBuyLabel;
    readonly List<GameObject> insBtns = new List<GameObject>();
    public bool InspectOpen { get { return insRoot != null && insRoot.gameObject.activeSelf; } }

    void BuildInspect()
    {
        insRoot = Box("Inspect", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        insRoot.anchorMin = Vector2.zero; insRoot.anchorMax = Vector2.one; insRoot.offsetMin = insRoot.offsetMax = Vector2.zero;
        var dim = Img("Dim", insRoot, white, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.6f));
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.offsetMin = dim.offsetMax = Vector2.zero;
        Img("Back", insRoot, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(764, 484), new Color(0.085f, 0.065f, 0.05f, 1f));
        insPanel = Img("InspectPanel", insRoot, panel9 != null ? panel9 : white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 500), panel9 != null ? Color.white : new Color(0.09f, 0.07f, 0.05f, 1f));
        if (panel9 != null) insPanel.GetComponent<Image>().type = Image.Type.Sliced;
        // the stage on the left
        var stage = Img("Stage", insPanel, white, new Vector2(0f, 0.5f), new Vector2(190, 10), new Vector2(320, 430), new Color(0.07f, 0.055f, 0.045f, 1f));
        insGlow = Img("Glow", stage, glow != null ? glow : white, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(330, 330), new Color(1f, 1f, 1f, 0.4f)).GetComponent<Image>();
        insView = Box("View", stage, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(300, 420));
        insRaw = insView.gameObject.AddComponent<RawImage>(); insRaw.raycastTarget = false;
        insHint = Center(Label(stage, "Hint", "", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(320, 20), new Color(1f, 1f, 1f, 0.5f)));
        insHint.rectTransform.anchoredPosition = new Vector2(0, -200);
        // the words on the right
        insName = Label(insPanel, "Name", "", 26, TextAnchor.UpperLeft, new Vector2(372, -34), new Vector2(380, 34), Gold); insName.fontStyle = FontStyle.Bold;
        insName.horizontalOverflow = HorizontalWrapMode.Wrap;
        insSlot = Label(insPanel, "Slot", "", 16, TextAnchor.UpperLeft, new Vector2(372, -72), new Vector2(380, 22), new Color(1f, 1f, 1f, 0.65f));
        insText = Label(insPanel, "Text", "", 17, TextAnchor.UpperLeft, new Vector2(372, -104), new Vector2(380, 250), new Color(1f, 0.92f, 0.8f));
        insText.horizontalOverflow = HorizontalWrapMode.Wrap; insText.supportRichText = true;
        var close = Img("Close", insPanel, circle, new Vector2(1f, 1f), new Vector2(-34, -34), new Vector2(46, 46), new Color(0.7f, 0.15f, 0.12f, 1f));
        Center(Label(close, "X", "X", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(46, 30), Color.white)).fontStyle = FontStyle.Bold;
        taps.Add(new TapBtn { rt = close, layer = 7, act = CloseInspect });
        insRoot.gameObject.SetActive(false);
    }

    // the window for a piece of gear; buy (optional) adds a button to buy it right here
    public void OpenInspect(ItemDef d, Action buy = null, string buyLabel = null)
    {
        if (d == null || !d.IsGear) return;
        if (insRoot == null) BuildInspect();
        insItem = d; insOn = false; insBuy = buy; insBuyLabel = buyLabel;
        insRoot.gameObject.SetActive(true); insRoot.SetAsLastSibling();
        ptrs.Clear();
        RenderInspect();
    }
    void CloseInspect()
    {
        if (insRoot != null) insRoot.gameObject.SetActive(false);
        AHItemStudio.StopHero(); ptrs.Clear(); RemoveTaps("ins");
        if (BagOpen) RefreshBag();
        if (WorkOpen && wkMode == "stats") RenderWork();
    }

    void InsBtn(string label, Vector2 pos, float w, Color c, Action act)
    {
        var b = Img("Btn", insPanel, tag9 != null ? tag9 : white, new Vector2(0f, 0f), pos, new Vector2(w, 52), c);
        if (tag9 != null) b.GetComponent<Image>().type = Image.Type.Sliced;
        Center(Label(b, "T", label, 19, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(w, 34), Color.white)).fontStyle = FontStyle.Bold;
        insBtns.Add(b.gameObject);
        taps.Add(new TapBtn { rt = b, layer = 7, group = "ins", act = act });
    }

    void RenderInspect()
    {
        foreach (var go in insBtns) Destroy(go); insBtns.Clear(); RemoveTaps("ins");
        var d = insItem; var p = g.player; var bag = p.bag;
        var q = AHItems.Quality(d);
        insName.text = d.name; insName.color = Color.Lerp(q, Color.white, 0.15f);
        insSlot.text = AHItems.SlotName(d.slot) + " · " + AHItems.WhoUses(d.id) + (d.cosmetic ? " · cosmetic" : "");
        var worn = AHItems.Get(bag.Worn(d.slot)); bool wearing = bag.Worn(d.slot) == d.id;
        string t = AHItems.StatLine(d) + SetLine(d, bag);
        if (!d.cosmetic) t += "\n\n" + (wearing ? "<color=#ffd86a>You are wearing this.</color>" : "Against " + (worn != null ? worn.name : "nothing worn") + ":\n" + Compare(d, worn));
        if (!AHItems.CanUse(d.id, p.cls.id)) t += "\n\n<color=#ff8a7a>Your class can’t use this.</color>";
        if (!string.IsNullOrEmpty(d.note)) t += "\n\n<i>" + d.note + "</i>";
        int have = bag.Count(d.id); if (have > 0 && !wearing) t += "\n\nIn your bag: " + have;
        insText.text = t;
        insGlow.color = new Color(q.r, q.g, q.b, 0.5f);
        // the picture: the piece, or your hero wearing it
        Texture tex;
        if (insOn) { string id = d.id; tex = AHItemStudio.HeroOf(p.cls, p.look, s => s == d.slot ? id : AHWardrobe.Shown(p, s)); insView.sizeDelta = new Vector2(300, 420); }
        else { AHItemStudio.StopHero(); tex = AHItemStudio.View(d); insView.sizeDelta = new Vector2(300, 300); }
        insRaw.texture = tex; insRaw.enabled = tex != null;
        insHint.text = insOn ? "Only a look: nothing is changed until you equip it." : tex == null ? "Getting the picture ready..." : "";
        // the buttons along the bottom
        float x = 372f, y = 30f;
        bool canWear = AHItems.CanUse(d.id, p.cls.id) && !d.cosmetic;
        InsBtn(insOn ? "Show the item" : "Try it on", new Vector2(x + 90, y + 26), 180, new Color(0.45f, 0.75f, 1f), () => { insOn = !insOn; RenderInspect(); });
        if (have > 0 && canWear && !wearing)
            InsBtn("Equip", new Vector2(x + 280, y + 26), 180, new Color(0.55f, 1f, 0.6f), () => { string msg = bag.Equip(d.id, p.cls.id); if (msg != null) Toast(msg); else { AHSound.Play("level"); Toast("Equipped " + d.name + "."); } RenderInspect(); });
        else if (insBuy != null)
            InsBtn(insBuyLabel ?? "Buy", new Vector2(x + 280, y + 26), 180, new Color(1f, 0.85f, 0.45f), () => { insBuy(); RenderInspect(); });
    }

    void InspectTick()
    {
        if (!InspectOpen) return;
        // the studio may still be making the picture
        if (!insRaw.enabled) { var tex = insOn ? null : AHItemStudio.View(insItem); if (tex != null) { insRaw.texture = tex; insRaw.enabled = true; insHint.text = ""; } }
        if (AHInput.BackKey()) CloseInspect();
    }

    // ---------- better gear: a card to put it on at once ----------
    RectTransform upRoot; Text upName, upLine; Image upIcon, upGlyph; float upT; string upId;
    readonly HashSet<string> upSeen = new HashSet<string>(); bool upReady; float upCheck;
    readonly Queue<string> upQueue = new Queue<string>();

    public static float GearScore(ItemDef d) { return d == null ? 0f : d.atk + d.def + d.hp * 0.25f + (d.dmg + d.cdr + d.heal) * 120f; }

    void BuildUpgrade()
    {
        upRoot = Img("Upgrade", transform, white, new Vector2(0.5f, 0f), new Vector2(0, 330), new Vector2(472, 104), new Color(0.085f, 0.065f, 0.05f, 0.97f));
        if (panel9 != null) { var fr = Img("Frame", upRoot, panel9, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, 112), Color.white); fr.GetComponent<Image>().type = Image.Type.Sliced; }
        var ic = Img("Icon", upRoot, circle, new Vector2(0f, 0.5f), new Vector2(56, 0), new Vector2(76, 76), new Color(0.2f, 0.15f, 0.11f, 1f)); upIcon = ic.GetComponent<Image>();
        upGlyph = Img("Glyph", ic, white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70), Color.white).GetComponent<Image>(); upGlyph.preserveAspect = true;
        var t = Label(upRoot, "Title", "Better gear!", 15, TextAnchor.UpperLeft, new Vector2(104, -12), new Vector2(240, 20), new Color(0.56f, 0.95f, 0.55f)); t.fontStyle = FontStyle.Bold;
        upName = Label(upRoot, "Name", "", 19, TextAnchor.UpperLeft, new Vector2(104, -32), new Vector2(240, 26), Gold); upName.fontStyle = FontStyle.Bold;
        upLine = Label(upRoot, "Line", "", 14, TextAnchor.UpperLeft, new Vector2(104, -60), new Vector2(240, 44), Color.white);
        upLine.horizontalOverflow = HorizontalWrapMode.Wrap; upLine.supportRichText = true;
        var eq = Img("Equip", upRoot, tag9 != null ? tag9 : white, new Vector2(1f, 0.5f), new Vector2(-70, 22), new Vector2(116, 44), new Color(0.55f, 1f, 0.6f));
        if (tag9 != null) eq.GetComponent<Image>().type = Image.Type.Sliced;
        Center(Label(eq, "T", "Equip", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(116, 30), Color.white)).fontStyle = FontStyle.Bold;
        var no = Img("Later", upRoot, tag9 != null ? tag9 : white, new Vector2(1f, 0.5f), new Vector2(-70, -26), new Vector2(116, 38), new Color(0.75f, 0.68f, 0.6f));
        if (tag9 != null) no.GetComponent<Image>().type = Image.Type.Sliced;
        Center(Label(no, "T", "Not now", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(116, 28), Color.white));
        taps.Add(new TapBtn { rt = eq, act = () => { var p = g.player; if (upId != null && p.bag.Count(upId) > 0) { string msg = p.bag.Equip(upId, p.cls.id); if (msg != null) Toast(msg); else { AHSound.Play("level"); Toast("Equipped " + AHItems.Get(upId).name + "."); } } HideUpgrade(); } });
        taps.Add(new TapBtn { rt = no, act = HideUpgrade });
        taps.Add(new TapBtn { rt = ic, act = () => { var d = AHItems.Get(upId); HideUpgrade(); OpenInspect(d); } });
        upRoot.gameObject.SetActive(false);
    }
    void HideUpgrade() { if (upRoot != null) upRoot.gameObject.SetActive(false); upId = null; upT = 0f; }

    // is this piece better than what you wear in its slot (and can you wear it)?
    bool IsUpgrade(string id)
    {
        var p = g.player; var d = AHItems.Get(id);
        if (d == null || !d.IsGear || d.cosmetic || !AHItems.CanUse(id, p.cls.id)) return false;
        var w = AHItems.Get(p.bag.Worn(d.slot));
        if (w != null && w.id == id) return false;
        return GearScore(d) > GearScore(w) + 0.5f;
    }

    void UpgradeTick(float dt)
    {
        var p = g.player; if (p == null || p.bag == null) return;
        upCheck -= dt; if (upCheck > 0f) return; upCheck = 0.5f;
        // what is in the bag now; anything new is looked at once
        foreach (var id in p.bag.order)
        {
            if (upSeen.Contains(id)) continue;
            upSeen.Add(id);
            if (upReady && IsUpgrade(id)) upQueue.Enqueue(id);
        }
        upReady = true;   // the bag as it was when the land loaded counts as seen
        if (upRoot != null && upRoot.gameObject.activeSelf)
        {
            upT -= 0.5f;
            if (upT <= 0f || upId == null || p.bag.Count(upId) <= 0 || !IsUpgrade(upId)) HideUpgrade();
            return;
        }
        if (Modal != 0) return;   // shown over the world, not over windows
        while (upQueue.Count > 0)
        {
            string id = upQueue.Dequeue();
            if (p.bag.Count(id) <= 0 || !IsUpgrade(id)) continue;
            if (upRoot == null) BuildUpgrade();
            var d = AHItems.Get(id); upId = id; upT = 15f;
            upName.text = d.name; upName.color = Color.Lerp(AHItems.Quality(d), Color.white, 0.2f);
            upLine.text = Compare(d, AHItems.Get(p.bag.Worn(d.slot)));
            var gs = AHItemIcons.Best(d); upGlyph.sprite = gs; upGlyph.enabled = gs != null;
            upRoot.gameObject.SetActive(true); upRoot.SetAsLastSibling();
            AHSound.Play("coin");
            break;
        }
    }
}
