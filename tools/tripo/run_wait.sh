#!/bin/bash
# rig each suit in a job list (lo-model name, out name, m|f, glow 0|1|2) as soon as its shrunk model is ready: run_wait.sh <jobs file>
cd /tmp/claude-0/rigg
while read s n g gl; do
  while [ ! -f /tmp/claude-0/inc/lo/$s.glb ]; do sleep 20; done
  [ -f final/$n.glb ] && continue
  echo "== $n $(date +%T)"; ./rig_one.sh /tmp/claude-0/inc/lo/$s.glb $n $g $gl ""
done < ${1:-jobs_sf.txt}
echo SFDONE
