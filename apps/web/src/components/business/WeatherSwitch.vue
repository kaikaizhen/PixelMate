<script setup lang="ts">
import { useWeatherStore } from '@/stores/useWeatherStore'
import type { WeatherScene } from '@/types/weather.types'

const OPTIONS: { value: WeatherScene; label: string }[] = [
  { value: 'sunny', label: '晴天' },
  { value: 'rain', label: '雨天' },
  { value: 'night', label: '夜晚' },
]

const weather = useWeatherStore()
</script>

<template>
  <div class="switch" role="radiogroup" aria-label="切換窗外天氣（模擬）">
    <button
      v-for="option in OPTIONS"
      :key="option.value"
      type="button"
      role="radio"
      class="switch__btn"
      :class="{ 'switch__btn--on': weather.scene === option.value }"
      :aria-checked="weather.scene === option.value"
      @click="weather.setScene(option.value)"
    >
      {{ option.label }}
    </button>
  </div>
</template>

<style scoped>
.switch {
  display: inline-flex;
  gap: 0.5rem;
}

.switch__btn {
  min-width: var(--tap-min);
  min-height: var(--tap-min);
  padding: 0 1.1rem;
  border: 3px solid var(--c-ink);
  background: #fff;
  color: var(--c-ink);
  font: inherit;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--c-ink);
}

.switch__btn--on {
  background: var(--c-yellow);
  transform: translate(2px, 2px);
  box-shadow: 1px 1px 0 var(--c-ink);
}
</style>
