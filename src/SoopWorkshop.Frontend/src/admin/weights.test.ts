import { describe, expect, it } from 'vitest'
import { defaultWeights, distributePoints, toWeightValues } from './weights'

describe('distributePoints', () => {
  // Die Standardgewichte aus Evaluation:CategoryWeights ergeben glatt 15/20/65.
  it('normiert die Standardgewichte auf 100', () => {
    expect(distributePoints([15, 20, 65])).toEqual([15, 20, 65])
  })

  // Fällt eine Kategorie aus der Wertung, verteilt sich ihr Gewicht auf die
  // übrigen - aus 15/20 wird 43/57. Das Backend rechnet in EvaluationScorer
  // identisch; weichen die beiden ab, zeigt die Vorschau eine andere Verteilung
  // als die Auswertung später vergibt.
  it('verteilt das Gewicht einer weggefallenen Kategorie', () => {
    expect(distributePoints([15, 20])).toEqual([43, 57])
  })

  it.each([
    [[1, 1, 1], [34, 33, 33]],
    [[1, 1], [50, 50]],
    [[1], [100]],
    [[10, 20, 30, 40], [10, 20, 30, 40]],
  ])('%j ergibt %j und immer die Summe 100', (gewichte, erwartet) => {
    const punkte = distributePoints(gewichte)

    expect(punkte).toEqual(erwartet)
    expect(punkte.reduce((a, b) => a + b, 0)).toBe(100)
  })

  // Bei gleichem Nachkommaanteil entscheidet die Position - sonst hänge das
  // Ergebnis an der Sortierstabilität der Laufzeitumgebung.
  it('bevorzugt bei Gleichstand den frueheren Eintrag', () => {
    expect(distributePoints([1, 1, 1])).toEqual([34, 33, 33])
  })

  it('summiert sich auch bei krummen Gewichten exakt auf 100', () => {
    const punkte = distributePoints([7, 11, 13, 17, 19])

    expect(punkte.reduce((a, b) => a + b, 0)).toBe(100)
  })

  // Ein Gewicht von 0 lehnt das Backend ab; hier fällt die Rechnung nur
  // stillschweigend auf lauter Nullen zurück, statt durch null zu teilen.
  it('liefert bei Gesamtgewicht 0 lauter Nullen', () => {
    expect(distributePoints([0, 0, 0])).toEqual([0, 0, 0])
  })

  it('kommt mit einer leeren Liste zurecht', () => {
    expect(distributePoints([])).toEqual([])
  })

  // Nachgemessen statt vermutet: die Restverteilung greift über
  // candidates[step % candidates.length] auf die Kandidaten zu. Das sieht nach
  // einer Doppelvergabe aus, sobald der Rest größer wäre als die Zahl der
  // Kandidaten - kann aber nicht eintreten: jeder Eintrag verliert beim
  // Abrunden weniger als 1, der Rest ist also stets kleiner als die Anzahl.
  // Der Modulo ist Absicherung, kein Fehler. Das Backend rechnet in
  // EvaluationScorer.LargestRemainder identisch.
  it('vergibt keinen Rest doppelt, auch bei vielen Kategorien nicht', () => {
    const gewichte = Array.from({ length: 7 }, () => 1)
    const punkte = distributePoints(gewichte)

    expect(punkte.reduce((a, b) => a + b, 0)).toBe(100)

    // Kein Eintrag darf mehr als einen Punkt über dem Abrunden liegen.
    const exakt = 100 / 7
    for (const wert of punkte) {
      expect(wert).toBeLessThanOrEqual(Math.floor(exakt) + 1)
    }
  })
})

describe('toWeightValues', () => {
  it('nimmt die Standardwerte, wenn nichts hinterlegt ist', () => {
    expect(toWeightValues([])).toEqual(defaultWeights())
  })

  it('ueberschreibt nur die angegebenen Kategorien', () => {
    const werte = toWeightValues([{ category: 'Functionality', weight: 80 }])

    expect(werte).toEqual({ CleanCode: 15, Compilability: 20, Functionality: 80 })
  })

  // Die nicht mehr bewerteten Enum-Werte dürfen nicht in die Maske durchsickern:
  // ein Gewicht darauf würde nie gelesen und vom Backend abgelehnt.
  it('ignoriert nicht mehr bewertete Kategorien', () => {
    const werte = toWeightValues([
      { category: 'CharacterSet', weight: 99 },
      { category: 'CleanCode', weight: 30 },
    ])

    expect(werte).toEqual({ CleanCode: 30, Compilability: 20, Functionality: 65 })
  })

  it('behaelt Nachkommastellen', () => {
    expect(toWeightValues([{ category: 'CleanCode', weight: 33.75 }]).CleanCode).toBe(33.75)
  })
})
