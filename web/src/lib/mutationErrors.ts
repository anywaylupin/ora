/**
 * The shape every mutation selects for its typed errors.
 */
type PayloadError = {
  readonly __typename: string
  readonly message?: string
  readonly field?: string
}

/**
 * Picks the message for one input field, or the first general message when no field is given.
 */
export function errorMessage(
  errors: readonly PayloadError[] | null | undefined,
  field?: string,
): string | null {
  const match = errors?.find((error) => (field ? error.field === field : !error.field))
  return match?.message ?? null
}
