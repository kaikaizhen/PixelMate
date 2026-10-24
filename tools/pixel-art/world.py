"""Animated world parts: window sky, clouds, sun/moon/stars, grass, plant, curtains. Original art."""
import math
import os
import sys
import random
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite
from PIL import Image

OUT = sys.argv[1]
LINE = "#4a3040"; FRAME = "#fff3dc"
WW, WH = 64, 72
GREEN = "#7fbf6a"; GREEN_L = "#9ad075"; GREEN_D = "#5a9a55"
POT = "#d9806a"; POT_D = "#b9604e"


def strip(sprites, name):
    w, h = sprites[0].w, sprites[0].h
    im = Image.new("RGBA", (w * len(sprites), h), (0, 0, 0, 0))
    for k, sp in enumerate(sprites):
        im.alpha_composite(sp.composite(1), (k * w, 0))
    im.save(os.path.join(OUT, name))


def single(sp, name):
    sp.save_png(os.path.join(OUT, name))


# ---- window sky backgrounds (no grass, no clouds) ----
def sky(name, top, bottom, hills):
    s = Sprite(WW, WH)
    for y in range(WH):
        t = y / (WH - 1)
        s.line(0, y, WW - 1, y, top if t < 0.45 else bottom)
    for x in range(WW):  # far hills
        h = 10 + int(4 * math.sin(x / 7.0) + 3 * math.sin(x / 3.1 + 1))
        s.line(x, WH - 14 - h, x, WH - 1, hills)
    single(s, "window_sky_%s.png" % name)

sky("sunny", "#9ad8f0", "#d4f0f6", "#8cc98a")
sky("rain", "#7f93ad", "#a9b8c9", "#6f9a73")
sky("night", "#232a50", "#3a4575", "#2f4a45")

# ---- clouds ----
def cloud(name, w, h, body, shade):
    s = Sprite(w, h)
    s.ellipse(0, h // 2, w - 1, h - 1, body)
    s.ellipse(w // 5, 1, w * 3 // 5, h - 3, body)
    s.ellipse(w * 2 // 5, 0, w * 4 // 5, h - 4, body)
    s.line(2, h - 2, w - 3, h - 2, shade)
    single(s, name)

cloud("cloud_white.png", 26, 11, "#ffffff", "#dbe9f2")
cloud("cloud_grey.png", 30, 13, "#a6b3c6", "#8191a8")
cloud("cloud_dark.png", 30, 13, "#6b7a94", "#56647d")

# ---- sun (2 frames: ray pulse) ----
def sun(rays):
    s = Sprite(24, 24)
    s.circle(12, 12, 6, "#ffd86b", fill=True)
    s.circle(12, 12, 6, "#ffbf4a", fill=False)
    px = [(12, 1), (12, 22), (1, 12), (22, 12), (4, 4), (19, 4), (4, 19), (19, 19)]
    for (x, y) in px:
        s.px(x, y, "#ffe9a8")
        if rays:
            dx = 1 if x > 12 else -1 if x < 12 else 0
            dy = 1 if y > 12 else -1 if y < 12 else 0
            s.px(x - dx, y - dy, "#ffe9a8")
    return s

strip([sun(False), sun(True)], "sun.png")

# ---- moon ----
m = Sprite(16, 16)
m.circle(8, 8, 7, "#ffe9a8", fill=True)
m.circle(11, 6, 6, None, fill=True)
m.px(4, 9, "#f2cf86"); m.px(5, 10, "#f2cf86"); m.px(4, 6, "#f2cf86")
single(m, "moon.png")

# ---- star twinkle (3 frames 5x5) ----
def star(k):
    s = Sprite(5, 5)
    s.px(2, 2, "#ffffff")
    if k >= 1:
        s.px(1, 2, "#ffe9a8"); s.px(3, 2, "#ffe9a8"); s.px(2, 1, "#ffe9a8"); s.px(2, 3, "#ffe9a8")
    if k == 2:
        s.px(2, 0, "#ffffff"); s.px(2, 4, "#ffffff"); s.px(0, 2, "#ffffff"); s.px(4, 2, "#ffffff")
    return s

strip([star(0), star(1), star(2), star(1)], "star.png")

# ---- grass: 4 frames, wave travelling through ----
def grass(frame, base, light, dark):
    gw, gh = WW, 16
    s = Sprite(gw, gh)
    rnd = random.Random(11)
    s.rect(0, gh - 4, gw - 1, gh - 1, base)
    for x in range(0, gw, 2):
        h = 5 + rnd.randrange(0, 7)
        for k in range(h):
            amp = (k / h) ** 1.6 * 2.4
            off = round(amp * math.sin((x / 9.0) - frame * math.pi / 2))
            col = light if k > h * 0.55 else base
            s.px(x + off, gh - 5 - k, col)
            if k < h // 2:
                s.px(x + off + 1, gh - 5 - k, dark)
    return s

for name, base, light, dark in [("day", GREEN, GREEN_L, GREEN_D), ("rain", "#6f9a73", "#86b08a", "#557a59"),
                                ("night", "#2f4a45", "#3f6258", "#243a36")]:
    strip([grass(f, base, light, dark) for f in range(4)], "grass_%s.png" % name)

# ---- window frame overlay (cross bars + border), on top of everything ----
fr = Sprite(WW, WH)
fr.rect(WW // 2 - 1, 0, WW // 2, WH - 1, FRAME); fr.rect(0, WH // 2 - 1, WW - 1, WH // 2, FRAME)
single(fr, "window_bars.png")

# ---- plant: 4 frames of sway, canvas 44x80 ----
def plant(sway):
    s = Sprite(44, 80)
    # pot (static) rows 52..77 at x 6..27 (shifted into canvas)
    px0, py0 = 11, 52
    leaves = [(2, 14, 14, 68), (14, 2, 23, 68), (22, 18, 34, 68), (9, 26, 21, 68)]
    for i, (x0, y0, x1, y1) in enumerate(leaves):
        tall = (y1 - y0)
        d = round(sway * (1.0 + 0.3 * i % 2) * 2.0)   # tip offset
        for yy in range(y0, y1 + 1):
            t = 1 - (yy - y0) / max(1, tall)          # 1 at tip, 0 at base
            off = round(d * t * t)
            wl = int((x1 - x0) / 2 * (1 - abs(2 * (yy - y0) / max(1, tall) - 0.7) ** 2) + 0.5)
            cx = (x0 + x1) // 2 + off
            if wl >= 0:
                s.line(cx - wl, yy, cx + wl, yy, GREEN)
        # spine
        for yy in range(y0 + 4, y1 + 1):
            t = 1 - (yy - y0) / max(1, tall)
            s.px((x0 + x1) // 2 + round(d * t * t), yy, GREEN_D)
    s.outline(LINE, where="outside")
    s.rect(px0, py0 + 14, px0 + 21, py0 + 27, POT); s.rect(px0, py0 + 14, px0 + 21, py0 + 27, LINE, fill=False)
    s.rect(px0 + 2, py0 + 20, px0 + 19, py0 + 23, POT_D)
    return s

strip([plant(v) for v in (0, 0.5, 0, -0.5)], "plant.png")

# ---- curtains: 2 sides, 4 frames, canvas 14x84 ----
def curtain(frame, left):
    s = Sprite(14, 84)
    CT = "#f4a6a0"; CT_D = "#e08a86"; CT_L = "#fbc4be"
    for y in range(84):
        t = y / 83.0
        sweep = round(3 * t * math.sin(frame * math.pi / 2 + t * 2.2))
        xs = 0 if left else 3 + (-sweep)
        xe = 10 + sweep if left else 13
        w = max(3, xe - xs)
        s.line(xs, y, xs + w, y, CT)
        s.px(xs + w // 2, y, CT_D)
        s.px(xs + 1, y, CT_L)
    s.rect(0, 0, 13, 2, "#a9744f")
    s.outline(LINE, where="outside")
    return s

for side, left in (("l", True), ("r", False)):
    strip([curtain(f, left) for f in range(4)], "curtain_%s.png" % side)

# ---- raindrop + ripple ----
d = Sprite(3, 6); d.px(1, 0, "#d9ecff"); d.px(1, 1, "#d9ecff"); d.px(1, 2, "#bcd9f2"); d.px(0, 3, "#bcd9f2"); d.px(2, 3, "#bcd9f2")
d.line(1, 3, 1, 5, "#d9ecff")
single(d, "raindrop.png")
def ripple(k):
    s = Sprite(9, 3)
    if k == 0: s.px(4, 1, "#d9ecff")
    if k == 1: s.line(3, 1, 5, 1, "#d9ecff")
    if k == 2: s.line(1, 1, 2, 1, "#d9ecff"); s.line(6, 1, 7, 1, "#d9ecff"); s.px(4, 0, "#d9ecff")
    return s
strip([ripple(0), ripple(1), ripple(2)], "ripple.png")
print("world ok")
