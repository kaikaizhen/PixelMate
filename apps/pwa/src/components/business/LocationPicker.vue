<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { useSubmit } from '@/composables/useSubmit'
import { useLocationStore } from '@/stores/useLocationStore'
import { usePetStore } from '@/stores/usePetStore'
import { SOURCE_LABEL, WEATHER_LABEL } from '@/types/weather.types'

// Where the player lives (for their weather): the browser's location, or a city from the list. Only a ~10 km grid cell is kept.
const emit = defineEmits<{ done: [] }>()
const loc = useLocationStore()
const pets = usePetStore()
const city = ref('')
const error = ref('')

onMounted(() => void loc.loadCities().catch(() => (error.value = '城市清單暫時載入不了。')))

const taiwan = computed(() => loc.cities.filter((c) => c.group === 'taiwan'))
const world = computed(() => loc.cities.filter((c) => c.group === 'world'))
const w = computed(() => pets.pet?.world)
const summary = computed(() => {
  if (!w.value?.weather) return ''
  const t = typeof w.value.temperatureC === 'number' ? ` ${Math.round(w.value.temperatureC)}°C` : ''
  return `${WEATHER_LABEL[w.value.weather] ?? w.value.weather}${t}　·　資料：${SOURCE_LABEL[w.value.weatherSource ?? 'simulated'] ?? ''}`
})
const place = computed(() => (loc.location ? `${loc.location.label || '已設定'}${loc.location.source === 'gps' ? '（定位）' : ''}` : '還沒設定（先當作住在臺北）'))

async function refreshPet() { await pets.load(true).catch(() => {}) }

const here = useSubmit(async () => {
  error.value = ''
  try {
    await loc.useHere()
    await refreshPet()
    emit('done')
  } catch (e) {
    error.value = e instanceof Error ? e.message : '定位失敗了。'
    if (e instanceof ApiError) error.value = e.message
  }
})
const pick = useSubmit(async () => {
  error.value = ''
  const c = loc.cities.find((x) => x.id === city.value)
  if (!c) return
  try {
    await loc.chooseCity(c)
    await refreshPet()
    emit('done')
  } catch (e) {
    error.value = e instanceof ApiError ? e.message : '儲存失敗了，請再試一次。'
  }
})
</script>

<template>
  <div class="loc">
    <p v-if="summary" class="now" role="status" aria-label="目前天氣">{{ summary }}</p>
    <p class="where">所在地：<strong>{{ place }}</strong></p>
    <p v-if="error" class="notice" role="alert">{{ error }}</p>
    <button type="button" class="primary" :disabled="here.loading.value" @click="here.submit()">{{ here.loading.value ? '定位中…' : '📍 使用目前位置' }}</button>
    <form class="pick" @submit.prevent="pick.submit()">
      <select v-model="city" aria-label="選擇縣市或城市">
        <option value="" disabled>或選一個縣市／城市</option>
        <optgroup label="臺灣"><option v-for="c in taiwan" :key="c.id" :value="c.id">{{ c.name }}</option></optgroup>
        <optgroup label="海外"><option v-for="c in world" :key="c.id" :value="c.id">{{ c.name }}</option></optgroup>
      </select>
      <button type="submit" class="ghost" :disabled="!city || pick.loading.value">套用</button>
    </form>
    <small>只會記住大約 10 公里的範圍，不會存精確位置；天色（日出、白天、日落、夜晚）依你這台裝置的當地時間。</small>
  </div>
</template>

<style scoped>
.loc { display: grid; gap: 0.5rem; }
.now { margin: 0; padding: 0.4rem 0.7rem; font-weight: 700; background: var(--c-cream); border: 2px solid var(--c-line); border-radius: 8px; font-size: 0.9rem; }
.where { margin: 0; font-size: 0.9rem; }
.notice { margin: 0; padding: 0.45rem 0.7rem; color: var(--c-error); background: var(--c-error-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.85rem; }
.primary { min-height: 44px; font: inherit; font-weight: 700; color: var(--c-ink); background: var(--c-yellow); color: var(--c-on-yellow); border: 3px solid var(--c-ink); border-radius: 8px; box-shadow: 2px 2px 0 var(--c-ink); cursor: pointer; }
.primary:disabled { opacity: 0.5; box-shadow: none; cursor: wait; }
.pick { display: flex; gap: 0.4rem; }
.pick select { flex: 1; min-width: 0; font: inherit; padding: 0.4rem 0.5rem; color: var(--c-ink); background: var(--c-field); border: 2px solid var(--c-ink); border-radius: 8px; }
.ghost { min-height: 40px; padding: 0 0.9rem; font: inherit; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.ghost:disabled { opacity: 0.4; cursor: not-allowed; }
small { font-size: 0.72rem; color: var(--c-ink-soft); }
</style>
