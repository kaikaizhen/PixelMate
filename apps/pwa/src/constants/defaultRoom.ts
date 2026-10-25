/** The starter room: furniture (game assetKeys) placed on the 240x240 logical canvas of the house art. */
export const ROOM_SIZE = 240
export const WINDOW = { x: 140, y: 28, w: 64, h: 72 }          // the opening in room.png
// Which part of the 240x240 weather picture shows through the window: its 128x144 region at (100, 30) drawn at half size, so the
// moon / sun, the sky and the hills all fit in the 64x72 opening (a 1:1 crop only caught empty sky).
export const WINDOW_VIEW = { x: 50, y: 15, scale: 0.5 }
/** Where the pet stands outdoors (top-left of its 48x48 frame; the feet land on the deck / the grass). */
export const OUTDOOR_SPOT = { balcony: { x: 70, y: 168 }, yard: { x: 150, y: 170 } } as const

export const DEFAULT_FURNITURE: { key: string; x: number; y: number; w: number }[] = [
  { key: 'world.furniture.table_garden', x: 150, y: 100, w: 96 },
]

export const PET_SPOT = { x: 96, y: 152 }                        // top-left of the 48x48 pet frame (feet land on the rug, y ~196)
