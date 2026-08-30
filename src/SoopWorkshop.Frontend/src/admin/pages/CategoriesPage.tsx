import { useState } from 'react'
import { Check, Eye, EyeOff, Pencil, Plus, RefreshCw, Trash2, X } from 'lucide-react'
import { useAdminCatalog } from '../adminOutlet'
import { OrderButtons } from '../components/OrderButtons'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { IconPickerDialog } from '../components/IconPickerDialog'
import { iconByName } from '../icons'
import {
  createCategory,
  deleteCategory,
  toggleCategoryVisibility,
  updateCategory,
} from '../api/catalog'
import { FIELD_LIMITS, checkMaxLength, checkRequired, collect } from '../validation'
import { inputClass } from '../components/formStyles'
import type { Category } from '../../api/types'

export function CategoriesPage() {
  const { categories, loading, error, reload } = useAdminCatalog()

  const [busy, setBusy] = useState(false)
  const [problem, setProblem] = useState<string | null>(null)

  const [newName, setNewName] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState('')
  const [pendingDelete, setPendingDelete] = useState<Category | null>(null)
  const [pendingIcon, setPendingIcon] = useState<Category | null>(null)

  // Jeder schreibende Aufruf läuft hierdurch: einmal sperren, Meldung des
  // Servers im Wortlaut übernehmen, danach neu laden. Ohne das gemeinsame
  // Neuladen zeigt die Seitenleiste noch den alten Stand.
  const run = async (action: () => Promise<{ kind: string; message?: string }>) => {
    setBusy(true)
    setProblem(null)

    const result = await action()

    setBusy(false)

    if (result.kind !== 'ok') {
      setProblem(result.message ?? 'Der Vorgang ist fehlgeschlagen.')
      return false
    }

    reload()
    return true
  }

  const add = async () => {
    const problems = collect(
      checkRequired('name', 'Der Name', newName),
      checkMaxLength('name', 'Der Name', newName, FIELD_LIMITS.categoryName),
    )

    if (problems.length > 0) {
      setProblem(problems[0].message)
      return
    }

    // Ans Ende einsortieren. Verschieben geht danach über die Pfeile — eine
    // Ordnungszahl beim Anlegen einzutippen ist eine Frage, auf die niemand
    // eine sinnvolle Antwort hat.
    const nextOrder = categories.reduce((max, c) => Math.max(max, c.order), 0) + 1

    if (await run(() => createCategory(newName.trim(), nextOrder))) setNewName('')
  }

  const saveName = async (category: Category) => {
    const problems = collect(
      checkRequired('name', 'Der Name', editingName),
      checkMaxLength('name', 'Der Name', editingName, FIELD_LIMITS.categoryName),
    )

    if (problems.length > 0) {
      setProblem(problems[0].message)
      return
    }

    const ok = await run(() =>
      updateCategory({ ...category, name: editingName.trim() }),
    )
    if (ok) setEditingId(null)
  }

  // Tauscht die Ordnungszahlen der beiden Nachbarn. Zwei Aufrufe, dafür ohne
  // Umnummerieren der ganzen Liste.
  const move = async (index: number, direction: -1 | 1) => {
    const current = categories[index]
    const neighbour = categories[index + direction]
    if (!neighbour) return

    setBusy(true)
    setProblem(null)

    const first = await updateCategory({ ...current, order: neighbour.order })
    const second =
      first.kind === 'ok'
        ? await updateCategory({ ...neighbour, order: current.order })
        : first

    setBusy(false)

    if (second.kind !== 'ok') {
      setProblem(second.message)
      return
    }

    reload()
  }

  const confirmDelete = async () => {
    if (!pendingDelete) return
    const ok = await run(() => deleteCategory(pendingDelete.id))
    if (ok) setPendingDelete(null)
  }

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
      <div className="mx-auto w-full max-w-3xl">
        <h1 className="text-2xl font-bold text-slate-800">Kategorien</h1>
        <p className="mt-1 text-slate-600">
          Die Reihenfolge bestimmt, wie die Kategorien beim Teilnehmer erscheinen.
        </p>

        {problem && (
          <div
            role="alert"
            className="mt-6 rounded-xl border border-rose-200 bg-rose-50 p-4 text-rose-800"
          >
            {problem}
          </div>
        )}

        {/* Anlegen */}
        <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
          <label htmlFor="neue-kategorie" className="block text-sm font-semibold text-slate-700">
            Neue Kategorie
          </label>
          <div className="mt-1.5 flex gap-3">
            <input
              id="neue-kategorie"
              type="text"
              value={newName}
              onChange={(event) => setNewName(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') void add()
              }}
              placeholder="z. B. Schleifen"
              disabled={busy}
              className={inputClass(false)}
            />
            <button
              type="button"
              onClick={add}
              disabled={busy || newName.trim().length === 0}
              className="flex shrink-0 items-center gap-2 rounded-xl bg-indigo-600 px-4 py-2 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:shadow-none"
            >
              <Plus className="w-4 h-4" aria-hidden="true" />
              Anlegen
            </button>
          </div>
          <p className="mt-2 text-xs text-slate-500">
            Neue Kategorien sind zunächst verborgen und werden hier freigeschaltet.
          </p>
        </div>

        {loading && (
          <div className="mt-6 space-y-3" aria-hidden="true">
            {[0, 1, 2].map((i) => (
              <div key={i} className="h-16 rounded-2xl bg-slate-200 animate-pulse" />
            ))}
          </div>
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

        {!loading && !error && (
          <ul className="mt-6 space-y-3">
            {categories.map((category, index) => (
              <li
                key={category.id}
                className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm"
              >
                <OrderButtons
                  label={`Kategorie ${category.name}`}
                  onUp={() => move(index, -1)}
                  onDown={() => move(index, 1)}
                  canMoveUp={index > 0 && !busy}
                  canMoveDown={index < categories.length - 1 && !busy}
                />

                {editingId === category.id ? (
                  <>
                    <input
                      type="text"
                      value={editingName}
                      onChange={(event) => setEditingName(event.target.value)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter') void saveName(category)
                        if (event.key === 'Escape') setEditingId(null)
                      }}
                      aria-label="Name der Kategorie"
                      autoFocus
                      disabled={busy}
                      className={inputClass(false)}
                    />
                    <button
                      type="button"
                      onClick={() => saveName(category)}
                      disabled={busy}
                      aria-label="Namen übernehmen"
                      className="rounded-lg p-2 text-emerald-900 hover:bg-emerald-50"
                    >
                      <Check className="w-4 h-4" aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingId(null)}
                      aria-label="Abbrechen"
                      className="rounded-lg p-2 text-slate-600 hover:bg-slate-100"
                    >
                      <X className="w-4 h-4" aria-hidden="true" />
                    </button>
                  </>
                ) : (
                  <>
                    {/* Zeigt das aktuelle Symbol und ist zugleich der Knopf, um
                        es zu wechseln — ein getrennter "Bearbeiten"-Knopf dafür
                        wäre ein Klick mehr für dieselbe Sache. */}
                    {(() => {
                      const Icon = iconByName(category.iconName)
                      return (
                        <button
                          type="button"
                          onClick={() => setPendingIcon(category)}
                          disabled={busy}
                          aria-label={`Symbol für ${category.name} wählen`}
                          className="shrink-0 rounded-lg border border-slate-200 p-2 text-slate-600 transition-colors hover:border-slate-400 hover:bg-slate-50"
                        >
                          <Icon className="w-4 h-4" aria-hidden="true" />
                        </button>
                      )
                    })()}

                    <span className="min-w-0 flex-1 truncate font-semibold text-slate-800">
                      {category.name}
                    </span>
                    <span className="shrink-0 text-sm tabular-nums text-slate-500">
                      {category.tasks.length === 1
                        ? '1 Aufgabe'
                        : `${category.tasks.length} Aufgaben`}
                    </span>

                    <button
                      type="button"
                      onClick={() => run(() => toggleCategoryVisibility(category.id))}
                      disabled={busy}
                      className={`flex shrink-0 items-center gap-1 rounded-md px-2 py-1 text-xs font-semibold transition-colors ${
                        category.isVisible
                          ? 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                          : 'bg-amber-50 text-amber-900 ring-1 ring-amber-200 hover:bg-amber-100'
                      }`}
                    >
                      {category.isVisible ? (
                        <Eye className="w-3.5 h-3.5" aria-hidden="true" />
                      ) : (
                        <EyeOff className="w-3.5 h-3.5" aria-hidden="true" />
                      )}
                      {category.isVisible ? 'Sichtbar' : 'Verborgen'}
                    </button>

                    <button
                      type="button"
                      onClick={() => {
                        setEditingId(category.id)
                        setEditingName(category.name)
                      }}
                      disabled={busy}
                      aria-label={`${category.name} umbenennen`}
                      className="rounded-lg p-2 text-slate-600 hover:bg-slate-100"
                    >
                      <Pencil className="w-4 h-4" aria-hidden="true" />
                    </button>

                    <button
                      type="button"
                      onClick={() => setPendingDelete(category)}
                      disabled={busy}
                      aria-label={`${category.name} löschen`}
                      className="rounded-lg p-2 text-rose-800 hover:bg-rose-50"
                    >
                      <Trash2 className="w-4 h-4" aria-hidden="true" />
                    </button>
                  </>
                )}
              </li>
            ))}

            {categories.length === 0 && (
              <li className="rounded-2xl border border-dashed border-slate-300 bg-white p-10 text-center text-slate-600">
                Noch keine Kategorie angelegt.
              </li>
            )}
          </ul>
        )}
      </div>

      {pendingIcon && (
        <IconPickerDialog
          value={pendingIcon.iconName}
          categoryName={pendingIcon.name}
          onCancel={() => setPendingIcon(null)}
          onSelect={async (iconName) => {
            const ziel = pendingIcon
            setPendingIcon(null)
            await run(() => updateCategory({ ...ziel, iconName }))
          }}
        />
      )}

      {pendingDelete && (
        <ConfirmDialog
          title={`„${pendingDelete.name}“ löschen?`}
          // Der Umfang gehört in den Dialog, nicht in eine Fußnote: das
          // Löschen nimmt per Cascade den ganzen Teilbaum mit, inklusive der
          // Abgaben der Teilnehmer und deren Auswertungen.
          message={
            pendingDelete.tasks.length === 0
              ? 'Die Kategorie ist leer und wird gelöscht.'
              : `Damit werden auch ${pendingDelete.tasks.length} ${
                  pendingDelete.tasks.length === 1 ? 'Aufgabe' : 'Aufgaben'
                } gelöscht — samt Testfällen, JUnit-Dateien und allen bereits abgegebenen Lösungen. Das lässt sich nicht rückgängig machen.`
          }
          confirmLabel="Endgültig löschen"
          busy={busy}
          onConfirm={confirmDelete}
          onCancel={() => setPendingDelete(null)}
        />
      )}
    </div>
  )
}
