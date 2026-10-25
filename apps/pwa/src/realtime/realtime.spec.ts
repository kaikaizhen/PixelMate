import { describe, expect, it, vi } from 'vitest'
import { batcher, wants, type Refreshers } from './routeEvent'
import { createRealtime } from './connection'
import type { HubConnection } from '@microsoft/signalr'

const refreshers = (): Refreshers => ({ pet: vi.fn(), inventory: vi.fn(), room: vi.fn(), mail: vi.fn(), friends: vi.fn(), finds: vi.fn(), devices: vi.fn() })
const ev = (type: string, eventId = type) => ({ seq: 1, eventId, type, occurredAt: '', payload: {} })

describe('wants', () => {
  it('maps every event type to the screens it touches and ignores strangers', () => {
    expect(wants('pet.updated')).toEqual(['pet'])
    expect(wants('pet.find')).toEqual(['pet', 'finds'])
    expect(wants('mail.received')).toEqual(['mail'])
    expect(wants('devices.updated')).toEqual(['devices'])
    expect(wants('something.new')).toEqual([])
  })
})

describe('batcher', () => {
  it('a burst of events re-reads each thing once, after the window', () => {
    const r = refreshers()
    const jobs: (() => void)[] = []
    const feed = batcher(r, 150, (fn) => jobs.push(fn))
    feed(ev('pet.find')); feed(ev('inventory.updated')); feed(ev('pet.updated')); feed(ev('unknown'))
    expect(r.pet).not.toHaveBeenCalled()
    expect(jobs).toHaveLength(1)
    jobs[0]()
    expect(r.pet).toHaveBeenCalledTimes(1)
    expect(r.finds).toHaveBeenCalledTimes(1)
    expect(r.inventory).toHaveBeenCalledTimes(1)
    expect(r.room).not.toHaveBeenCalled()
  })

  it('an unknown event schedules nothing', () => {
    const jobs: (() => void)[] = []
    batcher(refreshers(), 150, (fn) => jobs.push(fn))(ev('unknown'))
    expect(jobs).toHaveLength(0)
  })
})

function fakeHub() {
  const handlers: Record<string, (e: never) => void> = {}
  const cb: Record<string, () => void> = {}
  const hub = {
    on: (name: string, fn: (e: never) => void) => (handlers[name] = fn),
    onreconnecting: (fn: () => void) => (cb.reconnecting = fn),
    onreconnected: (fn: () => void) => (cb.reconnected = fn),
    onclose: (fn: () => void) => (cb.close = fn),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
  }
  return { hub: hub as unknown as HubConnection, handlers, cb, start: hub.start }
}

describe('createRealtime', () => {
  it('passes events on once, however often the same eventId arrives', async () => {
    const f = fakeHub()
    const got: string[] = []
    const rt = createRealtime((e) => got.push(e.eventId), () => {}, () => f.hub)
    await rt.start()
    f.handlers['pet.event'](ev('pet.updated', 'a') as never)
    f.handlers['pet.event'](ev('pet.updated', 'a') as never)
    f.handlers['pet.event'](ev('pet.updated', 'b') as never)
    expect(got).toEqual(['a', 'b'])
    expect(rt.connected.value).toBe(true)
    rt.stop()
  })

  it('re-reads everything when the link comes up and after a reconnect', async () => {
    const f = fakeHub()
    const back = vi.fn()
    const rt = createRealtime(() => {}, back, () => f.hub)
    await rt.start()
    expect(back).toHaveBeenCalledTimes(1)
    f.cb.reconnecting()
    expect(rt.connected.value).toBe(false)
    f.cb.reconnected()
    expect(back).toHaveBeenCalledTimes(2)
    expect(rt.connected.value).toBe(true)
    rt.stop()
  })

  it('a failed start is not an error for the app: it stays disconnected and tries again later', async () => {
    const f = fakeHub()
    f.start.mockRejectedValueOnce(new Error('offline'))
    vi.useFakeTimers()
    const rt = createRealtime(() => {}, () => {}, () => f.hub)
    await rt.start()
    expect(rt.connected.value).toBe(false)
    await vi.advanceTimersByTimeAsync(30_000)
    expect(f.start).toHaveBeenCalledTimes(2)
    expect(rt.connected.value).toBe(true)
    rt.stop()
    vi.useRealTimers()
  })
})
