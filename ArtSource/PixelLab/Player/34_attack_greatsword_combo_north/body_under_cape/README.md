# 북쪽 망토 안 몸 (2026-11-08, 초안)

사용자 결정: 망토가 젖혀질 때 안의 몸을 드러낸다. 참고 = `16_idle_greatsword_pauldron_breath/*.aseprite` 8방향.

- 손으로 찍은 픽셀(생성 없음). 64px idle 좌표, 레이어: `head.png`(12_run_calm 북쪽 머리, 외형 v1보다 왼쪽 1px — 11/08 사용자 채택), `torso.png`(등판·어깨·벨트·허리 천), `arms_upper.png`(옆 위팔), `legs.png`(다리·무릎·장화 목). 발 = idle 원본, 머리 = 12_run_calm 북쪽
- 색: 검정 판갑 + 금 테두리(어깨뼈·어깨판), 금 벨트, 베이지 허리 천·정강이(왼쪽 위 빛), 장화 목 금색. 15색(idle과 합쳐서)
- idle 망토 밖으로 나오는 몸 픽셀 3개(검사)
- `body_nocape_64.png` 합친 몸, `body_under_cape_compare_x6.png` 남·동 참고와 비교 + 망토 젖힘 시험, `south_vs_north_body_x10.png`
- 다시 만들기: `cd tools && python north_body_under_cape.py` (종료 코드 0)
- 위치: idle 기준점에 맞춤(어깨판 = 메달 x22~24·y26, 정강이 = 발 위), idle 윤곽 밖 0px
