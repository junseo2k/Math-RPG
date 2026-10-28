# 주인공 hurt 8방향 (2026-10-28, 1차 — 미확정)

- PixelLab 캐릭터 `11c04004-…` (CleanCape_NoAhoge, low top-down). v3 custom, 4프레임(+0번 기준 프레임 버림). W·SW는 고친 회전 그림을 시작 프레임으로 사용
- 명세 변경(사용자 결정): 3프레임·12fps → **4프레임·16fps = 0.25초**, 시트 258×66
- 다시 만들기: `python tools/process.py build hurt` → `python tools/sheet_clip.py hurt 05_hurt` (검사 결과 `verify_report.txt`, 수치 `report.json`)
- `<n>_<방향>.png`: 0 E, 1 NE, 2 N, 3 NW, 4 W, 5 SW, 6 S, 7 SE. 바깥 패딩 1px, 프레임 간격 0

## 엔진 참고값 (프로젝트 변경 아님)
Point 필터, 압축 없음, PPU 32, 슬라이스 64×64(오프셋 1,1), Pivot (0.5, 0.125). 게임은 아직 hurt 상태를 쓰지 않음

## 2차 (끝 프레임 고정, 같은 날)
- 방향마다 시작·끝 프레임을 정지 회전 그림으로 고정(`end_frame_url`)해 다시 생성. 1차는 `01_raw/trials/hurt_first/`
- 결과: 검사 실패 33 → 16건. idle 복귀 차이 감소(E 544→167px, SW 780→430px), 마지막 프레임 머리 높이가 idle과 1px 이내
- 남은 문제: NW f0 어깨 옆 검은 선 조각, 폭 37~42px(NW·SW·S 일부 프레임), 기준점(S는 망토 끝이 바닥줄에 닿아 측정상 x=26, N은 f0~f2 발이 1~2px 뜸, W는 x=33), 몸통 상하 2~5px
- NW 재생성(3차): 어깨 옆 검은 선 조각은 사라짐. 대신 f2에 손 옆 2~3px 점, f2 검날이 몸에 가려 손잡이만 보임, f0~f2 발이 1~2px 뜸(기준점 y=54~55), 폭 39~40px. 이전 NW는 `01_raw/trials/hurt_nw_endfix1/`

## Unity 삽입 (2026-10-28)
- `tools/export_unity.py`로 `02_final/hurt/<방향>.png`(패딩 없는 256×64)를 `Assets/Resources/Art/chr_player_hurt_<e|ne|n|nw|w|sw|s|se>.png`로 복사. 납품 시트(258×66, 패딩 1px)는 게임 로더(`AssetLibrary.LoadFrames`: 높이=프레임 크기)와 맞지 않아 쓰지 않음
- 임포트 설정은 idle과 같게(Point, 압축 없음, PPU 32, 밉맵 끔). 게임 코드는 아직 hurt 상태를 재생하지 않음(플레이 중 `SetLoop("hurt")`로만 확인)

## 정리 (2026-10-28)
- 게임 연결: `PlayerController`가 피격(`Health.Damaged`) 때 `PlayOnce("hurt")`를 이미 호출하고 있었음. `RoomManager.playerAnimSpeeds`에 hurt=16fps 추가(코드 기본값 + VerticalSlice 씬 저장값) → 4프레임 0.25초 1회 재생 후 idle 복귀 확인
- 납품 시트(258×66)·미리보기·검사 리포트와 이전 시도들은 휴지통으로 정리. 시트가 다시 필요하면 `python tools/sheet_clip.py hurt 05_hurt`

## 검사 기준 변경 (2026-10-28, 사용자 결정)
동작 크기는 커지는 대로 둔다. 36×48 상자·여백 4px·몸통 상하는 실패가 아닌 기록(`report.json`의 notes). 실패: 캔버스 가장자리 닿음, 발 위치가 같은 방향 정지 자세와 다름(death는 첫 프레임만), 색·알파·외곽선
- 2026-10-28 결함 수정: N·NW f0~f2 발 1~2px 뜸 → `PLANTED`로 프레임별 평행이동. 검사 종료 코드 0, 게임 폴더 hurt 갱신
- 2026-10-28 NW f2 손 옆 4px 점 제거: 외곽선 정리 뒤 남는 4px 이하 섬을 마지막에 지우는 단계 추가(run NW f0 검 끝 조각도 함께 제거)
