import { Suspense, useState } from 'react'
import { RelayEnvironmentProvider } from 'react-relay'
import { createBrowserRouter, Navigate, Outlet } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { RequireAuth } from './auth/RequireAuth'
import { SignInPage } from './auth/SignInPage'
import { SignUpPage } from './auth/SignUpPage'
import { useSignedIn } from './auth/useSignedIn'
import { PageLoading } from './components/PageStatus'
import { NotFound, RouteError } from './components/RouteError'
import { ClientsPage } from './features/clients/ClientsPage'
import { ProjectsPage } from './features/projects/ProjectsPage'
import { TimesheetPage } from './features/timesheet/TimesheetPage'
import { HomeRedirect } from './features/workspaces/HomeRedirect'
import { NewWorkspacePage } from './features/workspaces/NewWorkspacePage'
import { WorkspaceLayout } from './features/workspaces/WorkspaceLayout'
import { createEnvironment } from './relay/environment'

const router = createBrowserRouter([
  {
    element: (
      <Suspense fallback={<PageLoading />}>
        <Outlet />
      </Suspense>
    ),
    errorElement: <RouteError />,
    children: [
      { path: 'sign-in', element: <SignInPage /> },
      { path: 'sign-up', element: <SignUpPage /> },
      {
        element: <RequireAuth />,
        children: [
          { index: true, element: <HomeRedirect /> },
          { path: 'workspaces/new', element: <NewWorkspacePage /> },
          {
            path: 'w/:workspaceId',
            element: <WorkspaceLayout />,
            errorElement: <RouteError />,
            children: [
              { index: true, element: <Navigate to="timesheet" replace /> },
              { path: 'timesheet', element: <TimesheetPage /> },
              { path: 'projects', element: <ProjectsPage /> },
              { path: 'clients', element: <ClientsPage /> },
            ],
          },
        ],
      },
      { path: '*', element: <NotFound /> },
    ],
  },
])

/**
 * Starts a new Relay environment whenever the user signs in or out, so cached data never crosses sessions.
 */
export function App() {
  const signedIn = useSignedIn()
  const [session, setSession] = useState(() => ({ signedIn, environment: createEnvironment() }))
  if (session.signedIn !== signedIn) setSession({ signedIn, environment: createEnvironment() })

  return (
    <RelayEnvironmentProvider environment={session.environment}>
      <RouterProvider router={router} />
    </RelayEnvironmentProvider>
  )
}
