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

