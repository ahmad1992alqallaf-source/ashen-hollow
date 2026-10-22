// Ashen Hollow: first-time tips. The first time a window opens, one short line at its foot says what it is for;
// it fades by itself (or tap it) and never shows again for that window.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class AHUI
{
    static readonly Dictionary<string, string[]> FirstTips = new Dictionary<string, string[]>
    {
        { "hub",      new[] { "Everything in the game lives here, sorted into eight groups. A green number means something is ready for you.", "كل شيء في اللعبة هنا، مقسم إلى ثماني مجموعات. الرقم الأخضر يعني أن شيئًا جاهز لك." } },
        { "char",     new[] { "Your gear around your hero. Tap a slot to look at that piece; the tabs lead to stats, talents and looks.", "عتادك حول بطلك. اضغط خانة لترى القطعة؛ الألسنة تقودك إلى الإحصاءات والمواهب والمظهر." } },
        { "shop",     new[] { "Green ▲ means better than what you wear, red ▼ means worse. Sell tab: only your bag, never what you wear.", "▲ الأخضر أفضل مما تلبس، و▼ الأحمر أسوأ. البيع: حقيبتك فقط، لا ما ترتديه." } },
        { "craft",    new[] { "Pick a recipe, have the materials, tap Make. Better grades of material give better results.", "اختر وصفة، واجمع المواد، ثم اضغط اصنع. المواد الأجود تعطي نتائج أفضل." } },
        { "bank",     new[] { "The bank keeps what you don't carry. It is shared by all your heroes on this device.", "البنك يحفظ ما لا تحمله، ويشترك فيه كل أبطالك على هذا الجهاز." } },
        { "kitchen",  new[] { "Cook meals from what you gather. Meals feed you for longer and give a little boost.", "اطبخ وجبات مما تجمعه. الوجبات تُشبعك أطول وتمنحك دفعة صغيرة." } },
        { "daily",    new[] { "New goals every day. Claim them for marks, and log in each day for the calendar's gifts.", "أهداف جديدة كل يوم. اجمعها مقابل العلامات، وادخل يوميًا لهدايا التقويم." } },
        { "quick",    new[] { "Choose what each quick button uses. Leave it on Best I carry and it picks for you.", "اختر ما يستخدمه كل زر سريع. اتركه على الأفضل وسيختار لك." } },
        { "mail",     new[] { "Caravans, festivals and friends send things here. Take them when you have room.", "القوافل والمهرجانات والأصدقاء يرسلون الأشياء هنا. خذها حين يتسع لك." } },
        { "ways",     new[] { "Walk near a waystone to attune it. From the map, Fast travel takes you to any of them for a few coins.", "اقترب من حجر الطريق لتربطه. من الخريطة، السفر السريع يأخذك لأي منها مقابل بعض العملات." } },
        { "map",      new[] { "Gold star: your quest. Blue diamonds: waystones. Pin places you want to remember.", "النجمة الذهبية: مهمتك. المعيّنات الزرقاء: أحجار الطريق. ثبّت الأماكن التي تريد تذكرها." } },
        { "ranked",   new[] { "Fight rivals near your rank. Wins climb the ladder; the weekly chest grows with your tier.", "قاتل خصومًا قريبين من رتبتك. الانتصارات ترفعك، وصندوق الأسبوع يكبر مع مستواك." } },
        { "caravan",  new[] { "Send goods to other towns to sell. Guard the road and bandits won't take a share. The coin comes by mail.", "أرسل بضائعك لتُباع في مدن أخرى. احرس الطريق كي لا يأخذ اللصوص حصة. المال يصل بالبريد." } },
        { "jobs",     new[] { "Quick paid jobs around town. A good way to earn coin between adventures.", "أعمال سريعة مدفوعة في البلدة. طريقة جيدة لكسب المال بين المغامرات." } },
        { "farm",     new[] { "Plant, water and come back later. Crops grow while you play elsewhere.", "ازرع واسقِ وعد لاحقًا. المحاصيل تنمو وأنت تلعب في مكان آخر." } },
        { "ah",       new[] { "The auction house: buy from other traders, or list your own goods for sale.", "دار المزاد: اشترِ من التجار، أو اعرض بضائعك للبيع." } },
        { "orders",   new[] { "Work orders ask for things you can make. Fill them for coin and reputation.", "أوامر العمل تطلب أشياء تستطيع صنعها. أنجزها مقابل المال والسمعة." } },
        { "mastery",  new[] { "Every skill you use grows its mastery. Each level brings a small reward.", "كل مهارة تستخدمها تنمو خبرتها. كل مستوى يجلب مكافأة صغيرة." } },
        { "pass",     new[] { "The season's road: play to earn its steps. Nothing here makes you stronger, only finer looking.", "طريق الموسم: العب لتكسب خطواته. لا شيء هنا يجعلك أقوى، بل أجمل مظهرًا." } },
        { "book",     new[] { "Every beast you defeat fills a page. Full pages pay out.", "كل وحش تهزمه يملأ صفحة. الصفحات المكتملة تمنحك مكافأة." } },
        { "looks",    new[] { "Change how your gear looks without changing its stats.", "غيّر مظهر عتادك دون أن تتغير قوته." } },
        { "stats",    new[] { "Your numbers in full. Gear, talents and food all feed into them.", "أرقامك كاملة. العتاد والمواهب والطعام كلها تؤثر فيها." } },
        { "deephall", new[] { "The Ashen Deep: five floors, new every time. Clear it fast for the weekly board.", "أعماق الرماد: خمسة طوابق جديدة في كل مرة. أنهِها بسرعة لتدخل لوحة الأسبوع." } },
        { "story",    new[] { "Your story so far, chapter by chapter. The gold arrow always points to what's next.", "قصتك حتى الآن، فصلًا بفصل. السهم الذهبي يشير دائمًا إلى التالي." } },
        { "friends",  new[] { "Talk to people every day and bring gifts they love. Each new heart sends you a letter.", "تحدث مع الناس يوميًا وأحضر لهم ما يحبون. كل قلب جديد يرسل إليك رسالة." } },
        { "settings", new[] { "Sound, graphics, language and your HUD layout. Edit HUD layout lets you move the buttons.", "الصوت والرسومات واللغة وترتيب الأزرار. تعديل الواجهة يتيح لك تحريك الأزرار." } },
    };
    RectTransform tipRoot; Text tipText; float tipT;

    static bool TipSeen(string k) { try { return AHPrefs.GetInt("ah_tip_" + k, 0) == 1; } catch { return true; } }
    void WorkTip()
    {
        if (wkPanel == null) return;
        string[] t; if (wkMode == null || !FirstTips.TryGetValue(wkMode, out t) || TipSeen(wkMode)) return;
        try { AHPrefs.SetInt("ah_tip_" + wkMode, 1); } catch { }
        if (tipRoot == null)
        {
            tipRoot = Img("FirstTip", wkPanel, cgPillGlass != null ? cgPillGlass : white, new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(WkW - 60f, 44f), cgPillGlass != null ? Color.white : new Color(0.1f, 0.16f, 0.26f, 0.95f));
            if (cgPillGlass != null) tipRoot.GetComponent<Image>().type = Image.Type.Sliced;
            var ic = Img("Bulb", tipRoot, CGIcon("star") ?? circle, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(22, 22), new Color(1f, 0.85f, 0.4f)); ic.GetComponent<Image>().preserveAspect = true;
            tipText = Label(tipRoot, "T", "", 15, TextAnchor.MiddleLeft, new Vector2(46f, -6f), new Vector2(WkW - 120f, 34f), Color.white); Fit(tipText, 11);
            tipText.rectTransform.anchorMin = tipText.rectTransform.anchorMax = new Vector2(0f, 1f);
            taps.Add(new TapBtn { rt = tipRoot, layer = 5, group = "tip", act = () => tipT = 0f });
        }
        tipText.text = AHLoc.T(t[0], t[1]);
        tipText.alignment = AHLoc.Arabic ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        tipRoot.SetAsLastSibling(); tipRoot.gameObject.SetActive(true); tipT = 9f;
        var cg = tipRoot.GetComponent<CanvasGroup>(); if (cg == null) cg = tipRoot.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 1f;
    }
    void TipTick()
    {
        if (tipRoot == null || !tipRoot.gameObject.activeSelf) return;
        if (!WorkOpen) { tipRoot.gameObject.SetActive(false); return; }
        tipT -= Time.unscaledDeltaTime;
        var cg = tipRoot.GetComponent<CanvasGroup>(); if (cg != null) cg.alpha = Mathf.Clamp01(tipT / 0.6f);
        if (tipT <= 0f) tipRoot.gameObject.SetActive(false);
    }
}
