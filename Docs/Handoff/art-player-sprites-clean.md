# art/player-sprites-clean 인수인계

## 2026-10-28 idle 흔들림 수정
- 한 일: 주인공 idle SE·E·NE를 PixelLab v3로 다시 생성해 교체(머리 상하 SE 3→0px, E 2→0px, NE 1→1px). 원본 `01_raw/clean/idle/`, 교체 전 원본 `01_raw/trials/idle_before_still/`, 탈락 후보 `01_raw/trials/idle_candidates/`. `process.py build` 검사 통과, `export_unity.py`로 `Assets/Resources/Art/chr_player_idle_{se,e,ne}.png` 갱신. 비교 그림 `ArtSource/PixelLab/Player/03_preview/idle_bob_fix_compare_x4.png`
- 남은 일: 게임 화면(Unity) 미확인. PixelLab 생성 26회 사용
- idle N: 생성 후보 6개 모두 머리 2~5px 흔들림 → 사용자 결정으로 기본 포즈 고정 + 망토 끝 4줄만 좌우 1px(0,+1,0,-1). `process.py`의 `STILL`·`hem_sway`, 미리보기 `03_preview/idle_north_still_x6.png`
- 바꾼 파일: `ArtSource/PixelLab/Player/01_raw/clean/idle/{south-east,east,north-east}/`, `02_final/idle*`, `02_final/issues.txt`, `02_final/process_log.json`, `03_preview/idle_*`, `ISSUES.md`, `tools/process.py`, `chr_player_idle_n.png`, `Assets/Resources/Art/chr_player_idle_{se,e,ne}.png`
