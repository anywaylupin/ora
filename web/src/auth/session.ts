import { authUrl } from '../lib/config'

/**
 * The token pair from the Identity login and refresh endpoints, with the expiry turned into a timestamp.
 */
type Session = {
  accessToken: string
  refreshToken: string
  expiresAt: number
}

/**
 * The body the Identity login and refresh endpoints return.
 */
export type TokenResponse = {
  accessToken: string
  refreshToken: string
  expiresIn: number
}

const storageKey = 'ora.session'

/**
 * Refresh a minute early so a request never races the token's expiry.
 */
const refreshMarginMs = 60_000

const listeners = new Set<() => void>()
let current = read()
let refreshing: Promise<Session | null> | null = null

/**
 * True after the user signs out on purpose, so the next person on a shared device does not land on their last page.
 */
let signedOutByUser = false

/**
 * Checks untrusted JSON from the network or from storage before it becomes a session.
 */
function hasStringFields<K extends string>(
  value: unknown,
  ...keys: K[]
): value is Record<K, string> {
  return (
    typeof value === 'object' &&
    value !== null &&
    keys.every((key) => typeof Reflect.get(value, key) === 'string')
  )
}

export function isTokenResponse(value: unknown): value is TokenResponse {
  return (
    hasStringFields(value, 'accessToken', 'refreshToken') &&
    typeof Reflect.get(value, 'expiresIn') === 'number'
  )
}

function isSession(value: unknown): value is Session {
  return (
    hasStringFields(value, 'accessToken', 'refreshToken') &&
    typeof Reflect.get(value, 'expiresAt') === 'number'
  )
}

/**
 * Bearer tokens live in localStorage because the web app and API are on different domains, which rules out API cookies.
 *
 * The trade-off is exposure to XSS, which the ADR on bearer tokens discusses.
 */
function read(): Session | null {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    return isSession(parsed) ? parsed : null
  } catch {
    return null
  }
}

function write(session: Session | null) {
  current = session
  persist(session)
  notify()
}

/**
 * Private browsing can block storage, in which case the session lasts for this tab only.
 */
function persist(session: Session | null): boolean {
  try {
    if (session) localStorage.setItem(storageKey, JSON.stringify(session))
    else localStorage.removeItem(storageKey)
    return true
  } catch {
    return false
  }
}

function notify() {
  for (const listener of listeners) listener()
}

function toSession(response: TokenResponse): Session {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    expiresAt: Date.now() + response.expiresIn * 1000,
  }
}

/**
 * Keeps tabs in step, so signing out in one tab signs out the others.
 */
function onStorage(event: StorageEvent) {
  if (event.key !== storageKey) return
  current = read()
  notify()
}

window.addEventListener('storage', onStorage)

/**
 * Lets React re-render when the user signs in or out.
 */
export function subscribe(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function isSignedIn(): boolean {
  return current !== null
}

/**
 * Stores the tokens returned by a successful sign in.
 */
export function start(response: TokenResponse) {
  signedOutByUser = false
  write(toSession(response))
}

export function signOut() {
  signedOutByUser = true
  write(null)
}

/**
 * Ends a session the server no longer accepts, keeping the user's place for when they sign in again.
 */
export function expire() {
  signedOutByUser = false
  write(null)
}

export function wasSignedOutByUser(): boolean {
  return signedOutByUser
}

/**
 * Exchanges the refresh token for a new pair, sharing one request between concurrent callers.
 *
 * A rejected refresh ends the session, since the refresh token is no longer usable.
 */
export function refresh(): Promise<Session | null> {
  refreshing ??= requestRefresh().finally(() => {
    refreshing = null
  })
  return refreshing
}

async function requestRefresh(): Promise<Session | null> {
  const session = current
  if (!session) return null

  const response = await fetch(authUrl('refresh'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken: session.refreshToken }),
  })
  const body: unknown = response.ok ? await response.json() : null
  if (!isTokenResponse(body)) {
    expire()
    return null
  }

  const next = toSession(body)
  write(next)
  return next
}

/**
 * Returns an access token that is valid for at least another minute, refreshing when needed.
 */
export async function getAccessToken(): Promise<string | null> {
  const session = current
  if (!session) return null
  if (Date.now() < session.expiresAt - refreshMarginMs) return session.accessToken
  return (await refresh())?.accessToken ?? null
}
