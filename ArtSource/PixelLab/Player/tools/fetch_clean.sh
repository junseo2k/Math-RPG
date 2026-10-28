#!/bin/sh
# usage: fetch_clean.sh <clip> <frame_count>  < lines "<animation_uuid> <direction>"  -> 01_raw/clean/<clip>/<dir>/frame_XX.png
B=https://backblaze.pixellab.ai/file/pixellab-characters/49aa65eb-9321-441e-ad88-ac87fe06bae7/11c04004-21b7-4a70-ab98-d6a4a41cf26f/animations
while read u d; do
  OUT="$(dirname "$0")/../01_raw/clean/$1/$d"; mkdir -p "$OUT"
  i=0; while [ $i -lt $2 ]; do curl -sSf -o "$OUT/frame_$(printf %02d $i).png" "$B/$u/$d/$i.png" || echo "FAIL $1 $d $i"; i=$((i+1)); done
done
