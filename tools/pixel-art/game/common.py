"""Shared helpers for the game asset pipeline (stage builders register entries here)."""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from PIL import Image  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
ASSETS = os.path.join(ROOT, "assets")
FRAME = 48
CANVAS = 240
ANCHOR = [24, 46]          # feet centre of a 48x48 frame; shared by every pet layer


def path(*parts):
    p = os.path.join(ASSETS, *parts)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    return p


def write_strip(sprites, rel, w=FRAME, h=FRAME):
    """Save Sprite objects (or RGBA images) side by side as one transparent PNG."""
    n = len(sprites)
    im = Image.new("RGBA", (w * n, h), (0, 0, 0, 0))
    for k, sp in enumerate(sprites):
        frame = sp if isinstance(sp, Image.Image) else sp.composite(1)
        im.alpha_composite(frame, (k * w, 0))
    im.save(path(*rel.split("/")))
    return n


def hard_alpha(im, threshold=128):
    """No semi-transparent pixels: asset rule (sharp pixel edges)."""
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= threshold else (0, 0, 0, 0)
    return im


def recolor(im, mapping):
    out = im.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            key = "#%02x%02x%02x" % (r, g, b)
            if a and key in mapping:
                nr, ng, nb = mapping[key]
                px[x, y] = (nr, ng, nb, a)
    return out


def hex2rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def save_json(obj, rel):
    with open(path(*rel.split("/")), "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
        f.write("\n")
