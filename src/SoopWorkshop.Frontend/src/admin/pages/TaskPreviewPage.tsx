import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { AlertCircle, Eye, PencilLine, RefreshCw } from 'lucide-react'
import { TaskView } from '../../components/TaskView'
import { fetchAdminTask } from '../api/tasks'
import type { Task } from '../../api/types'

type LoadState =
  | { kind: 'loading' }
  | { kind: 'ok'; task: Task }
  | { kind: 'missing'; message: string }
  | { kind: 'unreachable'; message: string }

// Die Aufgabe so, wie ein Teilnehmer sie sieht — auch wenn sie noch verborgen ist.
//
// Zwei Dinge machen die Vorschau ehrlich, ohne dass hier etwas nachgebaut wird:
//
//  1. Sie benutzt dieselbe Komponente wie die Teilnehmersicht (TaskView).
//  2. Sie lädt über GET api/admin/tasks/{id}, und der liefert denselben
//     TaskItemDto wie der öffentliche Endpunkt — samt derselben Filterung auf
//     freigeschaltete JUnit-Dateien (TaskItemService.MapToDto). Eine hier
//     nachgebaute Filterung könnte davon abweichen; diese kann es nicht.
export function TaskPreviewPage() {
  const { taskId = '' } = useParams()

  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState({ kind: 'loading' })

    fetchAdminTask(taskId, controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return
        if (result.kind === 'ok') setState({ kind: 'ok', task: result.value })
        else if (result.kind === 'notFound') setState({ kind: 'missing', message: result.message })
        else setState({ kind: 'unreachable', message: result.message })
      })
      .catch((cause) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') return
        setState({ kind: 'unreachable', message: 'Die Aufgabe konnte nicht geladen werden.' })
      })

    return () => controller.abort()
  }, [taskId, attempt])

  const leiste = (
    // Deutlich gekennzeichnet: wer beim Hin- und Herspringen vergisst, wo er
    // ist, hält sonst die Vorschau für die echte Teilnehmersicht.
    <div className="sticky top-0 z-10 flex flex-wrap items-center gap-3 border-b border-amber-200 bg-amber-50 px-8 py-3 dark:border-amber-400/30 dark:bg-amber-950">
      <Eye className="w-4 h-4 shrink-0 text-amber-900 dark:text-amber-200" aria-hidden="true" />
      <span className="text-sm font-semibold text-amber-900 dark:text-amber-200">
        Vorschau — so sieht ein Teilnehmer die Aufgabe
      </span>
      <Link
        to={`/admin/aufgaben/${taskId}`}
        className="ml-auto flex items-center gap-1.5 rounded-lg border border-amber-300 px-3 py-1.5 text-sm font-semibold text-amber-900 transition-colors hover:bg-amber-100 dark:border-amber-400/40 dark:text-amber-200 dark:hover:bg-amber-500/15"
      >
        <PencilLine className="w-4 h-4" aria-hidden="true" />
        Zurück zum Bearbeiten
      </Link>
    </div>
  )

  if (state.kind === 'loading') {
    return (
      <div className="flex-1 overflow-y-auto bg-white dark:bg-neutral-950">
        {leiste}
        <div className="mx-auto w-full max-w-4xl space-y-6 p-8" aria-hidden="true">
          <div className="h-6 w-40 rounded-full bg-slate-200 animate-pulse dark:bg-neutral-800" />
          <div className="h-10 w-2/3 rounded bg-slate-200 animate-pulse dark:bg-neutral-800" />
          <div className="h-40 rounded-2xl bg-slate-100 animate-pulse dark:bg-neutral-800" />
        </div>
      </div>
    )
  }

  if (state.kind !== 'ok') {
    const unreachable = state.kind === 'unreachable'
    return (
      <div className="flex-1 overflow-y-auto bg-white dark:bg-neutral-950">
        {leiste}
        <div className="flex items-center justify-center p-8">
          <div className="max-w-md text-center">
            <div className="mx-auto mb-5 flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100 dark:bg-neutral-800">
              <AlertCircle className="w-8 h-8 text-slate-500 dark:text-neutral-400" aria-hidden="true" />
            </div>
            <h2 className="mb-2 text-2xl font-bold text-slate-800 dark:text-neutral-100">
              {unreachable ? 'Der Server antwortet nicht' : 'Diese Aufgabe gibt es nicht'}
            </h2>
            <p className="text-slate-600 dark:text-neutral-400">{state.message}</p>
            {unreachable && (
              <button
                type="button"
                onClick={() => setAttempt((n) => n + 1)}
                className="mt-5 inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 font-semibold text-white shadow-lg shadow-indigo-200 transition-all hover:bg-indigo-700 dark:shadow-black/30"
              >
                <RefreshCw className="w-4 h-4" aria-hidden="true" />
                Erneut versuchen
              </button>
            )}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 overflow-y-auto bg-white dark:bg-neutral-950">
      {leiste}
      <div className="mx-auto w-full max-w-4xl p-8 pb-20">
        <TaskView task={state.task} />

        {/* Der Abgabeteil fehlt bewusst: hochgeladen wird im Probelauf, direkt
            im Editor. Zwei Wege zum selben Ziel würden nur die Frage
            aufwerfen, welcher der richtige ist. */}
        <p className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600 dark:border-white/10 dark:bg-neutral-950/60 dark:text-neutral-400">
          Hier stünde für den Teilnehmer der Bereich zum Hochladen. Ausprobieren lässt sich die
          Bewertung im Editor unter <strong>Probelauf</strong>.
        </p>
      </div>
    </div>
  )
}
