#!/bin/bash
# rig every detailed animal that has arrived onto its low-poly animal's skeleton and clips
B=/mnt/user-data/outputs/AshenHollow/Resources/AH/Models/Beasts; IN=/mnt/user-data/uploads/AshenHollow/Incoming; cd /tmp/claude-0/hd; mkdir -p out
for pair in wolf:mistfang icewolf:mistfang shadowwolf:mistfang alphawolf:alphawolf mistfang:mistfang rimeclaw:rimeclaw boar:boar boarlord:boarlord goldhorn:goldhorn stag:stag doe:doe jackal:jackal tjackal:tjackal; do
  n=${pair%%:*}; b=${pair##*:}
  [ -f $IN/hd_$n.glb ] || continue; [ -f out/b_${n}_hd.glb ] && continue
  echo "== $n $(date +%T)"; timeout 900 /tmp/bv/bin/python hdrig.py $B/b_$b.glb $IN/hd_$n.glb out/b_${n}_hd.glb out/b_${n}_hd.jpg '{}' > out/$n.log 2>&1; python3 fixtime.py out/b_${n}_hd.glb >> out/$n.log; grep "fit after\|Error\|times" out/$n.log | head -4
done
echo HDDONE
