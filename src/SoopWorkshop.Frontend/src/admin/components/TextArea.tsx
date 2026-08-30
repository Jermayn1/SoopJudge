import { useId } from 'react'
import { Field } from './Field'
import { describedBy, inputClass } from './formStyles'

type TextAreaProps = {
  label: string
  value: string
  onChange: (value: string) => void
  hint?: string
  error?: string
  placeholder?: string
  rows?: number
  /** Monospace für alles, was Code oder Konsolenausgabe ist. */
  mono?: boolean
  disabled?: boolean
}

export function TextArea({
  label,
  value,
  onChange,
  hint,
  error,
  placeholder,
  rows = 5,
  mono,
  disabled,
}: TextAreaProps) {
  const id = useId()

  return (
    <Field id={id} label={label} hint={hint} error={error}>
      <textarea
        id={id}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        rows={rows}
        aria-describedby={describedBy(id, hint !== undefined, error !== undefined)}
        aria-invalid={error !== undefined}
        disabled={disabled}
        // Konsolenein- und -ausgaben werden Zeichen für Zeichen verglichen.
        // Ein automatischer Umbruch im Feld würde vortäuschen, dort stünden
        // Zeilenumbrüche, die gar nicht im Wert stehen.
        spellCheck={!mono}
        className={`${inputClass(error !== undefined)} ${mono ? 'font-mono text-sm' : ''}`}
      />
    </Field>
  )
}
