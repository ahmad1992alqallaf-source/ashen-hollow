# item id -> picture. A string is a glyph name; a dict can add:
#   badge: a small second glyph at the lower right (seeds show their crop, reins their mount, maps their land)
#   under: 'plate' / 'bowl' / 'bag' / 'card' drawn behind the glyph
#   c: a colour override
M = {
# ---- fish: every fish its own shape
'raw_trout': 'flying-trout', 'raw_salmon': 'salmon', 'raw_tuna': 'flatfish', 'raw_eel': 'eel',
'raw_swordfish': 'angler-fish', 'raw_lavaeel': {'g': 'snake', 'badge': 'flame'},
'trout': 'fried-fish', 'salmon': 'fish-cooked', 'tuna': {'g': 'flatfish', 'under': 'plate'}, 'eel': 'fish-smoking',
'swordfish': {'g': 'angler-fish', 'under': 'plate'}, 'lavaeel': {'g': 'snake', 'under': 'plate', 'badge': 'flame'},
# ---- gems and rare stones: each its own cut
'sapphire': 'saphir', 'ruby': 'fire-gem', 'emerald': 'emerald', 'diamond': 'cut-diamond', 'pearl': 'oyster-pearl',
'void_shard': 'floating-crystal', 'obsidian_shard': 'crystal-growth', 'magma_core': 'unstable-orb',
'pyrax_heart': 'mineral-heart', 'enh_stone': 'rune-stone',
# ---- ores
'copper_ore': 'ore', 'tin_ore': 'stone-pile', 'iron_ore': 'minerals', 'gold_ore': 'gold-nuggets',
'bronze_bar': 'metal-bar', 'iron_bar': 'crystal-bars',
'logs': 'log', 'pine_logs': 'wood-pile',
# ---- potions: each bottle different
'bog_slime': 'snake-jar', 'mana_potion': 'magic-potion', 'hp_potion': 'health-potion', 'big_potion': 'heart-bottle',
'elixir_might': 'fizzing-flask', 'elixir_swift': 'round-bottom-flask', 'elixir_iron': 'square-bottle',
'dragon_draught': 'fire-bottle', 'kraken_ink': 'bottled-shadow', 'fire_ward': 'standing-potion',
'great_mana': 'round-potion', 'tide_tonic': 'spiral-bottle', 'witch_brew': 'potion-of-madness',
# ---- meat
'raw_meat': 'meat', 'meat': 'ham-shank', 'burnt': 'smoking-volcano', 'spiced_kebab': 'kebab-spit', 'raw_beef': 'steak',
'raw_pork': 'bacon', 'raw_mutton': {'g': 'meat', 'badge': 'sheep'}, 'raw_chicken': 'chicken-leg', 'raw_duck': {'g': 'chicken-leg', 'badge': 'duck'},
'lamb_skewers': 'doner-kebab',
# ---- dishes (on a plate)
'cheese_omelette': {'g': 'fried-eggs', 'under': 'plate'}, 'farmers_breakfast': {'g': 'bacon', 'under': 'plate'},
'roast_chicken': {'g': 'roast-chicken', 'under': 'plate'}, 'goat_cheese_salad': {'g': 'monstera-leaf', 'under': 'plate'},
'stuffed_cabbage': {'g': 'sliced-sausage', 'under': 'plate'}, 'steak_frites': {'g': 'steak', 'under': 'plate'},
'roast_duck': {'g': 'duck', 'under': 'plate'}, 'truffle_pasta': 'noodles', 'kings_feast': 'hot-meal',
'sunscar_omelette': {'g': 'sun', 'under': 'plate'},
# ---- bowls and drinks
'stew': 'cooking-pot', 'cinder_stew': 'fire-bowl', 'hot_mead': 'beer-stein', 'bone_broth': 'bubbling-bowl',
'honey_porridge': 'bowl-of-rice', 'hunter_stew': 'camp-cooking-pot', 'tomato_soup': {'g': 'tomato', 'under': 'bowl'},
'corn_chowder': {'g': 'corn', 'under': 'bowl'}, 'chili_con_carne': 'cauldron',
# ---- pies and bakes
'shepherds_pie': 'pie-slice', 'fisherman_pie': {'g': 'fishbone', 'under': 'plate'}, 'pumpkin_pie': {'g': 'pumpkin', 'under': 'plate'},
'strawberry_tart': 'cake-slice', 'bread': 'bread', 'pie': 'meal', 'coconut': 'coconuts', 'pancakes': 'stack',
# ---- crops
'potato': 'potato', 'carrot': 'carrot', 'onion': 'leek', 'cabbage': 'cabbage', 'garlic': 'garlic', 'tomato': 'tomato',
'chili': 'chili-pepper', 'strawberry': 'strawberry', 'pumpkin': 'pumpkin', 'truffle': 'mushrooms',
'wheat': 'wheat', 'corn': 'corn', 'flax': 'grain', 'sugarcane': 'sugar-cane', 'straw': 'round-straw-bale',
'cotton': 'cotton-flower', 'wool': 'wool', 'yarn': 'yarn', 'linen': 'rolled-cloth', 'cotton_cloth': 'sewing-string',
# ---- herbs
'sunpetal': 'sunflower', 'mireroot': 'plant-roots', 'frostbloom': 'snowflake-2', 'emberthorn': 'thorny-vine',
'dragonfern': 'fern', 'ashbloom': 'fire-flower', 'reefmoss': 'coral',
# ---- hides
'boar_hide': 'animal-hide', 'deer_hide': {'g': 'animal-hide', 'badge': 'deer-head'}, 'wolf_pelt': {'g': 'animal-hide', 'badge': 'wolf-head'},
'lurker_moss': 'shambling-mound', 'frost_pelt': 'fur-shirt', 'troll_hide': {'g': 'animal-hide', 'badge': 'troll'},
'jackal_pelt': 'fox-tail', 'raptor_hide': 'dorsal-scales', 'ptera_leather': {'g': 'bat-wing'}, 'ember_hide': {'g': 'animal-hide', 'badge': 'flame'},
'drake_wing': 'feathered-wing', 'sea_silk': 'curled-tentacle', 'grave_pelt': {'g': 'animal-hide', 'badge': 'skull'},
'cowhide': {'g': 'animal-hide', 'badge': 'cow'}, 'pig_hide': {'g': 'animal-hide', 'badge': 'pig'}, 'bear_pelt': {'g': 'animal-hide', 'badge': 'bear-head'},
'croc_hide': 'stegosaurus-scales',
# ---- shells and scales
'scorpion_chitin': 'beetle-shell', 'horn_plate': 'armoured-shell', 'dragon_scale': 'fish-scales', 'salamander_scale': 'shoulder-scales',
'coral_shard': 'sea-star', 'serpent_scale': 'spiral-shell', 'turtle_shell': 'turtle-shell', 'ancient_relic': 'stone-tablet',
# ---- powders
'bone_dust': 'dust-cloud', 'manure': 'fertilizer-bag', 'bone_meal': 'powder-bag', 'compost': 'sprout', 'animal_feed': 'grain-bundle',
'flour': 'flour', 'sugar': 'salt-shaker', 'void_dust': 'pollen-dust',
# ---- teeth, horns, bones
'wolf_fang': 'fangs', 'queen_stinger': 'scorpion-tail', 'tyrant_tooth': 'saber-tooth', 'fossil_horn': 'rhinoceros-horn',
'bear_claw': 'bird-claw', 'croc_tooth': 'tooth', 'bones': 'crossed-bones', 'ram_horn': 'bull-horns',
# ---- farm goods
'egg': 'big-egg', 'duck_egg': 'raw-egg', 'boiled_eggs': 'nest-eggs', 'ostrich_egg': 'dinosaur-egg',
'feather': 'feather', 'down_feather': 'two-feathers', 'ostrich_plume': 'spear-feather',
'milk': 'milk-carton', 'goat_milk': {'g': 'milk-carton', 'badge': 'goat'}, 'cream': 'coffee-cup',
'butter': 'butter', 'cheese': 'cheese-wedge', 'goat_cheese': {'g': 'cheese-wedge', 'badge': 'goat'},
'honey': 'honey-jar', 'beeswax': 'honeycomb',
# ---- odds and ends
'mystery_sack': 'knapsack', 'lucky_charm': 'clover', 'fossil_map': {'g': 'treasure-map', 'badge': 'dinosaur-bones'},
}
# treasure maps: the map of each land, with the land's mark
for k, b, c in [('meadow', 'sheep', '#9ad06a'), ('silkwood', 'spider-web', '#c8b0e0'), ('mire', 'frog', '#6a9a6a'), ('vale', 'crown', '#e8c860'),
                ('frost', 'snowflake-2', '#bfe6ff'), ('sands', 'scorpion', '#e0b860'), ('isle', 'dinosaur-rex', '#d0703a'),
                ('tide', 'sea-serpent', '#3ab0c8'), ('ember', 'volcano', '#ff6a2a')]:
    M['tmap_' + k] = {'g': 'treasure-map', 'badge': b, 'bc': c}
# seeds: a seed bag showing the crop
for crop, g in [('wheat', 'wheat'), ('potato', 'potato'), ('carrot', 'carrot'), ('onion', 'leek'), ('cabbage', 'cabbage'), ('garlic', 'garlic'),
                ('tomato', 'tomato'), ('corn', 'corn'), ('flax', 'grain'), ('chili', 'chili-pepper'), ('strawberry', 'strawberry'),
                ('sugarcane', 'sugar-cane'), ('pumpkin', 'pumpkin'), ('cotton', 'cotton-flower'), ('sunpetal', 'sunflower'), ('frostbloom', 'snowflake-2')]:
    M[crop + '_seed'] = {'g': 'paper-bag-folded', 'c': '#c8a878', 'badge': g}
# reins: a saddle with the mount's head
for k, g in [('horse', 'horse-head'), ('destrier', 'spartan-helmet'), ('direwolf', 'wolf-head'), ('raptor', 'velociraptor'), ('goldsteed', 'unicorn'),
             ('nightmare', 'horse-head'), ('bogstrider', 'frog'), ('glacierwolf', 'direwolf'), ('dunestrider', 'camel-head'), ('tidelizard', 'seahorse'),
             ('emberlizard', 'salamander'), ('voidwing', 'bat-wing'), ('gladiator', 'spartan'), ('fossilraptor', 'dinosaur-bones')]:
    M['reins_' + k] = {'g': g, 'badge': 'saddle'}
M['reins_nightmare']['c'] = '#4a3a6a'
# monster cards: a card with the monster on it
CARDS = {'fskel': 'skeleton', 'fsentinel': 'raise-skeleton', 'fhound': 'hound', 'fnight': 'floating-ghost', 'fwarlord': 'horned-skull',
         'tricebone': 'triceratops-head', 'deer': 'deer', 'boar': 'boar', 'wolf': 'wolf-head', 'shadowwolf': 'werewolf', 'goldhorn': 'stag-head',
         'mistfang': 'wolf-howl', 'bogtoad': 'frog', 'lurker': 'shambling-mound', 'mirehulk': 'troll', 'bogmother': 'witch-face', 'crab': 'crab',
         'bandit': 'bandit', 'banditchief': 'hooded-assassin', 'boarlord': 'boar-tusks', 'alphawolf': 'direwolf', 'icewolf': 'snowflake-2',
         'troll': 'cyclops', 'rimeclaw': 'polar-bear', 'jackal': 'fox-head', 'scorpion': 'scorpion', 'sandqueen': 'queen-crown', 'sandwraith': 'hooded-figure',
         'raptor': 'velociraptor', 'pterra': 'pterodactylus', 'hornback': 'horned-reptile', 'tyrant': 'dinosaur-rex', 'wyrm': 'dragon-head',
         'reefcrab': 'sad-crab', 'pirate': 'pirate-skull', 'turtle': 'sea-turtle', 'seaserpent': 'sea-serpent', 'drowned': 'shambling-zombie',
         'saltbeard': 'pirate-captain', 'coralking': 'giant-squid', 'thalassa': 'mermaid', 'cinderhound': 'basset-hound-head', 'magmaimp': 'imp',
         'salamander': 'salamander', 'obsidiangolem': 'rock-golem', 'emberdrake': 'spiked-dragon-head', 'emberwarden': 'metal-golem-head',
         'magmatitan': 'giant', 'emberback': 'lizardman', 'pyraxis': 'dragon-breath', 'grull': 'blacksmith', 'w_bogking': 'frog-prince',
         'f_hrimgar': 'ice-golem', 't_anubek': 'anubis', 'm_ignis': 'burning-skull', 'h_nyx': 'evil-moon', 'riftling': 'magic-portal',
         'voidmaw': 'carnivore-mouth', 'bear': 'bear-head', 'croc': 'croc-jaws', 'ostrich': 'ostrich', 'ram': 'ram', 'thickskull': 'minotaur',
         'spikeback': 'porcupine'}
for k, g in CARDS.items(): M['card_' + k] = {'g': g, 'under': 'card'}
