/**
 * Reads a text field from a submitted form; file inputs and missing fields come back empty.
 */
export function formText(form: FormData, name: string): string {
  const value = form.get(name)
  return typeof value === 'string' ? value : ''
}
