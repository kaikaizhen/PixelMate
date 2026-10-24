"""B5 wearables: flower hat, rain hat, explorer hat themes, sweater themes, workwear, explorer jacket, sash themes,
wings (back layer), warm socks, trail boot, snack boxes, binoculars."""
import math

import gear_wear
import gear_wear4
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, new, edge, motif, pattern,
                       blit, MOTIFS, body_inside)
from gear_wear import T
from gear_wear4 import _fill

DENIM = {"post": ("#3f4d8f", "#26305a", "#6f7fc6"), "picnic": ("#e8736a", "#b84a4a", "#ff9e90"), "rainy": ("#d4a84a", "#a8801e", "#f0cc70"),
         "starlight": ("#40448a", "#27295a", "#6b6fb8"), "forest": ("#5f7f58", "#3a5a3c", "#8aaa7c"), "winter": ("#7898c0", "#566f98", "#a8c8e8")}


# ------------------------------------------------------------------ hats
def hat_flower(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    cx, cy = 24, 11
    n = {"post": 6, "garden": 8, "seaside": 7, "starlight": 5, "forest": 6}[theme]
    for i in range(n):                                                     # petals
        ang = math.radians(-90 + i * 360 / n)
        px_, py_ = cx + 9 * math.cos(ang), cy + 5.5 * math.sin(ang)
        if theme == "starlight":
            s.polygon([(cx, cy), (px_ - 2, py_), (px_, py_ - 3), (px_ + 2, py_)], m)
        elif theme == "forest":
            blit(s, int(px_) - 2, int(py_) - 2, MOTIFS["leaf"], ("#5f9a6a", "#93c488")[i % 2])
        else:
            s.circle(int(px_), int(py_), 3, m if i % 2 == 0 else l)
    s.ellipse(15, 13, 33, 18, d)                                           # cap under the petals
    s.ellipse(15, 13, 31, 16, m if theme != "forest" else "#5f9a6a", only="opaque")
    c = {"post": PAPER, "garden": "#ffd86b", "seaside": WHITE, "starlight": GOLD, "forest": "#a8764a"}[theme]
    s.circle(cx, cy, 3, c)
    if theme == "post":
        s.line(22, 10, 24, 12, "#d9574f"); s.line(26, 10, 24, 12, "#d9574f")
    elif theme == "garden":
        s.px(23, 10, GOLD_D); s.px(25, 12, GOLD_D)
    elif theme == "seaside":
        s.px(24, 11, "#a8dcf0")
    edge(s)
    return s


def hat_rain(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    if theme in ("post", "seaside"):                                       # sou'wester with a long back flap
        s.ellipse(11, 5, 36, 17, m); s.ellipse(11, 5, 32, 13, l, only=m)
        s.polygon([(8, 15), (38, 15), (41, 22), (6, 22)], m); s.line(6, 22, 41, 22, d)
        s.rect(11, 13, 36, 15, d)
        if theme == "post":
            s.rect(20, 7, 27, 12, PAPER); s.line(20, 7, 23, 10, "#d9574f"); s.line(27, 7, 24, 10, "#d9574f")
        else:
            motif(s, 24, 9, "wave", WHITE)
        s.line(36, 17, 40, 23, "#a8764a")
    elif theme in ("garden", "forest"):                                    # big leaf umbrella hat
        s.ellipse(6, 2, 42, 15, m if theme == "forest" else "#7fbf7a"); s.ellipse(6, 2, 38, 11, l, only="opaque")
        s.line(24, 3, 24, 14, d)
        for x in (12, 17, 31, 36):
            s.line(24, 14, x, 6, d)
        s.rect(21, 14, 27, 17, "#a8764a")
        if theme == "garden":
            s.circle(10, 12, 2, "#f4a6a0")
    else:                                                                  # starlight: storm-cloud hat with a drip
        for cx, cy, r in ((14, 11, 5), (22, 7, 6), (32, 9, 6), (38, 13, 4)):
            s.circle(cx, cy, r, "#6b6fb8")
        s.rect(10, 11, 41, 16, "#40448a")
        blit(s, 20, 9, MOTIFS["star"], GOLD)
        s.px(14, 19, "#8fd0f0"); s.px(24, 20, "#8fd0f0"); s.px(35, 19, "#8fd0f0")
    edge(s)
    return s


_exp = gear_wear4.hat_explorer


def hat_explorer5(theme):
    if theme not in ("starlight", "forest"):
        return _exp(theme)
    (m, d, l, a), t = T(theme)
    s = new()
    if theme == "starlight":                                               # astronomer's pith helmet with a lens
        s.ellipse(10, 13, 38, 20, "#40448a"); s.ellipse(14, 4, 34, 17, "#6b6fb8"); s.rect(14, 12, 34, 14, "#ffd86b")
        s.circle(24, 8, 3, "#9fd8ff"); s.circle(24, 8, 3, "#27295a", fill=False); blit(s, 14, 5, MOTIFS["star"], GOLD)
    else:                                                                  # forest ranger campaign hat
        s.ellipse(8, 14, 40, 21, "#6e5a3a"); s.rect(15, 5, 33, 16, "#8a7448"); s.rect(15, 5, 33, 7, "#a08a58")
        s.polygon([(15, 5), (33, 5), (30, 9), (18, 9)], "#6e5a3a"); s.rect(15, 13, 33, 15, "#3a5a3c")
        s.rect(20, 12, 28, 16, "#c9a14a"); blit(s, 22, 10, MOTIFS["leaf"], "#93c488")
    edge(s)
    return s


# ------------------------------------------------------------------ outfits
_sw = gear_wear4.outfit_sweater


def outfit_sweater5(theme):
    if theme in ("post", "picnic", "rainy", "starlight"):
        return _sw(theme)
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    _fill(s, 32, 46, pal)
    for x in range(2, 46, 3):
        for y in range(34, 46):
            if s.get(x, y) in (m, l, d) and (y // 2 + x // 3) % 2 == 0:
                s.px(x, y, l if s.get(x, y) != l else m)
    if theme == "forest":                                                  # shawl-collar cardigan with wooden toggles
        s.polygon([(14, 32), (23, 32), (22, 41)], d); s.polygon([(33, 32), (25, 32), (26, 41)], d)
        s.line(24, 34, 24, 45, d)
        for y in (38, 42):
            s.rect(22, y, 26, y + 1, "#a8764a")
        blit(s, 8, 40, MOTIFS["leaf"], "#93c488"); blit(s, 33, 40, MOTIFS["leaf"], "#93c488")
    else:                                                                  # winter: chunky cable-knit turtleneck, snow yoke
        s.rect(15, 32, 32, 35, WHITE); s.line(15, 35, 32, 35, l)
        for x in (17, 22, 27, 32):
            s.line(x, 36, x, 44, WHITE)
        blit(s, 6, 38, MOTIFS["snow"], WHITE); blit(s, 36, 38, MOTIFS["snow"], WHITE)
    for x in range(3, 45, 3):
        s.px(x, 45, d)
    edge(s)
    return s


def outfit_workwear(theme):
    """Dungarees: bib with chest pocket, two straps that start at the shoulder edges, belly pants with cuffs."""
    (m, d, l, a), t = T(theme)
    c, cd, cl = DENIM[theme]
    s = new()
    for y in range(33, 47):
        for x in range(48):
            if (y <= 38 and 16 <= x <= 31) or (y > 38 and body_inside(x, y, 1)):
                if body_inside(x, y, 1) or y > 44:
                    s.px(x, y, c)
    for y in range(33, 47):
        for x in range(48):
            if s.get(x, y) == c and x > 33:
                s.px(x, y, cd)
    for sx, bx in ((7, 16), (40, 31)):                                     # suspender strap: straight from the shoulder to the bib corner
        s.line(sx, 25, bx, 35, c); s.line(sx + (1 if sx < 20 else -1), 25, bx + (1 if sx < 20 else -1), 35, c)
        s.px(sx + (1 if sx < 20 else -1), 27, GOLD)                           # shoulder button
        s.px(bx, 36, GOLD)
    s.rect(19, 35, 28, 38, cd); s.rect(20, 36, 27, 37, cl)                 # chest pocket
    s.rect(2, 39, 45, 39, cd, only=c)                                      # waist seam
    s.rect(22, 40, 25, 44, cd)                                             # fly seam
    for x in range(4, 44, 4):                                              # stitching
        s.px(x, 41, cl)
    if theme == "post":
        s.rect(21, 36, 26, 37, PAPER)
    elif theme == "picnic":
        pattern(s, (3, 40, 44, 46), "check", (c, cd, cl, a), only=c)
    elif theme == "rainy":
        s.rect(2, 44, 45, 46, "#f2c94c", only=c); s.px(24, 36, "#1c1720")
    elif theme == "starlight":
        blit(s, 22, 35, MOTIFS["star"], GOLD); pattern(s, (3, 40, 44, 46), "stars", (c, cd, cl, GOLD), only=c)
    elif theme == "forest":
        blit(s, 22, 35, MOTIFS["leaf"], "#93c488"); s.rect(8, 42, 12, 45, "#a8764a"); s.rect(35, 42, 39, 45, "#a8764a")   # patches
    else:
        s.rect(2, 44, 45, 46, WHITE, only=c); blit(s, 22, 35, MOTIFS["snow"], WHITE)
    edge(s)
    return s


def outfit_explorer_jacket(theme):
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    _fill(s, 33, 46, pal)
    s.polygon([(13, 33), (22, 33), (23, 40)], d); s.polygon([(34, 33), (25, 33), (24, 40)], d)      # big collar flaps
    s.line(24, 34, 24, 45, d)
    s.rect(5, 33, 10, 35, l); s.rect(37, 33, 42, 35, l)                                              # epaulettes
    s.rect(2, 41, 45, 42, "#6e4c30", only=m); s.rect(22, 40, 26, 43, GOLD)                            # belt
    for x0 in (11, 29):                                                                              # flap pockets
        s.rect(x0, 36, x0 + 7, 40, d); s.rect(x0, 36, x0 + 7, 37, l); s.px(x0 + 3, 38, GOLD)
    if theme == "post":
        s.rect(12, 38, 17, 39, PAPER)
    elif theme == "picnic":
        pattern(s, (3, 43, 44, 45), "check", pal, only=m)
    elif theme == "rainy":
        s.rect(2, 44, 45, 45, "#f2c94c", only=m)
    elif theme == "starlight":
        blit(s, 12, 43, MOTIFS["star"], GOLD); blit(s, 32, 43, MOTIFS["star"], GOLD)
    elif theme == "forest":
        blit(s, 13, 43, MOTIFS["leaf"], "#93c488"); blit(s, 31, 43, MOTIFS["leaf"], "#93c488")
    else:
        s.rect(13, 33, 34, 34, WHITE); s.rect(2, 45, 45, 45, WHITE, only=m)
    edge(s)
    return s


# ------------------------------------------------------------------ accessories
_sash = gear_wear4.acc_sash


def acc_sash5(theme):
    if theme in ("friendship", "garden"):
        return _sash(theme)
    (m, d, l, a), t = T(theme)
    s = new()
    s.line(4, 27, 38, 46, m); s.line(4, 28, 38, 47, m); s.line(5, 27, 39, 46, d)
    s.rect(34, 43, 38, 46, a); s.px(35, 44, d)
    if theme == "seaside":
        for x in (10, 17, 24, 31):
            s.px(x, 29 + (x - 4) * 19 // 34, WHITE)                         # tiny wave dashes along the band
        s.rect(33, 46, 36, 47, "#a8dcf0")
    elif theme == "starlight":
        for x in (9, 16, 23, 30):
            s.px(x, 29 + (x - 4) * 19 // 34, GOLD)
        s.px(36, 47, GOLD); s.px(37, 47, GOLD)
    elif theme == "forest":
        for x in (10, 20, 30):
            s.px(x, 28 + (x - 4) * 19 // 34, "#93c488"); s.px(x + 1, 29 + (x - 4) * 19 // 34, "#93c488")
        s.rect(33, 46, 36, 47, "#5f9a6a")
    else:                                                                  # festival: confetti on the band + ribbon tails
        for k, x in enumerate((9, 15, 21, 27, 33)):
            s.px(x, 28 + (x - 4) * 19 // 34, ("#ffd86b", WHITE, "#6fb1d6")[k % 3])
        s.line(35, 47, 33, 47, a); s.line(37, 47, 39, 47, "#ffd86b")
    edge(s)
    return s


def _mirror(pts):
    return [(47 - px_, py_) for px_, py_ in pts]


def acc_wings(theme):
    """Wings on the back layer, fully inside the 48px frame: rooted behind the shoulders, rising up and outward."""
    (m, d, l, a), t = T(theme)
    s = new()
    wings = {
        "friendship": [("#fffdf6", [(14, 28), (3, 18), (1, 9), (6, 11), (10, 6), (12, 14), (16, 22)]),
                       ("#ffd6e6", [(14, 28), (4, 24), (3, 19), (9, 20), (13, 24)])],
        "garden": [(m, [(14, 26), (2, 8), (1, 17), (4, 24), (10, 27)]), (l, [(14, 28), (3, 28), (2, 22), (8, 24)]),
                   ("#fff6e2", [(5, 15), (7, 15), (6, 18), (4, 18)])],
        "seaside": [(m, [(14, 28), (4, 12), (1, 16), (1, 24), (5, 30), (13, 30)]), (l, [(14, 28), (4, 14), (4, 24), (12, 28)])],
        "starlight": [("#40448a", [(14, 28), (3, 10), (1, 14), (2, 24), (8, 30), (13, 30)]),
                      ("#6b6fb8", [(14, 28), (5, 14), (4, 22), (10, 28)])],
        "forest": [("#5f9a6a", [(14, 27), (3, 9), (1, 15), (6, 24)]), ("#93c488", [(14, 29), (2, 19), (1, 26), (8, 30)])],
        "festival": [(m, [(14, 28), (2, 20), (1, 12), (8, 18)]), (l, [(14, 28), (4, 8), (2, 6), (9, 16)]), (a, [(14, 28), (8, 6), (10, 5), (13, 18)]),
                     ("#ffd86b", [(14, 28), (12, 5), (14, 5), (15, 20)])],
    }[theme]
    for col, pts in wings:
        s.polygon(pts, col)
        s.polygon(_mirror(pts), col)
    if theme == "starlight":
        for px_, py_ in ((5, 13), (4, 20)):
            s.px(px_, py_, GOLD); s.px(47 - px_, py_, GOLD)
    elif theme == "forest":
        s.line(14, 27, 3, 10, "#3a6a49"); s.line(33, 27, 44, 10, "#3a6a49")
    elif theme == "seaside":
        for k in (16, 20, 24):
            s.line(13, 28, 3, k, d); s.line(34, 28, 44, k, d)
    edge(s)
    return s


# ------------------------------------------------------------------ feet
_tb = gear_wear4.feet_trail_boot


def feet_trail_boot5(theme):
    if theme != "winter":
        return _tb(theme)
    (m, d, l, a), t = T("winter")
    s = new()
    for x0 in (11, 26):
        s.rect(x0 + 1, 39, x0 + 10, 46, m); s.rect(x0 + 1, 39, x0 + 3, 46, l); s.rect(x0 + 8, 39, x0 + 10, 46, d)
        s.rect(x0, 37, x0 + 11, 40, WHITE)                                  # fleece collar
        s.rect(x0, 45, x0 + 11, 47, "#3a2a34")
        for x in range(x0, x0 + 12, 2):
            s.px(x, 47, "#6a5a64")
        s.line(x0 + 4, 42, x0 + 7, 42, "#d9574f"); s.line(x0 + 4, 44, x0 + 7, 44, "#d9574f")
    edge(s)
    return s


def feet_warm_socks(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (11, 26):
        s.rect(x0 + 1, 41, x0 + 10, 47, m); s.rect(x0 + 1, 41, x0 + 3, 47, l); s.rect(x0 + 8, 41, x0 + 10, 47, d)
        s.ellipse(x0, 45, x0 + 11, 47, m)
        s.rect(x0, 40, x0 + 11, 42, WHITE)                                   # fluffy cuff
        for x in range(x0 + 1, x0 + 11, 2):
            s.px(x, 39, WHITE)
        pattern(s, (x0 + 1, 43, x0 + 10, 46), {"picnic": "check", "rainy": "stripe_v", "winter": "dots"}[theme], (m, d, l, a), only=m)
        if theme == "winter":
            blit(s, x0 + 4, 43, MOTIFS["snow"], WHITE)
        elif theme == "rainy":
            s.px(x0 + 5, 44, a); s.px(x0 + 5, 45, a); s.rect(x0 + 8, 45, x0 + 11, 47, "#f2c94c")      # rubber toe patch
        else:                                                              # picnic: ribbon bow on the cuff
            s.rect(x0 + 4, 40, x0 + 7, 42, "#d9574f"); s.px(x0 + 3, 41, "#d9574f"); s.px(x0 + 8, 41, "#d9574f")
    edge(s)
    return s


# ------------------------------------------------------------------ handheld
_sb = gear_wear4.held_snack_box


def held_snack_box5(theme):
    if theme == "post":
        return _sb(theme)
    (m, d, l, a), t = T(theme if theme != "adventure" else "adventure")
    s = new()
    if theme == "garden":                                                  # wicker picnic basket with handle and cloth
        s.rect(32, 38, 46, 46, "#c98a5a"); s.rect(32, 38, 46, 40, "#e0a878")
        for x in range(32, 47, 3):
            s.line(x, 40, x, 46, "#8a5a34")
        s.line(33, 38, 36, 33, "#8a5a34"); s.line(36, 33, 42, 33, "#8a5a34"); s.line(42, 33, 45, 38, "#8a5a34")
        s.rect(33, 37, 45, 38, "#f4a6a0"); s.px(36, 37, WHITE); s.px(42, 37, WHITE)
    elif theme == "starlight":                                             # star-shaped bento tin
        s.polygon([(39, 34), (42, 40), (47, 41), (43, 45), (45, 50), (39, 47), (33, 50), (35, 45), (31, 41), (36, 40)], "#6b6fb8")
        s.circle(39, 43, 2, GOLD)
    else:                                                                  # adventure: tin ration box with strap
        s.rect(32, 37, 46, 46, "#9a6a2a"); s.rect(32, 37, 46, 39, "#d49a4a"); s.rect(32, 44, 46, 46, "#6e4c30")
        s.rect(37, 36, 41, 46, "#6e4c30"); s.rect(38, 40, 40, 42, GOLD)
        motif(s, 34, 41, "compass", "#f0c070") if False else None
    edge(s)
    return s


def held_binoculars(theme):
    s = new()
    if theme == "starlight":                                               # compact star telescope on a short pole
        s.rect(32, 37, 46, 41, "#40448a"); s.rect(32, 37, 46, 38, "#6b6fb8"); s.rect(44, 36, 47, 42, "#27295a")
        s.px(46, 39, "#9fd8ff"); s.line(38, 41, 38, 46, "#8a6a2a"); s.line(36, 46, 40, 46, "#8a6a2a"); s.px(35, 39, GOLD); s.px(34, 38, GOLD)
    elif theme == "garden":                                                # round barrels with a flower on the bridge
        for cx in (35, 43):
            s.circle(cx, 41, 4, "#7fbf7a"); s.circle(cx, 41, 2, "#9fd8ff"); s.circle(cx, 41, 4, "#4f8f58", fill=False)
        s.rect(38, 40, 40, 42, "#4f8f58"); blit(s, 37, 34, MOTIFS["flower"], "#f4a6a0")
    elif theme == "post":                                                  # brass binoculars with a neck strap
        for x0 in (33, 40):
            s.rect(x0, 36, x0 + 5, 45, "#b8902a"); s.rect(x0, 36, x0 + 1, 45, "#d9b060"); s.rect(x0 - 1, 44, x0 + 6, 46, "#8a6a2a")
            s.px(x0 + 2, 45, "#9fd8ff")
        s.rect(37, 39, 40, 42, "#8a6a2a"); s.rect(34, 34, 44, 35, "#a8764a"); s.px(34, 36, "#a8764a"); s.px(44, 36, "#a8764a")
    else:                                                                  # adventure: leather-wrapped, compass on the bridge
        for x0 in (33, 40):
            s.rect(x0, 36, x0 + 5, 45, "#9a6a2a"); s.rect(x0, 38, x0 + 5, 39, "#6e4c30"); s.rect(x0, 42, x0 + 5, 43, "#6e4c30")
            s.rect(x0 - 1, 44, x0 + 6, 46, "#6e4c30"); s.px(x0 + 2, 45, "#9fd8ff")
        s.rect(37, 39, 40, 42, "#d49a4a"); s.px(38, 40, "#d9574f")
    edge(s)
    return s


gear_wear.WEAR.update({
    "equip.hat.flower": (hat_flower, "head"), "equip.hat.rain": (hat_rain, "head"), "equip.hat.explorer": (hat_explorer5, "head"),
    "equip.outfit.workwear": (outfit_workwear, "body"),
    "equip.outfit.explorer_jacket": (outfit_explorer_jacket, "body"),
    "equip.accessory.sash": (acc_sash5, "front"), "equip.accessory.wings": (acc_wings, "back"),
    "equip.feet.trail_boot": (feet_trail_boot5, "feet"), "equip.feet.warm_socks": (feet_warm_socks, "feet"),
    "equip.handheld.snack_box": (held_snack_box5, "hand"), "equip.handheld.binoculars": (held_binoculars, "hand"),
})
