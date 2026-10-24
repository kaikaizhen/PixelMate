<script setup lang="ts">
import { computed, ref } from 'vue'
import AlertBox from '@/components/common/AlertBox.vue'
import FormField from '@/components/common/FormField.vue'
import PasswordChecklist from '@/components/common/PasswordChecklist.vue'
import PasswordField from '@/components/common/PasswordField.vue'
import PixelButton from '@/components/common/PixelButton.vue'
import AuthLayout from '@/components/business/AuthLayout.vue'
import { useOnline } from '@/composables/useOnline'
import { useSubmit } from '@/composables/useSubmit'
import { useAuthStore } from '@/stores/useAuthStore'
import { validateDisplayName, validateEmail, validatePassword } from '@/utils/validators'

const auth = useAuthStore()
const online = useOnline()

const displayName = ref('')
const email = ref('')
const password = ref('')
const confirm = ref('')
const local = ref<Record<string, string>>({})
const done = ref<{ confirm: boolean } | null>(null)

const { loading, error, fieldErrors, submit } = useSubmit(async () => {
  const r = await auth.register({ email: email.value.trim(), password: password.value, displayName: displayName.value.trim() })
  done.value = { confirm: r.emailConfirmationRequired }
})

const errors = computed(() => ({ ...fieldErrors.value, ...Object.fromEntries(Object.entries(local.value).filter(([, v]) => v)) }))

function onSubmit() {
  local.value = {
    displayName: validateDisplayName(displayName.value),
    email: validateEmail(email.value),
    password: validatePassword(password.value),
    confirm: confirm.value === '' ? '請再輸入一次密碼。' : confirm.value !== password.value ? '兩次輸入的密碼不一樣。' : '',
  }
  if (Object.values(local.value).every((v) => !v)) void submit()
}
</script>

<template>
  <AuthLayout :title="done ? '註冊完成' : '建立帳號'" :subtitle="done ? undefined : '註冊後就能領養你的第一隻小夥伴。'">
    <template v-if="done">
      <AlertBox tone="success">
        <template v-if="done.confirm">我們寄了一封確認信到 {{ email }}，請點信裡的連結後再登入。</template>
        <template v-else>帳號建立好了，現在可以登入。</template>
      </AlertBox>
      <RouterLink class="cta" :to="{ name: 'login', query: { email } }">前往登入</RouterLink>
    </template>

    <form v-else novalidate @submit.prevent="onSubmit">
      <AlertBox v-if="error && !Object.keys(fieldErrors).length" tone="error">{{ error.message }}</AlertBox>
      <FormField v-model="displayName" label="暱稱" autocomplete="nickname" :maxlength="20" :error="errors.displayName" hint="最多 20 個字，之後可以幫小夥伴取名。" />
      <FormField v-model="email" label="Email" type="email" inputmode="email" autocomplete="username" :error="errors.email" />
      <PasswordField v-model="password" label="密碼" autocomplete="new-password" :error="errors.password">
        <PasswordChecklist :password="password" />
      </PasswordField>
      <PasswordField v-model="confirm" label="再輸入一次密碼" autocomplete="new-password" :error="errors.confirm" />
      <PixelButton type="submit" :loading="loading" :disabled="!online">{{ online ? '註冊' : '目前沒有網路' }}</PixelButton>
    </form>

    <template #links>
      <p>已經有帳號？ <RouterLink :to="{ name: 'login' }">登入</RouterLink></p>
    </template>
  </AuthLayout>
</template>

<style scoped>
form { display: grid; gap: var(--space-2); }
.cta {
  display: grid;
  place-items: center;
  min-height: var(--tap-min);
  background: var(--c-yellow);
  color: #4a3040;
  font-weight: 700;
  text-decoration: none;
  border: 3px solid var(--c-ink);
  border-radius: var(--radius);
  box-shadow: 4px 4px 0 var(--c-ink);
}
</style>
