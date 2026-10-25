<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { authApi } from '@/api/authApi'
import AlertBox from '@/components/common/AlertBox.vue'
import FormField from '@/components/common/FormField.vue'
import PixelButton from '@/components/common/PixelButton.vue'
import AuthLayout from '@/components/business/AuthLayout.vue'
import { useOnline } from '@/composables/useOnline'
import { useSubmit } from '@/composables/useSubmit'
import { validateEmail } from '@/utils/validators'

const route = useRoute()
const online = useOnline()
const email = ref(typeof route.query.email === 'string' ? route.query.email : '')
const emailError = ref('')
const sent = ref(false)

const { loading, error, submit } = useSubmit(async () => {
  await authApi.forgotPassword(email.value.trim())
  sent.value = true
})

function onSubmit() {
  emailError.value = validateEmail(email.value)
  if (!emailError.value) void submit()
}
</script>

<template>
  <AuthLayout title="忘記密碼" subtitle="輸入註冊用的 Email，我們會寄一封重新設定密碼的信給你。">
    <AlertBox v-if="sent" tone="success">
      如果這個 Email 已經註冊過，幾分鐘內會收到信。找不到的話也看看垃圾郵件匣。連結 1 小時內有效。
    </AlertBox>
    <form v-else novalidate @submit.prevent="onSubmit">
      <AlertBox v-if="error" tone="error">{{ error.message }}</AlertBox>
      <FormField v-model="email" label="Email" type="email" inputmode="email" autocomplete="email" :error="emailError" />
      <PixelButton type="submit" :loading="loading" :disabled="!online">{{ online ? '寄出重設連結' : '目前沒有網路' }}</PixelButton>
    </form>
    <template #links>
      <RouterLink :to="{ name: 'login' }">回到登入</RouterLink>
    </template>
  </AuthLayout>
</template>

<style scoped>
form { display: grid; gap: 0.75rem; }
</style>
