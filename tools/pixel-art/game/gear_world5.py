"""B5 world / item / mail / weather / fx generators."""
import math
import random

import gear_world
import gear_world2
import gear_world3
import gear_world4
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, new, edge, motif, pattern,
                       blit, MOTIFS)
from gear_world import T, _cloud, _sky
from gear_world2 import _balcony, _yard, _hills, _draw_fx
from gear_world3 import _scene
from pixelstudio import Sprite


# ------------------------------------------------------------------ furniture
_ph = gear_world4.furn_phonograph


def furn_phonograph5(theme):
    if theme != "winter":
        return _ph(theme)
    (m, d, l, a), t = T("winter")
    s = new(96, 96)
    s.rect(24, 54, 72, 84, "#cfd6e0"); s.rect(24, 54, 72, 58, WHITE); s.rect(24, 80, 72, 84, "#8aa0bc")
    s.rect(28, 60, 68, 78, m); s.rect(28, 60, 68, 62, l)
    s.rect(20, 84, 26, 90, "#8aa0bc"); s.rect(70, 84, 76, 90, "#8aa0bc")
    s.ellipse(32, 44, 64, 56, "#1c1720"); s.ellipse(34, 45, 62, 54, "#3a3446"); s.circle(48, 50, 2, a)
    s.line(66, 56, 56, 48, "#b8b8cc")
    s.polygon([(50, 46), (58, 44), (70, 4), (94, 18), (80, 40)], "#e6f2ff"); s.polygon([(70, 4), (94, 18), (88, 24), (66, 10)], WHITE)
    s.line(56, 44, 82, 12, "#a8c8e8"); s.line(66, 40, 86, 20, "#a8c8e8")
    s.ellipse(66, 0, 94, 10, WHITE)                                        # snow on the horn
    blit(s, 40, 66, MOTIFS["snow"], WHITE); s.rect(34, 68, 38, 72, GOLD)
    edge(s)
    return s


def furn_speaker(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    cab = {"post": "#a8764a", "garden": "#7fbf7a", "seaside": "#6fb1d6", "starlight": "#40448a", "forest": "#5f9a6a", "winter": "#cfd6e0"}[theme]
    s.rect(26, 14, 70, 90, cab); s.rect(26, 14, 30, 90, WHITE if theme == "winter" else l); s.rect(66, 14, 70, 90, d)
    s.rect(26, 14, 70, 17, l)
    s.circle(48, 38, 15, "#1c1720"); s.circle(48, 38, 11, "#3a3446"); s.circle(48, 38, 5, "#6a6a80"); s.px(48, 38, WHITE)
    s.circle(48, 72, 8, "#1c1720"); s.circle(48, 72, 5, "#3a3446")
    if theme == "post":
        s.rect(34, 18, 62, 22, PAPER); s.line(34, 18, 48, 22, d); s.line(62, 18, 48, 22, d)          # mail-slot grille
    elif theme == "garden":
        for x in (32, 62):
            blit(s, x, 20, MOTIFS["flower"], "#f4a6a0")
        s.rect(30, 4, 66, 14, "#e6f5d8"); s.rect(32, 0, 34, 4, "#5f9a6a"); s.rect(46, 0, 48, 4, "#5f9a6a"); s.rect(60, 0, 62, 4, "#5f9a6a")
    elif theme == "seaside":
        for y in (58, 62):
            s.line(32, y, 64, y, WHITE)
        s.rect(46, 8, 50, 14, "#c9a14a")
    elif theme == "starlight":
        blit(s, 44, 8, MOTIFS["star"], GOLD); s.px(34, 24, GOLD); s.px(62, 24, GOLD)
    elif theme == "forest":
        s.rect(30, 82, 66, 88, "#a8764a"); blit(s, 30, 6, MOTIFS["leaf"], "#93c488"); blit(s, 58, 6, MOTIFS["leaf"], "#5f9a6a")
    else:
        s.ellipse(22, 8, 74, 20, WHITE)
    s.rect(32, 90, 38, 94, WOOD_D); s.rect(58, 90, 64, 94, WOOD_D)
    edge(s)
    return s


def furn_coffee_table(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    top = {"post": "round", "garden": "round", "seaside": "oval", "starlight": "round", "forest": "slice", "winter": "round"}[theme]
    s.ellipse(14, 48, 82, 66, WOOD_D)
    s.ellipse(14, 44, 82, 62, WOOD if theme != "winter" else WHITE)
    s.ellipse(18, 46, 78, 58, m if theme not in ("forest",) else "#d9b088", only="opaque")
    if top == "slice":
        s.ellipse(26, 49, 70, 57, "#a8764a", fill=False); s.ellipse(36, 51, 60, 55, "#a8764a", fill=False)
    s.rect(24, 62, 28, 86, WOOD_D); s.rect(68, 62, 72, 86, WOOD_D); s.rect(44, 64, 52, 82, WOOD)
    if theme == "post":                                                   # letter pile + stamp pad
        s.rect(32, 36, 48, 46, PAPER); s.line(32, 36, 40, 42, "#d9574f"); s.line(48, 36, 40, 42, "#d9574f")
        s.rect(54, 40, 66, 46, "#3a3446"); s.rect(56, 38, 64, 40, "#8a8aa0")
    elif theme == "garden":
        s.rect(40, 36, 56, 46, "#e6f5d8"); s.rect(46, 30, 50, 36, "#5f9a6a")
        for x, c in ((38, "#f4a6a0"), (44, "#ffd86b"), (50, "#fff6e2")):
            s.rect(x, 22, x + 4, 28, c); s.line(x + 2, 28, 48, 36, "#5f9a6a")
    elif theme == "seaside":
        s.polygon([(34, 46), (48, 46), (46, 36), (36, 36)], "#f3dca0"); s.rect(58, 32, 64, 46, "#a8dcf0"); s.rect(60, 28, 62, 32, "#a8dcf0")
    elif theme == "starlight":
        s.rect(40, 40, 56, 46, "#27295a"); s.line(48, 28, 48, 40, WOOD_D); blit(s, 44, 20, MOTIFS["star"], GOLD)
    elif theme == "forest":
        s.ellipse(32, 36, 56, 46, "#d9574f"); s.rect(42, 46, 46, 50, PAPER) if False else None
        s.px(38, 40, WHITE); s.px(48, 38, WHITE); s.px(52, 42, WHITE); blit(s, 58, 34, MOTIFS["leaf"], "#5f9a6a")
    else:
        s.rect(40, 38, 56, 46, WHITE); s.rect(56, 40, 60, 43, WHITE); s.px(46, 34, "#dbe8f4"); s.px(50, 32, "#dbe8f4")
    edge(s)
    return s


def furn_storage(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    kind = {"post": "trunk", "garden": "basket", "seaside": "chest", "starlight": "case", "forest": "logbox", "winter": "icebox"}[theme]
    if kind == "trunk":
        s.rect(14, 38, 82, 84, "#a8764a"); s.ellipse(14, 24, 82, 52, "#a8764a"); s.rect(14, 38, 82, 41, WOOD_L)
        for x in (24, 48, 72):
            s.rect(x, 26, x + 3, 84, "#6e4c30")
        s.rect(42, 48, 54, 58, GOLD); s.rect(46, 52, 50, 58, GOLD_D)
        s.rect(26, 62, 40, 74, PAPER); s.line(26, 62, 33, 68, "#d9574f"); s.line(40, 62, 33, 68, "#d9574f")
    elif kind == "basket":
        s.rect(14, 42, 82, 86, "#c98a5a"); s.rect(14, 42, 82, 48, "#e0a878")
        for x in range(16, 82, 6):
            s.line(x, 48, x, 86, "#8a5a34")
        for y in (56, 66, 76):
            s.line(14, y, 82, y, "#8a5a34")
        s.line(20, 42, 28, 24, "#8a5a34"); s.line(28, 24, 68, 24, "#8a5a34"); s.line(68, 24, 76, 42, "#8a5a34")
        blit(s, 44, 50, MOTIFS["flower"], "#f4a6a0")
    elif kind == "chest":
        s.rect(14, 44, 82, 84, "#c9a14a"); s.polygon([(14, 44), (82, 44), (76, 26), (20, 26)], "#a8764a"); s.rect(14, 44, 82, 48, "#8a6040")
        for x in (24, 70):
            s.rect(x, 26, x + 3, 84, "#6e6e84")
        s.rect(42, 46, 54, 58, "#6e6e84"); s.rect(46, 50, 50, 56, GOLD); s.px(48, 30, WHITE)
        s.polygon([(30, 70), (40, 70), (38, 78), (32, 78)], "#f3dca0")
    elif kind == "case":
        s.rect(14, 38, 82, 84, m); s.rect(14, 38, 82, 42, l); s.rect(14, 80, 82, 84, d)
        s.rect(40, 30, 56, 38, d); s.rect(44, 32, 52, 38, GOLD_D)
        for x, y in ((24, 52), (66, 60), (40, 72), (58, 48)):
            blit(s, x - 2, y - 2, MOTIFS["star"], GOLD)
    elif kind == "logbox":
        s.rect(14, 40, 82, 86, "#8a6040")
        for y in range(44, 86, 8):
            s.rect(14, y, 82, y + 4, "#a8764a"); s.line(14, y + 4, 82, y + 4, "#6e4c30")
        s.rect(14, 34, 82, 42, "#a8764a"); blit(s, 20, 26, MOTIFS["leaf"], "#5f9a6a"); blit(s, 60, 26, MOTIFS["leaf"], "#93c488")
        s.circle(20, 62, 3, WOOD_L); s.circle(76, 70, 3, WOOD_L)
    else:
        s.rect(14, 40, 82, 86, "#a8c8e8"); s.rect(14, 40, 82, 46, "#e6f2ff"); s.rect(14, 82, 82, 86, "#7898c0")
        s.ellipse(10, 28, 86, 46, WHITE)
        s.rect(40, 54, 56, 62, "#7898c0"); blit(s, 44, 66, MOTIFS["snow"], WHITE)
    motif(s, 48, 36 if kind in ("case",) else 70, t["motif"], a) if kind in ("chest", "icebox") else None
    edge(s)
    return s


# ------------------------------------------------------------------ decor
_mat = gear_world4.decor_mat


def decor_mat5(theme):
    if theme not in ("forest", "festival"):
        return _mat(theme)
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    if theme == "forest":
        s.ellipse(2, 20, 46, 42, "#3a6a49"); s.ellipse(4, 22, 44, 40, "#6a8a4a")
        for x, y in ((12, 30), (22, 26), (32, 32), (38, 28)):
            blit(s, x - 2, y - 2, MOTIFS["leaf"], ("#93c488", "#5f9a6a")[x % 2])
        s.ellipse(18, 30, 30, 36, "#a8764a", fill=False)
    else:
        s.rect(4, 22, 44, 40, m); s.rect(4, 22, 44, 24, l); s.rect(4, 38, 44, 40, d)
        for k, x in enumerate(range(8, 42, 6)):
            s.polygon([(x, 26), (x + 5, 26), (x + 2, 32)], (a, "#ffd86b", "#6fb1d6")[k % 3])
        blit(s, 22, 32, MOTIFS["heart"], WHITE)
    edge(s)
    return s


def decor_desk_obj(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.rect(4, 36, 44, 40, WOOD_L); s.rect(4, 40, 44, 43, WOOD_D)           # desk edge
    if theme == "garden":                                                  # pencil cup of flowers + notebook
        s.rect(10, 26, 20, 36, "#e6f5d8"); blit(s, 11, 18, MOTIFS["flower"], "#f4a6a0"); s.line(15, 24, 15, 26, "#5f9a6a")
        s.rect(26, 30, 40, 36, "#7fbf7a"); s.line(27, 32, 39, 32, WHITE)
    elif theme == "seaside":                                               # shell paperweight + postcard
        s.polygon([(10, 36), (22, 36), (20, 28), (12, 28)], "#f3dca0"); s.line(16, 28, 16, 36, "#d9b060")
        s.rect(26, 26, 40, 36, "#a8dcf0"); s.rect(26, 31, 40, 36, "#6fb1d6")
    elif theme == "starlight":                                             # globe lamp + star notebook
        s.circle(16, 26, 8, "#6b6fb8"); s.rect(14, 34, 18, 36, "#8a6a2a"); blit(s, 13, 23, MOTIFS["star"], GOLD)
        s.rect(28, 30, 40, 36, "#27295a"); s.px(33, 32, GOLD)
    elif theme == "forest":                                                # acorn pen holder + leaf notes
        s.ellipse(8, 22, 22, 36, "#a8764a"); s.rect(8, 22, 22, 25, "#6e4c30"); s.line(15, 14, 15, 22, "#6e4c30")
        s.line(18, 14, 15, 22, "#5f9a6a")
        blit(s, 26, 28, MOTIFS["leaf"], "#93c488"); blit(s, 32, 30, MOTIFS["leaf"], "#5f9a6a")
    else:                                                                  # festival: party hat + confetti
        s.polygon([(10, 36), (24, 36), (17, 14)], m); s.line(13, 28, 21, 28, a); s.line(15, 22, 19, 22, a); s.circle(17, 13, 2, "#ffd86b")
        for x, y, c in ((28, 30, a), (32, 33, "#ffd86b"), (36, 29, "#6fb1d6"), (40, 33, l)):
            s.rect(x, y, x + 1, y + 1, c)
    edge(s)
    return s


def decor_wall_hanger(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.rect(4, 8, 44, 14, WOOD); s.rect(4, 8, 44, 10, WOOD_L); s.rect(4, 13, 44, 14, WOOD_D)
    for x in (10, 24, 38):
        s.rect(x - 1, 14, x + 1, 17, GOLD)                                  # hooks
    if theme == "garden":
        for x, c in ((10, "#f4a6a0"), (24, "#ffd86b"), (38, "#fff6e2")):
            s.line(x, 17, x, 24, "#5f9a6a"); blit(s, x - 2, 22, MOTIFS["flower"], c)
    elif theme == "seaside":
        s.line(10, 17, 10, 36, "#c9a14a"); s.circle(10, 38, 3, "#c9a14a", fill=False)
        s.polygon([(20, 17), (28, 17), (30, 32), (18, 32)], "#a8dcf0"); s.line(38, 17, 38, 28, "#f3dca0"); s.circle(38, 30, 3, "#f3dca0")
    elif theme == "starlight":
        for x in (10, 24, 38):
            s.line(x, 17, x, 22, WHITE); blit(s, x - 2, 22, MOTIFS["star"], GOLD if x != 24 else "#fff0a8")
    elif theme == "forest":
        s.polygon([(6, 17), (16, 17), (12, 34)], "#6a8a4a"); s.rect(22, 17, 28, 30, "#a8764a"); blit(s, 34, 18, MOTIFS["leaf"], "#5f9a6a"); blit(s, 34, 24, MOTIFS["leaf"], "#93c488")
    else:
        for k, x in enumerate((10, 24, 38)):
            s.polygon([(x - 4, 17), (x + 4, 17), (x, 30)], (m, "#ffd86b", "#6fb1d6")[k]); s.px(x, 31, a)
    edge(s)
    return s


# ------------------------------------------------------------------ collectibles / mail
_pc = gear_world4.coll_postcard


def coll_postcard5(theme):
    if theme not in ("starlight", "forest", "winter"):
        return _pc(theme)
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(1, 4, 22, 20, PAPER)
    if theme == "starlight":
        s.rect(2, 5, 12, 19, "#2b3060"); blit(s, 5, 8, MOTIFS["star"], GOLD); s.px(4, 16, WHITE); s.px(10, 14, WHITE)
    elif theme == "forest":
        s.rect(2, 5, 12, 19, "#dff2cf"); s.polygon([(7, 7), (3, 17), (11, 17)], "#3a6a49"); s.rect(6, 17, 8, 19, WOOD_D)
    else:
        s.rect(2, 5, 12, 19, "#e6f2ff"); s.rect(2, 15, 12, 19, WHITE); blit(s, 5, 7, MOTIFS["snow"], "#7898c0")
    s.line(14, 11, 20, 11, "#c9b78f"); s.line(14, 14, 20, 14, "#c9b78f"); s.rect(17, 5, 20, 8, m); s.px(18, 6, WHITE)
    edge(s)
    return s


def coll_coin(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    shape = {"post": "round", "garden": "scallop", "seaside": "hex", "starlight": "star", "forest": "square", "winter": "snow"}[theme]
    col = {"post": GOLD, "garden": "#f4c8a0", "seaside": "#cfd6e0", "starlight": "#ffd86b", "forest": "#c9a14a", "winter": "#cfe6ff"}[theme]
    rim = {"post": GOLD_D, "garden": "#d9a070", "seaside": "#8a96a8", "starlight": GOLD_D, "forest": "#8a6a2a", "winter": "#8aa8d0"}[theme]
    if shape == "round":
        s.circle(12, 12, 9, rim); s.circle(12, 12, 7, col); s.rect(8, 10, 15, 14, PAPER); s.line(8, 10, 11, 12, "#d9574f"); s.line(15, 10, 12, 12, "#d9574f")
    elif shape == "scallop":
        for ang in range(0, 360, 45):
            s.circle(int(12 + 7 * math.cos(math.radians(ang))), int(12 + 7 * math.sin(math.radians(ang))), 3, rim)
        s.circle(12, 12, 7, col); blit(s, 10, 10, MOTIFS["flower"], "#f4a6a0") if False else blit(s, 9, 9, MOTIFS["flower"], "#e8708a")
    elif shape == "hex":
        s.polygon([(7, 3), (17, 3), (22, 12), (17, 21), (7, 21), (2, 12)], rim); s.polygon([(8, 5), (16, 5), (20, 12), (16, 19), (8, 19), (4, 12)], col)
        s.line(7, 11, 17, 11, "#6fb1d6"); s.line(7, 14, 17, 14, "#6fb1d6")
    elif shape == "star":
        s.circle(12, 12, 9, rim); s.circle(12, 12, 7, "#40448a"); blit(s, 9, 9, MOTIFS["star"], GOLD)
    elif shape == "square":
        s.rect(3, 3, 20, 20, rim); s.rect(5, 5, 18, 18, col); s.rect(11, 8, 12, 15, "#3a6a49"); blit(s, 8, 8, MOTIFS["leaf"], "#3a6a49")
    else:
        s.circle(12, 12, 9, rim); s.circle(12, 12, 7, col); blit(s, 9, 9, MOTIFS["snow"], WHITE)
    s.px(8, 7, WHITE)
    edge(s)
    return s


def coll_glass_bottle(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    glass = {"post": "#cfeefb", "garden": "#d6f0d0", "seaside": "#a8dcf0", "starlight": "#9fa4e6", "forest": "#b8d8a0", "winter": "#e6f2ff"}[theme]
    shape = {"post": "tall", "garden": "round", "seaside": "flat", "starlight": "potion", "forest": "jar", "winter": "teardrop"}[theme]
    if shape == "tall":
        s.rect(8, 6, 15, 22, glass); s.rect(10, 2, 13, 6, glass); s.rect(10, 1, 13, 2, WOOD_L)
        s.rect(10, 10, 13, 18, PAPER); s.line(10, 10, 11, 12, "#d9574f")             # message scroll
    elif shape == "round":
        s.circle(12, 15, 7, glass); s.rect(10, 4, 13, 9, glass); s.rect(10, 3, 13, 4, WOOD_L)
        blit(s, 10, 12, MOTIFS["flower"], "#f4a6a0")
    elif shape == "flat":
        s.rect(5, 8, 18, 22, glass); s.rect(9, 3, 14, 8, glass); s.rect(9, 2, 14, 3, "#c9a14a")
        s.rect(6, 16, 17, 21, "#f3dca0"); s.px(9, 18, "#d9b060")
    elif shape == "potion":
        s.ellipse(4, 10, 19, 22, glass); s.rect(10, 3, 13, 10, glass); s.rect(9, 2, 14, 3, WOOD_L)
        blit(s, 9, 12, MOTIFS["star"], GOLD); s.px(7, 15, WHITE)
    elif shape == "jar":
        s.rect(5, 8, 18, 22, glass); s.rect(4, 5, 19, 8, WOOD_L); blit(s, 9, 11, MOTIFS["leaf"], "#3a6a49"); s.px(14, 17, "#ffd86b"); s.px(8, 18, "#ffd86b")
    else:
        s.polygon([(12, 3), (19, 15), (17, 21), (7, 21), (5, 15)], glass); s.rect(10, 1, 13, 4, "#a8764a")
        blit(s, 9, 11, MOTIFS["snow"], WHITE)
    s.px(7, 12, WHITE); s.px(7, 13, WHITE)
    edge(s)
    return s


def stat_postmark5(theme):
    if theme == "post":
        return gear_world4.stat_postmark(theme)
    (m, d, l, a), t = T(theme)
    ink = {"garden": "#3a6a49", "starlight": "#40448a", "festival": "#b8406e"}[theme]
    s = new(24, 24)
    s.circle(11, 12, 9, ink, fill=False); s.circle(11, 12, 6, ink, fill=False)
    s.line(15, 7, 23, 7, ink); s.line(15, 10, 23, 10, ink); s.line(15, 13, 23, 13, ink)
    motif(s, 11, 12, t["motif"], ink)
    return s


def stat_gift_wrap(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(3, 8, 20, 21, m); s.rect(3, 8, 20, 10, l); s.rect(3, 19, 20, 21, d)
    pattern(s, (3, 11, 20, 19), t["pattern"], (m, d, l, a), only=m)
    s.rect(10, 8, 13, 21, a if theme != "post" else "#d9574f"); s.rect(3, 13, 20, 15, a if theme != "post" else "#d9574f")
    if theme == "post":
        s.polygon([(12, 8), (7, 3), (10, 8)], "#d9574f"); s.polygon([(12, 8), (17, 3), (14, 8)], "#d9574f")
    elif theme == "garden":
        blit(s, 9, 2, MOTIFS["flower"], "#f4a6a0"); s.line(3, 8, 20, 21, "#f4a6a0")     # diagonal ribbon
    elif theme == "starlight":
        blit(s, 9, 2, MOTIFS["star"], GOLD); s.rect(14, 3, 19, 6, "#fff0a8"); s.px(16, 4, "#27295a")   # gift tag
    else:
        for k, x in enumerate((6, 10, 14)):
            s.polygon([(x, 8), (x + 3, 8), (x + 1, 4)], (a, "#ffd86b", "#6fb1d6")[k])
    edge(s)
    return s


# ------------------------------------------------------------------ weather
def _place_fg(s, place, palette):
    if place == "balcony":
        _balcony(s, *palette["balcony"])
    elif place == "yard":
        _yard(s, *palette["yard"])


def _flakes(s, density, seed, color="#ffffff", size=2):
    rng = random.Random(seed)
    for _ in range(density):
        x, y = rng.randrange(0, 238), rng.randrange(0, 230)
        s.rect(x, y, x + size - 1, y + size - 1, color)


def weather_cold(place):
    s = _scene(place, lambda x: (_sky(x, ["#dfeaf4", "#d3e2ee", "#c8dae8", "#bed3e4", "#d6e4ee", "#e8f0f6"]),
                                 [_cloud(x, cx, cy, "#f4f8fb", sc) for cx, cy, sc in ((50, 50, 1.3), (170, 36, 1.6))],
                                 x.circle(196, 28, 12, "#f4f8fb")), ("#a8bcb8", "#98aea8", "#88a098"))
    for x in (40, 110, 190):                                              # bare winter trees
        s.line(x, 190, x, 150, "#6a5a4a"); s.line(x, 165, x - 14, 148, "#6a5a4a"); s.line(x, 160, x + 12, 142, "#6a5a4a")
        s.line(x - 14, 148, x - 18, 140, "#6a5a4a"); s.line(x + 12, 142, x + 18, 136, "#6a5a4a")
    if place == "balcony":
        _balcony(s, "#8a8c94", "#6a6c74", "#aab0b8", "#c8ced6")
    elif place == "yard":
        _yard(s, "#9ab0a8", "#e6ecf2", "#c0ccd8")
    for x in range(10, 240, 36):                                          # frost sparkles
        s.px(x, 205 + (x * 3) % 30, "#ffffff")
    return s


def weather_snow(place):
    s = _scene(place, lambda x: (_sky(x, ["#c8d4e2", "#c0cedc", "#b8c8d8", "#c0cedc", "#cdd8e4", "#dce4ee"]),
                                 [_cloud(x, cx, cy, "#eef2f8", sc) for cx, cy, sc in ((40, 40, 1.6), (160, 30, 2.0), (110, 90, 1.5))]),
               ("#e6eef6", "#d6e2ee", "#c6d6e6"))
    if place == "balcony":
        _balcony(s, "#e6eef6", "#c0cedc", "#cfd8e4", "#f4f8fc")
        s.ellipse(190, 186, 226, 214, "#d6e2ee"); s.ellipse(194, 178, 222, 196, WHITE)
    elif place == "yard":
        _yard(s, "#eef4fa", "#f4f8fc", "#c8d4e4")
        s.ellipse(100, 196, 150, 232, WHITE)
    _flakes(s, 90, 5, "#ffffff", 2)
    _flakes(s, 60, 6, "#e6f0fa", 1)
    return s


def weather_petals(place):
    s = _scene(place, lambda x: (_sky(x, ["#ffe8ef", "#ffe0ea", "#ffd8e6", "#ffe0ea", "#fff0f4", "#fff6f8"]),
                                 [_cloud(x, cx, cy, "#ffffff", sc) for cx, cy, sc in ((50, 46, 1.3), (180, 62, 1.5))]),
               ("#b8dc9c", "#a0cc88", "#88bc74"))
    for x in (50, 170):                                                   # blossom trees
        s.rect(x, 150, x + 4, 190, "#8a6050")
        for cx, cy, r in ((x - 14, 142, 14), (x + 18, 138, 16), (x + 2, 124, 18)):
            s.circle(cx, cy, r, "#ffc0d6"); s.circle(cx - 3, cy - 3, r // 2, "#ffe0ea")
    if place == "balcony":
        _balcony(s, "#9a7a64", "#7a5e4a", "#c9a090", "#e0bca8")
    elif place == "yard":
        _yard(s, "#a8d890", "#fff0f4", "#f0c8d4")
    rng = random.Random(9)
    for _ in range(46):                                                   # falling petals
        x, y = rng.randrange(0, 236), rng.randrange(0, 226)
        s.rect(x, y, x + 2, y + 1, "#ff9fc2"); s.px(x + 1, y + 2, "#ffc0d6")
    return s


# ------------------------------------------------------------------ fx
def _petal(s, cx, cy, big, col):
    rows = [".#.", "###", ".#."] if big else ["##", "#."]
    if big:
        rows = [".##", "###", "##."]
    blit(s, cx - 1, cy - 1, rows, col)


def _leaf(s, cx, cy, big, col):
    rows = ["..##", ".###", "###.", "#..."] if big else ["##", "#."]
    blit(s, cx - 2, cy - 2, rows, col)


def fx_nature(kind, mode):
    draw = {"petals": _petal, "leaves": _leaf}[kind]
    cols = {"petals": ("#ff9fc2", "#ffc0d6", "#f06a8a"), "leaves": ("#7fbf7a", "#d49a4a", "#5f9a6a")}[kind]
    out = []
    for f in range(4):
        s = new(48, 48)
        if mode == "drift":                                               # drift down the sides like falling petals/leaves
            for k, (x, y0) in enumerate(((3, 6), (44, 14), (24, 4))):
                y = y0 + f * 7
                if 2 < y < 46 and not (9 <= x <= 38 and 20 <= y <= 36):
                    draw(s, x + (1 if (f + k) % 2 else -1), y, k != 2, cols[k % 3])
        elif mode == "bounce":
            y = [14, 22, 30, 22][f]
            draw(s, 44, y if f != 2 else 36, True, cols[0])
            if f == 2:
                s.rect(38, 41, 47, 41, "#e3d9d2")
        else:
            r = 4 + f * 5
            for k, (dx, dy) in enumerate(((-1, 0), (-0.75, -0.6), (0, -1), (0.75, -0.6), (1, 0))):
                x, yy = int(24 + dx * (r + 4)), int(14 + dy * r)
                if 2 < yy < 21 and 2 < x < 46:
                    draw(s, x, yy, f < 3, cols[k % 3])
        edge(s)
        out.append(s)
    return out


# ------------------------------------------------------------------ registry
gear_world.WORLD.update({
    "world.furniture.phonograph": furn_phonograph5, "world.furniture.speaker": furn_speaker,
    "world.furniture.coffee_table": furn_coffee_table, "world.furniture.storage": furn_storage,
    "world.decor.mat": decor_mat5, "world.decor.desk_obj": decor_desk_obj, "world.decor.wall_hanger": decor_wall_hanger,
    "item.collectible.postcard": coll_postcard5, "item.collectible.coin": coll_coin, "item.collectible.glass_bottle": coll_glass_bottle,
    "item.stationery.postmark": stat_postmark5, "item.stationery.gift_wrap": stat_gift_wrap,
})
for _name, _fn in (("cold", weather_cold), ("snow", weather_snow), ("petals", weather_petals)):
    for _place in ("window", "balcony", "yard"):
        gear_world.WEATHER[f"world.weather.{_name}_{_place}"] = (lambda f=_fn, p=_place: f(p))
for _k in ("petals", "leaves"):
    for _m in ("drift", "bounce", "burst"):
        gear_world.FX[f"fx.{_k}_{_m}"] = (lambda k=_k, m=_m: fx_nature(k, m))
