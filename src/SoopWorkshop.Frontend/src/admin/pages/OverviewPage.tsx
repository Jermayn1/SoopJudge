import { Link } from 'react-router-dom'
import { Eye, EyeOff, PencilLine, Plus, RefreshCw } from 'lucide-react'
import { useAdminCatalog } from '../adminOutlet'
import { DIFFICULTY_CLASSES, DIFFICULTY_LABELS, MODE_LABELS } from '../../api/labels'
import { iconByName } from '../icons'
import type { Category } from '../../api/types'

// Kleines Schild für den Sichtbarkeitszustand. Bewusst mit Symbol UND Wort:
// eine Farbe allein ist keine Auskunft, und Grün/Rot sind im Projekt für
// Bewertungen reserviert.
function VisibilityBadge({ visible }: { visible: boolean }) {
  return visible ? (
    <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-700">
      <Eye className="w-3.5 h-3.5" aria-hidden="true" />
      Sichtbar
    </span>
  ) : (
    <span className="inline-flex items-center gap-1 rounded-md bg-amber-50 px-2 py-0.5 text-xs font-semibold text-amber-900 ring-1 ring-amber-200">
      <EyeOff className="w-3.5 h-3.5" aria-hidden="true" />
      Verborgen
    </span>
  )
}

function CategoryBlock({ category, index }: { category: Category; index: number }) {
  const Icon = iconByName(category.iconName)

  return (
    <section
      className="rounded-2xl border border-slate-200 bg-white shadow-sm anim-auf"
      style={{ animationDelay: `${index * 50}ms` }}
    >
      <header className="flex flex-wrap items-center gap-3 border-b border-slate-200 px-5 py-4">
        <Icon className="w-5 h-5 shrink-0 text-slate-500" aria-hidden="true" />
        <h2 className="text-lg font-bold text-slate-800">{category.name}</h2>
        <VisibilityBadge visible={category.isVisible} />
        <span className="ml-auto text-sm tabular-nums text-slate-500">
          {category.tasks.length === 1 ? '1 Aufgabe' : `${category.tasks.length} Aufgaben`}
        </span>

        {/* Die Kategorie kommt als Vorauswahl mit — angelegt wird meistens dort,
            wo man gerade hinsieht. */}
        <Link
          to={`/admin/aufgaben/neu?kategorie=${category.id}`}
          className="flex items-center gap-1 rounded-lg border border-slate-300 px-2 py-1 text-xs font-semibold text-slate-700 transition-colors hover:bg-slate-100"
        >
          <Plus className="w-3.5 h-3.5" aria-hidden="true" />
          Aufgabe
        </Link>
      </header>

      {category.tasks.length === 0 ? (
        <p className="px-5 py-6 text-sm italic text-slate-500">
          In dieser Kategorie gibt es noch keine Aufgaben.
        </p>
      ) : (
        <ul className="divide-y divide-slate-100">
          {category.tasks.map((task) => (
            <li key={task.id}>
              <Link
                to={`/admin/aufgaben/${task.id}`}
                className="flex flex-wrap items-center gap-3 px-5 py-3 transition-colors hover:bg-slate-50"
              >
                <span className="w-6 shrink-0 text-sm tabular-nums text-slate-400">
                  {task.order}
                </span>
                <span className="font-medium text-slate-800">{task.title}</span>

                <span
                  className={`rounded-md px-2 py-0.5 text-xs font-semibold ${DIFFICULTY_CLASSES[task.difficulty]}`}
                >
                  {DIFFICULTY_LABELS[task.difficulty]}
                </span>

                <span className="rounded-md bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-700">
                  {MODE_LABELS[task.evaluationMode]}
                </span>

                <span className="ml-auto flex items-center gap-3">
                  <VisibilityBadge visible={task.isVisible} />
                  <PencilLine className="w-4 h-4 text-slate-400" aria-hidden="true" />
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export function OverviewPage() {
  const { categories, loading, error, reload } = useAdminCatalog()

  const taskCount = categories.reduce((sum, category) => sum + category.tasks.length, 0)
  const visibleTaskCount = categories.reduce(
    (sum, category) => sum + category.tasks.filter((task) => task.isVisible).length,
    0,
  )

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
      <div className="mx-auto w-full max-w-4xl">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold text-slate-800">Übersicht</h1>
          {categories.length > 0 && (
            <Link
              to="/admin/aufgaben/neu"
              className="ml-auto flex items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0"
            >
              <Plus className="w-4 h-4" aria-hidden="true" />
              Neue Aufgabe
            </Link>
          )}
        </div>

        {/* Die Zahl steht erst da, wenn sie stimmt. Während des Ladens "0 von
            0 Aufgaben" anzuzeigen wäre eine Falschaussage, keine Ladeanzeige. */}
        {!loading && !error && (
          <p className="mt-1 text-slate-600">
            {categories.length === 1 ? '1 Kategorie' : `${categories.length} Kategorien`},{' '}
            <span className="tabular-nums">{taskCount}</span>{' '}
            {taskCount === 1 ? 'Aufgabe' : 'Aufgaben'}, davon{' '}
            <span className="tabular-nums">{visibleTaskCount}</span> für Teilnehmer sichtbar.
          </p>
        )}

        {loading && (
          <div className="mt-6 space-y-4" aria-hidden="true">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-28 rounded-2xl bg-slate-200 animate-pulse" />
            ))}
          </div>
        )}
        {loading && (
          <span className="sr-only" role="status" aria-live="polite">
            Aufgabenbestand wird geladen.
          </span>
        )}

        {error && !loading && (
          <div
            role="alert"
            className="mt-6 rounded-xl border border-rose-200 bg-rose-50 p-4 text-rose-800"
          >
            <p>{error}</p>
            <button
              type="button"
              onClick={reload}
              className="mt-3 flex items-center gap-1.5 text-sm font-semibold text-rose-800 hover:underline"
            >
              <RefreshCw className="w-4 h-4" aria-hidden="true" />
              Erneut versuchen
            </button>
          </div>
        )}

        {!loading && !error && categories.length === 0 && (
          <div className="mt-6 rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center">
            <p className="font-medium text-slate-700">Noch nichts angelegt.</p>
            <p className="mt-1 text-sm text-slate-500">
              Eine Aufgabe gehört immer in eine Kategorie — leg zuerst eine an.
            </p>
            <Link
              to="/admin/kategorien"
              className="mt-6 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700"
            >
              Zu den Kategorien
            </Link>
          </div>
        )}

        {!loading && !error && categories.length > 0 && (
          <div className="mt-6 space-y-4">
            {categories.map((category, index) => (
              <CategoryBlock key={category.id} category={category} index={index} />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
