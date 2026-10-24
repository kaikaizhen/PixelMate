<script setup lang="ts">
import DevicePreview from '@/components/business/DevicePreview.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
</script>

<template>
  <section id="platforms" class="platforms" aria-labelledby="platforms-title">
    <div class="page-width">
      <h2 id="platforms-title" class="platforms__title">兩個地方，都找得到牠</h2>
      <p class="platforms__lead">先從手機開始認識牠，接著，牠也會住進你的桌面。</p>

      <div class="platforms__grid">
        <article class="platforms__card">
          <div class="platforms__art"><DevicePreview kind="phone" /></div>
          <StatusBadge>即將登場</StatusBadge>
          <h3 class="platforms__name">手機・網頁 App（PWA）</h3>
          <p class="platforms__text">加到主畫面就像一個 App，打開就能看牠今天在做什麼。</p>
        </article>

        <article class="platforms__card">
          <div class="platforms__art platforms__art--desk"><DevicePreview kind="desk" /></div>
          <StatusBadge>即將登場</StatusBadge>
          <h3 class="platforms__name">桌面小螢幕（ESP32）</h3>
          <p class="platforms__text">放在桌上的一扇小窗，牠在裡面生活，也等著收你的信。</p>
        </article>
      </div>

      <p class="platforms__note">實際推出內容與時間，以官方公告為準。</p>
    </div>
  </section>
</template>

<style scoped>
.platforms {
  padding-block: var(--space-5);
  background: var(--c-paper);
  border-block: 4px dashed var(--c-ink);
  text-align: center;
}

.platforms__title {
  margin: 0;
  text-wrap: balance;
  font-size: clamp(1.7rem, 5vw, 2.4rem);
  font-weight: 900;
  text-shadow: 3px 3px 0 var(--c-pink);
}

.platforms__lead {
  margin: var(--space-1) auto 0;
  max-width: 28em;
  color: var(--c-ink-soft);
  font-size: 1.05rem;
}

.platforms__grid {
  display: grid;
  gap: var(--space-4);
  margin-top: var(--space-4);
}

.platforms__card {
  display: grid;
  justify-items: center;
  gap: var(--space-2);
  padding: var(--space-3);
  background: var(--c-cream);
  border: 4px solid var(--c-ink);
  box-shadow: 8px 8px 0 var(--c-ink);
}

.platforms__art {
  display: grid;
  place-items: end center;
  min-height: calc(128px * 3);
}

.platforms__name {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 800;
}

.platforms__text {
  margin: 0;
  max-width: 22em;
  color: var(--c-ink-soft);
}

.platforms__note {
  margin: var(--space-3) 0 0;
  color: var(--c-ink-soft);
  font-size: 0.85rem;
}

@media (max-width: 479px) {
  .platforms__art {
    min-height: calc(128px * 2);
  }
}

@media (min-width: 760px) {
  .platforms__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    align-items: stretch;
  }
}
</style>
