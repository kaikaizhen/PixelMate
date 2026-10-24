"""Validate assets/manifest.json against the rules in docs/素材.md. Exit code 1 on any failure."""
import json
import os
import re
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
ASSETS = os.path.join(ROOT, "assets")
KEY_RE = re.compile(r"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$")
LAYERS = {"room_background", "window_weather", "furniture_back", "pet_back_accessory", "pet_body", "pet_face",
          "pet_outfit", "pet_front_accessory", "furniture_front", "foreground_effects", "ui_icon", "audio"}

errors = []


def err(msg):
    errors.append(msg)


def main():
    mpath = os.path.join(ASSETS, "manifest.json")
    if not os.path.exists(mpath):
        print("manifest.json missing"); sys.exit(1)
    m = json.load(open(mpath, encoding="utf-8"))
    if m.get("logicalCanvas") != {"width": 240, "height": 240}:
        err("logicalCanvas must be 240x240")
    assets, anims = m.get("assets", {}), m.get("animations", {})
    used_paths = set()
    for key, a in assets.items():
        if not KEY_RE.match(key):
            err(f"bad assetKey: {key}")
        p = a.get("path", "")
        if p.startswith("/") or ".." in p or "\\" in p:
            err(f"{key}: path must be relative and clean ({p})")
        full = os.path.join(ASSETS, p)
        if not os.path.exists(full):
            err(f"{key}: file missing {p}"); continue
        used_paths.add(p)
        if a.get("layer") not in LAYERS:
            err(f"{key}: unknown layer {a.get('layer')}")
        if a["type"] in ("image", "spritesheet"):
            im = Image.open(full).convert("RGBA")
            fw, fh = a.get("frameWidth", im.width), a.get("frameHeight", im.height)
            frames = a.get("frames", 1)
            if (im.width, im.height) != (fw * frames, fh):
                err(f"{key}: size {im.size} != {fw}*{frames}x{fh}")
            semi = sum(1 for (_, _, _, al) in im.getdata() if 0 < al < 255)
            if semi:
                err(f"{key}: {semi} semi-transparent pixels")
            if not any(al for (_, _, _, al) in im.getdata()):
                err(f"{key}: fully transparent image")
    for key, an in anims.items():
        if not KEY_RE.match(key):
            err(f"bad animation key: {key}")
        for i, f in enumerate(an.get("frames", [])):
            b = f.get("body")
            if b:
                sheet = assets.get(f"{key.rsplit('.anim.', 1)[0]}.body.cream.{b['sheet']}")
                if not sheet or b["index"] >= sheet.get("frames", 1):
                    err(f"{key}[{i}]: body {b} not found / out of range")
            face = f.get("face")
            if face and f"{key.rsplit('.anim.', 1)[0]}.face.{face}" not in assets:
                err(f"{key}[{i}]: face '{face}' not found")
            for pr in f.get("props", []):
                if pr["key"] not in assets:
                    err(f"{key}[{i}]: prop {pr['key']} not found")
    # orphan files
    for dp, _, fs in os.walk(ASSETS):
        for fn in fs:
            rel = os.path.relpath(os.path.join(dp, fn), ASSETS).replace(os.sep, "/")
            if rel in ("manifest.json", "LICENSES.md", "README.md") or rel.startswith(("preview/", "readme/")) or fn.startswith("."):
                continue
            if rel not in used_paths:
                err(f"orphan file not in manifest: {rel}")
    print(f"assets: {len(assets)}  animations: {len(anims)}")
    if errors:
        print("FAILED:"); [print(" -", e) for e in errors]; sys.exit(1)
    print("OK: manifest valid")


if __name__ == "__main__":
    main()
