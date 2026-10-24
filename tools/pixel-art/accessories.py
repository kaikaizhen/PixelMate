"""Wearable layers aligned to pet_idle frames (same canvas 48x48, same bob). Original art."""
import sys
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite
from PIL import Image
from pet import strip, LINE

OUT = sys.argv[1]
BOB = [0, 0, 1, 1]  # must match pet_idle frames


def straw(b):
    s = Sprite(48, 48)
    s.ellipse(5, 12 + b, 42, 19 + b, "#e8c06a")
    s.ellipse(15, 6 + b, 32, 15 + b, "#f0cf80")
    s.rect(15, 11 + b, 32, 13 + b, "#f2706a")
    s.line(7, 17 + b, 40, 17 + b, "#c79a45")
    s.px(18, 8 + b, "#fff0b8"); s.px(19, 8 + b, "#fff0b8")
    s.outline(LINE, where="outside")
    return s


def beanie(b):
    s = Sprite(48, 48)
    s.ellipse(12, 8 + b, 35, 19 + b, "#f4a6a0")
    for x in range(14, 34, 4):
        s.line(x, 10 + b, x, 17 + b, "#f8bdb8")
    s.rect(10, 17 + b, 37, 21 + b, "#fff3dc")
    for x in range(11, 37, 3):
        s.line(x, 18 + b, x, 20 + b, "#ead7b4")
    s.outline(LINE, where="outside")
    return s


def scarf(b):
    s = Sprite(48, 48)
    s.rect(9, 36 + b, 38, 40 + b, "#8fc4e8")
    s.ellipse(8, 36 + b, 39, 40 + b, "#8fc4e8")
    for x in range(10, 38, 6):
        s.rect(x, 36 + b, x + 2, 40 + b, "#ffffff")
    s.rect(31, 40 + b, 37, 47, "#8fc4e8")                  # hanging tail
    s.rect(31, 43, 37, 44, "#ffffff")
    s.outline(LINE, where="outside")
    return s


for name, fn in (("straw_hat", straw), ("beanie", beanie), ("scarf", scarf)):
    strip([fn(b) for b in BOB], OUT + "/acc_%s.png" % name)

# icons (24x16) cropped from frame 0
icons = Image.new("RGBA", (24 * 3, 16), (0, 0, 0, 0))
for k, (name, box) in enumerate((("straw_hat", (4, 4, 44, 22)), ("beanie", (8, 6, 40, 23)), ("scarf", (6, 34, 40, 48)))):
    im = Image.open(OUT + "/acc_%s.png" % name).convert("RGBA").crop((0, 0, 48, 48)).crop(box)
    im.thumbnail((24, 16), Image.NEAREST)
    icons.alpha_composite(im, (k * 24, 0))
icons.save(OUT + "/acc_icons.png")
print("acc ok")
