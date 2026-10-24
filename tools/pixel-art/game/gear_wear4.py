"""B4 wearables: postal cap themes, sleep cap, explorer hat, sailor, sweater, pajamas themes, hairpins, shawl, sash,
skates, trail boots, books, lantern, snack box."""
import gear_wear
import gear_wear2
import gear_wear3
from gear_base import *  # noqa: F401,F403
from gear_base import (LINE, BODY, WHITE, PAPER, PAPER_D, GOLD, GOLD_D, WOOD, WOOD_D, WOOD_L, new, edge, motif, pattern,
                       blit, MOTIFS, body_inside)
from gear_wear import T, _hem, _pocket


def _fill(s, y0, y1, pal, inset=1):
    m, d, l, a = pal
    for y in range(y0, y1):
        for x in range(48):
            if body_inside(x, y, inset):
                s.px(x, y, m)
    for y in range(y0, y1):
        for x in range(48):
            if s.get(x, y) == m and (x < 13 or x > 35):
                s.px(x, y, l if x < 13 else d)


# ------------------------------------------------------------------ hats
def hat_post4(theme):
    if theme == "post":
        return gear_wear3.hat_post(theme)
    (m, d, l, a), t = T(theme)
    s = new()
    s.ellipse(11, 7, 36, 17, d); s.ellipse(11, 7, 33, 14, m, only="opaque")
    s.rect(11, 13, 36, 15, a); s.rect(11, 13, 36, 13, l)
    s.ellipse(15, 14, 40, 19, "#1c1720"); s.ellipse(15, 14, 38, 17, "#302638", only="opaque")
    s.rect(21, 8, 26, 12, GOLD); s.px(23, 9, WHITE)
    motif(s, 23, 10, t["motif"], d)
    if theme == "garden":
        blit(s, 12, 5, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":
        s.line(12, 14, 36, 14, WHITE)
    elif theme == "starlight":
        s.px(14, 9, GOLD); s.px(32, 10, GOLD); s.px(28, 7, WHITE)
    else:
        blit(s, 30, 4, MOTIFS["leaf"], "#93c488")
    edge(s)
    return s


def hat_sleep(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.ellipse(12, 9, 36, 18, m); s.ellipse(12, 9, 32, 15, l, only=m)
    s.rect(11, 14, 37, 17, d)                                               # cuff
    pattern(s, (13, 10, 35, 13), t["pattern"], (m, d, l, a), only=m)
    tail = {"post": [(30, 12), (40, 14), (44, 22), (40, 24), (36, 18)], "garden": [(30, 12), (42, 12), (46, 18), (42, 20), (36, 16)],
            "seaside": [(30, 12), (40, 10), (45, 14), (44, 20), (38, 16)], "starlight": [(30, 12), (42, 15), (45, 24), (42, 26), (37, 20)],
            "forest": [(30, 12), (40, 12), (44, 19), (41, 23), (36, 17)]}[theme]
    s.polygon(tail, m); s.polygon(tail[:3] + [tail[0]], l)
    tip = tail[3]
    if theme == "post":
        s.rect(tip[0] - 2, tip[1], tip[0] + 2, tip[1] + 3, PAPER)
    elif theme == "garden":
        blit(s, tip[0] - 2, tip[1] - 1, MOTIFS["flower"], "#f4a6a0")
    elif theme == "seaside":
        s.circle(tip[0], tip[1] + 1, 2, WHITE)
    elif theme == "starlight":
        blit(s, tip[0] - 2, tip[1] - 1, MOTIFS["star"], GOLD)
    else:
        s.ellipse(tip[0] - 2, tip[1], tip[0] + 2, tip[1] + 4, "#a8764a"); s.rect(tip[0] - 2, tip[1], tip[0] + 2, tip[1] + 1, "#6e4c30")
    edge(s)
    return s


def hat_explorer(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    if theme == "post":                                   # pith helmet
        s.ellipse(10, 13, 38, 20, "#e8d8b0"); s.ellipse(14, 4, 34, 17, "#f2e6c4"); s.rect(14, 12, 34, 14, m)
        s.rect(22, 3, 26, 5, "#c9b78f"); s.rect(14, 13, 34, 13, l)
    elif theme == "garden":                               # flat-brim straw explorer with flower band
        s.ellipse(8, 14, 40, 21, "#e8d08a"); s.ellipse(14, 6, 34, 17, "#f0dc9c"); s.rect(14, 12, 34, 14, "#7fbf7a")
        for x, c in ((16, "#f4a6a0"), (22, "#ffd86b"), (28, "#fff6e2")):
            s.rect(x, 11, x + 2, 13, c)
        s.line(34, 18, 40, 24, "#a8764a")                 # chin strap
    else:                                                 # seaside: sun hat with neck flap + shell
        s.ellipse(9, 14, 39, 21, "#fff6e2"); s.ellipse(14, 6, 34, 17, "#fffdf6"); s.rect(14, 12, 34, 14, m)
        s.rect(10, 18, 14, 25, "#fff6e2"); s.rect(34, 18, 38, 25, "#fff6e2")
        s.polygon([(21, 7), (27, 7), (26, 11), (22, 11)], "#f3dca0"); s.line(24, 7, 24, 11, "#d9b060")
    edge(s)
    return s


# ------------------------------------------------------------------ outfits
def outfit_sailor(theme):
    """Sailor top: a V-shaped white collar band (not a filled bib), accent stripes, neckerchief at the point."""
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    _fill(s, 34, 46, pal)
    band = "#fffdf6"
    s.polygon([(9, 34), (15, 34), (26, 44), (24, 45), (21, 45)], band)       # left V band
    s.polygon([(38, 34), (32, 34), (21, 44), (23, 45), (26, 45)], band)      # right V band
    s.line(9, 34, 21, 45, a); s.line(38, 34, 26, 45, a)                       # accent edge stripe
    s.rect(9, 34, 38, 34, band)                                               # collar top edge
    ribbon = {"post": "#d9574f", "picnic": "#ffd86b", "rainy": "#f2c94c", "starlight": "#ffd86b", "forest": "#a8764a", "winter": "#d9574f"}[theme]
    s.polygon([(20, 38), (24, 40), (20, 43)], ribbon); s.polygon([(28, 38), (24, 40), (28, 43)], ribbon); s.rect(23, 39, 25, 41, d)
    if theme == "starlight":
        blit(s, 11, 37, MOTIFS["star"], GOLD); blit(s, 31, 37, MOTIFS["star"], GOLD)
    elif theme == "rainy":
        s.line(24, 43, 24, 45, "#f2c94c"); s.rect(10, 41, 14, 43, "#f2c94c"); s.rect(33, 41, 37, 43, "#f2c94c")
    elif theme == "forest":
        blit(s, 11, 38, MOTIFS["leaf"], "#93c488"); blit(s, 32, 38, MOTIFS["leaf"], "#93c488")
    elif theme == "picnic":
        pattern(s, (4, 41, 43, 45), "check", pal, only=m)
    elif theme == "winter":
        for x in range(5, 44, 4):
            s.px(x, 44, WHITE) if s.get(x, 44) == m else None
        s.rect(5, 36, 7, 38, WHITE) if s.get(6, 37) == m else None
    else:
        motif(s, 33, 42, "stamp", WHITE)
    edge(s)
    return s


def outfit_sweater(theme):
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    _fill(s, 32, 46, pal)
    for y in range(36, 46, 2):                                              # knit rows
        s.line(2, y, 45, y, d, ) if False else None
    for x in range(2, 46, 3):
        for y in range(34, 46):
            if s.get(x, y) in (m, l, d) and (y // 2 + x // 3) % 2 == 0:
                s.px(x, y, l if s.get(x, y) != l else m)
    neck = {"post": "turtle", "picnic": "crew", "rainy": "turtle", "starlight": "v", "forest": "v", "winter": "turtle"}[theme]
    if neck == "turtle":
        s.rect(15, 32, 32, 35, d); s.line(15, 35, 32, 35, l)
    elif neck == "crew":
        s.rect(16, 32, 31, 33, d); s.rect(18, 34, 29, 34, d)
    else:
        s.polygon([(17, 32), (30, 32), (24, 39)], BODY); s.line(17, 32, 24, 39, d); s.line(30, 32, 24, 39, d)
    s.rect(2, 44, 45, 45, d, only=m) if False else None
    pattern(s, (3, 37, 44, 43), {"post": "stripe_h", "picnic": "dots", "rainy": "stripe_v", "starlight": "stars", "forest": "chevron", "winter": "check"}[theme], pal, only=m)
    if theme == "post":                                                    # envelope patch on the chest
        s.rect(14, 38, 21, 43, PAPER); s.line(14, 38, 17, 41, "#d9574f"); s.line(21, 38, 18, 41, "#d9574f")
    elif theme == "rainy":                                                 # hood-string toggles + pocket
        s.rect(18, 35, 19, 39, WHITE); s.rect(28, 35, 29, 39, WHITE); s.rect(14, 41, 33, 44, d)
    elif theme == "picnic":
        motif(s, 24, 41, t["motif"], a)
    elif theme == "forest":                                                # elbow-style leaf patches + wooden toggles
        blit(s, 8, 40, MOTIFS["leaf"], "#93c488"); blit(s, 34, 40, MOTIFS["leaf"], "#93c488"); s.rect(23, 38, 25, 39, "#a8764a"); s.rect(23, 42, 25, 43, "#a8764a")
    elif theme == "winter":                                                # snowflake yoke + fluffy cuff row
        blit(s, 9, 38, MOTIFS["snow"], WHITE); blit(s, 33, 38, MOTIFS["snow"], WHITE)
        for x in range(3, 45, 2):
            if s.get(x, 44) is not None:
                s.px(x, 44, WHITE)
    else:
        motif(s, 24, 41, t["motif"], a); s.rect(2, 38, 45, 38, GOLD_D)
    for x in range(3, 45, 3):
        s.px(x, 45, d)                                                      # ribbed hem
    edge(s)
    return s


_pj = gear_wear3.outfit_pajamas


def outfit_pajamas4(theme):
    if theme in ("post", "picnic"):
        return _pj(theme)
    (m, d, l, a), t = T(theme)
    pal = (m, d, l, a)
    s = new()
    _fill(s, 33, 46, pal)
    s.line(24, 34, 24, 45, d)
    for y in (36, 40, 44):
        s.px(26, y, WHITE)
    s.rect(14, 33, 33, 34, WHITE, only=m)
    if theme == "rainy":                                  # raindrops on a cloud pocket
        s.ellipse(14, 38, 22, 43, WHITE); blit(s, 16, 42, MOTIFS["drop"], a) if False else None
        for x in (15, 18, 21):
            s.px(x, 44, a)
    elif theme == "starlight":                            # moon & stars all over
        pattern(s, (3, 35, 44, 45), "stars", pal, only=m); s.circle(32, 40, 3, "#fff0a8"); s.circle(33, 39, 3, m)
    elif theme == "forest":                               # leaf print + hooded collar
        for x, y in ((8, 38), (30, 36), (16, 43), (36, 43)):
            blit(s, x, y, MOTIFS["leaf"], l)
        s.rect(12, 33, 35, 35, "#5f9a6a", only=m)
    else:                                                 # winter: snowflakes + fluffy trim
        pattern(s, (3, 35, 44, 45), "dots", pal, only=m); blit(s, 30, 38, MOTIFS["snow"], WHITE)
        for x in range(3, 45):
            if s.get(x, 44) == m:
                s.px(x, 44, WHITE)
    edge(s)
    return s


# ------------------------------------------------------------------ accessories
_hairpin = gear_wear3.acc_hairpin


def acc_hairpin4(theme):
    if theme not in ("forest", "festival"):
        return _hairpin(theme)
    (m, d, l, a), t = T(theme)
    s = new()
    x, y = 12, 13
    s.line(x, y + 2, x + 6, y, "#b8b8cc")
    if theme == "forest":
        blit(s, x - 2, y - 3, MOTIFS["leaf"], "#5f9a6a"); blit(s, x + 2, y - 3, MOTIFS["leaf"], "#93c488")
    else:
        s.polygon([(x - 3, y - 3), (x + 1, y - 1), (x - 3, y + 1)], m); s.polygon([(x + 4, y - 3), (x, y - 1), (x + 4, y + 1)], a)
        s.rect(x - 1, y - 2, x + 1, y, GOLD)
    edge(s)
    return s


def acc_shawl(theme):
    """Slim stole: a thin band draped over the shoulders, two short ends hanging in front."""
    (m, d, l, a), t = T(theme)
    s = new()
    for y, x0, x1 in ((34, 7, 40), (35, 6, 41)):                             # 2-row band along the shoulders / collarbone
        for x in range(x0, x1 + 1):
            if body_inside(x, y, 0):
                s.px(x, y, m)
    s.rect(7, 34, 40, 34, l, only=m)
    s.rect(6, 35, 41, 35, d, only=m)
    for k in range(7, 41, 3):                                                # tiny pattern dashes
        if s.get(k, 34) == l:
            s.px(k, 34, a if theme != "winter" else WHITE)
    s.rect(18, 36, 21, 43, m); s.rect(18, 36, 18, 43, l); s.rect(21, 36, 21, 43, d)     # left end
    s.rect(26, 36, 29, 41, m); s.rect(26, 36, 26, 41, l); s.rect(29, 36, 29, 41, d)     # shorter right end
    s.rect(22, 34, 25, 36, GOLD)                                             # pin
    end = {"friendship": "heart", "garden": "scallop", "seaside": "wave", "starlight": "star", "forest": "leaf", "festival": "fringe"}[theme]
    if end == "heart":
        blit(s, 17, 44, [".#.#.", "#####", ".###.", "..#.."], m)
    elif end == "scallop":
        s.px(18, 44, m); s.px(20, 44, m); s.px(26, 42, m); s.px(28, 42, m)
    elif end == "wave":
        s.line(18, 44, 21, 44, a); s.line(26, 42, 29, 42, a)
    elif end == "star":
        blit(s, 17, 44, MOTIFS["star"], GOLD); s.px(27, 42, GOLD)
    elif end == "leaf":
        blit(s, 17, 44, MOTIFS["leaf"], "#93c488"); blit(s, 26, 42, MOTIFS["leaf"], "#93c488")
    else:
        for x in (18, 20, 26, 28):
            s.line(x, 44 if x < 24 else 42, x, 46 if x < 24 else 44, a)
    edge(s)
    return s


def acc_sash(theme):
    """Pageant sash: starts on the left shoulder, runs down the body edge (clear of the cheek), then crosses the
    belly under the mouth to a bow and tail at the right hip."""
    (m, d, l, a), t = T(theme)
    s = new()
    s.line(4, 27, 38, 46, m); s.line(4, 28, 38, 47, m)                        # one straight 2px diagonal: left shoulder -> right hip
    s.line(5, 27, 39, 46, d)
    s.line(4, 27, 36, 45, l) if False else None
    s.rect(34, 43, 38, 46, a); s.px(35, 44, d)                                # small knot on the hip
    if theme == "friendship":                                                  # forked tail + heart badge
        s.polygon([(33, 46), (34, 47), (36, 46)], a); s.polygon([(36, 46), (38, 47), (38, 45)], a)
        s.px(18, 37, WHITE); s.px(19, 37, WHITE); s.px(18, 38, WHITE)
    else:                                                                      # garden: leaf badge, straight tail
        s.rect(33, 46, 36, 47, "#5f9a6a"); s.px(18, 37, "#fff6e2"); s.px(19, 36, "#fff6e2")
    edge(s)
    return s


# ------------------------------------------------------------------ feet
_skate = gear_wear3.feet_skate


def feet_skate4(theme):
    if theme == "picnic":
        return _skate(theme)
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (11, 26):
        s.ellipse(x0, 40, x0 + 11, 46, m); s.ellipse(x0 + 1, 40, x0 + 7, 43, l, only=m)
        s.rect(x0, 38, x0 + 10, 40, d if theme == "rainy" else WHITE)         # high ankle cuff / fur
        s.rect(x0 - 1, 46, x0 + 12, 46, "#b8b8cc"); s.rect(x0, 47, x0 + 11, 47, "#8a8aa0")
        s.px(x0 + 3, 42, WHITE); s.px(x0 + 5, 42, WHITE); s.px(x0 + 7, 41, a)
        if theme == "winter":
            s.circle(x0 + 5, 37, 2, WHITE)
        else:
            s.line(x0 + 1, 44, x0 + 9, 44, a)
    edge(s)
    return s


def feet_trail_boot(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    for x0 in (11, 26):
        s.rect(x0 + 1, 40, x0 + 10, 46, m); s.rect(x0 + 1, 40, x0 + 3, 46, l); s.rect(x0 + 8, 40, x0 + 10, 46, d)
        s.rect(x0, 39, x0 + 11, 41, "#6e4c30")                              # padded collar
        s.rect(x0, 45, x0 + 11, 47, "#3a2a34")                               # lug sole
        for x in range(x0, x0 + 12, 2):
            s.px(x, 47, "#6a5a64")
        s.line(x0 + 4, 42, x0 + 7, 42, a); s.line(x0 + 4, 44, x0 + 7, 44, a)
        if theme == "rainy":
            s.rect(x0 + 8, 42, x0 + 10, 44, "#f2c94c")
    edge(s)
    return s


# ------------------------------------------------------------------ handheld
_book = gear_wear3.held_book


def held_book4(theme):
    if theme != "adventure":
        return _book(theme)
    (m, d, l, a), t = T("adventure")
    s = new()
    s.rect(33, 35, 46, 46, "#9a6a2a"); s.rect(33, 35, 46, 36, "#d49a4a"); s.rect(33, 35, 34, 46, "#6e4c30")
    s.rect(36, 38, 44, 44, "#e8d8b0"); s.line(36, 41, 44, 41, "#c9b78f")
    s.rect(44, 35, 45, 40, "#d9574f")
    motif(s, 40, 40, "compass", "#6e4c30")
    s.line(36, 38, 44, 44, "#9a6a2a") if False else None
    edge(s)
    return s


def held_lantern(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.line(40, 34, 40, 36, "#6e6e84")
    shape = {"post": "box", "garden": "round", "starlight": "star", "adventure": "tall"}[theme]
    glow = "#ffe89a"
    if shape == "box":
        s.rect(34, 36, 46, 46, "#6e6e84"); s.rect(36, 38, 44, 44, glow); s.line(40, 38, 40, 44, "#d9b060"); s.rect(33, 35, 47, 37, d)
        s.rect(37, 40, 43, 42, PAPER) if False else None
    elif shape == "round":
        s.circle(40, 41, 6, "#b5e2a0"); s.circle(40, 41, 4, glow); s.line(34, 41, 46, 41, "#4f8f58"); s.rect(37, 34, 43, 36, "#4f8f58")
    elif shape == "star":
        blit(s, 37, 36, MOTIFS["star"], "#ffd86b"); s.rect(35, 36, 45, 46, None) if False else None
        s.polygon([(40, 36), (43, 41), (47, 41), (44, 44), (45, 48), (40, 45), (35, 48), (36, 44), (33, 41), (37, 41)], GOLD)
        s.px(40, 41, WHITE)
    else:
        s.rect(36, 36, 44, 47, "#9a6a2a"); s.rect(37, 38, 43, 45, glow); s.rect(35, 35, 45, 37, "#6e4c30"); s.line(40, 38, 40, 45, "#d9b060")
        s.line(36, 41, 44, 41, "#9a6a2a")
    edge(s)
    return s


def held_snack_box(theme):
    (m, d, l, a), t = T(theme)
    s = new()
    s.rect(32, 38, 46, 46, "#c98a5a"); s.rect(32, 38, 46, 40, "#e0a878"); s.rect(32, 45, 46, 46, "#8a5a34")
    s.rect(32, 36, 46, 38, "#fff6e2")                                       # lid
    s.rect(38, 36, 40, 46, "#d9574f")                                       # ribbon
    s.polygon([(36, 34), (39, 36), (36, 38)], "#d9574f"); s.polygon([(42, 34), (39, 36), (42, 38)], "#d9574f")
    s.rect(33, 41, 36, 44, PAPER); s.px(34, 42, "#d9574f")
    edge(s)
    return s


gear_wear.WEAR.update({
    "equip.hat.post": (hat_post4, "head"), "equip.hat.sleep": (hat_sleep, "head"), "equip.hat.explorer": (hat_explorer, "head"),
    "equip.outfit.pajamas": (outfit_pajamas4, "body"), "equip.outfit.sailor": (outfit_sailor, "body"),
    "equip.outfit.sweater": (outfit_sweater, "body"),
    "equip.accessory.hairpin": (acc_hairpin4, "front"), "equip.accessory.shawl": (acc_shawl, "front"),
    "equip.accessory.sash": (acc_sash, "front"),
    "equip.feet.skate": (feet_skate4, "feet"), "equip.feet.trail_boot": (feet_trail_boot, "feet"),
    "equip.handheld.book": (held_book4, "hand"), "equip.handheld.lantern": (held_lantern, "hand"),
    "equip.handheld.snack_box": (held_snack_box, "hand"),
})
