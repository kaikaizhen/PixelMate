<script setup lang="ts">
import EnvBadge from '@/components/common/EnvBadge.vue'
import OfflineBanner from '@/components/common/OfflineBanner.vue'
import UpdatePrompt from '@/components/common/UpdatePrompt.vue'
</script>

<template>
  <EnvBadge />
  <OfflineBanner />
  <RouterView />
  <UpdatePrompt />
</template>
