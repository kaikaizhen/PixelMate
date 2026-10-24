"""B3 wearables: top hat, wizard hat, postman cap, bucket(forest), dress, suit, pajamas, backpack, hairpin,
festival glasses, winter slipper, long boot, skate, adventure canteen, wand, book."""
import gear_wear
import gear_wear2
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, new, edge, motif, pattern,
                       blit, MOTIFS, body_inside)
from gear_wear import T, _hem, _pocket, POCKET


# ------------------------------------------------------------------ hats
def hat_top(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.ellipse(9, 15, 38, 20, d)                                             # brim
    s.ellipse(9, 15, 36, 18, m, only="opaque")
    h = {"post": 11, "garden": 9, "seaside": 10, "starlight": 7, "forest": 11}[theme]
    s.rect(15, h, 32, 17, m); s.rect(15, h, 17, 17, l); s.rect(30, h, 32, 17, d)   # tall crown
    s.rect(15, h, 32, h + 1, l)
    s.rect(15, 14, 32, 16, a if theme != "post" else "#d9574f")             # hat band
    pattern(s, (15, h + 2, 32, 13), t["pattern"], (m, d, l, a), only=m)
    if theme == "post":
        s.rect(21, 4 + 4, 26, 12, PAPER) if False else None
        s.rect(20, 14, 27, 16, PAPER); s.px(23, 15, "#d9574f")
    elif theme == "garden":
        for x, y, c in ((16, 12, "#f4a6a0"), (30, 11, "#ffd86b"), (23, 8, "#fff6e2")):
            s.rect(x, y, x + 2, y + 2, c)
    elif theme == "seaside":
        s.line(16, 14, 31, 14, WHITE); blit(s, 22, 9, MOTIFS["wave"], WHITE)
    elif theme == "starlight":
        blit(s, 21, 8, MOTIFS["star"], GOLD); s.px(17, 9, GOLD); s.px(30, 11, GOLD)
    else:
        blit(s, 28, 7, MOTIFS["leaf"], "#93c488"); blit(s, 16, 11, MOTIFS["leaf"], "#5f9a6a")
    edge(s)
    return s


def hat_wizard(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.ellipse(8, 15, 39, 21, d)                                             # wide brim
    s.ellipse(8, 15, 37, 19, m, only="opaque")
    bend = {"post": 5, "garden": -6, "seaside": 7, "starlight": 0, "forest": -4}[theme]
    s.polygon([(15, 17), (32, 17), (28 + bend, 6), (24 + bend, 1), (22 + bend // 2, 6)], m)   # tall bent cone
    s.polygon([(15, 17), (22, 17), (22 + bend // 2, 6), (24 + bend, 1)], l)
    s.rect(14, 15, 33, 17, a if theme != "post" else "#d9574f")             # band
    pattern(s, (17, 8, 31, 14), "stars" if theme == "starlight" else t["pattern"], (m, d, l, a), only=m)
    if theme == "starlight":
        blit(s, 22 + bend, 2, MOTIFS["star"], GOLD)
    elif theme == "garden":
        blit(s, 16, 16, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":
        s.circle(24, 16, 1, WHITE); s.line(28, 17, 36, 20, "#a8dcf0")      # shell tassel
    elif theme == "forest":
        blit(s, 28, 13, MOTIFS["leaf"], "#93c488")
    else:
        s.rect(21, 14, 26, 17, PAPER); s.px(23, 15, "#d9574f")
    edge(s)
    return s


def hat_post(theme):
    (m, d, l, a), t = T("post")
    s = new()
    s.ellipse(11, 7, 36, 17, "#26305a")                                      # postal cap crown
    s.ellipse(11, 7, 33, 14, "#3f4d8f", only="opaque")
    s.rect(11, 13, 36, 15, "#d9574f"); s.rect(11, 13, 36, 13, "#ee8a8a")
    s.ellipse(15, 14, 40, 19, "#1c1720"); s.ellipse(15, 14, 38, 17, "#302638", only="opaque")   # peak
    s.rect(21, 8, 26, 12, GOLD); s.px(23, 9, WHITE); s.px(24, 10, GOLD_D)    # badge
    edge(s)
    return s


_bucket = gear_wear2.hat_bucket


def hat_bucket3(theme):
    if theme != "forest":
        return _bucket(theme)
    (m, d, l, a), t = T("forest")
    s = new()
    s.ellipse(9, 15, 39, 21, "#3a6a49"); s.ellipse(9, 15, 37, 19, m, only="opaque")
    s.polygon([(15, 8), (32, 8), (36, 17), (11, 17)], m); s.polygon([(15, 8), (22, 8), (19, 17), (11, 17)], l)
    s.rect(11, 14, 36, 16, "#a8764a")
    for x in range(14, 34, 5):                                              # sewn-on leaves
        blit(s, x, 9, MOTIFS["leaf"], ("#93c488", "#5f9a6a")[(x // 5) % 2])
    s.line(34, 16, 38, 22, "#a8764a")
    edge(s)
    return s


# ------------------------------------------------------------------ outfits
def outfit_suit(theme):
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    for y in range(33, 46):
        for x in range(48):
            if body_inside(x, y, 1):
                s.px(x, y, m)
    for y in range(33, 46):
        for x in range(48):
            if s.get(x, y) == m and (x < 13 or x > 35):
                s.px(x, y, l if x < 13 else d)
    pattern(s, (2, 33, 45, 45), t["pattern"], pal, only=m)
    if theme == "post":                                                    # classic notch lapel + red tie
        s.polygon([(16, 33), (23, 33), (23, 43)], d); s.polygon([(32, 33), (25, 33), (25, 43)], d)
        s.rect(23, 33, 25, 45, WHITE); s.polygon([(23, 34), (25, 34), (26, 41), (24, 43), (22, 41)], "#d9574f")
        s.px(27, 41, GOLD); s.px(27, 44, GOLD); s.rect(14, 41, 18, 42, WHITE)
    elif theme == "picnic":                                                # suspenders + big bow tie, no lapel
        s.rect(23, 33, 25, 45, WHITE)
        for x in (15, 32):
            s.line(x, 33, x + (3 if x < 20 else -3), 45, "#2c2630"); s.line(x + 1, 33, x + 1 + (3 if x < 20 else -3), 45, "#2c2630")
        s.polygon([(19, 34), (23, 36), (19, 38)], "#d9574f"); s.polygon([(29, 34), (25, 36), (29, 38)], "#d9574f"); s.rect(23, 35, 25, 37, "#b84a4a")
    elif theme == "rainy":                                                 # double-breasted trench with belt
        s.polygon([(14, 33), (23, 33), (21, 42)], d); s.polygon([(34, 33), (25, 33), (27, 42)], d)
        for y in (37, 41):
            s.rect(17, y, 18, y + 1, GOLD); s.rect(29, y, 30, y + 1, GOLD)
        s.rect(9, 42, 38, 43, "#a8764a"); s.rect(22, 41, 26, 44, GOLD); s.px(24, 42, WHITE)
    elif theme == "starlight":                                             # high collar + star brooch
        s.rect(15, 33, 32, 36, d); s.polygon([(15, 33), (23, 33), (19, 38)], l); s.polygon([(32, 33), (25, 33), (29, 38)], l)
        blit(s, 21, 38, MOTIFS["star"], GOLD); s.line(24, 43, 24, 45, a)
    elif theme == "forest":                                                # leaf lapels, open vest, no tie
        s.polygon([(16, 33), (23, 33), (21, 41)], "#5f9a6a"); s.polygon([(32, 33), (25, 33), (27, 41)], "#5f9a6a")
        s.rect(22, 33, 26, 45, "#f6ecd6"); blit(s, 16, 37, MOTIFS["leaf"], "#93c488"); blit(s, 28, 37, MOTIFS["leaf"], "#93c488")
        s.px(24, 40, "#a8764a"); s.px(24, 43, "#a8764a")
    else:                                                                  # winter: fur collar + toggle buttons
        s.rect(13, 33, 34, 35, WHITE); s.rect(12, 34, 35, 35, "#dbe8f4")
        for y in (38, 42):
            s.rect(21, y, 27, y + 1, "#8a6040"); s.rect(23, y - 1, 25, y + 2, "#a8764a")
        s.line(24, 36, 24, 45, d)
    if theme in ("post", "rainy", "starlight"):
        motif(s, 31, 41, t["motif"], a)
    edge(s)
    return s


def outfit_pajamas(theme):
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    for y in range(33, 46):
        for x in range(48):
            if body_inside(x, y, 1):
                s.px(x, y, m)
    for y in range(33, 46):
        for x in range(48):
            if s.get(x, y) == m and (x < 13 or x > 35):
                s.px(x, y, l if x < 13 else d)
    if theme == "post":                                                     # airmail stripes + envelope pocket
        for x in range(2, 46, 6):
            s.rect(x, 33, x + 1, 45, "#6fb1d6", only=m)
        s.rect(15, 38, 22, 43, PAPER); s.line(15, 38, 18, 41, PAPER_D); s.line(22, 38, 19, 41, PAPER_D)
    else:                                                                   # picnic: gingham + moon patch
        pattern(s, (2, 33, 45, 45), "check", pal, only=m)
        s.circle(32, 40, 3, "#c98a5a"); s.circle(30, 37, 1, "#c98a5a"); s.circle(34, 37, 1, "#c98a5a"); s.px(31, 40, LINE); s.px(33, 40, LINE)   # teddy patch
    s.line(24, 34, 24, 45, d)
    for y in (36, 40, 44):
        s.px(26, y, WHITE)
    s.rect(14, 33, 33, 34, WHITE, only=m)                                   # ruffled collar
    for x in range(14, 34, 2):
        s.px(x, 35, WHITE)
    edge(s)
    return s


# ------------------------------------------------------------------ accessories
def acc_backpack(theme):
    """Backpack straps only: each strap starts on the shoulder, hugs the body edge (clear of the cheeks) and
    meets a chest strap with a themed buckle charm. No side bags."""
    (m, d, l, a), t = T(theme)
    s = new()
    style = {"friendship": "heart", "garden": "flower", "seaside": "shell", "starlight": "moon", "forest": "leaf", "festival": "star"}[theme]
    for sx, sign in ((3, 1), (44, -1)):
        pts = [(sx, 27), (sx + 4 * sign, 34), (sx + 5 * sign, 39), (sx + 11 * sign, 41)]
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            s.line(x0, y0, x1, y1, m); s.line(x0 + sign, y0, x1 + sign, y1, m)
            s.line(x0 - sign, y0, x1 - sign, y1, l if sign > 0 else d)
        if theme in ("seaside", "garden", "festival"):                      # strap decoration along the shoulder
            for k, (x, y) in enumerate(((sx + sign, 29), (sx + 3 * sign, 32))):
                s.px(x, y, a if k % 2 == 0 else WHITE)
    s.line(14, 41, 33, 41, m); s.line(14, 42, 33, 42, d)                    # chest strap
    s.rect(21, 39, 26, 44, GOLD_D); s.rect(22, 40, 25, 43, GOLD)             # buckle plate
    if style == "heart":
        blit(s, 21, 44, MOTIFS["heart"], "#f06a8a")
    elif style == "flower":
        blit(s, 21, 44, MOTIFS["flower"], "#f4a6a0"); s.px(23, 46, GOLD)
    elif style == "shell":
        s.polygon([(21, 45), (26, 45), (25, 49), (22, 49)], "#f3dca0"); s.line(23, 45, 23, 48, "#d9b060")
    elif style == "moon":
        s.circle(23, 46, 2, "#fff0a8"); s.circle(24, 45, 2, "#40448a")
    elif style == "leaf":
        blit(s, 21, 44, MOTIFS["leaf"], "#5f9a6a")
    else:
        blit(s, 21, 44, MOTIFS["star"], GOLD)
    edge(s)
    return s


def acc_hairpin(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    x, y = 12, 13                                                           # clipped to the left of the dome
    s.line(x, y + 2, x + 6, y, "#b8b8cc")
    if theme == "friendship":
        blit(s, x - 2, y - 3, MOTIFS["heart"], m)
    elif theme == "garden":
        blit(s, x - 2, y - 3, MOTIFS["flower"], "#f4a6a0"); s.px(x, y - 1, GOLD)
    elif theme == "seaside":
        s.polygon([(x - 2, y + 1), (x + 3, y + 1), (x + 4, y - 2), (x - 3, y - 2)], "#f3dca0"); s.line(x, y + 1, x, y - 2, "#d9b060")
    else:
        blit(s, x - 2, y - 3, MOTIFS["star"], GOLD)
    s.px(36, 14, GOLD) if False else None
    edge(s)
    return s


_glasses = gear_wear2.acc_glasses


def acc_glasses3(theme):
    if theme != "festival":
        return _glasses(theme)
    (m, d, l, a), t = T("festival")
    s = new()
    for cx, cy in ((15, 26), (32, 26)):                                     # party glasses: star frames
        blit(s, cx - 3, cy - 3, ["..###..", ".#...#.", "#.....#", "#.....#", ".#...#.", "..###.."], m)
        s.px(cx + 4, cy - 4, GOLD)
    s.line(21, 25, 26, 25, m); s.line(10, 25, 6, 28, m); s.line(37, 25, 41, 28, m)
    for x, y in ((6, 22), (41, 22)):                                        # confetti on the arms
        s.px(x, y, a)
    edge(s)
    return s


# ------------------------------------------------------------------ feet
def feet_long_boot(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (11, 26):
        s.rect(x0 + 1, 38, x0 + 9, 47, m); s.rect(x0 + 1, 38, x0 + 3, 47, l); s.rect(x0 + 8, 38, x0 + 9, 47, d)
        s.rect(x0, 38, x0 + 10, 40, d)                                      # cuff
        s.rect(x0, 46, x0 + 11, 47, "#3a2a34")
        pattern(s, (x0 + 1, 41, x0 + 9, 45), t["pattern"], (m, d, l, a), only=m)
        if theme == "winter":
            s.rect(x0, 37, x0 + 10, 38, WHITE)
        elif theme == "rainy":
            s.rect(x0 + 2, 42, x0 + 4, 44, a)
        else:
            s.line(x0 + 4, 41, x0 + 6, 41, GOLD); s.line(x0 + 4, 43, x0 + 6, 43, GOLD)   # laces
    edge(s)
    return s


def feet_skate(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (11, 26):
        s.ellipse(x0, 41, x0 + 11, 46, m); s.ellipse(x0 + 1, 41, x0 + 7, 44, l, only=m)
        s.rect(x0 - 1, 46, x0 + 12, 46, "#b8b8cc")                          # blade
        s.rect(x0, 47, x0 + 11, 47, "#8a8aa0")
        s.rect(x0 + 1, 46, x0 + 1, 47, "#8a8aa0"); s.rect(x0 + 10, 46, x0 + 10, 47, "#8a8aa0")
        s.px(x0 + 3, 43, WHITE); s.px(x0 + 5, 43, WHITE); s.px(x0 + 7, 42, a)
    edge(s)
    return s


_slip = gear_wear2.feet_slipper


def feet_slipper3(theme):
    if theme != "winter":
        return _slip(theme)
    (m, d, l, a), t = T("winter")
    s = new()
    for x0 in (11, 26):
        s.ellipse(x0, 41, x0 + 11, 47, WHITE); s.ellipse(x0 + 1, 43, x0 + 10, 47, m, only="opaque")
        s.line(x0 + 1, 41, x0 + 10, 41, "#dbe8f4")
        s.circle(x0 + 5, 40, 2, WHITE)                                      # pom
        blit(s, x0 + 4, 44, MOTIFS["snow"], d)
    edge(s)
    return s


# ------------------------------------------------------------------ handheld
def held_wand(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.line(36, 46, 42, 34, WOOD_D); s.line(37, 46, 43, 34, WOOD)
    if theme == "post":                                   # quill-like wand with a wax-seal tip
        s.circle(43, 32, 3, "#d9574f"); s.px(42, 31, "#ee8a8a")
        s.polygon([(44, 34), (47, 28), (45, 36)], PAPER)
    elif theme == "garden":                               # flower on a stem
        blit(s, 41, 29, MOTIFS["flower"], "#f4a6a0"); s.px(43, 31, GOLD)
        s.line(39, 42, 36, 40, "#5f9a6a")
    elif theme == "starlight":                            # big star
        blit(s, 40, 28, MOTIFS["star"], GOLD); s.px(42, 29, WHITE)
    else:                                                 # adventure: compass crystal
        s.polygon([(43, 28), (46, 32), (43, 36), (40, 32)], "#6fb1d6"); s.line(43, 28, 43, 36, WHITE)
    edge(s)
    return s


def held_book(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.rect(33, 36, 46, 46, m); s.rect(33, 36, 46, 37, l); s.rect(33, 45, 46, 46, d)
    s.rect(33, 36, 34, 46, d)                                               # spine
    s.rect(36, 39, 44, 43, PAPER) if theme == "post" else None
    motif(s, 40, 41, t["motif"], a if theme != "post" else "#d9574f")
    s.rect(44, 36, 45, 40, GOLD)                                            # bookmark
    edge(s)
    return s


_canteen = gear_wear2.held_canteen


def held_canteen3(theme):
    if theme != "adventure":
        return _canteen(theme)
    (m, d, l, a), t = T("adventure")
    s = new()
    s.rect(36, 32, 45, 46, "#d49a4a"); s.rect(36, 32, 37, 46, "#f0c070"); s.rect(38, 29, 43, 32, WOOD_D)
    s.line(36, 38, 45, 38, "#9a6a2a"); s.line(36, 41, 45, 41, "#9a6a2a")      # leather straps
    motif(s, 41, 35, "compass", "#6e4c30")
    edge(s)
    return s


WEAR3 = {
    "equip.hat.top": (hat_top, "head"), "equip.hat.wizard": (hat_wizard, "head"), "equip.hat.post": (hat_post, "head"),
    "equip.hat.bucket": (hat_bucket3, "head"),
    "equip.outfit.suit": (outfit_suit, "body"),
    "equip.outfit.pajamas": (outfit_pajamas, "body"),
    "equip.accessory.backpack": (acc_backpack, "front"), "equip.accessory.hairpin": (acc_hairpin, "front"),
    "equip.accessory.glasses": (acc_glasses3, "front"),
    "equip.feet.long_boot": (feet_long_boot, "feet"), "equip.feet.skate": (feet_skate, "feet"),
    "equip.feet.slipper": (feet_slipper3, "feet"),
    "equip.handheld.wand": (held_wand, "hand"), "equip.handheld.book": (held_book, "hand"),
    "equip.handheld.canteen": (held_canteen3, "hand"),
}
gear_wear.WEAR.update(WEAR3)
