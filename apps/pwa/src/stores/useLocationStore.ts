import { defineStore } from 'pinia'
import { ref } from 'vue'
import { weatherApi } from '@/api/weatherApi'
import type { City, UserLocation } from '@/types/weather.types'

const ASKED = 'pm.locationAsked'

/** Great-circle distance in km (to name a GPS fix after the nearest city). */
export function distanceKm(aLat: number, aLon: number, bLat: number, bLon: number): number {
  const rad = (d: number) => (d * Math.PI) / 180
  const h = Math.sin(rad(bLat - aLat) / 2) ** 2 + Math.cos(rad(aLat)) * Math.cos(rad(bLat)) * Math.sin(rad(bLon - aLon) / 2) ** 2
  return 2 * 6371 * Math.asin(Math.sqrt(h))
}

export function nearestCity(cities: City[], lat: number, lon: number): { city: City; km: number } | null {
  let best: { city: City; km: number } | null = null
  for (const c of cities) {
    const km = distanceKm(lat, lon, c.latitude, c.longitude)
    if (!best || km < best.km) best = { city: c, km }
  }
  return best
}

export const useLocationStore = defineStore('location', () => {
  const location = ref<UserLocation | null>(null)
  const cities = ref<City[]>([])
  const loaded = ref(false)

  async function load(): Promise<void> {
    location.value = await weatherApi.location()
    loaded.value = true
  }

  async function loadCities(): Promise<void> {
    if (!cities.value.length) cities.value = await weatherApi.cities()
  }

  async function chooseCity(city: City): Promise<void> {
    location.value = await weatherApi.saveLocation({ latitude: city.latitude, longitude: city.longitude, timeZone: city.timeZone, label: city.name, source: 'city' })
    markAsked()
  }

  /** Ask the browser where we are, then save it (the server keeps only a ~10 km grid cell). Rejects with a readable Error. */
  async function useHere(): Promise<void> {
    if (!('geolocation' in navigator)) throw new Error('這個瀏覽器不支援定位。')
    const pos = await new Promise<GeolocationPosition>((resolve, reject) =>
      navigator.geolocation.getCurrentPosition(resolve, (e) => reject(new Error(e.code === e.PERMISSION_DENIED ? '沒有取得定位權限，改選一個縣市吧。' : '暫時定位不到，改選一個縣市吧。')), { timeout: 10_000, maximumAge: 600_000 }))
    await loadCities().catch(() => {})
    const { latitude, longitude } = pos.coords
    const near = nearestCity(cities.value, latitude, longitude)
    location.value = await weatherApi.saveLocation({
      latitude, longitude, timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'Asia/Taipei',
      label: near && near.km < 80 ? `${near.city.name}附近` : '目前位置', source: 'gps',
    })
    markAsked()
  }

  const asked = () => { try { return localStorage.getItem(ASKED) === '1' } catch { return false } }
  function markAsked() { try { localStorage.setItem(ASKED, '1') } catch { /* private mode */ } }

  function clear() {
    location.value = null
    loaded.value = false
  }

  return { location, cities, loaded, load, loadCities, chooseCity, useHere, asked, markAsked, clear }
})
