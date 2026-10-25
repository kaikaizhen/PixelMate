<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { authApi } from '@/api/authApi'
import { ApiError } from '@/api/http'
import AlertBox from '@/components/common/AlertBox.vue'
import AuthLayout from '@/components/business/AuthLayout.vue'

const route = useRoute()
const state = ref<'loading' | 'ok' | 'fail'>('loading')
const message = ref('')

onMounted(async () => {
  const userId = route.query.userId
  const token = route.query.token
  if (typeof userId !== 'string' || typeof token !== 'string') {
    state.value = 'fail'
    message.value = '這個連結不完整。'
    return
  }
  try {
    await authApi.confirmEmail(userId, token)
    state.value = 'ok'
  } catch (e) {
    state.value = 'fail'
    message.value = e instanceof ApiError ? e.message : '確認失敗，請稍後再試。'
  }
})
</script>

<template>
  <AuthLayout title="確認 Email">
    <p v-if="state === 'loading'" role="status">確認中…</p>
    <AlertBox v-else-if="state === 'ok'" tone="success">Email 已確認，現在可以登入了。</AlertBox>
    <AlertBox v-else tone="error">{{ message }} 如果連結過期了，可以回到登入頁選「重新寄送確認信」。</AlertBox>
    <template #links>
      <RouterLink :to="{ name: 'login', query: state === 'ok' ? { confirmed: '1' } : {} }">前往登入</RouterLink>
    </template>
  </AuthLayout>
</template>
