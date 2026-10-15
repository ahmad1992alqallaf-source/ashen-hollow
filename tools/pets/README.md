# New pets: skeletons and moves, ready for the Tripo models

Four pets from the concept sheets (Ashen_Hollow_Pet_4_View_References):

| Game id | Name | Body | Moves | Knack | Mystic egg rarity |
|---|---|---|---|---|---|
| rock_golem | Rock golem | two legs, long arms | Idle, Walk, Gallop, Attack (slam), Sit, Sleep | Stoneskin: a stone shell soaks up blows for 8 s | common |
| shadow_cat | Night prowler | four legs, tail | Idle, Walk, Gallop, Attack (pounce), Sit, Sleep | Shadowstep: vanishes, strikes the nearest foe from behind, slows it | epic |
| spirit_wisp | Spirit wisp | floats, 6 tendrils, 2 upper wisps, a flame | Idle, Fly, Walk, Attack, Sleep (rest) | Mana spring: heals you and fills your mana | rare |
| spell_book | Spellbook (the mage's world pet) | an open book: covers as wings, pages, 2 ribbons | Idle, Fly, Walk (covers beat, pages riffle), Attack (snaps shut), Sleep (closed) | Arcane volley: bolts at up to 3 foes hunting you | world pet (Void and Ember eggs), rare |
| phoenix_chick | Flame hatchling | two legs, wings, tail, crest | Idle, Walk, Fly (wingbeat, legs tucked), Attack (peck), Sit, Sleep | Ember burst: a ring of fire burns what hunts you | legendary |

These four are store pets: found only in the Mystic egg (eggs.json MYSTIC.pets), never in world eggs. Pets are looks
only (eggs.json STATS false). The data is already in the game (companions.json PETS, eggs.json, AHPetSkills); a pet
stays out of every list, and the Mystic egg stays "coming soon", until a model file is in Resources/AH/Models/Comp
(pet_<id>_hd.glb). Feet are planted with IK ("ik", "rel"): they don't slide, and sitting folds the legs. A pet left
standing sits down after 14 s and sleeps after 35 s.

The Spellbook uses `"rigid": true` (each vertex follows one bone) so its covers turn whole instead of bending. For
Tripo: an open book seen from the front, covers spread flat like wings, pages in the middle, spine front to back,
nothing below it but two ribbon bookmarks. Its procanim entry is pet_book.

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

## Pictures for Tripo

Clean views cut from the full-size sheets, enlarged to 1024 px with the titles and panel edges taken out, are in
Tripo_Inputs (front, left, back, right for each pet). For the Night prowler use Front and Right: its Left and Back
views on the sheet have smoke and pieces of other drawings in them.

## Tips for Tripo

- One pet per image, centred, plain background, no text: the sheets' crops have labels and slivers of other
  views in them (the Night prowler's side and back views show more than one cat), which confuse multi-view.
- The crops are small (the pet is about 110 px wide); export the sheet again at full size if you can.
- Ask for a neutral pose: legs straight down, wings spread out to the sides, tail out behind, mouth closed.
  Rigging is far cleaner from that pose.
- Model only, no rig or animation from Tripo needed: the skeleton and moves here are made for these pets.
