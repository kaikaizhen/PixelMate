import { defineStore } from 'pinia'
import { ref } from 'vue'
import { socialApi } from '@/api/socialApi'
import type { LetterSummary, Relationship } from '@/types/social.types'

export const useSocialStore = defineStore('social', () => {
  const friends = ref<Relationship[]>([])
  const inbox = ref<LetterSummary[]>([])
  const sent = ref<LetterSummary[]>([])
  const unread = ref(0)
  let known = false   // the first count is just a starting point: only a later rise means "a letter arrived"

  async function loadFriends() {
    friends.value = await socialApi.relationships()
  }

  async function loadInbox() {
    const r = await socialApi.mail('inbox')
    inbox.value = r.letters
    unread.value = r.unread
    known = true
  }

  async function loadSent() {
    sent.value = (await socialApi.mail('sent')).letters
  }

  /** Cheap poll for the badge; true when new letters arrived since the last look. */
  async function refreshUnread(): Promise<boolean> {
    const n = (await socialApi.unread()).unread
    const arrived = known && n > unread.value
    unread.value = n
    known = true
    return arrived
  }

  function clear() {
    friends.value = []
    inbox.value = []
    sent.value = []
    unread.value = 0
    known = false
  }

  return { friends, inbox, sent, unread, loadFriends, loadInbox, loadSent, refreshUnread, clear }
})
