import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import './index.css'

const container = document.getElementById('root')

if (!container) {
  // Kein stiller Fehlschlag: ohne diesen Knoten bliebe die Seite weiß und
  // die Konsole leer — der teuerste Fehler beim Suchen.
  throw new Error('Das Wurzelelement #root fehlt in index.html.')
}

createRoot(container).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)
