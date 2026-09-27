# 0004. Store durations as integer minutes

- Status: accepted
- Date: 2026-09-27

## Context

Timesheets are summed constantly: per row, per day, per week, per project, and later per invoice.
Floating point hours cannot represent common values exactly. For example, 0.1 hours is 6 minutes, but 0.1 has no exact binary form, and sums of many entries drift.
Consultancies bill in minutes or in fractions of an hour, and a total that is off by a minute erodes trust in the whole tool.

## Decision

Store every duration as a whole number of minutes, in an `int` column named `DurationMinutes`.

- The API accepts and returns integer minutes; the GraphQL type is `Int`.
- The database enforces a range of 1 to 1,440 minutes per entry with a check constraint, and the API also caps a day at 24 hours across projects.
- The web app converts only at the edges: it parses what people type, such as `1:30`, `1.5`, or `90m`, into minutes, and formats minutes as `h:mm` for display.
- Decimal input is converted with integer arithmetic on the digits, so `1.25` becomes exactly 75 minutes.

## Consequences

- Sums are exact at every level, in SQL and in the browser.
- Minute precision is enough for time tracking. Rounding rules, such as billing in quarter hours, will be applied in reports, not in storage.
- Anyone reading the database directly must remember the unit, which the column name makes explicit.
