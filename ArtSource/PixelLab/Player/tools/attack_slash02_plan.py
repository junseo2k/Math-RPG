"""31번 올려베기 설계 그림: 핵심 포즈의 칼끝·손(칼 뿌리) 경로를 별도 그림에 표시한다. 납품 그림에는 선을 넣지 않는다.
  python attack_slash02_plan.py -> 31_.../preview/plan_path_x4.png, 좌표 출력
회색 칼날 픽셀의 양 끝 두 점 중 몸통 중심에서 먼 점 = 칼끝, 가까운 점 = 칼 뿌리(손 근처 추정값).
"""
import os
from PIL import Image, ImageDraw
from attack_slash_east import is_blade

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
POSES = ['f1_from_slash01_f8.png', 'key_a_prep.png', 'key_b_strike.png', 'key_c_finish.png']
S, Z = 96, 4
TORSO = (46, 70)  # 연결 프레임 가슴 근처(발 x 37~58, 머리 윗줄 y56)


def ends(im):
    p = im.load()
    bl = [(x, y) for y in range(S) for x in range(S) if is_blade(p[x, y])]
    cx, cy = sum(x for x, _ in bl) / len(bl), sum(y for _, y in bl) / len(bl)
    a = max(bl, key=lambda q: (q[0] - cx) ** 2 + (q[1] - cy) ** 2)
    b = max(bl, key=lambda q: (q[0] - a[0]) ** 2 + (q[1] - a[1]) ** 2)
    # 두 끝 중 몸통 중심(가슴 근처)에서 먼 쪽이 칼끝
    far = lambda q: (q[0] - TORSO[0]) ** 2 + (q[1] - TORSO[1]) ** 2
    return (b, a) if far(a) > far(b) else (a, b)


def main():
    bg = (58, 62, 72, 255)
    panels = Image.new('RGBA', (S * len(POSES), S), bg)
    pts = []
    for k, n in enumerate(POSES):
        im = Image.open(os.path.join(D, n)).convert('RGBA')
        panels.alpha_composite(im, (S * k, 0))
        pts.append(ends(im))
    big = panels.resize((panels.width * Z, panels.height * Z), Image.NEAREST)
    over = Image.new('RGBA', (S * Z, S * Z), bg)
    for k, n in enumerate(POSES):
        ghost = Image.open(os.path.join(D, n)).convert('RGBA').resize((S * Z, S * Z), Image.NEAREST)
        a = ghost.getchannel('A').point(lambda v: 60 if v else 0)
        ghost.putalpha(a)
        over.alpha_composite(ghost)
    dr = ImageDraw.Draw(over)
    sc = lambda q: (q[0] * Z + Z // 2, q[1] * Z + Z // 2)
    dr.line([sc(t) for _, t in pts], fill=(230, 80, 80, 255), width=3)    # 칼끝 경로(빨강)
    dr.line([sc(r) for r, _ in pts], fill=(80, 150, 240, 255), width=3)   # 칼 뿌리 경로(파랑)
    for r, t in pts:
        dr.line([sc(r), sc(t)], fill=(200, 200, 200, 255), width=1)       # 각 포즈의 칼(회색)
        for q, c in ((t, (230, 80, 80, 255)), (r, (80, 150, 240, 255))):
            x, y = sc(q)
            dr.ellipse((x - 5, y - 5, x + 5, y + 5), outline=c, width=2)
    # 범례 칸: 빨강 = 칼끝, 파랑 = 칼 뿌리(손 근처), 회색 = 각 포즈의 칼, 흐린 그림 = 연결·준비·타격·마무리 겹침
    leg = Image.new('RGBA', (S * Z, 40), bg)
    ld = ImageDraw.Draw(leg)
    for i, (c, t) in enumerate((((230, 80, 80, 255), 'blade tip'), ((80, 150, 240, 255), 'blade root (near hands)'), ((200, 200, 200, 255), 'blade per pose'))):
        ld.rectangle((8 + 130 * i, 14, 22 + 130 * i, 26), fill=c)
        ld.text((28 + 130 * i, 13), t, fill=(255, 255, 255, 255))
    out = Image.new('RGBA', (big.width + over.width, max(big.height, over.height + leg.height)), bg)
    out.alpha_composite(big, (0, 0))
    out.alpha_composite(over, (big.width, 0))
    out.alpha_composite(leg, (big.width, over.height))
    out.save(os.path.join(D, 'preview', 'plan_path_x4.png'))
    for n, (r, t) in zip(['연결 f1', '준비', '타격', '마무리'], pts):
        print(f'{n}\t칼 뿌리 {r}\t칼끝 {t}')


if __name__ == '__main__':
    main()
