export type DressSlot = 'hat' | 'neck' | 'feet'

export interface DressItem {
  id: 'straw_hat' | 'beanie' | 'scarf' | 'slippers'
  label: string
  slot: DressSlot
  sprite: string
  iconX: number
}
