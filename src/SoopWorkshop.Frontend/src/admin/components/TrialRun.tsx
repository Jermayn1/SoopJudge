import { useState } from 'react'
import { AlertTriangle, Loader2, RotateCcw } from 'lucide-react'
import { SubmissionForm } from '../../components/SubmissionForm'
import { ResultView } from '../../components/ResultView'
import { useSubmissionPolling } from '../../hooks/useSubmissionPolling'

type TrialRunProps = {
  taskItemId: string
}

// Eigene Musterlösung hochladen und die Bewertung sehen, ohne die Aufgabe
// sichtbar zu schalten.
//
// Das geht ohne neuen Endpunkt: die Abgabe-Kette prüft die Sichtbarkeit einer
// Aufgabe ohnehin nicht. Benutzt werden dieselben Bausteine wie beim
// Teilnehmer — SubmissionForm, useSubmissionPolling, ResultView —, damit hier
// nicht etwas anderes herauskommt als dort.
export function TrialRun({ taskItemId }: TrialRunProps) {
  const [submissionId, setSubmissionId] = useState<string | null>(null)
  const { phase } = useSubmissionPolling(submissionId)

  if (submissionId === null) {
    return (
      <div>
        <p className="mb-4 flex gap-2 rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
          <AlertTriangle className="w-4 h-4 shrink-0" aria-hidden="true" />
          Ein Probelauf erzeugt eine echte Abgabe in der Datenbank. Sie zählt damit auch bei der
          Warnung mit, die ein Import im Modus „Ersetzen“ anzeigt.
        </p>

        <SubmissionForm
          taskItemId={taskItemId}
          submitLabel="Probelauf starten"
          onSubmitted={setSubmissionId}
        />
      </div>
    )
  }

  const nochmal = (
    <button
      type="button"
      onClick={() => setSubmissionId(null)}
      className="flex items-center gap-2 rounded-xl border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-100"
    >
      <RotateCcw className="w-4 h-4" aria-hidden="true" />
      Noch ein Probelauf
    </button>
  )

  // Warteschlange und Prüfung sind verschiedene Zustände — "wartet" ist etwas
  // anderes als "wird geprüft".
  if (phase.kind === 'idle' || phase.kind === 'pending' || phase.kind === 'running') {
    return (
      <div className="flex items-center gap-3 rounded-xl border border-slate-200 bg-slate-50 p-6" role="status" aria-live="polite">
        <Loader2 className="w-5 h-5 shrink-0 animate-spin text-indigo-600" aria-hidden="true" />
        <p className="text-sm text-slate-700">
          {phase.kind === 'running'
            ? 'Wird gerade geprüft — kompilieren, Testfälle, Unit-Tests.'
            : 'In der Warteschlange.'}
        </p>
      </div>
    )
  }

  if (phase.kind === 'failed') {
    return (
      <div>
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4" role="alert">
          <p className="font-semibold text-rose-800">Die Auswertung ist nicht durchgelaufen</p>
          {/* Der Grund des Servers im Wortlaut — kein stiller Fehlschlag. */}
          <p className="mt-2 whitespace-pre-wrap break-words text-sm text-rose-800">{phase.message}</p>
        </div>
        <div className="mt-4">{nochmal}</div>
      </div>
    )
  }

  return (
    <div>
      <div className="rounded-2xl border border-slate-200 bg-slate-50 p-6">
        <ResultView result={phase.result} />
      </div>
      <div className="mt-4">{nochmal}</div>
    </div>
  )
}
