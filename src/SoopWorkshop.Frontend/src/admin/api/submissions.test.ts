import { afterEach, describe, expect, it, vi } from 'vitest'
import { fetchSubmissionDetail } from './submissions'

function antwortet(status: number, body: unknown, contentType = 'application/json') {
  const response = new Response(typeof body === 'string' ? body : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(response))
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('fetchSubmissionDetail', () => {
  it('uebernimmt Kopfzeile und Dateien', async () => {
    antwortet(200, {
      id: 'a1',
      taskItemId: 't1',
      taskTitle: 'Bankkonto',
      categoryName: 'OOP',
      submittedAt: '2026-08-23T10:00:00Z',
      files: [{ fileName: 'Konto.java', content: 'class Konto {}' }],
    })

    const result = await fetchSubmissionDetail('a1')

    expect(result.kind).toBe('ok')
    if (result.kind !== 'ok') return

    expect(result.value.taskTitle).toBe('Bankkonto')
    expect(result.value.categoryName).toBe('OOP')
    expect(result.value.files).toEqual([{ fileName: 'Konto.java', content: 'class Konto {}' }])
  })

  // .NET gibt im OpenAPI-Dokument kein "required" aus, dort ist also jedes
  // Feld optional. Genau dafuer gibt es die Umsetzung.
  it('fuellt fehlende Felder, statt undefined durchzureichen', async () => {
    antwortet(200, {})

    const result = await fetchSubmissionDetail('a1')

    expect(result.kind).toBe('ok')
    if (result.kind !== 'ok') return

    expect(result.value.taskTitle).toBe('')
    expect(result.value.files).toEqual([])
  })

  // Sortieren ist billig, eine wechselnde Reihenfolge verwirrt.
  it('sortiert die Dateien nach Namen', async () => {
    antwortet(200, {
      files: [
        { fileName: 'Kunde.java', content: '' },
        { fileName: 'Bank.java', content: '' },
        { fileName: 'Konto.java', content: '' },
      ],
    })

    const result = await fetchSubmissionDetail('a1')

    expect(result.kind).toBe('ok')
    if (result.kind !== 'ok') return

    expect(result.value.files.map((f) => f.fileName)).toEqual([
      'Bank.java',
      'Konto.java',
      'Kunde.java',
    ])
  })

  // "Gibt es nicht" bleibt "gibt es nicht" - die Seite zeigt dafuer eine
  // andere Ueberschrift als bei einem stummen Server.
  it('reicht notFound unveraendert weiter', async () => {
    antwortet(404, 'Einreichung nicht gefunden.', 'text/plain')

    const result = await fetchSubmissionDetail('a1')

    expect(result).toEqual({ kind: 'notFound', message: 'Einreichung nicht gefunden.' })
  })
})
