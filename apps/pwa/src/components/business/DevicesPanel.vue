<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { useSubmit } from '@/composables/useSubmit'
import { useDeviceStore } from '@/stores/useDeviceStore'
import { ago } from '@/utils/time'

// The owner's side of pairing: type the code a device shows on its screen, see which devices are connected, rename or remove them.
const store = useDeviceStore()
const code = ref('')
const name = ref('')
const message = ref('')
const error = ref('')
const confirmId = ref('')
const editing = ref('')
const newName = ref('')

onMounted(() => void store.load().catch(() => (error.value = '裝置清單暫時載入不了。')))

const pair = useSubmit(async () => {
  error.value = ''; message.value = ''
  try {
    const d = await store.pair(code.value, name.value)
    message.value = `已核准「${d.name}」！裝置幾秒內就會完成連線。`
    code.value = ''; name.value = ''
  } catch (e) {
    error.value = e instanceof ApiError ? e.message : '配對失敗了，請再試一次。'
  }
})

const revoke = useSubmit(async (id: string) => {
  if (confirmId.value !== id) return void (confirmId.value = id)
  confirmId.value = ''
  error.value = ''
  try { await store.revoke(id) } catch (e) { error.value = e instanceof ApiError ? e.message : '移除失敗了。' }
})

const rename = useSubmit(async (id: string) => {
  error.value = ''
  try {
    await store.rename(id, newName.value)
    editing.value = ''
  } catch (e) { error.value = e instanceof ApiError ? (e.fieldErrors.name?.[0] ?? e.message) : '改名失敗了。' }
})

function startRename(id: string, current: string) { editing.value = id; newName.value = current; confirmId.value = '' }
const STATE = { waiting: '等裝置連線中…', active: '使用中', revoked: '已移除' } as const
</script>

<template>
  <div class="dev">
    <p v-if="message" class="ok" role="status">{{ message }}</p>
    <p v-if="error" class="notice" role="alert">{{ error }}</p>

    <ul v-if="store.devices.length" class="list" aria-label="已配對的裝置">
      <li v-for="d in store.devices" :key="d.deviceId" :class="d.state">
        <div class="info">
          <template v-if="editing === d.deviceId">
            <form class="rename" @submit.prevent="rename.submit(d.deviceId)">
              <input v-model="newName" aria-label="裝置名稱" maxlength="30" />
              <button type="submit" class="ghost" :disabled="!newName.trim() || rename.loading.value">儲存</button>
              <button type="button" class="ghost" @click="editing = ''">取消</button>
            </form>
          </template>
          <template v-else>
            <strong>{{ d.name }}</strong>
            <small>{{ d.model }}</small>
            <small class="state" :data-state="d.state">{{ STATE[d.state] }}<template v-if="d.state === 'active'">　·　{{ ago(d.lastSeenAt) }}</template></small>
          </template>
        </div>
        <div v-if="editing !== d.deviceId" class="acts">
          <button type="button" class="ghost" @click="startRename(d.deviceId, d.name)">改名</button>
          <button type="button" class="ghost danger" :disabled="revoke.loading.value" @click="revoke.submit(d.deviceId)">{{ confirmId === d.deviceId ? '確定移除？' : '移除' }}</button>
        </div>
      </li>
    </ul>
    <p v-else-if="store.loaded" class="empty">還沒有配對裝置。打開你的小伴星裝置，畫面上會出現一組配對碼。</p>

    <form class="add" @submit.prevent="pair.submit()">
      <h3>配對新裝置</h3>
      <label>裝置畫面上的配對碼
        <input v-model="code" aria-label="配對碼" placeholder="ABCD-EFGH" autocomplete="off" autocapitalize="characters" spellcheck="false" maxlength="12" />
      </label>
      <label>幫它取個名字（選填）
        <input v-model="name" aria-label="裝置名稱（選填）" placeholder="例如：書桌上的小栗" maxlength="30" />
      </label>
      <button type="submit" class="primary" :disabled="!code.trim() || pair.loading.value">{{ pair.loading.value ? '配對中…' : '確認配對' }}</button>
      <small>配對碼 10 分鐘內有效、只能用一次。一隻小夥伴最多配對 3 台裝置，你隨時可以在這裡移除。</small>
    </form>
  </div>
</template>

<style scoped>
.dev { display: grid; gap: 0.6rem; }
.ok { margin: 0; padding: 0.5rem 0.75rem; color: var(--c-green); background: var(--c-green-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.85rem; }
.notice { margin: 0; padding: 0.5rem 0.75rem; color: var(--c-error); background: var(--c-error-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.85rem; }
.empty { margin: 0; color: var(--c-ink-soft); font-size: 0.85rem; }
.list { margin: 0; padding: 0; list-style: none; display: grid; gap: 0.4rem; }
.list li { display: grid; grid-template-columns: 1fr auto; align-items: center; gap: 0.5rem; padding: 0.45rem 0.6rem 0.45rem 0.8rem; background: var(--c-cream); border: 2px solid var(--c-line); border-radius: 8px; }
.list li.waiting { border-style: dashed; }
.info { display: grid; min-width: 0; }
.info strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.info small { color: var(--c-ink-soft); font-size: 0.72rem; }
.state[data-state='active'] { color: var(--c-green); font-weight: 700; }
.acts { display: flex; gap: 0.35rem; }
.rename { display: flex; gap: 0.35rem; grid-column: 1 / -1; }
.rename input { flex: 1; min-width: 0; }
input { font: inherit; font-size: 1rem; padding: 0.4rem 0.6rem; color: var(--c-ink); background: var(--c-field); border: 2px solid var(--c-ink); border-radius: 8px; }
.add { display: grid; gap: 0.4rem; padding: 0.6rem 0.7rem; border: 2px solid var(--c-line); border-radius: 8px; }
.add h3 { margin: 0; font-size: 0.95rem; }
.add label { display: grid; gap: 0.2rem; font-size: 0.82rem; }
.add input[aria-label='配對碼'] { text-transform: uppercase; font-family: var(--font-pixel); font-size: 0.95rem; letter-spacing: 0.06em; }
.add small { font-size: 0.72rem; color: var(--c-ink-soft); }
.primary { min-height: 44px; font: inherit; font-weight: 700; color: var(--c-on-yellow); background: var(--c-yellow); border: 3px solid var(--c-ink); border-radius: 8px; box-shadow: 2px 2px 0 var(--c-ink); cursor: pointer; }
.primary:disabled { opacity: 0.4; box-shadow: none; cursor: not-allowed; }
.ghost { min-height: 40px; padding: 0 0.8rem; font: inherit; font-size: 0.85rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.ghost.danger { color: var(--c-error); border-color: var(--c-error); }
.ghost:disabled { opacity: 0.4; cursor: not-allowed; }
</style>
