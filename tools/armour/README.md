# Making the armour lighter for phones

Two steps per suit (`Resources/AH/Models/Outfits/tqo_*.glb`):

1. `blender --background --python simplify_blender.py -- <suit.glb> <out.npz> 0.5 40`
   Welds the copies hard edges made, halves the triangles (UV seams and skin weights are kept), keeps edges
   sharper than 40 degrees hard, and saves the new geometry.
2. `python3 write_back.py <suit.glb> <out.npz> <new.glb>`
   Writes the new geometry back into the original file. The skeleton, bind poses, materials and pictures stay
   exactly as they were, so the suit still fits the hero.

The originals of the 85 suits done on 16 October 2026 are in `Backup_Armour/` next to `Assets/` on the laptop.
Check the result in Unity with Test: Collection Suit Shots (or Test: Armour Check for one collection).
