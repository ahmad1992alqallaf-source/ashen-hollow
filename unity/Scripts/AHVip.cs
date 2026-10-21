// Ashen Hollow: the VIP month. Comfort, never power: everything you kill drops straight into your bag (auto pickup),
// +2% XP for your class and skills, and +2% profession XP. No stats, no gear, nothing that wins a fight.
// It lasts thirty days from when it is bought in the store (or given), and more months add on to the end.
using UnityEngine;

public static class AHVip
{
    public const float XpK = 1.02f, ProfXpK = 1.02f;
    static long Now() { return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
    public static bool Active(AHPlayer p) { return p != null && p.prog != null && p.prog.vipUntil > Now(); }
    public static int DaysLeft(AHPlayer p) { if (!Active(p)) return 0; return Mathf.CeilToInt((p.prog.vipUntil - Now()) / 86400f); }
    public static void Grant(AHGame g, AHPlayer p, int days)
    {
        if (p == null) return;
        long from = System.Math.Max(Now(), p.prog.vipUntil);
        p.prog.vipUntil = from + days * 86400L;
        if (g != null && g.ui != null) g.ui.Banner("VIP: " + DaysLeft(p) + " days", "Auto pickup · +2% XP · +2% profession XP");
        AHSound.Play("level");
        if (g != null) g.MarkDirty();
    }
    // the XP multiplier for a skill (professions get their own +2%, the rest the class +2%)
    public static float K(AHPlayer p, bool prof) { return Active(p) ? (prof ? ProfXpK : XpK) : 1f; }
    public static string Describe(AHPlayer p)
    {
        return Active(p) ? "VIP · " + DaysLeft(p) + " days left · auto pickup, +2% XP, +2% profession XP"
                         : "VIP month: auto pickup of loot, +2% XP, +2% profession XP. Comfort only, never power.";
    }
}
