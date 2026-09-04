import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { SubmissionDetailPage } from './SubmissionDetailPage'

const ABGABE = '46837fb0-2af2-4a42-84db-b7f81cedfe8e'

const DETAIL = {
  id: ABGABE,
  taskItemId: 't1',
  taskTitle: 'Bankkonto',
  categoryName: 'OOP',
  submittedAt: '2026-08-23T09:34:04Z',
  files: [
    { fileName: 'Konto.java', content: 'public class Konto {}' },
    { fileName: 'Kunde.java', content: 'public class Kunde {}' },
  ],
}

const ERGEBNIS = {
  id: 'e1',
  submissionId: ABGABE,
  totalScore: 72,
  maxScore: 100,
  categoryResults: [
    {
      id: 'c1',
      category: 'CleanCode',
      passed: true,
      points: 15,
      maxPoints: 15,
      errorTip: '',
      testCaseResults: [
        {
          id: 'p1',
          description: 'Klassen-, Methoden- und Variablennamen kommen ohne Umlaute und ohne ß aus',
          input: '',
          expectedOutput: '',
          actualOutput: '',
          passed: true,
          order: 1,
        },
      ],
    },
  ],
}

// Stellt die drei Aufrufe nach, die die Seite macht: die Einzelansicht aus der
// Verwaltung und die beiden Endpunkte, aus denen der Polling-Hook den
// Auswertungsstand holt.
function serverAntwortet(overrides: { detailStatus?: number; detailBody?: unknown } = {}) {
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string) => {
      if (url.includes('/api/admin/submissions/')) {
        const status = overrides.detailStatus ?? 200
        return Promise.resolve(
          new Response(
            status === 200
              ? JSON.stringify(overrides.detailBody ?? DETAIL)
              : 'Einreichung nicht gefunden.',
            {
              status,
              headers: { 'Content-Type': status === 200 ? 'application/json' : 'text/plain' },
            },
          ),
        )
      }

      if (url.includes('/status')) {
        return Promise.resolve(
          new Response(JSON.stringify({ id: ABGABE, taskItemId: 't1', status: 'Done' }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        )
      }

      return Promise.resolve(
        new Response(JSON.stringify(ERGEBNIS), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      )
    }),
  )
}

function zeige() {
  return render(
    <MemoryRouter initialEntries={[`/admin/abgaben/${ABGABE}`]}>
      <Routes>
        <Route path="/admin/abgaben/:submissionId" element={<SubmissionDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('SubmissionDetailPage', () => {
  it('zeigt Kopfzeile, Bewertung und je ein Fenster pro Datei', async () => {
    serverAntwortet()
    zeige()

    expect(await screen.findByText('Bankkonto')).toBeInTheDocument()
    expect(screen.getByText('OOP')).toBeInTheDocument()

    // Die Bewertung kommt aus ResultView - derselben Komponente wie beim
    // Teilnehmer. Der Punktestand belegt, dass sie wirklich gerendert wird.
    expect(await screen.findByText('72')).toBeInTheDocument()
    expect(screen.getByText('Klassen-, Methoden- und Variablennamen kommen ohne Umlaute und ohne ß aus')).toBeInTheDocument()

    expect(screen.getByText('Konto.java')).toBeInTheDocument()
    expect(screen.getByText('Kunde.java')).toBeInTheDocument()
    expect(screen.getByText('2 Dateien')).toBeInTheDocument()
  })

  // "Gibt es nicht" und "Server antwortet nicht" verlangen verschiedene
  // Ueberschriften. Wer sie zusammenwirft, behauptet bei gestopptem Server,
  // die Abgabe sei geloescht.
  it('unterscheidet „gibt es nicht" von einem stummen Server', async () => {
    serverAntwortet({ detailStatus: 404 })
    zeige()

    expect(await screen.findByText('Diese Abgabe gibt es nicht (mehr).')).toBeInTheDocument()
    expect(screen.getByText('Einreichung nicht gefunden.')).toBeInTheDocument()
  })

  it('nennt bei unerreichbarem Server nicht die Abgabe als geloescht', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    zeige()

    await waitFor(() =>
      expect(screen.getByText('Die Abgabe konnte nicht geladen werden.')).toBeInTheDocument(),
    )
    expect(screen.queryByText('Diese Abgabe gibt es nicht (mehr).')).not.toBeInTheDocument()
  })

  // Der Fall, in dem der Code am meisten hilft: die Auswertung ist gar nicht
  // durchgelaufen, der Quelltext liegt aber vor.
  it('zeigt den Code auch ohne Dateien nicht als Fehler', async () => {
    serverAntwortet({ detailBody: { ...DETAIL, files: [] } })
    zeige()

    expect(
      await screen.findByText('Zu dieser Abgabe sind keine Dateien gespeichert.'),
    ).toBeInTheDocument()
  })
})
