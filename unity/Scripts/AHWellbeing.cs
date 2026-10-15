// Ashen Hollow: gentle rest reminders. After every 1, 2 or 3 hours of play (Settings -> Rest reminders; on, every 2
// hours, by default) a short note comes up: drink some water, rest your eyes, stretch, take a break. It never stops the
// game: a banner, a line in the chat, and a note that stays a few seconds. Time counts only while the game is open.
using UnityEngine;

public static class AHWellbeing
{
    const string KeyHours = "rest_hours";   // 0 = off
    public static float Hours { get { return AHPrefs.GetFloat(KeyHours, 2f); } set { AHPrefs.SetFloat(KeyHours, value); AHPrefs.Save(); next = played + Mathf.Max(0.25f, value) * 3600f; } }
    public static bool On { get { return Hours > 0f; } }

    static float played, next = -1f; static int tipI;

    // the tips, in turn (English, Arabic)
    static readonly string[][] Tips =
    {
        new[] { "Time for a sip of water: stay hydrated!", "حان وقت شرب الماء: حافظ على ترطيب جسمك!" },
        new[] { "Rest your eyes: look at something far away for 20 seconds.", "أرح عينيك: انظر إلى شيء بعيد لمدة ٢٠ ثانية." },
        new[] { "Don't forget to blink: it keeps your eyes from drying out.", "لا تنسَ أن ترمش: ذلك يحمي عينيك من الجفاف." },
        new[] { "Stand up and stretch your back, neck and shoulders.", "قف ومدّد ظهرك ورقبتك وكتفيك." },
        new[] { "You've been playing a while: a short break does you good.", "لقد لعبت فترة طويلة: استراحة قصيرة مفيدة لك." },
        new[] { "Sit up straight and hold your phone a little further away.", "اجلس باستقامة وأبعد هاتفك قليلًا عن عينيك." },
    };

    // called every frame by the UI (real time, so it counts while paused in menus too)
    public static void Tick(AHUI ui, float dt)
    {
        played += dt;
        if (!On || ui == null) return;
        if (next < 0f) next = Hours * 3600f;
        if (played < next) return;
        next = played + Hours * 3600f;
        var t = Tips[tipI++ % Tips.Length];
        int h = Mathf.Max(1, Mathf.RoundToInt(played / 3600f));
        string head = AHLoc.T("You've played for " + h + " hour" + (h > 1 ? "s" : ""), "لقد لعبت لمدة " + h + " ساعة");
        ui.Banner(head, AHLoc.T(t[0], t[1]));
        ui.Toast(AHLoc.T(t[0], t[1]) + AHLoc.T("  (Settings → Rest reminders)", "  (الإعدادات ← تذكير الراحة)"), 8f);
    }
}
