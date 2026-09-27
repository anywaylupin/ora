import { useTransition } from 'react'
import { graphql, useLazyLoadQuery } from 'react-relay'
import { useSearchParams } from 'react-router'
import { Button } from '../../components/ui/Button'
import { addDays, formatWeek, mondayOf, parseIsoDate, type IsoDate } from '../../lib/week'
import { useWorkspace } from '../workspaces/workspaceContext'
import type { TimesheetPageQuery } from './__generated__/TimesheetPageQuery.graphql'
import { TimesheetGrid } from './TimesheetGrid'

/**
 * The week comes from the URL, so a week can be bookmarked and the back button steps through weeks.
 *
 * Anything that is not a valid Monday falls back to the current week.
 */
function weekFromParam(value: string | null): IsoDate {
  const date = value ? parseIsoDate(value) : null
  return date && date.getDay() === 1 && value ? value : mondayOf(new Date())
}

/**
 * Revalidates on every visit, because assignments made on the projects page change which rows the week has.
 */
export function TimesheetPage() {
  const { workspaceId, viewerRole } = useWorkspace()
  const [searchParams, setSearchParams] = useSearchParams()
  const [isPending, startTransition] = useTransition()
  const weekStart = weekFromParam(searchParams.get('week'))
  const thisWeek = mondayOf(new Date())

  const data = useLazyLoadQuery<TimesheetPageQuery>(
    graphql`
      query TimesheetPageQuery($workspaceId: ID!, $weekStart: LocalDate!) {
        workspace: node(id: $workspaceId) {
          ... on Workspace {
            timesheet(weekStart: $weekStart) {
              ...TimesheetGrid_timesheet
            }
          }
        }
      }
    `,
    { workspaceId, weekStart },
    { fetchPolicy: 'store-and-network' },
  )

  function goTo(week: IsoDate) {
    startTransition(() => {
      setSearchParams(week === thisWeek ? {} : { week })
    })
  }

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <div className="mr-auto">
          <h1 className="text-xl font-semibold">Timesheet</h1>
          <p className="text-sm text-zinc-500">{formatWeek(weekStart)}</p>
        </div>
        <div className="flex gap-2" role="group" aria-label="Week">
          <Button
            size="sm"
            aria-label="Previous week"
            onClick={() => {
              goTo(addDays(weekStart, -7))
            }}
          >
            Previous
          </Button>
          <Button
            size="sm"
            disabled={weekStart === thisWeek}
            onClick={() => {
              goTo(thisWeek)
            }}
          >
            This week
          </Button>
          <Button
            size="sm"
            aria-label="Next week"
            onClick={() => {
              goTo(addDays(weekStart, 7))
            }}
          >
            Next
          </Button>
        </div>
      </div>
      {data.workspace?.timesheet ? (
        <div className={isPending ? 'opacity-60 transition-opacity' : ''}>
          <TimesheetGrid
            timesheet={data.workspace.timesheet}
            workspaceId={workspaceId}
            isAdmin={viewerRole === 'ADMIN'}
          />
        </div>
      ) : null}
    </section>
  )
}
