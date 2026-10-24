"""B3 world / item / mail / weather / fx generators."""
import math

import gear_world
import gear_world2
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, new, edge, motif, pattern,
                       blit, MOTIFS)
from gear_world import T, _cloud, _sky, weather_cloudy_window
from gear_world2 import (_balcony, _yard, _hills, _starry_sky, _draw_fx, FX_COL, fx_make, SMALL)
from pixelstudio import Sprite


# ------------------------------------------------------------------ furniture 96x96
def furn_lamp3(theme):
    if theme in ("post", "garden", "seaside"):
        return gear_world2.furn_lamp(theme)
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    if theme == "starlight":                          # crescent-moon floor lamp
        s.rect(46, 50, 50, 86, "#b8902a"); s.rect(38, 84, 58, 88, "#8a6a2a")
        s.circle(48, 34, 16, "#fff0a8"); s.circle(55, 30, 14, "#241c40" if False else "#fff8e0")
        s.circle(48, 34, 16, "#fff0a8", fill=False); blit(s, 46, 30, MOTIFS["star"], GOLD)
    elif theme == "forest":                           # mushroom lamp
        s.rect(42, 50, 54, 84, "#f6ecd6"); s.rect(44, 52, 46, 84, "#e8dcc4")
        s.ellipse(24, 24, 72, 56, "#d9574f"); s.ellipse(28, 28, 62, 50, "#ee8a8a", only="opaque")
        for x, y in ((34, 34), (48, 30), (58, 40), (40, 44)):
            s.circle(x, y, 2, WHITE)
        s.rect(40, 84, 56, 88, "#93c488")
    else:                                             # winter: snow lantern
        s.rect(46, 50, 50, 86, "#6e6e84"); s.rect(36, 84, 60, 88, "#8a8aa0")
        s.rect(34, 22, 62, 52, "#6e6e84"); s.rect(37, 25, 59, 49, "#ffe89a"); s.line(48, 25, 48, 49, "#d9b060")
        s.ellipse(30, 14, 66, 26, WHITE); s.rect(36, 52, 60, 54, WHITE)
    edge(s)
    return s


def furn_curtain(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(6, 6, 90, 10, WOOD_D); s.rect(6, 6, 90, 7, WOOD)               # rod
    for x in (6, 88):
        s.circle(x + 1, 8, 3, GOLD)
    for x0, x1 in ((10, 40), (56, 86)):                                   # two drapes
        s.rect(x0, 10, x1, 86, m); s.rect(x0, 10, x0 + 4, 86, l); s.rect(x1 - 3, 10, x1, 86, d)
        for x in range(x0 + 8, x1 - 2, 8):
            s.line(x, 10, x, 86, d)                                       # pleats
        pattern(s, (x0, 10, x1, 86), t["pattern"], (m, d, l, a), only=m)
    hem = {"post": "perforated", "garden": "scallop", "seaside": "wave", "starlight": "zigzag", "forest": "jagged", "winter": "fluff"}[theme]
    for x in range(10, 87, 4):
        if 40 < x < 56:
            continue
        if hem == "scallop" and (x // 4) % 2 == 0:
            s.rect(x, 84, x + 3, 86, None)
        elif hem == "zigzag":
            s.rect(x, 83 + (x // 4) % 2 * 3, x + 3, 86, None)
        elif hem == "wave":
            s.rect(x, 85, x + 3, 86, a)
        elif hem == "fluff":
            s.rect(x, 85, x + 3, 86, WHITE)
        elif hem == "jagged":
            s.rect(x, 84 + (x // 4) % 3, x + 3, 86, None)
        else:
            s.rect(x, 85, x + 1, 85, a)
    tie = {"post": "band", "garden": "bow", "seaside": "rope", "starlight": "tassel", "forest": "vine", "winter": "fluff"}[theme]
    if tie == "band":
        s.rect(38, 44, 58, 48, a); s.rect(44, 41, 52, 51, PAPER); s.line(44, 41, 48, 46, d); s.line(52, 41, 48, 46, d)
    elif tie == "bow":
        s.rect(38, 44, 58, 46, a); blit(s, 44, 40, MOTIFS["flower"], "#f4a6a0"); blit(s, 49, 40, MOTIFS["flower"], "#ffd86b")
    elif tie == "rope":
        s.line(38, 46, 58, 46, "#c9a14a"); s.line(38, 47, 58, 47, "#9a7a2a"); s.circle(48, 50, 3, "#c9a14a")
    elif tie == "tassel":
        s.rect(38, 44, 42, 46, GOLD); s.rect(54, 44, 58, 46, GOLD); s.line(40, 46, 40, 56, GOLD); s.line(56, 46, 56, 56, GOLD)
        s.rect(38, 56, 42, 58, GOLD_D); s.rect(54, 56, 58, 58, GOLD_D)
    elif tie == "vine":
        s.line(38, 44, 58, 48, "#5f9a6a")
        for x in range(40, 58, 5):
            blit(s, x, 41 + (x % 3), MOTIFS["leaf"], "#93c488")
    else:
        s.ellipse(36, 42, 46, 50, WHITE); s.ellipse(50, 42, 60, 50, WHITE)
    # valance / pelmet above the drapes differs per theme
    if theme == "garden":
        for x in range(8, 90, 8):
            s.ellipse(x, 8, x + 8, 20, "#f4a6a0")
    elif theme == "seaside":
        for x in range(8, 90, 16):
            s.line(x, 10, x + 8, 18, "#c9a14a"); s.line(x + 8, 18, x + 16, 10, "#c9a14a")
    elif theme == "starlight":
        for x in range(12, 90, 12):
            blit(s, x - 2, 10, MOTIFS["star"], GOLD)
    elif theme == "winter":
        s.ellipse(4, 3, 92, 14, WHITE)
    elif theme == "post":
        for x in range(10, 88, 10):
            s.rect(x, 11, x + 5, 15, PAPER); s.line(x, 11, x + 2, 13, "#d9574f")
    motif(s, 24, 30, t["motif"], a); motif(s, 70, 30, t["motif"], a)
    edge(s)
    return s


def furn_wardrobe(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(18, 8, 78, 88, WOOD); s.rect(18, 8, 78, 12, WOOD_L); s.rect(18, 84, 78, 88, WOOD_D)
    top = {"post": "flat", "garden": "arch", "seaside": "crown", "starlight": "moon", "forest": "branch", "winter": "snow"}[theme]
    if top == "arch":
        s.ellipse(22, 0, 74, 20, WOOD_L, only="empty")
    elif top == "crown":
        for x in (20, 36, 52, 68):
            s.polygon([(x, 8), (x + 5, 1), (x + 10, 8)], WOOD_L)
    elif top == "moon":
        s.circle(48, 4, 5, "#fff0a8"); blit(s, 60, 0, MOTIFS["star"], GOLD)
    elif top == "branch":
        s.line(22, 6, 74, 4, "#5f9a6a"); blit(s, 30, 0, MOTIFS["leaf"], "#93c488"); blit(s, 60, 0, MOTIFS["leaf"], "#5f9a6a")
    elif top == "snow":
        s.ellipse(16, 3, 80, 14, WHITE)
    for x0, x1 in ((22, 47), (49, 74)):                                   # two doors
        s.rect(x0, 16, x1, 82, m); s.rect(x0, 16, x0 + 3, 82, l); s.rect(x1 - 2, 16, x1, 82, d)
        s.rect(x0 + 4, 22, x1 - 4, 40, d); s.rect(x0 + 5, 23, x1 - 5, 39, l)   # upper panel
        s.rect(x0 + 4, 46, x1 - 4, 76, d); s.rect(x0 + 5, 47, x1 - 5, 75, l)    # lower panel
        pattern(s, (x0 + 5, 47, x1 - 5, 75), t["pattern"], (m, d, l, a), only=l)
    if theme == "seaside":                                                 # porthole windows on the doors
        s.circle(35, 31, 6, "#a8dcf0"); s.circle(35, 31, 6, "#c9a14a", fill=False); s.circle(62, 31, 6, "#a8dcf0"); s.circle(62, 31, 6, "#c9a14a", fill=False)
    elif theme == "post":                                                  # mail-slot on each door
        s.rect(28, 30, 42, 33, "#1c1720"); s.rect(55, 30, 69, 33, "#1c1720")
    s.rect(44, 44, 46, 52, GOLD); s.rect(50, 44, 52, 52, GOLD)             # handles
    motif(s, 35, 31, t["motif"], a); motif(s, 62, 31, t["motif"], a)
    s.rect(24, 88, 32, 92, WOOD_D); s.rect(64, 88, 72, 92, WOOD_D)
    edge(s)
    return s


def furn_mailbox(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(44, 52, 52, 90, WOOD_D); s.rect(44, 52, 46, 90, WOOD)            # post
    s.rect(34, 88, 62, 92, "#6e6e84")
    if theme == "post":                                                    # classic arched box
        s.rect(24, 24, 72, 54, m); s.ellipse(24, 14, 72, 38, m); s.rect(24, 24, 28, 54, l); s.rect(68, 24, 72, 54, d)
        s.rect(34, 36, 62, 42, "#1c1720"); s.rect(38, 46, 58, 50, PAPER)
    elif theme == "garden":                                                # window-box with flowers
        s.rect(26, 32, 70, 54, "#c98a5a"); s.rect(26, 32, 70, 35, "#e0a878"); s.rect(26, 52, 70, 54, "#8a5a34")
        s.rect(34, 40, 62, 44, "#1c1720")
        for x in range(28, 70, 6):
            s.rect(x, 26 + (x % 3), x + 3, 30 + (x % 3), ("#f4a6a0", "#ffd86b", "#fff6e2")[(x // 6) % 3]); s.rect(x + 1, 30, x + 1, 33, "#5f9a6a")
    elif theme == "seaside":                                               # shell-shaped box with rope
        s.ellipse(24, 20, 72, 56, "#a8dcf0")
        for x in (32, 40, 48, 56, 64):
            s.line(48, 54, x, 26, "#6fb1d6")
        s.rect(34, 40, 62, 44, "#1c1720"); s.line(26, 48, 70, 48, "#c9a14a")
    else:                                                                  # starlight: star-topped lamp-post box
        s.rect(26, 30, 70, 56, m); s.rect(26, 30, 70, 33, l); s.rect(26, 54, 70, 56, d)
        s.rect(34, 40, 62, 45, "#1c1720"); blit(s, 44, 22, MOTIFS["star"], GOLD); s.rect(47, 28, 49, 30, GOLD_D)
    s.rect(74, 26, 76, 44, "#d9574f"); s.rect(74, 26, 82, 30, "#d9574f")    # flag
    motif(s, 40, 50, t["motif"], a)
    edge(s)
    return s


# ------------------------------------------------------------------ decor 48x48
def decor_windowsill3(theme):
    if theme != "festival":
        return gear_world2.decor_windowsill(theme)
    (m, d, l, a), t = T("festival")
    s = new(48, 48)
    s.rect(2, 36, 45, 40, WOOD_L); s.rect(2, 40, 45, 43, WOOD_D)
    for x, c in ((8, m), (22, "#ffd86b"), (34, "#6fb1d6")):                 # three paper lanterns
        s.line(x + 4, 8, x + 4, 14, WOOD_D)
        s.ellipse(x, 14, x + 8, 34, c); s.line(x + 4, 14, x + 4, 34, d); s.rect(x + 2, 34, x + 6, 36, GOLD)
    s.polygon([(2, 4), (8, 4), (5, 10)], a); s.polygon([(38, 4), (44, 4), (41, 10)], l)
    edge(s)
    return s


def decor_fairy_light(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    pts = [(2, 8), (12, 14), (24, 16), (36, 14), (45, 8)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        s.line(x0, y0, x1, y1, "#4a3a3a")
    for i, (x, y) in enumerate(((7, 13), (17, 17), (24, 18), (31, 17), (41, 13))):
        s.line(x, y - 1, x, y + 1, "#4a3a3a")
        c = (m, a, l)[i % 3]
        if theme == "garden":
            blit(s, x - 2, y + 1, MOTIFS["flower"], c)
        elif theme == "seaside":
            s.polygon([(x - 3, y + 2), (x + 3, y + 2), (x + 2, y + 7), (x - 2, y + 7)], ("#f3dca0", "#a8dcf0", WHITE)[i % 3])
        elif theme == "starlight":
            blit(s, x - 2, y + 1, MOTIFS["star"], GOLD if i % 2 == 0 else "#fff0a8")
        elif theme == "forest":
            blit(s, x - 2, y + 1, MOTIFS["leaf"], ("#5f9a6a", "#93c488", "#ffd86b")[i % 3])
        else:
            s.polygon([(x - 3, y + 1), (x + 3, y + 1), (x, y + 8)], c)
    edge(s)
    return s


def decor_bouquet(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.line(24, 6, 24, 12, "#a8764a"); s.rect(22, 4, 26, 6, GOLD)           # hanger
    s.polygon([(18, 14), (30, 14), (27, 36), (21, 36)], "#e8d8b0"); s.line(20, 20, 22, 34, "#c9b78f")   # kraft wrap
    s.rect(19, 26, 29, 28, m)                                               # ribbon
    spots = [(15, 10), (22, 6), (29, 10), (18, 16), (28, 16)]
    for i, (x, y) in enumerate(spots):
        c = ((m, l, a), (l, a, m))[i % 2][i % 3]
        if theme == "garden":
            blit(s, x - 1, y + 4, MOTIFS["flower"], ("#f4a6a0", "#ffd86b", "#fff6e2")[i % 3])
        elif theme == "seaside":
            s.circle(x + 1, y + 6, 2, ("#f3dca0", "#a8dcf0", WHITE)[i % 3])
        elif theme == "starlight":
            blit(s, x - 1, y + 4, MOTIFS["star"], GOLD)
        elif theme == "forest":
            blit(s, x - 1, y + 4, MOTIFS["leaf"], ("#5f9a6a", "#93c488", "#a8764a")[i % 3])
        else:
            s.rect(x, y + 5, x + 2, y + 7, ("#d9574f", "#ffd86b", "#fff6e2")[i % 3])
    edge(s)
    return s


def decor_doorplate(theme):
    (m, d, l, a), t = T("garden")
    s = new(48, 48)
    s.polygon([(4, 14), (44, 14), (44, 34), (4, 34)], WOOD); s.rect(4, 14, 44, 16, WOOD_L); s.rect(4, 32, 44, 34, WOOD_D)
    s.rect(10, 20, 12, 28, "#fff6e2"); s.rect(10, 24, 16, 25, "#fff6e2"); s.rect(14, 20, 16, 28, "#fff6e2")   # "H"
    blit(s, 20, 20, ["###", "#.#", "#.#", "#.#", "###"], "#fff6e2"); blit(s, 26, 20, ["#.#", "###", "###", "#.#", "#.#"], "#fff6e2")
    blit(s, 32, 20, ["###", "#..", "##.", "#..", "###"], "#fff6e2")
    for x, y in ((8, 12), (38, 12)):
        s.line(x, 4, x, y, "#a8764a")
    for x in (6, 17, 30, 40):                                               # vines
        blit(s, x, 34, MOTIFS["leaf"], "#5f9a6a")
    blit(s, 38, 15, MOTIFS["flower"], "#f4a6a0")
    edge(s)
    return s


# ------------------------------------------------------------------ collectibles
_pin = gear_world2.coll_pin_box


def coll_pin_box3(theme):
    if theme not in ("forest", "winter"):
        return _pin(theme)
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(3, 12, 20, 21, m); s.rect(3, 12, 20, 14, l); s.rect(3, 20, 20, 21, d)
    if theme == "forest":                                  # acorn-top lid
        s.ellipse(3, 4, 20, 13, "#6e4c30"); s.rect(10, 1, 13, 4, "#6e4c30"); blit(s, 9, 7, MOTIFS["leaf"], "#93c488")
    else:                                                  # snow-capped lid
        s.polygon([(3, 12), (20, 12), (18, 5), (5, 5)], d); s.rect(4, 4, 19, 6, WHITE); blit(s, 9, 7, MOTIFS["snow"], WHITE)
    s.rect(10, 13, 13, 16, GOLD)
    edge(s)
    return s


def coll_keyring(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.circle(12, 6, 4, "#b8b8cc", fill=False); s.circle(12, 6, 3, "#b8b8cc", fill=False)
    s.line(12, 10, 12, 13, "#b8b8cc")
    if theme == "post":                                    # mini envelope
        s.rect(5, 13, 19, 21, PAPER); s.line(5, 13, 12, 18, m); s.line(19, 13, 12, 18, m); s.px(12, 18, "#d9574f")
    elif theme == "garden":                                # flower
        blit(s, 9, 13, MOTIFS["flower"], "#f4a6a0"); s.px(11, 15, GOLD); s.line(11, 18, 11, 22, "#5f9a6a")
    elif theme == "seaside":                               # starfish
        s.polygon([(12, 12), (14, 16), (19, 16), (15, 19), (17, 23), (12, 20), (7, 23), (9, 19), (5, 16), (10, 16)], "#f0a878")
    elif theme == "starlight":                             # moon & star
        s.circle(10, 18, 4, "#fff0a8"); s.circle(12, 17, 3, "#241c40" if False else "#b8b8cc" if False else "#6b6fb8"); blit(s, 14, 14, MOTIFS["star"], GOLD)
    elif theme == "forest":                                # acorn
        s.ellipse(7, 15, 17, 23, "#a8764a"); s.rect(6, 13, 18, 16, "#6e4c30"); s.px(12, 12, "#6e4c30")
    else:                                                  # snowflake
        blit(s, 9, 13, MOTIFS["snow"], "#cfe6ff"); s.px(11, 15, WHITE); s.line(11, 18, 11, 22, "#cfe6ff") if False else None
    edge(s)
    return s


def coll_pottery(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    shape = {"post": "jar", "garden": "pot", "seaside": "amphora", "starlight": "bowl", "forest": "mug", "winter": "teapot"}[theme]
    if shape == "jar":
        s.rect(6, 6, 17, 20, m); s.rect(8, 3, 15, 6, d); s.rect(6, 6, 8, 20, l); s.rect(7, 10, 16, 15, PAPER)
    elif shape == "pot":
        s.polygon([(5, 8), (18, 8), (16, 21), (7, 21)], "#c98a5a"); s.rect(4, 6, 19, 9, "#e0a878"); blit(s, 9, 12, MOTIFS["flower"], "#f4a6a0")
    elif shape == "amphora":
        s.ellipse(5, 8, 18, 22, m); s.rect(9, 3, 14, 9, m); s.rect(8, 2, 15, 4, d); s.line(5, 11, 3, 15, d); s.line(18, 11, 20, 15, d)
        s.line(7, 14, 16, 14, WHITE)
    elif shape == "bowl":
        s.ellipse(3, 9, 20, 21, m); s.rect(3, 9, 20, 11, d); s.rect(3, 9, 20, 9, l)
        for x in (7, 12, 16):
            s.px(x, 15, GOLD)
    elif shape == "mug":
        s.rect(5, 6, 16, 20, m); s.rect(5, 6, 6, 20, l); s.rect(15, 6, 16, 20, d); s.rect(17, 9, 20, 16, d)
        s.rect(18, 11, 19, 14, None); blit(s, 8, 10, MOTIFS["leaf"], "#93c488")
    else:
        s.ellipse(5, 10, 18, 22, m); s.rect(9, 7, 14, 10, d); s.rect(11, 5, 12, 7, WHITE); s.polygon([(18, 13), (22, 10), (20, 16)], d)
        s.rect(2, 12, 5, 16, d); blit(s, 9, 14, MOTIFS["snow"], WHITE)
    edge(s)
    return s


# ------------------------------------------------------------------ stationery
def stat_sticker3(theme):
    if theme == "post":
        return gear_world2.stat_sticker(theme)
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    if theme == "garden":
        s.polygon([(3, 20), (8, 6), (18, 3), (21, 14), (12, 21)], "#7fbf7a"); s.line(5, 19, 18, 6, "#4f8f58")
        blit(s, 14, 14, MOTIFS["flower"], "#f4a6a0")
    elif theme == "starlight":
        s.circle(12, 12, 9, "#2b3060"); blit(s, 9, 8, MOTIFS["star"], GOLD); s.px(7, 15, WHITE); s.px(16, 15, WHITE)
    else:
        s.circle(12, 9, 6, a); s.circle(10, 7, 2, l); s.line(12, 15, 12, 22, "#8a6040"); s.polygon([(10, 15), (14, 15), (12, 17)], d)
    s.polygon([(16, 17), (21, 21), (21, 16)], PAPER_D) if theme != "festival" else None
    edge(s)
    return s


def stat_paper_corner(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.polygon([(2, 2), (22, 2), (2, 22)], m)
    s.polygon([(2, 2), (14, 2), (2, 14)], l)
    s.line(22, 2, 2, 22, d)
    if theme == "post":
        s.line(4, 8, 14, 8, PAPER); s.line(4, 12, 10, 12, PAPER); s.px(5, 4, "#d9574f")
    elif theme == "garden":
        blit(s, 4, 4, MOTIFS["flower"], "#f4a6a0")
        for k in range(6, 20, 3):
            s.px(k, 20 - k + 4, "#5f9a6a"); s.px(k + 1, 20 - k + 5, "#7fbf7a")
    elif theme == "starlight":
        blit(s, 4, 4, MOTIFS["star"], GOLD)
        s.px(11, 5, GOLD); s.px(16, 4, WHITE); s.px(5, 11, WHITE); s.px(4, 16, GOLD)
        s.line(9, 11, 11, 9, GOLD_D)
    else:
        s.polygon([(4, 4), (8, 4), (6, 9)], a); s.polygon([(10, 4), (14, 4), (12, 9)], "#ffd86b"); s.polygon([(4, 10), (8, 10), (6, 15)], "#6fb1d6")
    edge(s)
    return s


def stat_note_card(theme):
    (m, d, l, a), t = T("post")
    s = new(48, 48)
    s.rect(5, 8, 42, 40, PAPER); s.rect(5, 8, 42, 10, "#d9574f")
    for y in range(16, 36, 5):
        s.line(10, y, 30, y, "#e3d9c4")
    s.rect(33, 14, 40, 22, "#d9574f"); s.rect(34, 15, 39, 21, PAPER); motif(s, 36, 18, "stamp", "#d9574f")
    s.line(10, 36, 20, 36, d)
    edge(s)
    return s


# ------------------------------------------------------------------ weather
def _rain(s, density, slant, length, color, seed=0):
    import random
    rng = random.Random(seed)
    for _ in range(density):
        x, y = rng.randrange(0, 240), rng.randrange(0, 200)
        s.line(x, y, x + slant, y + length, color)


def _stormy(s, dark=False):
    cols = ["#6a7488", "#5c6680", "#6a7488", "#7a8498", "#8a94a6", "#9aa4b4"] if not dark else \
           ["#242c40", "#2c364c", "#343e56", "#3c4660", "#465068", "#505a72"]
    _sky(s, cols)
    for cx, cy, sc in ((40, 36, 1.7), (150, 26, 2.0), (210, 76, 1.5), (80, 90, 1.8), (190, 130, 1.6)):
        _cloud(s, cx, cy, "#8892a4" if not dark else "#3a4458", sc)
        _cloud(s, cx + 6, cy + 10, "#6a7488" if not dark else "#2c364c", sc * 0.85)


def _scene(place, sky_fn, ground=("#8aa488", "#789676", "#668464")):
    s = Sprite(240, 240)
    sky_fn(s)
    _hills(s, *ground)
    if place == "balcony":
        _balcony(s, "#5a5a64", "#44444e", "#8a8a98", "#aaaab8")
    elif place == "yard":
        _yard(s, "#86a684", "#cfd6e0", "#a8b4c4")
    return s


def weather_drizzle(place):
    s = _scene(place, lambda x: (_sky(x, ["#c8d2de", "#bcc8d6", "#b0bece", "#bcc8d6", "#ccd6e0", "#d8e0e8"]), [_cloud(x, cx, cy, "#e8edf2", sc) for cx, cy, sc in ((40, 40, 1.6), (160, 30, 1.9), (110, 90, 1.5))]))
    _rain(s, 70, 1, 6, "#9fb4cc", 1)
    if place == "yard":
        for x in range(10, 240, 40):
            s.ellipse(x, 220, x + 22, 230, "#b8c8dc")
    return s


def weather_shower(place):
    s = _scene(place, _stormy, ("#7a9478", "#688668", "#567456"))
    _rain(s, 170, 3, 14, "#b8cae0", 2)
    _rain(s, 80, 3, 10, "#8aa0bc", 3)
    for x in range(8, 240, 36):
        s.ellipse(x, 214, x + 28, 228, "#9fb4cc")
    return s


def weather_thunder_window():
    s = Sprite(240, 240)
    _stormy(s, dark=True)
    _hills(s, "#2c3a34", "#24302c", "#1c2824")
    bolt = [(130, 20), (110, 80), (128, 80), (104, 150), (150, 70), (132, 70), (150, 20)]
    s.polygon(bolt, "#fff6a8"); s.polygon([(130, 30), (116, 78), (130, 78), (112, 130), (140, 74), (128, 74), (140, 30)], WHITE)
    _rain(s, 160, 3, 14, "#8aa0bc", 4)
    return s


def weather_starry_yard():
    s = Sprite(240, 240)
    _starry_sky(s)
    _hills(s, "#243050", "#1c2848", "#141c38")
    _yard(s, "#20304a", "#6a7a9a", "#44527a")
    for x in range(14, 240, 30):                       # fireflies
        s.px(x, 190 + (x * 7) % 40, GOLD)
    return s


# ------------------------------------------------------------------ fx
def _sweat_drop(s, cx, cy, big, col):
    rows = [".#.", "###", "###", ".#."] if big else [".#.", "###"]
    blit(s, cx - 1, cy - len(rows) // 2, rows, col)


def _alert_mark(s, cx, cy, big, col):
    rows = ["#", "#", "#", ".", "#"] if big else ["#", "#", ".", "#"]
    if big:
        rows = ["##", "##", "##", "..", "##"]
    blit(s, cx - 1, cy - 2, rows, col)


def fx_special(kind, mode):
    cols = {"sweat": ("#8fd0f0", "#cfeeff", "#5fb0d8"), "alert": ("#f0504a", "#ff9a8a", "#d9302a")}[kind]
    draw = _sweat_drop if kind == "sweat" else _alert_mark
    out = []
    for f in range(4):
        s = new(48, 48)
        if mode == "drift":
            spots = ((4, 14), (43, 18), (24, 12)) if kind == "sweat" else ((8, 14), (40, 14), (24, 10))
            for k, (x, y0) in enumerate(spots):
                y = y0 + (f * 5 if kind == "sweat" else -f * 3)             # sweat slides down, alert floats up
                if 2 < y < 46 and not (9 <= x <= 38 and 20 <= y <= 36):
                    draw(s, x, y, k != 2, cols[k % 3])
        elif mode == "bounce":
            y = [14, 8, 4, 8][f] if kind == "alert" else [14, 18, 22, 18][f]
            draw(s, 43 if kind == "sweat" else 24, y if kind == "sweat" else y, True, cols[0])
        else:
            r = 4 + f * 5
            for k, (dx, dy) in enumerate(((-1, 0.25), (-0.75, -0.6), (0, -1), (0.75, -0.6), (1, 0.25))):
                x, y = int(24 + dx * (r + 4)), int(15 + dy * r)
                if 2 < y < 21 and 2 < x < 46:
                    draw(s, x, y, f < 3, cols[k % 3])
        edge(s)
        out.append(s)
    return out


# ------------------------------------------------------------------ registry
gear_world.WORLD.update({
    "world.furniture.lamp": furn_lamp3, "world.furniture.curtain": furn_curtain, "world.furniture.wardrobe": furn_wardrobe,
    "world.furniture.mailbox": furn_mailbox, "world.decor.windowsill": decor_windowsill3,
    "world.decor.fairy_light": decor_fairy_light, "world.decor.bouquet": decor_bouquet, "world.decor.doorplate": decor_doorplate,
    "item.collectible.pin_box": coll_pin_box3, "item.collectible.keyring": coll_keyring, "item.collectible.pottery": coll_pottery,
    "item.stationery.sticker": stat_sticker3, "item.stationery.paper_corner": stat_paper_corner,
    "item.stationery.note_card": stat_note_card,
})
gear_world.WEATHER.update({
    "world.weather.starry_yard": weather_starry_yard,
    "world.weather.drizzle_window": lambda: weather_drizzle("window"), "world.weather.drizzle_balcony": lambda: weather_drizzle("balcony"),
    "world.weather.drizzle_yard": lambda: weather_drizzle("yard"),
    "world.weather.shower_window": lambda: weather_shower("window"), "world.weather.shower_balcony": lambda: weather_shower("balcony"),
    "world.weather.shower_yard": lambda: weather_shower("yard"),
    "world.weather.thunder_window": weather_thunder_window,
})
for _k in ("sweat",):
    for _m in ("drift", "bounce", "burst"):
        gear_world.FX[f"fx.{_k}_{_m}"] = (lambda k=_k, m=_m: fx_special(k, m))
gear_world.FX["fx.alert_drift"] = lambda: fx_special("alert", "drift")
gear_world.FX["fx.alert_bounce"] = lambda: fx_special("alert", "bounce")
gear_world.FX["fx.alert_burst"] = lambda: fx_special("alert", "burst")
