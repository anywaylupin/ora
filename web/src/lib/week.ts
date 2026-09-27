/**
 * Dates travel as ISO strings (YYYY-MM-DD) matching the API's LocalDate scalar, and are read in the user's own time zone.
 */
export type IsoDate = string

const isoPattern = /^(\d{4})-(\d{2})-(\d{2})$/

export function toIsoDate(date: Date): IsoDate {
  const month = (date.getMonth() + 1).toString().padStart(2, '0')
  const day = date.getDate().toString().padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/**
 * Parses at local midnight, because new Date('2026-09-21') would mean midnight UTC.
 */
export function parseIsoDate(value: IsoDate): Date | null {
  const match = isoPattern.exec(value)
  if (!match) return null
  const date = new Date(
    Number.parseInt(match[1] ?? '', 10),
    Number.parseInt(match[2] ?? '', 10) - 1,
    Number.parseInt(match[3] ?? '', 10),
  )
  return toIsoDate(date) === value ? date : null
}

export function addDays(value: IsoDate, days: number): IsoDate {
  const date = parseIsoDate(value) ?? new Date()
  date.setDate(date.getDate() + days)
  return toIsoDate(date)
}

/**
 * Weeks start on Monday, as in ISO 8601 and in most consultancies' billing.
 */
export function mondayOf(date: Date): IsoDate {
  const monday = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  monday.setDate(monday.getDate() - ((monday.getDay() + 6) % 7))
  return toIsoDate(monday)
}

export function today(): IsoDate {
  return toIsoDate(new Date())
}

const dayFormat = new Intl.DateTimeFormat(undefined, { weekday: 'short', day: 'numeric' })
const rangeStartFormat = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short' })
const rangeEndFormat = new Intl.DateTimeFormat(undefined, {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

/**
 * Column labels such as Mon 21.
 */
export function formatDay(value: IsoDate): string {
  const date = parseIsoDate(value)
  return date ? dayFormat.format(date) : value
}

/**
 * A week label such as 21 Sep to 27 Sep 2026, avoiding dash ranges.
 */
export function formatWeek(weekStart: IsoDate): string {
  const start = parseIsoDate(weekStart)
  const end = parseIsoDate(addDays(weekStart, 6))
  return start && end
    ? `${rangeStartFormat.format(start)} to ${rangeEndFormat.format(end)}`
    : weekStart
}
