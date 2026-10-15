# New pets: skeletons and moves, ready for the Tripo models

Four pets from the concept sheets (Ashen_Hollow_Pet_4_View_References):

| Game id | Name | Body | Moves | Knack | Egg / rarity |
|---|---|---|---|---|---|
| rock_golem | Rock golem | two legs, long arms | Idle, Walk, Gallop, Attack (slam) | Stoneskin: a stone shell soaks up blows for 8 s | Stone, Grove · rare |
| shadow_cat | Night prowler | four legs, tail | Idle, Walk, Gallop, Attack (pounce) | Shadowstep: vanishes, strikes the nearest foe from behind, slows it | Void · epic |
| spirit_wisp | Spirit wisp | floats, 4 tendrils, a flame | Idle, Fly, Walk, Attack | Mana spring: heals you and fills your mana | Void, Tide · epic |
| phoenix_chick | Flame hatchling | two legs, wings, tail, crest | Idle, Walk, Fly (wingbeat, legs tucked), Attack (peck) | Ember burst: a ring of fire burns what hunts you | Ember · legendary |

The data is already in the game (companions.json PETS, eggs.json, AHPetSkills); a pet stays out of every list until
its model file is in Resources/AH/Models/Comp (pet_<id>_hd.glb).

## When a Tripo model arrives

1. Point the pet's entry in `tools/mounts/procanim.json` (pet_golem, pet_cat, pet_chick, pet_wisp) at the glb
   (`"src"`). If it faces the wrong way add `"yaw"` (degrees). The skeleton is placed by fractions of the model's
   own box (`"fit": "box"`), so it follows a fatter or thinner model; check the preview and nudge a bone if needed.
2. `python3.11 tools/mounts/proc_anim.py -- tools/mounts/procanim.json pet_golem /tmp/.../anim_golem.glb`
   builds the skeleton, weights the skin to it and makes the moves.
3. `python3.11 tools/mounts/creature_mount.py -- tools/mounts/creatures.json pet_golem pet_rock_golem_hd.glb`
   sizes it (`"height"` in metres), thins it (`"tris"`) and shrinks its textures (`"texsize"`).
4. Copy to Resources/AH/Models/Comp on the PC (model files stay on the PC; they are not pushed).

`standins.py` makes the rough stand-in shapes these were tested on.

## Tips for Tripo

- One pet per image, centred, plain background, no text: the sheets' crops have labels and slivers of other
  views in them (the Night prowler's side and back views show more than one cat), which confuse multi-view.
- The crops are small (the pet is about 110 px wide); export the sheet again at full size if you can.
- Ask for a neutral pose: legs straight down, wings spread out to the sides, tail out behind, mouth closed.
  Rigging is far cleaner from that pose.
- Model only, no rig or animation from Tripo needed: the skeleton and moves here are made for these pets.
