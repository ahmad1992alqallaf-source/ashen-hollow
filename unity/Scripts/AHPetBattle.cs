// Ashen Hollow: pet battles, as in the web game (v66, PTYPES / PSTAT / TAMERS / pbStart / pbMove / pbSwap / pbEnd).
// Turn-based fights between teams of up to three pets. Types go round in a circle: beast beats critter, critter beats
// magic, magic beats flying, flying beats aquatic, aquatic beats dragon, dragon beats beast. Beat the realm's six tamers
// in order (each pays, a lot more the first time). Pets level up as they fight. From Menu → Pet battles.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHPetBattle
{
    static readonly string[] Types = { "beast", "critter", "magic", "flying", "aquatic", "dragon" };
    static float TypeK(string a, string b) { int i = Array.IndexOf(Types, a), j = Array.IndexOf(Types, b); return (i + 1) % 6 == j ? 1.5f : (j + 1) % 6 == i ? 0.67f : 1f; }
    // type, hp, attack, speed, three moves
    static readonly Dictionary<string, object[]> Stat = new Dictionary<string, object[]>
    {
        { "toad", new object[] { "aquatic", 110, 11, 7, "Tongue lash", "Bubble burst", "Hop away" } }, { "slime", new object[] { "magic", 130, 9, 5, "Splat", "Acid glob", "Wobble" } },
        { "fox", new object[] { "beast", 100, 13, 12, "Bite", "Ember pounce", "Curl up" } }, { "owl", new object[] { "flying", 95, 12, 14, "Talons", "Moonlit dive", "Fly high" } },
        { "drake", new object[] { "dragon", 140, 14, 10, "Claw", "Spark breath", "Wing shield" } }, { "parrot", new object[] { "flying", 95, 11, 15, "Peck", "Pirate squawk", "Fly high" } },
        { "pumpkin_slime", new object[] { "magic", 135, 10, 6, "Splat", "Pie-in-the-face", "Wobble" } }, { "lantern_owl", new object[] { "flying", 100, 12, 14, "Talons", "Lantern glow", "Fly high" } },
        { "snow_fox", new object[] { "beast", 105, 13, 13, "Bite", "Frost pounce", "Curl up" } },
    };
    public class Tamer { public string n, blurb; public string[] k; public int[] L; }
    public static readonly Tamer[] Tamers =
    {
        new Tamer { n = "Little Tess", blurb = "Ashen Hollow’s youngest tamer. Be gentle.", k = new[] { "toad" }, L = new[] { 3 } },
        new Tamer { n = "Fisher Pell", blurb = "Raises toads by the river in Varrow.", k = new[] { "toad", "slime" }, L = new[] { 6, 6 } },
        new Tamer { n = "Hunter Kael", blurb = "His fox and owl hunt as one.", k = new[] { "fox", "owl" }, L = new[] { 10, 10 } },
        new Tamer { n = "Sigrid Frostfur", blurb = "Highcairn’s champion. Her pets never feel the cold.", k = new[] { "owl", "fox", "slime" }, L = new[] { 14, 14, 14 } },
        new Tamer { n = "Old Wyn", blurb = "The alchemist’s pets glow faintly. Don’t ask why.", k = new[] { "slime", "toad", "drake" }, L = new[] { 18, 18, 18 } },
        new Tamer { n = "Grand Tamer Ilsa", blurb = "Nobody has beaten her team in years.", k = new[] { "drake", "parrot", "fox" }, L = new[] { 20, 20, 20 } },
    };
    public class Unit { public string k, type, name; public int L, mhp, hp, cd, weak; public float atk, spd; public bool mine, guard; public string[] moves; }
    public static Unit Make(string k, int L, bool mine)
    {
        object[] s; if (!Stat.TryGetValue(k, out s)) s = Stat["toad"];
        float g = 1f + 0.08f * (L - 1);
        return new Unit { k = k, L = L, mine = mine, type = (string)s[0], mhp = Mathf.RoundToInt((int)s[1] * g), hp = Mathf.RoundToInt((int)s[1] * g), atk = (int)s[2] * g, spd = (int)s[3] + L * 0.2f, moves = new[] { (string)s[4], (string)s[5], (string)s[6] }, name = AHComp.PetName(k) };
    }
    public static string TypeOf(string k) { object[] s; return Stat.TryGetValue(k, out s) ? (string)s[0] : "?"; }
    public static List<string> Team(AHPlayer p) { var o = new List<string>(); foreach (var k in p.pets) if (o.Count < 3 && Stat.ContainsKey(k)) o.Add(k); return o; }

    // the battle in progress
    public static List<Unit> A, B; public static int ai, bi, turn, tamer; public static bool over, won; public static List<string> log = new List<string>(); public static string label;

    public static void Start(AHGame g, int i)
    {
        var p = g.player; var T = Tamers[i];
        if (!p.pets.Exists(k => Stat.ContainsKey(k))) { p.pets.Add("toad"); g.ui.Banner("Mire toadling", "Little Tess gives you a toadling to start your team"); g.SaveProgress(); }
        A = new List<Unit>(); foreach (var k in Team(p)) A.Add(Make(k, Mathf.Max(1, AHComp.PetLv(p, k)), true));
        B = new List<Unit>(); for (int j = 0; j < T.k.Length; j++) B.Add(Make(T.k[j], T.L[j], false));
        ai = 0; bi = 0; turn = 1; tamer = i; over = false; won = false; label = T.n + " challenges you";
        log = new List<string> { label + "! Your " + A[0].name + " faces " + B[0].name + "." };
    }
    static string Act(Unit u, Unit foe, int mv)
    {
        if (mv == 2) { u.guard = true; int h = Mathf.RoundToInt(u.mhp * 0.1f); u.hp = Mathf.Min(u.mhp, u.hp + h); return (u.mine ? "Your " : "Their ") + u.name + " uses " + u.moves[2] + " and recovers " + h + "."; }
        bool sp = mv == 1; float k = TypeK(u.type, foe.type);
        float dmg = u.atk * (sp ? 1.9f : 1f) * (0.9f + UnityEngine.Random.value * 0.2f) * k * (u.weak > 0 ? 0.7f : 1f); if (foe.guard) dmg *= 0.5f;
        int d = Mathf.Max(1, Mathf.RoundToInt(dmg)); foe.hp = Mathf.Max(0, foe.hp - d);
        if (sp) { u.cd = 3; if (u.k == "parrot") foe.weak = 2; }
        return (u.mine ? "Your " : "Their ") + u.name + " uses " + u.moves[mv] + " for " + d + (k > 1 ? " (super effective!)" : k < 1 ? " (not very effective)" : "") + ".";
    }
    static int AiMove(Unit u) { if (u.cd <= 0 && UnityEngine.Random.value < 0.55f) return 1; if (u.hp < u.mhp * 0.3f && UnityEngine.Random.value < 0.4f) return 2; return 0; }
    public static void Move(AHGame g, int mv)
    {
        if (over) return; var a = A[ai]; var b = B[bi]; if (mv == 1 && a.cd > 0) return;
        a.guard = false; b.guard = false; int bm = AiMove(b);
        var order = a.spd >= b.spd ? new[] { new object[] { a, b, mv }, new object[] { b, a, bm } } : new[] { new object[] { b, a, bm }, new object[] { a, b, mv } };
        foreach (var o in order) { var u = (Unit)o[0]; var f = (Unit)o[1]; if (u.hp <= 0 || f.hp <= 0) continue; log.Add(Act(u, f, (int)o[2])); }
        foreach (var u in new[] { a, b }) { if (u.cd > 0) u.cd--; if (u.weak > 0) u.weak--; }
        After(g);
    }
    public static void Swap(AHGame g, int i)
    {
        if (over || i == ai || i >= A.Count || A[i].hp <= 0) return;
        ai = i; var a = A[i]; var b = B[bi]; log.Add("You send in " + a.name + "."); a.guard = false; b.guard = false; log.Add(Act(b, a, AiMove(b))); After(g);
    }
    static void After(AHGame g)
    {
        if (B[bi].hp <= 0) { log.Add(B[bi].name + " is out!"); int n = B.FindIndex(u => u.hp > 0); if (n < 0) { End(g, true); return; } bi = n; log.Add("They send in " + B[n].name + "."); }
        if (A[ai].hp <= 0) { log.Add(A[ai].name + " is out!"); int n = A.FindIndex(u => u.hp > 0); if (n < 0) { End(g, false); return; } ai = n; log.Add("You send in " + A[n].name + "."); }
        turn++; if (log.Count > 6) log.RemoveRange(0, log.Count - 6);
    }
    public static void End(AHGame g, bool w)
    {
        over = true; won = w; var p = g.player;
        int lsum = 0; foreach (var u in B) lsum += u.L; int xp = Mathf.RoundToInt(lsum * (w ? 12 : 4));
        foreach (var u in A) { int b0 = AHComp.PetLv(p, u.k); long x; p.petXp.TryGetValue(u.k, out x); p.petXp[u.k] = x + xp; if (AHComp.PetLv(p, u.k) > b0) log.Add(u.name + " reaches level " + AHComp.PetLv(p, u.k) + "!"); }
        log.Add(w ? "You win! Your pets gain " + xp + " XP each." : "You lose. Your pets still gain " + xp + " XP each.");
        if (w)
        {
            bool first = !p.prog.pbTamers.Contains(tamer); long c = (long)Mathf.Round((first ? 3000 : 500) * (tamer + 1) * (tamer + 1)) * AHDB.CU;
            if (first) p.prog.pbTamers.Add(tamer);
            p.AddMoney(c, p.transform.position); log.Add(Tamers[tamer].n + " pays you " + AHItems.MoneyText(c) + (first ? " (first win bonus)." : "."));
            p.prog.Bump("pb_wins");
        }
        if (log.Count > 6) log.RemoveRange(0, log.Count - 6);
        g.SaveProgress();
    }
}

public partial class AHUI
{
    public void OpenPetBattles() { wkMode = "pb"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderPetBattles(AHPlayer p)
    {
        var rows = new List<Action<int>>();
        if (AHPetBattle.A != null && wkMode == "pbfight")
        {
            var A = AHPetBattle.A; var B = AHPetBattle.B; var a = A[AHPetBattle.ai]; var b = B[AHPetBattle.bi];
            wkTitle.text = AHPetBattle.label + " · turn " + AHPetBattle.turn;
            wkHint.text = string.Join("  ", AHPetBattle.log.GetRange(Mathf.Max(0, AHPetBattle.log.Count - 3), Mathf.Min(3, AHPetBattle.log.Count)).ToArray());
            Func<AHPetBattle.Unit, string> bar = u => { int n = Mathf.RoundToInt(u.hp * 20f / u.mhp); string c = u.hp > u.mhp / 2 ? "#7dea8a" : u.hp > u.mhp / 4 ? "#ffd66a" : "#ff6a5a"; return "<color=" + c + ">" + new string('█', n) + "</color><color=#3a332e>" + new string('█', 20 - n) + "</color> " + u.hp + "/" + u.mhp; };
            rows.Add(s => Row(s, "Yours: " + a.name + "  L" + a.L + " " + a.type, new Color(0.6f, 0.9f, 0.5f), bar(a), (a.cd > 0 ? "Special in " + a.cd + " · " : "") + "bench: " + string.Join(", ", A.FindAll(u => u != a).ConvertAll(u => u.name + " " + u.hp + "/" + u.mhp).ToArray())));
            rows.Add(s => Row(s, "Theirs: " + b.name + "  L" + b.L + " " + b.type, new Color(1f, 0.6f, 0.5f), bar(b), "bench: " + string.Join(", ", B.FindAll(u => u != b).ConvertAll(u => u.name + " " + u.hp + "/" + u.mhp).ToArray())));
            if (!AHPetBattle.over)
            {
                rows.Add(s => Row(s, "Your move", Color.white, "", "",
                    new WkBtn { label = a.moves[0], on = true, col = Plain, act = () => { AHPetBattle.Move(g, 0); RenderWork(); } },
                    new WkBtn { label = a.moves[1], on = a.cd <= 0, col = Go, act = () => { AHPetBattle.Move(g, 1); RenderWork(); } },
                    new WkBtn { label = a.moves[2], on = true, col = Plain, act = () => { AHPetBattle.Move(g, 2); RenderWork(); } }));
                for (int i = 0; i < A.Count; i++) { int ii = i; var u = A[i]; if (i != AHPetBattle.ai && u.hp > 0) rows.Add(s => Row(s, "Swap in " + u.name, Color.white, "L" + u.L + " " + u.type + " · " + u.hp + "/" + u.mhp, "", new WkBtn { label = "Swap", on = true, col = Plain, act = () => { AHPetBattle.Swap(g, ii); RenderWork(); } })); }
                rows.Add(s => Row(s, "Forfeit", new Color(1f, 1f, 1f, 0.7f), "Call your pets back.", "", new WkBtn { label = "Forfeit", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { AHPetBattle.log.Add("You call your pets back."); AHPetBattle.End(g, false); RenderWork(); } }));
            }
            else rows.Add(s => Row(s, AHPetBattle.won ? "Victory!" : "Defeat", AHPetBattle.won ? new Color(0.6f, 0.9f, 0.5f) : new Color(1f, 0.6f, 0.5f), AHPetBattle.log[AHPetBattle.log.Count - 1], "", new WkBtn { label = "Back", on = true, col = Go, act = () => { AHPetBattle.A = null; OpenPetBattles(); } }));
        }
        else
        {
            var team = AHPetBattle.Team(p);
            wkTitle.text = "Pet battles · " + p.prog.pbTamers.Count + "/6 tamers beaten";
            wkHint.text = "Turn-based battles with up to three of your pets. Beast beats critter, critter beats magic, magic beats flying, flying beats aquatic, aquatic beats dragon, dragon beats beast. Your team: " + (team.Count > 0 ? string.Join(", ", team.ConvertAll(k => AHComp.PetName(k) + " L" + Mathf.Max(1, AHComp.PetLv(p, k)) + " " + AHPetBattle.TypeOf(k)).ToArray()) : "no pets yet. Little Tess will lend you a toadling.");
            for (int i = 0; i < AHPetBattle.Tamers.Length; i++)
            {
                int ii = i; var T = AHPetBattle.Tamers[i]; bool beaten = p.prog.pbTamers.Contains(i);
                var list = new List<string>(); for (int j = 0; j < T.k.Length; j++) list.Add(AHComp.PetName(T.k[j]) + " L" + T.L[j]);
                rows.Add(s => Row(s, T.n + (beaten ? "  <color=#9be37a>beaten</color>" : ""), new Color(1f, 0.8f, 0.45f), T.blurb, string.Join(" · ", list.ToArray()),
                    new WkBtn { label = "Battle", on = ii == 0 || p.prog.pbTamers.Contains(ii - 1), col = Go, act = () => { AHPetBattle.Start(g, ii); wkMode = "pbfight"; RenderWork(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
