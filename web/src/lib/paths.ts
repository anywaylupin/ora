/**
 * The pages inside a workspace.
 */
export type WorkspacePage = 'timesheet' | 'projects' | 'clients'

/**
 * Global IDs are base64 and can contain slashes, so they are always encoded in URLs.
 */
export function workspacePath(workspaceId: string, page: WorkspacePage = 'timesheet'): string {
  return `/w/${encodeURIComponent(workspaceId)}/${page}`
}

const lastWorkspaceKey = 'ora.lastWorkspace'

/**
 * Remembers the last workspace per browser, so signing in lands where the user left off.
 */
export function rememberWorkspace(workspaceId: string) {
  try {
    localStorage.setItem(lastWorkspaceKey, workspaceId)
  } catch {
    return
  }
}

export function lastWorkspace(): string | null {
  try {
    return localStorage.getItem(lastWorkspaceKey)
  } catch {
    return null
  }
}
