# Ashen Hollow: things to fix (from testing on the phone)

Collected while testing; fixed together in one round.

## To fix
1. **AUTO quest stops working after the first quests.** It works for the first few quests, but later ones (for
   example the wolf-hunting quest) do nothing when AUTO is on. AUTO should walk to, fight and finish every quest step.
   Seen in the video (quest "Wolves of Silkwood", hero in Hollow Meadow): AUTO says "no Wolf in this land. Travel
   to where they live, then tap AUTO" and stands still. AUTO must find which land the quest's foes (or NPC, or
   item) are in and travel there by itself, road by road (through the area exits / passes, or the waystone if it is
   unlocked), then carry on with the step. Same for "talk to" steps whose NPC is in another land.

## Waiting to be checked on the phone
- Gear and weapon pictures in the bag (0.9.8: read back through a plain copy, drawn icon if still empty).

## Fixed
- Blue screen / nothing could be tapped after Begin (0.9.6 ships' hulls NaN on phones; 0.9.7 shaders left out of
  phone builds).
