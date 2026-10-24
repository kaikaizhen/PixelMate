"""README GIFs built from the existing pixel strips (nearest-neighbour, integer scale)."""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

SRC, OUT = sys.argv[1], sys.argv[2]
BG = "#fff8ec"
FONT = "/System/Library/Fonts/STHeiti Medium.ttc"


def strip(name, w, h=None):
    im = Image.open(os.path.join(SRC, name)).convert("RGBA")
    h = h or im.height
    return [im.crop((i * w, 0, (i + 1) * w, h)) for i in range(im.width // w)]


def scaled(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


def save_gif(frames, path, ms):
    pal = [f.convert("RGB").quantize(colors=64, method=Image.MEDIANCUT, dither=Image.NONE) for f in frames]
    pal[0].save(path, save_all=True, append_images=pal[1:], duration=ms, loop=0, optimize=True, disposal=2)
    print(path, os.path.getsize(path) // 1024, "KB", len(frames), "frames")


# ---------- 1. animated room (all strips are 4 frames, loop together) ----------
room = Image.open(SRC + "/room.png").convert("RGBA")
sky = Image.open(SRC + "/window_sky_sunny.png").convert("RGBA")
sun = strip("sun.png", 24)
cloud = Image.open(SRC + "/cloud_white.png").convert("RGBA")
grass = strip("grass_day.png", 64)
bars = Image.open(SRC + "/window_bars.png").convert("RGBA")
plant = strip("plant.png", 44)
cl, cr = strip("curtain_l.png", 14), strip("curtain_r.png", 14)
pet = strip("pet_idle.png", 48)

frames = []
for f in range(8):
    k = f % 4
    win = sky.copy()
    win.alpha_composite(sun[f % 2], (36, 2))
    win.alpha_composite(cloud, (4 + f * 3, 14))
    win.alpha_composite(grass[k], (0, 56))
    win.alpha_composite(bars)
    scene = Image.new("RGBA", (240, 240), BG)
    scene.alpha_composite(win, (140, 28))
    scene.alpha_composite(room)
    scene.alpha_composite(plant[k], (15, 64))
    scene.alpha_composite(cl[k], (122, 24)); scene.alpha_composite(cr[k], (208, 24))
    scene.alpha_composite(pet[k], (96, 166))
    frames.append(scaled(scene, 2))
save_gif(frames, OUT + "/pixelmate-room.gif", 200)

# ---------- 2. character showcase with captions ----------
font = ImageFont.truetype(FONT, 22, index=0)
small = ImageFont.truetype(FONT, 14, index=0)
S = 5
W, H = 300, 330


def card(sprite, caption, extra=None):
    c = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(c)
    d.rectangle((6, 6, W - 7, H - 7), outline="#4a3040", width=4)
    s = sprite.copy()
    if extra:
        for e in extra:
            s.alpha_composite(e)
    c.alpha_composite(scaled(s, S), ((W - 48 * S) // 2, 24))
    tw = d.textlength(caption, font=font)
    d.text(((W - tw) / 2, 282), caption, font=font, fill="#4a3040")
    return c

segments = []   # (label, [frames], ms, repeats)
def add(label, strip_frames, ms, loops, overlays=None):
    segments.append((label, strip_frames, ms, loops, overlays))

idle = strip("pet_idle.png", 48)
add("星星小精靈", idle, 220, 3)
add("午睡", strip("pet_sleep.png", 48), 500, 2)
add("看書", strip("pet_read.png", 48), 400, 2)
add("看雨", strip("pet_watch.png", 48), 300, 2)
add("喝珍珠奶茶", strip("pet_boba.png", 48), 350, 2)
add("吃雞排", strip("pet_chicken.png", 48), 350, 2)
add("撿到禮物", strip("pet_gift.png", 48), 300, 4)
hat, bean, scarf, slip = (strip("acc_%s.png" % n, 48) for n in ("straw_hat", "beanie", "scarf", "slippers"))
add("草帽", idle, 220, 3, [hat])
add("毛線帽＋圍巾", idle, 220, 3, [bean, scarf])
add("草帽＋圍巾＋藍白拖", idle, 220, 3, [hat, scarf, slip])

frames, durs = [], []
for label, fr, ms, loops, ov in segments:
    for _ in range(loops):
        for i, base in enumerate(fr):
            extra = [o[i % len(o)] for o in ov] if ov else None
            frames.append(card(base, label, extra)); durs.append(ms)
pal = [f.convert("RGB").quantize(colors=48, method=Image.MEDIANCUT, dither=Image.NONE) for f in frames]
path = OUT + "/pixelmate-character.gif"
pal[0].save(path, save_all=True, append_images=pal[1:], duration=durs, loop=0, optimize=True, disposal=2)
print(path, os.path.getsize(path) // 1024, "KB", len(frames), "frames")
