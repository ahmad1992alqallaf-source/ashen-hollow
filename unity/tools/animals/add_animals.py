# October hunting update: brown bear, crocodile, ostrich, mountain ram (+ a few more meadow animals), their skins,
# the feather crafts and where they live. Run once on the data folder (it skips what is already there).
import json, random, os, sys
R = sys.argv[1] if len(sys.argv) > 1 else '/mnt/user-data/outputs/AshenHollow/Resources/AH/'
def load(p): return json.load(open(R + p))
def save(p, d): open(R + p, 'w').write(json.dumps(d, indent=1, ensure_ascii=False))
MON = load('Data/monsters.json'); RU = load('Data/rules.json'); IT = load('Data/items.json')
MOBS = {
 'bear':    dict(name='Brown bear', lvl=12, hp=230, dmg=15, speed=120, r=20, aggro=150, xp=200, skinReq=10, slam=True, atkCd=1.8, drops=[['honey', 0.2]]),
 'croc':    dict(name='Mire crocodile', lvl=14, hp=270, dmg=18, speed=95, r=20, aggro=170, xp=240, skinReq=11, lunge=True, drops=[['croc_tooth', 0.15]]),
 'ostrich': dict(name='Ostrich', lvl=17, hp=200, dmg=16, speed=190, r=16, aggro=0, xp=230, skinReq=13, drops=[['ostrich_egg', 0.25]]),
 'ram':     dict(name='Mountain ram', lvl=21, hp=320, dmg=22, speed=140, r=18, aggro=0, xp=300, skinReq=15, slam=True, atkCd=1.7),
}
Q = dict(long=True, noTint=True, idle='Idle', walk='Walk', move='Gallop', hit='Idle_HitReact_Left', death='Death', moveRate=0.9)
RA = 'RaptorArmature|Raptor_'
BEASTS = {
 'bear': dict(files=['b_bear'], size=2.6, attack=['Attack'], **Q),
 'croc': dict(files=['b_croc'], size=4.2, attack=['Attack'], **dict(Q, moveRate=0.7)),
 'ostrich': dict(files=['b_ostrich'], size=2.4, long=False, noTint=True, idle=RA+'Idle1_Anim', walk=RA+'Walk_Anim', move=RA+'Run1_Anim',
                 attack=[RA+'Bite1_Anim', RA+'Bite2_Anim'], hit=RA+'Hit1_Anim', death=RA+'Death1_Anim', moveRate=0.8),
 'ram': dict(files=['b_ram'], size=1.9, attack=['Attack_Headbutt'], **dict(Q, hit='Idle', move='Gallop')),
}
for k, v in MOBS.items(): MON['MOBS'].setdefault(k, v)
for k, v in BEASTS.items(): MON['BEASTS'].setdefault(k, v)
SK = {'bear': ['bear_pelt', 'bear_claw'], 'croc': ['croc_hide', 'croc_tooth'], 'ostrich': ['ostrich_plume', 'feather', 'feather'], 'ram': ['wool', 'wool', 'ram_horn']}
for k, v in SK.items(): RU['MOB_SKIN'].setdefault(k, v)
def mat(n, icon, c, val, note=None, **kw):
    return (dict(name=n, icon=icon, c=c, **kw), val, 'mats', note)
def gear(n, slot, style, form, stats, c, val, icon=None):
    return (dict(name=n, slot=slot, style=style, form=form, stats=stats, rarity='crafted', set=None, c=c, icon=icon or ('g_' + slot)), val, 'gear', None)
NEW = {
 'bear_pelt': mat('Bear pelt', 'hide', '#5b3b24', 40, 'Skinned from brown bears in Silkwood and Whitepine. Tailor it at the loom.'),
 'bear_claw': mat('Bear claw', 'fang', '#e8dcc0', 30, 'A brown bear claw: for gauntlets and charms.'),
 'croc_hide': mat('Crocodile hide', 'hide', '#4a5a32', 45, 'Tough scaled hide from the crocodiles of Duskmire and the Causeway.'),
 'croc_tooth': mat('Crocodile tooth', 'fang', '#efe6cf', 30, 'Strung on yarn it makes a hunter’s necklace.'),
 'ostrich_plume': mat('Ostrich plume', 'feather', '#f6f1e6', 35, 'A great white plume from the ostriches of Sunscar and Scorchwind. Fletching and fine hats use it.'),
 'ram_horn': mat('Ram horn', 'bone', '#cdbb92', 40, 'A curled horn from a Frostfang ram. The smith can make a helm of two.'),
 'ostrich_egg': (dict(name='Ostrich egg', icon='egg', c='#f2ead8', food={'hp': 20, 'hunger': 30}), 25, 'food', 'One egg feeds a crew. Cook it into a Sunscar omelette.'),
 'sunscar_omelette': (dict(name='Sunscar omelette', icon='dish', c='#ffd060', food={'hp': 60, 'hunger': 70}), 70, 'food', None),
 'bear_mantle': gear('Bearskin mantle', 'shoulders', 'wolf', 'fur', {'def': 7, 'hp': 15}, '#5b3b24', 160),
 'bear_cloak': gear('Bearskin cloak', 'cape', 'wolf', 'cloak', {'def': 7, 'hp': 20, 'atk': 1}, '#5b3b24', 190),
 'bearclaw_gloves': gear('Bear-claw gauntlets', 'hands', 'leather', 'gloves', {'def': 5, 'atk': 2}, '#6a4a30', 170),
 'croc_boots': gear('Crocodile-hide boots', 'feet', 'leather', 'boots', {'def': 7, 'hp': 5}, '#4a5a32', 190),
 'croc_vest': gear('Crocodile-scale jerkin', 'chest', 'leather', 'vest', {'def': 12, 'hp': 15}, '#4a5a32', 240),
 'croc_necklace': gear('Crocodile-tooth necklace', 'amulet', 'jewel', 'chain', {'hp': 20, 'atk': 2}, '#efe6cf', 180),
 'plumed_hat': gear('Plumed hunter’s hat', 'head', 'leather', 'hood', {'def': 5, 'hp': 10, 'atk': 1}, '#f6f1e6', 210),
 'feather_cloak': gear('Feather cloak', 'cape', 'wolf', 'cloak', {'def': 8, 'hp': 18, 'cdr': 3}, '#f4f0e6', 280),
 'ramhorn_helm': gear('Ram-horn helm', 'head', 'iron', 'helm', {'def': 10, 'hp': 10}, '#cdbb92', 260),
 'fletched_bow': gear('Plume-fletched longbow', 'weapon', 'iron', 'bow', {'atk': 11}, '#7a5a34', 260, icon='bow'),
}
for k, (d, val, cat, note) in NEW.items():
    IT['ITEMS'].setdefault(k, d); IT['VALUE'].setdefault(k, val); IT['AH_CAT'].setdefault(k, cat)
    if note: RU['ITEM_NOTE'].setdefault(k, note)
RU['ITEM_NOTE']['feather'] = RU['ITEM_NOTE'].get('feather') or 'From your hens and from ostriches. Fletch bows and make plumed hats and feather cloaks.'
REC = {
 'loom': [dict(out='bear_mantle', lvl=11, mats={'bear_pelt': 2}, skill='tailoring', xp=200),
          dict(out='bear_cloak', lvl=12, mats={'bear_pelt': 3, 'bear_claw': 1}, skill='tailoring', xp=230),
          dict(out='bearclaw_gloves', lvl=12, mats={'bear_pelt': 1, 'bear_claw': 2}, skill='tailoring', xp=220),
          dict(out='croc_boots', lvl=13, mats={'croc_hide': 2}, skill='tailoring', xp=240),
          dict(out='croc_vest', lvl=14, mats={'croc_hide': 3, 'croc_tooth': 1}, skill='tailoring', xp=290),
          dict(out='plumed_hat', lvl=14, mats={'ostrich_plume': 1, 'feather': 4, 'linen': 1}, skill='tailoring', xp=280),
          dict(out='feather_cloak', lvl=15, mats={'ostrich_plume': 2, 'feather': 6, 'down_feather': 2}, skill='tailoring', xp=330)],
 'anvil': [dict(out='fletched_bow', lvl=14, mats={'pine_logs': 3, 'iron_bar': 1, 'feather': 6, 'ostrich_plume': 1}, skill='smithing', xp=180),
           dict(out='ramhorn_helm', lvl=15, mats={'ram_horn': 2, 'iron_bar': 1}, skill='smithing', xp=200)],
 'jewel': [dict(out='croc_necklace', lvl=9, mats={'croc_tooth': 3, 'yarn': 1}, skill='jewelcrafting', xp=110, n=1)],
 'oven': [dict(out='sunscar_omelette', lvl=6, mats={'ostrich_egg': 1, 'cheese': 1}, skill='cooking', xp=40, n=2)],
}
for st, L in REC.items():
    have = {x['out'] for x in RU['RECIPES'][st]}
    for x in L:
        if x['out'] not in have: RU['RECIPES'][st].append(x)
save('Data/monsters.json', MON); save('Data/rules.json', RU); save('Data/items.json', IT)
# ---- where they live: near the spots of beasts that already roam there (known open ground)
PLACE = {
 'silkwood': [('bear', 4, ['wolf'])],
 'whitepine': [('bear', 2, ['wolf', 'icewolf']), ('ram', 3, ['icewolf'])],
 'mire': [('croc', 5, ['bogtoad', 'lurker'])],
 'causeway': [('croc', 3, ['bogtoad', 'lurker'])],
 'sands': [('ostrich', 6, ['jackal'])],
 'scorchwind': [('ostrich', 3, ['jackal'])],
 'frost': [('ram', 5, ['icewolf'])],
 'meadow': [('boar', 3, ['boar']), ('deer', 2, ['deer'])],
}
rnd = random.Random(7)
for area, adds in PLACE.items():
    p = 'Areas/' + area + '.json'; d = load(p)
    have = {t['id'] for t in d['mobTypes']}
    for typ, n, near in adds:
        if typ not in have:
            m = MON['MOBS'][typ]
            d['mobTypes'].append(dict(id=typ, name=m['name'], lvl=m['lvl'], hp=m['hp'], dmg=m['dmg'], speed=round(m['speed']/25, 2), radius=round(m['r']/25, 2),
                                      aggro=round(m['aggro']/25, 2), xp=m['xp'], flee=bool(m.get('flee', False)), model='Beasts/' + BEASTS[typ]['files'][0] if typ in BEASTS else None))
            have.add(typ)
        already = sum(1 for s in d['mobs'] if s['type'] == typ and s.get('oct'))
        anchors = [s for s in d['mobs'] if s['type'] in near and not s.get('oct')] or d['mobs']
        b = d['bounds']
        for i in range(n - already):
            for _ in range(50):
                a = rnd.choice(anchors); x = a['x'] + rnd.uniform(-10, 10); z = a['z'] + rnd.uniform(-10, 10)
                if not (b['x0'] + 6 < x < b['x1'] - 6 and b['z0'] + 6 < z < b['z1'] - 6): continue
                if all((s['x']-x)**2 + (s['z']-z)**2 > 25 for s in d['mobs']): break
            d['mobs'].append(dict(type=typ, x=round(x, 2), z=round(z, 2), oct=True))
    save(p, d); print(area, len(d['mobs']))
