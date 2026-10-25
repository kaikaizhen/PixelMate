export interface PetEvent {
  seq: number
  eventId: string
  type: string
  petId?: string | null
  revision?: number | null
  occurredAt: string
  payload: Record<string, unknown>
}

/** What the screen should re-read. An event is only a hint; the API stays the source of truth. */
export interface Refreshers {
  pet(): void
  inventory(): void
  room(): void
  mail(): void
  friends(): void
  finds(): void
  devices(): void
}

/** Which re-reads an event asks for (several events in a burst share one set, so a find that also changes the bag reads each once). */
export function wants(type: string): (keyof Refreshers)[] {
  switch (type) {
    case 'pet.updated': return ['pet']
    case 'pet.find': return ['pet', 'finds']
    case 'inventory.updated': return ['inventory']
    case 'room.updated': return ['room']
    case 'mail.received': return ['mail']
    case 'friends.updated': return ['friends']
    case 'devices.updated': return ['devices']
    default: return []
  }
}

/** Collects the re-reads asked for within `windowMs` and runs each once. Returns the function to feed events to. */
export function batcher(run: Refreshers, windowMs = 150, schedule: (fn: () => void, ms: number) => unknown = setTimeout) {
  const pending = new Set<keyof Refreshers>()
  let armed = false
  return (e: Pick<PetEvent, 'type'>) => {
    wants(e.type).forEach((w) => pending.add(w))
    if (armed || pending.size === 0) return
    armed = true
    schedule(() => {
      armed = false
      const todo = [...pending]
      pending.clear()
      todo.forEach((w) => run[w]())
    }, windowMs)
  }
}
