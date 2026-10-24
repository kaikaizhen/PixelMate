import { describe, expect, it } from 'vitest'
import { windowWeatherKey } from './roomWeather'

const at = (h: number) => new Date(2026, 9, 25, h, 0, 0)

describe('windowWeatherKey', () => {
  it('follows the time of day with assetKeys that exist in the manifest', () => {
    expect(windowWeatherKey(at(6))).toBe('world.weather.sunrise_window')
    expect(windowWeatherKey(at(12))).toBe('world.weather.cloudy_window')
    expect(windowWeatherKey(at(18))).toBe('world.weather.sunset_window')
    expect(windowWeatherKey(at(23))).toBe('world.weather.starry_window')
    expect(windowWeatherKey(at(3))).toBe('world.weather.starry_window')
  })

  it('a rainy server state overrides the clock', () => {
    expect(windowWeatherKey(at(12), 'rainy')).toBe('world.weather.shower_window')
  })
})
