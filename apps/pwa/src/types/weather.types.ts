export interface City {
  id: string
  name: string
  latitude: number
  longitude: number
  timeZone: string
  group: 'taiwan' | 'world'
}

export interface UserLocation {
  latitude: number
  longitude: number
  timeZone: string
  label: string
  source: 'gps' | 'city'
  updatedAt: string
}

export interface SaveLocationInput {
  latitude: number
  longitude: number
  timeZone: string
  label: string
  source: 'gps' | 'city'
}

/** How the pet world's weather words read in Chinese. */
export const WEATHER_LABEL: Record<string, string> = {
  clear: '晴', cloudy: '多雲', fog: '起霧', drizzle: '毛毛雨', shower: '陣雨', thunder: '雷雨', typhoon: '颱風', snow: '下雪', cold: '寒冷', petals: '花瓣飄落', sunrise: '日出', sunset: '日落', starry: '星空',
}
export const SOURCE_LABEL: Record<string, string> = { cwa: '中央氣象署', 'open-meteo': 'Open-Meteo', simulated: '模擬天氣' }
