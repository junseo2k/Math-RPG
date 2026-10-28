# 주인공 death 8방향 (2026-10-28, 1차 — 미확정)

- PixelLab 캐릭터 `11c04004-…`, v3 custom 6프레임(+0번 기준 프레임 버림) = 명세 그대로 6프레임·10fps·0.6초. W·SW는 고친 회전 그림을 시작 프레임으로
- 다시 만들기: `python tools/process.py build death` → `python tools/sheet_clip.py death 06_death` (`verify_report.txt`, `report.json`)
- `<n>_<방향>.png`: 0 E … 7 SE, 386×66, 바깥 패딩 1px, 간격 0. 게임용 원본(패딩 없음)은 `02_final/death/`
- N: 2회 모두 쓰러지지 않음(1차 서 있음, 2차 두 팔을 들고 서 있음). NW: 1차 손 옆 조각 → 재생성본 사용. 이전 시도 `01_raw/trials/death_first/`
- 게임 미적용. 게임 쪽 참고: 사망은 마지막 프레임 유지(`SpriteAnimator.PlayHold("death")`), 속도 death=10fps 추가 필요

## 검사 기준 변경 (2026-10-28, 사용자 결정)
동작 크기는 커지는 대로 둔다. 36×48 상자·여백 4px·몸통 상하는 실패가 아닌 기록(`report.json`의 notes). 실패: 캔버스 가장자리 닿음, 발 위치가 같은 방향 정지 자세와 다름(death는 첫 프레임만), 색·알파·외곽선

## 결함 수정 (2026-10-28)
- N: 두 번 쓰러지지 않아(서 있음 → 팔 들고 서 있음), NW의 누운 마지막 프레임을 48×48로 옮겨 끝 프레임으로 고정하고 다시 생성 → 앉았다가 옆으로 누워 정지. 시도본 `01_raw/trials/death_n_*`
- E·NE·NW·SW 첫 프레임 발 1px 어긋남 → `process.py` `PLANTED`로 그 프레임만 평행이동
- 검사 `sheet_clip.py death 06_death` 종료 코드 0
