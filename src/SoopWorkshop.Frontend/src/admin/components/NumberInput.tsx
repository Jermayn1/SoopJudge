import { useId } from 'react'
import { Field } from './Field'
import { describedBy, inputClass } from './formStyles'

type NumberInputProps = {
  label: string
  value: number
  onChange: (value: number) => void
  hint?: string
  error?: string
  min?: number
  disabled?: boolean
}

export function NumberInput({
  label,
  value,
  onChange,
  hint,
  error,
  min = 0,
  disabled,
}: NumberInputProps) {
  const id = useId()

  return (
    <Field id={id} label={label} hint={hint} error={error}>
      <input
        id={id}
        type="number"
        value={value}
        min={min}
        step={1}
        onChange={(event) => {
          // Ein leeres Feld liefert "" und damit NaN. Daraus 0 zu machen ist
          // ehrlicher als NaN durchzureichen — die Prüfung in validation.ts
          // sieht den Wert danach ohnehin.
          const parsed = Number.parseInt(event.target.value, 10)
          onChange(Number.isNaN(parsed) ? 0 : parsed)
        }}
        aria-describedby={describedBy(id, hint !== undefined, error !== undefined)}
        aria-invalid={error !== undefined}
        disabled={disabled}
        className={`${inputClass(error !== undefined)} tabular-nums`}
      />
    </Field>
  )
}
