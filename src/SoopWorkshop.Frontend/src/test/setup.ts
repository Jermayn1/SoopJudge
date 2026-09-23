import '@testing-library/jest-dom/vitest'
import { afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'
import { installMatchMedia, resetMatchMedia } from './matchMedia'

// ResultView fragt prefers-reduced-motion ab, das Theme prefers-color-scheme.
// Warum der Ersatz welche Antwort gibt, steht in matchMedia.ts.
installMatchMedia()

// Ohne das teilen sich aufeinanderfolgende Tests denselben DOM-Baum, und ein
// getByText fände das Element aus dem vorherigen Test.
//
// jsdom behält außerdem localStorage und das <html>-Element über alle Tests
// einer Datei. Ein Test, der dunkel schaltet, ließe den nächsten sonst im
// Dunkeln anfangen.
afterEach(() => {
  cleanup()
  resetMatchMedia()
  localStorage.clear()
  document.documentElement.className = ''
  document.documentElement.removeAttribute('style')
})
