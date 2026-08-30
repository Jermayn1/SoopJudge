import '@testing-library/jest-dom/vitest'
import { afterEach, vi } from 'vitest'
import { cleanup } from '@testing-library/react'

// jsdom kennt matchMedia nicht. ResultView fragt darüber
// prefers-reduced-motion ab; ohne Stub wirft der erste Render.
//
// matches: true ist Absicht und nicht bloß der bequemere Wert: damit setzt
// useCountUp den Zielwert sofort, statt ihn über requestAnimationFrame
// hochzuzählen, und der Punktestand steht ohne Warten im DOM. Geprüft wird
// damit das Ergebnis der Animation, nicht ihr Ablauf.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: true,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }),
})

// Ohne das teilen sich aufeinanderfolgende Tests denselben DOM-Baum, und ein
// getByText fände das Element aus dem vorherigen Test.
afterEach(() => {
  cleanup()
})
