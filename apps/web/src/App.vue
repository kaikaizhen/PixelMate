<script setup lang="ts">
import SiteHeader from '@/components/business/SiteHeader.vue'
import SiteFooter from '@/components/business/SiteFooter.vue'
</script>

<template>
  <SiteHeader />
  <main id="main">
    <RouterView />
  </main>
  <SiteFooter />
</template>
