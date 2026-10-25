"""Room / inventory / mail / weather / effect assets."""
from gear_base import *  # noqa: F401,F403
from gear_base import LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, THEMES, new, edge, \
    motif, pattern, blit, MOTIFS
from pixelstudio import Sprite


def T(theme):
    return THEMES[theme]["pal"], THEMES[theme]


# ------------------------------------------------------------------ furniture 96x96 (front view)
def furn_bed(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(6, 40, 14, 86, WOOD_D)                                            # headboard post
    s.rect(6, 40, 9, 86, WOOD)
    s.rect(80, 62, 88, 86, WOOD_D)                                           # footboard
    s.rect(6, 56, 88, 70, WOOD)                                              # frame
    s.rect(6, 70, 88, 72, WOOD_D)
    s.rect(14, 46, 38, 58, WHITE); s.rect(14, 46, 38, 48, "#eef1f6")         # pillow
    s.rect(14, 46, 16, 58, "#eef1f6")
    s.rect(14, 58, 82, 76, m)                                                # blanket
    s.rect(14, 58, 82, 61, l)
    s.rect(14, 73, 82, 76, d)
    pattern(s, (16, 62, 80, 72), t["pattern"], (m, d, l, a), only=m)
    motif(s, 60, 66, t["motif"], a)
    s.rect(10, 86, 16, 90, WOOD_D); s.rect(78, 86, 88, 90, WOOD_D)           # feet
    if theme == "starlight":                                                  # headboard stars
        blit(s, 8, 44, MOTIFS["star"], GOLD)
    elif theme == "post":
        s.rect(7, 46, 13, 52, PAPER)                                          # mail-slot plaque
    edge(s)
    return s


def furn_sofa(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(10, 34, 86, 66, m)                                                 # back
    s.rect(10, 34, 86, 38, l)
    s.rect(2, 46, 18, 78, d); s.rect(2, 46, 18, 50, m)                       # left arm
    s.rect(78, 46, 94, 78, d); s.rect(78, 46, 94, 50, m)                     # right arm
    s.rect(18, 58, 47, 76, l); s.rect(49, 58, 78, 76, l); s.rect(18, 58, 78, 60, WHITE)   # two seat cushions
    s.rect(47, 58, 48, 76, d)
    s.rect(18, 74, 78, 76, m)
    pattern(s, (20, 40, 76, 56), t["pattern"], (m, d, l, a), only=m)
    motif(s, 30, 68, t["motif"], d)
    motif(s, 66, 68, t["motif"], d)
    s.rect(6, 78, 12, 84, WOOD_D); s.rect(84, 78, 90, 84, WOOD_D)
    if theme == "forest":
        s.rect(10, 34, 86, 35, "#a8764a")
    edge(s)
    return s


def furn_bookshelf(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(14, 8, 82, 90, WOOD); s.rect(14, 8, 82, 12, WOOD_L)
    s.rect(18, 16, 78, 84, WOOD_D)
    for y0 in (16, 38, 60):
        s.rect(18, y0 + 20, 78, y0 + 22, WOOD)
        x = 20
        for i in range(9):
            w = 4 + (i * 3 + y0) % 3
            h = 12 + (i * 5 + y0) % 7
            c = (m, l, a, d)[(i + y0 // 22) % 4]
            s.rect(x, y0 + 20 - h, x + w, y0 + 19, c)
            s.line(x + 1, y0 + 20 - h + 2, x + 1, y0 + 18, d if c != d else l)
            x += w + 2
            if x > 70:
                break
    motif(s, 48, 6, t["motif"], a)
    if theme == "post":
        s.rect(60, 62, 72, 80, PAPER); s.line(60, 62, 66, 68, d)              # letters stack
    edge(s)
    return s


# ------------------------------------------------------------------ decor 48x48
def decor_clock(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.circle(24, 24, 17, d)
    s.circle(24, 24, 15, m)
    s.circle(24, 24, 12, "#fff6e2")
    for (x, y) in ((24, 14), (24, 34), (14, 24), (34, 24)):
        s.rect(x, y, x, y, LINE)
    s.line(24, 24, 24, 17, LINE); s.line(24, 24, 29, 27, LINE); s.px(24, 24, a)
    if theme == "garden":
        for ang_x, ang_y in ((24, 5), (24, 43), (5, 24), (43, 24), (11, 11), (37, 11), (11, 37), (37, 37)):
            s.rect(ang_x - 1, ang_y - 1, ang_x + 1, ang_y + 1, a)
    elif theme == "seaside":
        s.line(11, 30, 37, 30, l); s.rect(20, 3, 28, 5, d)
    elif theme == "starlight":
        for x, y in ((8, 8), (40, 10), (6, 38)):
            blit(s, x - 2, y - 2, MOTIFS["star"], GOLD)
    elif theme == "forest":
        blit(s, 20, 2, MOTIFS["leaf"], l); blit(s, 24, 2, MOTIFS["leaf"], l)
    else:  # festival: bunting flags
        for x in range(8, 41, 8):
            s.polygon([(x, 2), (x + 6, 2), (x + 3, 7)], (a, m)[x // 8 % 2])
    edge(s)
    return s


# ------------------------------------------------------------------ collectible 24x24
def coll_shell(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.polygon([(12, 21), (3, 14), (2, 9), (5, 5), (9, 3), (12, 2), (15, 3), (19, 5), (22, 9), (21, 14)], m)
    for x in (5, 8, 12, 16, 19):
        s.line(12, 20, x, 5, d)                                                  # ribs fan from hinge
    s.polygon([(9, 20), (15, 20), (13, 22), (11, 22)], l)
    s.line(4, 9, 8, 4, l)
    if theme == "garden":
        s.px(12, 9, a); s.px(11, 10, a); s.px(13, 10, a)
    elif theme == "starlight":
        s.px(8, 11, GOLD); s.px(16, 12, GOLD); s.px(12, 7, GOLD)
    elif theme == "winter":
        s.line(5, 13, 19, 13, WHITE)
    elif theme == "post":
        s.rect(10, 10, 14, 12, PAPER)
    edge(s)
    return s


# ------------------------------------------------------------------ stationery 24x24
def stat_wax_seal(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    s.ellipse(3, 3, 20, 20, m)
    s.ellipse(5, 4, 11, 9, l, only=m)
    s.ellipse(6, 6, 17, 17, d, fill=False)                                       # stamped ring
    s.rect(2, 17, 5, 19, m); s.rect(19, 5, 21, 7, m)                             # wobbly rim bumps
    s.rect(15, 19, 19, 22, m)                                                    # drip
    motif(s, 12, 12, t["motif"], a)
    edge(s)
    return s


# ------------------------------------------------------------------ weather 240x240
def weather_sunrise_window():
    s = Sprite(240, 240)
    bands = ["#ffd9c7", "#ffcfb8", "#ffc4a8", "#ffb89a", "#f8aa90", "#f09c8a"]
    for i, c in enumerate(bands):
        s.rect(0, i * 22, 239, i * 22 + 21, c)
    for y in range(128, 140):                                                  # dither blend into low sky
        for x in range(0, 240):
            if (x + y) % 2 == 0:
                s.px(x, y, "#ffe2a8")
    s.rect(0, 140, 239, 160, "#ffe2a8")
    s.circle(120, 150, 34, "#ffd86b"); s.circle(120, 150, 28, "#fff0a8")
    for y in range(152, 160):                                                  # sun glow rays
        s.line(60, y, 180, y, "#ffe2a8")
    s.rect(0, 160, 239, 239, "#a8d890")                                        # hills
    s.ellipse(-40, 150, 130, 260, "#8ac47a"); s.ellipse(100, 160, 300, 270, "#78b46c")
    s.ellipse(20, 190, 220, 300, "#5f9a6a")
    for cx, cy in ((40, 60), (170, 40), (200, 100)):                           # clouds
        s.ellipse(cx - 18, cy - 5, cx + 18, cy + 8, "#fff4ea")
        s.ellipse(cx - 8, cy - 12, cx + 12, cy + 2, "#fff4ea")
        s.ellipse(cx - 18, cy + 4, cx + 18, cy + 8, "#ffd9c7", only="opaque")
    for x, y in ((30, 205), (90, 215), (160, 208), (205, 222)):                # tiny flowers
        s.rect(x, y, x + 2, y + 2, "#f4a6a0"); s.px(x + 1, y + 1, "#ffd86b")
    return s


# ------------------------------------------------------------------ fx 48x48 x4 frames
def fx_heart_burst():
    frames = []
    for f in range(4):
        s = new(48, 48)
        r = 4 + f * 5
        pts = [(0, -1), (1, -0.7), (-1, -0.7), (0.9, 0.5), (-0.9, 0.5), (0, 1)]
        for k, (dx, dy) in enumerate(pts):
            cx, cy = int(24 + dx * r), int(24 + dy * r)
            big = 5 if f < 3 else 3
            if f == 3 and k % 2:
                continue
            rows = MOTIFS["heart"] if big == 5 else [".#.", "###", ".#."]
            blit(s, cx - len(rows[0]) // 2, cy - len(rows) // 2, rows, "#f06a8a" if k % 2 else "#ff9fc2")
        edge(s)
        frames.append(s)
    return frames


WORLD = {
    "world.furniture.bed": furn_bed, "world.furniture.sofa": furn_sofa, "world.furniture.bookshelf": furn_bookshelf,
    "world.decor.clock": decor_clock, "item.collectible.shell": coll_shell, "item.stationery.wax_seal": stat_wax_seal,
}


# =================================================================== E3 additions
HEADBOARD = {"post": "flat", "garden": "arch", "seaside": "slats", "starlight": "cutout", "forest": "log", "winter": "cloud"}
SOFA_BACK = {"post": "flat", "garden": "scallop", "seaside": "round", "starlight": "tufted", "forest": "log", "winter": "cloud"}


def _headboard(s, kind, x0, x1, top, bottom, m, d, l, a):
    s.rect(x0, top, x1, bottom, WOOD_D)
    s.rect(x0, top, x0 + 3, bottom, WOOD)
    if kind == "arch":
        s.ellipse(x0 - 1, top - 6, x1 + 1, top + 8, WOOD_D); s.ellipse(x0 + 1, top - 4, x1 - 1, top + 8, WOOD, only="opaque")
        blit(s, x0 + 2, top + 2, MOTIFS["flower"], a)
    elif kind == "slats":
        for x in range(x0 + 1, x1, 3):
            s.rect(x, top + 3, x, bottom - 4, l)
    elif kind == "cutout":
        blit(s, x0 + 1, top + 4, MOTIFS["star"], GOLD); blit(s, x0 + 1, top + 14, MOTIFS["star"], GOLD)
    elif kind == "log":
        for y in range(top + 4, bottom - 3, 6):
            s.rect(x0, y, x1, y + 1, WOOD)
        s.circle(x0 + 3, top + 2, 3, WOOD_L)
    elif kind == "cloud":
        s.ellipse(x0 - 2, top - 5, x0 + 6, top + 4, WHITE); s.ellipse(x0 + 2, top - 7, x0 + 9, top + 2, WHITE)
        s.rect(x0, top + 2, x1, bottom, "#dbe8f4")


def furn_bed(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    _headboard(s, HEADBOARD[theme], 6, 14, 40, 86, m, d, l, a)
    s.rect(80, 62, 88, 86, WOOD_D)
    s.rect(6, 56, 88, 70, WOOD); s.rect(6, 70, 88, 72, WOOD_D)
    s.rect(14, 46, 38, 58, "#fffdf6"); s.rect(14, 46, 16, 58, "#e6eaf2"); s.rect(14, 56, 38, 58, "#e6eaf2")
    s.rect(14, 58, 82, 76, m); s.rect(14, 58, 82, 61, l); s.rect(14, 73, 82, 76, d)
    pattern(s, (16, 62, 80, 72), t["pattern"], (m, d, l, a), only=m)
    motif(s, 60, 66, t["motif"], a)
    if theme == "post":
        s.rect(26, 63, 40, 70, PAPER); s.line(26, 63, 33, 67, d); s.line(40, 63, 33, 67, d)   # envelope-flap blanket
    elif theme == "winter":
        s.rect(14, 58, 82, 60, WHITE)
    elif theme == "forest":
        blit(s, 20, 62, MOTIFS["leaf"], l); blit(s, 70, 62, MOTIFS["leaf"], l)
    s.rect(10, 86, 16, 90, WOOD_D); s.rect(78, 86, 88, 90, WOOD_D)
    edge(s)
    return s


def furn_sofa(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    back = SOFA_BACK[theme]
    s.rect(10, 34, 86, 66, m); s.rect(10, 34, 86, 38, l)
    if back == "flat":                                                    # envelope-flap back cushion
        s.polygon([(24, 38), (72, 38), (48, 54)], l); s.line(24, 38, 48, 54, d); s.line(72, 38, 48, 54, d)
    if back == "scallop":
        for x in range(10, 86, 8):
            s.ellipse(x, 28, x + 8, 38, m)
    elif back == "round":
        s.ellipse(10, 24, 86, 50, m); s.ellipse(14, 26, 82, 40, l, only=m)
    elif back == "tufted":
        for x in range(20, 80, 12):
            for y in (44, 54):
                s.rect(x, y, x + 1, y + 1, d)
    elif back == "log":
        s.rect(10, 32, 86, 34, "#a8764a"); s.rect(10, 34, 86, 35, WOOD_D)
    elif back == "cloud":
        for x in (14, 34, 54, 70):
            s.ellipse(x, 26, x + 16, 40, WHITE)
    s.rect(2, 46, 18, 78, d); s.rect(2, 46, 18, 50, m)
    s.rect(78, 46, 94, 78, d); s.rect(78, 46, 94, 50, m)
    s.rect(18, 58, 47, 76, l); s.rect(49, 58, 78, 76, l)
    s.rect(18, 58, 47, 60, WHITE); s.rect(49, 58, 78, 60, WHITE)
    s.rect(18, 74, 78, 76, m); s.rect(47, 58, 48, 76, d)
    pattern(s, (20, 40, 76, 56), t["pattern"], (m, d, l, a), only=m)
    motif(s, 30, 68, t["motif"], d); motif(s, 66, 68, t["motif"], d)
    s.rect(6, 78, 12, 84, WOOD_D); s.rect(84, 78, 90, 84, WOOD_D)
    edge(s)
    return s


def furn_table(theme):
    (m, d, l, a), t = T(theme)
    s = new(96, 96)
    s.rect(14, 54, 82, 62, WOOD); s.rect(14, 54, 82, 56, WOOD_L); s.rect(14, 62, 82, 64, WOOD_D)
    s.rect(18, 64, 24, 86, WOOD_D); s.rect(72, 64, 78, 86, WOOD_D); s.rect(18, 64, 19, 86, WOOD)
    s.rect(24, 72, 72, 74, WOOD_D)                                           # stretcher
    s.rect(30, 54, 66, 58, m); s.rect(30, 58, 66, 61, d)                      # runner cloth
    pattern(s, (30, 54, 66, 61), t["pattern"], (m, d, l, a), only=m)
    if theme == "post":                                                       # letters + inkwell
        s.rect(34, 44, 46, 54, PAPER); s.line(34, 44, 40, 49, d); s.line(46, 44, 40, 49, d)
        s.rect(54, 46, 60, 54, "#2b2a44"); s.rect(56, 42, 58, 46, "#2b2a44"); s.px(57, 40, WHITE)
    elif theme == "garden":                                                   # vase + flowers
        s.rect(46, 46, 52, 54, "#e6f5d8"); s.rect(48, 46, 50, 47, d)
        for x, y, c in ((44, 40, "#f4a6a0"), (49, 37, "#ffd86b"), (54, 41, "#fff6e2")):
            s.rect(x, y, x + 3, y + 3, c); s.px(x + 1, y + 4, "#5f9a6a"); s.line(x + 1, y + 4, 49, 46, "#5f9a6a")
    elif theme == "seaside":                                                  # shell + bottle
        s.polygon([(38, 54), (46, 54), (44, 48), (40, 48)], "#f3dca0")
        s.rect(60, 44, 64, 54, "#a8dcf0"); s.rect(61, 40, 63, 44, "#a8dcf0")
    elif theme == "starlight":                                                # star lamp
        s.rect(46, 50, 52, 54, "#40448a"); s.line(49, 44, 49, 50, WOOD_D)
        blit(s, 46, 38, MOTIFS["star"], GOLD)
    elif theme == "forest":                                                   # mushroom + log slice
        s.ellipse(40, 44, 52, 50, "#d9574f"); s.rect(44, 50, 48, 54, PAPER); s.px(43, 46, WHITE); s.px(49, 47, WHITE)
        s.ellipse(58, 50, 68, 54, WOOD_L)
    else:                                                                     # winter: teacup + steam
        s.rect(44, 48, 54, 54, WHITE); s.rect(54, 49, 57, 51, WHITE); s.rect(45, 49, 53, 50, "#a8764a")
        s.px(47, 45, "#dbe8f4"); s.px(49, 43, "#dbe8f4"); s.px(51, 45, "#dbe8f4")
    edge(s)
    return s


def decor_painting(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    s.rect(4, 6, 43, 41, WOOD); s.rect(4, 6, 43, 8, WOOD_L); s.rect(4, 39, 43, 41, WOOD_D)
    sky = {"garden": "#cfeefb", "seaside": "#ffe8c0", "starlight": "#2b3060", "forest": "#dff2cf", "festival": "#ffd9e6"}[theme]
    s.rect(8, 10, 39, 37, sky)
    if theme == "garden":
        s.rect(8, 28, 39, 37, "#8ac47a")
        for x, c in ((12, "#f4a6a0"), (20, "#ffd86b"), (28, "#fff6e2"), (35, "#f4a6a0")):
            s.line(x + 1, 24, x + 1, 31, "#5f9a6a"); s.rect(x, 21, x + 2, 23, c); s.px(x + 1, 22, GOLD_D)
    elif theme == "seaside":
        s.circle(32, 18, 4, GOLD); s.rect(8, 24, 39, 37, "#6fb1d6")
        for y in (27, 31, 35):
            for x in range(8 + y % 4, 39, 6):
                s.line(x, y, x + 2, y, "#a8dcf0")
    elif theme == "starlight":
        s.ellipse(12, 12, 20, 20, "#fff0a8"); s.ellipse(15, 11, 22, 19, "#2b3060")
        for x, y in ((28, 14), (34, 24), (14, 30), (24, 31)):
            blit(s, x - 2, y - 2, MOTIFS["star"], GOLD)
    elif theme == "forest":
        for x in (13, 24, 33):
            s.polygon([(x, 14), (x - 5, 28), (x + 5, 28)], "#3a6a49"); s.polygon([(x, 18), (x - 4, 30), (x + 4, 30)], "#5f9a6a")
            s.rect(x - 1, 30, x + 1, 33, WOOD_D)
        s.rect(8, 33, 39, 37, "#8ac47a")
    else:
        for x in range(10, 38, 7):
            s.line(x, 10, x, 15, WOOD_D); s.ellipse(x - 2, 15, x + 2, 23, ("#e86a9a", "#ffd86b")[(x // 7) % 2])
        s.line(8, 12, 39, 12, WOOD_D)
    edge(s)
    return s


def decor_floor_tile(theme):
    (m, d, l, a), t = T(theme)
    s = new(48, 48)
    for ty in range(2):
        for tx in range(2):
            x0, y0 = 1 + tx * 23, 1 + ty * 23
            base = (m, l)[(tx + ty) % 2]
            s.rect(x0, y0, x0 + 21, y0 + 21, base)
            s.rect(x0, y0 + 21, x0 + 21, y0 + 21, d); s.rect(x0 + 21, y0, x0 + 21, y0 + 21, d)
            if theme == "garden":
                blit(s, x0 + 8, y0 + 8, MOTIFS["leaf"], d if base == l else l)
            else:
                for x in range(x0 + 2, x0 + 20, 6):
                    s.line(x, y0 + 8 + (x // 6) % 2, x + 3, y0 + 8 + (x // 6) % 2, WHITE)
                    s.line(x + 1, y0 + 14, x + 4, y0 + 14, WHITE)
    return s


def coll_stone(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    shape = {"post": [(3, 17), (5, 9), (12, 5), (20, 9), (21, 17), (12, 20)],
             "garden": [(3, 18), (4, 11), (10, 6), (18, 7), (21, 14), (19, 19), (8, 20)],
             "seaside": [(2, 16), (6, 9), (14, 7), (21, 11), (22, 17), (12, 20)],
             "starlight": [(4, 18), (3, 12), (9, 5), (15, 4), (21, 10), (20, 18)],
             "forest": [(3, 19), (5, 10), (12, 7), (19, 9), (21, 19)],
             "winter": [(2, 18), (5, 11), (11, 6), (18, 8), (22, 16), (20, 20)]}[theme]
    s.polygon(shape, m)
    s.polygon([(shape[0][0] + 2, shape[0][1] - 1), (shape[1][0] + 1, shape[1][1] + 1), (shape[2][0], shape[2][1] + 1),
               (shape[2][0] - 2, shape[2][1] + 5)], l)
    s.rect(14, 15, 20, 19, d, only=m)
    if theme == "post":
        s.rect(9, 12, 15, 15, PAPER, only=m); s.line(9, 12, 12, 14, d)
    elif theme == "garden":
        for x in (7, 11, 16):
            s.px(x, 18, "#5f9a6a"); s.px(x + 1, 17, "#7fbf7a")
    elif theme == "seaside":
        s.line(5, 13, 20, 13, WHITE); s.line(5, 15, 20, 15, WHITE)
    elif theme == "starlight":
        for x, y in ((8, 10), (13, 8), (16, 13), (10, 15)):
            s.px(x, y, GOLD)
    elif theme == "forest":
        blit(s, 8, 8, MOTIFS["leaf"], "#7fbf7a")
    else:
        for x, y in ((8, 8), (13, 10), (10, 13)):
            s.px(x, y, WHITE)
    edge(s)
    return s


def coll_bell(theme):
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    if theme == "post":
        s.polygon([(5, 17), (7, 9), (12, 5), (17, 9), (19, 17)], GOLD); s.rect(4, 17, 20, 19, GOLD_D)
        s.rect(8, 9, 9, 15, WHITE)
        s.rect(11, 19, 13, 21, m); s.rect(11, 2, 13, 4, d)
        s.rect(15, 11, 17, 13, m)                                            # ribbon tag
    else:
        s.ellipse(5, 6, 19, 19, "#b5e2a0")                                    # bud-shaped bell
        for x in (8, 12, 16):
            s.line(x, 8, x, 17, "#7fbf7a")
        s.rect(4, 17, 20, 19, "#7fbf7a"); s.rect(11, 19, 13, 21, "#f4a6a0"); s.rect(11, 2, 13, 5, "#5f9a6a")
        s.rect(9, 10, 10, 12, WHITE)
    edge(s)
    return s


def stat_letter_border(theme):
    s = new(48, 48)
    s.rect(2, 2, 45, 45, PAPER)
    for i in range(0, 44, 6):                                                # airmail slanted stripes
        c = "#d9574f" if (i // 6) % 2 == 0 else "#6fb1d6"
        for k in range(3):
            for x in (i + k, ):
                if x < 44:
                    s.rect(x + 2, 2, x + 2, 5, c); s.rect(x + 2, 42, x + 2, 45, c)
                    s.rect(2, x + 2, 5, x + 2, c); s.rect(42, x + 2, 45, x + 2, c)
    s.rect(6, 6, 41, 41, "#fffdf6")
    for y in range(12, 38, 5):
        s.line(10, y, 37, y, "#e3d9c4")
    edge(s)
    return s


def _heart(s, cx, cy, big, color):
    rows = MOTIFS["heart"] if big else [".#.#.", "#####", ".###.", "..#.."][:4]
    if not big:
        rows = [".#.", "###", ".#."]
    blit(s, cx - len(rows[0]) // 2, cy - len(rows) // 2, rows, color)


def fx_heart_drift():
    out = []
    for f in range(4):
        s = new(48, 48)
        for k, (x, y0) in enumerate(((16, 38), (30, 44), (22, 52))):
            y = y0 - f * 8
            if 4 < y < 46:
                _heart(s, x + (2 if (f + k) % 2 else -2), y, k != 2, ("#ff9fc2", "#f06a8a", "#ffc0d6")[k])
        edge(s)
        out.append(s)
    return out


def fx_heart_bounce():
    out = []
    for f in range(4):
        s = new(48, 48)
        y = [38, 28, 20, 28][f]
        w = [(7, 5), (5, 5), (5, 5), (5, 5)][f]
        cx = 24
        if f == 0:                                                           # squashed on the ground
            s.rect(cx - 7, y - 2, cx + 7, y + 3, "#f06a8a"); s.rect(cx - 6, y - 3, cx - 1, y - 2, "#f06a8a")
            s.rect(cx + 1, y - 3, cx + 6, y - 2, "#f06a8a"); s.rect(cx - 5, y - 2, cx - 3, y - 1, "#ffc0d6")
        else:
            blit(s, cx - 2, y - 2, MOTIFS["heart"], "#f06a8a"); s.px(cx - 1, y - 1, "#ffc0d6")
        s.rect(cx - 6, 44, cx + 6, 44, "#e3c9d2") if f in (0, 3) else None
        edge(s)
        out.append(s)
    return out


# ---- weather ---------------------------------------------------------------
def _sky(s, bands, h=40):
    for i, c in enumerate(bands):
        s.rect(0, i * h, 239, i * h + h - 1, c)


def _cloud(s, cx, cy, c="#fff4ea", sc=1.0):
    r = int(18 * sc)
    s.ellipse(cx - r, cy - 5, cx + r, cy + 8, c)
    s.ellipse(cx - r // 2, cy - 12, cx + r * 2 // 3, cy + 2, c)


def weather_sunrise_balcony():
    s = weather_sunrise_window()
    s.rect(0, 190, 239, 239, "#7a5a44")                                       # balcony floor planks
    for y in (200, 212, 226):
        s.line(0, y, 239, y, "#5e4232")
    s.rect(0, 160, 239, 168, "#d9b088"); s.rect(0, 168, 239, 172, "#8a6040")    # rail top
    for x in range(8, 240, 20):
        s.rect(x, 172, x + 5, 192, "#b98a5e"); s.rect(x, 172, x + 1, 192, "#d9b088")
    s.ellipse(190, 186, 226, 214, "#8a6040"); s.ellipse(194, 178, 222, 196, "#5f9a6a")   # plant pot
    for x, y in ((200, 182), (210, 178), (218, 184)):
        s.rect(x, y, x + 3, y + 3, "#f4a6a0")
    return s


def _tint(base, toward, t):
    """Mix two #rrggbb colours (t = how far toward the second)."""
    a = [int(base[i:i + 2], 16) for i in (1, 3, 5)]
    b = [int(toward[i:i + 2], 16) for i in (1, 3, 5)]
    return "#%02x%02x%02x" % tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def _stones(s, ground):
    """A few round stepping stones leading down from the gate, tinted from the ground so they belong to every weather and time of day."""
    stone, edge_c, light = _tint(ground, "#d8c8a8", 0.62), _tint(ground, "#6a5a48", 0.45), _tint(ground, "#f4ead2", 0.7)
    for x0, y0, x1, y1 in ((112, 196, 132, 205), (122, 208, 144, 218), (104, 221, 126, 231)):
        s.ellipse(x0, y0 + 1, x1, y1 + 1, edge_c)        # a little shadow under the stone
        s.ellipse(x0, y0, x1, y1, stone)
        s.ellipse(x0 + 3, y0 + 1, x0 + 9, y0 + 3, light)  # a highlight on its top-left


def weather_sunrise_yard():
    s = weather_sunrise_window()
    s.rect(0, 150, 239, 239, "#a8d890")
    s.ellipse(-40, 140, 150, 220, "#8ac47a")
    for x in range(0, 240, 14):                                               # picket fence
        s.rect(x, 168, x + 8, 200, "#fff6e2"); s.rect(x, 168, x + 1, 200, "#e8dcc4"); s.px(x + 4, 166, "#fff6e2")
    s.rect(0, 178, 239, 181, "#e8dcc4"); s.rect(0, 192, 239, 194, "#e8dcc4")
    for i in range(14):                                                       # grass + flowers
        x, y = (i * 53) % 230 + 4, 204 + (i * 29) % 30
        s.rect(x, y, x + 2, y + 2, ("#f4a6a0", "#ffd86b", "#fff6e2")[i % 3]); s.line(x + 1, y + 3, x + 1, y + 7, "#5f9a6a")
    _stones(s, "#a8d890")                                                   # stepping stones
    return s


def weather_cloudy_window():
    s = Sprite(240, 240)
    _sky(s, ["#cfd8e4", "#c3ceda", "#b8c4d2", "#c3ceda", "#d3dbe4", "#dfe5ec"])
    for cx, cy, sc in ((40, 40, 1.4), (150, 30, 1.8), (210, 80, 1.3), (80, 100, 1.6), (180, 140, 1.5), (30, 150, 1.4)):
        _cloud(s, cx, cy, "#f4f6f9", sc); _cloud(s, cx + 6, cy + 10, "#dfe5ec", sc * 0.8)
    s.rect(0, 180, 239, 239, "#b5c9b0"); s.ellipse(-30, 170, 160, 260, "#9bb597"); s.ellipse(110, 180, 290, 270, "#88a684")
    for x in (50, 130, 200):                                                  # distant trees
        s.rect(x, 168, x + 3, 186, "#6a5a4a"); s.ellipse(x - 9, 148, x + 12, 172, "#6f8f6c")
    s.line(30, 90, 60, 94, "#aab6c4"); s.line(160, 110, 200, 106, "#aab6c4")   # faint wind streaks
    return s


# =================================================================== registry
WORLD.update({
    "world.furniture.bed": furn_bed, "world.furniture.sofa": furn_sofa, "world.furniture.table": furn_table,
    "world.decor.painting": decor_painting, "world.decor.floor_tile": decor_floor_tile,
    "item.collectible.stone": coll_stone, "item.collectible.bell": coll_bell,
    "item.stationery.letter_border": stat_letter_border,
})
WEATHER = {"world.weather.sunrise_window": weather_sunrise_window, "world.weather.sunrise_balcony": weather_sunrise_balcony,
           "world.weather.sunrise_yard": weather_sunrise_yard, "world.weather.cloudy_window": weather_cloudy_window}
FX = {"fx.heart_drift": fx_heart_drift, "fx.heart_bounce": fx_heart_bounce, "fx.heart_burst": fx_heart_burst}


# ---- E3 revisions: shells / seals get a different silhouette per theme (not recolors)
def coll_shell(theme):                                                      # noqa: F811
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    if theme == "post":                       # flat-topped scallop with a postmark stripe
        s.polygon([(12, 21), (3, 15), (2, 7), (22, 7), (21, 15)], m)
        for x in (5, 9, 13, 17, 20):
            s.line(12, 20, x, 8, d)
        s.rect(2, 5, 22, 7, l); s.rect(9, 12, 15, 14, PAPER)
    elif theme == "garden":                   # petal-lobed clam
        for cx in (6, 12, 18):
            s.circle(cx, 10, 4, m)
        s.polygon([(4, 12), (20, 12), (12, 21)], m)
        s.line(12, 20, 6, 8, d); s.line(12, 20, 12, 7, d); s.line(12, 20, 18, 8, d); s.px(12, 9, a)
    elif theme == "seaside":                  # spiral conch
        s.polygon([(3, 15), (6, 8), (13, 5), (20, 9), (21, 16), (12, 20)], m)
        s.polygon([(20, 14), (23, 11), (21, 8)], m)
        for r in (6, 4, 2):
            s.circle(12, 13, r, d, fill=False)
        s.px(12, 13, l)
    elif theme == "starlight":                # sea-star shell
        s.polygon([(12, 2), (14, 9), (21, 9), (15, 14), (18, 21), (12, 17), (6, 21), (9, 14), (3, 9), (10, 9)], m)
        s.px(12, 10, GOLD); s.px(11, 11, GOLD); s.px(13, 11, GOLD); s.px(12, 12, GOLD)
    elif theme == "forest":                   # snail shell with a little foot
        s.circle(12, 11, 8, m)
        for r in (5, 3, 1):
            s.circle(12, 11, r, d, fill=False)
        s.rect(4, 18, 20, 21, "#93c488"); s.rect(2, 16, 5, 21, "#93c488"); s.px(2, 14, "#93c488"); s.px(4, 14, "#93c488")
    else:                                     # winter: frosted clam with icicle
        s.polygon([(12, 20), (3, 13), (4, 7), (12, 4), (20, 7), (21, 13)], m)
        for x in (6, 9, 12, 15, 18):
            s.line(12, 19, x, 6, d)
        s.rect(4, 4, 20, 5, WHITE); s.polygon([(10, 19), (14, 19), (12, 23)], WHITE)
    s.px(5, 8, l)
    edge(s)
    return s


def stat_wax_seal(theme):                                                   # noqa: F811
    (m, d, l, a), t = T(theme)
    s = new(24, 24)
    if theme == "post":                       # square postal stamp seal with perforations
        s.rect(3, 3, 20, 20, m)
        for x in range(3, 21, 3):
            s.px(x, 2, m); s.px(x, 21, m); s.px(2, x, m); s.px(21, x, m)
    elif theme == "garden":                   # flower-petal rim
        for ax, ay in ((12, 5), (19, 10), (16, 18), (8, 18), (5, 10)):
            s.circle(ax, ay, 4, m)
        s.circle(12, 12, 7, m)
    elif theme == "starlight":                # starburst rim
        s.polygon([(12, 1), (15, 8), (22, 6), (17, 12), (22, 18), (15, 16), (12, 23), (9, 16), (2, 18), (7, 12), (2, 6), (9, 8)], m)
    else:                                     # festival: heart-shaped seal
        s.circle(7, 9, 5, m); s.circle(16, 9, 5, m); s.polygon([(2, 11), (21, 11), (12, 21)], m)
    s.ellipse(5, 4, 10, 8, l, only=m)
    if theme != "festival":
        s.circle(12, 12, 6, d, fill=False)
    motif(s, 12, 12 if theme != "festival" else 10, t["motif"], a)
    edge(s)
    return s


_bs = furn_bookshelf


def furn_bookshelf(theme):                                                  # noqa: F811
    s = _bs(theme)
    (m, d, l, a), t = T(theme)
    if theme == "garden":
        s.rect(20, 3, 28, 8, "#c98a5a"); s.rect(22, 0, 26, 3, "#5f9a6a")      # flower pot on top
        for x in (66, 72, 78):
            s.rect(x, 4, x + 2, 7, ("#f4a6a0", "#ffd86b", "#fff6e2")[(x // 6) % 3]); s.line(x + 1, 8, x + 1, 10, "#5f9a6a")
    return s


WORLD.update({"item.collectible.shell": coll_shell, "item.stationery.wax_seal": stat_wax_seal,
              "world.furniture.bookshelf": furn_bookshelf})
