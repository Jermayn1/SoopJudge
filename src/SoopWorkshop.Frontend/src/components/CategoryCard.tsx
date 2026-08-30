import { useEffect, useState } from 'react'
import { Check, ChevronDown, X } from 'lucide-react'
import type { CategoryResult, TestCaseResult } from '../api/types'

const CATEGORY_LABELS: Record<string, string> = {
  CleanCode: 'Clean Code',
  Compilability: 'Kompilierbarkeit',
  Functionality: 'Funktionalität',
  // Altlasten aus früheren Auswertungen — sie werden nicht mehr vergeben,
  // kommen in alten Ergebnissen aber noch vor und brauchen einen Namen.
  CharacterSet: 'Zeichensatz',
  NamingConventions: 'Namenskonventionen',
  TestCases: 'Testfälle',
  UnitTests: 'Unit-Tests',
}

// Zeigt eine Teilprüfung. Die Darstellungsregeln gelten für alle Quellen
// gleich — Checker, Konsolen-Testfälle und JUnit:
//
//   - Eingabe nur, wenn es eine gab.
//   - Erwartet und Erhalten immer gemeinsam. Ein "Erwartet" ohne Gegenstück
//     lässt den Leser raten; fehlt eine Seite, steht dort ein Gedankenstrich.
//   - Bestandene Prüfungen zeigen nichts.
//
// Andere Komponenten, die Teilprüfungen anzeigen, verweisen auf diese Regeln.
function TestCaseRow({ test, index }: { test: TestCaseResult; index: number }) {
  const hasComparison = test.expectedOutput !== '' || test.actualOutput !== ''

  return (
    <div
      className={`anim-links flex gap-3 p-3 rounded-xl border text-sm ${
        test.passed ? 'bg-emerald-50 border-emerald-100' : 'bg-rose-50 border-rose-100'
      }`}
      style={{ animationDelay: `${index * 50}ms` }}
    >
      <div
        className={`mt-0.5 shrink-0 w-5 h-5 rounded-full flex items-center justify-center ${
          test.passed ? 'bg-emerald-600' : 'bg-rose-600'
        }`}
      >
        {test.passed ? (
          <Check className="w-3 h-3 text-white" strokeWidth={3} aria-hidden="true" />
        ) : (
          <X className="w-3 h-3 text-white" strokeWidth={3} aria-hidden="true" />
        )}
      </div>

      <div className="min-w-0 flex-1">
        <p className={`font-medium ${test.passed ? 'text-emerald-900' : 'text-rose-900'}`}>
          {test.description}
        </p>

        {/* Bestandene Prüfungen zeigen nichts weiter — die Zustimmung steht
            schon im Haken. */}
        {!test.passed && (
          <dl className="mt-2 grid grid-cols-[5.5rem_1fr] gap-x-3 gap-y-1 font-mono text-xs">
            {test.input !== '' && (
              <>
                <dt className="text-slate-600">Eingabe</dt>
                <dd className="text-slate-800 whitespace-pre-wrap break-words">{test.input}</dd>
              </>
            )}
            {hasComparison && (
              <>
                <dt className="text-slate-600">Erwartet</dt>
                <dd className="text-emerald-800 whitespace-pre-wrap break-words">
                  {test.expectedOutput || '—'}
                </dd>
                <dt className="text-slate-600">Erhalten</dt>
                <dd className="text-rose-800 whitespace-pre-wrap break-words">
                  {test.actualOutput || '—'}
                </dd>
              </>
            )}
          </dl>
        )}
      </div>
    </div>
  )
}

export function CategoryCard({ result, delay }: { result: CategoryResult; delay: number }) {
  const [open, setOpen] = useState(!result.passed)
  const percentage = result.maxPoints > 0 ? Math.round((result.points / result.maxPoints) * 100) : 0
  const passedCount = result.testCaseResults.filter((t) => t.passed).length
  const total = result.testCaseResults.length
  const hasDetails = total > 0 || result.errorTip !== ''

  // Der Balken startet bei 0 und wächst erst nach dem ersten Anzeigen auf
  // seinen Wert — sonst gäbe es nichts zu sehen, weil die Breite von Anfang
  // an stimmen würde.
  const [barWidth, setBarWidth] = useState(0)
  useEffect(() => {
    const timer = window.setTimeout(() => setBarWidth(percentage), 60 + delay * 1000)
    return () => window.clearTimeout(timer)
  }, [percentage, delay])

  return (
    <div
      className={`anim-auf bg-white rounded-2xl border shadow-sm overflow-hidden transition-shadow ${
        open
          ? `${result.passed ? 'border-emerald-200' : 'border-rose-200'} shadow-md`
          : 'border-slate-200 hover:shadow-md'
      }`}
      style={{ animationDelay: `${delay * 1000}ms` }}
    >
      <button
        type="button"
        onClick={() => hasDetails && setOpen((o) => !o)}
        aria-expanded={hasDetails ? open : undefined}
        aria-controls={hasDetails ? `kategorie-detail-${result.id}` : undefined}
        className={`w-full flex items-center gap-4 px-5 py-4 text-left transition-colors ${
          hasDetails ? 'hover:bg-slate-50/70 cursor-pointer' : 'cursor-default'
        }`}
      >
        <div className="flex-1 min-w-0">
          <p className="text-slate-600 text-xs font-semibold uppercase tracking-widest mb-1">
            {CATEGORY_LABELS[result.category] ?? result.category}
          </p>
          <div className="flex items-center gap-3">
            <span className="text-lg font-bold text-slate-800 tabular-nums">
              {result.points}
              <span className="text-slate-500 text-sm font-normal"> / {result.maxPoints}</span>
            </span>
            <div className="flex-1 h-1.5 bg-slate-100 rounded-full overflow-hidden">
              <div
                className={`h-full rounded-full transition-[width] duration-700 ease-out ${
                  result.passed
                    ? 'bg-gradient-to-r from-emerald-400 to-emerald-600'
                    : 'bg-gradient-to-r from-rose-400 to-rose-600'
                }`}
                style={{ width: `${barWidth}%` }}
              />
            </div>
            {total > 0 && (
              <span className="text-xs font-semibold text-slate-700 tabular-nums shrink-0">
                {passedCount}/{total}
              </span>
            )}
          </div>
        </div>

        {hasDetails && (
          <ChevronDown
            className={`w-5 h-5 text-slate-500 shrink-0 transition-transform duration-200 ${
              open ? 'rotate-180' : ''
            }`}
            aria-hidden="true"
          />
        )}
      </button>

      {hasDetails && (
        <div
          id={`kategorie-detail-${result.id}`}
          className={`klapp ${open ? '' : 'klapp-zu'}`}
          inert={!open}
        >
          <div className="klapp-inhalt">
            <div className="px-5 pb-5 pt-4 border-t border-slate-100 space-y-2">
              {result.errorTip !== '' && (
                <p className="rounded-xl border border-slate-200 bg-slate-50 p-3 text-sm text-slate-700 whitespace-pre-wrap break-words">
                  {result.errorTip}
                </p>
              )}
              {result.testCaseResults.map((test, index) => (
                <TestCaseRow key={test.id} test={test} index={index} />
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
