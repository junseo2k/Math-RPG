# 동쪽 약베기 수정본: 생성 프레임 순서 재배치 + 머리·하체 원본 고정 + 수직 칼 1장 합성 -> 512x64 + 검사. 종료 코드 0 = 측정 항목 통과
import glob, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SRC = os.path.join(ROOT, '01_raw/clean/attack_greatsword_light_01_fix/east')
REF_DIR = os.path.join(ROOT, '01_raw/attack_light_01_ref')
PAL = os.path.join(ROOT, '02_final/palette.hex')
OUT = os.path.join(ROOT, '19_attack_greatsword_light_01_fix')
OX, OY = 12, 2  # 생성 8번과 끝 그림이 가장 많이 겹치는 위치(캔버스 88x68)
GROUND = 54
GRAY = [(0x5b, 0x56, 0x42), (0x6b, 0x73, 0x6b), (0xa5, 0xa8, 0xa7), (0xbb, 0xbe, 0xbe)]
HEAD = (23, 12, 39, 28)  # x0, y0, x1, y1 (끝 제외): 원본 머리로 덮는 칸
LEGS = (22, 46, 37, 56)
# 수정본 프레임 = 생성 프레임 번호. 낮은 준비 -> 앞 -> 위 -> 왼쪽 어깨 뒤로 도는 순서가 되게 재배치. 'up'은 합성
ORDER = [3, 5, 1, 'up', 6, 8, 8, 8]


def is_gray(a):
    d = np.abs(a[:, :, :3].astype(int)[:, :, None] - np.array(GRAY)).sum(3).min(2)
    return (a[:, :, 3] > 0) & (d < 40)


def lock(a, ref, box):
    x0, y0, x1, y1 = box
    r = ref[y0:y1, x0:x1]
    t = a[y0:y1, x0:x1]
    t[~is_gray(t)] = 0  # 생성 몸은 지우고 칼은 남긴다
    keep = (r[:, :, 3] > 0) & ~is_gray(r)
    t[keep] = r[keep]


def up_frame(body):
    # 칼을 수직으로 세운 장면: 생성 1번 몸에서 칼을 빼고 머리 앞쪽에 세운다
    a = body.copy()
    m = is_gray(a)
    m[37:] = False
    a[m] = 0
    blade = np.zeros_like(a)
    for y in range(4, 36):
        lean = (36 - y) // 6  # 살짝 기울여 앞뒤 프레임과 이어지게
        for x, c in zip(range(35 + lean, 39 + lean), [(0x6b, 0x73, 0x6b), (0x6b, 0x73, 0x6b), (0xa5, 0xa8, 0xa7), (0xbb, 0xbe, 0xbe)]):
            blade[y, x] = (*c, 255)
    for x in range(32, 42):
        blade[36, x] = blade[37, x] = (0xd2, 0xa8, 0x55, 255)
    keep = a[:, :, 3] > 0
    blade[keep] = a[keep]
    return blade


def main():
    pal = np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in open(PAL).read().split()])
    ref = np.array(Image.open(os.path.join(REF_DIR, '0_east_f0.png')).convert('RGBA'))
    gen = [np.array(Image.open(f).convert('RGBA'))[OY:OY + 64, OX:OX + 64] for f in sorted(glob.glob(SRC + '/*.png'))]
    frames = []
    for k in ORDER:
        a = up_frame(gen[0]) if k == 'up' else gen[k - 1].copy()
        lock(a, ref, HEAD)
        lock(a, ref, LEGS)
        a[48:, :22][~is_gray(a[48:, :22])] = 0  # 왼쪽으로 벌어진 생성 다리 조각 제거
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
    for i in range(8):
        a = S[:, i * 64:(i + 1) * 64]
        p = np.argwhere(a[:, :, 3] > 0)
        y0, y1, x0, x1 = p[:, 0].min(), p[:, 0].max(), p[:, 1].min(), p[:, 1].max()
        margin = min(x0, y0, 63 - x1, 63 - y1)
        x, y, xe, ye = HEAD
        head = int((((a[y:ye, x:xe] != ref[y:ye, x:xe]).any(2)) & ~is_gray(a[y:ye, x:xe]) & ~is_gray(ref[y:ye, x:xe])).sum())
        foot = np.argwhere(a[52:55, :, 3] > 0)[:, 1]
        out.append(f'f{i + 1} 생성{ORDER[i]} x{x0}-{x1} y{y0}-{y1} 여백{margin} 바닥{y1} 발x{foot.min()}-{foot.max()} 머리차이{head}px')
        fail += margin < 2 or y1 != GROUND or head > 0
    open(os.path.join(OUT, 'check_20261106.txt'), 'w', encoding='utf-8').write('\n'.join(out) + f'\n실패 {fail}\n')

    cells = []
    for a in [ref] + frames:
        c = Image.new('RGBA', (64, 64), (60, 60, 70, 255))
        c.alpha_composite(Image.fromarray(a))
        cells.append(c.resize((256, 256), Image.NEAREST))
    g = [c.convert('P', palette=Image.ADAPTIVE) for c in cells[1:]]
    g[0].save(os.path.join(OUT, 'preview/0_east_16fps_x4.gif'), save_all=True, append_images=g[1:], duration=62, loop=0)
    b = Image.new('RGBA', (256 * 9, 256))
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
