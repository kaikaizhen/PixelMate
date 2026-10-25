import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

const KEY = 'pm.weatherMotion'

/** Per-device display preferences (kept in this browser only). */
export const useSettingsStore = defineStore('settings', () => {
  // Moving weather is on by default: it is ambient and tiny, and an operating-system "reduce motion" switch used to freeze it with no way back.
  let initial = true
  try { initial = localStorage.getItem(KEY) !== 'off' } catch { /* private mode: keep the default */ }
  const weatherMotion = ref(initial)
  watch(weatherMotion, (on) => { try { localStorage.setItem(KEY, on ? 'on' : 'off') } catch { /* ignore */ } })
  return { weatherMotion }
})
