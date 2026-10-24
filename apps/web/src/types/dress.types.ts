export type DressSlot = 'hat' | 'neck'

export interface DressItem {
  id: 'straw_hat' | 'beanie' | 'scarf'
  label: string
  slot: DressSlot
  sprite: string
  iconX: number
}
