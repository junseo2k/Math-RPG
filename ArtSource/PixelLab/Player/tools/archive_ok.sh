#!/bin/sh
# Keep a clip the user approved ("OK", put in game) outside the repo (user, 2026-10-31).
# usage: sh tools/archive_ok.sh <date> <clip> <delivery folder> <game state> "<note>"
#   e.g. sh tools/archive_ok.sh 2026-10-31 idle_greatsword 13_idle_greatsword idle "양손검 idle, 게임 반영"
# -> ~/Desktop/Math_RPG_승인_스프라이트/<date>_<clip>/{sheets/<n>_<dir>.png, game/chr_player_<state>_<d>.png}, README.md row
cd "$(dirname "$0")/.." || exit 1
DEST="$HOME/Desktop/Math_RPG_승인_스프라이트"
OUT="$DEST/$1_$2"
[ -e "$OUT" ] && { echo "exists: $OUT (approved copies are never overwritten; use a new date)"; exit 2; }
mkdir -p "$OUT/sheets" "$OUT/game"
[ -n "$3" ] && [ "$3" != "-" ] && cp "$3"/[0-7]_*.png "$OUT/sheets/"
cp ../../../Assets/Resources/Art/chr_player_"$4"_*.png "$OUT/game/"
[ -f "$DEST/README.md" ] || printf '# 승인된 스프라이트 보관\n\n사용자가 OK한(게임에 넣은) 결과만 날짜별로 복사해 둔다. 덮어쓰지 않는다.\n\n| 폴더 | 동작 | 설명 | 원본 |\n|---|---|---|---|\n' > "$DEST/README.md"
SRC="ArtSource/PixelLab/Player/$3"; [ "$3" = "-" ] && SRC="Assets/Resources/Art (게임 그림만, 납품 시트 없음)"
echo "| $1_$2 | $4 | $5 | $SRC |" >> "$DEST/README.md"
echo "archived: $OUT ($(ls "$OUT/sheets" | wc -l) sheets, $(ls "$OUT/game" | wc -l) game strips)"
