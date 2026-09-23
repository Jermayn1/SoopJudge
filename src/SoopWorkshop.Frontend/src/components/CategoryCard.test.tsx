import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { CategoryCard } from './CategoryCard'
import { kategorie, teilpruefung } from '../test/fixtures'

function zeige(result: Parameters<typeof CategoryCard>[0]['result']) {
  return render(<CategoryCard result={result} delay={0} />)
}

// Die Darstellungsregeln aus CategoryCard. Sie sind der Grund, warum die
// Ergebnisseite überhaupt verständlich ist - und sie werden hier geprüft,
// nicht in ResultView, weil CategoryCard sie umsetzt.
describe('Darstellung einer Teilprüfung', () => {
  // Bei 100 Punkten soll die Seite ruhig bleiben: die Details einer
  // bestandenen Prüfung stehen zugeklappt bereit, nicht offen da.
  it('zeigt eine bestandene Pruefung mit Werten zugeklappt', () => {
    const { container } = zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: true, input: '3 4', expectedOutput: '7', actualOutput: '7' }),
        ],
      }),
    )

    const knopf = screen.getByRole('button', { name: 'Das Programm gibt den Gruss aus' })
    expect(knopf).toHaveAttribute('aria-expanded', 'false')
    expect(container.querySelector('[id^="teilpruefung-detail-"]')).toHaveAttribute('inert')
  })

  it('klappt Eingabe, Erwartet und Erhalten einer bestandenen Pruefung auf', async () => {
    const user = userEvent.setup()
    const { container } = zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: true, input: '3 4', expectedOutput: '7', actualOutput: '7' }),
        ],
      }),
    )

    await user.click(screen.getByRole('button', { name: 'Das Programm gibt den Gruss aus' }))

    const bereich = container.querySelector('[id^="teilpruefung-detail-"]')
    expect(bereich).not.toHaveAttribute('inert')
    expect(screen.getByText('Eingabe')).toBeInTheDocument()
    expect(screen.getByText('3 4')).toBeInTheDocument()
    expect(screen.getByText('Erwartet')).toBeInTheDocument()
    expect(screen.getByText('Erhalten')).toBeInTheDocument()
  })

  it('klappt eine bestandene Pruefung auch mit der Tastatur auf', async () => {
    const user = userEvent.setup()
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [teilpruefung({ passed: true, expectedOutput: '7', actualOutput: '7' })],
      }),
    )

    const knopf = screen.getByRole('button', { name: 'Das Programm gibt den Gruss aus' })
    knopf.focus()
    await user.keyboard('{Enter}')
    expect(knopf).toHaveAttribute('aria-expanded', 'true')

    await user.keyboard(' ')
    expect(knopf).toHaveAttribute('aria-expanded', 'false')
  })

  // Bei JUnit kommen die Werte aus dem Testlauf, je Vergleich mit dem Aufruf.
  it('klappt die Vergleiche einer bestandenen JUnit-Pruefung auf', async () => {
    const user = userEvent.setup()
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({
            description: 'bezahlen liefert das Wechselgeld',
            passed: true,
            comparisons: [
              { call: 'kasse.bezahlen(20)', expected: '7.5', actual: '7.5', passed: true },
              { call: '', expected: 'true', actual: 'true', passed: true },
            ],
          }),
        ],
      }),
    )

    await user.click(screen.getByRole('button', { name: 'bezahlen liefert das Wechselgeld' }))

    expect(screen.getByText('kasse.bezahlen(20)')).toBeInTheDocument()
    expect(screen.getAllByText('Erwartet')).toHaveLength(2)
    // Ohne Aufruf steht auch keine leere Zeile "Aufruf" da.
    expect(screen.getAllByText('Aufruf')).toHaveLength(1)
  })

  // Bei einer roten Prüfung zeigen die Vergleiche, bis wohin es gestimmt hat.
  it('zeigt bei einer gescheiterten JUnit-Pruefung alle Vergleiche, den gescheiterten rot', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({
            passed: false,
            expectedOutput: '1.0',
            actualOutput: '5.0',
            comparisons: [
              { call: 'kasse.anzahl()', expected: '3', actual: '3', passed: true },
              { call: 'kasse.bezahlen(10)', expected: '1.0', actual: '5.0', passed: false },
            ],
          }),
        ],
      }),
    )

    expect(screen.getByText('kasse.anzahl()')).toBeVisible()
    expect(screen.getByText('5.0').className).toContain('text-rose-800')
    expect(screen.getAllByText('3')[1].className).toContain('text-emerald-800')
  })

  // Fiel der Test wegen einer Ausnahme durch, erklärt die Meldung den Grund,
  // nicht die Vergleiche davor.
  it('zeigt die Meldung, wenn kein Vergleich gescheitert ist', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({
            passed: false,
            actualOutput: 'NullPointerException',
            comparisons: [{ call: 'kasse.anzahl()', expected: '3', actual: '3', passed: true }],
          }),
        ],
      }),
    )

    expect(screen.getByText('NullPointerException')).toBeVisible()
    expect(screen.queryByText('kasse.anzahl()')).not.toBeInTheDocument()
  })

  // Rot hieße "falsch". Bei einer bestandenen Prüfung stimmt das Erhaltene.
  it('faerbt das Erhaltene einer bestandenen Pruefung nicht rot', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [teilpruefung({ passed: true, expectedOutput: 'Hallo', actualOutput: 'Hallo!' })],
      }),
    )

    const erhalten = screen.getByText('Hallo!')
    expect(erhalten.className).toContain('text-emerald-800')
    expect(erhalten.className).not.toContain('text-rose')
  })

  // Beim Kompilieren steht im Erhaltenen die Compilerausgabe, auch bei Erfolg
  // mit Warnungen. Ohne Erwartung gibt es nichts zu vergleichen, also auch
  // nichts aufzuklappen - sonst stünde dort "Erwartet —".
  it('bietet ohne Erwartung und Eingabe kein Aufklappen an', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ description: 'Der Code kompiliert', passed: true, actualOutput: 'Note: unchecked' }),
        ],
      }),
    )

    expect(screen.queryByRole('button', { name: 'Der Code kompiliert' })).not.toBeInTheDocument()
    expect(screen.queryByText('Erwartet')).not.toBeInTheDocument()
    expect(screen.queryByText('Note: unchecked')).not.toBeInTheDocument()
  })

  it('zeigt eine durchgefallene Pruefung sofort und ohne Knopf', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ description: 'Das Programm addiert', passed: false, expectedOutput: '7', actualOutput: '12' }),
        ],
      }),
    )

    expect(screen.queryByRole('button', { name: 'Das Programm addiert' })).not.toBeInTheDocument()
    expect(screen.getByText('12')).toBeVisible()
  })

  it('zeigt die Eingabe nur, wenn es eine gab', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, input: '', expectedOutput: 'Hallo', actualOutput: 'Tach' }),
        ],
      }),
    )

    expect(screen.queryByText('Eingabe')).not.toBeInTheDocument()
    expect(screen.getByText('Erwartet')).toBeInTheDocument()
  })

  it('zeigt die Eingabe, wenn eine vorlag', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, input: '3 4', expectedOutput: '7', actualOutput: '12' }),
        ],
      }),
    )

    expect(screen.getByText('Eingabe')).toBeInTheDocument()
    expect(screen.getByText('3 4')).toBeInTheDocument()
  })

  // Ein "Erwartet" ohne Gegenstück lässt den Leser raten. Die beiden gehören
  // zusammen, immer.
  it.each([
    ['nur Erwartet', 'Hallo', ''],
    ['nur Erhalten', '', 'Tach'],
    ['beides', 'Hallo', 'Tach'],
  ])('zeigt bei %s Erwartet UND Erhalten gemeinsam', (_fall, erwartet, erhalten) => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, expectedOutput: erwartet, actualOutput: erhalten }),
        ],
      }),
    )

    expect(screen.getByText('Erwartet')).toBeInTheDocument()
    expect(screen.getByText('Erhalten')).toBeInTheDocument()
  })

  // Fehlt eine Seite, steht dort ein Gedankenstrich - nicht nichts.
  it('setzt fuer die fehlende Seite einen Gedankenstrich', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: false, expectedOutput: 'Hallo', actualOutput: '' }),
        ],
      }),
    )

    expect(screen.getByText('—')).toBeInTheDocument()
  })

  // Ohne jede Erwartung gibt es nichts zu vergleichen; dann bleibt die Liste
  // leer, statt zweimal einen Gedankenstrich zu zeigen.
  it('zeigt ohne Erwartung gar keinen Vergleich', () => {
    zeige(
      kategorie({
        passed: false,
        testCaseResults: [
          teilpruefung({
            description: 'Der Code kompiliert',
            passed: false,
            expectedOutput: '',
            actualOutput: '',
          }),
        ],
      }),
    )

    expect(screen.getByText('Der Code kompiliert')).toBeInTheDocument()
    expect(screen.queryByText('Erwartet')).not.toBeInTheDocument()
    expect(screen.queryByText('—')).not.toBeInTheDocument()
  })
})

describe('CategoryCard', () => {
  it('uebersetzt die Kategorie ins Deutsche', () => {
    zeige(kategorie({ category: 'Compilability' }))

    expect(screen.getByText('Kompilierbarkeit')).toBeInTheDocument()
  })

  // Altlast-Kategorien kommen in alten Ergebnissen noch vor und brauchen einen
  // Namen, sonst stünde dort der englische Enum-Wert.
  it('kennt auch die abgeschafften Kategorien', () => {
    zeige(kategorie({ category: 'TestCases' }))

    expect(screen.getByText('Testfälle')).toBeInTheDocument()
  })

  it('zeigt Punkte, erreichbare Punkte und die Trefferquote', () => {
    zeige(
      kategorie({
        points: 40,
        maxPoints: 65,
        passed: false,
        testCaseResults: [
          teilpruefung({ passed: true }),
          teilpruefung({ passed: false }),
          teilpruefung({ passed: false }),
        ],
      }),
    )

    expect(screen.getByText('40')).toBeInTheDocument()
    expect(screen.getByText('/ 65')).toBeInTheDocument()
    expect(screen.getByText('1/3')).toBeInTheDocument()
  })

  // Eine durchgefallene Kategorie steht offen da: dort will man sofort
  // nachsehen. Eine bestandene bleibt zu und hält die Seite kurz.
  it('startet aufgeklappt, wenn die Kategorie durchgefallen ist', () => {
    zeige(kategorie({ passed: false, testCaseResults: [teilpruefung({ passed: false })] }))

    expect(screen.getByRole('button')).toHaveAttribute('aria-expanded', 'true')
  })

  it('startet zugeklappt, wenn alles bestanden ist', () => {
    zeige(kategorie({ passed: true, testCaseResults: [teilpruefung({ passed: true })] }))

    expect(screen.getByRole('button')).toHaveAttribute('aria-expanded', 'false')
  })

  it('laesst sich auf- und zuklappen', async () => {
    const user = userEvent.setup()
    zeige(kategorie({ passed: true, testCaseResults: [teilpruefung()] }))

    const knopf = screen.getByRole('button')
    await user.click(knopf)
    expect(knopf).toHaveAttribute('aria-expanded', 'true')

    await user.click(knopf)
    expect(knopf).toHaveAttribute('aria-expanded', 'false')
  })

  // Ohne Teilprüfungen und ohne Hinweis gibt es nichts aufzuklappen - dann
  // darf der Knopf auch nicht so tun, als gäbe es etwas.
  it('bietet ohne Inhalt kein Aufklappen an', () => {
    zeige(kategorie({ testCaseResults: [], errorTip: '' }))

    expect(screen.getByRole('button')).not.toHaveAttribute('aria-expanded')
  })

  it('klappt allein wegen eines Hinweises auf', () => {
    zeige(kategorie({ passed: false, testCaseResults: [], errorTip: 'Prüfe die Ausgabe.' }))

    expect(screen.getByRole('button')).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('Prüfe die Ausgabe.')).toBeInTheDocument()
  })

  // Was eingeklappt ist, bekommt inert. "overflow: hidden" beschneidet nur das
  // Zeichnen: ohne inert blieben die Elemente darin antabbar, obwohl niemand
  // sie sieht - eine unsichtbare Tastaturfalle.
  it('macht den eingeklappten Bereich unerreichbar', () => {
    const { container } = zeige(
      kategorie({ passed: true, testCaseResults: [teilpruefung({ passed: true })] }),
    )

    const bereich = container.querySelector('[id^="kategorie-detail-"]')
    expect(bereich).not.toBeNull()
    expect(bereich).toHaveAttribute('inert')
  })

  it('nimmt dem aufgeklappten Bereich das inert wieder', async () => {
    const user = userEvent.setup()
    const { container } = zeige(
      kategorie({ passed: true, testCaseResults: [teilpruefung({ passed: true })] }),
    )

    await user.click(screen.getByRole('button'))

    expect(container.querySelector('[id^="kategorie-detail-"]')).not.toHaveAttribute('inert')
  })

  // Ohne die Abfrage teilte die Anzeige durch null.
  it('kommt mit null erreichbaren Punkten zurecht', () => {
    zeige(kategorie({ points: 0, maxPoints: 0 }))

    expect(screen.getByText('/ 0')).toBeInTheDocument()
  })
})
