"""Seamless tiles cut from the house art (apps/web room.png) so the PWA can extend the room to a full screen:
wall stripes (16x16), baseboard (16x7) and floor planks (40x24). The art is periodic, so the tiles repeat without seams."""
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "apps", "web", "src", "assets", "pixel", "room.png")
OUT = os.path.join(ROOT, "apps", "pwa", "src", "assets", "pixel")
im = Image.open(SRC).convert("RGBA")
im.crop((0, 2, 16, 18)).save(os.path.join(OUT, "room_wall_tile.png"))          # vertical stripes, 8 light + 8 dark
im.crop((0, 144, 16, 151)).save(os.path.join(OUT, "room_base_tile.png"))       # baseboard rows 144..150
im.crop((0, 159, 40, 183)).save(os.path.join(OUT, "room_floor_tile.png"))      # two plank rows, joints offset by 20
print("tiles written to", OUT)
