"""Build every finished stage and write assets/manifest.json. Usage: python3 build_all.py [stage ...]"""
import importlib
import sys

from common import CANVAS, save_json

STAGES = ["stage1_pet", "stage2_gear", "stage3_world", "stage4_costume"]


def main():
    assets, anims = {}, {}
    for name in STAGES:
        a, n = importlib.import_module(name).build()
        for k in list(a) + list(n):
            if k in assets or k in anims:
                raise SystemExit(f"duplicate assetKey: {k}")
        assets.update(a); anims.update(n)
    save_json({"version": "1.0.0", "logicalCanvas": {"width": CANVAS, "height": CANVAS},
               "assets": dict(sorted(assets.items())), "animations": dict(sorted(anims.items()))}, "manifest.json")
    print(f"manifest: {len(assets)} assets, {len(anims)} animations")


if __name__ == "__main__":
    main()
