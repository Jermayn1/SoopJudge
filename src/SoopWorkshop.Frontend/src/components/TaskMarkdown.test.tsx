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

  // Tabellen sind KEIN CommonMark, sondern GitHub Flavored Markdown. Ohne
  // remark-gfm fällt eine Tabelle als Absatz durch - die Zeilen stehen samt
  // ihrer Pipe-Zeichen im Fließtext, ohne Fehler und ohne Warnung. Eine
  // Aufgabenbeschreibung darf Tabellen enthalten, also steht der Fall hier
  // fest.
  describe('Tabellen (GFM)', () => {
    const tabelle = [
      '| Was | Beispiel | Welcher Typ passt? |',
      '|---|---|---|',
      '| Name | Ada | Text |',
      '| Alter | 27 | ganze Zahl |',
    ].join('\n')

    it('setzt eine Tabelle als table statt als Absatz', () => {
      const { container } = render(<TaskMarkdown>{tabelle}</TaskMarkdown>)

      expect(container.querySelector('table')).not.toBeNull()
      expect(container.querySelectorAll('tbody tr')).toHaveLength(2)
    })

    it('nimmt die Kopfzeile als th', () => {
      render(<TaskMarkdown>{tabelle}</TaskMarkdown>)

      const kopf = screen.getAllByRole('columnheader').map((z) => z.textContent)
      expect(kopf).toEqual(['Was', 'Beispiel', 'Welcher Typ passt?'])
    })

    it('laesst kein Pipe-Zeichen im Text stehen', () => {
      const { container } = render(<TaskMarkdown>{tabelle}</TaskMarkdown>)

      // Genau das war das Fehlerbild: "| Name | Ada | Text |" als Absatz.
      expect(container.textContent).not.toContain('|')
    })

    it('gibt der Tabelle einen eigenen Scroll-Rahmen', () => {
      const { container } = render(<TaskMarkdown>{tabelle}</TaskMarkdown>)

      // Eine breite Tabelle darf nicht die ganze Seite quer schieben.
      const rahmen = container.querySelector('table')?.parentElement
      expect(rahmen?.className).toContain('overflow-x-auto')
    })
  })
})
