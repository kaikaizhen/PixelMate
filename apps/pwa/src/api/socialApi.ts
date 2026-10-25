import type { InviteCreated, InvitePreview, LetterFull, MailList, ReadResult, RelationKind, Relationship, SendLetterInput } from '@/types/social.types'
import { request } from './http'

export const socialApi = {
  relationships: async () => (await request<{ relationships: Relationship[] }>('GET', '/pairing/relationships')).relationships,
  createInvite: (kind: RelationKind) => request<InviteCreated>('POST', '/pairing/invites', { kind }),
  preview: (code: string) => request<InvitePreview>('POST', '/pairing/preview', { code }),
  accept: (code: string) => request<Relationship>('POST', '/pairing/accept', { code }),
  endRelationship: (id: string) => request('DELETE', `/pairing/relationships/${id}`),

  mail: (box: 'inbox' | 'sent' = 'inbox') => request<MailList>('GET', `/mail?box=${box}&limit=30`),
  unread: () => request<{ unread: number }>('GET', '/mail/unread'),
  letter: (id: string) => request<LetterFull>('GET', `/mail/${id}`),
  read: (id: string) => request<ReadResult>('POST', `/mail/${id}/read`),
  remove: (id: string) => request('DELETE', `/mail/${id}`),
  /** The key stays the same while the player retries one letter, so a double tap or a flaky network never sends it twice. */
  send: (input: SendLetterInput, idempotencyKey: string) => request('POST', '/mail', input, { 'Idempotency-Key': idempotencyKey }),
}
