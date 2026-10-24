import type { DailyActivity } from '@/types/daily.types'
import petSleep from '@/assets/pixel/pet_sleep.png'
import petRead from '@/assets/pixel/pet_read.png'
import petWatch from '@/assets/pixel/pet_watch.png'
import petGift from '@/assets/pixel/pet_gift.png'

export const DAILY_ACTIVITIES: readonly DailyActivity[] = [
  {
    id: 'nap',
    label: '午睡',
    caption: '陽光曬到地毯上，牠縮成一團，睡得很安穩。',
    weather: 'sunny',
    sprite: petSleep,
    frames: 2,
    duration: '2.6s',
    petX: 96,
    petY: 168,
    extra: 'zzz',
  },
  {
    id: 'read',
    label: '看書',
    caption: '翻開一本小小的書，看得入神，連星星都不搖了。',
    weather: 'sunny',
    sprite: petRead,
    frames: 4,
    duration: '3.2s',
    petX: 96,
    petY: 166,
  },
  {
    id: 'rain',
    label: '看雨',
    caption: '下雨了，牠坐在窗邊，看著雨滴一顆一顆落下。',
    weather: 'rain',
    sprite: petWatch,
    frames: 4,
    duration: '1.8s',
    petX: 118,
    petY: 154,
  },
  {
    id: 'gift',
    label: '撿到禮物',
    caption: '散步回來，牠撿到一頂草帽，開心得想讓你看看。',
    weather: 'sunny',
    sprite: petGift,
    frames: 2,
    duration: '0.7s',
    petX: 96,
    petY: 166,
    extra: 'sparkle',
  },
]
