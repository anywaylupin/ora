import { graphql, useLazyLoadQuery } from 'react-relay'
import { Navigate } from 'react-router'
import { lastWorkspace, workspacePath } from '../../lib/paths'
import type { HomeRedirectQuery } from './__generated__/HomeRedirectQuery.graphql'

/**
 * Opens the last used workspace, or the first one, or asks for a new one when there are none.
 */
export function HomeRedirect() {
  const data = useLazyLoadQuery<HomeRedirectQuery>(
    graphql`
      query HomeRedirectQuery {
        viewer {
          workspaces(first: 50) {
            nodes {
              id
            }
          }
        }
      }
    `,
    {},
    { fetchPolicy: 'network-only' },
  )
  const ids = data.viewer.workspaces?.nodes?.map((node) => node.id) ?? []
  const remembered = lastWorkspace()
  const target = remembered && ids.includes(remembered) ? remembered : ids.at(0)

  return <Navigate to={target ? workspacePath(target) : '/workspaces/new'} replace />
}
