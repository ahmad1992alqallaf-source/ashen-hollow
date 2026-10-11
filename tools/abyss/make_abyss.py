# Ashen Hollow: the Abyssal Realm (levels 60-75), from the user's map concept (250 x 250 m): islands of the fallen
# heroes' kingdom over the void, joined by bridges. Writes Resources/AH/Areas/abyss.json (where you may walk, the
# monsters and their places) and adds the realm's monsters to monsters.json (MOBS). Until their own models are made,
# each borrows a near look ("model") and is tinted violet.
import json, sys, random
root = sys.argv[1]
X0, Z0 = 10000.0, 0.0           # its own place on the world grid (no other land there)
S = 0.04                         # metres per web unit (speeds, ranges in MOBS are web units)

# islands and bridges: (x, z, w, h) in metres from the realm's corner (x runs east, z north)
islands = {
    "gate":     (95, 4, 60, 36),     # the broken gate, where you arrive
    "plains":   (30, 48, 190, 42),   # the ash-white plains of the old kingdom
    "shrine":   (10, 120, 60, 60),   # the fallen heroes' shrine (west)
    "rift":     (100, 110, 50, 40),  # the riven island in the middle
    "forge":    (170, 120, 70, 60),  # the war forge fortress (east)
    "span":     (105, 173, 40, 22),  # the great bridge's landing below the citadel
    "citadel":  (80, 205, 90, 40),   # the throne citadel
}
bridges = [
    (121, 40, 8, 8),    # gate - plains
    (36, 90, 8, 30),    # plains - shrine
    (201, 90, 8, 30),   # plains - forge
    (121, 90, 8, 20),   # plains - rift
    (121, 150, 8, 23),  # rift - span
    (121, 195, 8, 10),  # span - citadel
    (70, 150, 30, 7),   # shrine - rift
    (150, 150, 20, 7),  # rift - forge
]
walk = [{"x": X0 + x, "z": Z0 + z, "w": w, "h": h} for (x, z, w, h) in list(islands.values()) + bridges]

# the realm's people of the void: id, name, level, hp, damage, speed (web), reach r (web), aggro (web), xp, look, extra
types = [
    ("dm_thrall",   "Ashbound Thrall",          61, 980,  58, 75, 16, 240, 520,  "t_guard",       {}),
    ("dm_imp",      "Gnashing Imp",             62, 760,  60, 160, 13, 300, 500, "m_imp",         {"lunge": True}),
    ("dm_veilwitch","Veilwitch",                64, 1100, 66, 80, 18, 312, 640,  "sandwraith",    {}),
    ("dm_gloomwing","Gloomwing Fiend",          65, 1050, 64, 170, 16, 336, 620, "voidling",      {"lunge": True}),
    ("dm_hound",    "Cinder Hellhound",         67, 1180, 68, 175, 16, 312, 700, "shadowwolf",    {"lunge": True}),
    ("dm_brute",    "Brimstone Brute",          69, 1900, 78, 70, 24, 252, 900,  "obsidiangolem", {"slam": True, "atkCd": 1.9}),
    ("dm_knight",   "Hellforged Knight",        71, 1700, 82, 85, 20, 276, 950,  "fnight",        {}),
    ("dm_behemoth", "Rift Behemoth",            73, 9800, 105, 70, 34, 300, 4200, "wb_tusk",      {"elite": True, "slam": True, "atkCd": 2.1, "respawn": 300, "gold": 600}),
    ("dm_malgareth","Malgareth the Demon Lord", 75, 42000, 128, 80, 40, 420, 22000, "m_flameguard", {"slam": True, "atkCd": 2.0, "respawn": 600, "gold": 5000, "boss": True}),
]
violet, glow = 0x4a3a78, 0xb68cff
mons_path = root + "/Resources/AH/Data/monsters.json"
M = json.load(open(mons_path, encoding="utf-8"))
# monsters whose own model is made (the model files stay on the PC): their own look in BEASTS, not tinted; the
# four-legged ones wear an existing animal's skeleton and clips
own = {
    "dm_hound":    ("dm_hellhound", dict(M["BEASTS"]["shadowwolf"], files=["b_dm_hellhound"], size=2.0, noTint=True)),
    "dm_behemoth": ("dm_behemoth",  dict(M["BEASTS"]["wb_tusk"], files=["b_dm_behemoth"], noTint=True)),
}
for tid, (key, beast) in own.items(): M["BEASTS"][key] = beast
for (tid, name, lvl, hp, dmg, spd, r, aggro, xp, look, ex) in types:
    e = {"name": name, "lvl": lvl, "hp": hp, "dmg": dmg, "speed": spd, "r": r, "aggro": aggro, "xp": xp, "noSkin": True, "skinReq": 99,
         "model": look, "tint": violet, "tintK": 0.55, "glow": glow, "gold": ex.get("gold", int(lvl * 2.2)), "respawn": ex.get("respawn", 30),
         "drops": [["void_dust", 0.5]]}
    for k in ("lunge", "slam", "atkCd", "elite"):
        if k in ex: e[k] = ex[k]
    if ex.get("boss"): e["elite"] = True; e["scale"] = 2.0
    if tid == "dm_behemoth": e["scale"] = 1.6
    if tid in own:
        e["model"] = own[tid][0]
        for k in ("tint", "tintK", "glow"): e.pop(k, None)
    M["MOBS"][tid] = e
json.dump(M, open(mons_path, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))

mobTypes = [{"id": t[0], "name": t[1], "lvl": t[2], "hp": t[3], "dmg": t[4], "speed": round(t[5] * S, 2), "radius": round(t[6] * S * 1.2, 2),
             "aggro": round(t[7] * S, 2), "xp": t[8], "flee": False, "model": "Mobs/" + t[0]} for t in types]
# who lives where (counts per island)
rnd = random.Random(75)
where = {"plains": [("dm_thrall", 9), ("dm_imp", 7)], "shrine": [("dm_veilwitch", 5), ("dm_gloomwing", 4)],
         "rift": [("dm_hound", 6)], "forge": [("dm_brute", 4), ("dm_knight", 5)], "span": [("dm_behemoth", 1)], "citadel": [("dm_malgareth", 1)]}
mobs = []
for isl, lst in where.items():
    x, z, w, h = islands[isl]
    for tid, n in lst:
        for i in range(n):
            if tid in ("dm_behemoth", "dm_malgareth"): px, pz = x + w / 2, z + h * (0.5 if tid == "dm_behemoth" else 0.7)
            else: px, pz = x + 6 + rnd.random() * (w - 12), z + 6 + rnd.random() * (h - 12)
            mobs.append({"type": tid, "x": X0 + px, "z": Z0 + pz})
area = {"version": 2, "id": "abyss", "region": "The Abyssal Realm", "levels": "60–75",
        "groundMap": {"x0": X0, "z0": Z0, "w": 250, "h": 250}, "bounds": {"x0": X0, "z0": Z0, "x1": X0 + 250, "z1": Z0 + 250},
        "spawn": {"x": X0 + 125, "z": Z0 + 14}, "playerSpeed": 6.4, "mobTypes": mobTypes, "mobs": mobs,
        "circles": [], "boxes": [], "water": [], "exits": [], "gates": [], "dgates": [], "walk": walk, "dayLength": 1440,
        "islands": {k: {"x": X0 + v[0], "z": Z0 + v[1], "w": v[2], "h": v[3]} for k, v in islands.items()},
        "bridges": [{"x": X0 + b[0], "z": Z0 + b[1], "w": b[2], "h": b[3]} for b in bridges]}
json.dump(area, open(root + "/Resources/AH/Areas/abyss.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("mobs", len(mobs), "walk", len(walk))
