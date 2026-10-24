import { describe, expect, it } from 'vitest'
import { pickRandom } from './pickRandom'

describe('pickRandom', () => {
  it('回傳清單中的元素', () => {
    expect(['a', 'b', 'c']).toContain(pickRandom(['a', 'b', 'c']))
  })

  it('有排除值且選項大於一個時不會回傳排除值', () => {
    for (let i = 0; i < 50; i++) {
      expect(pickRandom(['a', 'b'], 'a')).toBe('b')
    }
  })

  it('只有一個選項時仍可回傳', () => {
    expect(pickRandom(['a'], 'a')).toBe('a')
  })
})
