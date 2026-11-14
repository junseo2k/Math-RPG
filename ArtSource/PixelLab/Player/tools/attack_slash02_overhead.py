"""31번 올려베기 마지막 자세를 3타 내려베기 시작 자세(머리 위)로 고친다(2026-11-07 사용자 명세).
성공본 key_c_finish.png·0_east.png는 읽기만 한다.
  python attack_slash02_overhead.py guide    -> guide_finish_overhead.png (생성 없음, 밑그림)
  python attack_slash02_overhead.py compose  -> key_c_finish_overhead.png (생성본의 어깨 위만 원본에 합성) + 전후 비교
  뒤에 v2를 붙이면 손을 더 앞으로·두 손 겹쳐 쥐기 판(_v2 파일)
칼은 각도·길이를 그대로 두고 손을 따라 평행 이동만 한다(칼은 손에 붙어 있으므로 손이 움직인 만큼만).
"""
import os
import sys
from PIL import Image
from attack_slash_east import snap, is_blade, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
PAL = {k: tuple(int(l.strip().lstrip('#')[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
       for k, l in zip('0123456789abcdef', [l for l in open(os.path.join(ROOT, '02_final', 'palette.hex')) if l.strip()])}
S = 96
BLADE_ROWS = 46            # 원본에서 y≤46은 칼날·코등이만 있음(픽셀 지도로 확인)
OLD_HANDS = (39, 49)       # 원본 손 중심(머리 왼쪽 위)
NEW_HANDS = (47, 44)       # 정수리(약 x45, y52) 약간 앞(오른쪽)·위
SHIFT = (NEW_HANDS[0] - OLD_HANDS[0], NEW_HANDS[1] - OLD_HANDS[1])
FAR = [(39, 62), (34, 53), (45, 46)]    # 먼 팔: 어깨 → 팔꿈치(머리 왼쪽 옆) → 손
NEAR = [(52, 61), (57, 53), (49, 46)]   # 가까운 팔: 어깨 → 팔꿈치(머리 오른쪽 옆) → 손
EDIT_Y = 63                # 합성 범위: y<63(어깨 위). 그 아래 몸통·다리는 원본 그대로
TAG = ''                   # 결과 파일 꼬리표
GRIP = None                # v2: 두 손을 손잡이 위아래로 겹쳐 쥠 [(아래 손), (위 손)]
if sys.argv[2:] == ['v2']:
    # 2026-11-07 사용자: 손을 더 앞(오른쪽)으로, 두 앞팔이 한 손잡이로 모이고 두 손이 위아래로 겹쳐 쥠
    NEW_HANDS = (52, 45)
    SHIFT = (NEW_HANDS[0] - OLD_HANDS[0], NEW_HANDS[1] - OLD_HANDS[1])
    GRIP = [(51, 46), (54, 44)]
    FAR = [(39, 62), (35, 53), GRIP[0]]
    NEAR = [(52, 61), (58, 53), GRIP[1]]
    TAG = '_v2'


def line(a, b):
    n = max(abs(b[0] - a[0]), abs(b[1] - a[1]), 1)
    return [(round(a[0] + (b[0] - a[0]) * t / n), round(a[1] + (b[1] - a[1]) * t / n)) for t in range(n + 1)]


def guide():
    src = Image.open(os.path.join(D, 'key_c_finish.png')).convert('RGBA')
    sp = src.load()
    g = src.copy()
    p = g.load()
    for y in range(S):          # 원본 칼·손 지우기: 칼 줄 전체, 손(y47~51), 머리 왼쪽 먼 팔(x≤40, y52~61)
        for x in range(S):
            if y <= 51 or (x <= 40 and 52 <= y <= 61):
                p[x, y] = (0, 0, 0, 0)
    for a, b, c in ((FAR[0], FAR[1], '2'), (FAR[1], FAR[2], '2'), (NEAR[0], NEAR[1], '0'), (NEAR[1], NEAR[2], '0')):
        for x, y in line(a, b):
            for dx in (-1, 0, 1):
                p[x + dx, y] = PAL[c]
            p[x + 2, y] = PAL['b']
    if GRIP is None:
        for y in range(NEW_HANDS[1] - 2, NEW_HANDS[1] + 3):     # 두 손이 같은 손잡이를 쥠(건틀릿 6×5)
            for x in range(NEW_HANDS[0] - 3, NEW_HANDS[0] + 3):
                p[x, y] = PAL['9'] if y == NEW_HANDS[1] else PAL['2']
    for y in range(BLADE_ROWS + 1):                         # 칼(코등이 포함)을 손과 같이 평행 이동
        for x in range(S):
            if sp[x, y][3]:
                nx, ny = x + SHIFT[0], y + SHIFT[1]
                if 0 <= nx < S and 0 <= ny < S:
                    p[nx, ny] = sp[x, y]
    # 원본 손잡이·코등이는 손 줄(y47~51)에 겹쳐 있어 같이 지워짐 → 손에서 칼 뿌리까지 손잡이, 칼 뿌리에 칼날과 수직인 코등이
    base = (OLD_HANDS[0] + 6 + SHIFT[0], OLD_HANDS[1] - 3 + SHIFT[1])     # 원본 칼 뿌리(45,46)를 옮긴 자리
    for x, y in line(NEW_HANDS, base):
        p[x, y] = PAL['2']
        p[x, y + 1] = PAL['0']
    for t in range(-3, 4):                                               # 칼날 방향 약 (4,-3)에 수직인 (3,4)
        p[base[0] + round(t * 0.6), base[1] + round(t * 0.8)] = PAL['9' if abs(t) == 3 else 'b']
    if GRIP:                                                             # 손잡이 위에 두 손(3×3 건틀릿, 금색 손마디)
        for hx, hy in GRIP:
            for y in range(hy - 1, hy + 2):
                for x in range(hx - 1, hx + 2):
                    p[x, y] = PAL['9'] if y == hy - 1 else PAL['2']
    g.save(os.path.join(D, f'guide_finish_overhead{TAG}.png'))
    b = g.getbbox()
    print('칼 이동', SHIFT, '밑그림 bbox', b, '여백 좌상우하', (b[0], b[1], S - b[2], S - b[3]))


def compose():
    src = Image.open(os.path.join(D, 'key_c_finish.png')).convert('RGBA')
    gen = snap(Image.open(os.path.join(D, 'keys_raw', f'c_finish_overhead{TAG}.png')).convert('RGBA'))
    out = src.copy()
    o, q = out.load(), gen.load()
    for y in range(EDIT_Y):
        for x in range(S):
            o[x, y] = q[x, y]
    out.save(os.path.join(D, f'key_c_finish_overhead{TAG}.png'))
    a = src.load()
    below = sum(a[x, y] != o[x, y] for y in range(EDIT_Y, S) for x in range(S))
    d = px(out)
    b = out.getbbox()
    print('어깨 아래(y≥63) 바뀐 픽셀', below, '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}),
          '| bbox', b, '여백', (b[0], b[1], S - b[2], S - b[3]))
    # 칼 크기 비교(회색 픽셀 수·열 두께)
    for n, im in (('이전', src), ('수정', out)):
        pp = im.load()
        cols = {}
        for y in range(S):
            for x in range(S):
                if is_blade(pp[x, y]):
                    cols.setdefault(x, []).append(y)
        th = sorted(len(v) for v in cols.values())
        print(f'{n} 칼 회색 {sum(th)}px, 열 두께 중앙값 {th[len(th) // 2]}')
    bg = (58, 62, 72, 255)
    names = ['key_c_finish.png'] + (['key_c_finish_overhead.png'] if TAG else []) + [f'guide_finish_overhead{TAG}.png', f'key_c_finish_overhead{TAG}.png']
    W = Image.new('RGBA', (60 * len(names), 92), bg)
    for i, n in enumerate(names):
        W.alpha_composite(Image.open(os.path.join(D, n)).convert('RGBA').crop((18, 2, 78, 94)), (60 * i, 0))
    W.resize((W.width * 5, W.height * 5), Image.NEAREST).save(os.path.join(D, 'preview', f'finish_overhead{TAG}_before_after_x5.png'))


if __name__ == '__main__':
    {'guide': guide, 'compose': compose}[sys.argv[1]]()
