# QA: 게임 idle → idle_calm(몸통 고정) 교체 (2026-10-29)

| 검사 | 방법 | 결과 |
|---|---|---|
| verify.ps1 | `.\verify.ps1` | 실행 못 함, 종료 코드 3 (Unity 에디터가 프로젝트를 열고 있음) |
| EditMode 테스트 | Unity 브리지 6400 직접 연결 `run_tests` (job 99db3ee1…) | 6/6 통과, 실패 0 |
| 게임 파일 일치 | `cmp` 02_final/idle_calm/<방향>.png ↔ Assets/Resources/Art/chr_player_idle_<방향>.png | 8방향 동일 |
| 로드 | `Resources.Load<Texture2D>("Art/chr_player_idle_*")` | 8방향 256×64(4프레임), Point 필터 |
| 화면 | 플레이 모드 스크린샷 `Screens/idle_calm_ingame_20261029.png` + 플레이어 주변 4배 확대·8px 격자 | 잘림·겹침 없음, 외곽선 유지(남서 방향 1장만 확인) |
| 빈 참조 검사 | verify.ps1에 포함 | 미실행 (verify.ps1을 못 돌림) |

- 다른 동작(run·attack·dash·hurt·death) 파일도 다시 복사했지만, 원본이 마지막 반영(01:14) 이후 바뀌지 않아 내용은 같다.

## 바깥 외곽선 제거판 반영 (2026-10-29 07:26)
| 검사 | 방법 | 결과 |
|---|---|---|
| 게임 파일 | `export_unity.py` `NO_OUTLINE`(idle_calm만) | idle 8개만 바뀜, 다른 동작 그대로 |
| 알파·색 | 파일 다시 열어 검사 | 알파 0/255, 방향별 10~16색 |
| 남은 가장자리 검은 픽셀 | 새 가장자리 중 RGB 0 | 시트당 21~35개(프레임당 5~9개, 외곽선 안쪽에 붙어 있던 검은 선) |
| EditMode 테스트 | 브리지 직접 연결 `run_tests` (job c3ffa2f1…) | 6/6 통과 |
| 로드 | `Resources.Load` | 8방향 256×64 |
| 화면 | `Screens/idle_calm_nooutline_ingame_20261029.png`(북동), `..._ingame2_...png`(북서) + 4배 확대·격자 | 바깥 검은 테두리 없음, 잘림·겹침 없음 |
| verify.ps1·빈 참조 | — | 미실행(에디터 열림, 종료 코드 3) |
- 발 아래 외곽선도 빠져 발끝이 기준선보다 1px 위(행 54)에 보인다.
