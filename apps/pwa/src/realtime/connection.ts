import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { ref } from 'vue'
import type { PetEvent } from './routeEvent'

/**
 * The live link to the API (SignalR). It only ever says "something changed"; callers re-read the API.
 * Losing it is harmless: the screen still re-reads on focus, on slot changes and slowly on a timer.
 */
export function createRealtime(onEvent: (e: PetEvent) => void, onBack: () => void, make: () => HubConnection = defaultConnection) {
  const connected = ref(false)
  const seen: string[] = []          // recent eventIds: the same event can arrive twice (reconnect, two servers)
  let hub: HubConnection | null = null
  let retry: number | undefined
  let stopped = false

  function accept(e: PetEvent) {
    if (seen.includes(e.eventId)) return
    seen.push(e.eventId)
    if (seen.length > 200) seen.shift()
    onEvent(e)
  }

  async function open() {
    if (stopped) return
    hub = make()
    hub.on('pet.event', accept)
    hub.onreconnecting(() => (connected.value = false))
    hub.onreconnected(() => { connected.value = true; onBack() })
    hub.onclose(() => { connected.value = false; if (!stopped) retry = window.setTimeout(open, 30_000) })
    try {
      await hub.start()
      connected.value = true
      onBack()                         // anything that happened before the link came up
    } catch {
      connected.value = false
      if (!stopped) retry = window.setTimeout(open, 30_000)
    }
  }

  function stop() {
    stopped = true
    window.clearTimeout(retry)
    connected.value = false
    void hub?.stop()
  }

  return { connected, start: open, stop, accept }
}

function defaultConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl('/api/v1/hub', { headers: { 'X-Requested-With': 'pixelmate' } })     // the API's CSRF marker (the library's own XMLHttpRequest value would be refused)
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.None)
    .build()
}
