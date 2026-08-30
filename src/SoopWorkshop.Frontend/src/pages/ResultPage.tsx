import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { AlertTriangle, ArrowLeft, Clock, Loader2 } from 'lucide-react'
import { ResultView } from '../components/ResultView'
import { useSubmissionPolling } from '../hooks/useSubmissionPolling'
import { fetchSubmissionState } from '../api/endpoints'

function Waiting({ title, text }: { title: string; text: string }) {
  return (
    <div className="flex-1 flex items-center justify-center bg-slate-50 p-8">
      <div className="anim-auf max-w-md text-center" role="status" aria-live="polite">
        <Loader2
          className="w-10 h-10 text-indigo-600 animate-spin mx-auto mb-5"
          aria-hidden="true"
        />
        <h2 className="text-2xl font-bold text-slate-800 mb-2">{title}</h2>
        <p className="text-slate-600">{text}</p>
      </div>
    </div>
  )
}

export function ResultPage() {
  const { submissionId = '' } = useParams()
  const { phase } = useSubmissionPolling(submissionId)

  // Für den Zurück-Link zur richtigen Aufgabe. Die Id kommt auf dem Status mit,
  // damit auch ein direkt aufgerufener Ergebnis-Link einen Weg zurück hat — ohne
  // sie wäre der Link nur aus der Navigationshistorie ableitbar.
  const [taskId, setTaskId] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()
    fetchSubmissionState(submissionId, controller.signal)
      .then((result) => {
        if (result.kind === 'ok') setTaskId(result.value.taskItemId)
      })
      .catch(() => {
        /* Der Zurück-Link ist Beiwerk — sein Fehlschlag darf nichts stören. */
      })
    return () => controller.abort()
  }, [submissionId])

  const backLink = (
    <Link
      to={taskId ? `/aufgaben/${taskId}` : '/'}
      className="flex items-center gap-2 text-slate-600 text-sm font-semibold hover:text-indigo-700 transition-colors mb-8 group w-fit"
    >
      <ArrowLeft
        className="w-4 h-4 group-hover:-translate-x-0.5 transition-transform"
        aria-hidden="true"
      />
      {taskId ? 'Zurück zur Aufgabe' : 'Zur Aufgabenliste'}
    </Link>
  )

  // Warteschlange und Prüfung sind verschiedene Zustände und bekommen
  // verschiedene Texte — "wartet" ist etwas anderes als "wird geprüft".
  if (phase.kind === 'idle' || phase.kind === 'pending') {
    return (
      <Waiting
        title="In der Warteschlange"
        text="Deine Abgabe ist angekommen und wartet auf einen freien Platz."
      />
    )
  }

  if (phase.kind === 'running') {
    return (
      <Waiting
        title="Wird gerade geprüft"
        text="Kompilieren, Testfälle, Unit-Tests. Das dauert meist ein paar Sekunden."
      />
    )
  }

  if (phase.kind === 'failed') {
    return (
      <div className="flex-1 overflow-y-auto bg-slate-50 p-8">
        <div className="max-w-3xl mx-auto">
          {backLink}
          <div className="anim-auf rounded-2xl border border-rose-200 bg-white overflow-hidden shadow-sm">
            <div className="flex items-center gap-2 px-5 py-4 border-b border-rose-100 bg-rose-50">
              <AlertTriangle className="w-5 h-5 text-rose-700" aria-hidden="true" />
              <h2 className="font-bold text-slate-800">Die Auswertung ist nicht durchgelaufen</h2>
            </div>
            <div className="p-5 space-y-4">
              {/* Der Grund des Servers im Wortlaut — kein stiller Fehlschlag. */}
              <p className="whitespace-pre-wrap break-words text-slate-700">{phase.message}</p>
              <div className="flex items-center gap-2 text-sm text-slate-600">
                <Clock className="w-4 h-4" aria-hidden="true" />
                Du kannst die Aufgabe erneut abgeben.
              </div>
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 overflow-y-auto bg-slate-50">
      <div className="max-w-3xl mx-auto px-6 py-10 pb-24">
        {backLink}
        <ResultView result={phase.result} />
      </div>
    </div>
  )
}
