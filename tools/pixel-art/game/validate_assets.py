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


SLOTS = {"head", "body", "front", "feet", "hand", "costume", "costume_back"}
warnings = []


def _mask(im):
    return {(x, y) for y in range(im.height) for x in range(im.width) if im.getpixel((x, y))[3]}


def _structure(im):
    """Colour-agnostic structure signature: pixels keep their shape/position, colours become first-seen indices."""
    seen, sig = {}, []
    for y in range(im.height):
        for x in range(im.width):
            p = im.getpixel((x, y))
            if p[3]:
                sig.append((x, y, seen.setdefault(p[:3], len(seen))))
    return tuple(sig)


def gear_checks(assets):
    """Wearables: anchor/rig, touches the pet body (no floating gear), no duplicates, no recolor-only twins."""
    body = Image.open(os.path.join(ASSETS, assets["pet.star_01.body.cream.idle"]["path"])).convert("RGBA").crop((0, 0, 48, 48))
    bmask = _mask(body)
    halo = {(x + dx, y + dy) for x, y in bmask for dx in (-2, -1, 0, 1, 2) for dy in (-2, -1, 0, 1, 2)}
    seen_hash, seen_struct, by_slot, struct_all = {}, {}, {}, {}
    for key, a in assets.items():
        if "batch" not in a:
            continue
        wear = a.get("slot") in SLOTS
        im = Image.open(os.path.join(ASSETS, a["path"])).convert("RGBA")
        mask = _mask(im)
        if wear:
            if a.get("anchor") != [24, 46] or a.get("rigs") != ["star_01"] or (im.width, im.height) != (48, 48):
                err(f"{key}: wearable needs 48x48, anchor [24,46], rigs ['star_01']")
            if not (mask & halo):
                err(f"{key}: gear does not touch the pet body (floating)")
            if a["slot"] == "head" and min(y for _, y in mask) > 20:
                err(f"{key}: hat is not on the head")
        if not (a.get("designId") and a.get("name")):
            err(f"{key}: missing designId/name metadata")
        h = hash(im.tobytes())
        if h in seen_hash:
            err(f"{key}: identical pixels to {seen_hash[h]}")
        seen_hash[h] = key
        st = _structure(im)
        if st in seen_struct:
            err(f"{key}: recolor-only twin of {seen_struct[st]}")
        seen_struct[st] = key
        struct_all[key] = st
        by_slot.setdefault(a["slot"], []).append((key, mask))
    structs = {k: dict(((x, y), c) for x, y, c in st_) for k, st_ in struct_all.items()}
    for slot, items in by_slot.items():          # near-recolor candidates = human review warning (not failure)
        for i in range(len(items)):
            for j in range(i + 1, len(items)):
                ka, kb = items[i][0], items[j][0]
                if ka.rsplit("_", 1)[0] != kb.rsplit("_", 1)[0]:
                    continue
                A, B = structs[ka], structs[kb]
                union = set(A) | set(B)
                same = sum(1 for p in union if A.get(p) == B.get(p))
                if union and same / len(union) > 0.95:
                    warnings.append(f"{ka} ~ {kb}: {same / len(union):.0%} identical structure (recolor risk)")


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
    gear_checks(assets)
    # orphan files
    for dp, _, fs in os.walk(ASSETS):
        for fn in fs:
            rel = os.path.relpath(os.path.join(dp, fn), ASSETS).replace(os.sep, "/")
            if rel in ("manifest.json", "LICENSES.md", "README.md") or rel.startswith(("preview/", "readme/", "catalog/", "gallery/")) or fn.startswith("."):
                continue
            if rel not in used_paths:
                err(f"orphan file not in manifest: {rel}")
    print(f"assets: {len(assets)}  animations: {len(anims)}")
    for w in warnings:
        print("WARN:", w)
    with open(os.path.join(ASSETS, "preview", "similarity_report.txt"), "w", encoding="utf-8") as f:
        f.write("# 近似素材候選（待人工目視；不是失敗）\n" + "\n".join(warnings) + "\n")
    if errors:
        print("FAILED:"); [print(" -", e) for e in errors]; sys.exit(1)
    print("OK: manifest valid")


if __name__ == "__main__":
    main()
