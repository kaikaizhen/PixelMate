"""Pet daily-activity sprites built on pet.sprite(): sleep, read, watch (rain), gift."""
import sys
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite
from PIL import Image
from pet import sprite, strip, LINE, EYE, WHITE

OUT = sys.argv[1]
BLUE = "#8fc4e8"; BLUE_D = "#5f9cc8"; PAPER = "#fff6e2"; STRAW = "#e8c06a"; STRAW_D = "#c79a45"; RIBBON = "#f2706a"


def with_pal(s):
    s.snap = False
    return s

# --- sleeping: closed eyes, slow breathing, lowered body ---
strip([sprite(bob=2, blink=True), sprite(bob=3, blink=True)], OUT + "/pet_sleep.png")

# --- reading: eyes down, book in front, page flips ---
def read(frame):
    s = sprite(bob=0, look="down")
    s.snap = False
    s.rect(12, 33, 35, 43, BLUE); s.rect(12, 33, 35, 43, LINE, fill=False)
    s.rect(14, 35, 23, 41, PAPER); s.rect(25, 35, 33, 41, PAPER)
    s.line(24, 34, 24, 42, LINE)
    for yy in (37, 39):
        s.line(15, yy, 22, yy, "#cdbfae"); s.line(26, yy, 32, yy, "#cdbfae")
    if frame == 1:
        s.rect(25, 35, 33, 41, PAPER); s.polygon([(24, 35), (29, 33), (29, 39), (24, 41)], "#ffffff")
        s.line(24, 35, 29, 33, LINE)
    return s
strip([read(0), read(0), read(1), read(0)], OUT + "/pet_read.png")

# --- watching the window: looks right, gentle sway ---
strip([sprite(bob=0, look="right"), sprite(bob=0, sway=1, look="right"), sprite(bob=1, sway=2, look="right"), sprite(bob=1, sway=1, look="right")],
      OUT + "/pet_watch.png")

# --- gift: happy, holds a straw hat up ---
def gift(bob):
    s = sprite(bob=bob, happy=True)
    s.snap = False
    hy = 12 + bob
    s.rect(36, hy + 4, 47, hy + 6, STRAW); s.rect(38, hy, 45, hy + 4, STRAW)
    s.line(36, hy + 6, 47, hy + 6, STRAW_D); s.line(38, hy + 3, 45, hy + 3, RIBBON)
    s.outline(LINE, where="outside") if False else None
    for x in range(36, 48):
        s.px(x, hy + 7, LINE)
    s.line(37, hy + 4, 37, hy + 6, LINE); s.line(38, hy - 1, 45, hy - 1, LINE)
    s.line(37, hy, 37, hy + 3, LINE); s.line(46, hy, 46, hy + 3, LINE)
    return s
strip([gift(0), gift(1)], OUT + "/pet_gift.png")

# --- zZ (3 frames 10x12) ---
def z(k):
    s = Sprite(10, 12)
    def zed(x, y, sz, c):
        s.line(x, y, x + sz, y, c); s.line(x + sz, y, x, y + sz, c); s.line(x, y + sz, x + sz, y + sz, c)
    for (x, y, sz) in [(1, 8 - k, 3), (4, 3 - k, 4)]:
        zed(x + 1, y + 1, sz, "#6f86a8"); zed(x, y, sz, "#ffffff")
    return s
strip([z(0), z(1), z(2)], OUT + "/zzz.png")

# --- sparkle (3 frames 7x7) ---
def sparkle(k):
    s = Sprite(7, 7)
    c = "#ffe97a"
    s.px(3, 3, c)
    if k >= 1: s.px(2, 3, c); s.px(4, 3, c); s.px(3, 2, c); s.px(3, 4, c)
    if k == 2: s.px(3, 0, c); s.px(3, 6, c); s.px(0, 3, c); s.px(6, 3, c)
    return s
strip([sparkle(0), sparkle(1), sparkle(2), sparkle(1)], OUT + "/sparkle.png")
print("activities ok")
