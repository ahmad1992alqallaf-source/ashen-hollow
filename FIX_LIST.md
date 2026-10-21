# Ashen Hollow: things to fix (from testing on the phone)

Collected while testing; fixed together in one round.

## To fix
1. **AUTO quest stops working after the first quests.** It works for the first few quests, but later ones (for
   example the wolf-hunting quest) do nothing when AUTO is on. AUTO should walk to, fight and finish every quest step.
   Seen in the video (quest "Wolves of Silkwood", hero in Hollow Meadow): AUTO says "no Wolf in this land. Travel
   to where they live, then tap AUTO" and stands still. AUTO must find which land the quest's foes (or NPC, or
   item) are in and travel there by itself, road by road (through the area exits / passes, or the waystone if it is
   unlocked), then carry on with the step. Same for "talk to" steps whose NPC is in another land.

2. **The boars in Silkwood look pixelated / like dark shadows** (the tusked boars). Check their model and textures
   on the phone (Mobs/boar and Mobs/boarlord, also used by Old Tusk): likely a low-resolution or badly compressed
   texture, or a material that looks wrong on phones. Compare with the editor.

3. **Burnt food needs a better icon.**
4. **The sign before entering Silkwood is hard to read**: its writing (the land name and level) doesn't show
   clearly. Make the text sharp and readable on the phone.
5. **Pink/purple weeds on the ground seen from far away vanish when you walk close.** Far away the ground shows
   pink weeds; up close they disappear. Likely the far-distance version of the grass/flowers (LOD or terrain
   detail) has the wrong colour, or the close version doesn't draw. Far and near should match.

6. **New: mining and smithing like World of Warcraft / Aion 2** (user's choice):
   - Ore rocks show on the minimap.
   - Ore quality: grey, green, blue, yellow. Better ore makes better gear.
   - Smithing recipe window (WoW style): pick a recipe and how many, made one after another with a progress bar;
     recipe colours (orange/yellow/green/grey) show if it still raises skill; new recipes from trainers and drops.
   - Not wanted: Aion 2's 3-roll gathering panel.

Not a problem: night darkness is fine as it is (keep it).

23. **Weapons don't show in the item preview** (anvil, also the bag): the picture of a weapon (bronze sword, mace,
    staff) shows a close-up of a headless robe/body instead of the weapon, and "Try it on" shows the hero still
    holding their own dagger, not the weapon being tried (seen as a Rogue; the item says "your class can't use
    this", but the preview should still show it). The preview must draw the weapon itself, and Try it on must put
    it in the hero's hand.
24. **Old Mill Road: the river/water is a flat bright-blue sheet** with no water look (and the hero walks along it).
25. **Old Mill Road: plain blue blocks** (stone slabs, ruins, posts near the deer) look like untextured boxes.
    The pillar lying on the ground near the deer is **see-through** (missing faces / one-sided or inside-out mesh).
26. **Silkwood fights: a huge green glowing cylinder and giant orange/green rings** on the ground around the hero
    (shaman totem / level-up / pet "seal" effects?) look like placeholders and cover the screen. (To confirm.)
27. **Wolves look pixelated / like dark shadows** in Silkwood fights, same problem as the boars (item 2).
30. **The brown bear ("Brown bear · lv 12", Silkwood) is invisible**: its name and health bar show but the body
    can't be seen.
31. **Bridges: you walk through them** instead of on them (no solid floor / sides).
32. **You can stand and walk on rivers.** Water should not be a floor: wade in shallow water, swim (or be blocked)
    in deep water.
28. **Skinning has no animation**: the hero just stands while the skinning bar fills. Add a kneel-and-cut
    animation (knife in hand, kneeling at the body) for the whole time skinning runs.
29. **Fishing like World of Warcraft**: hold a fishing pole and cast; the line flies out and a bobber lands on the
    water and floats; after a random wait (a few to ~20 s) the bobber dips and splashes with a sound; tap it (or the
    button) quickly to hook the fish; too slow and it gets away. Fish schools (rippling spots) give more and rarer
    fish. Lures add a bonus for a while. Each land has its own fish list by rarity (item 11); rare and better
    fish go into the short tension mini-game (item 20).

## Big ideas (to discuss, then build)
7. **Longer talent tree, like Aion 2's Daevanion boards** but in our own style: many more nodes, the player picks
   their own route from the centre (stat nodes, skill-level nodes, big bonus nodes at the ends); not enough points
   to take everything, so choices matter; a refund costs coin. Keep our class / path / form tree as the base.
8. **New fantasy interface** (like Aion 2, its own unique look, not an old-game look): HUD, bags, menus, buttons.
   **Chosen: style B "Celestial Glass"** (design canvas: https://claude.ai/artifact/SVJGDm947F32yq1LRXvJCk):
   frosted see-through glass panels, thin silver lines, sky-blue (#7fe0ff) glow, soft gold (#f0d58a) highlights,
   rounded shapes, Marcellus for titles and Manrope for text. Rounded-square minimap, round glass buttons,
   thin HP/MP bars, rarity shown as a coloured dot and a glow at the bottom of each bag slot.
9. **Travel loading screen**: 2 to 5 seconds when moving between lands, so the next land spawns fully before you see it.
10. **Minimap and a guide arrow**: a minimap to see where you are going, and an arrow pointing to the quest goal
    (works even when AUTO doesn't).
11. **Rarity for everything gathered**: common, uncommon, rare, epic, legendary for fish (each land has its own
    fish), ore, logs, hides (skinning), herbs. Rarer = better things made from it.
12. **Recipes for everything**: food, potions, cooking, banquets, gear. Bosses and monsters drop recipes; the
    Adventurer path finds them and sells them to Artisans, who make the gear. (Ties the two paths together.)
13. **Loot like WoW**: no automatic pickup. A killed monster (skinnable or not) gets a button: tap it to open its
    loot window and take what dropped.
14. **Auto pickup** comes from: a pet at 3 stars, OR a monthly VIP from the store.
15. **VIP (monthly, store)**: auto pickup, +2% XP, +2% profession XP. No power, not pay-to-win.
16. **Battle Pass.**

17. **Collection book**: each land's fish, ores, logs, hides and herbs by rarity; a full page gives a title or a look.
18. **Rare catch announcements** in chat ("Ahmad caught a Legendary Moonfin!").
19. **Loot glow**: a body holding rare loot glows in that rarity's colour.
20. **Fishing mini-game** (a short tension bar) for rare fish.
21. **Rich veins and ancient trees**: rare gathering spots that appear for a while with better-rarity materials.
22. **Battle Pass with two lines**: one for fighting, one for gathering and crafting.

## Waiting to be checked on the phone
Built in this round (items above are kept for the record; all of these are in the game now):
- 1 AUTO travels road by road to the land a quest step needs (foes, people, things to gather) and fights on the way.
- 2 / 27 Boar and wolf textures brightened; phones now draw the beasts with the same lit shader as the editor.
- 3 A new burnt-food icon. 4 Signposts: dark plank boards with crisp letters (a new text shader).
- 5 Small flowers and weeds use a phone-safe shader, so far and near match.
- 23 Weapon pictures show the weapon alone (tinted to its metal); Try it on puts it in the hero's hand.
- 24 / 32 Rivers keep their real width (you can't walk on them); 31 bridges have a walkable deck.
- 25 Plain grey blocks get a stone texture and are drawn from both sides (the see-through pillar).
- 28 Skinning: the hero kneels with a knife for the whole skin. 30 the brown bear shows on phones.
- 13 / 19 Loot like WoW: the body glows in its best item's colour; tap LOOT for a loot window (Take / Take all).
- 14 / 15 Auto pickup from a pet at 3 stars or the VIP month (store: auto pickup, +2% XP, +2% profession XP).
- 11 Grades for ore, bars, logs, hides and herbs (Common to Legendary); each land has five fish of its own.
- 12 Recipes above skill 10 are learned (the station's book, or recipe scrolls that bosses and elites drop).
- 6 WoW crafting window: recipe colours, counts, Create / Create all, best-grades switch; ore on the minimap.
- 21 Rich veins and ancient trees (gold shimmer, better grades, more XP).
- 29 / 20 WoW fishing: cast, bobber, bite, HOOK!; rare fish fight on a reel bar; schools and Glimmer lures.
- 17 Collection book (Menu): a page per land's fish and per material in all grades, with rewards.
- 18 Epic and Legendary finds are told in chat ([World] ...).
- 9 Loading screen between lands (2 to 5 seconds). 10 Minimap (left, turns with the camera) and a gold guide arrow.
- 7 Talent boards (Class → Boards): four boards of 24 stones, one point a level from 10, clearing costs coin.
- 16 / 22 Battle Pass (Menu): Fight and Craft lines, 30 tiers; Premium (Crowns) gives looks only.
- 8 Celestial Glass interface: glass buttons with silver rings, night-blue glass windows, Marcellus and Manrope.
- 26 (the green tube and rings) was not changed: still to be confirmed.


## Fixed
- Gear and weapon pictures in the bag were empty on phones (0.9.8).
- Blue screen / nothing could be tapped after Begin (0.9.6 ships' hulls NaN on phones; 0.9.7 shaders left out of
  phone builds).
