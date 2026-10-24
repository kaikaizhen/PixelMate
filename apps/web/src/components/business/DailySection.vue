<script setup lang="ts">
import { computed } from 'vue'
import DailyScene from '@/components/business/DailyScene.vue'
import { DAILY_ACTIVITIES } from '@/constants/dailyActivities'
import { useAutoRotate } from '@/composables/useAutoRotate'

const { index, select } = useAutoRotate(DAILY_ACTIVITIES.length, 6000)
const current = computed(() => DAILY_ACTIVITIES[index.value]!)

function onKey(event: KeyboardEvent) {
  const last = DAILY_ACTIVITIES.length - 1
  if (event.key === 'ArrowRight' || event.key === 'ArrowDown') select(index.value === last ? 0 : index.value + 1)
  else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') select(index.value === 0 ? last : index.value - 1)
  else return
  event.preventDefault()
  document.getElementById(`daily-tab-${DAILY_ACTIVITIES[index.value]!.id}`)?.focus()
}
</script>

<template>
  <section id="daily" class="daily" aria-labelledby="daily-title">
    <div class="page-width daily__inner">
      <div class="daily__copy">
        <h2 id="daily-title" class="daily__title">牠有自己的日常</h2>
        <p class="daily__lead">你忙的時候，牠也沒閒著。可能在午睡、看雨，或撿回一個小禮物。</p>
        <div class="daily__tabs" role="tablist" aria-label="牠的日常" @keydown="onKey">
          <button
            v-for="(activity, i) in DAILY_ACTIVITIES"
            :id="`daily-tab-${activity.id}`"
            :key="activity.id"
            type="button"
            role="tab"
            class="daily__tab"
            :class="{ 'daily__tab--on': i === index }"
            :aria-selected="i === index"
            :aria-controls="'daily-panel'"
            :tabindex="i === index ? 0 : -1"
            @click="select(i)"
          >
            {{ activity.label }}
          </button>
        </div>
        <p class="daily__caption" aria-live="polite">{{ current.caption }}</p>
      </div>
      <div id="daily-panel" class="daily__scene" role="tabpanel" :aria-labelledby="`daily-tab-${current.id}`">
        <DailyScene :activity="current" />
      </div>
    </div>
  </section>
</template>

<style scoped>
.daily {
  padding-block: var(--space-5);
  background: var(--c-paper);
  border-block: 4px dashed var(--c-ink);
}

.daily__inner {
  display: grid;
  gap: var(--space-4);
  align-items: center;
}

.daily__copy {
  display: grid;
  gap: var(--space-2);
  justify-items: start;
}

.daily__title {
  margin: 0;
  font-size: clamp(1.7rem, 5vw, 2.4rem);
  font-weight: 900;
  text-shadow: 3px 3px 0 var(--c-blue);
}

.daily__lead,
.daily__caption {
  margin: 0;
  max-width: 28em;
}

.daily__lead {
  color: var(--c-ink-soft);
  font-size: 1.05rem;
}

.daily__tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem;
}

.daily__tab {
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

.daily__tab--on {
  background: var(--c-pink);
  transform: translate(2px, 2px);
  box-shadow: 1px 1px 0 var(--c-ink);
}

.daily__caption {
  min-height: 3.4em;
  font-weight: 700;
}

.daily__scene {
  padding-bottom: 2rem;
}

@media (min-width: 960px) {
  .daily__inner {
    grid-template-columns: auto minmax(22rem, 1fr);
    gap: var(--space-5);
  }

  .daily__scene {
    order: -1;
  }
}
</style>
