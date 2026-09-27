import { useDeferredValue, useState } from 'react'
import { graphql, useLazyLoadQuery } from 'react-relay'
import { Button } from '../../components/ui/Button'
import { controlClassName } from '../../components/ui/Input'
import { useWorkspace } from '../workspaces/workspaceContext'
import type { ClientsPageQuery } from './__generated__/ClientsPageQuery.graphql'
import { ClientList } from './ClientList'

export type ClientStatus = 'active' | 'archived'

/**
 * Lists clients with a status switch and a name search; only admins get the controls to change them.
 */
export function ClientsPage() {
  const { workspaceId, viewerRole } = useWorkspace()
  const [status, setStatus] = useState<ClientStatus>('active')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const deferredSearch = useDeferredValue(search.trim())
  const deferredStatus = useDeferredValue(status)
  const isAdmin = viewerRole === 'ADMIN'

  const data = useLazyLoadQuery<ClientsPageQuery>(
    graphql`
      query ClientsPageQuery($workspaceId: ID!, $where: ClientFilterInput) {
        workspace: node(id: $workspaceId) {
          ...ClientList_workspace @alias(as: "clientList") @arguments(where: $where)
        }
      }
    `,
    {
      workspaceId,
      where: {
        isArchived: { eq: deferredStatus === 'archived' },
        ...(deferredSearch ? { name: { contains: deferredSearch } } : {}),
      },
    },
    { fetchPolicy: 'store-and-network' },
  )

  const clientList = data.workspace?.clientList
  if (!clientList) return null

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="mr-auto text-xl font-semibold">Clients</h1>
        {isAdmin ? (
          <Button
            variant="primary"
            onClick={() => {
              setCreating(true)
            }}
          >
            New client
          </Button>
        ) : null}
      </div>
      <div className="flex flex-col gap-3 sm:flex-row">
        <input
          type="search"
          aria-label="Search clients"
          placeholder="Search by name"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value)
          }}
          className={`${controlClassName} w-full sm:w-72`}
        />
        <select
          aria-label="Status"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value === 'archived' ? 'archived' : 'active')
          }}
          className={`${controlClassName} w-full sm:w-40`}
        >
          <option value="active">Active</option>
          <option value="archived">Archived</option>
        </select>
      </div>
      <ClientList
        workspace={clientList}
        workspaceId={workspaceId}
        isAdmin={isAdmin}
        status={deferredStatus}
        isStale={deferredSearch !== search.trim() || deferredStatus !== status}
        creating={creating}
        onCreateClose={() => {
          setCreating(false)
        }}
      />
    </section>
  )
}
