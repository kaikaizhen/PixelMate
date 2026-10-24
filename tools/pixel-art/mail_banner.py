"""E-mail header banners for apps/api (embedded as inline images): the star pet on a cream pixel scene.
Two moods: confirm (happy, holding a letter) and reset (look-down, with a little key). Output is 1200x320 for a 600px-wide mail."""
import os
import random

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SP = os.path.join(ROOT, "assets", "sprites", "pets", "star_01")
OUT = os.path.join(ROOT, "apps", "api", "src", "PixelMate.Api", "Mail", "Assets")
CREAM, PINK, INK, YELLOW, BLUE, GREEN = (255, 248, 236), (253, 226, 221), (74, 48, 64), (255, 216, 107), (143, 196, 232), (127, 191, 106)
W, H, S = 1200, 320, 6


def pet(face_name):
    body = Image.open(os.path.join(SP, "body_cream_idle.png")).convert("RGBA").crop((0, 0, 48, 48))
    face = Image.open(os.path.join(SP, face_name)).convert("RGBA")
    p = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    p.alpha_composite(body)
    p.alpha_composite(face)
    return p


def sparkle(d, x, y, r, c):
    d.rectangle([x - r, y - 2, x + r, y + 2], fill=c)
    d.rectangle([x - 2, y - r, x + 2, y + r], fill=c)


def pixel_cloud(d, x, y, c):
    for dx, dy, w, h in ((0, 12, 96, 24), (16, 0, 48, 24), (48, 6, 40, 24)):
        d.rectangle([x + dx, y + dy, x + dx + w, y + dy + h], fill=c)


def envelope(d, x, y):
    d.rectangle([x, y, x + 72, y + 48], fill=(255, 253, 248), outline=INK, width=4)
    d.line([x, y, x + 36, y + 28], fill=INK, width=4)
    d.line([x + 72, y, x + 36, y + 28], fill=INK, width=4)
    d.rectangle([x + 30, y + 22, x + 42, y + 34], fill=(244, 166, 160))


def key(d, x, y):
    d.ellipse([x, y, x + 40, y + 40], outline=INK, fill=YELLOW, width=4)
    d.rectangle([x + 38, y + 16, x + 92, y + 26], fill=YELLOW, outline=INK, width=3)
    d.rectangle([x + 70, y + 26, x + 80, y + 40], fill=YELLOW, outline=INK, width=3)
    d.rectangle([x + 84, y + 26, x + 92, y + 36], fill=YELLOW, outline=INK, width=3)


def banner(name, face, prop):
    im = Image.new("RGB", (W, H), CREAM)
    d = ImageDraw.Draw(im)
    d.rectangle([0, H - 56, W, H], fill=GREEN)                   # grass strip
    d.rectangle([0, H - 56, W, H - 48], fill=(95, 154, 106))
    for x in range(8, W, 56):
        d.rectangle([x, H - 64, x + 8, H - 56], fill=GREEN)
    pixel_cloud(d, 80, 40, PINK)
    pixel_cloud(d, 900, 70, (232, 244, 252))
    rng = random.Random(7)
    for _ in range(14):
        sparkle(d, rng.randrange(40, W - 40, 8), rng.randrange(24, 200, 8), rng.choice((6, 8, 10)), YELLOW)
    p = pet(face).resize((48 * S, 48 * S), Image.NEAREST)
    im.paste(p, ((W - p.width) // 2, H - p.height - 12), p)
    prop(d)
    im.save(os.path.join(OUT, name))


os.makedirs(OUT, exist_ok=True)
banner("banner-confirm.png", "face_happy.png", lambda d: envelope(d, 820, 170))
banner("banner-reset.png", "face_look_down.png", lambda d: key(d, 820, 190))
small = pet("face_normal.png").crop((0, 4, 48, 48))
small.resize((small.width * 3, small.height * 3), Image.NEAREST).save(os.path.join(OUT, "mascot.png"))
print("written to", OUT)
