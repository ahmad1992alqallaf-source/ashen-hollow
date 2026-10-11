# Ashen Hollow: writes Resources/AH/Data/life.json, the townsfolk's living-world lines in English and Arabic
# (greetings that notice you, the time, the weather and your class; things they call out; short talks between two of
# them). Every line has a fixed id, so its recorded voice file is AH/Voices/<lang>/<voice>/<id>.
import json, sys
G = {}   # greetings by situation
def add(key, *pairs):
    lst = G.setdefault(key, [])
    for en, ar in pairs: lst.append({"id": "%s_%02d" % (key, len(lst) + 1), "en": en, "ar": ar})

add("any",
    ("Well met, traveller.", "أهلًا بك يا مسافر."),
    ("Hello there!", "مرحبًا!"),
    ("How are you keeping?", "كيف حالك؟"),
    ("Good to see a friendly face.", "يسعدني أن أرى وجهًا ودودًا."),
    ("Safe roads to you.", "طرقك آمنة إن شاء الله."),
    ("Stay a while, the town is quiet today.", "ابقَ قليلًا، البلدة هادئة اليوم."),
    ("Mind your step, the cobbles are loose.", "انتبه لخطواتك، الحجارة مرتخية هنا."),
    ("Ah, a new face. Welcome!", "وجه جديد! أهلًا وسهلًا."),
    ("Back again? You're always welcome.", "عدت مجددًا؟ مرحبًا بك دائمًا."),
    ("May the ember light your way.", "لتُنِر الجمرة طريقك."))
add("morning",
    ("Good morning!", "صباح الخير!"),
    ("Up early, are we?", "استيقظت باكرًا اليوم!"),
    ("Fresh bread at the baker's, if you hurry.", "الخبز طازج عند الخبّاز، أسرع قبل أن ينفد."))
add("evening",
    ("Good evening.", "مساء الخير."),
    ("The day's nearly done. Rest soon.", "النهار يكاد ينتهي، استرح قريبًا."),
    ("Tavern's filling up. Join us later?", "الحانة تمتلئ بالناس، هل تنضم إلينا لاحقًا؟"))
add("night",
    ("Out this late? Careful in the dark.", "في الخارج في هذا الوقت؟ احذر من الظلام."),
    ("Can't sleep either, eh?", "لا تستطيع النوم أنت أيضًا؟"),
    ("Strange sounds beyond the walls tonight.", "أصوات غريبة خلف الأسوار الليلة."))
add("rain",
    ("Rain again! My roof leaks like a sieve.", "مطر مرة أخرى! سقفي يقطر كالغربال."),
    ("Get under cover, you'll catch a chill.", "احتمِ من المطر، ستصاب بالبرد."))
add("snow",
    ("Cold enough to freeze your breath.", "برد يجمّد الأنفاس."),
    ("Stay by a fire tonight, friend.", "ابقَ قرب النار الليلة يا صديقي."))
add("fog",
    ("Can barely see my own hands in this fog.", "لا أكاد أرى يديّ في هذا الضباب."),)
add("warrior",
    ("Fine steel you carry, warrior.", "سيف رائع تحمله أيها المحارب."),
    ("With a shield like that, I'd sleep easy.", "مع درع كهذا، أنام مطمئنًا."))
add("priest",
    ("Bless you, healer.", "باركك الله أيها المعالج."),
    ("Your light gives us hope.", "نورك يمنحنا الأمل."))
add("mage",
    ("Please don't set my stall on fire.", "أرجوك لا تحرق بسطتي."),
    ("Magic folk... always muttering to themselves.", "أهل السحر... يتمتمون لأنفسهم دائمًا."))
add("druid",
    ("You smell of pine and rain, druid.", "رائحتك صنوبر ومطر أيها الدرويد."),)
add("ranger",
    ("Sharp eyes, ranger. Seen anything out there?", "عينان حادتان أيها الرامي، هل رأيت شيئًا هناك؟"),)
add("shaman",
    ("The spirits walk with you, shaman.", "الأرواح تسير معك أيها الشامان."),)
add("rogue",
    ("I'll keep a hand on my purse, thank you.", "سأمسك محفظتي جيدًا، شكرًا لك."),
    ("Quiet steps, that one.", "خطوات هادئة... هذا واحد يُحذر منه."))
add("warden",
    ("A Warden! We sleep easier with you near.", "حارس! ننام مطمئنين وأنت قريب."),
    ("Stone and ember... the old stories are true.", "حجر وجمرة... الحكايات القديمة صحيحة إذن."))
add("strong",
    ("They say you've bested beasts no one else dared face.", "يقولون إنك هزمت وحوشًا لم يجرؤ أحد على مواجهتها."),
    ("The bards are already singing about you.", "الشعراء بدأوا يغنّون عنك."))
add("guard",
    ("Move along, citizen. All's well.", "تابع طريقك، كل شيء على ما يرام."),
    ("Keep your blade sheathed in town.", "أبقِ سلاحك في غمده داخل البلدة."),
    ("Report anything strange to the captain.", "أبلغ القائدة بأي شيء غريب."))
add("merchant",
    ("Best prices in town, I swear it!", "أفضل الأسعار في البلدة، أقسم لك!"),
    ("Come, come! Have a look!", "تعال، تعال! ألقِ نظرة!"),
    ("Fresh goods, just off the cart!", "بضاعة طازجة وصلت للتو!"))
add("child",
    ("Are you a real hero?", "هل أنت بطل حقيقي؟"),
    ("Can I hold your sword? Please?", "هل أستطيع أن أحمل سيفك؟ أرجوك!"),
    ("Mama says not to talk to strangers!", "أمي تقول لا تتحدث مع الغرباء!"))

# things they call out on their own (no one needs to be near them but you)
C = {}
def call(key, *pairs):
    lst = C.setdefault(key, [])
    for en, ar in pairs: lst.append({"id": "c_%s_%02d" % (key, len(lst) + 1), "en": en, "ar": ar})
call("merchant",
    ("Apples! Sweet apples!", "تفاح! تفاح حلو!"),
    ("Cloth from the coast, finest weave!", "قماش من الساحل، أجود نسج!"),
    ("Two for one, today only!", "اثنان بسعر واحد، اليوم فقط!"))
call("guard",
    ("Eyes open, lads.", "افتحوا أعينكم يا رجال."),
    ("Quiet watch tonight. I hope.", "حراسة هادئة الليلة... أتمنى."))
call("child",
    ("You can't catch me!", "لن تمسكني!"),
    ("Tag! You're it!", "لمستك! دورك الآن!"))

# two of them talking (A, B, A, B...): who = the kind of person who starts, "any" fits everyone
T = []
def talk(who, *pairs):
    T.append({"id": "t%02d" % (len(T) + 1), "who": who, "lines": [{"id": "t%02d_%d" % (len(T) + 1, i + 1), "en": en, "ar": ar} for i, (en, ar) in enumerate(pairs)]})
talk("any", ("Did you hear? Wolves near the old mill again.", "هل سمعت؟ الذئاب قرب الطاحونة القديمة مرة أخرى."),
            ("Third time this month. Someone should do something.", "المرة الثالثة هذا الشهر. على أحدهم أن يفعل شيئًا."),
            ("Maybe one of those adventurers will.", "ربما يفعلها أحد هؤلاء المغامرين."))
talk("any", ("How's your mother keeping?", "كيف حال والدتك؟"),
            ("Better, thank the stars. The healer's tea worked.", "أفضل والحمد لله، شاي المعالج نفع."),
            ("Good. Tell her I asked.", "جيد، بلّغها سلامي."))
talk("merchant", ("You call this a fair price?", "وتسمي هذا سعرًا عادلًا؟"),
            ("For silk from the coast? It's a gift!", "لحرير من الساحل؟ إنه هدية!"),
            ("A gift would be free.", "الهدية تكون مجانية."),
            ("Fine, fine. Ten off. Don't tell anyone.", "حسنًا حسنًا، أنقص عشرة. لا تخبر أحدًا."))
talk("guard", ("Another night shift. My feet are killing me.", "مناوبة ليلية أخرى. قدماي تؤلمانني."),
            ("Stop complaining. At least it's not raining.", "توقف عن التذمر، على الأقل لا تمطر."),
            ("...You had to say it.", "...كان لا بد أن تقولها."))
talk("any", ("The ash has been falling thicker in the east.", "الرماد يتساقط بكثافة أكبر في الشرق."),
            ("My grandfather said that means the old fire is waking.", "جدي كان يقول إن ذلك يعني أن النار القديمة تستيقظ."),
            ("Let's hope your grandfather was wrong.", "لنأمل أن جدك كان مخطئًا."))
talk("any", ("Smell that? The baker's making honey cakes.", "هل تشم هذا؟ الخبّاز يصنع كعك العسل."),
            ("Don't tempt me. I promised my wife.", "لا تغرني، لقد وعدت زوجتي."),
            ("One cake never hurt anyone.", "كعكة واحدة لم تؤذِ أحدًا قط."))
talk("any", ("Saw a ship come in from Coralport this morning.", "رأيت سفينة قادمة من مرفأ المرجان هذا الصباح."),
            ("Spices again?", "توابل مرة أخرى؟"),
            ("And rumours. Always rumours.", "وشائعات. دائمًا شائعات."))
talk("child", ("I'm going to be a Warden when I grow up!", "سأصبح حارسًا عندما أكبر!"),
            ("You can't even lift a bucket.", "أنت لا تستطيع حتى رفع دلو."),
            ("Can too!", "بل أستطيع!"))
talk("any", ("They say the Demon Realm is stirring.", "يقولون إن عالم الشياطين بدأ يتحرك."),
            ("Hush. Don't speak of it after dark.", "اصمت، لا تتحدث عنه بعد الغروب."))
talk("any", ("Lovely weather, isn't it?", "الجو جميل، أليس كذلك؟"),
            ("Enjoy it while it lasts.", "استمتع به ما دام باقيًا."))

out = {"greet": G, "call": C, "talk": T}
json.dump(out, open(sys.argv[1], "w", encoding="utf-8"), ensure_ascii=False, indent=1)
n = sum(len(v) for v in G.values()) + sum(len(v) for v in C.values()) + sum(len(t["lines"]) for t in T)
print("lines:", n)
