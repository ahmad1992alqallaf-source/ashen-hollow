#!/bin/bash
# keep processing staged glbs one at a time until STOP exists
cd /tmp/claude-0/inc
D=/mnt/user-data/uploads/AshenHollow/Incoming
while [ ! -f STOP ]; do
  f=$(ls -tr "$D"/*.glb 2>/dev/null | head -1)
  if [ -z "$f" ]; then sleep 5; continue; fi
  s1=$(stat -c %s "$f"); sleep 2; s2=$(stat -c %s "$f"); [ "$s1" != "$s2" ] && continue
  s=$(basename "$f" .glb | tr 'A-Z' 'a-z' | sed 's/[^a-z0-9]\+/_/g; s/_$//')
  if [ -f "lo/$s.glb" ] && [ -f "prev/$s.jpg" ]; then rm -f "$f"; continue; fi
  mkdir -p tmp/$s
  /tmp/bv/bin/python -c "import sys; sys.argv=['x','$f','lo/$s.glb','60000','prev/$s.jpg','tmp/$s']; exec(open('prep.py').read())" > tmp/$s.log 2>&1
  echo "$s $(grep '^INFO' tmp/$s.log)" >> done.txt
  rm -f "$f"
done
