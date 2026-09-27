import type { ReactNode } from 'react'

/**
 * The centered card shared by the sign in and sign up pages.
 */
export function AuthLayout({ title, children }: { title: string; children: ReactNode }) {
  return (
    <main className="flex min-h-dvh items-center justify-center px-4 py-12">
      <div className="w-full max-w-sm space-y-6">
        <div className="space-y-1 text-center">
          <p className="text-2xl font-semibold tracking-tight text-indigo-600 dark:text-indigo-400">
            Ora
          </p>
          <h1 className="text-lg font-medium">{title}</h1>
        </div>
        <div className="rounded-lg border border-zinc-200 bg-white p-6 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
          {children}
        </div>
      </div>
    </main>
  )
}

/**
 * Lists every message from a failed auth request.
 */
export function FormErrors({ messages }: { messages: string[] }) {
  if (messages.length === 0) return null
  return (
    <ul
      role="alert"
      className="space-y-1 rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-950/50 dark:text-red-300"
    >
      {messages.map((message) => (
        <li key={message}>{message}</li>
      ))}
    </ul>
  )
}
