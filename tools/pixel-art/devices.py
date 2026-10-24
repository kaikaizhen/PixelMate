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

# ---- desktop screen frame 88x96, hole (12,10)-(75,73), little stand ----
d = Sprite(88, 96)
d.rect(4, 2, 83, 81, "#2f2638"); d.rect(4, 2, 83, 81, LINE, fill=False)
d.rect(6, 4, 81, 79, "#3a3044")
d.rect(12, 10, 75, 73, None)
d.rect(40, 82, 47, 88, "#5b4a66"); d.rect(40, 82, 47, 88, LINE, fill=False)
d.rect(26, 88, 61, 93, "#7b6a86"); d.rect(26, 88, 61, 93, LINE, fill=False)
d.px(79, 77, "#8fc4e8")                                          # power LED
save(d, "desk_frame.png")
print("devices ok")
