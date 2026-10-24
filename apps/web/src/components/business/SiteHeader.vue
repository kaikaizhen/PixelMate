<script setup lang="ts">
import petFaces from '@/assets/pixel/pet_faces.png'
</script>

<template>
  <header class="site-header">
    <div class="page-width site-header__inner">
      <a class="site-header__brand" href="/" aria-label="小伴星PixelMate 首頁">
        <span class="site-header__logo pixelated" :style="{ backgroundImage: `url(${petFaces})` }" aria-hidden="true" />
        <span class="site-header__name">小伴星<span class="site-header__en">PixelMate</span></span>
      </a>
    </div>
  </header>
</template>

<style scoped>
.site-header {
  padding-block: var(--space-2);
}

.site-header__inner {
  display: flex;
  align-items: center;
}

.site-header__brand {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  min-height: var(--tap-min);
  color: var(--c-ink);
  text-decoration: none;
  font-weight: 800;
  font-size: 1.25rem;
}

.site-header__logo {
  width: 48px;
  height: 48px;
  background-size: 96px 48px;
  background-position: -48px 0;
  flex: none;
}

.site-header__en {
  margin-left: 0.5rem;
  font-family: var(--font-pixel);
  font-size: 0.6rem;
  font-weight: 400;
  color: var(--c-ink-soft);
}
</style>
