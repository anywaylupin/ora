import { useState, type SubmitEvent } from 'react'
import { graphql, useMutation } from 'react-relay'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Modal } from '../../components/ui/Modal'
import { errorMessage } from '../../lib/mutationErrors'
import type { ClientFormModalCreateMutation } from './__generated__/ClientFormModalCreateMutation.graphql'
import type { ClientFormModalUpdateMutation } from './__generated__/ClientFormModalUpdateMutation.graphql'
import { formText } from '../../lib/formData'

type ClientFormModalProps = {
  workspaceId: string
  client?: { id: string; name: string }
  connectionIds: string[]
  onClose: () => void
}

/**
 * Creates a client, or renames one when a client is passed in.
 *
 * New clients are prepended to the visible list, so the admin sees the result without a refetch.
 */
export function ClientFormModal({
  workspaceId,
  client,
  connectionIds,
  onClose,
}: ClientFormModalProps) {
  const [error, setError] = useState<string | null>(null)
  const [create, creating] = useMutation<ClientFormModalCreateMutation>(graphql`
    mutation ClientFormModalCreateMutation(
      $workspaceId: ID!
      $name: String!
      $connections: [ID!]!
    ) {
      createClient(input: { workspaceId: $workspaceId, name: $name }) {
        client @prependNode(connections: $connections, edgeTypeName: "ClientsEdge") {
          id
          name
          isArchived
          projects {
            totalCount
          }
        }
        errors {
          __typename
          ... on Error {
            message
          }
          ... on ValidationError {
            field
          }
        }
      }
    }
  `)
  const [update, updating] = useMutation<ClientFormModalUpdateMutation>(graphql`
    mutation ClientFormModalUpdateMutation($id: ID!, $name: String!) {
      updateClient(input: { id: $id, name: $name }) {
        client {
          id
          name
        }
        errors {
          __typename
          ... on Error {
            message
          }
          ... on ValidationError {
            field
          }
        }
      }
    }
  `)

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    const name = formText(new FormData(event.currentTarget), 'name')
    const onError = () => {
      setError('The client was not saved. Try again.')
    }

    if (client) {
      update({
        variables: { id: client.id, name },
        onCompleted: ({ updateClient }) => {
          if (updateClient.client) onClose()
          else
            setError(errorMessage(updateClient.errors, 'name') ?? errorMessage(updateClient.errors))
        },
        onError,
      })
    } else {
      create({
        variables: { workspaceId, name, connections: connectionIds },
        onCompleted: ({ createClient }) => {
          if (createClient.client) onClose()
          else
            setError(errorMessage(createClient.errors, 'name') ?? errorMessage(createClient.errors))
        },
        onError,
      })
    }
  }

  return (
    <Modal open title={client ? 'Edit client' : 'New client'} onClose={onClose}>
      <form className="space-y-4" onSubmit={handleSubmit}>
        <Input
          label="Name"
          name="name"
          defaultValue={client?.name}
          required
          maxLength={100}
          error={error}
          autoFocus
        />
        <div className="flex justify-end gap-2">
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="primary" disabled={creating || updating}>
            {client ? 'Save' : 'Create client'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
