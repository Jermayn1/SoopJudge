import { Check, Loader2, Save } from 'lucide-react'
import type { SaveState } from '../saveState'

type SaveBarProps = {
  state: SaveState
  /** Ob es überhaupt etwas zu speichern gibt. */
  dirty: boolean
  onSave: () => void
  onReset?: () => void
}

// Leiste am unteren Rand eines Formulars. Klebt beim Scrollen fest, damit der
// Speichern-Knopf bei einem langen Formular nicht außer Sicht gerät.
export function SaveBar({ state, dirty, onSave, onReset }: SaveBarProps) {
  const saving = state.kind === 'saving'

  return (
    <div className="sticky bottom-0 -mx-8 mt-8 border-t border-slate-200 bg-white/95 px-8 py-4 backdrop-blur">
      {state.kind === 'error' && (
        <p
          role="alert"
          className="mb-3 rounded-xl border border-rose-200 bg-rose-50 p-3 text-sm text-rose-800"
        >
          {state.message}
        </p>
      )}

      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={onSave}
          disabled={saving || !dirty}
          className="flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 hover:-translate-y-0.5 active:translate-y-0 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:shadow-none disabled:hover:translate-y-0"
        >
          {saving ? (
            <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" />
          ) : (
            <Save className="w-4 h-4" aria-hidden="true" />
          )}
          {saving ? 'Wird gespeichert …' : 'Speichern'}
        </button>

        {onReset && dirty && !saving && (
          <button
            type="button"
            onClick={onReset}
            className="rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
          >
            Änderungen verwerfen
          </button>
        )}

        {/* aria-live, damit die Rückmeldung auch vorgelesen wird — sie ist die
            einzige Bestätigung, dass der Klick angekommen ist. */}
        <span role="status" aria-live="polite" className="ml-auto text-sm">
          {state.kind === 'saved' && !dirty && (
            <span className="flex items-center gap-1.5 font-medium text-emerald-900">
              <Check className="w-4 h-4" aria-hidden="true" />
              Gespeichert
            </span>
          )}
          {dirty && state.kind !== 'saving' && (
            <span className="text-slate-500">Nicht gespeicherte Änderungen</span>
          )}
        </span>
      </div>
    </div>
  )
}
