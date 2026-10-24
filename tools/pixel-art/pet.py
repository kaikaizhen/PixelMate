"""PixelMate placeholder pet: round star-sprite (48x48). Original, species-neutral, temporary art."""
import os
import sys
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite
from PIL import Image

LINE = "#4a3040"; BODY = "#fbe3c8"; BODY_D = "#efc3a4"; BODY_L = "#fff4e4"
CHEEK = "#f4a6a0"; EYE = "#3a2433"; WHITE = "#ffffff"
STAR = "#ffd86b"; STAR_D = "#f2b84a"; FOOT = "#e9a98e"
PAL = [LINE, BODY, BODY_D, BODY_L, CHEEK, EYE, WHITE, STAR, STAR_D, FOOT]


def sprite(bob=0, sway=0, blink=False, happy=False, look=None, step=0):
    s = Sprite(48, 48, palette=PAL)
    y = bob
    # little feet
    la = 2 if step == 1 else 0   # left foot forward
    ra = 2 if step == 3 else 0   # right foot forward
    s.ellipse(13 + la, 41 - la, 22 + la, 46 - la, FOOT); s.ellipse(25 - ra, 41 - ra, 34 - ra, 46 - ra, FOOT)
    # round body
    s.ellipse(6, 12 + y, 41, 44 + y, BODY)
    s.ellipse(9, 14 + y, 28, 24 + y, BODY_L, only=BODY)       # top-left light
    s.ellipse(8, 34 + y, 39, 46 + y, BODY_D, only=BODY)       # bottom shade
    s.ellipse(10, 31 + y, 37, 42 + y, BODY, only=BODY_D)
    # nub arms
    s.ellipse(2, 26 + y, 9, 34 + y, BODY); s.ellipse(38, 26 + y, 45, 34 + y, BODY)
    s.outline(LINE, where="outside")
    # star sprout on head (sways)
    sx = 24 + sway
    s.line(24, 12 + y, sx, 7 + y, LINE)
    s.polygon([(sx, 0 + y), (sx + 2, 3 + y), (sx + 5, 3 + y), (sx + 3, 5 + y),
               (sx + 4, 8 + y), (sx, 6 + y), (sx - 4, 8 + y), (sx - 3, 5 + y),
               (sx - 5, 3 + y), (sx - 2, 3 + y)], STAR)
    s.px(sx - 1, 3 + y, WHITE); s.px(sx + 3, 6 + y, STAR_D); s.px(sx - 3, 6 + y, STAR_D)
    # face
    if blink:
        s.line(14, 27 + y, 18, 27 + y, EYE); s.line(29, 27 + y, 33, 27 + y, EYE)
    elif happy:
        for ex in (14, 29):
            s.px(ex, 28 + y, EYE); s.px(ex + 1, 27 + y, EYE); s.px(ex + 2, 26 + y, EYE)
            s.px(ex + 3, 27 + y, EYE); s.px(ex + 4, 28 + y, EYE)
    elif look == "down":
        s.rect(14, 26 + y, 17, 29 + y, EYE); s.rect(30, 26 + y, 33, 29 + y, EYE)
    elif look == "right":
        s.rect(16, 24 + y, 19, 28 + y, EYE); s.rect(32, 24 + y, 35, 28 + y, EYE)
        s.px(18, 24 + y, WHITE); s.px(34, 24 + y, WHITE)
    else:
        s.rect(14, 24 + y, 17, 28 + y, EYE); s.rect(30, 24 + y, 33, 28 + y, EYE)
        s.px(14, 24 + y, WHITE); s.px(30, 24 + y, WHITE)
    s.rect(9, 30 + y, 12, 32 + y, CHEEK); s.rect(35, 30 + y, 38, 32 + y, CHEEK)
    if happy:
        s.rect(22, 30 + y, 25, 31 + y, EYE); s.rect(23, 32 + y, 24, 33 + y, EYE)
    else:
        s.px(22, 31 + y, EYE); s.px(25, 31 + y, EYE); s.line(23, 32 + y, 24, 32 + y, EYE)
    return s


def strip(sprites, path):
    n = len(sprites); w, h = sprites[0].w, sprites[0].h
    im = Image.new("RGBA", (w * n, h), (0, 0, 0, 0))
    for k, sp in enumerate(sprites):
        im.alpha_composite(sp.composite(1), (k * w, 0))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)


def main():
    OUT = sys.argv[1]
    idle = [sprite(0, 0), sprite(0, 1), sprite(1, 2), sprite(1, 1)]
    strip(idle, OUT + "/pet_idle.png")
    strip([sprite(blink=True), sprite(happy=True)], OUT + "/pet_faces.png")
    strip([sprite(0, 0, step=0), sprite(1, 1, step=1), sprite(0, 2, step=0), sprite(1, 1, step=3)], OUT + "/pet_walk.png")


if __name__ == "__main__":
    main()
