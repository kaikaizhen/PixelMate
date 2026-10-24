"""Try-on contact sheets: every equipment asset in the manifest drawn on pet.star_01 (idle frame 0)."""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

from common import ASSETS, FRAME

SC = 3
BG = (255, 248, 236, 255)


def pet(m):
    body = Image.open(os.path.join(ASSETS, m["pet.star_01.body.cream.idle"]["path"])).convert("RGBA").crop((0, 0, 48, 48))
    face = Image.open(os.path.join(ASSETS, m["pet.star_01.face.normal"]["path"])).convert("RGBA")
    p = Image.new("RGBA", (48, 48), (0, 0, 0, 0)); p.alpha_composite(body); p.alpha_composite(face)
    return p


def wear(m, keys):
    """Layer several wearables in z order on the pet."""
    im = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    for k in keys:                                                        # back parts (tails) go behind the body
        bk = m[k].get("backKey")
        if bk:
            im.alpha_composite(Image.open(os.path.join(ASSETS, m[bk]["path"])).convert("RGBA"))
    im.alpha_composite(pet(m))
    for k in sorted(keys, key=lambda k: m[k]["z"]):
        im.alpha_composite(Image.open(os.path.join(ASSETS, m[k]["path"])).convert("RGBA"))
    return im


def sheet(m, keys, path, cols=6, title=""):
    font = ImageFont.load_default(size=10)
    cw, ch = 48 * SC + 8, 48 * SC + 20
    rows = (len(keys) + cols - 1) // cols
    out = Image.new("RGBA", (cw * cols, ch * rows), BG)
    d = ImageDraw.Draw(out)
    for i, k in enumerate(keys):
        x, y = (i % cols) * cw, (i // cols) * ch
        im = wear(m, [k]).resize((48 * SC, 48 * SC), Image.NEAREST)
        out.alpha_composite(im, (x + 4, y + 2))
        d.text((x + 4, y + 48 * SC + 5), k.split(".", 2)[2], fill=(74, 48, 64, 255), font=font)
    out.convert("RGB").save(path)


if __name__ == "__main__":
    m = json.load(open(os.path.join(ASSETS, "manifest.json"), encoding="utf-8"))["assets"]
    groups = {}
    for k, a in m.items():
        if a.get("slot") in ("head", "body", "front", "feet", "hand", "costume"):
            groups.setdefault(a["slot"], []).append(k)
    for slot, keys in groups.items():
        p = os.path.join(ASSETS, "preview", f"b1_wear_{slot}.png")
        sheet(m, sorted(keys), p)
        print(p, len(keys))
