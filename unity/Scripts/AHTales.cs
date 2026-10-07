// Ashen Hollow: tales of the townsfolk. Three people of Ashen Hollow each have a little story in three parts, told in
// short cutscenes like the Ashen King's: Baker Maudie (a bakery with no flour and a wolf at the door), Woodcarver
// Ansel (a carving for a lost brother) and Little Tess (who wants to be a hero). Talk to them when a gold "!" is over
// their head. Tasks: bring things, or beat monsters. Each part pays in xp, coin and sometimes a gift; the last
// part of each tale gives something to keep.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHTales
{
    public class Part { public string task, kind, item; public int need, xp; public long gold; public string gift; public AHStory.Line[] intro, outro; }
    public class Tale { public string who, name; public int lvl; public Part[] parts; }
    static AHStory.Line L(string w, string t) { return new AHStory.Line(w, t); }
    public static readonly Tale[] All =
    {
        new Tale { who = "Baker Maudie", name = "Bread and Wolves", lvl = 1, parts = new[]
        {
            new Part { task = "Bring Maudie 5 wheat", kind = "bring", item = "wheat", need = 5, xp = 250, gold = 600,
                intro = new[] { L("Baker Maudie", "Oh, thank goodness, a hero! My mill cart never came."), L("Baker Maudie", "No wheat, no bread, and the whole village eats from my ovens. Five sheaves would save the morning!") },
                outro = new[] { L("Baker Maudie", "Lovely, lovely! I'll have loaves in the oven within the hour."), L("Baker Maudie", "Here, take something warm for the road.") }, gift = "bread" },
            new Part { task = "Defeat 8 wolves or other beasts around the village", kind = "kill", need = 8, xp = 500, gold = 1200,
                intro = new[] { L("Baker Maudie", "Something keeps scratching at my back door at night. Big paws. Yellow eyes."), L("Baker Maudie", "The smell of bread brings them. Could you thin them out a little?") },
                outro = new[] { L("Baker Maudie", "Quiet nights again! My cat came back home, the brave little coward."), L("Baker Maudie", "I owe you more than bread. Come back tomorrow, I have one more favour.") } },
            new Part { task = "Bring Maudie 3 eggs and 2 milk", kind = "bring", item = "egg", need = 3, xp = 800, gold = 2000, gift = "honey_cake",
                intro = new[] { L("Baker Maudie", "The harvest feast is coming and I want to bake my mother's honey cake."), L("Baker Maudie", "Three eggs, and two milk too. The recipe is older than this village!") },
                outro = new[] { L("Baker Maudie", "Smell that? That is forty years of my family in one cake."), L("Baker Maudie", "The first slice is yours, hero. And my door is always open.") } },
        } },
        new Tale { who = "Woodcarver Ansel", name = "The Carved Brother", lvl = 5, parts = new[]
        {
            new Part { task = "Bring Ansel 10 logs", kind = "bring", item = "logs", need = 10, xp = 400, gold = 900,
                intro = new[] { L("Woodcarver Ansel", "My brother left for the Frost Peaks ten winters ago. He never came back."), L("Woodcarver Ansel", "I want to carve him, so the village remembers his face. I need good wood. Ten logs.") },
                outro = new[] { L("Woodcarver Ansel", "Good, straight grain. Wood remembers, you know."), L("Woodcarver Ansel", "Now I need something of the wild he loved.") } },
            new Part { task = "Bring Ansel 4 wolf fangs", kind = "bring", item = "wolf_fang", need = 4, xp = 700, gold = 1500,
                intro = new[] { L("Woodcarver Ansel", "He wore a necklace of wolf fangs. Said they kept the cold out."), L("Woodcarver Ansel", "Four fangs, and the carving will have it too.") },
                outro = new[] { L("Woodcarver Ansel", "Just like his. My hands are shaking."), L("Woodcarver Ansel", "One last thing, and then I'll show you.") } },
            new Part { task = "Defeat 12 beasts so Ansel can carve in peace", kind = "kill", need = 12, xp = 1100, gold = 2600, gift = "lucky_charm",
                intro = new[] { L("Woodcarver Ansel", "I carve at night, out by the old stump. The beasts don't like the knocking."), L("Woodcarver Ansel", "Keep them off me for a while, and I'll finish him.") },
                outro = new[] { L("Woodcarver Ansel", "There he is. My brother, smiling, the way I remember him."), L("Woodcarver Ansel", "He would have liked you. Take this charm, it was his.") } },
        } },
        new Tale { who = "Little Tess", name = "Tess the Brave", lvl = 3, parts = new[]
        {
            new Part { task = "Bring Tess 5 feathers for her hero's hat", kind = "bring", item = "feather", need = 5, xp = 300, gold = 500,
                intro = new[] { L("Little Tess", "When I grow up I'm going to be a hero, just like you!"), L("Little Tess", "Every hero needs a hat with feathers. Five feathers! Please please please?") },
                outro = new[] { L("Little Tess", "It's the best hat in the whole realm!"), L("Little Tess", "Now I need to learn to fight. Will you show me?") } },
            new Part { task = "Defeat 6 beasts while Tess watches", kind = "kill", need = 6, xp = 600, gold = 1000,
                intro = new[] { L("Little Tess", "I'll watch from right here. I won't get close, I promise."), L("Little Tess", "Show me how a real hero does it!") },
                outro = new[] { L("Little Tess", "WOW. You went whoosh and they went bonk!"), L("Little Tess", "I practised on a scarecrow. It lost. Can I have a real quest now?") } },
            new Part { task = "Bring Tess 3 boar hides for her 'armour'", kind = "bring", item = "boar_hide", need = 3, xp = 900, gold = 1800, gift = "mystery_sack",
                intro = new[] { L("Little Tess", "A hero needs armour. Mum says no swords, but she didn't say anything about armour."), L("Little Tess", "Three boar hides and I'll make the toughest armour ever!") },
                outro = new[] { L("Little Tess", "Look at me! Tess the Brave, defender of Ashen Hollow!"), L("Little Tess", "Here, this is my treasure sack. Heroes share. You taught me that.") } },
        } },
    };

    static int Step(AHPlayer p, Tale t) { return (int)AHProgress.Get(p.prog.stats, "tale:" + t.who); }      // parts done
    static int State(AHPlayer p, Tale t) { return (int)AHProgress.Get(p.prog.stats, "tales:" + t.who); }    // 0 offered, 1 under way
    static int Count(AHPlayer p, Tale t) { return (int)AHProgress.Get(p.prog.stats, "talen:" + t.who); }
    static void Set(AHPlayer p, string k, long v) { AHProgress.Add(p.prog.stats, k, v - AHProgress.Get(p.prog.stats, k)); }
    public static Tale Of(string who) { foreach (var t in All) if (t.who == who) return t; return null; }
    public static bool Done(AHPlayer p, Tale t) { return Step(p, t) >= t.parts.Length; }
    public static bool HasMark(AHPlayer p, string who)
    {
        var t = Of(who); if (t == null || p == null || Done(p, t) || p.level < t.lvl) return false;
        if (State(p, t) == 0) return true;
        var part = t.parts[Step(p, t)]; return Ready(p, part, Count(p, t));
    }
    static bool Ready(AHPlayer p, Part part, int n) { return part.kind == "kill" ? n >= part.need : p.bag.Count(part.item) >= part.need && (part.item != "egg" || p.bag.Count("milk") >= 2); }

    // talking to one of them: true when the tale took the conversation
    public static bool Talk(AHGame g, AHNpc n)
    {
        var p = g.player; var t = Of(n.npcName); if (t == null || Done(p, t) || p.level < t.lvl || AHCine.Active) return false;
        var part = t.parts[Step(p, t)]; int st = State(p, t);
        if (st == 0)
        {
            AHCine.Play(g, t.name + " · part " + (Step(p, t) + 1) + " of " + t.parts.Length, part.intro, () =>
            {
                Set(p, "tales:" + t.who, 1); Set(p, "talen:" + t.who, 0); g.MarkDirty();
                g.ui.Banner(t.name, part.task); AHChat.Add("story", t.who + ": " + part.task);
            });
            return true;
        }
        if (!Ready(p, part, Count(p, t)))
        {
            g.ui.Toast(t.who + ": " + part.task + (part.kind == "kill" ? " (" + Count(p, t) + "/" + part.need + ")" : " (you have " + p.bag.Count(part.item) + "/" + part.need + ")") + ".", 3f);
            return true;
        }
        AHCine.Play(g, null, part.outro, () =>
        {
            if (part.kind == "bring") { for (int i = 0; i < part.need; i++) p.bag.Take(part.item); if (part.item == "egg") { p.bag.Take("milk"); p.bag.Take("milk"); } }
            p.GainXp(part.xp); p.AddMoney(part.gold * AHDB.CU, p.transform.position);
            if (part.gift != null && AHItems.Get(part.gift) != null) p.bag.Add(part.gift);
            int s = Step(p, t) + 1; Set(p, "tale:" + t.who, s); Set(p, "tales:" + t.who, 0); Set(p, "talen:" + t.who, 0);
            g.ui.Banner(s >= t.parts.Length ? t.name + " · the end" : t.name + " · part " + s + " done", "+" + part.xp + " xp · " + AHItems.MoneyText(part.gold * AHDB.CU) + (part.gift != null && AHItems.Get(part.gift) != null ? " · " + AHItems.Get(part.gift).name : ""));
            AHSound.Play("level"); p.bag.Touch(); g.SaveProgress();
        });
        return true;
    }

    public static void OnKill(AHGame g, AHMob m)
    {
        var p = g.player; if (p == null || AHJuice.IsDummy(m)) return;
        foreach (var t in All)
        {
            if (Done(p, t) || State(p, t) != 1) continue;
            var part = t.parts[Step(p, t)]; if (part.kind != "kill") continue;
            int n = Count(p, t); if (n >= part.need) continue;
            Set(p, "talen:" + t.who, n + 1);
            if (n + 1 >= part.need) g.ui.Toast(t.name + ": done! Go back to " + t.who + ".", 3f);
        }
    }

    // the tales' window (MENU → Tales of the townsfolk)
    public static string Line(AHPlayer p, Tale t)
    {
        if (Done(p, t)) return "Done";
        if (p.level < t.lvl) return "Opens at level " + t.lvl;
        var part = t.parts[Step(p, t)];
        if (State(p, t) == 0) return "Part " + (Step(p, t) + 1) + ": talk to " + t.who + " in Ashen Hollow";
        return "Part " + (Step(p, t) + 1) + ": " + part.task + (part.kind == "kill" ? " (" + Count(p, t) + "/" + part.need + ")" : " (" + p.bag.Count(part.item) + "/" + part.need + ")");
    }
}

public partial class AHUI
{
    public void OpenTales() { wkMode = "tales"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderTales(AHPlayer p)
    {
        wkTitle.text = "Tales of the townsfolk";
        wkHint.text = "Little stories of the people of Ashen Hollow. A gold ! over someone's head means they have something for you.";
        var rows = new List<Action<int>>();
        foreach (var t in AHTales.All)
        {
            var tt = t; bool done = AHTales.Done(p, t);
            rows.Add(s => Row(s, tt.name + " · " + tt.who, done ? new Color(0.6f, 0.9f, 0.5f) : new Color(1f, 0.8f, 0.45f), AHTales.Line(p, tt), ""));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
