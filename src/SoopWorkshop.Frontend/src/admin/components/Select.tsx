import { useId } from 'react'
import { Field } from './Field'
import { describedBy, inputClass } from './formStyles'

type SelectOption<T extends string> = {
  value: T
  label: string
}

type SelectProps<T extends string> = {
  label: string
  value: T
  options: readonly SelectOption<T>[]
  onChange: (value: T) => void
  hint?: string
  error?: string
  disabled?: boolean
}

// Über den Wertetyp generisch, damit onChange den Enum-Typ zurückgibt und
// nicht einen beliebigen string — sonst fällt ein Tippfehler in der
// Optionsliste erst zur Laufzeit auf.
export function Select<T extends string>({
  label,
  value,
  options,
  onChange,
  hint,
  error,
  disabled,
}: SelectProps<T>) {
  const id = useId()

  return (
    <Field id={id} label={label} hint={hint} error={error}>
      <select
        id={id}
        value={value}
        onChange={(event) => onChange(event.target.value as T)}
        aria-describedby={describedBy(id, hint !== undefined, error !== undefined)}
        aria-invalid={error !== undefined}
        disabled={disabled}
        className={inputClass(error !== undefined)}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  )
}
