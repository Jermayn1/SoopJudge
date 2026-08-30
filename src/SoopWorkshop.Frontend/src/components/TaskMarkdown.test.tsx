import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { TaskMarkdown } from './TaskMarkdown'

// jsdom lädt kein Tailwind - berechnete Stile sagen hier nichts. Geprüft
// wird die Struktur, auf der die Regeln aufsetzen: Inline-Code bekommt eine
// Pille, ein Codeblock nicht, und die Unterscheidung geht über <pre>.
describe('TaskMarkdown', () => {
  const pyramide = ['Beispiel:', '', '```', '  *', ' ***', '*****', '```'].join('\n')

  it('setzt einen Codeblock als code im pre', () => {
    const { container } = render(<TaskMarkdown>{pyramide}</TaskMarkdown>)

    const code = container.querySelector('pre > code')
    expect(code).not.toBeNull()
  })

  it('gibt den Codeblock zeichengetreu wieder', () => {
    const { container } = render(<TaskMarkdown>{pyramide}</TaskMarkdown>)

    // Die führenden Leerzeichen sind die ganze Zentrierung. Geht eines davon
    // verloren, steht die Pyramide schief, ohne dass irgendwo ein Fehler
    // entsteht.
    expect(container.querySelector('pre > code')?.textContent).toBe('  *\n ***\n*****\n')
  })

  it('setzt Inline-Code ausserhalb eines pre', () => {
    const { container } = render(<TaskMarkdown>{'Die Klasse `Pyramide` schreiben.'}</TaskMarkdown>)

    const code = container.querySelector('code')
    expect(code).not.toBeNull()
    expect(code?.closest('pre')).toBeNull()
    expect(screen.getByText('Pyramide')).toBeInTheDocument()
  })
})
