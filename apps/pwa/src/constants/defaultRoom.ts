/** The starter room: furniture (game assetKeys) placed on the 240x240 logical canvas of the house art. */
export const ROOM_SIZE = 240
export const WINDOW = { x: 140, y: 28, w: 64, h: 72 }          // the opening in room.png
export const WINDOW_VIEW = { x: 88, y: 64 }                      // which part of the 240x240 weather scene shows through it

export const DEFAULT_FURNITURE: { key: string; x: number; y: number; w: number }[] = [
  { key: 'world.furniture.table_garden', x: 150, y: 100, w: 96 },
]

export const PET_SPOT = { x: 96, y: 152 }                        // top-left of the 48x48 pet frame (feet land on the rug, y ~196)
