"""E3: room / inventory / mail / weather / effect assets, driven by the catalog (only rows with a generator)."""
import os

import catalog
import gear_world
import gear_world2  # noqa: F401  (registers B2 generators)
from common import ANCHOR, FRAME, write_strip
from gear_base import finish

BATCHES = [b for b in os.environ.get("GEAR_BATCHES", "B1,B2").split(",") if b]
CATS = {"家具", "布置", "收藏品", "信件", "天氣", "特效"}
# prefix -> (dir, manifest layer, slot, frame size)
KIND = {
    "world.furniture": ("furniture", "furniture_back", "room", 96),
    "world.decor": ("decor", "furniture_back", "room", 48),
    "item.collectible": ("items", "ui_icon", "inventory", 24),
    "item.stationery": ("mail", "ui_icon", "mail", 24),
}
SIZE_OVERRIDE = {"world.decor.floor_tile": ("room_background", 48), "item.stationery.letter_border": ("ui_icon", 48)}


def build():
    entries = {}
    for r in catalog.in_scope(catalog.load()):
        if r["batch"] not in BATCHES or r["category"] not in CATS:
            continue
        key = r["assetKey"]
        pre, name = key.rsplit(".", 1)
        if key.startswith("world.weather."):
            fn = gear_world.WEATHER.get(key)
            if not fn:
                continue
            rel = f"backgrounds/{name}.png"
            write_strip([finish(fn())], rel, 240, 240)
            entries[key] = dict(type="image", layer="window_weather", slot="window_weather", path=rel,
                                frameWidth=240, frameHeight=240, designId=r["design_id"], name=r["display_name"], batch=r["batch"])
            continue
        if key.startswith("fx."):
            fn = gear_world.FX.get(key)
            if not fn:
                continue
            frames = [finish(f) for f in fn()]
            rel = f"sprites/effects/{name}.png"
            write_strip(frames, rel)
            entries[key] = dict(type="spritesheet", layer="foreground_effects", slot="effect", path=rel, frames=len(frames),
                                frameWidth=FRAME, frameHeight=FRAME, fps=6, anchor=ANCHOR,
                                designId=r["design_id"], name=r["display_name"], batch=r["batch"])
            continue
        typ, theme = name.rsplit("_", 1)
        reg = f"{pre}.{typ}"
        fn = gear_world.WORLD.get(reg)
        kind = next((v for k, v in KIND.items() if key.startswith(k + ".")), None)
        if not fn or not kind:
            continue
        dirname, layer, slot, size = kind
        if reg in SIZE_OVERRIDE:
            layer, size = SIZE_OVERRIDE[reg]
        im = finish(fn(theme))
        assert im.width == size, (key, im.width, size)
        rel = f"{'backgrounds' if layer == 'room_background' else 'sprites/' + dirname}/{name}.png"
        write_strip([im], rel, size, size)
        entries[key] = dict(type="image", layer=layer, slot=slot, path=rel, frameWidth=size, frameHeight=size,
                            designId=r["design_id"], name=r["display_name"], batch=r["batch"])
    return entries, {}
