import babel from '@rolldown/plugin-babel'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

/**
 * Vite compiles with Oxc, which has no Relay transform.
 *
 * Babel runs only the Relay plugin, and only on source files, so it swaps `graphql` tags for imports of generated artifacts.
 *
 * Framework code goes into its own chunks, which change rarely and stay cached across app releases.
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
  build: {
    rolldownOptions: {
      output: {
        codeSplitting: {
          groups: [
            {
              name: 'relay',
              test: /node_modules[\\/](react-relay|relay-runtime)[\\/]/,
              priority: 2,
            },
            {
              name: 'react',
              test: /node_modules[\\/](react|react-dom|react-router|scheduler)[\\/]/,
              priority: 1,
            },
          ],
        },
      },
    },
  },
})
