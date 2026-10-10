#!/bin/bash
# shrink every staged glb, then rig suits and prepare weapons for anything new
cd /tmp/claude-0/inc && bash run.sh > /dev/null 2>&1
cd /tmp/claude-0/inc/lo
for f in draconic_*_armor_*t.glb demonic_*_armor_*t.glb; do
  [ -f "$f" ] || continue
  s=${f%.glb}; coll=${s%%_*}; rest=${s#*_}; cls=${rest%%_*}
  if [[ $s == *female* ]]; then n=tqo_${coll}_${cls}_f; g=f; else n=tqo_${coll}_${cls}; g=m; fi
  [ -f /tmp/claude-0/rigg/final/$n.glb ] && continue
  echo "== $n"; (cd /tmp/claude-0/rigg && ./rig_one.sh /tmp/claude-0/inc/lo/$f $n $g 0 | tail -2)
done
for f in draconic_*_weapon_*.glb demonic_*_weapon_*.glb; do
  [ -f "$f" ] || continue
  s=${f%.glb}; coll=${s%%_*}; rest=${s#*_}; cls=${rest%%_*}; w=${s#*_weapon_}
  case $w in shield|tome) n=wpn_${coll}_${cls}_off;; *) n=wpn_${coll}_${cls};; esac
  grep -q " $n " /tmp/claude-0/wpn/wjobs.txt || echo "$s $n 0 0" >> /tmp/claude-0/wpn/wjobs.txt
done
cd /tmp/claude-0/wpn && bash wrun.sh
echo PIPE DONE
