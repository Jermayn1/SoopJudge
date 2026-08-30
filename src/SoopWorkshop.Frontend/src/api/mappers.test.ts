import { describe, expect, it } from 'vitest'
import {
  toCategory,
  toEvaluationResult,
  toSubmissionState,
  toTask,
  toTaskCategoryWeight,
} from './mappers'

describe('Zahlen aus dem Vertrag', () => {
  // .NET gibt int32 im OpenAPI-Dokument als "integer | string" an, weil ASP.NET
  // beim Binden auch Zahlen als Zeichenkette annimmt. Der Mapper behandelt
  // beide Fälle, statt es zu glauben.
  it.each([
    [7, 7],
    ['7', 7],
  ])('nimmt %j als Reihenfolge und macht %i daraus', (eingabe, erwartet) => {
    expect(toTask({ order: eingabe }).order).toBe(erwartet)
  })

  it('faellt bei fehlender Angabe auf 0 zurueck', () => {
    expect(toTask({}).order).toBe(0)
  })

  it('faellt bei unlesbarer Zeichenkette auf 0 zurueck', () => {
    expect(toTask({ order: 'keine Zahl' }).order).toBe(0)
  })

  // Gewichte sind Kommazahlen. Gingen sie durch dieselbe Umsetzung wie die
  // ganzzahligen Felder, schnitte parseInt die Nachkommastellen ab - aus 33,75
  // würde 33, und die Normierung ergäbe stillschweigend etwas anderes.
  it('behaelt beim Gewicht die Nachkommastellen', () => {
    expect(toTaskCategoryWeight({ weight: 33.75 }).weight).toBe(33.75)
  })

  // Ohne diesen Ersatzwert stünde auf der Ergebnisseite "80 von 0".
  it('nimmt fuer die erreichbare Punktzahl ersatzweise 100', () => {
    expect(toEvaluationResult({ totalScore: 80 }).maxScore).toBe(100)
  })
})

describe('Standardwerte', () => {
  // Der Vertrag kennt kein "required", also ist dort jedes Feld optional. Ohne
  // diese Schicht stünde in jeder Komponente ein ?? ''.
  it('macht aus einem leeren Vertragsobjekt einen vollstaendigen Typ', () => {
    const task = toTask({})

    expect(task).toEqual({
      id: '',
      categoryId: '',
      title: '',
      description: '',
      difficulty: 'Easy',
      order: 0,
      isVisible: false,
      evaluationMode: 'ConsoleOnly',
      expectedTypes: [],
      hints: [],
      visibleUnitTestFiles: [],
    })
  })

  it('setzt bei der Abgabe den Stand ersatzweise auf Pending', () => {
    expect(toSubmissionState({}).status).toBe('Pending')
  })
})

describe('Sortierung', () => {
  // Die API liefert bereits sortiert. Das Frontend sortiert trotzdem selbst:
  // Sortieren ist billig, eine wechselnde Reihenfolge verwirrt.
  it('sortiert Tipps, erwartete Typen und JUnit-Dateien nach Order', () => {
    const task = toTask({
      hints: [
        { id: 'b', content: 'Zweiter', order: 2 },
        { id: 'a', content: 'Erster', order: 1 },
      ],
      expectedTypes: [
        { id: 'y', name: 'Kunde', order: 2, methods: [] },
        { id: 'x', name: 'Konto', order: 1, methods: [] },
      ],
      visibleUnitTestFiles: [
        { id: 'n', fileName: 'ZweiTest.java', order: 2 },
        { id: 'm', fileName: 'EinsTest.java', order: 1 },
      ],
    })

    expect(task.hints.map((h) => h.content)).toEqual(['Erster', 'Zweiter'])
    expect(task.expectedTypes.map((t) => t.name)).toEqual(['Konto', 'Kunde'])
    expect(task.visibleUnitTestFiles.map((f) => f.fileName)).toEqual([
      'EinsTest.java',
      'ZweiTest.java',
    ])
  })

  it('sortiert die Aufgaben einer Kategorie nach Order', () => {
    const category = toCategory({
      tasks: [
        { id: 'b', title: 'Zweite', order: 2 },
        { id: 'a', title: 'Erste', order: 1 },
      ],
    })

    expect(category.tasks.map((t) => t.title)).toEqual(['Erste', 'Zweite'])
  })

  it('sortiert die Teilpruefungen einer Kategorie nach Order', () => {
    const ergebnis = toEvaluationResult({
      categoryResults: [
        {
          id: 'c',
          category: 'Functionality',
          testCaseResults: [
            { id: 'b', description: 'Zweite', order: 2 },
            { id: 'a', description: 'Erste', order: 1 },
          ],
        },
      ],
    })

    expect(ergebnis.categoryResults[0].testCaseResults.map((t) => t.description)).toEqual([
      'Erste',
      'Zweite',
    ])
  })

  // **Ist-Verhalten, bewusst so**: die Kategorien selbst werden hier NICHT
  // sortiert. Ihre Reihenfolge ist keine Zahl aus der Datenbank, sondern die
  // feste Anzeigereihenfolge aus EvaluationCategoryOrder - die kennt ResultView
  // und wendet sie dort an.
  it('laesst die Reihenfolge der Kategorien unangetastet', () => {
    const ergebnis = toEvaluationResult({
      categoryResults: [
        { id: 'a', category: 'Functionality' },
        { id: 'b', category: 'CleanCode' },
      ],
    })

    expect(ergebnis.categoryResults.map((c) => c.category)).toEqual([
      'Functionality',
      'CleanCode',
    ])
  })
})
