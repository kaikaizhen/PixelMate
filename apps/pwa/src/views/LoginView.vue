<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authApi } from '@/api/authApi'
import AlertBox from '@/components/common/AlertBox.vue'
import FormField from '@/components/common/FormField.vue'
import PasswordField from '@/components/common/PasswordField.vue'
import PixelButton from '@/components/common/PixelButton.vue'
import AuthLayout from '@/components/business/AuthLayout.vue'
import { useOnline } from '@/composables/useOnline'
import { useSubmit } from '@/composables/useSubmit'
import { useAuthStore } from '@/stores/useAuthStore'
import { safeRedirect, validateEmail } from '@/utils/validators'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const online = useOnline()

const email = ref(typeof route.query.email === 'string' ? route.query.email : '')
const password = ref('')
const emailError = ref('')
const resent = ref(false)

const { loading, error, submit } = useSubmit(async () => {
  await auth.login({ email: email.value.trim(), password: password.value })
  await router.replace(safeRedirect(route.query.redirect))
})
const resend = useSubmit(async () => {
  await authApi.resendConfirmation(email.value.trim())
  resent.value = true
})

function onSubmit() {
  emailError.value = validateEmail(email.value)
  if (!emailError.value && password.value) void submit()
}

const notice = route.query.reset ? '密碼已經更新，請用新密碼登入。' : route.query.confirmed ? 'Email 已確認，現在可以登入了。' : ''
</script>

<template>
  <AuthLayout title="歡迎回來" subtitle="登入後就能見到你的小夥伴。">
    <AlertBox v-if="notice" tone="success">{{ notice }}</AlertBox>
    <AlertBox v-if="error" tone="error">
      {{ error.message }}
      <template v-if="error.code === 'EMAIL_NOT_CONFIRMED'">
        <button type="button" class="linkish" :disabled="resend.loading.value" @click="resend.submit()">重新寄送確認信</button>
      </template>
    </AlertBox>
    <AlertBox v-if="resent" tone="success">已重新寄出確認信，請到信箱查看。</AlertBox>

    <form novalidate @submit.prevent="onSubmit">
      <FormField v-model="email" label="Email" type="email" inputmode="email" autocomplete="username" :error="emailError" />
      <PasswordField v-model="password" label="密碼" autocomplete="current-password" />
      <p class="forgot"><RouterLink :to="{ name: 'forgot-password', query: email ? { email } : {} }">忘記密碼？</RouterLink></p>
      <PixelButton type="submit" :loading="loading" :disabled="!online">{{ online ? '登入' : '目前沒有網路' }}</PixelButton>
    </form>

    <template #links>
      <p>還沒有帳號？ <RouterLink :to="{ name: 'register' }">立即註冊</RouterLink></p>
    </template>
  </AuthLayout>
</template>

<style scoped>
form { display: grid; gap: var(--space-2); }
.forgot { text-align: right; font-size: 0.9rem; margin-top: -0.5rem; }
.linkish { margin-left: 0.5rem; font: inherit; color: inherit; background: none; border: 0; text-decoration: underline; cursor: pointer; }
</style>
