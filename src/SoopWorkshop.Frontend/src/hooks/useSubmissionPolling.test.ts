import { act, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useSubmissionPolling } from './useSubmissionPolling'
import { fetchEvaluationResult, fetchSubmissionState } from '../api/endpoints'
import type { EvaluationResult, SubmissionState, SubmissionStatus } from '../api/types'

vi.mock('../api/endpoints', () => ({
  fetchSubmissionState: vi.fn(),
  fetchEvaluationResult: vi.fn(),
}))

const stand = vi.mocked(fetchSubmissionState)
const ergebnis = vi.mocked(fetchEvaluationResult)

const ABGABE = '11111111-1111-1111-1111-111111111111'

function zustand(status: SubmissionStatus, errorMessage = ''): SubmissionState {
  return { id: ABGABE, taskItemId: 'aufgabe', status, submittedAt: '', errorMessage }
}

const ERGEBNIS: EvaluationResult = {
  id: 'e1',
  submissionId: ABGABE,
  totalScore: 100,
  maxScore: 100,
  categoryResults: [],
}

beforeEach(() => {
  vi.useFakeTimers()
  stand.mockReset()
  ergebnis.mockReset()
})

afterEach(() => {
  vi.useRealTimers()
})

// Zwischen zwei Abfragen liegen zwei Sekunden. Die Uhr muss innerhalb von act
// laufen, sonst meldet React die Zustandsänderung als nicht umschlossen.
async function warteEinIntervall() {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(2000)
  })
}

describe('useSubmissionPolling', () => {
  it('bleibt ohne Abgabe im Leerlauf und fragt gar nicht', () => {
    const { result } = renderHook(() => useSubmissionPolling(null))

    expect(result.current.phase).toEqual({ kind: 'idle' })
    expect(stand).not.toHaveBeenCalled()
  })

  // Sofort fragen, nicht erst nach dem ersten Intervall. Sonst zeigte die
  // Ergebnisseite zwei Sekunden lang "In der Warteschlange", obwohl die
  // Auswertung längst fertig ist - beim Aufruf eines geteilten Links ist das
  // der Normalfall, nicht die Ausnahme.
  it('fragt sofort und nicht erst nach zwei Sekunden', async () => {
    stand.mockResolvedValue({ kind: 'ok', value: zustand('Done') })
    ergebnis.mockResolvedValue({ kind: 'ok', value: ERGEBNIS })

    renderHook(() => useSubmissionPolling(ABGABE))

    await act(async () => {})

    expect(stand).toHaveBeenCalledTimes(1)
  })

  // Pending und Running sind verschiedene Zustände und tragen im Frontend
  // verschiedene Texte: "in der Warteschlange" ist etwas anderes als "wird
  // gerade geprüft".
  it('geht von Pending ueber Running nach Done', async () => {
    stand
      .mockResolvedValueOnce({ kind: 'ok', value: zustand('Pending') })
      .mockResolvedValueOnce({ kind: 'ok', value: zustand('Running') })
      .mockResolvedValue({ kind: 'ok', value: zustand('Done') })
    ergebnis.mockResolvedValue({ kind: 'ok', value: ERGEBNIS })

    const { result } = renderHook(() => useSubmissionPolling(ABGABE))

    await act(async () => {})
    expect(result.current.phase.kind).toBe('pending')

    await warteEinIntervall()
    expect(result.current.phase.kind).toBe('running')

    // Nach dem dritten Tick meldet der Stand Done; der Hook holt danach noch
    // das Ergebnis, also müssen die Microtasks einmal zusätzlich leerlaufen.
    await warteEinIntervall()
    await act(async () => {})

    expect(result.current.phase).toEqual({ kind: 'done', result: ERGEBNIS })
  })

  it('hoert nach Done auf zu fragen', async () => {
    stand.mockResolvedValue({ kind: 'ok', value: zustand('Done') })
    ergebnis.mockResolvedValue({ kind: 'ok', value: ERGEBNIS })

    renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    const bisher = stand.mock.calls.length
    await warteEinIntervall()
    await warteEinIntervall()

    expect(stand).toHaveBeenCalledTimes(bisher)
  })

  it('uebernimmt bei Failed die Meldung des Servers', async () => {
    stand.mockResolvedValue({
      kind: 'ok',
      value: zustand('Failed', 'Der Server wurde waehrend der Auswertung beendet.'),
    })

    const { result } = renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    expect(result.current.phase).toEqual({
      kind: 'failed',
      message: 'Der Server wurde waehrend der Auswertung beendet.',
    })
  })

  // Ein Failed ohne Meldung darf keine leere Fehlerkarte ergeben.
  it('faellt bei Failed ohne Meldung auf einen Standardsatz zurueck', async () => {
    stand.mockResolvedValue({ kind: 'ok', value: zustand('Failed') })

    const { result } = renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    expect(result.current.phase).toEqual({
      kind: 'failed',
      message: 'Die Auswertung ist fehlgeschlagen.',
    })
  })

  // "Gibt es nicht" und "nicht erreichbar" tragen verschiedene Meldungen; der
  // Hook reicht die des Clients durch, statt eine eigene zu erfinden.
  it.each([
    ['notFound' as const, 'Diese Abgabe gibt es nicht.'],
    ['unreachable' as const, 'Der Server ist nicht erreichbar.'],
  ])('reicht %s im Wortlaut durch', async (kind, message) => {
    stand.mockResolvedValue({ kind, message })

    const { result } = renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    expect(result.current.phase).toEqual({ kind: 'failed', message })
  })

  it('meldet einen Fehlschlag beim Abholen des Ergebnisses', async () => {
    stand.mockResolvedValue({ kind: 'ok', value: zustand('Done') })
    ergebnis.mockResolvedValue({ kind: 'rejected', message: 'Die Antwort war unlesbar.' })

    const { result } = renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    expect(result.current.phase).toEqual({ kind: 'failed', message: 'Die Antwort war unlesbar.' })
  })

  // Ohne Obergrenze dreht sich die Seite endlos, falls der Stand wider Erwarten
  // nie einen Endzustand erreicht. 150 Versuche sind rund fünf Minuten.
  it('bricht nach 150 Versuchen mit einer Erklaerung ab', async () => {
    stand.mockResolvedValue({ kind: 'ok', value: zustand('Running') })

    const { result } = renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    for (let i = 0; i < 150; i++) {
      await warteEinIntervall()
    }

    expect(result.current.phase.kind).toBe('failed')
    expect(result.current.phase.kind === 'failed' && result.current.phase.message).toContain(
      'lange',
    )
    expect(stand).toHaveBeenCalledTimes(150)
  })

  // Ein Abbruch beim Verlassen der Seite ist gewollt und kein Fehler - er darf
  // nicht als fehlgeschlagene Auswertung durchschlagen.
  it('schluckt den Abbruch beim Aufraeumen', async () => {
    stand.mockImplementation(() =>
      Promise.reject(new DOMException('The operation was aborted.', 'AbortError')),
    )

    const { result, unmount } = renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    unmount()

    expect(result.current.phase.kind).not.toBe('failed')
  })

  it('meldet einen unerwarteten Fehler als fehlgeschlagen', async () => {
    stand.mockRejectedValue(new TypeError('irgendetwas ganz anderes'))

    const { result } = renderHook(() => useSubmissionPolling(ABGABE))
    await act(async () => {})

    expect(result.current.phase.kind).toBe('failed')
    expect(result.current.phase.kind === 'failed' && result.current.phase.message).toContain(
      'konnte nicht abgerufen werden',
    )
  })

  it('faellt beim Wechsel auf keine Abgabe zurueck in den Leerlauf', async () => {
    stand.mockResolvedValue({ kind: 'ok', value: zustand('Running') })

    const { result, rerender } = renderHook(({ id }) => useSubmissionPolling(id), {
      initialProps: { id: ABGABE as string | null },
    })
    await act(async () => {})
    expect(result.current.phase.kind).toBe('running')

    rerender({ id: null })
    await act(async () => {})

    expect(result.current.phase).toEqual({ kind: 'idle' })
  })
})
