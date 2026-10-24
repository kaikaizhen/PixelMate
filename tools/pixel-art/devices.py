"""Phone (PWA) and desktop screen (ESP32) illustrations. Screens are filled by CSS layers. Original art."""
import os
import sys
sys.path.insert(0, "/Volumes/Backup_ACASIS M001 Media/Dev/skills/pixel-art-studio/scripts")
from pixelstudio import Sprite
from PIL import Image

OUT = sys.argv[1]
LINE = "#4a3040"


def save(sp, name):
    sp.save_png(os.path.join(OUT, name))


room = Image.open(OUT + "/room.png").convert("RGBA")

# ---- screen backgrounds (static crops of the room, pet is overlaid live) ----
room.crop((60, 78, 116, 182)).save(OUT + "/phone_bg.png")      # 56x104
room.crop((72, 138, 136, 202)).save(OUT + "/desk_bg.png")      # 64x64

# ---- phone frame 72x128, screen hole (8,12)-(63,115) ----
p = Sprite(72, 128)
p.rect(0, 0, 71, 127, "#5b4a66"); p.rect(2, 2, 69, 125, "#2f2638")
p.rect(0, 0, 71, 127, LINE, fill=False)
for (x, y) in [(0, 0), (71, 0), (0, 127), (71, 127)]:
    p.px(x, y, None)
p.rect(8, 12, 63, 115, None)
p.rect(30, 5, 41, 7, "#1a1520")                                  # speaker
p.rect(30, 119, 41, 120, "#7b6a86")                              # home bar
p.line(3, 20, 3, 60, "#7b6a86")                                  # sheen
save(p, "phone_frame.png")

# ---- desktop device: small white cube-like screen with thick bezel (ESP32 touch display) ----
d = Sprite(88, 96)
WHITE = "#f6f3ee"; SHADE = "#d9d3ca"; DEEP = "#bdb5aa"
d.ellipse(10, 86, 78, 94, "#e6d3b6")                                # soft shadow on the desk
d.rect(2, 3, 85, 85, WHITE)                                          # body
d.rect(2, 79, 85, 85, SHADE)                                         # lower edge shading
d.rect(80, 3, 85, 85, SHADE)                                         # right edge shading
d.rect(2, 3, 5, 78, "#ffffff")                                       # left highlight
for (x, y) in [(2, 3), (85, 3), (2, 85), (85, 85)]:                  # rounded corners
    d.px(x, y, None)
d.rect(2, 3, 85, 85, LINE, fill=False)
for (x, y) in [(2, 3), (85, 3), (2, 85), (85, 85)]:
    d.px(x, y, None)
d.rect(12, 11, 75, 74, None)                                         # screen hole (64x64)
d.rect(11, 10, 76, 75, "#2f2638", fill=False)                        # screen rim
d.rect(11, 10, 76, 75, LINE, fill=False)
for k in range(5):                                                   # speaker holes
    d.px(52 + k * 4, 80, DEEP); d.px(52 + k * 4, 81, DEEP)
d.rect(86, 30, 87, 40, SHADE); d.rect(86, 30, 87, 40, LINE, fill=False)  # side button
d.px(14, 80, "#8fc4e8"); d.px(15, 80, "#8fc4e8")                     # status LED
save(d, "desk_frame.png")
print("devices ok")
