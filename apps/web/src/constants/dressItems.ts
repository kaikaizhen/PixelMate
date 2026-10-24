import type { DressItem } from '@/types/dress.types'
import strawHat from '@/assets/pixel/acc_straw_hat.png'
import beanie from '@/assets/pixel/acc_beanie.png'
import scarf from '@/assets/pixel/acc_scarf.png'

export const DRESS_ITEMS: readonly DressItem[] = [
  { id: 'straw_hat', label: '草帽', slot: 'hat', sprite: strawHat, iconX: 0 },
  { id: 'beanie', label: '毛線帽', slot: 'hat', sprite: beanie, iconX: 1 },
  { id: 'scarf', label: '藍圍巾', slot: 'neck', sprite: scarf, iconX: 2 },
]
