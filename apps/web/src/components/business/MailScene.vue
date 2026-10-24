<script setup lang="ts">
import PixelStage from '@/components/business/PixelStage.vue'
import SpriteStrip from '@/components/common/SpriteStrip.vue'
import type { MailPhase } from '@/composables/useMailDelivery'
import yard from '@/assets/pixel/yard.png'
import mailbox from '@/assets/pixel/mailbox.png'
import postmanWalk from '@/assets/pixel/postman_walk.png'
import postmanGive from '@/assets/pixel/postman_give.png'
import envelope from '@/assets/pixel/envelope.png'
import grass from '@/assets/pixel/grass_day.png'
import petIdle from '@/assets/pixel/pet_idle.png'
import petFaces from '@/assets/pixel/pet_faces.png'
import cloud from '@/assets/pixel/cloud_white.png'

const props = defineProps<{ phase: MailPhase; isPostmanLeaving: boolean }>()
const emit = defineEmits<{ ring: []; open: [] }>()

const hasMail = () => props.phase === 'arrived' || props.phase === 'reading'
</script>

<template>
  <PixelStage :height="144" :crop-x="40" label="小郵差把信送到信箱">
    <img class="yard__bg pixelated" :src="yard" alt="" width="240" height="144" />
    <span class="yard__cloud yard__cloud--a"><img class="pixelated" :src="cloud" alt="" width="26" height="11" /></span>
    <span class="yard__cloud yard__cloud--b"><img class="pixelated" :src="cloud" alt="" width="26" height="11" /></span>
    <SpriteStrip v-for="x in [0, 64, 128, 192]" :key="x" :src="grass" :frames="4" :width="64" :height="16" :x="x" :y="66" duration="1.6s" />

    <!-- 小夥伴 -->
    <div class="yard__pet" :class="{ 'yard__pet--hop': phase === 'arrived' }">
      <SpriteStrip v-if="phase !== 'arrived' && phase !== 'reading'" :src="petIdle" :frames="4" :width="48" :height="48" duration="0.9s" />
      <span v-else class="yard__pet-happy pixelated" :style="{ backgroundImage: `url(${petFaces})` }" />
    </div>

    <!-- 信箱：旗子在有信時升起 -->
    <button
      type="button"
      class="yard__mailbox"
      :disabled="phase !== 'idle' && phase !== 'done'"
      aria-label="點信箱，請郵差送一封示範信"
      @click="emit('ring')"
    >
      <span class="yard__mailbox-img pixelated" :class="{ 'yard__mailbox-img--up': hasMail() || phase === 'delivering' }" :style="{ backgroundImage: `url(${mailbox})` }" />
    </button>

    <!-- 郵差 -->
    <div v-if="phase === 'delivering'" class="yard__postman yard__postman--in">
      <SpriteStrip :src="postmanWalk" :frames="4" :width="40" :height="40" duration="0.5s" />
    </div>
    <div v-else-if="(phase === 'arrived' || phase === 'reading') && !isPostmanLeaving" class="yard__postman yard__postman--stay">
      <SpriteStrip :src="postmanGive" :frames="2" :width="40" :height="40" duration="0.8s" />
    </div>
    <div v-else-if="(phase === 'arrived' || phase === 'reading') && isPostmanLeaving" class="yard__postman yard__postman--out">
      <SpriteStrip :src="postmanWalk" :frames="4" :width="40" :height="40" duration="0.5s" />
    </div>

    <!-- 抵達的信 -->
    <button v-if="phase === 'arrived'" type="button" class="yard__letter" aria-label="打開信" @click="emit('open')">
      <img class="pixelated" :src="envelope" alt="" width="16" height="12" />
    </button>
  </PixelStage>
</template>

<style scoped>
.yard__bg,
.yard__cloud,
.yard__pet,
.yard__mailbox,
.yard__postman,
.yard__letter {
  position: absolute;
}

.yard__bg {
  left: 0;
  top: 0;
  width: calc(240px * var(--s));
  height: calc(144px * var(--s));
}

.yard__cloud {
  top: calc(12px * var(--s));
  left: calc(-32px * var(--s));
  animation: yard-drift 80s steps(120) infinite;
}

.yard__cloud--b {
  top: calc(34px * var(--s));
  animation-delay: -40s;
}

.yard__cloud img {
  width: calc(26px * var(--s));
  height: auto;
}

@keyframes yard-drift {
  to {
    transform: translateX(calc(290px * var(--s)));
  }
}

.yard__pet {
  left: calc(50px * var(--s));
  top: calc(80px * var(--s));
  width: calc(48px * var(--s));
  height: calc(48px * var(--s));
}

.yard__pet-happy {
  display: block;
  width: 100%;
  height: 100%;
  background-repeat: no-repeat;
  background-size: calc(96px * var(--s)) 100%;
  background-position-x: calc(-48px * var(--s));
}

.yard__pet--hop {
  animation: hop 0.6s steps(6) 3;
}

@keyframes hop {
  50% {
    transform: translateY(calc(-8px * var(--s)));
  }
}

.yard__mailbox {
  left: calc(150px * var(--s));
  top: calc(68px * var(--s));
  width: calc(24px * var(--s));
  height: calc(40px * var(--s));
  padding: 0;
  border: 0;
  background: none;
  cursor: pointer;
}

.yard__mailbox:disabled {
  cursor: default;
}

.yard__mailbox:not(:disabled) {
  animation: nudge 1.6s steps(2) infinite;
}

@keyframes nudge {
  50% {
    transform: translateY(calc(-1px * var(--s)));
  }
}

.yard__mailbox-img {
  display: block;
  width: 100%;
  height: 100%;
  background-repeat: no-repeat;
  background-size: calc(48px * var(--s)) 100%;
  background-position-x: 0;
}

.yard__mailbox-img--up {
  background-position-x: calc(-24px * var(--s));
}

.yard__postman {
  top: calc(86px * var(--s));
  left: calc(104px * var(--s));
  width: calc(40px * var(--s));
  height: calc(40px * var(--s));
}

.yard__postman--in {
  animation: postman-in 2.8s steps(154) forwards;
}

.yard__postman--out {
  animation: postman-out 2.6s steps(154) forwards;
}

@keyframes postman-in {
  from {
    transform: translateX(calc(-154px * var(--s)));
  }
  to {
    transform: translateX(0);
  }
}

@keyframes postman-out {
  from {
    transform: translateX(0) scaleX(-1);
  }
  to {
    transform: translateX(calc(-154px * var(--s))) scaleX(-1);
  }
}

.yard__letter {
  left: calc(152px * var(--s));
  top: calc(52px * var(--s));
  width: calc(20px * var(--s));
  min-width: var(--tap-min);
  min-height: var(--tap-min);
  padding: 0;
  border: 0;
  background: none;
  cursor: pointer;
  display: grid;
  place-items: center;
  animation: float 1s steps(4) infinite;
}

.yard__letter img {
  width: calc(16px * var(--s));
  height: calc(12px * var(--s));
}

@keyframes float {
  50% {
    transform: translateY(calc(-3px * var(--s)));
  }
}
</style>
