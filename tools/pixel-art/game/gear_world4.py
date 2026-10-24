"""B4 world / item / mail / weather / fx generators."""
import math

import gear_world
import gear_world2
import gear_world3
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, new, edge, motif, pattern,
                       blit, MOTIFS)
from gear_world import T, _cloud, _sky
from gear_world2 import _balcony, _yard, _hills
from gear_world3 import _rain, _stormy, _scene, fx_special
from pixelstudio import Sprite

POT = "#c98a5a"
PLANT = {"garden": "fern", "seaside": "cactus", "starlight": "moon", "forest": "mushroom", "winter": "pine", "post": "ivy"}


def _plant(s, x, y, kind, w=1.0):
    """Draw a small potted plant with its pot at (x..x+10, y..y+7); returns nothing."""
    s.polygon([(x, y), (x + 10, y), (x + 8, y + 7), (x + 2, y + 7)], POT); s.rect(x - 1, y - 1, x + 11, y + 1, "#e0a878")
    if kind == "fern":
        for dx, dy in ((-2, -8), (2, -11), (5, -12), (8, -11), (12, -8)):
            s.line(x + 5, y - 1, x + dx + 3, y + dy, "#5f9a6a"); s.line(x + 5, y - 1, x + dx + 4, y + dy, "#7fbf7a")
    elif kind == "cactus":
        s.rect(x + 3, y - 10, x + 7, y - 1, "#5f9a6a"); s.rect(x, y - 7, x + 3, y - 5, "#5f9a6a"); s.rect(x, y - 9, x + 1, y - 5, "#5f9a6a")
        s.rect(x + 7, y - 6, x + 10, y - 4, "#5f9a6a"); s.rect(x + 9, y - 8, x + 10, y - 4, "#5f9a6a"); s.px(x + 5, y - 11, "#f4a6a0")
    elif kind == "moon":
        s.circle(x + 5, y - 6, 4, "#fff0a8"); s.circle(x + 7, y - 7, 3, "#2b3060"); s.line(x + 5, y - 1, x + 5, y - 3, "#5f9a6a")
    elif kind == "mushroom":
        s.ellipse(x + 1, y - 8, x + 9, y - 3, "#d9574f"); s.rect(x + 4, y - 3, x + 6, y - 1, PAPER); s.px(x + 3, y - 6, WHITE); s.px(x + 7, y - 5, WHITE)
    elif kind == "pine":
        for k in range(3):
            s.polygon([(x + 5, y - 12 + k * 4), (x + 1 - k, y - 6 + k * 3), (x + 9 + k, y - 6 + k * 3)], "#3a6a49")
        s.px(x + 5, y - 12, WHITE)
    else:
        for k in range(5):
            s.px(x + 2 + k * 2, y - 2 - k, "#5f9a6a"); s.px(x + 3 + k * 2, y - 3 - k, "#7fbf7a")
        blit(s, x + 6, y - 11, MOTIFS["leaf"], "#93c488")


# ------------------------------------------------------------------ furniture
def furn_plant_shelf(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    frame = {"post": "ladder", "garden": "arch", "seaside": "rope", "starlight": "stars", "forest": "log", "winter": "frost"}[theme]
    s.rect(18, 8, 78, 90, WOOD_D); s.rect(21, 11, 75, 87, d if theme == "starlight" else l if theme == "winter" else "#f0e0c4")
    for y in (36, 62, 86):
        s.rect(18, y, 78, y + 4, WOOD); s.rect(18, y, 78, y + 1, WOOD_L)
    if frame == "arch":
        s.ellipse(18, 0, 78, 22, WOOD_D)
    elif frame == "rope":
        s.line(18, 8, 78, 8, "#c9a14a")
        for x in range(20, 78, 6):
            s.px(x, 9, "#9a7a2a")
    elif frame == "stars":
        for x, y in ((28, 20), (60, 22), (44, 14)):
            blit(s, x - 2, y - 2, MOTIFS["star"], GOLD)
    elif frame == "log":
        s.rect(18, 4, 78, 9, "#a8764a"); s.circle(22, 6, 3, WOOD_L)
    elif frame == "frost":
        s.ellipse(16, 3, 80, 13, WHITE)
    else:
        for x in range(22, 76, 8):
            s.rect(x, 12, x + 1, 86, WOOD)
    for i, y in enumerate((36, 62, 86)):
        for j, x in enumerate((26, 44, 62)):
            _plant(s, x, y - 8 - (7 if (i + j) % 2 else 0) + 7, PLANT[theme] if (i + j) % 2 == 0 else
                   ("fern", "cactus", "mushroom")[(i + j) % 3])
    motif(s, 48, 22, t["motif"], a)
    edge(s)
    return s


def furn_fireplace(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    brick = {"post": "#b8553c", "garden": "#c98a5a", "seaside": "#8fb0c4", "starlight": "#40448a", "forest": "#7a6a58", "winter": "#cfd6e0"}[theme]
    s.rect(14, 24, 82, 90, brick)
    for y in range(26, 90, 6):
        s.line(14, y, 82, y, "#00000022" if False else d)
        for x in range(14 + (y // 6 % 2) * 6, 82, 12):
            s.line(x, y, x, y + 5, d)
    s.rect(10, 18, 86, 26, WOOD); s.rect(10, 18, 86, 20, WOOD_L); s.rect(10, 25, 86, 26, WOOD_D)   # mantel
    s.rect(28, 46, 68, 90, "#1c1720"); s.ellipse(28, 38, 68, 56, "#1c1720")                        # opening
    s.polygon([(38, 90), (44, 66), (48, 74), (52, 60), (58, 90)], "#ff8a3a")
    s.polygon([(42, 90), (47, 74), (50, 80), (54, 70), (56, 90)], "#ffd86b"); s.rect(32, 86, 64, 90, WOOD_D)
    if theme == "post":                                                   # stack of letters + clock on mantel
        s.rect(22, 8, 34, 18, PAPER); s.line(22, 8, 28, 13, d); s.line(34, 8, 28, 13, d); s.circle(62, 12, 5, WHITE); s.line(62, 12, 62, 9, LINE)
    elif theme == "garden":
        for x, c in ((20, "#f4a6a0"), (26, "#ffd86b"), (70, "#fff6e2"), (76, "#f4a6a0")):
            s.rect(x, 10, x + 3, 17, "#5f9a6a"); s.rect(x - 1, 6, x + 4, 10, c)
    elif theme == "seaside":
        s.polygon([(22, 18), (32, 18), (30, 8), (24, 8)], "#f3dca0"); s.rect(66, 4, 72, 18, "#a8dcf0")
    elif theme == "starlight":
        s.circle(30, 10, 6, "#fff0a8"); blit(s, 60, 6, MOTIFS["star"], GOLD); blit(s, 70, 10, MOTIFS["star"], GOLD)
    elif theme == "forest":
        s.polygon([(20, 18), (28, 6), (36, 18)], "#3a6a49"); s.ellipse(60, 8, 72, 18, "#d9574f"); s.rect(64, 14, 68, 18, PAPER)
    else:
        for x in (18, 30):
            s.rect(x, 8, x + 5, 18, "#d9574f"); s.px(x + 2, 6, GOLD)
        s.ellipse(52, 4, 84, 18, WHITE)
    edge(s)
    return s


def furn_phonograph(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(24, 54, 72, 84, WOOD); s.rect(24, 54, 72, 58, WOOD_L); s.rect(24, 80, 72, 84, WOOD_D)    # cabinet
    s.rect(28, 60, 68, 78, m); s.rect(28, 60, 68, 62, l)
    pattern(s, (28, 62, 68, 78), t["pattern"], (m, d, l, a), only=m)
    s.rect(20, 84, 26, 90, WOOD_D); s.rect(70, 84, 76, 90, WOOD_D)
    s.ellipse(32, 44, 64, 56, "#1c1720"); s.ellipse(34, 45, 62, 54, "#3a3446"); s.circle(48, 50, 2, a)    # record
    s.line(66, 56, 56, 48, "#b8b8cc")                                                                     # tonearm
    horn = {"post": "#d9b060", "garden": "#7fbf7a", "seaside": "#a8dcf0", "starlight": "#6b6fb8", "forest": "#a8764a"}[theme]
    s.polygon([(50, 46), (58, 44), (70, 4), (94, 18), (80, 40)], horn); s.polygon([(70, 4), (94, 18), (88, 24), (66, 10)], l)
    s.line(56, 44, 82, 12, d); s.line(66, 40, 86, 20, d)
    if theme == "garden":
        blit(s, 74, 8, MOTIFS["flower"], "#f4a6a0")
    elif theme == "starlight":
        blit(s, 78, 8, MOTIFS["star"], GOLD)
    elif theme == "forest":
        blit(s, 76, 8, MOTIFS["leaf"], "#93c488")
    elif theme == "seaside":
        motif(s, 80, 14, "wave", WHITE)
    s.rect(34, 68, 40, 74, GOLD); motif(s, 56, 70, t["motif"], a)
    edge(s)
    return s


_mb = gear_world3.furn_mailbox


def furn_mailbox4(theme):
    if theme not in ("forest", "winter"):
        return _mb(theme)
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(44, 52, 52, 90, WOOD_D); s.rect(44, 52, 46, 90, WOOD); s.rect(34, 88, 62, 92, "#6e6e84")
    if theme == "forest":                                  # hollow log with leaf roof
        s.ellipse(22, 26, 74, 56, "#a8764a"); s.ellipse(22, 26, 40, 56, WOOD_L); s.rect(34, 40, 62, 46, "#1c1720")
        for x in range(26, 70, 8):
            blit(s, x, 20, MOTIFS["leaf"], ("#5f9a6a", "#93c488")[(x // 8) % 2])
    else:                                                  # igloo-style snow box
        s.ellipse(22, 22, 74, 58, "#f4f8ff"); s.ellipse(22, 22, 74, 40, WHITE, only="opaque")
        for y in range(28, 56, 8):
            s.line(24, y, 72, y, "#cfd6e0")
        s.rect(34, 42, 62, 48, "#1c1720"); s.rect(40, 50, 56, 56, "#a8c8e8")
    s.rect(78, 28, 80, 46, "#d9574f"); s.rect(78, 28, 86, 32, "#d9574f")
    edge(s)
    return s


# ------------------------------------------------------------------ decor
_dp = gear_world3.decor_doorplate


def decor_doorplate4(theme):
    if theme == "garden":
        return _dp(theme)
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    letters = ["###", "#.#", "#.#", "#.#", "###"]
    if theme == "seaside":                               # driftwood board with rope + anchor
        s.polygon([(3, 16), (45, 14), (44, 34), (4, 35)], "#c9b08a"); s.line(3, 24, 45, 24, "#a8906a")
        s.line(10, 4, 10, 15, "#c9a14a"); s.line(38, 4, 38, 14, "#c9a14a")
        motif(s, 24, 25, "wave", "#3f7fa8"); s.line(24, 18, 24, 31, "#3f7fa8"); s.line(20, 28, 28, 28, "#3f7fa8")
    elif theme == "starlight":                           # moon-shaped sign
        s.circle(24, 24, 15, "#2b3060"); s.ellipse(12, 12, 36, 36, "#40448a"); blit(s, 22, 20, MOTIFS["star"], GOLD)
        for x, y in ((12, 14), (36, 30), (14, 34)):
            s.px(x, y, WHITE)
    elif theme == "forest":                              # round log slice with rings
        s.ellipse(5, 8, 43, 40, "#a8764a"); s.ellipse(9, 12, 39, 36, "#d9b088"); s.ellipse(14, 16, 34, 32, "#a8764a", fill=False)
        s.ellipse(18, 19, 30, 28, "#a8764a", fill=False); s.px(24, 24, "#6e4c30"); blit(s, 36, 6, MOTIFS["leaf"], "#5f9a6a")
    else:                                                # festival bunting banner
        s.rect(3, 16, 44, 32, m); s.rect(3, 16, 44, 18, l)
        for x in range(3, 44, 8):
            s.polygon([(x, 32), (x + 8, 32), (x + 4, 40)], (a, "#ffd86b", "#6fb1d6")[(x // 8) % 3])
        blit(s, 20, 21, MOTIFS["heart"], WHITE)
    edge(s)
    return s


def decor_plant(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    kind = {"garden": "fern", "seaside": "cactus", "starlight": "moon", "forest": "mushroom", "festival": "ivy"}[theme]
    s.polygon([(12, 30), (36, 30), (32, 44), (16, 44)], POT); s.rect(10, 27, 38, 31, "#e0a878")
    if kind == "fern":
        for dx, dy in ((-12, -18), (-6, -26), (0, -28), (6, -26), (12, -18)):
            s.line(24, 26, 24 + dx, 26 + dy, "#5f9a6a"); s.line(25, 26, 25 + dx, 26 + dy, "#7fbf7a")
    elif kind == "cactus":
        s.rect(19, 8, 28, 28, "#5f9a6a"); s.rect(10, 16, 19, 20, "#5f9a6a"); s.rect(10, 10, 14, 20, "#5f9a6a")
        s.rect(28, 12, 37, 16, "#5f9a6a"); s.rect(33, 6, 37, 16, "#5f9a6a"); blit(s, 21, 2, MOTIFS["flower"], "#f4a6a0")
    elif kind == "moon":
        s.circle(24, 14, 10, "#fff0a8"); s.circle(29, 11, 8, "#2b3060"); s.line(24, 26, 24, 22, "#5f9a6a"); blit(s, 30, 14, MOTIFS["star"], GOLD)
    elif kind == "mushroom":
        s.ellipse(10, 6, 38, 24, "#d9574f"); s.rect(20, 24, 28, 29, PAPER)
        for x, y in ((16, 12), (26, 9), (32, 16)):
            s.circle(x, y, 2, WHITE)
    else:
        for k in range(9):
            s.px(20 + (k % 3) * 3, 26 - k * 2, "#5f9a6a"); s.px(28 - (k % 2) * 5, 25 - k * 2, "#7fbf7a")
        for x, y in ((14, 8), (30, 10), (22, 4)):
            s.rect(x, y, x + 2, y + 2, (a, "#ffd86b", "#6fb1d6")[x % 3])
    edge(s)
    return s


def decor_mat(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.ellipse(2, 20, 46, 42, d); s.ellipse(4, 22, 44, 40, m)
    if theme == "garden":                                # grass mat with flowers
        for x in range(8, 42, 3):
            s.line(x, 36, x + 1, 30, "#4f8f58")
        for x, y in ((14, 28), (24, 26), (34, 30)):
            blit(s, x - 2, y - 2, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":                             # rope-coil mat with anchor
        for r in (18, 14, 10):
            s.ellipse(24 - r, 31 - r // 2, 24 + r, 31 + r // 2, "#c9a14a", fill=False)
        s.line(24, 26, 24, 36, "#3f7fa8"); s.line(20, 34, 28, 34, "#3f7fa8"); s.circle(24, 25, 1, "#3f7fa8")
    else:                                                # starlight night mat
        pattern(s, (6, 24, 42, 38), "stars", (m, d, l, a), only=m)
        blit(s, 22, 27, MOTIFS["star"], GOLD); s.circle(12, 30, 3, "#fff0a8")
    edge(s)
    return s


# ------------------------------------------------------------------ collectibles
def coll_plush(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    animal = {"post": "bear", "garden": "rabbit", "seaside": "seal", "starlight": "star", "forest": "fox", "winter": "penguin"}[theme]
    body = {"post": "#c98a5a", "garden": "#fbf6f4", "seaside": "#b8c4d4", "starlight": "#ffd86b", "forest": "#e8803a", "winter": "#40506a"}[theme]
    s.ellipse(5, 10, 18, 21, body); s.ellipse(6, 3, 17, 12, body)
    if animal == "bear":
        s.circle(7, 4, 2, body); s.circle(16, 4, 2, body); s.rect(10, 17, 13, 19, "#d9574f")        # tiny mailbag/ribbon
    elif animal == "rabbit":
        s.ellipse(7, -2, 10, 6, body); s.ellipse(13, -2, 16, 6, body); blit(s, 14, 12, MOTIFS["flower"], "#f4a6a0")
    elif animal == "seal":
        s.polygon([(3, 17), (6, 14), (6, 20)], body); s.polygon([(19, 17), (16, 14), (16, 20)], body); s.px(11, 8, "#1c1720"); s.px(13, 8, "#1c1720")
    elif animal == "star":
        s.polygon([(12, 1), (14, 7), (21, 8), (15, 12), (17, 19), (12, 15), (7, 19), (9, 12), (3, 8), (10, 7)], body)
    elif animal == "fox":
        s.polygon([(6, 8), (6, 1), (11, 5)], body); s.polygon([(18, 8), (18, 1), (13, 5)], body); s.ellipse(8, 8, 15, 12, WHITE)
    else:
        s.ellipse(8, 12, 15, 21, WHITE); s.polygon([(11, 9), (13, 9), (12, 11)], "#f2a23c")
    s.px(9, 7, "#1c1720"); s.px(14, 7, "#1c1720")
    edge(s)
    return s


def coll_music_box(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(3, 11, 20, 21, m); s.rect(3, 11, 20, 13, l); s.rect(3, 20, 20, 21, d)
    s.rect(2, 8, 21, 11, d)                                                 # lid
    s.rect(21, 14, 23, 16, GOLD); s.px(23, 17, GOLD_D)                      # crank
    top = {"post": "letter", "garden": "flower", "seaside": "shell", "starlight": "star", "forest": "tree", "winter": "snow"}[theme]
    if top == "letter":
        s.rect(8, 2, 15, 8, PAPER); s.line(8, 2, 11, 5, "#d9574f"); s.line(15, 2, 12, 5, "#d9574f")
    elif top == "flower":
        blit(s, 9, 2, MOTIFS["flower"], "#f4a6a0")
    elif top == "shell":
        s.polygon([(8, 8), (15, 8), (14, 3), (9, 3)], "#f3dca0")
    elif top == "star":
        blit(s, 9, 2, MOTIFS["star"], GOLD)
    elif top == "tree":
        s.polygon([(12, 1), (8, 8), (16, 8)], "#3a6a49")
    else:
        s.circle(12, 5, 3, WHITE); s.px(11, 4, LINE)
    motif(s, 12, 16, t["motif"], a)
    edge(s)
    return s


def coll_postcard(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(1, 4, 22, 20, PAPER); s.rect(2, 5, 12, 19, "#a8dcf0" if theme == "seaside" else "#cfeefb")
    if theme == "garden":
        s.rect(2, 14, 12, 19, "#8ac47a"); blit(s, 5, 8, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":
        s.rect(2, 12, 12, 19, "#6fb1d6"); s.circle(8, 8, 2, GOLD)
    else:
        s.rect(2, 5, 12, 19, "#e8d8b0"); s.rect(5, 8, 9, 15, "#d9574f"); s.rect(6, 6, 8, 8, "#d9574f")        # postbox
    s.line(14, 11, 20, 11, "#c9b78f"); s.line(14, 14, 20, 14, "#c9b78f")
    s.rect(17, 5, 20, 8, m); s.px(18, 6, WHITE)
    edge(s)
    return s


# ------------------------------------------------------------------ stationery
_nc = gear_world3.stat_note_card


def stat_note_card4(theme):
    if theme == "post":
        return _nc(theme)
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    paper = {"garden": "#f4fbe9", "starlight": "#2b3060", "festival": "#fff0f6"}[theme]
    s.rect(5, 8, 42, 40, paper); s.rect(5, 8, 42, 11, m)
    for y in range(16, 36, 5):
        if theme == "starlight":
            s.line(10, y, 24 + (y % 3) * 4, y, "#5a60a0")
        else:
            s.line(10, y, 34, y, "#e3d9c4")
    if theme == "garden":
        for x in range(6, 42, 6):
            s.px(x, 12, "#7fbf7a"); s.px(x + 1, 13, "#7fbf7a")
        blit(s, 33, 28, MOTIFS["flower"], "#f4a6a0")
    elif theme == "starlight":
        blit(s, 33, 24, MOTIFS["star"], GOLD); s.px(12, 36, GOLD); s.circle(35, 15, 2, "#fff0a8"); s.circle(36, 14, 2, "#2b3060")
    else:
        for x in range(5, 42, 7):
            s.polygon([(x, 8), (x + 7, 8), (x + 3, 14)], (a, "#ffd86b", "#6fb1d6")[(x // 7) % 3])
        blit(s, 34, 28, MOTIFS["heart"], m)
    edge(s)
    return s


def stat_invitation(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.rect(3, 4, 20, 21, PAPER if theme != "starlight" else "#2b3060"); s.rect(3, 4, 20, 6, m)
    s.rect(6, 9, 17, 10, d if theme != "starlight" else "#6b6fb8"); s.rect(8, 12, 15, 13, l if theme != "starlight" else "#6b6fb8")
    if theme == "post":
        s.rect(15, 15, 19, 19, "#d9574f"); s.px(16, 16, WHITE)
    elif theme == "garden":
        blit(s, 9, 15, MOTIFS["flower"], "#f4a6a0")
    elif theme == "starlight":
        blit(s, 9, 15, MOTIFS["star"], GOLD)
    else:
        for k in range(3):
            s.polygon([(5 + k * 5, 4), (8 + k * 5, 4), (7 + k * 5, 9)], (a, "#ffd86b", "#6fb1d6")[k])
        blit(s, 9, 15, MOTIFS["heart"], m)
    edge(s)
    return s


def stat_postmark(theme):
    (m, d, l, a), t = T("post")
    s = new(24, 24)
    s.circle(11, 12, 9, "#3a3446", fill=False); s.circle(11, 12, 7, "#3a3446", fill=False)
    for x in range(1, 23, 2):
        s.px(x, 10 + (x % 4 // 2) * 2 if False else 9 + (x // 2 % 2) * 2, "#3a3446") if x > 15 else None
    s.line(14, 8, 23, 8, "#3a3446"); s.line(14, 11, 23, 11, "#3a3446"); s.line(14, 14, 23, 14, "#3a3446")
    s.line(6, 11, 12, 11, "#3a3446"); s.line(6, 14, 12, 14, "#3a3446"); s.px(9, 8, "#3a3446")
    edge(s) if False else None
    return s


# ------------------------------------------------------------------ weather
def weather_thunder(place):
    s = _scene(place, lambda x: _stormy(x, dark=True), ("#2c3a34", "#24302c", "#1c2824"))
    if place == "balcony":
        _balcony(s, "#2c2c36", "#1e1e28", "#5a5a6a", "#7a7a8a")
    elif place == "yard":
        _yard(s, "#26342e", "#6a7488", "#44505e")
    s.polygon([(168, 18), (150, 76), (166, 76), (142, 140), (186, 66), (170, 66), (186, 18)], "#fff6a8")
    s.polygon([(168, 28), (156, 74), (168, 74), (150, 124), (176, 70), (164, 70), (176, 28)], WHITE)
    _rain(s, 150, 3, 14, "#8aa0bc", 7)
    return s


def weather_typhoon(place):
    s = _scene(place, lambda x: _stormy(x, dark=True), ("#2a3a34", "#202e2a", "#18241f"))
    if place == "balcony":
        _balcony(s, "#2c2c36", "#1e1e28", "#5a5a6a", "#7a7a8a")
    elif place == "yard":
        _yard(s, "#26342e", "#6a7488", "#44505e")
    for i in range(14):                                       # long slanted wind-driven rain
        x, y = (i * 47) % 240, (i * 31) % 120
    _rain(s, 220, 9, 20, "#b8cae0", 11)
    for cx, cy in ((60, 120), (150, 60), (200, 150)):         # flying leaves
        s.rect(cx, cy, cx + 3, cy + 1, "#5f9a6a"); s.rect(cx + 3, cy + 1, cx + 5, cy + 2, "#7fbf7a")
    s.line(0, 110, 70, 100, "#8aa0bc"); s.line(170, 90, 239, 100, "#8aa0bc")   # wind streaks
    return s


def weather_fog(place):
    s = _scene(place, lambda x: _sky(x, ["#e6e8ec", "#e0e3e8", "#dadee4", "#d6dae0", "#d2d6dc", "#cfd3da"]), ("#b8c4bc", "#a8b8b0", "#98aaa2"))
    if place == "balcony":
        _balcony(s, "#8a8884", "#6e6c68", "#aaa8a4", "#c8c6c2")
    elif place == "yard":
        _yard(s, "#a8b8a4", "#e6e9ee", "#c8d0d8")
    if place == "window":                                     # tree silhouettes fading into the fog
        for x in (30, 90, 170, 215):
            s.rect(x, 130, x + 3, 175, "#8a948e"); s.ellipse(x - 16, 98, x + 20, 140, "#9aa49e")
    elif place == "balcony":                                  # hanging lantern with a soft glow
        s.line(120, 100, 120, 130, "#6a6c74"); s.rect(112, 130, 128, 150, "#6a6c74"); s.rect(115, 133, 125, 147, "#ffe89a")
        s.circle(120, 140, 18, "#f6ecc8", fill=False)
    else:                                                     # yard lamp post
        s.rect(60, 120, 64, 190, "#6a6c74"); s.rect(52, 108, 72, 122, "#6a6c74"); s.rect(55, 111, 69, 119, "#ffe89a")
        s.circle(62, 115, 16, "#f6ecc8", fill=False)
    for y in range(90, 240):                                  # fog thickens toward the ground (ordered dither)
        p = min(1.0, (y - 90) / 120)
        for xx in range(240):
            if ((xx * 7 + y * 13) % 16) < p * 16 or ((xx // 2 + y // 2) % 2 == 0 and p > 0.5):
                s.px(xx, y, "#eef0f4")
    for y in range(20, 90, 10):                               # thin high mist
        for xx in range(0, 240, 3):
            if (xx // 3 + y // 10) % 3 == 0:
                s.px(xx, y, "#f4f6f8")
    return s


# ------------------------------------------------------------------ fx
def _z(s, cx, cy, big, col):
    rows = ["###", "..#", ".#.", "#..", "###"] if big else ["##", ".#", "##"]
    blit(s, cx - 1, cy - 1, rows, col)


def _puff(s, cx, cy, big, col):
    s.circle(cx, cy, 3 if big else 2, col, fill=False)


def _sparkle(s, cx, cy, big, col):
    rows = ["..#..", "..#..", "#####", "..#..", "..#.."] if big else [".#.", "###", ".#."]
    blit(s, cx - len(rows[0]) // 2, cy - len(rows) // 2, rows, col)


def fx_simple(kind, mode):
    draw = {"yawn": _z, "sparkle": _sparkle}[kind]
    cols = {"yawn": ("#8a9ad8", "#b8c4f0", "#6b6fb8"), "sparkle": ("#fff0a8", "#ffd86b", "#ffffff")}[kind]
    out = []
    for f in range(4):
        s = new(48, 48)
        if mode == "drift":
            spots = ((4, 30), (44, 38), (24, 16)) if kind == "yawn" else ((4, 38), (44, 46), (24, 16))
            for k, (x, y0) in enumerate(spots):
                y = y0 - f * (4 if k == 2 else 7)
                if 3 < y < 46 and not (9 <= x <= 38 and 20 <= y <= 36):
                    draw(s, x + (1 if (f + k) % 2 else 0), y, k != 2, cols[k % 3])
        elif mode == "bounce":
            y = [36, 26, 16, 26][f]
            draw(s, 44, y, True, cols[0])
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
    "world.furniture.plant_shelf": furn_plant_shelf, "world.furniture.fireplace": furn_fireplace,
    "world.furniture.phonograph": furn_phonograph, "world.furniture.mailbox": furn_mailbox4,
    "world.decor.doorplate": decor_doorplate4, "world.decor.plant": decor_plant, "world.decor.mat": decor_mat,
    "item.collectible.plush": coll_plush, "item.collectible.music_box": coll_music_box, "item.collectible.postcard": coll_postcard,
    "item.stationery.note_card": stat_note_card4, "item.stationery.invitation": stat_invitation,
    "item.stationery.postmark": stat_postmark,
})
gear_world.WEATHER.update({
    "world.weather.thunder_balcony": lambda: weather_thunder("balcony"), "world.weather.thunder_yard": lambda: weather_thunder("yard"),
    "world.weather.typhoon_window": lambda: weather_typhoon("window"), "world.weather.typhoon_balcony": lambda: weather_typhoon("balcony"),
    "world.weather.typhoon_yard": lambda: weather_typhoon("yard"),
    "world.weather.fog_window": lambda: weather_fog("window"), "world.weather.fog_balcony": lambda: weather_fog("balcony"),
    "world.weather.fog_yard": lambda: weather_fog("yard"),
})
for _m in ("drift", "bounce", "burst"):
    gear_world.FX[f"fx.yawn_{_m}"] = (lambda m=_m: fx_simple("yawn", m))
for _m in ("drift", "bounce"):
    gear_world.FX[f"fx.sparkle_{_m}"] = (lambda m=_m: fx_simple("sparkle", m))
gear_world.FX["fx.sparkle_burst"] = lambda: fx_simple("sparkle", "burst")
gear_world.FX["fx.alert_bounce"] = lambda: fx_special("alert", "bounce")
gear_world.FX["fx.alert_burst"] = lambda: fx_special("alert", "burst")
