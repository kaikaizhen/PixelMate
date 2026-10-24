import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface GameAsset {
  path: string
  frames?: number
  frameWidth?: number
  frameHeight?: number
  slot?: string
  z?: number
  layer?: string
  backKey?: string
}

/** The game asset manifest (assets/manifest.json, copied to /web/game/). Everything is looked up by assetKey. */
export const useGameStore = defineStore('game', () => {
  const assets = ref<Record<string, GameAsset>>({})
  const ready = ref(false)
  let pending: Promise<void> | null = null

  function load(): Promise<void> {
    pending ??= (async () => {
      try {
        const res = await fetch(`${import.meta.env.BASE_URL}game/manifest.json`)
        if (res.ok) assets.value = (await res.json()).assets ?? {}
      } catch {
        /* offline / not synced: the scene falls back to the flat room */
      } finally {
        ready.value = true
      }
    })()
    return pending
  }

  /** URL for an assetKey, or '' when the key is unknown (never a hard-coded path). */
  function url(key: string): string {
    const a = assets.value[key]
    return a ? `${import.meta.env.BASE_URL}game/${a.path}` : ''
  }

  return { assets, ready, load, url }
})
