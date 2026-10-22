// Ashen Hollow: fishing, World of Warcraft style.
//   Cast   - the hero casts; the line flies out and a red-and-white bobber lands on the water.
//   Wait   - it floats and bobs. A few seconds to twenty (less with skill, in a school, with a lure, in the rain).
//   Bite   - it dips under with a splash and a plop: tap the big button quickly (the button says HOOK!).
//            Too slow and the fish gets away. Every time, the hero casts again.
//   Reel   - a Rare, Epic or Legendary fish fights: a needle sweeps a bar; tap while it is in the green to pull it in
//            (three good pulls land it; three misses and the line snaps). The green is narrower for the rarer fish.
// Which fish bites: the land's own five (AHFish), by rarity, only those your skill reaches. Schools (patches of
// rippling water that come and go on the fishing spots) bring rarer fish and quicker bites; a Glimmer lure from the
// bag does too, for ten minutes (one is put on by itself when you start fishing with one in the bag and none on).
using UnityEngine;

public static class AHFishing
{
    public enum Phase { None, Cast, Wait, Bite, Reel }
    public static Phase phase = Phase.None;
    public static bool Active { get { return phase != Phase.None; } }
    public static bool Reeling { get { return phase == Phase.Reel; } }
    static AHSpot spot; static float t, waitFor; static GameObject bobber; static Vector3 bobAt;
    static AHFish.Fish hooked; static int hookedTier;
    // the reel
    public static float needle, needleDir = 1f, zoneA, zoneW, speed; public static int hits, misses;
    public static string say; public static float sayT;
    public static bool HasBobber { get { return bobber != null; } }
    public static Vector3 BobberPos { get { return bobber != null ? bobber.transform.position + Vector3.up * 0.06f : bobAt; } }

    public static float Frac
    {
        get
        {
            switch (phase)
            {
                case Phase.Cast: return Mathf.Clamp01(t / 1f) * 0.15f;
                case Phase.Wait: return 0.15f + 0.6f * Mathf.Clamp01(t / Mathf.Max(0.1f, waitFor));
                case Phase.Bite: return 1f;
                case Phase.Reel: return hits / 3f;
            }
            return 0f;
        }
    }
    public static string Label { get { return phase == Phase.Bite ? "HOOK!" : phase == Phase.Reel ? "REEL" : "Fishing"; } }

    public static bool IsFishing(AHSpot s) { return s != null && (s.kind == "fish" || s.kind == "sea" || s.kind == "lava"); }

    public static void Begin(AHPlayer p, AHSpot s)
    {
        spot = s; hooked = null;
        // a lure goes on by itself
        long now = Now();
        if (p.prog.lureUntil < now && p.bag.Count("fishing_lure") > 0 && p.bag.Take("fishing_lure")) { p.prog.lureUntil = now + 600; AHGame.I.ui.Toast("Glimmer lure on: rarer fish bite for ten minutes.", 2.5f); }
        Cast(p);
    }
    static void Cast(AHPlayer p)
    {
        phase = Phase.Cast; t = 0f;
        // the bobber lands out on the water, away from the hero
        Vector3 out_ = spot.pos - p.transform.position; out_.y = 0f; out_ = out_.sqrMagnitude > 0.01f ? out_.normalized : p.transform.forward;
        bobAt = spot.pos + out_ * Random.Range(0.3f, 1.2f) + Quaternion.Euler(0, 90, 0) * out_ * Random.Range(-0.8f, 0.8f);
        bobAt.y = p.transform.position.y - 0.04f;
        p.BeginWork(bobAt, "Work_FishCast|Work_Fish|Fish", "rod");
        AHSound.Play("cast");
    }
    public static void Stop()
    {
        phase = Phase.None; hooked = null;
        if (bobber != null) Object.Destroy(bobber); bobber = null;
    }

    static long Now() { return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }

    // every frame while fishing (from AHGather.Update)
    public static void Tick(AHPlayer p, float dt)
    {
        if (spot == null) { Stop(); return; }
        t += dt; if (sayT > 0f) sayT -= dt;
        switch (phase)
        {
            case Phase.Cast:
                if (t >= 0.9f)
                {
                    MakeBobber(); phase = Phase.Wait; t = 0f;
                    float k = 1f + AHMastery.Rank(p, "fishing", AHMastery.FishTier(AHGame.AreaId)) * 0.12f; if (School(spot)) k *= 1.6f; if (p.prog.lureUntil > Now()) k *= 1.3f; k *= AHWeather.GatherK("fish");
                    waitFor = Random.Range(3f, 18f) / k;
                    p.BeginWork(bobAt, "Work_Fish|Fish", "rod");
                    AHGame.I.Ripple(bobAt, 0.6f);
                }
                break;
            case Phase.Wait:
                Bob(0.025f, 2.2f);
                if (t >= waitFor)
                {
                    phase = Phase.Bite; t = 0f;
                    AHSound.Play("splash"); AHGame.I.Ripple(bobAt, 1.4f);
                    AHSpark.Burst(bobAt + Vector3.up * 0.05f, new Color(0.8f, 0.92f, 1f, 0.9f), 12, 1.6f, 0.5f, 0.06f, 1.4f);
                    say = "A bite!"; sayT = 1f;
                }
                break;
            case Phase.Bite:
                if (bobber != null) bobber.transform.position = bobAt - Vector3.up * (0.12f + 0.04f * Mathf.Sin(t * 30f));
                if (t > 1.5f) { say = "It got away..."; sayT = 1.4f; Cast(p); }
                break;
            case Phase.Reel:
                Bob(0.07f, 9f);
                needle += needleDir * speed * dt;
                if (needle > 1f) { needle = 1f; needleDir = -1f; } else if (needle < 0f) { needle = 0f; needleDir = 1f; }
                break;
        }
    }
    static void Bob(float a, float f) { if (bobber != null) bobber.transform.position = bobAt + Vector3.up * Mathf.Sin(t * f) * a; }

    // the big button while fishing
    public static void Tap(AHPlayer p)
    {
        if (phase == Phase.Bite)
        {
            var land = AHFish.Of(AHGame.AreaId);
            hookedTier = RollTier(p, land); hooked = land[hookedTier];
            if (hookedTier >= 2)
            {
                phase = Phase.Reel; t = 0f; hits = 0; misses = 0; needle = 0f; needleDir = 1f;
                zoneW = hookedTier == 2 ? 0.26f : hookedTier == 3 ? 0.2f : 0.15f; zoneA = Random.Range(0.1f, 0.9f - zoneW);
                speed = hookedTier == 2 ? 1.0f : hookedTier == 3 ? 1.3f : 1.6f;
                p.BeginWork(bobAt, "Work_FishPull|Work_Fish|Fish", "rod");
                say = "It's a big one! Tap in the green"; sayT = 2f; AHSound.Play("coin");
            }
            else Land(p);
            return;
        }
        if (phase == Phase.Reel)
        {
            if (needle >= zoneA && needle <= zoneA + zoneW)
            {
                hits++; AHSound.Play("coin"); say = hits >= 3 ? "Landed!" : "Pull!"; sayT = 0.8f;
                zoneA = Random.Range(0.08f, 0.92f - zoneW); speed *= 1.12f;
                if (hits >= 3) Land(p);
            }
            else
            {
                misses++; say = misses >= 3 ? "The line snapped!" : "Slack..."; sayT = 1f;
                if (misses >= 3) { AHSound.Play("hit"); Cast(p); }
            }
            return;
        }
        if (phase == Phase.Wait) { say = "Wait for the bite..."; sayT = 0.8f; }
    }

    static int RollTier(AHPlayer p, AHFish.Fish[] land)
    {
        // no level locks: every fish of the land can bite; the rod's mark on these waters, a school, a lure and the
        // angler's specialisation make the rare ones likelier
        int rank = AHMastery.Rank(p, "fishing", AHMastery.FishTier(AHGame.AreaId));
        float[] w = { 60f, 25f, 10f, 4f, 1f };
        float k = (School(spot) ? 2f : 1f) * (p.prog.lureUntil > Now() ? 1.5f : 1f) * (p.path == "artisan" ? 1.25f : 1f) * (1f + 0.5f * rank) * (p.Spec("fisher", "angler") ? 1.2f : 1f);
        for (int i = 2; i < 5; i++) w[i] *= k;
        w[1] *= 1f + 0.25f * rank;
        float sum = 0f; for (int i = 0; i < 5; i++) sum += w[i];
        if (sum <= 0f) return 0;
        float r = Random.value * sum;
        for (int i = 0; i < 5; i++) { r -= w[i]; if (r <= 0f) return i; }
        return 0;
    }

    static void Land(AHPlayer p)
    {
        var h = hooked; hooked = null;
        if (h != null && !AHGather.GrantFish(p, spot, h.id, h.xp, hookedTier)) return;
        var gg = AHGame.I;
        if (h != null && hookedTier >= 4) { var fd = AHItems.Get(h.id); AHTrophies.AddSpecial(gg, p, "fish:" + h.id, fd != null ? fd.name : h.id, "", "fish"); }
        // after dark a glowfin may come up with the catch, shining like the moon
        if (h != null && gg != null && gg.IsNight && Random.value < 0.12f && p.bag.Add(AHFun3.Glowfin)) gg.ui.Float(p.transform.position + Vector3.up * 2.4f, AHLoc.T("+ Glowfin!", "+ سمكة مضيئة!"), new Color(0.5f, 0.9f, 1f));
        if (AHGather.CanDo(p, spot, false)) Cast(p); else AHGather.Cancel();
    }

    static void MakeBobber()
    {
        if (bobber != null) Object.Destroy(bobber);
        bobber = new GameObject("Bobber"); bobber.transform.position = bobAt;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var red = new Material(sh) { color = new Color(0.85f, 0.12f, 0.1f) }; var white = new Material(sh) { color = new Color(0.95f, 0.95f, 0.92f) };
        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(top.GetComponent<Collider>()); top.transform.SetParent(bobber.transform, false); top.transform.localScale = Vector3.one * 0.13f; top.transform.localPosition = Vector3.up * 0.05f; top.GetComponent<Renderer>().sharedMaterial = red;
        var bot = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(bot.GetComponent<Collider>()); bot.transform.SetParent(bobber.transform, false); bot.transform.localScale = new Vector3(0.13f, 0.08f, 0.13f); bot.transform.localPosition = Vector3.zero; bot.GetComponent<Renderer>().sharedMaterial = white;
        var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(tip.GetComponent<Collider>()); tip.transform.SetParent(bobber.transform, false); tip.transform.localScale = new Vector3(0.015f, 0.05f, 0.015f); tip.transform.localPosition = Vector3.up * 0.13f; tip.GetComponent<Renderer>().sharedMaterial = red;
    }

    // ---------- schools: rippling water on some fishing spots, for a few minutes at a time ----------
    static readonly System.Collections.Generic.Dictionary<AHSpot, float> schools = new System.Collections.Generic.Dictionary<AHSpot, float>();
    static float schoolTick, rippleT;
    public static bool School(AHSpot s) { float until; return s != null && schools.TryGetValue(s, out until) && Time.time < until; }
    // from the game every frame: a new school now and then, and their ripples
    public static void WorldTick(AHGame g, float dt)
    {
        schoolTick -= dt; rippleT -= dt;
        if (schoolTick <= 0f)
        {
            schoolTick = 40f;
            var list = new System.Collections.Generic.List<AHSpot>();
            foreach (var s in AHGather.Spots) if (IsFishing(s)) list.Add(s);
            if (list.Count > 0 && Random.value < 0.6f) { var s = list[Random.Range(0, list.Count)]; schools[s] = Time.time + Random.Range(120f, 240f); }
        }
        if (rippleT <= 0f)
        {
            rippleT = 0.7f;
            foreach (var kv in schools) if (Time.time < kv.Value && kv.Key != null) { g.Ripple(kv.Key.pos + new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f)), 0.5f); }
        }
    }
    public static void Clear() { schools.Clear(); Stop(); }
}
