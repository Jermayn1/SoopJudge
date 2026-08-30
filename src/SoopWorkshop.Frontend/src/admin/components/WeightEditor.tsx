import { NumberInput } from './NumberInput'
import { WEIGHTED_CATEGORIES, distributePoints, type WeightValues } from '../weights'

type WeightEditorProps = {
  values: WeightValues
  onChange: (values: WeightValues) => void
}

export function WeightEditor({ values, onChange }: WeightEditorProps) {
  const ordered = WEIGHTED_CATEGORIES.map((entry) => values[entry.category])
  const withAll = distributePoints(ordered)

  // Fällt Funktionalität weg — etwa weil eine Aufgabe gar keine Prüfung der
  // Funktionalität hat —, verteilt sich ihr Gewicht auf die übrigen. Aus
  // 15/20/65 wird dann 43/57. Genau das versteht ohne Anzeige niemand.
  const withoutFunctionality = distributePoints(ordered.slice(0, 2))

  const invalid = WEIGHTED_CATEGORIES.some((entry) => values[entry.category] <= 0)

  return (
    <div>
      <div className="grid gap-4 sm:grid-cols-3">
        {WEIGHTED_CATEGORIES.map((entry) => (
          <NumberInput
            key={entry.category}
            label={entry.label}
            value={values[entry.category]}
            min={1}
            onChange={(value) => onChange({ ...values, [entry.category]: value })}
            error={values[entry.category] <= 0 ? 'Das Gewicht muss größer als 0 sein.' : undefined}
          />
        ))}
      </div>

      <div className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
        <p className="text-sm font-semibold text-slate-700">Daraus ergeben sich diese Punkte</p>
        <p className="mt-0.5 text-xs text-slate-500">
          Gewichte sind kein Punktwert. Die erreichbaren Punkte entstehen erst durch die
          Normierung auf 100 — und nur über die Kategorien, die diese Aufgabe wirklich prüft.
        </p>

        {invalid ? (
          <p className="mt-3 text-sm text-slate-500">
            Erst wenn alle Gewichte größer als 0 sind.
          </p>
        ) : (
          <dl className="mt-3 space-y-2 text-sm">
            <div className="flex flex-wrap gap-x-4 gap-y-1">
              <dt className="w-full text-slate-600 sm:w-56">Mit allen drei Kategorien</dt>
              {WEIGHTED_CATEGORIES.map((entry, index) => (
                <dd key={entry.category} className="tabular-nums text-slate-800">
                  <span className="text-slate-500">{entry.label}</span> {withAll[index]}
                </dd>
              ))}
            </div>

            <div className="flex flex-wrap gap-x-4 gap-y-1">
              <dt className="w-full text-slate-600 sm:w-56">Ohne Prüfung der Funktionalität</dt>
              {WEIGHTED_CATEGORIES.slice(0, 2).map((entry, index) => (
                <dd key={entry.category} className="tabular-nums text-slate-800">
                  <span className="text-slate-500">{entry.label}</span>{' '}
                  {withoutFunctionality[index]}
                </dd>
              ))}
            </div>
          </dl>
        )}
      </div>
    </div>
  )
}
