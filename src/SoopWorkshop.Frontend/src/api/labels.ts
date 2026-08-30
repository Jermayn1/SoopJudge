import type { Difficulty, EvaluationMode } from './types'

// Deutsche Beschriftungen der Enums aus dem Vertrag.
//
// Herausgezogen aus TaskPage, weil die Verwaltung dieselben Wörter braucht.
// Zwei Listen nebeneinander laufen früher oder später auseinander, und dann
// heißt dieselbe Aufgabe an zwei Stellen verschieden schwer.
export const DIFFICULTY_LABELS: Record<Difficulty, string> = {
  Easy: 'Leicht',
  Medium: 'Mittel',
  Hard: 'Schwer',
}

export const DIFFICULTY_CLASSES: Record<Difficulty, string> = {
  Easy: 'bg-emerald-100 text-emerald-800',
  Medium: 'bg-amber-100 text-amber-800',
  Hard: 'bg-rose-100 text-rose-800',
}

export const MODE_LABELS: Record<EvaluationMode, string> = {
  ConsoleOnly: 'Konsole',
  UnitTestOnly: 'Unit-Tests',
  Both: 'Konsole + Unit-Tests',
}
