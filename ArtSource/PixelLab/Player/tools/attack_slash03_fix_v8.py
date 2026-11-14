"""32번 3타 일부 프레임 수정안 v8(2026-11-08 사용자): GIF 16(= 3타 f3) 내려치기 시작, GIF 18~22(= 3타 f5~f9) 제동·회수.
GIF 번호 대응: 이어 보기 GIF는 2타 f8과 3타 f2가 같아 한 장으로 합쳐져 25장 → GIF 16 = 3타 f3, 17 = f4(타격), 18~22 = f5~f9.
생성: edit_image_pro_flash 1회(f3), animate_image_pixminimax 1회(잔상 없는 타격 → f10, 8프레임). 재생성 없음.
기존 잔상(f4 초승달, f5 테두리)은 지우거나 다시 그리지 않는다. 비교 그림에서만 뺀다(clean). 승인 후 합칠 때 그대로 얹는다.
  python attack_slash03_fix_v8.py          -> fix_v8/compare_clean_x2.png, fix_v8/clean_before/after GIF (잔상 없음)
  python attack_slash03_fix_v8.py merge    -> 0_east_v8.png(기존 잔상 그대로 합침), 이어 보기 GIF
"""
import os
import sys
from PIL import Image
from attack_slash_east import snap, px

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
D3 = os.path.join(ROOT, '32_attack_greatsword_heavy_slash_03_east')
D2 = os.path.join(ROOT, '31_attack_greatsword_light_slash_02_up_east')
F = os.path.join(D3, 'fix_v8')
S = 96
# 3타 프레임(1부터) → 새 그림. seg 1(머리 윗줄 63, 가장 깊게 꺼짐)·5는 뺌.
NEW = {3: 'chop40', 5: 'raw/seg/frame_02.png', 6: 'raw/seg/frame_03.png', 7: 'raw/seg/frame_04.png',
       8: 'raw/seg/frame_06.png', 9: 'raw/seg/frame_07.png'}


def sheets():
    v7 = Image.open(os.path.join(D3, '0_east_v7.png')).convert('RGBA')
    v3 = Image.open(os.path.join(D3, '0_east_v3.png')).convert('RGBA')
    frames = [v7.crop((S * k, 0, S * k + S, S)) for k in range(12)]
    clean = list(frames)
    clean[3] = Image.open(os.path.join(D3, 'key_b_strike.png')).convert('RGBA')   # f4 잔상 없는 타격
    clean[4] = v3.crop((S * 4, 0, S * 5, S))                                      # f5 잔상 없는 감속 1
    return frames, clean


def chop40(clean):
    """GIF 16(3타 f3) = 내려치는 도중(2026-11-08 사용자 "중간에 내려치는 모션이 없다").
    f3 편집본(약함)·생성 구간(raw/chop, 바로 타격으로 건너뜀+잔상)은 쓰지 않음. 칼을 120°로 바꾸기 전 2타 마지막 자세
    (칼 40°, 두 손이 그 손잡이 축 위)의 허리 위(y<70) + 지금 f3의 무릎 굽힌 다리. 칼이 120° → 40° → 타격(앞 아래)로 이어짐."""
    top = Image.open(os.path.join(D2, 'key_c_finish_overhead_v7c_blade_low.png')).convert('RGBA')
    f = clean[2].copy()
    f.paste(Image.new('RGBA', (S, 70), (0, 0, 0, 0)), (0, 0))
    f.alpha_composite(top.crop((0, 0, S, 70)), (0, 0))
    return f


def new_clean(clean):
    out = list(clean)
    for k, n in NEW.items():
        out[k - 1] = chop40(clean) if n == 'chop40' else snap(Image.open(os.path.join(F, n)).convert('RGBA'))
    return out


def gif(frames, path):
    bg = (58, 62, 72, 255)
    g = []
    for f in frames:
        c = Image.new('RGBA', (S, S), bg)
        c.alpha_composite(f)
        g.append(c.resize((S * 4, S * 4), Image.NEAREST).convert('RGB'))
    g[0].save(path, save_all=True, append_images=g[1:], duration=([60, 70] * 40)[:len(g) - 1] + [600], loop=0, disposal=1)


def compare():
    _, clean = sheets()
    after = new_clean(clean)
    bg = (58, 62, 72, 255)
    ks = range(1, 12)                                   # 3타 f2~f12 = GIF 15~25
    W = Image.new('RGBA', (S * len(ks), S * 2), bg)
    for r, row in enumerate((clean, after)):
        for i, k in enumerate(ks):
            W.alpha_composite(row[k], (S * i, S * r))
    W = W.resize((W.width * 2, W.height * 2), Image.NEAREST)
    q = W.load()
    for i in range(len(ks)):                            # 칸 경계·바닥선
        for t in range(S * 4):
            q[S * 2 * i, t] = (120, 120, 120, 255)
        for t in range(S * 2):
            q[S * 2 * i + t, 2 * 94] = q[S * 2 * i + t, 2 * (S + 94)] = (70, 130, 220, 255)
    W.save(os.path.join(F, 'compare_clean_x2.png'))
    gif(clean[1:], os.path.join(F, 'clean_before_16fps_x4.gif'))
    gif(after[1:], os.path.join(F, 'clean_after_16fps_x4.gif'))
    for k in sorted(NEW):
        b = after[k - 1].getbbox()
        print(f'f{k}(GIF {k + 13}) 여백 좌상우하 {(b[0], b[1], S - b[2], S - b[3])}')
    d = [c for f in after for c in px(f) if c[3]]
    print('색 수', len({c[:3] for c in d}))


def merge():
    frames, clean = sheets()
    after = new_clean(clean)
    # 기존 잔상 그대로: f4는 기존 f4(타격+초승달) 그대로, f5는 새 그림 빈 칸에 기존 테두리 잔상 픽셀만
    v7 = Image.open(os.path.join(D3, '0_east_v7.png')).convert('RGBA')
    v3 = Image.open(os.path.join(D3, '0_east_v3.png')).convert('RGBA')
    a, b = v7.load(), v3.load()
    rim = {(x - S * 4, y): a[x, y] for y in range(S) for x in range(S * 4, S * 5) if a[x, y] != b[x, y]}
    after[3] = frames[3]
    p = after[4].load()
    for q, c in rim.items():
        if not p[q][3]:
            p[q] = c
    out = Image.new('RGBA', (S * 12, S), (0, 0, 0, 0))
    for k, f in enumerate(after):
        out.paste(f, (S * k, 0))
    out.save(os.path.join(D3, '0_east_v8.png'))
    s2 = Image.open(os.path.join(D2, '0_east_v2i.png')).convert('RGBA')
    first = Image.open(os.path.join(ROOT, '29_attack_greatsword_light_slash_east', '0_east_pauldron_w.png')).convert('RGBA')
    chain = ([first.crop((S * k, 0, S * k + S, S)) for k in range(8)] + [s2.crop((S * k, 0, S * k + S, S)) for k in range(1, 8)]
             + after[1:])
    gif(chain, os.path.join(D3, 'preview', 'slash01_02i_03v8_chain_16fps_x4.gif'))
    print('잔상 테두리', len(rim), 'px 다시 얹음 | 2타 f8 == 3타 f1:', s2.crop((S * 7, 0, S * 8, S)).tobytes() == after[0].tobytes())


if __name__ == '__main__':
    merge() if sys.argv[1:] == ['merge'] else compare()
