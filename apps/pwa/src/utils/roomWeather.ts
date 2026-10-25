export type OutdoorView = 'window' | 'balcony' | 'yard'

/** The weather names the server sends (pet.world.weather); each has a window, balcony and yard picture. */
export const WEATHERS = ['sunrise', 'sunset', 'starry', 'cloudy', 'fog', 'drizzle', 'shower', 'thunder', 'typhoon', 'petals', 'cold', 'snow'] as const

/** Local-time look, used only while the server has not said anything (offline, old API). */
function byClock(date: Date): string {
  const h = date.getHours()
  if (h >= 5 && h < 9) return 'sunrise'
  if (h >= 9 && h < 17) return 'cloudy'
  if (h >= 17 && h < 20) return 'sunset'
  return 'starry'
}

/** Night by the player's own clock (20:00 - 04:59). */
export const isNightHour = (date: Date): boolean => date.getHours() >= 20 || date.getHours() < 5

/**
 * The weather's name as the art knows it. The server says what the weather IS (clear, cloudy, shower, snow...); the time of day comes
 * from this device's own clock: clear weather shows sunrise / day / sunset / starry, and a dry night shows the stars.
 */
export function weatherName(date = new Date(), serverWeather?: string): string {
  const w = serverWeather === 'rain' || serverWeather === 'rainy' ? 'shower' : serverWeather
  if (!w || !(WEATHERS as readonly string[]).includes(w)) return byClock(date)       // "clear", an old API word, or nothing yet
  if (isNightHour(date) && (w === 'cloudy' || w === 'cold' || w === 'petals')) return 'starry'
  return w
}

/** assetKey of the outdoor picture for a weather (the server's word wins over the clock) and a view. */
export function weatherKey(date = new Date(), serverWeather?: string, view: OutdoorView = 'window'): string {
  return `world.weather.${weatherName(date, serverWeather)}_${view}`
}

/** Kept for the room's window. */
export const windowWeatherKey = (date = new Date(), serverWeather?: string) => weatherKey(date, serverWeather, 'window')
