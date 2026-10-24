"""Wearables for pet.star_01: hat / outfit / accessory / feet / handheld (48x48, shared anchor)."""
from gear_base import *  # noqa: F401,F403
from gear_base import LINE, BODY, BODY_D, BODY_L, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, THEMES, new, edge, motif, \
    pattern, finish, body_inside, blit, MOTIFS


def T(theme):
    t = THEMES[theme]
    return t["pal"], t


# ------------------------------------------------------------------ hats (slot head)
def hat_straw(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    wide = {"post": 15, "garden": 17, "seaside": 16, "starlight": 14, "forest": 14}[theme]
    s.ellipse(24 - wide, 13, 24 + wide, 19, "#f0d08a")                      # brim
    s.ellipse(24 - wide, 13, 24 + wide, 17, "#fbe6a8", only="opaque")
    s.rect(24 - wide + 2, 18, 24 + wide - 2, 19, "#d9b060")
    s.ellipse(14, 7, 33, 17, "#f0d08a")                                      # crown
    s.ellipse(14, 7, 31, 14, "#fbe6a8", only="opaque")
    for yy in range(8, 15, 2):                                              # weave
        for xx in range(15 + (yy % 4 == 0) * 1, 33, 3):
            s.px(xx, yy, "#d9b060")
    s.rect(14, 13, 33, 15, m)                                                # band
    s.rect(14, 15, 33, 15, d)
    s.rect(14, 13, 20, 13, l)
    motif(s, 24, 14, t["motif"], a)
    if theme == "post":      # luggage tag hanging off the band
        s.rect(31, 16, 35, 21, PAPER); s.px(33, 17, d); s.line(32, 19, 34, 19, d)
    elif theme == "garden":  # little flower cluster on the brim
        for (x, y, c) in ((11, 16, "#f4a6a0"), (14, 18, "#fff6e2"), (36, 17, "#f4a6a0")):
            s.rect(x, y, x + 1, y + 1, c)
    elif theme == "seaside": # ribbon tails
        s.line(34, 15, 38, 21, m); s.line(35, 15, 40, 20, d)
    elif theme == "starlight":
        for x, y in ((12, 15), (36, 14)):
            blit(s, x - 2, y - 2, MOTIFS["star"], GOLD)
    else:                    # forest: leaf sprig tucked in the band
        blit(s, 31, 8, MOTIFS["leaf"], "#5f9a6a")
    edge(s)
    return s


def hat_knit(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.ellipse(13, 7, 34, 20, m)                                              # beanie
    s.ellipse(13, 7, 31, 16, l, only="opaque")
    s.ellipse(15, 8, 34, 20, m, only="opaque")
    s.rect(12, 15, 35, 19, d)                                                # cuff
    for xx in range(12, 36, 2):
        s.line(xx, 15, xx, 19, m)
    pattern(s, (14, 8, 33, 14), t["pattern"], (m, d, l, a), only=m)
    if theme == "post":                      # folded flap with a pinned tag
        s.rect(28, 7, 33, 11, PAPER); s.line(28, 7, 30, 9, d); s.line(33, 7, 31, 9, d)
    elif theme == "forest":                  # leaf tuft
        blit(s, 14, 3, MOTIFS["leaf"], "#5f9a6a"); blit(s, 18, 4, MOTIFS["leaf"], "#93c488")
    elif theme == "winter":                 # fluffy pom + ear flap
        s.circle(24, 6, 3, WHITE); s.rect(11, 18, 14, 23, m); s.rect(33, 18, 36, 23, m)
    elif theme == "starlight":
        blit(s, 22, 3, MOTIFS["star"], GOLD)
    else:
        s.rect(23, 5, 25, 7, a)
    motif(s, 24, 12, t["motif"], a if theme != "winter" else d)
    edge(s)
    return s


def hat_beret(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    tilt = {"post": -2, "garden": 2, "seaside": -2, "starlight": 2, "forest": -2}[theme]
    s.polygon([(10, 15 + tilt), (16, 9), (30, 8), (38, 13 - tilt), (36, 18), (14, 19)], m)
    s.ellipse(11, 9, 36, 18, m, only="opaque")
    s.polygon([(12, 14 + tilt), (17, 10), (26, 9), (22, 13), (14, 16)], l)
    s.rect(30, 12, 37, 17, d, only=m)
    s.rect(12, 18, 36, 19, d)
    s.rect(22, 6, 25, 8, d)                                                  # stalk
    if theme == "post":
        s.rect(26, 13, 31, 16, PAPER); s.line(26, 13, 28, 15, d); s.line(31, 13, 29, 15, d)
    elif theme == "garden":
        for x, y in ((14, 11), (19, 10), (30, 15)):
            s.rect(x, y, x + 1, y + 1, a)
    elif theme == "seaside":
        s.line(13, 16, 36, 16, l)
    pattern(s, (12, 10, 35, 17), "stars" if theme == "starlight" else ("chevron" if theme == "forest" else "dots"),
            (m, d, l, a), only=m)
    motif(s, 29, 13, t["motif"], a)
    edge(s)
    return s


# ------------------------------------------------------------------ outfits (slot body, hugs pet mask)
HEM = {"post": "perforated", "garden": "scallop", "seaside": "wave", "starlight": "zigzag", "forest": "jagged",
       "winter": "fluff", "picnic": "ruffle", "rainy": "drip"}
NECK = {"post": "v", "starlight": "v", "winter": "v", "garden": "u", "picnic": "u", "rainy": "square", "forest": "square"}
POCKET = {"post": "flap", "garden": "round", "seaside": "wide", "starlight": "star", "forest": "leaf",
          "winter": "round", "picnic": "square", "rainy": "flap"}


def _hem(s, style, pal, y0=44, y1=47):
    m, d, l, a = pal
    for x in range(48):
        if s.get(x, y0 + 1) is None:
            continue
        for y in range(y0, y1):
            if s.get(x, y) is None:
                continue
            if style == "scallop" and y == y0 + 1 and (x // 3) % 2 == 0:
                s.px(x, y, None)
            elif style == "zigzag" and y - y0 <= (x % 6 if x % 6 < 3 else 6 - x % 6) - 1 + 0:
                s.px(x, y, None)
            elif style == "jagged" and y == y0 + 1 and x % 4 in (0, 1):
                s.px(x, y, None)
            elif style == "wave" and y == y0 + 1 and (x % 6) in (0, 1, 2):
                s.px(x, y, a)
            elif style == "perforated" and y == y0 and x % 3 == 0:
                s.px(x, y, a)
            elif style == "fluff" and y == y0 + 1:
                s.px(x, y, WHITE)
            elif style == "ruffle" and y == y0 and (x // 2) % 2 == 0:
                s.px(x, y, l)
            elif style == "drip" and y == y0 + 1 and x % 7 in (2, 3):
                s.px(x, y, a)


def _pocket(s, style, pal, x, y):
    m, d, l, a = pal
    if style == "round":
        s.ellipse(x, y, x + 7, y + 4, d); s.px(x + 2, y + 1, l)
    elif style == "star":
        blit(s, x + 1, y, MOTIFS["star"], a)
    elif style == "leaf":
        blit(s, x + 1, y, MOTIFS["leaf"], l)
    elif style == "wide":
        s.rect(x - 2, y + 1, x + 9, y + 3, d); s.line(x - 2, y + 1, x + 9, y + 1, l)
    elif style == "flap":
        s.rect(x, y, x + 7, y + 4, d); s.rect(x, y, x + 7, y + 1, l)
    else:
        s.rect(x, y, x + 7, y + 4, d); s.rect(x + 1, y + 1, x + 6, y + 3, l)


def outfit(kind, theme):
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    top = {"vest": 34, "hoodie": 33, "raincoat": 33}[kind]
    for y in range(top, 46):
        for x in range(48):
            if body_inside(x, y, 1):
                s.px(x, y, m)
    for y in range(top, 46):                                                # light left / shadow right
        for x in range(48):
            if s.get(x, y) == m:
                if x < 13:
                    s.px(x, y, l)
                elif x > 34:
                    s.px(x, y, d)
    pattern(s, (4, top, 43, 45), t["pattern"], pal, only=m)
    neck = NECK.get(theme, "v")
    if kind == "vest":
        if neck == "v":
            s.polygon([(19, top), (29, top), (24, top + 5)], BODY)
            s.line(19, top, 24, top + 5, d); s.line(29, top, 24, top + 5, d)
        elif neck == "u":
            s.ellipse(18, top - 4, 30, top + 4, BODY); s.ellipse(18, top - 4, 30, top + 4, d, fill=False)
        else:
            s.rect(19, top, 29, top + 2, BODY); s.rect(18, top, 18, top + 2, d); s.rect(30, top, 30, top + 2, d)
            s.line(19, top + 3, 29, top + 3, d)
        _pocket(s, POCKET[theme], pal, 10, 40)
        motif(s, 31, 41, t["motif"], a)
    elif kind == "hoodie":
        s.rect(8, top, 40, top + 3, l, only=m)                               # hood bunched at neck
        s.line(8, top + 3, 40, top + 3, d)
        s.line(21, top + 3, 20, top + 8, WHITE); s.line(27, top + 3, 28, top + 8, WHITE)  # strings
        s.px(20, top + 8, a); s.px(28, top + 8, a)
        _pocket(s, POCKET[theme], pal, 16, 40)
        s.rect(14, 41, 33, 41, d, only=m) if POCKET[theme] in ("wide", "flap") else None
        motif(s, 24, 37, t["motif"], a)
    else:                                                                     # raincoat
        s.rect(8, top, 40, top + 2, d, only=m)                               # collar
        s.rect(8, 43, 40, 45, d, only=m)                                     # hem band
        for y in range(top + 3, 43, 3):
            s.px(24, y, a); s.px(23, y, LINE)
        s.line(10, 40, 38, 40, d)                                            # belt
        motif(s, 15, 37, t["motif"], a)
        _pocket(s, POCKET[theme], pal, 29, 36) if POCKET[theme] != "wide" else None
    _hem(s, HEM[theme], pal)
    return s


# ------------------------------------------------------------------ accessories (front, slot front_or_back)
def acc_scarf(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for y in range(34, 39):
        for x in range(48):
            if body_inside(x, y, 1):
                s.px(x, y, m)
    s.rect(6, 36, 41, 36, l, only=m) if False else None
    pattern(s, (4, 34, 43, 38), {"seaside": "stripe_v", "festival": "stripe_v"}.get(theme, t["pattern"]),
            (m, d, l, a), only=m)
    s.rect(30, 38, 35, 45, m); s.rect(30, 38, 31, 45, l); s.rect(34, 38, 35, 45, d)     # hanging tail
    for x in range(30, 36, 2):
        s.line(x, 45, x, 47, a)                                                          # fringe
    motif(s, 23, 36, t["motif"], a)
    if theme == "festival":                                                   # bunting flags along the hem
        for x in range(8, 40, 6):
            s.polygon([(x, 39), (x + 4, 39), (x + 2, 43)], (a, l)[(x // 6) % 2])
    elif theme == "friendship":                                               # heart-shaped tail end
        motif(s, 32, 46, "heart", a)
    elif theme == "forest":
        for x in (30, 33):
            blit(s, x, 44, [".#.", "###"], l)
    edge(s)
    return s


def acc_bowtie(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.polygon([(14, 35), (23, 38), (14, 42)], m)
    s.polygon([(34, 35), (25, 38), (34, 42)], m)
    s.polygon([(14, 35), (23, 38), (14, 38)], l)
    s.polygon([(34, 42), (25, 38), (34, 38)], d)
    pattern(s, (13, 35, 35, 42), t["pattern"], (m, d, l, a), only=m)
    s.rect(22, 36, 26, 40, d); s.rect(23, 37, 25, 39, a)
    if theme == "festival":                                                   # streamers
        s.line(21, 41, 18, 46, m); s.line(27, 41, 30, 46, l)
    elif theme == "forest":                                                   # leaf wings
        s.polygon([(14, 35), (12, 38), (14, 42)], m); s.polygon([(34, 35), (36, 38), (34, 42)], m)
    elif theme == "friendship":
        motif(s, 24, 38, "heart", "#fff6e2")
    elif theme == "starlight":
        blit(s, 22, 35, MOTIFS["star"], GOLD)
    elif theme == "garden":
        motif(s, 24, 38, "flower", "#fff6e2")
    elif theme == "seaside":
        s.line(14, 38, 22, 38, WHITE); s.line(26, 38, 34, 38, WHITE)
    edge(s)
    return s


# ------------------------------------------------------------------ feet (slot feet)
def feet_rain_boot(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (12, 26):
        s.rect(x0, 41, x0 + 8, 47, m)
        s.rect(x0, 41, x0 + 2, 47, l); s.rect(x0 + 7, 41, x0 + 8, 47, d)
        s.rect(x0 - 1, 41, x0 + 9, 42, d)                                    # cuff
        s.rect(x0 - 1, 46, x0 + 10, 47, "#3a2a34")                            # sole
        pattern(s, (x0, 43, x0 + 8, 45), t["pattern"], (m, d, l, a), only=m)
    motif(s, 16, 44, t["motif"], a) if theme != "picnic" else None
    motif(s, 30, 44, t["motif"], a)
    edge(s)
    return s


def feet_sneaker(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (12, 26):
        s.ellipse(x0 - 1, 42, x0 + 10, 47, m)
        s.rect(x0 - 1, 46, x0 + 10, 47, WHITE)                               # sole
        s.ellipse(x0 + 5, 43, x0 + 10, 47, l, only=m)                        # toe cap
        s.px(x0 + 2, 43, WHITE); s.px(x0 + 4, 43, WHITE)                     # laces
        s.line(x0, 45, x0 + 8, 45, d)
        pattern(s, (x0, 43, x0 + 4, 44), t["pattern"], (m, d, l, a), only=m)
        if theme == "rainy":                                                 # reflective heel patch + yellow stripe
            s.rect(x0 - 1, 44, x0 + 1, 45, a); s.line(x0 + 2, 44, x0 + 9, 44, a)
        elif theme == "winter":                                              # fleece cuff + pom
            s.rect(x0 - 1, 41, x0 + 7, 43, WHITE); s.px(x0 + 8, 41, WHITE)
        else:                                                                # picnic: gingham toe
            pattern(s, (x0 + 5, 44, x0 + 10, 46), "check", (m, d, l, a), only=l)
    edge(s)
    return s


# ------------------------------------------------------------------ handheld (slot hand)
def held_letter(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    paper = {"post": PAPER, "garden": "#e6f5d8", "starlight": "#cfd2f4", "adventure": "#e8cfa0"}[theme]
    s.rect(31, 35, 45, 45, paper)
    s.polygon([(31, 35), (45, 35), (38, 41)], l if theme != "post" else PAPER_D)
    s.line(31, 35, 38, 41, d); s.line(45, 35, 38, 41, d)
    if theme == "post":                                                       # airmail edge
        for x in range(31, 46, 3):
            s.rect(x, 44, x + 1, 45, m); s.rect(x + 1, 44, x + 2, 45, a if False else "#6fb1d6")
    elif theme == "garden":
        s.line(31, 45, 45, 45, m)
        motif(s, 34, 43, "flower", "#f4a6a0")
    elif theme == "starlight":
        pattern(s, (31, 42, 45, 45), "stars", (m, d, l, a), only=paper)
    else:
        s.line(38, 36, 38, 45, "#a8764a"); s.line(31, 40, 45, 40, "#a8764a")   # rope tie
    s.rect(37, 40, 39, 42, m if theme != "adventure" else "#d9574f")
    s.px(38, 41, WHITE)
    edge(s)
    return s


def held_bouquet(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    stem = "#5f9a6a"
    s.polygon([(37, 46), (41, 46), (44, 38), (34, 38)], {"post": PAPER, "garden": "#e6f5d8",
                                                          "starlight": "#40448a", "adventure": "#d9b088"}[theme])
    s.line(36, 38, 39, 46, d); s.line(43, 38, 41, 46, d)
    bloom = {"post": ["#d9574f", "#fff6e2", "#f4a6a0"], "garden": ["#f4a6a0", "#ffd86b", "#fff6e2"],
             "starlight": ["#ffd86b", "#9fa4e6", "#fff6e2"], "adventure": ["#d49a4a", "#6fb1d6", "#ffd86b"]}[theme]
    spots = [(36, 34), (41, 33), (39, 29), (35, 30), (43, 30)] if theme != "post" else [(37, 33), (42, 33), (39, 29)]
    for i, (x, y) in enumerate(spots):
        c = bloom[i % 3]
        s.rect(x, y, x + 2, y + 2, c); s.px(x + 1, y + 1, GOLD_D if c != "#ffd86b" else WHITE)
        s.px(x + 1, y + 3, stem); s.px(x + 1, y + 4, stem)
    if theme == "starlight":
        blit(s, 39, 22, MOTIFS["star"], GOLD)
    edge(s)
    return s


WEAR = {
    "equip.hat.straw": (hat_straw, "head"), "equip.hat.knit": (hat_knit, "head"), "equip.hat.beret": (hat_beret, "head"),
    "equip.outfit.vest": (lambda th: outfit("vest", th), "body"),
    "equip.outfit.hoodie": (lambda th: outfit("hoodie", th), "body"),
    "equip.outfit.raincoat": (lambda th: outfit("raincoat", th), "body"),
    "equip.accessory.scarf": (acc_scarf, "front"), "equip.accessory.bowtie": (acc_bowtie, "front"),
    "equip.feet.rain_boot": (feet_rain_boot, "feet"), "equip.feet.sneaker": (feet_sneaker, "feet"),
    "equip.handheld.letter": (held_letter, "hand"), "equip.handheld.bouquet": (held_bouquet, "hand"),
}
