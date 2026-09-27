import { Suspense, useEffect } from 'react'
import { graphql, useLazyLoadQuery } from 'react-relay'
import { NavLink, Outlet, useParams } from 'react-router'
import { signOut } from '../../auth/session'
import { PageLoading, PageMessage } from '../../components/PageStatus'
import { Button } from '../../components/ui/Button'
import { rememberWorkspace, workspacePath, type WorkspacePage } from '../../lib/paths'
import type { WorkspaceLayoutQuery } from './__generated__/WorkspaceLayoutQuery.graphql'
import type { WorkspaceContext } from './workspaceContext'
import { WorkspaceSwitcher } from './WorkspaceSwitcher'

const navigation = [
  { page: 'timesheet', label: 'Timesheet' },
  { page: 'projects', label: 'Projects' },
  { page: 'clients', label: 'Clients' },
] satisfies { page: WorkspacePage; label: string }[]

/**
 * The frame around every workspace page: switcher, navigation, and sign out.
 *
 * A workspace the viewer cannot see resolves to null, and shows the same message as one that does not exist.
 */
export function WorkspaceLayout() {
  const { workspaceId = '' } = useParams()
  const data = useLazyLoadQuery<WorkspaceLayoutQuery>(
    graphql`
      query WorkspaceLayoutQuery($workspaceId: ID!) {
        viewer {
          user {
            email
          }
          ...WorkspaceSwitcher_viewer
        }
        workspace: node(id: $workspaceId) {
          __typename
          ... on Workspace {
            id
            name
            viewerRole
          }
        }
      }
    `,
    { workspaceId },
    { fetchPolicy: 'store-and-network' },
  )
  const workspace = data.workspace?.__typename === 'Workspace' ? data.workspace : null

  useEffect(() => {
    if (workspace) rememberWorkspace(workspace.id)
  }, [workspace])

  return (
    <div className="min-h-dvh">
      <header className="border-b border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900">
        <div className="mx-auto max-w-6xl space-y-3 px-4 py-3">
          <div className="flex items-center gap-3">
            <span className="text-lg font-semibold tracking-tight text-indigo-600 dark:text-indigo-400">
              Ora
            </span>
            <div className="min-w-0 flex-1 sm:flex-none">
              <WorkspaceSwitcher viewer={data.viewer} currentId={workspace?.id ?? ''} />
            </div>
            <div className="ml-auto flex shrink-0 items-center gap-3">
              <span className="hidden text-sm text-zinc-500 md:inline">
                {data.viewer.user.email}
              </span>
              <Button size="sm" variant="ghost" onClick={signOut}>
                Sign out
              </Button>
            </div>
          </div>
          {workspace ? (
            <nav aria-label="Workspace" className="-mx-1 flex gap-1 overflow-x-auto">
              {navigation.map(({ page, label }) => (
                <NavLink
                  key={page}
                  to={workspacePath(workspace.id, page)}
                  className={({ isActive }) =>
                    `rounded-md px-3 py-1.5 text-sm font-medium whitespace-nowrap ${
                      isActive
                        ? 'bg-indigo-50 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300'
                        : 'text-zinc-600 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800'
                    }`
                  }
                >
                  {label}
                </NavLink>
              ))}
            </nav>
          ) : null}
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">
        {workspace ? (
          <Suspense fallback={<PageLoading />}>
            <Outlet
              context={
                {
                  workspaceId: workspace.id,
                  viewerRole: workspace.viewerRole,
                } satisfies WorkspaceContext
              }
            />
          </Suspense>
        ) : (
          <PageMessage title="Workspace not found">
            It does not exist, or you are not a member. Pick another workspace above.
          </PageMessage>
        )}
      </main>
    </div>
  )
}
