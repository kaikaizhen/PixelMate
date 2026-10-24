import { describe, expect, it } from 'vitest'
import { DRESS_ITEMS } from '@/constants/dressItems'
import { useOutfit } from './useOutfit'

const [straw, beanie, scarf] = DRESS_ITEMS as [(typeof DRESS_ITEMS)[0], (typeof DRESS_ITEMS)[0], (typeof DRESS_ITEMS)[0]]

describe('useOutfit', () => {
  it('同一欄位換帽子會取代原本的', () => {
    const outfit = useOutfit(DRESS_ITEMS)
    outfit.toggle(straw)
    outfit.toggle(beanie)
    expect(outfit.worn.value.hat).toBe('beanie')
    expect(outfit.isWearing(straw)).toBe(false)
  })

  it('再點一次同一件會脫下', () => {
    const outfit = useOutfit(DRESS_ITEMS)
    outfit.toggle(scarf)
    outfit.toggle(scarf)
    expect(outfit.worn.value.neck).toBeNull()
  })

  it('帽子與圍巾可同時穿', () => {
    const outfit = useOutfit(DRESS_ITEMS)
    outfit.toggle(straw)
    outfit.toggle(scarf)
    expect(outfit.wornItems.value.map((i) => i.id)).toEqual(['straw_hat', 'scarf'])
  })

  it('clear 全部脫下', () => {
    const outfit = useOutfit(DRESS_ITEMS)
    outfit.toggle(straw)
    outfit.clear()
    expect(outfit.wornItems.value).toHaveLength(0)
  })
})
