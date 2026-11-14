# 북쪽 대검 3연격 외형 v2 — 전신 움직임 (2026-11-08)

승인된 전신 밑그림 `../../stick_v2_body/`를 외형 v1 레이어에 적용. 생성 호출 없음. 잔상은 아직 없음(사용자 순서: 승인 후 추가).

| 파일 | 내용 |
|---|---|
| `1_light_slash_north.png` / `2_rising_slash_north.png` / `3_heavy_chop_north.png` | 8·8·10장 시트, 96×96, 간격 0 |
| `chain_north_body_{16,4}fps_x4.gif` | 1→2→3 이어 보기 24장 |
| `compare_v1_vs_v2_16fps_x4.gif` | 외형 v1(몸 고정) | v2 |
| `board_numbered_x3.png` | 번호판 (파랑 = 바닥 y94) |
| `layers/` | 프레임별 feet·cape·head·arms·hands·sword |

- 머리: 밑그림 머리 중심으로 이동. 망토(메달 포함): 줄마다 변형 — 윗단은 어깨(이동·내려감·올림), 아랫단은 늦게 따라오는 단(기울기·출렁임). 발: 앞꿈치 줄 고정(검사), 뒤꿈치 들면 뒤 줄 빠짐, 회전하면 뒤 줄이 옆으로
- 1타 f1 = 3타 f10 = north idle 원본. 15색, 알파 0/255
- 다시 만들기: `cd ArtSource/PixelLab/Player/tools && python attack_north_dress_body.py` (종료 코드 0)
- v1(`../v1/`)은 그대로 둠
