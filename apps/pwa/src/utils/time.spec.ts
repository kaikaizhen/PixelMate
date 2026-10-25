import { describe, expect, it } from 'vitest'
import { ago, utc } from './time'

describe('utc', () => {
  it('reads a server time without a Z as UTC', () => {
    expect(utc('2026-10-25T03:00:00')).toBe(Date.parse('2026-10-25T03:00:00Z'))
    expect(utc('2026-10-25T03:00:00Z')).toBe(Date.parse('2026-10-25T03:00:00Z'))
    expect(utc('2026-10-25T11:00:00+08:00')).toBe(Date.parse('2026-10-25T03:00:00Z'))
  })
})

describe('ago', () => {
  const now = Date.parse('2026-10-25T03:00:00Z')
  it('speaks in minutes and hours, and says so when there was never a connection', () => {
    expect(ago(null, now)).toBe('還沒連線過')
    expect(ago('2026-10-25T02:59:40', now)).toBe('剛剛')
    expect(ago('2026-10-25T02:55:00', now)).toBe('5 分鐘前')
    expect(ago('2026-10-25T00:00:00', now)).toBe('3 小時前')
    expect(ago('2026-10-20T00:00:00', now)).toMatch(/^10\/20 /)
  })
  it('a clock that is a little ahead never shows a negative time', () => {
    expect(ago('2026-10-25T03:00:30', now)).toBe('剛剛')
  })
})
