<script setup lang="ts">
import PixelPet from './PixelPet.vue'

// The cream star pet used on the sign-in pages.
withDefaults(defineProps<{ scale?: number }>(), { scale: 3 })
</script>

<template>
  <PixelPet :scale="scale" />
</template>
