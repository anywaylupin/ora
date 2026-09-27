import { Navigate, Outlet, useLocation } from 'react-router'
import { wasSignedOutByUser } from './session'
import { useSignedIn } from './useSignedIn'

/**
 * Sends signed-out visitors to sign in, remembering the page they wanted unless they signed out on purpose.
 */
export function RequireAuth() {
  const signedIn = useSignedIn()
  const location = useLocation()

  if (!signedIn) {
    const next = encodeURIComponent(`${location.pathname}${location.search}`)
    return <Navigate to={wasSignedOutByUser() ? '/sign-in' : `/sign-in?next=${next}`} replace />
  }

  return <Outlet />
}
