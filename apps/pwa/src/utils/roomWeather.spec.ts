import { describe, expect, it } from 'vitest'
import { isNightHour, weatherKey, weatherName, windowWeatherKey } from './roomWeather'

const at = (h: number) => new Date(2026, 9, 25, h, 0, 0)

describe('weatherKey', () => {
  it('without a server weather it follows the clock, with assetKeys that exist in the manifest', () => {
    expect(windowWeatherKey(at(6))).toBe('world.weather.sunrise_window')
    expect(windowWeatherKey(at(12))).toBe('world.weather.cloudy_window')
    expect(windowWeatherKey(at(18))).toBe('world.weather.sunset_window')
    expect(windowWeatherKey(at(23))).toBe('world.weather.starry_window')
    expect(windowWeatherKey(at(3))).toBe('world.weather.starry_window')
  })

  it('the weather the server sends wins over the clock, for any view', () => {
    expect(windowWeatherKey(at(12), 'snow')).toBe('world.weather.snow_window')
    expect(weatherKey(at(3), 'thunder', 'yard')).toBe('world.weather.thunder_yard')
    expect(weatherKey(at(12), 'rainy', 'balcony')).toBe('world.weather.shower_balcony')
  })

  it('an unknown word falls back to the clock instead of a missing picture', () => {
    expect(windowWeatherKey(at(12), 'sunny')).toBe('world.weather.cloudy_window')
  })

  it('"clear" from the server means: look at this device\'s clock for the sky', () => {
    expect(weatherName(at(6), 'clear')).toBe('sunrise')
    expect(weatherName(at(13), 'clear')).toBe('cloudy')
    expect(weatherName(at(18), 'clear')).toBe('sunset')
    expect(weatherName(at(22), 'clear')).toBe('starry')
  })

  it('a dry night shows the stars whatever the weather word; rain and snow stay rain and snow', () => {
    for (const w of ['cloudy', 'cold', 'petals']) expect(weatherName(at(23), w)).toBe('starry')
    for (const w of ['cloudy', 'cold', 'petals']) expect(weatherName(at(13), w)).toBe(w)
    for (const w of ['shower', 'drizzle', 'thunder', 'typhoon', 'snow', 'fog']) expect(weatherName(at(23), w)).toBe(w)
  })

  it('night is 20:00 to 04:59 on the player\'s clock', () => {
    expect([4, 5, 19, 20, 0].map((h) => isNightHour(at(h)))).toEqual([true, false, false, true, true])
  })
})
