<script setup lang="ts">
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import petIdle from '@/assets/pixel/pet_idle.png'
import phoneFrame from '@/assets/pixel/phone_frame.png'
import phoneBg from '@/assets/pixel/phone_bg.png'
import deskFrame from '@/assets/pixel/desk_frame.png'
import deskBg from '@/assets/pixel/desk_bg.png'

// kind：phone（手機網頁 App）或 desk（桌面小螢幕）。像素尺寸以邏輯像素定義，由 --s 放大。
const props = defineProps<{ kind: 'phone' | 'desk' }>()

const SPEC = {
  phone: { w: 72, h: 128, frame: phoneFrame, bg: phoneBg, sx: 8, sy: 12, sw: 56, sh: 104, px: 4, py: 50 },
  desk: { w: 88, h: 96, frame: deskFrame, bg: deskBg, sx: 12, sy: 10, sw: 64, sh: 64, px: 8, py: 14 },
} as const
const spec = SPEC[props.kind]
</script>

<template>
  <div
    class="device pixelated"
    :class="`device--${kind}`"
    :style="{ '--w': spec.w, '--h': spec.h, '--sx': spec.sx, '--sy': spec.sy, '--sw': spec.sw, '--sh': spec.sh }"
    aria-hidden="true"
  >
    <div class="device__screen">
      <img class="device__bg pixelated" :src="spec.bg" alt="" :width="spec.sw" :height="spec.sh" />
      <SpriteStrip :src="petIdle" :frames="4" :width="48" :height="48" :x="spec.px" :y="spec.py" duration="0.9s" />
    </div>
    <img class="device__frame pixelated" :src="spec.frame" alt="" :width="spec.w" :height="spec.h" />
  </div>
</template>

<style scoped>
.device {
  --s: 3;
  position: relative;
  width: calc(var(--w) * var(--s) * 1px);
  height: calc(var(--h) * var(--s) * 1px);
}

.device__screen {
  position: absolute;
  left: calc(var(--sx) * var(--s) * 1px);
  top: calc(var(--sy) * var(--s) * 1px);
  width: calc(var(--sw) * var(--s) * 1px);
  height: calc(var(--sh) * var(--s) * 1px);
  overflow: hidden;
}

.device__bg,
.device__frame {
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
}

.device__frame {
  left: 0;
  top: 0;
}

.device--phone .device__frame,
.device--desk .device__frame {
  inset: 0;
  left: 0;
}

@media (max-width: 479px) {
  .device {
    --s: 2;
  }
}
</style>
