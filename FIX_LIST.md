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

Not a problem: night darkness is fine as it is (keep it).

## Waiting to be checked on the phone


## Fixed
- Gear and weapon pictures in the bag were empty on phones (0.9.8).
- Blue screen / nothing could be tapped after Begin (0.9.6 ships' hulls NaN on phones; 0.9.7 shaders left out of
  phone builds).
