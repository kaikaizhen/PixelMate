"""PixelMate placeholder room 240x240 + window layers (sunny / rain / night). Original art."""
import sys, random
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite

OUT = sys.argv[1]
LINE = "#4a3040"
WALL = "#f6d9c4"; WALL_D = "#ebc3ae"; WALL_STRIPE = "#f9e3d2"
FLOOR = "#d9a778"; FLOOR_D = "#c18f62"; FLOOR_L = "#e6bd91"
TRIM = "#a9744f"
RUG = "#9fd0c8"; RUG_D = "#7fb8b0"; RUG_L = "#c4e6df"
WOOD = "#b5835a"; WOOD_D = "#8f6340"
GREEN = "#7fbf6a"; GREEN_D = "#5a9a55"; POT = "#d9806a"; POT_D = "#b9604e"
FRAME = "#fff3dc"
PAL = [LINE, WALL, WALL_D, WALL_STRIPE, FLOOR, FLOOR_D, FLOOR_L, TRIM, RUG, RUG_D, RUG_L,
       WOOD, WOOD_D, GREEN, GREEN_D, POT, POT_D, FRAME, "#ffffff", "#f2a9a0", "#ffd86b", "#8fc4e8"]
WX, WY, WW, WH = 140, 28, 64, 72  # window hole

s = Sprite(240, 240, palette=PAL)
s.rect(0, 0, 239, 239, WALL)
for x in range(0, 240, 16):                      # wallpaper stripes
    s.rect(x, 0, x + 7, 149, WALL_STRIPE)
for y in range(8, 150, 16):                      # dots
    for x in range(8, 240, 32):
        s.px(x + (8 if (y // 16) % 2 else 0), y, WALL_D)
s.rect(0, 150, 239, 239, FLOOR)                  # floor
for y in range(158, 240, 12):
    s.line(0, y, 239, y, FLOOR_D)
for row, y in enumerate(range(150, 240, 12)):
    for x in range((row % 2) * 20, 240, 40):
        s.line(x, y, x, y + 11, FLOOR_D)
for x in range(0, 240, 24):
    s.px(x + 5, 154, FLOOR_L); s.px(x + 17, 166, FLOOR_L)
s.rect(0, 144, 239, 149, TRIM); s.line(0, 144, 239, 144, LINE)   # baseboard
s.line(0, 150, 239, 150, LINE)
# rug
s.ellipse(56, 188, 184, 232, RUG)
s.ellipse(64, 194, 176, 226, RUG_L, fill=False)
s.ellipse(72, 199, 168, 221, RUG_D, fill=False)
s.ellipse(56, 188, 184, 232, LINE, fill=False)
# window frame (hole stays transparent in separate layer)
s.rect(WX - 5, WY - 5, WX + WW + 4, WY + WH + 4, FRAME)
s.rect(WX - 5, WY - 5, WX + WW + 4, WY + WH + 4, LINE, fill=False)
s.rect(WX - 2, WY + WH + 5, WX + WW + 1, WY + WH + 9, TRIM)              # sill
s.rect(WX - 2, WY + WH + 5, WX + WW + 1, WY + WH + 9, LINE, fill=False)
# plant (left)
s.rect(26, 118, 47, 143, POT); s.rect(26, 118, 47, 143, LINE, fill=False)
s.rect(28, 132, 45, 135, POT_D)
for (x0, y0, x1, y1) in [(20, 84, 32, 118), (33, 70, 42, 118), (42, 88, 54, 118), (28, 96, 40, 118)]:
    s.ellipse(x0, y0, x1, y1, GREEN)
    s.line((x0 + x1) // 2, y0 + 4, (x0 + x1) // 2, y1, GREEN_D)
    s.ellipse(x0, y0, x1, y1, LINE, fill=False)
# shelf with frames
s.rect(40, 36, 108, 40, WOOD); s.rect(40, 36, 108, 40, LINE, fill=False)
s.rect(46, 14, 62, 34, FRAME); s.rect(46, 14, 62, 34, LINE, fill=False); s.rect(50, 18, 58, 30, "#f2a9a0")
s.rect(70, 22, 90, 34, FRAME); s.rect(70, 22, 90, 34, LINE, fill=False); s.rect(73, 25, 87, 31, "#8fc4e8")
s.rect(94, 24, 104, 35, POT); s.rect(94, 24, 104, 35, LINE, fill=False); s.px(98, 22, GREEN); s.px(99, 21, GREEN)
# window hole: transparent (separate layer shows through)
s.rect(WX, WY, WX + WW - 1, WY + WH - 1, None)
# cross bar drawn on top by window layer
s.save_png(OUT + "/room.png")
s.preview(OUT + "/_preview_room.png", scale=3, bg="#cfe3f0")

def window(name, sky, ground, extra):
    w = Sprite(WW, WH, palette=PAL + [sky[0], sky[1], ground])
    for y in range(WH):
        w.line(0, y, WW - 1, y, sky[0] if y < WH * 0.55 else sky[1])
    w.rect(0, WH - 14, WW - 1, WH - 1, ground)
    w.rect(0, WH - 14, WW - 1, WH - 14, GREEN_D)
    extra(w)
    w.rect(WW // 2 - 1, 0, WW // 2, WH - 1, FRAME); w.rect(0, WH // 2 - 1, WW - 1, WH // 2, FRAME)
    w.save_png(OUT + "/window_%s.png" % name)

def sunny(w):
    w.circle(46, 16, 8, "#ffd86b", fill=True)
    w.circle(46, 16, 8, "#ffbf4a", fill=False)
    for cx, cy in [(14, 20), (30, 38)]:
        w.ellipse(cx - 8, cy, cx + 8, cy + 8, "#ffffff"); w.ellipse(cx - 4, cy - 4, cx + 4, cy + 4, "#ffffff")

def rain(w):
    rnd = random.Random(7)
    w.rect(0, 0, WW - 1, 22, "#8a9ab0")
    for cx in (10, 28, 44, 58):
        w.ellipse(cx - 9, 8, cx + 9, 24, "#a6b3c6")
    for _ in range(26):
        x = rnd.randrange(2, WW - 2); y = rnd.randrange(22, WH - 16)
        w.line(x, y, x - 1, y + 3, "#d9ecff")

def night(w):
    rnd = random.Random(3)
    w.circle(18, 16, 8, "#ffe9a8", fill=True); w.circle(22, 14, 7, "#2b3358", fill=True)
    for _ in range(14):
        w.px(rnd.randrange(2, WW - 2), rnd.randrange(2, WH - 24), "#ffffff")
    w.px(44, 12, "#ffd86b"); w.px(45, 12, "#ffd86b"); w.px(44, 13, "#ffd86b")

window("sunny", ("#9ad8f0", "#d4f0f6"), "#9ad075", sunny)
window("rain", ("#7f93ad", "#a9b8c9"), "#6f9a73", rain)
window("night", ("#232a50", "#3a4575"), "#2f4a45", night)
