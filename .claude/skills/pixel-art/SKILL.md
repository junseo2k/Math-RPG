---
name: pixel-art
description: Pixel Brief 생성기가 만든 픽셀아트 명세 프롬프트를 받아 PixelLab으로 자산을 만들고, 후처리·납품 검사·게임 반영까지 하는 이 프로젝트의 제작 절차. 플레이어 애니메이션(idle·run·attack·hurt·death 등)이 주 대상. "생성 명세", "픽셀아트 제작 명세", "애니메이션 만들어 줘", "스프라이트 다시 만들어", "/pixel-art"에 사용.
argument-hint: "Pixel Brief 명세 프롬프트 전체 (붙여 넣기)"
user-invocable: true
---

# 픽셀아트 제작 (PixelLab → 후처리 → 게임)

명세의 수치·스타일은 명세를 따른다. 이 스킬은 **이 프로젝트에서 그 명세를 실행하는 방법**만 정한다.
작업 폴더: `ArtSource/PixelLab/Player/` (아래 경로는 여기 기준). 도구는 `tools/`.

## 0. 시작 전

- 인수인계 `Docs/Handoff/<브랜치>.md`를 읽는다. 메모리의 사용자 결정(칼은 오른손, 자연스러움 우선, 잘라내지 말고 밀리게 등)을 따른다.
- PixelLab 연결을 확인한다: `get_balance`(남은 생성 수), 플레이어 캐릭터 `11c04004-21b7-4a70-ab98-d6a4a41cf26f`.
- 명세를 실제 도구 스키마와 대응한 표를 먼저 만든다(명세 항목 | PixelLab 인자 | 처리). 인자 이름을 지어내지 않는다.
  - 생성 인자로 되는 것: 동작 문구 `action_description`, `frame_count`(v3: 4~16 짝수), 시작 그림 `custom_start_frame_base64`, 끝 그림 `end_frame_base64`, 방향 `directions`(v3는 한 번에 한 방향).
  - 생성 인자가 없는 것: 64×64·기준점·색 수·알파·외곽선·여백·시트 배열·몸통 고정 → 후처리. 시점·광원 → 시작 그림을 따름. fps → 게임 재생값.
  - **지원 안 됨**: 기존 애니메이션을 동작 참조로 넣기. 스켈레톤 v3(`animate_with_skeleton_v3` + `tools/run_skeleton.py`)는 칼이 사라지고 몸이 돌아 탈락했다(2026-10-30). 명세가 "기존 자세 재현"을 요구하면 먼저 보고하고 묘사 문구로 대신할지 묻는다.
  - 초 단위 목표는 가장 가까운 정수 프레임과 실제 시간(프레임 ÷ fps)을 보고한다.

## 1. 생성 (방향당 1생성, 64px 기준)

- 시작 그림 = **끝 그림 = 게임 idle 첫 프레임**(`Assets/Resources/Art/chr_player_idle_<방향>.png`의 0~63px, 외곽선 없음). base64는 `python -c "import run_skeleton as R; print(R.png_b64(R.frames('idle','<방향>')[0]))"`(tools에서).
  - 끝을 고정하지 않으면 방향이 돌거나 칼이 왼손으로 바뀐다(2026-10-30). 끝나는 자세가 idle이 아닌 동작(쓰러짐 등)은 그 자세 그림을 끝으로 준다.
- 동작 문구에 방향 유지·칼은 오른손·원래 자세로 돌아옴을 넣는다. 예: `running in place, keeps facing <dir> the whole time, ..., sword stays in the right hand pointing down, cape fluttering behind, returns to the start pose`.
- **사용자가 준 이미지의 방향(보통 남쪽) 하나로 먼저 시험** → 기존 그림·사용자 이미지와 나란한 비교 이미지를 보내고 승인 → 나머지 7방향은 같은 `animation_group_id`로 한 번에 넣는다. 이미지가 없으면 남쪽으로 시험한다(2026-10-31 사용자).
- 사용자 이미지 쓰는 법(먼저 크기·배경·외곽선을 열어 확인하고 어떻게 쓸지 알린다):
  - 이 캐릭터의 64px 스프라이트이고 동작 중 한 자세 → 그 방향의 끝 그림(`end_frame`) 또는 중간 핵심 자세로. 크기가 다르면 정수배로만 줄이고, 안 맞으면 키 자세로 쓰지 않는다.
  - 일반 그림·콘셉트 → 자세와 분위기를 동작 문구로 옮기고, 결과 비교용으로만 쓴다. 생성 입력으로 넣지 않는다(외형이 idle과 어긋남).
- 결과 받기: `get_character` 출력이 길어 파일로 저장되면 그 파일에서 그룹의 애니메이션 id를 찾고 `echo "<id> <방향>" | sh tools/fetch_clean.sh <클립> <프레임+1>` → `01_raw/clean/<클립>/<방향>/frame_XX.png`(0번 = 시작 그림). id 목록은 `01_raw/<클립>_jobs.txt`에 남긴다.
- 탈락본은 `01_raw/trials/<클립>_<사유>/`로 옮긴다. 지우지 않는다.

## 2. 후처리 (`tools/process.py`)

- 새 클립은 `CLIPS`에 `(프레임, fps, 반복, 앞에서 버릴 수)`로 추가하고, 필요한 규칙만 켠다. 각 규칙 옆에 날짜·사용자 결정 주석을 단다.

| 필요 | 설정 |
|---|---|
| 한 프레임을 게임 idle과 정확히 맞춤(정수 이동만) | `ALIGN_TO = ('idle_calm', 프레임 번호)` |
| 몸통 상하 0px | `STABLE` |
| 상체 좌우 고정 | `PIN_SIDE` |
| 발 고정 | `PLANTED` |
| 몸은 idle 그대로, 망토·머리카락 끝만 | `FREEZE` |
| 망토·칼 흔들기 | `SWAY`(방향별 `SWAY_DIR`), 칼 숨김 `NO_BLADE` |
| 칼 빠진 프레임 복원 | `lost_sword` 조건에 클립 추가 |

- 망토를 흔들 때 몸 위 붉은 천(허리 천·목도리)은 고정된다(`sway_cape`). 이걸 같이 옮기면 몸통에 구멍이 난다(2026-10-30 W).
- 칼이 몸을 가로지르는 방향은 다시 그리면 두 자루로 보인다 → `SWAY_DIR`에서 `blade=None`.
- 빌드 입력(기준 자세·게임 idle 완성본)은 a5e5566에서 지웠으므로 매번 되살렸다 지운다:

```bash
sh tools/build_env.sh restore
cd tools && python process.py build <클립> && python sheet_clip.py <클립> <납품폴더> 0 4
sh tools/build_env.sh clean
```

- `sheet_clip.py`: 외곽선 없는 납품은 `NO_OUTLINE`, 쓰러지는 동작은 `FALLS`, 발이 움직이는 동작은 `STRIDES`(몸통 높이 범위 0 검사)에 클립을 넣는다. 종료 코드 0 = 통과, 1 = 실패. 크기·여백 초과는 notes(실패 아님).

## 3. 확인 (사용자에게 보내기 전)

- `preview/<클립>_guides_x4.png`(sheet_clip이 만듦: 회색 4px 여백, 파랑 바닥선 y=56·중앙 x=32, 빨강 최대 상자)를 직접 열어 잘림·떨어진 칼·몸통 구멍·외딴 점을 본다.
- GIF: `cd tools && python gif_preview.py sheets <클립> <납품폴더>` → `<클립>_all_x4.gif`(8방향), `<클립>_vs_idle_x3.gif`(idle과 비교). `SendUserFile`로 보낸다.
- 의심 프레임은 8배 확대해서 본다. 몸 안 빈 칸·외딴 1px 점은 숫자로 센다.
- 명세의 "결과 확인 및 납품" 표: 항목 | 기준 | 실측 | 방법 | 통과·실패·미확인. 눈으로 본 것은 "(눈)"이라 적는다. 실행하지 않은 검사는 미확인. 소재별 명암 단계·광원 각도는 측정 방법이 없어 미확인.
- 기록: `<납품폴더>/README.md`(도구 대응표, 검사 표, 다시 만들기 명령)와 인수인계 항목.

## 4. 게임 반영 (사용자가 요청할 때만)

- `tools/export_unity.py`의 `STATES`에 `'<클립>': '<게임 상태>'`, 외곽선 없으면 `NO_OUTLINE`에 추가 → `cd tools && python export_unity.py <클립>`(다른 클립 원본이 없으니 꼭 클립 이름을 준다). 내보낸 8장이 납품 시트 프레임과 픽셀이 같은지 비교한다.
- 재생 속도: `RoomManager.playerAnimSpeeds`의 **코드 기본값과 VerticalSlice 씬 값 둘 다**. 씬은 Unity MCP `execute_code`로 `SerializedObject` 수정 후 `EditorSceneManager.SaveScene`. Unity가 꺼져 있으면 `.unity` 직접 수정 전에 승인을 받는다(AGENTS.md 4절).
- 검증: 에디터가 꺼져 있으면 `verify.ps1`(종료 코드·리포트 경로 보고), 켜져 있으면 Unity MCP로 테스트.
- 게임 화면: Play → `PlayerController`를 끄고 `SpriteAnimator.SetDirection/SetLoop`로 해당 동작을 틀어 캡처 → 플레이어 부분을 잘라 보조선을 겹쳐 `Docs/QA/Screens/<클립>_ingame_<방향>_<날짜>.png`. 끝나면 Play를 멈추고 `Assets/Screenshots` 같은 임시 캡처는 지운다.

## 5. 끝

- 커밋은 사용자가 요청할 때만. 세션 시작 전부터 있던 다른 변경(예: ProjectSettings)은 섞지 않는다.
- 사용자 결정·반려 이유는 날짜와 함께 메모리에 남긴다.
