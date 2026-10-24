import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { WeatherScene } from '@/types/weather.types'

// 官網一律使用模擬天氣，不代表任何地區的即時資料。
export const useWeatherStore = defineStore('weather', () => {
  const scene = ref<WeatherScene>('sunny')

  function setScene(next: WeatherScene) {
    scene.value = next
  }

  return { scene, setScene }
})
