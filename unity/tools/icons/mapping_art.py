# item id -> painted icon from the CraftPix "6200 Fantasy RPG Icons Pack" (folder key, number).
# Optional: under ('plate'), tint (multiply colour, for cooked/blackened versions), badge (game-icons glyph or ('ART', key, n)).
F = {'FI': 'Fishing Icons', 'FO': 'Food Icons', 'CI': 'Cooking Ingredients Icons', 'VE': 'Vegetable Icons', 'FR': 'Fruit Icons',
     'BS': 'Berry and Seed Icons', 'FA': 'Farming Icons', 'MI': 'Mining Icons', 'CR': 'Craft Resources Icons', 'AH': 'Alchemical Herbs Icons',
     'AP': 'Alchemical Potions Icons', 'EL': 'Elixir Icons', 'LI': 'Liquid Icons', 'JE': 'Jewelry Icons', 'H1': 'Hunting Icons 1',
     'H2': 'Hunting Icons 2', 'ML': 'Monster Loot Icons', 'DL': 'Dragon Loot Icons', 'SL': 'Sea Loot Icons', 'BO': 'Bones Icons',
     'SC': 'Scroll Icons', 'L1': 'Loot Icons 1'}
A = {
# fish: every fish its own, cooked ones on a plate
'raw_trout': ('FI', 22), 'raw_salmon': ('FI', 20), 'raw_tuna': ('FI', 27), 'raw_eel': ('FI', 25), 'raw_swordfish': ('FI', 24),
'raw_lavaeel': {'a': ('FI', 25), 'tint': '#ff7040'},
'trout': ('FO', 1), 'salmon': ('FO', 27), 'tuna': ('FO', 47), 'eel': {'a': ('FI', 25), 'under': 'plate', 'tint': '#a07850'},
'swordfish': {'a': ('FI', 24), 'under': 'plate', 'tint': '#d0a070'}, 'lavaeel': {'a': ('FI', 25), 'under': 'plate', 'tint': '#704030'},
# gems and rare stones
'sapphire': ('CR', 21), 'ruby': ('ML', 13), 'emerald': ('MI', 40), 'diamond': ('MI', 45), 'pearl': ('SL', 10), 'void_shard': ('MI', 43),
'obsidian_shard': ('CR', 9), 'magma_core': ('MI', 38), 'pyrax_heart': ('L1', 41), 'enh_stone': ('L1', 31),
# ores, bars, wood
'copper_ore': ('MI', 37), 'tin_ore': ('MI', 31), 'iron_ore': ('MI', 32), 'gold_ore': ('MI', 30), 'bronze_bar': ('CR', 19), 'iron_bar': ('CR', 18),
'logs': ('CR', 22), 'pine_logs': ('CR', 25),
# potions
'bog_slime': ('ML', 10), 'mana_potion': ('AP', 12), 'hp_potion': ('AP', 11), 'big_potion': ('AP', 22), 'elixir_might': ('AP', 24),
'elixir_swift': ('AP', 37), 'elixir_iron': ('AP', 33), 'dragon_draught': ('AP', 9), 'kraken_ink': ('EL', 39), 'fire_ward': ('AP', 28),
'great_mana': ('AP', 23), 'tide_tonic': ('AP', 25), 'witch_brew': ('AP', 16),
# meat
'raw_meat': ('H1', 1), 'meat': ('FO', 26), 'spiced_kebab': ('FO', 5), 'raw_beef': ('H1', 9), 'raw_pork': ('H1', 14), 'raw_mutton': ('H1', 13),
'raw_chicken': ('H1', 10), 'raw_duck': ('H1', 16), 'lamb_skewers': ('FO', 3),
# dishes and bowls
'cheese_omelette': ('FO', 24), 'farmers_breakfast': ('FO', 18), 'roast_chicken': ('FO', 34), 'goat_cheese_salad': ('FO', 35),
'stuffed_cabbage': ('FO', 45), 'steak_frites': ('FO', 28), 'roast_duck': ('FO', 19), 'truffle_pasta': ('FO', 38), 'kings_feast': ('FO', 40),
'sunscar_omelette': ('FO', 29), 'stew': ('FO', 31), 'cinder_stew': ('FO', 42), 'hot_mead': ('FO', 6), 'bone_broth': ('FO', 33),
'honey_porridge': ('CI', 40), 'hunter_stew': ('FO', 36), 'tomato_soup': ('FO', 44), 'corn_chowder': ('FO', 41), 'chili_con_carne': ('FO', 37),
'shepherds_pie': ('FO', 22), 'fisherman_pie': ('FO', 46), 'pumpkin_pie': {'a': ('CI', 47), 'under': 'plate'}, 'strawberry_tart': ('FO', 25),
'bread': ('FO', 11), 'pie': ('FO', 12), 'coconut': ('FR', 34), 'pancakes': ('FO', 21),
# crops
'potato': ('VE', 6), 'carrot': ('VE', 11), 'onion': ('VE', 18), 'cabbage': ('VE', 14), 'garlic': ('VE', 28), 'tomato': ('VE', 7),
'chili': ('VE', 32), 'strawberry': ('BS', 26), 'pumpkin': ('VE', 1), 'truffle': ('ML', 6), 'wheat': ('CI', 44), 'corn': ('VE', 23),
'flax': ('AH', 13), 'sugarcane': ('AH', 43), 'straw': ('CR', 28), 'cotton': ('CR', 46), 'wool': ('CR', 40), 'yarn': ('CR', 47),
'linen': ('CR', 42), 'cotton_cloth': ('CR', 43),
# herbs
'sunpetal': ('AH', 1), 'mireroot': ('AH', 44), 'frostbloom': ('AH', 33), 'emberthorn': ('AH', 42), 'dragonfern': ('AH', 30),
'ashbloom': ('AH', 6), 'reefmoss': ('SL', 26),
# hides
'boar_hide': ('H2', 31), 'deer_hide': ('CR', 33), 'wolf_pelt': ('H2', 34), 'lurker_moss': ('ML', 9), 'frost_pelt': ('H2', 35),
'troll_hide': ('H1', 36), 'jackal_pelt': ('H2', 30), 'raptor_hide': ('H2', 49), 'ptera_leather': ('H2', 7), 'ember_hide': ('CR', 32),
'drake_wing': ('DL', 4), 'sea_silk': ('CR', 41), 'grave_pelt': ('ML', 40), 'cowhide': ('H2', 43), 'pig_hide': ('H2', 45),
'bear_pelt': ('H2', 25), 'croc_hide': ('H1', 29),
# shells and scales
'scorpion_chitin': ('L1', 16), 'horn_plate': ('DL', 12), 'dragon_scale': ('DL', 5), 'salamander_scale': ('H2', 16), 'coral_shard': ('SL', 25),
'serpent_scale': ('H2', 11), 'ancient_relic': ('L1', 47),
# powders
'bone_dust': ('BO', 23), 'bone_meal': ('CI', 30), 'compost': ('FA', 27), 'animal_feed': ('FA', 26), 'flour': ('FA', 29), 'sugar': ('CI', 24),
'void_dust': ('ML', 27),
# teeth, horns, bones
'wolf_fang': ('BO', 20), 'tyrant_tooth': ('H1', 40), 'fossil_horn': ('H2', 12), 'bear_claw': ('H2', 10), 'croc_tooth': ('BO', 9),
'bones': ('BO', 11), 'ram_horn': ('H2', 4), 'queen_stinger': ('L1', 39),
# farm goods
'egg': ('ML', 26), 'boiled_eggs': ('CI', 37), 'ostrich_egg': ('ML', 20), 'feather': ('H2', 23), 'down_feather': ('CR', 44), 'ostrich_plume': ('H1', 31),
'milk': ('LI', 49), 'goat_milk': ('CI', 22), 'cream': ('CI', 34), 'butter': ('CI', 39), 'cheese': ('CI', 15), 'goat_cheese': ('CI', 48),
'honey': ('LI', 44), 'beeswax': ('ML', 30),
# odds and ends
'mystery_sack': ('MI', 16), 'lucky_charm': ('AH', 32), 'fossil_map': {'a': ('SC', 41), 'badge': 'dinosaur-bones'},
}
for k, b, c in [('meadow', 'sheep', '#9ad06a'), ('silkwood', 'spider-web', '#c8b0e0'), ('mire', 'frog', '#6a9a6a'), ('vale', 'crown', '#e8c860'),
                ('frost', 'snowflake-2', '#bfe6ff'), ('sands', 'scorpion', '#e0b860'), ('isle', 'dinosaur-rex', '#d0703a'),
                ('tide', 'sea-serpent', '#3ab0c8'), ('ember', 'volcano', '#ff6a2a')]:
    A['tmap_' + k] = {'a': ('SC', 41), 'badge': b, 'bc': c}
# seeds: the farm's seed sack with the crop on it
for crop in ['wheat', 'potato', 'carrot', 'onion', 'cabbage', 'garlic', 'tomato', 'corn', 'flax', 'chili', 'strawberry', 'sugarcane', 'pumpkin', 'cotton', 'sunpetal', 'frostbloom']:
    base = A[crop]
    A[crop + '_seed'] = {'a': ('FA', 28), 'badge': ('ART',) + tuple(base if isinstance(base, tuple) else base['a'])}
