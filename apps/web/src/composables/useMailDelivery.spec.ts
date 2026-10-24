import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DELIVER_MS, useMailDelivery } from './useMailDelivery'

describe('useMailDelivery', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('點信箱後送信，抵達後可開信、關信', () => {
    const mail = useMailDelivery()
    expect(mail.phase.value).toBe('idle')
    mail.startDelivery()
    expect(mail.phase.value).toBe('delivering')
    vi.advanceTimersByTime(DELIVER_MS)
    expect(mail.phase.value).toBe('arrived')
    mail.openLetter()
    expect(mail.phase.value).toBe('reading')
    mail.closeLetter()
    expect(mail.phase.value).toBe('done')
  })

  it('送信途中重複點擊不會重複觸發', () => {
    const mail = useMailDelivery()
    mail.startDelivery()
    vi.advanceTimersByTime(1000)
    mail.startDelivery()
    vi.advanceTimersByTime(DELIVER_MS - 1000)
    expect(mail.phase.value).toBe('arrived')
  })

  it('尚未抵達時不能開信', () => {
    const mail = useMailDelivery()
    mail.openLetter()
    expect(mail.phase.value).toBe('idle')
  })

  it('reset 回到初始狀態', () => {
    const mail = useMailDelivery()
    mail.startDelivery()
    mail.reset()
    expect(mail.phase.value).toBe('idle')
    vi.advanceTimersByTime(DELIVER_MS)
    expect(mail.phase.value).toBe('idle')
  })
})
