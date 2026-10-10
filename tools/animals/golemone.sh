#!/bin/bash
# one golem: merge its clips, shrink, fix the clip times, then delete the big staged files
v=$1; I=/mnt/user-data/uploads/AshenHollow/Incoming; cd /tmp/claude-0/hd
python3 mergeclips.py $I/rig_g_$v.glb gout/m_$v.glb Idle=$I/anim_g_${v}_Idle.glb Walk=$I/anim_g_${v}_Walk.glb Attack=$I/anim_g_${v}_Attack.glb Attack2=$I/anim_g_${v}_Attack2.glb Hit=$I/anim_g_${v}_Hit.glb Death=$I/anim_g_${v}_Death.glb | tail -1
timeout 900 /tmp/bv/bin/python golemfin.py gout/m_$v.glb gout/b_g_${v}_r.glb 24000 2>&1 | grep "mesh char\|actions"
python3 fixtime.py gout/b_g_${v}_r.glb
rm -f gout/m_$v.glb $I/rig_g_$v.glb $I/anim_g_${v}_*.glb
ls -la gout/b_g_${v}_r.glb
