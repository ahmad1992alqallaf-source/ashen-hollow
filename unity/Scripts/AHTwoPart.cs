// Ashen Hollow: two-part quests. Stories made of chapters for both paths, where each path has a part the other can't
// do alone. You play your own path's chapters; a partner of the other path plays theirs (the time it takes shows on
// the quest), and you can only go on once they are done.
//   The Sealed Vault (from level 14, or mastery 5 for an Artisan): an old guild vault under the Mire.
//     I.  (Adventurer) Clear the Mire Warrens; its boss carries the Vault seal.
//     II. (Artisan)    Forge the Vault key at an anvil from the seal, iron bars and gold ore.
//     III.(Adventurer) Fight the vault's guardians: defeat 25 foes.
//     IV. (Artisan)    Restore the treasure: gather 20 materials for the polish and the salve.
//   An Adventurer's partner is Tobin Ashby, the guild's young smith; an Artisan's partner is Captain Kael Varn.
//   Reward: coin, XP, a gem and an ancient relic, and the title of Vaultbreaker (Adventurer) or Keysmith (Artisan).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHTwoPartState { public int step; public int n; public long waitEnd; public bool started, done, claimed; }

public static class AHTwoPart
{
    public class Step { public string path, label, story, t, id; public int n, waitMins; }
    public const string Name = "The Sealed Vault", Seal = "vault_seal", Key = "vault_key";
    public static readonly Step[] Steps =
    {
        new Step { path = "adventurer", t = "dclear", id = "d_warrens", n = 1, waitMins = 30, label = "Clear the Mire Warrens and take the Vault seal from its boss",
            story = "Under the Mire lies an old guild vault, sealed since the fire. Its seal was carried off by the Warrens' master. Someone has to fight for it." },
        new Step { path = "artisan", t = "make", id = Key, n = 1, waitMins = 25, label = "Forge the Vault key at an anvil (the seal, 4 iron bars and 2 gold ore)",
            story = "The seal is a mould as much as a lock. Only a smith's hands can pour a key from it." },
        new Step { path = "adventurer", t = "kill", id = "any", n = 25, waitMins = 30, label = "Fight the vault's guardians: defeat 25 foes",
            story = "The key turns. Old things wake in the dark below, and they do not like visitors." },
        new Step { path = "artisan", t = "gather", id = "any", n = 20, waitMins = 20, label = "Restore the treasure: gather 20 materials (ore, herbs, wood or fish) for the polish and the salve",
            story = "The vault's treasure is tarnished and brittle. A maker can bring it back; a fighter would only break it." },
    };
    public static readonly string[] Chapters = { "The Seal", "The Key", "The Guardians", "The Treasure" };

    public static AHTwoPartState S(AHPlayer p) { if (p.prog.twoPart == null) p.prog.twoPart = new AHTwoPartState(); return p.prog.twoPart; }
    static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
    static string Path(AHPlayer p) { return p.path == "artisan" ? "artisan" : "adventurer"; }
    public static string Partner(AHPlayer p) { return p.path == "artisan" ? "Captain Kael Varn" : "Tobin Ashby"; }
    public static string Title(AHPlayer p) { return p.path == "artisan" ? "Keysmith" : "Vaultbreaker"; }
    public static bool Open(AHPlayer p) { return p.path == "artisan" ? (p.BestMastery() >= 5 || p.level >= 10) : p.level >= 14; }
    public static Step Cur(AHPlayer p) { var s = S(p); return s.step < Steps.Length ? Steps[s.step] : null; }
    public static bool Mine(AHPlayer p, Step st) { return st != null && st.path == Path(p); }
    public static bool Waiting(AHPlayer p) { var s = S(p); return s.started && !s.done && Cur(p) != null && !Mine(p, Cur(p)); }
    public static long WaitLeft(AHPlayer p) { return Math.Max(0, S(p).waitEnd - NowS); }

    public static void Start(AHGame g)
    {
        var p = g.player; var s = S(p); if (s.started || !Open(p)) return;
        s.started = true; s.step = 0; s.n = 0;
        BeginStep(g, p);
        g.MarkDirty();
    }

    static void BeginStep(AHGame g, AHPlayer p)
    {
        var s = S(p); s.n = 0; var st = Cur(p);
        if (st == null) { s.done = true; g.ui.Banner(Name + " · done", "Claim your reward in the Menu"); return; }
        if (!Mine(p, st)) s.waitEnd = NowS + st.waitMins * 60L;
        g.ui.Toast(Name + " · " + Chapters[s.step] + ": " + (Mine(p, st) ? st.label : Partner(p) + " takes this part (" + st.waitMins + " min)."), 4.5f);
    }

    static void Advance(AHGame g, AHPlayer p)
    {
        var s = S(p); var st = Cur(p);
        // what the chapter leaves you with
        if (st != null && st.id == "d_warrens") { if (p.path != "artisan") g.ui.Banner("The Vault seal", Partner(p) + " takes it to the forge."); }
        if (st != null && st.path == "adventurer" && st.t == "dclear" && p.path == "artisan") { p.bag.Add(Seal); g.ui.Banner(Partner(p) + " is back", "He brings you the Vault seal. Forge the key."); }
        if (st != null && st.t == "make" && p.path != "artisan") g.ui.Banner(Partner(p) + " is done", "The Vault key is forged. Go and open the vault.");
        if (st != null && st.t == "kill" && p.path == "artisan") g.ui.Banner(Partner(p) + " is back", "The guardians are down. Restore the treasure.");
        s.step++; BeginStep(g, p); g.MarkDirty();
    }

    // every few seconds (AHGame): a partner's chapter finishes when its time is up
    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null || p.prog == null || !Waiting(p)) return;
        if (WaitLeft(p) <= 0) Advance(g, p);
    }

    // everything the quest log hears (AHQuests.Event)
    public static void Event(AHGame g, string t, string id, int n)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        var s = S(p); var st = Cur(p); if (!s.started || s.done || st == null || !Mine(p, st)) return;
        bool match = st.t == t && (st.id == "any" || st.id == id);
        if (!match) return;
        s.n = Mathf.Min(st.n, s.n + n);
        if (s.n >= st.n) Advance(g, p); else g.MarkDirty();
    }

    // the key is made at the anvil: AHGather.Craft tells us
    public static void OnMade(AHGame g, string outId) { if (outId == Key) Event(g, "make", Key, 1); }

    public static void Claim(AHGame g)
    {
        var p = g.player; var s = S(p); if (!s.done || s.claimed) return;
        s.claimed = true;
        long coin = 3000L * AHDB.CU; p.AddMoney(coin, p.transform.position);
        string[] gems = { "sapphire", "ruby", "emerald" }; p.bag.Add(gems[UnityEngine.Random.Range(0, 3)]); p.bag.Add("ancient_relic");
        p.GainXp("attack", 1500, false);
        p.bag.Take(Key, p.bag.Count(Key));
        g.ui.Banner(Name + " complete", "You are now " + Title(p) + " · " + AHItems.MoneyText(coin) + ", a gem and an ancient relic");
        p.bag.Touch(); g.MarkDirty();
    }

    public static string Status(AHPlayer p)
    {
        var s = S(p);
        if (!s.started) return Open(p) ? "Ready to begin" : (p.path == "artisan" ? "Opens at mastery 5 or level 10" : "Opens at level 14");
        if (s.claimed) return "Done · " + Title(p);
        if (s.done) return "Done · claim your reward";
        var st = Cur(p);
        return "Chapter " + (s.step + 1) + " of " + Steps.Length + " · " + (Mine(p, st) ? st.label + (st.n > 1 ? " (" + s.n + " / " + st.n + ")" : "") : Partner(p) + " is on it · " + AHExpeditions.LeftText(WaitLeft(p)));
    }
}

public partial class AHUI
{
    public void OpenTwoPart() { wkMode = "twopart"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderTwoPart(AHPlayer p)
    {
        AHTwoPart.Tick(g);
        var s = AHTwoPart.S(p); var rows = new List<Action<int>>();
        wkTitle.text = "Two-part quest: " + AHTwoPart.Name;
        wkHint.text = "A story for both paths: you play the " + (p.path == "artisan" ? "Artisan's" : "Adventurer's") + " chapters, and " + AHTwoPart.Partner(p) + " plays the other path's. Neither can finish it alone.";
        for (int i = 0; i < AHTwoPart.Steps.Length; i++)
        {
            var st = AHTwoPart.Steps[i]; int ii = i; bool mine = AHTwoPart.Mine(p, st);
            bool done = s.started && (s.step > i || s.done), now = s.started && !s.done && s.step == i;
            string who = mine ? "You" : AHTwoPart.Partner(p);
            string line2 = done ? "<color=#9be37a>Done</color>" : now ? (mine ? st.label + (st.n > 1 ? " · " + s.n + " / " + st.n : "") : who + " is on it · back in " + AHExpeditions.LeftText(AHTwoPart.WaitLeft(p))) : (mine ? st.label : who + "'s part");
            rows.Add(sl => Row(sl, (ii + 1) + ". " + AHTwoPart.Chapters[ii] + " · " + (st.path == "artisan" ? "Artisan" : "Adventurer") + (mine ? " (you)" : ""), done ? new Color(0.62f, 0.9f, 0.55f) : now ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 1f, 1f, 0.6f),
                line2, now || !s.started && ii == 0 ? st.story : ""));
        }
        if (!s.started) rows.Add(sl => Row(sl, "Begin the story", Color.white, AHTwoPart.Status(p), "", new WkBtn { label = "Begin", on = AHTwoPart.Open(p), col = Go, act = () => { AHTwoPart.Start(g); RenderWork(); } }));
        else if (s.done && !s.claimed) rows.Add(sl => Row(sl, "Reward", new Color(1f, 0.85f, 0.4f), "Coin, XP, a gem, an ancient relic and the title " + AHTwoPart.Title(p), "", new WkBtn { label = "Claim", on = true, col = Go, act = () => { AHTwoPart.Claim(g); RenderWork(); } }));
        if (s.started && !s.done && AHTwoPart.Cur(p) != null && AHTwoPart.Cur(p).t == "make")
            rows.Add(sl => Row(sl, "The key's recipe", new Color(0.62f, 0.85f, 1f), "At an anvil: Vault seal, 4 iron bars, 2 gold ore (Smithing 5)", "You have " + p.bag.Count(AHTwoPart.Seal) + " seal, " + p.bag.Count("iron_bar") + " iron bars, " + p.bag.Count("gold_ore") + " gold ore"));
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
