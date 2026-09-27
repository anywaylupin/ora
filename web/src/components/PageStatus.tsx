import type { ReactNode } from 'react'

/**
 * The Suspense fallback for pages, announced politely to screen readers.
 */
export function PageLoading() {
  return (
    <div role="status" className="flex justify-center py-16 text-sm text-zinc-500">
      Loading
    </div>
  )
}

/**
 * A centered message for empty states and pages that cannot be shown.
 */
export function PageMessage({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="mx-auto max-w-md space-y-3 py-16 text-center">
      <h1 className="text-lg font-semibold">{title}</h1>
      {children ? <div className="text-sm text-zinc-500">{children}</div> : null}
    </div>
  )
}
