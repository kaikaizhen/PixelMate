"""B2 world / item / mail / weather / fx generators."""
import math

import gear_world
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, THEMES, new, edge,
                       motif, pattern, blit, MOTIFS)
from gear_world import T, weather_sunrise_window, weather_cloudy_window, _sky, _cloud
from pixelstudio import Sprite


# ------------------------------------------------------------------ furniture 96x96
def furn_chair(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    back = {"post": "slat", "garden": "arch", "seaside": "ladder", "starlight": "star", "forest": "log", "winter": "round"}[theme]
    s.rect(28, 40, 68, 66, WOOD)
    if back == "slat":
        for x in range(32, 66, 8):
            s.rect(x, 30, x + 4, 62, WOOD_L)
        s.rect(28, 28, 68, 32, WOOD_D)
    elif back == "arch":
        s.ellipse(28, 18, 68, 60, WOOD_D); s.ellipse(32, 22, 64, 62, m)
        blit(s, 44, 36, MOTIFS["flower"], a)
    elif back == "ladder":
        for y in (34, 44, 54):
            s.rect(28, y, 68, y + 3, WOOD_L)
        s.rect(28, 26, 32, 66, WOOD_D); s.rect(64, 26, 68, 66, WOOD_D)
    elif back == "star":
        s.rect(28, 30, 68, 66, d); s.rect(28, 30, 68, 33, m); blit(s, 44, 40, MOTIFS["star"], GOLD)
    elif back == "log":
        s.rect(28, 26, 68, 66, WOOD_D)
        for y in range(30, 64, 6):
            s.rect(30, y, 66, y + 1, WOOD)
    else:
        s.ellipse(26, 22, 70, 64, WHITE); s.ellipse(30, 26, 66, 62, l)
    s.rect(24, 62, 72, 74, m); s.rect(24, 62, 72, 65, l); s.rect(24, 72, 72, 74, d)   # seat cushion
    pattern(s, (26, 66, 70, 71), t["pattern"], (m, d, l, a), only=m)
    for x in (26, 66):
        s.rect(x, 74, x + 4, 90, WOOD_D)
    edge(s)
    return s


def furn_rug(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    if theme == "post":                                   # rectangle with airmail border + fringe
        s.rect(8, 40, 88, 80, PAPER)
        for x in range(8, 89, 8):
            s.rect(x, 40, x + 3, 43, "#d9574f"); s.rect(x + 4, 40, x + 7, 43, "#6fb1d6")
            s.rect(x, 77, x + 3, 80, "#d9574f"); s.rect(x + 4, 77, x + 7, 80, "#6fb1d6")
        s.rect(14, 48, 82, 72, m); motif(s, 48, 60, "stamp", PAPER)
        for y in range(42, 80, 4):
            s.px(5, y, d); s.px(90, y, d)
    elif theme == "garden":                               # round flower rug
        s.ellipse(10, 34, 86, 84, m); s.ellipse(18, 40, 78, 78, l)
        for i in range(8):
            ang = math.radians(i * 45)
            cx, cy = 48 + 24 * math.cos(ang), 59 + 14 * math.sin(ang)
            s.circle(int(cx), int(cy), 4, ("#f4a6a0", "#ffd86b")[i % 2])
        s.circle(48, 59, 5, "#fff6e2")
    elif theme == "seaside":                              # long oval with wave stripes
        s.ellipse(6, 42, 90, 80, m)
        for y in (50, 58, 66):
            for x in range(14, 84, 8):
                s.line(x, y, x + 3, y + 1, WHITE); s.line(x + 4, y + 1, x + 7, y, WHITE)
    elif theme == "starlight":                            # star-shaped rug
        pts = []
        for i in range(10):
            r = 40 if i % 2 == 0 else 17
            ang = math.radians(-90 + i * 36)
            pts.append((48 + r * math.cos(ang), 62 + r * 0.62 * math.sin(ang) + 0))
        s.polygon(pts, m)
        s.circle(48, 62, 6, GOLD)
    elif theme == "forest":                               # leaf-shaped rug
        s.polygon([(8, 62), (30, 40), (66, 36), (90, 56), (66, 80), (30, 82)], m)
        s.line(10, 62, 86, 58, d)
        for x in range(26, 72, 10):
            s.line(x, 59, x + 6, 44, d); s.line(x, 60, x + 6, 76, d)
    else:                                                 # winter: fluffy round with pom ring
        s.ellipse(12, 36, 84, 82, WHITE)
        for i in range(16):
            ang = math.radians(i * 22.5)
            s.circle(int(48 + 38 * math.cos(ang)), int(59 + 21 * math.sin(ang)), 3, "#dbe8f4")
        s.ellipse(26, 44, 70, 74, l); blit(s, 46, 56, MOTIFS["snow"], "#7898c0")
    edge(s)
    return s


def furn_lamp(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    if theme == "post":                                   # brass reading lamp with letter shade
        s.rect(40, 80, 56, 86, "#8a6a2a"); s.rect(46, 44, 50, 80, "#b8902a")
        s.polygon([(30, 46), (66, 46), (58, 22), (38, 22)], "#fff0c8"); s.line(30, 46, 48, 32, PAPER_D); s.line(66, 46, 48, 32, PAPER_D)
        s.rect(34, 46, 62, 49, "#b8902a"); s.px(48, 34, "#d9574f")
    elif theme == "garden":                               # tulip-shaped lamp
        s.rect(44, 52, 52, 84, "#5f9a6a"); s.polygon([(36, 84), (60, 84), (52, 78), (44, 78)], "#5f9a6a")
        s.polygon([(32, 54), (64, 54), (60, 24), (52, 32), (48, 20), (44, 32), (36, 24)], "#f4a6a0")
        s.ellipse(40, 40, 56, 54, "#ffd86b", only="opaque")
    else:                                                 # seaside: lighthouse lamp
        s.polygon([(36, 86), (60, 86), (56, 40), (40, 40)], WHITE)
        for y in (50, 62, 74):
            s.rect(38, y, 58, y + 5, "#d9574f")
        s.rect(38, 28, 58, 40, "#6fb1d6"); s.rect(42, 31, 54, 37, "#ffe89a")
        s.polygon([(36, 28), (60, 28), (48, 16)], "#3f7fa8"); s.line(60, 34, 74, 30, "#ffe89a"); s.line(36, 34, 22, 30, "#ffe89a")
    edge(s)
    return s


_bs = gear_world.furn_bookshelf


def furn_bookshelf2(theme):
    s = _bs(theme)
    (m, d, l, a), t = T(theme)
    if theme == "seaside":                                # anchor & telescope on top, shells shelf
        s.rect(60, 4, 76, 8, "#d9b088"); s.rect(76, 2, 80, 8, "#8a6040"); blit(s, 22, 3, MOTIFS["wave"], "#6fb1d6")
    elif theme == "starlight":                            # star mobile
        for x in (24, 40, 56):
            s.line(x, 0, x, 6, WHITE); blit(s, x - 2, 5, MOTIFS["star"], GOLD)
    elif theme == "forest":                               # log-stack top + moss
        s.rect(18, 4, 78, 9, "#5f9a6a")
        for x in range(20, 76, 6):
            s.rect(x, 3, x + 2, 5, "#93c488")
    elif theme == "winter":                               # snow cap + candle
        s.ellipse(12, 2, 84, 12, WHITE); s.rect(60, 0, 62, 5, "#d9574f"); s.px(61, -1, GOLD)
    edge(s)
    return s


# ------------------------------------------------------------------ decor 48x48
def decor_floor_tile2(theme):
    if theme in ("garden", "seaside"):
        return gear_world.decor_floor_tile(theme)
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    for ty in range(2):
        for tx in range(2):
            x0, y0 = 1 + tx * 23, 1 + ty * 23
            base = (m, d)[(tx + ty) % 2] if theme == "starlight" else m
            s.rect(x0, y0, x0 + 21, y0 + 21, base)
            if theme == "starlight":
                for k in range(3):
                    s.px(x0 + 4 + k * 7, y0 + 4 + (k * 5) % 14, GOLD)
            elif theme == "forest":                                      # wood planks with knots
                for yy in range(y0 + 5, y0 + 21, 6):
                    s.line(x0, yy, x0 + 21, yy, d)
                s.px(x0 + 8 + tx * 3, y0 + 3, d); s.circle(x0 + 14, y0 + 12, 1, d, fill=False)
            else:                                                        # festival confetti
                for k, c in enumerate((a, l, "#6fb1d6", "#ffd86b")):
                    s.rect(x0 + 3 + k * 5, y0 + 4 + (k * 7) % 14, x0 + 4 + k * 5, y0 + 5 + (k * 7) % 14, c)
            s.rect(x0, y0 + 21, x0 + 21, y0 + 21, d); s.rect(x0 + 21, y0, x0 + 21, y0 + 21, d)
    return s


def decor_wallpaper(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.rect(0, 0, 47, 47, l)
    if theme == "garden":
        for x, y in ((8, 8), (32, 24), (14, 36)):
            blit(s, x, y, MOTIFS["flower"], m if False else "#f4a6a0"); s.px(x + 2, y + 2, GOLD)
        for x in range(0, 48, 12):
            s.line(x, 0, x, 47, "#cfe8bc")
    elif theme == "seaside":
        for y in range(4, 48, 10):
            for x in range(0, 48, 8):
                s.line(x + (y // 10 % 2) * 4, y, x + 3 + (y // 10 % 2) * 4, y + 1, WHITE)
        s.rect(0, 40, 47, 47, "#f3dca0")
    elif theme == "starlight":
        s.rect(0, 0, 47, 47, "#2b3060")
        for x, y in ((6, 6), (28, 12), (16, 28), (38, 38), (8, 40)):
            blit(s, x - 2, y - 2, MOTIFS["star"], GOLD)
        for x, y in ((20, 8), (40, 24), (30, 40)):
            s.px(x, y, WHITE)
    elif theme == "forest":
        for x in range(2, 48, 12):
            s.polygon([(x + 4, 6), (x, 20), (x + 8, 20)], "#3a6a49"); s.polygon([(x + 4, 14), (x - 1, 30), (x + 9, 30)], "#5f9a6a")
            s.rect(x + 3, 30, x + 5, 36, WOOD_D)
        s.rect(0, 40, 47, 47, "#8ac47a")
    else:
        for x in range(0, 48, 8):
            s.polygon([(x, 0), (x + 8, 0), (x + 4, 7)], (a, m)[(x // 8) % 2])
        for k, (x, y) in enumerate(((10, 22), (30, 16), (22, 36), (40, 34))):
            s.rect(x, y, x + 1, y + 1, (a, "#6fb1d6", "#ffd86b", m)[k])
    return s


def decor_windowsill(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.rect(2, 36, 45, 40, WOOD_L); s.rect(2, 40, 45, 43, WOOD_D)
    if theme == "garden":                               # flower pot
        s.polygon([(16, 36), (30, 36), (28, 26), (18, 26)], "#c98a5a")
        for x, c in ((16, "#f4a6a0"), (21, "#ffd86b"), (26, "#fff6e2")):
            s.rect(x, 17 + (x % 3), x + 3, 20 + (x % 3), c); s.line(x + 1, 21, x + 1, 26, "#5f9a6a")
    elif theme == "seaside":                            # shells & bottle
        s.polygon([(8, 36), (16, 36), (14, 29), (10, 29)], "#f3dca0"); s.rect(32, 24, 36, 36, "#a8dcf0"); s.rect(33, 20, 35, 24, "#a8dcf0")
        s.circle(24, 33, 3, WHITE)
    elif theme == "starlight":                          # candle + moon
        s.rect(20, 26, 26, 36, "#fff6e2"); s.px(23, 24, GOLD); s.px(23, 23, "#ffb347")
        s.ellipse(32, 18, 40, 26, "#fff0a8"); s.ellipse(35, 17, 42, 25, "#fffdf6" if False else "#2b3060")
    else:                                               # forest: mossy log & mushroom
        s.ellipse(8, 28, 26, 36, "#a8764a"); s.ellipse(8, 28, 12, 36, WOOD_L)
        s.ellipse(30, 24, 42, 31, "#d9574f"); s.rect(34, 31, 38, 36, PAPER); s.px(33, 27, WHITE); s.px(39, 27, WHITE)
        s.rect(6, 34, 26, 36, "#5f9a6a")
    edge(s)
    return s


# ------------------------------------------------------------------ collectibles 24x24
def coll_bell2(theme):
    if theme in ("post", "garden"):
        return gear_world.coll_bell(theme)
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    if theme == "seaside":                              # ship's bell with rope
        s.polygon([(4, 18), (7, 9), (12, 5), (17, 9), (20, 18)], "#c9a14a"); s.rect(3, 18, 21, 20, "#9a7a2a")
        s.rect(11, 2, 13, 5, "#8a6040"); s.line(5, 13, 19, 13, m); s.rect(11, 20, 13, 22, WOOD_D)
    elif theme == "starlight":                          # star-cut chime
        s.polygon([(5, 18), (7, 9), (12, 5), (17, 9), (19, 18)], "#6b6fb8"); s.rect(4, 18, 20, 20, "#40448a")
        blit(s, 9, 9, MOTIFS["star"], GOLD); s.rect(11, 2, 13, 5, GOLD)
    elif theme == "forest":                             # acorn-cap bell
        s.ellipse(5, 7, 19, 20, "#a8764a"); s.rect(4, 5, 20, 10, "#6e4c30"); s.line(6, 8, 18, 8, "#8a6040"); s.px(12, 3, "#6e4c30"); s.rect(11, 20, 13, 22, GOLD)
    else:                                               # winter: icicle bell with ribbon
        s.polygon([(5, 18), (7, 9), (12, 5), (17, 9), (19, 18)], "#cfe6ff"); s.polygon([(5, 18), (19, 18), (12, 23)], "#a8c8e8")
        s.rect(10, 2, 14, 5, "#d9574f"); s.line(8, 11, 10, 15, WHITE)
    edge(s)
    return s


def coll_medal(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.polygon([(7, 1), (12, 8), (17, 1), (17, 8), (12, 12), (7, 8)], m if theme != "post" else "#d9574f")   # ribbon
    s.line(12, 8, 12, 12, d)
    shape = {"post": "round", "garden": "flower", "seaside": "shield", "starlight": "star", "forest": "hex", "winter": "snow"}[theme]
    cy = 16
    if shape == "round":
        s.circle(12, cy, 6, GOLD); s.circle(12, cy, 3, GOLD_D, fill=False)
    elif shape == "flower":
        for dx, dy in ((0, -4), (4, 0), (0, 4), (-4, 0)):
            s.circle(12 + dx, cy + dy, 3, "#f4a6a0")
        s.circle(12, cy, 2, GOLD)
    elif shape == "shield":
        s.polygon([(6, 11), (18, 11), (18, 18), (12, 23), (6, 18)], "#6fb1d6"); s.line(6, 14, 18, 14, WHITE)
    elif shape == "star":
        s.polygon([(12, 10), (14, 14), (19, 14), (15, 17), (17, 22), (12, 19), (7, 22), (9, 17), (5, 14), (10, 14)], GOLD)
    elif shape == "hex":
        s.polygon([(8, 11), (16, 11), (19, 16), (16, 21), (8, 21), (5, 16)], "#5f9a6a"); blit(s, 10, 14, MOTIFS["leaf"], "#d6f0b0")
    else:
        s.circle(12, cy, 6, "#cfe6ff"); blit(s, 10, cy - 2, MOTIFS["snow"], "#7898c0")
    s.px(10, 14, WHITE) if shape in ("round", "hex") else None
    edge(s)
    return s


def coll_pin_box(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(3, 12, 20, 21, m); s.rect(3, 12, 20, 14, l); s.rect(3, 20, 20, 21, d)
    lid = {"post": [(3, 12), (20, 12), (17, 6), (6, 6)], "garden": [(3, 12), (20, 12), (20, 5), (3, 5)],
           "seaside": [(3, 12), (20, 12), (16, 4), (7, 4)], "starlight": [(3, 12), (20, 12), (12, 3)]}[theme]
    s.polygon(lid, d)
    s.rect(10, 13, 13, 16, GOLD)
    if theme == "post":
        s.line(6, 7, 17, 7, PAPER); s.px(11, 9, PAPER)
    elif theme == "garden":
        blit(s, 9, 6, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":
        motif(s, 12, 8, "wave", WHITE)
    else:
        s.px(9, 8, GOLD); s.px(14, 7, GOLD)
    edge(s)
    return s


# ------------------------------------------------------------------ stationery
def stat_letter_border2(theme):
    if theme == "post":
        return gear_world.stat_letter_border(theme)
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    paper = {"garden": "#f4fbe9", "starlight": "#2b3060", "festival": "#fff0f6"}[theme]
    s.rect(2, 2, 45, 45, paper)
    if theme == "garden":
        for k in range(0, 44, 5):
            s.px(3 + k, 3 + (k // 5 % 2), "#7fbf7a"); s.px(3 + k, 44 - (k // 5 % 2), "#7fbf7a")
            s.px(3 + (k // 5 % 2), 3 + k, "#7fbf7a"); s.px(44 - (k // 5 % 2), 3 + k, "#7fbf7a")
        for x, y in ((3, 3), (42, 3), (3, 42), (42, 42)):
            blit(s, x - 1, y - 1, MOTIFS["flower"], "#f4a6a0")
    elif theme == "starlight":
        for x, y in ((6, 6), (40, 8), (10, 40), (38, 38)):
            blit(s, x - 2, y - 2, MOTIFS["star"], GOLD)
        s.rect(2, 2, 45, 3, "#6b6fb8"); s.rect(2, 44, 45, 45, "#6b6fb8")
    else:
        for x in range(2, 46, 6):
            s.polygon([(x, 2), (x + 6, 2), (x + 3, 8)], (a, m, "#6fb1d6")[(x // 6) % 3])
        for k, (x, y) in enumerate(((8, 30), (36, 24), (20, 40), (30, 36))):
            s.rect(x, y, x + 1, y + 1, (a, "#6fb1d6", GOLD, m)[k])
    s.rect(6, 12, 41, 41, "#fffdf6" if theme != "starlight" else "#3a4080")
    for y in range(16, 38, 5):
        s.line(10, y, 37, y, "#e3d9c4" if theme != "starlight" else "#5a60a0")
    edge(s)
    return s


def stat_stamp(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(2, 2, 21, 21, PAPER)
    for k in range(2, 22, 3):                            # perforation
        s.px(k, 1, PAPER); s.px(k, 22, PAPER); s.px(1, k, PAPER); s.px(22, k, PAPER)
    s.rect(5, 5, 18, 18, m)
    if theme == "post":
        s.rect(7, 8, 16, 15, PAPER); s.line(7, 8, 11, 12, d); s.line(16, 8, 12, 12, d); s.px(11, 12, "#d9574f")
    elif theme == "garden":
        s.rect(5, 12, 18, 18, "#8ac47a"); motif(s, 12, 10, "flower", "#fff6e2")
    elif theme == "starlight":
        s.rect(5, 5, 18, 18, "#2b3060"); blit(s, 9, 8, MOTIFS["star"], GOLD); s.px(7, 15, WHITE); s.px(16, 7, WHITE)
    else:
        for k, c in enumerate(("#ffd86b", "#fff6e2", "#6fb1d6")):
            s.polygon([(6 + k * 4, 6), (9 + k * 4, 6), (8 + k * 4, 12)], c)
        motif(s, 12, 14, "heart", "#fff6e2")
    s.line(14, 19, 20, 14, d)
    edge(s)
    return s


def stat_sticker(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.circle(12, 12, 9, WHITE)
    s.circle(12, 12, 7, m)
    s.rect(7, 9, 17, 15, PAPER); s.line(7, 9, 12, 13, d); s.line(17, 9, 12, 13, d); s.px(12, 13, "#d9574f")
    s.line(5, 5, 8, 8, l); s.polygon([(16, 17), (21, 21), (21, 16)], PAPER_D)   # peeling corner
    edge(s)
    return s


# ------------------------------------------------------------------ weather
def _hills(s, c1, c2, c3):
    s.rect(0, 160, 239, 239, c1)
    s.ellipse(-40, 150, 130, 260, c2); s.ellipse(100, 160, 300, 270, c2)
    s.ellipse(20, 190, 220, 300, c3)


def _balcony(s, wood="#7a5a44", plank="#5e4232", rail="#b98a5e", railtop="#d9b088"):
    s.rect(0, 190, 239, 239, wood)
    for y in (200, 212, 226):
        s.line(0, y, 239, y, plank)
    s.rect(0, 160, 239, 168, railtop); s.rect(0, 168, 239, 172, "#8a6040")
    for x in range(8, 240, 20):
        s.rect(x, 172, x + 5, 192, rail); s.rect(x, 172, x + 1, 192, railtop)
    s.ellipse(190, 186, 226, 214, "#8a6040"); s.ellipse(194, 178, 222, 196, "#5f9a6a")
    for x, y in ((200, 182), (210, 178), (218, 184)):
        s.rect(x, y, x + 3, y + 3, "#f4a6a0")


def _yard(s, ground="#a8d890", fence="#fff6e2", shade="#e8dcc4"):
    s.rect(0, 150, 239, 239, ground)
    for x in range(0, 240, 14):
        s.rect(x, 168, x + 8, 200, fence); s.rect(x, 168, x + 1, 200, shade); s.px(x + 4, 166, fence)
    s.rect(0, 178, 239, 181, shade); s.rect(0, 192, 239, 194, shade)
    for i in range(14):
        x, y = (i * 53) % 230 + 4, 204 + (i * 29) % 30
        s.rect(x, y, x + 2, y + 2, ("#f4a6a0", "#ffd86b", "#fff6e2")[i % 3]); s.line(x + 1, y + 3, x + 1, y + 7, "#5f9a6a")
    s.rect(100, 196, 140, 236, "#c8b090")


def _sunset_sky(s):
    for i, c in enumerate(["#5a4a8a", "#8a58a0", "#c4609a", "#f0788a", "#ff9a6a", "#ffc470"]):
        s.rect(0, i * 28, 239, i * 28 + 27, c)
    s.circle(120, 168, 38, "#ffd86b"); s.circle(120, 168, 32, "#fff0a8")
    for cx, cy in ((40, 70), (180, 54), (210, 112)):
        _cloud(s, cx, cy, "#ffb0a0")


def _starry_sky(s):
    for i, c in enumerate(["#141838", "#1c2250", "#242c64", "#2e3878", "#3a4488", "#4a5498"]):
        s.rect(0, i * 28, 239, i * 28 + 27, c)
    for i in range(40):
        x, y = (i * 59) % 236 + 2, (i * 37) % 130 + 4
        s.px(x, y, WHITE if i % 3 else GOLD)
        if i % 7 == 0:
            s.px(x + 1, y, WHITE); s.px(x - 1, y, WHITE); s.px(x, y - 1, WHITE); s.px(x, y + 1, WHITE)
    s.circle(188, 54, 16, "#fff0a8"); s.circle(194, 50, 14, "#1c2250")


def weather_sunset(place):
    s = Sprite(240, 240)
    _sunset_sky(s)
    _hills(s, "#6a4a7a", "#5a3a6a", "#442e58")
    if place == "balcony":
        _balcony(s, "#4a3040", "#38222e", "#8a5a6a", "#b87a8a")
    elif place == "yard":
        _yard(s, "#7a5a8a", "#f4d0c0", "#c09090")
    return s


def weather_starry(place):
    s = Sprite(240, 240)
    _starry_sky(s)
    _hills(s, "#243050", "#1c2848", "#141c38")
    if place == "balcony":
        _balcony(s, "#3a2e44", "#2a2034", "#6a5a84", "#8a7aa4")
    return s


def weather_cloudy(place):
    s = weather_cloudy_window()
    if place == "balcony":
        _balcony(s, "#6a6460", "#524c48", "#9a9490", "#bab4b0")
    elif place == "yard":
        _yard(s, "#9bb597", "#e6e9ee", "#b8c0cc")
        for x in range(8, 240, 36):                       # wet puddles
            s.ellipse(x, 218, x + 20, 228, "#b8c8dc")
    return s


# ------------------------------------------------------------------ fx
NOTE = ["..##", ".#.#", ".#..", ".#..", "##..", "##.."]
SMALL = {"heart": [".#.", "###", ".#."], "star": [".#.", "###", ".#."], "note": ["#.", "#.", "##"]}
FX_COL = {"heart": ("#ff9fc2", "#f06a8a", "#ffc0d6"), "star": ("#ffd86b", "#fff0a8", "#ffb347"), "note": ("#6b6fb8", "#9fa4e6", "#40448a"), "bubble": ("#a8dcf0", "#d6f0ff", "#6fb1d6")}


def _draw_fx(s, kind, cx, cy, big, col):
    if kind == "bubble":
        r = 4 if big else 3
        s.circle(cx, cy, r, col, fill=False); s.px(cx - r + 2, cy - r + 2, WHITE)
    elif kind == "note":
        rows = NOTE if big else SMALL["note"]
        blit(s, cx - len(rows[0]) // 2, cy - len(rows) // 2, rows, col)
    elif kind == "heart":
        rows = MOTIFS["heart"] if big else SMALL["heart"]
        blit(s, cx - len(rows[0]) // 2, cy - len(rows) // 2, rows, col)
    else:
        rows = MOTIFS["star"] if big else SMALL["star"]
        blit(s, cx - len(rows[0]) // 2, cy - len(rows) // 2, rows, col)


def fx_make(kind, mode):
    """Effects stay out of the face window (x 9..38, y 22..35): rise at the sides, bounce beside the head,
    burst fans out above and around the head."""
    cols = FX_COL[kind]
    out = []
    for f in range(4):
        s = new(48, 48)
        if mode == "drift":
            for k, (x, y0) in enumerate(((2, 38), (45, 46), (24, 15))):
                y = y0 - f * (6 if k == 2 else 8)
                if 3 < y < 46:
                    _draw_fx(s, kind, x + (1 if (f + k) % 2 else -1), y, k != 2, cols[k % 3])
        elif mode == "bounce":
            y = [38, 28, 18, 28][f]
            _draw_fx(s, kind, 43, y, True, cols[0])
            if f in (0, 3):
                s.rect(38, 44, 47, 44, "#e3d9d2")
        else:
            r = 4 + f * 5
            for k, (dx, dy) in enumerate(((-1, 0.25), (-0.75, -0.6), (0, -1), (0.75, -0.6), (1, 0.25), (-0.35, -0.9))):
                if f == 3 and k % 2:
                    continue
                x, y = int(24 + dx * (r + 4)), int(15 + dy * r)
                if 2 < y < 21 and 2 < x < 46:
                    _draw_fx(s, kind, x, y, f < 3, cols[k % 3])
        edge(s)
        out.append(s)
    return out


gear_world.WORLD.update({
    "world.furniture.chair": furn_chair, "world.furniture.rug": furn_rug, "world.furniture.lamp": furn_lamp,
    "world.furniture.bookshelf": furn_bookshelf2, "world.decor.floor_tile": decor_floor_tile2,
    "world.decor.wallpaper": decor_wallpaper, "world.decor.windowsill": decor_windowsill,
    "item.collectible.bell": coll_bell2, "item.collectible.medal": coll_medal, "item.collectible.pin_box": coll_pin_box,
    "item.stationery.letter_border": stat_letter_border2, "item.stationery.stamp": stat_stamp,
    "item.stationery.sticker": stat_sticker,
})
gear_world.WEATHER.update({
    "world.weather.cloudy_balcony": lambda: weather_cloudy("balcony"), "world.weather.cloudy_yard": lambda: weather_cloudy("yard"),
    "world.weather.sunset_window": lambda: weather_sunset("window"), "world.weather.sunset_balcony": lambda: weather_sunset("balcony"),
    "world.weather.sunset_yard": lambda: weather_sunset("yard"),
    "world.weather.starry_window": lambda: weather_starry("window"), "world.weather.starry_balcony": lambda: weather_starry("balcony"),
})
for _k in ("star", "note"):
    for _m in ("drift", "bounce", "burst"):
        gear_world.FX[f"fx.{_k}_{_m}"] = (lambda k=_k, m=_m: fx_make(k, m))
gear_world.FX["fx.bubble_drift"] = lambda: fx_make("bubble", "drift")
gear_world.FX["fx.bubble_bounce"] = lambda: fx_make("bubble", "bounce")
gear_world.FX["fx.bubble_burst"] = lambda: fx_make("bubble", "burst")

for _m in ("drift", "bounce", "burst"):                  # hearts use the same face-safe layout
    gear_world.FX[f"fx.heart_{_m}"] = (lambda m=_m: fx_make("heart", m))
