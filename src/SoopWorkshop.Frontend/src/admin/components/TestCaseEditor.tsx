import { Plus, Trash2 } from 'lucide-react'
import { OrderButtons } from './OrderButtons'
import { TextArea } from './TextArea'
import { TextInput } from './TextInput'
import { FIELD_LIMITS } from '../validation'
import type { TaskTestDraft } from '../api/tasks'

type TestCaseEditorProps = {
  tests: TaskTestDraft[]
  onChange: (tests: TaskTestDraft[]) => void
}

export function TestCaseEditor({ tests, onChange }: TestCaseEditorProps) {
  const replace = (index: number, patch: Partial<TaskTestDraft>) =>
    onChange(tests.map((test, i) => (i === index ? { ...test, ...patch } : test)))

  const remove = (index: number) => onChange(tests.filter((_, i) => i !== index))

  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction
    if (target < 0 || target >= tests.length) return

    const next = [...tests]
    ;[next[index], next[target]] = [next[target], next[index]]
    onChange(next)
  }

  return (
    <div>
      <ul className="space-y-4">
        {tests.map((test, index) => (
          <li key={index} className="rounded-xl border border-slate-200 bg-slate-50 p-4">
            <div className="mb-3 flex items-center gap-2">
              <OrderButtons
                label={`Testfall ${index + 1}`}
                onUp={() => move(index, -1)}
                onDown={() => move(index, 1)}
                canMoveUp={index > 0}
                canMoveDown={index < tests.length - 1}
              />
              <span className="text-sm font-semibold text-slate-700">Testfall {index + 1}</span>
              <button
                type="button"
                onClick={() => remove(index)}
                aria-label={`Testfall ${index + 1} entfernen`}
                className="ml-auto rounded-lg p-2 text-rose-800 hover:bg-rose-100"
              >
                <Trash2 className="w-4 h-4" aria-hidden="true" />
              </button>
            </div>

            <div className="space-y-3">
              <TextInput
                label="Beschreibung"
                // Eine Aussage über die Abgabe, nicht über das Ergebnis des
                // Laufs. Ob sie zutrifft, sagt später das Häkchen daneben.
                hint="Was das Programm können muss — z. B. „Das Programm addiert zwei positive Zahlen“."
                value={test.description}
                onChange={(value) => replace(index, { description: value })}
                maxLength={FIELD_LIMITS.testDescription}
              />

              <div className="grid gap-3 sm:grid-cols-2">
                <TextArea
                  label="Eingabe"
                  hint="Was über die Konsole hereinkommt. Leer lassen, wenn es keine Eingabe gibt."
                  value={test.input}
                  onChange={(value) => replace(index, { input: value })}
                  rows={3}
                  mono
                />
                <TextArea
                  label="Erwartete Ausgabe"
                  value={test.expectedOutput}
                  onChange={(value) => replace(index, { expectedOutput: value })}
                  rows={3}
                  mono
                />
              </div>
            </div>
          </li>
        ))}
      </ul>

      {tests.length === 0 && (
        <p className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500">
          Noch kein Testfall angelegt.
        </p>
      )}

      <button
        type="button"
        onClick={() => onChange([...tests, { input: '', expectedOutput: '', description: '' }])}
        className="mt-4 flex items-center gap-1.5 rounded-xl border border-slate-300 px-3 py-1.5 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
      >
        <Plus className="w-4 h-4" aria-hidden="true" />
        Testfall hinzufügen
      </button>
    </div>
  )
}
