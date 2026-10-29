# 1장 19시 신성한 지하수로 배경 (A안, 2026-10-27)

명세: `Docs/Art/Ch1_19_SacredAqueduct.md`. 16색 팔레트, 오브젝트 1px 검정 외곽선(4방향, 대각 모서리 없음).

| 폴더 | 내용 |
|---|---|
| `01_raw/` | PixelLab 원본: 타일셋 water(물↔바닥) `3d977347…`, dry(바닥↔메마름) `8d1058e9…`, wall(바닥↔벽) `637a4ab2…`, 오브젝트 pillar·lamp·exit_locked·exit_open |
| `02_final/` | 후처리 결과(= `Assets/Resources/Art`에 복사됨) |
| `03_preview/` | 4배 미리보기 |
| `tools/bg_export.py` | 16색 맞춤·외곽선·스트립 정렬·내보내기 (`python tools/bg_export.py`) |

스트립 `tile_water_ch1`·`tile_dry_ch1`: 칸 번호 = NW<<3|NE<<2|SW<<1|SE (1 = 그 꼭짓점이 물/메마름). 코드: `Assets/Scripts/Progression/RoomDecor.cs`.
후처리 색 조정: 메마름의 은색 → 회갈색, 기둥 한 단 어둡게, 바닥 한 단 어둡게(석재 아래 단), 벽 한 단 밝게(바닥과 구분).

남은 것: 물 4프레임 애니, 바닥 변형 3종(지금 1종 반복), 2D 라이트, 벽 앞면, 보스방(원판·제단).
