// Ein Zeitpunkt aus der API, deutsch geschrieben.
//
// Liegt außerhalb der Seiten, weil die Übersicht und die Einzelansicht ihn
// beide brauchen und derselbe Zeitpunkt in beiden gleich aussehen soll.
//
// Ein leerer oder unlesbarer Wert ergibt einen Gedankenstrich statt "Invalid
// Date": die Zeile bleibt damit lesbar und behauptet nichts.
export function formatDateTime(iso: string): string {
  if (!iso) return '—'

  const zeitpunkt = new Date(iso)
  if (Number.isNaN(zeitpunkt.getTime())) return '—'

  return zeitpunkt.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
