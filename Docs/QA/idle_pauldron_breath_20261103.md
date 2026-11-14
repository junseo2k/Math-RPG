# 왼쪽 어깨장식 숨쉬기 idle 게임 반영 (2026-11-03)

- 원본: 사용자 Aseprite 편집 `ArtSource/PixelLab/Player/16_idle_greatsword_pauldron_breath/*.aseprite` → Aseprite CLI로 PNG 시트(8프레임, 간격 0) → `export_unity.py --sheet idle_greatsword_pauldron_breath 16_idle_greatsword_pauldron_breath 0`
- 게임 그림: `Assets/Resources/Art/chr_player_idle_{e,ne,n,nw,w,sw,s,se}.png` (512×64, 8프레임). 시트와 픽셀 차이 0
- 시트 검사: `python breath_pauldron.py --check` 종료 코드 0 (`16_idle_greatsword_pauldron_breath/check_20261103.txt`): 16색, 알파 0/255, 발 차이 0, 머리 높이 0,0,1,1,1,1,0,0
- Unity EditMode 테스트: 6/6 통과 (Unity MCP)
- 게임 화면: `Docs/QA/Screens/idle_pauldron_breath_ingame_8dir_20261103.png` — Play 모드에서 PlayerController 끄고 `SpriteAnimator.SetDirection/SetLoop("idle")`로 8방향. 8방향 모두 `chr_player_idle_<방향>`의 64px 프레임(x=0~448)으로 표시됨, 잘림·겹침 없음(눈)
- 재생 속도: 아직 6fps(8프레임 1.33초). 명세는 8fps(1.00초) — 변경은 사용자 승인 대기
- 이전 게임 idle(4프레임) 승인본: `~/Desktop/Math_RPG_승인_스프라이트/2026-11-02_idle_greatsword_aseprite`
- 재생 속도 8fps로 변경(사용자 승인): `RoomManager.playerAnimSpeeds` 코드 기본값 idle 6→8, VerticalSlice 씬 값 6→8(Unity MCP SerializedObject, 씬 저장, git diff 해당 줄만). 컴파일 오류 0, EditMode 6/6 통과. Play 중 실제 값 읽기는 Unity 응답 시간 초과 2회로 미확인
