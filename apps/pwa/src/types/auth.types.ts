export interface User {
  userId: string
  email: string
  displayName: string
  emailConfirmed: boolean
}

export interface RegisterInput {
  email: string
  password: string
  displayName: string
}

export interface RegisterResult {
  userId: string
  emailConfirmationRequired: boolean
}

export interface LoginInput {
  email: string
  password: string
}

export interface ResetPasswordInput {
  email: string
  token: string
  newPassword: string
}
