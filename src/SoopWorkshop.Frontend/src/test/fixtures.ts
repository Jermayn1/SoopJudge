import type { CategoryResult, EvaluationResult, TestCaseResult } from '../api/types'

// Alle Felder in api/types.ts sind nicht-optional - das ist Absicht (der
// Mapper füllt sie), macht aber jedes Testobjekt von Hand unnötig lang.
// Diese Bauer setzen die Pflichtfelder und lassen überschreiben, was der
// jeweilige Test wirklich aussagt.

export function teilpruefung(overrides: Partial<TestCaseResult> = {}): TestCaseResult {
  return {
    id: crypto.randomUUID(),
    description: 'Das Programm gibt den Gruss aus',
    input: '',
    expectedOutput: '',
    actualOutput: '',
    passed: true,
    order: 1,
    ...overrides,
  }
}

export function kategorie(overrides: Partial<CategoryResult> = {}): CategoryResult {
  return {
    id: crypto.randomUUID(),
    category: 'Functionality',
    passed: true,
    points: 65,
    maxPoints: 65,
    errorTip: '',
    testCaseResults: [],
    ...overrides,
  }
}

export function auswertung(overrides: Partial<EvaluationResult> = {}): EvaluationResult {
  return {
    id: crypto.randomUUID(),
    submissionId: crypto.randomUUID(),
    totalScore: 100,
    maxScore: 100,
    categoryResults: [],
    ...overrides,
  }
}
