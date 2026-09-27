/**
 * The GraphQL endpoint, fixed at build time from VITE_GRAPHQL_URL.
 *
 * Auth endpoints are resolved relative to it, so the API can live under a path prefix such as an IIS virtual directory.
 */
export const graphqlUrl = new URL(
  import.meta.env.VITE_GRAPHQL_URL ?? 'http://localhost:5080/graphql',
  window.location.href,
)

/**
 * Builds an Identity endpoint URL next to the GraphQL endpoint.
 */
export function authUrl(path: 'login' | 'register' | 'refresh'): URL {
  return new URL(`auth/${path}`, graphqlUrl)
}
