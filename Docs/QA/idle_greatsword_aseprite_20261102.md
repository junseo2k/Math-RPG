# QA: 양손검 idle Aseprite 8방향 편집본 게임 반영 (2026-11-02)

| 항목 | 결과 |
|---|---|
| 납품 검사 `sheet_clip.py idle_greatsword 13_idle_greatsword 0 4 --check` | 종료 코드 0, ALL PASS (리포트 `ArtSource/PixelLab/Player/13_idle_greatsword/check_20261102.txt`) |
| 게임 반영 `export_unity.py --sheet idle_greatsword 13_idle_greatsword` | 종료 코드 0, 8방향 모두 납품 시트와 픽셀 0 차이 |
| EditMode 테스트(빈 참조 포함, Unity MCP) | 6/6 통과. verify.ps1은 에디터가 열려 있어 종료 코드 3(실행 안 됨) |
| 게임 화면 | `Docs/QA/Screens/idle_greatsword_edit_ingame_8dir_20261102.png`, 보조선 `..._guides_20261102.png`: 8방향 모두 바닥선 위, 잘림·겹침 없음 |

메모
- 시트 형식: 268×64, 4프레임, 간격 4px, 반투명 0px, 발끝 y=54(전 프레임).
- 팔레트 밖 색: West f0 (37~38, 28~29) `F7DD88` 4px. 사용자 결정으로 그대로 둠.
- 콘솔 오류 10건은 QA용 복제 오브젝트에서 컴포넌트를 지우려다 난 것(게임 코드와 무관), 플레이 종료로 사라짐.
