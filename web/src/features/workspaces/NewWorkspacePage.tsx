import { useState, type SubmitEvent } from 'react'
import { graphql, useMutation } from 'react-relay'
import { Link, useNavigate } from 'react-router'
import { AuthLayout } from '../../auth/AuthLayout'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { errorMessage } from '../../lib/mutationErrors'
import { workspacePath } from '../../lib/paths'
import type { NewWorkspacePageMutation } from './__generated__/NewWorkspacePageMutation.graphql'
import { formText } from '../../lib/formData'

/**
 * The creator becomes the workspace admin, so this is also where a new consultancy starts.
 */
export function NewWorkspacePage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const [commit, pending] = useMutation<NewWorkspacePageMutation>(graphql`
    mutation NewWorkspacePageMutation($name: String!) {
      createWorkspace(input: { name: $name }) {
        workspace {
          id
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
    commit({
      variables: { name },
      onCompleted: ({ createWorkspace }) => {
        const workspace = createWorkspace.workspace
        if (workspace) void navigate(workspacePath(workspace.id))
        else
          setError(errorMessage(createWorkspace.errors, 'name') ?? 'The workspace was not created.')
      },
      onError: () => {
        setError('The workspace was not created. Try again.')
      },
    })
  }

  return (
    <AuthLayout title="Create a workspace">
      <form className="space-y-4" onSubmit={handleSubmit}>
        <p className="text-sm text-zinc-500">
          A workspace holds your clients, projects, and timesheets. You will be its admin.
        </p>
        <Input
          label="Workspace name"
          name="name"
          required
          maxLength={100}
          error={error}
          autoFocus
        />
        <Button type="submit" variant="primary" className="w-full" disabled={pending}>
          {pending ? 'Creating workspace' : 'Create workspace'}
        </Button>
        <p className="text-center text-sm">
          <Link to="/" className="font-medium text-indigo-600 dark:text-indigo-400">
            Back to my workspaces
          </Link>
        </p>
      </form>
    </AuthLayout>
  )
}
