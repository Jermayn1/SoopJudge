import type { ReactNode } from 'react'
import { errorId, hintId } from './formStyles'

type FieldProps = {
  id: string
  label: string
  /** Steht immer da und erklärt das Feld — kein Ersatz für die Beschriftung. */
  hint?: string
  /** Steht nur bei einem Verstoß da, im Wortlaut aus validation.ts. */
  error?: string
  children: ReactNode
}

// Rahmen um ein Bedienelement: Beschriftung, Hinweis, Fehler. Die Bausteine
// darunter (TextInput, Select, …) benutzen ihn, damit die Verknüpfung über
// htmlFor und aria-describedby an genau einer Stelle stimmt.
export function Field({ id, label, hint, error, children }: FieldProps) {
  return (
    <div>
      <label htmlFor={id} className="block text-sm font-semibold text-slate-700">
        {label}
      </label>

      {hint && (
        <p id={hintId(id)} className="mt-0.5 text-xs text-slate-500">
          {hint}
        </p>
      )}

      <div className="mt-1.5">{children}</div>

      {error && (
        <p id={errorId(id)} role="alert" className="mt-1.5 text-sm font-medium text-rose-800">
          {error}
        </p>
      )}
    </div>
  )
}
