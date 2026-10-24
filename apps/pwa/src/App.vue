<script setup lang="ts">
import OfflineBanner from '@/components/common/OfflineBanner.vue'
import UpdatePrompt from '@/components/common/UpdatePrompt.vue'
</script>

<template>
  <OfflineBanner />
  <RouterView />
  <UpdatePrompt />
</template>
