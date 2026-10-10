#!/bin/bash
cd /tmp/claude-0/wpn; mkdir -p tmp
while read s n flip glow; do
  [ -z "$s" ] && continue
  [ -f out/$n.glb ] && continue
  f=/tmp/claude-0/inc/lo/$s.glb; [ -f "$f" ] || { echo "WAIT $n"; continue; }
  /tmp/bv/bin/python wpn.py $f tmp/$n.glb 9000 $flip > tmp/$n.log 2>&1
  /tmp/bv/bin/python /tmp/claude-0/rigg/finish.py tmp/$n.glb out/$n.glb $glow >> tmp/$n.log 2>&1
  grep WPN tmp/$n.log
done < wjobs.txt
