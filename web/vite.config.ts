import babel from '@rolldown/plugin-babel'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

/**
 * Vite compiles with Oxc, which has no Relay transform.
 *
 * Babel runs only the Relay plugin, and only on source files, so it swaps `graphql` tags for imports of generated artifacts.
 */
export default defineConfig({
  plugins: [
    babel({
      include: /\/src\/.*\.tsx?$/,
      plugins: [['relay', { eagerEsModules: true }]],
    }),
    react(),
    tailwindcss(),
  ],
  server: { port: 5173 },
})
