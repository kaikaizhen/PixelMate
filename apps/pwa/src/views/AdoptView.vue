<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import AlertBox from '@/components/common/AlertBox.vue'
import FormField from '@/components/common/FormField.vue'
import PixelButton from '@/components/common/PixelButton.vue'
import LookPicker from '@/components/business/LookPicker.vue'
import PixelPet from '@/components/business/PixelPet.vue'
import { NAME_IDEAS, PET_LOOKS, colorOf } from '@/constants/petLooks'
import { useOnline } from '@/composables/useOnline'
import { useSubmit } from '@/composables/useSubmit'
import { usePetStore } from '@/stores/usePetStore'
import { validatePetName } from '@/utils/validators'

const wide = typeof window !== 'undefined' && window.matchMedia?.('(min-width: 860px)').matches
const petScale = wide ? 7 : 3
const pets = usePetStore()
const router = useRouter()
const online = useOnline()

const look = ref<string>(PET_LOOKS[0].id)
const name = ref('')
const local = ref<Record<string, string>>({})
const color = computed(() => colorOf(look.value))
const face = computed(() => (name.value.trim() ? 'happy' : 'normal'))

const { loading, error, fieldErrors, submit } = useSubmit(async () => {
  try {
    await pets.adopt({ name: name.value.trim(), bodyVariant: look.value })
  } catch (e) {
    if (e instanceof ApiError && e.code === 'PET_ALREADY_EXISTS') {
      await pets.load(true) // another tab already adopted: just show that pet
      await router.replace({ name: 'home' })
      return
    }
    throw e
  }
  await router.replace({ name: 'home', query: { welcome: '1' } })
})

const errors = computed(() => ({ ...fieldErrors.value, ...Object.fromEntries(Object.entries(local.value).filter(([, v]) => v)) }))

function suggest() {
  const pool = NAME_IDEAS.filter((n) => n !== name.value)
  name.value = pool[Math.floor(Math.random() * pool.length)]
}

function onSubmit() {
  local.value = { name: validatePetName(name.value) }
  if (!local.value.name) void submit()
}
</script>

<template>
  <main class="adopt">
    <header>
      <PixelPet :color="color" :face="face" :scale="petScale" label="你選的小夥伴預覽" />
      <h1>領養你的第一隻小夥伴</h1>
      <p>選一個喜歡的外觀，幫牠取個名字吧。</p>
    </header>

    <form class="card" novalidate @submit.prevent="onSubmit">
      <AlertBox v-if="error && !Object.keys(fieldErrors).length" tone="error">{{ error.message }}</AlertBox>
      <LookPicker v-model="look" :error="errors.bodyVariant" />
      <FormField v-model="name" label="小夥伴的名字" autocomplete="off" :maxlength="12" :error="errors.name" hint="最多 12 個字，可以用中文、英文和數字。">
        <button type="button" class="idea" @click="suggest">幫我想一個 ✦</button>
      </FormField>
      <PixelButton type="submit" :loading="loading" :disabled="!online">{{ online ? '領養牠！' : '目前沒有網路' }}</PixelButton>
    </form>
  </main>
</template>

<style scoped>
.adopt {
  min-height: 100dvh;
  display: grid;
  align-content: center;
  justify-items: center;
  gap: 0.75rem;
  padding: 1rem;
}
header { display: grid; justify-items: center; gap: 0.4rem; text-align: center; max-width: 26rem; }
h1 { font-size: 1.3rem; line-height: 1.3; }
header p { color: var(--c-ink-soft); font-size: 0.9rem; }
.card {
  width: min(100%, 26rem);
  display: grid;
  gap: 0.85rem;
  padding: 1.25rem;
  background: var(--c-paper);
  border: 3px solid var(--c-ink);
  border-radius: 14px;
  box-shadow: 6px 6px 0 var(--c-ink);
}
.idea {
  justify-self: start;
  min-height: 40px;
  padding: 0 0.9rem;
  font: inherit;
  font-size: 0.9rem;
  color: var(--c-ink);
  background: var(--c-pink-soft);
  border: 2px solid var(--c-ink);
  border-radius: 8px;
  cursor: pointer;
}
@media (min-width: 860px) {
  .adopt { grid-template-columns: auto minmax(0, 26rem); column-gap: 4rem; justify-content: center; }
  h1 { font-size: 1.6rem; }
}
</style>
