# Roadmap

v0.1 is the foundation and a working weekly timesheet.
Everything below is deliberately out of v0.1 and is listed so the scope stays honest.

## v0.2: approvals

- Submit a week for approval, and lock it once submitted.
- Managers review, approve, or return weeks for the projects they manage.
- Give the Manager role its first rights beyond Member; in v0.1 the two behave the same.
- Let managers see their team's time entries, which are private to their author in v0.1.

## Teams and accounts

- Invite people by email, with an accept flow and an expiry.
- Send email for invitations, confirmation, and password reset; the Identity endpoints exist but have no sender yet.
- A members page to change roles and remove people.
- Display names, so the app can show people by name instead of email.
- Sign out everywhere, by rotating the user's security stamp to end all sessions.
- Rate limit the auth endpoints.

## Timesheet

- Edit notes from the grid; the API already stores a note per entry.
- Timers, which will need several entries per project per day and so a change to the one entry per cell rule.
- Copy last week's rows, and fill a week from assignments.
- A day view for phones, as an alternative to scrolling the week sideways.
- AI entry: describe the week in a sentence and review the suggested cells.
- Choose the first day of the week per workspace.

## Reports

- Hours by client, project, person, and period, with CSV export.
- Billing rounding rules, such as quarter hours, applied in reports rather than in storage.

## Engineering

- End to end tests with Playwright in CI, covering sign in, catalog edits, and the timesheet.
- Unit tests for the web app's duration parsing and week helpers.
- Move to TypeScript 7 once typescript-eslint supports it; it currently supports TypeScript below 6.1.
- A strict Content Security Policy for the web app.
- Encrypt the Data Protection key ring at rest, for example with a certificate or a cloud key vault.
- Health check endpoints for load balancers and container orchestrators.
- OpenTelemetry traces and metrics for the API.
- Trusted documents, so production accepts only operations the web app was built with.
- Report upstream that compressed Guid parts in Hot Chocolate composite node IDs cannot end in a backslash byte, and switch back to compressed parts once fixed.
