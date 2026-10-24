<script setup lang="ts">
import PixelRoom from '@/components/business/PixelRoom.vue'
import PixelStage from '@/components/business/PixelStage.vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import type { DailyActivity } from '@/types/daily.types'
import zzz from '@/assets/pixel/zzz.png'
import sparkle from '@/assets/pixel/sparkle.png'
import steam from '@/assets/pixel/steam.png'
import beach from '@/assets/pixel/beach.png'
import tetrapod from '@/assets/pixel/tetrapod.png'
import waves from '@/assets/pixel/waves.png'

defineProps<{ activity: DailyActivity }>()

// 消波塊堆疊位置（由後往前）
const TETRAPODS = [
  { x: 40, y: 76 },
  { x: 76, y: 78 },
  { x: 58, y: 62 },
]
</script>

<template>
  <PixelStage v-if="activity.scene === 'beach'" :height="144" :crop-x="30" label="海邊的消波塊上">
    <img class="beach__bg pixelated" :src="beach" alt="" width="240" height="144" />
    <SpriteStrip v-for="x in [0, 64, 128, 192]" :key="x" :src="waves" :frames="4" :width="64" :height="10" :x="x" :y="86" duration="2.4s" />
    <img
      v-for="(t, i) in TETRAPODS"
      :key="i"
      class="beach__tetra pixelated"
      :src="tetrapod"
      alt=""
      width="36"
      height="32"
      :style="{ '--x': t.x, '--y': t.y }"
    />
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
  </PixelStage>

  <PixelRoom v-else :weather="activity.weather">
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
    <template v-else-if="activity.extra === 'steam'">
      <SpriteStrip :src="steam" :frames="3" :width="8" :height="12" :x="activity.petX + 10" :y="activity.petY + 18" duration="1.2s" />
      <SpriteStrip :src="steam" :frames="3" :width="8" :height="12" :x="activity.petX + 28" :y="activity.petY + 16" duration="1.2s" delay="-0.5s" />
    </template>
  </PixelRoom>
</template>

<style scoped>
.beach__bg,
.beach__tetra {
  position: absolute;
}

.beach__bg {
  left: 0;
  top: 0;
  width: calc(240px * var(--s));
  height: calc(144px * var(--s));
}

.beach__tetra {
  left: calc(var(--x) * var(--s) * 1px);
  top: calc(var(--y) * var(--s) * 1px);
  width: calc(36px * var(--s));
  height: calc(32px * var(--s));
}
</style>
