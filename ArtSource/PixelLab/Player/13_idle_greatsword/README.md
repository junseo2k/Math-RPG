# idle_greatsword — 양손검 대기 8방향 (2026-10-31)

납품: `<n>_<방향>.png` 8장(E→SE), 시트 268×64, 4프레임, 간격 4px, 패딩 0, 6fps 반복. 게임 미반영.

## 명세 ↔ 도구 대응

| 명세 항목 | PixelLab 인자 | 처리 |
|---|---|---|
| 양손검으로 교체, 외형 유지 | `edit_image(images_base64=[게임 idle f0 ×8], description)` | 8방향 한 번에(10생성) → `base_out/` |
| idle 4프레임 | `animate_character` v3 `frame_count=4`, 시작=끝=`start/<방향>`, 그룹 `1378d46b…` | 8방향 각 1생성. 망토 윤곽만 가져옴(아래) |
| 64×64, 기준점, 16색, 알파, 외곽선 없음, 시트 배열 | 인자 없음 | `greatsword.py` + `sheet_clip.py` |
| 몸통 상하 0px | 인자 없음 | 몸·머리·칼은 기준 그림 고정, 망토 전체(기준 망토 +2px 안의 망토색·빈칸)를 생성본에서 가져옴(`greatsword.py freeze` → `cape_from`). 생성본의 검은 테두리는 버리고 한 겹으로 다시 그림, 망토와 몸 사이 빈칸은 기준 그림으로 메움 |
| 칼 흠집 없음 (사용자 추가) | 인자 없음 | `clean_blade`: 칼날 모양 유지, 구멍 메우고 밝음·중간·어둠 3줄로 다시 칠함 |
| 시점·광원 | 인자 없음 | 기준 그림을 따름 |

## 시험 기록 (`01_raw/trials/`)
- `idle_greatsword_south_cape_wide`: 숨쉬기 동작 문구. 망토가 크게 퍼지고 머리가 3px 올라감.
- `idle_greatsword_south_v3_redraws_body`: "거의 안 움직임" 문구. 망토 여전히 넓고, 갑옷·손을 프레임마다 다시 그려 흐트러짐.
→ 게임 idle_calm과 같은 방식(몸 고정, 망토만)으로 결정.
- 줄 밀기 망토(`greatsword.py still`, 생성 없음): 사용자 "망토가 어색" — 판처럼 통째로 밀림. 참고 = 게임 idle 서쪽(주름이 부드럽게 바뀜) → 생성본 망토 윤곽을 가져오는 `freeze`로 교체(2026-10-31).
- 윤곽만 가져오기(`freeze_body`, ±2px 아래쪽 절반): 사용자 "안 움직이는 망토"(E·NE·N·NW·SW) — 뒤쪽 방향 망토의 11~18%만 움직임 → 망토 전체를 가져오는 `cape_from`(2026-10-31).

## 5방향 + 좌우 반전 (2026-10-31)
- 사용자 결정: E·NE·N·S·SE 5방향만 그리고 W·NW·SW는 E·NE·SE를 기준점 x=32 축으로 반전(`sheet_clip.py MIRRORED`, 실행할 때마다 다시 씀). 반전 3방향 = 원본과 픽셀 대칭 일치 확인. 이전 손으로 만든 W·NW·SW 시트는 `/tmp` 백업만 있고 저장소에서는 반전본으로 교체됨(생성 원본은 `01_raw/clean/`에 남음). 광원이 반전 방향에서 왼쪽 위로 바뀜(알려진 차이)

## 납품 검사 (`report.json`, `sheet_clip.py` 종료 코드 0)

| 항목 | 기준 | 실측 | 방법 | 결과 |
|---|---|---|---|---|
| PNG·시트 크기 | 268×64, 4프레임, 간격 4, 패딩 0 | 같음 | 파일 다시 열어 비교 | 통과 |
| 색 수 | ≤16 | 15 | 알파 0 제외 고유 RGB | 통과 |
| 알파 | 0·255 | 0·255 | 고유값 | 통과 |
| 바깥 외곽선 | 없음 | 제거 | 외곽 검정 픽셀 제거 후 저장 | 통과 |
| 여백 | ≥4px | 최소 4(NE·NW) | 프레임별 bbox | 통과 |
| 기준점 흔들림 | 0px | 방향마다 프레임 간 0 | `planted_feet` | 통과 |
| 발 맨 아랫줄 | 게임 idle과 같음 | y=54 | bbox | 통과 |
| 몸통 상하 | 0px | 0 | body_top 범위 | 통과 |
| 대상 크기 | ≤56×48 (2026-10-31 사용자: 양손검이라 36→56) | 폭 35~54, 높이 43~48 | bbox | 통과 |
| 망토 흔들림 | 망토 전체가 움직임 (사용자 2026-10-31) | 움직이는 망토 픽셀 비율 E 70, NE 57, N 55, NW 58, W 57, SW 52, S 46, SE 36% (윤곽만 가져올 때 11~38%) | 4프레임 중 한 번이라도 바뀐 망토 픽셀 | 통과(눈). 외딴 점 0, 빈칸은 기준 그림 ±3 |
| 명암 단계·광원 | 목표값 | — | 측정 방법 없음 | 미확인 |
| 반복 연결 | 자연스럽게 | 마지막 = 기준 그림 | GIF (눈) | 통과(눈) |

## 다시 만들기
```bash
cd tools
python greatsword.py base                      # base_out -> base/ (팔레트·칼날 정리), start/ (외곽선 없음)
echo "<id> <방향>" | sh fetch_clean.sh idle_greatsword 5   # id 목록: 01_raw/idle_greatsword_jobs.txt
python greatsword.py freeze idle_greatsword    # 02_final/idle_greatsword/<방향>.png
python sheet_clip.py idle_greatsword 13_idle_greatsword 0 4
python gif_preview.py sheets idle_greatsword 13_idle_greatsword
```
