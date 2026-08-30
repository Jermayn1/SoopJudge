import { useEffect, useMemo, useRef, useState } from 'react'
import { Ban, Search, X } from 'lucide-react'
import { filterIcons, ICON_COUNT } from '../icons'
import { inputClass } from './formStyles'

type IconPickerDialogProps = {
  /** Der aktuell gewählte Name, oder leer für "kein Symbol". */
  value: string
  categoryName: string
  onSelect: (iconName: string) => void
  onCancel: () => void
}

export function IconPickerDialog({
  value,
  categoryName,
  onSelect,
  onCancel,
}: IconPickerDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const sucheRef = useRef<HTMLInputElement>(null)
  const [suche, setSuche] = useState('')

  const onCancelRef = useRef(onCancel)
  onCancelRef.current = onCancel

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return

    dialog.showModal()
    sucheRef.current?.focus()

    // Beide Wege, wie beim ConfirmDialog: React verdrahtet onCancel am <dialog>
    // nicht (das Ereignis blubbert nicht), und Chromes CloseWatcher springt
    // nicht bei jeder Eingabequelle an.
    const schliessen = () => onCancelRef.current()

    const onCancelEvent = (event: Event) => {
      event.preventDefault()
      schliessen()
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      event.preventDefault()
      schliessen()
    }

    dialog.addEventListener('cancel', onCancelEvent)
    dialog.addEventListener('keydown', onKeyDown)

    return () => {
      dialog.removeEventListener('cancel', onCancelEvent)
      dialog.removeEventListener('keydown', onKeyDown)
    }
  }, [])

  const gefiltert = useMemo(() => filterIcons(suche), [suche])

  const treffer = gefiltert.reduce((sum, group) => sum + group.eintraege.length, 0)

  return (
    <dialog
      ref={dialogRef}
      aria-label={`Symbol für ${categoryName} wählen`}
      className="m-auto w-full max-w-2xl rounded-2xl border border-slate-200 bg-white p-0 shadow-2xl backdrop:bg-slate-900/40"
    >
      <div className="flex max-h-[80vh] flex-col">
        <div className="border-b border-slate-200 p-5">
          <div className="flex items-center gap-3">
            <h2 className="text-lg font-bold text-slate-800">
              Symbol für „{categoryName}“
            </h2>
            <button
              type="button"
              onClick={onCancel}
              aria-label="Schließen"
              className="ml-auto rounded-lg p-2 text-slate-600 hover:bg-slate-100"
            >
              <X className="w-5 h-5" aria-hidden="true" />
            </button>
          </div>

          <div className="relative mt-3">
            <Search
              className="pointer-events-none absolute left-3 top-1/2 w-4 h-4 -translate-y-1/2 text-slate-400"
              aria-hidden="true"
            />
            <input
              ref={sucheRef}
              type="text"
              value={suche}
              onChange={(event) => setSuche(event.target.value)}
              placeholder={`Unter ${ICON_COUNT} Symbolen suchen — z. B. „schleife“ oder „konto“`}
              aria-label="Symbole durchsuchen"
              className={`${inputClass(false)} pl-9`}
            />
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-5">
          <button
            type="button"
            onClick={() => onSelect('')}
            className={`mb-5 flex w-full items-center gap-2 rounded-xl border px-3 py-2 text-sm font-medium transition-colors ${
              value === ''
                ? 'border-indigo-500 bg-indigo-50 text-indigo-900'
                : 'border-slate-200 text-slate-600 hover:bg-slate-50'
            }`}
          >
            <Ban className="w-4 h-4" aria-hidden="true" />
            Kein eigenes Symbol (Standard)
          </button>

          {gefiltert.map((group) => (
            <section key={group.titel} className="mb-5 last:mb-0">
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-wider text-slate-500">
                {group.titel}
              </h3>
              <div className="grid grid-cols-6 gap-2 sm:grid-cols-9">
                {group.eintraege.map((entry) => {
                  const Icon = entry.icon
                  const gewaehlt = entry.name === value

                  return (
                    <button
                      key={entry.name}
                      type="button"
                      onClick={() => onSelect(entry.name)}
                      // Der Name als Titel: beim Nachsehen in der Datenbank
                      // steht dort genau dieser Wert.
                      title={entry.name}
                      aria-label={entry.name}
                      aria-pressed={gewaehlt}
                      className={`flex aspect-square items-center justify-center rounded-xl border transition-colors ${
                        gewaehlt
                          ? 'border-indigo-500 bg-indigo-50 text-indigo-700'
                          : 'border-slate-200 text-slate-600 hover:border-slate-400 hover:bg-slate-50'
                      }`}
                    >
                      <Icon className="w-5 h-5" aria-hidden="true" />
                    </button>
                  )
                })}
              </div>
            </section>
          ))}

          {treffer === 0 && (
            <p className="py-8 text-center text-sm text-slate-500">
              Kein Symbol passt zu „{suche}“.
            </p>
          )}
        </div>
      </div>
    </dialog>
  )
}
