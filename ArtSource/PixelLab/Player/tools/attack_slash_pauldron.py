"""29번 공격 시트의 어깨 원반을 4_west idle의 5×5 태양 메달로 바꾼다(2026-11-07 사용자 결정). 생성 호출 없음.
원본 0_east.png는 읽기만 하고, 수정본은 0_east_pauldron_w.png로 따로 쓴다.
  python attack_slash_pauldron.py   -> 종료 코드 0 = 검사 통과
"""
import os
import sys
from PIL import Image
from pauldron import SUN, SUN_B

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '29_attack_greatsword_light_slash_east')
SRC, OUT = os.path.join(D, '0_east.png'), os.path.join(D, '0_east_pauldron_w.png')
W_IDLE = os.path.join(ROOT, '16_idle_greatsword_pauldron_breath', '4_west.png')
S = 96
GOLD = {(0xfc, 0xdb, 0x6d), (0xfd, 0xe6, 0xbd), (0xd2, 0xa8, 0x55), (0xc3, 0x96, 0x50), (0xae, 0x78, 0x3a), (0x91, 0x51, 0x1b), (0xd9, 0xc6, 0x9f)}
# 프레임 번호(1부터) -> 원반이 들어 있는 상자 (x0, y0, x1, y1, 포함)와 메달 중심. 픽셀 지도에서 읽음. f1~f3은 장식 없음(E와 같음).
DISC = {4: ((51, 66, 58, 74), (55, 70)),
        5: ((46, 64, 54, 73), (50, 69)),
        6: ((44, 65, 52, 73), (48, 69)),
        7: ((44, 65, 52, 73), (48, 69)),
        8: ((44, 65, 52, 73), (48, 69))}
MEDAL = [r.split() for r in SUN_B.splitlines()]


def disc_mask(p, box, c):
    """상자 안에서 중심과 이어진 금색 픽셀(=생성된 원반)."""
    x0, y0, x1, y1 = box
    seen, todo = set(), [c]
    while todo:
        x, y = todo.pop()
        if (x, y) in seen or not (x0 <= x <= x1 and y0 <= y <= y1) or not p[x, y][3] or p[x, y][:3] not in GOLD:
            continue
        seen.add((x, y))
        todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    return seen


def fill(p, mask):
    """원반 자리를 바깥 이웃 중 가장 많은 색으로 안쪽까지 채운다(어깨·소매·망토색)."""
    left = set(mask)
    while left:
        done = {}
        for x, y in left:
            nb = [p[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                  if (x + dx, y + dy) not in left and p[x + dx, y + dy][3]]
            if nb:
                done[(x, y)] = max(set(nb), key=nb.count)
        if not done:  # 바깥과 이어지지 않으면 비운다
            for q in left:
                p[q] = (0, 0, 0, 0)
            break
        for q, col in done.items():
            p[q] = col
        left -= set(done)


def stamp(p, cx, cy):
    for j, row in enumerate(MEDAL):
        for i, ch in enumerate(row):
            if ch != '.':
                p[cx - 2 + i, cy - 2 + j] = SUN[ch] + (255,)


def medal_score(p, cx, cy):
    return sum(p[cx - 2 + i, cy - 2 + j][:3] == SUN[ch] for j, row in enumerate(MEDAL) for i, ch in enumerate(row) if ch != '.')


def main():
    src = Image.open(SRC).convert('RGBA')
    out = src.copy()
    lines, fails, allowed = [], 0, set()
    for k, (box, c) in DISC.items():
        f = out.crop((S * (k - 1), 0, S * k, S))
        p = f.load()
        m = disc_mask(p, box, c)
        fill(p, m)
        stamp(p, *c)
        out.paste(f, (S * (k - 1), 0))
        area = m | {(c[0] - 2 + i, c[1] - 2 + j) for j in range(5) for i in range(5)}
        allowed |= {(x + S * (k - 1), y) for x, y in area}
        lines.append(f'f{k}: 원반 {len(m)}px 제거·채움, 메달 중심 {c}')
    out.save(OUT)

    def rec(name, ok, val):
        nonlocal fails
        fails += not ok
        lines.append(f"{'통과' if ok else '실패'}\t{name}\t{val}")

    a, b = src.load(), Image.open(OUT).convert('RGBA').load()
    changed = {(x, y) for y in range(S) for x in range(S * 8) if a[x, y] != b[x, y]}
    rec('바뀐 픽셀이 원반·메달 자리 안에만', changed <= allowed, f'바뀜 {len(changed)}px, 밖 {len(changed - allowed)}px')
    rec('f1~f3 그대로', not any(x < S * 3 for x, y in changed), '픽셀 비교')
    re = Image.open(OUT)
    rec('시트 크기·형식', re.size == (768, 96) and re.format == 'PNG', f'{re.format} {re.size}')
    px = list(re.convert('RGBA').get_flattened_data())
    cols = {q[:3] for q in px if q[3]}
    rec('RGB 색 수 ≤16', len(cols) <= 16, len(cols))
    rec('알파 0·255만', {q[3] for q in px} <= {0, 255}, sorted({q[3] for q in px}))
    w = Image.open(W_IDLE).convert('RGBA').load()
    rec('기준 W 메달 일치', medal_score(w, 37, 29) == 21, f'4_west f0 (37,29) {medal_score(w, 37, 29)}/21')
    for k, (_, c) in DISC.items():
        f = Image.open(OUT).convert('RGBA').crop((S * (k - 1), 0, S * k, S)).load()
        rec(f'f{k} 메달 무늬·색', medal_score(f, *c) == 21, f'{medal_score(f, *c)}/21')
        g = [(x, y) for y in range(c[1] - 6, c[1] + 7) for x in range(c[0] - 6, c[0] + 7) if f[x, y][3] and f[x, y][:3] in {(0xfc, 0xdb, 0x6d), (0xfd, 0xe6, 0xbd)}]
        far = [q for q in g if max(abs(q[0] - c[0]), abs(q[1] - c[1])) > 2 and q[1] >= c[1] - 4]
        rec(f'f{k} 메달 밖 밝은 금색 남음(원반 잔여, 눈 확인용)', True, f'{len(far)}px {far[:6]}')
    txt = '\n'.join(lines) + f'\n실패 {fails}건\nexit {1 if fails else 0}\n'
    open(os.path.join(D, 'check_pauldron_20261107.txt'), 'w', encoding='utf-8').write(txt)
    print(txt)

    bg = (58, 62, 72, 255)
    new = Image.open(OUT).convert('RGBA')
    cmp = Image.new('RGBA', (5 * 40, 2 * 32), bg)
    for i, (k, (_, c)) in enumerate(DISC.items()):
        for r, sh in enumerate((src, new)):
            cmp.alpha_composite(sh.crop((S * (k - 1) + c[0] - 20, c[1] - 18, S * (k - 1) + c[0] + 20, c[1] + 14)), (40 * i, 32 * r))
    cmp.resize((cmp.width * 6, cmp.height * 6), Image.NEAREST).save(os.path.join(D, 'preview', 'pauldron_before_after_x6.png'))
    frames = []
    for k in range(8):
        fr = Image.new('RGBA', (S, S), bg)
        fr.alpha_composite(new.crop((S * k, 0, S * k + S, S)))
        frames.append(fr.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    frames[0].save(os.path.join(D, 'preview', 'slash_pauldron_w_16fps_x4.gif'), save_all=True, append_images=frames[1:],
                   duration=[60, 70] * 3 + [60, 600], loop=0, disposal=1)
    return fails


if __name__ == '__main__':
    sys.exit(1 if main() else 0)
