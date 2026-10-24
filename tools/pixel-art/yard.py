"""Mail-delivery scene art: yard background, mailbox (flag down/up), postman, envelope. Original art."""
import math
import os
import random
import sys
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite
from PIL import Image
from pet import strip

OUT = sys.argv[1]
LINE = "#4a3040"
W, H = 240, 144


def save(sp, name):
    sp.save_png(os.path.join(OUT, name))

# ---- yard background ----
s = Sprite(W, H)
for y in range(0, 74):
    s.line(0, y, W - 1, y, "#9ad8f0" if y < 36 else "#d4f0f6")
for x in range(W):                               # distant hills
    h = 12 + int(5 * math.sin(x / 11.0) + 3 * math.sin(x / 4.3 + 2))
    s.line(x, 72 - h, x, 80, "#8cc98a")
s.rect(0, 74, W - 1, H - 1, "#9ad075")           # lawn
rnd = random.Random(5)
for _ in range(220):
    x, y = rnd.randrange(W), rnd.randrange(78, H)
    s.px(x, y, "#86c063"); s.px(x + 1, y, "#b0df8b")
# fence
for x in range(2, W, 10):
    s.rect(x, 62, x + 6, 80, "#fff3dc"); s.rect(x, 62, x + 6, 80, LINE, fill=False)
    s.px(x + 3, 61, LINE)
s.line(0, 68, W - 1, 68, LINE); s.line(0, 75, W - 1, 75, LINE)
# stone path
s.rect(0, 108, W - 1, 126, "#ecd9ae")
for x in range(0, W, 14):
    s.ellipse(x + 1, 111, x + 12, 123, "#f6e8c6"); s.ellipse(x + 1, 111, x + 12, 123, "#cdb98a", fill=False)
save(s, "yard.png")

# ---- mailbox 24x40, 2 frames (flag down / up) ----
def mailbox(up):
    m = Sprite(24, 40)
    m.rect(10, 18, 13, 39, "#a9744f"); m.rect(10, 18, 13, 39, LINE, fill=False)       # post
    m.rect(2, 4, 21, 19, "#f2706a"); m.rect(2, 4, 21, 19, LINE, fill=False)           # box
    m.rect(4, 6, 19, 8, "#f89a94")
    m.rect(5, 12, 18, 17, "#c9433f")                                                   # slot
    if up:
        m.rect(20, 0, 22, 12, "#ffd86b"); m.rect(20, 0, 22, 12, LINE, fill=False)
        m.rect(19, 0, 23, 3, "#ffd86b"); m.rect(19, 0, 23, 3, LINE, fill=False)
    else:
        m.rect(20, 8, 22, 15, "#ffd86b"); m.rect(20, 8, 22, 15, LINE, fill=False)
    return m
strip([mailbox(False), mailbox(True)], OUT + "/mailbox.png")

# ---- postman: round blue creature with cap and satchel, 40x40; 4 walk frames + 1 hand-over ----
B = "#bfe0f5"; B_D = "#8fc0e0"; B_L = "#e6f4fc"; CAP = "#f2706a"; CAP_D = "#c9433f"; BAG = "#b5835a"; EYE = "#3a2433"
def postman(bob=0, step=0, hand=False):
    p = Sprite(40, 40)
    y = bob
    # feet
    lf = 30 + (1 if step == 1 else 0); rf = 30 + (1 if step == 3 else 0)
    p.ellipse(9 + (2 if step == 1 else 0), 33, 17 + (2 if step == 1 else 0), 38, "#e9a98e")
    p.ellipse(22 - (2 if step == 3 else 0), 33, 30 - (2 if step == 3 else 0), 38, "#e9a98e")
    p.ellipse(4, 8 + y, 35, 36 + y, B)
    p.ellipse(7, 10 + y, 24, 18 + y, B_L, only=B)
    p.ellipse(6, 28 + y, 33, 38 + y, B_D, only=B)
    p.ellipse(8, 25 + y, 31, 34 + y, B, only=B_D)
    # arm (extended when handing over)
    if hand:
        p.ellipse(33, 20 + y, 40, 26 + y, B)
    else:
        p.ellipse(0, 20 + y, 6, 28 + y, B)
    p.outline(LINE, where="outside")
    # cap
    p.rect(8, 2 + y, 31, 8 + y, CAP); p.rect(8, 2 + y, 31, 8 + y, LINE, fill=False)
    p.rect(26, 7 + y, 38, 9 + y, CAP_D); p.rect(26, 7 + y, 38, 9 + y, LINE, fill=False)
    p.rect(17, 4 + y, 22, 6 + y, "#ffffff")
    # face
    p.rect(11, 17 + y, 13, 21 + y, EYE); p.rect(24, 17 + y, 26, 21 + y, EYE)
    p.px(11, 17 + y, "#ffffff"); p.px(24, 17 + y, "#ffffff")
    p.rect(6, 23 + y, 9, 24 + y, "#f4a6a0"); p.rect(29, 23 + y, 32, 24 + y, "#f4a6a0")
    p.line(17, 24 + y, 20, 24 + y, EYE)
    # satchel strap + bag
    p.line(32, 26 + y, 11, 31 + y, BAG); p.line(32, 27 + y, 11, 32 + y, "#8f6340")
    p.rect(2, 26 + y, 13, 34 + y, BAG); p.rect(2, 26 + y, 13, 34 + y, LINE, fill=False); p.rect(3, 28 + y, 12, 29 + y, "#8f6340")
    return p
frames = [postman(0, 0), postman(1, 1), postman(0, 2), postman(1, 3)]
strip(frames, OUT + "/postman_walk.png")
strip([postman(0, 0, hand=True), postman(1, 0, hand=True)], OUT + "/postman_give.png")

# ---- envelope 16x12 ----
e = Sprite(16, 12)
e.rect(0, 1, 15, 11, "#fff6e2"); e.rect(0, 1, 15, 11, LINE, fill=False)
e.line(1, 2, 7, 7, LINE); e.line(14, 2, 8, 7, LINE)
e.rect(6, 6, 9, 9, "#f2706a"); e.px(7, 7, "#ffd0cc")
save(e, "envelope.png")
print("yard ok")
