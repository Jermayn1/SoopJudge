import { useEffect, useState } from 'react'
import { Check, ChevronDown, X } from 'lucide-react'
import type { CategoryResult, TestCaseComparison, TestCaseResult } from '../api/types'

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
//   - Bestandene Prüfungen zeigen ihre Details erst auf Klick. Bei 100 Punkten
//     soll die Seite ruhig bleiben, aber wer sehen will, womit getestet wurde,
//     kommt heran — danach wurde im Workshop oft gefragt.
//   - Eine JUnit-Prüfung vergleicht oft mehrere Werte. Jeder Vergleich steht
//     für sich, mit dem Aufruf, der ihn geliefert hat.
//
// Andere Komponenten, die Teilprüfungen anzeigen, verweisen auf diese Regeln.

// Rot hieße "falsch". Was bestanden hat, steht in derselben Farbe wie die
// Erwartung.
function actualClass(passed: boolean): string {
  return passed ? 'text-emerald-800 dark:text-emerald-300' : 'text-rose-800 dark:text-rose-300'
}

function ComparisonDetails({ comparison }: { comparison: TestCaseComparison }) {
  return (
    <dl className="grid grid-cols-[5.5rem_1fr] gap-x-3 gap-y-1 py-2 font-mono text-xs first:pt-0 last:pb-0">
      {comparison.call !== '' && (
        <>
          <dt className="text-slate-600 dark:text-neutral-400">Aufruf</dt>
          <dd className="text-slate-800 whitespace-pre-wrap break-words dark:text-neutral-100">{comparison.call}</dd>
        </>
      )}
      <dt className="text-slate-600 dark:text-neutral-400">Erwartet</dt>
      <dd className="text-emerald-800 whitespace-pre-wrap break-words dark:text-emerald-300">
        {comparison.expected || '—'}
      </dd>
      <dt className="text-slate-600 dark:text-neutral-400">Erhalten</dt>
      <dd className={`whitespace-pre-wrap break-words ${actualClass(comparison.passed)}`}>
        {comparison.actual || '—'}
      </dd>
    </dl>
  )
}

// Die Vergleiche zeigen, wenn sie die Prüfung erklären: bei einer bestandenen
// immer, bei einer durchgefallenen nur, wenn einer von ihnen gescheitert ist.
// Fiel sie wegen einer Ausnahme in der Abgabe durch, steht der Grund in der
// Meldung und nicht in den Vergleichen davor.
function showsComparisons(test: TestCaseResult): boolean {
  if (test.comparisons.length === 0) return false
  return test.passed || test.comparisons.some((comparison) => !comparison.passed)
}

function TestCaseDetails({ test }: { test: TestCaseResult }) {
  if (showsComparisons(test)) {
    return (
      <div className="divide-y divide-slate-200 dark:divide-white/10">
        {test.comparisons.map((comparison, index) => (
          <ComparisonDetails key={index} comparison={comparison} />
        ))}
      </div>
    )
  }

  const hasComparison = test.expectedOutput !== '' || test.actualOutput !== ''

  return (
    <dl className="grid grid-cols-[5.5rem_1fr] gap-x-3 gap-y-1 font-mono text-xs">
      {test.input !== '' && (
        <>
          <dt className="text-slate-600 dark:text-neutral-400">Eingabe</dt>
          <dd className="text-slate-800 whitespace-pre-wrap break-words dark:text-neutral-100">{test.input}</dd>
        </>
      )}
      {hasComparison && (
        <>
          <dt className="text-slate-600 dark:text-neutral-400">Erwartet</dt>
          <dd className="text-emerald-800 whitespace-pre-wrap break-words dark:text-emerald-300">
            {test.expectedOutput || '—'}
          </dd>
          <dt className="text-slate-600 dark:text-neutral-400">Erhalten</dt>
          <dd className={`whitespace-pre-wrap break-words ${actualClass(test.passed)}`}>
            {test.actualOutput || '—'}
          </dd>
        </>
      )}
    </dl>
  )
}

function TestCaseRow({ test, index }: { test: TestCaseResult; index: number }) {
  const [open, setOpen] = useState(false)

  // Aufklappbar nur, wenn es etwas zu vergleichen gibt. An der Erwartung
  // hängt das, nicht am Erhaltenen: beim Kompilieren steht dort die
  // Compilerausgabe, und die kann auch bei Erfolg Warnungen enthalten.
  // JUnit-Prüfungen bringen ihre Vergleiche mit.
  const expandable =
    test.passed && (test.input !== '' || test.expectedOutput !== '' || test.comparisons.length > 0)
  const detailId = `teilpruefung-detail-${test.id}`

  return (
    <div
      className={`anim-links flex gap-3 p-3 rounded-xl border text-sm ${
        test.passed ? 'bg-emerald-50 border-emerald-100 dark:bg-emerald-500/10 dark:border-emerald-400/20' : 'bg-rose-50 border-rose-100 dark:bg-rose-500/10 dark:border-rose-400/20'
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
        {expandable ? (
          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            aria-expanded={open}
            aria-controls={detailId}
            className="group flex w-full items-start gap-2 text-left font-medium text-emerald-900 dark:text-emerald-200"
          >
            <span className="flex-1">{test.description}</span>
            <ChevronDown
              className={`mt-0.5 w-4 h-4 shrink-0 text-emerald-700 transition-transform duration-200 group-hover:text-emerald-900 dark:text-emerald-300 dark:group-hover:text-emerald-100 ${
                open ? '' : '-rotate-90'
              }`}
              aria-hidden="true"
            />
          </button>
        ) : (
          <p className={`font-medium ${test.passed ? 'text-emerald-900 dark:text-emerald-200' : 'text-rose-900 dark:text-rose-200'}`}>
            {test.description}
          </p>
        )}

        {!test.passed && (
          <div className="mt-2">
            <TestCaseDetails test={test} />
          </div>
        )}

        {/* Dieselbe Klapp-Mechanik wie bei den Kategorien. Der Abstand steht
            als Innenabstand im geklappten Teil, damit er mit zuklappt. */}
        {expandable && (
          <div id={detailId} className={`klapp ${open ? '' : 'klapp-zu'}`} inert={!open}>
            <div className="klapp-inhalt">
              <div className="pt-2">
                <TestCaseDetails test={test} />
              </div>
            </div>
          </div>
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
      className={`anim-auf bg-white rounded-2xl border shadow-sm overflow-hidden transition-shadow dark:bg-neutral-900 ${
        open
          ? `${result.passed ? 'border-emerald-200 dark:border-emerald-400/30' : 'border-rose-200 dark:border-rose-400/30'} shadow-md`
          : 'border-slate-200 hover:shadow-md dark:border-white/10'
      }`}
      style={{ animationDelay: `${delay * 1000}ms` }}
    >
      <button
        type="button"
        onClick={() => hasDetails && setOpen((o) => !o)}
        aria-expanded={hasDetails ? open : undefined}
        aria-controls={hasDetails ? `kategorie-detail-${result.id}` : undefined}
        className={`w-full flex items-center gap-4 px-5 py-4 text-left transition-colors ${
          hasDetails ? 'hover:bg-slate-50/70 cursor-pointer dark:hover:bg-neutral-800/50' : 'cursor-default'
        }`}
      >
        <div className="flex-1 min-w-0">
          <p className="text-slate-600 text-xs font-semibold uppercase tracking-widest mb-1 dark:text-neutral-400">
            {CATEGORY_LABELS[result.category] ?? result.category}
          </p>
          <div className="flex items-center gap-3">
            <span className="text-lg font-bold text-slate-800 tabular-nums dark:text-neutral-100">
              {result.points}
              <span className="text-slate-500 text-sm font-normal dark:text-neutral-400"> / {result.maxPoints}</span>
            </span>
            <div className="flex-1 h-1.5 bg-slate-100 rounded-full overflow-hidden dark:bg-neutral-800">
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
              <span className="text-xs font-semibold text-slate-700 tabular-nums shrink-0 dark:text-neutral-300">
                {passedCount}/{total}
              </span>
            )}
          </div>
        </div>

        {hasDetails && (
          <ChevronDown
            className={`w-5 h-5 text-slate-500 shrink-0 transition-transform duration-200 dark:text-neutral-400 ${
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
            <div className="px-5 pb-5 pt-4 border-t border-slate-100 space-y-2 dark:border-white/5">
              {result.errorTip !== '' && (
                <p className="rounded-xl border border-slate-200 bg-slate-50 p-3 text-sm text-slate-700 whitespace-pre-wrap break-words dark:border-white/10 dark:bg-neutral-950/60 dark:text-neutral-300">
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
