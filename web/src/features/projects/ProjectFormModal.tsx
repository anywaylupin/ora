import { Suspense, useState, type SubmitEvent } from 'react'
import { graphql, useLazyLoadQuery, useMutation } from 'react-relay'
import { Link } from 'react-router'
import { PageLoading } from '../../components/PageStatus'
import { Button } from '../../components/ui/Button'
import { Input, Select } from '../../components/ui/Input'
import { Modal } from '../../components/ui/Modal'
import { errorMessage } from '../../lib/mutationErrors'
import { workspacePath } from '../../lib/paths'
import type { ProjectFormModalClientsQuery } from './__generated__/ProjectFormModalClientsQuery.graphql'
import type { ProjectFormModalCreateMutation } from './__generated__/ProjectFormModalCreateMutation.graphql'
import type { ProjectFormModalUpdateMutation } from './__generated__/ProjectFormModalUpdateMutation.graphql'
import { projectColors } from './colors'
import { formText } from '../../lib/formData'

/**
 * The values an admin can change on an existing project.
 */
export type EditableProject = {
  id: string
  name: string
  color: string
  clientId: string
}

type ProjectFormModalProps = {
  workspaceId: string
  project?: EditableProject
  connectionIds: string[]
  onClose: () => void
}

export function ProjectFormModal(props: ProjectFormModalProps) {
  return (
    <Modal open title={props.project ? 'Edit project' : 'New project'} onClose={props.onClose}>
      <Suspense fallback={<PageLoading />}>
        <ProjectForm {...props} />
      </Suspense>
    </Modal>
  )
}

type FieldErrors = Partial<Record<'name' | 'color' | 'clientId' | 'form', string | null>>

/**
 * Loads active clients for the picker; a project on an archived client keeps that client as an option.
 */
function ProjectForm({ workspaceId, project, connectionIds, onClose }: ProjectFormModalProps) {
  const [errors, setErrors] = useState<FieldErrors>({})
  const data = useLazyLoadQuery<ProjectFormModalClientsQuery>(
    graphql`
      query ProjectFormModalClientsQuery($workspaceId: ID!) {
        workspace: node(id: $workspaceId) {
          ... on Workspace {
            clients(first: 100, where: { isArchived: { eq: false } }) {
              nodes {
                id
                name
              }
            }
          }
        }
      }
    `,
    { workspaceId },
    { fetchPolicy: 'store-and-network' },
  )
  const clients = data.workspace?.clients?.nodes ?? []

  const [create, creating] = useMutation<ProjectFormModalCreateMutation>(graphql`
    mutation ProjectFormModalCreateMutation(
      $clientId: ID!
      $name: String!
      $color: String!
      $connections: [ID!]!
    ) {
      createProject(input: { clientId: $clientId, name: $name, color: $color }) {
        project @prependNode(connections: $connections, edgeTypeName: "ProjectsEdge") {
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
  const [update, updating] = useMutation<ProjectFormModalUpdateMutation>(graphql`
    mutation ProjectFormModalUpdateMutation(
      $id: ID!
      $clientId: ID!
      $name: String!
      $color: String!
    ) {
      updateProject(input: { id: $id, clientId: $clientId, name: $name, color: $color }) {
        project {
          id
          name
          color
          client {
            id
            name
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

  if (clients.length === 0 && !project) {
    return (
      <div className="space-y-4 text-sm">
        <p>Projects belong to a client. Create a client first.</p>
        <div className="flex justify-end gap-2">
          <Button onClick={onClose}>Close</Button>
          <Link
            to={workspacePath(workspaceId, 'clients')}
            className="inline-flex h-10 items-center rounded-md bg-indigo-600 px-4 font-medium text-white hover:bg-indigo-500"
          >
            Go to clients
          </Link>
        </div>
      </div>
    )
  }

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const values = {
      clientId: formText(form, 'clientId'),
      name: formText(form, 'name'),
      color: formText(form, 'color'),
    }
    const showErrors = (payloadErrors: Parameters<typeof errorMessage>[0], saved: boolean) => {
      if (saved) {
        onClose()
        return
      }
      setErrors({
        name: errorMessage(payloadErrors, 'name'),
        color: errorMessage(payloadErrors, 'color'),
        clientId: errorMessage(payloadErrors, 'clientId'),
        form: errorMessage(payloadErrors),
      })
    }
    const onError = () => {
      setErrors({ form: 'The project was not saved. Try again.' })
    }

    if (project) {
      update({
        variables: { id: project.id, ...values },
        onCompleted: ({ updateProject }) => {
          showErrors(updateProject.errors, updateProject.project != null)
        },
        onError,
      })
    } else {
      create({
        variables: { ...values, connections: connectionIds },
        onCompleted: ({ createProject }) => {
          showErrors(createProject.errors, createProject.project != null)
        },
        onError,
      })
    }
  }

  const currentClientMissing = project && !clients.some((client) => client.id === project.clientId)

  return (
    <form className="space-y-4" onSubmit={handleSubmit}>
      {errors.form ? (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          {errors.form}
        </p>
      ) : null}
      <Input
        label="Name"
        name="name"
        defaultValue={project?.name}
        required
        maxLength={100}
        error={errors.name}
        autoFocus
      />
      <Select
        label="Client"
        name="clientId"
        defaultValue={project?.clientId}
        error={errors.clientId}
      >
        {currentClientMissing ? (
          <option value={project.clientId}>Current client (archived)</option>
        ) : null}
        {clients.map((client) => (
          <option key={client.id} value={client.id}>
            {client.name}
          </option>
        ))}
      </Select>
      <fieldset className="space-y-2">
        <legend className="text-sm font-medium">Color</legend>
        <div className="flex flex-wrap gap-2">
          {projectColors.map((color, index) => (
            <label key={color.value} className="cursor-pointer">
              <input
                type="radio"
                name="color"
                value={color.value}
                defaultChecked={project ? project.color === color.value : index === 0}
                className="peer sr-only"
              />
              <span
                title={color.label}
                className="block size-8 rounded-full ring-offset-2 ring-offset-white peer-checked:ring-2 peer-checked:ring-zinc-900 peer-focus-visible:ring-2 peer-focus-visible:ring-indigo-500 dark:ring-offset-zinc-900 dark:peer-checked:ring-zinc-100"
                style={{ backgroundColor: color.value }}
              />
              <span className="sr-only">{color.label}</span>
            </label>
          ))}
        </div>
        {errors.color ? (
          <p className="text-sm text-red-600 dark:text-red-400">{errors.color}</p>
        ) : null}
      </fieldset>
      <div className="flex justify-end gap-2">
        <Button onClick={onClose}>Cancel</Button>
        <Button type="submit" variant="primary" disabled={creating || updating}>
          {project ? 'Save' : 'Create project'}
        </Button>
      </div>
    </form>
  )
}
