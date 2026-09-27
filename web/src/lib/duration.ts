/**
 * Durations are whole minutes everywhere, matching the API, so totals never drift.
 */
export const minutesPerDay = 24 * 60

const clockPattern = /^(\d{1,2}):([0-5]\d)$/
const unitPattern = /^(?:(\d{1,2})h)?(?:(\d{1,4})m)?$/
const decimalPattern = /^(\d{1,2})(?:[.,](\d{1,2}))?$/

/**
 * Reads what people type into a timesheet cell.
 *
 * Accepts 1:30, 1.5, 1,5, 2h, 90m, 2h30m, and 8 (hours), and returns null for anything else.
 *
 * An empty cell means zero minutes, which clears the entry.
 */
export function parseDuration(input: string): number | null {
  const text = input.trim().toLowerCase().replaceAll(' ', '')
  if (text === '') return 0

  const clock = clockPattern.exec(text)
  if (clock) return toMinutes(clock[1], clock[2])

  const units = unitPattern.exec(text)
  if (units && (units[1] !== undefined || units[2] !== undefined)) {
    return withinDay(
      Number.parseInt(units[1] ?? '0', 10) * 60 + Number.parseInt(units[2] ?? '0', 10),
    )
  }

  const decimal = decimalPattern.exec(text)
  if (decimal) {
    const hundredths = Number.parseInt((decimal[2] ?? '0').padEnd(2, '0'), 10)
    return withinDay(
      Number.parseInt(decimal[1] ?? '0', 10) * 60 + Math.round((hundredths * 60) / 100),
    )
  }

  return null
}

function toMinutes(hours: string | undefined, minutes: string | undefined): number | null {
  return withinDay(Number.parseInt(hours ?? '0', 10) * 60 + Number.parseInt(minutes ?? '0', 10))
}

function withinDay(minutes: number): number | null {
  return minutes <= minutesPerDay ? minutes : null
}

/**
 * Shows minutes as hours and minutes, such as 7:30, and zero as blank so empty cells stay quiet.
 */
export function formatDuration(minutes: number, { blankZero = true } = {}): string {
  if (minutes === 0 && blankZero) return ''
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  return `${hours}:${rest.toString().padStart(2, '0')}`
}
