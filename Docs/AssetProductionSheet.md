# 자산 제작 시트 (시험판 A)

> 상위 문서: `Docs/ArtSpecification.md`, `Docs/SoundSpecification.md`
> 표기: **[확정]** 사용자 확정 / **[승인]** 2026-10-17 사용자 승인(시험판 A) / **[권장값]** 제안, 미확정 / **[보류]** 시험판 A에서 쓰지 않음
> AI 생성 도구는 **미정**이다. 프롬프트는 특정 도구에 맞추지 않은 일반 영문이다. 도구가 정해지면 그 형식에 맞게 다듬는다.
> 생성물은 반드시 보정·편집 체크리스트(아트 7-2, 사운드 8-2)를 거친 뒤 넣는다. 보정 전 파일을 완성 자산으로 보고하지 않는다.

---

## 1. 넣는 위치와 규칙 [확정]

| 종류 | 위치 | 형식 |
|---|---|---|
| 그림 | `Assets/Resources/Art/{이름}.png` | PNG, 투명 배경, PPU 32, Point 필터, 압축 없음 |
| 음악 | `Assets/Resources/Audio/{이름}.ogg` | OGG 44.1kHz 스테레오, 반복곡은 이음매 없이 |
| 효과음 | `Assets/Resources/Audio/{이름}.wav` | WAV 44.1kHz 모노, 앞 무음 제거 |

- 애니메이션은 **가로 한 줄 스트립**(프레임 크기 × 프레임 수). 피벗은 프레임 중앙.
- 파일이 없으면 게임은 지금의 임시 도형·무음으로 동작한다(자산 교체 장치).
- 이름 규칙: `chr_` 캐릭터, `boss_` 보스, `enemy_` 적, `fx_` 이펙트, `ui_` UI, `tile_`·`bg_`·`obj_` 배경·오브젝트, `bgm_`·`sfx_` 소리.

## 2. 공통 프롬프트

```
[그림 앞] pixel art, {크기}px frames, horizontal sprite strip of {프레임 수} frames, side view 2D platformer,
medieval dark fantasy, {대상 설명}, {상태},
[그림 뒤] limited 32-color palette, transparent background, no anti-aliasing, crisp pixels, centered pivot

[코드가 색칠·늘리는 이펙트] pixel art, {크기}px, single frame, plain white shape on transparent background,
{모양 설명}, 1px outline, flat fill, no shading, tileable when stretched

[음악] medieval {분위기}, {악기}, {선법} mode, {템포} BPM, seamless loop, instrumental, no vocals
[효과음] {소리 설명}, medieval fantasy game sound effect, short, dry, no music
```

---

## 3. 시험판 A 자산표 [승인]

열 설명
- **교체 방식** — 자동: 파일만 넣으면 코드가 바로 사용 / dev: 코드 연결 작업 필요 (ArtSpecification 8장 번호)
- **해상도**는 기준 해상도(640×360) 픽셀. 프레임 수·fps는 [권장값] (기본 10fps)
- **우선순위** — P0 전투 판독 / P1 화면 완성도 / P2 나머지

### 3-1. 주인공 (48×48, 금색 계열)
외형 [확정]: fallen sun deity in worn knightly garb, longsword, golden sun emblem, faint halo

| 이름 | 사용 위치 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|
| `chr_player_idle` | 대기 | 4 | P0 | 자동 | 기본 몸통·검 (모든 동작의 바탕) |
| `chr_player_run` | 이동 | 6 | P0 | 자동 | idle 몸통 |
| `chr_player_jump` | 공중 (상승·낙하 공용) | 2 (상승 1, 하강 1) | P0 | 자동 | — |
| `chr_player_fall` | 낙하 분리 시 | 2 | P2 | dev ④ | jump 하강 프레임 |
| `chr_player_dash` | 회피 | 3 | P0 | 자동 | `fx_dash_trail` 잔상 |
| `chr_player_attack` | 검 공격 | 4 | P0 | 자동 | `fx_sword_arc` |
| `chr_player_hurt` | 피격 | 2 | P0 | 자동 (빨간 번쩍·깜빡임은 dev ⑤) | — |
| `chr_player_death` | 사망 (0.0~0.4초) | 6 | P2 | 자동 (끝 프레임 유지) | — |

### 3-2. 일반 적 — 밤의 파수꾼 (32×32, 은청색)
외형 [확정]: petrified night sentinel, silver-blue cracked stone armor, faint moonlight eyes, short blade
행동: 순찰 → 플레이어 감지 시 추격 → 예비 동작 0.5초 → 베기 → 회복 1초 [임시 수치]

| 이름 | 사용 위치 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|
| `enemy_sentinel_idle` | 대기 | 4 | P1 | 자동 | — |
| `enemy_sentinel_walk` | 순찰·추격 | 6 | P1 | 자동 | — |
| `enemy_sentinel_windup` | 예비 동작 (위험 신호) | 2 | P0 | 자동 | 칼을 뒤로 당긴 자세가 분명해야 함 |
| `enemy_sentinel_slash` | 베기 | 3 | P0 | 자동 | `fx_sword_arc` 은청색 버전 가능 |
| `enemy_sentinel_hurt` | 피격 | 2 | P1 | 자동 | — |
| `enemy_sentinel_death` | 사망 | 5 | P2 | dev ③ (지금은 즉시 비활성) | 돌 부스러기 |

### 3-3. 시험판 보스 — 달의 시종(가칭) (96×96, 은청색)
외형은 미결 (D1). 실루엣 요구사항만 정한다: 패턴마다 다른 예비 자세 4종, 명도 차이가 큰 은청색(코드가 주황·빨강으로 덧칠).

| 이름 | 사용 위치 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|
| `boss_slice_idle` | 대기·회복 | 4 | P0 | 자동 | 기본 몸통 |
| `boss_slice_windup_slash` | 내려베기 예고 | 2 | P0 | dev ② | 검을 머리 위로 |
| `boss_slice_slash` | 내려베기 실행 | 3 | P1 | dev ② | `fx_sword_arc` 확대 |
| `boss_slice_windup_charge` | 돌진 예고 | 2 | P0 | dev ② | 몸을 낮춤 |
| `boss_slice_charge` | 돌진 실행 | 4 | P1 | dev ② | `fx_dash_trail` 은청색 버전 |
| `boss_slice_cast` | 탄막 예고·실행 | 3 | P0 | dev ② | 손에 달빛 결정 (`fx_boss_shot` 모티브) |
| `boss_slice_summon` | 낙하 예고·실행 | 3 | P0 | dev ② | 두 팔을 하늘로 |
| `boss_slice_death` | 처치 | 6 | P2 | dev ③ | — |

### 3-4. 보스 패턴 이펙트 — 코드 연결됨 (흰색으로 그림, 코드가 색칠·늘림)
| 이름 | 사용 위치 | 해상도 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|---|
| `fx_boss_zone` | 모든 패턴 예고 바깥 윤곽 | 32×32 (1유닛) | 1 | P0 | 자동 (늘어남, dev ① 권장) | `fx_boss_fill`과 같은 테두리 |
| `fx_boss_fill` | 예고 채움 → 실행 순간 꽉 참 | 32×32 | 1 | P0 | 자동 (늘어남) | 빗금 반복 무늬 권장 |
| `fx_boss_aim` | 탄막 조준선 | 32×32 | 1 | P0 | 자동 (가로로 늘어남) | 가로 반복 점선·화살촉 |
| `fx_boss_shot` | 탄막 탄 | 32×32 (화면 0.6유닛) | 1 | P0 | 자동 (dev ① 크기 고정 권장) | 달 파편 결정 |
| `fx_boss_rock` | 낙하 바위 | 32×32 (화면 1.6×1유닛) | 1 | P0 | 자동 (가로 늘어남 주의) | 달 파편 결정 큰 버전 |
| `fx_sword_range` | 검 판정 범위 (휘두를 때 0.1초) | 32×32 (화면 1.5×1유닛) | 1 | P1 | 자동 (늘어남) | `fx_boss_zone` 테두리 양식 |

### 3-5. 이펙트 — 연결 필요
| 이름 | 사용 위치 | 해상도 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|---|
| `fx_rewind_spiral` | 역행 발동 (플레이어 중심) | 96×96 | 6 | P0 | dev ⑤ | 청록 나선 + 금색 가장자리. 본편 보스 역행 `fx_rev_spiral`과 구분 필요 |
| `fx_rewind_screen` | 역행 정지 0.3초 화면 테두리 | 640×360 | 1 | P1 | dev ⑤ | 청록 비네트 |
| `fx_sword_arc` [확정] | 검 궤적, 보스 내려베기 확대 | 48×48 | 3 | P1 | dev ⑤ | 보스·파수꾼 공용 |
| `fx_hit` [확정] | 피격 순간, 보스 실행 백열 번쩍 확대 | 32×32 | 3 | P1 | dev ⑤ | 공용 |
| `fx_dash_trail` [확정] | 회피 잔상, 보스 돌진 잔상 | 48×48 | 3 | P1 | dev ⑤ | 공용 (색은 코드) |
| `fx_shield` | 보호막 보유 표시 | 48×48 | 2 | P2 | dev | 은빛 원 |

### 3-6. 배경·타일·오브젝트
| 이름 | 사용 위치 | 해상도 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|---|
| `tile_ground_ch1` [확정] | 바닥·벽·발판 | 32×32 이음매 없음 | 1 | P0 | 자동 (타일 반복) | 일반·보스 공용 |
| `bg_ch1_far` [확정] | 원경 (폐허 성당·성벽, 하늘 투명) | 640×360 | 1 | P1 | 연결 여부 미확인 (배경 코드 미열람) | 일반·보스 공용 |
| `bg_ch1_near` [확정] | 근경 (부서진 기둥·잔해) | 640×360 | 1 | P1 | 연결 여부 미확인 | 일반 스테이지 |
| `bg_ch1_boss_near` | 보스전 근경 (멈춘 천문 시계탑 잔해) | 640×360 | 1 | P2 | dev | `bg_ch1_far` 공유 |
| `obj_exit_{locked, open}` [확정] | 출구 | 32×64 | 1 | P1 | 자동 | — |
| `obj_shop_passage` | 숨은 시간의 통로 | 32×64 | 4 (은은한 깜빡임) | P2 | dev ⑥ | 출구 문틀 양식 + 멈춘 시계 문양 |

### 3-7. HUD
| 이름 | 사용 위치 | 해상도 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|---|
| `ui_hp_bar_frame` | HP 바 테두리 (태양 문장 끝장식) | 140×12 | 1 | P1 | dev ⑥ | `ui_boss_bar_frame` 양식 |
| `ui_boss_bar_frame` | 보스 HP 바 테두리 (은청색) | 280×10 | 1 | P1 | dev ⑥ | 위와 같은 양식 |
| `ui_icon_rewind` | 역행 쿨타임 | 16×16 | 1 | P1 | dev ⑥ | `fx_rewind_spiral` 축소 |
| `ui_icon_dodge` | 회피 게이지 | 16×16 | 1 | P2 | dev ⑥ | — |
| `ui_icon_potion` | 포션 수 | 16×16 | 1 | P1 | dev ⑥ | 상점 품목 아이콘 공용 |
| `ui_icon_gold` | G | 16×16 | 1 | P1 | dev ⑥ | 상점 가격 표시 공용 |
| `ui_icon_shard` | 기억 조각 | 16×16 | 1 | P1 | dev ⑥ | 성장 화면 공용 |
| `ui_clock_{face, hand}` [확정] | 시계 | 20×20 / 2×8 | 1 | P1 | 자동 | — |
| `ui_stage_{room, boss}` [확정] | 진행 3칸 | 8×8 / 12×12 | 1 | P2 | 자동 | — |

### 3-8. 보상·상점·성장 UI
| 이름 | 사용 위치 | 해상도 | 프레임 | 우선 | 교체 방식 | 공유 요소 |
|---|---|---|---|---|---|---|
| `ui_card` [확정] | 특성 카드, 상점 품목, 성장 항목 | 140×200 | 1 | P1 | 자동 (특성 카드) / dev ⑥ (상점·성장) | 공용 테두리 |
| `ui_icon_active` [확정 이름] | 특성 종류: **역행 전용** (마름모) | 32×32 | 1 | P1 | 자동 | — |
| `ui_icon_passive` [확정 이름] | 특성 종류: **공용** (원형) | 32×32 | 1 | P1 | 자동 | — |
| `ui_badge_level` | Lv 배지 | 60×14 | 1 | P2 | dev | 밀랍 봉인 양식 |
| `ui_trait_{lost_moment, reclaimed_strike, repeating_fate, past_afterimage, sharp_slash, tough_body, time_saving, time_refund}` | 특성 아이콘 8종 | 16×16 | 1 | P2 | dev | 16×16 격자, 역행 전용은 청록 테두리 |
| `ui_relic_{history_shard, cracked_pocket_watch, iron_spring, broken_needle, moon_shard}` | 유물 아이콘 5종 | 16×16 | 1 | P2 | dev | 16×16 격자, 희귀는 금색 테두리 |
| `ui_shop_bg` | 상점 배경 (멈춘 시계탑 내부) | 640×360 | 1 | P2 | dev ⑥ | — |
| `ui_growth_bg` | 영구 성장 배경 (희미한 기억) | 640×360 | 1 | P2 | dev ⑥ | — |

### 3-9. 프롬프트 예 [권장값]
| 대상 | 대상 설명 |
|---|---|
| `fx_boss_zone` / `fx_boss_fill` | plain white rectangle outline / white diagonal hatching fill |
| `fx_boss_aim` | white dashed horizontal line with small arrowheads |
| `fx_boss_shot` / `fx_boss_rock` | small pale moon crystal shard / large jagged moon crystal chunk |
| `fx_rewind_spiral` | teal counter-clockwise spiral with thin golden rim, reversing clock hands |
| `obj_shop_passage` | narrow gothic wall crack with a frozen clock face glowing faint gold |
| `ui_icon_shard` | small glowing memory crystal, pale gold |

---

## 4. 보류 — 이전 시험판(보스 3명)·본편 [보류]

### 4-1. 캐릭터·보스
| 이름 | 크기·프레임 | 대상 설명 | 비고 |
|---|---|---|---|
| `boss_accumulation_{idle 4, windup 2, charge 4, warning 4, release 4, exhausted 4, death 6}` | 96×96 | moon attendant in pale robes, bronze rings stacking around its body, hourglass core | [확정] 외형 |
| `boss_deferral_{idle 4, cast 3, death 6}` | 96×96 | veiled moon attendant holding a frozen hourglass, sand grains suspended in the air around it | [확정] 외형 |
| `boss_reversal_{idle 4, walk 6, slam 4, phase2 4, death 6}` | 96×96 | moon attendant in teal robes, counter-clockwise spiral halo, faint afterimages trailing behind | [확정] 외형 |

### 4-2. 이펙트
| 이름 | 크기·프레임 | 대상 설명 | 비고 |
|---|---|---|---|
| `fx_accum_ring` | 32×32 · 5 | bronze ring filling step by step, last frame white-hot | 축적 게이지 |
| `fx_shockwave_ring` | 128×128 · 5 | expanding golden ring wave | 축적 방출 |
| `fx_def_projectile` | 16×16 · 2 | amber glowing sand orb | 유예 투사체 |
| `fx_def_trail_dot` | 8×8 · 1 | amber dotted sand grain | 유예 궤적 점선 |
| `fx_def_trail_strike` | 32×32 · 3 | amber sand burst, horizontal tileable | 궤적 발동 |
| `fx_rev_wave` | 32×32 · 4 | teal ground shockwave crest | 지면 파동 |
| `fx_rev_spiral` | 48×48 · 6 | teal counter-clockwise spiral | 반시계 나선 |
| `fx_freeze_field` | 96×96 · 4 | pale blue frozen-time circle with clock marks | 순간 정지장 |

### 4-3. UI
| 이름 | 크기 | 대상 설명 | 비고 |
|---|---|---|---|
| `ui_badge_temp` | 34×14 | small red wax-seal tag | 시작 능력 [임시] 배지 |
| `ui_hp_{full, empty}` | 16×16 | golden sun emblem / cracked dark sun emblem | HP 5칸 방식 (현재 HP 바로 교체됨) |

---

## 5. 소리

> 시험판 A 기준 갱신은 sound 담당 작업이다. 아래는 이전 시험판 기준 그대로다 (축적·유예·역행 보스, 정지장, 능력 카드 항목은 시험판 A에서 쓰지 않음).

### 5-1. 음악
| 이름 | 장면 | 프롬프트 요소 | 비고 |
|---|---|---|---|
| `bgm_start` | 시작 지점 | calm dusk, lute arpeggio, low drone, mixolydian, 70 BPM | [확정] |
| `bgm_ch1` | 1장 일반 방 | steady tabor rhythm, recorder melody, dorian, 110 BPM | [확정] |
| `bgm_boss_accumulation` | 축적 보스 | layered chant choir, bells, tabor, hurdy-gurdy drone, phrygian, 120 BPM, building ostinato | [확정] |
| `bgm_boss_deferral` | 유예 보스 | sparse plucked strings with delayed echoes, suspended choir, phrygian, 105 BPM | [확정] |
| `bgm_boss_reversal` | 역행 보스 | driving hurdy-gurdy, reversed bell swells, dorian, 130 BPM, second half intensifies | [확정] |
| `bgm_clear` | 클리어 | short bright bell fanfare, not looping | [확정] |

### 5-2. 효과음
| 이름 | 상황 | 소리 설명 | 비고 |
|---|---|---|---|
| `sfx_sword_swing` / `sfx_sword_hit` | 검 공격 / 적중 | short metal whoosh / dull blade impact | [확정] |
| `sfx_jump` / `sfx_land` / `sfx_dash` | 이동 | cloth jump / soft stone landing / quick wind rush | [확정] |
| `sfx_player_hurt` / `sfx_player_death` | 피격 / 사망 | short dull thud / low fading string | [확정] |
| `sfx_enemy_hurt` | 적 피격 | stone chip crack | [확정] |
| `sfx_enemy_windup` / `sfx_enemy_slash` | 파수꾼 공격 | stone grinding / heavy blade swing | [확정] |
| `sfx_acc_stack` ~ `sfx_acc_charge` (6종) | 축적 보스 | 사운드 사양 5-1 그대로 | [확정] |
| `sfx_def_fire` | 유예 투사체 발사 | sand whoosh | [확정] |
| `sfx_def_trail_warn` | 궤적 발동 예고 | rising sand hiss | [확정] |
| `sfx_def_trail_strike` | 궤적 발동 | delayed echo burst | [확정] |
| `sfx_def_deferred` / `sfx_def_apply` | 피해 보류 / 보류 피해 적용 | ticking hourglass / muffled delayed thud | [확정] |
| `sfx_rev_windup` / `sfx_rev_wave` | 역행 내려치기 / 파동 | heavy breath / rolling ground rumble | [확정] |
| `sfx_rev_reverse` | 파동 역재생 시작 | reversed rumble swell | [확정] 콘셉트(역재생) |
| `sfx_rev_phase2` | 2단계 전환 | reversed choir swell | [확정] |
| `sfx_rev_rewind_warn` / `sfx_rev_rewind` | 위치 되감기 예고 / 발동 | clock ticking backwards / tape rewind whoosh | [확정] |
| `sfx_freeze` | 순간 정지장 | glass chime freeze | [확정] |
| `sfx_clock_advance` / `sfx_rebirth` | 시각 전진 / 재탄생 | clock bell strike / rising major chord | [확정] |
| `sfx_ui_hover` / `sfx_ui_select_active` / `sfx_ui_select_passive` | 능력 카드 | single lute note / sword draw + chord / soft bell + chord | [확정] |

---

## 6. 미결
- AI 생성 도구와 상업적 이용 조건
- 한글 픽셀 글꼴(현재 NotoSansKR 사용)
- 시험판 보스 외형 (D1)
- 배경(`bg_ch1_*`)의 코드 연결 여부 확인 (배경 관련 코드 미열람)
