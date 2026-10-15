# 자산 제작 시트 (시험판, P4-⑨)

> 상위 문서: `Docs/ArtSpecification.md`, `Docs/SoundSpecification.md`
> 표기: **[확정]** 사용자 확정
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

[음악] medieval {분위기}, {악기}, {선법} mode, {템포} BPM, seamless loop, instrumental, no vocals
[효과음] {소리 설명}, medieval fantasy game sound effect, short, dry, no music
```

---

## 3. 그림

### 3-1. 캐릭터·적·보스
| 이름 | 크기·프레임 | 대상 설명(프롬프트) | 비고 |
|---|---|---|---|
| `chr_player_{idle 4, run 6, jump 2, attack 4, dash 3, hurt 2, death 6}` | 48×48 | fallen sun deity in worn knightly garb, longsword, golden sun emblem, faint halo | [확정] 외형 |
| `enemy_sentinel_{idle 4, walk 6, windup 2, slash 3, hurt 2, death 5}` | 32×32 | petrified night sentinel, silver-blue cracked stone armor, faint moonlight eyes, short blade | [확정] 외형(세계관: 시종이 시간을 멈춰 굳힌 밤의 파수꾼) |
| `boss_accumulation_{idle 4, windup 2, charge 4, warning 4, release 4, exhausted 4, death 6}` | 96×96 | moon attendant in pale robes, bronze rings stacking around its body, hourglass core | [확정] 외형(시종 외형은 기획 미결) |
| `boss_deferral_{idle 4, cast 3, death 6}` | 96×96 | veiled moon attendant holding a frozen hourglass, sand grains suspended in the air around it | [확정] 외형 |
| `boss_reversal_{idle 4, walk 6, slam 4, phase2 4, death 6}` | 96×96 | moon attendant in teal robes, counter-clockwise spiral halo, faint afterimages trailing behind | [확정] 외형 |

### 3-2. 이펙트
| 이름 | 크기·프레임 | 대상 설명 | 비고 |
|---|---|---|---|
| `fx_sword_arc` | 48×48 · 3 | golden crescent sword slash arc | [확정] |
| `fx_hit` | 32×32 · 3 | small white impact burst | [확정] |
| `fx_dash_trail` | 48×48 · 3 | fading golden afterimage | [확정] |
| `fx_accum_ring` | 32×32 · 5 | bronze ring filling step by step, last frame white-hot | [확정] 축적 게이지 |
| `fx_shockwave_ring` | 128×128 · 5 | expanding golden ring wave | [확정] 축적 방출 |
| `fx_def_projectile` | 16×16 · 2 | amber glowing sand orb | [확정] 유예 투사체 |
| `fx_def_trail_dot` | 8×8 · 1 | amber dotted sand grain | [확정] 유예 궤적 점선 |
| `fx_def_trail_strike` | 32×32 · 3 | amber sand burst, horizontal tileable | [확정] 궤적 발동 |
| `fx_rev_wave` | 32×32 · 4 | teal ground shockwave crest | [확정] 지면 파동 |
| `fx_rev_spiral` | 48×48 · 6 | teal counter-clockwise spiral | [확정] 반시계 나선 |
| `fx_freeze_field` | 96×96 · 4 | pale blue frozen-time circle with clock marks | [확정] 순간 정지장 |

### 3-3. 배경·오브젝트
| 이름 | 크기 | 대상 설명 | 비고 |
|---|---|---|---|
| `bg_ch1_far` | 640×360 | silhouettes of a ruined gothic cathedral and city walls at dusk, no sky (transparent top) | [확정] 원경. 하늘색은 코드(SkyColor) |
| `bg_ch1_near` | 640×360 | broken pillars and stone rubble, transparent background | [확정] 근경 |
| `tile_ground_ch1` | 32×32 | worn cathedral stone floor tile, seamless | [확정] |
| `obj_exit_{locked, open}` | 32×64 | gothic arched wooden door; open version glowing gold | [확정] 출구 |

### 3-4. UI
| 이름 | 크기 | 대상 설명 |
|---|---|---|
| `ui_card` | 140×200 | parchment card with illuminated manuscript border |
| `ui_icon_{active, passive}` | 32×32 | active: sword-tip diamond frame / passive: round frame |
| `ui_badge_temp` | 34×14 | small red wax-seal tag |
| `ui_hp_{full, empty}` | 16×16 | golden sun emblem / cracked dark sun emblem |
| `ui_clock_{face, hand}` | 20×20 / 2×8 | medieval astronomical clock face / golden sun hand |
| `ui_stage_{room, boss}` | 8×8 / 12×12 | small stone square / diamond gem |

(UI 크기는 기준 해상도 640×360 픽셀. 화면에는 3배로 표시)

---

## 4. 소리

### 4-1. 음악
| 이름 | 장면 | 프롬프트 요소 | 비고 |
|---|---|---|---|
| `bgm_start` | 시작 지점 | calm dusk, lute arpeggio, low drone, mixolydian, 70 BPM | [확정] |
| `bgm_ch1` | 1장 일반 방 | steady tabor rhythm, recorder melody, dorian, 110 BPM | [확정] |
| `bgm_boss_accumulation` | 축적 보스 | layered chant choir, bells, tabor, hurdy-gurdy drone, phrygian, 120 BPM, building ostinato | [확정] |
| `bgm_boss_deferral` | 유예 보스 | sparse plucked strings with delayed echoes, suspended choir, phrygian, 105 BPM | [확정] 시험판 보스가 3명이 되어 추가 |
| `bgm_boss_reversal` | 역행 보스 | driving hurdy-gurdy, reversed bell swells, dorian, 130 BPM, second half intensifies | [확정] 추가 |
| `bgm_clear` | 클리어 | short bright bell fanfare, not looping | [확정] |

### 4-2. 효과음
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

## 5. 미결
- AI 생성 도구와 상업적 이용 조건
- 한글 픽셀 글꼴(현재 NotoSansKR 사용)
