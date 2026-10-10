#!/bin/bash
# mounts, farm animals and rigged pets onto their own skeletons; the simple pets fitted in place
C=/mnt/user-data/outputs/AshenHollow/Resources/AH/Models/Comp; IN=/mnt/user-data/uploads/AshenHollow/Incoming; cd /tmp/claude-0/hd; mkdir -p cout
for n in mount_horse mount_destrier mount_gladiator mount_goldsteed mount_nightmare mount_voidwing mount_direwolf mount_glacierwolf farm_cow farm_goat farm_pig farm_sheep pet_fox pet_snow_fox; do
  [ -f cout/${n}_hd.glb ] && continue
  o='{}'; case $n in mount_horse|mount_destrier|mount_gladiator|mount_goldsteed|mount_nightmare|mount_voidwing|farm_cow) o='{"tail":true}';; esac
  echo "== $n $(date +%T)"; timeout 900 /tmp/bv/bin/python hdrig.py $C/$n.glb $IN/hd_$n.glb cout/${n}_hd.glb cout/${n}_hd.jpg "$o" > cout/$n.log 2>&1; python3 fixtime.py cout/${n}_hd.glb >> cout/$n.log; grep "fit after" cout/$n.log
done
for n in pet_drake pet_lantern_owl pet_owl pet_parrot pet_pumpkin_slime pet_slime pet_toad; do
  [ -f cout/${n}_hd.glb ] && continue
  echo "== $n"; timeout 300 /tmp/bv/bin/python statfit.py $C/$n.glb $IN/hd_$n.glb cout/${n}_hd.glb 12000 2>&1 | grep fitted
done
echo COMPDONE
