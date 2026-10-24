"""Animation alignment test: for every pet animation frame, every wearable (with its tail) must stay attached
to the body when the frame's root/attach offsets are applied. Exit 1 on any detach."""
import json
import os
import sys

from PIL import Image

from common import ASSETS

m = json.load(open(os.path.join(ASSETS, "manifest.json"), encoding="utf-8"))
A, ANIMS = m["assets"], m["animations"]
img = {}


def load(key):
    if key not in img:
        img[key] = Image.open(os.path.join(ASSETS, A[key]["path"])).convert("RGBA")
    return img[key]


def mask(im, dx=0, dy=0, x0=0):
    return {(x + dx, y + dy) for y in range(im.height) for x in range(x0, x0 + 48) if im.getpixel((x, y))[3] for x in [x - x0]} \
        if False else {(x - x0 + dx, y + dy) for y in range(im.height) for x in range(x0, min(x0 + 48, im.width))
                       if im.getpixel((x, y))[3]}


def halo(ms, r=2):
    return {(x + a, y + b) for x, y in ms for a in range(-r, r + 1) for b in range(-r, r + 1)}


def main():
    gear = [k for k, a in A.items() if a.get("slot") in ("head", "body", "front", "feet", "hand", "costume", "costume_back", "back")]
    bad, checked = [], 0
    for an, anim in ANIMS.items():
        for i, f in enumerate(anim["frames"]):
            rx, ry = f.get("root", [0, 0]); ax, ay = f.get("attach", [0, 0])
            sheet = A[f"pet.star_01.body.cream.{f['body']['sheet']}"]
            body = mask(load(f"pet.star_01.body.cream.{f['body']['sheet']}"), rx, ry, f["body"]["index"] * 48)
            zone = halo(body)
            for g in gear:
                gm = mask(load(g), rx + ax, ry + ay)
                checked += 1
                if not (gm & zone):
                    bad.append(f"{an}[{i}] {g}: detached from body (root {rx, ry}, attach {ax, ay})")

    print(f"alignment: {len(ANIMS)} animations x {len(gear)} wearables = {checked} checks, {len(bad)} failures")
    for b in bad[:40]:
        print(" -", b)
    sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()
