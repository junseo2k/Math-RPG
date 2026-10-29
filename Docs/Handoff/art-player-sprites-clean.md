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

## 2026-10-29 idle 바깥 외곽선 제거
- 한 일: 사용자 결정("윤곽선 안 넣은 버전으로")으로 `export_unity.py`에 `NO_OUTLINE={'idle_calm'}` — 게임 복사본만 바깥 1px 외곽선 제거, `02_final`은 외곽선 유지. EditMode 6/6, 화면 `Docs/QA/Screens/idle_calm_nooutline_ingame*_20261029.png`. 비교 GIF `09_idle_calm/preview/idle_calm_outline_x3.gif`(`gif_preview.py outline`)
- 남은 일: 다른 동작(run·attack·dash·hurt·death)은 외곽선 있음 — 맞출지 결정 필요. 가장자리에 검은 픽셀 프레임당 5~9개 남음, 발끝 1px 위. verify.ps1 미실행. 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{export_unity.py,gif_preview.py}`, `Assets/Resources/Art/chr_player_idle_*.png`, `09_idle_calm/preview/idle_calm_outline_x3.gif`, `Docs/QA/`

## 2026-10-29 idle 북서 머리 교체
- 한 일: 사용자 요청으로 북서 idle 머리를 북동 머리 좌우 반전으로 교체(`process.py` `HEAD_FROM`/`head_from`, 북서는 머리카락 흔들림 끔). sheet_clip 종료 코드 0, 게임 반영(외곽선 없음), EditMode 6/6, 화면 `Docs/QA/Screens/idle_calm_nw_head_ingame_20261029.png`
- 남은 일: 반전 머리의 광원 방향 정밀 확인 안 함, verify.ps1 미실행, 커밋 안 함
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `02_final/idle_calm*`, `09_idle_calm/`, `Assets/Resources/Art/chr_player_idle_*.png`

## 2026-10-29 idle 머리카락 코드 흔들기 (미리보기만)
- 한 일: 머리카락을 생성 프레임 대신 코드로 흔듦(`process.py` `HAIR_SWAY` [0,+1,0,-1], 머리 위 4줄만 옆으로) → 8방향 모두 머리 윤곽 이동 1px로 통일. 망토 판별식이 금색 중간 톤까지 잡던 문제 수정(`cape_px`, 북서 머리 음영도 살아남). sheet_clip 종료 코드 0. 비교 GIF `09_idle_calm/preview/idle_calm_hair_x3.gif`(위 이전, 아래 코드), 이전 시트 `09_idle_calm/hair_gen/`
- 남은 일: 사용자 확인 후 게임 반영·커밋. 게임에는 아직 이전 idle
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,gif_preview.py}`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle 머리카락 재생성 (코드 흔들기 반려)
- 한 일: 사용자 반려(코드보다 재생성)로 코드 흔들기 삭제. 머리카락만 흔드는 문구로 8방향 v3 재생성(그룹 `8f5b66a2…`, 8회, 원본 `01_raw/clean/idle_hair/`, 북서는 북동 머리 반전 시작 자세 `idle_hair_start/`). 합성: 망토 끝 = 기존 idle_calm 생성, 머리 띠(±1px) = idle_hair 생성(`HAIR_FROM_CLIP`). 머리 변화 최대 px: 이전 0~37 → 34~50(8방향 모두 움직임). sheet_clip 종료 코드 0. GIF `09_idle_calm/preview/idle_calm_hair_x3.gif`
- 남은 일: 사용자 확인 후 게임 반영·커밋. 게임에는 아직 이전 idle
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `01_raw/clean/{idle_hair,idle_hair_start}/`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle 7방향 머리를 남서 머리 기준으로 수정
- 한 일: 사용자 요청(남서 참고, 머리만)으로 7방향 기준 자세를 `edit_image_pro_flash`(참고=남서 기준 자세, 35회분)로 다시 그리고 머리 11줄만 원래 자세에 붙임 → `01_raw/clean/idle_head_start/`(시안 `01_raw/trials/head_fix/`). 이 자세로 머리카락 흔들림 7방향 재생성(그룹 `dbd40c5c…`, `idle_hair2/`, 남서는 `idle_hair` 재사용). `process.py`: `REF_FROM`(고정 기준 = 새 머리 자세), `HEAD_FROM` 삭제, 망토 띠는 바닥 3줄 위까지(발 기준점 보호). sheet_clip 종료 코드 0, 머리 변화 최대 28~46px
- 남은 일: 사용자 확인 후 게임 반영·커밋. 게임에는 아직 이전 idle
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `01_raw/clean/{idle_head_start,idle_hair2}/`, `01_raw/trials/head_fix/`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle 머리카락 흔들림을 남서쪽에 맞춤 (머리 스타일 원복)
- 한 일: 오해 정정 — 머리 스타일은 원래대로(`REF_FROM = idle_hair_start`, 7방향 다시 그린 머리는 `01_raw/trials/idle_head_start_unused/`, `idle_hair2_redrawn_heads/`로 뺌). 남서 흔들림(1프레임 부풀고 2프레임 가라앉고 3프레임 원래대로) 측정 도구 `tools/hair_motion.py`. 남서 흔들림을 글로 적어 7방향 재생성(`idle_hair3` 7회, `idle_hair4` NW·W·SE 3회), 방향별로 남서와 가장 가까운 생성 선택(`HAIR_FROM_CLIP` 방향별). 남서와의 거리: E 20, NE 33, N 41, S 35, W 59, SE 59, NW 65. sheet_clip 종료 코드 0
- 남은 일: NW·W·SE는 여전히 남서와 차이 큼(NW·W 1프레임째 거의 안 움직임). 사용자 확인 후 게임 반영·커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,hair_motion.py}`, `01_raw/clean/{idle_hair3,idle_hair4}/`, `01_raw/clean/idle_hair_start/north-west.png`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle 머리 흔들림 살짝 줄임
- 한 일: 머리 합성 띠를 정수리 6줄로 좁힘(`FREEZE` `hair_rows=6`, 이전 11줄). 기준 자세 대비 머리 변화 820→649px(-21%), 머리 옆(6~12줄)은 거의 고정. sheet_clip 종료 코드 0. 줄이기 전 시트 `09_idle_calm/hair_full/`, GIF `preview/idle_calm_hair_x3.gif`(위 전, 아래 후)
- 남은 일: 사용자 확인 후 게임 반영·커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,gif_preview.py}`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle 정수리 흔들림 줄임
- 한 일: 정수리가 바깥으로 부풀지 않게 머리 합성을 시작 자세 실루엣 안쪽으로 제한(`FREEZE` `hair=3, hair_rows=6, hair_out=False`; 외곽선이 밖에 다시 그려져 부풀던 원인 차단). 정수리 5줄 프레임 간 변화 8방향 합 774→324px(-58%), 방향별 31~54px. sheet_clip 종료 코드 0. 이전 시트 `09_idle_calm/hair_rows6/`, GIF `preview/idle_calm_hair_x3.gif`(위 전, 아래 후)
- 남은 일: 사용자 확인 후 게임 반영·커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `02_final/idle_calm*`, `09_idle_calm/`

## 2026-10-29 idle 머리 흔들림 수정판 게임 반영
- 한 일: 정수리 줄인 버전을 외곽선 없이 게임에 반영(idle 8개). EditMode 6/6, 화면 `Docs/QA/Screens/idle_calm_hairfix_ingame_20261029.png`. 커밋함
- 남은 일: verify.ps1·빈 참조 미실행(에디터 열림), 다른 동작은 외곽선 있음(미결정)
- 바꾼 파일: `Assets/Resources/Art/chr_player_idle_*.png`, `Docs/QA/`

## 2026-10-29 쓰러짐(death_calm) 8방향 4프레임 납품본
- 한 일: 명세 충돌(idle과 쓰러짐)을 사용자가 결정(쓰러짐, 발 기준점 고정, 상자 완화). 게임 idle 첫 프레임(외곽선 없음)을 시작 프레임으로 v3 생성 8회 + N·NW 재생성 2회. `process.py`에 `death_calm` 클립, `PICK`(f0 = idle, 생성 2~4번), `START_FROM`(게임 idle과 정수 픽셀 정렬) 추가. `sheet_clip.py`에 외곽선 없는 납품(`NO_OUTLINE`)과 쓰러짐 기준점 규칙(`FALLS`) 추가. sheet_clip 종료 코드 0, 16색, 알파 0/255, f0 = 게임 idle 0px 차이. 기록 `ArtSource/PixelLab/Player/10_death_calm/README.md`
- 남은 일: NE f1 검 사라짐, N f1 검 2자루, NW f1~f3 검 없음(눈으로 확인한 실패). 수정 방법은 사용자 결정 필요. 게임 미반영·미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,sheet_clip.py}`, `01_raw/clean/{death_calm,death_calm_start}/`, `01_raw/trials/{death_calm_take1,death_calm_take2}/`, `02_final/{death_calm*,issues.txt,process_log.json,sprite_spec.json}`, `10_death_calm/`

## 2026-10-29 쓰러짐 NE·N 끝 프레임 고정 재생성
- 한 일: 누운 끝 자세를 end_frame으로 고정해 NE(끝 = E f3)·N(끝 = N f3)·NW(끝 = W f3)를 다시 생성(3회). NE·N은 검 한 자루가 유지돼 교체했고, NE 마지막 프레임은 E와 같은 그림이야. NW는 몸이 오른쪽으로 돌아서 버림. sheet_clip 종료 코드 0, f0 = 게임 idle 0px 차이
- 남은 일: NW 검 없음(f1~f3). 해결 방법은 사용자 결정 필요. 게임 미반영·미커밋
- 바꾼 파일: `01_raw/clean/{death_calm/north,death_calm/north-east,death_calm_end}/`, `01_raw/trials/{death_calm_take1_used,death_calm_take3}/`, `02_final/death_calm*`, `10_death_calm/`

## 2026-10-29 쓰러짐 칼 크기를 idle에 맞춤
- 한 일: `process.py` `fix_sword` 추가. 생성 프레임의 칼날이 길거나 두꺼우면 같은 손잡이·각도로 idle 길이 중앙값 11.6px, 두께 2px로 다시 그림(생성 없음). 보이는 칼날은 이제 11.2~12.4px. sheet_clip 종료 코드 0, 16색, 알파 0/255
- 남은 일: NW 검 없음. 게임 미반영·미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `02_final/death_calm*`, `10_death_calm/`

## 2026-10-29 쓰러짐(death_calm) 게임 반영
- 한 일: `export_unity.py`에서 death 원본을 death_calm으로 바꾸고 외곽선을 지움(`NO_OUTLINE`). `chr_player_death_*` 8장만 바뀌었고 납품 시트와 픽셀이 같음. death 6fps를 `RoomManager.playerAnimSpeeds`에 추가(코드 + 씬; Unity MCP로 씬 값 추가 후 저장). EditMode 6/6, 콘솔 오류 0, 플레이 화면 `Docs/QA/Screens/death_calm_ingame_20261029.png`·`death_calm_ingame_zoom_grid_20261029.png`(쓰러진 마지막 프레임 유지, 잘림 없음)
- 남은 일: NW 검 없음. verify.ps1 미실행(에디터 열림). 쓰러지는 중간 프레임은 게임 화면에서 따로 캡처하지 않음. 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/export_unity.py`, `Assets/Resources/Art/chr_player_death_*.png`, `Assets/Scripts/Progression/RoomManager.cs`, `Assets/Scenes/VerticalSlice.unity`, `Docs/QA/Screens/death_calm_ingame*`

## 2026-10-29 피격(hurt_calm) 8방향 4프레임 납품본
- 한 일: 게임 idle을 시작·끝 프레임으로 v3 생성 8회 + 뒤쪽 3방향 재생성 3회(NE·N 채택). `process.py`에 `hurt_calm` 클립, `ALIGN_TO`(마지막 프레임 = 게임 idle, 발 맞춤), `TRIM_ABOVE_IDLE`, 피격 전 방향 칼 붙이기 추가. sheet_clip 종료 코드 0, 16색, 알파 0/255. 기록 `ArtSource/PixelLab/Player/11_hurt_calm/README.md`
- 남은 일: NW f1·f2 칼을 든 팔이 올라감, N 충격 약함. 게임 미반영(반영 시 hurt 6fps 필요). 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/{process.py,sheet_clip.py}`, `01_raw/clean/hurt_calm/`, `01_raw/trials/hurt_calm_take{1,2}/`, `02_final/hurt_calm*`, `11_hurt_calm/`

## 2026-10-29 피격 NW·N 재생성
- 한 일: N(상체 젖힘 강조)·NW(칼 든 손 낮게 유지) 1회씩 재생성해 채택. N 충격 조각은 `drop_fragments`(gap 1, 20px)로 지움, NW 사라진 칼은 시작 자세 칼로 복원. sheet_clip 종료 코드 0, 16색, 알파 0/255, f3 = 게임 idle
- 남은 일: 게임 미반영(반영 시 hurt 6fps 필요). 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `01_raw/clean/hurt_calm/{north,north-west}`, `01_raw/trials/hurt_calm_{used2,take3}/`, `02_final/hurt_calm*`, `11_hurt_calm/`

## 2026-10-29 피격 머리 잘림 수정
- 한 일: 머리 위를 잘라내던 `TRIM_ABOVE_IDLE`을 끔(N 머리가 평평하게 잘렸음). N 재생성 1회도 머리가 솟아 버렸고, N만 몸을 idle로 고정하고 망토 자락만 생성본에서 가져옴(`FREEZE_DIRS`). sheet_clip 종료 코드 0, 16색, 알파 0/255
- 남은 일: 게임 미반영(hurt 6fps 필요). 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `01_raw/trials/hurt_calm_take4/`, `02_final/hurt_calm*`, `11_hurt_calm/`

## 2026-10-29 칼 처리 때문에 몸통이 뚫리던 문제 수정
- 한 일: `fix_sword`가 몸 안쪽 검은 명암까지 지우던 문제를 고침(칼날은 가장자리 회색 덩어리만, 정리는 칼날 주변 2px만). 피격·쓰러짐 다시 조립(sheet_clip 둘 다 종료 코드 0), death 그림 7장 게임에 다시 내보냄(납품 시트와 픽셀 일치). 플레이 화면 `Docs/QA/Screens/death_calm_sw_holefix_20261029.png`(+ 확대 보조선)
- 남은 일: 피격 게임 미반영. 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `02_final/{death_calm,hurt_calm}*`, `10_death_calm/`, `11_hurt_calm/`, `Assets/Resources/Art/chr_player_death_*.png`, `Docs/QA/Screens/death_calm_sw_holefix*`

## 2026-10-29 피격(hurt_calm) 게임 반영
- 한 일: `export_unity.py`에서 hurt를 hurt_calm(외곽선 없음)으로 바꿔 `chr_player_hurt_*` 8장 교체(납품 시트와 픽셀 일치). hurt 16fps → 6fps(코드 + 씬, 씬 변경은 그 한 줄뿐). EditMode 6/6, 콘솔 오류 0, 플레이 화면 `Docs/QA/Screens/hurt_calm_ingame_20261029.png`
- 남은 일: 피격 0.67초와 경직 시간(hitStun) 비교 안 함. verify.ps1 미실행(에디터 열림). 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/export_unity.py`, `Assets/Resources/Art/chr_player_hurt_*.png`, `Assets/Scripts/Progression/RoomManager.cs`, `Assets/Scenes/VerticalSlice.unity`, `Docs/QA/Screens/hurt_calm_ingame*`

## 2026-10-29 대기 머리카락 전체 흔들림 + 피격 N 머리카락
- 한 일: idle_calm 머리카락 구간을 정수리 6줄 → 26줄로 넓힘(정수리만 안쪽 제한, `crown_rows`). 피격 N은 대기 N의 머리카락 흔들림을 가져옴(`HAIR_FROM_FINAL`). idle·hurt·death 다시 조립(sheet_clip 모두 종료 코드 0), 게임 그림 24장 다시 내보냄(납품과 픽셀 일치, idle 첫 프레임 = 피격 끝 = 쓰러짐 시작). EditMode 6/6, 게임 화면 `Docs/QA/Screens/idle_fullhair_ingame_*`
- 남은 일: 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `02_final/{idle_calm,hurt_calm,death_calm}*`, `09_idle_calm/`, `10_death_calm/`, `11_hurt_calm/`, `Assets/Resources/Art/chr_player_{idle,hurt,death}_*.png`, `Docs/QA/Screens/idle_fullhair_*`

## 2026-10-29 동·서 얼굴 일그러짐 수정
- 한 일: `freeze_body` 머리카락 구간에서 피부와 주변 1px을 뺌(넓힌 머리카락 구간이 얼굴 옆선을 덮었음). idle·hurt·death 다시 조립(sheet_clip 모두 종료 코드 0), 게임 그림 다시 내보냄(납품과 픽셀 일치, 이음 프레임 일치). EditMode 6/6, 화면 `Docs/QA/Screens/idle_face_ew_ingame_zoom_grid_20261029.png`
- 남은 일: 미커밋
- 바꾼 파일: `ArtSource/PixelLab/Player/tools/process.py`, `02_final/*_calm*`, `09_idle_calm/`, `10_death_calm/`, `11_hurt_calm/`, `Assets/Resources/Art/chr_player_{idle,hurt,death}_*.png`, `Docs/QA/Screens/idle_face_*`
