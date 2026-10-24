"""E1: 20 representative samples + contact sheet (try-on on pet.star_01). Not wired into manifest yet."""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

from common import ASSETS
import gear_wear
import gear_world

SAMPLES = [
    "equip.hat.straw_post", "equip.hat.knit_starlight", "equip.hat.beret_garden",
    "equip.outfit.vest_post", "equip.outfit.hoodie_winter", "equip.outfit.raincoat_rainy",
    "equip.accessory.scarf_seaside", "equip.accessory.bowtie_festival",
    "equip.feet.rain_boot_rainy", "equip.feet.sneaker_picnic",
    "equip.handheld.letter_post", "equip.handheld.bouquet_garden",
    "world.furniture.bed_starlight", "world.furniture.sofa_forest", "world.furniture.bookshelf_post",
    "world.decor.clock_garden", "item.collectible.shell_seaside", "item.stationery.wax_seal_festival",
    "world.weather.sunrise_window", "fx.heart_burst",
]
SP = os.path.join(ASSETS, "sprites", "pets", "star_01")
BG = (255, 248, 236, 255)


def render(key):
    if key == "world.weather.sunrise_window":
        return gear_world.weather_sunrise_window().composite(1).copy(), None
    if key == "fx.heart_burst":
        return gear_world.fx_heart_burst()[2].composite(1).copy(), None
    pre, name = key.rsplit(".", 1)[0], key.rsplit(".", 1)[1]
    typ, theme = name.rsplit("_", 1)
    reg = f"{pre}.{typ}"
    if reg in gear_wear.WEAR:
        return gear_wear.WEAR[reg][0](theme).composite(1).copy(), gear_wear.WEAR[reg][1]
    return gear_world.WORLD[reg](theme).composite(1).copy(), None


def pet():
    body = Image.open(os.path.join(SP, "body_cream_idle.png")).crop((0, 0, 48, 48))
    face = Image.open(os.path.join(SP, "face_normal.png"))
    p = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    p.alpha_composite(body); p.alpha_composite(face)
    return p


def try_on(img, slot):
    base = Image.new("RGBA", (48, 48), (0, 0, 0, 0)); base.alpha_composite(pet())
    base.alpha_composite(img)
    return base


def sheet(path):
    font = ImageFont.load_default(size=11)
    cell, cols = 200, 5
    rows = (len(SAMPLES) + cols - 1) // cols
    out = Image.new("RGBA", (cell * cols, (cell + 18) * rows), BG)
    d = ImageDraw.Draw(out)
    for i, key in enumerate(SAMPLES):
        im, slot = render(key)
        if slot:
            im = try_on(im, slot)
        w = im.width
        sc = max(1, min(cell // w, 8)) if w <= 96 else None
        view = im.resize((w * sc, w * sc), Image.NEAREST) if sc else im.resize((cell, cell), Image.NEAREST)
        x, y = (i % cols) * cell, (i // cols) * (cell + 18)
        d.rectangle([x, y, x + cell - 1, y + cell + 17], outline=(225, 210, 190, 255))
        out.alpha_composite(view, (x + (cell - view.width) // 2, y + (cell - view.height) // 2))
        d.text((x + 4, y + cell + 2), key.split(".", 1)[1], fill=(74, 48, 64, 255), font=font)
    out.convert("RGB").save(path)


if __name__ == "__main__":
    dest = os.path.join(ASSETS, "preview", "b1_samples_contact.png")
    sheet(dest)
    print(dest)
