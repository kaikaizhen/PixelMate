"""B2 wearables: crown, bucket hat, beret variants, cape, apron, earrings, glasses, clog, slipper, umbrella, canteen."""
import math

import gear_wear
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, BODY_D, BODY_L, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, THEMES,
                       new, edge, motif, pattern, blit, MOTIFS, body_inside)
from gear_wear import T, _hem, _pocket, HEM, POCKET


# ------------------------------------------------------------------ hats
def hat_crown(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    if theme == "post":                      # flat-banded crown with envelope jewel
        s.rect(13, 10, 34, 16, GOLD); s.rect(13, 10, 34, 11, "#fff0a8"); s.rect(13, 15, 34, 16, GOLD_D)
        for x in (14, 22, 30):
            s.polygon([(x, 10), (x + 2, 5), (x + 4, 10)], GOLD)
        s.rect(21, 12, 27, 15, PAPER); s.line(21, 12, 24, 14, m); s.line(27, 12, 24, 14, m)
    elif theme == "garden":                  # flower wreath, no points
        s.rect(13, 12, 34, 15, "#7fbf7a")
        for i, x in enumerate(range(14, 34, 4)):
            c = ("#f4a6a0", "#fff6e2", "#ffd86b")[i % 3]
            s.rect(x, 9 + (i % 2), x + 2, 11 + (i % 2), c); s.px(x + 1, 10 + (i % 2), GOLD_D)
        blit(s, 22, 6, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":                 # shell-point crown with pearl
        s.rect(13, 12, 34, 16, "#a8dcf0"); s.rect(13, 12, 34, 13, WHITE)
        for x in (13, 19, 25, 31):
            s.ellipse(x, 6, x + 5, 13, "#f3dca0"); s.line(x + 2, 7, x + 2, 12, "#d9b060")
        s.circle(24, 14, 1, WHITE)
    elif theme == "starlight":               # five star-tipped spires
        s.rect(13, 12, 34, 16, "#40448a"); s.rect(13, 12, 34, 13, "#6b6fb8")
        for x in (14, 19, 24, 29, 33):
            s.line(x + 1, 11, x + 1, 8, "#9fa4e6"); blit(s, x - 1, 4, MOTIFS["star"], GOLD)
        s.rect(23, 14, 25, 15, "#9fd8ff")
    else:                                    # forest: twig + leaf ring
        s.line(13, 14, 34, 14, WOOD); s.line(13, 15, 34, 15, WOOD_D)
        for i, x in enumerate(range(14, 34, 5)):
            blit(s, x, 8 - (i % 2), MOTIFS["leaf"], ("#5f9a6a", "#93c488")[i % 2])
        s.rect(22, 10, 25, 12, "#d9574f")
    edge(s)
    return s


def hat_bucket(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    brim = {"post": (9, 38, 17, 20), "garden": (8, 39, 17, 21), "seaside": (10, 37, 16, 19), "starlight": (7, 40, 16, 18)}[theme]
    x0, x1, y0, y1 = brim
    s.ellipse(x0, y0 - 2, x1, y1 + 1, m)                                    # brim
    s.ellipse(x0, y0 - 2, x1 - 1, y1 - 1, l, only=m)
    s.polygon([(15, 9), (32, 9), (36, 17), (11, 17)], m)                    # crown, slightly tapered
    s.polygon([(15, 9), (22, 9), (19, 17), (11, 17)], l)
    s.rect(11, 14, 36, 16, d)                                               # band
    pattern(s, (13, 9, 34, 13), t["pattern"], (m, d, l, a), only=m)
    if theme == "post":
        s.rect(34, 17, 38, 22, PAPER); s.px(36, 19, d)                       # tag
    elif theme == "garden":
        for x in range(10, 40, 5):                                           # frilly brim
            s.px(x, 20, l); s.px(x + 1, 21, l)
        blit(s, 29, 8, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":
        for x in range(12, 36, 4):
            s.line(x, 14, x + 1, 16, WHITE)
        motif(s, 24, 15, "wave", WHITE)
        s.line(34, 16, 38, 22, m)                                            # chin cord
    else:
        blit(s, 22, 5, MOTIFS["star"], GOLD)
    edge(s)
    return s


_beret = gear_wear.hat_beret


def hat_beret2(theme):
    s = _beret(theme)
    (m, d, l, a), t = T(theme)
    if theme == "seaside":                    # sailor pom + stripe band
        s.circle(24, 6, 2, "#d9574f"); s.rect(12, 17, 36, 18, WHITE)
    elif theme == "starlight":                # crescent badge
        s.circle(29, 14, 3, GOLD); s.circle(30, 13, 3, m)
    elif theme == "forest":                   # mushroom-cap spots + feather
        s.line(34, 9, 38, 3, "#a8764a"); blit(s, 33, 3, MOTIFS["leaf"], "#93c488")
    edge(s)
    return s


# ------------------------------------------------------------------ outfits
def outfit_cape(theme):
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    for y in range(32, 46):
        for x in range(48):
            if body_inside(x, y, 0):
                s.px(x, y, m)
    for y in range(32, 46):
        for x in range(48):
            if s.get(x, y) == m and (x < 12 or x > 36):
                s.px(x, y, d if x > 36 else l)
    pattern(s, (2, 32, 45, 45), t["pattern"], pal, only=m)
    s.line(24, 33, 24, 45, d)                                               # front opening
    s.rect(18, 32, 30, 34, l, only=m)                                       # collar
    s.rect(22, 34, 26, 36, GOLD); s.px(23, 35, WHITE)                       # clasp
    motif(s, 14, 40, t["motif"], a); motif(s, 34, 40, t["motif"], a)
    _hem(s, {"post": "zigzag", "picnic": "scallop", "rainy": "drip", "starlight": "zigzag", "forest": "jagged",
             "winter": "fluff"}[theme], pal, 43, 47)
    if theme == "winter":
        s.rect(10, 32, 38, 33, WHITE, only=m)
    edge(s)
    return s


# ------------------------------------------------------------------ accessories
def acc_earring(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (4, 42):
        s.line(x0, 28, x0, 30, GOLD_D)                                      # hook
        cx, cy = x0, 33
        if theme == "friendship":
            blit(s, cx - 2, cy - 2, MOTIFS["heart"], m)
        elif theme == "garden":
            blit(s, cx - 2, cy - 2, MOTIFS["flower"], "#f4a6a0"); s.px(cx, cy, GOLD)
        elif theme == "seaside":
            s.circle(cx, cy + 1, 2, WHITE); s.px(cx - 1, cy, "#a8dcf0")
        elif theme == "starlight":
            blit(s, cx - 2, cy - 2, MOTIFS["star"], GOLD)
        elif theme == "forest":
            blit(s, cx - 2, cy - 2, MOTIFS["leaf"], "#5f9a6a")
        else:                                                                 # festival tassel
            s.rect(cx - 1, cy - 1, cx + 1, cy + 1, m); s.line(cx - 1, cy + 2, cx - 1, cy + 6, a); s.line(cx + 1, cy + 2, cx + 1, cy + 6, l)
    edge(s)
    return s


def acc_glasses(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    centres = ((15, 26), (32, 26))
    for cx, cy in centres:
        if theme == "friendship":
            blit(s, cx - 3, cy - 3, [".##.##.", "#.###.#", "#.....#", ".#...#.", "..#.#..", "...#..."], m)
        elif theme == "garden":
            s.circle(cx, cy, 5, m, fill=False); s.px(cx + 4, cy - 4, "#f4a6a0")
        elif theme == "seaside":
            s.rect(cx - 5, cy - 3, cx + 5, cy + 3, m, fill=False); s.rect(cx - 4, cy - 2, cx + 4, cy - 2, "#a8dcf0")
        elif theme == "starlight":
            blit(s, cx - 3, cy - 3, ["...#...", "..#.#..", "###.###", ".#...#.", "#.....#", "#.#.#.#"], GOLD)
        else:
            s.rect(cx - 5, cy - 4, cx + 5, cy + 4, WOOD_D, fill=False); s.px(cx - 5, cy - 4, "#5f9a6a"); s.px(cx + 5, cy + 4, "#5f9a6a")
    s.line(21, 25, 26, 25, m if theme != "forest" else WOOD_D)              # bridge
    s.line(10, 25, 6, 28, m); s.line(37, 25, 41, 28, m)                     # arms
    edge(s)
    return s


# ------------------------------------------------------------------ feet
def feet_clog(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (11, 26):
        s.ellipse(x0, 42, x0 + 11, 47, WOOD)
        s.ellipse(x0 + 4, 42, x0 + 11, 47, WOOD_L, only=WOOD)
        s.rect(x0, 45, x0 + 11, 47, WOOD_D)
        s.rect(x0 + 2, 42, x0 + 8, 44, m)                                    # strap band
        pattern(s, (x0 + 2, 42, x0 + 8, 44), t["pattern"], (m, d, l, a), only=m)
        if theme == "rainy":                                                 # rubber toe cap + drop
            s.ellipse(x0 + 7, 43, x0 + 11, 47, d); s.px(x0 + 4, 45, a)
        elif theme == "winter":                                              # wool lining peeking out
            s.rect(x0 + 1, 41, x0 + 10, 42, WHITE); s.px(x0 + 10, 41, WHITE); s.px(x0, 41, WHITE)
        else:                                                                # picnic: heel strap + gingham
            s.line(x0, 44, x0 + 1, 41, m); s.rect(x0 + 8, 44, x0 + 10, 45, l)
    edge(s)
    return s


def feet_slipper(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (11, 26):
        s.ellipse(x0, 41, x0 + 11, 47, m)
        s.ellipse(x0 + 1, 41, x0 + 7, 44, l, only=m)
        s.rect(x0 + 1, 41, x0 + 10, 42, WHITE)                               # fluffy rim
        if theme == "picnic":
            blit(s, x0 + 4, 43, MOTIFS["check"], WHITE)
        else:                                                                 # rainy: bunny-ear bow tied on toe
            s.polygon([(x0 + 5, 43), (x0 + 3, 40), (x0 + 6, 42)], a); s.polygon([(x0 + 7, 43), (x0 + 9, 40), (x0 + 6, 42)], a)
    edge(s)
    return s


# ------------------------------------------------------------------ handheld
def held_umbrella(theme):
    """Held low on the right so the canopy never covers the face (canopy y >= 35)."""
    (m, d, l, a), t = T(theme)
    s = new()
    top = {"post": "#fff6e2", "garden": "#b5e2a0", "starlight": "#40448a", "adventure": "#e8cfa0"}[theme]
    s.polygon([(32, 40), (47, 40), (45, 35), (39, 33), (34, 35)], top)
    for x in (35, 37, 39, 42, 45):
        s.line(39, 33, x, 40, d)
    for x in range(32, 48, 4):                                              # scalloped hem
        s.ellipse(x, 39, x + 4, 42, top)
    if theme == "post":
        for x in (34, 39, 44):
            s.rect(x, 35, x + 1, 39, m)
    elif theme == "garden":
        motif(s, 39, 37, "flower", "#f4a6a0")
    elif theme == "starlight":
        for x, y in ((35, 37), (40, 36), (44, 38)):
            s.px(x, y, GOLD)
    else:
        motif(s, 39, 37, "compass", "#9a6a2a")
    s.line(39, 42, 39, 46, WOOD_D); s.line(38, 46, 36, 47, WOOD_D)
    s.shift(-3, 0)                                                          # keep the hem inside the 48px frame
    edge(s)
    return s


def held_canteen(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    if theme == "post":                       # tall flask with letter label
        s.rect(36, 30, 45, 45, "#8a8aa0"); s.rect(36, 30, 37, 45, "#b8b8cc"); s.rect(38, 27, 43, 30, WOOD_D)
        s.rect(38, 34, 43, 40, PAPER); s.px(40, 36, m)
    elif theme == "garden":                   # round canteen with cork + strap
        s.circle(38, 38, 7, "#7fbf7a"); s.circle(36, 35, 3, "#b5e2a0", only="opaque")
        s.rect(36, 28, 40, 31, WOOD_L); s.line(32, 33, 44, 33, "#a8764a")
        blit(s, 36, 37, MOTIFS["leaf"], "#3a6a49")
    else:                                     # starlight: stoppered bottle with stars
        s.polygon([(34, 45), (43, 45), (43, 36), (40, 32), (40, 28), (37, 28), (37, 32), (34, 36)], "#6b6fb8")
        s.rect(37, 25, 40, 28, WOOD_L); s.px(36, 40, GOLD); s.px(40, 38, GOLD); s.px(38, 42, GOLD)
    edge(s)
    return s


WEAR2 = {
    "equip.hat.crown": (hat_crown, "head"), "equip.hat.bucket": (hat_bucket, "head"),
    "equip.outfit.cape": (outfit_cape, "body"),
    "equip.accessory.earring": (acc_earring, "front"), "equip.accessory.glasses": (acc_glasses, "front"),
    "equip.feet.clog": (feet_clog, "feet"), "equip.feet.slipper": (feet_slipper, "feet"),
    "equip.handheld.umbrella": (held_umbrella, "hand"), "equip.handheld.canteen": (held_canteen, "hand"),
}
gear_wear.WEAR.update(WEAR2)
gear_wear.WEAR["equip.hat.beret"] = (hat_beret2, "head")
