import type { LoginInput, RegisterInput, RegisterResult, ResetPasswordInput, User } from '@/types/auth.types'
import { request } from './http'

export const authApi = {
  register: (input: RegisterInput) => request<RegisterResult>('POST', '/auth/register', input),
  login: (input: LoginInput) => request<User>('POST', '/auth/login', input),
  logout: () => request('POST', '/auth/logout'),
  me: () => request<User>('GET', '/auth/me'),
  forgotPassword: (email: string) => request('POST', '/auth/forgot-password', { email }),
  resetPassword: (input: ResetPasswordInput) => request('POST', '/auth/reset-password', input),
  confirmEmail: (userId: string, token: string) => request('POST', '/auth/confirm-email', { userId, token }),
  resendConfirmation: (email: string) => request('POST', '/auth/resend-confirmation', { email }),
}
