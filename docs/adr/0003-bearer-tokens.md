# 0003. Authenticate with bearer tokens instead of cookies

- Status: accepted
- Date: 2026-09-27

## Context

The web app and the API will be deployed on different domains, for example a static host for the app and IIS or a container for the API.
Cookies set by the API would be third-party cookies from the browser's point of view.
Browsers increasingly block or partition those, and making them work needs `SameSite=None`, CORS with credentials, and CSRF protection.

ASP.NET Core Identity ships API endpoints for sign up, sign in, and token refresh that issue bearer tokens.

## Decision

Use the ASP.NET Core Identity API endpoints with bearer tokens only.

- `MapIdentityApi` serves `/auth/register`, `/auth/login`, and `/auth/refresh`.
- Authentication is registered with the bearer scheme alone, so no cookie scheme exists to misconfigure.
- Access tokens last one hour and refresh tokens fourteen days, the Identity defaults.
- The web app refreshes an access token a minute before it expires, shares one refresh between concurrent requests, and retries a request once after a refresh if the API reports `AUTH_NOT_AUTHENTICATED`.
- Tokens are protected with ASP.NET Core Data Protection, and the key ring is stored in the database, so tokens survive restarts and work across several API instances, in Docker or on IIS.
- CORS allows only the origins listed in configuration.

## Consequences

- No CSRF protection is needed, because the browser never attaches credentials automatically.
- The web app stores tokens in `localStorage` so a session survives a reload. Any script running on the page can read them, so XSS is the main threat. React escapes output by default, the app renders no raw HTML, and a strict Content Security Policy is on the roadmap.
- Tokens are opaque Data Protection payloads, not JWTs, so they cannot be read or verified by other services. That is fine while Ora has one API.
- Signing out forgets the tokens on the client. Identity has no endpoint to revoke a single bearer token, so a copied refresh token stays valid until it expires. Rotating the user's security stamp, which ends every session, is on the roadmap as "sign out everywhere".
- Email confirmation and password reset endpoints exist but send nothing yet, because email is out of scope for v0.1.
