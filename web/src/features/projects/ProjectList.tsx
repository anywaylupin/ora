import { useState } from 'react'
import { graphql, useMutation, usePaginationFragment } from 'react-relay'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/Table'
import type { ProjectList_workspace$key } from './__generated__/ProjectList_workspace.graphql'
import type { ProjectListArchiveMutation } from './__generated__/ProjectListArchiveMutation.graphql'
import type { ProjectListPaginationQuery } from './__generated__/ProjectListPaginationQuery.graphql'
import type { ProjectListRestoreMutation } from './__generated__/ProjectListRestoreMutation.graphql'
import { ProjectFormModal, type EditableProject } from './ProjectFormModal'
import type { ProjectStatus } from './ProjectsPage'
import { ProjectTeamModal } from './ProjectTeamModal'

type ProjectListProps = {
  workspace: ProjectList_workspace$key
  workspaceId: string
  isAdmin: boolean
  status: ProjectStatus
  isStale: boolean
  creating: boolean
  onCreateClose: () => void
}

/**
 * Pages through projects with a cursor and opens the edit and team dialogs for admins.
 */
export function ProjectList({
  workspace,
  workspaceId,
  isAdmin,
  status,
  isStale,
  creating,
  onCreateClose,
}: ProjectListProps) {
  const [editing, setEditing] = useState<EditableProject | null>(null)
  const [staffing, setStaffing] = useState<string | null>(null)
  const { data, hasNext, loadNext, isLoadingNext } = usePaginationFragment<
    ProjectListPaginationQuery,
    ProjectList_workspace$key
  >(
    graphql`
      fragment ProjectList_workspace on Workspace
      @refetchable(queryName: "ProjectListPaginationQuery")
      @argumentDefinitions(
        count: { type: "Int", defaultValue: 25 }
        cursor: { type: "String" }
        where: { type: "ProjectFilterInput" }
      ) {
        projects(first: $count, after: $cursor, where: $where)
          @connection(key: "ProjectList_projects", filters: ["where"]) {
          __id
          edges {
            node {
              id
              name
              color
              isArchived
              client {
                id
                name
              }
              assignees {
                id
                email
              }
            }
          }
        }
      }
    `,
    workspace,
  )
  const connectionId = data.projects?.__id
  const connections = connectionId ? [connectionId] : []

  const [archive] = useMutation<ProjectListArchiveMutation>(graphql`
    mutation ProjectListArchiveMutation($id: ID!, $connections: [ID!]!) {
      archiveProject(input: { id: $id }) {
        project {
          removedId: id @deleteEdge(connections: $connections)
          id
          isArchived
        }
      }
    }
  `)
  const [restore] = useMutation<ProjectListRestoreMutation>(graphql`
    mutation ProjectListRestoreMutation($id: ID!, $connections: [ID!]!) {
      restoreProject(input: { id: $id }) {
        project {
          removedId: id @deleteEdge(connections: $connections)
          id
          isArchived
        }
      }
    }
  `)

  const edges = data.projects?.edges ?? []

  return (
    <div className={`space-y-3 ${isStale ? 'opacity-60' : ''}`}>
      {edges.length === 0 ? (
        <p className="rounded-lg border border-dashed border-zinc-300 p-8 text-center text-sm text-zinc-500 dark:border-zinc-700">
          {status === 'active' ? 'No active projects match.' : 'No archived projects match.'}
        </p>
      ) : (
        <Table>
          <TableHead>
            <TableRow>
              <TableHeader>Project</TableHeader>
              <TableHeader>Client</TableHeader>
              <TableHeader>Team</TableHeader>
              <TableHeader>Status</TableHeader>
              {isAdmin ? (
                <TableHeader>
                  <span className="sr-only">Actions</span>
                </TableHeader>
              ) : null}
            </TableRow>
          </TableHead>
          <TableBody>
            {edges.map(({ node }) => (
              <TableRow key={node.id}>
                <TableCell className="font-medium">
                  <span className="flex items-center gap-2">
                    <span
                      aria-hidden
                      className="size-3 shrink-0 rounded-full"
                      style={{ backgroundColor: node.color }}
                    />
                    {node.name}
                  </span>
                </TableCell>
                <TableCell className="text-zinc-600 dark:text-zinc-400">
                  {node.client.name}
                </TableCell>
                <TableCell>
                  <span title={node.assignees.map((user) => user.email).join(', ')}>
                    {node.assignees.length === 1 ? '1 person' : `${node.assignees.length} people`}
                  </span>
                </TableCell>
                <TableCell>
                  {node.isArchived ? <Badge>Archived</Badge> : <Badge tone="success">Active</Badge>}
                </TableCell>
                {isAdmin ? (
                  <TableCell className="text-right whitespace-nowrap">
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        setStaffing(node.id)
                      }}
                    >
                      Team
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        setEditing({
                          id: node.id,
                          name: node.name,
                          color: node.color,
                          clientId: node.client.id,
                        })
                      }}
                    >
                      Edit
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => {
                        const config = { variables: { id: node.id, connections } }
                        if (node.isArchived) restore(config)
                        else archive(config)
                      }}
                    >
                      {node.isArchived ? 'Restore' : 'Archive'}
                    </Button>
                  </TableCell>
                ) : null}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
      {hasNext ? (
        <Button
          disabled={isLoadingNext}
          onClick={() => {
            loadNext(25)
          }}
        >
          {isLoadingNext ? 'Loading' : 'Load more'}
        </Button>
      ) : null}
      {creating ? (
        <ProjectFormModal
          workspaceId={workspaceId}
          connectionIds={status === 'active' ? connections : []}
          onClose={onCreateClose}
        />
      ) : null}
      {editing ? (
        <ProjectFormModal
          workspaceId={workspaceId}
          project={editing}
          connectionIds={[]}
          onClose={() => {
            setEditing(null)
          }}
        />
      ) : null}
      {staffing ? (
        <ProjectTeamModal
          workspaceId={workspaceId}
          projectId={staffing}
          onClose={() => {
            setStaffing(null)
          }}
        />
      ) : null}
    </div>
  )
}
