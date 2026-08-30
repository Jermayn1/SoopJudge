import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ResultPage } from './ResultPage'
import { fetchSubmissionState } from '../api/endpoints'
import { useSubmissionPolling, type PollingPhase } from '../hooks/useSubmissionPolling'
import { auswertung, kategorie, teilpruefung } from '../test/fixtures'

vi.mock('../api/endpoints', () => ({ fetchSubmissionState: vi.fn() }))
vi.mock('../hooks/useSubmissionPolling', () => ({ useSubmissionPolling: vi.fn() }))

const stand = vi.mocked(fetchSubmissionState)
const polling = vi.mocked(useSubmissionPolling)

const ABGABE = '11111111-1111-1111-1111-111111111111'

function zeige(phase: PollingPhase) {
  polling.mockReturnValue({ phase, restart: vi.fn() })

  return render(
    <MemoryRouter initialEntries={[`/abgaben/${ABGABE}`]}>
      <Routes>
        <Route path="/abgaben/:submissionId" element={<ResultPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  stand.mockReset()
  polling.mockReset()

  // Die Seite fragt den Stand ein zweites Mal ab, nur für den Zurück-Link.
  stand.mockResolvedValue({
    kind: 'ok',
    value: {
      id: ABGABE,
      taskItemId: 'aufgabe-7',
      status: 'Done',
      submittedAt: '',
      errorMessage: '',
    },
  })
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('ResultPage', () => {
  // Warteschlange und Prüfung sind verschiedene Zustände. Wer sie
  // zusammenfasst, behauptet zwei Minuten lang, es passiere gerade etwas,
  // obwohl die Abgabe nur wartet.
  it('nennt die Warteschlange beim Namen', () => {
    zeige({ kind: 'pending' })

    expect(screen.getByText('In der Warteschlange')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveAttribute('aria-live', 'polite')
  })

  it('unterscheidet die laufende Pruefung davon', () => {
    zeige({ kind: 'running' })

    expect(screen.getByText('Wird gerade geprüft')).toBeInTheDocument()
    expect(screen.queryByText('In der Warteschlange')).not.toBeInTheDocument()
  })

  it('behandelt den Leerlauf wie die Warteschlange', () => {
    zeige({ kind: 'idle' })

    expect(screen.getByText('In der Warteschlange')).toBeInTheDocument()
  })

  // Kein stiller Fehlschlag: der Grund des Servers steht im Wortlaut da.
  it('zeigt bei einem Fehlschlag den Grund des Servers', () => {
    zeige({ kind: 'failed', message: 'Der Server ist nicht erreichbar.' })

    expect(screen.getByText('Die Auswertung ist nicht durchgelaufen')).toBeInTheDocument()
    expect(screen.getByText('Der Server ist nicht erreichbar.')).toBeInTheDocument()
  })

  it('zeigt bei Done das Ergebnis', () => {
    zeige({
      kind: 'done',
      result: auswertung({
        totalScore: 100,
        categoryResults: [
          kategorie({ category: 'CleanCode', testCaseResults: [teilpruefung({ passed: true })] }),
        ],
      }),
    })

    expect(screen.getByText('Hervorragende Arbeit!')).toBeInTheDocument()
    expect(screen.getByText('Clean Code')).toBeInTheDocument()
  })

  // Der Zurück-Link muss zur richtigen Aufgabe führen. Dafür trägt der Status
  // die TaskItemId - sonst landete ein direkt aufgerufener Ergebnis-Link im
  // Nichts, weil es keine Navigationshistorie gibt, aus der sich das ableiten
  // ließe.
  it('fuehrt zurueck zur richtigen Aufgabe', async () => {
    zeige({ kind: 'done', result: auswertung() })

    const link = await screen.findByRole('link', { name: /Zurück zur Aufgabe/ })
    expect(link).toHaveAttribute('href', '/aufgaben/aufgabe-7')
  })

  // Der Zurück-Link ist Beiwerk. Lässt sich die Aufgabe nicht ermitteln,
  // führt er zur Liste statt zu verschwinden.
  it('weicht auf die Aufgabenliste aus, wenn die Aufgabe unbekannt bleibt', async () => {
    stand.mockResolvedValue({ kind: 'notFound', message: 'Gibt es nicht.' })

    zeige({ kind: 'done', result: auswertung() })

    const link = await screen.findByRole('link', { name: /Zur Aufgabenliste/ })
    expect(link).toHaveAttribute('href', '/')
  })

  // Während des Wartens gibt es noch keinen Zurück-Link - die Seite besteht
  // dann nur aus der Statusmeldung.
  it('zeigt beim Warten keinen Zurueck-Link', () => {
    zeige({ kind: 'pending' })

    expect(screen.queryByRole('link')).not.toBeInTheDocument()
  })
})
