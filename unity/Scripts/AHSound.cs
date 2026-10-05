// Ashen Hollow: sound, synthesized like the web game's (v66 Sound: Web Audio, no files). Sound effects are short tones
// and filtered noise with a quick attack and an exponential fall; the music is a small sequencer whose mood follows where
// you are and what you are doing (town, wild, night, dungeon, combat, boss); the ambience is soft wind with birds by day,
// crickets at night and a low hum underground. Everything is made sample by sample in OnAudioFilterRead.
using System;
using System.Collections.Generic;
using UnityEngine;

public class AHSound : MonoBehaviour
{
    public static AHSound I;
    public static bool Muted { get { return PlayerPrefs.GetInt("ah_muted", 0) == 1; } set { PlayerPrefs.SetInt("ah_muted", value ? 1 : 0); } }
    public static float MusicVol { get { return PlayerPrefs.GetFloat("ah_music", 0.5f); } set { PlayerPrefs.SetFloat("ah_music", value); } }
    public static float SfxVol { get { return PlayerPrefs.GetFloat("ah_sfx", 0.8f); } set { PlayerPrefs.SetFloat("ah_sfx", value); } }

    enum Wave : byte { Sine, Triangle, Square, Saw, Noise }
    enum Filt : byte { None, Low, High, Band }
    struct Voice
    {
        public bool on; public Wave w; public Filt ft; public int bus;   // 0 sfx, 1 music, 2 ambience
        public double start, len, att; public bool linAtt;
        public float f0, f1, vol, q, fq0, fq1, phase;
        public float b0, b1, b2, a1, a2, x1, x2, y1, y2; public int coefT;
    }
    const int MaxV = 96;
    readonly Voice[] voices = new Voice[MaxV];
    readonly object lk = new object();
    readonly List<Voice> pending = new List<Voice>();
    double sr = 48000; long nowS;    // the audio clock, in samples
    double Now { get { return System.Threading.Interlocked.Read(ref nowS) / sr; } }
    uint rng = 0x9e3779b9;
    float Rand() { rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5; return (rng & 0xffffff) / 8388608f - 1f; }

    // wind: noise through a low-pass, always on
    float windY, windF = 400f, windTarget = 400f;

    public static void Ensure(AHGame g)
    {
        if (I != null) return;
        var go = new GameObject("AH_Sound"); DontDestroyOnLoad(go);
        var src = go.AddComponent<AudioSource>(); src.playOnAwake = true; src.loop = true; src.spatialBlend = 0f; src.volume = 1f;
        // a silent clip so the filter runs
        src.clip = AudioClip.Create("ah_silence", 1024, 1, AudioSettings.outputSampleRate, false); src.Play();
        I = go.AddComponent<AHSound>(); I.sr = AudioSettings.outputSampleRate;
        if (FindAnyObjectByType<AudioListener>() == null && Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();
    }

    void Add(Voice v, float delay)
    {
        v.start = Now + 0.03 + delay; v.on = true;
        lock (lk) { if (pending.Count < 256) pending.Add(v); }
    }
    void Tone(Wave w, float f0, float f1, float dur, float vol, int bus = 0, float delay = 0f, float att = 0.005f, bool lin = false)
    { Add(new Voice { w = w, f0 = f0, f1 = f1 > 0 ? f1 : f0, len = dur, att = att, linAtt = lin, vol = vol, bus = bus, ft = Filt.None }, delay); }
    void Noise(float dur, float vol, Filt ft, float fq0, float fq1 = 0f, float q = 1f, int bus = 0, float delay = 0f)
    { Add(new Voice { w = Wave.Noise, ft = ft, fq0 = fq0, fq1 = fq1 > 0 ? fq1 : fq0, q = q, len = dur, att = 0.004, vol = vol, bus = bus }, delay); }

    // web SFX
    public static void Play(string n)
    {
        var s = I; if (s == null || Muted) return;
        switch (n)
        {
            case "swing": s.Noise(0.18f, 0.35f, Filt.Band, 2400, 600, 2); break;
            case "hit": s.Tone(Wave.Sine, 160, 60, 0.12f, 0.45f); s.Noise(0.08f, 0.28f, Filt.Low, 2000, 400); break;
            case "chop": s.Tone(Wave.Triangle, 240, 120, 0.08f, 0.5f); s.Noise(0.06f, 0.35f, Filt.Band, 1800, 900, 3); break;
            case "mine": s.Tone(Wave.Square, 1400, 900, 0.09f, 0.1f); s.Tone(Wave.Sine, 2600, 2000, 0.25f, 0.09f); s.Noise(0.05f, 0.3f, Filt.High, 3000); break;
            case "anvil": s.Tone(Wave.Sine, 1800, 1750, 0.5f, 0.16f); s.Tone(Wave.Sine, 2710, 2700, 0.35f, 0.07f); s.Noise(0.04f, 0.3f, Filt.High, 4000); break;
            case "fire": s.Noise(0.5f, 0.16f, Filt.Low, 900, 300); break;
            case "splash": s.Noise(0.35f, 0.35f, Filt.Band, 1400, 400, 0.8f); break;
            case "cast": s.Tone(Wave.Sine, 300, 900, 0.3f, 0.18f); s.Noise(0.3f, 0.1f, Filt.Band, 800, 3000, 2); break;
            case "boom": s.Tone(Wave.Sine, 110, 40, 0.5f, 0.6f); s.Noise(0.45f, 0.45f, Filt.Low, 1200, 100); break;
            case "heal": { float[] f = { 523, 659, 784 }; for (int i = 0; i < 3; i++) s.Tone(Wave.Sine, f[i], 0, 0.35f, 0.12f, 0, i * 0.07f); } break;
            case "level": { float[] f = { 523, 659, 784, 1046 }; for (int i = 0; i < 4; i++) s.Tone(Wave.Triangle, f[i], 0, 0.4f, 0.14f, 0, i * 0.09f); } break;
            case "hurt": s.Tone(Wave.Square, 180, 90, 0.15f, 0.1f); s.Noise(0.1f, 0.22f, Filt.Low, 800, 200); break;
            case "whoosh": s.Noise(0.3f, 0.3f, Filt.Band, 500, 2500, 1.5f); break;
            case "loot": { float[] f = { 784, 988, 1175, 1568 }; for (int i = 0; i < 4; i++) s.Tone(Wave.Sine, f[i], 0, 0.3f, 0.12f, 0, i * 0.06f); } break;
            case "step": s.Noise(0.05f, 0.05f, Filt.Low, 700, 300); break;
            case "roar": s.Tone(Wave.Saw, 95, 60, 0.9f, 0.14f); s.Noise(0.9f, 0.2f, Filt.Low, 600, 200); break;
            case "coin": { float[] f = { 1318, 1760, 2093 }; for (int i = 0; i < 3; i++) s.Tone(Wave.Square, f[i], 0, 0.1f, 0.035f, 0, i * 0.05f); } break;
            case "door": s.Tone(Wave.Saw, 210, 130, 0.4f, 0.035f); s.Noise(0.25f, 0.14f, Filt.Low, 700, 200); break;
        }
    }

    // ---------- spells: a cast sound and an impact sound for each element ----------
    public static void Spell(SpellEl el, bool impact, float delay = 0f)
    {
        var s = I; if (s == null || Muted) return;
        float d = delay, r = s.Rand();
        if (!impact)
            switch (el)
            {
                case SpellEl.Fire:     // a roaring whoosh with a low rumble under it
                    s.Noise(0.45f, 0.32f, Filt.Band, 350, 2200, 1.2f, 0, d); s.Tone(Wave.Saw, 90, 140, 0.4f, 0.06f, 0, d);
                    for (int i = 0; i < 4; i++) s.Noise(0.03f, 0.12f, Filt.High, 3500, 0, 1, 0, d + 0.08f + i * 0.07f + s.Rand() * 0.02f);
                    break;
                case SpellEl.Frost:    // icy chimes over a hiss of cold air
                    { float[] f = { 1568, 2093, 2349, 2637, 3136 }; for (int i = 0; i < 4; i++) s.Tone(Wave.Sine, f[(i * 2 + (int)(r * 3 + 3)) % 5], 0, 0.35f, 0.06f, 0, d + i * 0.05f); }
                    s.Noise(0.4f, 0.12f, Filt.High, 5000, 7000, 1, 0, d);
                    break;
                case SpellEl.Arcane:   // a rising, wavering hum
                    s.Tone(Wave.Sine, 330, 990, 0.4f, 0.13f, 0, d); s.Tone(Wave.Triangle, 335, 1000, 0.4f, 0.06f, 0, d + 0.01f);
                    s.Tone(Wave.Sine, 660, 1320, 0.25f, 0.05f, 0, d + 0.12f); s.Noise(0.35f, 0.07f, Filt.Band, 900, 3500, 3, 0, d);
                    break;
                case SpellEl.Storm:    // crackling static
                    s.Tone(Wave.Square, 70, 110, 0.35f, 0.05f, 0, d);
                    for (int i = 0; i < 6; i++) s.Noise(0.025f, 0.2f, Filt.High, 2500 + i * 300, 0, 1, 0, d + i * 0.045f + s.Rand() * 0.015f);
                    break;
                case SpellEl.Holy:     // a bright major chord swelling in, like a choir
                    { float[] f = { 523, 659, 784, 1046 }; for (int i = 0; i < 4; i++) s.Tone(Wave.Triangle, f[i], 0, 0.6f, 0.06f, 0, d + i * 0.03f, 0.12f, true); }
                    s.Tone(Wave.Sine, 2093, 0, 0.5f, 0.03f, 0, d + 0.1f, 0.1f, true);
                    break;
                case SpellEl.Heal:     // a soft rising arpeggio
                    { float[] f = { 523, 659, 784, 1046, 1318 }; for (int i = 0; i < 5; i++) s.Tone(Wave.Sine, f[i], 0, 0.45f, 0.09f, 0, d + i * 0.06f); }
                    s.Noise(0.4f, 0.05f, Filt.High, 6000, 0, 1, 0, d + 0.1f);
                    break;
                case SpellEl.Nature:   // rustling leaves and a little bird-like chirp
                    s.Noise(0.45f, 0.16f, Filt.Band, 1500, 900, 1.5f, 0, d);
                    s.Tone(Wave.Sine, 2200, 2900, 0.08f, 0.05f, 0, d + 0.06f); s.Tone(Wave.Sine, 2400, 3100, 0.07f, 0.04f, 0, d + 0.18f);
                    s.Tone(Wave.Triangle, 196, 220, 0.35f, 0.07f, 0, d);
                    break;
                case SpellEl.Star:     // twinkling falling notes
                    { float[] f = { 2637, 2349, 2093, 1760, 1568 }; for (int i = 0; i < 5; i++) s.Tone(Wave.Sine, f[i], 0, 0.3f, 0.055f, 0, d + i * 0.055f); }
                    s.Tone(Wave.Triangle, 392, 523, 0.5f, 0.05f, 0, d, 0.15f, true);
                    break;
                case SpellEl.Shadow:   // a dark swell that sucks inwards
                    s.Tone(Wave.Sine, 220, 55, 0.5f, 0.16f, 0, d, 0.3f, true); s.Noise(0.45f, 0.14f, Filt.Low, 400, 1600, 1, 0, d);
                    s.Tone(Wave.Saw, 110, 82, 0.4f, 0.03f, 0, d + 0.05f);
                    break;
                case SpellEl.Poison:   // bubbling
                    for (int i = 0; i < 6; i++) s.Tone(Wave.Sine, 260 + i * 40 + s.Rand() * 60, 520 + i * 60, 0.06f, 0.08f, 0, d + i * 0.05f);
                    s.Noise(0.3f, 0.08f, Filt.High, 4500, 0, 1, 0, d + 0.05f);
                    break;
                case SpellEl.Blade:    // a sharp swing with a ring of steel
                    s.Noise(0.16f, 0.38f, Filt.Band, 3200, 700, 2, 0, d); s.Tone(Wave.Sine, 2700 + r * 200, 2550, 0.18f, 0.035f, 0, d + 0.02f);
                    break;
                case SpellEl.Earth:    // a deep rumble
                    s.Tone(Wave.Sine, 70, 38, 0.6f, 0.4f, 0, d); s.Noise(0.6f, 0.3f, Filt.Low, 380, 120, 1, 0, d);
                    break;
                case SpellEl.Arrow:    // the string's twang and the bolt's whistle
                    s.Tone(Wave.Triangle, 210 + r * 10, 170, 0.12f, 0.2f, 0, d); s.Noise(0.05f, 0.25f, Filt.High, 2500, 0, 1, 0, d);
                    s.Noise(0.28f, 0.12f, Filt.Band, 3600, 1500, 5, 0, d + 0.03f);
                    break;
                case SpellEl.War:      // a battle shout
                    s.Tone(Wave.Saw, 150 + r * 10, 105, 0.55f, 0.11f, 0, d, 0.04f); s.Tone(Wave.Saw, 226, 158, 0.5f, 0.05f, 0, d + 0.02f, 0.04f);
                    s.Noise(0.55f, 0.18f, Filt.Band, 900, 500, 1.2f, 0, d);
                    break;
                case SpellEl.Wind:     // a rush of air
                    s.Noise(0.35f, 0.32f, Filt.Band, 500, 2600, 1.5f, 0, d); s.Noise(0.2f, 0.12f, Filt.Band, 2600, 900, 1.5f, 0, d + 0.18f);
                    break;
            }
        else
            switch (el)
            {
                case SpellEl.Fire:     // a burst and crackling flames
                    s.Tone(Wave.Sine, 120, 45, 0.4f, 0.4f, 0, d); s.Noise(0.5f, 0.38f, Filt.Low, 1800, 200, 1, 0, d);
                    for (int i = 0; i < 6; i++) s.Noise(0.025f, 0.12f, Filt.High, 3000, 0, 1, 0, d + 0.1f + i * 0.06f + s.Rand() * 0.02f);
                    break;
                case SpellEl.Frost:    // shattering ice
                    s.Noise(0.12f, 0.35f, Filt.High, 3500, 0, 1, 0, d); s.Tone(Wave.Sine, 2600, 1300, 0.2f, 0.07f, 0, d);
                    for (int i = 0; i < 5; i++) s.Tone(Wave.Sine, 3000 + s.Rand() * 900, 0, 0.12f, 0.035f, 0, d + 0.05f + i * 0.035f);
                    break;
                case SpellEl.Arcane:   // a falling zap
                    s.Tone(Wave.Sine, 1200, 180, 0.3f, 0.16f, 0, d); s.Noise(0.2f, 0.16f, Filt.Band, 2500, 500, 2, 0, d);
                    break;
                case SpellEl.Storm:    // a thunder crack
                    s.Noise(0.06f, 0.45f, Filt.High, 2000, 0, 1, 0, d); s.Tone(Wave.Saw, 110, 35, 0.6f, 0.16f, 0, d + 0.02f);
                    s.Noise(0.7f, 0.25f, Filt.Low, 900, 80, 1, 0, d + 0.04f);
                    break;
                case SpellEl.Holy:     // a ringing bell
                    s.Tone(Wave.Sine, 1046, 1040, 0.9f, 0.13f, 0, d); s.Tone(Wave.Sine, 2093, 2090, 0.6f, 0.05f, 0, d); s.Tone(Wave.Sine, 3136, 0, 0.4f, 0.025f, 0, d);
                    s.Noise(0.15f, 0.18f, Filt.Low, 1500, 300, 1, 0, d);
                    break;
                case SpellEl.Heal:
                    s.Tone(Wave.Sine, 1568, 0, 0.4f, 0.06f, 0, d); s.Tone(Wave.Sine, 2093, 0, 0.5f, 0.045f, 0, d + 0.08f);
                    break;
                case SpellEl.Nature:   // a woody thump and creaking
                    s.Tone(Wave.Triangle, 190, 110, 0.18f, 0.35f, 0, d); s.Noise(0.12f, 0.22f, Filt.Low, 900, 300, 1, 0, d);
                    s.Tone(Wave.Saw, 140, 95, 0.3f, 0.03f, 0, d + 0.08f);
                    break;
                case SpellEl.Star:     // a shimmering impact
                    s.Tone(Wave.Sine, 180, 60, 0.35f, 0.3f, 0, d); s.Noise(0.6f, 0.1f, Filt.High, 6000, 3000, 1, 0, d);
                    for (int i = 0; i < 4; i++) s.Tone(Wave.Sine, 2093 + i * 262, 0, 0.25f, 0.04f, 0, d + 0.04f + i * 0.05f);
                    break;
                case SpellEl.Shadow:   // a hollow thud
                    s.Tone(Wave.Sine, 140, 40, 0.35f, 0.35f, 0, d); s.Noise(0.25f, 0.2f, Filt.Low, 700, 150, 1, 0, d);
                    break;
                case SpellEl.Poison:   // a wet hiss
                    s.Noise(0.4f, 0.22f, Filt.High, 3800, 2200, 1, 0, d); s.Tone(Wave.Sine, 420, 180, 0.15f, 0.08f, 0, d);
                    break;
                case SpellEl.Blade:    // a hard hit with a clang
                    s.Tone(Wave.Sine, 170, 60, 0.13f, 0.5f, 0, d); s.Noise(0.09f, 0.32f, Filt.Low, 2200, 400, 1, 0, d);
                    s.Tone(Wave.Square, 1250, 1180, 0.12f, 0.03f, 0, d);
                    break;
                case SpellEl.Earth:    // the ground breaking
                    s.Tone(Wave.Sine, 90, 30, 0.7f, 0.6f, 0, d); s.Noise(0.7f, 0.45f, Filt.Low, 1400, 90, 1, 0, d);
                    for (int i = 0; i < 4; i++) s.Noise(0.04f, 0.14f, Filt.Band, 1500, 0, 2, 0, d + 0.12f + i * 0.09f);
                    break;
                case SpellEl.Arrow:    // a thunk into the target
                    s.Tone(Wave.Sine, 320, 110, 0.09f, 0.35f, 0, d); s.Noise(0.06f, 0.25f, Filt.Band, 1600, 600, 2, 0, d);
                    break;
                case SpellEl.War:
                    s.Tone(Wave.Sine, 110, 50, 0.3f, 0.35f, 0, d); s.Noise(0.25f, 0.25f, Filt.Low, 1200, 200, 1, 0, d);
                    break;
                case SpellEl.Wind:
                    s.Tone(Wave.Sine, 160, 60, 0.15f, 0.4f, 0, d); s.Noise(0.12f, 0.3f, Filt.Low, 1800, 300, 1, 0, d);
                    break;
            }
    }

    // ---------- music (web MZ / mstep) ----------
    class Mood { public float bpm, root; public int[][] prog; public int[] scale, arp; public int arpEvery; public float lead; public int bass, perc; public Wave wave; public bool drone, stab; }
    static readonly Dictionary<string, Mood> MZ = new Dictionary<string, Mood>
    {
        { "town", new Mood { bpm = 96, root = 261.63f, prog = new[] { new[] { 0, 4, 7 }, new[] { 5, 9, 12 }, new[] { 7, 11, 14 }, new[] { -3, 0, 4 } }, scale = new[] { 0, 2, 4, 5, 7, 9, 11 }, arp = new[] { 0, 1, 2, 1, 0, 2, 1, 2 }, arpEvery = 2, lead = 0.3f, bass = 1, perc = 1, wave = Wave.Triangle } },
        { "wild", new Mood { bpm = 80, root = 220, prog = new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { 3, 7, 10 }, new[] { -2, 2, 5 } }, scale = new[] { 0, 3, 5, 7, 10 }, arp = new[] { 0, 2, 1, 2 }, arpEvery = 4, lead = 0.28f, bass = 1, perc = 0, wave = Wave.Sine } },
        { "night", new Mood { bpm = 66, root = 146.83f, prog = new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { 3, 7, 10 }, new[] { -2, 2, 5 } }, scale = new[] { 0, 2, 3, 7, 8 }, arp = null, lead = 0.16f, bass = 1, perc = 0, wave = Wave.Sine } },
        { "dungeon", new Mood { bpm = 70, root = 164.81f, prog = new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { -7, -4, 0 }, new[] { -5, -1, 2 } }, scale = new[] { 0, 1, 3, 7, 8 }, arp = new[] { 0, 1, 2, 1 }, arpEvery = 4, lead = 0.1f, bass = 2, perc = 0, wave = Wave.Triangle, drone = true } },
        { "combat", new Mood { bpm = 132, root = 220, prog = new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { -2, 2, 5 }, new[] { -5, -1, 2 } }, scale = new[] { 0, 2, 3, 5, 7, 8, 10 }, arp = new[] { 0, 1, 2, 1, 2, 1, 0, 1 }, arpEvery = 1, lead = 0.2f, bass = 3, perc = 2, wave = Wave.Square } },
        { "boss", new Mood { bpm = 144, root = 146.83f, prog = new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { -7, -4, 0 }, new[] { -5, -1, 2 } }, scale = new[] { 0, 1, 3, 5, 7, 8, 10 }, arp = new[] { 0, 2, 1, 2, 0, 2, 1, 2 }, arpEvery = 1, lead = 0.12f, bass = 3, perc = 3, wave = Wave.Saw, stab = true } },
    };
    public string musicMode = "wild", ambMode = "day";
    int mStep, leadI = 2; double mNext;
    static float Hz(float root, int semi) { return root * Mathf.Pow(2f, semi / 12f); }
    void MNote(Wave w, float f, float at, float dur, float vol, float att = 0.01f) { Add(new Voice { w = w, f0 = f, f1 = f, len = Mathf.Max(att + 0.02f, dur), att = att, linAtt = true, vol = vol, bus = 1 }, at); }
    void MNoise(float at, float dur, float vol, Filt ft, float f) { Add(new Voice { w = Wave.Noise, ft = ft, fq0 = f, fq1 = f, q = 1, len = dur, att = 0.002, vol = vol, bus = 1 }, at); }
    void MStep(int i, float at)
    {
        Mood M; if (!MZ.TryGetValue(musicMode, out M)) M = MZ["wild"];
        float sd = 60f / M.bpm / 2f; int within = i % 16; var ch = M.prog[(i / 16) % 4];
        if (within == 0) { foreach (var n in ch) MNote(Wave.Triangle, Hz(M.root, n) / 2f, at, sd * 16, 0.05f, sd * 3); if (M.drone) MNote(Wave.Sine, Hz(M.root, ch[0]) / 4f, at, sd * 16, 0.07f, sd * 4); }
        if (M.bass == 1 && within % 8 == 0) MNote(Wave.Sine, Hz(M.root, ch[0]) / 4f, at, sd * 6, 0.12f, 0.02f);
        if (M.bass == 2 && within % 4 == 0) MNote(Wave.Sine, Hz(M.root, within % 8 != 0 ? ch[2] - 12 : ch[0]) / 4f, at, sd * 3, 0.1f, 0.02f);
        if (M.bass == 3 && within % 2 == 0) MNote(Wave.Triangle, Hz(M.root, within % 4 != 0 ? ch[2] - 12 : ch[0]) / 4f, at, sd * 1.6f, 0.13f, 0.005f);
        if (M.arp != null && within % M.arpEvery == 0) MNote(M.wave, Hz(M.root, ch[M.arp[(within / M.arpEvery) % M.arp.Length]] + 12), at, sd * 1.4f, M.wave == Wave.Saw || M.wave == Wave.Square ? 0.018f : 0.04f, 0.005f);
        if (M.lead > 0 && within % 2 == 0 && UnityEngine.Random.value < M.lead)
        {
            leadI = Mathf.Clamp(leadI + UnityEngine.Random.Range(-2, 3), 0, M.scale.Length * 2 - 1);
            int sm = M.scale[leadI % M.scale.Length] + 12 * (leadI / M.scale.Length);
            MNote(Wave.Sine, Hz(M.root, sm + 12), at, sd * (2 + UnityEngine.Random.Range(0, 3)), 0.045f, 0.03f);
        }
        if (M.perc >= 1 && within % 4 == 0) MNoise(at, 0.05f, M.perc >= 2 ? 0.05f : 0.025f, Filt.High, 6000);
        if (M.perc >= 2) { if (within % 4 == 0) MNote(Wave.Sine, 110, at, 0.18f, 0.28f, 0.003f); if (within % 8 == 4) MNoise(at, 0.14f, 0.12f, Filt.Band, 1800); if (within % 2 == 1) MNoise(at, 0.03f, 0.03f, Filt.High, 8000); }
        if (M.perc >= 3 && within % 8 == 6) MNote(Wave.Sine, 90, at, 0.2f, 0.25f, 0.003f);
        if (M.stab && within % 8 == 0) foreach (var n in ch) MNote(Wave.Saw, Hz(M.root, n), at, sd * 1.2f, 0.025f, 0.005f);
    }

    float ambT, stepT;
    volatile float cSfx = 0.8f, cMus = 0.5f; volatile bool cMuted;   // read by the audio thread (PlayerPrefs is main-thread only)
    void Update()
    {
        cSfx = SfxVol; cMus = MusicVol; cMuted = Muted;
        var g = AHGame.I; if (g == null || g.player == null) return;
        var p = g.player;
        // the mood: where you are and what you are doing (web musicFor)
        bool boss = false, fight = false;
        foreach (var m in g.mobs) if (m != null && m.Chasing && (m.transform.position - p.transform.position).sqrMagnitude < 900f) { fight = true; if (m.type.elite && m.type.hp > 3000) boss = true; }
        musicMode = boss ? "boss" : fight ? "combat" : g.Dark ? "dungeon" : g.InTown(p.transform.position) ? "town" : g.IsNight ? "night" : "wild";
        ambMode = g.Dark ? "dungeon" : g.IsNight ? "night" : "day";
        windTarget = ambMode == "dungeon" ? 150f : 400f;
        if (Muted) { mNext = Now + 0.2; return; }
        // the sequencer runs a quarter of a second ahead
        double t = Now;
        if (mNext < t) mNext = t + 0.05;
        Mood M; if (!MZ.TryGetValue(musicMode, out M)) M = MZ["wild"];
        while (mNext < t + 0.25) { MStep(mStep, (float)(mNext - t) - 0.03f); mNext += 60.0 / M.bpm / 2.0; mStep++; }
        // web ambTick
        ambT -= Time.unscaledDeltaTime;
        if (ambT <= 0f)
        {
            ambT = 0.7f;
            if (ambMode == "day" && UnityEngine.Random.value < 0.35f) { float b = 2200 + UnityEngine.Random.value * 1500; for (int i = 0; i < 3; i++) Tone(Wave.Sine, b, b * 1.3f, 0.08f, 0.04f, 2, i * 0.11f); }
            if (ambMode == "night" && UnityEngine.Random.value < 0.7f) for (int i = 0; i < 4; i++) Tone(Wave.Square, 4200, 4100, 0.03f, 0.012f, 2, i * 0.06f);
            if (ambMode == "dungeon" && UnityEngine.Random.value < 0.3f) Tone(Wave.Sine, 55, 45, 1.2f, 0.1f, 2);
        }
        // footsteps
        stepT -= Time.deltaTime;
        if (p.Moving && stepT <= 0f) { stepT = p.mounted ? 0.28f : 0.36f; Play("step"); }
    }

    // ---------- the synthesizer ----------
    void Coef(ref Voice v, float f)
    {
        f = Mathf.Clamp(f, 20f, (float)sr * 0.45f);
        double w0 = 2 * Math.PI * f / sr, cs = Math.Cos(w0), al = Math.Sin(w0) / (2 * Math.Max(0.1f, v.q));
        double b0, b1, b2, a0 = 1 + al, a1 = -2 * cs, a2 = 1 - al;
        switch (v.ft)
        {
            case Filt.Low: b0 = (1 - cs) / 2; b1 = 1 - cs; b2 = (1 - cs) / 2; break;
            case Filt.High: b0 = (1 + cs) / 2; b1 = -(1 + cs); b2 = (1 + cs) / 2; break;
            default: b0 = al; b1 = 0; b2 = -al; break;
        }
        v.b0 = (float)(b0 / a0); v.b1 = (float)(b1 / a0); v.b2 = (float)(b2 / a0); v.a1 = (float)(a1 / a0); v.a2 = (float)(a2 / a0);
    }

    void OnAudioFilterRead(float[] data, int ch)
    {
        int n = data.Length / ch; double dt = 1.0 / sr;
        float sfx = cSfx * 0.8f, mus = cMus * 0.8f, amb = 0.35f * 0.8f, mute = cMuted ? 0f : 1f;
        lock (lk)
        {
            int j = 0;
            foreach (var pv in pending) { while (j < MaxV && voices[j].on) j++; if (j >= MaxV) break; voices[j] = pv; }
            pending.Clear();
        }
        double now = nowS / sr;
        {
            for (int i = 0; i < n; i++)
            {
                double t = now + i * dt; float sum = 0f;
                // the wind
                windF += (windTarget - windF) * 0.0001f;
                float k = Mathf.Clamp01(2f * Mathf.PI * windF / (float)sr); windY += k * (Rand() - windY);
                sum += windY * 0.22f * amb;
                for (int v = 0; v < MaxV; v++)
                {
                    if (!voices[v].on) continue;
                    var vo = voices[v];
                    double u = t - vo.start; if (u < 0) continue;
                    if (u > vo.len + vo.att) { voices[v].on = false; continue; }
                    float frac = (float)(u / Math.Max(1e-4, vo.len));
                    // envelope (web env: exponential ramps from 0.0001)
                    float e;
                    if (u < vo.att) e = vo.linAtt ? (float)(u / vo.att) : Mathf.Pow(10000f, (float)(u / vo.att)) * 0.0001f;
                    else e = Mathf.Pow(0.0001f, Mathf.Clamp01((float)((u - vo.att) / Math.Max(1e-4, vo.len))));
                    float s;
                    if (vo.w == Wave.Noise)
                    {
                        float x = Rand();
                        if (vo.ft != Filt.None)
                        {
                            if (vo.coefT-- <= 0) { vo.coefT = 32; Coef(ref vo, vo.fq0 * Mathf.Pow(vo.fq1 / vo.fq0, Mathf.Clamp01(frac))); }
                            float y = vo.b0 * x + vo.b1 * vo.x1 + vo.b2 * vo.x2 - vo.a1 * vo.y1 - vo.a2 * vo.y2;
                            vo.x2 = vo.x1; vo.x1 = x; vo.y2 = vo.y1; vo.y1 = y; s = y;
                        }
                        else s = x;
                    }
                    else
                    {
                        float f = vo.f0 * Mathf.Pow(vo.f1 / vo.f0, Mathf.Clamp01(frac));
                        vo.phase += (float)(f * dt); if (vo.phase > 1f) vo.phase -= 1f;
                        float ph = vo.phase;
                        s = vo.w == Wave.Sine ? Mathf.Sin(ph * 6.2831853f) : vo.w == Wave.Square ? (ph < 0.5f ? 1f : -1f) : vo.w == Wave.Saw ? 2f * ph - 1f : 1f - 4f * Mathf.Abs(ph - 0.5f);
                    }
                    voices[v] = vo;
                    sum += s * e * vo.vol * (vo.bus == 0 ? sfx : vo.bus == 1 ? mus : amb);
                }
                sum *= mute;
                sum = sum / (1f + Mathf.Abs(sum));   // soft limit
                for (int c = 0; c < ch; c++) data[i * ch + c] = sum;
            }
            System.Threading.Interlocked.Add(ref nowS, n);
        }
    }
}
