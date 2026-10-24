import type { WeatherScene } from '@/types/weather.types'

export interface DailyActivity {
  id: 'nap' | 'read' | 'rain' | 'gift' | 'boba' | 'chicken' | 'beach'
  label: string
  caption: string
  weather: WeatherScene
  sprite: string
  frames: number
  duration: string
  petX: number
  petY: number
  extra?: 'zzz' | 'sparkle' | 'steam'
  scene?: 'room' | 'beach'
}
