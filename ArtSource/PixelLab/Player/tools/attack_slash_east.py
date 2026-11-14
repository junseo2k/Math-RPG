"""동쪽 대검 약베기(29번) 납품 조립·검사. 생성 호출 없음. 확대·축소 없음.
python attack_slash_east.py  -> 0_east.png, preview/*, check_20261107.txt 를 만들고 실패가 있으면 종료 코드 1
"""
import os
import sys
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '29_attack_greatsword_light_slash_east')
S, N, FPS = 96, 8, 16
AX, AY = 48, 93  # 바닥선 y=94는 칸 경계: 그 위 마지막 픽셀 줄이 y=93이고 아래 여백이 정확히 2px
# 생성본 9장(0=입력 idle) 중 raw 3은 raw 2와 거의 같은 준비 자세라 뺀다.
ORDER = [0, 1, 2, 4, 5, 6, 7, 8]
PAL = [tuple(int(l.strip().lstrip('#')[i:i + 2], 16) for i in (0, 2, 4))
       for l in open(os.path.join(ROOT, '02_final', 'palette.hex')) if l.strip()]


def px(im):
    return list(im.get_flattened_data())


def snap(im):
    """팔레트의 가장 가까운 색으로 옮긴다(디더링 없음, 좌표 그대로)."""
    out = im.copy()
    out.putdata([(0, 0, 0, 0) if p[3] == 0 else
                 min(PAL, key=lambda c: sum((a - b) ** 2 for a, b in zip(c, p[:3]))) + (255,)
                 for p in px(im)])
    return out


def is_blade(p):
    """회색·흰색 계열(칼날·잔상)."""
    r, g, b = p[:3]
    return p[3] and max(r, g, b) - min(r, g, b) < 24 and max(r, g, b) > 70


def is_hair(p):
    r, g, b = p[:3]
    return p[3] and r > 180 and g > 130 and b < 140


def build():
    frames = [snap(Image.open(os.path.join(D, 'raw', f'frame_{i:02d}.png')).convert('RGBA')) for i in ORDER]
    sheet = Image.new('RGBA', (S * N, S), (0, 0, 0, 0))
    for k, f in enumerate(frames):
        sheet.paste(f, (S * k, -1))  # 생성본 전체를 정수 1px 올림(start_96은 y=94까지 그려져 여백 1px)
    sheet.save(os.path.join(D, '0_east.png'))


def check():
    path = os.path.join(D, '0_east.png')
    rows, fails = [], 0

    def rec(name, std, val, how, ok):
        nonlocal fails
        fails += ok == '실패'
        rows.append(f'{ok}\t{name}\t기준 {std}\t실측 {val}\t{how}')

    raw = open(path, 'rb').read(8)
    im = Image.open(path)
    rec('형식', 'PNG', im.format, '파일 열기', '통과' if raw[:4] == b'\x89PNG' else '실패')
    sheet = im.convert('RGBA')
    rec('시트 크기', '768×96', f'{sheet.width}×{sheet.height}', '파일 열기', '통과' if sheet.size == (768, 96) else '실패')
    d = px(sheet)
    alphas = sorted({p[3] for p in d})
    rec('알파', '0·255만', alphas, '전 픽셀', '통과' if alphas == [0, 255] else '실패')
    cols = {p[:3] for p in d if p[3]}
    rec('RGB 색 수', '16 이하', len(cols), '알파 0 제외 합산', '통과' if len(cols) <= 16 else '실패')

    frames = [sheet.crop((S * k, 0, S * k + S, S)) for k in range(N)]
    empty = [k + 1 for k, f in enumerate(frames) if not f.getbbox()]
    rec('프레임 수·간격·패딩', '8·0·0', f'{N}칸 96px 연속, 빈 칸 {empty}', '칸 계산', '통과' if not empty else '실패')
    base = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    base.paste(Image.open(os.path.join(D, 'start_96.png')).convert('RGBA'), (0, -1))
    diff = sum(a != b for a, b in zip(px(frames[0]), px(base)))
    rec('1프레임 = 원본 idle', '차이 0', f'{diff}px', 'start_96을 1px 올려 픽셀 비교', '통과' if not diff else '실패')

    tops, footxs = [], []
    for k, f in enumerate(frames):
        b = f.getbbox()
        margins = (b[0], b[1], S - b[2], S - b[3])
        w, h = b[2] - b[0], b[3] - b[1]
        ok = min(margins) >= 2 and w <= 92 and h <= 92
        rec(f'f{k + 1} 대상 영역·여백', '≤92×92, 각 ≥2', f'{w}×{h}, 좌상우하 {margins}', 'bbox', '통과' if ok else '실패')
        p = f.load()
        low = [x for x in range(S) if p[x, b[3] - 1][3]]
        cx = (min(low) + max(low)) / 2
        body_low = [x for x in range(S) if p[x, AY][3] and not is_blade(p[x, AY])]
        rec(f'f{k + 1} 맨 아랫줄', f'맨 아랫줄 y={AY}(바닥선 94 바로 위), 중앙 x={AX}, 오차 0',
            f'y={b[3] - 1}, x {min(low)}~{max(low)} 중앙 {cx:g}, 몸색 x {body_low[:1]}~{body_low[-1:]}', '최하단 줄',
            '통과' if b[3] - 1 == AY and cx == AX else '실패')
        hair = [y for y in range(S) for x in range(S) if is_hair(p[x, y])]
        tops.append(min(hair) if hair else None)
        fx = [x for y in range(AY - 2, AY + 1) for x in range(S) if p[x, y][3] and not is_blade(p[x, y])]
        footxs.append((min(fx), max(fx)) if fx else None)
    valid = [t for t in tops if t is not None]
    rec('머리 윗줄 상하(몸통 대리값)', '범위 ≤2px', f'{tops}, 범위 {max(valid) - min(valid)}', '금발 색 최상단', '통과' if max(valid) - min(valid) <= 2 else '실패')
    rec('발 x범위(아래 3줄, 칼 색 제외)', '미끄러짐 없음', footxs, '측정값, 판정은 눈', '미확인')
    rec('재생 시간', '약 0.50초', f'{N}÷{FPS}={N / FPS}초', '계산', '통과')
    txt = '\n'.join(rows) + f'\n실패 {fails}건\nexit {1 if fails else 0}\n'
    open(os.path.join(D, 'check_20261107.txt'), 'w', encoding='utf-8').write(txt)
    print(txt)
    return frames, fails


def preview(frames):
    bg = (58, 62, 72, 255)
    g = Image.new('RGBA', (S * N, S), bg)
    for k, f in enumerate(frames):
        g.alpha_composite(f, (S * k, 0))
    g = g.resize((g.width * 2, g.height * 2), Image.NEAREST)
    q = g.load()
    for k in range(N):
        ox = 2 * S * k
        for t in range(2 * S):
            q[ox, t] = (120, 120, 120, 255)  # 칸 경계
            q[ox + 2 * AX, t] = (70, 130, 220, 255)  # 중앙 x=48
            q[ox + t, 2 * (AY + 1)] = (70, 130, 220, 255)  # 바닥선 y=94
        for t in range(4, 2 * S - 4):
            for a, b in ((ox + t, 4), (ox + t, 2 * S - 5), (ox + 4, t), (ox + 2 * S - 5, t)):
                q[a, b] = (220, 70, 70, 255)  # 92×92 안전 영역
    g.save(os.path.join(D, 'preview', 'guides_x2.png'))

    gif = []
    for f in frames:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(f)
        gif.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    # GIF는 1/100초 단위: 60·70을 번갈아 평균 62.5ms(16fps), 마지막은 0.6초 멈춤
    dur = [60, 70] * 3 + [60, 600]
    gif[0].save(os.path.join(D, 'preview', 'slash_16fps_x4.gif'), save_all=True, append_images=gif[1:],
                duration=dur, loop=0, disposal=1)


if __name__ == '__main__':
    build()
    fr, n = check()
    preview(fr)
    sys.exit(1 if n else 0)
