"""Compose every animation from the manifest into contact sheets (alignment check) at 1x/integer scale."""
import json
import os
import sys

from PIL import Image, ImageDraw

from common import ASSETS, FRAME

SCALE = int(sys.argv[1]) if len(sys.argv) > 1 else 3
m = json.load(open(os.path.join(ASSETS, "manifest.json"), encoding="utf-8"))
A = m["assets"]
cache = {}


def load(key):
    if key not in cache:
        cache[key] = Image.open(os.path.join(ASSETS, A[key]["path"])).convert("RGBA")
    return cache[key]


def cell(key, i=0):
    im = load(key)
    return im.crop((i * FRAME, 0, (i + 1) * FRAME, FRAME))


def compose(prefix, color, f):
    out = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    rx, ry = f.get("root", [0, 0]); ax, ay = f.get("attach", [0, 0])
    body = f["body"]
    out.alpha_composite(cell(f"{prefix}.body.{color}.{body['sheet']}", body["index"]), (rx, ry)) if False else None
    layer = Image.new("RGBA", (FRAME * 3, FRAME * 3), (0, 0, 0, 0))
    o = FRAME   # padding so offsets never clip
    layer.alpha_composite(cell(f"{prefix}.body.{color}.{body['sheet']}", body["index"]), (o + rx, o + ry))
    layer.alpha_composite(cell(f"{prefix}.face.{f['face']}"), (o + rx + ax, o + ry + ay))
    for pr in f.get("props", []):
        layer.alpha_composite(cell(pr["key"], pr["index"]), (o + rx + ax, o + ry + ay))
    return layer.crop((o, o - 12, o + FRAME, o + FRAME))


def sheet(prefix, color, bg="#fff8ec"):
    anims = {k: v for k, v in m["animations"].items() if k.startswith(prefix + ".anim.")}
    maxf = max(len(v["frames"]) for v in anims.values())
    cw, ch = FRAME * SCALE, (FRAME + 12) * SCALE
    img = Image.new("RGBA", (maxf * cw + 140, len(anims) * ch), bg)
    d = ImageDraw.Draw(img)
    for r, (k, v) in enumerate(anims.items()):
        d.text((4, r * ch + 4), k.split(".anim.")[1], fill="#4a3040")
        for c, f in enumerate(v["frames"]):
            fr = compose(prefix, color, f).resize((cw, ch), Image.NEAREST)
            img.alpha_composite(fr, (140 + c * cw, r * ch))
            d.rectangle((140 + c * cw, r * ch, 140 + (c + 1) * cw - 1, (r + 1) * ch - 1), outline="#e3d4bd")
    return img


if __name__ == "__main__":
    out = os.path.join(ASSETS, "preview"); os.makedirs(out, exist_ok=True)
    for color in ("cream", "mint"):
        p = os.path.join(out, f"pet_star_01_{color}.png")
        sheet("pet.star_01", color).convert("RGB").save(p); print(p)
