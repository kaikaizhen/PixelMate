"""Shared drawing kit for equipment / room assets (star pet rig, 48x48 frame, anchor [24,46]).

A *theme* is not a recolor: it chooses palette + motif (5x5 emblem) + pattern + silhouette
parameters, so two themes of one garment differ in at least two visible ways.
"""
import os

from PIL import Image

from common import ASSETS, FRAME, hard_alpha  # noqa: F401
from pixelstudio import Sprite

_circle = Sprite.circle
Sprite.circle = lambda self, cx, cy, r, color, fill=True, only=None: _circle(self, cx, cy, r, color, fill, only)  # filled by default

LINE = "#4a3040"                  # same outline as pet.star_01
BODY, BODY_D, BODY_L = "#fbe3c8", "#efc3a4", "#fff4e4"
WHITE, PAPER, PAPER_D = "#ffffff", "#fff6e2", "#d9c9ac"
GOLD, GOLD_D = "#ffd86b", "#f2b84a"
WOOD, WOOD_D, WOOD_L = "#b98a5e", "#8a6040", "#d9b088"

# main, dark, light, accent
THEMES = {
    "post":       dict(pal=("#d9574f", "#a63b3c", "#f08a7e", "#fff6e2"), motif="stamp",  pattern="stripe_d", tail="tag"),
    "garden":     dict(pal=("#7fbf7a", "#4f8f58", "#b5e2a0", "#f4a6a0"), motif="flower", pattern="dots",     tail="flowers"),
    "seaside":    dict(pal=("#6fb1d6", "#3f7fa8", "#a8dcf0", "#f3dca0"), motif="wave",   pattern="stripe_h",  tail="ribbon"),
    "starlight":  dict(pal=("#6b6fb8", "#40448a", "#9fa4e6", "#ffd86b"), motif="star",   pattern="stars",     tail="star"),
    "forest":     dict(pal=("#5f9a6a", "#3a6a49", "#93c488", "#a8764a"), motif="leaf",   pattern="chevron",   tail="leaf"),
    "winter":     dict(pal=("#a8c8e8", "#7898c0", "#e6f2ff", "#d9574f"), motif="snow",   pattern="dots",      tail="fluff"),
    "picnic":     dict(pal=("#e8736a", "#b84a4a", "#ff9e90", "#fff6e2"), motif="check",  pattern="check",     tail="napkin"),
    "rainy":      dict(pal=("#7f9fb8", "#566f8a", "#aac4d8", "#f2c94c"), motif="drop",   pattern="stripe_v",  tail="drops"),
    "festival":   dict(pal=("#e86a9a", "#b8406e", "#ff9fc2", "#ffd86b"), motif="heart",  pattern="stars",     tail="bunting"),
    "friendship": dict(pal=("#f08fb0", "#c0608a", "#ffc0d6", "#8fc4e8"), motif="knot",   pattern="dots",      tail="ribbon"),
    "adventure":  dict(pal=("#d49a4a", "#9a6a2a", "#f0c070", "#6fb1d6"), motif="compass", pattern="chevron",  tail="rope"),
}

MOTIFS = {
    "stamp":   ["#####", "#...#", "#.#.#", "#...#", "#####"],
    "flower":  ["#.#.#", ".###.", "#####", ".###.", "#.#.#"],
    "wave":    ["#.#.#", ".#.#.", "#.#.#", ".....", "....."],
    "star":    ["..#..", ".###.", "#####", ".###.", ".#.#."],
    "leaf":    ["...##", "..###", ".###.", "###..", "#...."],
    "snow":    ["..#..", "#.#.#", ".###.", "#.#.#", "..#.."],
    "check":   ["#.#.#", ".#.#.", "#.#.#", ".#.#.", "#.#.#"],
    "drop":    ["..#..", ".###.", "#####", "#####", ".###."],
    "heart":   [".#.#.", "#####", "#####", ".###.", "..#.."],
    "knot":    ["#...#", ".#.#.", "..#..", ".#.#.", "#...#"],
    "compass": ["..#..", ".#.#.", "#.#.#", ".#.#.", "..#.."],
}

_body_cache = {}


def new(w=FRAME, h=FRAME):
    return Sprite(w, h)


def blit(s, x, y, rows, color, only=None):
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch == "#":
                if only is None or s.get(x + i, y + j) == _norm(only):
                    s.px(x + i, y + j, color)


def _norm(c):
    return c


def motif(s, cx, cy, name, color):
    rows = MOTIFS[name]
    blit(s, cx - len(rows[0]) // 2, cy - len(rows) // 2, rows, color)


def pattern(s, box, name, pal, only=None):
    """Fill a pattern into box (x0,y0,x1,y1); `only` clips to existing pixels of that colour."""
    x0, y0, x1, y1 = box
    main, dark, light, accent = pal
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c = None
            if name == "stripe_h" and y % 4 in (0, 1):
                c = light
            elif name == "stripe_v" and x % 4 in (0, 1):
                c = light
            elif name == "stripe_d" and (x + y) % 6 in (0, 1):
                c = accent
            elif name == "dots" and x % 4 == 1 and y % 4 == 1:
                c = accent
            elif name == "check" and ((x // 3) + (y // 3)) % 2 == 0:
                c = light
            elif name == "stars" and x % 6 == 2 and y % 6 == 2:
                c = accent
            elif name == "chevron" and (x + (y % 6 if y % 6 < 3 else 6 - y % 6)) % 6 == 0:
                c = light
            if c and (only is None or s.get(x, y) == only):
                s.px(x, y, c)


def edge(s):
    s.outline(LINE, where="outside")


def body_mask():
    """Alpha mask of the pet's body (idle frame 0) so garments hug the real silhouette."""
    if "m" not in _body_cache:
        im = Image.open(os.path.join(ASSETS, "sprites", "pets", "star_01", "body_cream_idle.png")).crop((0, 0, 48, 48))
        _body_cache["m"] = im
    return _body_cache["m"]


def body_inside(x, y, inset=1):
    m = body_mask()
    for dx in range(-inset, inset + 1):
        for dy in range(-inset, inset + 1):
            xx, yy = x + dx, y + dy
            if not (0 <= xx < 48 and 0 <= yy < 48) or m.getpixel((xx, yy))[3] == 0:
                return False
    return True


def finish(s):
    """Return a hard-alpha RGBA Image copy of the composite."""
    return hard_alpha(s.composite(1).copy())
