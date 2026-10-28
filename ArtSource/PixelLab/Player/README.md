# 주인공 스프라이트 (몰락한 태양신)

명세: `SPEC.md` · 남은 문제: `ISSUES.md`

| 폴더 | 내용 |
|---|---|
| `00_concept/` | 설정화에서 인물만 잘라낸 참고 이미지 |
| `01_raw/clean/` | 현재 PixelLab 원본 (rotations 48×48, 동작 프레임, 0번 = 기준 회전 이미지). W·SW 회전은 어깨 장식을 고친 판 |
| `02_final/` | 최종: `<동작>/<방향>.png`(64×64 프레임 가로), `<동작>_sheet.png`(행 = 방향 순서), `rotations/`, `palette.hex`, `shadow.png`, `sprite_spec.json`, `process_log.json`, `issues.txt` |
| `05_hurt/` | hurt 기록(`NOTES.md`). 시트는 `02_final/hurt/` |
| `tools/` | `process.py`(후처리·검사), `pngio.py`(PNG 입출력, 표준 라이브러리+numpy), `fetch_clean.sh`(PixelLab 프레임 내려받기), `export_unity.py`(게임 폴더로 복사), `sheet_clip.py <동작> <폴더>`(납품 시트 조립·검사) |

PixelLab 캐릭터 ID: `11c04004-21b7-4a70-ab98-d6a4a41cf26f` (상태 CleanCape_NoAhoge, 2026-10-28~).
2026-10-28 중간·탈락본(이전 판 원본, 후보, 미리보기, high top-down 판)은 정리함(휴지통).

## 다시 만들기
```bash
python tools/process.py build     # 전체 후처리 + 검사 + 검증(팔레트·알파·외곽선 위반 시 실패). 미리보기는 03_preview/에 다시 생김
python tools/export_unity.py      # Assets/Resources/Art/chr_player_{idle,run,attack,dash,hurt}_{방향}.png
```

## Unity
Point 필터, 압축 없음, PPU 32. 게임은 시트를 높이 기준 정사각 프레임으로 잘라 씀(`AssetLibrary.LoadFrames`). 재생 속도는 `RoomManager.playerAnimSpeeds`(idle 6, run·attack·dash 12, hurt 16).
