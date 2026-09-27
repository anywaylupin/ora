import { useOutletContext } from 'react-router'
import type { WorkspaceLayoutQuery$data } from './__generated__/WorkspaceLayoutQuery.graphql'

/**
 * What every page inside a workspace needs to know about it.
 */
export type WorkspaceContext = {
  workspaceId: string
  viewerRole: Extract<
    WorkspaceLayoutQuery$data['workspace'],
    { __typename: 'Workspace' }
  >['viewerRole']
}

export function useWorkspace(): WorkspaceContext {
  return useOutletContext<WorkspaceContext>()
}
