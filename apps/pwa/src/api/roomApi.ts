import type { Room, RoomPlacement } from '@/types/room.types'
import { request } from './http'

export const roomApi = {
  get: () => request<Room>('GET', '/room'),
  /** Replaces the whole layout; `expectedRevision` makes a save from a stale screen fail with 409 instead of overwriting. */
  save: (expectedRevision: number, floorItemId: string | null, placements: Pick<RoomPlacement, 'itemId' | 'x' | 'y'>[]) =>
    request<Room>('PUT', '/room', { expectedRevision, floorItemId, placements: placements.map(({ itemId, x, y }) => ({ itemId, x, y })) }),
}
