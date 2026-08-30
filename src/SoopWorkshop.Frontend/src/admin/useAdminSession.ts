import { useCallback, useEffect, useState } from 'react'
import { fetchSession, login, logout } from './api/session'

// Vier Zustände, nicht zwei. "Nicht angemeldet" und "Server antwortet nicht"
// verlangen verschiedene Bildschirme: beim einen hilft das Passwort, beim
// anderen nur ein laufendes Backend. Wer beides zusammenwirft, zeigt bei
// gestopptem Server eine Anmeldemaske, die nie funktionieren kann.
export type AdminSessionState =
  | { kind: 'checking' }
  | { kind: 'anonymous' }
  | { kind: 'authenticated' }
  | { kind: 'unreachable'; message: string }

export function useAdminSession() {
  const [state, setState] = useState<AdminSessionState>({ kind: 'checking' })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState({ kind: 'checking' })

    fetchSession(controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return

        if (result.kind === 'ok') setState({ kind: 'authenticated' })
        else if (result.kind === 'unauthorized') setState({ kind: 'anonymous' })
        else setState({ kind: 'unreachable', message: result.message })
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setState({ kind: 'unreachable', message: 'Der Anmeldestatus konnte nicht geprüft werden.' })
      })

    return () => controller.abort()
  }, [attempt])

  // Erneut versuchen nach dem Muster aus AppLayout: ein Zähler in der
  // Abhängigkeitsliste stößt den Effekt an.
  const recheck = useCallback(() => setAttempt((n) => n + 1), [])

  // Liefert die Begründung des Servers zurück, oder null bei Erfolg. Die
  // Meldung kommt im Wortlaut aus der API — wie überall sonst auch.
  const signIn = useCallback(async (password: string): Promise<string | null> => {
    const result = await login(password)

    if (result.kind === 'ok') {
      setState({ kind: 'authenticated' })
      return null
    }

    return result.message
  }, [])

  const signOut = useCallback(async () => {
    // Das Ergebnis wird bewusst nicht geprüft: der Endpunkt antwortet auch
    // dann mit 204, wenn gar keine Sitzung mehr bestand. Und scheitert der
    // Aufruf am Netz, ist Abmelden trotzdem das, was der Nutzer wollte.
    await logout()
    setState({ kind: 'anonymous' })
  }, [])

  return { state, signIn, signOut, recheck }
}
