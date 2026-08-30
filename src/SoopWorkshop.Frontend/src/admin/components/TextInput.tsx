import { useId } from 'react'
import { Field } from './Field'
import { describedBy, inputClass } from './formStyles'

type TextInputProps = {
  label: string
  value: string
  onChange: (value: string) => void
  hint?: string
  error?: string
  placeholder?: string
  maxLength?: number
  disabled?: boolean
}

export function TextInput({
  label,
  value,
  onChange,
  hint,
  error,
  placeholder,
  maxLength,
  disabled,
}: TextInputProps) {
  const id = useId()

  return (
    <Field id={id} label={label} hint={hint} error={error}>
      <input
        id={id}
        type="text"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        // Absichtlich KEIN maxLength am Element: der Browser würde die Eingabe
        // dann kommentarlos abschneiden. Die Grenze prüft validation.ts und
        // sagt, was zu viel ist.
        aria-describedby={describedBy(id, hint !== undefined, error !== undefined)}
        aria-invalid={error !== undefined}
        disabled={disabled}
        className={inputClass(error !== undefined)}
      />
      {maxLength !== undefined && (
        <p className="mt-1 text-right text-xs tabular-nums text-slate-500">
          {value.length} / {maxLength}
        </p>
      )}
    </Field>
  )
}
