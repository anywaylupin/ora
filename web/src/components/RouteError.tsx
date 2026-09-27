import { isRouteErrorResponse, Link, useRouteError } from 'react-router'
import { PageMessage } from './PageStatus'

/**
 * Catches render and network errors for a route, so one failure never blanks the whole app.
 */
export function RouteError() {
  const error = useRouteError()
  const notFound = isRouteErrorResponse(error) && error.status === 404

  return (
    <PageMessage title={notFound ? 'Page not found' : 'Something went wrong'}>
      <p>{notFound ? 'Check the address and try again.' : 'Reload the page to try again.'}</p>
      <p className="mt-4">
        <Link to="/" className="font-medium text-indigo-600 dark:text-indigo-400">
          Go to your timesheet
        </Link>
      </p>
    </PageMessage>
  )
}

/**
 * Shown for addresses that match no route.
 */
export function NotFound() {
  return (
    <PageMessage title="Page not found">
      <p>Check the address and try again.</p>
      <p className="mt-4">
        <Link to="/" className="font-medium text-indigo-600 dark:text-indigo-400">
          Go to your timesheet
        </Link>
      </p>
    </PageMessage>
  )
}
