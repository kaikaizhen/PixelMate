"""X1: animal costumes (user-requested extension, catalog assets/catalog/擴充設計.csv).
Front layer covers body + head (face window); optional back layer carries the tail behind the body."""
import catalog
import gear_costume
from common import ANCHOR, FRAME, write_strip
from gear_base import finish


def build():
    entries = {}
    for r in catalog.load_extra():
        if not r["assetKey"].startswith("equip.costume."):
            continue
        kind = r["assetKey"].rsplit(".", 1)[1]
        meta = dict(frameWidth=FRAME, frameHeight=FRAME, anchor=ANCHOR, rigs=["star_01"], designId=r["design_id"],
                    name=r["display_name"], batch=r["batch"])
        back = gear_costume.tail(kind)
        rel = f"sprites/equipment/costumes/{kind}.png"
        write_strip([finish(gear_costume.costume(kind))], rel)
        front = dict(type="image", layer="pet_outfit", slot="costume", z=2, occupies=["body", "head"], path=rel, **meta)
        if back is not None:
            brel = f"sprites/equipment/costumes/{kind}_tail.png"
            write_strip([finish(back)], brel)
            front["backKey"] = r["assetKey"] + ".tail"
            entries[front["backKey"]] = dict(type="image", layer="pet_back_accessory", slot="costume_back", z=0,
                                             path=brel, **meta)
        entries[r["assetKey"]] = front
    return entries, {}
