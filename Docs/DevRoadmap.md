# 개발 로드맵 (시험판 우선)

> 기준일: 2026-10-15
> 상위 문서: `Docs/VerticalSliceSpec.md`, `Docs/ForesightAI.md`
> 방침 [확정]: GRU 예지 보스는 보류하고, 플레이 가능한 로그라이크 시험판 완성을 우선한다.

---

## 1. 구현 현황

| 기능 | 상태 | 파일 | 비고 |
|---|---|---|---|
| 게임 상태 흐름(능력 선택 → 진행 → 사망/클리어) | ✅ 완료 | `Core/GameFlow.cs` | |
| 시작 능력 추첨·선택 | ✅ 완료 | `Player/StartAbilityPool.cs` | 임시 3개, **효과 없음** |
| 시간 진행(보스 처치 +1시간, 06:00 연결) | ✅ 완료 | `Core/GameFlow.cs` | |
| 사망·재탄생(2초, 임시/유지 상태 분리) | ✅ 완료 | `Progression/RunState.cs` | 연출 없음, 저장 없음(메모리) |
| 기본 전투(이동·점프·검·대시) | ✅ 완료 | `Player/PlayerController.cs`, `PlayerAttack.cs`, `Combat/Health.cs` | 수치는 임시 기본값 |
| 축적 보스 | ✅ 완료 | `Enemy/AccumulationBoss.cs` | 임시 그래픽 |
| 데이터 수집 | ✅ 완료 | `Player/PlayerRecorder.cs` | GRU 보류 중에도 유지 |
| 씬 연결 자동화 | ✅ 완료 | `Editor/SceneWiring.cs` | |
| 디버그 UI | 🛠 디버그 | `UI/FlowDebugUI.cs` | F1로 켜고 끔(건너뛰기·강제 사망) |
| 일반 방 구조(가로 방 3종 무작위, 적 전멸 → 출구, 카메라 추적) | ✅ 완료 | `Progression/RoomManager.cs`, `RoomLayouts.cs`, `RoomExit.cs`, `Core/CameraFollow.cs` | 임시 그래픽. 보스2·3 방은 빈 방 + 열린 출구 |
| 일반 적(근접 순찰형) | ✅ 완료 | `Enemy/PatrolEnemy.cs` | 임시 그래픽 |
| 시작 능력 효과 3개(정지장·잔광·가벼운 발걸음) | ✅ 완료 | `Player/AbilityEffects.cs`, `Combat/IFreezable.cs` | 정지장은 일반 적만 |
| 유예 보스(투사체 궤적 예약, 피해 유예) | ✅ 완료 | `Enemy/DeferralBoss.cs`, `DeferredProjectile.cs`, `TrailStrike.cs`, `Combat/DeferredDamage.cs` | 임시 그래픽 |
| 역행 보스(지면 파동 역재생, 2단계 위치 되감기) | ✅ 완료 | `Enemy/ReversalBoss.cs`, `GroundWave.cs` | 임시 그래픽 |
| 시험판 UI(능력 카드, HUD, 결과 화면) | ✅ 완료 | `UI/GameUI.cs`, `AbilitySelectUI.cs`, `HUD.cs`, `ResultUI.cs` | 임시 그래픽, NotoSansKR(`Assets/Resources/Fonts`) |
| 사망·재탄생 연출, 시간 시각화(하늘색·시계·+1시간) | ✅ 완료 | `UI/RebirthOverlay.cs`, `Core/SkyColor.cs`, `UI/HUD.cs` | 화면 연출만(소리 없음), 임시 도형 |
| 자산 교체 장치(그림·BGM·효과음) | ✅ 완료 | `Core/AssetLibrary.cs`, `Core/SoundPlayer.cs`, `Editor/PixelArtImporter.cs` | 스트립 애니메이션(`Core/SpriteAnimator.cs`), 덕킹 포함 |
| 아트·사운드 자산 | ❌ 미제작 | | 제작 시트: `AssetProductionSheet.md`. AI 도구 미정 |
| GRU 예지 보스 | ⏸ 보류 | | `ForesightAI.md` 0장 |

(파일 경로는 `Assets/Scripts/` 기준)

---

## 2. 우선순위 [확정]

| 순위 | 작업 | 담당 | 먼저 필요한 결정 |
|---|---|---|---|
| **P1** 플레이 루프 | ① 일반 적 1종 + 방 구조 | planner → dev | 일반 적 설계, 방 지형 |
| | ② 시작 능력 효과 3개 | planner → dev | 임시 능력 3개 효과 |
| **P2** 1장 보스 완성 | ③ 유예 보스 | planner → dev | 보스 상세 기획(`Docs/Bosses/` 형식) |
| | ④ 역행 보스 | planner → dev | 보스 상세 기획 |
| **P3** 시험판 UI | ⑤ 능력 카드 ⑥ HUD ⑦ 결과 화면 | dev (+artist) | 결과 화면 표시 정보 |
| **P4** 연출·자산 | ⑧ 사망·재탄생 연출, 시간 시각화 | dev (+artist, sound) | 아트·사운드 [제안] 수치 승인 |
| | ⑨ 아트·사운드 교체 | artist, sound → dev | AI 생성 도구·이용 조건 |
| **P5** GRU 재개 | ForesightAI 2~4단계 | dev | `ForesightAI.md` 0-1 재개 조건 |

- 각 작업은 기획(미결 해소) → 사용자 승인 → 구현 순서로 진행한다.
- 디버그 UI는 씬 컴포넌트 보존을 위해 삭제하지 않고 F1 토글로 전환했다.

---

## 3. 다음 작업

**P4-⑨**: 자산 제작(사용자, `AssetProductionSheet.md` 기준) → `Assets/Resources/Art`·`Audio`에 넣으면 자동 적용. 제작 전 필요: AI 도구·이용 조건 결정.

그 다음: **P5 GRU 재개** 조건 중 남은 것 — 예지 보스 기획(planner), 장기 기억용 저장 기능 설계, 데이터 수집 목표량 결정(`ForesightAI.md` 0-1).
