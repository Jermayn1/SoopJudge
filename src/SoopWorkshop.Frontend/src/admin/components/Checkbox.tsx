import { useId } from 'react'
import { describedBy, hintId } from './formStyles'

type CheckboxProps = {
  label: string
  checked: boolean
  onChange: (checked: boolean) => void
  hint?: string
  disabled?: boolean
}

// Nicht über Field gebaut: bei einem Kästchen steht die Beschriftung daneben
// und nicht darüber, und das ganze Paar soll anklickbar sein.
export function Checkbox({ label, checked, onChange, hint, disabled }: CheckboxProps) {
  const id = useId()

  return (
    <div className="flex gap-2.5">
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
        aria-describedby={describedBy(id, hint !== undefined, false)}
        disabled={disabled}
        className="mt-0.5 h-4 w-4 shrink-0 rounded border-slate-300 text-indigo-600 disabled:cursor-not-allowed"
      />
      <div>
        <label htmlFor={id} className="text-sm font-semibold text-slate-700">
          {label}
        </label>
        {hint && (
          <p id={hintId(id)} className="text-xs text-slate-500">
            {hint}
          </p>
        )}
      </div>
    </div>
  )
}
