import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ResultView } from './ResultView'
import { auswertung, kategorie, teilpruefung } from '../test/fixtures'

describe('ResultView', () => {
  // Die Reihenfolge steht in EvaluationCategoryOrder und ist bewusst eine
  // andere als die Zahlenreihenfolge des Enums. Die API liefert bereits
  // sortiert; das Frontend sortiert trotzdem selbst, weil eine wechselnde
  // Reihenfolge mehr verwirrt, als das Sortieren kostet.
  it('zeigt die Kategorien in der Anzeigereihenfolge', () => {
    render(
      <ResultView
        result={auswertung({
          categoryResults: [
            kategorie({ category: 'Functionality' }),
            kategorie({ category: 'CleanCode' }),
            kategorie({ category: 'Compilability' }),
          ],
        })}
      />,
    )

    const ueberschriften = screen
      .getAllByText(/Clean Code|Kompilierbarkeit|Funktionalität/)
      .map((el) => el.textContent)

    expect(ueberschriften).toEqual(['Clean Code', 'Kompilierbarkeit', 'Funktionalität'])
  })

  // Altlast-Kategorien aus alten Ergebnissen sollen nicht zwischen den
  // aktuellen auftauchen, sondern hinten anstehen.
  it('haengt unbekannte Kategorien hinten an', () => {
    render(
      <ResultView
        result={auswertung({
          categoryResults: [
            kategorie({ category: 'TestCases' }),
            kategorie({ category: 'CleanCode' }),
          ],
        })}
      />,
    )

    const ueberschriften = screen
      .getAllByText(/Clean Code|Testfälle/)
      .map((el) => el.textContent)

    expect(ueberschriften).toEqual(['Clean Code', 'Testfälle'])
  })

  it.each([
    [100, 'Hervorragende Arbeit!'],
    [80, 'Hervorragende Arbeit!'],
    [79, 'Guter Versuch!'],
    [50, 'Guter Versuch!'],
    [49, 'Da geht noch mehr!'],
    [0, 'Da geht noch mehr!'],
  ])('nennt bei %i Punkten "%s"', (score, ueberschrift) => {
    render(<ResultView result={auswertung({ totalScore: score })} />)

    expect(screen.getByText(ueberschrift)).toBeInTheDocument()
  })

  // matchMedia meldet im Test prefers-reduced-motion: reduce, deshalb steht der
  // Zielwert sofort da, statt hochgezählt zu werden. Geprüft wird also das
  // Ergebnis der Animation, nicht ihr Ablauf.
  it('zeigt die erreichte Punktzahl', () => {
    render(<ResultView result={auswertung({ totalScore: 73 })} />)

    expect(screen.getByText('73')).toBeInTheDocument()
  })

  it('meldet, wenn alle Teilpruefungen bestanden sind', () => {
    render(
      <ResultView
        result={auswertung({
          categoryResults: [
            kategorie({ testCaseResults: [teilpruefung({ passed: true })] }),
          ],
        })}
      />,
    )

    expect(screen.getByText('Alle Teilprüfungen bestanden.')).toBeInTheDocument()
  })

  it('zaehlt bestandene und offene Teilpruefungen ueber alle Kategorien', () => {
    render(
      <ResultView
        result={auswertung({
          totalScore: 40,
          categoryResults: [
            kategorie({
              category: 'CleanCode',
              passed: false,
              testCaseResults: [teilpruefung({ passed: true }), teilpruefung({ passed: false })],
            }),
            kategorie({
              category: 'Functionality',
              passed: false,
              testCaseResults: [teilpruefung({ passed: false })],
            }),
          ],
        })}
      />,
    )

    expect(screen.getByText(/1 bestanden, 2 offen/)).toBeInTheDocument()
  })
})
