"""Animal costumes (布偶裝 / onesies) for pet.star_01.

Two layers per animal:
  front  `equip.costume.<animal>`       pet_outfit  — covers body + head, with a face window
  back   `equip.costume.<animal>.tail`  pet_back_accessory — tail drawn *behind* the body so it
                                        grows out of the pet instead of floating beside it
Each animal has its own silhouette (ears / horns / mane / tail / paws); never a recolor."""
import math

from gear_base import *  # noqa: F401,F403
from gear_base import BODY, LINE, WHITE, GOLD, edge, new, body_inside, blit, MOTIFS

# main, dark, light, inner, extra
A = {
    "dog":     ("小狗布偶裝", ("#c9935a", "#8a5a34", "#f0d0a0", "#f4a6a0", "#6a4226")),
    "cat":     ("小貓布偶裝", ("#e8a860", "#b87430", "#fff0d8", "#f4a6a0", "#b87430")),
    "rabbit":  ("兔兔布偶裝", ("#f4f0ee", "#cfc6cc", "#ffffff", "#f6a8b8", "#cfc6cc")),
    "bear":    ("小熊布偶裝", ("#a8744a", "#744a2c", "#d9ae82", "#d9ae82", "#744a2c")),
    "fox":     ("狐狸布偶裝", ("#e8803a", "#b0521c", "#fff2e0", "#2c2230", "#b0521c")),
    "panda":   ("熊貓布偶裝", ("#f6f4f2", "#cdc8cc", "#ffffff", "#2c2630", "#2c2630")),
    "penguin": ("企鵝布偶裝", ("#40506a", "#27344a", "#f4f6fa", "#f2a23c", "#27344a")),
    "frog":    ("青蛙布偶裝", ("#7fc468", "#4f9248", "#d6f0a8", "#f4a6a0", "#4f9248")),
    "shiba":   ("柴犬布偶裝", ("#e0a050", "#a8681e", "#fff4e0", "#f4a6a0", "#6a4226")),
    "tiger":   ("老虎布偶裝", ("#f0a030", "#b86a14", "#fff2d8", "#f4a6a0", "#2c2230")),
    "cow":     ("乳牛布偶裝", ("#fbfaf8", "#d6d2d4", "#ffffff", "#f4a6a0", "#2c2630")),
    "sheep":   ("綿羊布偶裝", ("#f3efe6", "#cfc6b4", "#ffffff", "#4a3a3a", "#4a3a3a")),
    "pig":     ("小豬布偶裝", ("#f6a8b8", "#d4788c", "#ffd6de", "#e8708a", "#d4788c")),
    "mouse":   ("小鼠布偶裝", ("#b8b4c0", "#827e90", "#e6e2ea", "#f6a8b8", "#827e90")),
    "duck":    ("小鴨布偶裝", ("#ffd84a", "#d4a022", "#fff2a8", "#f2a23c", "#d4a022")),
    "lion":    ("獅子布偶裝", ("#e8b050", "#a86c20", "#fff0c8", "#f4a6a0", "#8a4e1c")),
    "unicorn": ("獨角獸布偶裝", ("#fbf6fb", "#d8cde0", "#ffffff", "#f6a8d0", "#b8a0e8")),
    "dino":    ("恐龍布偶裝", ("#6cc07c", "#3f8f58", "#d6f0b0", "#f2a23c", "#e87a5a")),
    "koala":   ("無尾熊布偶裝", ("#a8a8b4", "#76768a", "#e2e2ea", "#f6e0e8", "#4a4458")),
    "owl":     ("貓頭鷹布偶裝", ("#a8805a", "#6e4c30", "#f0dcc0", "#f2a23c", "#4a3020")),
}
TAILLESS = {"penguin", "frog", "koala", "owl", "panda_none"}


# ---------------------------------------------------------------- ears / head gear (front layer)
def _ears(s, k, p):
    m, d, l, inner, x = p
    if k == "dog":
        s.polygon([(5, 11), (14, 8), (15, 18), (11, 29), (5, 27), (3, 18)], x); s.polygon([(6, 12), (13, 10), (14, 18), (10, 26), (6, 24)], m)
        s.polygon([(42, 11), (33, 8), (32, 18), (36, 29), (42, 27), (44, 18)], x); s.polygon([(41, 12), (34, 10), (33, 18), (37, 26), (41, 24)], m)
    elif k == "cat":
        s.polygon([(10, 16), (11, 4), (20, 12)], m); s.polygon([(12, 13), (12, 8), (17, 12)], inner)
        s.polygon([(37, 16), (36, 4), (28, 12)], m); s.polygon([(35, 13), (35, 8), (31, 12)], inner)
    elif k == "rabbit":
        s.ellipse(11, -3, 19, 15, m); s.ellipse(13, 0, 17, 13, inner)
        s.ellipse(28, -3, 36, 15, m); s.ellipse(30, 0, 34, 13, inner)
    elif k == "bear":
        s.circle(11, 12, 4, m); s.circle(11, 12, 2, inner); s.circle(36, 12, 4, m); s.circle(36, 12, 2, inner)
    elif k == "fox":
        s.polygon([(9, 17), (10, 2), (21, 12)], m); s.polygon([(10, 2), (12, 7), (9, 8)], inner)
        s.polygon([(38, 17), (37, 2), (26, 12)], m); s.polygon([(37, 2), (35, 7), (38, 8)], inner)
    elif k == "panda":
        s.circle(11, 11, 4, inner); s.circle(36, 11, 4, inner)
    elif k == "penguin":
        s.polygon([(20, 12), (24, 6), (28, 12)], m)
    elif k == "frog":
        s.circle(14, 10, 5, m); s.circle(33, 10, 5, m)
        s.circle(14, 10, 3, WHITE); s.circle(33, 10, 3, WHITE); s.rect(13, 9, 15, 11, LINE); s.rect(32, 9, 34, 11, LINE)
    elif k == "shiba":
        s.polygon([(8, 16), (11, 3), (20, 12)], m); s.polygon([(11, 12), (12, 7), (16, 11)], l)
        s.polygon([(39, 16), (36, 3), (27, 12)], m); s.polygon([(36, 12), (35, 7), (31, 11)], l)
    elif k == "tiger":
        s.circle(11, 12, 4, m); s.circle(11, 12, 2, inner); s.circle(36, 12, 4, m); s.circle(36, 12, 2, inner)
        s.line(9, 9, 12, 9, x); s.line(35, 9, 38, 9, x)
    elif k == "cow":
        s.polygon([(3, 12), (12, 11), (11, 18), (3, 17)], m); s.polygon([(44, 12), (35, 11), (36, 18), (44, 17)], m)   # side ears
        s.rect(4, 13, 8, 16, x); s.rect(39, 13, 43, 16, WHITE)
        s.polygon([(13, 12), (12, 5), (17, 10)], "#e8d8b0"); s.polygon([(34, 12), (35, 5), (30, 10)], "#e8d8b0")        # horns
    elif k == "sheep":
        for cx, cy in ((10, 14), (15, 9), (32, 9), (37, 14), (24, 7)):
            if cx != 24:
                s.circle(cx, cy, 4, m)
        s.polygon([(3, 22), (10, 20), (10, 27), (3, 26)], x); s.polygon([(44, 22), (37, 20), (37, 27), (44, 26)], x)   # side ears
    elif k == "pig":
        s.polygon([(9, 15), (12, 5), (20, 11)], m); s.polygon([(11, 11), (12, 8), (16, 10)], inner)
        s.polygon([(38, 15), (35, 5), (27, 11)], m); s.polygon([(36, 11), (35, 8), (31, 10)], inner)
        s.px(15, 11, d); s.px(32, 11, d)
    elif k == "mouse":
        s.circle(10, 10, 6, m); s.circle(10, 10, 4, inner); s.circle(37, 10, 6, m); s.circle(37, 10, 4, inner)
    elif k == "duck":
        s.ellipse(15, 14, 32, 19, inner); s.line(16, 16, 31, 16, d)                      # bill across the brow
        s.polygon([(22, 10), (24, 4), (26, 10)], m)                                      # feather tuft
    elif k == "lion":
        s.circle(10, 15, 3, m); s.circle(10, 15, 1, inner); s.circle(37, 15, 3, m); s.circle(37, 15, 1, inner)
    elif k == "unicorn":
        s.polygon([(13, 14), (15, 0), (19, 12)], GOLD); s.line(14, 10, 18, 9, "#f2b84a"); s.line(14, 6, 17, 5, "#f2b84a")
        s.polygon([(28, 12), (32, 4), (36, 14)], m); s.polygon([(30, 11), (32, 7), (34, 12)], inner)
        s.polygon([(10, 16), (9, 8), (16, 12)], m); s.polygon([(11, 14), (11, 10), (14, 12)], inner)
    elif k == "dino":
        for xx in (14, 20, 26, 32):
            s.polygon([(xx, 13), (xx + 2, 6), (xx + 4, 13)], x)
    elif k == "koala":
        s.circle(9, 12, 7, m); s.circle(9, 12, 4, inner); s.circle(38, 12, 7, m); s.circle(38, 12, 4, inner)
    elif k == "owl":
        s.polygon([(10, 16), (8, 3), (19, 12)], m); s.polygon([(37, 16), (39, 3), (28, 12)], m)
        s.line(11, 6, 15, 11, d); s.line(36, 6, 32, 11, d)


# ---------------------------------------------------------------- tails (back layer: behind the body)
def tail(k):
    m, d, l, inner, x = A[k][1]
    s = new()
    # every tail starts inside the body (hidden by it) around (30..36, 36..44) and grows outward
    if k == "dog":
        s.polygon([(30, 42), (40, 36), (46, 30), (47, 34), (42, 42), (34, 46)], m)
    elif k == "cat":
        s.polygon([(32, 44), (42, 42), (46, 36), (45, 28), (42, 30), (42, 36), (36, 40)], m)
        s.line(45, 30, 46, 33, d); s.line(43, 37, 46, 38, d)
    elif k == "rabbit":
        s.circle(38, 41, 5, l)
    elif k == "bear":
        s.circle(37, 42, 4, m)
    elif k == "fox":
        s.polygon([(30, 42), (40, 40), (46, 33), (47, 26), (44, 31), (38, 36)], m)
        s.polygon([(47, 26), (46, 31), (43, 32), (44, 28)], WHITE)
    elif k == "panda":
        s.ellipse(33, 40, 45, 46, x); s.line(34, 41, 38, 41, d)
    elif k == "shiba":                                  # curled over the back
        s.circle(40, 34, 6, m); s.circle(40, 34, 3, None) if False else None
        s.circle(40, 34, 3, l)
    elif k == "tiger":
        s.polygon([(31, 44), (42, 42), (47, 34), (46, 26), (43, 28), (43, 35), (36, 40)], m)
        for yy in (30, 35, 40):
            s.line(42, yy, 46, yy - 1, x)
    elif k == "cow":
        s.line(32, 42, 42, 40, d); s.line(42, 40, 44, 34, d); s.line(32, 43, 42, 41, m)
        s.polygon([(42, 36), (46, 33), (46, 38), (43, 40)], x)
    elif k == "sheep":
        s.circle(38, 42, 4, m)
    elif k == "pig":
        s.circle(40, 38, 4, m); s.circle(40, 38, 2, None) if False else None; s.circle(40, 38, 2, d)
    elif k == "mouse":
        s.line(32, 44, 40, 45, inner); s.line(40, 45, 45, 40, inner); s.line(45, 40, 46, 34, inner)
        s.line(32, 45, 40, 46, d); s.line(40, 46, 46, 41, d)
    elif k == "duck":
        s.polygon([(32, 44), (41, 40), (45, 34), (42, 38), (46, 40), (40, 46)], m)
    elif k == "lion":
        s.line(32, 43, 42, 44, d); s.line(42, 44, 46, 38, d); s.circle(45, 35, 3, x)
    elif k == "unicorn":
        for i, c in enumerate(("#f6a8d0", "#b8a0e8", "#8fc4e8")):
            s.polygon([(32, 42 + i), (42, 38 + i * 2), (46, 30 + i * 3), (42, 43 + i)], c)
    elif k == "dino":
        s.polygon([(30, 42), (41, 40), (47, 44), (40, 46), (32, 46)], m)
        s.polygon([(38, 40), (40, 36), (42, 40)], x); s.polygon([(42, 42), (44, 38), (46, 43)], x)
    else:
        return None
    edge(s)
    return s


# ---------------------------------------------------------------- front layer
def costume(k):
    m, d, l, inner, x = A[k][1]
    p = A[k][1]
    s = new()
    _ears(s, k, p)
    for y in range(12, 46):
        for xx in range(48):
            if body_inside(xx, y, 1):
                s.px(xx, y, m)
    for y in range(12, 46):
        for xx in range(48):
            if s.get(xx, y) == m and xx > 36:
                s.px(xx, y, d)
    s.ellipse(15, 35, 32, 46, l, only=m)                             # belly patch
    s.ellipse(5, 19, 42, 37, l if k not in ("lion", "sheep") else m, only=m)   # hood trim ring
    # ---- markings
    if k == "cat":
        for xx in (19, 24, 29):
            s.line(xx, 13, xx, 16, d)
        for yy in (38, 42):
            s.line(8, yy, 13, yy, d); s.line(34, yy, 39, yy, d)
    elif k == "dog":
        s.circle(33, 15, 3, x); s.rect(21, 41, 26, 44, WHITE, only=l)
    elif k == "rabbit":
        s.ellipse(15, 36, 32, 46, WHITE, only=l)
    elif k == "fox":
        s.polygon([(5, 28), (11, 30), (6, 34)], WHITE); s.polygon([(42, 28), (36, 30), (41, 34)], WHITE)
        s.rect(22, 12, 26, 15, WHITE, only=m)
    elif k == "panda":
        s.rect(1, 36, 11, 44, x, only=m); s.rect(36, 36, 46, 44, x, only=m)
        s.circle(10, 27, 5, x, only=l); s.circle(37, 27, 5, x, only=l)
    elif k == "penguin":
        s.rect(1, 34, 7, 44, x, only=m); s.rect(40, 34, 46, 44, x, only=m)
        s.ellipse(9, 20, 38, 36, l, only=m)
    elif k == "frog":
        s.ellipse(11, 38, 36, 46, l, only=m)
        for xx, yy in ((9, 16), (38, 17), (21, 14)):
            s.rect(xx, yy, xx + 2, yy + 1, d)
    elif k == "shiba":
        s.polygon([(5, 27), (12, 31), (6, 36)], l); s.polygon([(42, 27), (35, 31), (41, 36)], l)
        s.rect(22, 12, 25, 18, l, only=m)
    elif k == "tiger":
        for yy in (14, 17):
            s.line(14, yy, 19, yy + 1, x); s.line(33, yy, 28, yy + 1, x)
        for yy in (38, 42):
            s.line(6, yy, 12, yy + 1, x); s.line(41, yy, 35, yy + 1, x)
        s.line(24, 12, 24, 16, x)
    elif k == "cow":
        s.circle(10, 16, 4, x, only=m); s.circle(38, 38, 5, x, only=m); s.circle(11, 40, 4, x, only=m); s.rect(2, 30, 8, 34, x, only=m)
        s.ellipse(14, 40, 33, 46, "#f6c8c8", only=l)
    elif k == "sheep":
        for xx, yy in ((7, 33), (15, 40), (33, 41), (40, 34), (24, 44)):
            s.circle(xx, yy, 3, m)
            s.circle(xx, yy, 3, d, fill=False)
    elif k == "pig":
        s.ellipse(14, 38, 33, 46, l, only=m)
    elif k == "mouse":
        s.ellipse(14, 38, 33, 46, l, only=m)
    elif k == "duck":
        s.polygon([(1, 36), (8, 32), (9, 42), (3, 44)], d); s.polygon([(46, 36), (39, 32), (38, 42), (44, 44)], d)   # wings
    elif k == "lion":                                    # shaggy mane ring around the face window
        s.ellipse(14, 36, 33, 46, l, only=m)
        for i in range(24):
            ang = math.radians(i * 15)
            s.circle(int(23.5 + 19.5 * math.cos(ang)), int(28 + 10.5 * math.sin(ang)), 3, x if i % 2 else d, only="opaque")
    elif k == "unicorn":
        for i, c in enumerate(("#f6a8d0", "#b8a0e8", "#8fc4e8")):
            s.line(38 + i, 16, 41 + i, 28, c); s.line(39 + i, 16, 42 + i, 28, c)                  # rainbow mane
    elif k == "dino":
        for yy in (36, 40, 44):
            s.rect(14, yy, 33, yy, l, only=l)
        for xx in range(8, 40, 6):
            s.px(xx, 34, d)
    elif k == "koala":
        s.ellipse(15, 37, 32, 46, l, only=m)
    elif k == "owl":
        for yy in range(37, 46, 3):
            for xx in range(15, 33, 4):
                s.line(xx, yy, xx + 2, yy + 1, l)
        s.rect(1, 34, 7, 44, d, only=m); s.rect(40, 34, 46, 44, d, only=m)
    # ---- face window: the pet's own face layer shows through
    cx, cy, rx, ry = 23.5, 28, 16.5, 7.5
    for yy in range(48):
        for xx in range(48):
            if ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2 <= 1:
                s.px(xx, yy, None)
    # ---- paws
    for x0 in (12, 26):
        pawc = inner if k in ("penguin", "duck", "dino") else m
        s.ellipse(x0 - 1, 42, x0 + 9, 47, pawc)
        s.line(x0 + 2, 45, x0 + 2, 46, d); s.line(x0 + 5, 45, x0 + 5, 46, d)
        s.px(x0 + 3, 43, WHITE if k in ("penguin", "duck", "dino") else l)
    edge(s)
    return s
