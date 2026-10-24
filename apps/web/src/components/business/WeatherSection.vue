<script setup lang="ts">
import { computed } from 'vue'
import WeatherRoom from '@/components/business/WeatherRoom.vue'
import WeatherSwitch from '@/components/business/WeatherSwitch.vue'
import { useWeatherStore } from '@/stores/useWeatherStore'
import type { WeatherScene } from '@/types/weather.types'

const CAPTIONS: Record<WeatherScene, string> = {
  sunny: '晴天，窗外的草輕輕搖，牠想出去散散步。',
  rain: '下雨了，牠窩在窗邊，看雨滴一顆一顆落下。',
  night: '夜深了，月亮和星星陪著，牠睡得很沉。',
}

const weather = useWeatherStore()
const caption = computed(() => CAPTIONS[weather.scene])
</script>

<template>
  <section id="weather" class="weather" aria-labelledby="weather-title">
    <div class="page-width weather__inner">
      <div class="weather__copy">
        <h2 id="weather-title" class="weather__title">每一天，窗外的世界都不同</h2>
        <p class="weather__lead">晴天牠想散步，下雨牠想窩在窗邊。</p>
        <WeatherSwitch />
        <p class="weather__caption" aria-live="polite">{{ caption }}</p>
        <p class="weather__note">互動預覽｜天氣為模擬畫面，不代表任何地區的即時天氣。</p>
      </div>
      <div class="weather__scene">
        <WeatherRoom :weather="weather.scene" />
      </div>
    </div>
  </section>
</template>

<style scoped>
.weather {
  padding-block: var(--space-5);
}

.weather__inner {
  display: grid;
  gap: var(--space-4);
  align-items: center;
}

.weather__copy {
  display: grid;
  gap: var(--space-2);
  justify-items: start;
}

.weather__title {
  margin: 0;
  text-wrap: balance;
  font-size: clamp(1.7rem, 5vw, 2.4rem);
  font-weight: 900;
  text-shadow: 3px 3px 0 var(--c-blue);
}

.weather__lead,
.weather__caption,
.weather__note {
  margin: 0;
  max-width: 28em;
}

.weather__lead {
  color: var(--c-ink-soft);
  font-size: 1.05rem;
}

.weather__caption {
  min-height: 3.4em;
  font-weight: 700;
}

.weather__note {
  color: var(--c-ink-soft);
  font-size: 0.85rem;
}

.weather__scene {
  padding-bottom: 2rem;
}

@media (min-width: 960px) {
  .weather__inner {
    grid-template-columns: minmax(22rem, 1fr) auto;
    gap: var(--space-5);
  }
}
</style>
