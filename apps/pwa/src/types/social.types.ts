export type RelationKind = 'friend' | 'couple'

export interface Friend {
  userId: string
  displayName: string
  petName: string | null
  bodyVariant: string | null
}

export interface Relationship {
  relationshipId: string
  kind: RelationKind
  since: string
  other: Friend
}

export interface InviteCreated {
  inviteId: string
  code: string
  kind: RelationKind
  expiresAt: string
}

export interface InvitePreview {
  inviterName: string
  inviterPetName: string | null
  kind: RelationKind
  expiresAt: string
}

export interface LetterSummary {
  letterId: string
  otherUserId: string
  otherDisplayName: string
  preview: string
  sentAt: string
  deliverAt: string
  delivered: boolean
  readAt: string | null
  hasGift: boolean
  paperItemId: string | null
}

export interface MailList {
  letters: LetterSummary[]
  unread: number
  nextCursor: string | null
}

export interface Gift {
  itemId: string
  name: string
  assetKey: string
  rarity: string
}

export interface LetterFull {
  letterId: string
  senderUserId: string
  senderDisplayName: string
  recipientUserId: string
  body: string
  sentAt: string
  deliverAt: string
  readAt: string | null
  paperItemId: string | null
  gift: Gift | null
  mine: boolean
}

export interface ReadResult {
  readAt: string
  gift: Gift | null
  giftGranted: boolean
}

export interface SendLetterInput {
  recipientUserId: string
  body: string
  paperItemId?: string | null
  giftItemId?: string | null
}

export const MAX_LETTER_LENGTH = 500
