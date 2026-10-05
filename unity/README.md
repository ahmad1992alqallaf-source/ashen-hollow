# Ashen Hollow: Unity slice v0.3 (Hollow Meadow)

The first piece of Ashen Hollow running in Unity: Hollow Meadow taken straight out of the web game
(ground, trees, mountains, the camp, the lake), your hero, boars, deer and wolves, combat, levels,
living water, and a sky with a day and night cycle.

## Set up (once, about 10 minutes)
1. **Unity Hub > New project > Unity 6 (6000.0 LTS or newer) > "Universal 3D" template.** Create it.
2. In Unity: **Window > Package Manager > "+" (top left) > Add package by name**, type
   `com.unity.cloud.gltfast` and press Add. This reads the .glb models.
3. Copy this whole **AshenHollow** folder into your project's **Assets** folder
   (drag it onto "Assets" in the Project window). Wait for the import to finish.
4. Menu bar: **Ashen Hollow > Set Up Meadow Scene**. It makes the water and sky materials and the scene.
5. Press **Play**.

## Updating from an older version
In Unity's Project panel, right-click the old **AshenHollow** folder > Delete. Drag the new one onto Assets,
then run **Ashen Hollow > Set Up Meadow Scene** again (it makes the new materials).
**Ashen Hollow > Reset Saved Progress** clears your class and level so you can pick again.

## Classes
Warrior, Mage, Priest, Rogue, Ranger and Druid, each with five spells from the web game.
You choose on first Play; the CLASS button (top right) changes class at any time.

## Built from the web game's data (v67)
Everything now comes from `Resources/AH/Data` (see HANDOFF.md): items, monsters, classes, quests, townsfolk,
recipes and professions, with the web game's rules.
- **Town:** Captain Mara gives the 80 story quests (tracker top right; gold ! and ? over the quest-giver).
  The 9 Ashen Hollow villagers talk, and Guild Clerk Mina opens the professions window. Town is a safe zone.
- **Gathering:** walk up to a tree, rock, herb or fishing spot and press the big button (or E).
  Trees fall and grow back, rocks and herbs refill. With logs you can light a campfire in the wild.
- **Stations:** cook at a fire, smelt at the furnace, make gear and potions at the anvil, loom, cauldron and jeweler's bench.
- **Professions:** take up any of the 8 at the Guild: +25% skill XP, mastery, perks and a specialisation at mastery 15.
  Artisans level their class through their work and follow the Artisan's Road.
- **Saving:** everything is kept: hero, bag, gear, money, skills, class levels, quests, the road, professions, where you stood.
- **The whole world of the web game** (31 areas): the mainland regions and every road between them, the cities
  (Varrow, Highcairn, Mirewatch, Sunspire Oasis, Cinderhold, Coralport) through their gates, and over the water
  Dragonscale Isle, Tidewake Isles and the Fossil Lands (talk to the ship captains in Varrow's harbour),
  plus Emberreach by Ashfall Pass. Every area has its own monsters with the web game's models: wolves, raptors,
  the Tyrant king, the dragon, skeletons that rise from the ground, cinder hounds, elemental golems.
- **Potions:** HP and MP quick buttons (H / J), elixirs from the bag.
- **Looks:** new sky, water you can wade into (ripples), detailed ground, and a hero built from your creator choices
  (the 'Look Hero' switch on the Ashen Hollow object brings back the old heroes).
- **Dungeons:** the Sunken Forge (burning portal in Silkwood, Forgemaster Grull) and five delves with doors in the
  regions: the Mire Warrens (Duskmire), Frostpeak Caverns (Frostfang), the Tomb of the Sun King (Sunscar),
  the Molten Depths (Emberreach) and the Hollow Below (the meadow well, opened by the story). Four rooms each:
  an iron gate opens when the room behind you is cleared, a mini-boss guards room 2, the boss waits in the last hall.
  Bosses warn before they strike (red circles fill up, then hit), call helpers at 60% and 30%, and rage below 30%.
  The boss leaves a chest with a piece of that dungeon's set for your class plus gems; the first clear pays a big bonus,
  and a cleared dungeon seals for 20 minutes. Underground the hero carries a lantern and the camera keeps out of the rock.
- **Shops and the bank:** walk up to any shop door (26 shops across the cities and Ashen Hollow) and press the big
  button: buy at the web game's prices, sell from your bag (shops pay extra for the trade goods they want).
  Bank vaults in every city keep whole stacks safe and never fill up.
- **Companions (PETS button):** pets follow you and level up to learn Fetch, Forager, Guard, Mend and Loyal;
  mounts from the stables (RIDE button or R) make you up to twice as fast and get faster as you ride;
  hire up to two sellswords at a tavern (Sir Aldric tanks, Wren shoots, Sister Mae heals). All of it is saved.
- **The Auction House:** auctioneers in the cities run a living market: browse and bid, buy out,
  list your own goods; sold coins and won items arrive in your mail.
- **Guild work orders:** take orders for your profession, deliver the goods for pay, mastery and guild rank.
- **Your homestead:** buy a deed from a Land Agent (Varrow or Ashen Hollow), then build a house, six fields and
  animal pens, plant and water crops, collect eggs, milk and wool, run a market stall, add home comforts,
  and sleep at home for rested XP (double XP from kills until it runs out).
- **MENU button:** the Dungeon Finder, daily goals, deeds, reputation, enhancement, companions, work orders, going home.
- **Dungeon Finder:** pick a role (Tank, Healer or Damage) and "Go now": followers fill the open roles, your own
  sellswords wait outside. Tanks get +30% HP and take less damage, healers heal the group every 3 s, damage deals +15%.
  Tanks and healers get a Satchel of Helpful Goods after the boss.
- **Raid, the Ember Throne** (level 50+, from the Dungeon Finder): Vaelor, the Ember King, a burning titan that
  grows with your level and party. Flame circles under you every 6 s, a ring of fire every 13 s, ember imps at 70%
  and 35%, ember rain at 50%, enraged after 7 minutes. His Kingsflame set (shoulders, legs, boots, cape) drops once a week.
- **Well Fed:** dishes from your kitchen come out plain, Fine or Masterwork; eating one gives its buff
  (damage, armor, max HP, speed, attack speed, mana, gathering, XP or healing) for its time. Shown under your purse.
- **Towns and reputation:** the seven towns remember what you do nearby. Friendly, Honored (5% off in their shops,
  their tabard), Revered (10% off, their signet ring), Exalted. Wear a town's tabard to champion it everywhere.
- **Bounty boards** in every city: four hunts, take up to three, paid in coin, class XP and reputation; plus a daily writ.
- **Deeds:** 72 achievements, each with a title to wear under your name.
- **Daily goals** (chip under your purse): three daily goals, three weekly challenges, a 7-day login calendar,
  and Adventurer's Marks to spend in the marks shop (potions, gems, an owl pet, a direwolf mount). Mystery sacks open from the bag.
- **Enhancement:** raise weapons and armor to +9 with enhancement stones (monsters drop them, bosses more) and coins.
  From +3 it can fail; from +6 a failure drops the piece a level unless a Lucky charm protects it.
- **Timed events** (chip under the daily chip): seven rare monsters, each out 25 minutes every 2 hours, and Voidmaw,
  the world boss, at 1 pm and 9 pm in the Riven Crater. Each can be slain once per appearance.
- **Waystones and hidden treasure:** walk near a waystone to attune it, touch one to travel to any you have attuned
  (or home). 28 hidden treasure chests across the world, each with coins and an item.
- **Map** (M, or Menu → Map): a picture of the area you are in, with you, waystones, dungeon doors, the ways out and bounty boards.
- **Stats** (Menu → Stats): a point every class level for STR, END, DEX, INT or SPR.
- **Gem sockets** (Menu → Gem sockets): rubies, sapphires, emeralds, diamonds and void shards in your gear slots.
- **Monster cards and licenses** (Menu → Collection): cards drop from monsters (tap twice in the bag to add them):
  +1% damage against that monster and a bonus for each finished set. 25/100/300/1,000 kills of one kind give +3/6/10/15% against it.
- **Class progression** (Menu → Class): at level 30 pick one of two paths (a new spell replaces one of the five,
  plus passives), at level 60 one of its two forms (an ultimate on a sixth button, ★ or key 6). A talent point every
  3 levels in three tiers. The spellbook: ten more spells per class, one every 5 levels; choose the five on your ring.
  (The Pyromancer's Phoenix form is left out of this port.)
- **Friends of the realm** (Menu → Friends): 22 townsfolk. Chat once a day and bring one gift a day (favourites count
  most); five hearts each, and every heart sends a letter with a present. Each weekday has its own small festival
  (a skill learns faster; at the weekend friendship grows twice as fast).
- **Barbers** (Barber Finn in Ashen Hollow, Barber Lyra in Varrow) change your hair, face, body or name for a price;
  the mirror in your homestead house restyles hair and face for free.
- **Graves in the Fossil Lands:** dig for coins, bone dust and ancient relics, but the dead may wake.
- **Kingdom Quests** (Trial-master Garron in Varrow, Arena crier Bess in Ashen Hollow): every hour at :00 the Siege
  (five waves, then the Arena champion), at :30 the Gold Rush (catch gold imps), each open 10 minutes, matched to your level.
- **Festivals:** the Harvest Fair (mid-September to mid-October), Lantern Nights in Ramadan (and an Eid gift), and the
  Winter Feast. A festival stall and decorations by the waystones of Ashen Hollow and Varrow, three daily tasks for
  tokens, a town activity (carve pumpkins, light lanterns, open your gift), cosmetics, a festival pet and a title.
- **Pet battles** (Menu → Pet battles): turn-based battles with up to three pets against the realm's six tamers.
- **Merchants' Quarter** (the Market Warden in every city): rent a stall in your trade, stock it, set your prices;
  townsfolk buy even while you are away and the takings wait in your till.
- **Night market:** after dark, lantern stalls open by the waystone in every city: a cook, a herbalist, a curio dealer
  and a travelling trader, with new goods every night below shop prices.
- **Wardrobe** (Menu → Wardrobe): cosmetics no longer take a bag slot; they go straight to your Wardrobe together with
  every piece of gear you have ever owned. Choose what each slot shows (your gear, any collected piece, or nothing for
  head and cape) and save up to five outfits. Stats always come from your real gear. What you show is now drawn on the
  hero: helms, hoods, crowns, horns and hats, shoulder pads, breastplates or robes, gauntlets, greaves, boots, and
  cloaks, coat-tails, wings or quivers that swing as you run. Deed: *Fashion Icon* (12 cosmetics).
- **City house** (green sign, near the square in every city): rent it by the week for a bed that fills rested XP
  and restores you, your bank chest, and waking up at home after a fall in that city.
- **Spell sparks:** every kind of spell has its own particle signature (bolt trails, slash sparks, nova rings,
  falling embers, healing spirals, vanish smoke), with sparks on every hit, dust on every kill and a golden
  fountain on level-up. The materials are made automatically (or by *Set Up Meadow Scene*).
- **Ashen Phoenix** (rare mount): a great bird of living flame that hovers, beats its wings faster as you run and
  sheds embers; the fastest mount (+115%). Drops from Emberback the world boss (3%) and from Vaelor in the Ember
  Throne raid (6% on your weekly loot). Rare-mount reins in your bag can now be used to learn the mount.
- **Sellswords** now have real, animated bodies dressed in kit drawn from the game's items: Sir Aldric in steel
  plate with sword, shield and a red cape; Wren in a green hood with a quiver and crossbow; Sister Mae in holy robes
  with wand and book. They walk, fight, fall and get back up with proper animations.
- **Better cities:** every house, shop, temple and hall in the six cities is now a detailed building (KayKit
  Medieval Hexagon, CC0) on the same lot and facing the same street: homes, a church, a fortified town hall, the
  smithy with its furnace, market stalls, the tavern with its great barrel and the alchemist's tower, with a roof
  colour for each city, and street life in front of them (crates and sacks at the markets, weapon racks at the
  smithies, barrels at the taverns, banners at the halls, timber at the stables); the houses of the villages and camps out in the wild are rebuilt the same way. Collisions
  are unchanged (the original area models stay; a copy with the old houses cut out, *_world_kk.glb, is loaded).
- **People among the monsters:** road bandits and their chief, Tidewake pirates and Captain Saltbeard, the frost and
  toad cultists and the High Priest of the tomb now have rigged, animated bodies with real weapons and kit (hoods,
  tricorns, coats, robes, plate, cloaks) instead of cylinder figures.
- **Better trees:** every tree in the realm (the ones you chop and the ones that only stand in the woods) is a new
  stylised tree on the same spot: leafy trees with lumpy rounded crowns and branches, pines with drooping jagged
  tiers, light on top and shaded underneath, in their area's colours (snowy Frostfang pines, ash-black Ember pines,
  olive mire trees). The crowns sway in the wind; a felled tree leaves a stump and grows back.
- **Better ore rocks:** every mining rock is a chunky boulder with crystals of its ore growing out of it, in the ore's
  colour (copper, tin, iron, gold, obsidian, coral); a mined-out rock loses its crystals and goes dull until it is back.
- **Dressed dungeons:** the caves are no longer bare rock: iron torches on posts line the walls with flickering light
  and drifting embers (blue-white in Frostpeak, hot orange in the Molten Depths, sickly green in the Mire Warrens,
  gold in the tomb), with stacked crates and barrels, kegs, trunks, rubble and broken shields between them, and by
  each boss two decorated pillars and a chest of gold. Nothing blocks the way.
- **Real animals:** boars, deer, jackals and the rare wolves are no longer built from blocks. They are animated
  Quaternius animals (CC0), reshaped and recoloured for Ashen Hollow: wild boars with a hump, a bristle crest, a snout
  and tusks (Old Tusker grizzled with long tusks, Goldhorn gleaming gold); stags and does in the meadows; sandy dune
  jackals and the black, gold-collared Jackal wardens of the tomb; the white Whitepine Alpha, ghostly Mistfang with
  glowing eyes and Rimeclaw with ice crystals along its back. They idle, walk, gallop, attack, flinch and fall with
  their own animations.
- **More monsters on proper models:** forge, magma and depth imps and the void riftlings are winged demons; the
  Ember drake and the Pterra are dragons; the Hornback is a great horned dinosaur.
- **Living mounts:** every mount is now an animated animal with a saddle, blanket and stirrups that stands, breathes and
  lowers its head while you wait and gallops while you ride: the chestnut horse, the barded destrier (red caparison,
  steel face plate, plumed), the golden Goldhorn steed with its horn, the violet-maned Mistfang nightmare, the
  gladiator's warhorse in gold and crimson, the winged Voidwing, the frost dire wolf and the ice-spined glacier wolf,
  and five saddled raptors (island green, bogstrider, sand-gold dunestrider, coral-crested tidecrest runner,
  obsidian Emberback runner).
- **Townsfolk dressed for their work:** every townsperson in every city now wears clothes that fit who they are:
  gate guards in their city's armour and tabard with sword and shield, captains in tricorns and long coats, sailors,
  caravan masters in desert veils, merchants, robed archivists and seers, a jarl with a crown and a great axe, the
  bard, the lantern-keeper with his lantern, trappers and scouts with crossbows, farmers in straw hats. Men and women,
  hair and height vary. Granny Wormwood's cat is a real black cat now.
- **City walls and towers:** the six cities' walls are KayKit castle walls with battlements, and their towers round
  watchtowers in the city's colour; each gate has two tall lookout towers with pennants and a lintel across. You are
  stopped exactly where you were before.
- **Weapons held properly:** every kind of weapon now has its own grip (from the hand bones): swords, axes and wands
  in a fist with the blade out on the thumb side, staffs upright at the side, crossbows held by the grip, shields
  strapped to the outside of the forearm. Applies to the hero, sellswords, townsfolk and the people among the monsters.
  (Tick *Tune Grip* on the Game object to try a grip live with Weapon Rot / Weapon Offset.)
- **Pets:** the ember and snow fox kits are animated Quaternius foxes that walk, run and sit about; the baby dragon is a
  small ember-red dragon that scampers beside you.
- **Herbs:** every herb is a little plant of its own kind instead of a green blob: golden sunpetal daisies, ashbloom with
  pale petals and ember hearts, glowing ice-crystal frostbloom, emberthorn bristling with glowing thorns, mireroot's
  lilac bulbs, dragonfern's arching fronds and red fiddleheads, and reefmoss sprouting pink coral. Picking takes the
  flowers and dulls the leaves until it grows back. (*Herb Snapshots* in the menu shows all seven.)
- **Campfires, furnaces and anvils:** campfires are a ring of stones round glowing embers with logs leaning together and
  live, licking flames, a breathing firelight and drifting sparks (your own campfires too); furnaces are laid up from
  rough stone bricks with an arched, glowing mouth and a brick chimney; anvils are horned iron anvils on a tree stump
  with a hammer on top. (*Station Snapshots* in the menu.)
- **Waystones:** a stepped stone platform ringed with leaning standing stones, a tapered pillar with glowing runes down
  its faces, and a crystal floating and turning above it.
- **Chests, boards and night stalls:** hidden treasures are iron-banded KayKit chests and the bosses' loot chests gold
  ones (KayKit Dungeon Remastered, CC0), with lids that swing open; bounty boards have log posts, a planked board under a
  little shingled roof, notices pinned at all angles and a lantern; the night market's traders sell from KayKit stalls.
- **Street lamps:** Varrow's lamps are black iron posts with a curled arm and a glass lantern that lights the street
  after dark (only the lamps near you shine, so a street full of them stays cheap).
- **Fountain:** Varrow's plaza fountain is an eight-sided stone basin with a rippling pool, a carved pedestal and
  bowl, a gold finial and eight arcing streams of water.
- **Braziers:** the arena's torch posts and the Ember Throne's ring of fires are carved stone pillars topped with
  iron fire bowls of glowing coals and live, flickering flames. The arena's team banners hang from wooden poles
  with gilt crossbars, gold hems and swallow-tailed ends.
- **The Ember Throne:** the raid boss sits on a throne of dark stone on a three-step dais with a red carpet, a tall
  pointed back, gold trims, curling horns and a burning brazier at each side; the arena is ringed by fluted volcanic
  columns with gold bands and an ember bowl burning on top; its lava pools have rocky rims, floating crust and
  glowing bubbles that swell and pop in sparks.
- **Stalls, wells and festivals:** the shop booths in the camps and cities are KayKit market stalls in the area's colour,
  the Hollow Meadow well a stone KayKit well; the festival stall is a market stall in the festival's colour, the Harvest
  Fair has ribbed pumpkins with stems and leaves beside crates and sacks, and the Winter Feast little pines with gifts.
- **Sea and swamp beasts:** shore crabs are real sand crabs that scuttle sideways (they turn side-on to where they go); reef crabs (teal) and the Coral
  King (coral red, huge) are a spiked giant crab with idle, walk, two claw attacks and a death; Shellback turtles paddle
  along the shore; Mire toads are warty orange toads and the Warren toads a fanged frog beast, both hopping in arcs
  with a squash as they land and breathing while they sit. Crabs, turtles and toads die on their backs. The Tide serpent is a long-necked sea serpent with a fin crest whose
  bones are moved in code: waves roll down its body and tail, its neck sways, its head looks about and its four
  flippers paddle.
- **Feathered wings:** the wings cosmetic is now a pair of real feathered wings: a curved gold-edged arm, two rows
  of coverts and nine long flight feathers fanning out to the tip, beating slowly and faster while you run. The Ashen Phoenix now has the same
  feathered wings, much larger: burning orange flight feathers over a golden layer, red coverts and a gold edge.
- **Desert and undead beasts:** sand, tomb and arena scorpions are a fully animated scorpion (idle, walk, a sweeping
  attack, a guard when hit and a death); Pterra is an animated pteranodon that stands, walks and takes wing to chase;
  Sandwraiths and Starving shades are a hooded reaper with a rune scythe that drifts above the ground, bobbing and
  leaning in, and the Bone sentinel a robed skeleton mage with a crystal staff hovering just off the ground. The Night-risen that walk the grave lands after dark are a
  skeletal bone drake with glowing blue eyes.
- **The Ashen Phoenix** is now a real animated firebird, jewel green with a long flaming peacock tail, that flies
  beneath you, beats faster as you run, glows and trails embers.
- **Dragonscale Isle dinosaurs:** raptors are now a detailed, fully animated velociraptor (idle, stalking walk, run,
  bites and a leaping pounce, hit reactions and a death) and two new beasts roam the isle: packs of Thickskulls
  (pachycephalosaurs that charge and head-butt) and lone Spikebacks (huge stegosaurs that leave you alone until you
  strike, then lash their spiked tails and stomp). They have real voices: screeches and roars when they spot you,
  growls as they bite, yelps when hit and death cries.
- **Spell icons:** every spell (all 125, from the starting five to the evolution ultimates) has its own painted round
  icon in the spell's colour, on the spell buttons and in the spellbook. The buttons dim while cooling down and turn
  grey-blue when you lack the mana; when you cast, the button pops and the spell's name floats up above it.
- **Casting animations:** spells now play their own moves: forward and sideways sword cuts, a leaping heavy blow, a
  rogue's quick jab, a spinning slash for Whirlwind and Blade Storm, a ground slam for Leap Slam and Ground Slam, a
  two-handed crossbow shot for the ranger, a hand thrust for blasts, a throw for bombs, a battle roar for war cries and
  Bear Form, kneeling to set a trap or bless the ground, a lunge for Charge, a hop back for Disengage and a roll for
  Vanish and Evasion. The Dodge button rolls too, and basic sword attacks alternate two different cuts.
- **Spell sounds:** each element has its own cast and impact sound: fire roars and crackles, frost chimes and
  shatters, arcane hums and zaps, lightning crackles and thunders, holy spells swell like a choir and ring like a bell,
  nature rustles and thumps, star spells twinkle, shadow swells darkly, poison bubbles and hisses, blades ring, the
  earth rumbles, arrows twang and thunk, and war cries shout.
- **Spell shots:** arrow spells and the ranger's crossbow fire real bolts with steel heads and red fletching; frost
  flies as long ice shards; other bolts are glowing orbs with a white-hot core (arcane, star and shadow ones spin).
- **Real trees:** every leafy tree is now a full broadleaf tree with leaf-card crowns and bark branches, and every
  pine a spruce with drooping needle boughs. They keep each area's colours (green meadows, olive mire, dark Frostfang
  spruces, ash-black Ember pines), are darker low and inside the crown, sway in the wind, and a felled tree still
  leaves a stump.
- **Mountains:** the plain seven-sided cones around every area are now craggy rock mountains with ridges, gullies and
  crooked summits, in the area's own colour.
- **Bridges and ruins:** the river crossings are wooden footbridges (stone abutments, an arched plank deck, posts and
  handrails) instead of grey slabs, and the ancient ruins are broken fluted stone columns with fallen drums, rubble and
  moss instead of brown poles. The ice spires of Frostfang, Whitepine and Highcairn and Kingsvale's rift stones are
  clusters of faceted glowing crystals. The palms of Tidewake and Sunspire have curved ringed trunks, drooping
  feathery fronds and coconuts, and the desert cacti are ribbed saguaros with bent arms and pink flowers.
  In the Fossil Lands the bone spikes are great curved tusks rising from mounds of earth and the graves have
  weathered headstones (round, cross or broken) over grassy mounds; Tidewake's coral is reef clusters of branching
  coral, sea whips and brain coral. The border peaks that some areas drew as one batch of cones (the cities, the
  Fossil Lands, Emberreach, your homestead) are craggy mountains too.
- **Landmarks:** Kingsvale's windmill is a timbered windmill whose sails turn, its keep a castle of three KayKit
  towers, and the lone web watchtowers outside the walled cities are KayKit watchtowers. Tidewake's wreck is the ribs
  of a beached ship with a snapped mast and torn sail. The city statues are stone knights, and the desert obelisks are tapering sandstone obelisks with glyph bands
  and a gilded tip. Varrow's training dummies are straw-stuffed sacks on posts with painted targets. Emberreach's obsidian
  spikes and the black rocks round the dragon's lair are clusters of black glass crystals, and Dragonscale Isle's
  ferns have arching fronds. The bank is an iron-bound gold strongbox on a plinth with coin stacks and a lantern.
  The web game's own waystones, treasure chests, bounty boards and bank chests no longer show through the new ones.
- **Bounty boards:** the web game's flat board no longer shows through the new one, the notices face the road,
  and a gold "!" floats over the board when one of your bounties is ready to claim.
  *Test: Go To Next Landmark* hops between the bridges, ruins, crystals, palms, cacti, bones, coral, graves and camp tents of an area.
- **Item icons:** every item in the bag, gear slots, shops, crafting stations and the bank shows a picture of the
  thing on its coloured circle (a log, ore, a bar, a fish, a helm, a hood, a robe, boots, a ring, a sword, a bow, a
  staff, a potion, a pie, a card...), 75 pictures chosen by the item's form, instead of three letters. The auction
  house and the enhancing list show them too.
- **Coralport** stands on its sandy plaza again (the sea plane no longer covers the town).
- **Mushrooms** in Mirewatch and the mire are real toadstools (stem, gilled cap, spots) instead of flat purple discs.
- **Your homestead:** the first home is a canvas ridge tent with a log seat and a fire ring (no more pyramid), the
  second a timbered cottage and the third a manor with a corner tower and a cottage wing (KayKit Medieval Builder).
  Unbuilt plots have a framed signboard and are pegged out with rope.
  *Test: Area Overview Shots* saves six views of the current area to HeroShots; *Test: Cycle House Tier* shows each home.
- **Sunspire palace:** a real desert palace instead of a box under a glossy ball: stepped plinth, walls with a blue tile
  band and pointed-arch windows, a crenellated parapet, a great arched portal with carved doors, an onion dome on a
  windowed drum with a gilt finial, two wing domes and four minarets with balconies and little domes.
- **Varrow's castle:** crenellated curtain walls with arrow slits, a gatehouse with a pointed arch, a portcullis and
  banners, KayKit round towers at the corners and a tall keep, and a paved courtyard with a well and stores.
- **Varrow seen from Kingsvale:** its walls are KayKit walls with real openings at the three gates, each with two gate
  towers and a battlemented lintel, all in Varrow's blue. *Go To Next Landmark* now also visits castles and palaces.
- **Walled cities seen from outside** (Highcairn from Frostfang, Cinderhold from Emberreach, Coralport from Tidewake,
  Mirewatch from Duskmire, Sunspire from the Sunscar Wastes): KayKit walls and towers, with a gatehouse over every gate.
- **Volcanoes** (Emberreach, Dragonscale Isle): a craggy ash cone with ridges and gullies, a jagged crater with a glowing
  lava pool, lava trickling down from a notch in the rim and embers rising from the crater (Models/Nature/volcano).
- **Real cities:** the homes in every city (and the villages around them) are townhouses built for the place
  (Models/Town, 108 of them, made for Ashen Hollow): Varrow's stone ground floors under jettied half-timbered storeys
  and blue slate roofs with dormers and chimneys; Highcairn's dark timber with snow lying on the slate; Mirewatch's
  plank houses with mossy roofs and lamplit windows; Cinderhold's basalt with red tiles and forge-glow windows;
  Coralport's whitewash, terracotta and teal roofs and blue shutters; Sunspire's flat-roofed adobe with parapets,
  beam ends, arched windows, striped awnings, roof terraces and little domes. One to three storeys (cities often a
  storey higher), three widths and two looks each, fitted to each lot; shop fronts with awnings and window boxes.
  Shops, inns, temples and halls stay as they were.
- **Cobbled streets:** inside the walls the streets and squares are laid with cobblestones in each city's own colour
  (grass and water stay as they are), and beyond the walls the land runs on to the horizon (fields, sand, snow or
  ash) so the gates no longer open onto nothing.
- **Townhouses from every side:** windows in the side walls on every floor, so a house seen over a wall or down an
  alley is not a blank box. Old roofs turned 45° and door signs that stuck out past a lot are cleared with the old house.
- **Ships and piers:** the box-and-board ships at Varrow's quay, round Tidewake and in the port towns are sailing ships
  (AHShips): a planked, curved hull with a painted band, a stern castle with lit windows and a lantern, tapered masts
  with a crow's nest, yards and bellied sails, a bowsprit and jib, shrouds and stays, a pennant, and they rock on the
  swell. The plank-on-sticks jetties are planked piers on round piles with rope and a bollard.
- **Sea serpents** lie in the water and swim as they should (the model stood on end, 20 m tall, before).
- **Cloaks** drape: they wrap round the shoulders, fall in folds and flare at a rounded hem, with gold clasps (they
  were flat boards). Ember cloaks keep their gold hem.
- **Statue gardens** (a stepped plinth, flowerbeds, hedges, benches and shade trees round each city statue) and a
  **training yard** (sand, fence, hay bales, straw targets, a weapon rack) round the practice dummies.
- **Bog pools:** Mirewatch has three dark bog pools and Highcairn a frozen pond where the ground map paints them;
  Duskmire's own pools get mossy stones, reeds, bulrushes and lily pads (the square lily boards are gone).
- **Plain old props rebuilt:** basalt columns, market stalls, cauldrons, looms, jeweller's benches, broken pillars,
  boulders, standing stones, thrones, braziers, box huts, gate posts and lava vents; the neighbours' herbs and ore
  rocks along an area's edges become plants and rocks too, and neighbouring waystones show as real waystones.
- **Peaks:** the steeper grey peaks with a separate white tip are one crag with a snow cap; the old block townsfolk
  baked into the city models (including the simpler ball-headed ones) are hidden, the real townsfolk walk instead.
- Test helpers: *Dump Old Shapes* lists every untouched old piece in the area (HeroShots/shapes_<area>.txt), and
  *Go To Next Landmark* also visits ships, statues, castles and palaces.
- **Sound:** effects, music that follows where you are and what you do, and ambience, all synthesized (Menu → Sound).
- Cave walls, cliffs, deep water and lava in Emberreach, Tidewake, the Fossil Lands and the cities now block your way as in the web game.
- Test helpers in the menu: *Go To Next Townsperson*, *Go To Next Work Spot*, *Take Next Way Out*, *Give Potions* (also enhancement stones), *Give Meals*, *Give Money*, *Level Up +10*, *Go To Waystone or Treasure*, *Go To Bounty Board*, *Go To Next Shop*, *Finish Quest Objectives*, *Make It Day/Night*, *Give Cosmetics*, *Next Cosmetic Look*, *Hero Snapshots* (front/side/back pictures of the hero into a HeroShots folder), *Cast Pose Snapshots* (every casting animation on one sheet), *Open Spellbook*, *Beast and Mount Parade* and *Townsfolk Snapshots* (contact sheets of every new animal, mount and townsperson), *Ride Next Mount*, *Next Pet*, *Travel (test)* to jump to any area, and *Dungeons (test)* (enter any dungeon, go to the boss or a dungeon door, defeat the nearest beast, clear the seals).

## What is new in v0.3
- **Loot**: beasts leave a glowing pile (violet if it holds gear). Walk over it to pick it up.
- **Bag** (BAG button or B): 20 slots, items stack, gold on the HUD.
- **Gear**: Weapon, Helmet, Chest, Legs and Boots raise damage and health. In the bag, tap an item to look, tap again to wear it (or take it off). New heroes start with five starter pieces.
- **Mana** for Mage, Priest and Druid: spells cost mana, blue bar under health.
- **Warden Elda** at the camp gives quests (TALK button or E): *Boar Trouble* (hunt 5 boars) then *Pelts for Winter* (bring 3 wolf pelts). Tracker at the top right.
- **Saving**: class, level, gold, bag, worn gear and quests are kept between plays.
- **Vanish** turns the Rogue see-through.
- After updating, run **Ashen Hollow > Set Up Meadow Scene** once (it makes the new Ghost material).
- **Ashen Hollow > Reset Saved Progress** starts a brand-new hero. **Ashen Hollow > Test: Give 3 Wolf Pelts** is an editor-only helper for testing.

## Controls
- Phone: left thumb = joystick, ATTACK = attack (hold to keep swinging), the five round buttons = spells, DODGE = roll,
  drag anywhere else to turn the camera, pinch to zoom.
- Editor: WASD or arrows to move, Space = attack, 1 to 5 = spells, Left Shift = dodge, B = bag, E = talk, Esc = close,
  hold the right mouse button and drag to turn, mouse wheel to zoom.

## Put it on your phone (Android)
Unity Hub > Installs > your Unity 6 > Add modules > Android Build Support. Then in Unity:
File > Build Profiles > Android > Switch Platform, plug in the phone (USB debugging on) and press Build And Run.
For iPhone you need a Mac with Xcode.

## If something looks wrong
- **Pink objects**: the project is not using URP. Use the "Universal 3D" template.
- **A model faces sideways**: select the "Ashen Hollow" object in the scene and set Hero Yaw Fix
  or Wolf Yaw Fix to 90, -90 or 180.
- **Red errors in the Console**: take a screenshot and send it to Claude.

## What is in here
- `Scripts/`: the game (AHGame world and day, AHPlayer hero, AHMob beasts, AHUI controls and HUD,
  AHAnim animation, AHInput touch/mouse/keyboard, AHModel loading).
- `Shaders/`: AHWater (waves, glints, foam) and AHSky (gradient, sun, moon, stars, clouds) for URP.
- `Resources/AH/`: `meadow_world.glb` and `meadow.json` exported from the web game; models in `Models/`.
- `Editor/`: the "Ashen Hollow" menu.

## What changed in v0.2.1
- The camera moves in closer when a tree's leaves would block your view of the hero, then eases back out.
- Very quick taps and clicks on buttons now always count.
- Grass shows properly when the project's ambient occlusion is on.

## Credits
- Heroes (Knight, Mage, Rogue, Barbarian): KayKit Adventurers by Kay Lousberg, CC0.
- City buildings, walls, towers and props: KayKit Medieval Hexagon Pack by Kay Lousberg (www.kaylousberg.com), CC0.
- Dungeon torches and props: KayKit Dungeon Remastered by Kay Lousberg (www.kaylousberg.com), CC0.
- Wolf: Quaternius, CC0.
- Trees: broadleaf trees from 'Idyllic Fantasy Nature' and spruces from 'Ultimate Nature Starter' (Unity Asset Store,
  free, Standard Asset Store EULA: fine to ship in the game, not to share as loose model files). The mountains are made
  for Ashen Hollow with the rock texture from 'Idyllic Fantasy Nature'.
- Windmill: 'Fantasy landscape' (Unity Asset Store, free).
- Ice and rift crystals: 'Stylized Crystals - Low Poly RPG Packs - Lite' by FinottiGames (Unity Asset Store, free),
  coloured with the Imphenzia PixPal palette (CC0).
- Velociraptor, Pachycephalosaurus and Stegosaurus models, animations and sounds: 'PBR Animated Dinosaurs' by Ferocious
  Industries (Unity Asset Store, free).
- 'Phoenix on fire update' by NORBERTO-3D (sketchfab.com/norberto3d), CC-BY 4.0. The scorpion, the pteranodon, the
  grim reaper and the skeleton mage are free downloads without licence notes in their files; check their pages
  before release.
- 'Sea Serpent' by Sammy The Citipati (sketchfab.com/SammyTheCitipati), CC-BY 4.0.
- 'Sea Turtle' by Eloi (sketchfab.com/Eloiart), CC-BY 4.0. 'Frog MONSTER' (Warren toad) by samuco
  (sketchfab.com/samueldc42), CC-BY 4.0. 'Animated Crab rigged FREE' (shore crab) by TwilightFox (sketchfab.com/twilightfox),
  Sketchfab Standard licence. The giant crab (reef crab, Coral King) and the flame-warted toad (Mire toad) are free
  Sketchfab downloads; check their pages for the author and licence before release.
- Casting and combat animations: Quaternius Universal Animation Library 1 and 2 (quaternius.com), CC0.
- Spell and item icon glyphs: game-icons.net by Lorc, Delapouite, Cathelineau, Skoll and Sbed, CC BY 3.0
  (https://creativecommons.org/licenses/by/3.0/), recoloured and placed on round shaded backgrounds.
- Animals and animal mounts (wolf, fox, stag, horse; reshaped into the boars, jackals, deer, rare wolves, cat and the
  horse and wolf mounts): Quaternius Ultimate Animated Animal Pack (quaternius.com), CC0.
- Hollow Meadow: made for Ashen Hollow.
- 'European Dragon' (the wyrm, the Ember drakes and the Pterra) by Nonexistent 101 on Sketchfab, CC-BY 4.0.
- 'Randaling T-Rex, animated!' (the Tyrant king) by quander on Sketchfab, CC-BY 4.0.
