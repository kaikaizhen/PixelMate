"""Contact sheets for non-wearable assets (furniture / decor / items+mail+fx / weather)."""
import json
import os

from PIL import Image, ImageDraw, ImageFont

from common import ASSETS

m = json.load(open(os.path.join(ASSETS, "manifest.json"), encoding="utf-8"))["assets"]
font = ImageFont.load_default(size=10)


def sheet(keys, path, cell, cols, scale):
    rows = (len(keys) + cols - 1) // cols
    out = Image.new("RGBA", (cell * cols, (cell + 16) * rows), (255, 248, 236, 255))
    d = ImageDraw.Draw(out)
    for i, k in enumerate(keys):
        a = m[k]
        im = Image.open(os.path.join(ASSETS, a["path"])).convert("RGBA").crop((0, 0, a["frameWidth"], a["frameHeight"]))
        v = im.resize((int(im.width * scale(a)), int(im.height * scale(a))), Image.NEAREST)
        x, y = (i % cols) * cell, (i // cols) * (cell + 16)
        out.alpha_composite(v, (x + (cell - v.width) // 2, y + (cell - v.height) // 2))
        d.text((x + 3, y + cell + 2), k.split(".", 1)[1], fill=(74, 48, 64, 255), font=font)
    out.convert("RGB").save(path)


P = os.path.join(ASSETS, "preview")
sheet(sorted(k for k in m if k.startswith("world.furniture")), P + "/b1_world_furniture.png", 200, 5, lambda a: 2)
sheet(sorted(k for k in m if k.startswith("world.decor")), P + "/b1_world_decor.png", 150, 6, lambda a: 3)
sheet(sorted(k for k in m if k.startswith(("item.", "fx."))), P + "/b1_items_mail_fx.png", 110, 8,
      lambda a: 4 if a["frameWidth"] == 24 else 2)
sheet(sorted(k for k in m if k.startswith("world.weather")), P + "/b1_weather.png", 250, 4, lambda a: 1)
