"""31번 2타 v2e → v2f: f3 잔상을 120° 칼까지 늘림(생성 없음, 2026-11-07 사용자: "칼 각도를 바꾸고 잔상도 그만큼 늘려줘").
측정(코등이 (62,55) 기준, 화면 0°=오른쪽·90°=위): 기존 잔상은 −90°~40°, 위쪽 끝 반지름 약 13~28, 바깥 크림색 테두리.
40°→120°를 같은 반지름 띠로 잇는다(바깥 r 28 고정, 안쪽 반지름은 칼 쪽으로 갈수록 커져 얇아짐).
안쪽 회색 a, 바깥 r≥25 크림색 f. 빈 칸에만 칠함(칼·몸이 앞). 기존 끝(25~45°)의 어두운 경계선 7은 같은 색으로 바꿔 이음.
  python attack_slash02_smear120.py -> 0_east_v2f.png, preview/f3_smear120_x5.png, GIF
"""
import math
import os
from PIL import Image
from attack_slash_east import px
from attack_slash02_fararm import PAL

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
S, K = 96, 3
G = (62, 55)
A0, A1 = 40, 120
R_OUT, R_IN0, R_IN1, RIM = 28, 14, 22, 25
SEAM = (25, 45)


def polar(x, y):
    return math.degrees(math.atan2(G[1] - y, x - G[0])), math.hypot(x - G[0], y - G[1])


def main():
    sheet = Image.open(os.path.join(D, '0_east_v2e.png')).convert('RGBA')
    f = sheet.crop((S * (K - 1), 0, S * K, S))
    p = f.load()
    seam = 0
    for y in range(S):                     # 기존 잔상 끝 어두운 경계선 → 이어지는 색
        for x in range(G[0] + 1, S):
            a, r = polar(x, y)
            if SEAM[0] <= a <= SEAM[1] and 12 <= r <= R_OUT + 1 and p[x, y][3] and p[x, y][:3] == PAL['7'][:3]:
                p[x, y] = PAL['f' if r >= RIM else 'a']
                seam += 1
    added = 0
    for y in range(S):
        for x in range(S):
            a, r = polar(x, y)
            if not (A0 <= a <= A1) or p[x, y][3]:
                continue
            r_in = R_IN0 + (R_IN1 - R_IN0) * (a - A0) / (A1 - A0)
            if r_in <= r <= R_OUT:
                p[x, y] = PAL['f' if r >= RIM else 'a']
                added += 1
    out = sheet.copy()
    out.paste(f, (S * (K - 1), 0))
    out.save(os.path.join(D, '0_east_v2f.png'))
    d = px(out)
    b = f.getbbox()
    print(f'f{K}: 잔상 {added}px 추가, 경계선 {seam}px 색 바꿈, 여백 좌상우하 {(b[0], b[1], S - b[2], S - b[3])}, 크기 {b[2] - b[0]}×{b[3] - b[1]}',
          '| 색 수', len({c[:3] for c in d if c[3]}), '| 알파', sorted({c[3] for c in d}))
    bg = (58, 62, 72, 255)
    W = Image.new('RGBA', (S * 2, S), bg)
    W.alpha_composite(sheet.crop((S * (K - 1), 0, S * K, S)), (0, 0))
    W.alpha_composite(f, (S, 0))
    W.resize((W.width * 5, W.height * 5), Image.NEAREST).save(os.path.join(D, 'preview', 'f3_smear120_x5.png'))
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    fs = [out.crop((S * k, 0, S * k + S, S)) for k in range(8)]
    for name, fr in (('slash02_v2f_16fps_x4.gif', fs), ('slash01_02_chain_v2f_16fps_x4.gif', [first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + fs[1:])):
        g = []
        for fr_ in fr:
            c = Image.new('RGBA', (S, S), bg)
            c.alpha_composite(fr_)
            g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
        g[0].save(os.path.join(D, 'preview', name), save_all=True, append_images=g[1:],
                  duration=([60, 70] * 20)[:len(g) - 1] + [600], loop=0, disposal=1)


if __name__ == '__main__':
    main()
