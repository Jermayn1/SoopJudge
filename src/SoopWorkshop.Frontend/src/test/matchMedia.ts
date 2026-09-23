// jsdom kennt matchMedia nicht, also steht hier ein Ersatz.
//
// Je Anfrage gibt es genau ein Objekt, damit Listener daran hängen bleiben
// und setSystemDark sie auch erreicht. Mit einem frischen Objekt je Aufruf
// liefe ein "change" ins Leere.
//
// prefers-reduced-motion meldet true. Das ist Absicht und nicht bloß der
// bequemere Wert: damit setzt useCountUp in ResultView den Zielwert sofort,
// statt ihn über requestAnimationFrame hochzuzählen, und der Punktestand
// steht ohne Warten im DOM. Geprüft wird das Ergebnis der Animation, nicht
// ihr Ablauf.
//
// prefers-color-scheme: dark meldet dagegen false, sonst liefen alle Tests im
// Dunkelmodus. Die Theme-Tests stellen es über setSystemDark um.

type Listener = (event: MediaQueryListEvent) => void

let systemDark = false
const lists = new Map<string, MediaQueryList>()
const listenersByQuery = new Map<string, Set<Listener>>()

function isDarkQuery(query: string): boolean {
  return query.includes('prefers-color-scheme: dark')
}

function listFor(query: string): MediaQueryList {
  const existing = lists.get(query)
  if (existing) return existing

  const listeners = new Set<Listener>()
  listenersByQuery.set(query, listeners)

  const list = {
    get matches() {
      return isDarkQuery(query) ? systemDark : true
    },
    media: query,
    onchange: null,
    addListener: (listener: Listener) => listeners.add(listener),
    removeListener: (listener: Listener) => listeners.delete(listener),
    addEventListener: (_type: string, listener: Listener) => listeners.add(listener),
    removeEventListener: (_type: string, listener: Listener) => listeners.delete(listener),
    dispatchEvent: () => true,
  } as unknown as MediaQueryList

  lists.set(query, list)
  return list
}

export function installMatchMedia(): void {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: listFor,
  })
}

export function setSystemDark(dark: boolean): void {
  systemDark = dark
  for (const [query, listeners] of listenersByQuery) {
    if (!isDarkQuery(query)) continue
    const event = { matches: dark, media: query } as MediaQueryListEvent
    for (const listener of [...listeners]) listener(event)
  }
}

export function listenerCount(query: string): number {
  return listenersByQuery.get(query)?.size ?? 0
}

export function resetMatchMedia(): void {
  systemDark = false
  lists.clear()
  listenersByQuery.clear()
}
