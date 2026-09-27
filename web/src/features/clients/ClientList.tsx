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
import type { ClientList_workspace$key } from './__generated__/ClientList_workspace.graphql'
import type { ClientListArchiveMutation } from './__generated__/ClientListArchiveMutation.graphql'
import type { ClientListPaginationQuery } from './__generated__/ClientListPaginationQuery.graphql'
import type { ClientListRestoreMutation } from './__generated__/ClientListRestoreMutation.graphql'
import { ClientFormModal } from './ClientFormModal'
import type { ClientStatus } from './ClientsPage'

type ClientListProps = {
  workspace: ClientList_workspace$key
  workspaceId: string
  isAdmin: boolean
  status: ClientStatus
  isStale: boolean
  creating: boolean
  onCreateClose: () => void
}

/**
 * Pages through clients with a cursor, and removes a row in place when it moves between active and archived.
 */
export function ClientList({
  workspace,
  workspaceId,
  isAdmin,
  status,
  isStale,
  creating,
  onCreateClose,
}: ClientListProps) {
  const [editing, setEditing] = useState<{ id: string; name: string } | null>(null)
  const { data, hasNext, loadNext, isLoadingNext } = usePaginationFragment<
    ClientListPaginationQuery,
    ClientList_workspace$key
  >(
    graphql`
      fragment ClientList_workspace on Workspace
      @refetchable(queryName: "ClientListPaginationQuery")
      @argumentDefinitions(
        count: { type: "Int", defaultValue: 25 }
        cursor: { type: "String" }
        where: { type: "ClientFilterInput" }
      ) {
        clients(first: $count, after: $cursor, where: $where)
          @connection(key: "ClientList_clients", filters: ["where"]) {
          __id
          edges {
            node {
              id
              name
              isArchived
              projects {
                totalCount
              }
            }
          }
        }
      }
    `,
    workspace,
  )
  const connectionId = data.clients?.__id
  const connections = connectionId ? [connectionId] : []

  const [archive] = useMutation<ClientListArchiveMutation>(graphql`
    mutation ClientListArchiveMutation($id: ID!, $connections: [ID!]!) {
      archiveClient(input: { id: $id }) {
        client {
          removedId: id @deleteEdge(connections: $connections)
          id
          isArchived
        }
      }
    }
  `)
  const [restore] = useMutation<ClientListRestoreMutation>(graphql`
    mutation ClientListRestoreMutation($id: ID!, $connections: [ID!]!) {
      restoreClient(input: { id: $id }) {
        client {
          removedId: id @deleteEdge(connections: $connections)
          id
          isArchived
        }
      }
    }
  `)

  const edges = data.clients?.edges ?? []

  return (
    <div className={`space-y-3 ${isStale ? 'opacity-60' : ''}`}>
      {edges.length === 0 ? (
        <p className="rounded-lg border border-dashed border-zinc-300 p-8 text-center text-sm text-zinc-500 dark:border-zinc-700">
          {status === 'active' ? 'No active clients match.' : 'No archived clients match.'}
        </p>
      ) : (
        <Table>
          <TableHead>
            <TableRow>
              <TableHeader>Name</TableHeader>
              <TableHeader className="text-right">Projects</TableHeader>
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
                <TableCell className="font-medium">{node.name}</TableCell>
                <TableCell className="text-right tabular-nums">
                  {node.projects?.totalCount ?? 0}
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
                        setEditing({ id: node.id, name: node.name })
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
        <ClientFormModal
          workspaceId={workspaceId}
          connectionIds={status === 'active' ? connections : []}
          onClose={onCreateClose}
        />
      ) : null}
      {editing ? (
        <ClientFormModal
          workspaceId={workspaceId}
          client={editing}
          connectionIds={[]}
          onClose={() => {
            setEditing(null)
          }}
        />
      ) : null}
    </div>
  )
}
