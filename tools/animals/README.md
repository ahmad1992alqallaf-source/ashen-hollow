# Detailed animals (code only; the models stay on the PC)

1. `refshot.py <animal glb> <out prefix>`: a reference picture of the game's low-poly animal in its rest pose.
2. Meshy job file: an `img` job paints a detailed animal over that picture (same pose), then a `single | none+remesh` job makes the model.
3. `hdrig.py <low-poly glb> <meshy glb> <out glb> <preview jpg> '{}'`: fits the new body onto the old one (every axis turn
   tried, the best few refined), copies the old body's bone weights (a leg only from its own side), smooths them, and
   exports with the old skeleton and every clip. `fixtime.py <glb>` makes each clip start at time 0.
4. Save as `Resources/AH/Models/Beasts/b_<name>_hd.glb`: the game uses it in place of `b_<name>` whenever it is there.
