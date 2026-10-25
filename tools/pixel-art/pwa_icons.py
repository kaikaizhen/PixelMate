"""PWA icons for apps/pwa from the star pet (pet.star_01): integer nearest-neighbour upscales on the cream background."""
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SP = os.path.join(ROOT, "assets", "sprites", "pets", "star_01")
OUT = os.path.join(ROOT, "apps", "pwa", "public", "icons")
PIXEL = os.path.join(ROOT, "apps", "pwa", "src", "assets", "pixel")
CREAM = (255, 248, 236, 255)

body = Image.open(os.path.join(SP, "body_cream_idle.png")).convert("RGBA").crop((0, 0, 48, 48))
face = Image.open(os.path.join(SP, "face_normal.png")).convert("RGBA")
pet = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
pet.alpha_composite(body)
pet.alpha_composite(face)
bbox = pet.getbbox()
pet = pet.crop(bbox)


def icon(size, scale, path):
    im = Image.new("RGBA", (size, size), CREAM)
    p = pet.resize((pet.width * scale, pet.height * scale), Image.NEAREST)
    im.alpha_composite(p, ((size - p.width) // 2, (size - p.height) // 2))
    im.convert("RGB").save(path)


os.makedirs(OUT, exist_ok=True)
os.makedirs(PIXEL, exist_ok=True)
icon(192, 3, os.path.join(OUT, "icon-192.png"))
icon(512, 8, os.path.join(OUT, "icon-512.png"))
icon(512, 6, os.path.join(OUT, "maskable-512.png"))      # extra padding: the safe zone is the inner 80 %
icon(180, 3, os.path.join(OUT, "apple-touch-icon.png"))
fav = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
fav.alpha_composite(pet.resize((pet.width * 32 // pet.width * 0 + 32, 32 * pet.height // pet.width), Image.NEAREST), (0, 0))
fav.save(os.path.join(OUT, "favicon-32.png"))
for name in ("body_cream_idle.png", "face_normal.png"):      # mascot used by the login pages
    Image.open(os.path.join(SP, name)).save(os.path.join(PIXEL, name))
print("icons written to", OUT)
