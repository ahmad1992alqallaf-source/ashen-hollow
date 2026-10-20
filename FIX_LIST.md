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
26. **Silkwood fights: a huge green glowing cylinder and giant orange/green rings** on the ground around the hero
    (shaman totem / level-up / pet "seal" effects?) look like placeholders and cover the screen. (To confirm.)
27. Wolves in Silkwood look like dark shadows, like the boars (item 2). (To confirm.)
28. **Skinning has no animation**: the hero just stands while the skinning bar fills. Add a kneel-and-cut
    animation (knife in hand, kneeling at the body) for the whole time skinning runs.

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


## Fixed
- Gear and weapon pictures in the bag were empty on phones (0.9.8).
- Blue screen / nothing could be tapped after Begin (0.9.6 ships' hulls NaN on phones; 0.9.7 shaders left out of
  phone builds).
