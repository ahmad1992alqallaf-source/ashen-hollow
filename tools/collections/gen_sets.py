# Ashen Hollow: the four armour collections made from our own concept art (Draconic, Lava, Demonic, Fossil), a set
# for every class in each, dropped by the collection's world boss (and the matching dungeon). Writes items.json,
# rules.json, monsters.json and events.json in place (run again safely: it replaces what it made before).
import json, sys, os
D = sys.argv[1]   # .../Resources/AH/Data
J = lambda f: json.load(open(os.path.join(D, f)))
items, rules, mobs, events = J('items.json'), J('rules.json'), J('monsters.json'), J('events.json')

COLL = {   # key: (display word, boss, boss level, dungeon, set name, colours main/hi/trim/dark/glow, item colour)
 'draconic': ('Dragonscale', 'wyrm',      40, None,       'Dragonscale Ascendant', (0x1d3a6e, 0x3f7fd8, 0xc8d2e0, 0x0b1730, 0x6fb4ff), '#3f7fd8'),
 'lava':     ('Magmaforged', 'pyraxis',   50, 'd_molten', "Molten King's",         (0x2a1a14, 0xff6a1c, 0xd08a3a, 0x100a08, 0xff8a2c), '#ff6a1c'),
 'demonic':  ('Demonbound',  'voidmaw',   55, 'd_hollow', 'Demonbound',            (0x1a1214, 0xb0202a, 0xc89a4a, 0x0a0606, 0xff3030), '#b0202a'),
 'fossil':   ('Fossilbone',  'tricebone', 60, None,       'Ancient Fossil',        (0xd8cdb0, 0x8a7a5a, 0xc89a3a, 0x3a3226, 0xffb040), '#c8b890'),
}
BONUS = {
 'draconic': [[2, '+60 max HP', {'hp': 60}], [4, '+15% damage', {'dmg': 0.15}], [6, 'All cooldowns 15% shorter', {'cdr': 0.15}]],
 'lava':     [[2, '+12% damage', {'dmg': 0.12}], [4, 'Hits set enemies ablaze', {'ignite': True}], [6, '+90 max HP and cooldowns 10% shorter', {'hp': 90, 'cdr': 0.1}]],
 'demonic':  [[2, '+10% critical chance', {'crit': 0.1}], [4, '+20% damage', {'dmg': 0.2}], [6, '+120 max HP', {'hp': 120}]],
 'fossil':   [[2, '+150 max HP', {'hp': 150}], [4, '+15% damage and healing', {'dmg': 0.15, 'heal': 0.15}], [6, 'All cooldowns 20% shorter', {'cdr': 0.2}]],
}
ARMOR = {'warrior': 'plate', 'warden': 'plate', 'rogue': 'leather', 'ranger': 'leather', 'shaman': 'leather', 'mage': 'cloth', 'priest': 'cloth', 'druid': 'cloth'}
FORMS = {   # head, shoulders, chest, hands, legs, feet, weapon
 'warrior': ('forgehelm', 'spikes', 'molten', 'gauntlet', 'greaves', 'boots', 'sword'),
 'warden':  ('helm', 'plates', 'molten', 'gauntlet', 'greaves', 'boots', 'warhammer'),
 'rogue':   ('cowl', 'spikes', 'vest', 'gloves', None, 'boots', 'kris'),
 'ranger':  ('hood', 'fur', 'vest', 'gloves', None, 'boots', 'longbow'),
 'shaman':  ('hood', 'fur', 'vest', 'gloves', None, 'boots', 'totemaxe'),
 'mage':    ('mhood', 'spikes', 'robe', 'gloves', None, 'boots', 'magmastaff'),
 'priest':  ('halo', 'plates', 'robe', 'gloves', None, 'boots', 'sunmace'),
 'druid':   ('antlers', 'feathers', 'robe', 'gloves', None, 'boots', 'bloomstaff'),
}
NAMES = {'plate': ('Helm', 'Pauldrons', 'Breastplate', 'Gauntlets', 'Greaves', 'Sabatons'),
         'leather': ('Cowl', 'Mantle', 'Jerkin', 'Grips', 'Leggings', 'Boots'),
         'cloth': ('Hood', 'Mantle', 'Robe', 'Gloves', 'Leggings', 'Slippers')}
WEAPON = {'warrior': 'Greatsword', 'warden': 'Warhammer', 'rogue': 'Twin Daggers', 'ranger': 'Longbow', 'shaman': 'Totem Axe',
          'mage': 'Staff', 'priest': 'Mace', 'druid': 'Branchstaff'}
ICON = ('g_head', 'g_shoulders', 'g_chest', 'g_hands', 'g_legs', 'g_feet')
SLOTS = ('head', 'shoulders', 'chest', 'hands', 'legs', 'feet')
# stats at level 55 (the Starguard / Hollow Below tier), scaled by the boss's level
BASE = {'plate':   [{'def': 23, 'hp': 33}, {'def': 15, 'hp': 20}, {'def': 36, 'hp': 73}, {'def': 17, 'atk': 7}, {'def': 20, 'hp': 30}, {'def': 12, 'hp': 18}],
        'leather': [{'def': 8, 'atk': 6, 'hp': 15}, {'def': 6, 'atk': 4, 'hp': 10}, {'def': 17, 'atk': 8, 'hp': 30}, {'def': 6, 'atk': 7}, {'def': 6, 'atk': 4, 'hp': 10}, {'def': 5, 'atk': 4, 'hp': 8}],
        'cloth':   [{'def': 13, 'atk': 10, 'hp': 26}, {'def': 9, 'atk': 6, 'hp': 16}, {'def': 20, 'atk': 13, 'hp': 53}, {'def': 10, 'atk': 7}, {'def': 9, 'atk': 6, 'hp': 16}, {'def': 7, 'atk': 5, 'hp': 12}]}
WATK = 37

I, R = items['ITEMS'], rules
made = set()
for coll, (word, boss, lvl, dng, setname, col, c) in COLL.items():
    k = lvl / 55.0
    items['STYLE'][coll] = dict(zip(('main', 'hi', 'trim', 'dark', 'glow'), col))
    R['SETS'][coll] = {'name': setname, 'bonuses': BONUS[coll]}
    pools = {}
    for cls, armor in ARMOR.items():
        style = coll + '_' + cls
        R['ARMOR_OF'][style] = armor
        items['STYLE'][style] = items['STYLE'][coll]
        f = FORMS[cls]; pool = []
        for i, slot in enumerate(SLOTS):
            if f[i] is None: continue
            iid = style + '_' + slot
            st = {s: max(1, round(v * k)) for s, v in BASE[armor][i].items()}
            I[iid] = {'name': word + ' ' + NAMES[armor][i], 'slot': slot, 'style': style, 'form': f[i], 'stats': st,
                      'rarity': 'set', 'set': coll, 'c': c, 'icon': ICON[i], 'cls': cls}
            pool.append(iid)
        wid = style + '_weapon'
        I[wid] = {'name': word + ' ' + WEAPON[cls], 'slot': 'weapon', 'style': style, 'form': f[6], 'stats': {'atk': max(1, round(WATK * k))},
                  'rarity': 'set', 'set': coll, 'c': c, 'icon': 'sword' if cls in ('warrior', 'rogue') else 'mace' if cls in ('warden', 'priest') else 'staff', 'cls': cls}
        pool.append(wid)
        for iid in pool:
            items['VALUE'][iid] = int(4000 * k); items['AH_CAT'][iid] = 'gear'; made.add(iid)
        pools[cls] = pool
    # the boss: one piece for the killer's class every kill, a second one now and then
    mobs['MOBS'][boss]['classSet'] = coll
    # the dungeon: the collection joins its loot chests
    if dng:
        for d in events['DUNGEONS']:
            if d.get('id') != dng: continue
            for cls, pool in pools.items():
                cur = [x for x in d['sets'].get(cls, []) if not x.startswith(coll + '_')]
                d['sets'][cls] = cur + pool
R['COLLECTIONS'] = {coll: {'name': v[4], 'word': v[0], 'boss': v[1]} for coll, v in COLL.items()}

for f, o in (('items.json', items), ('rules.json', rules), ('monsters.json', mobs), ('events.json', events)):
    t = open(os.path.join(D, f)).read()
    if t.startswith('{\n'): out = json.dumps(o, ensure_ascii=False, indent=t[2:].index('"') if t[2] == ' ' else 1)   # keep each file's own layout
    elif '": ' in t[:200]: out = json.dumps(o, ensure_ascii=False)
    else: out = json.dumps(o, ensure_ascii=False, separators=(',', ':'))
    open(os.path.join(D, f), 'w').write(out)
print('items made', len(made))
