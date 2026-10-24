"""Review sheets for one batch: python sheet4.py B4  -> assets/preview/<batch>_wear|furn|small|weather.png"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

from common import ASSETS
from gear_sheet import sheet as wear_sheet

batch = sys.argv[1] if len(sys.argv) > 1 else "B4"
m = json.load(open(os.path.join(ASSETS, "manifest.json"), encoding="utf-8"))["assets"]
font = ImageFont.load_default(size=10)
P = os.path.join(ASSETS, "preview")
mine = {k: a for k, a in m.items() if a.get("batch") == batch}


def sheet(keys, path, cell, cols, sc):
    rows = (len(keys) + cols - 1) // cols
    out = Image.new("RGBA", (cell * cols, (cell + 16) * rows), (255, 248, 236, 255))
    d = ImageDraw.Draw(out)
    for i, k in enumerate(keys):
        a = m[k]
        im = Image.open(os.path.join(ASSETS, a["path"])).convert("RGBA").crop((0, 0, a["frameWidth"], a["frameHeight"]))
        z = sc(a)
        v = im.resize((int(im.width * z), int(im.height * z)), Image.NEAREST)
        x, y = (i % cols) * cell, (i // cols) * (cell + 16)
        out.alpha_composite(v, (x + (cell - v.width) // 2, y + (cell - v.height) // 2))
        d.text((x + 3, y + cell + 2), k.split(".", 1)[1], fill=(74, 48, 64, 255), font=font)
    out.convert("RGB").save(path)


wear = sorted(k for k, a in mine.items() if a.get("slot") in ("head", "body", "front", "feet", "hand", "costume"))
wear_sheet(m, wear, os.path.join(P, f"{batch.lower()}_wear.png"), cols=8)
sheet(sorted(k for k in mine if k.startswith("world.furniture")), os.path.join(P, f"{batch.lower()}_furn.png"), 190, 5, lambda a: 2)
sheet(sorted(k for k in mine if k.startswith(("world.decor", "item.", "fx."))), os.path.join(P, f"{batch.lower()}_small.png"), 100, 10,
      lambda a: 2 if a["frameWidth"] == 48 else 3)
sheet(sorted(k for k in mine if k.startswith("world.weather")), os.path.join(P, f"{batch.lower()}_weather.png"), 250, 4, lambda a: 1)
print(len(wear), "wearables")
