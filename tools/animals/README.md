# Detailed animals (code only; the models stay on the PC)

1. `refshot.py <animal glb> <out prefix>`: a reference picture of the game's low-poly animal in its rest pose.
2. Meshy job file: an `img` job paints a detailed animal over that picture (same pose), then a `single | none+remesh` job makes the model.
3. `hdrig.py <low-poly glb> <meshy glb> <out glb> <preview jpg> '{}'`: fits the new body onto the old one (every axis turn
   tried, the best few refined), copies the old body's bone weights (a leg only from its own side), smooths them, and
   exports with the old skeleton and every clip. `fixtime.py <glb>` makes each clip start at time 0.
4. Save as `Resources/AH/Models/Beasts/b_<name>_hd.glb`: the game uses it in place of `b_<name>` whenever it is there.

## Mounts, farm animals and pets
- `compall.sh`: mounts and farm animals onto their own skeletons (`hdrig.py`; horses and the cow with `{"tail":true}`, so a
  long hanging tail goes with the tail bones); the simple sphere pets fitted in place with `statfit.py`. The game uses
  `Comp/<name>_hd` in place of `Comp/<name>` when it is there.

## Golems, giants and trolls
- Meshy jobs: a T-pose concept (front and back), `multi | t-pose`, `rig`, and six `anim` clips per creature.
- `golemone.sh <name>`: `mergeclips.py` copies each clip onto the rigged model by joint name (no heavy Blender load),
  `golemfin.py` shrinks the mesh to 24k triangles and the textures to 1K. `crackglow.py` makes a rock texture's darkest
  seams glow like lava.

## Phones
- `glbtex.py <in> <out> 1024`: every texture inside a glb shrunk to 1K, everything else copied byte for byte.
