<script setup lang="ts">
import StatusBadge from '@/components/common/StatusBadge.vue'
import WeatherSwitch from '@/components/business/WeatherSwitch.vue'
import PixelScene from '@/components/business/PixelScene.vue'
</script>

<template>
  <section class="hero" aria-labelledby="hero-title">
    <div class="page-width hero__inner">
      <div class="hero__copy">
        <StatusBadge>即將與你見面</StatusBadge>
        <h1 id="hero-title" class="hero__title"><span class="hero__line">有些想念，</span><span class="hero__line">會自己走到你身邊。</span></h1>
        <p class="hero__lead">
          一隻會陪你生活、陪你成長，也會幫你把心意送到遠方的像素小夥伴。
        </p>
      </div>
      <div class="hero__scene">
        <PixelScene />
        <div class="hero__switch"><WeatherSwitch /></div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.hero {
  padding-block: var(--space-3) var(--space-5);
}

.hero__inner {
  display: grid;
  gap: var(--space-4);
  align-items: center;
}

.hero__copy {
  display: grid;
  justify-items: start;
  gap: var(--space-2);
}

.hero__title {
  margin: 0;
  font-size: clamp(1.9rem, 7vw, 3rem);
  line-height: 1.25;
  font-weight: 900;
  text-shadow: 3px 3px 0 var(--c-pink);
}

.hero__line {
  display: block;
  white-space: nowrap;
}

.hero__lead {
  margin: 0;
  max-width: 28em;
  font-size: 1.1rem;
  color: var(--c-ink-soft);
}

.hero__scene {
  padding-bottom: 1rem;
}

.hero__switch {
  display: flex;
  justify-content: center;
  margin-top: 3rem;
}

@media (min-width: 960px) {
  .hero__inner {
    grid-template-columns: minmax(26rem, 1fr) auto;
    gap: var(--space-5);
  }
}
</style>
