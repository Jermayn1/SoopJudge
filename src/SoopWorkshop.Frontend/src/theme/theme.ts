// Hell oder dunkel, als kleiner Speicher außerhalb von React.
//
// Die Wahrheit ist die Klasse "dark" auf <html>. Das Skript im <head> von
// index.html setzt sie, bevor React überhaupt lädt, und alles hier liest sie
// nur noch ab. Ein zweiter Zustand in React könnte davon abweichen, und dann
// stünde im Umschalter "hell", während die Seite dunkel ist.
//
// Außerhalb von React, weil zwei Umschalter gleichzeitig im DOM stehen können
// (Kopfzeile und aufgeklappte Seitenleiste). Beide hängen über
// useSyncExternalStore an diesem Speicher und können deshalb nicht
// auseinanderlaufen.

export type Theme = 'light' | 'dark'

// Steht wörtlich auch im Skript in index.html. theme.test.ts prüft, dass
// beide dasselbe entscheiden.
export const STORAGE_KEY = 'soop-judge.theme'

const SYSTEM_DARK_QUERY = '(prefers-color-scheme: dark)'

// Eine gespeicherte Wahl schlägt das System. Alles andere, auch ein
// unbekannter Wert von einer älteren Fassung, zählt als "nichts gewählt".
export function resolveTheme(stored: string | null, systemDark: boolean): Theme {
  if (stored === 'light' || stored === 'dark') return stored
  return systemDark ? 'dark' : 'light'
}

export function readStoredChoice(): Theme | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    return stored === 'light' || stored === 'dark' ? stored : null
  } catch (cause) {
    // Privates Fenster oder gesperrter Speicher. Dann gilt das System, und
    // das ist hier auch richtig so. Gemeldet wird es trotzdem.
    console.warn('Die Wahl hell/dunkel ist nicht lesbar, es gilt das System.', cause)
    return null
  }
}

function writeStoredChoice(theme: Theme): void {
  try {
    localStorage.setItem(STORAGE_KEY, theme)
  } catch (cause) {
    console.warn('Die Wahl hell/dunkel lässt sich nicht speichern, sie gilt nur bis zum Neuladen.', cause)
  }
}

function systemPrefersDark(): boolean {
  return window.matchMedia(SYSTEM_DARK_QUERY).matches
}

export function getTheme(): Theme {
  return document.documentElement.classList.contains('dark') ? 'dark' : 'light'
}

const listeners = new Set<() => void>()

function notify(): void {
  for (const listener of listeners) listener()
}

export function applyTheme(theme: Theme): void {
  const root = document.documentElement
  if (getTheme() === theme && root.style.colorScheme === theme) return

  // Übergänge aus, umschalten, einmal rechnen lassen, Übergänge wieder an.
  // Ohne das erzwungene Rechnen sähe der Browser alle drei Schritte als einen
  // an, und die Übergänge liefen doch.
  root.classList.add('theme-wechsel')
  root.classList.toggle('dark', theme === 'dark')
  root.style.colorScheme = theme
  void window.getComputedStyle(root).opacity
  root.classList.remove('theme-wechsel')

  notify()
}

export function setTheme(theme: Theme): void {
  writeStoredChoice(theme)
  applyTheme(theme)
}

// Solange nichts gespeichert ist, folgt die Seite dem Betriebssystem, auch
// wenn es abends von selbst auf dunkel geht.
function onSystemChange(): void {
  if (readStoredChoice() === null) applyTheme(resolveTheme(null, systemPrefersDark()))
}

// Ein anderer Tab hat umgeschaltet. key ist null, wenn dort der ganze
// Speicher geleert wurde; dann gilt wieder das System.
function onStorage(event: StorageEvent): void {
  if (event.key !== STORAGE_KEY && event.key !== null) return
  applyTheme(resolveTheme(readStoredChoice(), systemPrefersDark()))
}

let systemQuery: MediaQueryList | null = null

// Die Listener am Fenster hängen genau einmal, egal wie viele Umschalter es
// gibt. StrictMode abonniert beim Entwickeln doppelt; ohne den Zähler hinge
// dann jeder Listener zweimal.
export function subscribe(listener: () => void): () => void {
  listeners.add(listener)

  if (listeners.size === 1) {
    systemQuery = window.matchMedia(SYSTEM_DARK_QUERY)
    systemQuery.addEventListener('change', onSystemChange)
    window.addEventListener('storage', onStorage)
  }

  return () => {
    listeners.delete(listener)
    if (listeners.size > 0) return

    systemQuery?.removeEventListener('change', onSystemChange)
    systemQuery = null
    window.removeEventListener('storage', onStorage)
  }
}
