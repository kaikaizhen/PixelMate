/** Error codes returned by the API (see apps/api Infrastructure/ApiError.cs). */
export type ApiErrorCode =
  | 'AUTH_REQUIRED'
  | 'EMAIL_ALREADY_REGISTERED'
  | 'EMAIL_NOT_CONFIRMED'
  | 'INVALID_TOKEN'
  | 'VALIDATION_FAILED'
  | 'RATE_LIMITED'
  | 'CSRF_REJECTED'
  | 'SERVICE_UNAVAILABLE'
  | 'INTERNAL_ERROR'
  | 'NOT_FOUND'
  | 'NETWORK'
  | (string & {})

export class ApiError extends Error {
  readonly status: number
  readonly code: ApiErrorCode
  readonly requestId: string
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, code: ApiErrorCode, message: string, requestId = '', fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.requestId = requestId
    this.fieldErrors = fieldErrors
  }
}

const BASE = '/api/v1'

/**
 * Thin fetch wrapper: same-origin cookies, JSON in/out, the CSRF header on every unsafe call,
 * and every failure normalised into an ApiError (so views never parse responses themselves).
 */
export async function request<T = void>(method: string, path: string, body?: unknown): Promise<T> {
  let res: Response
  try {
    res = await fetch(BASE + path, {
      method,
      credentials: 'same-origin',
      headers: {
        Accept: 'application/json',
        ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
        ...(method !== 'GET' ? { 'X-Requested-With': 'pixelmate' } : {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ApiError(0, 'NETWORK', '連不上伺服器，請檢查網路後再試一次。')
  }

  if (res.ok) {
    const text = await res.text()
    return (text ? JSON.parse(text) : undefined) as T
  }

  let payload: { code?: string; message?: string; requestId?: string; errors?: Record<string, string[]> } = {}
  try {
    payload = await res.json()
  } catch {
    /* non-JSON error (proxy page, 502 ...) */
  }
  throw new ApiError(
    res.status,
    payload.code ?? (res.status >= 500 ? 'INTERNAL_ERROR' : 'UNKNOWN'),
    payload.message ?? (res.status >= 500 ? '伺服器暫時有問題，請稍後再試。' : '請求失敗，請稍後再試。'),
    payload.requestId,
    payload.errors,
  )
}
