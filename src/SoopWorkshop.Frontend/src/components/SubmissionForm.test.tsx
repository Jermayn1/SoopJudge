import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { SubmissionForm } from './SubmissionForm'
import { createSubmission } from '../api/endpoints'

vi.mock('../api/endpoints', () => ({ createSubmission: vi.fn() }))

const absenden = vi.mocked(createSubmission)

beforeEach(() => {
  absenden.mockReset()
})

afterEach(() => {
  vi.restoreAllMocks()
})

function javaDatei(name: string, size = 100): File {
  const file = new File(['public class Konto {}'], name, { type: 'text/plain' })
  Object.defineProperty(file, 'size', { value: size })
  return file
}

// Der Datei-Input trägt className="hidden" und ist damit über keine Rolle
// erreichbar - der sichtbare Knopf löst ihn aus. Für den Test wird er direkt
// befüllt.
function dateiEingabe(container: HTMLElement) {
  const input = container.querySelector<HTMLInputElement>('input[type="file"]')
  if (!input) throw new Error('Kein Datei-Eingabefeld gefunden.')
  return input
}

// Der Ablagebereich hängt am Elternelement des Eingabefelds.
function fallenlassen(container: HTMLElement, ...dateien: File[]) {
  const bereich = dateiEingabe(container).parentElement
  if (!bereich) throw new Error('Kein Ablagebereich gefunden.')

  fireEvent.drop(bereich, { dataTransfer: { files: dateien } })
}

describe('SubmissionForm', () => {
  it('nennt die Grenzen, bevor jemand etwas falsch macht', () => {
    render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    const text = document.body.textContent ?? ''
    expect(text).toContain('.java')
    expect(text).toContain('10')
    expect(text).toContain('1,0 MB')
  })

  it('zeigt eine ausgewaehlte Datei mit Namen an', async () => {
    const user = userEvent.setup()
    const { container } = render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    await user.upload(dateiEingabe(container), javaDatei('Konto.java'))

    expect(screen.getByText('Konto.java')).toBeInTheDocument()
  })

  // Eine verworfene Datei darf nicht kommentarlos verschwinden. Im
  // Referenzprojekt kam hier ein alert() mit einem Satz für alle Fälle.
  //
  // Geprüft wird über Drag & Drop, und das ist kein Umweg: der Datei-Dialog
  // trägt accept=".java" und lässt eine .txt gar nicht erst durch. Beim
  // Fallenlassen greift accept nicht - das ist der Weg, auf dem eine falsche
  // Datei wirklich ankommt, und damit der Weg, den checkFiles absichern muss.
  it('nennt bei einer verworfenen Datei den Grund', () => {
    const { container } = render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    fallenlassen(container, javaDatei('notiz.txt'))

    expect(screen.getByText("„notiz.txt“ ist keine .java-Datei.")).toBeInTheDocument()
    expect(screen.queryByText('notiz.txt')).not.toBeInTheDocument()
  })

  it('behaelt beim Fallenlassen die gueltigen und verwirft nur die anderen', () => {
    const { container } = render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    fallenlassen(container, javaDatei('Konto.java'), javaDatei('notiz.txt'))

    expect(screen.getByText('Konto.java')).toBeInTheDocument()
    expect(screen.getByText("„notiz.txt“ ist keine .java-Datei.")).toBeInTheDocument()
  })

  it('laesst eine einzelne Datei wieder entfernen', async () => {
    const user = userEvent.setup()
    const { container } = render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    await user.upload(dateiEingabe(container), javaDatei('Konto.java'))
    await user.click(screen.getByRole('button', { name: 'Konto.java entfernen' }))

    expect(screen.queryByText('Konto.java')).not.toBeInTheDocument()
  })

  it('meldet die Abgabe und reicht die Id weiter', async () => {
    const user = userEvent.setup()
    const onSubmitted = vi.fn()
    absenden.mockResolvedValue({
      kind: 'ok',
      value: { id: 'abgabe-1', taskItemId: 'a1', submittedAt: '', status: 'Pending' },
    })

    const { container } = render(<SubmissionForm taskItemId="a1" onSubmitted={onSubmitted} />)

    await user.upload(dateiEingabe(container), javaDatei('Konto.java'))
    await user.click(screen.getByRole('button', { name: /prüfen/i }))

    await waitFor(() => expect(onSubmitted).toHaveBeenCalledWith('abgabe-1'))
  })

  // Die API antwortet auf Ablehnungen mit fertigen deutschen Sätzen als
  // Klartext. Sie werden im Wortlaut angezeigt und nicht durch eine eigene
  // Meldung ersetzt - nur der Server kennt den genauen Grund.
  it('zeigt die Ablehnung des Servers im Wortlaut', async () => {
    const user = userEvent.setup()
    absenden.mockResolvedValue({
      kind: 'rejected',
      message: '„Konto.java“ ist größer als 1024 KB.',
    })

    const { container } = render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    await user.upload(dateiEingabe(container), javaDatei('Konto.java'))
    await user.click(screen.getByRole('button', { name: /prüfen/i }))

    const meldung = await screen.findByRole('alert')
    expect(meldung).toHaveTextContent('„Konto.java“ ist größer als 1024 KB.')
  })

  it('meldet einen nicht erreichbaren Server als solchen', async () => {
    const user = userEvent.setup()
    absenden.mockResolvedValue({
      kind: 'unreachable',
      message: 'Der Server ist nicht erreichbar. Läuft das Backend?',
    })

    const { container } = render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    await user.upload(dateiEingabe(container), javaDatei('Konto.java'))
    await user.click(screen.getByRole('button', { name: /prüfen/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('nicht erreichbar')
  })

  it('sendet ohne ausgewaehlte Datei nichts ab', async () => {
    const user = userEvent.setup()
    render(<SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} />)

    const knopf = screen.getByRole('button', { name: /prüfen/i })
    expect(knopf).toBeDisabled()

    await user.click(knopf)
    expect(absenden).not.toHaveBeenCalled()
  })

  // Der Probelauf im Panel benutzt dieselbe Komponente, nur mit anderer
  // Beschriftung - eine zweite Auswahl würde beim ersten Umbau auseinanderlaufen.
  it('uebernimmt eine abweichende Beschriftung', () => {
    render(
      <SubmissionForm taskItemId="a1" onSubmitted={vi.fn()} submitLabel="Probelauf starten" />,
    )

    expect(screen.getByRole('button', { name: 'Probelauf starten' })).toBeInTheDocument()
  })
})
