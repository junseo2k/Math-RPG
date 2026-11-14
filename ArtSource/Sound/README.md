# 효과음 원본 (2026-11-13)

출처: Kenney Impact Sounds · RPG Audio (CC0, `candidates/Kenney_License.txt`)

| 게임 파일 | 원본 | 처리 |
|---|---|---|
| `sfx_sword_swing.wav` | S1 knifeSlice | 소리 시작 5ms 전부터 0.28초, 끝 60ms 페이드, 모노 48kHz |
| `sfx_sword_hit.wav` | H1 impactPunch_heavy_002 + H4 impactMetal_medium_001(-4dB) | 시작을 맞춰 겹침, 0.30초, 끝 80ms 페이드, 모노 44.1kHz |

둘 다 최대 음량 0.89로 맞춤. 후보 전체는 `candidates/`.
| `sfx_footstep_0~2.wav` | footstep_concrete_000~002 | 시작 1ms 전부터 끝까지(약 0.1초), 최대 음량 0.45(전투음보다 작게), 달리기 프레임 0·4에서 무작위 1개 |
| `sfx_player_hurt.wav` | impactPunch_heavy_003 | 사양대로 -3반음·저역 통과 1.5kHz, 0.25초, 끝 80ms 페이드 |

## Sonniss GDC 2024 묶음 1of9 (royalty-free, 출처 표기 불필요, `candidates/sonniss/License - GDC Game Audio.pdf`)

96kHz 24bit → 48kHz 모노로 바꿈. 소리 시작 2ms 전부터 자르고 끝을 페이드.

| 게임 파일 | 원본 | 처리 |
|---|---|---|
| `sfx_rewind` | InMotionAudio ClockMultiReverse51 | 거꾸로 감기는 소리가 커져 첫 큰 봉우리에서 끝나는 1.2초 |
| `sfx_clock_advance` | InMotionAudio Clock53 | 시계 한 번 치는 소리 1.5초 |
| `sfx_potion` | Eiravaein Flask glass | 1.2초 |
| `sfx_enemy_hurt` | Bluezone stone_impact_hammer_015 | 0.4초 |
| `sfx_acc_release` | Bluezone stone_impact_041 | 1.6초 |
| `sfx_player_death` | Bluezone modern_cinematic_impact_boom_003 | 2.2초 |
| `sfx_passage_open` | Bluezone industrial_lever_switch_039 | 1.2초 |
| `sfx_ui_hover` | Bluezone tiny_gears click_003 | 0.05초, 작게 |
| `amb_ch1` | InMotionAudio CaveDesign01 | 20.6초 반복, 끝 1.5초를 처음과 섞어 이음매 제거. 일반 방에서만 재생(`SoundPlayer.ambienceVolume` 0.35) |

`sfx_rewind_alt`·`amb_ch1_drip`은 후보로만 둠.
