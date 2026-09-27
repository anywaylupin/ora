import { useId, type ComponentProps, type ReactNode } from 'react'

/**
 * Shared by text inputs and selects so every form control looks the same; callers choose the width.
 */
export const controlClassName =
  'block rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 placeholder:text-zinc-400 focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/30 focus:outline-none aria-invalid:border-red-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100'

type FieldProps = {
  label: string
  error?: string | null | undefined
  hint?: string
}

function Field({
  id,
  label,
  error,
  hint,
  children,
}: FieldProps & { id: string; children: ReactNode }) {
  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-sm font-medium">
        {label}
      </label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-sm text-red-600 dark:text-red-400">
          {error}
        </p>
      ) : hint ? (
        <p className="text-sm text-zinc-500">{hint}</p>
      ) : null}
    </div>
  )
}

/**
 * A labelled text input that links its error message for screen readers.
 */
export function Input({
  label,
  error,
  hint,
  id,
  className = '',
  ...props
}: FieldProps & ComponentProps<'input'>) {
  const generatedId = useId()
  const inputId = id ?? generatedId
  return (
    <Field id={inputId} label={label} error={error} hint={hint}>
      <input
        id={inputId}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${inputId}-error` : undefined}
        className={`${controlClassName} w-full ${className}`}
        {...props}
      />
    </Field>
  )
}

/**
 * A labelled native select, which stays usable on phones without a custom picker.
 */
export function Select({
  label,
  error,
  hint,
  id,
  className = '',
  ...props
}: FieldProps & ComponentProps<'select'>) {
  const generatedId = useId()
  const selectId = id ?? generatedId
  return (
    <Field id={selectId} label={label} error={error} hint={hint}>
      <select
        id={selectId}
        aria-invalid={error ? true : undefined}
        className={`${controlClassName} w-full ${className}`}
        {...props}
      />
    </Field>
  )
}
