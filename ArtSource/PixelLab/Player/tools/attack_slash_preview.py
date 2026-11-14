# 세 포즈로 만든 연결 미리보기(생성 없음, 최종본 아님). 사이 프레임은 가까운 포즈의 몸을 옮기고 칼·손만 설계 위치로 그린다
import os, sys
import numpy as np
from PIL import Image
import attack_keys_final as F

TAKE = os.path.join(F.RAW, 'take2')
BODY = {k: F.body_only(i, os.path.join(TAKE, k + '.png')) for k, i in F.KEYS.items()}
# (설계 프레임 번호, 쓸 몸, 몸 이동칸, 표시 ms). 16fps = 62.5ms, 마지막만 1200ms 유지
SEQ = [(2, 'f3_windup', 0, 120), (3, 'f4_hit', 0, 60), (4, 'f4_hit', 1, 60), (5, 'f8_finish', 1, 60), (6, 'f8_finish', 0, 70), (7, 'f8_finish', 0, 1200)]


def shifted(b, dx):
    # 발은 그대로 두고 허리 위만 옮긴다
    a = np.array(b)
    top = a[:F.K.OY + 46].copy()
    a[:F.K.OY + 46] = np.roll(top, dx, axis=1)
    return Image.fromarray(a).copy()


frames = [F.compose(i, shifted(BODY[k], dx)) for i, k, dx, _ in SEQ]
S = 3
bg = (58, 60, 70, 255)
out = []
for f in frames:
    c = Image.new('RGBA', (F.K.W, F.K.H), bg)
    c.alpha_composite(f)
    out.append(c.resize((F.K.W * S, F.K.H * S), 0).convert('P', palette=Image.ADAPTIVE, colors=64))
path = os.path.join(F.OUT, 'preview', 'slash_preview.gif')
out[0].save(path, save_all=True, append_images=out[1:], duration=[q[3] for q in SEQ], loop=0, disposal=1)
print('frames', len(frames), 'ms', [q[3] for q in SEQ], 'total', sum(q[3] for q in SEQ))
