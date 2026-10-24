<script setup lang="ts"></script>

<template>
  <footer class="site-footer">
    <div class="page-width site-footer__inner">
      <p class="site-footer__brand">小伴星PixelMate</p>
      <p class="site-footer__note">官網中的互動皆為示範預覽，各項功能仍在準備中。</p>
      <p class="site-footer__copy">© 2026 小伴星PixelMate</p>
    </div>
  </footer>
</template>

<style scoped>
.site-footer {
  margin-top: var(--space-5);
  padding-block: var(--space-4);
  border-top: 4px dashed var(--c-ink);
  background: var(--c-paper);
  text-align: center;
}

.site-footer__inner > p {
  margin: 0.25rem 0;
}

.site-footer__brand {
  font-weight: 800;
}

.site-footer__note,
.site-footer__copy {
  color: var(--c-ink-soft);
  font-size: 0.9rem;
}
</style>
