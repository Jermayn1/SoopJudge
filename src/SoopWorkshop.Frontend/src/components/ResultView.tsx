import { useEffect, useState } from 'react'
import { Trophy } from 'lucide-react'
import { CategoryCard } from './CategoryCard'
import type { EvaluationCategory, EvaluationResult } from '../api/types'

// Anzeigereihenfolge wie in SoopWorkshop.Shared/Constants/EvaluationCategoryOrder.cs.
// Die API liefert bereits sortiert; das Frontend sortiert trotzdem selbst.
const CATEGORY_ORDER: EvaluationCategory[] = ['CleanCode', 'Compilability', 'Functionality']

function orderOf(category: EvaluationCategory): number {
  const index = CATEGORY_ORDER.indexOf(category)
  return index < 0 ? CATEGORY_ORDER.length : index
}

// Zählt von 0 auf den Zielwert. Bewusst mit requestAnimationFrame statt über
// eine Animationsbibliothek — das sind zwanzig Zeilen und kann nicht dazu
// führen, dass am Ende etwas Unsichtbares stehen bleibt.
function useCountUp(target: number, durationMs = 1100, delayMs = 250): number {
  const [value, setValue] = useState(0)

  useEffect(() => {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setValue(target)
      return
    }

    let frame = 0
    let start = 0
    const step = (now: number) => {
      if (start === 0) start = now
      const elapsed = now - start - delayMs
      if (elapsed < 0) {
        frame = requestAnimationFrame(step)
        return
      }
      const fortschritt = Math.min(elapsed / durationMs, 1)
      // easeOutCubic — schnell los, sanft ankommen
      setValue(Math.round(target * (1 - Math.pow(1 - fortschritt, 3))))
      if (fortschritt < 1) frame = requestAnimationFrame(step)
    }

    frame = requestAnimationFrame(step)
    return () => cancelAnimationFrame(frame)
  }, [target, durationMs, delayMs])

  return value
}

function ScoreCircle({ score }: { score: number }) {
  const gezeigt = useCountUp(score)
  const great = score >= 80

  // Der hellste Punkt des Verlaufs trägt die weiße Zahl und muss deshalb
  // 3:1 halten (Großschrift). Nachgemessen: emerald-500 lag bei 2,47:1,
  // emerald-600 liegt bei 3,65:1. Blau (3,76) und Rot (3,75) passen ab -500.
  const gradient = great
    ? 'from-emerald-600 to-emerald-800'
    : score >= 50
      ? 'from-blue-500 to-blue-700'
      : 'from-rose-500 to-rose-700'

  return (
    <div className="anim-feder relative inline-block mb-6">
      <div
        className={`w-36 h-36 rounded-full bg-gradient-to-br ${gradient} flex items-center justify-center shadow-2xl ring-8 ring-white`}
      >
        <div className="text-white text-center">
          <div className="text-5xl font-black tabular-nums leading-none">{gezeigt}</div>
          <div className="text-xs font-semibold uppercase tracking-widest opacity-90 mt-1">
            von 100
          </div>
        </div>
      </div>
      {great && (
        <div className="anim-wackeln absolute -top-3 -right-3 bg-amber-500 w-11 h-11 rounded-full flex items-center justify-center shadow-lg border-4 border-white">
          <Trophy className="w-5 h-5 text-white" aria-hidden="true" />
        </div>
      )}
    </div>
  )
}

type ResultViewProps = {
  result: EvaluationResult
}

// Die Auswertung, wie der Teilnehmer sie liest.
//
// Eigene Komponente statt Teil von ResultPage, damit der Probelauf im
// Verwaltungsbereich dieselbe Darstellung benutzt — inklusive der Regeln für
// eine Teilprüfung, die in CategoryCard stehen. Eine zweite, nachgebaute
// Anzeige würde früher oder später etwas anderes zeigen als das, was der
// Teilnehmer sieht, und genau das soll der Probelauf ja verhindern.
export function ResultView({ result }: ResultViewProps) {
  const categories = [...result.categoryResults].sort(
    (a, b) => orderOf(a.category) - orderOf(b.category),
  )

  const allTests = categories.flatMap((c) => c.testCaseResults)
  const passed = allTests.filter((t) => t.passed).length
  const open = allTests.length - passed

  const headline =
    result.totalScore >= 80
      ? 'Hervorragende Arbeit!'
      : result.totalScore >= 50
        ? 'Guter Versuch!'
        : 'Da geht noch mehr!'

  return (
    <>
      <div className="text-center mb-10">
        <ScoreCircle score={result.totalScore} />
        <h1
          className="anim-auf text-3xl font-extrabold text-slate-900 mb-2"
          style={{ animationDelay: '380ms' }}
        >
          {headline}
        </h1>
        <p className="anim-ein text-slate-600 text-sm" style={{ animationDelay: '480ms' }}>
          {open === 0
            ? 'Alle Teilprüfungen bestanden.'
            : `${passed} bestanden, ${open} offen. Klapp die Kategorien auf, um zu sehen, woran es liegt.`}
        </p>
      </div>

      <div className="space-y-3">
        {categories.map((category, index) => (
          <CategoryCard key={category.id} result={category} delay={0.1 + index * 0.1} />
        ))}
      </div>
    </>
  )
}
