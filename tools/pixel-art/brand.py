"""Favicon, touch icon and OG image from the placeholder pet/room art."""
import sys
from PIL import Image, ImageDraw, ImageFont

SRC, PUB = sys.argv[1], sys.argv[2]
idle = Image.open(SRC + "/pet_idle.png").convert("RGBA").crop((0, 0, 48, 48))
head = idle.copy()  # 48x48
fav = head
fav.resize((32, 32), Image.NEAREST).save(PUB + "/favicon-32.png")
touch = Image.new("RGBA", (180, 180), "#fff3dc")
touch.alpha_composite(head.resize((144, 144), Image.NEAREST), (18, 18))
touch.save(PUB + "/apple-touch-icon.png")

og = Image.new("RGBA", (1200, 630), "#fff8ec")
room = Image.open(SRC + "/room.png").convert("RGBA")
win = Image.open(SRC + "/window_sunny.png").convert("RGBA")
scene = Image.new("RGBA", (240, 240)); scene.alpha_composite(win, (140, 28)); scene.alpha_composite(room)
scene.alpha_composite(idle, (96, 150))
big = scene.resize((480, 480), Image.NEAREST)
d = ImageDraw.Draw(og)
d.rectangle((650 + 12, 70 + 12, 650 + 12 + 480, 70 + 12 + 480), fill="#4a3040")
og.alpha_composite(big, (650, 70))
d.rectangle((650, 70, 650 + 480, 70 + 480), outline="#4a3040", width=8)
font_path = "/System/Library/Fonts/STHeiti Medium.ttc"
f_big = ImageFont.truetype(font_path, 64, index=0) if True else None
f_mid = ImageFont.truetype(font_path, 32, index=0)
f_br = ImageFont.truetype(font_path, 40, index=0)
d.text((70, 120), "小伴星PixelMate", font=f_br, fill="#4a3040")
for i, line in enumerate(["有些想念，", "會自己走到", "你身邊。"]):
    d.text((70 + 4, 200 + i * 86 + 4), line, font=f_big, fill="#f4a6a0")
    d.text((70, 200 + i * 86), line, font=f_big, fill="#4a3040")
d.rectangle((70, 490, 330, 548), fill="#ffd86b", outline="#4a3040", width=4)
d.text((92, 498), "即將與你見面", font=f_mid, fill="#4a3040")
og.convert("RGB").save(PUB + "/og-image.png")
