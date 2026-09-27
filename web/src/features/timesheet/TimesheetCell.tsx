import { useState, type KeyboardEvent } from 'react'
import { graphql, useFragment, useMutation } from 'react-relay'
import { formatDuration, parseDuration } from '../../lib/duration'
import type { TimesheetCell_cell$key } from './__generated__/TimesheetCell_cell.graphql'
import type { TimesheetCellMutation } from './__generated__/TimesheetCellMutation.graphql'

type TimesheetCellProps = {
  cell: TimesheetCell_cell$key
  projectId: string
  label: string
  editable: boolean
  position: string
  onEnter: () => void
  onStatus: (message: string | null) => void
}

/**
 * One day of one project, saved when the input loses focus.
 *
 * The save is optimistic: the cell and every total update at once, and roll back if the server refuses.
 */
export function TimesheetCell({
  cell,
  projectId,
  label,
  editable,
  position,
  onEnter,
  onStatus,
}: TimesheetCellProps) {
  const data = useFragment(
    graphql`
      fragment TimesheetCell_cell on TimesheetCell {
        id
        date
        durationMinutes
        note
      }
    `,
    cell,
  )
  const [commit] = useMutation<TimesheetCellMutation>(graphql`
    mutation TimesheetCellMutation(
      $projectId: ID!
      $date: LocalDate!
      $durationMinutes: Int!
      $note: String
    ) {
      upsertTimeEntry(
        input: {
          projectId: $projectId
          date: $date
          durationMinutes: $durationMinutes
          note: $note
        }
      ) {
        timesheetCell {
          id
          durationMinutes
          note
        }
        errors {
          __typename
          ... on Error {
            message
          }
        }
      }
    }
  `)
  const [draft, setDraft] = useState<string | null>(null)
  const [invalid, setInvalid] = useState(false)
  const formatted = formatDuration(data.durationMinutes)

  if (!editable) {
    return (
      <span className="block px-2 py-1.5 text-center text-zinc-500 tabular-nums" aria-label={label}>
        {formatted}
      </span>
    )
  }

  function save() {
    if (draft === null) return
    const minutes = parseDuration(draft)
    if (minutes === null) {
      setInvalid(true)
      onStatus(`${label}: use a time such as 1:30, 1.5, or 90m.`)
      return
    }

    setDraft(null)
    setInvalid(false)
    onStatus(null)
    if (minutes === data.durationMinutes) return

    commit({
      variables: { projectId, date: data.date, durationMinutes: minutes, note: data.note },
      optimisticResponse: {
        upsertTimeEntry: {
          timesheetCell: { id: data.id, durationMinutes: minutes, note: data.note },
          errors: [],
        },
      },
      onCompleted: ({ upsertTimeEntry }) => {
        const message = upsertTimeEntry.errors?.at(0)?.message
        if (message) onStatus(`${label}: ${message}`)
      },
      onError: () => {
        onStatus(`${label}: not saved. Check your connection and try again.`)
      },
    })
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Enter') {
      event.preventDefault()
      event.currentTarget.blur()
      onEnter()
    }
    if (event.key === 'Escape') {
      setDraft(null)
      setInvalid(false)
      onStatus(null)
    }
  }

  return (
    <input
      type="text"
      inputMode="text"
      autoComplete="off"
      aria-label={label}
      aria-invalid={invalid || undefined}
      data-cell={position}
      value={draft ?? formatted}
      onChange={(event) => {
        setDraft(event.target.value)
      }}
      onFocus={(event) => {
        event.currentTarget.select()
      }}
      onBlur={save}
      onKeyDown={handleKeyDown}
      className="w-full rounded-md border border-transparent bg-transparent px-2 py-1.5 text-center tabular-nums hover:border-zinc-300 focus:border-indigo-500 focus:bg-white focus:ring-2 focus:ring-indigo-500/30 focus:outline-none aria-invalid:border-red-500 aria-invalid:bg-red-50 dark:hover:border-zinc-700 dark:focus:bg-zinc-950 dark:aria-invalid:bg-red-950/40"
    />
  )
}
