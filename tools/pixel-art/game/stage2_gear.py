"""E2: wearables (hat / outfit / accessory / feet / handheld) for pet.star_01, driven by the catalog.

Only catalog rows of the batches in BATCHES that have a generator are built; everything else
stays `todo` (never counted as done)."""
import os

import catalog
import gear_wear
import gear_wear2  # noqa: F401  (registers B2 generators)
from common import ANCHOR, FRAME, write_strip
from gear_base import finish

BATCHES = [b for b in os.environ.get("GEAR_BATCHES", "B1,B2").split(",") if b]
CATS = {"帽子", "服飾", "配件", "鞋履", "手持物"}
LAYER = {"head": "pet_front_accessory", "body": "pet_outfit", "front": "pet_front_accessory",
         "feet": "pet_outfit", "hand": "pet_front_accessory"}
Z = {"feet": 1, "body": 2, "front": 3, "hand": 4, "head": 5}     # draw order inside the pet stack
DIR = {"head": "hats", "body": "outfits", "front": "accessories", "feet": "feet", "hand": "handheld"}


def split(key):
    pre, name = key.rsplit(".", 1)
    typ, theme = name.rsplit("_", 1)
    return f"{pre}.{typ}", theme


def build():
    rows = [r for r in catalog.in_scope(catalog.load()) if r["batch"] in BATCHES and r["category"] in CATS]
    entries = {}
    for r in rows:
        reg, theme = split(r["assetKey"])
        if reg not in gear_wear.WEAR:
            continue
        fn, slot = gear_wear.WEAR[reg]
        im = finish(fn(theme))
        rel = f"sprites/equipment/{DIR[slot]}/{r['assetKey'].rsplit('.', 1)[1]}.png"
        write_strip([im], rel)
        entries[r["assetKey"]] = dict(
            type="image", layer=LAYER[slot], slot=slot, z=Z[slot], path=rel, frameWidth=FRAME, frameHeight=FRAME,
            anchor=ANCHOR, rigs=["star_01"], designId=r["design_id"], name=r["display_name"], batch=r["batch"])
    return entries, {}
