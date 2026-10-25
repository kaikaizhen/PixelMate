export interface RoomPlacement {
  placementId: string
  itemId: string
  x: number
  y: number
}

export interface Room {
  revision: number
  floorItemId: string | null
  placements: RoomPlacement[]
}

export const MAX_PLACEMENTS = 40
