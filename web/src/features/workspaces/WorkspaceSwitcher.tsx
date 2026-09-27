import { graphql, useFragment } from 'react-relay'
import { useNavigate } from 'react-router'
import { controlClassName } from '../../components/ui/Input'
import { workspacePath } from '../../lib/paths'
import type { WorkspaceSwitcher_viewer$key } from './__generated__/WorkspaceSwitcher_viewer.graphql'

const newWorkspace = 'new'

/**
 * A native select keeps switching workspaces easy on a phone.
 */
export function WorkspaceSwitcher({
  viewer,
  currentId,
}: {
  viewer: WorkspaceSwitcher_viewer$key
  currentId: string
}) {
  const navigate = useNavigate()
  const data = useFragment(
    graphql`
      fragment WorkspaceSwitcher_viewer on Viewer {
        workspaces(first: 50) {
          nodes {
            id
            name
          }
        }
      }
    `,
    viewer,
  )

  return (
    <select
      aria-label="Workspace"
      value={currentId}
      className={`${controlClassName} w-full min-w-0 py-1.5 sm:w-56`}
      onChange={(event) => {
        const value = event.target.value
        void navigate(value === newWorkspace ? '/workspaces/new' : workspacePath(value))
      }}
    >
      {data.workspaces?.nodes?.map((workspace) => (
        <option key={workspace.id} value={workspace.id}>
          {workspace.name}
        </option>
      ))}
      <option value={newWorkspace}>New workspace</option>
    </select>
  )
}
