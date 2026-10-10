#!/bin/bash
cd /tmp/claude-0/rigg
while read s n g gl ex; do [ -z "$s" ] && continue; echo "== $n"; ./rig_one.sh /tmp/claude-0/inc/lo/$s.glb $n $g $gl "$ex"; done < ${1:-jobs.txt}
echo ALLDONE
