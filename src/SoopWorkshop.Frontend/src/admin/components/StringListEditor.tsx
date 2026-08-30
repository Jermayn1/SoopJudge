import { Plus, Trash2 } from 'lucide-react'
import { OrderButtons } from './OrderButtons'
import { inputClass } from './formStyles'

type StringListEditorProps = {
  label: string
  hint?: string
  /** Was ein einzelner Eintrag ist — landet in den Vorlesehilfen der Knöpfe. */
  itemNoun: string
  values: string[]
  onChange: (values: string[]) => void
  placeholder?: string
  mono?: boolean
  addLabel: string
}

// Liste von Zeichenketten mit Reihenfolge. Benutzt für die erwarteten
// Methodensignaturen und für die Tipps — beide sind fachlich dasselbe:
// eine geordnete Liste von Sätzen, bei der die Position zählt.
export function StringListEditor({
  label,
  hint,
  itemNoun,
  values,
  onChange,
  placeholder,
  mono,
  addLabel,
}: StringListEditorProps) {
  const replace = (index: number, value: string) =>
    onChange(values.map((current, i) => (i === index ? value : current)))

  const remove = (index: number) => onChange(values.filter((_, i) => i !== index))

  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction
    if (target < 0 || target >= values.length) return

    const next = [...values]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
  }

  return (
    <div>
      <p className="text-sm font-semibold text-slate-700">{label}</p>
      {hint && <p className="mt-0.5 text-xs text-slate-500">{hint}</p>}

      <ul className="mt-2 space-y-2">
        {values.map((value, index) => (
          // Der Index als Schlüssel ist hier richtig: die Einträge haben keine
          // eigene Identität, ihre Position IST ihre Bedeutung.
          <li key={index} className="flex items-start gap-2">
            <OrderButtons
              label={`${itemNoun} ${index + 1}`}
              onUp={() => move(index, -1)}
              onDown={() => move(index, 1)}
              canMoveUp={index > 0}
              canMoveDown={index < values.length - 1}
            />
            <input
              type="text"
              value={value}
              onChange={(event) => replace(index, event.target.value)}
              placeholder={placeholder}
              aria-label={`${itemNoun} ${index + 1}`}
              className={`${inputClass(false)} ${mono ? 'font-mono text-sm' : ''}`}
            />
            <button
              type="button"
              onClick={() => remove(index)}
              aria-label={`${itemNoun} ${index + 1} entfernen`}
              className="mt-1 shrink-0 rounded-lg p-2 text-rose-800 hover:bg-rose-50"
            >
              <Trash2 className="w-4 h-4" aria-hidden="true" />
            </button>
          </li>
        ))}
      </ul>

      <button
        type="button"
        onClick={() => onChange([...values, ''])}
        className="mt-2 flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
      >
        <Plus className="w-4 h-4" aria-hidden="true" />
        {addLabel}
      </button>
    </div>
  )
}
