import { authUrl } from '../lib/config'
import { isTokenResponse, start } from './session'

/**
 * The outcome of a sign in or sign up, with user-facing messages when it fails.
 */
export type AuthResult = { ok: true } | { ok: false; messages: string[] }

/**
 * Identity returns validation problems as a map of error codes to messages; the messages are what people need.
 */
function problemMessages(body: unknown): string[] {
  const errors: unknown =
    typeof body === 'object' && body !== null ? Reflect.get(body, 'errors') : null
  if (typeof errors !== 'object' || errors === null) return []
  return Object.values(errors).flatMap((value: unknown) =>
    Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : [],
  )
}

async function post(path: 'login' | 'register', body: object): Promise<Response> {
  return fetch(authUrl(path), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export async function signIn(email: string, password: string): Promise<AuthResult> {
  const response = await post('login', { email, password })
  const body: unknown = response.ok ? await response.json() : null
  if (!isTokenResponse(body)) {
    return { ok: false, messages: ['The email or password is incorrect.'] }
  }

  start(body)
  return { ok: true }
}

/**
 * Registers the account and signs in straight away, since Ora v0.1 does not confirm emails.
 */
export async function signUp(email: string, password: string): Promise<AuthResult> {
  const response = await post('register', { email, password })
  if (!response.ok) {
    const messages = problemMessages(await response.json().catch(() => null))
    return { ok: false, messages: messages.length > 0 ? messages : ['Sign up failed. Try again.'] }
  }

  return signIn(email, password)
}
