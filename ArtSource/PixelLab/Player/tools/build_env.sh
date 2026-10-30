#!/bin/sh
# Inputs process.py / sheet_clip.py need but a5e5566 removed from the tree: the standing poses (idle_breath_start)
# and the game idle (02_final/idle_calm). Restore them untracked for a build, then clean the build leftovers.
#   sh tools/build_env.sh restore   # before: process.py build <clip> && sheet_clip.py <clip> ...
#   sh tools/build_env.sh clean     # after: removes them again, plus rotations/shadow/03_preview, resets the shared logs
cd "$(dirname "$0")/.." || exit 1
SRC="01_raw/clean/idle_breath_start 02_final/idle_calm"
case "$1" in
  restore) git checkout a5e5566^ -- $SRC && git reset -q $SRC && echo "restored: $SRC" ;;
  clean)   rm -rf $SRC 02_final/rotations 02_final/shadow.png 03_preview
           git checkout -- 02_final/issues.txt 02_final/process_log.json && echo "cleaned" ;;
  *)       echo "usage: build_env.sh restore|clean"; exit 2 ;;
esac
