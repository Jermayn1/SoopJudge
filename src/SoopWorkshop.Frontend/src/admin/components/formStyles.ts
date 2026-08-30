// Gemeinsames Aussehen und die Verknüpfung von Feld, Hinweis und Fehler.
//
// Bewusst eine eigene Datei ohne Komponenten: eine Datei, die beides
// exportiert, meldet der Linter (react/only-export-components), und die
// Klassenketten sollen ohnehin an genau einer Stelle stehen.

export function hintId(id: string): string {
  return `${id}-hinweis`
}

export function errorId(id: string): string {
  return `${id}-fehler`
}

// Ein Bedienelement muss auf seinen Hinweis UND seinen Fehler zeigen, sonst
// liest ein Screenreader nur eins von beidem vor. undefined statt eines leeren
// Strings, damit das Attribut ganz wegfällt.
export function describedBy(id: string, hasHint: boolean, hasError: boolean): string | undefined {
  const parts = [hasHint ? hintId(id) : null, hasError ? errorId(id) : null].filter(Boolean)
  return parts.length > 0 ? parts.join(' ') : undefined
}

export function inputClass(hasError: boolean): string {
  const base =
    'w-full rounded-xl border bg-white px-3 py-2 text-slate-900 transition-colors placeholder:text-slate-400 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500'

  // Der Fehler wird nicht nur über die Farbe angezeigt — darunter steht der
  // Satz, und das Feld trägt aria-invalid. Farbe allein wäre für jemanden
  // mit Rotschwäche keine Information.
  return hasError
    ? `${base} border-rose-300 hover:border-rose-400 focus:border-rose-500`
    : `${base} border-slate-300 hover:border-slate-400 focus:border-indigo-500`
}
