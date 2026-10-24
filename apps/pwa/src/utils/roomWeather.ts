/** Which window scene (assetKey) the room shows for a given local time, until the server drives the weather. */
export function windowWeatherKey(date = new Date(), serverWeather = 'sunny'): string {
  if (serverWeather === 'rain' || serverWeather === 'rainy') return 'world.weather.shower_window'
  const h = date.getHours()
  if (h >= 5 && h < 9) return 'world.weather.sunrise_window'
  if (h >= 9 && h < 17) return 'world.weather.cloudy_window'
  if (h >= 17 && h < 20) return 'world.weather.sunset_window'
  return 'world.weather.starry_window'
}
