"""Taiwan-flavoured art: bubble tea, fried chicken cutlet, blue-white slippers, beach with tetrapods. Original art."""
import math
import os
import random
import sys
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite
from PIL import Image
from pet import sprite, strip, LINE

OUT = sys.argv[1]
WHITE = "#ffffff"


def save(sp, name):
    sp.save_png(os.path.join(OUT, name))


# ---------- bubble tea (珍珠奶茶) ----------
def cup(s, x, y, sip=False):
    # lid + straw
    s.rect(x + 1, y + 2, x + 10, y + 3, "#ffffff")
    s.rect(x + 6, y - 6 if not sip else y - 3, x + 7, y + 2, "#f2706a")
    # body (tapered)
    for k in range(14):
        inset = k // 7
        s.line(x + 1 + inset, y + 4 + k, x + 10 - inset, y + 4 + k, "#e9c7a0" if k < 9 else "#c9a278")
    for k in range(3):
        s.line(x + 2, y + 5 + k, x + 3, y + 5 + k, "#f6e0c4")
    for (px_, py_) in [(4, 14), (7, 14), (5, 16), (8, 16), (3, 16), (6, 12), (9, 17)]:
        s.px(x + px_, y + py_, "#4a3040"); s.px(x + px_ + 1 if px_ < 9 else x + px_, y + py_, "#4a3040")
    s.rect(x + 1, y + 2, x + 10, y + 3, "#ffffff")
    s.line(x + 1, y + 4, x + 1, y + 17, LINE); s.line(x + 10, y + 4, x + 10, y + 17, LINE)
    s.line(x + 2, y + 18, x + 9, y + 18, LINE); s.line(x + 1, y + 2, x + 10, y + 2, LINE)


def boba(frame):
    s = sprite(bob=frame % 2, happy=True)
    s.snap = False
    cup(s, 34, 24 - (2 if frame % 2 else 0), sip=bool(frame % 2))
    return s

strip([boba(0), boba(1), boba(2), boba(3)], OUT + "/pet_boba.png")

# ---------- fried chicken cutlet (雞排) ----------
GOLD = "#e8a64a"; GOLD_D = "#c47f2e"; GOLD_L = "#f6c872"


def chicken(frame):
    bite = frame >= 2
    s = sprite(bob=frame % 2, happy=(frame % 2 == 1))
    s.snap = False
    # paper bag
    s.polygon([(12, 38), (35, 38), (33, 47), (14, 47)], "#f5ecd6")
    s.line(12, 38, 35, 38, LINE); s.line(12, 38, 14, 47, LINE); s.line(35, 38, 33, 47, LINE); s.line(14, 47, 33, 47, LINE)
    s.line(16, 42, 31, 42, "#d9c9a4")
    # cutlet: wide, flat, bumpy edge
    s.ellipse(3, 30, 44, 42, GOLD)
    s.ellipse(6, 31, 36, 36, GOLD_L, only=GOLD)
    s.ellipse(4, 37, 43, 43, GOLD_D, only=GOLD)
    s.ellipse(6, 35, 41, 40, GOLD, only=GOLD_D)
    for (x, y) in [(9, 33), (15, 35), (22, 32), (29, 35), (35, 33), (40, 36), (12, 39), (20, 38), (28, 39), (34, 38)]:
        s.px(x, y, GOLD_D); s.px(x + 1, y, GOLD_L)
    for x in (4, 8, 13, 18, 24, 30, 36, 41):                 # crispy bumps on top edge
        s.px(x, 30 + (x % 3), GOLD_L)
    if bite:
        s.circle(40, 33, 3, None, fill=True); s.circle(37, 31, 2, None, fill=True)
    s.ellipse(3, 30, 44, 42, LINE, fill=False)
    return s

strip([chicken(0), chicken(1), chicken(2), chicken(3)], OUT + "/pet_chicken.png")


def steam(k):
    s = Sprite(8, 12)
    for y in range(12):
        x = 3 + round(1.5 * math.sin((y + k * 2) / 2.0))
        s.px(x, y, "#ffffff"); s.px(x + 1, y, "#ffffff") if y % 3 else None
    return s

strip([steam(0), steam(1), steam(2)], OUT + "/steam.png")


# ---------- blue-white slippers (藍白拖), layer aligned to pet feet (static) ----------
def slippers():
    s = Sprite(48, 48)
    for x0 in (11, 24):
        s.ellipse(x0, 42, x0 + 12, 47, "#ffffff")                       # white sole
        s.polygon([(x0 + 3, 43), (x0 + 9, 43), (x0 + 6, 46)], "#4a8fd0")   # blue thong strap
        s.line(x0 + 2, 44, x0 + 10, 44, "#4a8fd0")
        s.px(x0 + 6, 46, "#2f6aa8")
    s.outline(LINE, where="outside")
    return s

strip([slippers()] * 4, OUT + "/acc_slippers.png")

# ---------- beach with tetrapods (消波塊) ----------
W, H = 240, 240
b = Sprite(W, H)
for y in range(0, 64):
    t_ = y / 63
    b.line(0, y, W - 1, y, "#8fd0ee" if t_ < 0.5 else "#cdeaf5")
for y in range(64, 132):
    t_ = (y - 64) / 67
    b.line(0, y, W - 1, y, "#5fb4d8" if t_ < 0.5 else "#4a9fcb")
for y in range(132, H):
    b.line(0, y, W - 1, y, "#f2dfae")
rnd = random.Random(9)
for _ in range(420):
    x, y = rnd.randrange(W), rnd.randrange(136, H)
    b.px(x, y, "#e2c98a" if rnd.random() < .6 else "#fff1c9")
for x in range(0, W):
    b.px(x, 132 + int(1.5 * math.sin(x / 9.0)), "#ffffff"); b.px(x, 133 + int(1.5 * math.sin(x / 9.0)), "#cfe8f0")
for (x, y) in [(150, 76), (190, 90), (30, 82), (110, 100), (60, 112), (200, 118)]:
    b.line(x, y, x + 10, y, "#8fd0ee")
save(b, "beach.png")


def tetrapod():
    t = Sprite(36, 32)
    C = "#aab3b8"; CL = "#cfd6da"; CD = "#7d878d"
    t.polygon([(12, 0), (23, 0), (21, 16), (14, 16)], CL)            # top limb (flared end)
    t.polygon([(0, 24), (4, 31), (16, 20), (10, 11)], C)             # left-down limb
    t.polygon([(35, 24), (31, 31), (19, 20), (25, 11)], CD)          # right-down limb
    t.ellipse(9, 10, 26, 26, C)
    t.ellipse(11, 12, 19, 19, CL, only=C)
    t.ellipse(19, 19, 26, 26, CD, only=C)
    t.outline(LINE, where="outside")
    t.line(14, 3, 14, 12, C); t.line(21, 3, 21, 12, CD); t.line(5, 25, 11, 18, CD); t.line(30, 25, 24, 18, CL)
    t.px(17, 6, CD); t.px(17, 7, CD); t.px(8, 27, CD); t.px(28, 28, CL)
    return t

save(tetrapod(), "tetrapod.png")

# waves (4 frames, 64x10): foam line sliding
def wave(k):
    w = Sprite(64, 10)
    for x in range(64):
        y = 4 + round(2 * math.sin((x + k * 16) / 10.0 * math.pi / 3 * 2))
        w.px(x, y, "#ffffff"); w.px(x, y + 1, "#cfe8f0")
    return w

strip([wave(i) for i in range(4)], OUT + "/waves.png")

# icons strip rebuild (4 items 24x16)
names = [("straw_hat", (4, 4, 44, 22)), ("beanie", (8, 6, 40, 23)), ("scarf", (6, 34, 40, 48)), ("slippers", (8, 41, 38, 48))]
icons = Image.new("RGBA", (24 * len(names), 16), (0, 0, 0, 0))
for k, (n, box) in enumerate(names):
    im = Image.open(OUT + "/acc_%s.png" % n).convert("RGBA").crop((0, 0, 48, 48)).crop(box)
    im.thumbnail((24, 16), Image.NEAREST)
    icons.alpha_composite(im, (k * 24, 0))
icons.save(OUT + "/acc_icons.png")
print("taiwan ok")
