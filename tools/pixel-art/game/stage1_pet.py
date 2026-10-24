"""S1: star pet body / face / props / animations (layered, anchor-aligned). Original art."""
import os
from common import ANCHOR, FRAME, hard_alpha, hex2rgb, recolor, write_strip, ASSETS  # noqa: F401
import pet
from pet import draw_face, sprite
from pixelstudio import Sprite
from PIL import Image

KEY = "pet.star_01"
DIR = "sprites/pets/star_01"
COLORS = {
    "cream": None,
    "mint": {  # body ramp -> mint; star / cheeks / outline untouched
        "#fbe3c8": hex2rgb("#cdeedb"), "#efc3a4": hex2rgb("#9fd4b8"), "#fff4e4": hex2rgb("#eefbf2"),
        "#e9a98e": hex2rgb("#7fbf9c"),
    },
}

# body sheets (face excluded): name -> list of kwargs for pet.sprite
BODY_SHEETS = {
    "idle": [dict(bob=0, sway=0), dict(bob=0, sway=1), dict(bob=1, sway=2), dict(bob=1, sway=1)],
    "walk": [dict(bob=0, sway=0, step=0), dict(bob=1, sway=1, step=1), dict(bob=0, sway=2, step=0), dict(bob=1, sway=1, step=3)],
    "sleep": [dict(bob=2, sway=0), dict(bob=3, sway=0)],
}
BODY_BOB = {name: [k["bob"] for k in frames] for name, frames in BODY_SHEETS.items()}

FACES = {  # expr -> draw_face kwargs
    "normal": {}, "happy": dict(happy=True), "sleepy": dict(sleepy=True),
    "blink": dict(blink=True), "look_down": dict(look="down"), "look_right": dict(look="right"),
}


def body_frames(name, color):
    out = []
    for kw in BODY_SHEETS[name]:
        s = sprite(face=False, **kw)
        im = hard_alpha(s.composite(1).copy())
        if COLORS[color]:
            im = recolor(im, COLORS[color])
        out.append(im)
    return out


def face_image(kind):
    s = Sprite(FRAME, FRAME, palette=pet.PAL)
    draw_face(s, 0, **FACES[kind])
    return hard_alpha(s.composite(1).copy())


def book_frames():
    BLUE, PAPER = "#8fc4e8", "#fff6e2"
    out = []
    for flip in (False, True):
        s = Sprite(FRAME, FRAME)
        s.rect(12, 33, 35, 43, BLUE); s.rect(12, 33, 35, 43, pet.LINE, fill=False)
        s.rect(14, 35, 23, 41, PAPER); s.rect(25, 35, 33, 41, PAPER)
        s.line(24, 34, 24, 42, pet.LINE)
        for yy in (37, 39):
            s.line(15, yy, 22, yy, "#cdbfae"); s.line(26, yy, 32, yy, "#cdbfae")
        if flip:
            s.polygon([(24, 35), (29, 33), (29, 39), (24, 41)], "#ffffff"); s.line(24, 35, 29, 33, pet.LINE)
        out.append(hard_alpha(s.composite(1).copy()))
    return out


def frame(sheet, index, face, attach=(0, 0), root=(0, 0), props=()):
    f = {"body": {"sheet": sheet, "index": index}, "face": face, "attach": list(attach), "root": list(root)}
    if props:
        f["props"] = list(props)
    return f


def animations():
    bob = BODY_BOB
    a = {}
    a["idle"] = dict(fps=4, loop=True, frames=[frame("idle", i, "normal", (0, bob["idle"][i])) for i in range(4)])
    a["blink"] = dict(fps=8, loop=False, frames=[frame("idle", 0, "normal"), frame("idle", 0, "blink")])
    a["walk"] = dict(fps=6, loop=True, frames=[frame("walk", i, "normal", (0, bob["walk"][i])) for i in range(4)])
    a["sleep"] = dict(fps=1, loop=True, frames=[frame("sleep", i, "sleepy", (0, bob["sleep"][i])) for i in range(2)])
    a["read"] = dict(fps=3, loop=True, frames=[
        frame("idle", 0, "look_down", (0, 0), props=[{"key": f"{KEY}.prop.book", "index": 0}]),
        frame("idle", 0, "look_down", (0, 0), props=[{"key": f"{KEY}.prop.book", "index": 0}]),
        frame("idle", 1, "look_down", (0, 0), props=[{"key": f"{KEY}.prop.book", "index": 1}]),
        frame("idle", 0, "look_down", (0, 0), props=[{"key": f"{KEY}.prop.book", "index": 0}]),
    ])
    hop = [0, -3, -6, -3]
    a["react_happy"] = dict(fps=8, loop=False, frames=[frame("idle", 0, "happy", (0, 0), root=(0, hop[i])) for i in range(4)])
    a["receive_mail"] = dict(fps=6, loop=False, frames=[
        frame("idle", 0, "happy", (0, 0), root=(0, [0, -2, -5, -2][i])) for i in range(4)])
    a["rain_watch"] = dict(fps=2, loop=True, frames=[frame("idle", i, "look_right", (0, bob["idle"][i])) for i in range(4)])
    return {f"{KEY}.anim.{k}": v for k, v in a.items()}


def build():
    entries = {}
    for color in COLORS:
        for name in BODY_SHEETS:
            frames = body_frames(name, color)
            rel = f"{DIR}/body_{color}_{name}.png"
            write_strip(frames, rel)
            entries[f"{KEY}.body.{color}.{name}"] = dict(
                type="spritesheet", layer="pet_body", path=rel, frames=len(frames),
                frameWidth=FRAME, frameHeight=FRAME, fps=4, anchor=ANCHOR)
    for kind in FACES:
        rel = f"{DIR}/face_{kind}.png"
        write_strip([face_image(kind)], rel)
        entries[f"{KEY}.face.{kind}"] = dict(type="image", layer="pet_face", path=rel,
                                              frameWidth=FRAME, frameHeight=FRAME, anchor=ANCHOR)
    rel = f"{DIR}/prop_book.png"
    write_strip(book_frames(), rel)
    entries[f"{KEY}.prop.book"] = dict(type="spritesheet", layer="pet_front_accessory", path=rel, frames=2,
                                       frameWidth=FRAME, frameHeight=FRAME, fps=2, anchor=ANCHOR)
    return entries, animations()
