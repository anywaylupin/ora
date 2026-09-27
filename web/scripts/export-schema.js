import { execFileSync } from 'node:child_process'
import { copyFileSync, mkdtempSync, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

/**
 * Exports the API's GraphQL schema into web/schema.graphql for the Relay compiler.
 *
 * Hot Chocolate also writes a Fusion settings file next to the schema, so the export goes to a temporary folder and only the schema is copied.
 */
const webRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const project = resolve(webRoot, '../api/Ora.Api')
const outputDirectory = mkdtempSync(join(tmpdir(), 'ora-schema-'))

try {
  execFileSync(
    'dotnet',
    [
      'run',
      '--project',
      project,
      '--no-launch-profile',
      '--',
      'schema',
      'export',
      '--output',
      join(outputDirectory, 'schema.graphql'),
    ],
    { stdio: ['ignore', 'ignore', 'inherit'] },
  )
  copyFileSync(join(outputDirectory, 'schema.graphql'), join(webRoot, 'schema.graphql'))
  console.log('Exported the schema to web/schema.graphql')
} finally {
  rmSync(outputDirectory, { recursive: true, force: true })
}
