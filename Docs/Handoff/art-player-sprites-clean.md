# art/player-sprites-clean 인수인계

## 2026-10-28 idle 흔들림 수정
- 한 일: 주인공 idle SE·E·NE를 PixelLab v3로 다시 생성해 교체(머리 상하 SE 3→0px, E 2→0px, NE 1→1px). 원본 `01_raw/clean/idle/`, 교체 전 원본 `01_raw/trials/idle_before_still/`, 탈락 후보 `01_raw/trials/idle_candidates/`. `process.py build` 검사 통과, `export_unity.py`로 `Assets/Resources/Art/chr_player_idle_{se,e,ne}.png` 갱신. 비교 그림 `ArtSource/PixelLab/Player/03_preview/idle_bob_fix_compare_x4.png`
- 남은 일: 게임 화면(Unity) 미확인. PixelLab 생성 26회 사용
- idle N: 생성 후보 6개 모두 머리 2~5px 흔들림 → 사용자 결정으로 기본 포즈 고정 + 망토 끝 4줄만 좌우 1px(0,+1,0,-1). `process.py`의 `STILL`·`hem_sway`, 미리보기 `03_preview/idle_north_still_x6.png`
- 바꾼 파일: `ArtSource/PixelLab/Player/01_raw/clean/idle/{south-east,east,north-east}/`, `02_final/idle*`, `02_final/issues.txt`, `02_final/process_log.json`, `03_preview/idle_*`, `ISSUES.md`, `tools/process.py`, `chr_player_idle_n.png`, `Assets/Resources/Art/chr_player_idle_{se,e,ne}.png`

## 2026-10-28 경계 idle (idle_alert) 8방향 납품
- 한 일: 사용자 명세(64×64, 16색, 4프레임 반복, 8방향)로 PixelLab v3 생성. 1차는 검을 휘둘러 실패(`07_idle_alert/take1/`). 2차는 시작·끝 프레임을 정지 회전 그림으로 고정해 검이 고정됨. 북서 1회 재생성, 북은 기본 포즈 고정 + 망토 끝 1px(`STILL`). 시트 268×64 8장 `ArtSource/PixelLab/Player/07_idle_alert/`, 판정표 `07_idle_alert/README.md`. `sheet_clip.py`에 패딩·간격 인자 추가, `process.py` CLIPS에 `idle_alert` 추가. PixelLab 생성 18회
- 남은 일: 명세 실패 — 폭 36px 초과(NE 37, NW·SW 39, SE 40), 몸통 2px(NW·SW·SE), 반복 연결(E), 기준점 W (33,56)·S (26,56). 망토 끝(N 외)·광원 미측정. 게임 폴더 미반영(요청 없음), 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,sheet_clip.py}`, `01_raw/clean/idle_alert/`, `01_raw/trials/idle_alert_*`, `02_final/idle_alert*`, `02_final/{issues.txt,process_log.json,sprite_spec.json}`, `03_preview/idle_alert_*`, `07_idle_alert/`
