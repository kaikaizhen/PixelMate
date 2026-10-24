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

/** assetKey of the outdoor picture for a weather (the server's word wins over the clock) and a view. */
export function weatherKey(date = new Date(), serverWeather?: string, view: OutdoorView = 'window'): string {
  const name = serverWeather === 'rain' || serverWeather === 'rainy' ? 'shower' : (WEATHERS as readonly string[]).includes(serverWeather ?? '') ? serverWeather! : byClock(date)
  return `world.weather.${name}_${view}`
}

/** Kept for the room's window. */
export const windowWeatherKey = (date = new Date(), serverWeather?: string) => weatherKey(date, serverWeather, 'window')
