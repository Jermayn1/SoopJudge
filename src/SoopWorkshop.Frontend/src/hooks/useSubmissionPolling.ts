import { useCallback, useEffect, useRef, useState } from 'react'
import { fetchEvaluationResult, fetchSubmissionState } from '../api/endpoints'
import type { EvaluationResult } from '../api/types'

const INTERVAL_MS = 2000

// Nach etwa fünf Minuten wird abgebrochen. Ohne Obergrenze dreht sich die
// Seite endlos, falls der Status wider Erwarten nie einen Endzustand erreicht.
const MAX_ATTEMPTS = 150

export type PollingPhase =
  | { kind: 'idle' }
  /** In der Warteschlange — noch niemand hat die Abgabe angefasst. */
  | { kind: 'pending' }
  /** Wird gerade geprüft. Bewusst ein eigener Zustand: das ist etwas
   *  anderes als Warten, und der Teilnehmer soll den Unterschied sehen. */
  | { kind: 'running' }
  | { kind: 'done'; result: EvaluationResult }
  | { kind: 'failed'; message: string }

export function useSubmissionPolling(submissionId: string | null) {
  const [phase, setPhase] = useState<PollingPhase>({ kind: 'idle' })
  const attemptsRef = useRef(0)

  const restart = useCallback(() => {
    attemptsRef.current = 0
    setPhase({ kind: 'idle' })
  }, [])

  useEffect(() => {
    if (!submissionId) {
      setPhase({ kind: 'idle' })
      return
    }

    const controller = new AbortController()
    let timer: number | undefined
    let stopped = false

    attemptsRef.current = 0
    setPhase({ kind: 'pending' })

    const stop = () => {
      stopped = true
      if (timer !== undefined) window.clearTimeout(timer)
    }

    const tick = async () => {
      if (stopped) return

      if (++attemptsRef.current > MAX_ATTEMPTS) {
        setPhase({
          kind: 'failed',
          message:
            'Die Auswertung dauert ungewöhnlich lange. Lade die Seite neu oder reiche noch einmal ein.',
        })
        return
      }

      try {
        const state = await fetchSubmissionState(submissionId, controller.signal)
        if (stopped) return

        if (state.kind !== 'ok') {
          // "gibt es nicht" und "nicht erreichbar" tragen verschiedene
          // Meldungen — hier reicht die des Clients, sie ist bereits richtig.
          setPhase({ kind: 'failed', message: state.message })
          return
        }

        if (state.value.status === 'Failed') {
          setPhase({
            kind: 'failed',
            message: state.value.errorMessage || 'Die Auswertung ist fehlgeschlagen.',
          })
          return
        }

        if (state.value.status !== 'Done') {
          setPhase({ kind: state.value.status === 'Running' ? 'running' : 'pending' })
          timer = window.setTimeout(tick, INTERVAL_MS)
          return
        }

        const result = await fetchEvaluationResult(submissionId, controller.signal)
        if (stopped) return

        if (result.kind !== 'ok') {
          setPhase({ kind: 'failed', message: result.message })
          return
        }

        setPhase({ kind: 'done', result: result.value })
      } catch (error) {
        // Nur ein Abbruch landet hier — der ist gewollt und kein Fehler.
        if (error instanceof DOMException && error.name === 'AbortError') return
        if (stopped) return
        setPhase({
          kind: 'failed',
          message: 'Der Auswertungsstand konnte nicht abgerufen werden. Lade die Seite neu.',
        })
      }
    }

    // Sofort fragen, nicht erst nach dem ersten Intervall. Sonst zeigt die
    // Ergebnisseite zwei Sekunden lang "In der Warteschlange", obwohl die
    // Auswertung längst fertig ist — beim Aufruf eines geteilten Links
    // ist das der Normalfall, nicht die Ausnahme.
    void tick()

    return () => {
      stop()
      controller.abort()
    }
  }, [submissionId])

  return { phase, restart }
}
