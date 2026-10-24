import { describe, expect, it } from 'vitest'
import { weatherKey, windowWeatherKey } from './roomWeather'

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
})
