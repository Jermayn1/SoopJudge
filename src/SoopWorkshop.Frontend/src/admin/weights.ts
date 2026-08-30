import type { EvaluationCategory } from '../api/types'

// Nur die drei Kategorien, die noch bewertet werden — in der Anzeigereihenfolge
// aus EvaluationCategoryOrder. Die übrigen Werte von EvaluationCategory stehen
// nur noch wegen der Altdaten im Enum und werden vom Backend abgelehnt.
export const WEIGHTED_CATEGORIES = [
  { category: 'CleanCode' as const, label: 'Clean Code' },
  { category: 'Compilability' as const, label: 'Kompilierbarkeit' },
  { category: 'Functionality' as const, label: 'Funktionalität' },
]

export type WeightValues = Record<'CleanCode' | 'Compilability' | 'Functionality', number>

// Bildet DistributeMaxPoints aus dem EvaluationScorer nach: Gewichte auf 100
// normieren, abrunden, den Rest nach größtem Nachkommaanteil verteilen, bei
// Gleichstand nach Position. Bewusst dieselbe Rechnung — eine Vorschau, die
// anders rundet als die Bewertung, ist schlimmer als gar keine.
export function distributePoints(weights: number[]): number[] {
  const total = weights.reduce((sum, weight) => sum + weight, 0)
  if (total <= 0) return weights.map(() => 0)

  const exact = weights.map((weight) => (weight / total) * 100)
  const result = exact.map((value) => Math.floor(value))
  const remainder = 100 - result.reduce((sum, value) => sum + value, 0)

  const candidates = exact
    .map((value, index) => ({ index, fraction: value - Math.floor(value) }))
    .sort((a, b) => b.fraction - a.fraction || a.index - b.index)

  for (let step = 0; step < remainder && candidates.length > 0; step++) {
    result[candidates[step % candidates.length].index]++
  }

  return result
}

// Die Standardwerte aus Evaluation:CategoryWeights. Sie gelten, solange die
// Aufgabe keine eigenen Gewichte hinterlegt hat.
export function defaultWeights(): WeightValues {
  return { CleanCode: 15, Compilability: 20, Functionality: 65 }
}

export function toWeightValues(
  entries: { category: EvaluationCategory; weight: number }[],
): WeightValues {
  const values = defaultWeights()

  for (const entry of entries) {
    if (entry.category in values) {
      values[entry.category as keyof WeightValues] = entry.weight
    }
  }

  return values
}
