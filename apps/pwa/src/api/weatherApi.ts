import type { City, SaveLocationInput, UserLocation } from '@/types/weather.types'
import { request } from './http'

export const weatherApi = {
  cities: async () => (await request<{ cities: City[] }>('GET', '/weather/cities')).cities,
  location: async () => (await request<{ location: UserLocation | null }>('GET', '/me/location')).location,
  saveLocation: (input: SaveLocationInput) => request<UserLocation>('PUT', '/me/location', input),
}
