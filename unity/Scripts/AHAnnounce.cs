// Ashen Hollow: rare finds are told to the world. An Epic or Legendary catch, ore, log, hide, herb or drop puts a
// gold line in the chat ("[World] Ahmad caught a Sunscale koi!") with a chime; a Legendary one also gets the
// banner across the screen and a burst of light round the hero.
using UnityEngine;

public static class AHAnnounce
{
    static readonly string[] Verb = { "found", "found", "found", "found", "found" };
    public static int Tier(string name)
    {
        switch (name) { case "legendary": return 4; case "epic": return 3; case "rare": return 2; case "uncommon": return 1; }
        return 0;
    }
    static string Hex(int tier) { return tier >= 4 ? "#ff9a2e" : tier == 3 ? "#c36bff" : tier == 2 ? "#4aa8ff" : "#7ee06a"; }

    // a rare thing: tierName is common/uncommon/rare/epic/legendary; only Epic and Legendary are told to the world
    public static void Rare(AHGame g, AHPlayer p, string what, string tierName, string verb = null)
    {
        int t = Tier(tierName); if (t < 3 || p == null) return;
        string who = string.IsNullOrEmpty(p.heroName) ? "A hero" : p.heroName;
        AHChat.Add("world", who + " " + (verb ?? "found") + " <color=" + Hex(t) + ">[" + what + "]</color>!");
        AHSound.Play(t >= 4 ? "level" : "loot");
        if (t >= 4)
        {
            if (g != null && g.ui != null) g.ui.Banner(what, "Legendary!");
            AHSpark.Burst(p.transform.position + Vector3.up * 1.2f, new Color(1f, 0.62f, 0.2f, 1f), 40, 4f, 1.2f, 0.12f, 2f);
            AHSpark.Ring(p.transform.position + Vector3.up * 0.1f, new Color(1f, 0.8f, 0.4f, 1f), 30, 3f, 0.9f, 0.14f);
        }
    }
    // an item that came into the bag (loot, gathering): told by its quality
    public static void Item(AHGame g, AHPlayer p, ItemDef d, string verb = null)
    {
        if (d == null) return;
        int r = AHGrades.Rank(d); if (r < 3) return;
        Rare(g, p, d.name, r >= 4 ? "legendary" : "epic", verb);
    }
}
