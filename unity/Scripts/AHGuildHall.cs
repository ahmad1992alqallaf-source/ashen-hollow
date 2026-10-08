// Ashen Hollow: inside the guild hall. The Guild hall on Hollow Meadow's square has a door you can walk through
// (it uses the same room machinery as your house, AHInterior). Inside: a stone hall with long oak tables, a roaring
// hearth, iron chandeliers, and a banner for every guild of the realm hanging on the walls, the ones you belong to
// trimmed in gold and the guild you work for lit up. At the back the Guild Clerk's counter (professions), the work
// order board (its notes are your orders; the ones ready to deliver are pinned in green) and the trophy shelf, which
// fills with cups as your guild rank rises.
using System.Collections.Generic;
using UnityEngine;

public static partial class AHInterior
{
    // the guilds' banners: profession, its colour and the colour of its emblem
    static readonly string[] GuildKeys = { "smith", "tailor", "chef", "alchemist", "jeweler", "farmer", "miner", "fisher", "woodcutter" };
    static readonly int[] GuildCol = { 0x8a2a22, 0x5a3a8a, 0xb8862f, 0x2f7a5a, 0x2a5a9a, 0x6a8a2a, 0x5a5550, 0x2a6a8a, 0x6a4a2a };
    static readonly int[] GuildMark = { 0xd9d2c4, 0xf0d070, 0xf4ecd8, 0xb8f0c0, 0xa8e8ff, 0xf0e090, 0xe8b070, 0xd8f4ff, 0x9ad070 };
    // where they hang (x, z on the hall's floor plan) and which way they face into the hall (yaw)
    static readonly Vector3[] BannerAt =
    {
        new Vector3(-7.85f, -5f, 90f), new Vector3(-7.85f, -3f, 90f), new Vector3(-7.85f, 3f, 90f), new Vector3(-7.85f, 5f, 90f),
        new Vector3(7.85f, -5f, -90f), new Vector3(7.85f, -3f, -90f), new Vector3(7.85f, 3f, -90f), new Vector3(7.85f, 5f, -90f),
        new Vector3(-5.5f, -6.85f, 0f),
    };

    public static void EnterGuild(AHGame g, bool asPreview = false)
    {
        var p = g != null ? g.player : null; if (p == null || Inside) return;
        if (p.mounted) AHComp.Dismount(g, true);
        guild = true; W = 16f; D = 14f; H = 5f;
        GoIn(g, asPreview);
        AHChat.Add("system", p.profMain == null ? "You step into the guild hall. The clerk at the back can sign you up to a guild." : "You step into the guild hall. Your guild's banner hangs lit on the wall.");
    }

    static IEnumerable<Rect> GuildBlocks()
    {
        foreach (float x in new[] { -3f, 3f }) yield return R(x, 0f, 2.4f, 5.6f);   // the long tables and their benches
        yield return R(0f, D / 2 - 2.1f, 3.4f, 1f);                                  // the clerk's counter
        yield return R(-W / 2 + 0.5f, 0f, 1.1f, 3f);                                 // the hearth
        yield return R(W / 2 - 1.6f, D / 2 - 0.4f, 2.6f, 0.8f);                      // the trophy shelf
        foreach (float sx in new[] { -1f, 1f }) foreach (float z in new[] { -1.5f, 1.5f }) yield return R(sx * (W / 2 - 0.35f), z, 0.7f, 0.7f);   // pillars
    }

    static void BuildGuild(AHGame g)
    {
        var p = g.player;
        root = new GameObject("Guild hall interior").transform; root.position = c;
        var shell = new GameObject("Shell"); shell.transform.SetParent(root, false); shell.transform.localScale = new Vector3(80f, 40f, 80f); shell.transform.localPosition = new Vector3(0, 8f, 0);
        shell.AddComponent<MeshFilter>().sharedMesh = InsideOutCube(); shell.AddComponent<MeshRenderer>().sharedMaterial = Glow(new Color(0.04f, 0.035f, 0.03f));

        // the floor: big stone flags, each a slightly different grey
        var rnd = new System.Random(11);
        for (int i = 0; i < 8; i++) for (int j = 0; j < 7; j++)
        {
            int[] tones = { 0x7d776d, 0x857e73, 0x766f66, 0x8c867a };
            Box(root, new Vector3(-W / 2 + 1f + i * 2f, -0.05f, -D / 2 + 1f + j * 2f), new Vector3(1.96f, 0.1f, 1.96f), tones[rnd.Next(tones.Length)]);
        }
        Box(root, new Vector3(0f, -0.08f, 0f), new Vector3(W, 0.05f, D), 0x3a342c);   // dark joints between the flags
        // a long red runner from the door to the counter
        Box(root, new Vector3(0f, 0.01f, -0.6f), new Vector3(1.6f, 0.02f, D - 3.4f), Red); Box(root, new Vector3(0f, 0.02f, -0.6f), new Vector3(1.3f, 0.02f, D - 3.6f), 0xa83a30);

        Wall(new Vector3(0, 0, D / 2), new Vector3(W, H, 0.3f), Vector3.forward, false);
        Wall(new Vector3(0, 0, -D / 2), new Vector3(W, H, 0.3f), Vector3.back, true);
        Wall(new Vector3(W / 2, 0, 0), new Vector3(0.3f, H, D), Vector3.right, false);
        Wall(new Vector3(-W / 2, 0, 0), new Vector3(0.3f, H, D), Vector3.left, false);
        foreach (var x in new[] { -W / 2, W / 2 }) foreach (var z in new[] { -D / 2, D / 2 }) Box(root, new Vector3(x, H / 2, z), new Vector3(0.5f, H, 0.5f), Stone);
        // stone pillars along the side walls, with oak beams across the top
        foreach (float sx in new[] { -1f, 1f }) foreach (float z in new[] { -1.5f, 1.5f })
        {
            Box(root, new Vector3(sx * (W / 2 - 0.35f), H / 2, z), new Vector3(0.6f, H, 0.6f), Stone);
            Box(root, new Vector3(sx * (W / 2 - 0.35f), 0.15f, z), new Vector3(0.8f, 0.3f, 0.8f), 0x6a645a);
        }
        foreach (float z in new[] { -1.5f, 1.5f }) Box(root, new Vector3(0f, H - 0.2f, z), new Vector3(W, 0.3f, 0.35f), DarkOak);

        // the long tables, benches, and candles down their middle
        foreach (float x in new[] { -3f, 3f })
        {
            var t = Group("Table", new Vector3(x, 0f, 0f));
            Box(t, new Vector3(0, 0.78f, 0), new Vector3(1.1f, 0.08f, 5.2f), Oak);
            foreach (float z in new[] { -2.2f, 2.2f }) foreach (float lx in new[] { -0.4f, 0.4f }) Box(t, new Vector3(lx, 0.38f, z), new Vector3(0.1f, 0.76f, 0.1f), DarkOak);
            foreach (float bx in new[] { -0.95f, 0.95f }) { Box(t, new Vector3(bx, 0.45f, 0), new Vector3(0.35f, 0.07f, 5f), Oak); foreach (float z in new[] { -2f, 0f, 2f }) Box(t, new Vector3(bx, 0.22f, z), new Vector3(0.08f, 0.44f, 0.08f), DarkOak); }
            foreach (float z in new[] { -1.6f, 0f, 1.6f })
            {
                Box(t, new Vector3(0, 0.88f, z), new Vector3(0.07f, 0.12f, 0.07f), 0xf2ead2, 0f, PrimitiveType.Cylinder);
                var fl = Box(t, new Vector3(0, 1.0f, z), new Vector3(0.045f, 0.075f, 0.045f), 0, 0f, PrimitiveType.Sphere); fl.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 0.8f, 0.35f));
            }
            // tankards and a platter
            Box(t, new Vector3(0.3f, 0.9f, -1f), new Vector3(0.12f, 0.16f, 0.12f), 0x8a8a8a, 0f, PrimitiveType.Cylinder); Box(t, new Vector3(-0.3f, 0.9f, 0.8f), new Vector3(0.12f, 0.16f, 0.12f), 0x8a8a8a, 0f, PrimitiveType.Cylinder);
            Box(t, new Vector3(0f, 0.83f, 0.8f), new Vector3(0.5f, 0.02f, 0.5f), 0xb0a080, 0f, PrimitiveType.Cylinder);
            Lamp(t, new Vector3(0, 1.4f, 0), new Color(1f, 0.75f, 0.45f), 5f, 0.7f);
        }

        // the hearth, on the left wall
        {
            var t = Group("Hearth", new Vector3(-W / 2 + 0.55f, 0f, 0f)); t.localRotation = Quaternion.Euler(0f, -90f, 0f);   // its open front (-z) faces into the hall
            Box(t, new Vector3(0, 0.9f, 0), new Vector3(2.8f, 1.8f, 1f), Stone); Box(t, new Vector3(0, 1.85f, -0.05f), new Vector3(3f, 0.16f, 1.1f), DarkOak);
            Box(t, new Vector3(0, 3.4f, 0.15f), new Vector3(1.8f, 3.1f, 0.7f), Stone);
            Box(t, new Vector3(0, 0.65f, -0.48f), new Vector3(1.6f, 1.1f, 0.1f), 0x1a1410);
            var fire = Box(t, new Vector3(0, 0.45f, -0.52f), new Vector3(1.1f, 0.6f, 0.1f), 0, 0f, PrimitiveType.Sphere); fire.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 0.55f, 0.15f));
            var fire2 = Box(t, new Vector3(0.15f, 0.7f, -0.53f), new Vector3(0.5f, 0.55f, 0.08f), 0, 0f, PrimitiveType.Sphere); fire2.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 0.82f, 0.4f));
            for (int i = 0; i < 4; i++) Box(t, new Vector3(-0.45f + i * 0.3f, 0.2f, -0.55f), new Vector3(0.14f, 0.3f, 0.14f), DarkOak, 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 25f * (i - 1.5f), 0f);
            // a shield and crossed swords over the mantel
            Box(t, new Vector3(0, 2.75f, -0.22f), new Vector3(0.75f, 0.06f, 0.75f), 0x7a1f1a, 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box(t, new Vector3(0, 2.75f, -0.26f), new Vector3(0.3f, 0.04f, 0.3f), Gold, 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (float a in new[] { -40f, 40f }) Box(t, new Vector3(0, 2.75f, -0.2f), new Vector3(0.06f, 1.4f, 0.02f), 0xc8ccd2).localRotation = Quaternion.Euler(0f, 0f, a);
            var lg = Lamp(t, new Vector3(0, 0.9f, -1.3f), new Color(1f, 0.55f, 0.25f), 11f, 2.4f); lg.AddComponent<AHFlicker>();
        }

        // iron chandeliers
        foreach (float z in new[] { -3.5f, 2.5f })
        {
            var t = Group("Chandelier", new Vector3(0f, H - 1.1f, z));
            Box(t, Vector3.zero, new Vector3(1.8f, 0.05f, 1.8f), 0x2a2622, 0f, PrimitiveType.Cylinder);
            Box(t, new Vector3(0, 0.6f, 0), new Vector3(0.04f, 1.2f, 0.04f), 0x2a2622);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad; Vector3 at = new Vector3(Mathf.Cos(a) * 0.85f, 0.08f, Mathf.Sin(a) * 0.85f);
                Box(t, at, new Vector3(0.05f, 0.1f, 0.05f), 0xf2ead2, 0f, PrimitiveType.Cylinder);
                var fl = Box(t, at + Vector3.up * 0.1f, new Vector3(0.04f, 0.07f, 0.04f), 0, 0f, PrimitiveType.Sphere); fl.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 0.82f, 0.4f));
            }
            Lamp(t, new Vector3(0, -0.3f, 0), new Color(1f, 0.8f, 0.55f), 11f, 1.5f);
        }

        // the banners of the guilds
        for (int i = 0; i < GuildKeys.Length && i < BannerAt.Length; i++)
        {
            string k = GuildKeys[i]; bool member = p.profs != null && p.profs.Contains(k), focus = p.profMain == k;
            var t = Group("Banner " + k, new Vector3(BannerAt[i].x, 0f, BannerAt[i].y)); t.localRotation = Quaternion.Euler(0f, BannerAt[i].z, 0f);
            Box(t, new Vector3(0, 3.3f, 0.05f), new Vector3(1.4f, 0.07f, 0.07f), DarkOak);   // the pole
            Box(t, new Vector3(0, 2.15f, 0.08f), new Vector3(1.15f, 2.2f, 0.04f), GuildCol[i]);
            // a notched end: two dark triangles cut the bottom
            foreach (float s in new[] { -1f, 1f }) Box(t, new Vector3(s * 0.29f, 1.05f, 0.08f), new Vector3(0.4f, 0.4f, 0.045f), GuildCol[i]).localRotation = Quaternion.Euler(0f, 0f, 45f);
            // the emblem: a ring with a shape in it
            // (the hall side of the cloth is +z: the emblem sits in front of it)
            Box(t, new Vector3(0, 2.35f, 0.11f), new Vector3(0.62f, 0.02f, 0.62f), GuildMark[i], 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box(t, new Vector3(0, 2.35f, 0.12f), new Vector3(0.48f, 0.02f, 0.48f), GuildCol[i], 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box(t, new Vector3(0, 2.35f, 0.13f), new Vector3(0.24f, 0.24f, 0.02f), GuildMark[i]).localRotation = Quaternion.Euler(0f, 0f, 45f + i * 10f);
            if (member) { Box(t, new Vector3(0, 3.22f, 0.11f), new Vector3(1.19f, 0.08f, 0.04f), Gold); foreach (float s in new[] { -1f, 1f }) Box(t, new Vector3(s * 0.585f, 2.15f, 0.11f), new Vector3(0.04f, 2.2f, 0.04f), Gold); }
            if (focus) Lamp(t, new Vector3(0, 2.4f, 1.2f), new Color(1f, 0.85f, 0.55f), 4.5f, 2.2f);
            // it hangs on its wall, and steps aside with it when the wall is between you and the camera
            Vector3 outw = Quaternion.Euler(0f, BannerAt[i].z, 0f) * Vector3.back;
            foreach (var w in walls) if (Vector3.Dot(w.Value, outw) > 0.9f) { t.SetParent(w.Key, true); break; }
        }

        // the clerk's counter, with a ledger, a quill and a bell
        {
            var t = Group("Counter", new Vector3(0f, 0f, D / 2 - 2.1f));
            Box(t, new Vector3(0, 0.55f, 0), new Vector3(3.2f, 1.1f, 0.8f), DarkOak); Box(t, new Vector3(0, 1.12f, 0), new Vector3(3.4f, 0.06f, 0.95f), Oak);
            foreach (float x in new[] { -1f, 0f, 1f }) Box(t, new Vector3(x, 0.55f, -0.41f), new Vector3(0.7f, 0.8f, 0.02f), Oak);
            Box(t, new Vector3(-0.5f, 1.17f, 0f), new Vector3(0.6f, 0.05f, 0.45f), 0xe8dcc0); Box(t, new Vector3(-0.5f, 1.18f, 0f), new Vector3(0.02f, 0.06f, 0.45f), 0x6a4a2a);
            Box(t, new Vector3(-0.1f, 1.22f, 0.1f), new Vector3(0.02f, 0.2f, 0.02f), 0xf0f0f0).localRotation = Quaternion.Euler(0f, 0f, 30f);
            Box(t, new Vector3(0.8f, 1.2f, 0f), new Vector3(0.14f, 0.1f, 0.14f), Gold, 0f, PrimitiveType.Sphere);
            // the guild's great banner on the wall behind: gold on deep blue
            var b = Group("Great banner", new Vector3(0f, 0f, D / 2 - 0.2f));
            Box(b, new Vector3(0, 2.9f, 0), new Vector3(2.4f, 3.2f, 0.05f), 0x1f2f5a); Box(b, new Vector3(0, 4.55f, 0), new Vector3(2.8f, 0.09f, 0.09f), DarkOak);
            Box(b, new Vector3(0, 3.1f, -0.03f), new Vector3(1.2f, 0.03f, 1.2f), Gold, 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box(b, new Vector3(0, 3.1f, -0.05f), new Vector3(0.95f, 0.03f, 0.95f), 0x1f2f5a, 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(90f, 0f, 0f);
            Box(b, new Vector3(0, 3.2f, -0.07f), new Vector3(0.12f, 0.6f, 0.03f), Gold); Box(b, new Vector3(0, 3.45f, -0.07f), new Vector3(0.45f, 0.18f, 0.03f), Gold);   // a hammer
            Box(b, new Vector3(0, 1.55f, -0.03f), new Vector3(2.4f, 0.12f, 0.04f), Gold);
            Spot(g, "Guild Clerk", t.position + new Vector3(0f, 0f, -0.9f), () => { if (preview) g.ui.Toast("The Guild Clerk: professions and specialisations."); else g.ui.OpenProf(); });
        }

        // the work order board: one note for each order you hold, the ready ones pinned in green
        {
            var t = Group("Order board", new Vector3(-4.5f, 0f, D / 2 - 0.2f));
            Box(t, new Vector3(0, 1.75f, 0), new Vector3(2.4f, 1.6f, 0.08f), 0x8a6a42); Box(t, new Vector3(0, 1.75f, 0.02f), new Vector3(2.6f, 1.8f, 0.06f), DarkOak);
            foreach (float x in new[] { -1.15f, 1.15f }) Box(t, new Vector3(x, 0.85f, 0f), new Vector3(0.1f, 1.7f, 0.1f), DarkOak);
            int n = p.orders != null ? Mathf.Min(p.orders.Count, 8) : 0; if (preview && n == 0) n = 5;
            for (int i = 0; i < n; i++)
            {
                float x = -0.85f + (i % 4) * 0.56f, y = 2.15f - (i / 4) * 0.75f; var r2 = new System.Random(i * 7 + 3);
                Box(t, new Vector3(x, y, -0.06f), new Vector3(0.42f, 0.55f, 0.01f), 0xe8dcc0).localRotation = Quaternion.Euler(0f, 0f, (float)(r2.NextDouble() - 0.5) * 10f);
                for (int l = 0; l < 4; l++) Box(t, new Vector3(x, y + 0.15f - l * 0.09f, -0.067f), new Vector3(0.3f, 0.015f, 0.005f), 0x5a4a3a);
                bool ready = !preview && OrderReady(p, i);
                Box(t, new Vector3(x, y + 0.24f, -0.075f), new Vector3(0.05f, 0.05f, 0.05f), ready ? 0x3ac25a : 0xb03030, 0f, PrimitiveType.Sphere);
            }
            Spot(g, "Work orders", t.position + new Vector3(0f, 0f, -0.9f), () => { if (preview) g.ui.Toast("Your guild's work orders."); else g.ui.OpenOrders(); });
        }

        // the trophy shelf: a gold cup for each rank you have reached, and silver plates for every 10 orders delivered
        {
            var t = Group("Trophies", new Vector3(W / 2 - 1.6f, 0f, D / 2 - 0.4f));
            Box(t, new Vector3(0, 1.3f, 0), new Vector3(2.6f, 2.6f, 0.5f), DarkOak);
            for (int sh = 0; sh < 3; sh++) Box(t, new Vector3(0, 0.45f + sh * 0.8f, -0.04f), new Vector3(2.45f, 0.05f, 0.44f), Oak);
            int rank = AHOrders.Rank(p), cups = preview ? 5 : rank + 1, plates = preview ? 6 : Mathf.Min(10, p.ordersDone / 10);
            for (int i = 0; i < cups; i++)
            {
                var cup = Group("Cup", Vector3.zero); cup.SetParent(t, false); cup.localPosition = new Vector3(-0.9f + i * 0.45f, 2.1f, -0.05f);
                Box(cup, new Vector3(0, 0.03f, 0), new Vector3(0.18f, 0.03f, 0.18f), Gold, 0f, PrimitiveType.Cylinder);
                Box(cup, new Vector3(0, 0.12f, 0), new Vector3(0.05f, 0.08f, 0.05f), Gold, 0f, PrimitiveType.Cylinder);
                Box(cup, new Vector3(0, 0.27f, 0), new Vector3(0.2f, 0.18f, 0.2f), Gold, 0f, PrimitiveType.Sphere);
                Box(cup, new Vector3(0, 0.34f, 0), new Vector3(0.21f, 0.04f, 0.21f), Gold, 0f, PrimitiveType.Cylinder);
                foreach (float s in new[] { -1f, 1f }) Box(cup, new Vector3(s * 0.13f, 0.27f, 0), new Vector3(0.04f, 0.12f, 0.08f), Gold);
            }
            for (int i = 0; i < plates; i++) Box(t, new Vector3(-1f + (i % 5) * 0.5f, 1.45f - (i / 5) * 0.8f, -0.12f), new Vector3(0.32f, 0.03f, 0.32f), 0xc0c4c8, 0f, PrimitiveType.Cylinder).localRotation = Quaternion.Euler(70f, 0f, 0f);
            if (cups >= 5) Lamp(t, new Vector3(0, 2.4f, -0.8f), new Color(1f, 0.85f, 0.5f), 3f, 1.4f);
            Spot(g, "Guild standing", t.position + new Vector3(0f, 0f, -1f), () =>
                g.ui.Toast(preview ? "Your guild rank and delivered orders show here." : "Guild rank: " + AHOrders.RankName[AHOrders.Rank(p)] + " · " + p.ordersDone + " orders delivered · " + p.guildRep + " standing", 4f));
        }

        // the double door, and the way out
        var door = Group("Door", new Vector3(0f, 0f, -D / 2 + 0.17f));
        foreach (var w in walls) if (w.Value == Vector3.back) door.SetParent(w.Key, true);
        foreach (float s in new[] { -0.55f, 0.55f }) { Box(door, new Vector3(s, 1.5f, 0), new Vector3(1.08f, 3f, 0.12f), DarkOak); Box(door, new Vector3(s * 0.25f, 1.4f, -0.08f), new Vector3(0.1f, 0.25f, 0.06f), Gold); for (int b = 0; b < 3; b++) Box(door, new Vector3(s, 0.5f + b * 1f, -0.07f), new Vector3(1f, 0.08f, 0.03f), 0x2a2622); }
        Box(root, new Vector3(0f, 0.015f, -D / 2 + 0.7f), new Vector3(2.2f, 0.03f, 0.8f), 0x6a5030);
        Spot(g, "Leave guild hall", door.position + Vector3.forward * 0.4f, () => Leave(g));
        Tick(g);
    }

    static bool OrderReady(AHPlayer p, int i)
    {
        try { return AHOrders.Ready(p, p.orders[i]); } catch { return false; }
    }
}
