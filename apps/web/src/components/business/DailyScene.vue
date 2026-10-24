<script setup lang="ts">
import PixelRoom from '@/components/business/PixelRoom.vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import type { DailyActivity } from '@/types/daily.types'
import zzz from '@/assets/pixel/zzz.png'
import sparkle from '@/assets/pixel/sparkle.png'

defineProps<{ activity: DailyActivity }>()
</script>

<template>
  <PixelRoom :weather="activity.weather">
    <SpriteStrip
      :key="activity.id"
      :src="activity.sprite"
      :frames="activity.frames"
      :width="48"
      :height="48"
      :x="activity.petX"
      :y="activity.petY"
      :duration="activity.duration"
    />
    <template v-if="activity.extra === 'zzz'">
      <SpriteStrip :src="zzz" :frames="3" :width="10" :height="12" :x="activity.petX + 34" :y="activity.petY - 6" duration="2.4s" />
    </template>
    <template v-else-if="activity.extra === 'sparkle'">
      <SpriteStrip :src="sparkle" :frames="4" :width="7" :height="7" :x="activity.petX - 6" :y="activity.petY + 6" duration="0.8s" />
      <SpriteStrip :src="sparkle" :frames="4" :width="7" :height="7" :x="activity.petX + 44" :y="activity.petY - 4" duration="0.8s" delay="-0.4s" />
      <SpriteStrip :src="sparkle" :frames="4" :width="7" :height="7" :x="activity.petX + 36" :y="activity.petY + 30" duration="1.1s" delay="-0.2s" />
    </template>
  </PixelRoom>
</template>
