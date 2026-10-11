#!/bin/bash
# one Abyssal Realm monster rigged by Meshy: merge its clips onto the rigged model, shrink it for phones, fix the clip
# times, then delete the big staged files.  dmone.sh <name> [triangles] [extra clip names...]
v=$1; tris=${2:-24000}; shift; shift
I=/mnt/user-data/uploads/AshenHollow/Incoming; cd /tmp/claude-0/hd; mkdir -p gout
clips=""; for c in Idle Walk Attack Attack2 Hit Death "$@"; do clips="$clips $c=$I/anim_dm_${v}_$c.glb"; done
python3 mergeclips.py $I/rig_dm_$v.glb gout/m_$v.glb $clips | tail -1
timeout 1500 /tmp/bv/bin/python golemfin.py gout/m_$v.glb gout/b_dm_${v}.glb $tris 2>&1 | grep "mesh char\|actions"
python3 fixtime.py gout/b_dm_${v}.glb
rm -f gout/m_$v.glb $I/rig_dm_$v.glb $I/anim_dm_${v}_*.glb
ls -la gout/b_dm_${v}.glb
