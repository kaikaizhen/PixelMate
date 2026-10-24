<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authApi } from '@/api/authApi'
import AlertBox from '@/components/common/AlertBox.vue'
import PasswordChecklist from '@/components/common/PasswordChecklist.vue'
import PasswordField from '@/components/common/PasswordField.vue'
import PixelButton from '@/components/common/PixelButton.vue'
import AuthLayout from '@/components/business/AuthLayout.vue'
import { useOnline } from '@/composables/useOnline'
import { useSubmit } from '@/composables/useSubmit'
import { validatePassword } from '@/utils/validators'

const route = useRoute()
const router = useRouter()
const online = useOnline()

const email = computed(() => (typeof route.query.email === 'string' ? route.query.email : ''))
const token = computed(() => (typeof route.query.token === 'string' ? route.query.token : ''))
const linkOk = computed(() => !!email.value && !!token.value)

const password = ref('')
const confirm = ref('')
const local = ref<Record<string, string>>({})

const { loading, error, fieldErrors, submit } = useSubmit(async () => {
  await authApi.resetPassword({ email: email.value, token: token.value, newPassword: password.value })
  await router.replace({ name: 'login', query: { reset: '1', email: email.value } })
})

const errors = computed(() => ({ ...fieldErrors.value, ...Object.fromEntries(Object.entries(local.value).filter(([, v]) => v)) }))

function onSubmit() {
  local.value = {
    password: validatePassword(password.value),
    confirm: password.value && confirm.value !== password.value ? '兩次輸入的密碼不一樣。' : confirm.value ? '' : '請再輸入一次密碼。',
  }
  if (Object.values(local.value).every((v) => !v)) void submit()
}
</script>

<template>
  <AuthLayout title="設定新密碼">
    <AlertBox v-if="!linkOk" tone="error">這個連結不完整，請重新申請一封重設密碼的信。</AlertBox>
    <form v-else novalidate @submit.prevent="onSubmit">
      <AlertBox v-if="error && error.code === 'INVALID_TOKEN'" tone="error">
        {{ error.message }} <RouterLink :to="{ name: 'forgot-password', query: { email } }">重新申請</RouterLink>
      </AlertBox>
      <AlertBox v-else-if="error && !Object.keys(fieldErrors).length" tone="error">{{ error.message }}</AlertBox>
      <PasswordField v-model="password" label="新密碼" autocomplete="new-password" :error="errors.password || errors.newPassword">
        <PasswordChecklist :password="password" />
      </PasswordField>
      <PasswordField v-model="confirm" label="再輸入一次新密碼" autocomplete="new-password" :error="errors.confirm" />
      <PixelButton type="submit" :loading="loading" :disabled="!online">{{ online ? '更新密碼' : '目前沒有網路' }}</PixelButton>
    </form>
    <template #links>
      <RouterLink :to="{ name: 'login' }">回到登入</RouterLink>
    </template>
  </AuthLayout>
</template>

<style scoped>
form { display: grid; gap: 0.75rem; }
</style>
