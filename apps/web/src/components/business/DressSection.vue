<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from 'vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import { DRESS_ITEMS } from '@/constants/dressItems'
import { useOutfit } from '@/composables/useOutfit'
import petIdle from '@/assets/pixel/pet_idle.png'
import sparkle from '@/assets/pixel/sparkle.png'
import icons from '@/assets/pixel/acc_icons.png'

const { wornItems, changeCount, toggle, clear, isWearing } = useOutfit(DRESS_ITEMS)

const isSparkling = ref(false)
let sparkleTimer: ReturnType<typeof setTimeout> | undefined
watch(changeCount, () => {
  isSparkling.value = true
  clearTimeout(sparkleTimer)
  sparkleTimer = setTimeout(() => (isSparkling.value = false), 900)
})
onUnmounted(() => clearTimeout(sparkleTimer))

const caption = computed(() =>
  wornItems.value.length
    ? `今天的牠戴著${wornItems.value.map((item) => item.label).join('、')}。`
    : '素素的牠也很可愛。挑一件試試看吧。',
)
</script>

<template>
  <section id="dress" class="dress" aria-labelledby="dress-title">
    <div class="page-width dress__inner">
      <div class="dress__stage">
        <div class="dress__pet">
          <SpriteStrip :src="petIdle" :frames="4" :width="48" :height="48" duration="0.9s" />
          <SpriteStrip
            v-for="item in DRESS_ITEMS"
            :key="item.id"
            class="dress__layer"
            :class="{ 'dress__layer--off': !isWearing(item) }"
            :src="item.sprite"
            :frames="4"
            :width="48"
            :height="48"
            duration="0.9s"
          />
          <template v-if="isSparkling">
            <SpriteStrip :src="sparkle" :frames="4" :width="7" :height="7" :x="2" :y="10" duration="0.5s" />
            <SpriteStrip :src="sparkle" :frames="4" :width="7" :height="7" :x="40" :y="4" duration="0.5s" delay="-0.2s" />
            <SpriteStrip :src="sparkle" :frames="4" :width="7" :height="7" :x="42" :y="34" duration="0.5s" delay="-0.35s" />
          </template>
        </div>
      </div>

      <div class="dress__copy">
        <h2 id="dress-title" class="dress__title">長成獨一無二的模樣</h2>
        <p class="dress__lead">一頂帽子、一條圍巾，都是你們一起走過的小日子。</p>
        <div class="dress__items" role="group" aria-label="挑一件給牠">
          <button
            v-for="item in DRESS_ITEMS"
            :key="item.id"
            type="button"
            class="dress__item"
            :aria-pressed="isWearing(item)"
            @click="toggle(item)"
          >
            <span class="dress__icon pixelated" :style="{ backgroundImage: `url(${icons})`, backgroundPositionX: `${item.iconX * -48}px` }" aria-hidden="true" />
            {{ item.label }}
          </button>
          <button v-if="wornItems.length" type="button" class="dress__clear" @click="clear">全部脫下</button>
        </div>
        <p class="dress__caption" aria-live="polite">{{ caption }}</p>
        <p class="dress__note">互動預覽｜外觀選項僅為示範</p>
      </div>
    </div>
  </section>
</template>

<style scoped>
.dress {
  padding-block: var(--space-5);
  background: var(--c-paper);
  border-block: 4px dashed var(--c-ink);
}

.dress__inner {
  display: grid;
  gap: var(--space-4);
  align-items: center;
}

.dress__stage {
  display: grid;
  place-items: center;
  width: min(100%, 26rem);
  margin-inline: auto;
  padding: var(--space-4) var(--space-2);
  background:
    repeating-conic-gradient(#fde2dd 0 25%, #fff8ec 0 50%) 0 0 / 32px 32px;
  border: 4px solid var(--c-ink);
  box-shadow: 8px 8px 0 var(--c-ink);
}

.dress__pet {
  --s: 4;
  position: relative;
  width: calc(48px * var(--s));
  height: calc(48px * var(--s));
}

.dress__layer--off {
  visibility: hidden;
}

.dress__copy {
  display: grid;
  gap: var(--space-2);
  justify-items: start;
}

.dress__title {
  margin: 0;
  text-wrap: balance;
  font-size: clamp(1.7rem, 5vw, 2.4rem);
  font-weight: 900;
  text-shadow: 3px 3px 0 var(--c-yellow);
}

.dress__lead,
.dress__caption,
.dress__note {
  margin: 0;
  max-width: 28em;
}

.dress__lead {
  color: var(--c-ink-soft);
  font-size: 1.05rem;
}

.dress__items {
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem;
}

.dress__item,
.dress__clear {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  min-height: var(--tap-min);
  padding: 0 1rem;
  border: 3px solid var(--c-ink);
  background: #fff;
  color: var(--c-ink);
  font: inherit;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 3px 3px 0 var(--c-ink);
}

.dress__item[aria-pressed='true'] {
  background: var(--c-green);
  transform: translate(2px, 2px);
  box-shadow: 1px 1px 0 var(--c-ink);
}

.dress__clear {
  background: var(--c-paper);
}

.dress__icon {
  width: 48px;
  height: 32px;
  background-size: 144px 32px;
  background-repeat: no-repeat;
  flex: none;
}

.dress__caption {
  min-height: 3.4em;
  font-weight: 700;
}

.dress__note {
  color: var(--c-ink-soft);
  font-size: 0.85rem;
}

@media (min-width: 960px) {
  .dress__inner {
    grid-template-columns: auto minmax(22rem, 1fr);
    gap: var(--space-5);
  }
}
</style>
