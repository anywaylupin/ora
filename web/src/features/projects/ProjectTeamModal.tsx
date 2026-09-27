import { Suspense } from 'react'
import { graphql, useLazyLoadQuery, useMutation } from 'react-relay'
import { PageLoading } from '../../components/PageStatus'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Modal } from '../../components/ui/Modal'
import type { ProjectTeamModalAssignMutation } from './__generated__/ProjectTeamModalAssignMutation.graphql'
import type { ProjectTeamModalQuery } from './__generated__/ProjectTeamModalQuery.graphql'
import type { ProjectTeamModalUnassignMutation } from './__generated__/ProjectTeamModalUnassignMutation.graphql'

type ProjectTeamModalProps = {
  workspaceId: string
  projectId: string
  onClose: () => void
}

/**
 * Assigned members get a row for this project in their timesheet.
 */
export function ProjectTeamModal(props: ProjectTeamModalProps) {
  return (
    <Modal open title="Project team" onClose={props.onClose}>
      <Suspense fallback={<PageLoading />}>
        <ProjectTeam {...props} />
      </Suspense>
    </Modal>
  )
}

const roleLabels = { ADMIN: 'Admin', MANAGER: 'Manager', MEMBER: 'Member' } as const

/**
 * Each checkbox saves immediately with an optimistic update, so the list feels instant.
 */
function ProjectTeam({ workspaceId, projectId, onClose }: ProjectTeamModalProps) {
  const data = useLazyLoadQuery<ProjectTeamModalQuery>(
    graphql`
      query ProjectTeamModalQuery($workspaceId: ID!, $projectId: ID!) {
        workspace: node(id: $workspaceId) {
          ... on Workspace {
            members(first: 100) {
              nodes {
                id
                role
                user {
                  id
                  email
                }
              }
            }
          }
        }
        project: node(id: $projectId) {
          ... on Project {
            id
            name
            assignees {
              id
              email
            }
          }
        }
      }
    `,
    { workspaceId, projectId },
    { fetchPolicy: 'store-and-network' },
  )
  const [assign] = useMutation<ProjectTeamModalAssignMutation>(graphql`
    mutation ProjectTeamModalAssignMutation($projectId: ID!, $userId: ID!) {
      assignProjectMember(input: { projectId: $projectId, userId: $userId }) {
        project {
          id
          assignees {
            id
            email
          }
        }
      }
    }
  `)
  const [unassign] = useMutation<ProjectTeamModalUnassignMutation>(graphql`
    mutation ProjectTeamModalUnassignMutation($projectId: ID!, $userId: ID!) {
      unassignProjectMember(input: { projectId: $projectId, userId: $userId }) {
        project {
          id
          assignees {
            id
            email
          }
        }
      }
    }
  `)

  const assignees = data.project?.assignees ?? []
  const members = data.workspace?.members?.nodes ?? []

  function toggle(user: { id: string; email: string }, checked: boolean) {
    const next = checked
      ? [...assignees, user].toSorted((a, b) => a.email.localeCompare(b.email))
      : assignees.filter((assignee) => assignee.id !== user.id)
    const variables = { projectId, userId: user.id }

    if (checked) {
      assign({
        variables,
        optimisticResponse: {
          assignProjectMember: { project: { id: projectId, assignees: next } },
        },
      })
    } else {
      unassign({
        variables,
        optimisticResponse: {
          unassignProjectMember: { project: { id: projectId, assignees: next } },
        },
      })
    }
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-zinc-500">
        Choose who logs time on {data.project?.name ?? 'this project'}.
      </p>
      <ul className="max-h-80 divide-y divide-zinc-200 overflow-y-auto dark:divide-zinc-800">
        {members.map((member) => (
          <li key={member.id}>
            <label className="flex cursor-pointer items-center gap-3 py-2 text-sm">
              <input
                type="checkbox"
                className="size-4 accent-indigo-600"
                checked={assignees.some((assignee) => assignee.id === member.user.id)}
                onChange={(event) => {
                  toggle(member.user, event.target.checked)
                }}
              />
              <span className="min-w-0 flex-1 truncate">{member.user.email}</span>
              <Badge tone={member.role === 'ADMIN' ? 'info' : 'neutral'}>
                {roleLabels[member.role]}
              </Badge>
            </label>
          </li>
        ))}
      </ul>
      <div className="flex justify-end">
        <Button onClick={onClose}>Done</Button>
      </div>
    </div>
  )
}
