// Ashen Hollow: first-time hints. A new hero gets one short tip the first time each thing comes up: moving and looking,
// talking to someone, a monster close by, a body to skin, the bag and the map. Each tip shows once per save
// (cleared by a new game).
using UnityEngine;

public static class AHTutorial
{
    static readonly string[] ids = { "move", "talk", "fight", "skin", "bag", "map" };
    static float wait = 2.5f, t; static Vector3 start; static bool started;

    public static void ClearAll() { foreach (var id in ids) PlayerPrefs.DeleteKey("ah_tut_" + id); started = false; }

    static bool Done(string id) { return PlayerPrefs.GetInt("ah_tut_" + id, 0) == 1; }
    static bool Show(AHUI ui, string id, string text)
    {
        if (Done(id)) return false;
        PlayerPrefs.SetInt("ah_tut_" + id, 1);
        ui.Toast("Tip: " + text, 6f);
        wait = 9f;   // one tip at a time
        return true;
    }

    // called every frame by the HUD while nothing is open
    public static void Tick(AHGame g, AHUI ui, float dt)
    {
        var p = g.player;
        if (p == null || p.dead || ui.CreatorOpen) return;
        if (!started) { started = true; start = p.transform.position; }
        if (wait > 0f) { wait -= dt; return; }
        t += dt; if (t < 0.4f) return; t = 0f;
        bool touch = Application.isMobilePlatform;
        if (Show(ui, "move", touch ? "move with the stick on the left; drag on the right to look around, pinch to zoom." : "move with WASD; drag with the mouse to look around, scroll to zoom.")) return;
        Vector3 pp = p.transform.position;
        if (AHNpc.Nearest(pp) != null && Show(ui, "talk", touch ? "tap the big action button to talk." : "press E to talk.")) return;
        foreach (var m in g.mobs)
        {
            if (m == null) continue;
            float d = (m.transform.position - pp).magnitude;
            if (!m.dead && d < 12f && Show(ui, "fight", touch ? "tap ATTACK to fight; your spells sit around it." : "hold Space to attack; 1-6 cast your spells, H drinks a potion.")) return;
            if (m.CanSkin && m.type.skin != null && d < 6f && Show(ui, "skin", touch ? "stand by the body and tap the action button to skin it for hide." : "stand by the body and press E to skin it for hide.")) return;
        }
        if ((pp - start).magnitude > 25f)
        {
            if (Show(ui, "bag", touch ? "BAG holds your items: tap one to eat, wear or use it." : "B opens your bag: click an item to eat, wear or use it.")) return;
            if (Show(ui, "map", touch ? "MAP shows the land and your quest marker." : "M opens the map (M again closes it).")) return;
        }
    }
}
