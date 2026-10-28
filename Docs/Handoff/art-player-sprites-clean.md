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

## 2026-10-29 idle 칼 오른손 고정·북쪽 망토
- 한 일: 서쪽 계열이 동쪽 거울상이라 W·NW·S·SE idle이 왼손 검이었음. 사용자 결정으로 idle만 수정. NW·SE·W는 회전 그림을 PixelLab `edit_image_pro_flash`로 오른손 검으로 고친 뒤(W는 1회 재시도, 칼이 몸 뒤) 그 그림을 시작·끝 프레임으로 v3 재생성(그룹 bafe2cab·3186e8d4·b96c060f). S는 정면이라 `process.py` `MIRROR`로 좌우 반전. 북쪽 망토는 `hem_sway`를 아래 8줄 점진(위 0 → 끝 2px)으로 변경. `export_unity.py`로 `chr_player_idle_{n,nw,w,s,se}.png` 갱신. Unity EditMode 6/6 통과, 게임 화면 `Docs/QA/Screens/idle_righthand_20261029*.png`. PixelLab 약 18회(편집 5×3 + 생성 3)
- 남은 일: run·attack·dash·hurt·death는 여전히 W·NW·S·SE 왼손 검(idle↔달리기 전환 때 손이 바뀜). S 기준점 검사는 반전 때문에 실패로 나옴(발 위치는 1px 이내로 같음). 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `01_raw/clean/idle_alert/{west,north-west,south-east}/`, `01_raw/trials/idle_alert_lefthand/`, `02_final/idle_alert*`, `07_idle_alert/`, `Assets/Resources/Art/chr_player_idle_{n,nw,w,s,se}.png`, `Docs/QA/Screens/idle_righthand_20261029*.png`

## 2026-10-29 북쪽 idle 망토 펄럭임
- 한 일: 북쪽 망토가 여전히 어색하다는 의견으로 `hem_sway`를 `cape_flutter`로 교체. 어깨 아래 망토 전체가 아래로 갈수록 좌우로 휘고(끝 2px) 넓어졌다 좁아짐(끝 폭 ±12%). 머리·발 고정. `chr_player_idle_n.png` 갱신, 게임 화면 `Docs/QA/Screens/idle_north_cape_20261029.png`
- 남은 일: NW idle 수정(곧은 검·손 보이게 + 자세 고정) 진행 중 — 편집 그림만 받아 둠, 게임 미반영
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `02_final/idle_alert*`, `07_idle_alert/`, `Assets/Resources/Art/chr_player_idle_n.png`

## 2026-10-29 북쪽 idle 망토 8프레임 시계추
- 한 일: 펄럭임(넓어졌다 좁아짐)·크게·물결 안이 모두 끊겨 보인다는 의견으로, 망토 전체가 좌우로 흔들리는 시계추 움직임(끝 2px, 아래가 살짝 늦게)을 **8프레임**으로 바꿈(사용자 결정, 북쪽만 8프레임). `sheet_clip.py`는 방향마다 프레임 수를 읽도록, `process.py`는 프레임 수가 다른 행을 합칠 수 있게(`stack_rows`) 고침. 게임에서 N 8장·E 4장 로드 확인, 화면 `Docs/QA/Screens/idle_north_sway8_20261029.png`
- 남은 일: NW idle 수정(곧은 검·손) 진행 중, 게임 미반영
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,sheet_clip.py}`, `02_final/idle_alert*`, `07_idle_alert/`, `Assets/Resources/Art/chr_player_idle_n.png`

## 2026-10-29 호흡 idle (idle_breath) 8방향 납품
- 한 일: 사용자 명세로 idle을 다시 생성. 결정: 4프레임 반복, 지금 캐릭터로 idle만, 8방향. 시작·끝 프레임은 오른손 검 기준 자세(`01_raw/clean/idle_breath_start/`, W·NW·SE 편집본, S 반전). 시트 `ArtSource/PixelLab/Player/08_idle_breath/`, 판정표 `08_idle_breath/README.md`. `process.py` CLIPS·PLANTED에 `idle_breath` 추가. PixelLab 생성 8회
- 남은 일: 명세 실패 — 몸통 상하 2~3px(6방향), N 머리 위 뻗침·반복 튐, SW 폭 41, NW 여백 3px, 기준점 W·S. 게임 폴더 미반영(요청 없음), 커밋 안 함. 게임의 현재 idle은 idle_alert(북쪽 8프레임 시계추) 그대로
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `01_raw/clean/idle_breath*/`, `02_final/idle_breath*`, `03_preview/idle_breath_*`, `08_idle_breath/`

## 2026-10-29 호흡 idle 실패 방향 재생성
- 한 일: 몸통 상하 실패 6방향(E·N·S·W·SW·SE)을 문구 2종 × 2 = 12회 재생성, 방향마다 3개 중 최선 선택(E r2, N r2, S r1, SW r2, W·SE 1차 유지). N 머리 위 뻗침·반복 튐 해결, SW 몸통 0px. 판정표 `08_idle_breath/README.md` 갱신
- 남은 일: 몸통 상하 여전히 E·W·S·SE 2px, N 3px(기준 1px) — 3회씩 생성해도 v3가 통제 못 함. SW 폭 38, NW 여백 3px. 게임 미반영, 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/01_raw/clean/idle_breath/{east,north,south,south-west}/`, `01_raw/trials/idle_breath_{take1,r1,r2}/`, `02_final/idle_breath*`, `08_idle_breath/`

## 2026-10-29 호흡 idle 게임 반영
- 한 일: 사용자 결정("이대로 게임에")으로 `export_unity.py` STATES를 `idle_breath → idle`로 바꿔 `chr_player_idle_{8방향}.png` 교체(전부 4프레임, 북쪽 8프레임 시계추는 빠짐). Unity EditMode 6/6 통과, 8방향 4장씩 로드 확인, 화면 `Docs/QA/Screens/idle_breath_ingame_20261029.png`
- 남은 일: 몸통 상하 E·W·S·SE 2px·N 3px, run·attack 등은 W·NW·S·SE 왼손 검(보류). 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/export_unity.py`, `Assets/Resources/Art/chr_player_idle_*.png`

## 2026-10-29 모든 동작 오른손·머리 고정·NW 검
- 한 일: 사용자 결정으로 달리기·공격·회피·피격·사망의 W·NW·SE를 오른손 기준 자세로 재생성(15회), S는 좌우 반전(`MIRROR`). 위치 기준 자세를 오른손 그림으로 바꿈(`ROTATIONS = 'idle_breath_start'`, 이전엔 왼손 그림이라 NW가 밀려 검이 잘림). idle·공격·피격 머리 높이 고정(`STABLE`, `pin_head`) → 8방향 0px. NW 검 소실: v3가 검을 1px 선으로 그려 잡티 정리에 지워짐 → idle NW는 기준 자세 검을 매 프레임 붙이고, 나머지 NW는 서 있는 자세에 가까운데 검이 빈 프레임만 붙임(`lost_sword`). NW idle 재생성 1회는 검이 또 빠져 탈락(`01_raw/trials/idle_breath_nw_r1`). `export_unity.py`에 death 추가. Unity EditMode 6/6 통과, 6동작 × 8방향 로드 확인, 화면 `Docs/QA/Screens/all_clips_righthand_20261029.png`
- 남은 일: 달리기 1~4px·회피·사망 흔들림은 결정대로 유지. W는 오른손이 몸 뒤라 검 위치는 눈으로만 확인. 왼손 원본 `01_raw/trials/<동작>_lefthand/`. 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,export_unity.py}`, `01_raw/clean/{run,attack,dodge,hurt,death}/{west,north-west,south-east}/`, `01_raw/trials/*_lefthand/`, `02_final/*`, `08_idle_breath/`, `Assets/Resources/Art/chr_player_*`

## 2026-10-29 경계 대기 idle(idle_calm) 8방향 생성
- 한 일: 명세(몸통 0px, 머리카락·망토 끝만 흔들림)로 v3 8방향 생성(8회, 그룹 `68121eeb…`), `process.py`에 `idle_calm` 등록, 시트 `ArtSource/PixelLab/Player/09_idle_calm/` (sheet_clip 종료 코드 0, 판정표 README)
- 남은 일: 움직임이 명세보다 큼(단계마다 111~499px), NW 금색 조각·N 머리 뻗침·E 망토 벌어짐, NW·SW 크기 초과, N 반복 튐. 재생성 또는 코드 흔들림 방식 결정 필요. 게임 미반영, 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `01_raw/clean/idle_calm/`, `02_final/idle_calm*`, `02_final/{sprite_spec,process_log}.json`, `02_final/issues.txt`, `09_idle_calm/`

## 2026-10-29 idle_calm E·N·NW 재생성 + GIF
- 한 일: E·N·NW를 "거의 정지" 문구로 1회씩 재생성(그룹 `98f78885…`)해 교체 → NW 금색 조각·N 뻗침·E 망토 벌어짐·N 반복 튐 해소. sheet_clip 종료 코드 0. GIF 미리보기 도구 `tools/gif_preview.py`(PIL 없이), `09_idle_calm/preview/idle_calm_{compare,all}_x4.gif`
- 남은 일: 움직임은 여전히 명세(1~2px)보다 큼, NW 폭 37·여백 3px, SW 폭 40. 게임 미반영, 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/gif_preview.py`, `01_raw/clean/idle_calm/{east,north,north-west}/`, `01_raw/trials/idle_calm_take1/`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle_calm 몸통 고정
- 한 일: 사용자 결정(재생성 방향, 몸통은 움직이지 않게)으로 `process.py`에 `FREEZE`/`freeze_body` 추가 — 기준 자세 위에 망토 끝(±2px)·머리카락 끝(±1px) 윤곽 변화만 합성. 8방향 몸 안쪽 변화 0, 단계마다 25~142px. sheet_clip 종료 코드 0. 고정 전 `09_idle_calm/regen/`, GIF `preview/idle_calm_{compare_x3,all_x4}.gif`
- 남은 일: NE 38·NW 37·SW 39 폭 초과, NW 여백 3px. 게임 미반영, 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,gif_preview.py}`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle_calm 게임 반영
- 한 일: 사용자 결정("게임에 넣기")으로 `export_unity.py` STATES `idle_calm → idle`, `chr_player_idle_{8방향}.png` 교체. EditMode 6/6 통과(브리지 직접 연결), 8방향 로드 확인, 화면 `Docs/QA/Screens/idle_calm_ingame_20261029.png`, 리포트 `Docs/QA/idle_calm_ingame_20261029.md`
- 남은 일: verify.ps1·빈 참조 검사는 에디터가 열려 있어 못 돌림(종료 코드 3). 화면은 남서 방향 1장만 확인. 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/export_unity.py`, `Assets/Resources/Art/chr_player_idle_*.png`, `Docs/QA/`
