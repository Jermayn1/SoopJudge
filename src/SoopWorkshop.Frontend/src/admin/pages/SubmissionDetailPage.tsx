import { useEffect, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { AlertTriangle, ArrowLeft, FileCode2, Loader2 } from 'lucide-react'
import { CodeWindow } from '../../components/CodeWindow'
import { ResultView } from '../../components/ResultView'
import { useSubmissionPolling } from '../../hooks/useSubmissionPolling'
import { fetchSubmissionDetail, type SubmissionDetail } from '../api/submissions'
import { formatDateTime } from '../formatDateTime'

// Der Rahmen wie auf jeder anderen Seite der Verwaltung: eigener
// Scroll-Container, p-8, mittige Spalte.
//
// Das <main> im AdminLayout trägt overflow-hidden und scrollt selbst nicht.
// Eine Seite ohne diesen Rahmen wird deshalb an der Fensterkante abgeschnitten,
// und was darunter liegt — hier: die Code-Fenster — ist mit keiner Geste
// erreichbar. Als eigene Komponente, weil alle drei Zustände der Seite
// denselben Rahmen brauchen und ein dreifach abgeschriebener nach der ersten
// Änderung dreifach verschieden wäre.
function Seite({ children }: { children: ReactNode }) {
  return (
    <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
      <div className="mx-auto w-full max-w-4xl space-y-6">{children}</div>
    </div>
  )
}

type LoadError = {
  /** Getrennt gehalten, weil „gibt es nicht" und „nicht erreichbar" verschiedene
   *  Überschriften verdienen. Wer sie zusammenwirft, behauptet bei gestopptem
   *  Server, die Abgabe sei gelöscht. */
  missing: boolean
  message: string
}

// Eine einzelne Abgabe: die Bewertung, wie der Teilnehmer sie gesehen hat, und
// darunter der Quelltext, den er eingereicht hat.
//
// Die Bewertung kommt aus ResultView und dem Polling-Hook — denselben
// Bausteinen wie auf der Teilnehmerseite. Ein Nachbau liefe beim ersten Umbau
// auseinander, und es fiele erst auf, wenn hier etwas anderes stünde als beim
// Teilnehmer.
export function SubmissionDetailPage() {
  const { submissionId = '' } = useParams()

  const [detail, setDetail] = useState<SubmissionDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<LoadError | null>(null)

  const { phase } = useSubmissionPolling(submissionId)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    fetchSubmissionDetail(submissionId, controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return

        if (result.kind === 'ok') setDetail(result.value)
        else setError({ missing: result.kind === 'notFound', message: result.message })
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setError({ missing: false, message: 'Die Abgabe konnte nicht geladen werden.' })
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [submissionId])

  const zurueck = (
    <Link
      to="/admin/abgaben"
      className="group flex w-fit items-center gap-2 text-sm font-semibold text-slate-600 transition-colors hover:text-indigo-700"
    >
      <ArrowLeft
        className="h-4 w-4 transition-transform group-hover:-translate-x-0.5"
        aria-hidden="true"
      />
      Zurück zu den Abgaben
    </Link>
  )

  if (loading) {
    return (
      <Seite>
        {zurueck}
        <div className="flex items-center gap-2 text-slate-500">
          <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
          Wird geladen …
        </div>
      </Seite>
    )
  }

  if (error) {
    return (
      <Seite>
        {zurueck}
        <div className="flex items-start gap-3 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-rose-900">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" aria-hidden="true" />
          <div>
            <p className="font-medium">
              {error.missing
                ? 'Diese Abgabe gibt es nicht (mehr).'
                : 'Die Abgabe konnte nicht geladen werden.'}
            </p>
            {/* Der Grund des Servers im Wortlaut. */}
            <p className="text-sm break-words">{error.message}</p>
          </div>
        </div>
      </Seite>
    )
  }

  return (
    <Seite>
      {zurueck}

      <header>
        <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
          {detail?.categoryName}
        </p>
        <h1 className="text-2xl font-bold text-slate-800">{detail?.taskTitle}</h1>
        <p className="text-sm text-slate-500">
          Abgegeben am {formatDateTime(detail?.submittedAt ?? '')}
        </p>
      </header>

      {/* Der Name des Abschnitts steht als aria-label und nicht als
          sr-only-Überschrift. „sr-only" arbeitet mit position: absolute, und
          ein absolut gesetztes Kind ohne positionierten Vorfahren entkommt
          jedem overflow-hidden darüber — es macht das Dokument so hoch wie
          seine eigene Position und damit scrollbar. Ein aria-label hat gar
          kein Kästchen und kann das nicht. */}
      <section aria-label="Bewertung">

        {/* Warteschlange und laufende Prüfung sind verschiedene Zustände und
            bekommen verschiedene Texte — genau wie beim Teilnehmer. */}
        {(phase.kind === 'idle' || phase.kind === 'pending' || phase.kind === 'running') && (
          <div
            className="flex items-center gap-3 rounded-xl border border-slate-200 bg-slate-50 p-6"
            role="status"
            aria-live="polite"
          >
            <Loader2 className="h-5 w-5 shrink-0 animate-spin text-indigo-600" aria-hidden="true" />
            <p className="text-sm text-slate-700">
              {phase.kind === 'running'
                ? 'Wird gerade geprüft — kompilieren, Testfälle, Unit-Tests.'
                : 'In der Warteschlange.'}
            </p>
          </div>
        )}

        {phase.kind === 'failed' && (
          <div className="rounded-xl border border-rose-200 bg-rose-50 p-4" role="alert">
            <p className="font-semibold text-rose-800">Die Auswertung ist nicht durchgelaufen</p>
            {/* Der Grund des Servers im Wortlaut — kein stiller Fehlschlag. */}
            <p className="mt-2 text-sm break-words whitespace-pre-wrap text-rose-800">
              {phase.message}
            </p>
            <p className="mt-2 text-sm text-rose-800">
              Der abgegebene Code steht trotzdem darunter.
            </p>
          </div>
        )}

        {phase.kind === 'done' && (
          <div className="rounded-2xl border border-slate-200 bg-slate-50 p-6">
            <ResultView result={phase.result} />
          </div>
        )}
      </section>

      <section aria-labelledby="code-ueberschrift" className="space-y-4">
        <div className="flex items-center gap-2">
          <FileCode2 className="h-5 w-5 text-slate-500" aria-hidden="true" />
          <h2 id="code-ueberschrift" className="text-lg font-bold text-slate-800">
            Abgegebener Code
          </h2>
          {detail && detail.files.length > 0 && (
            <span className="text-sm text-slate-500 tabular-nums">
              {detail.files.length === 1 ? '1 Datei' : `${detail.files.length} Dateien`}
            </span>
          )}
        </div>

        {detail?.files.length === 0 ? (
          <p className="rounded-xl border border-slate-200 bg-white px-4 py-8 text-center text-slate-500">
            Zu dieser Abgabe sind keine Dateien gespeichert.
          </p>
        ) : (
          detail?.files.map((datei) => (
            <CodeWindow key={datei.fileName} fileName={datei.fileName} code={datei.content} />
          ))
        )}
      </section>
    </Seite>
  )
}
