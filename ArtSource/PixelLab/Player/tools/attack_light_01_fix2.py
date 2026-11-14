# 동쪽 약베기 수정본 2: 생성 몸(팔 자세) + 원본 머리·하체 + 프레임별 칼 각도를 직접 그림 -> 512x64 + 검사. 종료 코드 0 = 측정 항목 통과
import glob, math, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SRC = os.path.join(ROOT, '01_raw/clean/attack_greatsword_light_01_fix2/east')
REF_DIR = os.path.join(ROOT, '01_raw/attack_light_01_ref')
PAL = os.path.join(ROOT, '02_final/palette.hex')
OUT = os.path.join(ROOT, '20_attack_greatsword_light_01_fix2')
OX, OY = 12, 2  # 생성 8번과 끝 그림이 가장 많이 겹치는 위치(캔버스 88x68)
GROUND = 54
GRAY = np.array([(0x5b, 0x56, 0x42), (0x6b, 0x73, 0x6b), (0xa5, 0xa8, 0xa7), (0xbb, 0xbe, 0xbe)])
HEAD = (23, 12, 39, 28)  # x0, y0, x1, y1(끝 제외): 원본 머리로 덮는 칸
LEGS = (22, 46, 37, 56)
CAPE = (0x40, 0x0b, 0x18)
BLADE = [(0x6b, 0x73, 0x6b), (0x6b, 0x73, 0x6b), (0xa5, 0xa8, 0xa7), (0xbb, 0xbe, 0xbe)]
GOLD = (0xd2, 0xa8, 0x55)
# 칼은 몸 앞 수평 타원 위를 돈다: 캐릭터 오른쪽(화면 앞) = 칼끝이 아래, 왼쪽(화면 안쪽) = 칼끝이 위이고 짧아 보임
# 프레임: (생성 몸 번호, 손 x, 손 y, 칼 각도(위 +), 칼 길이, 칼이 몸 앞인가)
PLAN = [
    None,                    # f1 = 원본 idle
    (2, 12, 36, -18, 28, True),   # f2 준비: 손을 오른쪽 허리 뒤로
    (1, 32, 37, -25, 29, True),   # f3 빠른 베기: 몸 앞 오른쪽
    (7, 33, 38, 4, 28, False),    # f4 빠른 베기: 정면 통과
    (8, 40, 33, 12, 21, False),   # f5 감속: 왼쪽으로
    (8, 40, 33, 17, 21, False),   # f6
    (8, 40, 33, 20, 21, False),   # f7 왼쪽에 정착
    (8, 40, 33, 20, 21, False),   # f8 두 번째 베기 준비 자세
]


def is_gray(a):
    d = np.abs(a[:, :, :3].astype(int)[:, :, None] - GRAY).sum(3).min(2)
    return (a[:, :, 3] > 0) & (d < 45)


def lock(a, ref, box):
    x0, y0, x1, y1 = box
    r = ref[y0:y1, x0:x1]
    a[y0:y1, x0:x1] = 0
    keep = (r[:, :, 3] > 0) & ~is_gray(r)
    a[y0:y1, x0:x1][keep] = r[keep]


def body(gen, ref):
    # 생성 칼을 지우고, 칼에 가렸던 자리는 원본 몸이나 망토 색으로 메운다
    a = gen.copy()
    m = is_gray(a)
    m[:28] = m[:28] & (np.arange(64)[None, :] >= 39)
    r_ok = (ref[:, :, 3] > 0) & ~is_gray(ref)
    a[m] = 0
    fill = m & r_ok
    a[fill] = ref[fill]
    cape = m & ~r_ok & (np.arange(64)[None, :] < 24)
    a[cape] = (*CAPE, 255)
    return a


def draw_blade(a, hx, hy, deg, length, front, guard):
    t = math.radians(deg)
    b = np.zeros_like(a)
    for i in range(length * 3):
        s = i / 3
        cx, cy = hx + s * math.cos(t), hy - s * math.sin(t)
        for j, c in enumerate(BLADE):
            o = (j - 1.5) * 0.95
            x, y = round(cx + o * math.sin(t)), round(cy + o * math.cos(t))
            if 0 <= x < 64 and 0 <= y < 64:
                b[y, x] = (*c, 255)
    if guard:
        for o in range(-4, 5):
            x, y = round(hx + o * math.sin(t)), round(hy + o * math.cos(t))
            b[y, x] = (*GOLD, 255)
    keep = a[:, :, 3] > 0
    bm = b[:, :, 3] > 0
    if front:
        out = a.copy()
        out[bm] = b[bm]
    else:
        out = b
        out[keep] = a[keep]
    return out


def main():
    pal = np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in open(PAL).read().split()])
    ref = np.array(Image.open(os.path.join(REF_DIR, '0_east_f0.png')).convert('RGBA'))
    gen = [np.array(Image.open(f).convert('RGBA'))[OY:OY + 64, OX:OX + 64] for f in sorted(glob.glob(SRC + '/*.png'))]
    frames = [ref.copy()]
    for g, hx, hy, deg, length, front in PLAN[1:]:
        a = body(gen[g - 1], ref)
        a[48:, :22] = 0  # 왼쪽으로 벌어진 생성 다리 조각 제거
        a[:, 44:] = 0  # 생성 칼 잔여 점 제거(몸은 x44 안쪽)
        a[12:28, :23] = 0  # 생성 머리카락이 원본 머리 밖으로 삐져나온 조각 제거
        lock(a, ref, HEAD)
        lock(a, ref, LEGS)
        a = draw_blade(a, hx, hy, deg, length, front, guard=g != 8)
        m = a[:, :, 3] > 0
        rgb = a[m][:, :3].astype(int)
        a[m, :3] = pal[((rgb[:, None] - pal[None]) ** 2).sum(2).argmin(1)]
        a[:, :, 3] = np.where(m, 255, 0)
        frames.append(a)
    os.makedirs(os.path.join(OUT, 'preview'), exist_ok=True)
    Image.fromarray(np.concatenate(frames, axis=1)).save(os.path.join(OUT, '0_east.png'))

    s = Image.open(os.path.join(OUT, '0_east.png'))
    S = np.array(s.convert('RGBA'))
    out = [f'파일 {s.format} {s.size} 프레임 {len(frames)}']
    fail = int(s.size != (512, 64) or s.format != 'PNG')
    alpha = set(np.unique(S[:, :, 3]).tolist())
    cols = {tuple(v) for v in S[S[:, :, 3] > 0][:, :3].tolist()}
    out += [f'알파 {sorted(alpha)}', f'색 수 {len(cols)}']
    fail += (not alpha <= {0, 255}) + (len(cols) > 16)
    d0 = int((S[:, :64] != ref).any(axis=2).sum())
    out.append(f'f1과 원본 idle 차이 {d0}px')
    fail += d0 > 0
    for i in range(8):
        a = S[:, i * 64:(i + 1) * 64]
        p = np.argwhere(a[:, :, 3] > 0)
        y0, y1, x0, x1 = p[:, 0].min(), p[:, 0].max(), p[:, 1].min(), p[:, 1].max()
        margin = min(x0, y0, 63 - x1, 63 - y1)
        foot = np.argwhere(a[52:55, 22:37, 3] > 0)[:, 1] + 22
        out.append(f'f{i + 1} x{x0}-{x1} y{y0}-{y1} 여백{margin} 바닥{y1} 발x{foot.min()}-{foot.max()}')
        fail += margin < 2 or y1 != GROUND
    open(os.path.join(OUT, 'check_20261107.txt'), 'w', encoding='utf-8').write('\n'.join(out) + f'\n실패 {fail}\n')

    cells = []
    for a in frames:
        c = Image.new('RGBA', (64, 64), (60, 60, 70, 255))
        c.alpha_composite(Image.fromarray(a))
        cells.append(c.resize((256, 256), Image.NEAREST))
    g = [c.convert('P', palette=Image.ADAPTIVE) for c in cells]
    g[0].save(os.path.join(OUT, 'preview/0_east_16fps_x4.gif'), save_all=True, append_images=g[1:], duration=62, loop=0, disposal=1, optimize=False)
    b = Image.new('RGBA', (256 * 8, 256))
    for i, c in enumerate(cells):
        d = ImageDraw.Draw(c)
        d.rectangle((0, 0, 255, 255), outline=(255, 60, 60, 255))
        d.rectangle((8, 8, 247, 247), outline=(140, 140, 140, 255))
        d.line((0, GROUND * 4 + 4, 255, GROUND * 4 + 4), fill=(60, 120, 255, 255))
        b.paste(c, (i * 256, 0))
    b.save(os.path.join(OUT, 'preview/0_east_guides_x4.png'))
    print('\n'.join(out), f'\n실패 {fail}')
    return 1 if fail else 0


if __name__ == '__main__':
    sys.exit(main())
