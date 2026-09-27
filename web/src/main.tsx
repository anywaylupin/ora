import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'

const root = document.getElementById('root')
if (!root) throw new Error('The root element is missing from index.html.')

createRoot(root).render(
  <StrictMode>
    <p className="p-4">Ora</p>
  </StrictMode>,
)
