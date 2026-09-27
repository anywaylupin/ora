import {
  Environment,
  Network,
  RecordSource,
  Store,
  type FetchFunction,
  type GraphQLResponse,
} from 'relay-runtime'
import { expire, getAccessToken, refresh } from '../auth/session'
import { graphqlUrl } from '../lib/config'

/**
 * Hot Chocolate reports a missing or expired token as a GraphQL error with this code, not as HTTP 401.
 */
const notAuthenticated = 'AUTH_NOT_AUTHENTICATED'

function hasAuthError(body: unknown): boolean {
  const errors: unknown =
    typeof body === 'object' && body !== null ? Reflect.get(body, 'errors') : null
  return (
    Array.isArray(errors) &&
    errors.some((error: unknown) => {
      const extensions: unknown =
        typeof error === 'object' && error !== null ? Reflect.get(error, 'extensions') : null
      return (
        typeof extensions === 'object' &&
        extensions !== null &&
        Reflect.get(extensions, 'code') === notAuthenticated
      )
    })
  )
}

/**
 * Relay trusts the shape of what the fetch function returns, so the JSON is checked first.
 */
function isGraphQLResponse(body: unknown): body is GraphQLResponse {
  return typeof body === 'object' && body !== null && ('data' in body || 'errors' in body)
}

async function post(query: string | null, variables: object, token: string | null) {
  const response = await fetch(graphqlUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/graphql-response+json, application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ query, variables }),
  })
  const body: unknown = await response.json()
  return body
}

/**
 * Sends operations with the current bearer token, and retries once after a refresh if the server rejects it.
 *
 * A failed refresh ends the session, which sends the user to sign in and back afterwards.
 */
const fetchGraphQL: FetchFunction = async (request, variables) => {
  const token = await getAccessToken()
  let body = await post(request.text, variables, token)

  if (token && hasAuthError(body)) {
    const next = await refresh()
    if (next) body = await post(request.text, variables, next.accessToken)
    else expire()
  }

  if (!isGraphQLResponse(body)) throw new Error('The server returned an unexpected response.')
  return body
}

/**
 * A fresh environment per session, so one user's cached data never shows for the next.
 */
export function createEnvironment(): Environment {
  return new Environment({
    network: Network.create(fetchGraphQL),
    store: new Store(new RecordSource()),
  })
}
