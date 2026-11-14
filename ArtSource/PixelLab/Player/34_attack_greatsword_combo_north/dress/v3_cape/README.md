# 북쪽 대검 3연격 외형 v3 — 망토 분리 (2026-11-08)

승인된 밑그림 `../../stick_v3_cape/`를 외형으로. 생성 호출 없음. 잔상은 아직 없음.

| 파일 | 내용 |
|---|---|
| `1_light_slash_north.png` / `2_rising_slash_north.png` / `3_heavy_chop_north.png` | 8·8·10장, 96×96, 간격 0 |
| `chain_north_cape_{16,4}fps_x4.gif` | 1→2→3 이어 보기 24장 |
| `compare_v2_vs_v3_16fps_x4.gif` | 외형 v2(망토 고정) | v3 |
| `board_numbered_x3.png` | 번호판 |
| `layers/` | 프레임별 feet·legs·torso·arms·cape·head·sword·hands |

- 몸: `../../body_under_cape/` 레이어(다리·몸통·머리 왼쪽 1px)를 밑그림 몸 위치로 이동. 다리는 움직이는 골반과 고정된 발 사이를 줄마다 이어 붙이고, 무릎은 굽힌 만큼 바깥으로
- 망토: idle 망토(메달 포함)를 idle 망토 격자(5열 × 위·가운데·끝단)에서 밑그림 망토 격자로 옮겨 찍음. 끝단이 2px 넘게 들린 곳은 안감(진한 빨강) 띠
- 팔: 밑그림 어깨 → 팔꿈치 → 손을 검정 소매 + 금 팔꿈치로. 머리 위로 들 때만 망토 위
- 1타 f1 = 3타 f10 = north idle 원본. 15색, 알파 0/255, 앞꿈치 고정 검사 통과
- 다시 만들기: `cd ArtSource/PixelLab/Player/tools && python attack_north_dress_v3.py` (종료 코드 0, 약 10초)
- v1·v2는 그대로 둠
