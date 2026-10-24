"""ESP32 simulation: compose the 240x240 logical scene exactly as the PWA would, then integer-scale 2x -> 480x480 (nearest)."""
import json
import os

from PIL import Image

from common import ASSETS

m = json.load(open(os.path.join(ASSETS, "manifest.json"), encoding="utf-8"))["assets"]


def L(key, idx=0, w=None):
    a = m[key]
    im = Image.open(os.path.join(ASSETS, a["path"])).convert("RGBA")
    w = a["frameWidth"]
    return im.crop((idx * w, 0, (idx + 1) * w, a["frameHeight"]))


def scene(weather, furniture, pet_gear, face="normal"):
    s = Image.new("RGBA", (240, 240), (0, 0, 0, 255))
    s.alpha_composite(L(weather))                                         # window_weather
    s.alpha_composite(Image.new("RGBA", (240, 70), (166, 124, 92, 255)), (0, 175))   # plain floor strip (placeholder)
    for key, (x, y) in furniture:
        s.alpha_composite(L(key), (x, y))
    px, py = 96, 150                                                      # pet 48x48 placed at integer coords
    stack = Image.new("RGBA", (48, 48), (0, 0, 0, 0))
    backs = [m[k]["backKey"] for k in pet_gear if "backKey" in m[k]]
    for b in backs:
        stack.alpha_composite(L(b))
    stack.alpha_composite(L("pet.star_01.body.cream.idle", 0)); stack.alpha_composite(L(f"pet.star_01.face.{face}"))
    for k in sorted(pet_gear, key=lambda k: m[k]["z"]):
        stack.alpha_composite(L(k))
    s.alpha_composite(stack, (px, py))
    return s


if __name__ == "__main__":
    out = os.path.join(ASSETS, "preview")
    a = scene("world.weather.sunrise_window", [("world.furniture.bed_starlight", (128, 120)),
                                               ("world.decor.painting_garden", (20, 60))],
              ["equip.hat.straw_post", "equip.outfit.vest_post", "equip.accessory.bowtie_festival"])
    b = scene("world.weather.sunrise_yard", [("world.furniture.sofa_forest", (20, 118)),
                                             ("world.decor.clock_garden", (150, 40))],
              ["equip.costume.fox", "equip.feet.sneaker_picnic"], face="happy")
    sheet = Image.new("RGBA", (960 + 8, 480), (255, 248, 236, 255))
    for i, sc in enumerate((a, b)):
        sc.resize((480, 480), Image.NEAREST).save(os.path.join(out, f"esp32_480_sim_{i + 1}.png"))
        sheet.alpha_composite(sc.resize((480, 480), Image.NEAREST), (i * 488, 0))
    sheet.convert("RGB").save(os.path.join(out, "esp32_480_sim.png"))
    print("esp32 sims written (240 logical -> 480 integer 2x)")
