// Ashen Hollow: the tester windows. "Send feedback" (a note, a picture of the screen, the device and the errors, sent
// through the phone's share sheet) and "What's new" (Resources/AH/Data/whatsnew.txt, shown once after each update and
// any time from Settings). Both are opened from Settings; the version button there also switches developer mode.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AHUI
{
    // ---------- Send feedback ----------
    int fbKind; string fbNote = ""; byte[] fbPng; bool fbPic = true, fbTyping; TouchScreenKeyboard fbKb;
    public bool FeedbackTyping { get { return fbTyping && WorkOpen && wkMode == "feedback"; } }

    // the window closes for a moment so the picture shows the game, not the window
    public void OpenFeedback() { ShowWork(false); StartCoroutine(FeedbackShot()); }
    IEnumerator FeedbackShot()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        fbPng = null;
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        if (tex != null)
        {
            // a phone-sized picture: about 1280 wide is plenty to see what went wrong
            if (tex.width > 1400)
            {
                int w = 1280, h = Mathf.RoundToInt(tex.height * 1280f / tex.width);
                var rt = RenderTexture.GetTemporary(w, h); Graphics.Blit(tex, rt);
                var small = new Texture2D(w, h, TextureFormat.RGB24, false); var was = RenderTexture.active;
                RenderTexture.active = rt; small.ReadPixels(new Rect(0, 0, w, h), 0, 0); small.Apply(); RenderTexture.active = was;
                RenderTexture.ReleaseTemporary(rt); Destroy(tex); tex = small;
            }
            fbPng = tex.EncodeToPNG(); Destroy(tex);
        }
        fbTyping = false; fbKb = null; fbPic = fbPng != null;
        wkMode = "feedback"; wkPageI = 0; ShowWork(true); RenderWork();
    }

    void StartFeedbackTyping()
    {
        fbTyping = true; AHInput.TypedChars();
        if (TouchScreenKeyboard.isSupported) fbKb = TouchScreenKeyboard.Open(fbNote, TouchScreenKeyboardType.Default, false, true, false, false, "What happened? What would you change?", 600);
        RenderWork();
    }

    void FeedbackTick()
    {
        if (!fbTyping) return;
        if (!WorkOpen || wkMode != "feedback") { fbTyping = false; if (fbKb != null) fbKb.active = false; fbKb = null; return; }
        string was = fbNote;
        if (fbKb != null)
        {
            fbNote = fbKb.text ?? "";
            if (fbKb.status != TouchScreenKeyboard.Status.Visible) { fbTyping = false; fbKb = null; RenderWork(); return; }
        }
        else
            foreach (char c in AHInput.TypedChars())
            {
                if (c == '\b') { if (fbNote.Length > 0) fbNote = fbNote.Substring(0, fbNote.Length - 1); }
                else if (c == '\n' || c == '\r') { fbTyping = false; RenderWork(); return; }
                else if (!char.IsControl(c) && fbNote.Length < 600) fbNote += c;
            }
        if (fbNote != was) RenderWork();
    }

    void RenderFeedback(AHPlayer p)
    {
        wkTitle.text = "Send feedback";
        wkHint.text = "Thank you for testing! Tell us what broke, what felt wrong or what you would love to see. Your note goes with a picture of your screen and the game’s details.";
        Color cT = new Color(1f, 0.85f, 0.5f), cI = new Color(0.7f, 0.9f, 1f);
        var rows = new List<System.Action<int>>();
        var kinds = new WkBtn[AHFeedback.Kinds.Length];
        for (int i = 0; i < kinds.Length; i++) { int k = i; kinds[i] = new WkBtn { label = AHFeedback.Kinds[k], on = true, col = fbKind == k ? Go : Plain, act = () => { fbKind = k; RenderWork(); } }; }
        rows.Add(s => Row(s, "What is it about?", cT, "Pick the closest one.", "", kinds));
        string shown = fbNote.Length > 110 ? "…" + fbNote.Substring(fbNote.Length - 110) : fbNote;
        rows.Add(s => Row(s, "Your note" + (fbNote.Length > 0 ? "  (" + fbNote.Length + " / 600)" : ""), cT,
            fbTyping ? (shown.Length > 0 ? shown : "") + "_" : (shown.Length > 0 ? shown : "Tap Write and tell us in your own words (any language)."),
            fbTyping ? (fbKb == null ? "Type, then press Enter when you are done." : "Tap Done on the keyboard when you are finished.") : "",
            new WkBtn { label = fbTyping ? "Done" : "Write", on = true, col = Go, act = () => { if (fbTyping) { fbTyping = false; if (fbKb != null) fbKb.active = false; fbKb = null; RenderWork(); } else StartFeedbackTyping(); } },
            new WkBtn { label = "Clear", on = fbNote.Length > 0, col = Plain, act = () => { fbNote = ""; if (fbKb != null) fbKb.text = ""; RenderWork(); } }));
        rows.Add(s => Row(s, "Picture of your screen", cI, fbPng == null ? "No picture could be taken." : fbPic ? "Attached: the screen as it was when you opened this." : "Not attached.", "",
            new WkBtn { label = "Attach", on = fbPng != null, col = fbPic ? Go : Plain, act = () => { fbPic = true; RenderWork(); } },
            new WkBtn { label = "Leave out", on = fbPng != null, col = !fbPic ? Go : Plain, act = () => { fbPic = false; RenderWork(); } }));
        rows.Add(s => Row(s, "Sent with it", cI, "Version " + AHBuildInfo.Label + " · " + SystemInfo.deviceModel,
            "Where you are, your hero’s level" + (AHErrors.Count > 0 ? " and " + AHErrors.Count + " error" + (AHErrors.Count == 1 ? "" : "s") + " the game hit" : "") + ". Nothing else."));
        rows.Add(s => Row(s, "Send", new Color(0.6f, 1f, 0.6f), Application.isEditor ? "In the editor it is saved to Builds/feedback." : "Your phone asks which app to send it with: WhatsApp, Gmail, Telegram…", "",
            new WkBtn { label = "Send", on = true, col = Go, act = () => {
                fbTyping = false; fbKb = null;
                string where = AHFeedback.Send(AHFeedback.Report(g, AHFeedback.Kinds[fbKind], fbNote.Trim()), fbPic ? fbPng : null);
                fbNote = ""; fbPng = null; ShowWork(false); Toast(where, 3f);
            } }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    // ---------- What's new ----------
    // whatsnew.txt: "# 0.9 · title" starts a section (newest first); "- line" is a change, "- line | more" adds a second line
    class WnSection { public string head; public List<string[]> lines = new List<string[]>(); }
    static List<WnSection> WhatsNewData()
    {
        var res = new List<WnSection>(); var ta = Resources.Load<TextAsset>("AH/Data/whatsnew");
        if (ta == null) return res;
        foreach (var raw in ta.text.Split('\n'))
        {
            string l = raw.Trim();
            if (l.StartsWith("#")) res.Add(new WnSection { head = l.TrimStart('#').Trim() });
            else if (l.StartsWith("-") && res.Count > 0)
            {
                string body = l.Substring(1).Trim(); int bar = body.IndexOf('|');
                res[res.Count - 1].lines.Add(bar < 0 ? new[] { body, "" } : new[] { body.Substring(0, bar).Trim(), body.Substring(bar + 1).Trim() });
            }
        }
        return res;
    }

    public void OpenWhatsNew() { wkMode = "whatsnew"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderWhatsNew(AHPlayer p)
    {
        var data = WhatsNewData();
        wkTitle.text = "What’s new";
        wkHint.text = "Version " + AHBuildInfo.Label + ". Thank you for testing: tell us what you think with Send feedback in Settings.";
        var rows = new List<System.Action<int>>();
        Color cH = new Color(1f, 0.82f, 0.45f), cL = new Color(0.92f, 0.92f, 1f);
        foreach (var sec in data)
        {
            var se = sec;
            rows.Add(s => Row(s, se.head, cH, se.lines.Count + " change" + (se.lines.Count == 1 ? "" : "s"), ""));
            foreach (var ln in se.lines) { var l = ln; rows.Add(s => Row(s, "  • " + l[0], cL, l[1], "")); }
        }
        if (rows.Count == 0) rows.Add(s => Row(s, "Nothing listed yet", cL, "", ""));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    // after an update, the newest section opens by itself once (a few seconds after you arrive, when nothing else is open)
    bool wnChecked; float wnWait = 4f;
    void WhatsNewTick(float dt)
    {
        if (wnChecked || g.player == null || Application.isEditor || AHTraining.Active) return;
        if (Modal != 0) return;
        wnWait -= dt; if (wnWait > 0f) return;
        wnChecked = true;
        var data = WhatsNewData(); if (data.Count == 0) return;
        string head = data[0].head;
        if (PlayerPrefs.GetString("ah_whatsnew_seen", "") == head) return;
        PlayerPrefs.SetString("ah_whatsnew_seen", head); PlayerPrefs.Save();
        OpenWhatsNew();
    }

    // ---------- the tester rows at the top of Settings ----------
    void TesterRows(List<System.Action<int>> rows)
    {
        Color cV = new Color(0.6f, 1f, 0.7f);
        rows.Add(s => Row(s, "Send feedback", cV, "Found a bug or have an idea? Send a note with a picture of your screen.", "",
            new WkBtn { label = "Send feedback", on = true, col = Go, act = OpenFeedback }));
        rows.Add(s => Row(s, "Version " + AHBuildInfo.Version, cV, AHBuildInfo.Label + (AHDev.On && !Application.isEditor ? " · developer mode" : ""), "",
            new WkBtn { label = "What’s new", on = true, col = Plain, act = OpenWhatsNew },
            new WkBtn { label = "v" + AHBuildInfo.Version, on = true, col = Plain, act = () => { string m = AHDev.Tap(); if (m != null) { Toast(m, 1.6f); RenderWork(); } } }));
    }
}
