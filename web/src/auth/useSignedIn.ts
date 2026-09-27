import { useSyncExternalStore } from 'react'
import { isSignedIn, subscribe } from './session'

/**
 * Re-renders when the user signs in or out, in this tab or another.
 */
export function useSignedIn(): boolean {
  return useSyncExternalStore(subscribe, isSignedIn)
}
