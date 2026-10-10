#!/bin/bash
# rig_one.sh <src lo glb> <out name e.g. tqo_lava_rogue_f> <m|f> <glow 0|1>
set -e
cd /tmp/claude-0/rigg
src=$1; name=$2; g=$3; glow=$4; extra=${5:-}
K=/tmp/claude-0/outfits/glb/qoMale_Knight.glb; [ "$g" = f ] && K=/tmp/claude-0/outfits/glb/qoFemale_Knight.glb
T=tmp/$name; mkdir -p $T
/tmp/bv/bin/python rig_G.py $K $src $T/r.glb $T/rig.jpg "{\"tmp\":\"$T\"$extra}" > $T/rig.log 2>&1
/tmp/bv/bin/python finish.py $T/r.glb $T/g.glb $glow > $T/fin.log 2>&1
/tmp/bv/bin/python /tmp/claude-0/tripo/fixbind.py $K $T/g.glb $T/r.glb.delta.json final/$name.glb > $T/fix.log 2>&1
/tmp/bv/bin/python posecheck.py final/$name.glb final/$name.jpg $T > $T/pc.log 2>&1
grep MEASURE $T/rig.log | cut -c1-300; tail -1 $T/fix.log; ls -la final/$name.glb | awk '{print $5}'
