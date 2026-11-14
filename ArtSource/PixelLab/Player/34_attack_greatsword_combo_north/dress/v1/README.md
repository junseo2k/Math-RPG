# 북쪽 대검 3연격 외형 v1 (2026-11-08)

| 파일 | 내용 |
|---|---|
| `1_light_slash_north.png` | 1타 8장 (768×96) |
| `2_rising_slash_north.png` | 2타 8장 (768×96) |
| `3_heavy_chop_north.png` | 3타 10장, 잔상 없음 (960×96) |
| `3_heavy_chop_north_smear.png` | 3타 10장, f2에 잔상 |
| `chain_north_*_x4.gif` | 1→2→3타 이어 보기 (겹치는 프레임 2장 빼고 24장) |
| `board_numbered_x3.png` | 프레임 번호판 |
| `layers/` | 프레임별 feet·cape·head·arms·hands·sword, `h3_f2_smear` |
| `coords.json` | 프레임별 좌표·레이어 순서·어깨 올림 |

- 96×96, 16fps, 투명, 알파 0/255, 15색, 바깥 외곽선 없음, 프레임 간격 0
- 생성 호출 없음. 몸·망토·메달 = `16_.../2_north_1.png`(idle 칼·손 마스크 제거), 뒷머리 = `12_run_calm/2_north.png` 3번째 프레임, 칼·손·팔 = 밑그림 좌표로 다시 그림. 마스크: `../trial1/masks/`
- 1타 f1 = 3타 f10 = north idle 원본 픽셀 그대로 (idle과 끊김 없게). 이 두 장만 칼 모양이 idle 원본 칼
- 연결 검사: 1타 f8 = 2타 f1, 2타 f8 = 3타 f1, 3타 f10 = 1타 f1 (픽셀 같음)
- 승인 시안 3장(1타 f5·2타 f8·3타 f2·잔상판)과 픽셀 같음
- 팔은 머리 위로 들 때(2타 f5~f8, 3타 f1)만 그림. 그 밖에는 밑그림 규칙대로 안 보임
- 다시 만들기: `cd ArtSource/PixelLab/Player/tools && python attack_north_dress_build.py` (종료 코드 0 = 통과)
- 게임 미반영
