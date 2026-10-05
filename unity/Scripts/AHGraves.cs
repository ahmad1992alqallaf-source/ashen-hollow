// Ashen Hollow: the graves of the Fossil Lands, as in the web game (v66, fossilConsider / fossilDig / fossilTick).
// Dig a grave for coins, bone dust and sometimes an ancient relic; it may wake the one inside (more often at night).
// A dug grave fills back in after 5 minutes.
using System.Collections.Generic;
using UnityEngine;

public static class AHGraves
{
    public static void Setup(AHGame g)
    {
        if (AHGame.AreaId != "fossil") return;
        var obst = AHDB.List("world", "obst"); if (obst == null) return;
        float S = AHDB.S;
        foreach (var o in obst)
        {
            if (!AHJson.B(o, "grave")) continue;
            Vector3 at = g.W((float)AHJson.N(o, "x") * S, (float)AHJson.N(o, "y") * S);
            if (!g.InArea(at)) continue;
            AHSpot spot = null; float until = 0f;
            spot = new AHSpot { kind = "use", name = "Dig grave", pos = at, r = 0.5f, reach = 2.4f };
            spot.use = () =>
            {
                if (Time.time < until) { g.ui.Toast("This grave was dug recently. The earth is still loose."); return; }
                until = Time.time + 300f;
                var p = g.player;
                long c = 60 + Random.Range(0, 140); p.AddMoney(c, at);
                int dust = 1 + Random.Range(0, 3); p.bag.Add("bone_dust", dust);
                string msg = "You dig up " + dust + " bone dust";
                if (Random.value < 0.1f && p.bag.Add("ancient_relic")) msg += " and an ancient relic!"; else msg += ".";
                g.ui.Toast(msg, 3f);
                AHFx.Pop(at + Vector3.up * 0.3f, 1.4f, new Color(0.75f, 0.68f, 0.52f));
                g.quests.Event("gather", "bone_dust", dust, g);
                if (Random.value < (g.IsNight ? 0.6f : 0.35f))
                {
                    string type = g.IsNight ? "fnight" : "fskel";
                    AHMobType t = null; foreach (var m0 in g.mobs) if (m0.type.id == type) { t = m0.type; break; }
                    if (t == null) foreach (var m0 in g.mobs) if (m0.type.id == "fskel") { t = m0.type; break; }
                    if (t != null)
                    {
                        int n = Random.value < 0.3f ? 2 : 1;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 sp = g.Resolve(at + new Vector3(Mathf.Cos(i * 2.5f + 1f), 0, Mathf.Sin(i * 2.5f + 1f)) * 2f, t.radius);
                            var m = AHMob.Create(g, t, sp); m.add = true; m.Engage(); g.mobs.Add(m);
                            AHFx.Pop(sp + Vector3.up * 0.4f, 1.6f, new Color(0.75f, 0.68f, 0.52f));
                        }
                        g.ui.Toast("Something stirs in the grave…", 3f);
                    }
                }
                g.MarkDirty();
            };
            AHGather.Spots.Add(spot);
        }
    }
}
