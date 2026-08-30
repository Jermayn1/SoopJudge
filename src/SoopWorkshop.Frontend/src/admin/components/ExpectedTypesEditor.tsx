import { Plus, Trash2 } from 'lucide-react'
import { OrderButtons } from './OrderButtons'
import { StringListEditor } from './StringListEditor'
import { inputClass } from './formStyles'
import type { ExpectedTypeDraft } from '../api/tasks'

type ExpectedTypesEditorProps = {
  types: ExpectedTypeDraft[]
  onChange: (types: ExpectedTypeDraft[]) => void
}

// Der Vertrag als Baum: geforderte Klassen, und in jeder ihre Methoden.
//
// Vorher war das ein einzelnes Feld für den Klassennamen und daneben eine
// flache Liste von Signaturen. Für die OOP-Aufgaben am Ende des Workshops
// reicht das nicht: dort hängen mehrere Klassen voneinander ab, und die
// Bewertung soll wissen, dass 'einzahlen' zu 'Konto' gehört.
export function ExpectedTypesEditor({ types, onChange }: ExpectedTypesEditorProps) {
  const replace = (index: number, patch: Partial<ExpectedTypeDraft>) =>
    onChange(types.map((type, i) => (i === index ? { ...type, ...patch } : type)))

  const remove = (index: number) => onChange(types.filter((_, i) => i !== index))

  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction
    if (target < 0 || target >= types.length) return

    const next = [...types]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
  }

  return (
    <div>
      <ul className="space-y-4">
        {types.map((type, index) => (
          <li key={index} className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <div className="flex items-start gap-2">
              <OrderButtons
                label={`Klasse ${index + 1}`}
                onUp={() => move(index, -1)}
                onDown={() => move(index, 1)}
                canMoveUp={index > 0}
                canMoveDown={index < types.length - 1}
              />
              <input
                type="text"
                value={type.name}
                onChange={(event) => replace(index, { name: event.target.value })}
                placeholder="z. B. Konto"
                aria-label={`Name der geforderten Klasse ${index + 1}`}
                className={`${inputClass(false)} font-mono`}
              />
              <button
                type="button"
                onClick={() => remove(index)}
                aria-label={`Klasse ${index + 1} entfernen`}
                className="mt-1 shrink-0 rounded-lg p-2 text-rose-800 hover:bg-rose-100"
              >
                <Trash2 className="w-4 h-4" aria-hidden="true" />
              </button>
            </div>

            <div className="mt-4 border-l-2 border-slate-200 pl-4">
              <StringListEditor
                label={
                  type.name.trim().length > 0
                    ? `Methoden in ${type.name.trim()}`
                    : 'Methoden in dieser Klasse'
                }
                hint="Je Zeile eine Signatur, wie sie in der Aufgabenstellung steht. Geprüft wird der Name — und zwar im Rumpf genau dieser Klasse."
                itemNoun="Signatur"
                values={type.methods}
                onChange={(methods) => replace(index, { methods })}
                placeholder="public void einzahlen(double betrag)"
                mono
                addLabel="Signatur hinzufügen"
              />
            </div>
          </li>
        ))}
      </ul>

      {types.length === 0 && (
        <p className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
          Keine Klasse gefordert. Die Abgabe darf dann heißen, wie sie will.
        </p>
      )}

      <button
        type="button"
        onClick={() => onChange([...types, { name: '', methods: [] }])}
        className="mt-4 flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
      >
        <Plus className="w-4 h-4" aria-hidden="true" />
        Klasse hinzufügen
      </button>
    </div>
  )
}
