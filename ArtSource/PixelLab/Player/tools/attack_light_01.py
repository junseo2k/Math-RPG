# attack_greatsword_light_01 East: 생성 8프레임 -> 64x64·16색·512x64 시트 + 납품 검사. 종료 코드 0 = 측정 항목 통과
import glob, os, sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SRC = os.path.join(ROOT, '01_raw/clean/attack_greatsword_light_01/east')
REF = os.path.join(ROOT, '01_raw/attack_light_01_ref/0_east_f0.png')
PAL = os.path.join(ROOT, '02_final/palette.hex')
OUT = os.path.join(ROOT, '18_attack_greatsword_light_01')
GROUND = 54  # 게임 idle 바닥선. 명세 y60과 다름(2026-11-03 사용자 결정)
OX, OY = 6, 2  # 첫 프레임과 기준 그림이 가장 많이 겹치는 위치(생성 캔버스 76x68)



def main():
    pal = np.array([[int(h[i:i + 2], 16) for i in (0, 2, 4)] for h in open(PAL).read().split()])
    ref = np.array(Image.open(REF).convert('RGBA'))
    frames, lines, fail = [], [], 0
    for f in sorted(glob.glob(SRC + '/*.png')):
        a = np.array(Image.open(f).convert('RGBA'))
        c = np.zeros((64, 64, 4), np.uint8)
        c[:] = a[OY:OY + 64, OX:OX + 64]
        cut = int((a[:, :, 3] > 0).sum() - (c[:, :, 3] > 0).sum())
        dy = GROUND - int(np.argwhere(c[:, :, 3] > 0)[:, 0].max())  # 뜬 프레임은 바닥으로 정수 이동
        c = np.roll(c, dy, axis=0)
        m = c[:, :, 3] > 0
        rgb = c[m][:, :3].astype(int)
        c[m, :3] = pal[((rgb[:, None] - pal[None]) ** 2).sum(2).argmin(1)]
        c[:, :, 3] = np.where(m, 255, 0)
        frames.append(c)
        lines.append((cut, dy))
    os.makedirs(os.path.join(OUT, 'preview'), exist_ok=True)
    sheet = np.concatenate(frames, axis=1)
    Image.fromarray(sheet).save(os.path.join(OUT, '0_east.png'))

    # 납품 검사는 저장한 파일을 다시 열어서 한다
    s = Image.open(os.path.join(OUT, '0_east.png'))
    S = np.array(s.convert('RGBA'))
    out = [f'파일 {s.format} {s.size} 프레임 {len(frames)}']
    fail += s.size != (512, 64) or s.format != 'PNG' or len(frames) != 8
    alpha = set(np.unique(S[:, :, 3]).tolist())
    cols = {tuple(v) for v in S[S[:, :, 3] > 0][:, :3].tolist()}
    out += [f'알파 {sorted(alpha)}', f'색 수 {len(cols)}']
    fail += not alpha <= {0, 255}
    fail += len(cols) > 16
    for i, (cut, dy) in enumerate(lines):
        a = S[:, i * 64:(i + 1) * 64]
        p = np.argwhere(a[:, :, 3] > 0)
        y0, y1, x0, x1 = p[:, 0].min(), p[:, 0].max(), p[:, 1].min(), p[:, 1].max()
        margin = min(x0, y0, 63 - x1, 63 - y1)
        foot = np.argwhere(a[GROUND - 2:GROUND + 1, :, 3] > 0)[:, 1]
        out.append(f'f{i + 1} x{x0}-{x1} y{y0}-{y1} 크기{x1 - x0 + 1}x{y1 - y0 + 1} 여백{margin} 바닥{y1} 발x{foot.min()}-{foot.max()} 보정dy{dy} 잘림{cut}px')
        fail += margin < 2 or cut > 0 or y1 != GROUND
    d0 = int((S[:, :64] != ref).any(axis=2).sum())
    out.append(f'f1과 기준 그림 차이 {d0}px')
    open(os.path.join(OUT, 'check_20261106.txt'), 'w', encoding='utf-8').write('\n'.join(out) + f'\n실패 {fail}\n')

    # 미리보기: 16fps(62ms), 4배. 맨 앞은 기준 그림
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
        d.line((128, 0, 128, 255), fill=(60, 120, 255, 255))
        d.line((0, GROUND * 4 + 4, 255, GROUND * 4 + 4), fill=(60, 120, 255, 255))
        b.paste(c, (i * 256, 0))
    b.save(os.path.join(OUT, 'preview/0_east_guides_x4.png'))
    print('\n'.join(out), f'\n실패 {fail}')
    return 1 if fail else 0


if __name__ == '__main__':
    sys.exit(main())
