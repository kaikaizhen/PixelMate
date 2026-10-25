<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { socialApi } from '@/api/socialApi'
import { useSubmit } from '@/composables/useSubmit'
import { useGameStore } from '@/stores/useGameStore'
import { useInventoryStore } from '@/stores/useInventoryStore'
import { useSocialStore } from '@/stores/useSocialStore'
import { MAX_LETTER_LENGTH, type InviteCreated, type InvitePreview, type LetterFull, type LetterSummary, type ReadResult, type RelationKind } from '@/types/social.types'
import { shortTime } from '@/utils/time'

// The mailbox card: inbox / sent / friends, reading a letter, writing one, and pairing with a friend by invite code.
type View = 'inbox' | 'sent' | 'friends' | 'read' | 'write'
const social = useSocialStore()
const bag = useInventoryStore()
const game = useGameStore()

const view = ref<View>('inbox')
const error = ref('')
const fail = (e: unknown, fallback = '這個操作暫時失敗了，請稍後再試。') => (error.value = e instanceof ApiError ? e.message : fallback)

async function open(v: 'inbox' | 'sent' | 'friends', keepNote = false) {
  view.value = v
  error.value = ''
  if (!keepNote) sentOk.value = ''
  try {
    if (v === 'inbox') await social.loadInbox()
    else if (v === 'sent') await social.loadSent()
    else await social.loadFriends()
  } catch (e) { fail(e, '信箱暫時打不開，請稍後再試。') }
}
onMounted(() => void Promise.all([open('inbox'), social.loadFriends().catch(() => {}), bag.load().catch(() => {}), game.load()]))

// ---- reading ---------------------------------------------------------------------------------------------------
const letter = ref<LetterFull | null>(null)
const received = ref<ReadResult | null>(null)
const confirmDelete = ref(false)

const readOp = useSubmit(async (s: LetterSummary) => {
  error.value = ''
  letter.value = await socialApi.letter(s.letterId)
  received.value = null
  confirmDelete.value = false
  view.value = 'read'
  if (!letter.value.mine && !letter.value.readAt) {            // opening a letter marks it read and hands over its gift
    received.value = await socialApi.read(s.letterId)
    letter.value = { ...letter.value, readAt: received.value.readAt }
    await Promise.all([social.loadInbox(), received.value.giftGranted ? bag.load(true) : null])
  }
})
const openLetter = (s: LetterSummary) => readOp.submit(s)

const delOp = useSubmit(async () => {
  if (!letter.value) return
  if (!confirmDelete.value) return void (confirmDelete.value = true)
  await socialApi.remove(letter.value.letterId)
  await open(letter.value.mine ? 'sent' : 'inbox')
})

// ---- writing ---------------------------------------------------------------------------------------------------
const PRESETS = ['今天也想著你。', '謝謝你！', '加油，你最棒了！', '晚安，好夢。', '好久不見，最近好嗎？', '我們的小夥伴一起玩吧！']
const to = ref('')
const body = ref('')
const paper = ref('')
const gift = ref('')
const papers = computed(() => bag.items.filter((i) => i.category === 'stationery'))
const gifts = computed(() => bag.items.filter((i) => !(i.equipped && i.quantity <= 1)))
const left = computed(() => MAX_LETTER_LENGTH - [...new Intl.Segmenter().segment(body.value.trim())].length)
const sentOk = ref('')
let lastSig = ''
let lastKey = ''

function write(userId = '') {
  to.value = userId || social.friends[0]?.other.userId || ''
  body.value = ''; paper.value = ''; gift.value = ''
  error.value = ''; sentOk.value = ''
  view.value = 'write'
}

const sendOp = useSubmit(async () => {
  error.value = ''
  const input = { recipientUserId: to.value, body: body.value, paperItemId: paper.value || null, giftItemId: gift.value || null }
  const sig = JSON.stringify(input)
  if (sig !== lastSig) { lastSig = sig; lastKey = crypto.randomUUID() }      // same letter = same key, so a retry never sends two
  await socialApi.send(input, lastKey)
  lastSig = ''
  sentOk.value = '郵差出發了！大約 5～15 分鐘後會送到。'
  await Promise.all([open('sent', true), gift.value ? bag.load(true) : null])
})
const canSend = computed(() => !!to.value && body.value.trim().length > 0 && left.value >= 0)

// ---- friends ---------------------------------------------------------------------------------------------------
const kind = ref<RelationKind>('friend')
const invite = ref<InviteCreated | null>(null)
const copied = ref(false)
const code = ref('')
const found = ref<InvitePreview | null>(null)
const joined = ref('')
const confirmEnd = ref('')

const inviteOp = useSubmit(async () => {
  error.value = ''
  invite.value = await socialApi.createInvite(kind.value)
  copied.value = false
})
async function copy() {
  try { await navigator.clipboard.writeText(invite.value!.code); copied.value = true } catch { /* the code is on screen anyway */ }
}
const previewOp = useSubmit(async () => {
  error.value = ''; joined.value = ''
  found.value = await socialApi.preview(code.value)
})
const acceptOp = useSubmit(async () => {
  error.value = ''
  const r = await socialApi.accept(code.value)
  joined.value = `你和${r.other.displayName}成為${r.kind === 'couple' ? '情侶' : '好友'}了！`
  found.value = null; code.value = ''
  await social.loadFriends()
})
const endOp = useSubmit(async (id: string) => {
  if (confirmEnd.value !== id) return void (confirmEnd.value = id)
  confirmEnd.value = ''
  await socialApi.endRelationship(id)
  await social.loadFriends()
})
const errText = (o: { error: { value: ApiError | null } }) => o.error.value?.message
</script>

<template>
  <div class="mail">
    <p v-if="error" class="notice" role="alert">{{ error }}</p>
    <p v-else-if="errText(readOp) || errText(sendOp) || errText(delOp)" class="notice" role="alert">{{ errText(readOp) || errText(sendOp) || errText(delOp) }}</p>

    <p v-if="sentOk" class="ok" role="status">{{ sentOk }}</p>

    <div class="tabs" role="tablist" aria-label="信箱">
      <button type="button" role="tab" :aria-selected="view === 'inbox'" :class="{ on: view === 'inbox' }" @click="open('inbox')">收件匣<b v-if="social.unread" class="dot" :aria-label="`${social.unread} 封未讀`">{{ social.unread }}</b></button>
      <button type="button" role="tab" :aria-selected="view === 'sent'" :class="{ on: view === 'sent' }" @click="open('sent')">寄件匣</button>
      <button type="button" role="tab" :aria-selected="view === 'friends'" :class="{ on: view === 'friends' }" @click="open('friends')">好友</button>
    </div>

    <!-- inbox / sent -->
    <template v-if="view === 'inbox' || view === 'sent'">
      <button type="button" class="primary" :disabled="!social.friends.length" @click="write()">✏️ 寫信</button>
      <p v-if="!social.friends.length" class="hint">先到「好友」交個朋友，就能寫信囉。</p>
      <ul v-if="(view === 'inbox' ? social.inbox : social.sent).length" class="letters" :aria-label="view === 'inbox' ? '收到的信' : '寄出的信'">
        <li v-for="l in (view === 'inbox' ? social.inbox : social.sent)" :key="l.letterId">
          <button type="button" class="letter" :class="{ new: view === 'inbox' && !l.readAt }" :disabled="readOp.loading.value" @click="openLetter(l)">
            <span class="who">{{ view === 'inbox' ? '' : '寄給 ' }}{{ l.otherDisplayName }}<em v-if="l.hasGift"> 🎁</em></span>
            <span class="pv">{{ l.preview }}</span>
            <small>{{ view === 'sent' && !l.delivered ? '🚶 郵差送信中…' : shortTime(l.deliverAt) }}<template v-if="view === 'sent' && l.delivered && l.readAt"> ・已讀</template></small>
          </button>
        </li>
      </ul>
      <p v-else class="empty">{{ view === 'inbox' ? '還沒有收到信。' : '還沒有寄出信。' }}</p>
    </template>

    <!-- reading -->
    <template v-else-if="view === 'read' && letter">
      <article class="paper" aria-label="信件內容">
        <header><strong>{{ letter.mine ? '寄出的信' : `${letter.senderDisplayName} 寄來的信` }}</strong><small>{{ shortTime(letter.deliverAt) }}</small></header>
        <p class="text">{{ letter.body }}</p>
        <p v-if="letter.gift" class="gift">
          <img v-if="game.url(letter.gift.assetKey)" :src="game.url(letter.gift.assetKey)" alt="" draggable="false" class="pixelated" />
          <span>{{ letter.mine ? `附上禮物：${letter.gift.name}` : received?.giftGranted ? `收到禮物：${letter.gift.name}，已放進背包！` : `禮物：${letter.gift.name}` }}</span>
        </p>
      </article>
      <div class="acts">
        <button type="button" class="ghost" @click="open(letter.mine ? 'sent' : 'inbox')">返回</button>
        <button v-if="!letter.mine && social.friends.some((f) => f.other.userId === letter!.senderUserId)" type="button" class="ghost" @click="write(letter.senderUserId)">回信</button>
        <button type="button" class="ghost" :disabled="delOp.loading.value" @click="delOp.submit()">{{ confirmDelete ? '確定刪除？再按一次' : '刪除' }}</button>
      </div>
    </template>

    <!-- writing -->
    <form v-else-if="view === 'write'" class="write" @submit.prevent="sendOp.submit()">
      <label>寄給
        <select v-model="to" aria-label="收信人"><option v-for="f in social.friends" :key="f.other.userId" :value="f.other.userId">{{ f.other.displayName }}{{ f.other.petName ? `（${f.other.petName}）` : '' }}</option></select>
      </label>
      <label>內容
        <textarea v-model="body" rows="4" aria-label="信的內容" placeholder="想說的話…" />
      </label>
      <div class="presets" aria-label="快速短句"><button v-for="p in PRESETS" :key="p" type="button" @click="body = (body ? body + ' ' : '') + p">{{ p }}</button></div>
      <small :class="{ over: left < 0 }">還可以寫 {{ left }} 字</small>
      <label v-if="papers.length">信紙
        <select v-model="paper" aria-label="信紙"><option value="">不用信紙</option><option v-for="p in papers" :key="p.itemId" :value="p.itemId">{{ p.name }}</option></select>
      </label>
      <label v-if="gifts.length">附上小禮物（會從背包送出）
        <select v-model="gift" aria-label="禮物"><option value="">不附禮物</option><option v-for="g in gifts" :key="g.itemId" :value="g.itemId">{{ g.name }}{{ g.quantity > 1 ? ` ×${g.quantity}` : '' }}</option></select>
      </label>
      <div class="acts">
        <button type="button" class="ghost" @click="open('inbox')">取消</button>
        <button type="submit" class="primary" :disabled="!canSend || sendOp.loading.value">{{ sendOp.loading.value ? '寄出中…' : '請郵差送信' }}</button>
      </div>
    </form>

    <!-- friends -->
    <template v-else-if="view === 'friends'">
      <p v-if="joined" class="ok" role="status">{{ joined }}</p>
      <ul v-if="social.friends.length" class="friends" aria-label="好友">
        <li v-for="f in social.friends" :key="f.relationshipId">
          <span class="who">{{ f.other.displayName }}<em>{{ f.kind === 'couple' ? '💕 情侶' : '好友' }}</em><small v-if="f.other.petName">小夥伴：{{ f.other.petName }}</small></span>
          <button type="button" class="ghost" @click="write(f.other.userId)">寫信</button>
          <button type="button" class="ghost" :disabled="endOp.loading.value" @click="endOp.submit(f.relationshipId)">{{ confirmEnd === f.relationshipId ? '確定？' : '解除' }}</button>
        </li>
      </ul>
      <p v-else class="empty">還沒有好友。</p>

      <section class="box" aria-label="邀請朋友">
        <h3>邀請朋友</h3>
        <div class="kinds" role="radiogroup" aria-label="關係">
          <button type="button" role="radio" :aria-checked="kind === 'friend'" :class="{ on: kind === 'friend' }" @click="kind = 'friend'">好友</button>
          <button type="button" role="radio" :aria-checked="kind === 'couple'" :class="{ on: kind === 'couple' }" @click="kind = 'couple'">💕 情侶</button>
          <button type="button" class="ghost" :disabled="inviteOp.loading.value" @click="inviteOp.submit()">產生邀請碼</button>
        </div>
        <p v-if="errText(inviteOp)" class="notice" role="alert">{{ errText(inviteOp) }}</p>
        <div v-if="invite" class="code" role="status"><strong aria-label="邀請碼">{{ invite.code }}</strong><button type="button" class="ghost" @click="copy">{{ copied ? '已複製' : '複製' }}</button><small>24 小時內有效，只能用一次。</small></div>
      </section>

      <section class="box" aria-label="輸入邀請碼">
        <h3>輸入朋友給你的邀請碼</h3>
        <form class="enter" @submit.prevent="previewOp.submit()">
          <input v-model="code" aria-label="邀請碼" placeholder="ABCD-EFGH" autocomplete="off" autocapitalize="characters" maxlength="12" />
          <button type="submit" class="ghost" :disabled="!code.trim() || previewOp.loading.value">查看</button>
        </form>
        <p v-if="errText(previewOp) || errText(acceptOp)" class="notice" role="alert">{{ errText(previewOp) || errText(acceptOp) }}</p>
        <div v-if="found" class="found" role="status">
          <span>{{ found.inviterName }}{{ found.inviterPetName ? `（${found.inviterPetName}）` : '' }} 想和你成為{{ found.kind === 'couple' ? '情侶' : '好友' }}</span>
          <button type="button" class="primary" :disabled="acceptOp.loading.value" @click="acceptOp.submit()">確認配對</button>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.mail { display: grid; gap: 0.5rem; }
.notice { margin: 0; padding: 0.5rem 0.75rem; color: var(--c-error); background: var(--c-error-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.85rem; }
.ok { margin: 0; padding: 0.5rem 0.75rem; color: var(--c-green); background: var(--c-green-soft); border: 2px solid currentColor; border-radius: 8px; font-size: 0.85rem; }
.hint, .empty { margin: 0; color: var(--c-ink-soft); font-size: 0.85rem; }
.tabs { display: flex; gap: 0.4rem; }
.tabs button, .kinds button { position: relative; min-height: 40px; padding: 0 0.8rem; font: inherit; font-size: 0.85rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.tabs button.on, .kinds button.on { background: var(--c-yellow); color: var(--c-on-yellow); font-weight: 700; }
.dot { margin-left: 0.3rem; padding: 0 0.35rem; font-family: var(--font-pixel); font-size: 0.6rem; color: #fff; background: var(--c-error-solid); border-radius: 8px; }
.primary { min-height: 44px; padding: 0 1rem; font: inherit; font-weight: 700; color: var(--c-ink); background: var(--c-yellow); color: var(--c-on-yellow); border: 3px solid var(--c-ink); border-radius: 8px; box-shadow: 2px 2px 0 var(--c-ink); cursor: pointer; }
.primary:disabled { opacity: 0.4; box-shadow: none; cursor: not-allowed; }
.ghost { min-height: 40px; padding: 0 0.9rem; font: inherit; font-size: 0.9rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-ink); border-radius: 8px; cursor: pointer; }
.ghost:disabled { opacity: 0.4; cursor: not-allowed; }
.acts { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.letters, .friends { margin: 0; padding: 0; list-style: none; display: grid; gap: 0.35rem; }
.letter { display: grid; gap: 0.1rem; width: 100%; padding: 0.45rem 0.7rem; text-align: left; font: inherit; color: var(--c-ink); background: var(--c-cream); border: 2px solid var(--c-line); border-radius: 8px; cursor: pointer; }
.letter.new { border-color: var(--c-ink); background: var(--c-pink-soft); }
.letter .who { font-weight: 700; font-size: 0.9rem; }
.letter .pv { font-size: 0.85rem; color: var(--c-ink-soft); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.letter small { font-size: 0.7rem; color: var(--c-ink-soft); }
.paper { padding: 0.7rem 0.9rem; background: var(--c-letter); border: 3px solid var(--c-ink); border-radius: 8px; }
.paper header { display: flex; justify-content: space-between; align-items: baseline; margin: 0 0 0.4rem; }
.paper small { color: var(--c-ink-soft); font-size: 0.7rem; }
.text { margin: 0; white-space: pre-wrap; overflow-wrap: anywhere; font-size: 0.95rem; line-height: 1.6; }
.gift { display: flex; align-items: center; gap: 0.5rem; margin: 0.6rem 0 0; padding-top: 0.5rem; border-top: 2px dashed var(--c-line); font-size: 0.85rem; }
.gift img { width: 40px; height: 40px; object-fit: contain; }
.write { display: grid; gap: 0.45rem; }
.write label { display: grid; gap: 0.2rem; font-size: 0.85rem; }
.write select, .write textarea, .enter input { font: inherit; font-size: 1rem; padding: 0.4rem 0.6rem; color: var(--c-ink); background: var(--c-field); border: 2px solid var(--c-ink); border-radius: 8px; }
.write textarea { resize: none; }
.presets { display: flex; gap: 0.3rem; flex-wrap: wrap; }
.presets button { min-height: 32px; padding: 0 0.55rem; font: inherit; font-size: 0.75rem; color: var(--c-ink); background: var(--c-paper); border: 2px solid var(--c-line); border-radius: 14px; cursor: pointer; }
.write small { font-size: 0.75rem; color: var(--c-ink-soft); }
.write small.over { color: var(--c-error); }
.friends li { display: grid; grid-template-columns: 1fr auto auto; align-items: center; gap: 0.4rem; padding: 0.3rem 0.5rem 0.3rem 0.7rem; background: var(--c-cream); border: 2px solid var(--c-line); border-radius: 8px; }
.friends .who { display: grid; font-weight: 700; font-size: 0.9rem; }
.friends .who em { font-style: normal; font-weight: 400; font-size: 0.75rem; }
.friends .who small { font-weight: 400; font-size: 0.7rem; color: var(--c-ink-soft); }
.box { display: grid; gap: 0.4rem; padding: 0.55rem 0.7rem; border: 2px solid var(--c-line); border-radius: 8px; }
.box h3 { margin: 0; font-size: 0.9rem; }
.kinds { display: flex; gap: 0.4rem; flex-wrap: wrap; }
.code { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
.code strong { font-family: var(--font-pixel); font-size: 1.1rem; letter-spacing: 0.08em; padding: 0.3rem 0.6rem; background: var(--c-yellow); color: var(--c-on-yellow); border: 2px solid var(--c-ink); border-radius: 8px; }
.code small { flex-basis: 100%; font-size: 0.72rem; color: var(--c-ink-soft); }
.enter { display: flex; gap: 0.4rem; }
.enter input { flex: 1; min-width: 0; text-transform: uppercase; }
.found { display: grid; gap: 0.4rem; font-size: 0.9rem; }
</style>
