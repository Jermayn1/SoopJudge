import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { CodeWindow } from './CodeWindow'

const KONTO = `public class Konto {
    private int stand = 0;
}`

describe('CodeWindow', () => {
  it('nennt den Dateinamen in der Titelleiste', () => {
    render(<CodeWindow fileName="Konto.java" code={KONTO} />)

    expect(screen.getByText('Konto.java')).toBeInTheDocument()
  })

  it('nummeriert so viele Zeilen, wie die Datei hat', () => {
    render(<CodeWindow fileName="Konto.java" code={KONTO} />)

    expect(screen.getByText('1')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
    expect(screen.queryByText('4')).not.toBeInTheDocument()
  })

  // Fast jede Datei endet mit einem Zeilenumbruch. Ohne den Schnitt zeigte
  // das Fenster eine Zeile mehr, als die Datei hat - und die Nummern liefen
  // gegen den Code aus dem Ruder.
  it('zaehlt einen abschliessenden Zeilenumbruch nicht als Zeile', () => {
    render(<CodeWindow fileName="Konto.java" code={`${KONTO}\n`} />)

    expect(screen.getByText('3')).toBeInTheDocument()
    expect(screen.queryByText('4')).not.toBeInTheDocument()
  })

  // Der Test, der die Entscheidung fuer dangerouslySetInnerHTML traegt: der
  // Inhalt kommt von Teilnehmern, und highlight.js maskiert ihn. Das wird
  // geprueft und nicht geglaubt.
  it('maskiert HTML aus dem abgegebenen Code', () => {
    const { container } = render(
      <CodeWindow
        fileName="Main.java"
        code={'class Main { void f() { System.out.println("<script>alert(1)</script>"); } }'}
      />,
    )

    expect(container.querySelector('script')).toBeNull()

    // Und sichtbar ist der Text trotzdem - maskiert heisst nicht verschluckt.
    expect(container.textContent).toContain('<script>alert(1)</script>')
  })

  it('faerbt Schluesselwoerter, Zeichenketten und Kommentare unterschiedlich', () => {
    const { container } = render(
      <CodeWindow fileName="Main.java" code={'// Hinweis\nclass Main { String s = "hallo"; }'} />,
    )

    expect(container.querySelector('.hljs-keyword')).not.toBeNull()
    expect(container.querySelector('.hljs-string')).not.toBeNull()
    expect(container.querySelector('.hljs-comment')).not.toBeNull()
  })

  // Waechter, kein Selbstzweck. Ein absolut gesetztes Element ohne
  // positionierten Vorfahren haengt am Ursprungsblock des Dokuments, und weil
  // overflow-hidden nur positionierte Nachfahren beschneidet, entkommt es
  // jedem Scroll-Container darueber - das Dokument wird dadurch als Ganzes
  // scrollbar und die Kopfleiste der Verwaltung rutscht beim Blaettern weg.
  // Genau das ist im Browser passiert, ueber die Meldezone mit "sr-only":
  // diese Klasse arbeitet mit position: absolute, ohne dass ihr Name es
  // verraet.
  it('laesst kein absolut gesetztes Kind aus der Karte entkommen', () => {
    const { container } = render(<CodeWindow fileName="Konto.java" code={KONTO} />)

    const karte = container.firstElementChild!
    expect(karte).toHaveClass('relative')

    const abgesetzt = Array.from(container.querySelectorAll('.absolute, .sr-only'))
    expect(abgesetzt.length).toBeGreaterThan(0)

    for (const kind of abgesetzt) {
      const anker = kind.closest('.relative')
      expect(anker).not.toBeNull()
      // Der Anker darf die Karte selbst sein oder etwas darin - Hauptsache,
      // er liegt nicht ausserhalb.
      expect(karte.contains(anker!)).toBe(true)
    }
  })

  // Ohne inert blieben der Codeblock und der Text darin antabbar, obwohl
  // niemand sie sieht - eine unsichtbare Tastaturfalle.
  it('nimmt den eingeklappten Bereich aus der Tab-Reihenfolge', async () => {
    const user = userEvent.setup()
    render(<CodeWindow fileName="Konto.java" code={KONTO} />)

    const schalter = screen.getByRole('button', { name: /einklappen/i })
    const bereich = document.getElementById(schalter.getAttribute('aria-controls') ?? '')

    expect(bereich).not.toBeNull()
    expect(bereich).not.toHaveAttribute('inert')

    await user.click(schalter)

    expect(schalter).toHaveAttribute('aria-expanded', 'false')
    expect(bereich).toHaveAttribute('inert')
  })

  it('meldet zurueck, wenn das Kopieren geklappt hat', async () => {
    const user = userEvent.setup()
    render(<CodeWindow fileName="Konto.java" code={KONTO} />)

    // userEvent richtet navigator.clipboard selbst ein - genau der Weg, den
    // der Knopf im Browser zuerst versucht.
    await user.click(screen.getByRole('button', { name: /kopieren/i }))

    expect(await screen.findByText('Kopiert')).toBeInTheDocument()
    expect(await navigator.clipboard.readText()).toBe(KONTO)
  })
})
