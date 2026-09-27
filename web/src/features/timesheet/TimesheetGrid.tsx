import { useRef, useState } from 'react'
import { graphql, useFragment } from 'react-relay'
import { Link } from 'react-router'
import { Table, TableBody, TableHead, TableHeader, TableRow } from '../../components/ui/Table'
import { formatDuration } from '../../lib/duration'
import { workspacePath } from '../../lib/paths'
import { formatDay, parseIsoDate, today } from '../../lib/week'
import type { TimesheetGrid_timesheet$key } from './__generated__/TimesheetGrid_timesheet.graphql'
import { TimesheetCell } from './TimesheetCell'

type TimesheetGridProps = {
  timesheet: TimesheetGrid_timesheet$key
  workspaceId: string
  isAdmin: boolean
}

const stickyColumn =
  'sticky left-0 z-10 shadow-[1px_0_0_0_var(--color-zinc-200)] dark:shadow-[1px_0_0_0_var(--color-zinc-800)]'
const stickyBody = `${stickyColumn} bg-white dark:bg-zinc-900`
const stickyShaded = `${stickyColumn} bg-zinc-50 dark:bg-zinc-900`

/**
 * A row per project and a column per day, with totals for each row, each day, and the week.
 *
 * Totals read the same cell records the inputs update optimistically, so they change as soon as a cell is left.
 */
export function TimesheetGrid({ timesheet, workspaceId, isAdmin }: TimesheetGridProps) {
  const grid = useRef<HTMLDivElement>(null)
  const [status, setStatus] = useState<string | null>(null)
  const data = useFragment(
    graphql`
      fragment TimesheetGrid_timesheet on Timesheet {
        days
        rows {
          isEditable
          project {
            id
            name
            color
            client {
              name
            }
          }
          cells {
            id
            durationMinutes
            ...TimesheetCell_cell
          }
        }
      }
    `,
    timesheet,
  )

  const todayIso = today()
  const dayTotals = data.days.map((_, day) =>
    data.rows.reduce((sum, row) => sum + (row.cells[day]?.durationMinutes ?? 0), 0),
  )
  const weekTotal = dayTotals.reduce((sum, minutes) => sum + minutes, 0)

  /**
   * Enter moves down a row in the same day, like a spreadsheet.
   */
  function focusCell(row: number, day: number) {
    grid.current?.querySelector<HTMLInputElement>(`[data-cell="${row}:${day}"]`)?.focus()
  }

  if (data.rows.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-zinc-300 p-8 text-center text-sm text-zinc-500 dark:border-zinc-700">
        <p>You are not assigned to any active projects in this workspace.</p>
        <p className="mt-2">
          {isAdmin ? (
            <Link
              to={workspacePath(workspaceId, 'projects')}
              className="font-medium text-indigo-600 dark:text-indigo-400"
            >
              Assign yourself on the projects page
            </Link>
          ) : (
            'Ask a workspace admin to add you to a project.'
          )}
        </p>
      </div>
    )
  }

  return (
    <div ref={grid} className="space-y-2">
      <Table className="min-w-[44rem] table-fixed">
        <colgroup>
          <col className="w-48" />
          {data.days.map((day) => (
            <col key={day} />
          ))}
          <col className="w-20" />
        </colgroup>
        <TableHead>
          <TableRow>
            <TableHeader className={stickyShaded}>Project</TableHeader>
            {data.days.map((day) => (
              <TableHeader
                key={day}
                className={`text-center ${day === todayIso ? 'text-indigo-600 dark:text-indigo-400' : ''}`}
              >
                {formatDay(day)}
              </TableHeader>
            ))}
            <TableHeader className="text-right">Total</TableHeader>
          </TableRow>
        </TableHead>
        <TableBody>
          {data.rows.map((row, rowIndex) => {
            const rowTotal = row.cells.reduce((sum, cell) => sum + cell.durationMinutes, 0)
            return (
              <TableRow key={row.project.id}>
                <th scope="row" className={`${stickyBody} px-3 py-2 text-left font-normal`}>
                  <span className="flex items-center gap-2">
                    <span
                      aria-hidden
                      className="size-2.5 shrink-0 rounded-full"
                      style={{ backgroundColor: row.project.color }}
                    />
                    <span className="min-w-0">
                      <span className="block truncate font-medium">{row.project.name}</span>
                      <span className="block truncate text-xs text-zinc-500">
                        {row.project.client.name}
                        {row.isEditable ? '' : ', read only'}
                      </span>
                    </span>
                  </span>
                </th>
                {row.cells.map((cell, dayIndex) => (
                  <td
                    key={cell.id}
                    className={`p-1 ${isWeekend(data.days[dayIndex]) ? 'bg-zinc-50 dark:bg-zinc-900/40' : ''}`}
                  >
                    <TimesheetCell
                      cell={cell}
                      projectId={row.project.id}
                      label={`${row.project.name}, ${formatDay(data.days[dayIndex] ?? '')}`}
                      editable={row.isEditable}
                      position={`${rowIndex}:${dayIndex}`}
                      onEnter={() => {
                        focusCell(rowIndex + 1, dayIndex)
                      }}
                      onStatus={setStatus}
                    />
                  </td>
                ))}
                <td className="px-3 py-2 text-right font-medium tabular-nums">
                  {formatDuration(rowTotal, { blankZero: false })}
                </td>
              </TableRow>
            )
          })}
        </TableBody>
        <tfoot className="border-t border-zinc-200 bg-zinc-50 font-medium dark:border-zinc-800 dark:bg-zinc-900/60">
          <tr>
            <th scope="row" className={`${stickyShaded} px-3 py-2 text-left`}>
              Total
            </th>
            {dayTotals.map((minutes, day) => (
              <td key={data.days[day]} className="px-1 py-2 text-center tabular-nums">
                {formatDuration(minutes)}
              </td>
            ))}
            <td className="px-3 py-2 text-right tabular-nums">
              {formatDuration(weekTotal, { blankZero: false })}
            </td>
          </tr>
        </tfoot>
      </Table>
      <p
        role="status"
        aria-live="polite"
        className="min-h-5 text-sm text-red-600 dark:text-red-400"
      >
        {status}
      </p>
      <p className="text-xs text-zinc-500">
        Type times such as 1:30, 1.5, or 90m. Changes save when you leave a cell.
      </p>
    </div>
  )
}

function isWeekend(day: string | undefined): boolean {
  const date = day ? parseIsoDate(day) : null
  return date ? date.getDay() === 0 || date.getDay() === 6 : false
}
